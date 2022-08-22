# Legend

Display a legend for a single layer in a map, optionally including sublayers.

<img src="https://user-images.githubusercontent.com/1378165/73389924-011fc200-428a-11ea-91bf-4ea1c2bf6683.png" width="105" title="Legend" />

## Features

- Binds to a `GeoView`.
- Enables filtering out layers that are hidden because they are out of scale via the `FilterByVisibleScaleRange` property.
- Enables filtering out hidden layers via the `FilterHiddenLayers` property.
- Supports reversing the order of displayed layers via the `ReverseLayerOrder` property.

## Usage

```xml
<esri:Legend GeoView="{Binding ElementName=mapView}" />
```