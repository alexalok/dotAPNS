using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace dotAPNS.AspNetCore
{
    public interface IApnsService
    {
        /// <summary>
        /// Use the async equivalent <see cref="SendPushAsync"/>
        /// </summary>
        /// <param name="push"></param>
        /// <param name="cert"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete]
        Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert, bool useSandbox = false);
        /// <summary>
        /// Use the async equivalent <see cref="SendPushAsync(ApplePush, ApnsJwtOptions, bool, CancellationToken)"/>
        /// </summary>
        /// <param name="push"></param>
        /// <param name="jwtOptions"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete]
        Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false);
        /// <summary>
        /// Use the Async equivalent <see cref="SendPushesAsync(IReadOnlyCollection{ApplePush}, X509Certificate2, bool, CancellationToken)"/>
        /// </summary>
        /// <param name="pushes"></param>
        /// <param name="cert"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete]
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false);
        /// <summary>
        /// Use the async equivalent <see cref="SendPushesAsync(IReadOnlyCollection{ApplePush}, ApnsJwtOptions, bool, CancellationToken)"/>
        /// </summary>
        /// <param name="pushes"></param>
        /// <param name="jwtOptions"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete]
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false);


        Task<ApnsResponse> SendPushAsync(ApplePush push, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default);
        Task<ApnsResponse> SendPushAsync(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false, CancellationToken cancellationToken = default);
        Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default);
        Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false, CancellationToken cancellationToken = default);
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
        [Obsolete]
        public Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert, bool useSandbox = false) => SendPushAsync(push, cert, useSandbox);
        [Obsolete]
        public Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false) => SendPushAsync(push, jwtOptions, useSandbox);
        [Obsolete]
        public Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false) => SendPushesAsync(pushes, cert, useSandbox);
        [Obsolete]
        public Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false) => SendPushesAsync(pushes, jwtOptions, useSandbox);

        public Task<ApnsResponse> SendPushAsync(ApplePush push, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + cert.Thumbprint;
            var client = _cachedCertClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingCert(cert, useSandbox, _options.DisableServerCertificateValidation));

            try
            {
                return client.SendAsync(push, cancellationToken);
            }
            catch
            {
                _cachedCertClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }

        public Task<ApnsResponse> SendPushAsync(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false, CancellationToken cancellationToken = default)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + jwtOptions.BundleId;
            var client = _cachedJwtClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingJwt(jwtOptions, useSandbox, _options.DisableServerCertificateValidation));
            try
            {
                return client.SendAsync(push, cancellationToken);
            }
            catch
            {
                _cachedJwtClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }

        public async Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default) //TODO implement concurrent sendings
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
                    result.Add(await client.SendAsync(push, cancellationToken));
                return result;
            }
            catch
            {
                _cachedCertClients.TryRemove(cert.Thumbprint, out _);
                throw;
            }
        }

        public async Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false, CancellationToken cancellationToken = default)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + jwtOptions.BundleId;
            var client =  _cachedJwtClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingJwt(jwtOptions, useSandbox, _options.DisableServerCertificateValidation));
            var result = new List<ApnsResponse>(pushes.Count);
            try
            {
                foreach (var push in pushes)
                    result.Add(await client.SendAsync(push, cancellationToken));
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
