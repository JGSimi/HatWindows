using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hat.Models;

namespace Hat.Services;

/// <summary>
/// Secure API key storage using Windows DPAPI (Data Protection API).
/// Replaces macOS Keychain (KeychainManager.swift).
/// Stores encrypted keys per provider in %APPDATA%/Hat/credentials.dat.
/// </summary>
public class CredentialManager
{
    private static readonly Lazy<CredentialManager> _instance = new(() => new CredentialManager());
    public static CredentialManager Shared => _instance.Value;

    private readonly string _credentialsPath;
    private Dictionary<string, string> _cache = new();

    private CredentialManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var hatDir = Path.Combine(appData, "Hat");
        Directory.CreateDirectory(hatDir);
        _credentialsPath = Path.Combine(hatDir, "credentials.dat");
        LoadAll();
    }

    /// <summary>
    /// Save an API key for a specific cloud provider.
    /// </summary>
    public void SaveKey(string key, CloudProvider provider)
    {
        var trimmed = key.Trim();
        var account = provider.CredentialKey();

        if (string.IsNullOrEmpty(trimmed))
        {
            DeleteKey(provider);
            return;
        }

        _cache[account] = trimmed;
        PersistAll();
    }

    /// <summary>
    /// Load the API key for a specific cloud provider.
    /// </summary>
    public string? LoadKey(CloudProvider provider)
    {
        var account = provider.CredentialKey();
        return _cache.TryGetValue(account, out var value) ? value : null;
    }

    /// <summary>
    /// Delete the API key for a specific cloud provider.
    /// </summary>
    public void DeleteKey(CloudProvider provider)
    {
        var account = provider.CredentialKey();
        _cache.Remove(account);
        PersistAll();
    }

    /// <summary>
    /// Encrypts and writes all keys to disk using DPAPI.
    /// </summary>
    private void PersistAll()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_credentialsPath, encrypted);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to persist credentials: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads and decrypts all keys from disk.
    /// </summary>
    private void LoadAll()
    {
        try
        {
            if (!File.Exists(_credentialsPath))
            {
                _cache = new Dictionary<string, string>();
                return;
            }

            var encrypted = File.ReadAllBytes(_credentialsPath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load credentials: {ex.Message}");
            _cache = new Dictionary<string, string>();
        }
    }
}
