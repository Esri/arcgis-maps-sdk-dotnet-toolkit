#if !__IOS__ && !WINDOWS && !__ANDROID__ && !__MACCATALYST__
// Make sure net6.0 non-os target clearly states which platforms are supported by the toolkit
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows10.0.19041")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("android26.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("ios14.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("maccatalyst14.0")]
#endif

[assembly: Microsoft.Maui.Controls.XmlnsPrefix("http://schemas.esri.com/arcgis/runtime/2013", "esri")]
[assembly: XmlnsDefinition("http://schemas.esri.com/arcgis/runtime/2013", "Esri.ArcGISRuntime.Toolkit.Maui")]
