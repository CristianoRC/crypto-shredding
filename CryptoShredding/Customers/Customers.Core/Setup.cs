using Customers.Core.Data;
using Customers.Core.Services.CryptoService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Core;

public static class Setup
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CustomersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CustomersDb")));

        services.AddDbContext<KeyVaultDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("KeyVault")));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        services.Configure<MasterKeyOptions>(configuration.GetSection(MasterKeyOptions.SectionName));
        services.AddScoped<CryptoService>();

        return services;
    }
}
