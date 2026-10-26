using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Continuations;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions
{
    internal static class UnstuckActions
    {
        private const int CombatWindowMilliseconds = 30000;
        private const int StrafeMilliseconds = 750;
        private const int UnstuckAttackRange = 4;

        internal static bool InProgress { get; private set; }

        internal static void Unstuck()
        {
            Unstuck(null);
        }

        internal static void Unstuck(Action onComplete)
        {
            if (InProgress)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] Unstuck already in progress");
                return;
            }

            var options = VTUtilities.GetVTOptions(new[] { VTOptionNames.EnableCombat, VTOptionNames.AttackDistance });

            string combatEnabled;
            options.TryGetValue(VTOptionNames.EnableCombat, out combatEnabled);

            string originalAttackDistance;
            options.TryGetValue(VTOptionNames.AttackDistance, out originalAttackDistance);

            if (string.Equals(combatEnabled, "True", StringComparison.OrdinalIgnoreCase))
            {
                REPlugin.Instance.Chat.WriteLine("[RE] Unstuck : clearing busy and wiggling");
                ClearBusyAndWiggle(onComplete);
                return;
            }

            InProgress = true;
            REPlugin.Instance.Chat.WriteLine("[RE] Unstuck : enabling combat for {0} seconds", CombatWindowMilliseconds / 1000);

            VTActions.SetAttackRange(UnstuckAttackRange);
            VTActions.EnableCombat();

            DelayedContinuation.ContinueAfterDelayOnGameThread(
                state =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(originalAttackDistance))
                        {
                            VTUtilities.SetOption(VTOptionNames.AttackDistance, originalAttackDistance);
                        }

                        VTActions.DisableCombat();
                        REPlugin.Instance.Chat.WriteLine("[RE] Unstuck : combat restored, clearing busy and wiggling");
                        ClearBusyAndWiggle(onComplete);
                    }
                    finally
                    {
                        InProgress = false;
                    }
                },
                CombatWindowMilliseconds,
                null);
        }

        private static void ClearBusyAndWiggle(Action onComplete)
        {
            VTActions.ClearBusy();

            ACUtilities.ProcessArbitraryCommand("/ub setmotion StrafeLeft 1");

            DelayedContinuation.ContinueAfterDelayOnGameThread(
                leftState =>
                {
                    ACUtilities.ProcessArbitraryCommand("/ub setmotion StrafeLeft 0");
                    ACUtilities.ProcessArbitraryCommand("/ub setmotion StrafeRight 1");

                    DelayedContinuation.ContinueAfterDelayOnGameThread(
                        rightState =>
                        {
                            ACUtilities.ProcessArbitraryCommand("/ub setmotion StrafeRight 0");

                            if (onComplete != null)
                            {
                                onComplete();
                            }
                        },
                        StrafeMilliseconds,
                        null);
                },
                StrafeMilliseconds,
                null);
        }
    }
}
