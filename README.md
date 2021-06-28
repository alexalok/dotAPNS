## 🍎 What is dotAPNS?
[![Build status](https://img.shields.io/github/workflow/status/alexalok/dotAPNS/Test)](https://github.com/alexalok/dotAPNS/actions?query=workflow%3ATest)	[![Nuget package](https://img.shields.io/nuget/v/dotAPNS)](https://www.nuget.org/packages/dotAPNS/)	[![Nuget package downloads](https://img.shields.io/nuget/dt/dotAPNS?label=nuget%20downloads)](https://www.nuget.org/packages/dotAPNS/)

dotAPNS is a small library used to send pushes to Apple devices via the [HTTP/2 APNs API](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server), which is an officially recommended way of interacting with APNs (*Apple Push Notification service*).

dotAPNS itself targets **.NET Framework 4.6, netstandard2.0 and netstandard2.1**, while dotAPNS ASP.NET Core integration library targets **netstandard2.0**. The latter, however, is not needed to use the main library - it just provides some infrastructure helpers to ease the integration of the library into ASP.NET Core projects.

## 🛠️ How to use it?

1. Register your app with APNs [(?)](https://developer.apple.com/documentation/usernotifications/registering_your_app_with_apns)
2. Decide whether you will use [certificate-based](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_certificate-based_connection_to_apns) or [token-based](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_token-based_connection_to_apns) connection to APNs. Token-based is easier since you won't have to perform any certificate conversions. For specific instructions, see below.
3. Start sending pushes!

---



## 📃 Certificate-based connection

**Important:** dotAPNS supports certificate-based connection only on .NET Core 3.0 and above. Full .NET Framework and .NET Core 2.2 and below are not supported! [(Why?)](https://github.com/alexalok/dotAPNS/issues/6)

```c#
var apns = ApnsClient.CreateUsingCert("voip.p12");
```
or
```c#
var cert = new X509Certificate2("voip.p12");
var apns = ApnsClient.CreateUsingCert(cert);
```

Change `voip.p12` so that it points to the certificate you are intended to use.

**Note:** dotAPNS supports certificates only in x509 format. You might have to convert the certificate you download from Developer Center to the supported format.

## 🔏 Token-based connection

**Important:** on .NET Core 2.0, 2.1 and 2.2 you need to execute the following code once before using library: `AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);`. Again, this needs to be called only *once*.

First, you need to specify some options that the library will use to create an authentication token:

```c#
var options = new ApnsJwtOptions() 
{
    BundleId = "bundle", 
    CertContent = "-----BEGIN PRIVATE KEY-----\r\nMIGTA ... -----END PRIVATE KEY-----", 
    // CertFilePath = "secret.p8", // use either CertContent or CertFilePath, not both
    KeyId = "key", 
    TeamId = "team"
};
```

`BundleId` - your app’s bundle ID. Should not include specific topics (i.e. `com.myapp` but not `com.myapp.voip`).

`CertFilePath` - path to the .p8 certificate you have downloaded from the Developer Center.

`CertContent` - contents of the .p8 certificate. If specified, `CertFilePath` **must not be set**. This option allows you to use the library without having to store the actual certificate file. Contents may include everything in the certificate file (even the `BEGIN/END PRIVATE KEY` lines) or just the base64-encoded body, with or without line breaks (both `\r\n` and `\r` are supported ). 

`KeyId` - The 10-character Key ID you obtained from your developer account [(?)](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_token-based_connection_to_apns#2943371).

`TeamId` - The 10-character Team ID you use for developing your company’s apps. Obtain this value from your developer account.

Once you've gathered all the information needed and created an options instance, it's time to call

```c#
var options = new ApnsJwtOptions() { ... };
var apns = ApnsClient.CreateUsingJwt(new HttpClient(), options);
```

**Important:** when on .NET Framework, make sure that the `HttpClient` instance you pass to `CreateUsingJwt()` has a HTTP2 support. The default `HttpClient` **does not have it and will not work**. As a workaround it is suggested to install [System.Net.Http.WinHttpHandler](https://www.nuget.org/packages/System.Net.Http.WinHttpHandler/) .nuget package and instantiate the `HttpClient` with this custom handler as follows: `new HttpClient(new WinHttpHandler())`.

Note that the library *requires* you to supply `HttpClient` instance - this is done so that you can manage its lifecycle. Please see [this article](https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/) to learn more about why this is important. Also note that certificate-based connection method does not support `HttpClient` injection as of moment of writing.

**🎉You're now all set to start sending pushes!🎉**

---



## 🔔 Send a push

To send the most basic alert notifications you might use the following code:

```c#
var options = new ApnsJwtOptions() { ... };
var apns = ApnsClient.CreateUsingJwt(new HttpClient(), options); 
var push = new ApplePush(ApplePushType.Alert)
    .AddAlert("title", "body")
    .AddToken("token");

try
{
    var response = await apns.Send(push);
    if (response.IsSuccessful)
    {
        Console.WriteLine("An alert push has been successfully sent!");
    }
    else
    {
        switch (response.Reason)
        {
            case ApnsResponseReason.BadCertificateEnvironment:
                // The client certificate is for the wrong environment.
                // TODO: retry on another environment
                break;
                // TODO: process other reasons we might be interested in
            default:
                throw new ArgumentOutOfRangeException(nameof(response.Reason), response.Reason, null);
        }
        Console.WriteLine("Failed to send a push, APNs reported an error: " + response.ReasonString);
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("Failed to send a push: HTTP request timed out.");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Failed to send a push. HTTP request failed: " + ex);
}
catch (ApnsCertificateExpiredException)
{
    Console.WriteLine("APNs certificate has expired. No more push notifications can be sent using it until it is replaced with a new one.");
}
```

## Background (aka "silent") push
```c#
var push = new ApplePush(ApplePushType.Background)
    .AddContentAvailable()
    .AddToken("replaceme");
```

Note that background pushes are subject to [certain limitations](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/pushing_background_updates_to_your_app). There is no way to circumvent this.

## Custom payload fields

To add your own fields to the push payload you may use the `ApplePush.AddCustomProperty` method, for example:
### To add the field to the root of the payload
```c#
var push = new ApplePush(ApplePushType.Alert)
.AddCustomProperty("thread-id", "123");
```
###  Or to the `aps` section
```c#
var push = new ApplePush(ApplePushType.Alert)
.AddCustomProperty("thread-id", "123", true);
```
An example can also be found [here](https://github.com/alexalok/dotAPNS/blob/72f23e74f81717968b7dfcc997660b105d8c3d63/dotAPNS.Tests/ApplePush_Tests.cs#L90).

Check out more examples [here](https://github.com/alexalok/dotAPNS/tree/master/dotAPNS.Tests).



---

# dotAPNS ASP.NET Core integration
[![NuGet](https://img.shields.io/nuget/v/dotAPNS.AspNetCore?style=plastic)](https://www.nuget.org/packages/dotAPNS.AspNetCore/ "NuGet")

**dotAPNS.AspNetCore** is a library containing **ApnsService** class and some helpers methods to simplify its usage.

**ApnsService** is a class that is supposed to be consumed by controllers or other services of ASP.NET Core application. It is expected to be registered as a singleton.

**ApnsService** provides caching - it will reuse ApnsClients created earlier if they have the same certificate thumbprint (for certificate-based connections) or the same bundle id (for token-based connections). At the moment of writing cached ApnsClients never expire and there is no way to make them expire manually. 

**Note: at the moment of writing caching cannot be disabled for ApnsService!**

You can register ApnsService automatically within your application by calling

`services.AddApns();` in `ConfigureServices` method of your `Startup` class:

```c#
public void ConfigureServices(IServiceCollection services)
{
	services.AddRazorPages();
	services.AddApns(); // <-- this call registers ApnsService
    // ...
}
```

`AddApns` does the following:

1. Registers named HttpClient with name `dotAPNS`.
2. Registers **ApnsClientFactory** as a singleton - this is the factory used by ApnsService to create ApnsClient instances.
3. Registers **ApnsService** as a singleton itself.



After registering, you can inject the ApnsService into any controller:

```c#
readonly IApnsService _apnsService;

public MyController(IApnsService apnsService)
{
	_apnsService = apnsService;
}
```

**Note:  you need to inject the interface (IApnsService), not the type itself!**

 

IApnsService defines 4 methods that can be used to send pushes:

```c#
Task<ApnsResponse> SendPush(ApplePush push, X509Certificate2 cert);

Task<ApnsResponse> SendPush(ApplePush push, ApnsJwtOptions jwtOptions);

Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, X509Certificate2 cert);

Task<List<ApnsResponse>> SendPushes(IReadOnlyCollection<ApplePush> pushes, ApnsJwtOptions jwtOptions);
```

For example, the following code can be used to send a simple [background](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/pushing_background_updates_to_your_app) push:

```c#
 var push = new ApplePush(ApplePushType.Background)
     .AddContentAvailable()
     .AddToken("token");
var options = new ApnsJwtOptions() { ... };
var response = await _apnsService.SendPush(push, options);
```

