using System.Text.Json;
using System.Text.Json.Schema;
using StrataScene.Core.Logging;

namespace StrataScene.Core.Config;

public sealed class ConfigRepository
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly string _schemaFilePath;
    private readonly ILog _log;
    private readonly Lock _ioLock = new();

    public ConfigRepository(ILog log, string? baseDirectory = null)
    {
        _log = log;
        _configDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StrataScene");

        Directory.CreateDirectory(_configDirectory);
        _configFilePath = Path.Combine(_configDirectory, "config.json");
        _schemaFilePath = Path.Combine(_configDirectory, "schema.json");
    }

    public string ConfigDirectory => _configDirectory;
    public string ConfigFilePath => _configFilePath;

    public (AppConfig Config, bool WasRecovered) LoadOrCreate()
    {
        lock (_ioLock)
        {
            EnsureSchemaFile();

            if (!File.Exists(_configFilePath))
            {
                _log.Info("Config file does not exist. Creating default configuration.");
                var defaultConfig = ConfigDefaults.CreateDefault();
                Save(defaultConfig);
                return (defaultConfig, false);
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.AppConfig);

                if (config == null)
                {
                    throw new InvalidOperationException("Deserialized config was null.");
                }

                var errors = ConfigValidator.Validate(config);
                if (errors.Count > 0)
                {
                    _log.Warn($"Config validation warnings/errors: {string.Join("; ", errors)}");
                }

                return (config, false);
            }
            catch (Exception ex)
            {
                _log.Error("Failed to parse config file. Backing up invalid file and restoring default.", ex);
                BackupInvalidFile();

                var fallback = ConfigDefaults.CreateDefault();
                Save(fallback);
                return (fallback, true);
            }
        }
    }

    public void Save(AppConfig config)
    {
        lock (_ioLock)
        {
            var tempFile = _configFilePath + ".tmp";
            var json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.AppConfig);
            File.WriteAllText(tempFile, json);

            if (File.Exists(_configFilePath))
            {
                File.Replace(tempFile, _configFilePath, null);
            }
            else
            {
                File.Move(tempFile, _configFilePath);
            }

            _log.Info("Configuration saved successfully.");
        }
    }

    private void BackupInvalidFile()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupPath = Path.Combine(_configDirectory, $"config.invalid-{timestamp}.json");
                File.Move(_configFilePath, backupPath, overwrite: true);
                _log.Info($"Backed up corrupt config to {backupPath}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("Failed to backup invalid config file", ex);
        }
    }

    private void EnsureSchemaFile()
    {
        try
        {
            var schemaNode = ConfigJsonContext.Default.Options.GetJsonSchemaAsNode(typeof(AppConfig));
            var schemaJson = schemaNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_schemaFilePath, schemaJson);
        }
        catch (Exception ex)
        {
            _log.Warn("Failed to export JSON schema", ex);
        }
    }
}
