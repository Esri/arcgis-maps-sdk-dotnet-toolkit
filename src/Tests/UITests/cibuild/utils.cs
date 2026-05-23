using System.IO.Compression;
using System.Diagnostics;
using System.Formats.Tar;

internal class Program
{
    static int Main(string[] args)
    {
        var workspace = Environment.GetEnvironmentVariable("WORKSPACE");
        var dotnetExe = Environment.GetEnvironmentVariable("DOTNET_EXE");
        var yamlConfig = Environment.GetEnvironmentVariable("YAML_CONFIG");

        if (String.IsNullOrWhiteSpace(workspace) || String.IsNullOrEmpty(dotnetExe) || String.IsNullOrEmpty(yamlConfig)) {
            throw new ArgumentException("Environment variables WORKSPACE, DOTNET_DIR, and YAML_CONFIG must all be set.");
        }
        if (!Path.Exists(workspace) || !Path.Exists(dotnetExe) || !Path.Exists(yamlConfig)) {
            throw new ArgumentException("Workspace and dotnet directory must be existing paths.");
        }

        var nodeWorkspace = Path.Join(workspace, ".node");
        var nodeVersion = ReadYamlValue(yamlConfig, "node-version");
        var (nodeExe, npmExe) = InstallNode(nodeWorkspace, nodeVersion);

        RunBinary(npmExe, $"install appium --prefix \"{nodeWorkspace}\"");

        // TODO: Install android driver
        
        return 0;
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
        Console.WriteLine("Starting Node.js install...");

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
        Console.WriteLine($"Running {binary} {arguments}");

        var startInfo = new ProcessStartInfo
        {
            FileName = binary,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                Console.WriteLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
    }
}