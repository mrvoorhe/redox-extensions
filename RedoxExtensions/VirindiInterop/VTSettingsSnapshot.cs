using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.VirindiInterop
{
    public class VTSettingsSnapshot
    {
        private readonly Dictionary<string, string> _queriedSettings;

        public VTSettingsSnapshot(Dictionary<string, string> queriedSettings)
        {
            this._queriedSettings = queriedSettings;
        }

        #region Public Static Methods

        public static VTSettingsSnapshot Capture(IEnumerable<string> settingNames)
        {
            return new VTSettingsSnapshot(VTUtilities.GetVTOptions(settingNames));
        }

        #endregion

        public bool IsNavEnabled
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsCombatEnabled
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
