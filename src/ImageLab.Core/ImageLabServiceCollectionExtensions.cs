using ImageLab.Core.Options;
using ImageLab.Core.Pipeline;
using ImageLab.Core.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageLab.Core;

public static class ImageLabServiceCollectionExtensions
{
    /// <summary>
    /// Registers an IImageGenerator built from the selected provider, wrapped in
    /// logging, telemetry, and file-saving stages. Register at least one
    /// IImageGeneratorProvider (for example AddFakeImageProvider) before resolving it.
    /// </summary>
    public static IServiceCollection AddImageLab(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ImageLabOptions>().Bind(configuration);
        services.AddSingleton<IImageGenerator>(CreatePipeline);
        return services;
    }

    private static IImageGenerator CreatePipeline(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<ImageLabOptions>>().Value;
        var providers = services.GetServices<IImageGeneratorProvider>().ToList();

        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.Name, options.Provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No image provider named '{options.Provider}'. Registered providers: " +
                string.Join(", ", providers.Select(p => p.Name)));

        // The first stage added is the outermost, so it sees each call first.
        // Logging wraps telemetry so failures are logged and counted; file saving sits
        // closest to the provider, so telemetry durations include the write.
        return new ImageGeneratorBuilder(p => provider.Create(p))
            .Use((inner, sp) => new Pipeline.LoggingImageGenerator(
                inner,
                provider.Name,
                sp.GetRequiredService<ILogger<Pipeline.LoggingImageGenerator>>()))
            .Use((inner, _) => new TelemetryImageGenerator(inner, provider.Name))
            .Use((inner, _) => new FileSavingImageGenerator(inner, options.OutputDirectory))
            .Build(services);
    }
}
