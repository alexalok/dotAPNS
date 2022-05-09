using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace dotAPNS.Tests
{
    [TestClass]
    public class ApplePush_Tests
    {
        [TestMethod]
        public void Adding_ContentAvailable_To_Push_With_Badge_or_Sound_Fails()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();

            Assert.ThrowsException<InvalidOperationException>(() => pushWithContentAvailable.AddBadge(0));
            Assert.ThrowsException<InvalidOperationException>(() => pushWithContentAvailable.AddSound("sound"));
        }

        [TestMethod]
        public void Adding_Token_and_VoipToken_Together_Fails()
        {
            var pushWithToken = ApplePush.CreateContentAvailable().AddToken("token");
            var pushWithVoipToken = ApplePush.CreateContentAvailable(true).AddVoipToken("voip");
            var alertPushWithToken = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.ThrowsException<InvalidOperationException>(() => pushWithToken.AddVoipToken("voip"));
            Assert.ThrowsException<InvalidOperationException>(() => pushWithVoipToken.AddToken("token"));
            Assert.ThrowsException<InvalidOperationException>(() => alertPushWithToken.AddVoipToken("voip"));
        }

        [TestMethod]
        public void Adding_Token_Multiple_Times_Fails_WithoutBatch_Succeeds_With_Batched()
        {
            var push1 = new ApplePush(ApplePushType.Alert);
            var push2 = new ApplePush(ApplePushType.Alert).AsBatched();
            
            push1.AddToken("token1");
            
            Assert.ThrowsException<InvalidOperationException>(() => push1.AddToken("token2"));
            push2.AddToken("token2");
        }




        [TestMethod]
        public void Ensure_Type_Correspond_To_Payload()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();
            var pushWithAlert = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.AreEqual(ApplePushType.Background, pushWithContentAvailable.Type);
            Assert.AreEqual(ApplePushType.Alert, pushWithAlert.Type);
        }

        [TestMethod]
        public void Ensure_Priority_Corresponds_To_Payload()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();
            var pushWithAlert = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.AreEqual(5, pushWithContentAvailable.Priority);
            Assert.AreEqual(10, pushWithAlert.Priority);
        }

        [TestMethod]
        public void Adding_Voip_To_Alert_Push_Throws_InvalidOperationException()
        {
            var alertPush = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.ThrowsException<InvalidOperationException>(() => alertPush.AddVoipToken("voip"));
        }

        [TestMethod]
        public void Adding_Voip_To_NonVoip_Type_Throws_InvalidOperationException()
        {
            var backgroundPush = ApplePush.CreateContentAvailable();
            var alert = ApplePush.CreateContentAvailable();

            Assert.ThrowsException<InvalidOperationException>(() => backgroundPush.AddVoipToken("voip"));
        }

        [TestMethod]
        public void Adding_Token_To_Voip_Type_Throws_InvalidOperationException()
        {
            var backgroundPush = ApplePush.CreateContentAvailable(true);

            Assert.ThrowsException<InvalidOperationException>(() => backgroundPush.AddToken("token"));
        }

        [TestMethod]
        public void CreateContentAvailable_Has_Background_Type()
        {
            var voipPush = ApplePush.CreateContentAvailable();
            Assert.AreEqual(ApplePushType.Background, voipPush.Type);
        }

        [TestMethod]
        public void CreateContentAvailableAsVoip_Has_Voip_Type()
        {
            var voipPush = ApplePush.CreateContentAvailable(true);
            Assert.AreEqual(ApplePushType.Voip, voipPush.Type);
        }

        [TestMethod]
        public void AddCustomProperty_Correctly_Adds_String_Value()
        {
            var push = ApplePush
                .CreateAlert("testAlert")
                .AddCustomProperty("customPropertyKey", "customPropertyValue");

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);

            const string referencePayloadJson = "{\"aps\":{\"alert\":\"testAlert\"},\"customPropertyKey\":\"customPropertyValue\"}";
            Assert.AreEqual(referencePayloadJson, payloadJson);
        }

        [TestMethod]
        public void AddCustomProperty_Correctly_Adds_Complex_Value()
        {
            var push = ApplePush
                .CreateAlert("testAlert")
                .AddCustomProperty("customPropertyKey", new { value1 = "123", value2 = 456 });

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);

            const string referencePayloadJson = "{\"aps\":{\"alert\":\"testAlert\"},\"customPropertyKey\":{\"value1\":\"123\",\"value2\":456}}";
            Assert.AreEqual(referencePayloadJson, payloadJson);
        }

        [TestMethod]
        public void Setting_Custom_Priority()
        {
            var push = ApplePush.CreateContentAvailable();
            Assert.AreEqual(5, push.Priority);
            push.SetPriority(10);
            Assert.AreEqual(10, push.Priority);
        }

        [TestMethod]
        public void AddContentAvailable()
        {
            var push = new ApplePush(ApplePushType.Background);
            
            push.AddContentAvailable();

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\"}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }

        [TestMethod]
        public void AddExpiration()
        {
            var now = DateTimeOffset.UtcNow;
            var push = new ApplePush(ApplePushType.Alert);

            push.AddExpiration(now);

            Assert.AreEqual(now, push.Expiration);
        }

        [TestMethod]
        public void AddImmediateExpiration()
        {
            var push = new ApplePush(ApplePushType.Alert);

            push.AddImmediateExpiration();

            Assert.AreEqual(DateTimeOffset.MinValue, push.Expiration);
        }

        [TestMethod]
        public void Creating_Push_With_ContentAvailable_MutableContent_Alert()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddContentAvailable()
                .AddMutableContent()
                .AddAlert("title", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\",\"mutable-content\":\"1\",\"alert\":{\"title\":\"title\",\"body\":\"body\"}}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }

        [TestMethod]
        public void Creating_Push_With_Alert_Only_Body()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\"}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }

        [TestMethod]
        public void Creating_Push_With_Alert_Title_Body()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("title", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"alert\":{\"title\":\"title\",\"body\":\"body\"}}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }

        [TestMethod]
        public void Creating_Push_With_Alert_Title_Body_Subtitle()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("title", "subtitle", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"alert\":{\"title\":\"title\",\"subtitle\":\"subtitle\",\"body\":\"body\"}}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }

        [TestMethod]
        public void Creating_Push_With_Localized_Alert_Key_And_Args()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddLocalizedAlert("LOCALIZED_KEY", new[] {"LocalizedArg"});

            var payload = push.GeneratePayload();
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull});
            const string referenceJson = "{\"aps\":{\"alert\":{\"loc-key\":\"LOCALIZED_KEY\",\"loc-args\":[\"LocalizedArg\"]}}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }
        
        [TestMethod]
        public void Creating_Push_With_Localized_Alert_TitleKey_TitleKeyArgs_Key_Args_ActionKey()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddLocalizedAlert("LOCALIZED_TITLE", new[] { "LocalizedTitleArgument", "5"}, "LOCALIZED_KEY", new[] {"LocalizedArg", "6"}, "LOCALIZED_ACTION" );

            var payload = push.GeneratePayload();
            var payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"alert\":{\"title-loc-key\":\"LOCALIZED_TITLE\",\"title-loc-args\":[\"LocalizedTitleArgument\",\"5\"],\"loc-key\":\"LOCALIZED_KEY\",\"loc-args\":[\"LocalizedArg\",\"6\"],\"action-loc-key\":\"LOCALIZED_ACTION\"}}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }
        
        [TestMethod]
        public void Creating_Push_With_Alert_Only_Body_And_Localized_Alert_Should_Return_Only_Alert()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .AddLocalizedAlert("LOCALIZED_KEY", new[] {"LocalizedArg"});

            var payload = push.GeneratePayload();
            string payloadJson = JsonSerializer.Serialize(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\"}}";
            Assert.AreEqual(referenceJson, payloadJson);
        }

        [TestMethod]
        public void Creating_Push_With_Development_Server_Should_Set_Property()
        {
            var prodPush = new ApplePush(ApplePushType.Alert)
                .AddAlert("body");

            var devPush = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .SendToDevelopmentServer();

            Assert.IsFalse(prodPush.IsSendToDevelopmentServer);
            Assert.IsTrue(devPush.IsSendToDevelopmentServer);
        }
        [TestMethod]
        public void AddTokens_Adds_Tokens()
        {
            var push = new ApplePush(ApplePushType.Alert).AsBatched();
            var tokens = new string[]
            {
                "token1",
                "token2"
            };
            push.AddTokens(tokens);
            Assert.AreEqual(push.Tokens.Count, 2);
        }
    }
}
