using Customers.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Customers.Core.Data;

public class CustomersDbContext(DbContextOptions<CustomersDbContext> options) : DbContext(options)
{
    public DbSet<EncryptedCustomer> Customers => Set<EncryptedCustomer>();
}
