# ArcGIS Runtime SDK for .NET - Toolkit

The ArcGIS Runtime SDK for .NET Toolkit contains controls and utilities you can use with [ArcGIS Runtime SDK for .NET](http://links.esri.com/dotnetsdk).

You can use the Toolkit in your projects by:

1. Building the source code available in this repo ([see instructions](docs/buildingtoolkit.md))
2. Installing the latest stable release or pre-release from NuGet:
   - [Esri.ArcGISRuntime.Toolkit](https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit)
	- [Esri.ArcGISRuntime.Toolkit.Xamarin.Forms](https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit.Xamarin.Forms)
	- [Esri.ArcGISRuntime.ARToolkit](https://www.nuget.org/packages/Esri.ArcGISRuntime.ARToolkit)
	- [Esri.ArcGISRuntime.ARToolkit.Forms](https://www.nuget.org/packages/Esri.ArcGISRuntime.ARToolkit.Forms)
   
## Features

- ARSceneView: Part of the AR Toolkit, enables integration of GIS content and ARKit/ARCore.
- Bookmarks: Shows bookmarks, from a map, scene, or a list; navigates the associated MapView/SceneView when a bookmark is selected.
- Compass: Shows a compass direction when the map is rotated. Auto-hides when the map points north up.
- FeatureDataField: Displays and optionally allows editing of a single field attribute of a feature.
- Legend: Displays a legend for a single layer in your map (and optionally for its sub layers).
- MeasureToolbar: Allows measurement of distances and areas on the map view.
- PopupViewer: Display details and media, edit attributes, geometry and related records, and manage the attachments of features and graphics (popups are defined in the popup property of features and graphics).
- ScaleLine: Displays current scale reference.
- SymbolDisplay: Renders a symbol in a control.
- TimeSlider: Allows interactively defining a temporal range (i.e. time extent) and animating time moving forward or backward.  Can be used to manipulate the time extent in a MapView or SceneView.

### Features in Preview

- ChallengeHandler: Displays SignInForm when accessing secure ArcGIS resources, as well as helper classes for storing credentials in Windows' credentials cache.
- SignInForm: Displays a UI dialog to enter or select credentials to use when accessing secure ArcGIS resources.

## Resources

- [Documentation](docs/index.md)
- [List of controls](docs/controls.md)
- [Building the SDK](docs/buildingtoolkit.md)
- [System Requirements](docs/requirements.md)
- [ArcGIS Runtime SDK for .NET](http://esriurl/dotnetsdk)

## Issues

Find a bug or want to request a new feature?  Please let us know by [submitting an issue](https://github.com/Esri/arcgis-toolkit-dotnet/issues/new).

## Contributing

Anyone and everyone is welcome to [contribute](CONTRIBUTING.md).

## Licensing

Copyright © 2014-2020 Esri.

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
