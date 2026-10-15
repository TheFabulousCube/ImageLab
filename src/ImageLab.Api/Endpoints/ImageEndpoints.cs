using ImageLab.Core.Pipeline;
using Microsoft.Extensions.AI;

namespace ImageLab.Api.Endpoints;

public sealed record GenerateImageRequest(string Prompt);

public sealed record GeneratedImage(string MediaType, string? FilePath);

public sealed record GenerateImageResponse(IReadOnlyList<GeneratedImage> Images);

public static class ImageEndpoints
{
    private const int MaxPromptLength = 1000;

    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/generate-image", async (
            GenerateImageRequest body,
            IImageGenerator generator,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(body.Prompt))
            {
                return Results.BadRequest(new { error = "Prompt is required." });
            }

            if (body.Prompt.Length > MaxPromptLength)
            {
                return Results.BadRequest(new { error = $"Prompt must be {MaxPromptLength} characters or fewer." });
            }

            var response = await generator.GenerateAsync(
                new ImageGenerationRequest(body.Prompt),
                options: null,
                cancellationToken);

            var images = response.Contents
                .OfType<DataContent>()
                .Select(content => new GeneratedImage(content.MediaType ?? "application/octet-stream", SavedPath(content)))
                .ToList();

            return Results.Ok(new GenerateImageResponse(images));
        });

        return app;
    }

    private static string? SavedPath(AIContent content)
        => content.AdditionalProperties is { } properties
           && properties.TryGetValue(FileSavingImageGenerator.FilePathKey, out var path)
            ? path?.ToString()
            : null;
}
