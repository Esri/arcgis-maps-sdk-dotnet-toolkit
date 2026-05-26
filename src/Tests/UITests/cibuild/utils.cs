using System.IO.Compression;
using System.Diagnostics;
using System.Formats.Tar;

internal class Program
{
    static int Main(string[] args)
    {
        // Required inputs
        var workspace = Environment.GetEnvironmentVariable("WORKSPACE");
        var dotnetExe = Environment.GetEnvironmentVariable("DOTNET_EXE");
        var yamlConfig = Environment.GetEnvironmentVariable("YAML_CONFIG");

        if (String.IsNullOrWhiteSpace(workspace) || String.IsNullOrEmpty(dotnetExe) || String.IsNullOrEmpty(yamlConfig)) {
            throw new ArgumentException("Environment variables WORKSPACE, DOTNET_DIR, and YAML_CONFIG must all be set.");
        }
        if (!Path.Exists(workspace) || !Path.Exists(dotnetExe) || !Path.Exists(yamlConfig)) {
            throw new ArgumentException("Workspace and dotnet directory must be existing paths.");
        }

        // Derived variables
        var toolkitSrc = Path.GetFullPath(Path.Join(Path.GetDirectoryName(yamlConfig), "..", "..", ".."));

        // Install maui
        RunBinary(dotnetExe, "workload install maui-android");

        // Install node
        var nodeWorkspace = Path.Join(workspace, ".node");
        var nodeVersion = ReadYamlValue(yamlConfig, "node-version");
        var (nodeExe, npmExe) = InstallNode(nodeWorkspace, nodeVersion);

        // Install appium
        Environment.SetEnvironmentVariable("APPIUM_HOME", Path.Join(workspace, ".appium"));
        var appiumEntry = Path.Join(nodeWorkspace, "node_modules", "appium", "index.js");
        RunBinary(npmExe, $"install appium --prefix \"{nodeWorkspace}\"");

        // Install appium android driver
        try {
            RunBinary(nodeExe, $"\"{appiumEntry}\" driver install uiautomator2");

            // Additional setup for android? May need to have build task set ANDROID_HOME, JAVA_HOME,
            // and add adb to path for session if not already there. bundletool.jar probably not
            // required, can just run the app on device using dotnet anyway
        }
        catch {
            Console.WriteLine("Appium driver install failed. This may be a real error, or the driver may already be installed. Check preceeding logs for details.");
        }

        var nugetRepo = Environment.GetEnvironmentVariable("NUGET_REPO");
        if (!String.IsNullOrWhiteSpace(nugetRepo)) {
            SetNugetSource(workspace, dotnetExe, nugetRepo);
        }

        var buildParamsCommon = new List<string>() {
            "-c Release",
            "-p:ArtifactsPivots=TestBuild",
            "-p:UseArtifactsOutput=true"
        };

        var androidFramework = "net10.0-android";
        var buildParamsApp = new List<string>() {
            $"-f {androidFramework}",
            $"-p:TargetFrameworks={androidFramework}",
            "-r android-arm64"
        };
        var releaseVersion = Environment.GetEnvironmentVariable("RELEASE_VERSION");
        if (!String.IsNullOrWhiteSpace(releaseVersion)) {
            buildParamsApp.Add($"-p:UseNugetPackage={releaseVersion}");
        }

        var appPath = Path.Join(toolkitSrc, "Tests", "UITests", "Toolkit.UITests.Maui.App", "Toolkit.UITests.Maui.App.csproj");
        RunBinary(dotnetExe, $"build {appPath} {String.Join(" ", buildParamsCommon)} {String.Join(" ", buildParamsApp)}");

        var runnerPath = Path.Join(toolkitSrc, "Tests", "UITests", "Toolkit.UITests.MauiAndroid", "Toolkit.UITests.MauiAndroid.csproj");
        RunBinary(dotnetExe, $"build {runnerPath} {String.Join(" ", buildParamsCommon)}");


        
        return 0;
    }

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
}