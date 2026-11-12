using System;
using System.Collections.Generic;
using System.IO;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Wrapper.Compatibility;

/// <summary>
/// Reads the wrapper's switches from settings.json next to RedoxExtensions.Wrapper.dll.  deploy.bat never
/// overwrites that file, so a hand-set value survives deploys.
/// </summary>
internal static class CompatibilitySettings
{
    public const string EnableLargeAddressAlignmentFixKey = "EnableLargeAddressAlignmentFix";

    public static string SettingsFilePath => Path.Combine(WrapperUtilities.RedoxExtensionsBinDirectory, "settings.json");

    public static bool ReadEnableLargeAddressAlignmentFix()
    {
        var path = SettingsFilePath;
        return !File.Exists(path) || IsLargeAddressAlignmentFixEnabled(File.ReadAllText(path));
    }

    /// <summary>Default true: only an explicit <c>"EnableLargeAddressAlignmentFix": false</c> turns the fix off.</summary>
    public static bool IsLargeAddressAlignmentFixEnabled(string settingsJson)
    {
        if (MiniJson.Parse(settingsJson.TrimStart('﻿')) is not IDictionary<string, object> settings)
            return true;
        return !settings.TryGetValue(EnableLargeAddressAlignmentFixKey, out var value) || value is not false;
    }
}
