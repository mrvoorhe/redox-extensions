using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Core.Utilities;
using RedoxExtensions.MagToolsInterop;
using RedoxExtensions.Mine;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions;

/// <summary>
/// General purpose actions that a character can perform
/// </summary>
public static class CharacterActions
{
    internal static List<AccountCharacter> GetAccountCharacters()
    {
        var characters = new List<AccountCharacter>();
        foreach (AccountCharInfo info in REPlugin.Instance.CharacterFilter.Characters)
            characters.Add(new AccountCharacter { Name = info.Name, Id = info.Id, Index = info.Index });
        return AccountCharacters.SortedByName(characters);
    }

    public static void PullLegendaryKeys()
    {
        VTActions.StopVT();
        VTActions.DisableAllVTStates();

        VTActions.LoadMetaProfile(Settings.ActiveSettings.Instance.VTProfiles.Meta.PullLegendaryKeys);
        VTActions.LoadLootProfile(Settings.ActiveSettings.Instance.VTProfiles.Loot.LegendaryChestPulls);

        VTActions.SetMetaState("Check");

        VTActions.EnableMeta();
        VTActions.StartVT();
    }

    // Gives Mag-Filter's /mf lnc set time to take effect before the logout starts.
    private const int MillisecondsDelayBeforeSwitchLogout = 1000;

    internal static void SwitchCharacter(IList<string> args)
    {
        REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() => SwitchCharacterNow(args));
    }

    private static void SwitchCharacterNow(IList<string> args)
    {
        var chat = REPlugin.Instance.Chat;
        var names = GetAccountCharacters().ConvertAll(c => c.Name);

        if (!AccountCharacters.TryMatchSwitchArguments(args, names, REPlugin.Instance.CharacterFilter.Name, out var target, out var command, out var error))
        {
            chat.WriteLine("[RE] Switch : {0}", error);
            return;
        }

        chat.WriteLine(command == null ? "[RE] Switching to {0}" : "[RE] Switching to {0}, then running : {1}", target, command);

        ACUtilities.ProcessArbitraryCommand("/mf lnc set " + target);
        Continuations.DelayedContinuation.ContinueAfterDelayOnGameThread(_ => SimpleActions.Logout(), MillisecondsDelayBeforeSwitchLogout, null);
    }

    private const string StartMetaSettingsProfile = "Normal";
    private const string StartMetaLootProfile = "QuestLoot.utl";
    private const string StartMetaFellowName = "X";
    private const int StartMetaAttackRange = 20;
    private const string StartMetaFellowMessage = "!start";

    // Leaving and creating a fellowship each require a round trip to the server
    private const int MillisecondsDelayBeforeCreatingFellow = 1500;
    private const int MillisecondsDelayBeforeFellowMessage = 2000;

    public static void StartMeta(string metaName)
    {
        // Run on the next frame rather than inside the chat intercept of the /re command that triggered us.
        // Issuing many nested chat commands from inside that intercept can leave the /re command un-eaten.
        REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() => RunStartMetaStage("init", () => StartMetaInit(metaName)));
    }

    private static void StartMetaInit(string metaName)
    {
        var chat = REPlugin.Instance.Chat;
        var normalizedMetaName = VTActions.NormalizeMetaName(metaName ?? string.Empty);

        if (normalizedMetaName.Length == 0)
        {
            chat.WriteLine("[RE] StartMeta : no meta name given");
            return;
        }

        var matches = VTActions.FindMetaProfiles(normalizedMetaName);
        if (matches.Count == 0)
        {
            chat.WriteLine("[RE] StartMeta : could not find a meta file matching {0}", normalizedMetaName);
            return;
        }

        if (matches.Count > 1)
        {
            chat.WriteLine("[RE] StartMeta : {0} meta files matched {1} :", matches.Count, normalizedMetaName);
            foreach (var match in matches)
            {
                chat.WriteLine("[RE]   {0}.met", match);
            }

            return;
        }

        var resolvedMetaName = matches[0];

        if (!VTActions.LoadSmartSettingsProfile(StartMetaSettingsProfile))
        {
            chat.WriteLine("[RE] StartMeta : could not find settings profile {0}", StartMetaSettingsProfile);
            return;
        }

        chat.WriteLine("[RE] StartMeta : {0}", resolvedMetaName);

        VTActions.StopVT();
        VTActions.DisableNav();
        VTActions.DisableCombat();
        VTActions.DisableLooting();
        VTActions.EnableBuffing();
        VTActions.SetBoostNavPriority(false);
        VTActions.SetIdlePeaceMode(false);
        VTActions.LoadLootProfile(StartMetaLootProfile);

        // If we are not in a fellowship, this will harmlessly fail
        MTActions.QuitFellow();

        Continuations.DelayedContinuation.ContinueAfterDelayOnGameThread(
            createState => RunStartMetaStage("start", () => StartMetaStart(resolvedMetaName)),
            MillisecondsDelayBeforeCreatingFellow,
            null);
    }

    private static void StartMetaStart(string metaName)
    {
        REPlugin.Instance.Chat.WriteLine("[RE] StartMeta : loading meta and creating fellowship {0}", StartMetaFellowName);

        VTActions.SetAttackRange(StartMetaAttackRange);
        VTActions.LoadMetaProfile(metaName);
        VTActions.EnableMeta();
        VTActions.StartVT();

        // MagTools creates the fellowship by driving the UI over the next few frames, so do it last
        MTActions.CreateFellow(StartMetaFellowName);

        Continuations.DelayedContinuation.ContinueAfterDelayOnGameThread(
            messageState => RunStartMetaStage("fellow message", () => TellActions.TellNativeFellow(StartMetaFellowMessage)),
            MillisecondsDelayBeforeFellowMessage,
            null);
    }

    private static void RunStartMetaStage(string stageName, Action stage)
    {
        try
        {
            stage();
        }
        catch (Exception e)
        {
            // Release builds don't write errors anywhere visible, so always report failures in chat
            REPlugin.Instance.Chat.WriteLine("[RE] StartMeta : {0} failed : {1}", stageName, e.Message);
            REPlugin.Instance.Error.LogError(e);
        }
    }

    public static bool SetMainProfileRaw(string profileName) => VTActions.LoadCharacterSettingsProfile(profileName);

    public static bool SetMainProfile(string profileName)
    {
        try
        {
            return SetMainProfile((MyMainProfiles)Enum.Parse(typeof(MyMainProfiles), profileName, true));
        }
        catch (ArgumentException)
        {
            // It's not one of my profiles.  See if there is a match
            // for the name as is.
            return VTActions.LoadSmartSettingsProfile(profileName);
        }
    }

    public static bool SetMainProfile(MyMainProfiles myProfile) => SetMainProfileRaw(MyUtilities.LookupProfileName(myProfile));
}
