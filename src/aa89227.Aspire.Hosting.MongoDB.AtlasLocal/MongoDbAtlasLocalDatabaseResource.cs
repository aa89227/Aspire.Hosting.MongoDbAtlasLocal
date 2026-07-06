namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// MongoDB Atlas Local 中的資料庫子資源，繼承父容器的連線字串。
/// </summary>
public sealed class MongoDbAtlasLocalDatabaseResource(
    string name,
    string databaseName,
    MongoDbAtlasLocalResource parent)
    : Resource(name), IResourceWithConnectionString,
        IResourceWithParent<MongoDbAtlasLocalResource>
{
    public MongoDbAtlasLocalResource Parent => parent;

    public string DatabaseName { get; } = databaseName;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{Parent}/{DatabaseName}?directConnection=true&authSource=admin");
}
