using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;

namespace dotAPNS.AspNetCore
{
    public interface IApnsClientFactory
    {
        IApnsClient CreateUsingCert([NotNull] X509Certificate2 cert, bool useSandbox = false, bool disableServerCertValidation = false);
        IApnsClient CreateUsingCert([NotNull] string pathToCert, bool useSandbox = false, bool disableServerCertValidation = false);
        IApnsClient CreateUsingJwt([NotNull] ApnsJwtOptions options, bool useSandbox = false, bool disableServerCertValidation = false);
    }

    public class ApnsClientFactory : IApnsClientFactory
    {
        readonly IHttpClientFactory _httpClientFactory;

        public ApnsClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IApnsClient CreateUsingCert(X509Certificate2 cert, bool useSandbox, bool disableServerCertValidation = false)
        {
            var client = disableServerCertValidation
                ? CreateUsingCertWithNoServerCertValidation(cert)
                : ApnsClient.CreateUsingCert(cert);
            if (useSandbox)
                client.UseSandbox();
            return client;
        }

        public IApnsClient CreateUsingCert(string pathToCert, bool useSandbox = false, bool disableServerCertValidation = false)
        {
            var cert = new X509Certificate2(pathToCert);
            return CreateUsingCert(cert, useSandbox, disableServerCertValidation);
        }

        public IApnsClient CreateUsingJwt(ApnsJwtOptions options, bool useSandbox = false, bool disableServerCertValidation = false)
        {
            var httpClient = _httpClientFactory.CreateClient(disableServerCertValidation ? "dotAPNS_DisableCerverCertValidation" : "dotAPNS");
            var client = ApnsClient.CreateUsingJwt(httpClient, options);
            if (useSandbox)
                client.UseSandbox();
            return client;
        }

        ApnsClient CreateUsingCertWithNoServerCertValidation(X509Certificate2 cert)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };

            handler.ClientCertificates.Add(cert);
            var httpClient = new HttpClient(handler);
            var client = ApnsClient.CreateUsingCustomHttpClient(httpClient, cert);
            return client;
        }
    }
}
