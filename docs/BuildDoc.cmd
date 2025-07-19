@ECHO OFF

SET DocFXVersion=2.78.3
SET DocFxFolder=%~dp0..\.tools\docfx

REM Download DocFx

IF NOT EXIST "%DocFxFolder%\docfx.exe" (
   MKDIR "%DocFXFolder%"
   powershell -ExecutionPolicy ByPass -command "Invoke-WebRequest -Uri "https://github.com/dotnet/docfx/releases/download/v%DocFXVersion%/docfx-win-x64-v%DocFXVersion%.zip" -OutFile '%DocFxFolder%\docfx.zip'"
   powershell -ExecutionPolicy ByPass -command "Expand-Archive -LiteralPath '%DocFxFolder%\docfx.zip' -DestinationPath '%DocFxFolder%'"
   DEL "%DocFxFolder%\docfx.zip" /Q
)
REM Build the output site (HTML) from the generated metadata and input files (uses configuration in docfx.json in this folder)
%DocFxFolder%\docfx.exe %~dp0\docfx.json
ECHO Fixing API Reference Links
powershell -ExecutionPolicy ByPass -command "%~dp0FixApiRefLinks" -Path %~dp0..\Output\docs_site\api\
start http://localhost:8080
%DocFxFolder%\docfx.exe serve %~dp0..\Output\docs_site\

REM Publishing doc:
REM cd %~dp0..\Output\docs_site
REM git init
REM git add .
REM git commit -m "Update doc"
REM git push --force https://github.com/Esri/arcgis-maps-sdk-dotnet-toolkit.git main:gh-pages
