using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RedoxExtensions.Wrapper.Diagnostics
{
    public class ErrorLogger
    {
        private readonly string _currentPath;
        private readonly object _writeLock = new object();
        private readonly IPluginServices _pluginServices;

        public ErrorLogger(IPluginServices pluginServices, string logNamePrefix, string logNamePostFix)
        {
            this._pluginServices = pluginServices;
            var logFileName = string.Format("{0}_Errors_{1}.txt", logNamePrefix, logNamePostFix);

            this._currentPath = Path.Combine(WrapperUtilities.LogDirectory, logFileName);
        }

        public ErrorLogger(IPluginServices pluginServices, string logNamePrefix)
            : this(pluginServices, logNamePrefix, pluginServices.CoreManager.CharacterFilter.Name)
        {
        }

        public void LogError(Exception ex)
        {
            lock (_writeLock)
            {
                using (StreamWriter errorStream = new StreamWriter(this._currentPath, true))
                {
                    errorStream.WriteLine("============================================================================");
                    errorStream.WriteLine(DateTime.Now.ToString());
                    errorStream.WriteLine("Error: " + ex.Message);
                    errorStream.WriteLine("Source: " + ex.Source);
                    errorStream.WriteLine("Stack: " + ex.ToFullStackTrace());
                    //if (ex.InnerException != null)
                    //{
                    //    errorStream.WriteLine("Inner: " + ex.InnerException.Message);
                    //    errorStream.WriteLine("Inner Stack: " + ex.InnerException.StackTrace);
                    //}
                    errorStream.WriteLine("============================================================================");
                    errorStream.WriteLine("");
                }
            }

            // Get some message in game so it's easy to see there was a problem
            if (this._pluginServices.Debug != null)
            {
                this._pluginServices.Debug.WriteLine(ex.ToString());
            }
        }
    }
}
