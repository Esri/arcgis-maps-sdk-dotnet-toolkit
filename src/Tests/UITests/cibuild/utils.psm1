function Install-Dotnet {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]$workspace,

    [Parameter(Mandatory)]
    [string]$dotnet_version,

    [string]$dotnet_cache_folder=$env:DOTNET_CACHE_FOLDER
  )

  Write-Host 'Starting dotnet install...'

  # Install the requested dotnet version if it is not already present.
  if ([string]::IsNullOrWhiteSpace($dotnet_cache_folder)) {
    $dotnet_install_folder = Join-Path $workspace '.dotnet'
  }
  else {
    $dotnet_install_folder = Join-Path $dotnet_cache_folder $dotnet_version
  }

  $dotnet_exe = Join-Path $dotnet_install_folder 'dotnet.exe'

  if ([string]::IsNullOrWhiteSpace($dotnet_cache_folder) -or !(Test-Path -Path $dotnet_exe)) {
    $installerPath = Join-Path $workspace 'dotnet-install.ps1'
    & curl.exe -L https://dot.net/v1/dotnet-install.ps1 -o $installerPath
    & $installerPath -Version $dotnet_version -InstallDir $dotnet_install_folder -NoPath
    if ($LASTEXITCODE -ne 0) {
     exit 1
    }

    Write-Host "Dotnet installed at $($dotnet_exe)`n"
  }
  else {
    Write-Host "Found cached dotnet at $($dotnet_exe)`n"
  }

  # Set dotnet location for apps that rely on .NET runtime: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_host_path
  $env:DOTNET_ROOT = $dotnet_install_folder

  return $dotnet_exe
}

function Install-Nodejs {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]$workspace,

    [Parameter(Mandatory)]
    [string]$node_version
  )

  Write-Host "Starting Node.js install..."

  $node_dir = Join-Path $workspace "node-v$($node_version)-win-x64"
  $node_exe = Join-Path $node_dir 'node.exe'
  $npm_exe = Join-Path $node_dir 'npm.cmd'
  if (!(Test-Path $node_exe)) {
    $node_url = "https://nodejs.org/dist/v$($node_version)/node-v$($node_version)-win-x64.zip"
    $node_zip = Join-Path $workspace "node.zip"

    & curl.exe -L $node_url -o $node_zip --create-dirs
    if ($LASTEXITCODE -ne 0) {
      Write-Error 'Failed to download Node.js zip'
      exit 1
    }

    Expand-Archive -Path $node_zip -Destination $workspace
    if (!$?) {
      Write-Error 'Failed to extract Node.js zip'
      exit 1
    }

    Write-Host "Node.js installed at $($node_exe)`n"
  }
  else {
    Write-Host "Found cached Node.js at $($node_exe)`n"
  }

  # Node sometimes calls itself base on path internally, so we cannot rely on returning the local variables
  $env:PATH = "$($node_dir);$($env:PATH)"

  return $node_exe, $npm_exe
}

function Set-NugetSource {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]$workspace,

    [Parameter(Mandatory)]
    [string]$dotnet_exe,

    [Parameter(Mandatory)]
    [string]$nuget_repo
  )

  Write-Host "Configuring nuget..."

  $LASTEXITCODE = 0
  & $dotnet_exe new nugetconfig --force -o $workspace
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }

  $configfile = Join-Path $workspace 'nuget.config'
  & $dotnet_exe nuget add source $nuget_repo --configfile $configfile
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }

  $nuget_dir = Join-Path $workspace '.nuget'
  $env:NUGET_PACKAGES = Join-Path $nuget_dir 'packages'
  $env:NUGET_HTTP_CACHE_PATH = Join-Path $nuget_dir 'cache'
  if (!$?) {
    Write-Error "Failed to set nuget environment variables."
    exit 1
  }

  Write-Host "Done configuring nuget.`n"
}
