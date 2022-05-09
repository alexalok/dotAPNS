#if !NET46
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotAPNS.AspNetCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace dotAPNS.Tests
{
    [TestCategory("certs")]
    [TestClass]
    public class ApnsService_Tests
    {
        readonly CertificateFixture _certs;

        public ApnsService_Tests()
        {
            _certs = new CertificateFixture();
        }

        [TestMethod]
        public async Task Ensure_Client_Caching_Works_With_Jwt()
        {
            var service = BoostrapApnsService();
            var jwtOpt1 = new ApnsJwtOptions()
            {
                KeyId = "1234567890",
                TeamId = "1234567890",
                BundleId = "bundleid1",
                CertContent = _certs.P8CertData
            };
            var jwtOpt2 = new ApnsJwtOptions()
            {
                KeyId = "1234567890",
                TeamId = "1234567890",
                BundleId = "bundleid2",
                CertContent = _certs.P8CertData
            };
            var firstPush = ApplePush.CreateContentAvailable().AddToken("token");
            var secondPush = ApplePush.CreateAlert(new ApplePushAlert(null, "body")).AddToken("token");
            var thirdPush = ApplePush.CreateAlert(new ApplePushAlert(null, "body")).AddToken("token");
            var fourthPush = ApplePush.CreateContentAvailable().AddToken("token");

            await service.SendPush(firstPush, jwtOpt1);
            await service.SendPush(secondPush, jwtOpt1);
            await service.SendPush(thirdPush, jwtOpt2);
            await service.SendPush(fourthPush, jwtOpt2);

            Assert.AreEqual(2, ((IDictionary)service.GetType().GetField("_cachedJwtClients", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service)).Count);
        }

        //[TestMethod] cert uses real handler. Can't test until refactored.
        public async Task Ensure_Client_Caching_Works_With_Cert()
        {
            var service = BoostrapApnsService();
            var firstPush = ApplePush.CreateContentAvailable().AddToken("token");
            var secondPush = ApplePush.CreateAlert(new ApplePushAlert(null, "body")).AddToken("token");

            await service.SendPush(firstPush, _certs.P12Cert);
            await service.SendPush(secondPush, _certs.P12Cert);

            Assert.IsTrue(((IDictionary)service.GetType().GetField("_cachedJwtClients", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service)).Count == 1);
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
                    Content = JsonContent.Create("{}")
                });
            httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(httpHandler.Object));
            var apnsClientFactory = new ApnsClientFactory(httpClientFactory.Object);
            var service = new ApnsService(apnsClientFactory, new OptionsWrapper<ApnsServiceOptions>(new ApnsServiceOptions()));
            return service;
        }

    }
}
#endif