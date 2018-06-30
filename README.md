Azure Active Directory Proxy for SMART on FHIR
----------------------------------------------

[Azure Active Directory](https://azure.microsoft.com/en-us/services/active-directory/) supports [OAuth2 authorization flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-protocols-oauth-code), however, some of the query parameter naming conventions are not compatible with [SMART on FHIR authorization](http://docs.smarthealthit.org/authorization/).

Specifically SMART on FHIR expects to initiate the authorization with something like:

```
GET https://auth-server/authorize?aud=https://fhir-server&client_id=XXX&scope=patient/*.read&...
```

Azure Active Directory offers [two version](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-compare) of the OAuth endpoint. Version 1 expects the `aud` (audience) parameter to be supplied as `resource` and in version 2, the audience is included in the fully qualified scope specifications. Specifically, if `aud=https://fhir-server` and `scope=patient/*.read`, the version 2 endpoint would expect no `aud` or `resource` parameter but the `scope` be fully qualified as `https://fhir-server/patient/*.read`. Moreover, the version 2 endpoint does not accept scopes with `/` (slash) in the name of the scope. All of these small differences makes it challenging to use Azure Active Directory in SMART on FHIR applications. 

This repository contains a small ASP.NET Core app that will act as a proxy in front of Azure Active Directory and translate the parameter specifications provided by a SMART on FHIR application into something that Azure Active directory can accept. 

You can deploy this app in an [Azure Web App](https://azure.microsoft.com/en-us/services/app-service/web/), or run it locally if you have .NET Core 2.1 installed. Run it locally with:

```
dotnet run
```

If you then initiate an autorization flow with:

```
GET http://localhost:5000/{TENANT-ID}/oauth2/v2.0/authorize?aud=https://fhir-server&client_id=XXX&scope=patient/*.read&...
```

It will effectively get translated into:

```
GET https://login.microsoftonline.com/{TENANT-ID}/oauth2/v2.0/authorize?client_id=XXX&scope=https://fhir-server/patient-*.read&...
```

Notice that the scope has been renamed with a hyphen and is now fully qualified. The app also supports the version 1 endpoint, in which case a request like:

```
GET http://localhost:5000/{TENANT-ID}/oauth2/authorize?aud=https://fhir-server&client_id=XXX&scope=patient/*.read&...
```

Would get translated to:

```
GET https://login.microsoftonline.com/{TENANT-ID}/oauth2/authorize?resource=https://fhir-server&client_id=XXX&...
```

The version 1 endpoint ignores scopes, they must be statically set when configuring the client application. 

Tokens can be obtained from the token endpoint (e.g. `http://localhost:5000/{TENANT-ID}/oauth2/v2.0/token`). The proxy performs a *redirect* when using the `authorize` endpoint, but since the token request is a `POST` operation, the app acts as a *proxy* for the request.

