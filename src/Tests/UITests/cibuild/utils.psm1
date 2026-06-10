function Invoke-WindowsUITests {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$workspace,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$runner_name,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$app_name,

    [Parameter(Mandatory)]
    [string[]]$build_parameters,

    [Parameter(Mandatory)]
    [bool]$install_maui,

    [Parameter()]
    [string]$nuget_repo,

    [Parameter()]
    [string]$trx_filename,

    [Parameter()]
    [string[]]$build_parameters_app
  )

  $config_file = Join-Path $PSScriptRoot "variables.yml"
  $runner_project = Join-Path $PSScriptRoot "..\${runner_name}\${runner_name}.csproj"
  $app_project = Join-Path $PSScriptRoot "..\${app_name}\${app_name}.csproj"

  # Install dotnet
  $dotnet_version = Get-YamlValue $config_file 'dotnet-version'
  $dotnet_exe = Install-Dotnet $workspace $dotnet_version $env:DOTNET_CACHE_FOLDER

  # Install maui workload if required
  if ($install_maui) {
    & $dotnet_exe workload install maui
    if ($LASTEXITCODE -ne 0) { Write-Error 'maui workload install failed.'; exit $LASTEXITCODE }
  }

  # Install Node.js
  $node_version = Get-YamlValue $config_file 'node-version'
  $node_workspace = Join-Path $workspace '.node'
  $node_exe, $npm_exe = Install-Nodejs $node_workspace $node_version

  # Install appium and driver
  $appium_entry = Join-Path $node_workspace 'node_modules\appium\index.js'
  $env:APPIUM_HOME = Join-Path $workspace '.appium'

  & $npm_exe install appium --prefix $node_workspace
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }

  $drivers_installed = & $node_exe $appium_entry driver list --installed 2>&1
  if (!(Select-String -InputObject $drivers_installed -Pattern "windows" -Quiet)) {
    & $node_exe $appium_entry driver install windows
  }
  else {
    & $node_exe $appium_entry driver update windows
  }
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Error installing or updating appium driver. See logs."
    exit $LASTEXITCODE
  }

  # Install koffi
  & $npm_exe install koffi --prefix $node_workspace
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Error installing koffi node module. Koffi is necessary to avoid reliance on node-ffi, which in turn requires a Visual Studio install."
    exit $LASTEXITCODE
  }

  # Extract and configure WindowsAppDriver for appium
  $wad_installer = Join-Path $workspace 'WinAppDriver.msi'
  & curl.exe -fsSL https://github.com/microsoft/WinAppDriver/releases/download/v1.2.1/WindowsApplicationDriver_1.2.1.msi -o $wad_installer
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to download Windows App Driver installer."
    & $dotnet_exe build-server shutdown
    exit $LASTEXITCODE
  }

  $wad_install_dir = Join-Path $workspace '.WAD'
  $env:APPIUM_WAD_PATH = Join-Path $wad_install_dir 'Windows Application Driver\WinAppDriver.exe'
  & msiexec.exe /a $wad_installer TARGETDIR=$wad_install_dir /qn
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to install Windows App Driver."
    & $dotnet_exe build-server shutdown
    exit $LASTEXITCODE
  }

  # Set nuget source if provided
  if (![string]::IsNullOrWhiteSpace($nuget_repo)) {
    $toolkit_src_dir = Join-Path $PSScriptRoot '..\..\..'
    Set-NugetSource $toolkit_src_dir $dotnet_exe $nuget_repo
    if (!$?) {
      exit 1
    }
  }

  # Ensure predictable artifacts output layout for setting env:TKUITEST_APP later
  $build_params_artifacts = @('-p:ArtifactsPivots=TestBuild', '-p:UseArtifactsOutput=true')
  $build_parameters += $build_params_artifacts

  try {
    # Build app and runner projects
    & $dotnet_exe build $app_project @build_parameters @build_parameters_app
    if ($LASTEXITCODE -ne 0) {
      Write-Error "App build failed. Aborting."
      exit $LASTEXITCODE
    }

    & $dotnet_exe build $runner_project @build_parameters
    if ($LASTEXITCODE -ne 0) {
      Write-Error "Runner build failed. Aborting."
      exit $LASTEXITCODE
    }

    # The tests seem to need to be run from the toolkit source root
    $toolkit_src_root = Join-Path $PSScriptRoot '..\..\..'
    Push-Location -Path $toolkit_src_root

    try {
      # Start appium server and wait for it to report as ready
      $appium_server_process = Start-Process -FilePath $node_exe -ArgumentList @($appium_entry) -PassThru
      if (!$?) {
        exit 1
      }

      $ready = $false
      $deadline = (Get-Date).AddSeconds(60)
      while ((Get-Date) -lt $deadline) {
          try {
              $r = Invoke-RestMethod -Uri 'http://127.0.0.1:4723/status' -TimeoutSec 2
              if ($r.value.ready) { $ready = $true; break }
          } catch { Start-Sleep -Milliseconds 500 }
      }
      if (-not $ready) { Write-Error 'Appium did not become ready within 60s.'; exit 1 }

      # Run tests
      $results_dir = Join-Path $workspace 'TestResults'
      $test_run_params = @('--no-build', '--report-trx', '--results-directory', $results_dir)
      if (![string]::IsNullOrWhiteSpace($trx_filename)) {
        $test_run_params += @('--report-trx-filename', $trx_filename)
      }

      $env:TKUITEST_APP = Join-Path $PSScriptRoot "..\artifacts\bin\${app_name}\TestBuild\${app_name}.exe"
      & $dotnet_exe test --project $runner_project @build_parameters @test_run_params
    }
    finally {
      # Kill appium and return to original location
      if ($appium_server_process) {
        & taskkill /T /F /PID $($appium_server_process.Id)
        Pop-Location
      }
    }
  }
  finally {
    # Kill dotnet build server
    & $dotnet_exe build-server shutdown
  }
}

function Get-YamlValue {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]$path,

    [Parameter(Mandatory)]
    [string]$value_name
  )

  return $(Select-String -Path $path -Pattern "${value_name}: ""(.*)""")[0].Matches[0].Groups[1].Value
}

function Install-Dotnet {

  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]$workspace,

    [Parameter(Mandatory)]
    [string]$dotnet_version,

    [string]$dotnet_cache_folder=$env:DOTNET_CACHE_FOLDER
  )

  if (![string]::IsNullOrWhiteSpace($env:DOTNET_EXE)) {
    Write-Host 'DOTNET_EXE provided as an environment variable. Skipping install.'
    $env:DOTNET_ROOT = [System.IO.Path]::GetDirectoryName($env:DOTNET_EXE)
    return $env:DOTNET_EXE
  }

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
    & curl.exe -fsSL https://dot.net/v1/dotnet-install.ps1 -o $installerPath
    & $installerPath -Version $dotnet_version -InstallDir $dotnet_install_folder -NoPath
    if ($LASTEXITCODE -ne 0) {
      exit 1
    }

    Write-Host "Dotnet installed at $dotnet_exe`n"
  }
  else {
    Write-Host "Found cached dotnet at $dotnet_exe`n"
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

  $node_dir = Join-Path $workspace "node-v${node_version}-win-x64"
  $node_exe = Join-Path $node_dir 'node.exe'
  $npm_exe = Join-Path $node_dir 'npm.cmd'
  if (!(Test-Path $node_exe)) {
    $node_url = "https://nodejs.org/dist/v${node_version}/node-v${node_version}-win-x64.zip"
    $node_zip = Join-Path $workspace "node.zip"

    & curl.exe -fsSL $node_url -o $node_zip --create-dirs
    if ($LASTEXITCODE -ne 0) {
      Write-Error 'Failed to download Node.js zip'
      exit 1
    }

    Expand-Archive -Path $node_zip -Destination $workspace
    if (!$?) {
      Write-Error 'Failed to extract Node.js zip'
      exit 1
    }

    Write-Host "Node.js installed at $node_exe`n"
  }
  else {
    Write-Host "Found cached Node.js at $node_exe`n"
  }

  # Node sometimes calls itself base on path internally, so we cannot rely on returning the local variables
  $env:PATH = "${node_dir};${env:PATH}"

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
