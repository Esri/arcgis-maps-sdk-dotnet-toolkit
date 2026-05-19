if ([string]::IsNullOrWhiteSpace($env:WORKSPACE)) {
  Write-Error 'WORKSPACE environment variable is not set. Please set it to the root of your workspace.'
  exit 1
}

# Import utils module
Import-Module $(Join-Path $PSScriptRoot 'utils.psm1') -Force
if (-not $?) {
  exit 1
}

$config_file = Join-Path $PSScriptRoot "variables.yml"

# Install dotnet
$dotnet_version = Get-YamlValue $config_file 'dotnet-version'
$dotnet_exe = Install-Dotnet $env:WORKSPACE $dotnet_version $env:DOTNET_CACHE_FOLDER

# Install Node.js
$node_version = Get-YamlValue $config_file 'node-version'
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

# Extract and configure WindowsAppDriver for appium
$env:APPIUM_WAD_PATH = Join-Path $env:APPIUM_HOME 'WinAppDriver1.2.1\WinAppDriver.exe'
if (!(Test-Path $env:APPIUM_WAD_PATH)) {
  $wap_zip = Join-Path $PSScriptRoot 'WinAppDriver1.2.1.zip'
  Expand-Archive -Path $wap_zip -Destination $env:APPIUM_HOME
  if (!$?) {
    Write-Error 'Failed to extract WinAppDriver zip'
    exit 1
  }
}

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
  $property_UseNugetPackage = "-p:UseNugetPackage=$env:RELEASE_VERSION"
}
$common_build_params = "-c Release $property_UseNugetPackage"

$wpf_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WPF\Toolkit.UITests.WPF.csproj'
$wpf_app_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WPF.App\Toolkit.UITests.WPF.App.csproj'
Invoke-Expression "$dotnet_exe build $wpf_app_project $common_build_params"
Invoke-Expression "$dotnet_exe build $wpf_project $common_build_params"
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
  $parameter_trxfilename = "--report-trx-filename $env:TRX_FILENAME"
}

Set-Location -Path $toolkit_src_root
Invoke-Expression "$dotnet_exe test $wpf_project $common_build_params --results-directory $results_dir --report-trx $parameter_trxfilename"

# Kill the appium server process and build server
Stop-Process -InputObject $appium_server_process
& $dotnet_exe build-server shutdown
