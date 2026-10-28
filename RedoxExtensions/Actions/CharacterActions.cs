using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.MagToolsInterop;
using RedoxExtensions.Mine;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions;

/// <summary>
/// General purpose actions that a character can perform
/// </summary>
public static class CharacterActions
{
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
