using Esri.ArcGISRuntime;
using UIKit;
using ARToolkit.SampleApp.iOS;

// This is the main entry point of the application.
 // Initialize the ArcGIS Maps SDK for .NET before any components are created.
ArcGISRuntimeEnvironment.Initialize();

// If you want to use a different Application Delegate class from "AppDelegate"
// you can specify it here.
UIApplication.Main (args, null, typeof (AppDelegate));
