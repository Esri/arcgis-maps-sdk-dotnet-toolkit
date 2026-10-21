# OfflineMapAreasView

OfflineMapAreasView lets users take a web map offline by downloading map areas. It is available for WPF, WinUI, and .NET MAUI.

## Features

- Download preplanned areas prepared by the map author.
- Create on-demand areas by choosing an extent and level of detail.
- View download progress, status, and area details.
- Open downloaded areas or remove them from the device.

## Usage

The control uses `OfflineManager` to manage download jobs and stored areas.

- **Online:** Set `OnlineMap` to an offline-enabled web map. Users can download its preplanned areas, or create on-demand areas when none are defined.
- **Offline:** Set `OfflineMapInfo` to an entry from `OfflineManager.Shared.OfflineMapInfos` to display areas already stored on the device.

Setting either property to a non-null value clears the other. Bind `MapView.Map` to `SelectedMap` to display the user's selection. An optional button bound to `GoOnlineCommand` returns to the online map.

Set `OnlineMap` or `OfflineMapInfo` after `InitializeComponent()` in these examples.

### WPF

```xml
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition Width="320" />
    </Grid.ColumnDefinitions>
    <esri:MapView Map="{Binding SelectedMap, ElementName=MyOfflineMapAreasView}" />
    <esri:OfflineMapAreasView x:Name="MyOfflineMapAreasView" Grid.Column="1" />
</Grid>
```

### WinUI

```xml
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
      xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls">
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition Width="320" />
    </Grid.ColumnDefinitions>
    <esri:MapView Map="{x:Bind MyOfflineMapAreasView.SelectedMap, Mode=OneWay}" />
    <toolkit:OfflineMapAreasView x:Name="MyOfflineMapAreasView" Grid.Column="1" />
</Grid>
```

### .NET MAUI

```xml
<Grid xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
      RowDefinitions="*,300">
    <esri:MapView x:Name="MyMapView" />
    <esri:OfflineMapAreasView x:Name="MyOfflineMapAreasView" Grid.Row="1" />
</Grid>
```

Add the coded binding after `InitializeComponent()`:

```csharp
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Toolkit.Maui;
using Microsoft.Maui.Controls;

MyMapView.SetBinding(MapView.MapProperty,
    static (OfflineMapAreasView view) => view.SelectedMap,
    mode: BindingMode.OneWay, source: MyOfflineMapAreasView);
```
