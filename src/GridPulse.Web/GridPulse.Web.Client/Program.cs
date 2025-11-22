using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "GridPulseTheme";
    options.Duration = TimeSpan.FromDays(365);
});

await builder.Build().RunAsync();
