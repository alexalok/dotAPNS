using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace dotAPNS
{
    public class ApplePush
    {
        public string Token { get; private set; }
        public string VoipToken { get; private set; }

        public int Priority =>
            CustomPriority ?? (Type == ApplePushType.Background ? 5 : 10); // 5 for background, 10 for everything else

        public ApplePushType Type { get; }

        /// <summary>
        /// If specified, this value will be used as a `apns-
        /// </summary>
        public int? CustomPriority { get; private set; }

        [CanBeNull]
        public ApplePushAlert Alert { get; private set; }

        [CanBeNull]
        public ApplePushLocalizedAlert LocalizedAlert { get; private set; }

        public int? Badge { get; private set; }

        [CanBeNull]
        public string Sound { get; private set; }

        /// <summary>
        /// See <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcontent/1649866-categoryidentifier">official documentation</a> for reference.
        /// </summary>
        [CanBeNull]
        public string Category { get; private set; }

        public bool IsContentAvailable { get; private set; }

        public bool IsMutableContent { get; private set; }

        /// <summary>
        /// The date at which the notification is no longer valid.
        /// If set to <i>null</i> (default) then <i>apns-expiration</i> header is not sent and expiration time is undefined (<seealso href="https://stackoverflow.com/questions/44630196/what-is-the-default-value-of-the-apns-expiration-field">but is probably large</seealso>).
        /// </summary>
        public DateTimeOffset? Expiration { get; private set; }

        /// <summary>
        /// An identifier you use to coalesce multiple notifications into a single notification for the user. 
        /// Typically, each notification request causes a new notification to be displayed on the user’s device. 
        /// When sending the same notification more than once, use the same value in this header to coalesce the requests.
        /// <b>The value of this key must not exceed 64 bytes.</b>
        /// </summary>
        [CanBeNull]
        public string CollapseId { get; private set; }

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

        internal bool IsSendToDevelopmentServer {get; private set; }

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
            new ApplePush(sendAsVoipType ? ApplePushType.Voip : ApplePushType.Background) {IsContentAvailable = true};

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alert"></param>
        /// <param name="sendAsVoipType">True if push must be sent with 'voip' type rather than 'alert'.</param>
        /// <returns></returns>
        [Obsolete("Please use " + nameof(AddAlert) + " instead.")]
        public static ApplePush CreateAlert(ApplePushAlert alert, bool sendAsVoipType = false) =>
            new ApplePush(sendAsVoipType ? ApplePushType.Voip : ApplePushType.Alert) {Alert = alert};

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
        /// <param name="subtitle">Alert subtitle. Can be null.</param>
        /// <param name="body">Alert body. <b>Cannot be null.</b></param>
        /// <returns></returns>
        public ApplePush AddAlert([CanBeNull] string title, [CanBeNull] string subtitle, [NotNull] string body)
        {
            Alert = new ApplePushAlert(title, subtitle, body);
            if (title == null)
                _sendAlertAsText = true;
            return this;
        }

        /// <summary>
        /// Add alert to the payload.
        /// </summary>
        /// <param name="title">Alert title. Can be null.</param>
        /// <param name="body">Alert body. <b>Cannot be null.</b></param>
        /// <returns></returns>
        public ApplePush AddAlert([CanBeNull] string title, [NotNull] string body)
        {
            Alert = new ApplePushAlert(title, body);
            if (title == null)
                _sendAlertAsText = true;
            return this;
        }

        /// <summary>
        /// Add alert to the payload.
        /// </summary>
        /// <param name="body">Alert body. <b>Cannot be null.</b></param>
        /// <returns></returns>
        public ApplePush AddAlert([NotNull] string body)
        {
            return AddAlert(null, body);
        }

        /// <summary>
        /// Add localized alert to the payload. When alert is already present, localized alert will be omitted when generating payload.
        /// </summary>
        /// <param name="locKey">Key to an alert-message string in a Localizable.strings file for the current localization. <b>Cannot be null.</b></param>
        /// <param name="locArgs">Variable string values to appear in place of the format specifiers in loc-key. <b>Cannot be null.</b></param>
        /// <param name="titleLocKey">The key to a title string in the Localizable.strings file for the current localization. Can be null.</param>
        /// <param name="tittleLocArgs">Variable string values to appear in place of the format specifiers in title-loc-key. Can be null.</param>
        /// <param name="actionLocKey">The string is used as a key to get a localized string in the current localization to use for the right button’s title instead of “View". Can be null.</param>
        /// <returns></returns>
        public ApplePush AddLocalizedAlert([CanBeNull] string titleLocKey, [CanBeNull] string[] tittleLocArgs,
            [NotNull] string locKey, [NotNull] string[] locArgs, [CanBeNull] string actionLocKey)
        {
            LocalizedAlert = new ApplePushLocalizedAlert(titleLocKey, tittleLocArgs, locKey, locArgs, actionLocKey);
            return this;
        }

        /// <summary>
        /// Add localized alert to the payload. When alert is already present, localized alert will be omitted when generating payload.
        /// </summary>
        /// <param name="locKey">Key to an alert-message string in a Localizable.strings file for the current localization. <b>Cannot be null.</b></param>
        /// <param name="locArgs">Variable string values to appear in place of the format specifiers in loc-key. <b>Cannot be null.</b></param>
        /// <returns></returns>
        public ApplePush AddLocalizedAlert([NotNull] string locKey, [NotNull] string[] locArgs)
        {
            return AddLocalizedAlert(null, null, locKey, locArgs, null);
        }

        public ApplePush SetPriority(int priority)
        {
            if (priority < 0 || priority > 10)
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

        public ApplePush AddCategory([NotNull] string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(category));
            if (Category != null)
                throw new InvalidOperationException($"{nameof(Category)} already exists.");
            Category = category;
            return this;
        }

        /// <summary>
        ///  APNs stores the notification and tries to deliver it at least once, repeating the attempt as needed until the specified date.
        /// </summary>
        /// <param name="expirationDate">The date at which the notification is no longer valid.</param>
        /// 
        public ApplePush AddExpiration(DateTimeOffset expirationDate)
        {
            Expiration = expirationDate;
            return this;
        }

        /// <summary>
        /// APNs attempts to deliver the notification only once and doesn’t store it.
        /// </summary>
        /// <seealso cref="AddExpiration"/>
        public ApplePush AddImmediateExpiration()
        {
            Expiration = DateTimeOffset.MinValue;
            return this;
        }

        public ApplePush AddToken([NotNull] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(token));
            EnsureTokensNotExistGuard();
            if (Type == ApplePushType.Voip)
                throw new InvalidOperationException(
                    $"Please use AddVoipToken() when sending {nameof(ApplePushType.Voip)} pushes.");
            Token = token;
            return this;
        }

        public ApplePush AddVoipToken([NotNull] string voipToken)
        {
            if (string.IsNullOrWhiteSpace(voipToken))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(voipToken));
            EnsureTokensNotExistGuard();
            if (Type != ApplePushType.Voip)
                throw new InvalidOperationException(
                    $"VoIP token may only be used with {nameof(ApplePushType.Voip)} pushes.");
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

        public ApplePush AddCollapseId([NotNull] string collapseId)
        {
            if (string.IsNullOrEmpty(collapseId))
                throw new ArgumentException($"'{nameof(collapseId)}' cannot be null or empty", nameof(collapseId));
            if (!string.IsNullOrEmpty(CollapseId))
                throw new InvalidOperationException($"{nameof(CollapseId)} is already added.");
            CollapseId = collapseId;
            return this;
        }

        /// <summary>
        /// Indicates that the push must be sent to a development server instead of production one.
        /// See <a href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/sending_notification_requests_to_apns">Establish a Connection to APNs</a> for more info.
        /// </summary>
        /// <returns></returns>
        public ApplePush SendToDevelopmentServer()
        {
            IsSendToDevelopmentServer = true;
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
            if (IsMutableContent)
                apsAsDict["mutable-content"] = "1";

            if (Alert != null)
            {
                object alert;
                if (_sendAlertAsText)
                    alert = Alert.Body;
                else if (Alert.Subtitle == null)
                    alert = new {title = Alert.Title, body = Alert.Body};
                else
                    alert = new {title = Alert.Title, subtitle = Alert.Subtitle, body = Alert.Body};
                payload.aps.alert = alert;
            }
            else if (LocalizedAlert != null)
            {
                object localizedAlert = LocalizedAlert;
                payload.aps.alert = localizedAlert;
            }

            if (Badge != null)
                payload.aps.badge = Badge.Value;

            if (Sound != null)
                payload.aps.sound = Sound;

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
            Dictionary<string, object> appList = payload.aps;
            if (appList.Count == 0)
            {
                var dict = (IDictionary<string, object>)payload;
                dict.Remove("aps");
                return dict;
            } else
            {
                return payload;
            }
        }
    }

    public class ApplePushAlert
    {
        public string Title { get; }

        public string Subtitle { get; }

        public string Body { get; }

        public ApplePushAlert([CanBeNull] string title, [NotNull] string body)
        {
            Title = title;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public ApplePushAlert([CanBeNull] string title, [CanBeNull] string subtitle, [NotNull] string body)
        {
            Title = title;
            Subtitle = subtitle;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ApplePushLocalizedAlert
    {
        [JsonProperty("title-loc-key")]
        public string TitleLocKey { get; }

        [JsonProperty("title-loc-args")]
        public string[] TitleLocArgs { get; }

        [JsonProperty("loc-key")]
        public string LocKey { get; }

        [JsonProperty("loc-args")]
        public string[] LocArgs { get; }

        [JsonProperty("action-loc-key")]
        public string ActionLocKey { get; }

        public ApplePushLocalizedAlert([NotNull] string locKey, [NotNull] string[] locArgs)
        {
            LocKey = locKey ?? throw new ArgumentNullException(nameof(locKey));
            LocArgs = locArgs ?? throw new ArgumentNullException(nameof(locArgs));
        }

        public ApplePushLocalizedAlert(string titleLocKey, string[] titleLocArgs, string locKey, string[] locArgs,
            string actionLocKey)
        {
            TitleLocKey = titleLocKey;
            TitleLocArgs = titleLocArgs;
            LocKey = locKey ?? throw new ArgumentNullException(nameof(locKey));
            ;
            LocArgs = locArgs ?? throw new ArgumentNullException(nameof(locArgs));
            ActionLocKey = actionLocKey;
        }
    }
}