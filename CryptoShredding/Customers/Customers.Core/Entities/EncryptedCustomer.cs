namespace Customers.Core.Entities;

public class EncryptedCustomer
{
    public required Guid Id { get; init; }
    public required string EncryptedName { get; set; }
    public required string EncryptedDocument { get; set; }
    public required string EncryptedAddress { get; set; }
}
