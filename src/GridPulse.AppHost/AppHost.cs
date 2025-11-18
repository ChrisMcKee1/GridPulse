var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.GridPulse_WebApi>("gridpulse-api");

var web = builder.AddProject<Projects.GridPulse_Web>("gridpulse-web")
	.WithReference(api)
	.WithExternalHttpEndpoints();

builder.Build().Run();
