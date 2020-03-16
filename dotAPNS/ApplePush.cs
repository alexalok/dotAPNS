using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using JetBrains.Annotations;

namespace dotAPNS
{
    public class ApplePush
    {
        public string Token { get; private set; }
        public string VoipToken { get; private set; }
        public int Priority => CustomPriority ?? (Type == ApplePushType.Background ? 5 : 10); // 5 for background, 10 for everything else
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

        /// <summary>
        /// If specified, this value will be used as a `apns-
        /// </summary>
        public int? CustomPriority { get; private set; }

        [CanBeNull]
        public ApplePushAlert Alert { get; private set; }

        public int? Badge { get; private set; }

        [CanBeNull]
        public string Sound { get; private set; }

        [CanBeNull]
        public string Location { get; private set; } // undocumented, but probably works

        public bool IsContentAvailable { get; private set; }

        /// <summary>
        /// User-defined properties that are sent in the payload.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; }

        /// <summary>
        /// Indicates whether push must be sent with 'voip' type. If false, push is sent with its default type.
        /// </summary>
        bool _sendAsVoipType;

        /// <summary>
        /// Indicates whether alert must be sent as a string. 
        /// </summary>
        bool _sendAlertAsText;

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
        public static ApplePush CreateAlert(ApplePushAlert alert, bool sendAsVoipType = false) => 
            new ApplePush() { Alert = alert, _sendAsVoipType = sendAsVoipType };

        /// <summary>
        /// Send alert push with alert as string.
        /// </summary>
        /// <param name="alert"></param>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'alert'.</param>
        /// <returns></returns>
        public static ApplePush CreateAlert(string alert, bool sendAsVoipType = false) => 
            new ApplePush() { Alert = new ApplePushAlert(null, alert), _sendAsVoipType = sendAsVoipType, _sendAlertAsText = true};

        public ApplePush SetPriority(int priority)
        {
            if(priority < 0 || priority > 10)
                throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be between 0 and 10.");
            CustomPriority = priority;
            return this;
        }

        public ApplePush AddBadge(int badge)
        {
            IsContentAvailableGuard();
            if (Badge != null)
                throw new InvalidOperationException("Badge already exists");
            Badge = badge;
            return this;
        }

        public ApplePush AddSound([NotNull] string sound = "default")
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

        public ApplePush AddCustomProperty(string key, object value)
        {
            if(CustomProperties == null)
                CustomProperties = new Dictionary<string, object>();
            CustomProperties.Add(key, value);
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

        public object GeneratePayload()
        {
            dynamic payload = new ExpandoObject();
            payload.aps = new ExpandoObject();
            if (IsContentAvailable)
            {
                IDictionary<string, object> apsAsDict = payload.aps;
                apsAsDict["content-available"] = "1";
                return payload;
            }

            if (Alert != null)
            {
                object alert;
                if (_sendAlertAsText)
                    alert = Alert.Body;
                else
                    alert = new { title = Alert.Title, body = Alert.Body };
                payload.aps.alert = alert;
            }

            if (Badge != null)
                payload.aps.badge = Badge.Value;

            if (Sound != null)
                payload.aps.sound = Sound;

            if (Location != null)
                payload.aps.Location = Location;

            if (CustomProperties != null)
            {
                IDictionary<string, object> payloadAsDict = payload;
                foreach (var customProperty in CustomProperties) 
                    payloadAsDict[customProperty.Key] = customProperty.Value;
            }

            return payload;
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
