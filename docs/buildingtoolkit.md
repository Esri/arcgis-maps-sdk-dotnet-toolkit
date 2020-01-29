# Instructions for Building the toolkit

1. Confirm that your system meets the requirements for compiling the Toolkit:
    - Visual Studio 2019 Update 4 (or later) with the following workloads:
        - `Universal Windows Platform development` (UWP)
        - `.NET desktop development` (WPF)
        - `Mobile development with .NET` (Xamarin.Android, Xamarin.iOS, and Xamarin.Forms)
        - `.NET Core cross-platform development`

2. Confirm your system meets the requirements for developing with ArcGIS Runtime SDK for .NET:
   - [WPF](https://developers.arcgis.com/net/latest/wpf/guide/system-requirements.htm)
   - [UWP](https://developers.arcgis.com/net/latest/uwp/guide/system-requirements.htm)
   - [Xamarin.Android](https://developers.arcgis.com/net/latest/android/guide/system-requirements.htm)
   - [Xamarin.iOS](https://developers.arcgis.com/net/latest/ios/guide/system-requirements.htm)
   - [Xamarin.Forms (Android, iOS, and UWP)](https://developers.arcgis.com/net/latest/forms/guide/system-requirements.htm)

3. Fork and then clone the repo or download the .zip file.

4. Include (i) or reference (ii) the Toolkit in your projects:
    > Note the Toolkit references [ArcGIS Runtime SDK for .NET](http://esriurl.com/dotnetsdk) by Nuget package. The package is automatically downloaded when you build the solution for the first time.
    1. Include the appropriate platform Projects in your Solution.
        - WPF (src\Esri.ArcGISRuntime.Toolkit\WPF\Esri.ArcGISRuntime.Toolkit.WPF.csproj)
        - UWP (\src\Esri.ArcGISRuntime.Toolkit\UWP\Esri.ArcGISRuntime.Toolkit.UWP.csproj)
        - Xamarin.Android (\src\Esri.ArcGISRuntime.Toolkit\Android\Esri.ArcGISRuntime.Toolkit.Android.csproj)
        - Xamarin.iOS (\src\Esri.ArcGISRuntime.Toolkit\iOS\Esri.ArcGISRuntime.Toolkit.iOS.csproj)
        - Xamarin.Forms (\src\Esri.ArcGISRuntime.Toolkit\XamarinForms\Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.csproj)
    2. Build the Toolkit and reference the NuGet package you built.
        - Building each Toolkit project automatically creates the NuGet package for each platform in the project Output folder.
        - Create a local nuget source/feed and set it to the \Output\NuGet\Release folder.
        - Add the nuget package using the standard Nuget package manager dialog in Visual Studio.
        - See [Setting Up Local NuGet Feeds](https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds) for more information.
