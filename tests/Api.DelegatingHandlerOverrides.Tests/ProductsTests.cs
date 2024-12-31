using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bogus;

namespace Api.Tests;

[Collection(nameof(CollectionIntegrationTests))]
public sealed class ProductsTests
{
    private readonly IntegrationTestsFactory _factory;

    public ProductsTests(IntegrationTestsFactory factory)
    {
        _factory = factory;
        var cosmosClient = _factory.CosmosClient;
        var setup = new DatabaseSetup(cosmosClient);
        setup.StartAsync(default)
             .GetAwaiter().GetResult();
    }

    [Fact]
    public async Task When_get_product_created()
    {
        // Arrange
        var newProduct = new Faker<ProductRequest>()
            .RuleFor(p => p.Name, s => s.Commerce.ProductName())
            .RuleFor(p => p.Quantity, s => s.Random.Int(1, 100))
            .Generate();


        // Act
        var createResponse = await _factory.CreateClient()
            .PostAsync("/products", JsonContent.Create(newProduct));
        var productCreated = (await createResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        var getResponse = await _factory.CreateClient()
            .GetAsync($"/products/{productCreated.Id}");


        // Assert
        createResponse.Should().Be201Created();
        productCreated.Should().Match<ProductResponse>(m =>
            m.Name == newProduct.Name &&
            m.Quantity == newProduct.Quantity);

        getResponse.Should()
           .Be200Ok()
           .And.Satisfy<ProductResponse>(model =>
                model.Should().Match<ProductResponse>(m =>
                    m.Id == productCreated.Id &&
                    m.Name == productCreated.Name &&
                    m.Quantity == productCreated.Quantity));
    }

    [Fact]
    public async Task When_get_products_created()
    {
        // Arrange
        var newProducts = new Faker<ProductRequest>()
            .RuleFor(p => p.Name, s => s.Commerce.ProductName())
            .RuleFor(p => p.Quantity, s => s.Random.Int(1, 100))
            .Generate(2);


        // Arrange
        foreach(var newProduct in newProducts)
        {
            await _factory.CreateClient()
                .PostAsync("/products", JsonContent.Create(newProduct));
        }

        var getResponse = await _factory.CreateClient()
            .GetAsync("/products");


        // Act
        getResponse.Should()
            .Be200Ok()
            .And.Satisfy<List<ProductResponse>>(model =>
                model.Should().HaveCountGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task When_update_product_created()
    {
        // Arrange
        var newProduct = new Faker<ProductRequest>()
            .RuleFor(p => p.Name, s => s.Commerce.ProductName())
            .RuleFor(p => p.Quantity, s => s.Random.Int(1, 100))
            .Generate();

        var updateProduct = new Faker<ProductRequest>()
            .RuleFor(p => p.Name, s => s.Commerce.ProductName())
            .RuleFor(p => p.Quantity, s => s.Random.Int(1, 100))
            .Generate();


        // Act
        var createResponse = await _factory.CreateClient()
            .PostAsync("/products", JsonContent.Create(newProduct));
        var productCreated = (await createResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        var updateResponse = await _factory.CreateClient()
            .PutAsync($"/products/{productCreated.Id}", JsonContent.Create(updateProduct));

        var getResponse = await _factory.CreateClient()
            .GetAsync($"/products/{productCreated.Id}");


        // Assert
        createResponse.Should().Be201Created();
        updateResponse.Should().Be204NoContent();

        getResponse.Should()
           .Be200Ok()
           .And.Satisfy<ProductResponse>(model =>
                model.Should().Match<ProductResponse>(m =>
                    m.Id == productCreated.Id &&
                    m.Name == updateProduct.Name &&
                    m.Quantity == updateProduct.Quantity));
    }


    [Fact]
    public async Task When_delete_product_created()
    {
        // Arrange
        var newProduct = new Faker<ProductRequest>()
            .RuleFor(p => p.Name, s => s.Commerce.ProductName())
            .RuleFor(p => p.Quantity, s => s.Random.Int(1, 100))
            .Generate();


        // Act
        var createResponse = await _factory.CreateClient()
            .PostAsync("/products", JsonContent.Create(newProduct));
        var productCreated = (await createResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        var deleteResponse = await _factory.CreateClient()
            .DeleteAsync($"/products/{productCreated.Id}");


        // Assert
        createResponse.Should().Be201Created();
        deleteResponse.Should().Be204NoContent();
    }
}
