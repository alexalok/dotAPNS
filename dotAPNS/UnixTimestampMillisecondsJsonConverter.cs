using System;
using Newtonsoft.Json;

namespace dotAPNS
{
    public class UnixTimestampMillisecondsJsonConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long) reader.Value);
        }

        public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite { get; } = false;
    }
}