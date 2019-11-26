using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace dotAPNS.AspNetCore
{
    public interface IApnsService
    {
        Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert);
        Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions);
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert);
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions);
    }

    public class ApnsService : IApnsService
    {
        readonly IApnsClientFactory _apnsClientFactory;

        // TODO implement expiration policy
        readonly ConcurrentDictionary<string, IApnsClient> _cachedCertClients = new ConcurrentDictionary<string, IApnsClient>(); // key is cert thumbprint
        readonly ConcurrentDictionary<string, IApnsClient> _cachedJwtClients = new ConcurrentDictionary<string, IApnsClient>(); // key is bundle id

        public ApnsService(IApnsClientFactory apnsClientFactory)
        {
            _apnsClientFactory = apnsClientFactory;
        }

        public Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert)
        {
            var client = GetOrCreateCached(cert);
            try
            {
                return client.Send(push);
            }
            catch
            {
                _cachedCertClients.TryRemove(cert.Thumbprint, out _);
                throw;
            }
        }

        public Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions)
        {
            var client = GetOrCreateCached(jwtOptions);
            try
            {
                return client.Send(push);
            }
            catch
            {
                _cachedJwtClients.TryRemove(jwtOptions.BundleId, out _);
                throw;
            }
        }

        public async Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert) //TODO implement concurrent sendings
        {
            var client = GetOrCreateCached(cert);

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

        public async Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions)
        {
            var client = GetOrCreateCached(jwtOptions);
            var result = new List<ApnsResponse>(pushes.Count);
            try
            {
                foreach (var push in pushes)
                    result.Add(await client.Send(push));
                return result;
            }
            catch
            {
                _cachedJwtClients.TryRemove(jwtOptions.BundleId, out _);
                throw;
            }
        }

        IApnsClient GetOrCreateCached(X509Certificate2 cert) =>
            _cachedCertClients.GetOrAdd(cert.Thumbprint ?? throw new InvalidOperationException("Certificate does not have a thumbprint."),
                _ => _apnsClientFactory.CreateUsingCert(cert));

        IApnsClient GetOrCreateCached(ApnsJwtOptions jwtOptions) =>
            _cachedJwtClients.GetOrAdd(jwtOptions.BundleId, _ => _apnsClientFactory.CreateUsingJwt(jwtOptions));
    }
}