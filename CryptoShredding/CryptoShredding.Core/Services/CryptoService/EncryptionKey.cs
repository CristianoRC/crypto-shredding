namespace CryptoShredding.Core.Services.CryptoService;

public record EncryptionKey
{
    public required byte[] Key { get; init; }
    public required byte[] InitializationVector { get; init; }
}