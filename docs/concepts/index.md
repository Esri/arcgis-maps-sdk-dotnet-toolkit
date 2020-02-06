# ArcGIS Runtime toolkit for .NET

The ArcGIS Runtime SDK for .NET toolkit contains controls and utilities you can use with [ArcGIS Runtime SDK for .NET](http://links.esri.com/dotnetsdk).

There are two ways to use toolkit and AR toolkit in your apps:

1. [Install from Nuget](../installtoolkit.md) - the fastest way to get toolkit into your app
2. [Build from source](../buildingtoolkit.md) - do this if you want to customize toolkit
   
## Features

> See [the doc](../controls.md) for a full list of controls with screenshots

- [ARSceneView](../ar.md): Part of the AR Toolkit, enables integration of GIS content and ARKit/ARCore.
- [Bookmarks](../bookmarks-view.md): Shows bookmarks, from a map, scene, or a list; navigates the associated MapView/SceneView when a bookmark is selected.
- **Compass**: Shows a compass direction when the map is rotated. Auto-hides when the map points north up.
- **FeatureDataField**: Displays and optionally allows editing of a single field attribute of a feature.
- **Legend**: Displays a legend for a single layer in your map (and optionally for its sub layers).
- **MeasureToolbar**: Allows measurement of distances and areas on the map view.
- **PopupViewer**: Display details and media, edit attributes, geometry and related records, and manage the attachments of features and graphics (popups are defined in the popup property of features and graphics).
- **ScaleLine**: Displays current scale reference.
- **SymbolDisplay**: Renders a symbol in a control.
- **TimeSlider**: Allows interactively defining a temporal range (i.e. time extent) and animating time moving forward or backward.  Can be used to manipulate the time extent in a MapView or SceneView.

### Features in Preview

- **ChallengeHandler**: Displays SignInForm when accessing secure ArcGIS resources, as well as helper classes for storing credentials in Windows' credentials cache.
- **SignInForm**: Displays a UI dialog to enter or select credentials to use when accessing secure ArcGIS resources.