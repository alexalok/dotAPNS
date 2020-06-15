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
        public ApplePushType Type { get; }

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

        /// <summary>
        /// See <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcontent/1649866-categoryidentifier">official documentation</a> for reference.
        /// </summary>
        [CanBeNull]
        public string Category { get; private set; }

        public bool IsContentAvailable { get; private set; }
        
        public bool IsMutableContent { get; private set; }

        /// <summary>
        /// User-defined properties that will be attached to the root payload dictionary.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; }

        /// <summary>
        /// User-defined properties that will be attached to the <i>aps</i> payload dictionary.
        /// </summary>
        public IDictionary<string, object> CustomApsProperties { get; set; }

        /// <summary>
        /// Indicates whether alert must be sent as a string. 
        /// </summary>
        bool _sendAlertAsText;

        public ApplePush(ApplePushType pushType)
        {
            Type = pushType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'background'.</param>
        /// <returns></returns>
        [Obsolete("Please use " + nameof(AddContentAvailable) + " instead.")]
        public static ApplePush CreateContentAvailable(bool sendAsVoipType = false) =>
            new ApplePush(sendAsVoipType ? ApplePushType.Voip : ApplePushType.Background) { IsContentAvailable = true };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alert"></param>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'alert'.</param>
        /// <returns></returns>
        [Obsolete("Please use " + nameof(AddAlert) + " instead.")]
        public static ApplePush CreateAlert(ApplePushAlert alert, bool sendAsVoipType = false) =>
            new ApplePush(sendAsVoipType ? ApplePushType.Voip : ApplePushType.Alert) { Alert = alert };

        /// <summary>
        /// Send alert push with alert as string.
        /// </summary>
        /// <param name="alert"></param>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'alert'.</param>
        /// <returns></returns>
        [Obsolete("Please use " + nameof(AddAlert) + " instead.")]
        public static ApplePush CreateAlert(string alert, bool sendAsVoipType = false)
        {
            var push = CreateAlert(new ApplePushAlert(null, alert), sendAsVoipType);
            push._sendAlertAsText = true;
            return push;
        }

        /// <summary>
        /// Add `content-available: 1` to the payload.
        /// </summary>
        public ApplePush AddContentAvailable()
        {
            IsContentAvailable = true;
            return this;
        }

        /// <summary>
        /// Add `mutable-content: 1` to the payload.
        /// </summary>
        /// <returns></returns>
        public ApplePush AddMutableContent()
        {
            IsMutableContent = true;
            return this;
        }

        /// <summary>
        /// Add alert to the payload.
        /// </summary>
        /// <param name="title">Alert title. Can be null.</param>
        /// <param name="body">Alert body. <b>Cannot be null.</b></param>
        /// <returns></returns>
        public ApplePush AddAlert(string title = null, string body = null)
        {
            Alert = new ApplePushAlert(title, body);
            if (title == null)
                _sendAlertAsText = true;
            return this;
        }

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

        public ApplePush AddCategory([NotNull] string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(category));
            if (Category != null)
                throw new InvalidOperationException($"{nameof(Category)} already exists.");
            Category = category;
            return this;
        }

        public ApplePush AddToken([NotNull] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Value cannot be null or empty.", nameof(token));
            EnsureTokensNotExistGuard();
            if (Type == ApplePushType.Voip)
                throw new InvalidOperationException($"Please use AddVoipToken() when sending {nameof(ApplePushType.Voip)} pushes.");
            Token = token;
            return this;
        }

        public ApplePush AddVoipToken([NotNull] string voipToken)
        {
            if (string.IsNullOrWhiteSpace(voipToken))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(voipToken));
            EnsureTokensNotExistGuard();
            if(Type != ApplePushType.Voip)
                throw new InvalidOperationException($"VoIP token may only be used with {nameof(ApplePushType.Voip)} pushes.");
            VoipToken = voipToken;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addToApsDict">If <b>true</b>, property will be added to the <i>aps</i> dictionary, otherwise to the root dictionary. Default: <b>false</b>.</param>
        /// <returns></returns>
        public ApplePush AddCustomProperty(string key, object value, bool addToApsDict = false)
        {
            if (addToApsDict)
            {
                CustomApsProperties ??= new Dictionary<string, object>();
                CustomApsProperties.Add(key, value);
            }
            else
        {
                CustomProperties ??= new Dictionary<string, object>();
            CustomProperties.Add(key, value);
            }
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
            IDictionary<string, object> apsAsDict = payload.aps;
            if (IsContentAvailable)
                apsAsDict["content-available"] = "1";
            if(IsMutableContent)
                apsAsDict["mutable-content"] = "1";

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

            if (Category != null)
                payload.aps.category = Category;

            if (CustomProperties != null)
            {
                IDictionary<string, object> payloadAsDict = payload;
                foreach (var customProperty in CustomProperties) 
                    payloadAsDict[customProperty.Key] = customProperty.Value;
            }

            if (CustomApsProperties != null)
            {
                foreach (var customApsProperty in CustomApsProperties)
                    apsAsDict[customApsProperty.Key] = customApsProperty.Value;
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
