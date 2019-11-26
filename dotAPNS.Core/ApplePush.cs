using System;
using JetBrains.Annotations;

namespace dotAPNS.Core
{
    public class ApplePush
    {
        public string Token { get; private set; }
        public string VoipToken { get; private set; }
        public int Priority => Type == ApplePushType.Background ? 5 : 10; // 5 for background, 10 for everything else
        public ApplePushType Type
        {
            get
            {
                if (_sendAsVoipType)
                    return ApplePushType.Voip;
                if (IsContentAvailable)
                    return ApplePushType.Background;
                if (Alert != null)
                    return ApplePushType.Alert;
                throw new InvalidOperationException("Cannot determine type for push.");
            }
        }

        [CanBeNull]
        public ApplePushAlert Alert { get; private set; }

        public int? Badge { get; private set; }

        [CanBeNull]
        public string Sound { get; private set; }

        [CanBeNull]
        public string Location { get; private set; } // undocumented, but probably works

        public bool IsContentAvailable { get; private set; }

        /// <summary>
        /// Indicates whether push must be sent with 'voip' type. If false, push is sent with its default type.
        /// </summary>
        bool _sendAsVoipType;

        ApplePush()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'background'.</param>
        /// <returns></returns>
        public static ApplePush CreateContentAvailable(bool sendAsVoipType = false) => new ApplePush() { IsContentAvailable = true, _sendAsVoipType = sendAsVoipType };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alert"></param>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'alert'.</param>
        /// <returns></returns>
        public static ApplePush CreateAlert(ApplePushAlert alert, bool sendAsVoipType = false) => new ApplePush() { Alert = alert, _sendAsVoipType = sendAsVoipType };

        public ApplePush AddBadge(int badge)
        {
            IsContentAvailableGuard();
            if (Badge != null)
                throw new InvalidOperationException("Badge already exists");
            Badge = badge;
            return this;
        }

        public ApplePush AddSound([NotNull] string sound)
        {
            if (string.IsNullOrWhiteSpace(sound))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sound));
            IsContentAvailableGuard();
            if (Sound != null)
                throw new InvalidOperationException("Sound already exists");
            Sound = sound;
            return this;
        }

        [Obsolete("'Location' property is not offifically documented. It is not guaranteed to work.")]
        public ApplePush AddLocation([NotNull] string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(location));
            IsContentAvailableGuard();
            if (Location != null)
                throw new InvalidOperationException("Sound already exists");
            Location = location;
            return this;
        }

        public ApplePush AddToken([NotNull] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Value cannot be null or empty.", nameof(token));
            EnsureTokensNotExistGuard();
            if (Type == ApplePushType.Voip)
                throw new InvalidOperationException($"Cannot add push token to push that is being sent with 'voip' type.");
            Token = token;
            return this;
        }

        public ApplePush AddVoipToken([NotNull] string voipToken)
        {
            if (string.IsNullOrWhiteSpace(voipToken))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(voipToken));
            EnsureTokensNotExistGuard();
            if (Alert != null && !_sendAsVoipType)
                throw new InvalidOperationException("Cannot add voip token to alert push.");
            if (IsContentAvailable && !_sendAsVoipType)
                throw new InvalidOperationException($"Cannot add voip token to 'content-available' push that is being sent with '{Type.ToString().ToLowerInvariant()}' type.");
            VoipToken = voipToken;
            return this;
        }

        void EnsureTokensNotExistGuard()
        {
            if (!(string.IsNullOrEmpty(Token) && string.IsNullOrEmpty(VoipToken)))
                throw new InvalidOperationException("Notification already has token");
        }

        void IsContentAvailableGuard()
        {
            if (IsContentAvailable)
                throw new InvalidOperationException("Cannot add fields to a push with content-available");
        }
    }

    public class ApplePushAlert
    {
        public string Title { get; }

        public string Body { get; }

        public ApplePushAlert([CanBeNull] string title, [NotNull] string body)
        {
            Title = title;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }
    }

}
