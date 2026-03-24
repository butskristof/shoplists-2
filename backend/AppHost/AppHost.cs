var builder = DistributedApplication.CreateBuilder(args);

// var frontend = builder
//     .AddJavaScriptApp(name: "frontend", appDirectory: "../../frontend")
//     .WithHttpEndpoint(env: "NITRO_PORT")
//     .WithExternalHttpEndpoints();

builder.Build().Run();