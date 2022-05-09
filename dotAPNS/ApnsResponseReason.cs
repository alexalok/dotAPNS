namespace dotAPNS
{
    public enum ApnsResponseReason
    {
        Unknown = - 1,

        /// <summary>
        /// 200
        /// </summary>
        Success,

        /// <summary>
        /// 400
        /// </summary>
        BadCollapseId,
        BadDeviceToken,
        BadExpirationDate,
        BadMessageId,
        BadPriority,
        BadTopic,
        DeviceTokenNotForTopic,
        DuplicateHeaders,
        IdleTimeout,
        InvalidPushType,
        MissingDeviceToken,
        MissingTopic,
        PayloadEmpty,
        TopicDisallowed,

        /// <summary>
        /// 403
        /// </summary>
        BadCertificate,
        BadCertificateEnvironment,
        ExpiredProviderToken,
        Forbidden,
        InvalidProviderToken,
        MissingProviderToken,

        /// <summary>
        /// 404
        /// </summary>
        BadPath,

        /// <summary>
        /// 405
        /// </summary>
        MethodNotAllowed,

        /// <summary>
        /// 410
        /// </summary>
        Unregistered,

        /// <summary>
        /// 413
        /// </summary>
        PayloadTooLarge,

        /// <summary>
        /// 429
        /// </summary>
        TooManyProviderTokenUpdates,
        TooManyRequests,

        /// <summary>
        /// 500
        /// </summary>
        InternalServerError,

        // 503
        ServiceUnavailable,
        Shutdown
    }
}