using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using RedoxExtensions.MagToolsInterop;
using RedoxExtensions.Settings;
using RedoxLib;

namespace RedoxExtensions.Mine;

/// <summary>
/// Hacks specific to my accounts
/// </summary>
public static class MyUtilities
{
    public const int NpcSleepDelayInMilliseconds = 1000;

    private static readonly HashSet<string> _whitelist = new();
    private static readonly Dictionary<string, int> _sleepDelayFactorByCharacter = new();


    internal static void Shutdown()
    {
        _whitelist.Clear();
        _sleepDelayFactorByCharacter.Clear();
    }

    public static void Init()
    {
        Shutdown();

        int counter = 0;
        foreach (var characters in ActiveSettings.Instance.CharactersGroupedByAccount)
        {
            foreach (var character in characters)
            {
                _whitelist.Add(character);
                _sleepDelayFactorByCharacter.Add(character, counter);
            }

            counter++;
        }
    }

    public static bool IsCharacterOnMasterWhiteList(string characterName)
    {
        // you is always okay.
        if (characterName.ToLower() == "you")
            return true;

        return _whitelist.Contains(characterName);
    }

    public static int GetNpcSleepDelayForCurrentAccount() => GetNpcSleepDelayForCurrentAccount(1000);

    private static int GetNpcSleepDelayFactorForCurrentAccount() => GetNpcSleepDelayFactor(CurrentCharacter.Name);

    public static int GetNpcSleepDelayFactor(string characterName)
    {
        if (_sleepDelayFactorByCharacter.TryGetValue(characterName, out var factor))
        {
            return factor;
        }

        throw new InvalidOperationException($"Unknown character : {characterName}");
    }

    public static int GetNpcSleepDelayForCurrentAccount(int baseDelayBetweenAccounts)
    {
        return GetNpcSleepDelayFactorForCurrentAccount() * baseDelayBetweenAccounts;
    }

    public static string LookupProfileName(string characterName, MyMainProfiles requestedProfile)
    {
        if (requestedProfile == MyMainProfiles.CharacterSpecificDefault)
        {
            if (ActiveSettings.Instance.VTProfiles.Main.CharacterDefaults.TryGetValue(characterName, out var profileName))
            {
                return profileName;
            }

            return ActiveSettings.Instance.VTProfiles.Main.Default;
        }

        return requestedProfile.ToString();
    }

    public static string LookupProfileName(MyMainProfiles requestedProfile) => LookupProfileName(CurrentCharacter.Name, requestedProfile);

    public static int GetFormationRangeForCurrentCharacter(string formation) => GetFormationRange(CurrentCharacter.Name, formation);

    public static int GetFormationRange(string characterName, string formationName)
    {
        if (ActiveSettings.Instance.Formations.TryGetValue(formationName.ToLower(), out var formation))
        {
            if (formation.RangeTable.TryGetValue(characterName, out var range))
            {
                return range;
            }

            return formation.RangeDefault;
        }

        throw new InvalidOperationException($"Unknown formation : {formationName}");
    }

    public static bool EnableLootForFormation(string characterName, string formationName)
    {
        if (ActiveSettings.Instance.Formations.TryGetValue(formationName.ToLower(), out var formation))
        {
            return formation.Looters.Contains(characterName);
        }

        throw new InvalidOperationException($"Unknown formation : {formationName}");
    }

    public static bool EnableLootForFormationForCurrentCharacter(string formation) => EnableLootForFormation(CurrentCharacter.Name, formation);

    public static void RecruitUsualSuspects()
    {
        foreach (var character in _whitelist)
        {
            MTActions.RecruitFellow(character);
        }
    }
}
