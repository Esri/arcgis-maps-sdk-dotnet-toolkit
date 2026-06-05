using System.IO.Compression;
using System.Diagnostics;
using System.Formats.Tar;
using System.Text.Json.Nodes;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Text;

internal class Program
{
    // Supported test platforms
    private static string[] _testPlatforms = { "MauiAndroid", "MauiMac" };

#region CleanupEvent
    private static event EventHandler? BuildEnding;
    private static Lock _cleanupLock = new Lock();
    private static bool _startedCleanup = false;
    private static void OnBuildEnding()
    {
        lock(_cleanupLock)
        {
            if (!_startedCleanup)
            {
                Console.WriteLine("\nStarting build cleanup...");
                _startedCleanup = true;
                BuildEnding?.Invoke(null, new EventArgs());
            }
        }
    }
#endregion

    static async Task<int> Main(string[] args)
    {
        // Handle CTRL+C gracefully
        Console.CancelKeyPress += (sender, args) => OnBuildEnding();

        // Required inputs
        if (args.Length < 1)
            throw new ArgumentException($"A test platform must be passed as the first and only command line argument. Supported platforms are [{string.Join(", ", _testPlatforms)}].");
        var testPlatform = args[0].Trim();
        if (!_testPlatforms.Contains(testPlatform))
            throw new ArgumentException($"Test platform '{testPlatform}' not recognized. Supported platforms are [{string.Join(", ", _testPlatforms)}].");

        var workspace = Environment.GetEnvironmentVariable("WORKSPACE");
        var dotnetExe = Environment.GetEnvironmentVariable("DOTNET_PATH");
        var toolkitSrc = Environment.GetEnvironmentVariable("TOOLKIT_SRC");
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrEmpty(dotnetExe) || string.IsNullOrEmpty(toolkitSrc)) {
            throw new ArgumentException("Environment variables WORKSPACE, DOTNET_PATH, and TOOLKIT_SRC must all be set.");
        }
        if (!Path.Exists(workspace) || !Path.Exists(dotnetExe) || !Path.Exists(toolkitSrc)) {
            throw new ArgumentException("Workspace and dotnet directory must be existing paths.");
        }

        // Derived variables
        var yamlConfig = Path.Join(toolkitSrc, "Tests", "UITests", "cibuild", "variables.yml");
        var dependencies = new CommonDependencies(dotnetExe);

        try {
            // Ensure dotnet will always shut down on failure
            BuildEnding += (_,_) => RunBinary(dotnetExe, "build-server shutdown");

            // Configure nuget repo if set
            var nugetRepo = Environment.GetEnvironmentVariable("NUGET_REPO");
            if (!string.IsNullOrWhiteSpace(nugetRepo)) {
                SetNugetSource(toolkitSrc, dependencies.DotnetExe, nugetRepo);
            }

            // Install node
            var nodeWorkspace = Path.Join(workspace, ".node");
            var nodeVersion = ReadYamlValue(yamlConfig, "node-version");
            (dependencies.NodeExe, dependencies.NpmExe) = await InstallNodeAsync(nodeWorkspace, nodeVersion);
            dependencies.AppiumEntry = Path.Join(nodeWorkspace, "node_modules", "appium", "index.js");

            // Install appium
            Console.WriteLine("\nInstalling appium...");
            Environment.SetEnvironmentVariable("APPIUM_HOME", Path.Join(workspace, ".appium"));
            RunBinary(dependencies.NpmExe, ["install", "appium", "--prefix", nodeWorkspace]);

            // Platform-specific setup
            BuildSettings buildSettings = testPlatform switch
            {
                "MauiAndroid" => SetupAndroid(dependencies, nodeWorkspace, toolkitSrc, workspace),
                "MauiMac" => SetupMac(dependencies, workspace),
                _ => throw new ArgumentException($"The test platform '{testPlatform}' was not recognized. Aborting tests.")
            };

            // Build app and runner
            Console.WriteLine("\nBuilding test app...");
            var uiTestsPath = Path.Join(toolkitSrc, "Tests", "UITests");
            var appPath = Path.Join(uiTestsPath, buildSettings.AppName, $"{buildSettings.AppName}.csproj");
            RunBinary(dependencies.DotnetExe, $"build {appPath} {string.Join(" ", buildSettings.BuildParamsCommon)} {string.Join(" ", buildSettings.BuildParamsApp)}");

            Console.WriteLine("\nBuilding test runner...");
            var runnerPath = Path.Join(uiTestsPath, buildSettings.RunnerName, $"{buildSettings.RunnerName}.csproj");
            RunBinary(dependencies.DotnetExe, $"build {runnerPath} {string.Join(" ", buildSettings.BuildParamsCommon)}");

            // Run appium in background
            Console.WriteLine("\nStarting appium...");
            var appiumStandardOutput = new List<string>();
            var appiumStandardError = new List<string>();
            var appiumProcess = RunBinaryBackground(
                dependencies.NodeExe,
                dependencies.AppiumEntry,
                appiumStandardOutput,
                appiumStandardError
            );
            await WaitForAppiumReadyAsync("http://127.0.0.1:4723/status", TimeSpan.FromSeconds(30));
            BuildEnding += (_,_) => KillAppium(appiumProcess, appiumStandardOutput, appiumStandardError);

            // Run tests
            Console.WriteLine("\nRunning tests...");
            var artifactsPath = Path.Join(uiTestsPath, "artifacts", "bin");
            var runnerExe = Path.Join(artifactsPath, buildSettings.RunnerName, "TestBuild", buildSettings.RunnerName);
            var appExe = Path.Join(artifactsPath, buildSettings.AppName, "TestBuild", buildSettings.BinaryName);
            Environment.SetEnvironmentVariable("TKUITEST_APP", appExe);
            RunBinary(runnerExe, $"{string.Join(" ", buildSettings.TestParams)}");
        }
        finally {
            OnBuildEnding();
        }
        
        return 0;
    }

#region CommonDependencies
    private static void SetNugetSource(string workspace, string dotnetExe, string nugetRepo)
    {
        Console.WriteLine("\nConfiguring nuget...");

        RunBinary(dotnetExe, ["new", "nugetconfig", "--force", "-o", workspace]);

        var configFile = Path.Join(workspace, "nuget.config");
        RunBinary(dotnetExe, ["nuget", "add", "source", nugetRepo, "--configfile", configFile]);

        var nugetDir = Path.Join(workspace, ".nuget");
        Environment.SetEnvironmentVariable("NUGET_PACKAGES", Path.Join(nugetDir, "packages"));
        Environment.SetEnvironmentVariable("NUGET_HTTP_CACHE_PATH", Path.Join(nugetDir, "cache"));

        Console.WriteLine("Done configuring nuget.\n");
    }

    private static string ReadYamlValue(string yamlFile, string name) {
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

    private static async Task<(string NodeExe, string NpmExe)> InstallNodeAsync(string workspace, string nodeVersion)
    {
        Console.WriteLine("\nStarting Node.js install...");

        var nodeDir = Path.Combine(workspace, $"node-v{nodeVersion}-darwin-arm64/bin");
        var nodeExe = Path.Combine(nodeDir, "node");
        var npmExe = Path.Combine(nodeDir, "npm");

        if (!File.Exists(nodeExe))
        {
            Directory.CreateDirectory(workspace);
            var nodeUrl = $"https://nodejs.org/dist/v{nodeVersion}/node-v{nodeVersion}-darwin-arm64.tar.gz";
            var nodeTarGz = Path.Combine(workspace, "node.tar.gz");

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(nodeUrl);
            response.EnsureSuccessStatusCode();

            using (var output = File.Create(nodeTarGz))
            {
                await response.Content.CopyToAsync(output);
            }

            var nodeTar = Path.Combine(workspace, "node.tar");
            {
                using var gzStream = File.Open(nodeTarGz, FileMode.Open);
                using var tarStream = File.Create(nodeTar);
                using var decompressor = new GZipStream(gzStream, CompressionMode.Decompress);
                decompressor.CopyTo(tarStream);
            }
            TarFile.ExtractToDirectory(nodeTar, workspace, true);

            Console.WriteLine($"Node.js installed at {nodeExe}");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"Found cached Node.js at {nodeExe}");
            Console.WriteLine();
        }

        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        Environment.SetEnvironmentVariable("PATH", $"{nodeDir}{Path.PathSeparator}{currentPath}");

        return (nodeExe, npmExe);
    }

    private class CommonDependencies
    {
        public CommonDependencies(string dotnetExe)
        {
            DotnetExe = dotnetExe;
        }

        public string DotnetExe { get; }
        public string NodeExe { get; set; } = string.Empty;
        public string NpmExe { get; set; } = string.Empty;
        public string AppiumEntry { get; set; } = string.Empty;
    }
#endregion

#region PlatformDependencies
    private static BuildSettings SetupMac(CommonDependencies dependencies, string workspace)
    {
        // Define build settings
        var buildSettings = new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiMac");
        var macFramework = "net10.0-maccatalyst";
        buildSettings.BuildParamsApp.AddRange([
            $"-f {macFramework}",
            $"-p:TargetFrameworks={macFramework}",
            "-r maccatalyst-arm64"
        ]);
        var testAppPackage = "Toolkit.UITests.Maui.App.app";
        buildSettings.BinaryName = testAppPackage;
        AppendPlatformIndependentBuildSettings(buildSettings, workspace);

        // Install maui maccatalyst workload
        Console.WriteLine("\nInstalling maui maccatalyst workload...");
        RunBinary(dependencies.DotnetExe, "workload install maui-maccatalyst");

        // Install appium mac driver
        InstallAppiumDriver(dependencies, "mac2");

        // App cleanup since it doesn't seem to be included in the appium process tree for mac
        BuildEnding += (_,_) =>
        {
            try
            {
                Console.WriteLine("\nKilling mac test app...");
                var listProcesses = RunBinary("ps", $"-o pid=,command= -e", true, false);
                var matches = Regex.Matches(listProcesses!.StandardOutput, @$"^(\d+) .*{Regex.Escape(testAppPackage)}.*$", RegexOptions.Multiline);
                if (matches.Count < 1)
                {
                    Console.WriteLine($"No {testAppPackage} processes found. Skipping maui test app kill.");
                    return;
                }
                foreach (Match match in matches)
                {
                    var pid = match.Groups[1].Value;
                    RunBinary("kill", pid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to kill Maui test app during cleanup. See below error.");
                Console.WriteLine(ex);
            }
        };

        return buildSettings;
    }

    private static BuildSettings SetupAndroid(CommonDependencies dependencies, string nodeWorkspace, string toolkitSrc, string workspace)
    {
        var jdkDirectory = $"{workspace}/jdk";
        var androidSdkDirectory = $"{workspace}/android-sdk";

        // Define build settings
        var buildSettings = new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiAndroid");
        var androidFramework = "net10.0-android";
        buildSettings.BuildParamsApp.AddRange([
            $"-f {androidFramework}",
            $"-p:TargetFrameworks={androidFramework}",
            "-r android-arm64",
            $"-p:JavaSdkDirectory={jdkDirectory}",
            $"-p:AndroidSdkDirectory={androidSdkDirectory}"
        ]);
        buildSettings.BinaryName = "com.esri.toolkit.uitests.maui-Signed.apk";
        AppendPlatformIndependentBuildSettings(buildSettings, workspace);

        // Install maui android
        Console.WriteLine("\nInstalling maui android workload...");
        RunBinary(dependencies.DotnetExe, "workload install maui-android");

        // Install appium android driver
        InstallAppiumDriver(dependencies, "uiautomator2");

        // Manually install koffi to avoid appium using ffi, which would require us to also have Visual Studio installed
        RunBinary(dependencies.NpmExe, ["install", "koffi", "--prefix", nodeWorkspace]);

        // Install jdk and android sdk
        Console.WriteLine("\nInstalling android and java sdks...");
        var appPath = Path.Join(toolkitSrc, "Tests", "UITests", buildSettings.AppName, $"{buildSettings.AppName}.csproj");
        RunBinary(dependencies.DotnetExe, $"build {appPath} -t InstallAndroidDependencies -p:AcceptAndroidSdkLicenses=true {string.Join(" ", buildSettings.BuildParamsApp)}");
        Environment.SetEnvironmentVariable("JAVA_HOME", jdkDirectory);
        Environment.SetEnvironmentVariable("ANDROID_HOME", androidSdkDirectory);

        return buildSettings;
    }

    private static void InstallAppiumDriver(CommonDependencies dependencies, string driverName)
    {
        Console.WriteLine($"\nInstalling appium {driverName} driver...");
        var installCheck = RunBinary(dependencies.NodeExe, [dependencies.AppiumEntry, "driver", "list", "--installed"], true);
        var driverInstalled = Regex.IsMatch(installCheck!.StandardError, Regex.Escape(driverName));
        if (driverInstalled)
            RunBinary(dependencies.NodeExe, [dependencies.AppiumEntry, "driver", "update", driverName]);
        else
            RunBinary(dependencies.NodeExe, [dependencies.AppiumEntry, "driver", "install", driverName]);
    }
#endregion

#region BuildSettings
    private static void AppendPlatformIndependentBuildSettings(BuildSettings settings, string workspace)
    {
        // Universal build parameters for the ci builds
        settings.BuildParamsCommon.AddRange([
            "-c Release",
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
            $"--results-directory {testResultsDir}"
        ]);
        var trxFilename = Environment.GetEnvironmentVariable("TRX_FILENAME");
        if (!string.IsNullOrWhiteSpace(trxFilename)) {
            settings.TestParams.Add($"--report-trx-filename {trxFilename}");
        }
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
#endregion

#region Helpers
    private static void KillAppium(Process appiumProcess, List<string> appiumStandardOutput, List<string> appiumStandardError)
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

    private static async Task WaitForAppiumReadyAsync(string statusUrl, TimeSpan timeout)
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

    private static Process RunBinaryBackground(string binary, string arguments, List<string> standardOutput, List<string> standardError) {
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

    private static BinaryOutput? RunBinary(string binary, Collection<string> arguments, bool captureStdOut = false, bool throwOnError = true) {
        Console.WriteLine($"Running {binary} {string.Join(" ", arguments)}");

        var startInfo = new ProcessStartInfo(binary, arguments);
        return RunBinary(startInfo, captureStdOut, throwOnError);
    }

    private static BinaryOutput? RunBinary(string binary, string arguments, bool captureStdOut = false, bool throwOnError = true) {
        Console.WriteLine($"Running {binary} {arguments}");

        var startInfo = new ProcessStartInfo(binary, arguments);
        return RunBinary(startInfo, captureStdOut, throwOnError);
    }

    private static BinaryOutput? RunBinary(ProcessStartInfo startInfo, bool captureOutput = false, bool throwOnError = true) {
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

    private class BinaryOutput
    {
        public string StandardOutput;
        public string StandardError;

        public BinaryOutput(string standardOut, string standardErr)
        {
            StandardOutput = standardOut;
            StandardError = standardErr;
        }
    }
#endregion
}
