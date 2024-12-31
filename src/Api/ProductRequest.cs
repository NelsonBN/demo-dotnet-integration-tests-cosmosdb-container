namespace Api;

public record ProductRequest
{
    public required string Name { get; init; }
    public int Quantity { get; init; }
};
