var builder = DistributedApplication.CreateBuilder(args);


// Backing services
var postgres = builder
        .AddPostgres("postgres")
        .WithPgAdmin()
        //.WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");

var cache = builder
    .AddRedis("cache")
    .WithRedisInsight()
    //.WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    //.WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var keycloack = builder
    .AddKeycloak("keycloak", 8080)
    //.WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

if (builder.ExecutionContext.IsRunMode)
{
    // Data volumes don't work on ACA for postgres so only add when running 
    postgres.WithDataVolume();
    rabbitmq.WithDataVolume();
    keycloack.WithDataVolume();
}

var ollama = builder
    .AddOllama("ollama", 11434)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOpenWebUI();

var llma = ollama.AddModel("llama3.2");
var embedding = ollama.AddModel("all-minilm");

// Projects
var catalog = builder
    .AddProject<Projects.Catalog>("catalog")
    .WithReference(catalogDb)
    .WithReference(rabbitmq)                            // The WithReference Method injects environement variables so catalog ms can connect to rabbitMq
    .WithReference(llma)
    .WithReference(embedding)
    .WaitFor(catalogDb)
    .WaitFor(rabbitmq)                                  // The WaitFor ensures catalog ms waits for rabbitMQ to be ready before starting
    .WaitFor(llma)
    .WaitFor(embedding);


var basket = builder
    .AddProject<Projects.Basket>("basket")
    .WithReference(cache)
    .WithReference(catalog)
    .WithReference(rabbitmq)
    .WithReference(keycloack)
    .WaitFor(cache)
    .WaitFor(rabbitmq)
    .WaitFor(keycloack);

var webapp = builder
    .AddProject<Projects.WebApp>("webapp")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(catalog)
    .WithReference(basket)
    .WaitFor(catalog)
    .WaitFor(basket);

builder.Build().Run();
