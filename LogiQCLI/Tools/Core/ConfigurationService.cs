using System;
using System.IO;
using System.Text.Json;
using LogiQCLI.Core.Models.Configuration;

namespace LogiQCLI.Core.Services
{
    public class ConfigurationService
    {
        private readonly string _dataDirectory;
        private readonly string _configFilePath;

        public ConfigurationService()
        {
            var companyName = "LogiQ";
            var appName = "LogiQCLI";
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataDirectory = Path.Combine(appDataPath, companyName, appName);
            _configFilePath = Path.Combine(_dataDirectory, "settings.json");

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        public string GetDataDirectory()
        {
            return _dataDirectory;
        }

        public ApplicationSettings? LoadSettings()
        {
            if (!File.Exists(_configFilePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<ApplicationSettings>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SaveSettings(ApplicationSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_configFilePath, json);
        }
    }
}
