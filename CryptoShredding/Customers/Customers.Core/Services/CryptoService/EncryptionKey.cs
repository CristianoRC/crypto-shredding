namespace Customers.Core.Services.CryptoService;

public class EncryptionKey
{
    public required Guid CustomerId { get; init; }

    /// <summary>
    /// Chave AES + IV do cliente (a DEK), embrulhados com a master key (KEK). Nunca fica em claro no banco.
    /// </summary>
    public required byte[] WrappedKeyMaterial { get; init; }
}
