using Microsoft.Extensions.DependencyInjection;

namespace dotAPNS.AspNetCore
{
    public static class AspNetCoreExtensions
    {
        public static IServiceCollection AddApns(this IServiceCollection services)
        {
            services.AddHttpClient("dotAPNS");
            services.AddSingleton<IApnsClientFactory, ApnsClientFactory>();
            services.AddSingleton<IApnsService, ApnsService>();
            return services;
        }
    }
}