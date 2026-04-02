var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var oidcSessionSecret = builder.AddParameter("oidc-session-secret", secret: true)
    .WithDescription("Random string (min 48 chars) used to encrypt the user session.");
var oidcAuthSessionSecret = builder.AddParameter("oidc-auth-session-secret", secret: true)
    .WithDescription("Random string (min 48 chars) used to encrypt individual OAuth flow sessions.");
var oidcTokenKey = builder.AddParameter("oidc-token-key", secret: true)
    .WithDescription("Base64-encoded AES-256-GCM key used to encrypt the server-side token store.");

var frontend = builder
    .AddJavaScriptApp(name: "frontend", appDirectory: "../../frontend")
    .WithHttpEndpoint(env: "NITRO_PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NUXT_OIDC_SESSION_SECRET", oidcSessionSecret)
    .WithEnvironment("NUXT_OIDC_AUTH_SESSION_SECRET", oidcAuthSessionSecret)
    .WithEnvironment("NUXT_OIDC_TOKEN_KEY", oidcTokenKey)
    .WithEnvironment("REDIS_HOST", redis.Resource.Host)
    .WithEnvironment("REDIS_PORT", redis.Resource.Port)
    .WithEnvironment("REDIS_PASSWORD", redis.Resource.PasswordParameter!)
    .WaitFor(redis);

builder.Build().Run();