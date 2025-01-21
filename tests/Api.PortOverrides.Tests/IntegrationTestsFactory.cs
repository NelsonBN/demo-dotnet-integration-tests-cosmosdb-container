using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Api.Tests;

public sealed class IntegrationTestsFactory : WebApplicationFactory<ProductRequest>, IAsyncLifetime
{
    private readonly int _cosmosDbContainerApPort;
    private readonly IContainer _container;

    public IntegrationTestsFactory()
    {
        using(var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            _cosmosDbContainerApPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-EN20250117")
            .WithEnvironment("ENABLE_EXPLORER", "false")
            .WithEnvironment("PORT", _cosmosDbContainerApPort.ToString())
            .WithPortBinding(_cosmosDbContainerApPort, _cosmosDbContainerApPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort((ushort)_cosmosDbContainerApPort)))
            .WithOutputConsumer(
                Consume.RedirectStdoutAndStderrToStream(
                    Console.OpenStandardOutput(),
                    Console.OpenStandardError()))
            .Build();
    }

    public string GetConnectionString()
        => $"AccountEndpoint=http://{_container.Hostname}:{_cosmosDbContainerApPort}/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public Task InitializeAsync()
        => _container.StartAsync();

    public new Task DisposeAsync()
        => _container.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = GetConnectionString()
            })
            .Build());

        builder.ConfigureTestServices(services
            => services.Remove(services.Single(s => s.ImplementationType == typeof(DatabaseSetup))));
    }
}

[CollectionDefinition(nameof(CollectionIntegrationTests))]
public sealed class CollectionIntegrationTests : ICollectionFixture<IntegrationTestsFactory> { }
