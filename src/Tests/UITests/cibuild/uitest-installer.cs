using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("These installation workloads require Windows.");

            using var appEnv = new AppiumEnvironment(Helpers.GetRequiredEnvironmentPath("WORKSPACE"));

            if (args.Length < 1)
                throw new ArgumentException($"Must pass at least one argument (clean, run, install)");

            if (args[0] == "clean")
            {
                appEnv.Clean();
            }
            else if (args[0] == "run")
            {
                if (args.Length < 2)
                    throw new ArgumentException($"Must pass workload id after run");

                await appEnv.Install(args[1]);
                await appEnv.RunAppium(args[1]);
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
            await WaitForAppiumReadyAsync("http://127.0.0.1:4724/status", TimeSpan.FromSeconds(30));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public async Task Install(string workloadId)
    {
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
    }

    public IReadOnlyDictionary<string, IReadOnlyList<TestDependency>> GetWorkloads()
    {
        var paths = new RequiredPathsDependency(this);
        var node = new NodeDependency(this);
        var appium = new AppiumDependency(this);
        var driver = new WinCoreDriverDependency(this);
        var nuget = new NugetDependency(this);
        var maui = new MauiDependency(this);
        var workloads = new Dictionary<string, IReadOnlyList<TestDependency>>
        {
            ["windows"] = [paths, nuget, node, appium, driver],
            ["maui-windows"] = [paths, nuget, maui, node, appium, driver],
            ["all"] = [paths, nuget, maui, node, appium, driver]
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
            AppEnvironment.DotnetExe = Path.GetFullPath(Helpers.GetRequiredEnvironmentPath("DOTNET_PATH"));
            AppEnvironment.ToolkitSrc = Path.GetFullPath(Helpers.GetRequiredEnvironmentPath("TOOLKIT_SRC"));
            AppEnvironment.YamlFilePath = Path.Join(AppEnvironment.ToolkitSrc, "Tests", "UITests", "cibuild", "variables.yml");
        }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
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

        public override void Restore(InstalledDependency installed)
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

    class WinCoreDriverDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        public override string Name => "appium-driver-wincore";

        public override Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
        {
            var nodeExe = AppiumEnvironment.GetRequiredInfo(installed, "node", "node-exe");
            var appiumEntry = AppiumEnvironment.GetRequiredInfo(installed, "appium", "appium-entry");
            InstallAppiumDriver(nodeExe, appiumEntry, "wincore", "appium-wincore-driver");
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
            Helpers.RunBinary(AppEnvironment.DotnetExe, ["workload", "install", "maui"]);
            return Task.FromResult(new InstalledDependency());
        }

        public override void Restore(InstalledDependency installed) { }

        public override bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
        {
            version = null;
            return true;
        }
    }

    /// <summary>Sets the nuget source to point to the folder where the ArcGIS nugets are stored (if provided)</summary>
    class NugetDependency(AppiumEnvironment environment) : TestDependency(environment)
    {
        private const string _nugetRepoEnvVar = "NUGET_REPO";
        private const string _unconfiguredVersion = "unconfigured";

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
                version = nugetRepo;
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

