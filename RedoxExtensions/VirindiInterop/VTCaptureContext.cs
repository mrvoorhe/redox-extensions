using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Mine;

namespace RedoxExtensions.VirindiInterop
{
    /// <summary>
    /// Creates a scope in which to query & capture Virindi settings
    /// </summary>
    public class VTCaptureContext : IDisposable
    {
        public VTCaptureContext()
        {
            this.CapturedSettings = new Dictionary<string, string>();

            REPlugin.Instance.CoreManager.CommandLineText += Current_CommandLineText;
        }

        public static string BuildQueryOptionCommand(string optionName)
        {
            return string.Format("/vt opt get {0}", optionName);
        }

        public Dictionary<string, string> CapturedSettings { get; private set; }

        public void QueryOption(string optionName)
        {
            ACUtilities.ProcessArbitraryCommand(BuildQueryOptionCommand(optionName));
        }

        public void QueryOptions(IEnumerable<string> optionNames)
        {
            foreach (var optionName in optionNames)
            {
                this.QueryOption(optionName);
            }
        }

        public void Dispose()
        {
            REPlugin.Instance.CoreManager.CommandLineText -= Current_CommandLineText;
        }

        private void Current_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                KeyValuePair<string, string> keyValue;
                if (VTUtilities.TryParseOptionOutput(e.Text, out keyValue))
                {
                    e.Eat = true;

                    this.CapturedSettings.Add(keyValue.Key, keyValue.Value);

                    REPlugin.Instance.Debug.WriteLine("Captured VT Option : {0} = {1}", keyValue.Key, keyValue.Value);
                }
            });
        }
    }
}
