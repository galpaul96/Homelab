var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres");
var webDatabase = postgres.AddDatabase("WebDatabase");
var apiDatabase = postgres.AddDatabase("ApiDatabase");
var mongo = builder.AddMongoDB("mongo").AddDatabase("MyDb");

var apiService = builder.AddProject<Projects.Homelab_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(apiDatabase)
    .WithReference(mongo)
    .WaitFor(apiDatabase)
    .WaitFor(mongo);

builder.AddProject<Projects.Homelab_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(webDatabase)
    .WithReference(cache)
    .WaitFor(webDatabase)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
