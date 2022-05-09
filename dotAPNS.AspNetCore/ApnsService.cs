using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
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
        [Obsolete("useSandbox is ignored in here. Set in your push now. Use the async version.")]
        Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert, bool useSandbox = false);
        /// <summary>
        /// Use the async equivalent <see cref="SendPushAsync(ApplePush, ApnsJwtOptions, bool, CancellationToken)"/>
        /// </summary>
        /// <param name="push"></param>
        /// <param name="jwtOptions"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete("useSandbox is ignored in here. Set in your push now. Use the async version.")]
        Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false);
        /// <summary>
        /// Use the Async equivalent <see cref="SendPushesAsync(IReadOnlyCollection{ApplePush}, X509Certificate2, bool, CancellationToken)"/>
        /// </summary>
        /// <param name="pushes"></param>
        /// <param name="cert"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete("useSandbox is ignored in here. Set in your push now. Use the async version.")]
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false);
        /// <summary>
        /// Use the async equivalent <see cref="SendPushesAsync(IReadOnlyCollection{ApplePush}, ApnsJwtOptions, bool, CancellationToken)"/>
        /// </summary>
        /// <param name="pushes"></param>
        /// <param name="jwtOptions"></param>
        /// <param name="useSandbox"></param>
        /// <returns></returns>
        [Obsolete("useSandbox is ignored in here. Set in your push now. Use the async version.")]
        Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false);


        Task<ApnsResponse> SendPushAsync(ApplePush push, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default);
        Task<ApnsResponse> SendPushAsync(ApplePush push, ApnsJwtOptions? jwtOptions = null, bool useSandbox = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<ApnsResponse>> SendBatchAsync(ApplePush push, ApnsJwtOptions? jwtOptions = null, CancellationToken cancellationToken = default);
        Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default);
        Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions? jwtOptions = null, bool useSandbox = false, CancellationToken cancellationToken = default);
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
        public Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert, bool useSandbox = false) => SendPushAsync(push, cert);
        [Obsolete]
        public Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions, bool useSandbox = false) => SendPushAsync(push, jwtOptions);
        [Obsolete]
        public Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, bool useSandbox = false) => SendPushesAsync(pushes, cert);
        [Obsolete]
        public Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions, bool useSandbox = false) => SendPushesAsync(pushes, jwtOptions);

        public Task<ApnsResponse> SendPushAsync(ApplePush push, X509Certificate2 cert, bool useSandbox = false, CancellationToken cancellationToken = default)
        {
            string clientCacheId = (useSandbox ? "s_" : "") + cert.Thumbprint;
            var client = _cachedCertClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingCert(cert, _options.DisableServerCertificateValidation));

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
        public Task<IEnumerable<ApnsResponse>> SendBatchAsync(ApplePush push, ApnsJwtOptions? jwtOptions = null, CancellationToken cancellationToken = default)
        {
            Guard.IsTrue(push.IsBatched, "IsBatched", "SendBatchAsync must be used only with batched push notifications. Use SendAsync if you want to send a single push.");
            if (jwtOptions == null)
            {
                jwtOptions = _options.DefaultApnsJwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions), "Cannot be null if no DefaultApnsJwtOptions are provided.");
            }
            string clientCacheId = (push.IsSandbox ? "s_" : "") + jwtOptions.BundleId;
            var client = _cachedJwtClients.GetOrAdd(clientCacheId, _ =>
                _apnsClientFactory.CreateUsingJwt(jwtOptions, _options.DisableServerCertificateValidation));
            try
            {
                return client.SendBatchAsync(push, cancellationToken);
            }
            catch
            {
                _cachedJwtClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }

        public Task<ApnsResponse> SendPushAsync(ApplePush push, ApnsJwtOptions? jwtOptions = null, CancellationToken cancellationToken = default)
        {
            if (jwtOptions == null)
            {
                jwtOptions = _options.DefaultApnsJwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions), "Cannot be null if no DefaultApnsJwtOptions are provided.");
            }
            string clientCacheId = jwtOptions.BundleId;
            var client = _cachedJwtClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingJwt(jwtOptions, _options.DisableServerCertificateValidation));
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

        public async Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert, CancellationToken cancellationToken = default) //TODO implement concurrent sendings
        {
            if (string.IsNullOrWhiteSpace(cert.Thumbprint))
                throw new InvalidOperationException("Certificate does not have a thumbprint.");

            string clientCacheId = cert.Thumbprint;
            var client = _cachedCertClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingCert(cert, _options.DisableServerCertificateValidation));

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

        public async Task<List<ApnsResponse>> SendPushesAsync(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions? jwtOptions = null, CancellationToken cancellationToken = default)
        {
            if(jwtOptions == null)
            {
                jwtOptions = _options.DefaultApnsJwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions), "Cannot be null if no DefaultApnsJwtOptions are provided.");
            }
            string clientCacheId = jwtOptions.BundleId;
            var client =  _cachedJwtClients.GetOrAdd(clientCacheId, _ => 
                _apnsClientFactory.CreateUsingJwt(jwtOptions, _options.DisableServerCertificateValidation));
            
            try
            {
#if NET6_0_OR_GREATER
                var results = new ConcurrentBag<ApnsResponse>();
                var concurrent = new ConcurrentBag<ApplePush>(pushes);
                await Parallel.ForEachAsync(concurrent, cancellationToken, async (push, token) => 
                {
                    results.Add(await client.SendAsync(push, token).ConfigureAwait(false));
                });
#else
                var tasks = new List<Task<ApnsResponse>>();
                foreach(var push in pushes)
                {
                    tasks.Add(client.SendAsync(push, cancellationToken));
                }
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
                return results.ToList();
            }
            catch
            {
                _cachedJwtClients.TryRemove(clientCacheId, out _);
                throw;
            }
        }
    }
}
