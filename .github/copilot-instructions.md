# ArcGIS Maps SDK for .NET Toolkit

## Build, test, and lint commands

- CI builds the main toolkit SDK project heads with .NET:

```powershell
dotnet build src\Toolkit\Toolkit.WPF\Esri.ArcGISRuntime.Toolkit.WPF.csproj -c Release
dotnet build src\Toolkit\Toolkit.WinUI\Esri.ArcGISRuntime.Toolkit.WinUI.csproj -c Release
dotnet build src\Toolkit\Toolkit.Maui\Esri.ArcGISRuntime.Toolkit.Maui.csproj -c Release
```

Samples likewise builds in a similar fashion:
```powershell
dotnet build src\Samples\Toolkit.SampleApp.WPF\Toolkit.SampleApp.WPF.csproj -c Release
dotnet build src\Samples\Toolkit.SampleApp.WinUI\Toolkit.SampleApp.WinUI.csproj-c Release
dotnet build src\Samples\Toolkit.SampleApp.Maui\Toolkit.SampleApp.Maui.csproj -c Release
```

In addition for testing purposes, the sample apps can also be built with generated Toolkit NuGet packages by setting the `UseNugetPackage` parameter to the package version. For example:
```powershell
dotnet build src\Samples\Toolkit.SampleApp.WPF\Toolkit.SampleApp.WPF.csproj -c Release -p:UseNugetPackage=300.0.0-testbuild
dotnet build src\Samples\Toolkit.SampleApp.WinUI\Toolkit.SampleApp.WinUI.csproj-c Release -p:UseNugetPackage=300.0.0-testbuild
dotnet build src\Samples\Toolkit.SampleApp.Maui\Toolkit.SampleApp.Maui.csproj -c Release -p:UseNugetPackage=300.0.0-testbuild
```

- There is no separate lint script. StyleCop/code analysis is wired into project builds through `src\Esri.ArcGISRuntime.Toolkit.ruleset` and `src\stylecop.json`, so building the relevant project is the lint path.

## High-level architecture

- The primary shipping libraries are platform-specific heads:
  - `src\Toolkit\Toolkit.WPF\Esri.ArcGISRuntime.Toolkit.WPF.csproj`
  - `src\Toolkit\Toolkit.WinUI\Esri.ArcGISRuntime.Toolkit.WinUI.csproj`
  - `src\Toolkit\Toolkit.Maui\Esri.ArcGISRuntime.Toolkit.Maui.csproj`
- Most implementation lives in shared projects under `src\Toolkit\Toolkit\`. The shared project file is `Esri.ArcGISRuntime.Toolkit.Shared.shproj`, imported by all three platform heads.
- `src\Toolkit\Toolkit.UI.Controls\` is a second shared project used by WPF and WinUI only. It contains older Windows-only shared control code; prefer putting new cross-platform logic in `src\Toolkit\Toolkit\` unless the work is genuinely Windows-specific.
- The platform head projects mainly provide packaging, platform assets, and control templates while importing shared code through `.projitems`.
- `src\Analyzers\` contains the MAUI Roslyn analyzer and code fix projects. The MAUI toolkit package packs those analyzers so MAUI-specific setup rules ship with the library.
- `src\Samples\` contains the WPF, WinUI, and MAUI sample apps. These are the main functional/manual test beds for toolkit controls.
- `src\ARToolkit`, `src\ARToolkit.Maui`, `src\ARSamples`, and `src\ProjectTemplates` are present but treated as deprecated/secondary unless the task explicitly targets AR functionality or templates.

## Key conventions

- Cross-platform features are usually split into shared code plus platform-specific companions. Common patterns are:
  - shared implementation in `src\Toolkit\Toolkit\...`
  - `.Windows.cs` partials for WPF/WinUI-specific behavior
  - `.MAUI.cs` partials for MAUI-specific behavior or MAUI specific project.
- Platform conditionals are already established and should be reused instead of creating new ad hoc switches: `WPF`, `WINDOWS_XAML`, `MAUI`, `__IOS__`, `__ANDROID__`, and `MACCATALYST`.
- The sample apps share code across platforms where convenient (for example, some sample view models are linked from the WPF sample into MAUI and WinUI). Check linked includes before duplicating sample code.
- Do not suppress build warnings, but address them instead.

## Tests
Unit Tests for the toolkit lives in the Test folder.

```powershell
dotnet test --project src\Tests\Toolkit.Tests\Toolkit.Tests.csproj
```


- Run a single test with an MSTest filter:

```powershell
dotnet test --project src\Tests\Toolkit.Tests\Toolkit.Tests.csproj --filter "FullyQualifiedName~Toolkit.Tests.FileDownloadTaskTests.ResumeFromJsonRestoresPartialDownloadAndCompletes"
```


There are additional automated UI Tests for all platforms relying on Appium in src\Tests\UITests\ with documentation in src\Tests\UITests\README.md


## Analyzers

Automated tests for analyzers currently live under the MAUI analyzer test project:

```powershell
dotnet test --project src\Analyzers\Toolkit.Maui.Analyzers.UnitTests\Esri.ArcGISRuntime.Toolkit.Maui.Analyzers.UnitTests.csproj
```

- Run a single test with an MSTest filter:

```powershell
dotnet test --project src\Analyzers\Toolkit.Maui.Analyzers.UnitTests\Esri.ArcGISRuntime.Toolkit.Maui.Analyzers.UnitTests.csproj --filter "FullyQualifiedName~UseToolkitInitializationAnalyzerTests.DetectMissingUseCall"
```
