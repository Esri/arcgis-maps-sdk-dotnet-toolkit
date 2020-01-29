@ECHO OFF

SET DocFXVersion=2.48.1
SET DocFxFolder=%~dp0..\.tools\docfx

REM Download DocFx

IF NOT EXIST "%DocFxFolder%\v%DocFXVersion%\docfx.exe" (
   MKDIR "%DocFXFolder%\v%DocFXVersion%"
   powershell -ExecutionPolicy ByPass -command "Invoke-WebRequest -Uri "https://github.com/dotnet/docfx/releases/download/v%DocFXVersion%/docfx.zip" -OutFile '%DocFxFolder%\docfx_v%DocFXVersion%.zip'"
   powershell -ExecutionPolicy ByPass -command "Expand-Archive -LiteralPath '%DocFxFolder%\docfx_v%DocFXVersion%.zip' -DestinationPath '%DocFxFolder%\v%DocFXVersion%'"
   DEL "%DocFxFolder%\docfx_v%DocFXVersion%.zip" /Q
)
REM Build the output site (HTML) from the generated metadata and input files (uses configuration in docfx.json in this folder)
%DocFxFolder%\v%DocFXVersion%\docfx.exe %~dp0\docfx.json --serve
