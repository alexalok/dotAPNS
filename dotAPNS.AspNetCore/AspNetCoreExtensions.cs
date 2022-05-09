using System;
using System.Net;
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
            services.AddHttpClient("dotAPNS")
#if NET5_0_OR_GREATER
                .ConfigureHttpClient(client =>
                {
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                    client.DefaultRequestVersion = HttpVersion.Version20;
                })
#endif
                ;
            services.AddHttpClient("dotAPNS_DisableCerverCertValidation")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (m, x, c, s) => true
                })
#if NET5_0_OR_GREATER
                .ConfigureHttpClient(client =>
                {
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                    client.DefaultRequestVersion = HttpVersion.Version20;
                })
#endif
                ;
            services.AddSingleton<IApnsClientFactory, ApnsClientFactory>();
            services.AddSingleton<IApnsService, ApnsService>();
            return services;
        }
    }
}
