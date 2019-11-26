using System;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApnsClient_Tests
    {
        [Fact]
        public void Sending_NonVoip_Type_With_Voip_Cert_Fails()
        {
            var apns = ApnsClient.CreateUsingCert("voip.p12");
            var push = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await apns.Send(push));
        }
    }
}