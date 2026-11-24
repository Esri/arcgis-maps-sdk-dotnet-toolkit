using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;


/* General Idea:
 * Workloads are just lists defining required dependencies
 * When installing a workload, dependencies are installed in order they appear in the workloads list
 * Dependencies can rely on other dependencies for installation. The script keeps track of what is installed and throws an error if it's not present
 * Each workload has an install function
 * There will be a clean command that deletes the entire dependencies workspace
 */
Paths.YamlFilePath = "dadad";

var workloads = GetWorkloads();

// todo: read workload arg
var workload_name = "windows";
if (!workloads.TryGetValue(workload_name, out var workload))
{
    // todo: throw error or smth
    return;
}

// todo: function to read list of already-installed dependencies

var installed = new Dictionary<string, InstalledDependency>();

foreach (var dependency in workload)
{
    if (installed.TryGetValue(dependency.Name, out var existing))
    {
        if (VersionIsValid(dependency, existing, installed))
            continue;

        // todo: throw some error about invalid version
        return;
    }


    try
    {
        var newInstalled = await dependency.Install(installed);
        installed[dependency.Name] = newInstalled;
    }
    catch (Exception ex)
    {
        // throw error with inner error, failed to install dependency
        Console.WriteLine(ex);
        return;
    }
}

foreach (var kvp in installed)
{
    if (kvp.Value.Serialize == false)
        installed.Remove(kvp.Key);
}

var jsonString = JsonSerializer.Serialize(installed, DependencySerializationContext.Default.DictionaryStringInstalledDependency);
Console.WriteLine(jsonString);


IReadOnlyDictionary<string, IReadOnlyList<ITestDependency>> GetWorkloads()
{
    var wrklds = new Dictionary<string, IReadOnlyList<ITestDependency>>();
    var paths = new RequiredPathsDependency();
    var node = new NodeDependency();
    wrklds["windows"] = [ paths, node ];
    /*
    wrklds["windows"] = [ "node", "appium", "appium-driver-windows", "koffi", "wad" ];
    wrklds["android"] = [ "node", "appium", "appium-driver-android", "koffi", "maui", "android-deps" ];
    wrklds["ios"] = [ "node", "appium", "appium-driver-ios", "wda", "maui" ];
    wrklds["mac"] = [ "node", "appium", "appium-driver-mac", "maui" ];
    */
    return wrklds.AsReadOnly();
}

bool VersionIsValid(ITestDependency dependency, InstalledDependency existing, Dictionary<string, InstalledDependency> installed)
{
    if (dependency.TryGetVersion(installed, out var desiredVersion))
    {
        if (desiredVersion == null)
            return true; // null desired version means version doesn't matter
        else
            return existing.Version == desiredVersion;
    }
    else
    {
        Console.WriteLine($"Could not get desired version for dependency \"{dependency.Name}\". See above logs.");
        return false;
    }
}

#region Dependencies
class RequiredPathsDependency : ITestDependency
{
    public string Name => "required-paths";

    public async Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
    {
        Paths.WorkspacePath = Helpers.GetRequiredEnvironmentPath("WORKSPACE");
        Paths.DotnetExe = Helpers.GetRequiredEnvironmentPath("DOTNET_PATH");
        Paths.ToolkitSrc = Helpers.GetRequiredEnvironmentPath("TOOLKIT_SRC");
        Paths.ApiKey = Helpers.GetRequiredEnvironmentVariable("ARCGIS_API_KEY");
        Paths.YamlFilePath = Path.Join(Paths.ToolkitSrc, "Tests", "UITests", "cibuild", "variables.yml");

        return new InstalledDependency(null, null, serialize: false);
    }

    public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
    {
        version = null;
        return true;
    }
}

class NodeDependency : ITestDependency
{
    public string Name => "node";

    public async Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed)
    {
        Console.WriteLine("hi");
        return new InstalledDependency(null, null);
    }

    public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version)
    {
        return Helpers.ReadYamlValue(Paths.YamlFilePath, "node-version", out version);
    }
}
#endregion Dependencies

#region Helpers
class Helpers
{
    public static bool ReadYamlValue(string yamlFile, string name, out string? val)
    {
        try
        {
            using StreamReader sr = File.OpenText(yamlFile);
            string? s;
            while ((s = sr.ReadLine()) != null) {
                s = s.Trim();
                if (s.StartsWith(name)) {
                    val = s.Substring(name.Length+3, s.Length-name.Length-4);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not read yaml value: {ex.ToString()}");
        }
        val = null;
        return false;
    }

    public static string GetRequiredEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Environment variable '{variableName}' must be set.");
        }

        return value;
    }

    public static string GetRequiredEnvironmentPath(string variableName)
    {
        var path = GetRequiredEnvironmentVariable(variableName);
        if (!Path.Exists(path))
        {
            throw new ArgumentException($"Environment variable '{variableName}' must point to an existing path. Current value: '{path}'.");
        }

        return path;
    }
}

public class Paths
{
    public static string WorkspacePath = string.Empty;
    public static string DotnetExe = string.Empty;
    public static string ToolkitSrc = string.Empty;
    public static string ApiKey = string.Empty;
    public static string YamlFilePath = string.Empty;
}

interface ITestDependency
{
    public string Name { get; }
    public Task<InstalledDependency> Install(IReadOnlyDictionary<string, InstalledDependency> installed);
    public bool TryGetVersion(IReadOnlyDictionary<string, InstalledDependency> installed, out string? version);
}

class InstalledDependency(string? version = null, Dictionary<string, string>? info = null, bool serialize = true)
{
    /// <summary> A <c>null<c> version means the dependency is not tracked.</summary>
    public readonly string? Version = version;
    public readonly Dictionary<string, string>? Info = info;
    public bool Serialize = serialize;
}

[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
[JsonSerializable(typeof(Dictionary<string, InstalledDependency>))]
partial class DependencySerializationContext  : JsonSerializerContext {}
#endregion Helpers

