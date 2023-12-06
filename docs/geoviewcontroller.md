# GeoViewController

GeoViewController is a helper class for enabling MVVM patterns adoption is an ArcGIS Maps SDK for .NET applications. 
The helper class allows you to perform view operations on the MapView from your ViewModel, through an attached proxy-object that ensures you keep ViewModel and View separated.

![compass](https://user-images.githubusercontent.com/1378165/73389839-d9c8f500-4289-11ea-923c-18232489b3e0.png)

## Features

- GeoViewController class can manage most common `GeoView` operations like setting viewpoint, performing identify and showing callouts. Any specific `MapView` or `SceneView` operations are not accessible via this helper class.
- The helpher class is extensible so you can add your own custom map and scene operations, or interface for allowing testing (see WPF sample).
- Currently, there are no events exposed. You can use `EventToCommand` or any similar already established approach to forward view events back to the ViewModel (see samples).


## Usage

```xml
<esri:MapView x:Name="MyMapView"
              Map="{Binding Map, Source={StaticResource VM}}"
              toolkit:GeoViewController.GeoViewController="{Binding Controller, Source={StaticResource VM}}">
```