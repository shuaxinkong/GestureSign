﻿using GestureSign.Common.Log;
using ManagedWinapi.Hooks;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading;
using Windows.Storage;

namespace GestureSign.Common.Configuration
{
    public class AppConfig
    {
        static System.Configuration.Configuration _config;
        static Timer Timer;
        public static event EventHandler ConfigChanged;

        private static string ConfigPath;

        private static ExeConfigurationFileMap ExeMap;

        public static string ApplicationDataPath { private set; get; }

        public static string LocalApplicationDataPath { private set; get; }

        public static System.Drawing.Color VisualFeedbackColor
        {
            get
            {
                return (System.Drawing.Color)GetValue("VisualFeedbackColor", System.Drawing.Color.DeepSkyBlue);
            }
            set
            {
                SetValue("VisualFeedbackColor", value);
            }
        }

        public static int VisualFeedbackWidth
        {
            get
            {
                return (int)GetValue("VisualFeedbackWidth", 9);
            }
            set
            {
                SetValue("VisualFeedbackWidth", value);
            }
        }
        public static int MinimumPointDistance
        {
            get
            {
                return (int)GetValue("MinimumPointDistance", 20);
            }
            set
            {
                SetValue("MinimumPointDistance", value);
            }
        }

        public static double Opacity
        {
            get
            {
                return (double)GetValue("Opacity", 0.35);
            }
            set
            {
                SetValue("Opacity", value);
            }
        }

        public static bool IsOrderByLocation
        {
            get
            {
                return (bool)GetValue("IsOrderByLocation", true);
            }
            set
            {
                SetValue("IsOrderByLocation", value);
            }
        }

        public static bool UiAccess { get; set; }
        public static bool ShowTrayIcon
        {
            get
            {
                return (bool)GetValue("ShowTrayIcon", true);
            }
            set
            {
                SetValue("ShowTrayIcon", value);
            }
        }
        public static bool ShowBalloonTip
        {
            get
            {
                return (bool)GetValue("ShowBalloonTip", false);
            }
            set
            {
                SetValue("ShowBalloonTip", value);
            }
        }
        public static string CultureName
        {
            get
            {
                return (string)GetValue("CultureName", "");
            }
            set
            {
                SetValue("CultureName", value);
            }
        }

        public static bool SendErrorReport
        {
            get
            {
                return (bool)GetValue("SendErrorReport", true);
            }
            set
            {
                SetValue("SendErrorReport", value);
            }
        }

        public static DateTime LastErrorTime
        {
            get
            {
                return GetValue("LastErrorTime", DateTime.MinValue);
            }
            set
            {
                SetValue("LastErrorTime", value);
            }
        }

        public static int InitialTimeout
        {
            get
            {
                return (int)GetValue(nameof(InitialTimeout), 0);
            }
            set
            {
                SetValue(nameof(InitialTimeout), value);
            }
        }

        public static MouseActions DrawingButton
        {
            get
            {
                return (MouseActions)GetValue(nameof(DrawingButton), 0);
            }
            set
            {
                SetValue(nameof(DrawingButton), (int)value);
            }
        }

        public static bool RegisterTouchPad
        {
            get
            {
                return (bool)GetValue(nameof(RegisterTouchPad), false);
            }
            set
            {
                SetValue(nameof(RegisterTouchPad), value);
            }
        }

        public static bool IgnoreFullScreen
        {
            get
            {
                return GetValue(nameof(IgnoreFullScreen), true);
            }
            set
            {
                SetValue(nameof(IgnoreFullScreen), value);
            }
        }

        static AppConfig()
        {
#if uiAccess
            UiAccess = true;
#endif
            ConfigPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"GestureSign\GestureSign.config");
            ApplicationDataPath = System.IO.Path.GetDirectoryName(ConfigPath);
            LocalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestureSign");

            if (!Directory.Exists(ApplicationDataPath))
                Directory.CreateDirectory(ApplicationDataPath);

            FileManager.WaitFile(ConfigPath);

            ExeMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = ConfigPath,
                RoamingUserConfigFilename = ConfigPath,
                LocalUserConfigFilename = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GestureSign\GestureSign.config")
            };

            _config = ConfigurationManager.OpenMappedExeConfiguration(ExeMap, ConfigurationUserLevel.None);
            Timer = new Timer(SaveFile, null, Timeout.Infinite, Timeout.Infinite);
        }

        public static void Reload()
        {
            try
            {
                FileManager.WaitFile(ConfigPath);

                _config = ConfigurationManager.OpenMappedExeConfiguration(ExeMap, ConfigurationUserLevel.None);
                // ConfigurationManager.RefreshSection("appSettings");
                if (ConfigChanged != null)
                    ConfigChanged(new object(), EventArgs.Empty);
            }
            catch (Exception e) { Logging.LogException(e); }
        }


        private static void Save()
        {
            Timer.Change(100, Timeout.Infinite);
        }

        private static void SaveFile(object state)
        {
            try
            {
                FileManager.WaitFile(ConfigPath);
                // Save the configuration file.    
                _config.AppSettings.SectionInformation.ForceSave = true;
                _config.Save(ConfigurationSaveMode.Modified);
            }
            catch (ConfigurationErrorsException)
            {
                Reload();
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
            // Force a reload of the changed section.    
            ConfigurationManager.RefreshSection("appSettings");
            ConfigChanged?.Invoke(new object(), EventArgs.Empty);
        }

        private static T GetValue<T>(string key, T defaultValue, Func<string, T> converter)
        {
            var setting = _config.AppSettings.Settings[key];
            if (setting != null)
            {
                try
                {
                    return converter(setting.Value);
                }
                catch
                {
                    _config.AppSettings.Settings.Remove(key);
                    return defaultValue;
                }
            }
#if ConvertedDesktopApp
            var roamingSetting = GetRoamingSetting(key);
            if (roamingSetting != null)
            {
                try
                {
                    return (T)roamingSetting;
                }
                catch
                {
                    return defaultValue;
                }
            }
#endif
            return defaultValue;
        }

        private static int GetValue(string key, int defaultValue)
        {
            return GetValue(key, defaultValue, s => int.Parse(s));
        }

        private static double GetValue(string key, double defaultValue)
        {
            return GetValue(key, defaultValue, s => double.Parse(s));
        }

        private static bool GetValue(string key, bool defaultValue)
        {
            return GetValue(key, defaultValue, s => bool.Parse(s));
        }

        private static string GetValue(string key, string defaultValue)
        {
            return GetValue(key, defaultValue, s => s);
        }

        private static DateTime GetValue(string key, DateTime defaultValue)
        {
            string setting = GetValue(key, string.Empty);
            if (!string.IsNullOrEmpty(setting))
            {
                try
                {
                    return DateTime.Parse(setting);
                }
                catch
                {
                    _config.AppSettings.Settings.Remove(key);
                    return defaultValue;
                }
            }
            else return defaultValue;
        }

        private static System.Drawing.Color GetValue(string key, System.Drawing.Color defaultValue)
        {
            string setting = GetValue(key, string.Empty);
            if (!string.IsNullOrEmpty(setting))
            {
                try
                {
                    return System.Drawing.ColorTranslator.FromHtml(setting);
                }
                catch
                {
                    _config.AppSettings.Settings.Remove(key);
                    return defaultValue;
                }
            }
            else return defaultValue;
        }

        private static void SetValue<T>(string key, T value)
        {
            if (_config.AppSettings.Settings[key] != null)
            {
                _config.AppSettings.Settings[key].Value = value.ToString();
            }
            else
            {
                _config.AppSettings.Settings.Add(key, value.ToString());
            }
            Save();

#if ConvertedDesktopApp
            SetRoamingSetting(key, value);
#endif
        }

        private static void SetValue(string key, System.Drawing.Color value)
        {
            SetValue(key, System.Drawing.ColorTranslator.ToHtml(value));
        }

        private static void SetValue(string key, DateTime value)
        {
            SetValue(key, value.ToString(CultureInfo.InvariantCulture));
        }

#if ConvertedDesktopApp
        private static bool SetRoamingSetting<T>(string key, T value)
        {
            try
            {
                ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
                roamingSettings.Values[key] = value;
                return true;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return false;
            }
        }

        private static object GetRoamingSetting(string key)
        {
            try
            {
                ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
                return roamingSettings.Values[key];
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return null;
            }
        }
#endif
    }
}
