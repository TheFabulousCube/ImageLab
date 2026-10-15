---
name: "imagelab-scaffold"
description: "Scaffold or rebuild the ImageLab .NET 8 sample: an IImageGenerator pipeline with logging, telemetry, file saving, swappable providers (Fake, Azure OpenAI), and a minimal API. Use when asked to create, recreate, or extend ImageLab."
---

# ImageLab scaffold

Use this skill to create the ImageLab solution from scratch in the current directory, or to extend it. The design is in the project README (`README.md` in the solution root once created). Follow the steps in order and build after each group of files.

## Goals

- Generate images through Microsoft.Extensions.AI `IImageGenerator`.
- Keep `ImageLab.Core` free of provider SDKs.
- Select the provider by name from configuration.
- Every call goes through logging, telemetry, and file-saving stages.
- No caching. Users rarely repeat a prompt exactly.

## Steps

1. Check prerequisites: `dotnet --version` should report 8.x. Restore needs access to api.nuget.org. If restore is blocked, stop and tell the user.
2. Create the solution and projects:
   - `dotnet new sln -n ImageLab`
   - `dotnet new classlib -n ImageLab.Core -o src/ImageLab.Core`
   - `dotnet new classlib -n ImageLab.Providers.Fake -o src/ImageLab.Providers.Fake`
   - `dotnet new classlib -n ImageLab.Providers.OpenAI -o src/ImageLab.Providers.OpenAI`
   - `dotnet new web -n ImageLab.Api -o src/ImageLab.Api`
   - `dotnet new xunit -n ImageLab.Core.Tests -o tests/ImageLab.Core.Tests`
   - Add all projects to the solution. Delete template files such as Class1.cs and UnitTest1.cs.
3. Add `Directory.Build.props` at the root with `Nullable` enabled, `ImplicitUsings` enabled, and `<NoWarn>$(NoWarn);MEAI001</NoWarn>`. MEAI001 is the experimental diagnostic for IImageGenerator.
4. Package versions to use:
   - `Microsoft.Extensions.AI` 10.0.0-preview.1.25560.10 in Core
   - `Microsoft.Extensions.AI.OpenAI` 10.0.0-preview.1.25560.10 and `Azure.AI.OpenAI` 2.1.0 in Providers.OpenAI
   - `OpenTelemetry.Extensions.Hosting` and `OpenTelemetry.Exporter.Console` 1.9.0 in Api
   - Use Microsoft.Extensions.* 8.0.x for DI, Logging, Options, and Configuration packages. Align versions if NuGet reports conflicts.
   - Test project: xunit 2.9.2, xunit.runner.visualstudio 2.8.2, Microsoft.NET.Test.Sdk 17.11.1
5. Write Core:
   - `Pipeline/DelegatingImageGenerator.cs`: abstract base implementing IImageGenerator. It forwards GenerateAsync, GetService, and Dispose to an inner generator.
   - `Pipeline/LoggingImageGenerator.cs`: ILogger. Log start with prompt length, completion with count and elapsed ms, and failures with the exception. Log the prompt only at Debug.
   - `Telemetry/ImageLabTelemetry.cs`: static ActivitySource and Meter, both named "ImageLab". Counters `imagelab.image.requests` and `imagelab.image.failures`. Histogram `imagelab.image.duration` in seconds.
   - `Pipeline/TelemetryImageGenerator.cs`: starts an `image.generate` activity. Tags are `imagelab.provider`, `imagelab.prompt.length`, and `imagelab.output.count`. On failure, set status Error and tag `error.type`. Never tag the prompt text. Record duration in a finally block.
   - `Pipeline/FileSavingImageGenerator.cs`: for each DataContent with data, write a file named `yyyyMMdd-HHmmss-<guid>.<ext>` to the output folder. Map image/png, image/jpeg, image/webp, and image/gif to extensions, and use .bin otherwise. Store the full path in `AdditionalProperties["ImageLab.FilePath"]`. Pass URI content through unchanged. Define the key as the constant `FilePathKey`.
   - `Options/ImageLabOptions.cs`: `Provider` (default "Fake") and `OutputDirectory` (default "generated-images").
   - `Providers/IImageGeneratorProvider.cs`: `string Name { get; }` and `IImageGenerator Create(IServiceProvider services)`.
   - `ImageLabServiceCollectionExtensions.cs`: `AddImageLab(IServiceCollection, IConfiguration)` binds options and registers a singleton IImageGenerator factory. The factory finds the provider by name, case-insensitively. If none matches, it throws an InvalidOperationException that lists the registered names. It then builds: `new ImageGeneratorBuilder(p => provider.Create(p)).Use(logging).Use(telemetry).Use(fileSaving).Build(services)`. The first Use is outermost.
6. Write Providers.Fake:
   - `FakeImageGenerator` returns a fixed 1x1 PNG as `DataContent` (media type image/png). The base64 is in the project README.
   - `FakeImageGeneratorProvider` has Name "Fake".
   - Extension `AddFakeImageProvider(this IServiceCollection)` registers it as a singleton IImageGeneratorProvider.
7. Write Providers.OpenAI:
   - `OpenAIImageOptions` with `Endpoint`, `ApiKey`, and `Model` (default "gpt-image-1").
   - `OpenAIImageGeneratorProvider` (Name "OpenAI"). Validate Endpoint and ApiKey, then create `new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))` and return `client.GetImageClient(model).AsIImageGenerator()`. This call is taken from the Microsoft quickstart but was not compiled. If it fails, check the Azure.AI.OpenAI and Microsoft.Extensions.AI.OpenAI APIs for the installed versions.
   - Extension `AddOpenAIImageProvider(this IServiceCollection, IConfiguration section)` binds options and registers the provider.
8. Write Api:
   - `Program.cs`: `AddImageLab(config.GetSection("ImageLab"))`, `AddFakeImageProvider()`, `AddOpenAIImageProvider(config.GetSection("ImageLab:OpenAI"))`. Add OpenTelemetry with `AddSource(ImageLabTelemetry.SourceName)`, `AddMeter(ImageLabTelemetry.MeterName)`, and console exporters. Then `MapImageEndpoints()`.
   - `Endpoints/ImageEndpoints.cs`: `POST /generate-image` with body `{ "prompt": string }`. Return 400 for an empty prompt or one over 1000 characters. Return `{ "images": [ { "mediaType", "filePath" } ] }`.
   - `appsettings.json`: Logging with ImageLab at Debug. The `ImageLab` section has Provider "Fake", OutputDirectory, and an `OpenAI` subsection with empty Endpoint and ApiKey. Never commit a real key.
9. Write tests:
   - Pipeline: configure in memory with Provider Fake and a temp OutputDirectory. Generate once. Assert one DataContent, that the file exists, and that the path is inside the output directory. Delete the directory in finally.
   - Unknown provider: assert InvalidOperationException whose message contains the bad name and "Fake".
   - Telemetry: use an ActivityListener on "ImageLab". Assert one activity named `image.generate` with the provider and output-count tags. Add a failure case that expects Error status.
10. Run `dotnet build` and `dotnet test`. Fix compile errors, which are most likely in package versions or the Azure call. Do not change the design to work around them.
11. Run `dotnet run --project src/ImageLab.Api`. Call the endpoint with the Fake provider and confirm a file appears in generated-images.

## Guardrails

- Do not put API keys in any committed file. Use user secrets.
- Do not add caching.
- Do not log prompt text above Debug, and do not put it in telemetry tags.
- Keep ImageLab.Core free of provider package references.
- If a git repo is initialized, do not commit until the user asks.