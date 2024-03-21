# FeatureDataField

Display and optionally allow editing of a single attribute of a feature.

![FeatureDataField](https://user-images.githubusercontent.com/1378165/73389879-ebaa9800-4289-11ea-8e4e-de153a6a371a.png)

> **Note**: In the above screenshot, the `FeatureDataField` is used to render individual cells in the table. `FeatureDataField` alone does not implement attribute table functionality.

## Features

- Binds to a `Feature`.
- Exposes a `ValidationException` property, which can be used to check for errors in an editing scenario.
- Supports extended customization via the `ReadOnlyTemplate`, `SelectorTemplate`, and `InputTemplate` properties.

## Usage

### UWP/WinUI/WPF:

```xml
<esri:FeatureDataField Feature="{Binding selectedFeature}" FieldName="objectid" IsReadOnly="True" />
```
