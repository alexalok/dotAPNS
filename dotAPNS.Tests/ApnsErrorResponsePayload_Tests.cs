using System;
using System.Collections.Generic;
using ExpectedObjects;
using Newtonsoft.Json;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApnsErrorResponsePayload_Tests
    {
        [Theory]
        [MemberData(nameof(Payloads))]
        public void Ensure_Parses_Correctly(string json, ApnsErrorResponsePayload expectedPayload)
        {
            var parsedPayload = JsonConvert.DeserializeObject<ApnsErrorResponsePayload>(json);
            expectedPayload.ToExpectedObject().ShouldEqual(parsedPayload);
        }

        /*
         * {"reason":"DeviceTokenNotForTopic"}
            // {"reason":"Unregistered","timestamp":1454948015990}
         */

        public static IEnumerable<object[]> Payloads => new[]
        {
            new object[]
            {
                "{\"reason\":\"DeviceTokenNotForTopic\"}",
                new ApnsErrorResponsePayload { ReasonRaw = "DeviceTokenNotForTopic" },
            },

            new object[]
            {
                "{\"reason\":\"Unregistered\",\"timestamp\":1454948015990}",
                new ApnsErrorResponsePayload { ReasonRaw = "Unregistered", Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1454948015990) }
            }
        };
    }
}