using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotAPNS
{
    public class UnixTimestampMillisecondsJsonConverter : JsonConverter<DateTimeOffset>
    {
#if NET46
        public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long) reader.Value);
        }

        public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override bool CanWrite { get; } = false;
#else


#endif


        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64()),
            JsonTokenType.String => DateTimeOffset.FromUnixTimeMilliseconds(long.TryParse(reader.GetString(), out var value) ? value : throw new JsonException("Cannot parse the string to a long")),
            _ => throw new JsonException("Can Only Convert a number or a string (must be a long sent as a string) to a DateTimeOffset.")
        };
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}