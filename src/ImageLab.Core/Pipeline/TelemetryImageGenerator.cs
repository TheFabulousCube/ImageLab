using System.Diagnostics;
using ImageLab.Core.Telemetry;
using Microsoft.Extensions.AI;

namespace ImageLab.Core.Pipeline;

/// <summary>
/// Emits a trace span and metrics (request count, failure count, duration) per generation.
/// Tags carry the provider and prompt length, never the prompt text.
/// </summary>
public sealed class TelemetryImageGenerator : DelegatingImageGenerator
{
    private readonly string _providerName;

    public TelemetryImageGenerator(IImageGenerator inner, string providerName)
        : base(inner)
    {
        _providerName = providerName;
    }

    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ImageLabTelemetry.ActivitySource.StartActivity("image.generate", ActivityKind.Client);
        activity?.SetTag("imagelab.provider", _providerName);
        activity?.SetTag("imagelab.prompt.length", request.Prompt?.Length ?? 0);

        var tags = new TagList { { "provider", _providerName } };
        ImageLabTelemetry.Requests.Add(1, tags);

        var start = Stopwatch.GetTimestamp();
        try
        {
            var response = await base.GenerateAsync(request, options, cancellationToken);
            activity?.SetTag("imagelab.output.count", response.Contents.Count);
            return response;
        }
        catch (Exception ex)
        {
            ImageLabTelemetry.Failures.Add(1, tags);
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            ImageLabTelemetry.Duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
        }
    }
}
