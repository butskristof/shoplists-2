namespace Shoplists.ServiceDefaults.Constants;

public static class HealthCheckConstants
{
    internal const string BasePath = "/health";

    public static class Endpoints
    {
        public const string Live = $"{BasePath}/live";
        public const string Ready = $"{BasePath}/ready";
    }

    internal static class Tags
    {
        internal const string Live = "live";
    }

    internal static class Names
    {
        internal const string Self = "self";
    }
}
