namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// MongoDB Atlas Local 容器資源（mongodb/mongodb-atlas-local），
/// 內建支援 $search 與 $vectorSearch。
/// </summary>
public sealed class MongoDbAtlasLocalResource(
    string name,
    ParameterResource? userName,
    ParameterResource password)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "mongodb";
    private const string DefaultUserName = "admin";

    private EndpointReference? _primaryEndpoint;

    public EndpointReference PrimaryEndpoint =>
        _primaryEndpoint ??= new(this, PrimaryEndpointName);

    internal ParameterResource? UserNameParameter { get; } = userName;

    internal ParameterResource PasswordParameter { get; } = password;

    private ReferenceExpression UserNameReference =>
        UserNameParameter is not null
            ? ReferenceExpression.Create($"{UserNameParameter}")
            : ReferenceExpression.Create($"{DefaultUserName}");

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"mongodb://{UserNameReference}:{PasswordParameter}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
}
