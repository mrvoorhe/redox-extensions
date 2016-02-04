using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NiceIO;

namespace RedoxExtensions.Settings
{
    public static class ActiveSettings
    {
        private static UserSettings _activeSettings;

        public static void Clear()
        {
            _activeSettings = null;
        }

        public static void Init(UserSettings userSettings)
        {
            _activeSettings = userSettings;
        }

        public static UserSettings Instance
        {
            get
            {
                if (_activeSettings == null)
                {
                    var mainSettings = JsonConvert.DeserializeObject<Main>(GetMainSettingsFilePath().ReadAllText());
                    var expanded = Environment.ExpandEnvironmentVariables(mainSettings.UserSettingsFilePath);
                    _activeSettings = JsonConvert.DeserializeObject<UserSettings>(System.IO.File.ReadAllText(expanded));
                }

                return _activeSettings;
            }
        }

        private static NPath GetMainSettingsFilePath()
        {
            return new Uri(typeof(Main).Assembly.CodeBase).LocalPath.ToNPath().Parent.Combine("settings.json");
        }
    }
}
