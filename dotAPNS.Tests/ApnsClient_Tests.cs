using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using dotAPNS.AspNetCore;
using Moq;
using Moq.Protected;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApnsClient_Tests
    {
        const string CertContent = "-----BEGIN PRIVATE KEY-----\r\nMIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgir767IOFOYHsYtNQ\r\nwsvLeJVu3bxCLL/SURQvMZw6QumgCgYIKoZIzj0DAQehRANCAARuwGOLtHY99zLl\r\niyACJp6xmj6YfE8bOLxHTZGkoC/+yNgf/fBpwf5Nin2pzyM8FUOYXg1R1v2bQqJy\r\nwHYtSkc1\r\n-----END PRIVATE KEY-----";

        [Fact]
        public void Sending_NonVoip_Type_With_Voip_Cert_Fails()
        {
            var apns = ApnsClient.CreateUsingCert("voip.p12");
            var push = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await apns.Send(push));
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

            var client = ApnsClient.CreateUsingJwt(new HttpClient(httpHandler.Object), new ApnsJwtOptions() {BundleId = "bundle", CertContent = CertContent, KeyId = "key", TeamId = "team"});
            return client;
        }
    }
}