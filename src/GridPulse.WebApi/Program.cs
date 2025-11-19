using System.Text.Json.Serialization;
using GridPulse.Application;
using GridPulse.Application.Services;
using GridPulse.Infrastructure;
using GridPulse.Infrastructure.Persistence;
using GridPulse.WebApi.Extensions;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<GridPulseDbContext>("gridpulse-db");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "GridPulse API";
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Default");
app.MapDefaultEndpoints();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

var outagesGroup = app.MapGroup("/api/outages")
    .WithTags("Outages");

outagesGroup.MapGet(
    "/",
    async (IOutageReadService service, CancellationToken cancellationToken) =>
    {
        var items = await service.GetRecentAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(items);
    })
    .WithName("GetRecentOutages");

outagesGroup.MapGet(
    "/{id:guid}",
    async (Guid id, IOutageReadService service, CancellationToken cancellationToken) =>
    {
        var outage = await service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return outage is null ? Results.NotFound() : Results.Ok(outage);
    })
    .WithName("GetOutageById");

await app.Services.InitializeDatabaseAsync();

app.Run();
