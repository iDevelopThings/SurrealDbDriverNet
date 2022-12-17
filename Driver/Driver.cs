using Driver.Rpc;
using Driver.Rpc.Request;
using Driver.Rpc.Response;

namespace Driver;

public class Driver
{
    private RpcConnection _connection = null!;

    public Driver(RpcConnection connection)
    {
        _connection = connection;
    }

    public void SetLoggingState(bool state) => _connection.SetLoggingState(state);

    public async Task Connect() => await _connection.Open();

    public async Task Disconnect()
    {
        await _connection.Close();
    }

    public bool IsConnected() => _connection.IsConnected();

    public async Task<bool> Signin(SigninRequest data)
    {
        // {"id":"axQKgyjB","method":"signin","params":[{"user":"root","pass":"root"}]}
        var response = await _connection.Send(new RpcRequest("signin", new() {data}));

        return !response.IsError();
    }

    public async Task<bool> Use(UseRequest data)
    {
        var response = await _connection.Send(new RpcRequest("use", new() {data.Database, data.Namespace}));

        return !response.IsError();
    }

    public async Task<DbQueryResponse<object?>?> Query(string query, Dictionary<string, object?>? vars)
        => await Query<object?>(query, vars);

    public async Task<DbQueryResponse<T?>> Query<T>(string query, Dictionary<string, object?>? vars)
    {
        vars ??= new Dictionary<string, object?>();

        var response = await _connection.Send<DbQueryResponse<T?>>(new DbQueryRequest(query, vars));
        if (response == null) {
            throw new Exception("Response is null");
        }

        if (response.IsError()) {
            return response;
        }

        response.ParseResponse();

        return response;
    }

    public async Task<T?> Send<T>(IRpcRequest req) where T : class
        => await _connection.Send<T>(req);

    public async Task<IRpcResponse> Send(IRpcRequest req)
        => await _connection.Send(req);

    public RpcConnection GetConnection() => _connection;
}