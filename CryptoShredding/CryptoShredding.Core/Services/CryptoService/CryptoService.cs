using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CryptoShredding.Core.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace CryptoShredding.Core.Services.CryptoService;

public class CryptoService(IDistributedCache keysDataBase)
{
    public async Task<User> EncryptUser(User user)
    {
        var encryptionKey = await GetEncryptionKey(user.Id) ?? await GenerateEncryptKey(user.Id);

        var aes = Aes.Create();
        aes.Key = encryptionKey.Key;
        aes.IV = encryptionKey.InitializationVector;

        var encryptedName = aes.EncryptCbc(Encoding.UTF8.GetBytes(user.Name), aes.IV, PaddingMode.PKCS7);
        var encryptedDocument = aes.EncryptCbc(Encoding.UTF8.GetBytes(user.Document), aes.IV, PaddingMode.PKCS7);
        var encryptedAddress = aes.EncryptCbc(Encoding.UTF8.GetBytes(user.Address), aes.IV, PaddingMode.PKCS7);

        return user with
        {
            Name = Convert.ToBase64String(encryptedName),
            Document = Convert.ToBase64String(encryptedDocument),
            Address = Convert.ToBase64String(encryptedAddress)
        };
    }

    public async Task<User> DecryptUser(User encryptedUser)
    {
        var encryptionKey = await GetEncryptionKey(encryptedUser.Id);
        if (encryptedUser is null)
            throw new ArgumentException("Chave de criptografia para este usuário não foi encontrada", nameof(User.Id));

        var aes = Aes.Create();
        aes.Key = encryptionKey.Key;
        aes.IV = encryptionKey.InitializationVector;
        
        var decryptedName = aes.DecryptCbc(Convert.FromBase64String(encryptedUser.Name), aes.IV, PaddingMode.PKCS7);
        var decryptedDocument = aes.DecryptCbc(Convert.FromBase64String(encryptedUser.Document), aes.IV, PaddingMode.PKCS7);
        var decryptedAddress = aes.DecryptCbc(Convert.FromBase64String(encryptedUser.Address), aes.IV, PaddingMode.PKCS7);
        
        return encryptedUser with
        {
            Name = Encoding.UTF8.GetString(decryptedName),
            Document = Encoding.UTF8.GetString(decryptedDocument),
            Address = Encoding.UTF8.GetString(decryptedAddress)
        };
    }

    public async Task DeleteEncryptKey(Guid userId)
    {
        await keysDataBase.RemoveAsync(userId.ToString());
    }

    private async Task<EncryptionKey?> GetEncryptionKey(Guid userId)
    {
        var encryptKeyJson = await keysDataBase.GetStringAsync(userId.ToString());
        return encryptKeyJson is null ? null : JsonSerializer.Deserialize<EncryptionKey>(encryptKeyJson);
    }

    private async Task SaveEncryptKey(Guid userId, EncryptionKey encryptionKey)
    {
        var jsonData = JsonSerializer.Serialize(encryptionKey);
        await keysDataBase.SetStringAsync(userId.ToString(), jsonData);
    }

    private async Task<EncryptionKey> GenerateEncryptKey(Guid userId)
    {
        var random = RandomNumberGenerator.Create();
        var key = new byte[16];
        var iv = new byte[16];

        random.GetBytes(key);
        random.GetBytes(iv);

        var encryptionKey = new EncryptionKey {Key = key, InitializationVector = iv};

        await SaveEncryptKey(userId, encryptionKey);
        return encryptionKey;
    }
}