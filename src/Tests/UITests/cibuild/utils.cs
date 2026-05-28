using System.IO.Compression;
using System.Diagnostics;
using System.Formats.Tar;

internal class Program
{
    // Supported test platforms
    private static string[] _testPlatforms = { "MauiAndroid" };

    static int Main(string[] args)
    {
        // Required inputs
        if (args.Length < 1)
            throw new ArgumentException($"A test platform must be passed as the first and only command line argument. Supported platforms are [{string.Join(", ", _testPlatforms)}].");
        var testPlatform = args[0].Trim();
        if (!_testPlatforms.Contains(testPlatform))
            throw new ArgumentException($"Test platform '{testPlatform}' not recognized. Supported platforms are [{string.Join(", ", _testPlatforms)}].");

        var workspace = Environment.GetEnvironmentVariable("WORKSPACE");
        var dotnetExe = Environment.GetEnvironmentVariable("DOTNET_EXE");
        var toolkitSrc = Environment.GetEnvironmentVariable("TOOLKIT_SRC");
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrEmpty(dotnetExe) || string.IsNullOrEmpty(toolkitSrc)) {
            throw new ArgumentException("Environment variables WORKSPACE, DOTNET_DIR, and TOOLKIT_SRC must all be set.");
        }
        if (!Path.Exists(workspace) || !Path.Exists(dotnetExe) || !Path.Exists(toolkitSrc)) {
            throw new ArgumentException("Workspace and dotnet directory must be existing paths.");
        }

        // Derived variables
        var yamlConfig = Path.Join(toolkitSrc, "Tests", "UITests", "cibuild", "variables.yml");

        // Configure nuget repo if set
        var nugetRepo = Environment.GetEnvironmentVariable("NUGET_REPO");
        if (!string.IsNullOrWhiteSpace(nugetRepo)) {
            SetNugetSource(toolkitSrc, dotnetExe, nugetRepo);
        }

        // Install node
        var nodeWorkspace = Path.Join(workspace, ".node");
        var nodeVersion = ReadYamlValue(yamlConfig, "node-version");
        var (nodeExe, npmExe) = InstallNode(nodeWorkspace, nodeVersion);

        // Install appium
        Environment.SetEnvironmentVariable("APPIUM_HOME", Path.Join(workspace, ".appium"));
        var appiumEntry = Path.Join(nodeWorkspace, "node_modules", "appium", "index.js");
        RunBinary(npmExe, $"install appium --prefix \"{nodeWorkspace}\"");

        // Platform-specific setup
        var buildSettings = GetBuildSettings(testPlatform, workspace);
        switch (testPlatform)
        {
            case "MauiAndroid": SetupAndroid(dotnetExe, nodeExe, appiumEntry, toolkitSrc, buildSettings); break;
            default: throw new ArgumentException($"The test platform '{testPlatform}' was not recognized. Aborting tests.");
        }

        // Run appium in background
        var appiumStandardOutput = new List<string>();
        var appiumStandardError = new List<string>();
        var appiumProcess = RunBinaryBackground(
            nodeExe,
            appiumEntry,
            appiumStandardOutput,
            appiumStandardError
        );

        // Configure appium cleanup
        Action cleanup = () => {
            Console.WriteLine("\nStarting test cleanup...");

            appiumProcess.Kill();
            RunBinary(dotnetExe, "build-server shutdown");

            if (Environment.GetEnvironmentVariable("PRINT_APPIUM_LOGS")?.ToLower() == "true") {
                Console.WriteLine("\nAppium standard output logs:");
                foreach (var line in appiumStandardOutput) {
                    Console.WriteLine(line);
                }
            }

            if (appiumStandardError.Count > 0) {
                Console.WriteLine("\nAppium standard error logs:");
                foreach (var line in appiumStandardError) {
                    Console.WriteLine(line);
                }
            }
        };
        Console.CancelKeyPress += (sender, args) => cleanup();

        try {
            var uiTestsPath = Path.Join(toolkitSrc, "Tests", "UITests");

            // Build app and runner
            var appPath = Path.Join(uiTestsPath, buildSettings.AppName, $"{buildSettings.AppName}.csproj");
            RunBinary(dotnetExe, $"build {appPath} {string.Join(" ", buildSettings.BuildParamsCommon)} {string.Join(" ", buildSettings.BuildParamsApp)}");

            var runnerPath = Path.Join(uiTestsPath, buildSettings.RunnerName, $"{buildSettings.RunnerName}.csproj");
            RunBinary(dotnetExe, $"build {runnerPath} {string.Join(" ", buildSettings.BuildParamsCommon)}");

            // Run tests
            var artifactsPath = Path.Join(uiTestsPath, "artifacts", "bin");
            var runnerExe = Path.Join(artifactsPath, buildSettings.RunnerName, "TestBuild", buildSettings.RunnerName);
            var appExe = Path.Join(artifactsPath, buildSettings.AppName, "TestBuild", buildSettings.BinaryName);
            Environment.SetEnvironmentVariable("TKUITEST_APP", appExe);
            RunBinary(runnerExe, $"{string.Join(" ", buildSettings.TestParams)}");
        }
        finally {
            cleanup();
        }
        
        return 0;
    }

#region PlatformDependencies
    private static void SetupAndroid(string dotnetExe, string nodeExe, string appiumEntry, string toolkitSrc, BuildSettings buildSettings)
    {
        // Install maui android
        RunBinary(dotnetExe, "workload install maui-android");

        // Install appium android driver
        try {
            RunBinary(nodeExe, $"\"{appiumEntry}\" driver install uiautomator2");
        }
        catch {
            Console.WriteLine("Appium driver install failed. This may be a real error, or the driver may already be installed. Check preceeding logs for details.");
        }

        // Install jdk and android sdk
        try
        {
            var appPath = Path.Join(toolkitSrc, "Tests", "UITests", buildSettings.AppName, $"{buildSettings.AppName}.csproj");
            RunBinary(dotnetExe, $"build {appPath} -t InstallAndroidDependencies {string.Join(" ", buildSettings.BuildParamsApp)}");
        }
        finally
        {
            RunBinary(dotnetExe, "build-server shutdown");
        }

        // Additional setup for android? May need to have build task set ANDROID_HOME, JAVA_HOME,
        // and add adb to path for session if not already there. bundletool.jar probably not
        // required, can just run the app on device using dotnet anyway
    }
#endregion

#region CommonDependencies
    private static void SetNugetSource(string workspace, string dotnetExe, string nugetRepo)
    {
        Console.WriteLine("\nConfiguring nuget...");

        RunBinary(dotnetExe, $"new nugetconfig --force -o \"{workspace}\"");

        var configFile = Path.Join(workspace, "nuget.config");
        RunBinary(dotnetExe, $"nuget add source \"{nugetRepo}\" --configfile \"{configFile}\"");

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
        throw new Exception($"Could find variable ${name} in ${yamlFile}");
    }

    private static (string NodeExe, string NpmExe) InstallNode(string workspace, string nodeVersion)
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
            using var response = httpClient.GetAsync(nodeUrl).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using (var output = File.Create(nodeTarGz))
            {
                response.Content.CopyToAsync(output).GetAwaiter().GetResult();
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
#endregion

#region BuildSettings
    private static BuildSettings GetBuildSettings(string testPlatform, string workspace)
    {
        BuildSettings settings;
        switch (testPlatform)
        {
            case "MauiAndroid":
                settings = new BuildSettings("Toolkit.UITests.Maui.App", "Toolkit.UITests.MauiAndroid");

                var androidFramework = "net10.0-android";
                settings.BuildParamsApp.AddRange([
                    $"-f {androidFramework}",
                    $"-p:TargetFrameworks={androidFramework}",
                    "-r android-arm64",
                    $"-p:JavaSdkDirectory={workspace}/jdk",
                    $"-p:AndroidSdkDirectory={workspace}/android-sdk",
                    "-p:AcceptAndroidSdkLicenses=true"
                ]);

                settings.BinaryName = "com.esri.toolkit.uitests.maui-Signed.apk";

                break;
            default:
                throw new ArgumentException($"The test platform '{testPlatform}' was not recognized. Aborting tests.");
        }

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

        return settings;
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
    private static Process RunBinaryBackground(string binary, string arguments, List<string> standardOutput, List<string> standardError) {
        Console.WriteLine($"\nRunning {binary} {arguments}");

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

    private static void RunBinary(string binary, string arguments) {
        Console.WriteLine($"\nRunning {binary} {arguments}");

        var startInfo = new ProcessStartInfo
        {
            FileName = binary,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                Console.WriteLine(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                Console.Error.WriteLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command failed with exit code {process.ExitCode}: {binary} {arguments}");
        }
    }
#endregion
}