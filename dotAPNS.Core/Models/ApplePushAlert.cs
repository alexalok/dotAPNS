using dotAPNS.Core.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace dotAPNS
{
    public class ApplePushAlert
    {
        [JsonPropertyName("title")]
        public string? Title { get; }
        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; }
        [JsonPropertyName("body")]
        public string Body { get; }

        public ApplePushAlert(string? title, string body)
        {
            Title = title;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public ApplePushAlert(string? title, string? subtitle, string body)
        {
            Title = title;
            Subtitle = subtitle;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }
    }

    
    public class ApplePushLocalizedAlert
    {
        [JsonPropertyName("title-loc-key")]
        public string? TitleLocKey { get; }

        [JsonPropertyName("title-loc-args")]
        public string[]? TitleLocArgs { get; }

        [JsonPropertyName("loc-key")]
        public string? LocKey { get; }

        [JsonPropertyName("loc-args")]
        public string[]? LocArgs { get; }

        [JsonPropertyName("action-loc-key")]
        public string? ActionLocKey { get; }

        public ApplePushLocalizedAlert(string locKey, string[] locArgs)
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
            LocArgs = locArgs ?? throw new ArgumentNullException(nameof(locArgs));
            ActionLocKey = actionLocKey;
        }
    }
}