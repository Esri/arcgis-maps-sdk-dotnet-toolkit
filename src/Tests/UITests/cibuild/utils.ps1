if ([string]::IsNullOrWhiteSpace($env:WORKSPACE)) {
  Write-Error 'WORKSPACE environment variable is not set. Please set it to the root of your workspace.'
  exit 1
}

if (!(Test-Path -LiteralPath $env:WORKSPACE -PathType Container)) {
  New-Item -Path $env:WORKSPACE -ItemType Directory -ErrorAction Stop | Out-Null
}

if ([string]::IsNullOrWhiteSpace($env:DOTNET_VERSION)) {
    $yaml_path = Join-Path $PSScriptRoot 'variables.yml'
    $value_name = 'dotnet-version'
    $dotnet_version = $(Select-String -Path $yaml_path -Pattern "${value_name}: ""(.*)""")[0].Matches[0].Groups[1].Value
}
else {
    $dotnet_version = $env:DOTNET_VERSION
}

Write-Host 'Starting dotnet install...'
if (![string]::IsNullOrWhiteSpace($env:DOTNET_EXE)) {
    Write-Host 'DOTNET_EXE provided as an environment variable. Skipping install.'
    $dotnet_exe = $env:DOTNET_EXE
}
else {
    # Install the requested dotnet version if it is not already present.
    if ([string]::IsNullOrWhiteSpace($env:DOTENT_CACHE_FOLDER)) {
        $dotnet_install_folder = Join-Path $env:WORKSPACE '.dotnet'
    }
    else {
        $dotnet_install_folder = Join-Path $env:DOTENT_CACHE_FOLDER $dotnet_version
    }

    $dotnet_exe = Join-Path $dotnet_install_folder 'dotnet.exe'

    if ([string]::IsNullOrWhiteSpace($env:DOTENT_CACHE_FOLDER) -or !(Test-Path -Path $dotnet_exe)) {
    $installerPath = Join-Path $env:WORKSPACE 'dotnet-install.ps1'
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
}

$env:DOTNET_ROOT = [System.IO.Path]::GetDirectoryName($dotnet_exe)

$test_script = Join-Path $PSScriptRoot 'uitest-installer.cs' 
& $dotnet_exe $test_script $args

