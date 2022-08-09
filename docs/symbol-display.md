# SymbolDisplay

Displays a symbol from ArcGIS Runtime outside of a `GeoView`.

![SymbolDisplay](https://user-images.githubusercontent.com/1378165/73390051-31676080-428a-11ea-9feb-afb5d2aa6385.png)

## Features

- Supports binding.
- Renders `Symbol` objects.

## Usage

WPF, UWP, Xamarin.Forms:

```xml
<esri:SymbolDisplay Symbol="{Binding Symbol}" />
```

Android:

```cs
var sd = new SymbolDisplay(this) { Symbol = symbol };
```

iOS:

```cs
var sd = new SymbolDisplay { Symbol = symbol };
```