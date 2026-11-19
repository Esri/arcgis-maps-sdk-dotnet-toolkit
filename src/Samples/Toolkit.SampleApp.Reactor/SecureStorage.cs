// Based on https://github.com/dotnet/maui/blob/main/src/Essentials/src/SecureStorage/SecureStorage.windows.cs
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using SecureStorageDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>;

internal static class AppUtils
{
    internal static string AppDataDirectory => _AppDataDirectory.Value;
    private static readonly Lazy<string> _AppDataDirectory = new(valueFactory: () =>
    {
        if (IsPackagedApp)
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArcGISReactorSample", "Data");
            if (!File.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    });

    /// <summary>
    /// Gets if this app is a packaged app.
    /// </summary>
    public static bool IsPackagedApp { get; } = new Lazy<bool>(() =>
    {
        try
        {
            return Windows.ApplicationModel.Package.Current != null;
        }
        catch { }
        return false;
    }).Value;
}

public partial class SecureStorage
{
    /// <summary>
    /// Gets and decrypts the value for a given key.
    /// </summary>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <returns>The decrypted string value or <see langword="null"/> if a value was not found.</returns>
    public static Task<string?> GetAsync(string key) => Default.GetAsyncImpl(key);

    /// <summary>
    /// Sets and encrypts a value for a given key.
    /// </summary>
    /// <param name="key">The key to set the value for.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    public static Task SetAsync(string key, string value) => Default.SetAsyncImpl(key, value);

    /// <summary>
    /// Removes a key and its associated value if it exists.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public static bool Remove(string key) => Default._secureStorage.Remove(key);
    /// <summary>
    /// Removes all of the stored encrypted key/value pairs.
    /// </summary>
    public static void RemoveAll() => Default._secureStorage.RemoveAll();

    private static SecureStorage Default { get; } = new SecureStorage();

    static SecureStorage? defaultImplementation;

    readonly ISecureStorageImplementation _secureStorage;
    
    private SecureStorage()
    {
        _secureStorage = AppUtils.IsPackagedApp
            ? new PackagedSecureStorageImplementation()
            : new UnpackagedSecureStorageImplementation();
    }

    private async Task<string?> GetAsyncImpl(string key)
    {
        var encBytes = await _secureStorage.GetAsync(key);

        if (encBytes == null)
            return null;

        var provider = new DataProtectionProvider();

        var buffer = await provider.UnprotectAsync(encBytes.AsBuffer());

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private async Task SetAsyncImpl(string key, string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);

        // LOCAL=user and LOCAL=machine do not require enterprise auth capability
        var provider = new DataProtectionProvider("LOCAL=user");

        var buffer = await provider.ProtectAsync(bytes.AsBuffer());

        var encBytes = buffer.ToArray();

        await _secureStorage.SetAsync(key, encBytes);
    }
}

internal interface ISecureStorageImplementation
{
    Task<byte[]> GetAsync(string key);

    Task SetAsync(string key, byte[] value);

    bool Remove(string key);

    void RemoveAll();
}

internal class PackagedSecureStorageImplementation : ISecureStorageImplementation
{
    public Task<byte[]> GetAsync(string key)
    {
        var settings = GetSettings("preferences");
        var encBytes = settings.Values[key] as byte[];
        return Task.FromResult(encBytes);
    }

    public Task SetAsync(string key, byte[] data)
    {
        var settings = GetSettings("preferences");
        settings.Values[key] = data;
        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        var settings = GetSettings("preferences");
        return settings.Values.Remove(key);
    }

    public void RemoveAll()
    {
        var settings = GetSettings("preferences");
        settings.Values.Clear();
    }

    static ApplicationDataContainer GetSettings(string name)
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        if (!localSettings.Containers.ContainsKey(name))
            localSettings.CreateContainer(name, ApplicationDataCreateDisposition.Always);
        return localSettings.Containers[name];
    }
}

class UnpackagedSecureStorageImplementation : ISecureStorageImplementation
{
    static readonly string AppSecureStoragePath = Path.Combine(AppUtils.AppDataDirectory, "..", "Settings", "securestorage.dat");

    readonly SecureStorageDictionary _secureStorage = new();

    public UnpackagedSecureStorageImplementation()
    {
        Load();
    }

    void Load()
    {
        if (!File.Exists(AppSecureStoragePath))
            return;

        try
        {
            using var stream = File.OpenRead(AppSecureStoragePath);

            SecureStorageDictionary readPreferences = JsonSerializer.Deserialize(stream, SecureStorageJsonSerializerContext.Default.SecureStorageDictionary);

            if (readPreferences != null)
            {
                _secureStorage.Clear();
                foreach (var pair in readPreferences)
                    _secureStorage.TryAdd(pair.Key, pair.Value);
            }
        }
        catch (JsonException)
        {
            // if deserialization fails proceed with empty settings
        }
    }

    void Save()
    {
        var dir = Path.GetDirectoryName(AppSecureStoragePath);
        Directory.CreateDirectory(dir);

        using var stream = File.Create(AppSecureStoragePath);
        JsonSerializer.Serialize(stream, _secureStorage, SecureStorageJsonSerializerContext.Default.SecureStorageDictionary);
    }

    public Task<byte[]> GetAsync(string key)
    {
        _secureStorage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, byte[] value)
    {
        if (value is null)
            _secureStorage.TryRemove(key, out _);
        else
            _secureStorage[key] = value;
        Save();
        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        var result = _secureStorage.TryRemove(key, out _);
        Save();
        return result;
    }

    public void RemoveAll()
    {
        _secureStorage.Clear();
        Save();
    }
}

[JsonSerializable(typeof(SecureStorageDictionary), TypeInfoPropertyName = nameof(SecureStorageDictionary))]
internal partial class SecureStorageJsonSerializerContext : JsonSerializerContext
{
}
