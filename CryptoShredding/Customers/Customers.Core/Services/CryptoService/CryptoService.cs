using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Customers.Core.Data;
using Customers.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Customers.Core.Services.CryptoService;

public enum DecryptStatus
{
    NotFound,
    Shredded,
    Ok
}

public record DecryptResult(DecryptStatus Status, Customer? Customer, string? Source);

public record CustomerSummary(Guid Id, string EncryptedName, string EncryptedDocument, string EncryptedAddress, bool KeyExists);

public class CryptoService(CustomersDbContext customersDb, KeyVaultDbContext keyVaultDb, IDistributedCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Customer> CreateAsync(Customer customer)
    {
        var id = customer.Id == Guid.Empty ? Guid.NewGuid() : customer.Id;
        var encryptionKey = GenerateEncryptionKey(id);

        using var aes = Aes.Create();
        aes.Key = encryptionKey.Key;
        aes.IV = encryptionKey.InitializationVector;

        var encryptedCustomer = new EncryptedCustomer
        {
            Id = id,
            EncryptedName = Encrypt(aes, customer.Name),
            EncryptedDocument = Encrypt(aes, customer.Document),
            EncryptedAddress = Encrypt(aes, customer.Address)
        };

        keyVaultDb.EncryptionKeys.Add(encryptionKey);
        customersDb.Customers.Add(encryptedCustomer);

        await keyVaultDb.SaveChangesAsync();
        await customersDb.SaveChangesAsync();

        return customer with { Id = id };
    }

    public async Task<CustomerSummary?> GetEncryptedAsync(Guid id)
    {
        var encryptedCustomer = await customersDb.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (encryptedCustomer is null)
            return null;

        var keyExists = await keyVaultDb.EncryptionKeys.AsNoTracking().AnyAsync(k => k.CustomerId == id);
        return ToSummary(encryptedCustomer, keyExists);
    }

    public async Task<IReadOnlyList<CustomerSummary>> ListAsync()
    {
        var encryptedCustomers = await customersDb.Customers.AsNoTracking().ToListAsync();
        var keyIds = (await keyVaultDb.EncryptionKeys.AsNoTracking().Select(k => k.CustomerId).ToListAsync()).ToHashSet();

        return encryptedCustomers
            .Select(c => ToSummary(c, keyIds.Contains(c.Id)))
            .ToList();
    }

    public async Task<DecryptResult> TryDecryptAsync(Guid id)
    {
        var cacheKey = CacheKey(id);
        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return new DecryptResult(DecryptStatus.Ok, JsonSerializer.Deserialize<Customer>(cached), "cache");

        var encryptedCustomer = await customersDb.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (encryptedCustomer is null)
            return new DecryptResult(DecryptStatus.NotFound, null, null);

        var encryptionKey = await keyVaultDb.EncryptionKeys.AsNoTracking().FirstOrDefaultAsync(k => k.CustomerId == id);
        if (encryptionKey is null)
            return new DecryptResult(DecryptStatus.Shredded, null, null);

        using var aes = Aes.Create();
        aes.Key = encryptionKey.Key;
        aes.IV = encryptionKey.InitializationVector;

        var customer = new Customer(
            id,
            Decrypt(aes, encryptedCustomer.EncryptedName),
            Decrypt(aes, encryptedCustomer.EncryptedDocument),
            Decrypt(aes, encryptedCustomer.EncryptedAddress));

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(customer),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });

        return new DecryptResult(DecryptStatus.Ok, customer, "database");
    }

    public async Task<bool> ShredAsync(Guid id)
    {
        var encryptionKey = await keyVaultDb.EncryptionKeys.FirstOrDefaultAsync(k => k.CustomerId == id);
        if (encryptionKey is null)
            return false;

        keyVaultDb.EncryptionKeys.Remove(encryptionKey);
        await keyVaultDb.SaveChangesAsync();

        // Sem isso, o dado "esquecido" continuaria servível pelo cache até o TTL expirar.
        await cache.RemoveAsync(CacheKey(id));

        return true;
    }

    private static CustomerSummary ToSummary(EncryptedCustomer encryptedCustomer, bool keyExists) => new(
        encryptedCustomer.Id,
        encryptedCustomer.EncryptedName,
        encryptedCustomer.EncryptedDocument,
        encryptedCustomer.EncryptedAddress,
        keyExists);

    private static string CacheKey(Guid id) => $"customer:{id}:decrypted";

    private static EncryptionKey GenerateEncryptionKey(Guid customerId)
    {
        var key = new byte[16];
        var iv = new byte[16];
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(iv);

        return new EncryptionKey { CustomerId = customerId, Key = key, InitializationVector = iv };
    }

    private static string Encrypt(Aes aes, string plainText) =>
        Convert.ToBase64String(aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), aes.IV, PaddingMode.PKCS7));

    private static string Decrypt(Aes aes, string cipherText) =>
        Encoding.UTF8.GetString(aes.DecryptCbc(Convert.FromBase64String(cipherText), aes.IV, PaddingMode.PKCS7));
}
