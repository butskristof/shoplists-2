using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using Shoplists.Api.Authentication;
using Shoplists.Api.Extensions;
using Shoplists.Api.OpenApi;
using Shoplists.Application;
using Shoplists.Application.Common.Authentication;
using Shoplists.Infrastructure;
using Shoplists.Persistence;
using Shoplists.ServiceDefaults;
using Shoplists.ServiceDefaults.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.AddPersistence(Resources.AppDb);

builder.Services.AddScoped<ICurrentUser, FakeCurrentUser>();

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = typeInfo =>
    {
        var defaultId = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
        if (defaultId is null)
            return null;

        // Nested types (e.g. CreateShoplist.Response) get the default ID "Response",
        // which collides when multiple outer classes define the same nested type name.
        // Prefix with the declaring type name to match C# nested type notation.
        var declaringType = typeInfo.Type.DeclaringType;
        return declaringType is not null ? $"{declaringType.Name}.{defaultId}" : defaultId;
    };

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
