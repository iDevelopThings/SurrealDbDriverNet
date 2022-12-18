using Driver.Json;
using Driver.Rpc.Request;
using Driver.Rpc.Response;

namespace Driver.Rpc.Handler;

internal interface ICustomHandler : IHandler
{
    public void HandleCustom(string id, object response);
}

public class WebsocketCustomResponseHandler : WebsocketResponseHandler, ICustomHandler
{
    public WebsocketCustomResponseHandler(IRpcRequest request, CancellationToken ct, Type responseType) : base(request, ct)
    {
        requiresCustomProcessing = true;
        customResponseType       = responseType;
    }

    public void HandleCustom(string id, object response)
    {
        var rpcRes            = (BasicRpcResponse) response;
        var deserializeObject = DbJson.Deserialize(rpcRes.RawJson, customResponseType) as IRpcResponseExtended;
        deserializeObject!.RawJson = rpcRes.RawJson;
        deserializeObject!.Request = request;

        tcs.SetResult((id, deserializeObject)!);
    }
}