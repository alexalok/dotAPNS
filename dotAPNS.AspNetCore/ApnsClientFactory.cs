using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;

namespace dotAPNS.AspNetCore
{
    public interface IApnsClientFactory
    {
        IApnsClient CreateUsingCert([NotNull] X509Certificate2 cert);
        IApnsClient CreateUsingCert([NotNull] string pathToCert);
        IApnsClient CreateUsingJwt([NotNull] ApnsJwtOptions options);
    }

    public class ApnsClientFactory : IApnsClientFactory
    {
        readonly IHttpClientFactory _httpClientFactory;

        public ApnsClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IApnsClient CreateUsingCert(X509Certificate2 cert) => ApnsClient.CreateUsingCert(cert);

        public IApnsClient CreateUsingCert(string pathToCert) => ApnsClient.CreateUsingCert(pathToCert);

        public IApnsClient CreateUsingJwt(ApnsJwtOptions options) => ApnsClient.CreateUsingJwt(_httpClientFactory.CreateClient("dotAPNS"), options);
    }
}