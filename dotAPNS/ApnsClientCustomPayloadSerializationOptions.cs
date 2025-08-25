using Newtonsoft.Json;

namespace dotAPNS;

public static class ApnsClientCustomPayloadSerializationOptions
{
    public static JsonSerializerSettings Value { get; set; } = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };
}