var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("gridpulse-postgres")
    .WithDataVolume();

var outageDb = postgres.AddDatabase("gridpulse-db");

var api = builder.AddProject<Projects.GridPulse_WebApi>("gridpulse-api")
    .WithReference(outageDb);

var web = builder.AddProject<Projects.GridPulse_Web>("gridpulse-web")
	.WithReference(api)
	.WithExternalHttpEndpoints();

builder.Build().Run();
