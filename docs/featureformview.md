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

To apply the edits, you can make your own buttons and allow users to submit or discard edits.

 ```xml
 <Button Content="Discard" Click="DiscardButton_Click" />
 <Button Content="Apply" Click="ApplyButton_Click" IsEnabled="{Binding IsValid, ElementName=featureFormView}" />
 ```
 ```cs
 private void DiscardButton_Click(object sender, RoutedEventArgs e)
 {
     var result = MessageBox.Show("Discard edits?", "Confirm", MessageBoxButton.YesNo);
     if(result == MessageBoxResult.Yes)
         formViewer.FeatureForm.DiscardEdits();
 }
 private async void ApplyButton_Click(object sender, RoutedEventArgs e)
 {
	 // Collect all errors to display to the user (If you disable the button with the above 'IsValid' binding expression this step isn't necessary)
	 // Note that often you'll still be able to apply edits ignoring the feature form's set of rules, as long as those rules don't violate the underlying table schema.
     var errors = await formViewer.FeatureForm.EvaluateExpressionsAsync(); //Ensure all expressions are fully evaluated.
     var errorList = formViewer.FeatureForm.Elements.OfType<FieldFormElement>().SelectMany(s => s.ValidationErrors).Concat(errors);
     if (errorList.Any())
     {
         MessageBox.Show("Form has errors:\n" + string.Join("\n", errorList.Select(e => e.Message)), "Can't apply");
         return;
     }
     try
     {
         await formViewer.FeatureForm.Feature.FeatureTable.UpdateFeatureAsync(formViewer.FeatureForm.Feature);
     }
     catch (Exception ex)
     {
         MessageBox.Show("Failed to apply edits:\n" + ex.Message, "Error");
     }
 }
 ```