using Shoplists.Application;
using Shoplists.Infrastructure;
using Shoplists.Persistence;
using Shoplists.ServiceDefaults;
using Shoplists.ServiceDefaults.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.AddPersistence(Resources.Postgres);

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
