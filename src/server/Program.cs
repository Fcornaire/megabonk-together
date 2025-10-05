using MegabonkTogether.Server;
using MegabonkTogether.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<ConfigOptions>(builder.Configuration.GetSection("Config"));
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<IRendezVousServer, RendezVousServer>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("MegabonkTogether.Server")
            .AddPrometheusExporter();
    });

builder.Configuration.GetSection("Config").Get<ConfigOptions>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear(); //This should be fine as long as we are behind a trusted proxy (the server run behind a nginx)
    options.KnownProxies.Clear(); //You absolute want to remove these two lines if you are modifying this for you and not using a proxy
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseWebSockets();

app.MapPrometheusScrapingEndpoint("/metrics");

_ = app.Services.GetRequiredService<IRendezVousServer>(); // Initialize UDP server
_ = app.Services.GetRequiredService<IMetricsService>(); // Initialize Metrics service

app.MapWhen(context => context.Request.Path.StartsWithSegments("/ws"), wsApp =>
{
    wsApp.Run(async context =>
    {
        var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var queueId = context.Request.QueryString.ToString().Replace("?", "");

            await handler.HandleClientAsync(webSocket,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Connection.RemotePort,
                queueId,
                context.RequestAborted);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    });
});

await app.RunAsync();