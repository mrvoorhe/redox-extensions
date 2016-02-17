using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Decal.Adapter;

namespace RedoxExtensions.Wrapper
{
    public static class WrapperUtilities
    {
        private static readonly string _redoxExtensionsBinDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
        private static readonly string _logDirectory = Path.Combine(_redoxExtensionsBinDirectory, "Logs");
        private static readonly string _tmpDllDirectory = Path.Combine(_redoxExtensionsBinDirectory, "TmpDlls");
        private static readonly string _signalsDirectory = Path.Combine(_redoxExtensionsBinDirectory, "Signals");

        private static string _currentCopyDir;

        static WrapperUtilities()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            if (!Directory.Exists(_tmpDllDirectory))
            {
                Directory.CreateDirectory(_tmpDllDirectory);
            }

            if (!Directory.Exists(_signalsDirectory))
            {
                Directory.CreateDirectory(_signalsDirectory);
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }


        public static string RedoxExtensionsBinDirectory
        {
            get
            {
                return _redoxExtensionsBinDirectory;
            }
        }

        public static string LogDirectory
        {
            get
            {
                return _logDirectory;
            }
        }

        public static string TmpDllDirectory
        {
            get
            {
                return _tmpDllDirectory;
            }
        }

        internal static string DllFilePathToPdbFilePath(string dllFilePath)
        {
            return dllFilePath.Substring(0, dllFilePath.Length - 4) + ".pdb";
        }

        internal static void TryCleanupTmpDlls()
        {
            var clients = System.Diagnostics.Process.GetProcessesByName("acclient.exe");

            // If there is more than 1 client running, don't even bother trying to clean up.  It would have already tried
            if (clients.Length > 1)
            {
                return;
            }

            // try/catch => ignore errors because other AC instances could be running (or starting) and using the dlls.
            // we are just trying to clean up what we can so that there isn't a massive accumulation of files.
            foreach (var file in Directory.GetFiles(TmpDllDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);

                    // If we don't fail to delete the .dll, then also try to delete the pdb
                    // If we fail to delete the .dll, it's probably because it's in use still, so no sense in
                    // trying to delete the pdb
                    var pdbFile = DllFilePathToPdbFilePath(file);
                    File.Delete(pdbFile);

                }
                catch (Exception)
                {
                }
            }
        }

        internal static IWrappedPlugin CreateWrappedPluginInstance(string assemblyName)
        {
            var fullAssemblyPath = Path.Combine(RedoxExtensionsBinDirectory, assemblyName + ".dll");
            var fullPdbAssemblyPath = Path.Combine(RedoxExtensionsBinDirectory, assemblyName + ".pdb");

            // Copy to a tmp name since we can't fully unload it.
            var tempFileName = Path.GetRandomFileName();

            var newDirectory = Path.Combine(TmpDllDirectory, tempFileName);

            // Cache the current dll directory so that when the assembly resolve fires we can locate our dependencies
            _currentCopyDir = newDirectory;

            Directory.CreateDirectory(newDirectory);

            CopyDependencies(newDirectory);

            var newFilePathBase = Path.Combine(newDirectory, string.Format("{0}-{1}", assemblyName, tempFileName));
            var newDllFilePath = newFilePathBase + ".dll";
            var newPdbFilePath = newFilePathBase + ".pdb";

            File.Copy(fullAssemblyPath, newDllFilePath);

            if (File.Exists(fullPdbAssemblyPath))
            {
                File.Copy(fullPdbAssemblyPath, newPdbFilePath);
            }

            Assembly assembly = Assembly.LoadFile(newDllFilePath);

            Type mostDerived = null;
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                Type intrface = type.GetInterface(typeof(IWrappedPlugin).Name);
                if (intrface == null)
                    continue;

                if (mostDerived == null || type.IsSubclassOf(mostDerived))
                    mostDerived = type;
            }

            if (mostDerived != null)
                return assembly.CreateInstance(mostDerived.FullName) as IWrappedPlugin;


            throw new InvalidOperationException("No IWrappedPlugin found");
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!string.IsNullOrEmpty(_currentCopyDir))
            {
                string searchedAssemblyName = new AssemblyName(args.Name).Name + ".dll";

                var possiblePath = Path.Combine(_currentCopyDir, searchedAssemblyName);
                if (File.Exists(possiblePath))
                {
                    return Assembly.LoadFrom(possiblePath);
                }
            }

            return null;
        }

        private static void CopyDependencies(string tmpDllDirectory)
        {
            CopyLibraryDependency(tmpDllDirectory, "RedoxLib");
            CopyLibraryDependency(tmpDllDirectory, "Newtonsoft.Json");
            CopyFileDependency(tmpDllDirectory, "mysettings.json");
            CopyFileDependency(tmpDllDirectory, "settings.json");
        }

        private static void CopyLibraryDependency(string tmpDllDirectory, string assemblyName)
        {
            var fullAssemblyPath = Path.Combine(RedoxExtensionsBinDirectory, assemblyName + ".dll");
            var fullPdbAssemblyPath = Path.Combine(RedoxExtensionsBinDirectory, assemblyName + ".pdb");

            var newFilePathBase = Path.Combine(tmpDllDirectory, assemblyName);

            var newDllFilePath = newFilePathBase + ".dll";
            var newPdbFilePath = newFilePathBase + ".pdb";

            File.Copy(fullAssemblyPath, newDllFilePath);

            if (File.Exists(fullPdbAssemblyPath))
            {
                File.Copy(fullPdbAssemblyPath, newPdbFilePath);
            }
        }

        private static void CopyFileDependency(string tmpDllDirectory, string filename)
        {
            var fullSourcePath = Path.Combine(RedoxExtensionsBinDirectory, filename);

            var destFilePath = Path.Combine(tmpDllDirectory, Path.GetFileName(filename));

            File.Copy(fullSourcePath, destFilePath);
        }
    }
}
