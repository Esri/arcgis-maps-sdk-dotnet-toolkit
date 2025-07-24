@ECHO OFF

SET DocFXVersion=2.77.0
SET DocFxFolder=%~dp0..\.tools\docfx

REM Download DocFx

IF NOT EXIST "%DocFxFolder%\v%DocFXVersion%\docfx.exe" (
   MKDIR "%DocFXFolder%\v%DocFXVersion%"
   powershell -ExecutionPolicy ByPass -command "Invoke-WebRequest -Uri "https://github.com/dotnet/docfx/releases/download/v%DocFXVersion%/docfx-win-x64-v${env:DOCFXVERSION}.zip" -OutFile '%DocFxFolder%\docfx_v%DocFXVersion%.zip'"
   powershell -ExecutionPolicy ByPass -command "Expand-Archive -LiteralPath '%DocFxFolder%\docfx_v%DocFXVersion%.zip' -DestinationPath '%DocFxFolder%\v%DocFXVersion%'"
   DEL "%DocFxFolder%\docfx_v%DocFXVersion%.zip" /Q
)
IF NOT EXIST "../Output/dotnet.xrefmap.json" (
   powershell -ExecutionPolicy ByPass -command "Invoke-WebRequest -Uri "https://github.com/dotnet/docfx/raw/main/.xrefmap.json" -OutFile '../Output/dotnet.xrefmap.json'"
)

ECHO SEARCHING FOR VISUAL STUDIO...
"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -prerelease -version [17.12,18.0) -sort -requires Microsoft.Component.MSBuild -products * -property InstallationPath > %TEMP%\vsinstalldir.txt
SET /p _VSINSTALLDIR=<%TEMP%\vsinstalldir.txt
ECHO VISUAL STUDIO FOUND IN: %_VSINSTALLDIR%
DEL %TEMP%\vsinstalldir.txt
IF "%_VSINSTALLDIR%"=="" (
  ECHO ERROR: VISUAL STUDIO NOT FOUND
  EXIT /B 1
)
IF "%VSINSTALLDIR%"=="" (
  CALL "%_VSINSTALLDIR%\Common7\Tools\VsDevCmd.bat"
)

msbuild /restore /t:Restore %~dp0../src/Toolkit/Toolkit.WPF/Esri.ArcGISRuntime.Toolkit.WPF.csproj /p:Configuration=Release
msbuild /restore /t:Restore %~dp0../src/Toolkit/Toolkit.WinUI/Esri.ArcGISRuntime.Toolkit.WinUI.csproj /p:Configuration=Release
msbuild /restore /t:Restore %~dp0../src/Toolkit/Toolkit.UWP/Esri.ArcGISRuntime.Toolkit.UWP.csproj /p:Configuration=Release
msbuild /restore /t:Restore %~dp0../src/Toolkit/Toolkit.Maui/Esri.ArcGISRuntime.Toolkit.Maui.csproj /p:Configuration=Release


REM Build the output site (HTML) from the generated metadata and input files (uses configuration in docfx.json in this folder)
%DocFxFolder%\v%DocFXVersion%\docfx.exe %~dp0\docfx.json
ECHO Fixing API Reference Links
powershell -ExecutionPolicy ByPass -command "%~dp0FixApiRefLinks" -Path %~dp0..\Output\docs_site\api\
start http://localhost:8080
%DocFxFolder%\v%DocFXVersion%\docfx.exe serve %~dp0..\Output\docs_site\

REM Publishing doc:
REM cd %~dp0..\Output\docs_site
REM git init
REM git add .
REM git commit -m "Update doc"
REM git push --force https://github.com/Esri/arcgis-maps-sdk-dotnet-toolkit.git main:gh-pages
