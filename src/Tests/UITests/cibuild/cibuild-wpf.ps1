if ([string]::IsNullOrWhiteSpace($env:WORKSPACE)) {
  Write-Error 'WORKSPACE environment variable is not set. Please set it to the root of your workspace.'
  exit 1
}

# Import utils module
Import-Module $(Join-Path $PSScriptRoot 'utils.psm1') -Force
if (-not $?) {
  exit 1
}

# Define build parameters
$build_params = @('-c', 'Release')
if (![string]::IsNullOrWhiteSpace($env:RELEASE_VERSION)) {
  $build_params += "-p:UseNugetPackage=$env:RELEASE_VERSION"
}

if ([string]::IsNullOrWhiteSpace($env:ARCGIS_API_KEY)) {
  Write-Error "The environment variable ARCGIS_API_KEY must be supplied and set to a valid API key."
  exit 1
}
$build_params_app = @("-p:TestAppApiKey=$env:ARCGIS_API_KEY")

# Run tests
$runner_name = 'Toolkit.UITests.WPF'
$app_name = 'Toolkit.UITests.WPF.App'
Invoke-WindowsUITests $env:WORKSPACE $runner_name $app_name $build_params $false $env:NUGET_REPO $env:TRX_FILENAME $build_params_app