# ScaleLine

Display the current scale reference for a map.

![ScaleLine](https://user-images.githubusercontent.com/1378165/73390077-3debb900-428a-11ea-8b2f-dfd4914a637e.png)

## Features

- Supports binding to a MapView.
- Supports display an arbitrary scale via the `MapScale` property.
- Displays both metric and imperial units.

## Usage

WPF, UWP, Xamarin.Forms:

```xml
<esri:ScaleLine MapView="{Binding ElementName=mapView}" />
```