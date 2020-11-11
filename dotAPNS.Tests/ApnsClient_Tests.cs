using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ExpectedObjects;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Bson;
using Nito.AsyncEx;
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
#if !NETCOREAPP3_1
            return;
#endif
            var apns = ApnsClient.CreateUsingCert(_certs.P12Cert);
            var push = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await apns.SendAsync(push));
        }

        [Fact]
        public void Creating_Client_With_Cert_Not_Fails_Only_On_NetCore3_0()
        {
#if !NETCOREAPP3_1
            Assert.Throws<NotSupportedException>(() => ApnsClient.CreateUsingCert(_certs.P12Cert));
#else
            ApnsClient.CreateUsingCert(_certs.P12Cert);
#endif
        }

        [Fact]
        public async Task Sending_Push_Not_Throws()
        {
            var (client, _) = BoostrapApnsClient();
            var push = CreateStubPush();
            await client.SendAsync(push);
        }

        [Fact]
        public void CreateUsingJwt_Using_Cert_Path_Succeeds()
        {
            var jwtOpt = new ApnsJwtOptions()
            {
                BundleId = "bundle",
                CertFilePath = _certs.P8CertPath,
                KeyId = "key",
                TeamId = "team"
            };
            var client = ApnsClient.CreateUsingJwt(new HttpClient(), jwtOpt);
        }

        [Theory]
        [MemberData(nameof(Ensure_Error_When_Sending_Push_Is_Correctly_Handled_Data))]
        public async Task Ensure_Error_When_Sending_Push_Is_Correctly_Handled(int statusCode, string payload, ApnsResponse expectedResponse)
        {
            var (apns, _) = BoostrapApnsClient(statusCode, payload);
            var push = CreateStubPush();
            var resp = await apns.SendAsync(push);

            expectedResponse.ToExpectedObject().ShouldEqual(resp);
        }

        [Fact]
        public void Ensure_Push_Expiration_Setting_Is_Respected()
        {
            var now = DateTimeOffset.UtcNow;
            long unixNow = now.ToUnixTimeSeconds();
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();
            push.AddExpiration(now);

            apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-expiration").Value.Single() == unixNow.ToString()),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [Fact]
        public void Ensure_Push_Immediate_Expiration_Setting_Is_Respected()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();
            push.AddImmediateExpiration();

            apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-expiration").Value.Single() == "0"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [SuppressMessage("ReSharper", "AsyncConverter.AsyncWait")]
        [Fact]
        public async Task Ensure_Send_Does_Not_Deadlock_When_Sync_Waiting_On_Result_With_Sync_Context()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();

            // This always runs on a theadpool thread and sets single-threaded ctx only for that thread.
            // Thereby, we're guaranteed to not be locked when Task.Delay-ing below if ctx is set before
            // Task.Delay has a chance to capture the current ctx.
            var t = Task.Run(() =>
            {
                var singleThreadedSyncCtx = new AsyncContext().SynchronizationContext;
                SynchronizationContext.SetSynchronizationContext(singleThreadedSyncCtx);
                _ = apns.SendAsync(push).Result;
            });
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (!t.IsCompleted)
                throw new Exception("Code has deadlocked.");
        }

        public static IEnumerable<object[]> Ensure_Error_When_Sending_Push_Is_Correctly_Handled_Data => new[]
        {
            new object[]
            {
                400,
                "{\"reason\":\"DeviceTokenNotForTopic\"}",
                ApnsResponse.Error(ApnsResponseReason.DeviceTokenNotForTopic, "DeviceTokenNotForTopic")
            },
            new object[]
            {
                410,
                "{\"reason\":\"Unregistered\",\"timestamp\":1454948015990}",
                ApnsResponse.Error(ApnsResponseReason.Unregistered, "Unregistered")
            }
        };

        (IApnsClient apns, Mock<HttpMessageHandler> httpHandlerMock) BoostrapApnsClient(int statusCode = 200, string responseContent = "{}")
        {
            var httpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Returns<HttpRequestMessage, CancellationToken>(async (r, c) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1), c).ConfigureAwait(false); // technically library-side code, thus ignoring sync ctx
                    return new HttpResponseMessage()
                    {
                        StatusCode = (HttpStatusCode) statusCode,
                        Content = new JsonContent(responseContent)
                    };
                });
            var jwt = CreateStubJwt();
            var client = ApnsClient.CreateUsingJwt(new HttpClient(httpHandler.Object), jwt);
            return (client, httpHandler);
        }

        ApplePush CreateStubPush()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddToken("token");
            return push;
        }

        ApnsJwtOptions CreateStubJwt()
        {
            var jwt = new ApnsJwtOptions() { BundleId = "bundle", CertContent = _certs.P8CertData, KeyId = "key", TeamId = "team" };
            return jwt;
        }
    }
}