
namespace Driver.Models;

public partial class SurrealModel<TModel> : ISurrealModel
    where TModel : class, ISurrealModel
{

    public static async Task<TModel> Create(TModel model)
    {
        var finalModel = await Database.Insert(model);

        return finalModel!;
    }

}