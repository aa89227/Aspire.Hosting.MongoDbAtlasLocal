using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.MongoDbAtlasLocal.Tests;

public class MongoDbAtlasLocalExtensionsTests
{
    [Fact]
    public void AddMongoDbAtlasLocal_CreatesResourceWithCorrectName()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo");

        Assert.Equal("mongo", mongo.Resource.Name);
    }

    [Fact]
    public void AddMongoDbAtlasLocal_SetsCorrectImage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo");

        var imageAnnotation = mongo.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        Assert.Equal("mongodb/mongodb-atlas-local", imageAnnotation.Image);
        Assert.Equal("latest", imageAnnotation.Tag);
    }

    [Fact]
    public void AddMongoDbAtlasLocal_WithImageTag_OverridesDefault()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo").WithImageTag("8.2.6");

        var imageAnnotation = mongo.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        Assert.Equal("8.2.6", imageAnnotation.Tag);
    }

    [Fact]
    public void AddMongoDbAtlasLocal_ConfiguresEndpoint()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo", port: 27027);

        var endpoint = mongo.Resource.Annotations.OfType<EndpointAnnotation>()
            .Single(e => e.Name == "mongodb");
        Assert.Equal(27017, endpoint.TargetPort);
        Assert.Equal(27027, endpoint.Port);
    }

    [Fact]
    public void AddMongoDbAtlasLocal_WithoutPort_UsesRandomPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo");

        var endpoint = mongo.Resource.Annotations.OfType<EndpointAnnotation>()
            .Single(e => e.Name == "mongodb");
        Assert.Equal(27017, endpoint.TargetPort);
        Assert.Null(endpoint.Port);
    }

    [Fact]
    public async Task AddMongoDbAtlasLocal_SetsHealthCheckRuntimeArgs()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo");

        var runtimeArgsAnnotations = mongo.Resource.Annotations
            .OfType<ContainerRuntimeArgsCallbackAnnotation>().ToList();

        var args = new List<object>();
        foreach (var annotation in runtimeArgsAnnotations)
        {
            await annotation.Callback(new ContainerRuntimeArgsCallbackContext(args));
        }

        var stringArgs = args.Select(a => a.ToString()!).ToList();
        Assert.Contains("--health-cmd", stringArgs);
        Assert.Contains("--health-interval", stringArgs);
        Assert.Contains("--health-start-period", stringArgs);
    }

    [Fact]
    public async Task AddMongoDbAtlasLocal_SetsContainerHostname()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mydb");

        var runtimeArgsAnnotations = mongo.Resource.Annotations
            .OfType<ContainerRuntimeArgsCallbackAnnotation>().ToList();

        var args = new List<object>();
        foreach (var annotation in runtimeArgsAnnotations)
        {
            await annotation.Callback(new ContainerRuntimeArgsCallbackContext(args));
        }

        var stringArgs = args.Select(a => a.ToString()!).ToList();
        var hostnameIndex = stringArgs.IndexOf("--hostname");
        Assert.True(hostnameIndex >= 0);
        Assert.Equal("mydb-atlas-local", stringArgs[hostnameIndex + 1]);
    }

    [Fact]
    public void ConnectionString_HasCorrectFormat()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mongo = builder.AddMongoDbAtlasLocal("mongo");

        var format = mongo.Resource.ConnectionStringExpression.Format;
        Assert.StartsWith("mongodb://", format);
    }

    [Fact]
    public void AddDatabase_CreatesSubResourceWithCorrectParent()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo");
        var db = mongo.AddDatabase("mydb");

        Assert.Equal("mydb", db.Resource.Name);
        Assert.Equal("mydb", db.Resource.DatabaseName);
        Assert.Equal("mongo", db.Resource.Parent.Name);
    }

    [Fact]
    public void AddDatabase_UsesCustomDatabaseName()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo");
        var db = mongo.AddDatabase("db", databaseName: "my_custom_db");

        Assert.Equal("db", db.Resource.Name);
        Assert.Equal("my_custom_db", db.Resource.DatabaseName);
    }

    [Fact]
    public void DatabaseConnectionString_IncludesDatabaseNameAndDirectConnection()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mongo = builder.AddMongoDbAtlasLocal("mongo");
        var db = mongo.AddDatabase("mydb");

        var format = db.Resource.ConnectionStringExpression.Format;
        Assert.Contains("mydb", format);
        Assert.Contains("directConnection=true", format);
        Assert.Contains("authSource=admin", format);
    }

    [Fact]
    public void WithAtlasDataVolume_AddsDataAndConfigVolumes()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo")
            .WithAtlasDataVolume("test-data");

        var volumes = mongo.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .Where(v => v.Type == ContainerMountType.Volume).ToList();

        Assert.Contains(volumes, v => v.Source == "test-data" && v.Target == "/data/db");
        Assert.Contains(volumes, v => v.Source == "test-data-configdb" && v.Target == "/data/configdb");
    }

    [Fact]
    public void WithSearchVolume_AddsMongotVolume()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("mongo")
            .WithSearchVolume("test-mongot");

        var volumes = mongo.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .Where(v => v.Type == ContainerMountType.Volume).ToList();

        Assert.Contains(volumes, v => v.Source == "test-mongot" && v.Target == "/data/mongot");
    }

    [Fact]
    public void WithAtlasDataVolume_DefaultsToResourceName()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("myserver")
            .WithAtlasDataVolume();

        var volumes = mongo.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .Where(v => v.Type == ContainerMountType.Volume).ToList();

        Assert.Contains(volumes, v => v.Source == "myserver-data" && v.Target == "/data/db");
        Assert.Contains(volumes, v => v.Source == "myserver-configdb" && v.Target == "/data/configdb");
    }

    [Fact]
    public void WithSearchVolume_DefaultsToResourceName()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo = builder.AddMongoDbAtlasLocal("myserver")
            .WithSearchVolume();

        var volumes = mongo.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .Where(v => v.Type == ContainerMountType.Volume).ToList();

        Assert.Contains(volumes, v => v.Source == "myserver-mongot" && v.Target == "/data/mongot");
    }
}
