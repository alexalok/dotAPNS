using JetBrains.Annotations;
using Newtonsoft.Json;

namespace dotAPNS
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ApplePushCriticalSound
    {
        [JsonProperty("critical")]
        public int Critical => 1;

        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("volume", DefaultValueHandling = DefaultValueHandling.Include)]
        public double Volume { get; }

        internal ApplePushCriticalSound([NotNull] string name, double volume)
        {
            Name = name;
            Volume = volume;
        }
    }
}
