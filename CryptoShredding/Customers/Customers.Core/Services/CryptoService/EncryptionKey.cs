namespace Customers.Core.Services.CryptoService;

public class EncryptionKey
{
    public required Guid CustomerId { get; init; }
    public required byte[] Key { get; init; }
    public required byte[] InitializationVector { get; init; }
}
