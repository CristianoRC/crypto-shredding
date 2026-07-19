using Microsoft.EntityFrameworkCore;
using Orders.Api.Entities;

namespace Orders.Api.Data;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
