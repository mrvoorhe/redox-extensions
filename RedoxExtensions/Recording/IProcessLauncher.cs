using System.Diagnostics;

namespace RedoxExtensions.Recording;

public interface IProcessLauncher
{
    /// <summary>Starts the program without waiting for it and returns its process id.  Throws when it can't start.</summary>
    int Launch(string program, string arguments, string workingDirectory);
}

public sealed class ProcessLauncher : IProcessLauncher
{
    public int Launch(string program, string arguments, string workingDirectory)
    {
        var info = new ProcessStartInfo(program, arguments ?? string.Empty)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
        };

        using var process = Process.Start(info);
        return process.Id;
    }
}
