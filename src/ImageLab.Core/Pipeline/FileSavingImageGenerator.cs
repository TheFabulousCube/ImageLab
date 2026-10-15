using Microsoft.Extensions.AI;

namespace ImageLab.Core.Pipeline;

/// <summary>
/// Writes each returned image (DataContent) to the output folder. The saved path is
/// added to the content's AdditionalProperties under <see cref="FilePathKey"/>.
/// URI-based results are passed through unchanged.
/// </summary>
public sealed class FileSavingImageGenerator : DelegatingImageGenerator
{
    public const string FilePathKey = "ImageLab.FilePath";

    private readonly string _outputDirectory;

    public FileSavingImageGenerator(IImageGenerator inner, string outputDirectory)
        : base(inner)
    {
        _outputDirectory = outputDirectory;
    }

    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GenerateAsync(request, options, cancellationToken);

        foreach (var content in response.Contents.OfType<DataContent>())
        {
            if (content.Data.IsEmpty)
            {
                continue;
            }

            Directory.CreateDirectory(_outputDirectory);

            var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{ExtensionFor(content.MediaType)}";
            var path = Path.GetFullPath(Path.Combine(_outputDirectory, fileName));

            await using (var stream = File.Create(path))
            {
                await stream.WriteAsync(content.Data, cancellationToken);
            }

            content.AdditionalProperties ??= new();
            content.AdditionalProperties[FilePathKey] = path;
        }

        return response;
    }

    private static string ExtensionFor(string? mediaType) => mediaType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".bin",
    };
}
