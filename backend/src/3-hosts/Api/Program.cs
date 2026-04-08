using Scalar.AspNetCore;
using Shoplists.Api.Extensions;
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
builder.Services.AddApi();

var app = builder.Build();

app.MapDefaultEndpoints();

app
// the default exception handler will catch unhandled exceptions and return
// them as ProblemDetails with status code 500 Internal Server Error
.UseExceptionHandler()
    // the status code pages will map additional failed requests (outside of
    // those throwing exceptions) to responses with ProblemDetails body content
    // this includes 404, method not allowed, ... (all status codes between 400 and 599)
    // keep in mind that this middleware will only activate if the body is empty when
    // it reaches it
    .UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(pattern: "/openapi/{documentName}.json");
    app.MapOpenApi(pattern: "/openapi/{documentName}.yaml");
    app.MapOpenApi(pattern: "/openapi/{documentName}.yml");
    app.MapScalarApiReference();
}

app.MapShoplistsApi();

await app.RunAsync();
