# arcgis-toolkit-dotnet

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
2. The Toolkit requires the ArcGIS Runtime SDK for .NET.  Confirm that your system meets the requirements for using the ArcGIS Runtime SDK for .NET with [Windows Desktop](http://developers.arcgis.com/net/desktop/guide/system-requirements.htm), Windows Store](http://developers.arcgis.com/net/store/guide/system-requirements.htm), and/or [Windows Phone](http://developers.arcgis.com/net/phone/guide/system-requirements.htm).  
3. Download and install the [ArcGIS Runtime SDK for .NET](http://esriurl.com/dotnetsdk).  Login to the beta community requires an Esri Global account, which can be created for free.
4. To include the Toolkit source in your projects:
 *  In Visual Studio, add the ArcGIS Runtime Toolkit project to your solution. 
	- Windows Desktop (WinDesktop\Esri.ArcGISRuntime.Toolkit\Esri.ArcGISRuntime.Toolkit.proj)
    - Windows Store	(WinStore\Esri.ArcGISRuntime.Toolkit\Esri.ArcGISRuntime.Toolkit.proj)
	- Windows Phone (WinPhone\Esri.ArcGISRuntime.Toolkit\Esri.ArcGISRuntime.Toolkit.proj)
 *  For other projects in the solution, add a reference to the ArcGIS Runtime Toolkit project.
 
Optional: Distribution of the Toolkit for the ArcGIS Runtime
1. Windows Desktop 
 *  Open the solution (WinDesktop-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2012 or 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.
 *  Add a reference to the Esri.ArcGISRuntime.Toolkit.dll in your projects.  
2. Windows Store 
 *  Open the solution (WinStore-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.   Be sure to build for Release on the ARM, x86, and x64 platforms.
3. Windows Phone: 
 *  Open the solution (WinPhone-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2012 or 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.  Be sure to build for Release on the ARM and x86 platforms.
4. To distribute the Toolkit for the ArcGIS Runtime SDK for .NET for use in a Windows Store or Windows Phone project, they need to be packaged as Visual Studio extensions.  Install the [Visual Studio 2013 SDK](http://msdn.microsoft.com/en-us/library/bb166441.aspx).  This SDK is required to build Visual Studio extension installers (VSIX).  
5. Under the Deployment\VSIX folder in this repo, open the VSIX.sln and build the Windows Store and Windows Phone projects.  If you get errors indicating files cannot be found, be sure to build the Windows Store and Windows Phone Toolkit projects with for the release configuration on all platforms.  A set of *.vsix files will be generated in the project output folders. 
6. Run the vsix to install the Toolkit as an extension SDK for Windows Store or Windows Phone projects.  To add a reference in your project, open the Add Reference dialog, navigate to Windows > Extensions, and check the box next to the Toolkit for ArcGIS Runtime.  
        
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
* [Visual Studio 2013 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=40758)

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


