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
$node_exe, $npm_exe = Install-Nodejs $env:WORKSPACE $node_version
$node_prefix = Join-Path $env:WORKSPACE '.node'

# Install appium and driver
$appium_entry = Join-Path $node_prefix 'node_modules\appium\index.js'
$env:APPIUM_HOME = Join-Path $env:WORKSPACE '.appium'

& $npm_exe install appium --prefix $node_prefix
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

& $node_exe $appium_entry driver install windows

# Build both projects manually since it is easier to see build errors when they are built separately
$output_dir = Join-Path $env:WORKSPACE 'wpf-cibuild-output'
$wpf_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WPF\Toolkit.UITests.WPF.csproj'
$wpf_app_project = Join-Path $PSScriptRoot '..\Toolkit.UITests.WPF.App\Toolkit.UITests.WPF.App.csproj'
& $dotnet_exe build $wpf_app_project
& $dotnet_exe build $wpf_project -o $output_dir
if (!$?) {
  exit 1
}

# Kill the dotnet build server as it is no longer needed
& $dotnet_exe build-server shutdown

# Start appium
$appium_server_process = Start-Process -FilePath $node_exe -ArgumentList @($appium_entry) -PassThru
if (!$?) {
  exit 1
}

# Run tests
$test_exe = Join-Path $output_dir 'Toolkit.UITests.WPF.exe'
$results_dir = Join-Path $env:WORKSPACE 'TestResults'
Write-Host $trx_file
if ([string]::IsNullOrWhiteSpace($env:TRX_FILENAME)) {
  & $test_exe --results-directory $results_dir --report-trx
}
else {
  & $test_exe --results-directory $results_dir --report-trx --report-trx-filename $env:TRX_FILENAME
}

# Kill the appium server process
Stop-Process -InputObject $appium_server_process
