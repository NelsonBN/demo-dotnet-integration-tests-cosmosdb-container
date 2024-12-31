using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Api;

public class ProductsDao([FromKeyedServices(typeof(Product))] Container container)
{
    private readonly Container _container = container;


    public async Task<IEnumerable<Product>> ListAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition($"SELECT * FROM c");

        var iterator = _container.GetItemQueryIterator<Product>(query);

        var products = new List<Product>();

        while(iterator.HasMoreResults)
        {
            foreach(var product in await iterator.ReadNextAsync(cancellationToken))
            {
                products.Add(product);
            }
        }

        return products;
    }

    public async Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var productId = id.ToString();
            var response = await _container.ReadItemAsync<Product>(
                productId,
                new PartitionKey(productId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch(CosmosException exception) when(exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }


    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            product,
            new PartitionKey(product.Id),
            cancellationToken: cancellationToken);

        if(response?.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException("The product was not created");
        }
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var response = await _container.UpsertItemAsync(
            product,
            new PartitionKey(product.Id),
            cancellationToken: cancellationToken);

        if(!(response?.StatusCode == HttpStatusCode.OK || response?.StatusCode == HttpStatusCode.Created))
        {
            throw new InvalidOperationException("The product was not updated");
        }
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        var response = await _container.DeleteItemAsync<Product>(
            product.Id,
            new PartitionKey(product.Id),
            cancellationToken: cancellationToken);

        if(response?.StatusCode != HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException("The product was not deleted");
        }
    }
}
