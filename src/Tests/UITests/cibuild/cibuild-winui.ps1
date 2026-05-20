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
$build_params = @('-c', 'Release', '-r', 'win-x64', '-p:SelfContained=true', '-p:PublishProfile=win-x64.pubxml', '-p:WindowsAppSDKSelfContained=true')
if (![string]::IsNullOrWhiteSpace($env:RELEASE_VERSION)) {
  $build_params += "-p:UseNugetPackage=$env:RELEASE_VERSION"
}

# Run tests
$runner_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WinUI\Toolkit.UITests.WinUI.csproj'
$app_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WinUI.App\Toolkit.UITests.WinUI.App.csproj'

$env:UITEST_APP_PATH = Join-Path $PSScriptRoot '..\Toolkit.UITests.WinUI.App\bin\Release\net10.0-windows10.0.19041.0\win-x64\Toolkit.UITests.WinUI.App.exe'
Invoke-WindowsUITests $env:WORKSPACE $runner_project $app_project $build_params $env:NUGET_REPO $env:TRX_FILENAME