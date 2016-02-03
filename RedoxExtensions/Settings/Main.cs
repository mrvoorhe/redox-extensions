using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;

namespace RedoxExtensions.Settings
{
    public class Main
    {
        private static Main _activeSettings;

        public string UserSettingsFilePath;

        public static Main Instance
        {
            get
            {
                if (_activeSettings == null)
                    _activeSettings = JsonConvert.DeserializeObject<Main>(GetMainSettingsFilePath().ReadAllText());

                return _activeSettings;
            }
        }

        public UserSettings User
        {
            get
            {
                var expanded = Environment.ExpandEnvironmentVariables(UserSettingsFilePath);
                return JsonConvert.DeserializeObject<UserSettings>(System.IO.File.ReadAllText(expanded));
            }
        }

        private static NPath GetMainSettingsFilePath()
        {
            return new Uri(typeof (Main).Assembly.CodeBase).LocalPath.ToNPath().Parent.Combine("settings.json");
        }
    }
}
