using Customers.Core.Services.CryptoService;
using Microsoft.EntityFrameworkCore;

namespace Customers.Core.Data;

public class KeyVaultDbContext(DbContextOptions<KeyVaultDbContext> options) : DbContext(options)
{
    public DbSet<EncryptionKey> EncryptionKeys => Set<EncryptionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncryptionKey>().HasKey(k => k.CustomerId);
    }
}
