using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class IntegrationTestsFactory : WebApplicationFactory<ProductRequest>, IAsyncLifetime
{
    private readonly IContainer _container;
    private const int PORT_HEALTHCHECK = 8080;
    private const int PORT_API = 8081;

    public IntegrationTestsFactory()
        => _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-EN20251022")
            .WithEnvironment("ENABLE_EXPLORER", "false")
            .WithPortBinding(PORT_HEALTHCHECK, true)
            .WithPortBinding(PORT_API, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(
                Console.OpenStandardOutput(),
                Console.OpenStandardError()))
            .Build();

    public CosmosClient CosmosClient
        => new(
            GetConnectionString(),
            new()
            {
                ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => new(new UriRewriter(_container.Hostname, _container.GetMappedPublicPort(PORT_API)))
            });

    public string GetConnectionString()
        => $"AccountEndpoint=http://{_container.Hostname}:{_container.GetMappedPublicPort(PORT_API)}/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public Task InitializeAsync()
        => _container.StartAsync();

    public new Task DisposeAsync()
        => _container.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(s => s.ImplementationType == typeof(DatabaseSetup)));
            services.AddSingleton(_ => CosmosClient);
        });

    private sealed class UriRewriter(string hostname, ushort port)
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

    private sealed class WaitUntil : IWaitUntil
    {
        /// <inheritdoc />
        public async Task<bool> UntilAsync(IContainer container)
        {
            // CosmosDB's preconfigured HTTP client will redirect the request to the container.
            const string REQUEST_URI = "http://localhost/alive"; // -> https://github.com/Azure/azure-cosmos-db-emulator-docker/issues/209#issuecomment-3487701068

            using var httpClient = new HttpClient(new UriRewriter(container.Hostname, container.GetMappedPublicPort(8080)));

            try
            {
                using var httpResponse = await httpClient.GetAsync(REQUEST_URI)
                    .ConfigureAwait(false);
                if(httpResponse.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}

[CollectionDefinition(nameof(CollectionIntegrationTests))]
public sealed class CollectionIntegrationTests : ICollectionFixture<IntegrationTestsFactory> { }
