using System.Collections.Generic;
using System.Linq;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Models.Modes;
using LogiQCLI.Core.Models.Modes.Interfaces;

namespace LogiQCLI.Core.Services
{
    public class ModeManager : IModeManager
    {
        private readonly ConfigurationService _configurationService;
        private ModeSettings _modeSettings;
        private Mode _currentMode;

        public ModeManager(ConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            InitializeModeSettings();
            
            var mode = GetMode(_modeSettings.ActiveModeId);
            if (mode == null)
            {
                mode = GetMode("default");
                if (mode == null)
                    throw new InvalidOperationException("Default mode not found. System is in an invalid state.");
                _modeSettings.ActiveModeId = "default";
                SaveModeSettings();
            }
            
            _currentMode = mode;
        }

        public Mode GetCurrentMode()
        {
            return _currentMode;
        }

        public bool SetCurrentMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                throw new ArgumentNullException(nameof(modeId), "Mode ID cannot be null or empty.");

            var mode = GetMode(modeId);
            if (mode == null)
                throw new InvalidOperationException($"Mode with ID '{modeId}' does not exist.");

            _currentMode = mode;
            _modeSettings.ActiveModeId = modeId;
            SaveModeSettings();
            return true;
        }

        public List<Mode> GetAvailableModes()
        {
            var allModes = new List<Mode>();
            allModes.AddRange(_modeSettings.DefaultModes);
            allModes.AddRange(_modeSettings.CustomModes);
            return allModes;
        }

        public Mode? GetMode(string modeId)
        {
            return GetAvailableModes().FirstOrDefault(m => m.Id == modeId);
        }

        public bool AddCustomMode(Mode mode)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode), "Mode cannot be null.");

            if (string.IsNullOrWhiteSpace(mode.Id))
                throw new ArgumentException("Mode ID cannot be null or empty.", nameof(mode));

            if (GetMode(mode.Id) != null)
                throw new InvalidOperationException($"Mode with ID '{mode.Id}' already exists.");

            if (mode.AllowedTools == null || !mode.AllowedTools.Any())
                throw new ArgumentException("Mode must have at least one allowed tool.", nameof(mode));

            mode.IsBuiltIn = false;
            _modeSettings.CustomModes.Add(mode);
            SaveModeSettings();
            return true;
        }

        public bool RemoveCustomMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                throw new ArgumentNullException(nameof(modeId), "Mode ID cannot be null or empty.");

            var mode = _modeSettings.CustomModes.FirstOrDefault(m => m.Id == modeId);
            if (mode == null)
                throw new InvalidOperationException($"Custom mode with ID '{modeId}' does not exist.");

            if (mode.IsBuiltIn)
                throw new InvalidOperationException("Cannot remove built-in modes.");

            _modeSettings.CustomModes.Remove(mode);
            
            if (_currentMode.Id == modeId)
            {
                var defaultMode = GetMode("default");
                if (defaultMode == null)
                    throw new InvalidOperationException("Default mode not found.");
                    
                _currentMode = defaultMode;
                _modeSettings.ActiveModeId = "default";
            }
            
            SaveModeSettings();
            return true;
        }

        public bool IsToolAllowedInCurrentMode(string toolName)
        {
            return _currentMode.AllowedTools.Contains(toolName);
        }

        private void InitializeModeSettings()
        {
            var settings = _configurationService.LoadSettings();
            
            if (settings?.ModeSettings == null)
            {
                _modeSettings = new ModeSettings
                {
                    DefaultModes = BuiltInModes.GetBuiltInModes(),
                    CustomModes = new List<Mode>(),
                    ActiveModeId = "default"
                };
                SaveModeSettings();
                return;
            }

            _modeSettings = settings.ModeSettings;
            bool needsSave = false;

            if (_modeSettings.DefaultModes == null || _modeSettings.DefaultModes.Count == 0)
            {
                _modeSettings.DefaultModes = BuiltInModes.GetBuiltInModes();
                needsSave = true;
            }

            if (_modeSettings.CustomModes == null)
            {
                _modeSettings.CustomModes = new List<Mode>();
                needsSave = true;
            }

            _modeSettings.CustomModes = _modeSettings.CustomModes
                .Where(m => IsValidMode(m))
                .ToList();

            if (string.IsNullOrWhiteSpace(_modeSettings.ActiveModeId) || 
                !GetAvailableModes().Any(m => m.Id == _modeSettings.ActiveModeId))
            {
                _modeSettings.ActiveModeId = "default";
                needsSave = true;
            }

            if (needsSave)
            {
                SaveModeSettings();
            }
        }

        private bool IsValidMode(Mode mode)
        {
            if (mode == null) return false;
            if (string.IsNullOrWhiteSpace(mode.Id)) return false;
            if (mode.AllowedTools == null || !mode.AllowedTools.Any()) return false;
            
            var distinctTools = mode.AllowedTools.Distinct().ToList();
            if (distinctTools.Count != mode.AllowedTools.Count)
            {
                mode.AllowedTools = distinctTools;
            }

            return true;
        }

        private void SaveModeSettings()
        {
            var settings = _configurationService.LoadSettings() ?? new ApplicationSettings();
            settings.ModeSettings = _modeSettings;
            _configurationService.SaveSettings(settings);
        }
    }
}
