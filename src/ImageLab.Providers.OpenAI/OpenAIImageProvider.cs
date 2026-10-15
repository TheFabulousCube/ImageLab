using OpenAI;
using ImageLab.Core.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImageLab.Providers.OpenAI;

public sealed class OpenAIImageOptions
{
    /// <summary>Azure OpenAI resource endpoint, for example https://your-resource.openai.azure.com/</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Store in user secrets or an environment variable, never in appsettings.json.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Deployment name of the image model.</summary>
    public string Model { get; set; } = "gpt-image-1-mini";
}

public sealed class OpenAIImageGeneratorProvider : IImageGeneratorProvider
{
    private readonly IOptions<OpenAIImageOptions> _options;

    public OpenAIImageGeneratorProvider(IOptions<OpenAIImageOptions> options)
    {
        _options = options;
    }

    public string Name => "OpenAI";

    public IImageGenerator Create(IServiceProvider services)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "ImageLab:OpenAI:Endpoint and ImageLab:OpenAI:ApiKey must be set (use user secrets for the key).");
        }

        var client = new OpenAIClient(options.ApiKey);
        return client.GetImageClient(options.Model).AsIImageGenerator();
    }
}

public static class OpenAIImageServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAIImageProvider(this IServiceCollection services, IConfiguration section)
    {
        services.AddOptions<OpenAIImageOptions>().Bind(section);
        services.AddSingleton<IImageGeneratorProvider, OpenAIImageGeneratorProvider>();
        return services;
    }
}
