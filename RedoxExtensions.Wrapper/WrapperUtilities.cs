using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

using Decal.Adapter;

namespace RedoxExtensions.Wrapper;

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


    public static string RedoxExtensionsBinDirectory => _redoxExtensionsBinDirectory;

    public static string LogDirectory => _logDirectory;

    public static string TmpDllDirectory => _tmpDllDirectory;

    internal static string DllFilePathToPdbFilePath(string dllFilePath) => dllFilePath.Substring(0, dllFilePath.Length - 4) + ".pdb";

    /// <summary>
    /// A copy folder younger than this may belong to a client that is starting up and hasn't loaded its dlls yet.
    /// </summary>
    internal static readonly TimeSpan TmpDllMinimumAge = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Deletes the copy folders that no client is using, on a thread pool thread so startup doesn't wait for it.
    /// </summary>
    internal static void CleanupTmpDllsInBackground()
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                CleanupTmpDllFolders(TmpDllDirectory, DateTime.UtcNow, TmpDllMinimumAge);
            }
            catch (Exception)
            {
                // Best effort.  Whatever is left gets another try at the next startup.
            }
        });
    }

    /// <summary>
    /// Every load copies the plugin into its own folder under <paramref name="root"/> (see CreateWrappedPluginInstance),
    /// and a loaded assembly can't be unloaded, so the folders pile up.  Deletes each folder that is at least
    /// <paramref name="minimumAge"/> old and whose dlls no client has loaded.  A folder in use is left whole: the
    /// running plugin also reads its settings files from there.  Returns how many folders were deleted.
    /// </summary>
    internal static int CleanupTmpDllFolders(string root, DateTime nowUtc, TimeSpan minimumAge)
    {
        if (!Directory.Exists(root))
            return 0;

        var deleted = 0;
        foreach (var folder in Directory.GetDirectories(root))
        {
            try
            {
                if (nowUtc - Directory.GetCreationTimeUtc(folder) < minimumAge || AnyDllInUse(folder))
                    continue;

                Directory.Delete(folder, true);
                deleted++;
            }
            catch (Exception)
            {
                // Another client may be loading from or cleaning up the same folder.  Leave it for next time.
            }
        }

        return deleted;
    }

    private static bool AnyDllInUse(string folder)
    {
        foreach (var dll in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                // A loaded assembly is mapped into the client, which stops anyone opening it for writing.
                using (new FileStream(dll, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        return false;
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

        var newFilePathBase = Path.Combine(newDirectory, $"{assemblyName}-{tempFileName}");
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
        CopyDirectoryDependency(tmpDllDirectory, "Databases");
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

    private static void CopyDirectoryDependency(string tmpDllDirectory, string directoryName)
    {
        var sourcePath = Path.Combine(RedoxExtensionsBinDirectory, directoryName);

        var destPath = Path.Combine(tmpDllDirectory, Path.GetFileName(directoryName));

        Directory.CreateDirectory(destPath);

        foreach (var sourceFile in Directory.GetFiles(sourcePath))
        {
            File.Copy(sourceFile, Path.Combine(destPath, Path.GetFileName(sourceFile)));
        }
    }
}
