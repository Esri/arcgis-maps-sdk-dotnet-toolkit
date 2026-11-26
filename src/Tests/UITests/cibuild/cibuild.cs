using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var workspace = Path.GetFullPath(Helpers.GetRequiredEnvironmentPath("WORKSPACE"));
            using var appEnv = new AppiumEnvironment(workspace);

            if (args.Length < 1)
                throw new ArgumentException($"Must pass at least one argument (clean, run, install)");

            if (args[0] == "clean")
            {
                appEnv.Clean();
            }
            else if (args[0] == "run")
            {
                if (args.Length < 2)
                    throw new ArgumentException("Must pass a test platform after run (Wpf, WinUI, MauiWinUI, MauiAndroid, MauiMac, MauiiOS).");

                await RunTests(appEnv, workspace, args[1].Trim().ToLowerInvariant(), args);
            }
            else if (args[0] == "install")
            {
                if (args.Length < 2)
                    throw new ArgumentException($"Must pass workload id after install");

                await appEnv.Install(args[1]);
            }
            else
            {
                throw new ArgumentException($"Unknown first argument");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static async Task RunTests(AppiumEnvironment appEnv, string workspace, string testPlatform, string[] consoleArgs)
    {
        var workloadId = testPlatform switch
        {
            "wpf" or "winui" => "windows",
            "mauiwinui" => "maui-windows",
            "mauiandroid" => "maui-android",
            "mauimac" => "maui-mac",
            "mauiios" => "maui-ios",
            _ => throw new ArgumentException($"The test platform '{testPlatform}' was not recognized. Supported platforms are [Wpf, WinUI, MauiWinUI, MauiAndroid, MauiMac, MauiiOS].")
        };
        var apiKey = Helpers.GetRequiredEnvironmentVariable("ARCGIS_API_KEY");
        await appEnv.Install(workloadId);

        var buildSettings = testPlatform switch
        {
            "mauiandroid" => SetupAndroid(appEnv),
            "mauimac" => SetupMac(),
            "mauiios" => SetupiOS(),
            _ => SetupWindows(testPlatform, appEnv.YamlFilePath)
        };
        AppendPlatformIndependentBuildSettings(buildSettings, workspace, apiKey, consoleArgs);

        var startedCleanup = false;
        void ShutdownBuildServer(object? sender, EventArgs args)
        {
            if (startedCleanup)
                return;
            startedCleanup = true;
            try
            {
                Helpers.RunBinary(appEnv.DotnetExe, "build-server shutdown");
            }
            finally
            {
                if (testPlatform == "mauimac")
                    KillMacTestApp(buildSettings.BinaryName);
            }
        }

        Console.CancelKeyPress += ShutdownBuildServer;
        AppDomain.CurrentDomain.ProcessExit += ShutdownBuildServer;
        try
        {
            // Build app and runner
            Console.WriteLine("\nBuilding test app...");
            var uiTestsPath = Path.Join(appEnv.ToolkitSrc, "Tests", "UITests");
            var appPath = Path.Join(uiTestsPath, buildSettings.AppName, $"{buildSettings.AppName}.csproj");
            Helpers.RunBinary(appEnv.DotnetExe, ["build", appPath, .. buildSettings.BuildParamsCommon, .. buildSettings.BuildParamsApp]);

            Console.WriteLine("\nBuilding test runner...");
            var runnerPath = Path.Join(uiTestsPath, buildSettings.RunnerName, $"{buildSettings.RunnerName}.csproj");
            Helpers.RunBinary(appEnv.DotnetExe, ["build", runnerPath, .. buildSettings.BuildParamsCommon]);

            await appEnv.RunAppium(workloadId);

            // Run tests
            Console.WriteLine("\nRunning tests...");
            var artifactsPath = Path.Join(uiTestsPath, "artifacts", "bin");
            var runnerExtension = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
            var appExtension = testPlatform is "wpf" or "winui" or "mauiwinui" ? ".exe" : string.Empty;
            var runnerExe = Path.Join(artifactsPath, buildSettings.RunnerName, "TestBuild", $"{buildSettings.RunnerName}{runnerExtension}");
            var appExe = Path.Join(artifactsPath, buildSettings.AppName, "TestBuild", $"{buildSettings.BinaryName}{appExtension}");
            Environment.SetEnvironmentVariable("TKUITEST_APP", appExe);
            var startInfo = new ProcessStartInfo(runnerExe, buildSettings.TestParams)
            {
                // The tests seem to need to be run from the toolkit source root.
                WorkingDirectory = appEnv.ToolkitSrc
            };
            Helpers.RunBinary(startInfo);
        }
        finally
        {
            Console.CancelKeyPress -= ShutdownBuildServer;
            AppDomain.CurrentDomain.ProcessExit -= ShutdownBuildServer;
            ShutdownBuildServer(null, EventArgs.Empty);
        }
    }

    #region BuildSettings
    private static BuildSettings SetupAndroid(AppiumEnvironment appEnv)
    {
        var buildSettings = new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiAndroid");
        buildSettings.BuildParamsApp.AddRange([
            "-f", "net10.0-android",
            "-r", "android-arm64",
            $"-p:JavaSdkDirectory={Path.Join(appEnv.Workspace, "jdk")}",
            $"-p:AndroidSdkDirectory={Path.Join(appEnv.Workspace, "android-sdk")}"
        ]);
        buildSettings.BinaryName = "com.esri.toolkit.uitests.maui-Signed.apk";

        // Enforce vertical orientation since some tests fail on phones in landscape mode.
        var adb = Path.Join(appEnv.Workspace, "android-sdk", "platform-tools", OperatingSystem.IsWindows() ? "adb.exe" : "adb");
        Helpers.RunBinary(adb, ["shell", "settings", "put", "system", "accelerometer_rotation", "0"]);
        Helpers.RunBinary(adb, ["shell", "settings", "put", "system", "user-rotation", "0"]);
        return buildSettings;
    }

    private static BuildSettings SetupMac()
    {
        var buildSettings = new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiMac");
        buildSettings.BuildParamsApp.AddRange(["-f", "net10.0-maccatalyst", "-r", "maccatalyst-arm64"]);
        buildSettings.BinaryName = "Toolkit.UITests.Maui.App.app";
        return buildSettings;
    }

    private static BuildSettings SetupiOS()
    {
        var buildSettings = new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiiOS");
        buildSettings.BuildParamsApp.AddRange(["-f", "net10.0-ios", "-r", "ios-arm64"]);
        buildSettings.BuildParamsCommon.Add("-p:BuildApp=false");
        buildSettings.BinaryName = "Toolkit.UITests.Maui.App.app";
        return buildSettings;
    }

    private static void KillMacTestApp(string testAppPackage)
    {
        try
        {
            Console.WriteLine("\nKilling mac test app...");
            var processes = Helpers.RunBinary("ps", ["-o", "pid=,command=", "-e"], true);
            var matches = Regex.Matches(processes!.StandardOutput, @$"^\s*(\d+) .*{Regex.Escape(testAppPackage)}.*$", RegexOptions.Multiline);
            foreach (Match match in matches)
                Helpers.RunBinary("kill", [match.Groups[1].Value]);
            if (matches.Count == 0)
                Console.WriteLine($"No {testAppPackage} processes found. Skipping maui test app kill.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to kill Maui test app during cleanup. See below error.");
            Console.WriteLine(ex);
        }
    }

    private static BuildSettings SetupWindows(string testPlatform, string yamlConfig)
    {
        var buildSettings = testPlatform switch
        {
            "wpf" => new BuildSettings("Toolkit.UITests.Wpf.App", "Toolkit.UITests.Wpf"),
            "winui" => new BuildSettings("Toolkit.UITests.WinUI.App", "Toolkit.UITests.WinUI"),
            "mauiwinui" => new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiWinUI"),
            _ => throw new ArgumentException($"The test platform '{testPlatform}' was not recognized.")
        };

        if (testPlatform == "wpf")
        {
            buildSettings.BuildParamsCommon.Add("-p:BuildApp=false");
        }
        else if (testPlatform == "winui")
        {
            buildSettings.BuildParamsCommon.AddRange([
                "-r", "win-x64",
                "-p:SelfContained=true",
                "-p:PublishProfile=win-x64.pubxml",
                "-p:WindowsAppSDKSelfContained=true"
            ]);
        }
        else if (testPlatform == "mauiwinui")
        {
            buildSettings.BuildParamsCommon.AddRange([
                "-r", "win-x64", "-p:UseMonoRuntime=false", "-p:BuildApp=false"
            ]);
            var dotnetVersion = Helpers.ReadYamlValue(yamlConfig, "dotnet-version");
            var dotnetMajorVersion = Regex.Match(dotnetVersion, @"\d+\.\d+(?=\.)").Value;
            var windowsSdkVersion = Helpers.ReadYamlValue(yamlConfig, "windows-sdk-version");
            buildSettings.BuildParamsApp.AddRange([
                $"-p:MauiVersion={dotnetMajorVersion}*",
                "-f", $"net{dotnetMajorVersion}-windows{windowsSdkVersion}"
            ]);
        }

        return buildSettings;
    }

    private static void AppendPlatformIndependentBuildSettings(BuildSettings settings, string workspace, string apiKey, string[] consoleArgs)
    {
        // Universal build parameters for the ci builds
        settings.BuildParamsCommon.AddRange([
            "-c", "Release",
            "-p:ArtifactsPivots=TestBuild",
            "-p:UseArtifactsOutput=true"
        ]);

        // Release version config
        var releaseVersion = Environment.GetEnvironmentVariable("RELEASE_VERSION");
        if (!string.IsNullOrWhiteSpace(releaseVersion)) {
            settings.BuildParamsApp.Add($"-p:UseNugetPackage={releaseVersion}");
        }

        // Configure the trx output for ci jobs
        var testResultsDir = Path.Join(workspace, "TestResults");
        settings.TestParams.AddRange([
            "--report-trx",
            "--results-directory", testResultsDir
        ]);
        var trxFilename = Environment.GetEnvironmentVariable("TRX_FILENAME");
        if (!string.IsNullOrWhiteSpace(trxFilename)) {
            settings.TestParams.AddRange(["--report-trx-filename", trxFilename]);
        }

        // Ignore exit code 2 in test results since it just means that one or more tests failed
        // https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-troubleshooting
        settings.TestParams.AddRange(["--ignore-exit-code", "2"]);

        // Append filter from console arguments if it exists
        var filterArgIndex = consoleArgs.IndexOf("--filter");
        if (filterArgIndex > -1)
        {
            if (filterArgIndex + 1 >= consoleArgs.Length)
                throw new ArgumentException("Recieved '--filter' flag, but no argument was provided.");
            settings.TestParams.AddRange(["--filter", consoleArgs[filterArgIndex+1]]);
        }

        // API key
        settings.BuildParamsApp.Add($"-p:TestAppApiKey={apiKey}");
    }

    private class BuildSettings
    {
        private string? _binaryName;

        public List<string> BuildParamsCommon = new();
        public List<string> BuildParamsApp = new();
        public List<string> TestParams = new();
        public string AppName;
        public string RunnerName;

        /// <summary> Defaults to AppName if not explicitly set. </summary>
        public string BinaryName
        {
            get => _binaryName ?? AppName;
            set => _binaryName = value;
        }

        public BuildSettings(string appName, string runnerName)
        {
            AppName = appName;
            RunnerName = runnerName;
        }
    }
    #endregion BuildSettings
}

#region AppiumEnvironment
internal class AppiumEnvironment : IDisposable
{
    private const string _dependenciesInstallDir = ".uitest";
    private const string _stateFileName = "installed.json";

    // Variables for killing appium afterwards
    private Dictionary<string, InstalledDependency> _installed = new();
    private readonly List<string> _appiumStandardOutput = new();
    private readonly List<string> _appiumStandardError = new();
    private Process? _appiumProcess;
    private bool _disposed;

    public readonly string Workspace;
    public string DotnetExe = string.Empty;
    public string ToolkitSrc = string.Empty;
    public string YamlFilePath = string.Empty;

    public AppiumEnvironment(string workspace)
    {
        Workspace = Path.GetFullPath(Path.Join(workspace, _dependenciesInstallDir));
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
    }

    public async Task RunAppium(string workloadId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_appiumProcess != null)
            throw new InvalidOperationException("Appium has already been started.");

        var nodeExe = GetRequiredInfo(_installed, "node", "node-exe");
        var appiumEntry = GetRequiredInfo(_installed, "appium", "appium-entry");
        Console.WriteLine("\nStarting appium...");
        _appiumProcess = Helpers.RunBinaryBackground(
            nodeExe,
            $"\"{appiumEntry}\"",
            _appiumStandardOutput,
            _appiumStandardError
        );

        try
        {
            await WaitForAppiumReadyAsync("http://127.0.0.1:4723/status", TimeSpan.FromSeconds(30));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public async Task Install(string workloadId)
    {
            if ((workloadId is "windows" or "maui-windows") && !OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException($"Workload '{workloadId}' requires Windows.");
            if ((workloadId is "maui-mac" or "maui-ios") && !OperatingSystem.IsMacOS())
                throw new PlatformNotSupportedException($"Workload '{workloadId}' requires macOS.");

            var workloads = GetWorkloads();
            if (!workloads.ContainsKey(workloadId))
                throw new ArgumentException($"Workload \"{workloadId}\" not recognized");

            Directory.CreateDirectory(Workspace);
            var stateFile = Path.Join(Workspace, _stateFileName);
            if (File.Exists(stateFile))
                _installed = JsonSerializer.Deserialize(
                        File.ReadAllText(stateFile),
                        DependencySerializationContext.Default.DictionaryStringInstalledDependency
                    ) ?? new Dictionary<string, InstalledDependency>();
            else
                _installed = new();

            foreach (var dependency in workloads[workloadId])
            {
                if (_installed.TryGetValue(dependency.Name, out var existing))
                {
                    AssertVersion(dependency, existing, _installed);

                    Console.WriteLine($"\nRestoring dependency \"{dependency.Name}\"...");
                    dependency.Restore(existing);
                    continue;
                }

                try
                {
                    Console.WriteLine($"\nInstalling dependency \"{dependency.Name}\"...");
                    var newInstalled = await dependency.Install(_installed);
                    _installed[dependency.Name] = newInstalled;

                    // Save state after each successful install so it can be cleaned if a latter install breaks
                    var persistent = _installed.Where(pair => pair.Value.Serialize).ToDictionary();
                    var json = JsonSerializer.Serialize(persistent, DependencySerializationContext.Default.DictionaryStringInstalledDependency);
                    File.WriteAllText(stateFile + ".tmp", json);
                    File.Move(stateFile + ".tmp", stateFile, overwrite: true);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to install dependency '{dependency.Name}'.", ex);
                }
            }

            Console.WriteLine($"\nDependencies installed. Install state recorded in {stateFile}.");
            return;
    }

    public void Clean()
    {
        Console.WriteLine("Cleaning test environment...");
        var workload = GetWorkloads()["all"];
        var stateFile = Path.Join(Workspace, _stateFileName);
        if (File.Exists(stateFile))
        {
            var installed = JsonSerializer.Deserialize(File.ReadAllText(stateFile), DependencySerializationContext.Default.DictionaryStringInstalledDependency)
                ?? new Dictionary<string, InstalledDependency>();
            foreach (var dependency in workload.Reverse())
            {
                if (!installed.TryGetValue(dependency.Name, out var existing))
                    continue;
                try
                {
                    dependency.Clean(existing);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to clean dependency '{dependency.Name}'.", ex);
                }
            }
        }

        if (Directory.Exists(Workspace))
            Directory.Delete(Workspace, true);

        Console.WriteLine("Clean completed");
    }

    public IReadOnlyDictionary<string, IReadOnlyList<TestDependency>> GetWorkloads()
    {
        var paths = new RequiredPathsDependency(this);
        var node = new NodeDependency(this);
        var appium = new AppiumDependency(this);
        var winDriver = new AppiumDriverDependency(this, "wincore", "appium-wincore-driver");
        var androidDriver = new AppiumDriverDependency(this, "uiautomator2");
        var macDriver = new AppiumDriverDependency(this, "mac2");
        var iosDriver = new AppiumDriverDependency(this, "xcuitest");
        var nuget = new NugetDependency(this);
        var maui = new MauiDependency(this);
        var koffi = new KoffiDependency(this);
        var android = new AndroidDependency(this);
        var iosConfiguration = new IosConfigurationDependency(this);
        var wda = new WdaDependency(this);
        var workloads = new Dictionary<string, IReadOnlyList<TestDependency>>
        {
            ["windows"] = [paths, nuget, node, appium, winDriver],
            ["maui-windows"] = [paths, nuget, maui, node, appium, winDriver],
            ["maui-android"] = [paths, nuget, maui, node, appium, androidDriver, koffi, android],
            ["maui-mac"] = [paths, nuget, maui, node, appium, macDriver],
            ["maui-ios"] = [paths, iosConfiguration, nuget, maui, node, appium, iosDriver, wda],
            ["all"] = OperatingSystem.IsWindows()
                ? [paths, nuget, maui, node, appium, winDriver, androidDriver, koffi, android]
                : [paths, iosConfiguration, nuget, maui, node, appium, macDriver, iosDriver, wda, androidDriver, koffi, android]
        };
        return workloads.AsReadOnly();
    }

    #region Dependencies
    class RequiredPathsDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "required-paths";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var dependency = new InstalledDependency(serialize: false);
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public override void Restore(InstalledDependency installed)
        {
            string? directoryPath = AppContext.GetData("EntryPointFileDirectoryPath") as string;
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new InvalidOperationException("The entry-point file directory is unavailable. Run this script as a file-based app.");

            // By default use dotnet name unless provided a specific path
            AppEnvironment.DotnetExe = Environment.GetEnvironmentVariable("DOTNET_PATH") ?? string.Empty;
            if (string.IsNullOrEmpty(AppEnvironment.DotnetExe))
                AppEnvironment.DotnetExe = "dotnet";
            else if (!File.Exists(AppEnvironment.DotnetExe))
                throw new FileNotFoundException("The required dotnet exe path was not found.", AppEnvironment.DotnetExe);
            else
                AppEnvironment.DotnetExe = Path.GetFullPath(AppEnvironment.DotnetExe);

            Console.WriteLine(AppEnvironment.DotnetExe);

            AppEnvironment.ToolkitSrc = Path.GetFullPath(Path.Join(directoryPath, "..", "..", ".."));
            AppEnvironment.YamlFilePath = Path.Join(directoryPath, "variables.yml");
            if (!File.Exists(AppEnvironment.YamlFilePath))
                throw new FileNotFoundException("The required variables.yml configuration file was not found.", AppEnvironment.YamlFilePath);
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    class IosConfigurationDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "ios-configuration";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var dependency = new InstalledDependency(serialize: false);
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public override void Restore(InstalledDependency installed)
        {
            RequestEnvironmentVariable("TKUITEST_DEVICE");
            if (Environment.GetEnvironmentVariable("BUILD_WDA_MANUAL") == "true")
                return;

            RequestEnvironmentVariable("TKUITEST_PARAM_xcodeOrgId");
            RequestEnvironmentVariable("TKUITEST_PARAM_xcodeSigningId", "Apple Development");
            RequestEnvironmentVariable("TKUITEST_PARAM_updatedWDABundleId", "com.esri.wda");
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }

        private static void RequestEnvironmentVariable(string variableName, string? defaultValue = null)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName)))
                return;

            var value = defaultValue;
            if (value == null)
            {
                Console.Write($"Enter {variableName}: ");
                value = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException($"Environment variable '{variableName}' must be set or provided at the prompt.");
            }
            Environment.SetEnvironmentVariable(variableName, value);
        }
    }

    // Install Node.js
    class NodeDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "node";

        public override async Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            if (!TryGetVersion(installed, out var version) || string.IsNullOrWhiteSpace(version))
                throw new Exception("Could not get desired node.js version to install");

            var workspace = Path.Join(AppEnvironment.Workspace, ".node");
            var nodePlatform = OperatingSystem.IsWindows() ? "win-x64" : "darwin-arm64";
            var nodeRoot = $"node-v{version}-{nodePlatform}";
            var nodeDir = OperatingSystem.IsWindows() ? Path.Join(workspace, nodeRoot) : Path.Join(workspace, nodeRoot, "bin");
            var nodeExe = Path.Join(nodeDir, OperatingSystem.IsWindows() ? "node.exe" : "node");
            var npmExe = Path.Join(nodeDir, OperatingSystem.IsWindows() ? "npm.cmd" : "npm");
            if (!File.Exists(nodeExe))
            {
                Directory.CreateDirectory(workspace);
                var archiveExtension = OperatingSystem.IsWindows() ? ".zip" : ".tar.gz";
                var nodeUrl = $"https://nodejs.org/dist/v{version}/{nodeRoot}{archiveExtension}";
                var nodeArchive = Path.Join(workspace, $"node{archiveExtension}");

                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(nodeUrl);
                response.EnsureSuccessStatusCode();

                using (var output = File.Create(nodeArchive))
                {
                    await response.Content.CopyToAsync(output);
                }

                if (OperatingSystem.IsWindows())
                {
                    ZipFile.ExtractToDirectory(nodeArchive, workspace);
                }
                else
                {
                    using var archive = File.OpenRead(nodeArchive);
                    using var decompressor = new GZipStream(archive, CompressionMode.Decompress);
                    TarFile.ExtractToDirectory(decompressor, workspace, overwriteFiles: true);
                }
                Console.WriteLine($"Node.js installed at {nodeExe}");
            }
            else
                Console.WriteLine($"Found cached Node.js at {nodeExe}");
            var dependency = new InstalledDependency(version, new() { ["node-dir"] = nodeDir, ["node-exe"] = nodeExe, ["npm-exe"] = npmExe });
            Restore(dependency);
            return dependency;
        }

        public override void Restore(InstalledDependency installed)
        {
            var nodeDir = installed.Info!["node-dir"];
            var nodeExe = installed.Info["node-exe"];
            var npmExe = installed.Info["npm-exe"];
            foreach (var file in new[] { nodeExe, npmExe })
            {
                if (!File.Exists(file))
                    throw new FileNotFoundException("An installed dependency is missing. Run clean before reinstalling.", file);
            }
            var nodeExeDir = Path.GetDirectoryName(nodeExe);
            // Node sometimes calls itself base on path internally, so we cannot rely on returning the local variables
            Environment.SetEnvironmentVariable("PATH", $"{nodeExeDir}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}");
            Environment.SetEnvironmentVariable("npm_config_cache", Path.Join(nodeDir, "npm-cache"));
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
            => AppEnvironment.TryReadYamlVersion("node-version", out version);
    }

    // Install appium and driver
    class AppiumDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "appium";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var npmExe = AppiumEnvironment.GetRequiredInfo(installed, "node", "npm-exe");
            var workspace = Path.Join(AppEnvironment.Workspace, ".node");
            var appiumHome = Path.Join(AppEnvironment.Workspace, ".appium");
            Helpers.RunBinary(npmExe, ["install", "appium", "--prefix", workspace]);
            var dependency = new InstalledDependency(info: new()
            {
                ["appium-entry"] = Path.Join(workspace, "node_modules", "appium", "index.js"),
                ["appium-home"] = appiumHome
            });
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public override void Restore(InstalledDependency installed)
        {
            var appiumEntry = installed.Info!["appium-entry"];
            if (!File.Exists(appiumEntry))
                throw new FileNotFoundException("An installed dependency is missing. Run clean before reinstalling.", appiumEntry);
            Environment.SetEnvironmentVariable("APPIUM_HOME", installed.Info["appium-home"]);
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    class AppiumDriverDependency(AppiumEnvironment environment, string driverName, string? npmPackage = null) : TestDependency(environment)
    {
        public override string Name => $"appium-driver-{driverName}";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var nodeExe = AppiumEnvironment.GetRequiredInfo(installed, "node", "node-exe");
            var appiumEntry = AppiumEnvironment.GetRequiredInfo(installed, "appium", "appium-entry");
            InstallAppiumDriver(nodeExe, appiumEntry, driverName, npmPackage);
            return Task.FromResult(new InstalledDependency());
        }

        public override void Restore(InstalledDependency installed) { }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    // Install maui workload if required
    class MauiDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "maui";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            Collection<string> arguments = ["workload", "install", "maui"];
            _ = TryGetVersion(installed, out var version);
            if (version != null)
            {
                arguments.Add("--version");
                arguments.Add(version);
            }
            Helpers.RunBinary(AppEnvironment.DotnetExe, arguments);
            return Task.FromResult(new InstalledDependency(version));
        }

        public override void Restore(InstalledDependency installed) { }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            var workloadVersion = Environment.GetEnvironmentVariable("MAUI_WORKLOAD_VERSION");
            version = string.IsNullOrWhiteSpace(workloadVersion) ? null : workloadVersion;
            return true;
        }
    }

    class KoffiDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "koffi";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var npmExe = GetRequiredInfo(installed, "node", "npm-exe");
            Helpers.RunBinary(npmExe, ["install", "koffi", "--prefix", Path.Join(AppEnvironment.Workspace, ".node")]);
            return Task.FromResult(new InstalledDependency());
        }

        public override void Restore(InstalledDependency installed) { }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    class AndroidDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "android";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var jdkDirectory = Path.Join(AppEnvironment.Workspace, "jdk");
            var androidSdkDirectory = Path.Join(AppEnvironment.Workspace, "android-sdk");
            var appPath = Path.Join(AppEnvironment.ToolkitSrc, "Tests", "UITests", "Toolkit.UITests.Maui.App", "Toolkit.UITests.Maui.App.csproj");
            Console.WriteLine("\nInstalling android and java sdks...");
            Helpers.RunBinary(AppEnvironment.DotnetExe, [
                "build", appPath, "-t:InstallAndroidDependencies",
                "-p:AcceptAndroidSdkLicenses=true",
                "-f", "net10.0-android", "-r", "android-arm64",
                $"-p:JavaSdkDirectory={jdkDirectory}",
                $"-p:AndroidSdkDirectory={androidSdkDirectory}"
            ]);
            var dependency = new InstalledDependency(info: new()
            {
                ["java-home"] = jdkDirectory,
                ["android-home"] = androidSdkDirectory
            });
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public override void Restore(InstalledDependency installed)
        {
            foreach (var key in new[] { "java-home", "android-home" })
            {
                if (!Directory.Exists(installed.Info![key]))
                    throw new DirectoryNotFoundException($"An installed dependency is missing: '{installed.Info[key]}'. Run clean before reinstalling.");
            }
            Environment.SetEnvironmentVariable("JAVA_HOME", installed.Info!["java-home"]);
            Environment.SetEnvironmentVariable("ANDROID_HOME", installed.Info["android-home"]);
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    class WdaDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        private string WdaVersion => Helpers.ReadYamlValue(AppEnvironment.YamlFilePath, "wda-github-tag");

        public override string Name => "wda";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            _ = TryGetVersion(installed, out var version);
            if (version == null)
                return Task.FromResult(new InstalledDependency(serialize: false));

            Console.WriteLine("\nStarting manual WDA clone and build...");
            var developmentTeam = Helpers.GetRequiredEnvironmentVariable("DEVELOPMENT_TEAM");
            var provisioningProfile = Helpers.GetRequiredEnvironmentVariable("PROVISIONING_PROFILE_SPECIFIER");
            var bundleId = Environment.GetEnvironmentVariable("WDA_BUNDLE_ID") ?? "com.esri.wda";
            var wdaRoot = Path.Join(AppEnvironment.Workspace, "wda");
            if (Directory.Exists(wdaRoot))
                Directory.Delete(wdaRoot, true);
            Helpers.RunBinary("git", ["clone", "https://github.com/appium/WebDriverAgent.git", "--depth", "1", "--branch", WdaVersion, wdaRoot]);

            var wdaBuild = Path.Join(wdaRoot, "build");
            Helpers.RunBinary("xcodebuild", [
                "build-for-testing",
                "-project", Path.Join(wdaRoot, "WebDriverAgent.xcodeproj"),
                "-derivedDataPath", wdaBuild,
                "-scheme", "WebDriverAgentRunner",
                "-destination", $"generic/platform=iOS",
                $"DEVELOPMENT_TEAM={developmentTeam}",
                $"PRODUCT_BUNDLE_IDENTIFIER={bundleId}",
                $"PROVISIONING_PROFILE_SPECIFIER={provisioningProfile}",
                "CODE_SIGN_STYLE=Manual", "-quiet"
            ]);

            var wdaPath = Path.Join(wdaBuild, "Build", "Products", "Debug-iphoneos", "WebDriverAgentRunner-Runner.app");

            // The WebDriverAgent breaks if duplicate libraries are installed: https://appium.github.io/appium-xcuitest-driver/v13/guides/run-preinstalled-wda/#additional-requirement-for-real-devices-on-iostvos-17
            var frameworksPath = Path.Join(wdaPath, "Frameworks");
            if (Directory.Exists(frameworksPath))
            {
                foreach (var pattern in new[] { "XC*", "libXCTestSwiftSupport.dylib", "Testing.framework" })
                {
                    foreach (var path in Directory.GetFileSystemEntries(frameworksPath, pattern))
                    {
                        if (Directory.Exists(path))
                            Directory.Delete(path, true);
                        else
                            File.Delete(path);
                    }
                }
            }

            var dependency = new InstalledDependency(version, new()
            {
                ["wda-path"] = wdaPath,
                ["wda-bundle-id"] = bundleId
            });
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public override void Restore(InstalledDependency installed)
        {
            if (Environment.GetEnvironmentVariable("BUILD_WDA_MANUAL") != "true")
                return;

            var wdaPath = installed.Info!["wda-path"];
            if (!Directory.Exists(wdaPath))
                throw new DirectoryNotFoundException($"An installed dependency is missing: '{wdaPath}'. Run clean before reinstalling.");

            Environment.SetEnvironmentVariable("TKUITEST_PARAM_prebuiltWDAPath", wdaPath);
            Environment.SetEnvironmentVariable("TKUITEST_PARAM_usePreinstalledWDA", "true");
            Environment.SetEnvironmentVariable("TKUITEST_PARAM_updatedWDABundleId", installed.Info["wda-bundle-id"]);
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            if (Environment.GetEnvironmentVariable("BUILD_WDA_MANUAL") != "true")
                return true;

            // A signed build can only be reused with the same device and signing configuration.
            var configuration = string.Join('\0', [
                Helpers.GetRequiredEnvironmentVariable("DEVELOPMENT_TEAM"),
                Helpers.GetRequiredEnvironmentVariable("PROVISIONING_PROFILE_SPECIFIER"),
                Environment.GetEnvironmentVariable("WDA_BUNDLE_ID") ?? "com.esri.wda"
            ]);
            version = $"{WdaVersion}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configuration)))}";
            return true;
        }
    }

    /// <summary>Sets the nuget source to point to the folder where the ArcGIS nugets are stored (if provided)</summary>
    class NugetDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        private const string _nugetRepoEnvVar = "NUGET_REPO";
        private const string _unconfiguredVersion = "unconfigured-nuget-repo";
        private const string _configuredVersionPrefix = "configure-nuget-repo:";

        public override string Name => "nuget";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            // Only set up custom nuget config if path to specific SDK nugets is set
            var nugetRepo = Environment.GetEnvironmentVariable(_nugetRepoEnvVar);
            if (string.IsNullOrWhiteSpace(nugetRepo))
                return Task.FromResult(new InstalledDependency(version: _unconfiguredVersion));

            Helpers.RunBinary(AppEnvironment.DotnetExe, ["new", "nugetconfig", "--force", "-o", AppEnvironment.ToolkitSrc]);

            var configFile = Path.Join(AppEnvironment.ToolkitSrc, "nuget.config");
            Helpers.RunBinary(AppEnvironment.DotnetExe, ["nuget", "add", "source", nugetRepo, "--configfile", configFile]);

            // Workaround for the build machine package mapping changes as applied in samples repo
            var config = new XmlDocument();
            config.Load(configFile);
            var mappings = config.DocumentElement!.AppendChild(config.CreateElement("packageSourceMapping"));
            mappings!.AppendChild(config.CreateElement("clear"));
            config.Save(configFile);

            var nugetDir = Path.Join(AppEnvironment.ToolkitSrc, ".nuget");
            _ = TryGetVersion(installed, out var version);
            var dependency = new InstalledDependency(
                version: version,
                info: new()
                {
                    ["nuget-config"] = configFile,
                    ["nuget-packages"] = Path.Join(nugetDir, "packages"),
                    ["nuget-http-cache-path"] = Path.Join(nugetDir, "cache")
                },
                serialize: true
            );
            Restore(dependency);

            return Task.FromResult(dependency);
        }

        public override void Restore(InstalledDependency installed)
        {
            if (installed.Info == null)
                return;
            var configFile = installed.Info["nuget-config"];
            if (!File.Exists(configFile))
                throw new FileNotFoundException($"NuGet configuration for dependency '{Name}' is missing. Run clean before reinstalling.", configFile);
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", installed.Info["nuget-packages"]);
            Environment.SetEnvironmentVariable("NUGET_HTTP_CACHE_PATH", installed.Info["nuget-http-cache-path"]);
        }

        public override void Clean(InstalledDependency installed)
        {
            if (installed.Info == null)
                return;

            var packages = installed.Info["nuget-packages"];
            var cache = installed.Info["nuget-http-cache-path"];
            var configFile = installed.Info["nuget-config"];
            File.Delete(configFile);
            foreach (var directory in new[] { packages, cache })
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            // If the nuget repo was previously set to a different version, we probably want to do a full clean
            var nugetRepo = Environment.GetEnvironmentVariable(_nugetRepoEnvVar);
            if (!string.IsNullOrWhiteSpace(nugetRepo))
                version = $"{_configuredVersionPrefix}{nugetRepo}";
            else
                version = _unconfiguredVersion;
            return true;
        }
    }

    private static void InstallAppiumDriver(string nodeExe, string appiumEntry, string driverName, string? npmPackage = null)
    {
        Console.WriteLine($"\nInstalling appium {driverName} driver...");
        var installCheck = Helpers.RunBinary(nodeExe, [appiumEntry, "driver", "list", "--installed"], true);
        var driverInstalled = Regex.IsMatch(installCheck!.StandardError, Regex.Escape(driverName));
        if (driverInstalled)
            Helpers.RunBinary(nodeExe, [appiumEntry, "driver", "update", driverName]);
        else if (npmPackage != null)
            Helpers.RunBinary(nodeExe, [appiumEntry, "driver", "install", "--source=npm", npmPackage]);
        else
            Helpers.RunBinary(nodeExe, [appiumEntry, "driver", "install", driverName]);
    }

    private static void AssertVersion(TestDependency dependency, InstalledDependency existing, Dictionary<string, InstalledDependency> installed)
    {
        if (!dependency.TryGetVersion(installed, out var desiredVersion))
            throw new InvalidOperationException($"Could not determine expected version for dependency '{dependency.Name}'. Current version: '{existing.Version ?? "<untracked>"}'. See above logs.");

        if (desiredVersion != null && existing.Version != desiredVersion)
            throw new InvalidOperationException($"Version mismatch for dependency '{dependency.Name}': expected '{desiredVersion}', current '{existing.Version ?? "<untracked>"}'. Run clean before reinstalling.");
    }

    private static string GetRequiredInfo(IReadOnlyDictionary<string, InstalledDependency> installed, string dependency, string key)
    {
        if (!installed.TryGetValue(dependency, out var value) || value.Info == null || !value.Info.TryGetValue(key, out var info))
            throw new InvalidOperationException($"Dependency '{dependency}' must be installed before using '{key}'.");
        return info;
    }

    private bool TryReadYamlVersion(string name, out string? version)
    {
        try
        {
            version = Helpers.ReadYamlValue(YamlFilePath, name);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            version = null;
            return false;
        }
    }
    #endregion Dependencies

    #region Appium
    public static void KillAppium(Process appiumProcess, List<string> appiumStandardOutput, List<string> appiumStandardError)
    {
        Console.WriteLine("\nKilling appium...");

        appiumProcess.Kill(entireProcessTree: true);
        appiumProcess.WaitForExit();

        if (Environment.GetEnvironmentVariable("PRINT_APPIUM_LOGS")?.ToLower() == "true") {
            Console.WriteLine("Appium standard output logs:");
            foreach (var line in appiumStandardOutput) {
                Console.WriteLine(line);
            }
            Console.WriteLine("End appium standard output logs\n");
        }

        if (appiumStandardError.Count > 0) {
            Console.WriteLine("Appium standard error logs:");
            foreach (var line in appiumStandardError) {
                Console.WriteLine(line);
            }
            Console.WriteLine("End appium standard error logs");
        }
    }

    public static async Task WaitForAppiumReadyAsync(string statusUrl, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var body = await http.GetStringAsync(statusUrl);
                if (JsonNode.Parse(body)?["value"]?["ready"]?.GetValue<bool>() == true)
                    return;
            }
            catch { /* not up / not ready yet */ }
            await Task.Delay(500);
        }
        throw new Exception($"Appium did not become ready within {timeout.TotalSeconds:0}s.");
    }

    private void OnProcessExit(object? sender, EventArgs args) => Dispose();

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs args) => Dispose();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Console.CancelKeyPress -= OnCancelKeyPress;
        if (_appiumProcess != null)
        {
            try
            {
                KillAppium(_appiumProcess, _appiumStandardOutput, _appiumStandardError);
            }
            finally
            {
                _appiumProcess.Dispose();
            }
        }
    }
    #endregion Appium
}
#endregion AppiumEnvironment

#region Helpers
public class Helpers
{
    public static string GetRequiredEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Environment variable '{variableName}' must be set.");

        return value;
    }

    public static string GetRequiredEnvironmentPath(string variableName)
    {
        var path = Helpers.GetRequiredEnvironmentVariable(variableName);
        if (!Path.Exists(path))
            throw new ArgumentException($"Environment variable '{variableName}' must point to an existing path. Current value: '{path}'.");

        return path;
    }

    public static string ReadYamlValue(string yamlFile, string name) {
        using (StreamReader sr = File.OpenText(yamlFile)) {
            string? s;
            while ((s = sr.ReadLine()) != null) {
                s = s.Trim();
                if (s.StartsWith(name)) {
                    return s.Substring(name.Length+3, s.Length-name.Length-4);
                }
            }
        }
        throw new Exception($"Could not find variable {name} in {yamlFile}");
    }

    public static Process RunBinaryBackground(string binary, string arguments, List<string> standardOutput, List<string> standardError) {
        Console.WriteLine($"Running \"{binary} {arguments}\" in background");

        var startInfo = new ProcessStartInfo
        {
            FileName = binary,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = new Process();
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                standardOutput.Add(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                standardError.Add(e.Data);
            }
        };
        process.StartInfo = startInfo;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    public static BinaryOutput? RunBinary(string binary, Collection<string> arguments, bool captureStdOut = false, bool throwOnError = true) {
        Console.WriteLine($"Running {binary} {string.Join(" ", arguments)}");

        var startInfo = new ProcessStartInfo(binary, arguments);
        return Helpers.RunBinary(startInfo, captureStdOut, throwOnError);
    }

    public static BinaryOutput? RunBinary(string binary, string arguments, bool captureStdOut = false, bool throwOnError = true) {
        Console.WriteLine($"Running {binary} {arguments}");

        var startInfo = new ProcessStartInfo(binary, arguments);
        return Helpers.RunBinary(startInfo, captureStdOut, throwOnError);
    }

    public static BinaryOutput? RunBinary(ProcessStartInfo startInfo, bool captureOutput = false, bool throwOnError = true) {
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        using var process = new Process();
        process.StartInfo = startInfo;

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                if (captureOutput) { stdOut.Append(e.Data).Append('\n'); }
                else { Console.WriteLine(e.Data); }
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                if (captureOutput) { stdErr.Append(e.Data).Append('\n'); }
                else { Console.Error.WriteLine(e.Data); }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0 && throwOnError)
        {
            throw new Exception($"Call to {startInfo.FileName} failed with exit code {process.ExitCode}.");
        }

        return captureOutput ? new BinaryOutput(stdOut.ToString(), stdErr.ToString()) : null;
    }

}

public class BinaryOutput
{
    public string StandardOutput;
    public string StandardError;

    public BinaryOutput(string standardOut, string standardErr)
    {
        StandardOutput = standardOut;
        StandardError = standardErr;
    }
}

abstract class TestDependency(AppiumEnvironment environment)
{
    protected readonly AppiumEnvironment AppEnvironment = environment;
    public abstract string Name { get; }
    abstract public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed);
    abstract public void Restore(InstalledDependency installed);
    abstract public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version);
    public virtual void Clean(InstalledDependency installed) {}
}

class InstalledDependency(string? version = null, Dictionary<string, string>? info = null, bool serialize = true)
{
    public string? Version { get; set; } = version;
    public Dictionary<string, string>? Info { get; set; } = info;
    [JsonIgnore]
    public bool Serialize { get; set; } = serialize;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, InstalledDependency>))]
partial class DependencySerializationContext : JsonSerializerContext {}
#endregion Helpers

