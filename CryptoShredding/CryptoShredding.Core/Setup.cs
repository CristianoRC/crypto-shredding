using Microsoft.Extensions.DependencyInjection;

namespace CryptoShredding.Core;

public static class Setup
{
    public static IServiceCollection AddCore(this IServiceCollection service)
    {
        return service;
    }
}