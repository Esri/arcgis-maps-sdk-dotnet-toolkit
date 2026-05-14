function Install-Dotnet {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]$workspace,

    [Parameter(Mandatory)]
    [string]$dotnet_version,

    [string]$dotnet_cache_folder=$env:DOTNET_CACHE_FOLDER
  )

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
    curl -L https://dot.net/v1/dotnet-install.ps1 -o $installerPath
    & $installerPath -Version $dotnet_version -InstallDir $dotnet_install_folder -NoPath
    if ($LASTEXITCODE -ne 0) {
     exit 1
    }

    Write-Host "Dotnet installed at $dotnet_exe"
  }
  else {
    Write-Host "Found cached dotnet at $dotnet_exe"
  }

  return $dotnet_exe
}