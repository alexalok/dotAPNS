using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace dotAPNS.Tests
{
    [Collection("certs")]
    public class ApnsClient_Tests
    {
        readonly CertificateFixture _certs;

        public ApnsClient_Tests(CertificateFixture certs)
        {
            _certs = certs;
        }

        [Fact]
        public void Sending_NonVoip_Type_With_Voip_Cert_Fails()
        {
#if !NETCOREAPP3_0
            return;
#endif
            var apns = ApnsClient.CreateUsingCert(_certs.P12Cert);
            var push = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await apns.Send(push));
        }

        public void Creating_Client_With_Cert_Not_Fails_Only_On_NetCore3_0()
        {
#if !NETCOREAPP3_0
            Assert.Throws<NotSupportedException>(() => ApnsClient.CreateUsingCert(_certs.P12Cert));
#else
            ApnsClient.CreateUsingCert(_certs.P12Cert);
#endif
        }

        [Fact]
        public async Task Sending_Push_Not_Throws()
        {
            var client = BoostrapApnsClient();
            var push = ApplePush.CreateAlert("body")
                .AddToken("token")
                .AddBadge(1)
                .AddSound()
                .AddLocation("location");
            await client.Send(push);
        }

        IApnsClient BoostrapApnsClient()
        {
            var httpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new JsonContent("{}")
                });

            var client = ApnsClient.CreateUsingJwt(new HttpClient(httpHandler.Object), new ApnsJwtOptions() { BundleId = "bundle", CertContent = _certs.P8CertData, KeyId = "key", TeamId = "team" });
            return client;
        }
    }
}