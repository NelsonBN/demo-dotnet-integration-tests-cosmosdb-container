using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;

namespace Api;

public class DatabaseSetup(CosmosClient client) : IHostedService
{
    private readonly CosmosClient _client = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var database = (await _client.CreateDatabaseIfNotExistsAsync(
            Constants.DATABASE_NAME,
            cancellationToken: cancellationToken)).Database;

        await database.CreateContainerIfNotExistsAsync(
            Constants.CONTAINER_NAME,
            "/id",
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
