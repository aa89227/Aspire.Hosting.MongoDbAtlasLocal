using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// MongoDB Atlas Local 容器映像的預設值（仿 Aspire 內建元件的 ContainerImageTags 慣例）。
/// 預設使用 latest；需要固定版本時用標準的 <c>WithImageTag("8.2.6")</c> 覆寫。
/// </summary>
public static class MongoDbAtlasLocalContainerImageTags
{
    public const string Image = "mongodb/mongodb-atlas-local";
    public const string Tag = "latest";
}

/// <summary>
/// 用於註冊與設定 <see cref="MongoDbAtlasLocalResource"/> 的擴充方法。
/// </summary>
public static class MongoDbAtlasLocalExtensions
{
    /// <summary>
    /// 將 MongoDB Atlas Local 容器加入分散式應用程式模型。
    /// </summary>
    public static IResourceBuilder<MongoDbAtlasLocalResource> AddMongoDbAtlasLocal(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        var passwordParameter = password?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
                builder, $"{name}-password", special: false);

        var resource = new MongoDbAtlasLocalResource(name, userName?.Resource, passwordParameter);

        return builder.AddResource(resource)
            .WithImage(MongoDbAtlasLocalContainerImageTags.Image)
            .WithImageTag(MongoDbAtlasLocalContainerImageTags.Tag)
            // atlas-local 以容器 hostname 作為 replica set 名稱並持久化到 /data/db；
            // 固定 hostname 才能讓資料 volume 在容器重建（hostname 改變）後仍可啟動，
            // 否則 replSet 名稱不符會卡在 RSGhost 導致容器 panic。
            .WithContainerRuntimeArgs("--hostname", $"{name}-atlas-local")
            .WithEndpoint(port: port, targetPort: 27017, name: MongoDbAtlasLocalResource.PrimaryEndpointName)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["MONGODB_INITDB_ROOT_USERNAME"] =
                    resource.UserNameParameter is not null
                        ? ReferenceExpression.Create($"{resource.UserNameParameter}")
                        : ReferenceExpression.Create($"admin");
                context.EnvironmentVariables["MONGODB_INITDB_ROOT_PASSWORD"] =
                    ReferenceExpression.Create($"{resource.PasswordParameter}");
            })
            // 容器內透過 mongosh ping 確認 mongod 與 replica set 已就緒；
            // WaitFor 會等到 Docker HEALTHCHECK 回報 healthy 才放行。
            .WithContainerRuntimeArgs("--health-cmd", "mongosh --eval \"db.runCommand({ping:1})\" --username $MONGODB_INITDB_ROOT_USERNAME --password $MONGODB_INITDB_ROOT_PASSWORD --quiet")
            .WithContainerRuntimeArgs("--health-interval", "5s")
            .WithContainerRuntimeArgs("--health-timeout", "5s")
            .WithContainerRuntimeArgs("--health-retries", "6")
            .WithContainerRuntimeArgs("--health-start-period", "15s");
    }

    /// <summary>
    /// 在 Atlas Local 容器中新增資料庫子資源。
    /// </summary>
    public static IResourceBuilder<MongoDbAtlasLocalDatabaseResource> AddDatabase(
        this IResourceBuilder<MongoDbAtlasLocalResource> builder,
        string name,
        string? databaseName = null)
    {
        var dbName = databaseName ?? name;
        var resource = new MongoDbAtlasLocalDatabaseResource(name, dbName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// 掛載資料磁碟區（/data/db 與 /data/configdb），保留資料庫內容。
    /// </summary>
    public static IResourceBuilder<MongoDbAtlasLocalResource> WithAtlasDataVolume(
        this IResourceBuilder<MongoDbAtlasLocalResource> builder,
        string? name = null)
    {
        return builder
            .WithVolume(name ?? $"{builder.Resource.Name}-data", "/data/db")
            .WithVolume($"{name ?? builder.Resource.Name}-configdb", "/data/configdb");
    }

    /// <summary>
    /// 掛載 Atlas Search 索引磁碟區（/data/mongot），保留 $search / $vectorSearch 索引。
    /// </summary>
    public static IResourceBuilder<MongoDbAtlasLocalResource> WithSearchVolume(
        this IResourceBuilder<MongoDbAtlasLocalResource> builder,
        string? name = null)
    {
        return builder.WithVolume(name ?? $"{builder.Resource.Name}-mongot", "/data/mongot");
    }
}
