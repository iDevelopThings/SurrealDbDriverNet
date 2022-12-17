using Driver.Models.Types;

namespace Driver.Models.Utils;

public class ModelData<TModel>
    where TModel : class, ISurrealModel
{

    public static string GetTableName() => ModelUtils.GetTableName<TModel>();

    public static Thing GetTableThing() => Thing.From<TModel>("");


}