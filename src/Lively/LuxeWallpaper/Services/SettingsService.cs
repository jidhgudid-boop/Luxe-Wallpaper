using LuxeWallpaper.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace LuxeWallpaper.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private UserSettings _currentSettings;

        public SettingsService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "LuxeWallpaper");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _settingsFilePath = Path.Combine(appFolder, "settings.json");
            LoadSettings();
        }

        public UserSettings GetSettings()
        {
            return _currentSettings;
        }

        public void SaveSettings(UserSettings settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
                _currentSettings = settings;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存设置失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _currentSettings = JsonConvert.DeserializeObject<UserSettings>(json);
                }
                else
                {
                    _currentSettings = new UserSettings();
                    SaveSettings(_currentSettings);
                }
            }
            catch
            {
                _currentSettings = new UserSettings();
            }
        }

        public void ResetSettings()
        {
            _currentSettings = new UserSettings();
            SaveSettings(_currentSettings);
        }
    }
}
