using System.Diagnostics;
using ImageLab.Core.Pipeline;
using ImageLab.Core.Telemetry;
using ImageLab.Providers.Fake;
using Microsoft.Extensions.AI;

namespace ImageLab.Core.Tests;

public class TelemetryTests
{
    [Fact]
    public async Task Generate_EmitsActivityWithProviderAndOutputCount()
    {
        var captured = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ImageLabTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => captured.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        var generator = new TelemetryImageGenerator(new FakeImageGenerator(), "Fake");

        await generator.GenerateAsync(new ImageGenerationRequest("a lighthouse at dusk"));

        var activity = Assert.Single(captured);
        Assert.Equal("image.generate", activity.OperationName);
        Assert.Equal("Fake", activity.GetTagItem("imagelab.provider"));
        Assert.Equal(1, activity.GetTagItem("imagelab.output.count"));
        Assert.Equal("a lighthouse at dusk".Length, activity.GetTagItem("imagelab.prompt.length"));
    }

    [Fact]
    public async Task Generate_WhenInnerFails_MarksActivityAsError()
    {
        var captured = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ImageLabTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => captured.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        var generator = new TelemetryImageGenerator(new FailingImageGenerator(), "Failing");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync(new ImageGenerationRequest("anything")));

        var activity = Assert.Single(captured);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    /// <summary>Test double that always throws, to exercise the failure path.</summary>
    private sealed class FailingImageGenerator : IImageGenerator
    {
        public Task<ImageGenerationResponse> GenerateAsync(
            ImageGenerationRequest request,
            ImageGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new HttpRequestException("simulated provider outage");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
