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

        public List<string> Mage;
        public List<string> AttackMage;
        public List<string> SupportMage;

        public List<string> Support;

        public List<string> Archer;
        public List<string> AttackArcher;
        public List<string> SupportArcher;

        public List<string> Melee;
    }
}
