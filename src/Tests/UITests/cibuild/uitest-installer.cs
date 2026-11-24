using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

/* General Idea:
 * Workloads are just lists defining required dependencies
 * When installing a workload, dependencies are installed in order they appear in the workloads list
 * Dependencies can rely on other dependencies for installation. The script keeps track of what is installed a
nd throws an error if it's not present
 * Each workload has an install function
 * There will be a clean command that deletes the entire dependencies workspace
 */

internal class Program
{
    private const string DependenciesInstallDir = ".uitest";
    private const string StateFileName = "installed.json";

    static async Task<int> Main(string[] args)
    {
        try
        {
            var workloads = GetWorkloads();
            if (args.Length != 1 || (args[0] != "clean" && !workloads.ContainsKey(args[0])))
                throw new ArgumentException("Pass one command: windows, maui-windows, all, or clean.");
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("These installation workloads require Windows.");

            Paths.WorkspacePath = Path.Join(GetRequiredEnvironmentPath("WORKSPACE"), DependenciesInstallDir);

            if (args[0] == "clean")
            {
                Clean(workloads["all"]);
                return 0;
            }

            Directory.CreateDirectory(Paths.WorkspacePath);
            var stateFile = Path.Join(Paths.WorkspacePath, StateFileName);
            Dictionary<string, InstalledDependency> installed;
            if (File.Exists(stateFile))
                installed = JsonSerializer.Deserialize(
                        File.ReadAllText(stateFile),
                        DependencySerializationContext.Default.DictionaryStringInstalledDependency
                    ) ?? new Dictionary<string, InstalledDependency>();
            else
                installed = new();

            foreach (var dependency in workloads[args[0]])
            {
                if (installed.TryGetValue(dependency.Name, out var existing))
                {
                    AssertVersion(dependency, existing, installed);

                    Console.WriteLine($"\nRestoring dependency \"{dependency.Name}\"...");
                    dependency.Restore(existing);
                    continue;
                }

                try
                {
                    Console.WriteLine($"\nInstalling dependency \"{dependency.Name}\"...");
                    var newInstalled = await dependency.Install(installed);
                    installed[dependency.Name] = newInstalled;

                    // Save state after each successful install so it can be cleaned if a latter install breaks
                    var persistent = installed.Where(pair => pair.Value.Serialize).ToDictionary();
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
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static void Clean(IReadOnlyList<ITestDependency> workload)
    {
        var stateFile = Path.Join(Paths.WorkspacePath, StateFileName);
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

        if (Directory.Exists(Paths.WorkspacePath))
            Directory.Delete(Paths.WorkspacePath, true);
    }

    static IReadOnlyDictionary<string, IReadOnlyList<ITestDependency>> GetWorkloads()
    {
        var paths = new RequiredPathsDependency();
        var node = new NodeDependency();
        var appium = new AppiumDependency();
        var driver = new WinCoreDriverDependency();
        var nuget = new NugetDependency();
        var maui = new MauiDependency();
        var workloads = new Dictionary<string, IReadOnlyList<ITestDependency>>
        {
            ["windows"] = [paths, nuget, node, appium, driver],
            ["maui-windows"] = [paths, nuget, maui, node, appium, driver],
            ["all"] = [paths, nuget, maui, node, appium, driver]
        };
        return workloads.AsReadOnly();
    }

    static void AssertVersion(ITestDependency dependency, InstalledDependency existing, Dictionary<string, InstalledDependency> installed)
    {
        if (!dependency.TryGetVersion(installed, out var desiredVersion))
            throw new InvalidOperationException($"Could not determine expected version for dependency '{dependency.Name}'. Current version: '{existing.Version ?? "<untracked>"}'. See above logs.");

        if (desiredVersion != null && existing.Version != desiredVersion)
            throw new InvalidOperationException($"Version mismatch for dependency '{dependency.Name}': expected '{desiredVersion}', current '{existing.Version ?? "<untracked>"}'. Run clean before reinstalling.");
    }

    static string GetRequiredInfo(IReadOnlyDictionary<string, InstalledDependency> installed, string dependency, string key)
    {
        if (!installed.TryGetValue(dependency, out var value) || value.Info == null || !value.Info.TryGetValue(key, out var info))
            throw new InvalidOperationException($"Dependency '{dependency}' must be installed before using '{key}'.");
        return info;
    }

    static bool TryReadYamlVersion(string name, out string? version)
    {
        try
        {
            version = ReadYamlValue(Paths.YamlFilePath, name);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            version = null;
            return false;
        }
    }

    #region Dependencies
    class RequiredPathsDependency : ITestDependency
    {
        public string Name => "required-paths";

        public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var dependency = new InstalledDependency(serialize: false);
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public void Restore(InstalledDependency installed)
        {
            Paths.DotnetExe = GetRequiredEnvironmentPath("DOTNET_PATH");
            Paths.ToolkitSrc = GetRequiredEnvironmentPath("TOOLKIT_SRC");
            Paths.YamlFilePath = Path.Join(Paths.ToolkitSrc, "Tests", "UITests", "cibuild", "variables.yml");
        }

        public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    // Install Node.js
    class NodeDependency : ITestDependency
    {
        public string Name => "node";

        public async Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            if (!TryGetVersion(installed, out var version) || string.IsNullOrWhiteSpace(version))
                throw new Exception("Could not get desired node.js version to install");

            var workspace = Path.Join(Paths.WorkspacePath, ".node");
            var nodeDir = Path.Join(workspace, $"node-v{version}-win-x64");
            var nodeExe = Path.Join(nodeDir, "node.exe");
            var npmExe = Path.Join(nodeDir, "npm.cmd");
            if (!File.Exists(nodeExe))
            {
                Directory.CreateDirectory(workspace);
                var nodeUrl = $"https://nodejs.org/dist/v{version}/node-v{version}-win-x64.zip";
                var nodeZip = Path.Join(workspace, "node.zip");

                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(nodeUrl);
                response.EnsureSuccessStatusCode();

                using (var output = File.Create(nodeZip))
                {
                    await response.Content.CopyToAsync(output);
                }

                ZipFile.ExtractToDirectory(nodeZip, workspace);
                Console.WriteLine($"Node.js installed at {nodeExe}");
            }
            else
                Console.WriteLine($"Found cached Node.js at {nodeExe}");
            var dependency = new InstalledDependency(version, new() { ["node-exe"] = nodeExe, ["npm-exe"] = npmExe });
            Restore(dependency);
            return dependency;
        }

        public void Restore(InstalledDependency installed)
        {
            var nodeExe = installed.Info!["node-exe"];
            var npmExe = installed.Info["npm-exe"];
            foreach (var file in new[] { nodeExe, npmExe })
            {
                if (!File.Exists(file))
                    throw new FileNotFoundException("An installed dependency is missing. Run clean before reinstalling.", file);
            }
            var nodeDir = Path.GetDirectoryName(nodeExe);
            // Node sometimes calls itself base on path internally, so we cannot rely on returning the local variables
            Environment.SetEnvironmentVariable("PATH", $"{nodeDir}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}");
        }

        public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
            => TryReadYamlVersion("node-version", out version);
    }

    // Install appium and driver
    class AppiumDependency : ITestDependency
    {
        public string Name => "appium";

        public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var npmExe = GetRequiredInfo(installed, "node", "npm-exe");
            var workspace = Path.Join(Paths.WorkspacePath, ".node");
            var appiumHome = Path.Join(Paths.WorkspacePath, ".appium");
            RunBinary(npmExe, ["install", "appium", "--prefix", workspace]);
            var dependency = new InstalledDependency(info: new()
            {
                ["appium-entry"] = Path.Join(workspace, "node_modules", "appium", "index.js"),
                ["appium-home"] = appiumHome
            });
            Restore(dependency);
            return Task.FromResult(dependency);
        }

        public void Restore(InstalledDependency installed)
        {
            var appiumEntry = installed.Info!["appium-entry"];
            if (!File.Exists(appiumEntry))
                throw new FileNotFoundException("An installed dependency is missing. Run clean before reinstalling.", appiumEntry);
            Environment.SetEnvironmentVariable("APPIUM_HOME", installed.Info["appium-home"]);
        }

        public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    class WinCoreDriverDependency : ITestDependency
    {
        public string Name => "appium-driver-wincore";

        public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var nodeExe = GetRequiredInfo(installed, "node", "node-exe");
            var appiumEntry = GetRequiredInfo(installed, "appium", "appium-entry");
            InstallAppiumDriver(nodeExe, appiumEntry, "wincore", "appium-wincore-driver");
            return Task.FromResult(new InstalledDependency());
        }

        public void Restore(InstalledDependency installed) { }

        public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    // Install maui workload if required
    class MauiDependency : ITestDependency
    {
        public string Name => "maui";

        public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            RunBinary(Paths.DotnetExe, ["workload", "install", "maui"]);
            return Task.FromResult(new InstalledDependency());
        }

        public void Restore(InstalledDependency installed) { }

        public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    /// <summary>Sets the nuget source to point to the folder where the ArcGIS nugets are stored (if provided)</summary>
    class NugetDependency : ITestDependency
    {
        private const string NugetRepoEnvVar = "NUGET_REPO";
        private const string UnconfiguredVersion = "unconfigured";

        public string Name => "nuget";

        public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            // Only set up custom nuget config if path to specific SDK nugets is set
            var nugetRepo = Environment.GetEnvironmentVariable(NugetRepoEnvVar);
            if (string.IsNullOrWhiteSpace(nugetRepo))
                return Task.FromResult(new InstalledDependency(version: UnconfiguredVersion));

            RunBinary(Paths.DotnetExe, ["new", "nugetconfig", "--force", "-o", Paths.ToolkitSrc]);

            var configFile = Path.Join(Paths.ToolkitSrc, "nuget.config");
            RunBinary(Paths.DotnetExe, ["nuget", "add", "source", nugetRepo, "--configfile", configFile]);

            // Workaround for the build machine package mapping changes as applied in samples repo
            var config = new XmlDocument();
            config.Load(configFile);
            var mappings = config.DocumentElement!.AppendChild(config.CreateElement("packageSourceMapping"));
            mappings!.AppendChild(config.CreateElement("clear"));
            config.Save(configFile);

            var nugetDir = Path.Join(Paths.ToolkitSrc, ".nuget");
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

        public void Restore(InstalledDependency installed)
        {
            if (installed.Info == null)
                return;
            var configFile = installed.Info["nuget-config"];
            if (!File.Exists(configFile))
                throw new FileNotFoundException($"NuGet configuration for dependency '{Name}' is missing. Run clean before reinstalling.", configFile);
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", installed.Info["nuget-packages"]);
            Environment.SetEnvironmentVariable("NUGET_HTTP_CACHE_PATH", installed.Info["nuget-http-cache-path"]);
        }

        public void Clean(InstalledDependency installed)
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

        public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            // If the nuget repo was previously set to a different version, we probably want to do a full clean
            var nugetRepo = Environment.GetEnvironmentVariable(NugetRepoEnvVar);
            if (!string.IsNullOrWhiteSpace(nugetRepo))
                version = nugetRepo;
            else
                version = UnconfiguredVersion;
            return true;
        }
    }
    #endregion

    #region Helpers
    private static void InstallAppiumDriver(string nodeExe, string appiumEntry, string driverName, string? npmPackage = null)
    {
        Console.WriteLine($"\nInstalling appium {driverName} driver...");
        var installCheck = RunBinary(nodeExe, [appiumEntry, "driver", "list", "--installed"], true);
        var driverInstalled = Regex.IsMatch(installCheck!.StandardError, Regex.Escape(driverName));
        if (driverInstalled)
            RunBinary(nodeExe, [appiumEntry, "driver", "update", driverName]);
        else if (npmPackage != null)
            RunBinary(nodeExe, [appiumEntry, "driver", "install", "--source=npm", npmPackage]);
        else
            RunBinary(nodeExe, [appiumEntry, "driver", "install", driverName]);
    }

    private static string GetRequiredEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Environment variable '{variableName}' must be set.");

        return value;
    }

    private static string GetRequiredEnvironmentPath(string variableName)
    {
        var path = GetRequiredEnvironmentVariable(variableName);
        if (!Path.Exists(path))
            throw new ArgumentException($"Environment variable '{variableName}' must point to an existing path. Current value: '{path}'.");

        return path;
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

public class Paths
{
    public static string WorkspacePath = string.Empty;
    public static string DotnetExe = string.Empty;
    public static string ToolkitSrc = string.Empty;
    public static string YamlFilePath = string.Empty;
}

interface ITestDependency
{
    public string Name { get; }
    public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed);
    public void Restore(InstalledDependency installed);
    public void Clean(InstalledDependency installed) {}
    public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version);
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
