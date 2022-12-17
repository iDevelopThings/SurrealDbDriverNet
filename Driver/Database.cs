using System.Reflection;
using Driver.Models;
using Driver.Rpc;
using Driver.Rpc.Request;
using Driver.Rpc.Response;
using IocContainer;
using Driver.Models.Utils;

namespace Driver;

public class Database
{
    private static bool _isInitialized = false;

    private static Driver                 _driver = null!;
    private static IDatabaseConfiguration _config = null!;

    static Database()
    {
        ModelDriver.RegisterAllModels();
        Container.RegisterSingleton<IDatabaseConfiguration, DatabaseConfiguration>();
        Container.RegisterSingleton<Driver>();
        Container.RegisterSingleton<RpcConnection>();
        Container.RegisterSingleton<Database>();
    }

    public Database(IDatabaseConfiguration configuration, Driver driver)
    {
        _config = configuration;
        _driver = driver;
    }

    public static void Initialize()
    {
        Container.Resolve<Database>();
    }

    public static async Task Connect()
    {
        if (_isInitialized) return;

        await _driver.Connect();

        if (!await _driver.Signin(new() {Username = _config.AuthUsername!, Password = _config.AuthPassword!})) {
            throw new Exception("Failed to sign in to database");
        }

        await _driver.Use(new() {Database = _config.DatabaseName!, Namespace = _config.Namespace!});

        _isInitialized = true;
    }

    public static async Task<TModel?> Create<TModel>(TModel obj) where TModel : class, ISurrealModel
    {
        var response = await _driver.Send<DbQueryResponse<TModel?>>(
            new RpcRequest("create", new() {
                obj.Id is {HasKey: true} ? obj.Id : ModelUtils.GetTableName<TModel>(),
                obj
            })
        );
        if (response == null) {
            throw new Exception("Response is null");
        }

        if (response.IsError()) {
            return default;
        }

        response.ParseResponse();

        return response == null! ? default : response.First();
    }

    public static async Task<TModel?> Insert<TModel>(TModel obj) where TModel : class, ISurrealModel
    {
        var result = await _driver.Query<TModel>($"INSERT INTO {obj.GetTableName()} $obj", new() {{"obj", obj}});

        return result == null ? default : result.First();
    }

    public static async Task<List<TModel?>> Insert<TModel>(IEnumerable<TModel> objs) where TModel : class, ISurrealModel
    {
        var grouped      = objs.GroupBy(x => x.GetTableName()).ToDictionary(x => x.Key, x => x.ToList());
        var resultModels = new Dictionary<string, List<TModel>>();

        foreach (var (table, models) in grouped) {
            if (!resultModels.ContainsKey(table)) {
                resultModels.Add(table, new());
            }

            var result = await _driver.Query<TModel>($"INSERT INTO {table} $objs", new() {{"objs", models}});
            if (result != null!) {
                resultModels[table].AddRange(result.Get()!);
            }
        }

        return resultModels.SelectMany(x => x.Value).ToList()!;
    }

    public static async Task<DbQueryResponse<object?>?> Query(string query, Dictionary<string, object?>? vars = null)
    {
        return await _driver.Query(query, vars);
    }

    public static async Task<DbQueryResponse<T?>> Query<T>(string query, Dictionary<string, object?>? vars = null)
    {
        return await _driver.Query<T>(query, vars);
    }

    public static void Configure(Action<IDatabaseConfiguration> cb)
    {
        cb(Container.Resolve<IDatabaseConfiguration>());
    }

    public static bool IsConnected()
    {
        return (_driver != null!) && _driver.IsConnected();
    }

    public static async Task Disconnect()
    {
        if (_driver != null!) {
            await _driver.Disconnect();
            _isInitialized = false;
        }
    }

    public static Driver GetDriver() => _driver;
}