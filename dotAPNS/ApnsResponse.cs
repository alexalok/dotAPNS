using System;
using Newtonsoft.Json;

namespace dotAPNS
{
    public class ApnsResponse
    {
        public ApnsResponseReason Reason { get; }
        public string ReasonString { get; }
        public bool IsSuccessful { get; }
        public DateTimeOffset? Timestamp {get; }

        [JsonConstructor]
        ApnsResponse(ApnsResponseReason reason, string reasonString, bool isSuccessful, DateTimeOffset? timestamp)
        {
            Reason = reason;
            ReasonString = reasonString;
            IsSuccessful = isSuccessful;
            Timestamp = timestamp;
        }

        public static ApnsResponse Successful() => new ApnsResponse(ApnsResponseReason.Success, null, true, null);

        public static ApnsResponse Error(ApnsResponseReason reason, string reasonString, DateTimeOffset? timestamp=null) => new ApnsResponse(reason, reasonString, false, timestamp);
    }
}