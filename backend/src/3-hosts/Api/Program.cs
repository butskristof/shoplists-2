using Scalar.AspNetCore;
using Shoplists.Api.Extensions;
using Shoplists.Api.OpenApi;
using Shoplists.Application;
using Shoplists.Infrastructure;
using Shoplists.Persistence;
using Shoplists.ServiceDefaults;
using Shoplists.ServiceDefaults.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.AddPersistence(Resources.AppDb);

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<StronglyTypedIdSchemaTransformer>();
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapShoplistsApi();

await app.RunAsync();
