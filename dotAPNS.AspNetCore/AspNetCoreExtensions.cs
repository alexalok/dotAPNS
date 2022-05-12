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
            var options = new ApnsServiceOptions();
            if (configureApns != null)
            {
                optionsBuilder.Configure(configureApns);
                configureApns(options);
            }



            services.AddHttpClient("dotAPNS")
#if NET5_0_OR_GREATER
                .ConfigureHttpClient(client =>
                {
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultRequestHeaders.ConnectionClose = false;
                })
                .ConfigurePrimaryHttpMessageHandler(services =>
                {
                    var sh = new SocketsHttpHandler();
                    sh.EnableMultipleHttp2Connections = true;

                    sh.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
                    sh.KeepAlivePingDelay = TimeSpan.FromSeconds(120);
                    sh.KeepAlivePingTimeout = TimeSpan.FromHours(12);

                    sh.MaxConnectionsPerServer = options.MaxConcurrentConnections ?? 15;
                    sh.AutomaticDecompression = DecompressionMethods.All;
                    return sh;
                });
#endif
                ;
            services.AddHttpClient("dotAPNS_DisableCerverCertValidation")
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.ConnectionClose = false;
                })
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
                .ConfigurePrimaryHttpMessageHandler(services =>
                {
                    var sh = new SocketsHttpHandler();
                    sh.EnableMultipleHttp2Connections = true;

                    sh.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
                    sh.KeepAlivePingDelay = TimeSpan.FromSeconds(60);
                    sh.KeepAlivePingTimeout = TimeSpan.FromHours(12); //https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/CommunicatingwithAPNs.html keeping the connections open for longer

                    sh.MaxConnectionsPerServer = options.MaxConcurrentConnections ?? 15;
                    sh.AutomaticDecompression = DecompressionMethods.All;
                    return sh;
                });
#endif
            ;
            services.AddSingleton<IApnsClientFactory, ApnsClientFactory>();
            services.AddSingleton<IApnsService, ApnsService>();
            return services;
        }
    }
}
