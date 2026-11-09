using System;
using System.Globalization;
using System.IO;
using System.Linq;

using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Core;

/// <summary>
/// The command <c>/re switch</c> hands to the next character.  Saved before logout, taken once by that character
/// when its plugin initializes.
/// </summary>
internal class SwitchHandoff
{
    public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(30);

    private readonly string _directory;
    private readonly Func<DateTime> _utcNow;

    public SwitchHandoff(string directory = null, Func<DateTime> utcNow = null)
    {
        this._directory = directory ?? Path.Combine(WrapperUtilities.LogDirectory, "switch");
        this._utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public string FilePathFor(string server, string account, string character)
    {
        var name = $"{server}-{account}-{character}".ToLowerInvariant();
        var invalid = Path.GetInvalidFileNameChars().Concat("\\/:*?\"<>|").ToArray();
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return Path.Combine(this._directory, safe + ".txt");
    }

    public void Save(string server, string account, string character, string command)
    {
        Directory.CreateDirectory(this._directory);
        var timestamp = this._utcNow().ToString("o", CultureInfo.InvariantCulture);
        File.WriteAllText(this.FilePathFor(server, account, character), timestamp + Environment.NewLine + command);
    }

    public void Clear(string server, string account, string character)
    {
        var path = this.FilePathFor(server, account, character);
        if (File.Exists(path))
            File.Delete(path);
    }

    public bool TryTake(string server, string account, string character, TimeSpan maxAge, out string command)
    {
        command = null;
        var path = this.FilePathFor(server, account, character);
        if (!File.Exists(path))
            return false;

        var lines = File.ReadAllLines(path);
        File.Delete(path);

        if (lines.Length < 2
            || !DateTime.TryParse(lines[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var savedUtc)
            || this._utcNow() - savedUtc > maxAge)
            return false;

        command = string.Join(" ", lines.Skip(1).ToArray()).Trim();
        if (command.Length == 0)
        {
            command = null;
            return false;
        }

        return true;
    }
}
