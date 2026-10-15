using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ImageLab.Core.Pipeline;

/// <summary>
/// Logs each generation request: start, completion with duration, and failures.
/// The prompt itself is logged only at Debug level, since prompts can contain personal data.
/// </summary>
public sealed class LoggingImageGenerator : DelegatingImageGenerator
{
    private readonly string _providerName;
    private readonly ILogger<LoggingImageGenerator> _logger;

    public LoggingImageGenerator(
        IImageGenerator inner,
        string providerName,
        ILogger<LoggingImageGenerator> logger)
        : base(inner)
    {
        _providerName = providerName;
        _logger = logger;
    }

    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var promptLength = request.Prompt?.Length ?? 0;
        _logger.LogInformation(
            "Generating image with provider {Provider}. Prompt length: {PromptLength}",
            _providerName, promptLength);
        _logger.LogDebug("Prompt for {Provider}: {Prompt}", _providerName, request.Prompt);

        var start = Stopwatch.GetTimestamp();
        try
        {
            var response = await base.GenerateAsync(request, options, cancellationToken);

            _logger.LogInformation(
                "Generated {ImageCount} item(s) with provider {Provider} in {ElapsedMs} ms",
                response.Contents.Count,
                _providerName,
                Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Image generation failed with provider {Provider} after {ElapsedMs} ms",
                _providerName,
                Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            throw;
        }
    }
}
