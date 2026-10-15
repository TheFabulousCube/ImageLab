using ImageLab.Api.Endpoints;
using ImageLab.Core;
using ImageLab.Core.Telemetry;
using ImageLab.Providers.Fake;
using ImageLab.Providers.OpenAI;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImageLab(builder.Configuration.GetSection("ImageLab"));
builder.Services.AddFakeImageProvider();
builder.Services.AddOpenAIImageProvider(builder.Configuration.GetSection("ImageLab:OpenAI"));

// Console exporters are for development. Swap in OTLP or Application Insights later.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(ImageLabTelemetry.SourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(ImageLabTelemetry.MeterName)
        .AddConsoleExporter());

var app = builder.Build();

app.MapImageEndpoints();

app.Run();
