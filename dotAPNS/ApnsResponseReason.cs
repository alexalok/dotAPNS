namespace dotAPNS
{
    public enum ApnsResponseReason
    {
        Unknown = - 1,

        // 200
        Success,

        // 400
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

        // 403
        BadCertificate,
        BadCertificateEnvironment,
        ExpiredProviderToken,
        Forbidden,
        InvalidProviderToken,
        MissingProviderToken,

        // 404
        BadPath,

        // 405
        MethodNotAllowed,

        // 410
        Unregistered,

        // 413
        PayloadTooLarge,

        // 429
        TooManyProviderTokenUpdates,
        TooManyRequests,

        // 500
        InternalServerError,

        // 503
        ServiceUnavailable,
        Shutdown
    }
}