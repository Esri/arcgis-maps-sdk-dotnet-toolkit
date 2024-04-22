# SymbolDisplay

Displays a symbol outside of a `GeoView`.

![SymbolDisplay](https://user-images.githubusercontent.com/1378165/73390051-31676080-428a-11ea-9feb-afb5d2aa6385.png)

## Features

- Supports binding.
- Renders `Symbol` objects.

## Usage

### .NET MAUI/WPF:

```xml
<esri:SymbolDisplay xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
                    Symbol="{Binding Symbol}" />
```

## UWP/WinUI:

```xml
<esri:SymbolDisplay xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls"
                    Symbol="{Binding Symbol}" />
```
