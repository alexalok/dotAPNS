using System;
using Newtonsoft.Json;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApplePushCriticalSound_Tests
    {
        [Fact]
        public void Creating_Push_With_Default_Critical_Sound()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .AddCriticalSound();

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\",\"sound\":{\"critical\":1,\"name\":\"default\",\"volume\":1.0}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Custom_Critical_Sound_And_Volume()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .AddCriticalSound("alarm.aiff", 0.5);

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\",\"sound\":{\"critical\":1,\"name\":\"alarm.aiff\",\"volume\":0.5}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_Zero_Critical_Sound_Volume_Includes_Volume_When_Default_Values_Are_Ignored()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .AddCriticalSound(volume: 0.0);

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            const string referenceJson = "{\"aps\":{\"alert\":\"body\",\"sound\":{\"critical\":1,\"name\":\"default\",\"volume\":0.0}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Existing_Regular_Sound_Payload_Remains_Unchanged()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert("body")
                .AddSound("alarm.aiff");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"alert\":\"body\",\"sound\":\"alarm.aiff\"}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Adding_Critical_Sound_With_Invalid_Name_Fails()
        {
            Assert.Throws<ArgumentException>(() => new ApplePush(ApplePushType.Alert).AddCriticalSound(null));
            Assert.Throws<ArgumentException>(() => new ApplePush(ApplePushType.Alert).AddCriticalSound(" "));
        }

        [Fact]
        public void Adding_Critical_Sound_With_Invalid_Volume_Fails()
        {
            var invalidVolumes = new[]
            {
                -0.1,
                1.1,
                double.NaN,
                double.NegativeInfinity,
                double.PositiveInfinity
            };

            foreach (double invalidVolume in invalidVolumes)
            {
                var push = new ApplePush(ApplePushType.Alert);
                Assert.Throws<ArgumentOutOfRangeException>(() => push.AddCriticalSound(volume: invalidVolume));
            }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void Adding_Critical_Sound_With_Boundary_Volume_Succeeds(double volume)
        {
            var push = new ApplePush(ApplePushType.Alert).AddCriticalSound(volume: volume);

            Assert.Equal(volume, push.CriticalSound.Volume);
        }

        [Theory]
        [InlineData(ApplePushType.Unknown)]
        [InlineData(ApplePushType.Background)]
        [InlineData(ApplePushType.Voip)]
        [InlineData(ApplePushType.Location)]
        [InlineData(ApplePushType.LiveActivity)]
        [InlineData(ApplePushType.Mdm)]
        public void Adding_Critical_Sound_To_NonAlert_Push_Fails(ApplePushType pushType)
        {
            var push = new ApplePush(pushType);

            Assert.Throws<InvalidOperationException>(() => push.AddCriticalSound());
        }

        [Fact]
        public void Creating_Alert_Push_With_ContentAvailable_And_Critical_Sound()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddContentAvailable()
                .AddAlert("body")
                .AddCriticalSound();

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\",\"alert\":\"body\",\"sound\":{\"critical\":1,\"name\":\"default\",\"volume\":1.0}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Adding_More_Than_One_Sound_Fails()
        {
            var pushWithSound = new ApplePush(ApplePushType.Alert).AddSound();
            var pushWithCriticalSound = new ApplePush(ApplePushType.Alert).AddCriticalSound();

            Assert.Throws<InvalidOperationException>(() => pushWithSound.AddCriticalSound());
            Assert.Throws<InvalidOperationException>(() => pushWithCriticalSound.AddSound());
            Assert.Throws<InvalidOperationException>(() => pushWithCriticalSound.AddCriticalSound());
        }
    }
}
