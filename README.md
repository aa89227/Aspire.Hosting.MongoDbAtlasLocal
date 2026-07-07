# Aspire.Hosting.MongoDbAtlasLocal

[![NuGet](https://img.shields.io/nuget/v/aa89227.Aspire.Hosting.MongoDbAtlasLocal)](https://www.nuget.org/packages/aa89227.Aspire.Hosting.MongoDbAtlasLocal)

A .NET Aspire hosting integration for the [MongoDB Atlas Local](https://www.mongodb.com/docs/atlas/cli/current/atlas-cli-deploy-docker/) container (`mongodb/mongodb-atlas-local`), which provides a single-node MongoDB replica set with built-in support for Atlas Search (`$search`) and Vector Search (`$vectorSearch`).

## Installation

```shell
dotnet add package aa89227.Aspire.Hosting.MongoDbAtlasLocal
```

## Usage

### Basic

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDbAtlasLocal("mongo");
var db = mongo.AddDatabase("mydb");

builder.AddProject<Projects.MyApi>("api")
    .WithReference(db)
    .WaitFor(mongo);

builder.Build().Run();
```

### Pin image tag

```csharp
var mongo = builder.AddMongoDbAtlasLocal("mongo")
    .WithImageTag("8.2.6");
```

### Persist data with volumes

```csharp
var mongo = builder.AddMongoDbAtlasLocal("mongo")
    .WithAtlasDataVolume()    // /data/db + /data/configdb
    .WithSearchVolume();      // /data/mongot (Atlas Search indexes)
```

### Custom credentials

```csharp
var user = builder.AddParameter("mongo-user");
var pass = builder.AddParameter("mongo-pass", secret: true);

var mongo = builder.AddMongoDbAtlasLocal("mongo",
    userName: user,
    password: pass);
```

### Fixed port

```csharp
var mongo = builder.AddMongoDbAtlasLocal("mongo", port: 27017);
```

## API Reference

| Method | Description |
|--------|-------------|
| `AddMongoDbAtlasLocal(name, port?, userName?, password?)` | Add an Atlas Local container resource |
| `.AddDatabase(name, databaseName?)` | Add a database sub-resource |
| `.WithAtlasDataVolume(name?)` | Mount volumes for `/data/db` and `/data/configdb` |
| `.WithSearchVolume(name?)` | Mount volume for `/data/mongot` |

## Connection Strings

The container resource exposes a connection string in the format:

```
mongodb://{user}:{password}@{host}:{port}
```

Database sub-resources append the database name with `directConnection=true&authSource=admin`:

```
mongodb://{user}:{password}@{host}:{port}/{dbName}?directConnection=true&authSource=admin
```

## Requirements

- .NET 10+
- .NET Aspire 9.2+
- Docker (for running the MongoDB Atlas Local container)

## License

MIT
