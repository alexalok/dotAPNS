namespace dotAPNS
{
    public class ApnsResponse
    {
        public ApnsResponseReason Reason { get; }
        public string? ReasonString { get; }
        public bool IsSuccessful { get; }
        public string Token { get; }

        ApnsResponse(ApnsResponseReason reason, string? reasonString, bool isSuccessful, string token)
        {
            Reason = reason;
            ReasonString = reasonString;
            IsSuccessful = isSuccessful;
            Token = token;
        }

        public static ApnsResponse Successful(string token) => new ApnsResponse(ApnsResponseReason.Success, null, true, token);

        public static ApnsResponse Error(ApnsResponseReason reason, string reasonString, string token) => new ApnsResponse(reason, reasonString, false, token);
    }
}