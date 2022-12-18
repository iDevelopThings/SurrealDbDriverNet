using Driver.Rpc.Request;
using Driver.Rpc.Response;

namespace Driver.Rpc.Handler;

internal interface IHandler : IDisposable
{
    public string Id { get; }

    public bool Persistent { get; }

    public void Handle(string id, IRpcResponse response);

    public bool RequiresCustomResponseParsing();

    public Task<(string id, IRpcResponse response)> Task { get; }
}

public class WebsocketResponseHandler : IHandler
{
    protected readonly TaskCompletionSource<(string id, IRpcResponse response)> tcs = new();
    protected readonly string                                                   id;
    protected readonly IRpcRequest                                              request;
    protected readonly CancellationToken                                        ct;

    protected bool requiresCustomProcessing = false;
    protected Type customResponseType       = null!;

    public WebsocketResponseHandler(IRpcRequest request, CancellationToken ct)
    {
        id           = request.Id;
        this.request = request;
        this.ct      = ct;
    }

    public Task<(string id, IRpcResponse response)> Task => tcs!.Task;

    public string Id => id;

    public bool Persistent => false;

    public void Handle(string id, IRpcResponse response)
    {
        response!.RawJson = response.RawJson;
        response!.Request = request;

        tcs.SetResult((id, response));
    }

    public void Dispose()
    {
        tcs.TrySetCanceled();
    }

    public bool RequiresCustomResponseParsing() => requiresCustomProcessing;

    public void UseCustomResponseHandler(Type responseType)
    {
        requiresCustomProcessing = true;
        customResponseType       = responseType;
    }
}