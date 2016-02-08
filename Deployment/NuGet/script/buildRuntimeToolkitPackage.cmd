setlocal EnableDelayedExpansion
@ECHO OFF

REM package metadata
SET VERSION=10.2.6
SET RUNTIMEID=Esri.ArcGISRuntime
SET ID=%RUNTIMEID%.Toolkit
SET TITLENOTE=
SET TYPE=
SET BUILDNUM=1026

REM source locations
SET WINSTORETOOLKITFOLDER=..\..\..\output\WinStore\VSIX
SET WINPHONETOOLKITFOLDER=..\..\..\output\WinPhone\VSIX
SET WINDESKTOPTOOLKITFOLDER=..\..\..\output\WinDesktop

REM intermediate build locations
SET PACKAGEFOLDER=..\..\..\output\intermediate\NuGet\toolkit_package_stage
SET EXTENSIONSDKFOLDER=..\..\..\output\intermediate\NuGet\toolkit_sdk_stage

REM output location
SET OUTPUTFOLDER=..\..\..\output\Common\NuGet\

REM Clean/create temp folder for package
ECHO deleting staging folders
RMDIR %PACKAGEFOLDER% /S /Q
RMDIR %EXTENSIONSDKFOLDER% /S /Q

xcopy ..\package %PACKAGEFOLDER%\ /S /Y

ECHO Generating %ID% %VERSION%.%BUILDNUM%%TYPE% %TITLENOTE%

ECHO
ECHO Unzipping extension SDK VSIX for store and phone...
ECHO
REM Unpack Extension SDKs
7za.exe x -o%EXTENSIONSDKFOLDER%\%VERSION%\win81\ %WINSTORETOOLKITFOLDER%\Esri.ArcGISRuntime.Toolkit.WS.vsix 
7za.exe x -o%EXTENSIONSDKFOLDER%\%VERSION%\wpa81\ %WINPHONETOOLKITFOLDER%\Esri.ArcGISRuntime.Toolkit.WP.vsix

ECHO
ECHO Copying files from extension SDK to package staging area for store and phone...
ECHO

REM ==========================
REM Copy Windows Store files
REM ==========================

REM copy neutral files to all three platforms
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\Redist\CommonConfiguration\neutral %PACKAGEFOLDER%\sdk\win81\ARM\ /S /Y
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\Redist\CommonConfiguration\neutral %PACKAGEFOLDER%\sdk\win81\x64\ /S /Y
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\Redist\CommonConfiguration\neutral %PACKAGEFOLDER%\sdk\win81\x86\ /S /Y

REM copy Generic.xaml to all platforms
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\DesignTime\CommonConfiguration\neutral\Themes\Generic.xaml %PACKAGEFOLDER%\sdk\win81\ARM\Esri.ArcGISRuntime.Toolkit.WindowsStore\Themes\ /S /Y
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\DesignTime\CommonConfiguration\neutral\Themes\Generic.xaml %PACKAGEFOLDER%\sdk\win81\x64\Esri.ArcGISRuntime.Toolkit.WindowsStore\Themes\ /S /Y
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\DesignTime\CommonConfiguration\neutral\Themes\Generic.xaml %PACKAGEFOLDER%\sdk\win81\x86\Esri.ArcGISRuntime.Toolkit.WindowsStore\Themes\ /S /Y

REM copy ARM files to ARM package staging
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\References\CommonConfiguration\ARM %PACKAGEFOLDER%\sdk\win81\ARM\ /S /Y

REM copy x64 files to x64 package staging
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\References\CommonConfiguration\x64 %PACKAGEFOLDER%\sdk\win81\x64\ /S /Y

REM copy x86 files to x86 package staging
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\win81\References\CommonConfiguration\x86 %PACKAGEFOLDER%\sdk\win81\x86\ /S /Y

REM ==========================
REM Copy Windows Phone files
REM ==========================

REM copy neutral files to all platforms
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\Redist\CommonConfiguration\neutral %PACKAGEFOLDER%\sdk\wpa81\ARM\ /S /Y
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\Redist\CommonConfiguration\neutral %PACKAGEFOLDER%\sdk\wpa81\x86\ /S /Y

REM copy design assembly
REM xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\DesignTime\CommonConfiguration\x86 %PACKAGEFOLDER%\sdk\wpa81\x86\ /S /Y

REM copy Generic.xaml to all platforms
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\DesignTime\CommonConfiguration\neutral\Esri.ArcGISRuntime.Toolkit.WindowsPhone\Themes\Generic.xaml %PACKAGEFOLDER%\sdk\wpa81\ARM\Esri.ArcGISRuntime.Toolkit.WindowsPhone\Themes\ /S /Y
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\DesignTime\CommonConfiguration\neutral\Esri.ArcGISRuntime.Toolkit.WindowsPhone\Themes\Generic.xaml %PACKAGEFOLDER%\sdk\wpa81\x86\Esri.ArcGISRuntime.Toolkit.WindowsPhone\Themes\ /S /Y

REM copy ARM files to ARM package staging
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\References\CommonConfiguration\ARM %PACKAGEFOLDER%\sdk\wpa81\ARM\ /S /Y

REM copy x86 files to x86 package staging
xcopy %EXTENSIONSDKFOLDER%\%VERSION%\wpa81\References\CommonConfiguration\x86 %PACKAGEFOLDER%\sdk\wpa81\x86\ /S /Y

REM ==================================================
REM Copy xr.xml files that aren't in extension SDK
REM ==================================================

REM copy xr.xml file to all Windows Store platforms
xcopy %WINSTORETOOLKITFOLDER%\..\ARM\Release\Esri.ArcGISRuntime.Toolkit.WindowsStore\Esri.ArcGISRuntime.Toolkit.WindowsStore.xr.xml %PACKAGEFOLDER%\sdk\win81\ARM\Esri.ArcGISRuntime.Toolkit.WindowsStore\ /S /Y
xcopy %WINSTORETOOLKITFOLDER%\..\x86\Release\Esri.ArcGISRuntime.Toolkit.WindowsStore\Esri.ArcGISRuntime.Toolkit.WindowsStore.xr.xml %PACKAGEFOLDER%\sdk\win81\x86\Esri.ArcGISRuntime.Toolkit.WindowsStore\ /S /Y
xcopy %WINSTORETOOLKITFOLDER%\..\x64\Release\Esri.ArcGISRuntime.Toolkit.WindowsStore\Esri.ArcGISRuntime.Toolkit.WindowsStore.xr.xml %PACKAGEFOLDER%\sdk\win81\x64\Esri.ArcGISRuntime.Toolkit.WindowsStore\ /S /Y

REM copy xr.xml file to all Windows Phone platforms
xcopy %WINPHONETOOLKITFOLDER%\..\ARM\Release\Esri.ArcGISRuntime.Toolkit.WindowsPhone\Esri.ArcGISRuntime.Toolkit.WindowsPhone.xr.xml %PACKAGEFOLDER%\sdk\wpa81\ARM\Esri.ArcGISRuntime.Toolkit.WindowsPhone\ /S /Y
xcopy %WINPHONETOOLKITFOLDER%\..\x86\Release\Esri.ArcGISRuntime.Toolkit.WindowsPhone\Esri.ArcGISRuntime.Toolkit.WindowsPhone.xr.xml %PACKAGEFOLDER%\sdk\wpa81\x86\Esri.ArcGISRuntime.Toolkit.WindowsPhone\ /S /Y

REM =================
REM DESKTOP STAGING
REM =================

xcopy %WINDESKTOPTOOLKITFOLDER%\Release\Esri.ArcGISRuntime.Toolkit.dll %PACKAGEFOLDER%\sdk\net45\ /S /Y
xcopy %WINDESKTOPTOOLKITFOLDER%\Release\Esri.ArcGISRuntime.Toolkit.xml %PACKAGEFOLDER%\sdk\net45\ /S /Y

REM rename .targets files to match package ID
rename %PACKAGEFOLDER%\build\win81\Esri.ArcGISRuntime.Toolkit.targets %ID%.targets
rename %PACKAGEFOLDER%\build\wpa81\Esri.ArcGISRuntime.Toolkit.targets %ID%.targets
rename %PACKAGEFOLDER%\build\net45\Esri.ArcGISRuntime.Toolkit.targets %ID%.targets

REM ===================
REM Generate package
REM ===================

NuGet.exe pack %PACKAGEFOLDER%\ArcGISRuntimeSDKToolkit.nuspec -properties version=%VERSION%.%BUILDNUM%%TYPE% -properties id=%ID% -properties TITLENOTE="%TITLENOTE%" -properties runtimeid=%RUNTIMEID%

REM Move package to output location
xcopy %ID%.%VERSION%.%BUILDNUM%%TYPE%.nupkg %OUTPUTFOLDER% /S /Y
del %ID%.%VERSION%.%BUILDNUM%%TYPE%.nupkg /F /Q