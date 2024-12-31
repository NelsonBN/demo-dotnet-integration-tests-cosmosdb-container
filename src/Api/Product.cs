using Newtonsoft.Json;

namespace Api;

public class Product
{
    [JsonProperty("id")]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
}
