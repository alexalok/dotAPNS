using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dotAPNS.Tests
{
    [TestClass]
    public class ApnsErrorResponsePayload_Tests
    {
        [DataTestMethod]
        [DynamicData(nameof(Payloads), DynamicDataSourceType.Property)]
        public void Ensure_Parses_Correctly(string json, ApnsErrorResponsePayload expectedPayload)
        {
            var parsedPayload = JsonSerializer.Deserialize<ApnsErrorResponsePayload>(json);
            expectedPayload.Should().BeEquivalentTo(parsedPayload);
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