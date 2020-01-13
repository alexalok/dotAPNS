using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if !NET46
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
#endif

namespace dotAPNS
{
    public interface IApnsClient
    {
        Task<ApnsResponse> Send(ApplePush push);
    }

    public class ApnsClient : IApnsClient
    {
        const string DevelopmentEndpoint = "https://api.development.push.apple.com:443/3/device/";
        const string ProductionEndpoint = "https://api.push.apple.com:443/3/device/";

#if NET46
        readonly CngKey _key;
#else
        readonly ECDsa _key;
#endif

        readonly string _keyId;
        readonly string _teamId;

        string _jwt;
        DateTime _lastJwtGenerationTime;

        readonly HttpClient _http;
        readonly bool _useCert;

        /// <summary>
        /// True if certificate provided can only be used for 'voip' type pushes, false otherwise.
        /// </summary>
        readonly bool _isVoipCert;

        readonly string _bundleId;
        bool _useSandbox;

        ApnsClient(HttpClient http, [NotNull] X509Certificate cert)
        {
            _http = http;
            var split = cert.Subject.Split(new[] { "0.9.2342.19200300.100.1.1=" }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
                throw new InvalidOperationException("Provided certificate does not appear to be a valid APNs certificate.");
            string topic = split[1];
            _isVoipCert = topic.EndsWith(".voip");
            _bundleId = split[1].Replace(".voip", "");
            _useCert = true;
        }

        ApnsClient([NotNull] HttpClient http, [NotNull]
#if NET46 
                   CngKey
#else
                   ECDsa
#endif
                   key, [NotNull] string keyId, [NotNull] string teamId, [NotNull] string bundleId)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _key = key ?? throw new ArgumentNullException(nameof(key));

            _keyId = keyId ?? throw new ArgumentNullException(nameof(keyId),
                $"Make sure {nameof(ApnsJwtOptions)}.{nameof(ApnsJwtOptions.KeyId)} is set to a non-null value.");

            _teamId = teamId ?? throw new ArgumentNullException(nameof(teamId),
                $"Make sure {nameof(ApnsJwtOptions)}.{nameof(ApnsJwtOptions.TeamId)} is set to a non-null value.");

            _bundleId = bundleId ?? throw new ArgumentNullException(nameof(bundleId),
                $"Make sure {nameof(ApnsJwtOptions)}.{nameof(ApnsJwtOptions.BundleId)} is set to a non-null value.");
        }

        public async Task<ApnsResponse> Send(ApplePush push)
        {
            if (_useCert)
            {
                if (_isVoipCert && push.Type != ApplePushType.Voip)
                    throw new InvalidOperationException("Provided certificate can only be used to send 'voip' type pushes.");
            }

            var payload = push.GeneratePayload();

            var req = new HttpRequestMessage(HttpMethod.Post, (_useSandbox ? DevelopmentEndpoint : ProductionEndpoint) + (push.Token ?? push.VoipToken));
            req.Version = new Version(2, 0);
            req.Headers.Add("apns-priority", push.Priority.ToString());
            req.Headers.Add("apns-push-type", push.Type.ToString().ToLowerInvariant());
            req.Headers.Add("apns-topic", GetTopic(push.Type));
            if (!_useCert)
                req.Headers.Authorization = new AuthenticationHeaderValue("bearer", GetOrGenerateJwt());

            req.Content = new JsonContent(payload);

            var resp = await _http.SendAsync(req);
            string respContent = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                //{"reason":"DeviceTokenNotForTopic"}

                dynamic obj = JObject.Parse(respContent);
                if (!Enum.TryParse((string)obj.reason, out ApnsResponseReason errReason))
                    errReason = ApnsResponseReason.Unknown;
                return ApnsResponse.Error(errReason, (string)obj.reason);
            }

            return ApnsResponse.Successful();
        }

        public static ApnsClient CreateUsingJwt([NotNull] HttpClient http, [NotNull]ApnsJwtOptions options)
        {
            if (http == null) throw new ArgumentNullException(nameof(http));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IEnumerable<string> certContent;
            if (options.CertFilePath != null)
            {
                Debug.Assert(options.CertContent == null);
                certContent = File.ReadAllLines(options.CertFilePath)
                    .Where(l => !l.StartsWith("-"));
            }
            else if (options.CertContent != null)
            {
                Debug.Assert(options.CertFilePath == null);
                string delimeter = options.CertContent.Contains("\r\n") ? "\r\n" : "\n";
                certContent = options.CertContent
                    .Split(new[] { delimeter }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !l.StartsWith("-"));
            }
            else
            {
                throw new ArgumentException("Either certificate file path or certificate contents must be provided.", nameof(options));
            }

            string base64 = string.Join("", certContent);

#if !NET46
            base64 = $"-----BEGIN PRIVATE KEY-----\n{base64}\n-----END PRIVATE KEY-----";
            var ecPrivateKeyParameters = (ECPrivateKeyParameters)new PemReader(new StringReader(base64)).ReadObject();
            var x = ecPrivateKeyParameters.Parameters.G.AffineXCoord.GetEncoded();
            var y = ecPrivateKeyParameters.Parameters.G.AffineYCoord.GetEncoded();
            var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();
            var msEcp = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = { X = x, Y = y }, D = d };
            var key = ECDsa.Create(msEcp);
#else
            var key = CngKey.Import(Convert.FromBase64String(base64), CngKeyBlobFormat.Pkcs8PrivateBlob);
#endif
            return new ApnsClient(http, key, options.KeyId, options.TeamId, options.BundleId);
        }

        public static ApnsClient CreateUsingCert([NotNull] X509Certificate2 cert)
        {
#if NETSTANDARD2_0 || NET46
            throw new NotSupportedException(
                "Certificate-based connection is not supported on all .NET Framework versions and on .NET Core 2.x or lower. " +
                "For more information, see: https://github.com/alexalok/dotAPNS/issues/6");
#elif NETSTANDARD2_1
            if (cert == null) throw new ArgumentNullException(nameof(cert));

            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;

            handler.ClientCertificates.Add(cert);
            var client = new HttpClient(handler);

            return CreateUsingCustomHttpClient(client, cert);
#endif
        }

        public static ApnsClient CreateUsingCustomHttpClient([NotNull] HttpClient httpClient, [NotNull] X509Certificate2 cert)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            if (cert == null) throw new ArgumentNullException(nameof(cert));

            var apns = new ApnsClient(httpClient, cert);
            return apns;
        }

        public static ApnsClient CreateUsingCert([NotNull] string pathToCert)
        {
            if (string.IsNullOrWhiteSpace(pathToCert))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(pathToCert));

            var cert = new X509Certificate2(pathToCert);
            return CreateUsingCert(cert);
        }

        public ApnsClient UseSandbox()
        {
            _useSandbox = true;
            return this;
        }

        string GetTopic(ApplePushType pushType)
        {
            switch (pushType)
            {
                case ApplePushType.Background:
                case ApplePushType.Alert:
                    return _bundleId;
                    break;
                case ApplePushType.Voip:
                    return _bundleId + ".voip";
                case ApplePushType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(pushType), pushType, null);
            }
        }

        string GetOrGenerateJwt()
        {
            if (_lastJwtGenerationTime > DateTime.UtcNow - TimeSpan.FromMinutes(20)) // refresh no more than once every 20 minutes
                return _jwt;
            var now = DateTimeOffset.UtcNow;

            string header = JsonConvert.SerializeObject((new { alg = "ES256", kid = _keyId }));
            string payload = JsonConvert.SerializeObject(new { iss = _teamId, iat = now.ToUnixTimeSeconds() });

            string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(header));
            string payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            string unsignedJwtData = $"{headerBase64}.{payloadBase64}";

            byte[] signature;
#if NET46
            using (var dsa = new ECDsaCng(_key))
            {
                dsa.HashAlgorithm = CngAlgorithm.Sha256;
                signature = dsa.SignData(Encoding.UTF8.GetBytes(unsignedJwtData));
            }
#else
            signature = _key.SignData(Encoding.UTF8.GetBytes(unsignedJwtData), HashAlgorithmName.SHA256);
#endif
            _jwt = $"{unsignedJwtData}.{Convert.ToBase64String(signature)}";
            _lastJwtGenerationTime = now.UtcDateTime;
            return _jwt;
        }
    }
}