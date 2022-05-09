using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotAPNS.Tests
{
    [TestClass]
    public class UnixTimestampMillisecondsJsonConverter_Tests
    {
        [TestMethod]
        public void Ensure_Converts_Timestamp_Correctly()
        {
            const string json = "{\"reason\":\"Unregistered\",\"timestamp\":1454948015990}";
            var obj = JsonSerializer.Deserialize<Payload>(json);
            var expected = DateTimeOffset.FromUnixTimeMilliseconds(1454948015990);
            Assert.AreEqual(expected, obj.Timestamp);
        }


        class Payload
        {
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimestampMillisecondsJsonConverter))]
            public DateTimeOffset Timestamp { get; set; }
        }
    }
}