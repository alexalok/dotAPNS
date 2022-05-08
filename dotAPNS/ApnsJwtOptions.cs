using System;

namespace dotAPNS
{
    public class ApnsJwtOptions
    {
        /// <summary>
        /// Path to a .p8 certificate containing a key to be used to encrypt JWT. If specified, <see cref="CertContent"/> must be null.
        /// </summary>
        public string? CertFilePath
        {
            get => _certFilePath;
            set
            {
                if (value != null && CertContent != null)
                    throw new InvalidOperationException("Either path to the certificate or certificate's contents must be provided, not both.");
                _certFilePath = value;
            }
        }

        string? _certFilePath;

        /// <summary>
        /// Contents of a .p8 certificate containing a key to be used to encrypt JWT. Can include BEGIN/END headers, line breaks, etc. If specified, <see cref="CertContent"/> must be null.
        /// </summary>
        public string? CertContent
        {
            get => _certContent;
            set
            {
                if (value != null && CertFilePath != null)
                    throw new InvalidOperationException("Either path to the certificate or certificate's contents must be provided, not both.");
                _certContent = value;
            }
        }

        string? _certContent;

        /// <summary>
        /// The 10-character Key ID you obtained from your developer account. See <a href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_token-based_connection_to_apns#2943371">Reference</a>.
        /// </summary>
        public string KeyId
        {
            get => _keyId;
            set => _keyId = value ?? throw new ArgumentNullException(nameof(KeyId));
        }

        string? _keyId;

        /// <summary>
        /// 10-character Team ID you use for developing your company's apps.
        /// </summary>
        
        public string TeamId
        {
            get => _teamId;
            set => _teamId = value ?? throw new ArgumentNullException(nameof(TeamId));
        }

        string _teamId;

        /// <summary>
        /// Your app's bundle ID.
        /// </summary>
        public string BundleId
        {
            get => _bundleId;
            set => _bundleId = value ?? throw new ArgumentNullException(nameof(BundleId));
        }

        string _bundleId;
    }
}