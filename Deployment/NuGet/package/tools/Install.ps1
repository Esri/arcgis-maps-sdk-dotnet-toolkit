param($installPath, $toolsPath, $package, $project) 

# Save any outstanding changes
$dte.ExecuteCommand("File.SaveAll", "")
$project.Save();

# Resolve relative path to package
$fileInfo = new-object -typename System.IO.FileInfo -ArgumentList $project.FullName
$tmp = Get-Location
Set-Location $fileInfo.DirectoryName
$relativeInstallPath = Resolve-Path -relative $installPath
Set-Location $tmp

# Get target framework
$moniker = $project.Properties.Item("TargetFrameworkMoniker").Value
$frameworkName = New-Object System.Runtime.Versioning.FrameworkName($moniker)

# Initialize assembly name and location variables
$apiInclude = "Esri.ArcGISRuntime.Toolkit"
$apiRefName = "ArcGIS Runtime Toolkit for Windows Store apps"
$apiAssembly = "Esri.ArcGISRuntime.Toolkit.WindowsStore.dll";
$frameworkAbbr = "win81"
$hintPathRoot = "$($relativeInstallPath)\sdk\"

if ($frameworkName.Identifier -eq "WindowsPhoneApp") # Windows Phone
{
    $apiRefName = "ArcGIS Runtime Toolkit for Windows Phone"
$apiAssembly = "Esri.ArcGISRuntime.Toolkit.WindowsPhone.dll";
    $frameworkAbbr = "wpa81"
    $hintPathRoot = "$($hintPathRoot)$($frameworkAbbr)\`$(Platform)\"
}
elseif ($frameworkName.Identifier -ne ".NETCore") # Desktop
{
    $apiRefName = "Esri.ArcGISRuntime.Toolkit"
    $apiAssembly = "Esri.ArcGISRuntime.Toolkit.dll";
    $frameworkAbbr = "net45"
    $hintPathRoot = "$($hintPathRoot)$($frameworkAbbr)\"
} 
else # Windows Store
{
    $hintPathRoot = "$($hintPathRoot)$($frameworkAbbr)\`$(Platform)\"
}

# Get the Visual Studio project as an MSBuild project
$msbuildProj = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.LoadProject($project.FullName) | Select-Object -First 1

# Guard against package being installed when Toolkit is already being referenced
$references = $msbuildProj.GetItems("Reference")
foreach($ref in $references)
{
	if($ref.EvaluatedInclude.StartsWith($apiInclude) -or $ref.EvaluatedInclude -eq $apiRefName)
	{
        [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.UnloadProject($msbuildProj)
        throw "The ArcGIS Runtime SDK for .NET - Toolkit is already referenced in this project.  Please remove this reference and try installing again."
	}
}

# Define ItemGroup for assembly references
$itemGroup = $msbuildProj.Xml.AddItemGroup()

# Dictionary to define HintPath
$metadata = New-Object 'System.Collections.Generic.Dictionary[String, String]'

# Add API assembly ref
$metadata["HintPath"] = "$($hintPathRoot)$($apiAssembly)";
$itemGroup.AddItem("Reference", $apiRefName, $metadata);

# Save and unload the project
$msbuildProj.Save()
[Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.UnloadProject($msbuildProj)
