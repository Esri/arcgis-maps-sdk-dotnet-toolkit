---
name: run-ui-tests
description: 'Run the Appium-based UI tests for this repository''s UITests projects. Use when testing coverage for a toolkit control'
---


## What this repo expects

- UI tests are Appium-driven MSTest projects.
- Shared test logic lives in `src\Tests\UITests\Toolkit.UITests.Shared\`.
- Shared test page code-behind lives in `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\`.
- Mirrored XAML pages live in:
  - `src\Tests\UITests\Toolkit.UITests.Wpf.App\TestPages\`
  - `src\Tests\UITests\Toolkit.UITests.WinUI.App\TestPages\`
  - `src\Tests\UITests\Toolkit.UITests.Maui.App\TestPages\`
- Test pages should derive from `Toolkit.UITests.App.TestPages.TestPage`.
- New pages are discovered automatically by reflection in each test app. There is no manual registration list. Keep the page in the `.TestPages.` namespace and make it derive from the correct platform base (`UserControl` on WPF/WinUI via `TestPage`, `ContentView` on MAUI via `TestPage`).

## First step: verify dependencies

Run these before trying to add or execute tests:

```powershell
node --version
npm --version
dotnet --info
appium --version
appium driver list --installed
```

If `appium` is missing, install it:

```powershell
npm install -g appium
```

Install only the drivers needed for the platform you are targeting:

```powershell
appium driver install windows
appium driver install uiautomator2
appium driver install xcuitest
appium driver install mac2
```

Validate the installed drivers:

```powershell
appium driver doctor windows
appium driver doctor uiautomator2
appium driver doctor xcuitest
appium driver doctor mac2
```

Additional setup notes from this repo:

- `Appium Inspector` is recommended for users to investigate element IDs and layout.
- Android (`uiautomator2`) may require `bundletool.jar` on `PATH`, and on Windows `.jar` may need to be present in `PATHEXT`.
- iOS setup is the hardest. Review `src\Tests\UITests\README.md` and `src\Tests\UITests\Directory.Build.props` for `iOSDeviceUdid`, WDA signing, and preinstalled WDA settings.
- Android and iOS device selection can be configured in `src\Tests\UITests\Directory.Build.props`.

Before running tests, start the Appium server in a separate terminal:

```powershell
appium
```

## Platform execution rules

Follow the repo's documented runner/app split:

- `Toolkit.UITests.Wpf`, `Toolkit.UITests.MauiWinUI`, and `Toolkit.UITests.MauiiOS` can build their apps automatically before running tests.
- `Toolkit.UITests.WinUI`, `Toolkit.UITests.MauiAndroid`, and `Toolkit.UITests.MauiMac` require the app to be built and launched manually first.
- `Toolkit.UITests.MauiAndroid` usually expects the MAUI app to already be installed because MAUI Android FastDeploy debug packages are not suitable for Appium.

Common runner commands:

```powershell
dotnet test --project src\Tests\UITests\Toolkit.UITests.Wpf\Toolkit.UITests.Wpf.csproj
dotnet test --project src\Tests\UITests\Toolkit.UITests.WinUI\Toolkit.UITests.WinUI.csproj
dotnet test --project src\Tests\UITests\Toolkit.UITests.MauiWinUI\Toolkit.UITests.MauiWinUI.csproj
dotnet test --project src\Tests\UITests\Toolkit.UITests.MauiAndroid\Toolkit.UITests.MauiAndroid.csproj
dotnet test --project src\Tests\UITests\Toolkit.UITests.MauiMac\Toolkit.UITests.MauiMac.csproj
dotnet test --project src\Tests\UITests\Toolkit.UITests.MauiiOS\Toolkit.UITests.MauiiOS.csproj
```

Use an MSTest filter while iterating:

```powershell
dotnet test --project src\Tests\UITests\Toolkit.UITests.Wpf\Toolkit.UITests.Wpf.csproj --filter "FullyQualifiedName~CompassTests"
```

