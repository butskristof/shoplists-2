using Projects;
using Shoplists.ServiceDefaults.Constants;

var builder = DistributedApplication.CreateBuilder(args);

#region Redis

var valkey = builder.AddValkey(Resources.Valkey).WithDataVolume().WithPersistence();

#endregion

#region Database

// No WithPersistence() needed — unlike Valkey/Redis, PostgreSQL writes to disk by default (WAL).
var postgres = builder.AddPostgres(Resources.Postgres).WithDataVolume();
var appDb = postgres.AddDatabase(Resources.AppDb);

// Dedicated worker that applies EF Core migrations and exits.
// WaitFor(postgres): don't start until the database container is healthy.
// The API (and any future services) use WaitForCompletion on this resource,
// so they won't start until migrations have been applied successfully.
// If the migrator fails, dependent resources remain blocked — the failure is immediately
// visible in the Aspire dashboard.
var databaseMigrator = builder
    .AddProject<DatabaseMigrator>(Resources.DatabaseMigrator)
    .WithReference(appDb)
    .WaitFor(appDb);

#endregion

#region OIDC Parameters

var oidcAuthority = builder
    .AddParameter("oidc-authority")
    .WithDescription("OIDC issuer base URL (JWT Bearer Authority for the backend API).");
var oidcAudience = builder
    .AddParameter("oidc-audience")
    .WithDescription("Expected audience claim in JWT access tokens.");
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

#endregion

#region API

var api = builder
    .AddProject<Api>(Resources.Api)
    .WithReference(appDb)
    .WaitForCompletion(databaseMigrator)
    .WithEnvironment("Authentication__Authority", oidcAuthority)
    .WithEnvironment("Authentication__Audience", oidcAudience)
    .WithEnvironment("Authentication__ClientId", oidcClientId)
    .WithEnvironment("Authentication__ClientSecret", oidcClientSecret)
    .WithEnvironment("Authentication__AuthorizationUrl", oidcAuthorizationUrl)
    .WithEnvironment("Authentication__TokenUrl", oidcTokenUrl)
    .WithHttpHealthCheck(HealthCheckConstants.Endpoints.Ready)
    .WithUrlForEndpoint(
        "http",
        static _ => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar" }
    )
    .WithUrlForEndpoint(
        "http",
        static _ => new ResourceUrlAnnotation
        {
            Url = "/openapi/v1.json",
            DisplayText = "OpenAPI (json)",
        }
    )
    .WithUrlForEndpoint(
        "http",
        static _ => new ResourceUrlAnnotation
        {
            Url = "/openapi/v1.yaml",
            DisplayText = "OpenAPI (yaml)",
        }
    );

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
