namespace Customers.Core.Services.CryptoService;

/// <summary>
/// A KEK (key-encryption-key) que embrulha a DEK de cada cliente. Hoje vem do appsettings; em produção
/// seria o master key de um Azure Key Vault ou de um HashiCorp Vault (transit engine).
/// </summary>
public class MasterKeyOptions
{
    public const string SectionName = "MasterKey";

    public required string Key { get; init; }
    public required string Iv { get; init; }
}
