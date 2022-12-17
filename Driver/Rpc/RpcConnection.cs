using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using Driver.Json;
using Driver.Rpc.Handler;
using Driver.Rpc.Request;
using Driver.Rpc.Response;
using Microsoft.IO;
using Newtonsoft.Json;

namespace Driver.Rpc;

public class RpcConnection
{
    public static int DefaultBufferSize => 16 * 1024;

    private static readonly Lazy<RecyclableMemoryStreamManager>    SManager  = new(static () => new());
    private readonly        ConcurrentDictionary<string, IHandler> _handlers = new();

    private readonly IDatabaseConfiguration  _config;
    private readonly CancellationTokenSource _cts = new();
    private          ClientWebSocket         _ws;
    private          Task                    _receiver = Task.CompletedTask;

    private bool _logRpcResponses = false;
    private bool _logRpcRequests  = false;

    public RpcConnection(IDatabaseConfiguration config)
    {
        _config = config;
        _ws     = new ClientWebSocket();
#if DB_LOG_RPC_RESPONSES
        _logRpcResponses = true;
#endif
#if DB_LOG_RPC_REQUESTS
        _logRpcRequests = true;
#endif
    }

    public async Task Open()
    {
        await _ws.ConnectAsync(_config.RpcUri, CancellationToken.None);

        _receiver = Task.Run(async () => await Receive(_cts.Token), _cts.Token);
    }

    public async Task Receive(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) {
            var owner    = MemoryPool<byte>.Shared.Rent(DefaultBufferSize);
            var response = await _ws.ReceiveAsync(owner.Memory, stoppingToken);

            if (response.Count <= 0) {
                return;
            }

            var buffer = owner.Memory.Slice(0, response.Count).ToArray();

            var rawJson = Encoding.UTF8.GetString(buffer);

            if (_logRpcResponses)
                Console.WriteLine("(RPC) Received: " + rawJson);


            var responseObj = DbJson.Deserialize<BasicRpcResponse>(rawJson);
            responseObj.RawJson = rawJson;

            if (responseObj.IsError()) {
                responseObj.FinalErrorHandling();
            }

            if (!_handlers.TryGetValue(responseObj.Id!, out var handler)) {
                continue;
            }

            if (handler.RequiresCustomResponseParsing()) {
                ((ICustomHandler) handler).HandleCustom(responseObj.Id!, responseObj);
            } else {
                handler.Handle(responseObj.Id!, responseObj);
            }

            if (!handler.Persistent) {
                UnregisterHandler(handler);
            }
        }
    }

    private async Task<(string id, IRpcResponse response)> RequestOnce(IRpcRequest req, CancellationToken ct = default, Type? responseType = null)
    {
        if (_logRpcRequests)
            Console.WriteLine("(RPC) Sent: " + DbJson.Serialize(req, Formatting.Indented));

        await using RecyclableMemoryStream stream = new(SManager.Value);

        var serializer = DbJson.Create(); //JsonSerializer.Create(new JsonSerializerSettings() { });

        await using var writer = new StreamWriter(stream);
        serializer.Serialize(writer, req);
        await writer.FlushAsync();

        stream.Position = 0;

        if (stream is MemoryStream ms && ms.TryGetBuffer(out var raw)) {
            await _ws.SendAsync(raw, WebSocketMessageType.Text, true, _cts.Token);
        } else {
            throw new Exception("wat");
        }

        IHandler? handler = null;
        handler = responseType != null
            ? new WebsocketCustomResponseHandler(req, ct, responseType)
            : new WebsocketResponseHandler(req, ct);

        RegisterHandler(handler);

        return await handler.Task;
    }

    public async Task<T?> Send<T>(IRpcRequest req) where T : class
    {
        var (id, response) = await RequestOnce(req, _cts.Token, typeof(T));
        return response as T;
    }

    public async Task<IRpcResponse> Send(IRpcRequest req)
    {
        var (id, response) = await RequestOnce(req, _cts.Token);
        return response;
    }

    private async Task CloseWs()
    {
        if (_ws.State == WebSocketState.Closed) {
            return;
        }

        try {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "client disconnect", _cts.Token);
        }
        catch (TaskCanceledException) {
            // ignore
        }
        catch (OperationCanceledException) {
            if (_cts.Token.IsCancellationRequested) {
                return;
            }
        }
    }

    public async Task Close()
    {
        var t1 = CloseWs();
        var t2 = Task.Run(ClearHandlers, _cts.Token);
        _cts.Cancel();

        await t1;
        await t2;
    }

    private void RegisterHandler(IHandler handler)
    {
        if (!_handlers.TryAdd(handler.Id, handler)) {
            ThrowDuplicateId(handler.Id);
        }
    }

    private void UnregisterHandler(IHandler handler)
    {
        if (!_handlers.TryRemove(handler.Id, out var h)) {
            return;
        }

        h.Dispose();
    }

    private void ClearHandlers()
    {
        foreach (var handler in _handlers.Values) {
            UnregisterHandler(handler);
        }
    }

    [DoesNotReturn]
    private static void ThrowDuplicateId(string id)
    {
        throw new ArgumentOutOfRangeException(nameof(id), $"A request with the Id `{id}` is already registered");
    }

    public bool IsConnected()
    {
        return _ws.State == WebSocketState.Open;
    }

    public void SetLoggingState(bool state)
    {
        _logRpcRequests  = state;
        _logRpcResponses = state;
    }
}