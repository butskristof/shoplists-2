#pragma warning disable MA0048 // File name must match type name — top-level statements file

using Shoplists.AppHost.Constants;
using Shoplists.ServiceDefaults.Constants;

var builder = DistributedApplication.CreateBuilder(args);

#region Redis

var valkey = builder.AddValkey(Resources.Valkey).WithDataVolume().WithPersistence();

#endregion

#region API

var api = builder
    .AddProject<Projects.Api>(Resources.Api)
    .WithHttpHealthCheck(HealthCheckConstants.Endpoints.Ready);

#endregion

#region Frontend

var oidcSessionSecret = builder
    .AddParameter("oidc-session-secret", secret: true)
    .WithDescription("Random string (min 48 chars) used to encrypt the user session.");
var oidcAuthSessionSecret = builder
    .AddParameter("oidc-auth-session-secret", secret: true)
    .WithDescription(
        "Random string (min 48 chars) used to encrypt individual OAuth flow sessions."
    );
var oidcTokenKey = builder
    .AddParameter("oidc-token-key", secret: true)
    .WithDescription("Base64-encoded AES-256-GCM key used to encrypt the server-side token store.");

var oidcClientId = builder
    .AddParameter("oidc-client-id")
    .WithDescription("OIDC client ID registered with the identity provider.");
var oidcClientSecret = builder
    .AddParameter("oidc-client-secret", secret: true)
    .WithDescription("OIDC client secret provided by identity provider.");
var oidcOpenIdConfiguration = builder
    .AddParameter("oidc-openid-configuration")
    .WithDescription(
        "URL to the OpenID Connect discovery document (e.g. https://idp.example.com/.well-known/openid-configuration)."
    );
var oidcAuthorizationUrl = builder
    .AddParameter("oidc-authorization-url")
    .WithDescription("OIDC authorization endpoint URL.");
var oidcTokenUrl = builder
    .AddParameter("oidc-token-url")
    .WithDescription("OIDC token endpoint URL.");
var oidcUserInfoUrl = builder
    .AddParameter("oidc-userinfo-url")
    .WithDescription("OIDC userinfo endpoint URL.");
var oidcLogoutUrl = builder
    .AddParameter("oidc-logout-url")
    .WithDescription("OIDC end session (logout) endpoint URL.");

var frontend = builder
    .AddJavaScriptApp(name: Resources.Frontend, appDirectory: "../../frontend")
    .WithHttpEndpoint(env: "NITRO_PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(valkey)
    .WaitFor(api);

frontend
    .WithEnvironment("NUXT_OIDC_SESSION_SECRET", oidcSessionSecret)
    .WithEnvironment("NUXT_OIDC_AUTH_SESSION_SECRET", oidcAuthSessionSecret)
    .WithEnvironment("NUXT_OIDC_TOKEN_KEY", oidcTokenKey)
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_CLIENT_ID", oidcClientId)
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_CLIENT_SECRET", oidcClientSecret)
    .WithEnvironment(
        "NUXT_OIDC_PROVIDERS_OIDC_REDIRECT_URI",
        () =>
        {
            var endpoint = frontend.GetEndpoint("http");
            return $"{endpoint.Url}/auth/oidc/callback";
        }
    )
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_OPEN_ID_CONFIGURATION", oidcOpenIdConfiguration)
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_AUTHORIZATION_URL", oidcAuthorizationUrl)
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_TOKEN_URL", oidcTokenUrl)
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_USER_INFO_URL", oidcUserInfoUrl)
    .WithEnvironment("NUXT_OIDC_PROVIDERS_OIDC_LOGOUT_URL", oidcLogoutUrl)
    .WithEnvironment("NUXT_REDIS_HOST", valkey.Resource.Host)
    .WithEnvironment("NUXT_REDIS_PORT", valkey.Resource.Port)
    .WithEnvironment("NUXT_REDIS_PASSWORD", valkey.Resource.PasswordParameter!)
    .WithEnvironment(
        "NUXT_REDIS_TLS",
        () => valkey.Resource.PrimaryEndpoint.TlsEnabled ? "true" : "false"
    );

#endregion

builder.Build().Run();
