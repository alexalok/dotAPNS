using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotAPNS
{
    public class UnixTimestampMillisecondsJsonConverter : JsonConverter<DateTimeOffset>
    {
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