using Shoplists.Application.Common.Authentication;
using Shoplists.DatabaseMigrator;
using Shoplists.Persistence;
using Shoplists.ServiceDefaults;
using Shoplists.ServiceDefaults.Constants;

// This host applies EF Core migrations to the database and then exits.
// It runs as part of the Aspire orchestration: the AppHost starts this project after
// Postgres is ready (WaitFor), and blocks dependent services like the API until this
// project completes successfully (WaitForCompletion).
//
// The migration logic itself lives in the Persistence project (DatabaseMigrationRunner)
// to keep AppDbContext internal. This host is a thin orchestration shell.

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Register AppDbContext with the Aspire-provided connection string.
// This is the same AddPersistence call the API host uses — the connection name must match
// the database resource name defined in the AppHost.
builder.AddPersistence(Resources.AppDb);

// AppDbContext requires ICurrentUser for authorization-scoped queries. The migrator never
// executes application queries — it only runs MigrateAsync() — but the DI container still
// needs to resolve the dependency when constructing AppDbContext.
builder.Services.AddSingleton<ICurrentUser, MigrationCurrentUser>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();
