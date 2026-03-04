# .NET Toolkit UI Tests
The toolkit's UI Tests use Appium to simulate gestures on all six supported platforms (WPF, WinUI, Maui Android, Maui iOS, and Maui Mac Catalyst).

## Structure
### Test Runner Projects
Each platform has its own test runner project with the naming convention `Toolkit.UITests.Platform`. Each test project is an MSTest SDK application that can run either in its own console or using VSTest.

Almost all code, including individual tests, is shared between the projects in the [`Toolkit.UITests.Shared`](./Toolkit.UITests.Shared) library. Only minimal setup code is present in the projects themselves.

The test projects must be run from either a Windows or Mac machine with appium installed. See the below table for details.
| Required Platform | Runners |
| --- | --- |
| Windows | `.MauiAndroid`, `.MauiWinUI`, `.WinUI`, `.WPF` |
| Mac | `.MauiiOS`, `.MauiMac` |

### Test App Projects
Each framework (WPF, WinUI, and Maui) has a test app project with the naming convention `Toolkit.UITests.Framework.App`. Test apps make use of the ArcGIS Maps SDK for .NET Toolkit and are manipulated by the test runners during test execution.

Each app contains a collection of mirrored test pages. Xaml files are necessarily unique to each framework, but code-behinds are shared in the [`Toolkit.UITests.TestPages.Shared`](./Toolkit.UITests.TestPages.Shared) library.


## Setting Up the Tests
Appium must be installed and running on the machine before executing any tests.

See the appium documentation for instructions installing appium and drivers: https://appium.io/docs/en/latest/quickstart/install/

### Drivers
The following drivers are needed to run the tests:
| Platforms | Driver Name | Links |
| --- | --- | --- |
| WPF, WinUI, Maui WinUI | windows | GitHub: https://github.com/appium/appium-windows-driver |
| Maui Android | uiautomator2 | GitHub: https://github.com/appium/appium-uiautomator2-driver |
| Maui iOS | xcuitest | GitHub: https://github.com/appium/appium-xcuitest-driver <br>Documentation: https://appium.github.io/appium-xcuitest-driver/latest/ |
| Maui MacCatalyst | mac2 | GitHub: https://github.com/appium/appium-mac2-driver |

Use `appium driver install <drivername>` to install drivers. Check installations using `appium driver doctor <drivername>`.

#### uiautomator2
To install the bundletool.jar dependency on windows you must make sure it is in `PATH` and add `.jar` to `PATHEXT`. You may also need to `chmod +x` the file.

#### xcuitest
XCUITest can be an involved setup. Some of the install settings listed in the documentation are configurable in the tests. See below.


## Running the Tests
Before running any tests, first open a terminal on the host machine and start appium. (This is done by executing the `appium` command with no arguments.)

For most platforms, building and running a test runner on the host machine will also build and run the test app automatically before execution of the tests. MauiAndroid is the one exception, and is discussed in more detail below.

The other outlier is MauiiOS which requires additional configuration before tests can run.

### MauiAndroid
In Debug mode Maui Android uses a FastDeploy process which skips embedding some assemblies into the generated APK. Appium cannot install these APKs, so instead the Maui test app must first be run (and therefore installed) on the target device using the dotnet commandline or visual studio. This must be done before starting the `.MauiAndroid` test runner, and must be done manually any time a new change needs testing.

> Note: The [`UITests/Directory.Build.props`](./Directory.Build.props) file includes an option to not use a preinstalled app (set `AndroidUsePreinstalledApp` to `false` or nothing). This will cause the `.MauiAndroid` build process to attempt to build the `.Maui.App` app in `Release` mode before tests run. This is not recommended as the `Release` build is slow, appears to do a full rebuild each time, and may not work at all if `.MauiAndroid` itself is being built in `Debug`.

### MauiiOS
XCUITest has a somewhat involved setup process as documented on their wiki: https://appium.github.io/appium-xcuitest-driver/latest/preparation/. The `.MauiiOS` test runner supports both automatic WDA setup and preinstalled WDA setup configs (though with a limited number of supported options). These options can be configured in the [`UITests/Directory.Build.props`](./Directory.Build.props) file.


## Contributing
### Guidelines
The intent of the UITests is to allow as much code as possible to be shared between frameworks and platforms. Towards this goal, test pages created for the app projects should be as similar as possible. The recommended way to create a test page is to use the cross-framework [`TestPage`](./Toolkit.UITests.TestPages.Shared/TestPage.cs) class. This class allows xaml pages from different platforms to use the same code-behind, which helps all three test apps stay in sync. You can refer to existing test pages such as [`CompassMap.xaml.cs`](./Toolkit.UITests.TestPages.Shared/CompassMap.xaml.cs) to see how the class is used.

Try to keep tests small with few steps. On some platforms in particular the lag between an instruction being sent by the test runner, translated by the appium server, sent to the test app, sent back to appium, and finally sent back to the runner can be quite long.

We avoid doing direct screenshot comparisons in favor of light image analysis like blob counting. The [ImageMagick](https://imagemagick.org/) library (see [here](https://github.com/dlemstra/Magick.NET) for the .NET-specific repo) is included in the test runner projects for use in custom analyses. If you see a similar analysis being used by multiple separate tests consider consolidating it as a single function in a new image comparison class.

### Platform-Specific Code
Platform-specific code can be achieved by either
1. Using preprocessor constants in one of the shared libraries, or
2. Creating a file in a non-shared project
   - Platform-specific xaml files are a great example of this

Custom preprocessor constants include the following.
| Project Type | Constants |
|--- | --- |
| Runners | `WINDOWS_RUNNER`, `MAC_RUNNER`, `WPF_TEST`, `WINUI_TEST`, `MAUI_TEST`, `WINDOWS_TEST`, `ANDROID_TEST`, `MAC_TEST`, `IOS_TEST` |
| Apps | `WPF_APP`, `WINUI_APP`, `MAUI_APP` (Maui also provides its own automatic constants for different platforms) |