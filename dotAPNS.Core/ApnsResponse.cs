namespace dotAPNS.Core
{
    public class ApnsResponse
    {
        public ApnsResponseReason Reason { get; }
        public string ReasonString { get; }
        public bool IsSuccessful { get; }

        public ApnsResponse(ApnsResponseReason reason, string reasonString) : this(false)
        {
            Reason = reason;
            ReasonString = reasonString;
        }

        ApnsResponse(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }

        public static ApnsResponse Successful() => new ApnsResponse(true);

        public static ApnsResponse Error(ApnsResponseReason reason, string reasonString) => new ApnsResponse(reason, reasonString);
    }
}