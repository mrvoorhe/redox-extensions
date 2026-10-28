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

        if (!VTActions.MetaProfileExists(normalizedMetaName))
        {
            chat.WriteLine("[RE] StartMeta : could not find meta file {0}.met", normalizedMetaName);
            return;
        }

        if (!VTActions.LoadSmartSettingsProfile(StartMetaSettingsProfile))
        {
            chat.WriteLine("[RE] StartMeta : could not find settings profile {0}", StartMetaSettingsProfile);
            return;
        }

        chat.WriteLine("[RE] StartMeta : {0}", normalizedMetaName);

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
            createState => RunStartMetaStage("start", () => StartMetaStart(normalizedMetaName)),
            MillisecondsDelayBeforeCreatingFellow,
            null);
    }

    private static void StartMetaStart(string normalizedMetaName)
    {
        REPlugin.Instance.Chat.WriteLine("[RE] StartMeta : loading meta and creating fellowship {0}", StartMetaFellowName);

        VTActions.SetAttackRange(StartMetaAttackRange);
        VTActions.LoadMetaProfile(normalizedMetaName);
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
