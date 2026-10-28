using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Actions;
using RedoxExtensions.Continuations;
using RedoxExtensions.MagToolsInterop;
using RedoxExtensions.Mine;
using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// Runs a chain of VTank metas, one after the other.
    ///
    /// The chain is read from MetaChainList-[character]-[chain].txt (or MetaChainList-[chain].txt) in the RE directory,
    /// one meta name per line.  A meta is assumed to be complete once the character has neither moved nor attacked for
    /// a while.  Progress is persisted to MetaChainList-[character]-Current.txt so the chain can resume after a crash.
    /// </summary>
    public class MetaChainRunner : IDisposable
    {
        private const string FilePrefix = "MetaChainList";
        private const string CurrentChainName = "Current";

        private const int TickIntervalMilliseconds = 2000;
        private const int MillisecondsDelayBeforeCreatingFellow = 2000;
        private const int MillisecondsDelayBeforeResuming = 15000;
        private static readonly TimeSpan IdleTimeBeforeMetaComplete = TimeSpan.FromSeconds(60);

        // Movement smaller than this (in raw coordinate units) is ignored
        private const double MovementThreshold = 0.5;

        private const string VTMetaPrefix = "[VTank] Meta";

        // Ex: You slash Drudge Ravener for 50 points of damage!
        // Ex: Critical hit!  You scorch Drudge Ravener for 120 points with Flame Bolt VII.
        private static readonly Regex YouDamagedRegex = new Regex(@"^(Critical hit!\s+)?You \w+ .+ for \d+ points?", RegexOptions.Compiled);

        private readonly Timer _tickTimer;

        private List<string> _remaining = new List<string>();
        private string _currentMeta;
        private bool _running;
        private DateTime _lastActivity;

        private int _lastLandcell;
        private double _lastX;
        private double _lastY;
        private double _lastZ;

        static MetaChainRunner()
        {
            ChainDirectory = WrapperUtilities.RedoxExtensionsBinDirectory;
        }

        public MetaChainRunner()
        {
            this._tickTimer = new Timer();
            this._tickTimer.Interval = TickIntervalMilliseconds;
            this._tickTimer.Tick += _tickTimer_Tick;

            REPlugin.Instance.Events.Decal.ChatBoxMessage += Decal_ChatBoxMessage;
            REPlugin.Instance.Events.Decal.SpellCast += Decal_SpellCast;

            this.TryResume();
        }

        public static string ChainDirectory { get; set; }

        public bool IsRunning
        {
            get { return this._running; }
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.ChatBoxMessage -= Decal_ChatBoxMessage;
            REPlugin.Instance.Events.Decal.SpellCast -= Decal_SpellCast;

            this._tickTimer.Stop();
            this._tickTimer.Dispose();
        }

        #region File Helpers

        internal static string FindChainFile(string characterName, string chainName)
        {
            var characterSpecific = Path.Combine(ChainDirectory, string.Format("{0}-{1}-{2}.txt", FilePrefix, characterName, chainName));
            if (File.Exists(characterSpecific))
            {
                return characterSpecific;
            }

            var generic = Path.Combine(ChainDirectory, string.Format("{0}-{1}.txt", FilePrefix, chainName));
            if (File.Exists(generic))
            {
                return generic;
            }

            return null;
        }

        internal static string GetCurrentFilePath(string characterName)
        {
            return Path.Combine(ChainDirectory, string.Format("{0}-{1}-{2}.txt", FilePrefix, characterName, CurrentChainName));
        }

        internal static List<string> ReadChain(string path)
        {
            var metas = new List<string>();
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                {
                    continue;
                }

                metas.Add(trimmed);
            }

            return metas;
        }

        internal static void WriteChain(string path, IEnumerable<string> metas)
        {
            File.WriteAllLines(path, new List<string>(metas).ToArray());
        }

        #endregion

        public void Start(string chainName)
        {
            if (this._running)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: A chain is already running.  Use /re stopchain first.");
                return;
            }

            var chainFile = FindChainFile(CharacterName, chainName);
            if (chainFile == null)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Could not find a chain file for `{0}` in {1}", chainName, ChainDirectory);
                return;
            }

            var metas = ReadChain(chainFile);
            if (metas.Count == 0)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Chain file {0} is empty", chainFile);
                return;
            }

            REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Starting chain `{0}` with {1} meta(s)", chainName, metas.Count);
            this.BeginRun(metas);
        }

        public void Stop()
        {
            this._tickTimer.Stop();
            this._running = false;
            this._remaining.Clear();
            this._currentMeta = null;
            DeleteCurrentFile();

            REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Stopped");
        }

        private static string CharacterName
        {
            get { return REPlugin.Instance.CharacterFilter.Name; }
        }

        private void TryResume()
        {
            var currentFile = GetCurrentFilePath(CharacterName);
            if (!File.Exists(currentFile))
            {
                return;
            }

            var metas = ReadChain(currentFile);
            if (metas.Count == 0)
            {
                DeleteCurrentFile();
                return;
            }

            REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Resuming chain with {0} meta(s) remaining", metas.Count);

            // Mark as running right away so a manual runchain doesn't collide with the pending resume
            this._running = true;

            // Give VT and other tools a chance to finish loading before we start issuing commands
            DelayedContinuation.ContinueAfterDelayOnGameThread((stateObj) =>
            {
                // A stopchain may have been issued while we were waiting
                if (this._running)
                {
                    this.BeginRun(metas);
                }
            },
            MillisecondsDelayBeforeResuming,
            null);
        }

        private void BeginRun(List<string> metas)
        {
            this._running = true;
            this._remaining = new List<string>(metas);

            MTActions.QuitFellow();

            // Give the quit a chance to process before creating the new fellow
            DelayedContinuation.ContinueAfterDelayOnGameThread((stateObj) =>
            {
                if (!this._running)
                {
                    return;
                }

                MTActions.CreateFellow("X");

                CharacterActions.SetMainProfile(MyMainProfiles.Normal);
                VTActions.SetBoostNavPriority(false);
                VTActions.DisableLooting();
                VTActions.DisableCombat();
                VTActions.EnableBuffing();
                VTActions.DisableNav();
                VTActions.EnableMeta();

                this.LoadNextMeta();
            },
            MillisecondsDelayBeforeCreatingFellow,
            null);
        }

        private void LoadNextMeta()
        {
            if (this._remaining.Count == 0)
            {
                this.Complete();
                return;
            }

            this._currentMeta = this._remaining[0];
            this._remaining.RemoveAt(0);

            // Persist the current meta along with the remaining ones.  If we crash mid-meta, the meta will be restarted
            var persisted = new List<string> { this._currentMeta };
            persisted.AddRange(this._remaining);
            WriteChain(GetCurrentFilePath(CharacterName), persisted);

            REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Loading meta `{0}` ({1} remaining after this)", this._currentMeta, this._remaining.Count);

            VTActions.LoadMetaProfile(this._currentMeta);
            VTActions.StartVT();
            TellActions.TellNativeFellow("!start");

            this.CaptureSelfPosition();
            this.MarkActivity();
            this._tickTimer.Start();
        }

        private void Complete()
        {
            this._tickTimer.Stop();
            this._running = false;
            this._currentMeta = null;
            DeleteCurrentFile();

            VTActions.StopVT();

            REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Chain complete");
        }

        private static void DeleteCurrentFile()
        {
            var currentFile = GetCurrentFilePath(CharacterName);
            if (File.Exists(currentFile))
            {
                File.Delete(currentFile);
            }
        }

        private void MarkActivity()
        {
            this._lastActivity = DateTime.Now;
        }

        /// <summary>
        /// Records the current position of the character.  Returns true if the character moved since the last capture
        /// </summary>
        private bool CaptureSelfPosition()
        {
            var self = REPlugin.Instance.WorldFilter[REPlugin.Instance.CharacterFilter.Id];
            if (self == null)
            {
                return false;
            }

            var landcell = self.Values(LongValueKey.Landblock);
            var coords = self.RawCoordinates();

            var dx = coords.X - this._lastX;
            var dy = coords.Y - this._lastY;
            var dz = coords.Z - this._lastZ;
            var moved = landcell != this._lastLandcell || Math.Sqrt(dx * dx + dy * dy + dz * dz) > MovementThreshold;

            this._lastLandcell = landcell;
            this._lastX = coords.X;
            this._lastY = coords.Y;
            this._lastZ = coords.Z;

            return moved;
        }

        private static bool IsSelfAttackMessage(string text)
        {
            return YouDamagedRegex.IsMatch(text)
                || text.Contains("evaded your attack")
                || text.Contains("resists your spell");
        }

        #region Event Handlers

        void _tickTimer_Tick(object sender, EventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                if (!this._running || this._currentMeta == null)
                {
                    this._tickTimer.Stop();
                    return;
                }

                if (this.CaptureSelfPosition())
                {
                    this.MarkActivity();
                    return;
                }

                if (DateTime.Now - this._lastActivity >= IdleTimeBeforeMetaComplete)
                {
                    this._tickTimer.Stop();
                    REPlugin.Instance.Chat.WriteLine("[RE] MetaChain: Meta `{0}` appears complete", this._currentMeta);
                    this.LoadNextMeta();
                }
            });
        }

        private void Decal_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            if (!this._running || string.IsNullOrEmpty(e.Text))
            {
                return;
            }

            // Meta state changes / actions are a good sign the meta is still doing work
            if (e.Text.StartsWith(VTMetaPrefix) || IsSelfAttackMessage(e.Text))
            {
                this.MarkActivity();
            }
        }

        private void Decal_SpellCast(object sender, SpellCastEventArgs e)
        {
            if (this._running)
            {
                this.MarkActivity();
            }
        }

        #endregion
    }
}
