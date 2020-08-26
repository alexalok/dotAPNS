using System;
using Newtonsoft.Json;
using Xunit;

namespace dotAPNS.Tests
{
    public class UnixTimestampMillisecondsJsonConverter_Tests
    {
        [Fact]
        public void Ensure_Converts_Timestamp_Correctly()
        {
            const string json = "{\"reason\":\"Unregistered\",\"timestamp\":1454948015990}";
            var obj = JsonConvert.DeserializeObject<Payload>(json);
            var expected = DateTimeOffset.FromUnixTimeMilliseconds(1454948015990);
            Assert.Equal(expected, obj.Timestamp);
        }


        class Payload
        {
            [JsonConverter(typeof(UnixTimestampMillisecondsJsonConverter))]
            public DateTimeOffset Timestamp { get; set; }
        }
    }
}