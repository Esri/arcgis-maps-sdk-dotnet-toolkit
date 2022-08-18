# Compass

Show a compass direction for a `MapView` or `SceneView`. Resets the view orientation when tapped/clicked.

![compass](https://user-images.githubusercontent.com/1378165/73389839-d9c8f500-4289-11ea-923c-18232489b3e0.png)

## Features

- Supports binding to a `GeoView`.
- Can be configured to hide itself automatically when the heading is 0 via the `AutoHide` property.
- Allows display of a manually-set heading via the `Heading` property.


## Usage

```xml
<esri:Compass GeoView="{Binding ElementName=mapView}" AutoHide="False" />
```