using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ImageLab.Core.Telemetry;

/// <summary>
/// Shared ActivitySource and Meter for ImageLab. Register these names with
/// OpenTelemetry (or any other listener) to receive the data.
/// </summary>
public static class ImageLabTelemetry
{
    public const string SourceName = "ImageLab";
    public const string MeterName = "ImageLab";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> Requests =
        Meter.CreateCounter<long>("imagelab.image.requests", description: "Image generation requests started.");

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("imagelab.image.failures", description: "Image generation requests that failed.");

    public static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("imagelab.image.duration", unit: "s", description: "Image generation duration.");
}
