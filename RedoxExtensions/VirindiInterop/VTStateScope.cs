using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using RedoxExtensions.Wrapper;

namespace RedoxExtensions.VirindiInterop
{
    [Flags]
    public enum VTStateOptions
    {
        DontChange = 0,
        Combat = 1,
        Nav = 2,
        Looting = 4,
        Buffing = 8,
        Meta = 16
    }

    public class VTStateScope : IDisposable
    {
        private readonly Dictionary<string, string> _previousStateOptions;

        public VTStateScope(VTStateOptions desiredEnabledStates, VTStateOptions desiredDisabledStates)
        {
            List<string> optionsToCheck = new List<string>();

            // Only check the options that will get changed.  That way we don't even mess with options
            // the scope doesn't care about.
            AddOptionIfGoingToChange(optionsToCheck, desiredEnabledStates, desiredDisabledStates, VTStateOptions.Combat, VTOptionNames.EnableCombat);
            AddOptionIfGoingToChange(optionsToCheck, desiredEnabledStates, desiredDisabledStates, VTStateOptions.Nav, VTOptionNames.EnableNav);
            AddOptionIfGoingToChange(optionsToCheck, desiredEnabledStates, desiredDisabledStates, VTStateOptions.Looting, VTOptionNames.EnableLooting);
            AddOptionIfGoingToChange(optionsToCheck, desiredEnabledStates, desiredDisabledStates, VTStateOptions.Buffing, VTOptionNames.EnableBuffing);
            AddOptionIfGoingToChange(optionsToCheck, desiredEnabledStates, desiredDisabledStates, VTStateOptions.Meta, VTOptionNames.EnableMeta);

            this._previousStateOptions = VTUtilities.GetVTOptions(optionsToCheck);

            REPlugin.Instance.Debug.WriteObject(this._previousStateOptions, "VTStateScope-CurrentBackup");

            // Apply enables first
            if (desiredEnabledStates != VTStateOptions.DontChange)
            {
                CheckAndSetState(desiredEnabledStates, VTStateOptions.Combat, VTActions.EnableCombat);
                CheckAndSetState(desiredEnabledStates, VTStateOptions.Nav, VTActions.EnableNav);
                CheckAndSetState(desiredEnabledStates, VTStateOptions.Looting, VTActions.EnableLooting);
                CheckAndSetState(desiredEnabledStates, VTStateOptions.Buffing, VTActions.EnableBuffing);
                CheckAndSetState(desiredEnabledStates, VTStateOptions.Meta, VTActions.EnableMeta);
            }

            // Then apply disables.  Yes, they could overwrite, consider that programmer error
            if (desiredDisabledStates != VTStateOptions.DontChange)
            {
                CheckAndSetState(desiredDisabledStates, VTStateOptions.Combat, VTActions.DisableCombat);
                CheckAndSetState(desiredDisabledStates, VTStateOptions.Nav, VTActions.DisableNav);
                CheckAndSetState(desiredDisabledStates, VTStateOptions.Looting, VTActions.DisableLooting);
                CheckAndSetState(desiredDisabledStates, VTStateOptions.Buffing, VTActions.DisableBuffing);
                CheckAndSetState(desiredDisabledStates, VTStateOptions.Meta, VTActions.DisableMeta);
            }
        }

        public static VTStateScope Enter(VTStateOptions enabledStates, VTStateOptions disabledStates)
        {
            return new VTStateScope(enabledStates, disabledStates);
        }

        public static VTStateScope EnterAllDisabled()
        {
            return new VTStateScope(VTStateOptions.DontChange, VTStateOptions.Nav | VTStateOptions.Meta | VTStateOptions.Looting | VTStateOptions.Combat | VTStateOptions.Buffing);
        }

        public static VTStateScope EnterNavDisabled()
        {
            return new VTStateScope(VTStateOptions.DontChange, VTStateOptions.Nav);
        }

        public void Dispose()
        {
            // Restore original state
            REPlugin.Instance.Debug.WriteObject(this._previousStateOptions, "VTStateScope-RestoringBackup");
            VTUtilities.SetOptions(this._previousStateOptions);
        }

        internal static void AddOptionIfGoingToChange(List<string> optionsToCheck, VTStateOptions desiredEnabledStates, VTStateOptions desiredDisabledStates, VTStateOptions checkState, string optionValueToAdd)
        {
            if ((desiredEnabledStates & checkState) != 0 || (desiredDisabledStates & checkState) != 0)
            {
                optionsToCheck.Add(optionValueToAdd);
            }
        }

        private static void CheckAndSetState(VTStateOptions desiredState, VTStateOptions checkState, SafeAction matchAction)
        {
            if ((desiredState & checkState) != 0)
            {
                matchAction();
            }
        }
    }
}
