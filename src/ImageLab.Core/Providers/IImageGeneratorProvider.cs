using Microsoft.Extensions.AI;

namespace ImageLab.Core.Providers;

/// <summary>
/// A named backend that can create an IImageGenerator. Provider packages register
/// an implementation so ImageLab can pick one by name from configuration.
/// </summary>
public interface IImageGeneratorProvider
{
    string Name { get; }

    IImageGenerator Create(IServiceProvider services);
}
