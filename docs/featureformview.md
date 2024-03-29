# FeatureFormView

Generates an interactive form from a [FeatureFormDefinition](https://developers.arcgis.com/net/api-reference/api/netwin/Esri.ArcGISRuntime/Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureFormDefinition.html). 

![image](https://github.com/Esri/arcgis-maps-sdk-dotnet-toolkit/assets/1378165/ff655fac-9eae-48ce-a044-588a2e32afa8)

## Features

- Supports interactive editing of Features through text, checkboxes, comboboxes, switches etc.

## Usage

FeatureFormView displays a feature from using an underlying [`FeatureForm`](https://developers.arcgis.com/net/api-reference/api/netwin/Esri.ArcGISRuntime/Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm.html).


The following code shows how to get a `Feature` and its `FeatureFormDefinition` from an identify result:

```cs
 private ArcGISFeature? GetFeature(IEnumerable<IdentifyLayerResult> results, out FeatureFormDefinition? definition)
{
    def = null;
    if (results == null)
        return null;
    foreach (var result in results.Where(r => r.LayerContent is FeatureLayer layer && (layer.FeatureFormDefinition is not null || (layer.FeatureTable as ArcGISFeatureTable)?.FeatureFormDefinition is not null)))
    {
        var feature = result.GeoElements?.OfType<ArcGISFeature>()?.FirstOrDefault();
        def = (result.LayerContent as FeatureLayer)?.FeatureFormDefinition ?? ((result.LayerContent as FeatureLayer)?.FeatureTable as ArcGISFeatureTable)?.FeatureFormDefinition;
        if (feature != null && def != null)
        {
            return feature;
        }
    }
    
    return null;
}
```

The following code shows how to get a `FeatureForm` from a `FeatureFormDefinition` and an `ArcGISFeature`:

```cs
var featureForm = new FeatureForm(feature, definition);
```

To display a `FeatureForm` in the UI:

```xml
<esri:FeatureFormView x:Name="featureFormView" />
```

To present a `FeatureForm` in a `FeatureFormView`:

```cs
featureFormView.FeatureForm = featureForm;
```