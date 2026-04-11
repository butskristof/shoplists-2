namespace Shoplists.ServiceDefaults.Constants;

// resources used in the Aspire AppHost, can also be used in the projects themselves to refer to these resources,
// e.g. when using them in DI setup (like registering the DbContext using AppDb)
public static class Resources
{
    public const string Valkey = "valkey";
    public const string Postgres = "postgres";
    public const string AppDb = "appdb";

    public const string Api = "api";
    public const string DatabaseMigrator = "database-migrator";
    public const string Frontend = "frontend";
}
