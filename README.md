# ImageLab

A small .NET 10 sample that generates images through Microsoft.Extensions.AI's `IImageGenerator`.
Providers are swappable by configuration. Every call passes through logging, telemetry, and file-saving stages.

## Projects

| Project                         | Purpose                                                                                            |
| ------------------------------- | -------------------------------------------------------------------------------------------------- |
| `src/ImageLab.Core`             | Provider-agnostic pipeline: logging, telemetry, file saving, provider selection. No provider SDKs. |
| `src/ImageLab.Providers.Fake`   | Returns a fixed 1x1 PNG. No network, no cost. Use for development and tests.                       |
| `src/ImageLab.Providers.OpenAI` | Azure OpenAI image generation via `AsIImageGenerator()`.                                           |
| `src/ImageLab.Api`              | Minimal API with `POST /generate-image` and console OpenTelemetry exporters.                       |
| `tests/ImageLab.Core.Tests`     | xUnit tests for the pipeline and telemetry.                                                        |

## Requirements

- .NET 10 SDK
- Network access to NuGet (`api.nuget.org`)
- For the OpenAI provider only: an Azure OpenAI resource with an image model deployment (the sample uses `gpt-image-1`)

## Build and run

```
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ImageLab.Api
```

Try the fake provider first. It needs no configuration:

```
curl -X POST http://localhost:5000/generate-image -H "Content-Type: application/json" -d "{\"prompt\":\"a red bicycle\"}"
```

(Check the port in the console output. It may not be 5000.)

## Configuration

`src/ImageLab.Api/appsettings.json`:

```json
"ImageLab": {
  "Provider": "Fake",
  "OutputDirectory": "generated-images",
  "OpenAI": { "Endpoint": "", "ApiKey": "", "Model": "gpt-image-1" }
}
```

- `Provider` selects a registered provider by name: `Fake` or `OpenAI`.
- `OutputDirectory` is where generated images are saved.
- Keep the API key out of source control. Use user secrets:

```
cd src/ImageLab.Api
dotnet user-secrets set "ImageLab:OpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "ImageLab:OpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
```

Then set `"Provider": "OpenAI"` to switch providers.

## Pipeline

Stages are added with `ImageGeneratorBuilder.Use(...)`. The first stage added is the outermost, so it sees each call first:

1. `LoggingImageGenerator`: logs start, completion with duration, and failures. Prompt text is logged only at Debug level.
2. `TelemetryImageGenerator`: emits an `image.generate` activity and the metrics `imagelab.image.requests`, `imagelab.image.failures`, and `imagelab.image.duration`. Tags hold the provider name and prompt length, never the prompt.
3. `FileSavingImageGenerator`: writes each returned image to `OutputDirectory` and records the path in `AdditionalProperties["ImageLab.FilePath"]`.
4. The provider's `IImageGenerator`.

There is no caching. Users rarely repeat a prompt exactly, so a cache would add complexity without a benefit.

## Adding a provider

1. Create a project that references `ImageLab.Core`.
2. Implement `IImageGeneratorProvider` (`Name` plus `Create(IServiceProvider)`).
3. Register it in `Program.cs`, for example with an `AddXxxImageProvider` extension method.
4. Set `ImageLab:Provider` to the new name.

Nothing in `ImageLab.Core` needs to change.

## Notes

- `IImageGenerator` is experimental. The `MEAI001` warning is suppressed in `Directory.Build.props`.
- Telemetry goes to the console in development. For production, replace the console exporters with OTLP or your monitoring service.
- The fake provider returns the same image every time, so file-saving tests write identical bytes under unique names.
