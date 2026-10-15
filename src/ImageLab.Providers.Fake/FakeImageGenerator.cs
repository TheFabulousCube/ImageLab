using ImageLab.Core.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ImageLab.Providers.Fake;

/// <summary>
/// Returns the same 1x1 PNG every time. No network, no cost. Use it for development and tests.
/// </summary>
public sealed class FakeImageGenerator : IImageGenerator
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    public Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contents = new List<AIContent> { new DataContent(Png, "image/png") };
        return Task.FromResult(new ImageGenerationResponse(contents));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }
}

public sealed class FakeImageGeneratorProvider : IImageGeneratorProvider
{
    public string Name => "Fake";

    public IImageGenerator Create(IServiceProvider services) => new FakeImageGenerator();
}

public static class FakeImageServiceCollectionExtensions
{
    public static IServiceCollection AddFakeImageProvider(this IServiceCollection services)
    {
        services.AddSingleton<IImageGeneratorProvider, FakeImageGeneratorProvider>();
        return services;
    }
}
