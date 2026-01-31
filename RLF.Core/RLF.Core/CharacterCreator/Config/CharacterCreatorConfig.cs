using System;
using System.IO;
using Newtonsoft.Json;

namespace RLF.Core.CharacterCreator.Config
{
    public static class CharacterCreatorConfig
    {
        private static CharacterCreatorSettings _settings;
        private static string _configPath;
        private static JsonSerializerSettings _jsonSettings;

        static CharacterCreatorConfig()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public static CharacterCreatorSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    Load();
                }
                return _settings;
            }
        }

        public static void Initialize(string configPath = null)
        {
            _configPath = configPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RLF", "Config", "character_creator.json"
            );

            Load();
        }

        public static void Load()
        {
            try
            {
                if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _settings = JsonConvert.DeserializeObject<CharacterCreatorSettings>(json, _jsonSettings);
                }
                else
                {
                    _settings = new CharacterCreatorSettings();
                    Save();
                }
            }
            catch
            {
                _settings = new CharacterCreatorSettings();
            }

            if (_settings == null)
                _settings = new CharacterCreatorSettings();
        }

        public static void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(_configPath)) return;

                var directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(_settings, _jsonSettings);
                File.WriteAllText(_configPath, json);
            }
            catch
            {
                // Silently fail
            }
        }

        public static void ResetToDefault()
        {
            _settings = new CharacterCreatorSettings();
            Save();
        }
    }
}