using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;

Console.WriteLine("Starting...");


const int PORT_HEALTHCHECK = 8080;
const int PORT_API = 8081;
const string DATABASE_NAME = "Demo";
const string CONTAINER_NAME = "Products";


while(true)
{
    IContainer? container = default;
    try
    {
        Console.WriteLine("Starting CosmosDB Emulator...");

        container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-EN20251022")
            .WithEnvironment("ENABLE_EXPLORER", "false")
            .WithPortBinding(PORT_HEALTHCHECK, true)
            .WithPortBinding(PORT_API, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))
            //.WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(
            //    Console.OpenStandardOutput(),
            //    Console.OpenStandardError()))
            .Build();

        await container.StartAsync();

        var connectionString = $"AccountEndpoint=http://{container.Hostname}:{container.GetMappedPublicPort(PORT_API)}/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        var client = new CosmosClient(
            connectionString,
            new()
            {
                ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => new HttpClient(new UriRewriter(container.Hostname, container.GetMappedPublicPort(PORT_API)))
            });


        Database database;
        try
        {
            database = (await client.CreateDatabaseIfNotExistsAsync(DATABASE_NAME)).Database;
        }
        catch(Exception ex)
        {
            var error = ex.ToString();
            throw new Exception($"--------------------> [ERROR][GETTING DATABASE] {ex.Message}");
        }



        await database.CreateContainerIfNotExistsAsync(CONTAINER_NAME, "/id");

        var databaseProperties = (await client.GetDatabaseQueryIterator<DatabaseProperties>().ReadNextAsync()).First();

        Console.WriteLine($"--------------------> [SUCCESS] Database: {databaseProperties.Id}");


        //Console.WriteLine("Press any key 'ESC' to stop or any other key to restart...");
        //if(Console.ReadKey().Key == ConsoleKey.Escape)
        //{
        //    Console.WriteLine("Stopping CosmosDB Emulator...");
        //    break;
        //}

    }
    catch(Exception exception)
    {
        Console.WriteLine($"--------------------> [ERROR][GLOBAL] {exception.Message}");
    }
    if(container is not null)
    {
        await container.StopAsync();
        await container.DisposeAsync();
    }

    Console.WriteLine("Restarting CosmosDB Emulator...");
}

//Console.WriteLine("Press any key to exit...");
//Console.ReadKey();



internal class UriRewriter(string hostname, ushort port)
    : DelegatingHandler(new HttpClientHandler())
{
    private readonly string _hostname = hostname;

    private readonly ushort _port = port;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.RequestUri = new UriBuilder(Uri.UriSchemeHttp, _hostname, _port, request.RequestUri?.PathAndQuery).Uri;
        return base.SendAsync(request, cancellationToken);
    }
}

internal class WaitUntil : IWaitUntil
{
    public async Task<bool> UntilAsync(IContainer container)
    {
        // CosmosDB's preconfigured HTTP client will redirect the request to the container.
        const string REQUEST_URI = "http://localhost";

        using var httpClient = new HttpClient(new UriRewriter(container.Hostname, container.GetMappedPublicPort(8080)));


        try
        {
            using var httpResponse = await httpClient
                .GetAsync(REQUEST_URI).ConfigureAwait(false);

            if(httpResponse.IsSuccessStatusCode)
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                //Console.WriteLine($"--------------------> [HEALTH CHECK] {content}");

                // Deserialize and check the ready field and validate if was returned as true
                using var jsonDocument = System.Text.Json.JsonDocument.Parse(content);
                if(jsonDocument.RootElement.TryGetProperty("ready", out var readyProperty) &&
                   readyProperty.GetBoolean())
                {
                    return true;
                }
                // else
                // {
                //     Console.WriteLine($"--------------------> [HEALTH CHECK] Container not ready yet");
                // }
            }
        }
        catch(Exception exception)
        {
            //Console.WriteLine($"--------------------> [ERROR][HEALTH CHECK] {exception.Message}");
        }
        return false;
    }
}
