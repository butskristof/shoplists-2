using Shoplists.DatabaseMigrator;
using Shoplists.Persistence;
using Shoplists.ServiceDefaults;
using Shoplists.ServiceDefaults.Constants;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddPersistence(Resources.Postgres);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
