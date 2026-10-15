using Microsoft.Extensions.AI;

namespace ImageLab.Core.Pipeline;

/// <summary>
/// Base class for pipeline stages. Forwards every call to the inner generator
/// so that a stage only needs to override the behavior it adds.
/// </summary>
public abstract class DelegatingImageGenerator : IImageGenerator
{
    protected DelegatingImageGenerator(IImageGenerator inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    protected IImageGenerator Inner { get; }

    public virtual Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => Inner.GenerateAsync(request, options, cancellationToken);

    public virtual object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this)
            ? this
            : Inner.GetService(serviceType, serviceKey);

    public virtual void Dispose() => Inner.Dispose();
}
