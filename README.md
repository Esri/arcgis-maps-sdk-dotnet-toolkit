# arcgis-toolkit-dotnet

This project contains source code for controls and utilities you can use with the [ArcGIS Runtime SDK for .NET](http://links.esri.com/dotnetsdk).   Build from source code available in this repo (see instructions below) or install the latest stable release from NuGet: [https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit](https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit)   

## Features

- Compass - Shows a compass direction when the map is rotated. Auto-hides when the map points north up.
- Legend - Displays a legend for a single layer in your map (and optionally for its sub layers).
- ScaleLine - Displays current scale reference.
- ChallengeHandler - (WPF only) Displays a UI dialog to enter or select credentials to use when accessing secure ArcGIS resources, as well as helper classes for storing credentials in Windows' credentials cache. 
- SymbolDisplay - Renders a symbol in a control.
- TableOfContents (WPF)- Creates a tree-view of the entire map document. Optionally displays legend information for the layers as well. 

See the [wiki](https://github.com/Esri/arcgis-toolkit-dotnet/wiki) for more details.

## Instructions 

1. Fork and then clone the repo or download the .zip file.
2. The Toolkit requires the ArcGIS Runtime SDK for .NET.  Confirm that your system meets the requirements for using the ArcGIS Runtime SDK for .NET with [WPF](http://developers.arcgis.com/net/desktop/guide/system-requirements.htm), and/or [Windows Universal](https://developers.arcgis.com/net/latest/uwp/guide/system-requirements.htm).  
 * Note that [ArcGIS Runtime SDK for .NET](http://esriurl.com/dotnetsdk) is referenced by using a Nuget package. It is automatically downloaded when the solution is built for the first time.
3. To include Toolkit source in your projects:
 *  In Visual Studio, add the ArcGIS Runtime Toolkit project to your solution. 
    - WPF (src\Esri.ArcGISRuntime.Toolkit\WPF\Esri.ArcGISRuntime.Toolkit.WPF.csproj)
    - Windows Universal	(\src\Esri.ArcGISRuntime.Toolkit\UWP\Esri.ArcGISRuntime.Toolkit.UWP.csproj)
 *  For other projects in the solution, add a reference to the ArcGIS Runtime Toolkit project.

## Requirements for Compiling
 - Visual Studio 2017 Update 3+
 - .NET Core 2.0 SDK
 - Xamarin iOS and Android SDKs

## Requirements

* Supported system configurations for: 
  * [Windows Desktop](https://developers.arcgis.com/net/latest/wpf/guide/system-requirements.htm)
  * [Windows Universal](https://developers.arcgis.com/net/latest/uwp/guide/system-requirements.htm)
  * [Xamarin Android](https://developers.arcgis.com/net/latest/android/guide/system-requirements.htm)
  * [Xamarin iOS](https://developers.arcgis.com/net/latest/ios/guide/system-requirements.htm)
  * [Xamarin.Forms](https://developers.arcgis.com/net/latest/forms/guide/system-requirements.htm)

### Referencing the toolkit in an app:
 
 1.  Create a local nuget-repository and point it to the \Output\NuGet\Release folder. Then add the nuget package using the standard Nuget reference manager in Visual Studio.
 2. It is also possible to point a nuget repository to the latest build. It is automatically updated after each pull-request gets merged: https://ci.appveyor.com/nuget/arcgis-toolkit-dotnet 

## Resources

* [ArcGIS Runtime SDK for .NET](http://esriurl/dotnetsdk)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Anyone and everyone is welcome to [contribute](CONTRIBUTING.md).

### v10.2.7
If you're looking for the earlier Toolkit released with v10.2.7, you can find it in the 10.2.7 tag:
https://github.com/Esri/arcgis-toolkit-dotnet/tree/v10.2.7

## Licensing
Copyright © 2014-2016 Esri.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt](/license.txt) file.
