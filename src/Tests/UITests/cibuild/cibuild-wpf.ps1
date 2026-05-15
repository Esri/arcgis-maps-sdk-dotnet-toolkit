if ([string]::IsNullOrWhiteSpace($env:WORKSPACE)) {
  Write-Error 'WORKSPACE environment variable is not set. Please set it to the root of your workspace.'
  exit 1
}

# Import utils module
Import-Module $(Join-Path $PSScriptRoot 'utils.psm1') -Force
if (-not $?) {
  exit 1
}

# Install dotnet
$dotnet_version = "10.0.300"
$dotnet_exe = Install-Dotnet $env:WORKSPACE $dotnet_version $env:DOTNET_CACHE_FOLDER

# Install Node.js
$node_version = '24.15.0'
$node_workspace = Join-Path $env:WORKSPACE '.node'
$node_exe, $npm_exe = Install-Nodejs $node_workspace $node_version

# Install appium and driver
$appium_entry = Join-Path $node_workspace 'node_modules\appium\index.js'
$env:APPIUM_HOME = Join-Path $env:WORKSPACE '.appium'

& $npm_exe install appium --prefix $node_workspace
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

& $node_exe $appium_entry driver install windows

# Set nuget source if provided
if (![string]::IsNullOrWhiteSpace($env:NUGET_REPO)) {
  $toolkit_src_dir = Join-Path $PSScriptRoot '..\..\..'
  Set-NugetSource $toolkit_src_dir $dotnet_exe $env:NUGET_REPO
  if (!$?) {
    exit 1
  }
}

# Build the test app and test runner projects
if (![string]::IsNullOrWhiteSpace($env:RELEASE_VERSION)) {
  $property_UseNugetPackage = "-p:UseNugetPackage=$($env:RELEASE_VERSION)"
}

$wpf_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WPF\Toolkit.UITests.WPF.csproj'
$wpf_app_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WPF.App\Toolkit.UITests.WPF.App.csproj'
& $dotnet_exe build $wpf_app_project -c Release $property_UseNugetPackage
& $dotnet_exe build $wpf_project -c Release $property_UseNugetPackage
if (!$?) {
  exit 1
}

# Start appium
$appium_server_process = Start-Process -FilePath $node_exe -ArgumentList @($appium_entry) -PassThru
if (!$?) {
  & $dotnet_exe build-server shutdown
  exit 1
}

# Run tests
$results_dir = Join-Path $env:WORKSPACE 'TestResults'
$toolkit_src_root = Join-Path $PSScriptRoot '..\..\..'
if (![string]::IsNullOrWhiteSpace($env:TRX_FILENAME)) {
  $parameter_trxfilename = "--report-trx-filename $($env:TRX_FILENAME)"
}

Set-Location -Path $toolkit_src_root
Invoke-Expression "$dotnet_exe test $wpf_project $property_UseNugetPackage --results-directory $results_dir --report-trx $parameter_trxfilename"

# Kill the appium server process and build server
Stop-Process -InputObject $appium_server_process
& $dotnet_exe build-server shutdown
