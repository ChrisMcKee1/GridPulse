using GridPulse.Web.Client.Pages;
using GridPulse.Web.Components;
using GridPulse.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddServiceDiscovery();

builder.Services.AddHttpClient<GridPulseApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var configuredValue = configuration["Api:BaseAddress"];

        if (!string.IsNullOrWhiteSpace(configuredValue) && Uri.TryCreate(configuredValue, UriKind.Absolute, out var configuredUri))
        {
            client.BaseAddress = configuredUri;
            return;
        }

        client.BaseAddress = new Uri("https+http://gridpulse-api");
    })
    .AddServiceDiscovery();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GridPulse.Web.Client._Imports).Assembly);

app.Run();
