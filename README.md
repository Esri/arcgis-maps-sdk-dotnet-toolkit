# ArcGIS Runtime SDK for .NET - Toolkit

The ArcGIS Runtime SDK for .NET Toolkit contains controls and utilities you can use with [ArcGIS Runtime SDK for .NET](http://links.esri.com/dotnetsdk).

You can use the Toolkit in your projects by:
1. Building the source code available in this repo (see instructions below)
2. Installing the latest stable release from NuGet: [https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit](https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit)   

## Features

- Compass - Shows a compass direction when the map is rotated. Auto-hides when the map points north up.
- Legend - Displays a legend for a single layer in your map (and optionally for its sub layers).
- ScaleLine - Displays current scale reference.
- ChallengeHandler - (WPF only) Displays a UI dialog to enter or select credentials to use when accessing secure ArcGIS resources, as well as helper classes for storing credentials in Windows' credentials cache. 
- SymbolDisplay - Renders a symbol in a control.
- TableOfContents (WPF)- Creates a tree-view of the entire map document. Optionally displays legend information for the layers as well. 

## Instructions for Building

1. Fork and then clone the repo or download the .zip file.
2. The Toolkit requires ArcGIS Runtime SDK for .NET. 
 * Confirm that your system meets the requirements for using ArcGIS Runtime SDK for .NET with:
   - [WPF](http://developers.arcgis.com/net/desktop/guide/system-requirements.htm)
   - [UWP](https://developers.arcgis.com/net/latest/uwp/guide/system-requirements.htm)
   - [Xamarin.Android](https://developers.arcgis.com/net/latest/android/guide/system-requirements.htm)
   - [Xamarin.iOS](https://developers.arcgis.com/net/latest/ios/guide/system-requirements.htm)
   - [Xamarin.Forms (Android, iOS, and UWP)](https://developers.arcgis.com/net/latest/forms/guide/system-requirements.htm)
 * Note that the Toolkit references [ArcGIS Runtime SDK for .NET](http://esriurl.com/dotnetsdk) by Nuget package. The package is automatically downloaded when you build the solution for the first time.
3. Confirm that your system meets the requirements for compiling the Toolkit
    - Visual Studio 2017 Update 3+
    - .NET Core 2.0 SDK
    - Xamarin.iOS and Xamarin.Android SDKs
4. Include or reference the Toolkit in your projects:
    - a. Include the appropriate platform Projects in your Solution.
     - WPF (src\Esri.ArcGISRuntime.Toolkit\WPF\Esri.ArcGISRuntime.Toolkit.WPF.csproj)
     - UWP (\src\Esri.ArcGISRuntime.Toolkit\UWP\Esri.ArcGISRuntime.Toolkit.UWP.csproj)
     - Xamarin.Android (\src\Esri.ArcGISRuntime.Toolkit\Android\Esri.ArcGISRuntime.Toolkit.Android.csproj)
     - Xamarin.iOS (\src\Esri.ArcGISRuntime.Toolkit\iOS\Esri.ArcGISRuntime.Toolkit.iOS.csproj)
     - Xamarin.Forms (\src\Esri.ArcGISRuntime.Toolkit\XamarinForms\Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.csproj)
    - b. Build the Toolkit and reference the NuGet package you built.
     - Building each Toolkit project automatically creates the NuGet package for each platform in the project Output folder.
     - Create a local nuget source and set it to the \Output\NuGet\Release folder.
     - Add the nuget package using the standard Nuget package manager dialog in Visual Studio.
     * It is also possible to create a nuget source pointing to the latest build (automatically updated after each pull-request gets merged): `https://ci.appveyor.com/nuget/arcgis-toolkit-dotnet` 

## System Requirements

* Requirements for development and deployment: 
  * [Windows Desktop](https://developers.arcgis.com/net/latest/wpf/guide/system-requirements.htm)
  * [Universal Windows Platform (UWP)](https://developers.arcgis.com/net/latest/uwp/guide/system-requirements.htm)
  * [Xamarin.Android](https://developers.arcgis.com/net/latest/android/guide/system-requirements.htm)
  * [Xamarin.iOS](https://developers.arcgis.com/net/latest/ios/guide/system-requirements.htm)
  * [Xamarin.Forms](https://developers.arcgis.com/net/latest/forms/guide/system-requirements.htm)

## Resources

* [ArcGIS Runtime SDK for .NET](http://esriurl/dotnetsdk)

## Issues

Find a bug or want to request a new feature?  Please let us know by [submitting an issue](https://github.com/Esri/arcgis-toolkit-dotnet/issues/new).

## Contributing

Anyone and everyone is welcome to [contribute](CONTRIBUTING.md).

## v10.2.7
Looking for the Toolkit for ArcGIS Runtime SDK for .NET v10.2.7?

Go to the 10.2.7 tag: https://github.com/Esri/arcgis-toolkit-dotnet/tree/v10.2.7

## Licensing
Copyright © 2014-2017 Esri.

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
