﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Foundation;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("$ext_safeprojectname$.iOS")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("$ext_safeprojectname$.iOS")]
[assembly: AssemblyCopyright("Copyright © $registeredorganization$ $year$")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("72bdc44f-c588-44f3-b6df-9aace7daafdd")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Prevent linker from excluding required types from app package
[assembly: Preserve(typeof(global::Esri.ArcGISRuntime.ARToolkit.Forms.ARSceneView), AllMembers = true)]
[assembly: Preserve(typeof(global::Esri.ArcGISRuntime.Xamarin.Forms.SceneView), AllMembers = true)]
[assembly: Preserve(typeof(global::Esri.ArcGISRuntime.Xamarin.Forms.GeoView), AllMembers = true)]