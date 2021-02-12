using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace dotAPNS.AspNetCore
{
    public interface IApnsService
    {
        Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert, bool useSandbox = false);
        Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false);
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false);
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false);
    }

    public class ApnsService : IApnsService
    {
        readonly IApnsClientFactory _apnsClientFactory;
        readonly ApnsServiceOptions _options;

        // TODO implement expiration policy
        readonly ConcurrentDictionary<string, IApnsClient> _cachedCertClients = new ConcurrentDictionary<string, IApnsClient>(); // key is cert thumbprint and sandbox prefix
        readonly ConcurrentDictionary<string, IApnsClient> _cachedJwtClients = new ConcurrentDictionary<string, IApnsClient>(); // key is bundle id and sandbox prefix

        public ApnsService(IApnsClientFactory apnsClientFactory, IOptions<ApnsServiceOptions> options)
        {
            _apnsClientFactory = apnsClientFactory;
            _options = options.Value;
        }

        public Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert, bool useSandbox = false)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + cert.Thumbprint;
            var client = _cachedCertClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingCert(cert, useSandbox, _options.DisableServerCertificateValidation));

            try
            {
                return client.Send(push);
            }
            catch
            {
                _cachedCertClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }

        public Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + jwtOptions.BundleId;
            var client = _cachedJwtClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingJwt(jwtOptions, useSandbox, _options.DisableServerCertificateValidation));
            try
            {
                return client.Send(push);
            }
            catch
            {
                _cachedJwtClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }

        public async Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false) //TODO implement concurrent sendings
        {
            if (string.IsNullOrWhiteSpace(cert.Thumbprint))
                throw new InvalidOperationException("Certificate does not have a thumbprint.");

            string clientCacheId = (useSandbox ? "s_" : "") + cert.Thumbprint;
            var client = _cachedCertClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingCert(cert, useSandbox, _options.DisableServerCertificateValidation));

            var result = new List<ApnsResponse>(pushes.Count);
            try
            {
                foreach (var push in pushes)
                    result.Add(await client.Send(push));
                return result;
            }
            catch
            {
                _cachedCertClients.TryRemove(cert.Thumbprint, out _);
                throw;
            }
        }

        public async Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + jwtOptions.BundleId;
            var client =  _cachedJwtClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingJwt(jwtOptions, useSandbox, _options.DisableServerCertificateValidation));
            var result = new List<ApnsResponse>(pushes.Count);
            try
            {
                foreach (var push in pushes)
                    result.Add(await client.Send(push));
                return result;
            }
            catch
            {
                _cachedJwtClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }
    }
}
