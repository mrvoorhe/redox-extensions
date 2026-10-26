using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib;

/// <summary>
/// A class full of helpful shortcuts to information about the current character
/// </summary>
public static class CurrentCharacter
{
    public static string Name => PluginProvider.Instance.CharacterFilter.Name;
}
