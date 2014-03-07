arcgis-toolkit-dotnet

This project contains source code for controls you can use with the ArcGIS Runtime SDK for .NET.  

NOTE: This library requires the ArcGIS Runtime SDK for .NET. See the instructions below on how to get started.

## Controls

- Legend - Displays a legend for a set of layers in your map.
- Attribution - Displays copyright/attribution information for layers in your map.
- ScaleLine - Displays current scale reference.
- SignInDialog - (Desktop only) SignIn dialog that challenges the user when accessign ArcGIS secured resources.
- FeatureDataField - Edit control for working with individual feature attributes.
- FeatureDataForm - A simple form layout of all editable fields in a feature using the FeatureDataField for each attribute.

## Instructions

1. Fork and then clone the repo or download the .zip file. 
2. Download and install the [ArcGIS Runtime SDK for .NET](http://esriurl.com/dotnetsdk).  Login to the beta community requires an Esri Global account, which can be created for free.  
3. For Windows Desktop:
*  Confirm that your system meets the requirements for [Windows Desktop](http://developers.arcgis.com/net/desktop/guide/system-requirements.htm).
*  Open the solution (WinDesktop-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2012 or 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.
*  Add a reference to the Esri.ArcGISRuntime.Toolkit.dll in your solution.   
4. For Windows Store: 
*  Confirm that your system meets the requirements for [Windows Store](http://developers.arcgis.com/net/store/guide/system-requirements.htm). 
*  Open the solution (WinStore-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.
5. For Windows Phone: 
*  Confirm that your system meets the requirements for [Windows Phone](http://developers.arcgis.com/net/phone/guide/system-requirements.htm). 
*  Open the solution (WinPhone-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2012 and build the Esri.ArcGISRuntime.Toolkit.dll.

## Use 
Once a reference to the ArcGIS Toolkit is made in a project, register the xmlns namespace in the `Page` or `UserControl` where you want to use a Toolkit control:

```xml
  xmlns:esriTK="using:Esri.ArcGISRuntime.Toolkit.Controls" 
```

<i>Note: Some of the following examples assumes an ArcGIS map view control named 'MyMapView' containing an ArcGIS map named 'MyMap' is present.</i>

####Legend
Set the `Layers` property to the collection of layers you want to show legend for.
If you only want to display layers that are visible at the current scale range, also set the `Scale` property. Set this to 'NaN' (or don't set it) to avoid filtering by layer scale visibility.
Both of these properties are available for binding directly from the map and the map view. Example:
```xml
  <esriTK:Legend Layers="{Binding Layers, ElementName=MyMap}" 
    Scale="{Binding Scale, ElementName=MyMapView}" />
```
Other useful properties:
* `ShowOnlyVisibleLayers` - Don't show legend for layers not currently visible
* `ReverseLayersOrder` - Reverses the layer order to easily order layers so that layers that are on top in the map is showing first in the legend. If you want more control over the order, use a converter or bind the collection via your view model to create a more custom collection.

####Attribution
Simply set the `Layers` property to those layers that you want to display attribution for. Note that the terms of use for many services requires you to display some form of attribution, and this control will make that easy for you.
Attribution is displayed for visible layers only.
```xml
  <esriTK:Attribution Layers="{Binding Layers, ElementName=MyMap}" />
```
If you only want to display attribution for layers that are visible at the current scale range, also set the `Scale` property. Set this to 'NaN' (or don't set it) to avoid filtering by layer scale visibility.
```xml
  <esriTK:Attribution Layers="{Binding Layers, ElementName=MyMap}" 
    Scale={Binding Scale, ElementName=MyMapView} />
```
For a support of layers with attribution depending on the current extent and scale also set the `Extent` property.
```xml
  <esriTK:Attribution Layers="{Binding Layers, ElementName=MyMap}" 
    Scale={Binding Scale, ElementName=MyMapView} 
    Extent={Binding Extent, ElementName=MyMapView} />
```

####ScaleLine
```xml
   <esriTK:ScaleLine Scale="{Binding Scale, ElementName=MyMapView}" />
```

####SignInDialog (Desktop only)
The simplest way to use it is by setting the IdentityManager ChallengeMethod to the static DoSignIn:
```code
   IdentityManager.Current.ChallengeMethod = SignInDialog.DoSignIn;
```

####FeatureDataField
Set the `GeodatabaseFeature` property to the GeodatabaseFeature you want to work with, then set the `FieldName` property with the name of the field you would like to edit or display.

```xml
  <esriTK:FeatureDataField GeodatabaseFeature="{Binding MyGdbFeature}" FieldName="MyField" IsReadOnly="True" />
```

FeatureDataField Properties:
* `GeodatabaseFeature` - GeodatabaseFeature that contains information used for valiation requirements and defines UI for `FieldName`
* `FieldName` - The name of the field found in GdbFeature.Attributes that UI will be created for.
* `IsReadOnly` - Allows user to make an editable field readonly. Doesn't allow readonly fields to become editable.
* `Value` - The current value of the FeatureDataField control.
* `ValidationException` - `Exception` property that hold any messaged generated when the `Value` doesn't pass validation requirement.
* `ReadOnlyTemplate` - This `DataTemplate` is used if the `IsReadOnly` is true or if the `FieldInfo` defines the field as readonly.
* `InputTemplate` - This `DataTemplate` is used for standard input for string, number, and datetime types. 
* `SelectorTemplate` - This `DataTempalte` is used when `FieldInfo` defines a `CodedValueDomain`.

####FeatureDataForm
Set the `GeodatabaseFeature` property to the GeodatabaseFeature you want to work with. Use the ApplyCompleted event to listen for when the user clicked apply, and then add the edited feature back into the GeodatabaseFeatureTable using the UpdateAsync(...) method.

```xml
  <esriTK:FeatureDataForm GeodatabaseFeature="{Binding MyGdbFeature}" IsReadOnly="False" />
```

## Resources

* [ArcGIS Runtime SDK for .NET](http://esriurl/dotnetsdk)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Anyone and everyone is welcome to contribute. 

## Licensing
Copyright 2014 Esri

This source is subject to the Microsoft Public License (Ms-PL).
You may obtain a copy of the License at

http://www.microsoft.com/en-us/openness/licenses.aspx#MPL

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt]( https://raw.github.com/Esri/arcgis-toolkit-dotnet/master/license.txt) file.

[](Esri Tags: ArcGIS Runtime SDK .NET WinRT WinStore WPF WinPhone C# C-Sharp DotNet XAML)
[](Esri Language: DotNet)


