using CryptoShredding.Core.Services.CryptoService;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoShredding.Core;

public static class Setup
{
    public static IServiceCollection AddCore(this IServiceCollection service)
    {
        service.AddDistributedMemoryCache();
        service.AddTransient<CryptoService>();
        return service;
    }
}