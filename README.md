[![Link: ArcGIS Developers Home](https://img.shields.io/badge/ArcGIS%20Developers%20Home-633b9b?style=flat-square)](https://developers.arcgis.com)
[![Link: Documentation](https://img.shields.io/badge/Documentation-633b9b?style=flat-square)](https://developers.arcgis.com/net/)
[![Link: Samples](https://img.shields.io/badge/Samples-633b9b?style=flat-square)](https://developers.arcgis.com/net/wpf/sample-code/)
[![Link: Templates](https://img.shields.io/badge/Templates-633b9b?style=flat-square&logo=visualstudio&labelColor=gray)](https://www.nuget.org/packages?q=ArcGIS+Runtime+Templates)
[![Link: NuGet](https://img.shields.io/badge/NuGet-633b9b?style=flat-square&logo=nuget&labelColor=gray)](https://www.nuget.org/profiles/Esri_Inc)
[![Link: Early Adopter Community](https://img.shields.io/badge/🙋-Get%20help%20in%20Early%20Adopter%20Community-633b9b?style=flat-square)](https://esriurl.com/agsrt200beta)

# ArcGIS Runtime SDK for .NET Toolkit

The ArcGIS Runtime SDK for .NET Toolkit contains controls and components to accelerate your development with [ArcGIS Runtime SDK for .NET](https://developers.arcgis.com/net/).

> **IMPORTANT** This branch showcases changes we will be releasing in our upcoming 200.0 release. To report any toolkit related bug or question please create an issue in this repo. 
For any other feedback please join the [Esri Early Adopter community](https://esriurl.com/agsrt200beta).

> **Note**: At version 200.0, Toolkit assemblies are now built individually for each platform. Please replace any existing `Esri.ArcGISRuntime.Toolkit` NuGet reference with Toolkit NuGet package specific to that platform. For example if you have a WPF application using Toolkit, you will need to uninstall `Esri.ArcGISRuntime.Toolkit` NuGet package and add `Esri.ArcGISRuntime.Toolkit.WPF` NuGet package.

# Documentation

[![Link: Toolkit Guide](https://img.shields.io/badge/%F0%9F%93%84-Toolkit%20Guide-633b9b?style=flat-square)](docs/controls.md)

## Features

| Component | Screenshot | Description | Availability |
|-----------|------------|-------------|--------------|
|[BasemapGallery](docs/basemap-gallery.md) | <img width="150" title="Basemap Gallery" src="https://user-images.githubusercontent.com/29742178/124198151-f2dc6380-da84-11eb-8e78-4e705d14c33d.png" />| Display a list or grid of Basemaps. | WinUI, MAUI, UWP, WPF |
|[BookmarksView](docs/bookmarks-view.md) | ![image](https://user-images.githubusercontent.com/29742178/150397137-28029b87-5384-41b1-aabf-98260885152d.png) | Show and navigate to bookmarks from a map or a custom list. | WinUI, MAUI, UWP, WPF |
| [Compass](docs/compass.md) | ![compass](https://user-images.githubusercontent.com/1378165/73389839-d9c8f500-4289-11ea-923c-18232489b3e0.png) | Show a compass direction when the map or scene is rotated. | WinUI, MAUI, UWP, WPF |
| [FeatureDataField](docs/feature-data-field.md)   | ![FeatureDataField](https://user-images.githubusercontent.com/1378165/73389879-ebaa9800-4289-11ea-8e4e-de153a6a371a.png) | Display and optionally allow editing of a single field attribute of a feature. | UWP, WPF |
|[FloorFilter](docs/floor-filter.md) | ![image](https://user-images.githubusercontent.com/29742178/158746908-71a39e28-596f-44b6-9230-e2a04bdaeb9e.png) | Browse floor-aware maps and scenes and filter the view to show levels in a facility. | WinUI, MAUI, UWP, WPF |
| [Legend](docs/legend.md)   | <img src="https://user-images.githubusercontent.com/1378165/73389924-011fc200-428a-11ea-91bf-4ea1c2bf6683.png" width="105" title="Legend" />| Display a legend for a single layer in a map, optionally including sublayers. | WinUI, MAUI, UWP, WPF |
| [MeasureToolbar](docs/measure-toolbar.md)  | ![MeasureToolbar](https://user-images.githubusercontent.com/1378165/73389958-0f6dde00-428a-11ea-8c78-7192d49ea605.png) | Measure distances, areas, and features in a map view. | UWP, WPF |
|[OverviewMap](docs/overview-map.md) | <img src="https://user-images.githubusercontent.com/29742178/121975740-34f07000-cd37-11eb-9162-462925cb3fe7.png" width="150" title="Overview Map" /> | Display an interactive inset map for a map or scene. | WinUI, MAUI, UWP, WPF |
| [ScaleLine](docs/scale-line.md)   | ![ScaleLine](https://user-images.githubusercontent.com/1378165/73390077-3debb900-428a-11ea-8b2f-dfd4914a637e.png) | Display the current scale reference for a map. | WinUI, MAUI, UWP, WPF |
|[SearchView](docs/search-view.md) | <img title="Search View" width="150" src="https://user-images.githubusercontent.com/29742178/142301018-4bbeb0f2-3021-49a7-b5ec-f642c5700bd0.png" /> | Search using one or more locators, with support for suggestions, automatic zooming, and custom search sources. | WinUI, MAUI, UWP, WPF |
| [SymbolDisplay](docs/symbol-display.md)   | ![SymbolDisplay](https://user-images.githubusercontent.com/1378165/73390051-31676080-428a-11ea-9feb-afb5d2aa6385.png) | Render a symbol in a control. | WinUI, MAUI, UWP, WPF |
|[TimeSlider](docs/time-slider.md) | ![TimeSlider on UWP](https://user-images.githubusercontent.com/29742178/147712751-6d6db182-3e72-4dfc-ba23-3fbe97b1f934.png) | Interactively manipulate or animate the time extent for a map or scene. | WinUI, UWP, WPF |
|[UtilityNetworkTraceTool](docs/un-trace.md) | ![Utility Network Trace Tool on WPF](https://user-images.githubusercontent.com/29742178/173907265-73cd3a39-c836-433e-baf0-4c60f921ba86.png) | Use named trace configurations defined in a web map to perform connected trace operations and compare results. | WPF, UWP, WinUI, MAUI |

## Get started

> **Note**: At version 200.0, Toolkit assemblies are now built individually for each platform. Please replace any existing `Esri.ArcGISRuntime.Toolkit` NuGet reference with Toolkit NuGet package specific to that platform. For example if you have a WPF application using Toolkit, you will need to uninstall `Esri.ArcGISRuntime.Toolkit` NuGet package and add `Esri.ArcGISRuntime.Toolkit.WPF` NuGet package.

The simplest way to get started is to add the platform-specific NuGet package(s) to your projects:

- WPF: `Esri.ArcGISRuntime.Toolkit.WPF`
- UWP: `Esri.ArcGISRuntime.Toolkit.WinUI`
- UWP: `Esri.ArcGISRuntime.Toolkit.UWP`
- MAUI: `Esri.ArcGISRuntime.Toolkit.Maui`

## Customize

The ArcGIS Runtime SDK Toolkit is provided as an open-source project so you can customize it for your requirements. [See the docs](https://esri.github.io/arcgis-toolkit-dotnet/buildingtoolkit.html) for instructions on building the Toolkit yourself.

## Compatibility

Nuget packages for Toolkit are tested and published in sync with the `Esri.ArcGISRuntime.*` packages. Toolkit is only supported when used with the matching Runtime API version.

## Contribute

Anyone and everyone is welcome to [contribute](CONTRIBUTING.md).

Toolkit is provided as an open source project to enable you to customize for your requirements. [See the docs](https://esri.github.io/arcgis-toolkit-dotnet/buildingtoolkit.html) for instructions on building the Toolkit yourself.

Find a bug or want to request a new feature? Please let us know by [submitting an issue](https://github.com/Esri/arcgis-toolkit-dotnet/issues/new).

## License

Copyright © 2014-2022 Esri.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt](/license.txt) file.
