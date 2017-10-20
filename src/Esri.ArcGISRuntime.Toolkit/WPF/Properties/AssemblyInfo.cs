using System.Resources;
using System.Windows;

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                             //(used if a resource is not found in the page, 
                             // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                      //(used if a resource is not found in the page, 
                                      // app, or any theme specific resource dictionaries)
)]

[assembly: NeutralResourcesLanguageAttribute("en-US")]

[assembly: System.Windows.Markup.XmlnsPrefix("http://schemas.esri.com/arcgis/runtime/2013", "esri")]
[assembly: System.Windows.Markup.XmlnsDefinition("http://schemas.esri.com/arcgis/runtime/2013", "Esri.ArcGISRuntime.Toolkit.UI")]
[assembly: System.Windows.Markup.XmlnsDefinition("http://schemas.esri.com/arcgis/runtime/2013", "Esri.ArcGISRuntime.Toolkit.UI.Controls")]
