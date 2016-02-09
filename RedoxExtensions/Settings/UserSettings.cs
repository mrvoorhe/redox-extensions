using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Settings
{
    public class UserSettings
    {
        public List<List<string>> CharactersGroupedByAccount;
        public Dictionary<string, Formation> Formations;
        public VTProfiles VTProfiles;
        public Diagnostics.DebugLevel DebugLevel;
    }
}
