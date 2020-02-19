using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;

namespace dotAPNS.AspNetCore
{
    public interface IApnsClientFactory
    {
        IApnsClient CreateUsingCert([NotNull] X509Certificate2 cert, bool useSandbox = false);
        IApnsClient CreateUsingCert([NotNull] string pathToCert, bool useSandbox = false);
        IApnsClient CreateUsingJwt([NotNull] ApnsJwtOptions options, bool useSandbox = false);
    }

    public class ApnsClientFactory : IApnsClientFactory
    {
        readonly IHttpClientFactory _httpClientFactory;

        public ApnsClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IApnsClient CreateUsingCert(X509Certificate2 cert, bool useSandbox)
        {
            var client = ApnsClient.CreateUsingCert(cert);
            if (useSandbox)
                client.UseSandbox();
            return client;
        }

        public IApnsClient CreateUsingCert(string pathToCert, bool useSandbox = false)
        {
            var client = ApnsClient.CreateUsingCert(pathToCert);
            if (useSandbox)
                client.UseSandbox();
            return client;
        }

        public IApnsClient CreateUsingJwt(ApnsJwtOptions options, bool useSandbox = false)
        {
            var client = ApnsClient.CreateUsingJwt(_httpClientFactory.CreateClient("dotAPNS"), options);
            if (useSandbox)
                client.UseSandbox();
            return client;
        }
    }
}