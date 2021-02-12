using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace dotAPNS.AspNetCore
{
    public static class AspNetCoreExtensions
    {
        public static IServiceCollection AddApns(this IServiceCollection services, Action<ApnsServiceOptions> configureApns = null)
        {
            var optionsBuilder = services.AddOptions<ApnsServiceOptions>();
            if (configureApns != null)
                optionsBuilder.Configure(configureApns);
            services.AddHttpClient("dotAPNS");
            services.AddHttpClient("dotAPNS_DisableCerverCertValidation")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (m, x, c, s) => true
                });
            services.AddSingleton<IApnsClientFactory, ApnsClientFactory>();
            services.AddSingleton<IApnsService, ApnsService>();
            return services;
        }
    }
}
