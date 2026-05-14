if ([string]::IsNullOrWhiteSpace($env:WORKSPACE)) {
  Write-Error 'WORKSPACE environment variable is not set. Please set it to the root of your workspace.'
  exit 1
}

Import-Module $(Join-Path $PSScriptRoot 'utils.psm1') -Force
if (-not $?) {
  exit 1
}

$dotnet_version = "10.0.300"
$dotnet_exe = Install-Dotnet $env:WORKSPACE $dotnet_version $env:DOTNET_CACHE_FOLDER
