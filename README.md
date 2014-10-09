# arcgis-toolkit-dotnet

This project contains source code for controls and utilities you can use with the [ArcGIS Runtime SDK for .NET](http://links.esri.com/dotnetsdk).   Build from source code available in this repo (see instructions below) or install the latest stable release from NuGet: [https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit](https://www.nuget.org/packages/Esri.ArcGISRuntime.Toolkit)   

## Features

- Legend - Displays a legend for a set of layers in your map.
- Attribution - Displays copyright/attribution information for layers in your map.
- ScaleLine - Displays current scale reference.
- SignInChallengeHandler - (Desktop and Phone only) Displays a UI dialog to enter or select credentials to use when accessing secure ArcGIS resources.  
- FeatureDataField - Edit control for working with individual feature attributes.
- FeatureDataForm - A simple form layout of all editable fields in a feature using the FeatureDataField for each attribute.
- SymbolDisplay - Displays a symbol outside of a map control.
- TemplatePicker - Displays symbols of selectable feature types advertised by a feature layer. 

See the [wiki](https://github.com/Esri/arcgis-toolkit-dotnet/wiki) for more details.

## Instructions 

1. Fork and then clone the repo or download the .zip file.
2. The Toolkit requires the ArcGIS Runtime SDK for .NET.  Confirm that your system meets the requirements for using the ArcGIS Runtime SDK for .NET with [Windows Desktop](http://developers.arcgis.com/net/desktop/guide/system-requirements.htm), [Windows Store](http://developers.arcgis.com/net/store/guide/system-requirements.htm), and/or [Windows Phone](http://developers.arcgis.com/net/phone/guide/system-requirements.htm).  
3. Download and install the [ArcGIS Runtime SDK for .NET](http://esriurl.com/dotnetsdk). 
4. To include Toolkit source in your projects:
 *  In Visual Studio, add the ArcGIS Runtime Toolkit project to your solution. 
    - Windows Desktop (WinDesktop\Esri.ArcGISRuntime.Toolkit\Esri.ArcGISRuntime.Toolkit.WindowsDesktop.proj)
    - Windows Store	(WinStore\Esri.ArcGISRuntime.Toolkit\Esri.ArcGISRuntime.Toolkit.WindowsStore.proj)
    - Windows Phone (WinPhone\Esri.ArcGISRuntime.Toolkit\Esri.ArcGISRuntime.Toolkit.WindowsPhone.proj)
 *  For other projects in the solution, add a reference to the ArcGIS Runtime Toolkit project.
 
#### Optional: Build to distribute the Toolkit
Building the SDK:

1.  Open the solution (Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2013 Update 2 or newer and build the 3 projects.
2.  For Windows Phone, build both ARM and x86 configurations, for Windows Store, also build x64 configuration.

#####Referencing the project in a Windows Desktop (WPF) app:
 
 1.  Add a reference to the \output\WinDesktop\Release\Esri.ArcGISRuntime.Toolkit.dll in your projects.  

#####Referencing the project in a Windows Store or Windows Phone app:
Make sure you built both x86, ARM and x64 (x64 doesn't apply to Windows Phone).
The Toolkit must be referenced as an ExtensionSDK. Referencing the SDK by browsing to a DLL as with desktop apps will not work.
There's two options to 'install' the extension sdk without building an actual installer.

1.  Copy the contents of the folder \Output\ExtensionSDKs\ to "[USERFOLDER]\AppData\Local\Microsoft SDKs\"
2.  Add the following tag to your app project at the very bottom of your project file right before the <Target> tags:
```
<PropertyGroup>
    <SDKReferenceDirectoryRoot>[TOOLKITFOLDER]\output\ExtensionSDKs;$(SDKReferenceDirectoryRoot)</SDKReferenceDirectoryRoot>
</PropertyGroup>
```
In Visual Studio, right-click the project references, select "Add Reference" and choose the "Windows 8.1->Extensions" or "Windows Phone 8.1->Extensions" tab and check the box next to "ArcGIS Runtime Toolkit...".

#####Windows Store and Windows Phone as an installable extension
 1. Install the [Visual Studio 2013 SDK](http://msdn.microsoft.com/en-us/library/bb166441.aspx).  To distribute the Toolkit for use in a Windows Store or Windows Phone project, it should be packaged as Visual Studio extension.  The Visual Studio 2013 SDK is required to build Visual Studio extension installers (VSIX). 
 2. Windows Store 
   *  Open the solution (WinStore-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.   Be sure to build for Release on the ARM, x86, and x64 platforms.
 3. Windows Phone: 
   *  Open the solution (WinPhone-Esri.ArcGISRuntime.Toolkit.sln) in Visual Studio 2012 or 2013 and build the Esri.ArcGISRuntime.Toolkit.dll.  Be sure to build for Release on the ARM and x86 platforms.
 4. Under the Deployment\VSIX folder in this repo, open the VSIX.sln and build.  The [Visual Studio 2013 SDK](http://msdn.microsoft.com/en-us/library/bb166441.aspx) is required to open projects in this solution.  Also be sure to build the Windows Store and Windows Phone Toolkit projects for the release configuration on all platforms.  A set of *.vsix files will be generated in the project output folders. 
 5. Run the vsix to install the Toolkit as an extension SDK for Windows Store or Windows Phone projects.  To add a reference in your project, open the Add Reference dialog, navigate to Windows > Extensions, and check the box next to the "ArcGIS Runtime Toolkit...". 

## Resources

* [ArcGIS Runtime SDK for .NET](http://esriurl/dotnetsdk)
* [Visual Studio 2013 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=40758)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Anyone and everyone is welcome to [contribute](CONTRIBUTING.md).

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


