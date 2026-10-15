using ImageLab.Core;
using ImageLab.Core.Pipeline;
using ImageLab.Providers.Fake;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageLab.Core.Tests;

public class PipelineTests
{
    [Fact]
    public async Task Generate_SavesImageAndReportsPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "imagelab-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Provider"] = "Fake",
                    ["OutputDirectory"] = outputDirectory,
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddImageLab(config);
            services.AddFakeImageProvider();

            using var provider = services.BuildServiceProvider();
            var generator = provider.GetRequiredService<IImageGenerator>();

            var response = await generator.GenerateAsync(new ImageGenerationRequest("a red bicycle"));

            var image = Assert.IsType<DataContent>(Assert.Single(response.Contents));
            var path = Assert.IsType<string>(image.AdditionalProperties?[FileSavingImageGenerator.FilePathKey]);

            Assert.True(File.Exists(path));
            Assert.Equal("image/png", image.MediaType);
            Assert.Equal(outputDirectory, Path.GetDirectoryName(path));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void AddImageLab_UnknownProvider_ThrowsWithAvailableNames()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Provider"] = "Nope" })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImageLab(config);
        services.AddFakeImageProvider();

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IImageGenerator>());

        Assert.Contains("Nope", exception.Message);
        Assert.Contains("Fake", exception.Message);
    }
}
