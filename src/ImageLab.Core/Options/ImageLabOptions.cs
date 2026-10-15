namespace ImageLab.Core.Options;

public sealed class ImageLabOptions
{
    /// <summary>Name of the registered provider to use, such as "Fake" or "OpenAI".</summary>
    public string Provider { get; set; } = "Fake";

    /// <summary>Folder where generated images are saved.</summary>
    public string OutputDirectory { get; set; } = "generated-images";
}
