using Esri.ArcGISRuntime;
using UIKit;

namespace ARToolkit.SampleApp
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // Initialize the ArcGIS Runtime before any components are created.
            ArcGISRuntimeEnvironment.Initialize();

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}