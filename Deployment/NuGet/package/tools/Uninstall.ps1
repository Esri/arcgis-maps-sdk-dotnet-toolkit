param($installPath, $toolsPath, $package, $project) 

# Save files
$dte.ExecuteCommand("File.SaveAll", "");
$project.Save();

# Get target framework
$moniker = $project.Properties.Item("TargetFrameworkMoniker").Value
$frameworkName = New-Object System.Runtime.Versioning.FrameworkName($moniker)

# Initialize assembly name and location variables
$apiRefName = "ArcGIS Runtime Toolkit for Windows Store apps"
$frameworkAbbr = "win81"
$hintPathRoot = "$($installPath)\sdk\"

if ($frameworkName.Identifier -eq "WindowsPhoneApp") # Windows Phone
{
    $apiRefName = "ArcGIS Runtime Toolkit for Windows Phone"
    $frameworkAbbr = "wpa81"
}
elseif ($frameworkName.Identifier -ne ".NETCore") # Desktop
{
    $apiRefName = "Esri.ArcGISRuntime.Toolkit"
    $frameworkAbbr = "net45"
} 
$hintPathRoot = "$($hintPathRoot)$($frameworkAbbr)\"

# Remove NuGet package references, if present
foreach($ref in $project.Object.References)
{
    if ($ref.Name -eq $apiRefName -and $ref.Path.StartsWith($hintPathRoot))
    {
        $ref.Remove()
    }
}

# Save changes
$dte.ExecuteCommand("File.SaveAll", "")
$project.Save();