using System;
using Newtonsoft.Json;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApplePush_Tests
    {
        [Fact]
        public void Adding_ContentAvailable_To_Push_With_Badge_or_Sound_Fails()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();

            Assert.Throws<InvalidOperationException>(() => pushWithContentAvailable.AddBadge(0));
            Assert.Throws<InvalidOperationException>(() => pushWithContentAvailable.AddSound("sound"));
        }

        [Fact]
        public void Adding_Token_and_VoipToken_Together_Fails()
        {
            var pushWithToken = ApplePush.CreateContentAvailable().AddToken("token");
            var pushWithVoipToken = ApplePush.CreateContentAvailable(true).AddVoipToken("voip");
            var alertPushWithToken = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.Throws<InvalidOperationException>(() => pushWithToken.AddVoipToken("voip"));
            Assert.Throws<InvalidOperationException>(() => pushWithVoipToken.AddToken("token"));
            Assert.Throws<InvalidOperationException>(() => alertPushWithToken.AddVoipToken("voip"));
        }

        [Fact]
        public void Ensure_Type_Correspond_To_Payload()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();
            var pushWithAlert = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.Equal(ApplePushType.Background, pushWithContentAvailable.Type);
            Assert.Equal(ApplePushType.Alert, pushWithAlert.Type);
        }

        [Fact]
        public void Ensure_Priority_Corresponds_To_Payload()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();
            var pushWithAlert = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.Equal(5, pushWithContentAvailable.Priority);
            Assert.Equal(10, pushWithAlert.Priority);
        }

        [Fact]
        public void Adding_Voip_To_Alert_Push_Throws_InvalidOperationException()
        {
            var alertPush = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.Throws<InvalidOperationException>(() => alertPush.AddVoipToken("voip"));
        }

        [Fact]
        public void Adding_Voip_To_NonVoip_Type_Throws_InvalidOperationException()
        {
            var backgroundPush = ApplePush.CreateContentAvailable();
            var alert = ApplePush.CreateContentAvailable();

            Assert.Throws<InvalidOperationException>(() => backgroundPush.AddVoipToken("voip"));
        }

        [Fact]
        public void Adding_Token_To_Voip_Type_Throws_InvalidOperationException()
        {
            var backgroundPush = ApplePush.CreateContentAvailable(true);

            Assert.Throws<InvalidOperationException>(() => backgroundPush.AddToken("token"));
        }

        [Fact]
        public void CreateContentAvailable_Has_Background_Type()
        {
            var voipPush = ApplePush.CreateContentAvailable();
            Assert.Equal(ApplePushType.Background, voipPush.Type);
        }

        [Fact]
        public void CreateContentAvailableAsVoip_Has_Voip_Type()
        {
            var voipPush = ApplePush.CreateContentAvailable(true);
            Assert.Equal(ApplePushType.Voip, voipPush.Type);
        }

        [Fact]
        public void AddCustomProperty_Correctly_Adds_String_Value()
        {
            var push = ApplePush
                .CreateAlert("testAlert")
                .AddCustomProperty("customPropertyKey", "customPropertyValue");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);

            const string referencePayloadJson = "{\"aps\":{\"alert\":\"testAlert\"},\"customPropertyKey\":\"customPropertyValue\"}";
            Assert.Equal(referencePayloadJson, payloadJson);
        }

        [Fact]
        public void AddCustomProperty_Correctly_Adds_Complex_Value()
        {
            var push = ApplePush
                .CreateAlert("testAlert")
                .AddCustomProperty("customPropertyKey", new { value1 = "123", value2 = 456 });

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);

            const string referencePayloadJson = "{\"aps\":{\"alert\":\"testAlert\"},\"customPropertyKey\":{\"value1\":\"123\",\"value2\":456}}";
            Assert.Equal(referencePayloadJson, payloadJson);
        }

        [Fact]
        public void Setting_Custom_Priority()
        {
            var push = ApplePush.CreateContentAvailable();
            Assert.Equal(5, push.Priority);
            push.SetPriority(10);
            Assert.Equal(10, push.Priority);
        }

        [Fact]
        public void AddContentAvailable()
        {
            var push = new ApplePush(ApplePushType.Background);
            
            push.AddContentAvailable();

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\"}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void AddExpiration()
        {
            var now = DateTimeOffset.UtcNow;
            var push = new ApplePush(ApplePushType.Alert);

            push.AddExpiration(now);

            Assert.Equal(now, push.Expiration);
        }

        [Fact]
        public void AddImmediateExpiration()
        {
            var push = new ApplePush(ApplePushType.Alert);

            push.AddImmediateExpiration();

            Assert.Equal(DateTimeOffset.MinValue, push.Expiration);
        }

        [Fact]
        public void Creating_Push_With_ContentAvailable_MutableContent_Alert()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddContentAvailable()
                .AddMutableContent()
                .AddAlert("title", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\",\"mutable-content\":\"1\",\"alert\":{\"title\":\"title\",\"body\":\"body\"}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Alert_Only_Body()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\"}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Alert_Title_Body()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("title", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":{\"title\":\"title\",\"body\":\"body\"}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Alert_Title_Body_Subtitle()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("title", "subtitle", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":{\"title\":\"title\",\"subtitle\":\"subtitle\",\"body\":\"body\"}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Localized_Alert_Key_And_Args()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddLocalizedAlert("LOCALIZED_KEY", new[] {"LocalizedArg"});

            var payload = push.GeneratePayload();
            var payloadJson = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            const string referenceJson = "{\"aps\":{\"alert\":{\"loc-key\":\"LOCALIZED_KEY\",\"loc-args\":[\"LocalizedArg\"]}}}";
            Assert.Equal(referenceJson, payloadJson);
        }
        
        [Fact]
        public void Creating_Push_With_Localized_Alert_TitleKey_TitleKeyArgs_Key_Args_ActionKey()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddLocalizedAlert("LOCALIZED_TITLE", new[] { "LocalizedTitleArgument", "5"}, "LOCALIZED_KEY", new[] {"LocalizedArg", "6"}, "LOCALIZED_ACTION" );

            var payload = push.GeneratePayload();
            var payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":{\"title-loc-key\":\"LOCALIZED_TITLE\",\"title-loc-args\":[\"LocalizedTitleArgument\",\"5\"],\"loc-key\":\"LOCALIZED_KEY\",\"loc-args\":[\"LocalizedArg\",\"6\"],\"action-loc-key\":\"LOCALIZED_ACTION\"}}}";
            Assert.Equal(referenceJson, payloadJson);
        }
        
        [Fact]
        public void Creating_Push_With_Alert_Only_Body_And_Localized_Alert_Should_Return_Only_Alert()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .AddLocalizedAlert("LOCALIZED_KEY", new[] {"LocalizedArg"});

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\"}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Development_Server_Should_Set_Property()
        {
            var prodPush = new ApplePush(ApplePushType.Alert)
                .AddAlert("body");

            var devPush = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .SendToDevelopmentServer();

            Assert.False(prodPush.IsSendToDevelopmentServer);
            Assert.True(devPush.IsSendToDevelopmentServer);
        }
    }
}
