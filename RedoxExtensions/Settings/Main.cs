using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Settings
{
    public class Main
    {
        public string UserSettingsFilePath;

        public UserSettings User
        {
            get
            {
                // TODO : Cache in singleton style
                var mainSettings = JsonConvert.DeserializeObject<Main>(System.IO.File.ReadAllText(GetMainSettingsFilePath()));
                var expanded = Environment.ExpandEnvironmentVariables(mainSettings.UserSettingsFilePath);
                return JsonConvert.DeserializeObject<UserSettings>(System.IO.File.ReadAllText(expanded));
            }
        }

        private static string GetMainSettingsFilePath()
        {
            // TODO : Load from along side dll.  Use GetCodeBase
            throw new NotImplementedException();
        }
    }
}
