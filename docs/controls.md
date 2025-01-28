# List of controls

### BasemapGallery

BasemapGallery displays a collection of basemaps from ArcGIS Online, a user-defined portal, or a custom collection. When a basemap is selected from the gallery, the basemap used in the connected GeoModel is replaced.

![BasemapGallery List View](https://user-images.githubusercontent.com/29742178/124198151-f2dc6380-da84-11eb-8e78-4e705d14c33d.png)

[Documentation](basemap-gallery.md)

### Bookmarks
Shows bookmarks, from a map, scene, or a list; navigates the associated MapView/SceneView when a bookmark is selected.

[Documentation](bookmarks-view.md)


### Compass
Shows a compass direction when the map is rotated. Auto-hides when the map points north up.

![compass](https://user-images.githubusercontent.com/1378165/73389839-d9c8f500-4289-11ea-923c-18232489b3e0.png)


### FeatureDataField

Displays and optionally allows editing of a single field attribute of a feature.

![FeatureDataField](https://user-images.githubusercontent.com/1378165/73389879-ebaa9800-4289-11ea-8e4e-de153a6a371a.png)

### FloorFilter

Browse floor-aware maps and scenes and filter the view to show levels in a facility.

![FloorFilter](https://user-images.githubusercontent.com/29742178/158746908-71a39e28-596f-44b6-9230-e2a04bdaeb9e.png)

### GeoViewController

A helper class for enabling easy adoption of [Model-View-ViewModel (MVVM)](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm) patterns in ArcGIS Maps SDK for .NET applications.

### Legend

Displays a legend for a map or scene view.

![Legend](https://user-images.githubusercontent.com/1378165/73389924-011fc200-428a-11ea-91bf-4ea1c2bf6683.png)


### MeasureToolbar

Allows measurement of distances and areas on the map view.

![MeasureToolbar](https://user-images.githubusercontent.com/1378165/73389958-0f6dde00-428a-11ea-8c78-7192d49ea605.png)

### OverviewMap

Displays an interactive inset map for a map or scene.

![OverviewMap](https://user-images.githubusercontent.com/29742178/121975740-34f07000-cd37-11eb-9162-462925cb3fe7.png)

[Documentation](overview-map.md)

### PopupViewer

Display details and media, edit attributes, geometry and related records, and manage the attachments of features and graphics (popups are defined in the popup property of features and graphics).

![PopupViewer](https://user-images.githubusercontent.com/1378165/73389991-1e549080-428a-11ea-81f3-b2f9c29f61ad.png)


### ScaleLine

Displays current scale reference.

![ScaleLine](https://user-images.githubusercontent.com/1378165/73390077-3debb900-428a-11ea-8b2f-dfd4914a637e.png)

### SearchView

Enables searching using one or more locators, with support for suggestions, automatic zooming, and custom search sources.

![SearchView](https://user-images.githubusercontent.com/29742178/142301018-4bbeb0f2-3021-49a7-b5ec-f642c5700bd0.png)

### SymbolDisplay

Renders a symbol in a control.

![SymbolDisplay](https://user-images.githubusercontent.com/1378165/73390051-31676080-428a-11ea-9feb-afb5d2aa6385.png)


### TimeSlider

Allows interactively defining a temporal range (i.e. time extent) and animating time moving forward or backward.  Can be used to manipulate the time extent in a MapView or SceneView.

![TimeSlider on UWP](https://user-images.githubusercontent.com/29742178/147712751-6d6db182-3e72-4dfc-ba23-3fbe97b1f934.png)

### UtilityNetworkTraceTool

Use named trace configurations defined in a web map to perform connected trace operations and compare results.

![Utility Network Trace Tool on WPF](https://user-images.githubusercontent.com/29742178/173907265-73cd3a39-c836-433e-baf0-4c60f921ba86.png) 

## Feature availability by platform/API

|Component |UWP and WinUI |WPF  |MAUI | WinUI AoT Compatible|
|---|---|---|---|---|
|[BasemapGallery](basemap-gallery.md) | ✔ | ✔ |  ✔ | ✔ |
|[BookmarksView](bookmarks-view.md)   | ✔ | ✔ | ✔ | ❌ |
|[Compass](compass.md)   | ✔ | ✔ | ✔ | ✔ |
|[FeatureDataField](feature-data-field.md)   | ✔ | ✔ | ❌ | ❌ |
|[FloorFilter](floor-filter.md) | ✔  | ✔ | ✔ | ❌ |
|[GeoViewController](geoviewcontroller.md) | ✔  | ✔ | ✔ | ✔ |
|[Legend](legend.md)   | ✔ | ✔ | ✔ | ❌ |
|[MeasureToolbar](measure-toolbar.md)   | ✔ | ✔ | ❌ | ❌ |
|[OverviewMap](overview-map.md) | ✔ | ✔ | ✔ | ❌ |
|[PopupViewer](popup-viewer.md) | ✔ | ✔ | ❌ | ❌ |
|[ScaleLine](scale-line.md)   | ✔ | ✔ | ✔ | ✔ |
|[SearchView](search-view.md) | ✔ | ✔ | ✔ | ✔ |
|[SymbolDisplay](symbol-display.md)   | ✔ | ✔ | ✔ | ✔ |
|TableOfContents   | N/A | Preview | N/A  | ❌ |
|[TimeSlider](time-slider.md)   | ✔ | ✔ | ❌ | ❌ |
|[UtilityNetworkTraceTool](un-trace.md) | ✔ | ✔ | ✔ | ❌ |

