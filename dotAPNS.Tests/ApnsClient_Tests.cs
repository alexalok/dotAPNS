using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ExpectedObjects;
using JetBrains.Annotations;
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

        [Theory]
        [InlineData(true, true, "2197")]
        [InlineData(true, false, "2197")]
        [InlineData(false, true, "")]
        [InlineData(false, false, "")]
        public async Task SendAsync_Uses_Correct_Port(bool useBackupPort, bool useSandbox, string expectedPort)
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();
            if (useBackupPort)
                apns.UseBackupPort();
            if (useSandbox)
                apns.UseSandbox();

            await apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri.GetComponents(UriComponents.Port, UriFormat.Unescaped) ==
                        expectedPort),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData(false, false, ApnsClient.ProductionEndpoint)]
        [InlineData(false, true, ApnsClient.DevelopmentEndpoint)]
        [InlineData(true, false, ApnsClient.DevelopmentEndpoint)] 
        [InlineData(true, true, ApnsClient.DevelopmentEndpoint)]
        public async Task SendAsync_Should_Use_Correct_Environment_Server(bool isClientDevelopment, bool isPushDevelopment, 
            string expectedUrl)
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();

            var push = CreateStubPush();
            if (isPushDevelopment)
                push.SendToDevelopmentServer();

            if (isClientDevelopment)
                apns.UseSandbox();

            await apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri.GetComponents(UriComponents.Scheme | UriComponents.Host, UriFormat.Unescaped) ==
                        expectedUrl),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public Task SendAsync_Throws_When_Canceled()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient(delay:TimeSpan.FromSeconds(10));
            var push = CreateStubPush();
            return Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                Task sendTask = apns.SendAsync(push, cts.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(1));
                cts.Cancel();
                await sendTask;
            });
        }

        [Fact]
        public async Task Adding_Collapse_Id_Sets_Header()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();
            push.AddCollapseId("test_collapse_id");

            await apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-collapse-id").Value.Single() == "test_collapse_id"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [Fact]
        public async Task No_Collapse_Id_Header_If_Collapse_Id_Is_Not_Added()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();

            await apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.All(h => h.Key != "apns-collapse-id")),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [FactOnlyFor(PlatformID.Win32NT)]
        public async Task Ensure_SendAsync_Throws_On_Expired_Certificate_On_Windows()
        {
            // Arrange
            var ex = new HttpRequestException(null,
                new AuthenticationException(null,
                    new Win32Exception(- 2146893016)));
            var (apns, httpHandlerMock) = BoostrapApnsClient(throwOnResponse: ex);
            var push = CreateStubPush();

            // Act and Assert
            await Assert.ThrowsAsync<ApnsCertificateExpiredException>(() => apns.SendAsync(push));
        }

        [FactOnlyFor(PlatformID.Unix)]
        public async Task Ensure_SendAsync_Throws_On_Expired_Certificate_On_Linux()
        {
            // Arrange
            var ex = new HttpRequestException(null,
                new IOException(null,
                    new IOException(null,
                        new IOException(null,
                            new Exception(null,
                                new ExternalException(null, 336151573))))));
            var (apns, httpHandlerMock) = BoostrapApnsClient(throwOnResponse: ex);
            var push = CreateStubPush();

            // Act and Assert
            await Assert.ThrowsAsync<ApnsCertificateExpiredException>(() => apns.SendAsync(push));
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

        (ApnsClient apns, Mock<HttpMessageHandler> httpHandlerMock) BoostrapApnsClient(int statusCode = 200, string responseContent = "{}", TimeSpan delay=default, [CanBeNull] Exception throwOnResponse = null)
        {
            if(delay==default) delay = TimeSpan.FromMilliseconds(1);
            var httpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var sendAsyncSetup = httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                );

            if (throwOnResponse != null)
            {
                sendAsyncSetup.ThrowsAsync(throwOnResponse);
            }
            else
            {
                sendAsyncSetup.Returns<HttpRequestMessage, CancellationToken>(async (r, c) =>
                {
                    await Task.Delay(delay, c).ConfigureAwait(false); // technically library-side code, thus ignoring sync ctx
                    return new HttpResponseMessage()
                    {
                        StatusCode = (HttpStatusCode) statusCode,
                        Content = JsonContent.Create(responseContent)
                    };
                });
            }
                
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