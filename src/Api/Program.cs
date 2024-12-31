using System;
using System.Data;
using System.Linq;
using System.Threading;
using Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddHostedService<DatabaseSetup>()
    .AddSingleton(sp
        => new CosmosClient(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("Default"),
            new()
            {
                ConnectionMode = ConnectionMode.Gateway
            }))
    .AddKeyedSingleton(typeof(Product), (sp, _)
        => sp.GetRequiredService<CosmosClient>()
             .GetContainer(Constants.DATABASE_NAME, Constants.CONTAINER_NAME))
    .AddScoped<ProductsDao>();


var app = builder.Build();


app.MapGet("/products", async (ProductsDao dao, CancellationToken cancellationToken) =>
{
    var products = await dao.ListAsync(cancellationToken);
    return Results.Ok(products.Select(product => new ProductResponse
    {
        Id = Guid.Parse(product.Id),
        Name = product.Name,
        Quantity = product.Quantity
    }));
});

app.MapGet("/products/{id:guid}", async (ProductsDao dao, Guid id, CancellationToken cancellationToken) =>
{
    var product = await dao.GetAsync(id, cancellationToken);
    if(product is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new ProductResponse
    {
        Id = Guid.Parse(product.Id),
        Name = product.Name,
        Quantity = product.Quantity
    });
}).WithName("GetProduct");

app.MapPost("/products", async (ProductsDao dao, ProductRequest request, CancellationToken cancellationToken) =>
{
    if(string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest("Name is required");
    }

    var id = Guid.NewGuid();

    var product = new Product
    {
        Id = id.ToString(),
        Name = request.Name,
        Quantity = request.Quantity
    };

    await dao.AddAsync(product, cancellationToken);

    return TypedResults.CreatedAtRoute(
        new ProductResponse
        {
            Id = id,
            Name = product.Name,
            Quantity = product.Quantity
        },
        "GetProduct",
        new { id = product.Id });
});

app.MapPut("/products/{id:guid}", async (ProductsDao dao, Guid id, ProductRequest request, CancellationToken cancellationToken) =>
{
    if(string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest("Name is required");
    }

    var product = await dao.GetAsync(id, cancellationToken);
    if(product is null)
    {
        return Results.NotFound();
    }

    product.Name = request.Name;
    product.Quantity = request.Quantity;

    await dao.UpdateAsync(product, cancellationToken);

    return Results.NoContent();
});

app.MapDelete("/products/{id:guid}", async (ProductsDao dao, Guid id, CancellationToken cancellationToken) =>
{
    var product = await dao.GetAsync(id, cancellationToken);
    if(product is null)
    {
        return Results.NotFound();
    }
    await dao.DeleteAsync(product, cancellationToken);

    return Results.NoContent();
});

await app.RunAsync();
