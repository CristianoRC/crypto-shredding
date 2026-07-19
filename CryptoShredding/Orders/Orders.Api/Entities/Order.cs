namespace Orders.Api.Entities;

public class Order
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required string Product { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime CreatedAt { get; init; }
}
