using Driver.Rpc;
using Driver.Rpc.Request;
using Driver.Rpc.Response;
using Driver.Schema;

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

    public async Task<DbInfoResponse?> GetDbInfo()
    {
        var result = await Query<DbInfoResponse>("INFO FOR DB;", new() { });
        if (result.IsError()) {
            return null;
        }

        var info = result.First();
        return info;
    }

    public async Task<DbTableInfoResponse?> GetTableInfo(string name)
    {
        var result = await Query<DbTableInfoResponse>($"INFO FOR TABLE {name};", new() { });
        if (result.IsError()) {
            return null;
        }

        var info = result.First();

        return info;
    }

    /*public async Task<Dictionary<string, DbTableInfoResponse?>> GetTablesInfo(params string[] names)
    {
        var qparams = new Dictionary<string, object?>();
        var query   = new List<string>();

        for (var i = 0; i < names.Length; i++) {
            var name = names[i];
            query.Add($"INFO FOR TABLE $name{i};");
            qparams.Add($"name{i}", name);
        }

        var result = await Query<DbTableInfoResponse>(string.Join(Environment.NewLine, query), qparams);

        // var result = await Query<DbTableInfoResponse>("INFO FOR TABLE $name;", new() {{"name", name}});
        if (result.IsError()) {
            return null!;
        }

        var info       = new Dictionary<string, DbTableInfoResponse?>();
        var tableInfos = result.Get();

        for (var i = 0; i < names.Length; i++) {
            var name = names[i];
            info.Add(name, tableInfos[i]);
        }

        return info;
    }*/

    public async Task<DbQueryResponse<object?>?> Query(string query, Dictionary<string, object?>? vars)
        => await Query<object?>(query, vars);

    public async Task<DbQueryResponse<object?>?> Query(List<string> query, Dictionary<string, object?>? vars)
        => await Query<object?>(query, vars);

    public async Task<DbQueryResponse<T?>> Query<T>(string query, Dictionary<string, object?>? vars)
        => await Query<T?>(new List<string> {query}, vars);

    public async Task<DbQueryResponse<T?>> Query<T>(List<string> query, Dictionary<string, object?>? vars)
    {
        vars ??= new Dictionary<string, object?>();

        var request  = new DbQueryRequest(query, vars);
        var response = await _connection.Send<DbQueryResponse<T?>>(request);
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

    public async Task<DatabaseSchema?> LoadSchema()
    {
        var dbInfo = await GetDbInfo();
        if (dbInfo == null!) {
            return null;
        }

        var schema = new DatabaseSchema();
        // var addTableTasks = new List<Task<DatabaseTable>>();

        foreach (var (tableName, tableDefineString) in dbInfo.Tables) {
            await schema.AddTable(tableName, tableDefineString);
        }

        return schema;
    }
}