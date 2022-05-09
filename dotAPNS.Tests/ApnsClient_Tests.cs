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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Nito.AsyncEx;

namespace dotAPNS.Tests
{
    [TestCategory("certs")]
    [TestClass]
    public class ApnsClient_Tests
    {
        readonly CertificateFixture _certs;

        public ApnsClient_Tests()
        {
            _certs = new CertificateFixture();
        }
        //I do not have a voip cert so I cannot run this test.
        //[TestMethod]
        public async Task Sending_NonVoip_Type_With_Voip_Cert_Fails()
        {
#if !NETCOREAPP3_1
            return;
#endif
            var apns = ApnsClient.CreateUsingCert(_certs.P12Cert);
            var push = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await apns.SendAsync(push));

            var batchPush = CreateBatchedPush();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await apns.SendBatchAsync(batchPush));
        }

        [TestMethod]
        public void Creating_Client_With_Cert_Not_Fails_Only_On_NetCore3_0()
        {
#if NETCOREAPP3_0
            Assert.ThrowsException<NotSupportedException>(() => ApnsClient.CreateUsingCert(_certs.P12Cert));
#else
            ApnsClient.CreateUsingCert(_certs.P12Cert);
#endif
        }

        [TestMethod]
        public async Task SendBatchAsync_Fails_With_NonBatched_Push()
        {
            var (client, _) = BoostrapApnsClient();
            var push = CreateStubPush();
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await client.SendBatchAsync(push));
        }

        [TestMethod]
        public async Task SendAsync_Fails_With_Batched_Push()
        {
            var (client, _) = BoostrapApnsClient();
            var push = CreateBatchedPush();
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await client.SendAsync(push));
        }

        [TestMethod]
        public async Task SendBatchAsync_Not_Throws()
        {
            var (client, _) = BoostrapApnsClient();
            var push = CreateBatchedPush();
            await client.SendBatchAsync(push);
        }

        [TestMethod]
        public async Task Sending_Push_Not_Throws()
        {
            var (client, _) = BoostrapApnsClient();
            var push = CreateStubPush();
            await client.SendAsync(push);
        }

        [TestMethod]
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

        [DataTestMethod]
        [DynamicData(nameof(Ensure_Error_When_Sending_Push_Is_Correctly_Handled_Data))]
        public async Task Ensure_Error_When_Sending_Push_Is_Correctly_Handled(int statusCode, string payload, ApnsResponse expectedResponse)
        {
            var (apns, _) = BoostrapApnsClient(statusCode, payload);
            var push = CreateStubPush();
            var resp = await apns.SendAsync(push);

            expectedResponse.Should().BeEquivalentTo(resp);
        }

        [DataTestMethod]
        [DynamicData(nameof(Ensure_Error_When_Sending_Push_Is_Correctly_Handled_Data))]
        public async Task Ensure_Error_When_SendingBatch_Push_Is_Correctly_Handled(int statusCode, string payload, ApnsResponse expectedResponse)
        {
            var (apns, _) = BoostrapApnsClient(statusCode, payload);
            var push = CreateBatchedPush();
            var resps = await apns.SendBatchAsync(push);
            foreach(var resp in resps)
            {
                expectedResponse.Should().BeEquivalentTo(resp);
            }
        }

        [TestMethod]
        public async Task Ensure_Push_Expiration_Setting_Is_Respected()
        {
            var now = DateTimeOffset.UtcNow;
            long unixNow = now.ToUnixTimeSeconds();
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();
            push.AddExpiration(now);

            await apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-expiration").Value.Single() == unixNow.ToString()),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [TestMethod]
        public async Task Ensure_Push_Expiration_Setting_Is_Respected_SendBatchAsync()
        {
            var now = DateTimeOffset.UtcNow;
            long unixNow = now.ToUnixTimeSeconds();
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateBatchedPush();
            push.AddExpiration(now);

            await apns.SendBatchAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Exactly(push.Tokens.Count),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-expiration").Value.Single() == unixNow.ToString()),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [TestMethod]
        public async Task Ensure_Push_Immediate_Expiration_Setting_Is_Respected()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateStubPush();
            push.AddImmediateExpiration();

            await apns.SendAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-expiration").Value.Single() == "0"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [TestMethod]
        public async Task Ensure_Push_Immediate_Expiration_Setting_Is_Respected_SendBatchAsync()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateBatchedPush();
            push.AddImmediateExpiration();

            await apns.SendBatchAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Exactly(push.Tokens.Count),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-expiration").Value.Single() == "0"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [TestMethod]
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
        [TestMethod]
        public async Task Ensure_SendBatchAsync_Does_Not_Deadlock_When_Sync_Waiting_On_Result_With_Sync_Context()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateBatchedPush();

            // This always runs on a theadpool thread and sets single-threaded ctx only for that thread.
            // Thereby, we're guaranteed to not be locked when Task.Delay-ing below if ctx is set before
            // Task.Delay has a chance to capture the current ctx.
            var t = Task.Run(() =>
            {
                var singleThreadedSyncCtx = new AsyncContext().SynchronizationContext;
                SynchronizationContext.SetSynchronizationContext(singleThreadedSyncCtx);
                _ = apns.SendBatchAsync(push).Result;
            });
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (!t.IsCompleted)
                throw new Exception("Code has deadlocked.");
        }

        [DataTestMethod]
        [DataRow(true, true, "2197")]
        [DataRow(true, false, "2197")]
        [DataRow(false, true, "")]
        [DataRow(false, false, "")]
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

        [DataTestMethod]
        [DataRow(true, true, "2197")]
        [DataRow(true, false, "2197")]
        [DataRow(false, true, "")]
        [DataRow(false, false, "")]
        public async Task SendBatchAsync_Uses_Correct_Port(bool useBackupPort, bool useSandbox, string expectedPort)
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateBatchedPush();
            if (useBackupPort)
                apns.UseBackupPort();
            if (useSandbox)
                apns.UseSandbox();

            await apns.SendBatchAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Exactly(push.Tokens.Count),
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri.GetComponents(UriComponents.Port, UriFormat.Unescaped) ==
                        expectedPort),
                    ItExpr.IsAny<CancellationToken>());
        }

        [DataTestMethod]
        [DataRow(false, false, ApnsClient.ProductionEndpoint)]
        [DataRow(false, true, ApnsClient.DevelopmentEndpoint)]
        [DataRow(true, false, ApnsClient.DevelopmentEndpoint)] 
        [DataRow(true, true, ApnsClient.DevelopmentEndpoint)]
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

        [DataTestMethod]
        [DataRow(false, false, ApnsClient.ProductionEndpoint)]
        [DataRow(false, true, ApnsClient.DevelopmentEndpoint)]
        [DataRow(true, false, ApnsClient.DevelopmentEndpoint)]
        [DataRow(true, true, ApnsClient.DevelopmentEndpoint)]
        public async Task SendBatchAsync_Should_Use_Correct_Environment_Server(bool isClientDevelopment, bool isPushDevelopment,
            string expectedUrl)
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();

            var push = CreateBatchedPush(isPushDevelopment); //order is important, first SendToDevServer then AddToken

            if (isClientDevelopment)
                apns.UseSandbox();

            await apns.SendBatchAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Exactly(push.Tokens.Count),
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri.GetComponents(UriComponents.Scheme | UriComponents.Host, UriFormat.Unescaped) ==
                        expectedUrl),
                    ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public Task SendAsync_Throws_When_Canceled()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient(delay:TimeSpan.FromSeconds(10));
            var push = CreateStubPush();
            return Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                Task sendTask = apns.SendAsync(push, cts.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(1));
                cts.Cancel();
                await sendTask;
            });
        }
        [TestMethod]
        public async Task SendBatchAsync_Throws_When_Canceled()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient(delay: TimeSpan.FromSeconds(10));
            var push = CreateBatchedPush();
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                Task sendTask = apns.SendBatchAsync(push, cts.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(1));
                cts.Cancel();
                await sendTask;
            });
        }

        [TestMethod]
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

        [TestMethod]
        public async Task Adding_Collapse_Id_Sets_Header_SendBatchAsync()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateBatchedPush();
            push.AddCollapseId("test_collapse_id");

            await apns.SendBatchAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Exactly(push.Tokens.Count),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.Single(h => h.Key == "apns-collapse-id").Value.Single() == "test_collapse_id"),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [TestMethod]
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

        [TestMethod]
        public async Task No_Collapse_Id_Header_If_Collapse_Id_Is_Not_Added_SendBatchAsync()
        {
            var (apns, httpHandlerMock) = BoostrapApnsClient();
            var push = CreateBatchedPush();

            await apns.SendBatchAsync(push);

            httpHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Exactly(push.Tokens.Count),
                    ItExpr.Is<HttpRequestMessage>(m => m.Headers.All(h => h.Key != "apns-collapse-id")),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [TestMethod]
        public async Task Ensure_SendAsync_Throws_On_Expired_Certificate_On_Windows()
        {
            if(
#if NET5_0_OR_GREATER
                OperatingSystem.IsWindows()
#else
                Environment.OSVersion.Platform == PlatformID.Win32NT
#endif       
                )
            {
                // Arrange
                var ex = new HttpRequestException(null,
                    new AuthenticationException(null,
                        new Win32Exception(-2146893016)));
                var (apns, httpHandlerMock) = BoostrapApnsClient(throwOnResponse: ex);
                var push = CreateStubPush();

                // Act and Assert
                await Assert.ThrowsExceptionAsync<ApnsCertificateExpiredException>(() => apns.SendAsync(push));
            }
        }

        [TestMethod]
        public async Task Ensure_SendBatchAsync_Throws_On_Expired_Certificate_On_Windows()
        {
            if (
#if NET5_0_OR_GREATER
                OperatingSystem.IsWindows()
#else
                Environment.OSVersion.Platform == PlatformID.Win32NT
#endif       
                )
            {
                // Arrange
                var ex = new HttpRequestException(null,
                    new AuthenticationException(null,
                        new Win32Exception(-2146893016)));
                var (apns, httpHandlerMock) = BoostrapApnsClient(throwOnResponse: ex);
                var push = CreateBatchedPush();

                // Act and Assert
                await Assert.ThrowsExceptionAsync<ApnsCertificateExpiredException>(() => apns.SendBatchAsync(push));
            }
        }

        [TestMethod]
        public async Task Ensure_SendAsync_Throws_On_Expired_Certificate_On_Linux()
        {
            if (
#if NET5_0_OR_GREATER
                OperatingSystem.IsLinux()
#else
                Environment.OSVersion.Platform == PlatformID.Unix
#endif  
                )
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
                await Assert.ThrowsExceptionAsync<ApnsCertificateExpiredException>(() => apns.SendAsync(push));
            }
        }
        [TestMethod]
        public async Task Ensure_SendBatchAsync_Throws_On_Expired_Certificate_On_Linux()
        {
            if (
#if NET5_0_OR_GREATER
                OperatingSystem.IsLinux()
#else
                Environment.OSVersion.Platform == PlatformID.Unix
#endif  
                )
            {
                // Arrange
                var ex = new HttpRequestException(null,
                    new IOException(null,
                        new IOException(null,
                            new IOException(null,
                                new Exception(null,
                                    new ExternalException(null, 336151573))))));
                var (apns, httpHandlerMock) = BoostrapApnsClient(throwOnResponse: ex);
                var push = CreateBatchedPush();

                // Act and Assert
                await Assert.ThrowsExceptionAsync<ApnsCertificateExpiredException>(() => apns.SendBatchAsync(push));
            }
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

        (ApnsClient apns, Mock<HttpMessageHandler> httpHandlerMock) BoostrapApnsClient(int statusCode = 200, string responseContent = "{}", TimeSpan delay=default, Exception? throwOnResponse = null)
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

        ApplePush CreateBatchedPush(bool dev = false)
        {
            var push = new ApplePush(ApplePushType.Alert);
            if (dev)
            {
                push = push.SendToDevelopmentServer();
            }
            push.AsBatched()
                .AddToken("token1")
                .AddToken("token2");
            return push;
        }

        ApnsJwtOptions CreateStubJwt()
        {
            var jwt = new ApnsJwtOptions() { BundleId = "bundle", CertContent = _certs.P8CertData, KeyId = "key", TeamId = "team" };
            return jwt;
        }
    }
}