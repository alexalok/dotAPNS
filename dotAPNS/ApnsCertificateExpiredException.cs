using System;

namespace dotAPNS
{
    public class ApnsCertificateExpiredException : Exception
    {
        const string ConstMessage = "Your APNs certificate has expired. Please renew it at. More info: https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_certificate-based_connection_to_apns";

        public ApnsCertificateExpiredException(string message = ConstMessage, Exception? innerException = null) : base(ConstMessage, innerException)
        {
        }
    }
}
