#if !__IOS__ && !WINDOWS && !__ANDROID__ && !__MACCATALYST__
// Make sure net6.0 non-os target clearly states which platforms are supported by the toolkit
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows10.0.19041")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("android28.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("ios17.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("maccatalyst17.0")]
#endif

[assembly: Microsoft.Maui.Controls.XmlnsPrefix("http://schemas.esri.com/arcgis/runtime/2013", "esri")]
[assembly: XmlnsDefinition("http://schemas.esri.com/arcgis/runtime/2013", "Esri.ArcGISRuntime.Toolkit.Maui")]
