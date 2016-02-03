using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data
{
    /// <summary>
    /// Holds settings that can be persisted
    /// </summary>
    public class RTSettings
    {
        private static readonly string _defaultSettingsLocation = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;

        private RTSettings(string rootDirectory, string characterName)
        {
            // TODO : Implement
            this.DebugLevel = DebugLevel.Medium;
        }

        public static RTSettings Resume(string characterName)
        {
            // TODO : Use XmlSerializer - http://support.microsoft.com/kb/815813
            return new RTSettings(_defaultSettingsLocation, characterName);
        }

        public void Save()
        {
            //throw new NotImplementedException();
            // TODO : Implement
        }

        public DebugLevel DebugLevel { get; set; }
    }
}
