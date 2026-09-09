var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Web>("web", launchProfileName: "http");

builder.Build().Run();
