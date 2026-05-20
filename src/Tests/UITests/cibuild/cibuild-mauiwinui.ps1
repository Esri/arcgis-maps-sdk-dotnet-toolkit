if ([string]::IsNullOrWhiteSpace($env:WORKSPACE)) {
  Write-Error 'WORKSPACE environment variable is not set. Please set it to the root of your workspace.'
  exit 1
}

# Import utils module
Import-Module $(Join-Path $PSScriptRoot 'utils.psm1') -Force
if (-not $?) {
  exit 1
}

# Define common build parameters
$config_file = Join-Path $PSScriptRoot "variables.yml"
$build_params = @('-c', 'Release', '-r', 'win-x64', '-p:UseMonoRuntime=false')
if (![string]::IsNullOrWhiteSpace($env:RELEASE_VERSION)) {
  $build_params += "-p:UseNugetPackage=$env:RELEASE_VERSION"
}

# Define app build parameters, including setting MauiVersion to the latest available
$dotnet_version = Get-YamlValue $config_file 'dotnet-version'
$dotnet_major_version = $(Select-String -InputObject $dotnet_version -Pattern '\d+\.\d+(?=\.)')[0].Matches[0].Value
$build_params_app = @("-p:MauiVersion=${dotnet_major_version}*")

$framework = "net${dotnet_major_version}-windows$(Get-YamlValue $config_file 'windows-sdk-version')"
$build_params_app += @('-f', $framework)

# Run tests
$runner_name = 'Toolkit.UITests.MauiWinUI'
$app_name = 'Toolkit.UITests.Maui.App'
Invoke-WindowsUITests $env:WORKSPACE $runner_name $app_name $build_params $true $env:NUGET_REPO $env:TRX_FILENAME $build_params_app