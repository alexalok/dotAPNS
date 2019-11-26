using System.Collections;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotAPNS.AspNetCore;
using Moq;
using Moq.Protected;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApnsService_Tests
    {
        const string CertContent = "-----BEGIN PRIVATE KEY-----\r\nMIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgir767IOFOYHsYtNQ\r\nwsvLeJVu3bxCLL/SURQvMZw6QumgCgYIKoZIzj0DAQehRANCAARuwGOLtHY99zLl\r\niyACJp6xmj6YfE8bOLxHTZGkoC/+yNgf/fBpwf5Nin2pzyM8FUOYXg1R1v2bQqJy\r\nwHYtSkc1\r\n-----END PRIVATE KEY-----";

        [Fact]
        public async Task Ensure_Client_Caching_Works_With_Jwt()
        {
            var service = BoostrapApnsService();
            var jwtOpt1 = new ApnsJwtOptions()
            {
                KeyId = "1234567890",
                TeamId = "1234567890",
                BundleId = "bundleid1",
                CertContent = CertContent
            };
            var jwtOpt2 = new ApnsJwtOptions()
            {
                KeyId = "1234567890",
                TeamId = "1234567890",
                BundleId = "bundleid2",
                CertContent = CertContent
            };
            var firstPush = ApplePush.CreateContentAvailable().AddToken("token");
            var secondPush = ApplePush.CreateAlert(new ApplePushAlert(null, "body")).AddToken("token");
            var thirdPush = ApplePush.CreateAlert(new ApplePushAlert(null, "body")).AddToken("token");
            var fourthPush = ApplePush.CreateContentAvailable().AddToken("token");

            await service.SendPush(firstPush, jwtOpt1);
            await service.SendPush(secondPush, jwtOpt1);
            await service.SendPush(thirdPush, jwtOpt2);
            await service.SendPush(fourthPush, jwtOpt2);

            Assert.Equal(2, ((IDictionary)service.GetType().GetField("_cachedJwtClients", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service)).Count);
        }

        //[Fact] cert uses real handler. Can't test until refactored.
        public async Task Ensure_Client_Caching_Works_With_Cert()
        {
            var service = BoostrapApnsService();
            var firstPush = ApplePush.CreateContentAvailable().AddToken("token");
            var secondPush = ApplePush.CreateAlert(new ApplePushAlert(null, "body")).AddToken("token");

            await service.SendPush(firstPush, new X509Certificate2("voip.p12"));
            await service.SendPush(secondPush, new X509Certificate2("voip.p12"));

            Assert.Single((IDictionary)service.GetType().GetField("_cachedJwtClients", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service));
        }

        ApnsService BoostrapApnsService()
        {
            var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var httpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new JsonContent("{}")
                });
            httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(httpHandler.Object));
            var apnsClientFactory = new ApnsClientFactory(httpClientFactory.Object);
            var service = new ApnsService(apnsClientFactory);
            return service;
        }
    }
}