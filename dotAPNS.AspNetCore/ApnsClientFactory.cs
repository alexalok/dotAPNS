using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace dotAPNS.AspNetCore
{
    public interface IApnsClientFactory
    {
        IApnsClient CreateUsingCert(X509Certificate2 cert, bool disableServerCertValidation = false);
        IApnsClient CreateUsingCert(string pathToCert, bool disableServerCertValidation = false);
        IApnsClient CreateUsingJwt(ApnsJwtOptions options, bool disableServerCertValidation = false);
    }

    public class ApnsClientFactory : IApnsClientFactory
    {
        readonly IHttpClientFactory _httpClientFactory;
        public ApnsClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }


        public IApnsClient CreateUsingCert(X509Certificate2 cert, bool disableServerCertValidation = false)
        {
            var client = disableServerCertValidation
                ? CreateUsingCertWithNoServerCertValidation(cert)
                : ApnsClient.CreateUsingCert(cert);
            return client;
        }

        public IApnsClient CreateUsingCert(string pathToCert, bool disableServerCertValidation = false)
        {
            var cert = new X509Certificate2(pathToCert);
            return CreateUsingCert(cert, disableServerCertValidation);
        }

        public IApnsClient CreateUsingJwt(ApnsJwtOptions options, bool disableServerCertValidation = false)
        {
            var httpClient = _httpClientFactory.CreateClient(disableServerCertValidation ? "dotAPNS_DisableCerverCertValidation" : "dotAPNS");
            var client = ApnsClient.CreateUsingJwt(httpClient, options);
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
