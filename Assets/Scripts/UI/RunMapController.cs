using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class RunMapController : MonoBehaviour
    {
        private SaveFileService _saveFile;
        private ProfileService _profile;

        private RunAutoSaveCoordinator _autoSave;
        private RunDirector _run;
        private bool _rewardsGrantedForCurrentPuzzle;
        private readonly Dictionary<int, LevelConfig> _fixedNodeConfigs = new Dictionary<int, LevelConfig>();
        private RunNavigationSnapshot _pendingGenerationRollback;
        private bool _hasPendingGenerationRollback;
        private readonly SemaphoreSlim _preBakeGate = new SemaphoreSlim(2, 2);
        // Cancellation token for background node pre-bake tasks.
        // Renewed at every floor change and run bind to abort stale tasks from the previous floor.
        private CancellationTokenSource _preBakeCancel = new CancellationTokenSource();

        private void Awake()
        {
            var slot = SaveProfileService.ActiveSlot;
            _saveFile = new SaveFileService(slot);
            _profile = new ProfileService(_saveFile);
        }

        public void BindRun(RunDirector run, SaveFileService saveFile = null)
        {
            if (run == null) return;
            if (saveFile != null)
            {
                _saveFile = saveFile;
                _profile = new ProfileService(_saveFile);
                _autoSave = null; // force rebuild in BindAutoSave with new file
            }
            // Cancel any in-flight pre-bake tasks from a previous run before spawning new ones.
            if (_run != null)
            {
                _preBakeCancel.Cancel();
                _preBakeCancel = new CancellationTokenSource();
                _run.ClearPreBakedNodePuzzles();
            }
            _run = run;
            _rewardsGrantedForCurrentPuzzle = _run.CurrentLevelState != null && _run.IsLevelComplete;
            PrepareFixedNodeConfigs();
            BindAutoSave();
        }

        public bool TryClaimCurrentPuzzleRewards(out int goldEarned, out List<ItemInstance> slots,
            bool buildItemSlots = true)
        {
            goldEarned = 0;
            slots = new List<ItemInstance>();

            if (_run == null || _run.State == null || _run.CurrentLevelState == null)
                return false;
            if (!_run.IsLevelComplete)
                return false;
            if (_rewardsGrantedForCurrentPuzzle)
                return false;

            var beforeGold = _run.State.CurrentGold;
            _run.CompleteLevelAndGrantRewards();
            goldEarned = Mathf.Max(0, _run.State.CurrentGold - beforeGold);

            if (buildItemSlots)
                slots = _run.BuildItemRewardSlots() ?? new List<ItemInstance>();
            _rewardsGrantedForCurrentPuzzle = true;
            return true;
        }

        public List<RunNode> GetFloorGraph()
        {
            return _run?.CurrentFloorGraph ?? new List<RunNode>();
        }

        /// <summary>
        /// Boss-specific async path. Advances to the boss node and starts overlay generation in the
        /// background via <see cref="Run.RunDirector.StartLevelAsync"/>. The caller must poll
        /// <c>Run.TryCompleteAsyncLevel()</c> each frame to detect when the level is ready.
        /// </summary>
        public bool TryAdvanceToBossNodeAsync(int nodeIndex, out RunNode node, out LevelConfig nextLevel)
        {
            node = null;
            nextLevel = null;

            if (_run == null || _run.State == null) return false;
            var rollback = _run.CaptureNavigationSnapshot();
            if (!_run.TryAdvanceToNode(nodeIndex, false)) return false;

            node = _run.GetCurrentNode();
            if (node == null || node.Type != NodeType.Boss)
            {
                _run.RestoreNavigationSnapshot(rollback);
                return false;
            }

            // The fixed config (built at floor entry, shown on the map) plus the modifiers chosen at the gate.
            // Bosses follow the floor's size range; only modifiers that need a larger board raise it (SPEC-CONF-028).
            nextLevel = GetFixedLevelConfig(node) ?? _run.BuildNodeLevelConfig(node, nodeIndex);

            CancelPreBakeForInteractiveGeneration();
            _run.StartLevelAsync(nextLevel);
            StorePendingGenerationRollback(rollback);
            _rewardsGrantedForCurrentPuzzle = false;
            return true;
        }

        public bool TryAdvanceToNodeAndStartPuzzle(int nodeIndex, out RunNode node, out LevelConfig nextLevel, bool forced = false)
        {
            node = null;
            nextLevel = null;

            if (_run == null || _run.State == null) return false;

            var rollback = _run.CaptureNavigationSnapshot();
            if (!_run.TryAdvanceToNode(nodeIndex, forced)) return false;

            node = _run.GetCurrentNode();
            if (node == null)
            {
                _run.RestoreNavigationSnapshot(rollback);
                return false;
            }

            if (!RequiresPuzzleNode(node.Type))
            {
                nextLevel = null;
                if (node.Type == NodeType.Cursed)
                    StorePendingGenerationRollback(rollback);
                else
                    ClearPendingGenerationRollback();
                return true;
            }

            nextLevel = GetFixedLevelConfig(node) ?? _run.BuildNodeLevelConfig(node, nodeIndex);

            // Use a pre-baked puzzle if the background task finished in time; otherwise generate async.
            if (_run.TryConsumePreBakedNode(nodeIndex, out var preBakedBoard, out var preBakedOverlay))
            {
                Debug.Log($"[RunMapController] Pre-baked hit for node {nodeIndex} — skipping synchronous generation.");
                _run.StartLevelWithPreBakedPuzzle(nextLevel, preBakedBoard, preBakedOverlay);
                ClearPendingGenerationRollback();
            }
            else
            {
                CancelPreBakeForInteractiveGeneration();
                _run.StartLevelAsync(nextLevel);
                StorePendingGenerationRollback(rollback);
            }
            _rewardsGrantedForCurrentPuzzle = false;
            return true;
        }

        public RunNavigationSnapshot CaptureNavigationSnapshot()
        {
            return _run != null ? _run.CaptureNavigationSnapshot() : default;
        }

        public void StartPuzzleAsyncWithRollback(LevelConfig config, RunNavigationSnapshot rollback)
        {
            if (_run == null || config == null) return;
            CancelPreBakeForInteractiveGeneration();
            _run.StartLevelAsync(config);
            StorePendingGenerationRollback(rollback);
            _rewardsGrantedForCurrentPuzzle = false;
        }

        public void StartPuzzleAsync(LevelConfig config)
        {
            if (_run == null || config == null) return;
            CancelPreBakeForInteractiveGeneration();
            _run.StartLevelAsync(config);
            _rewardsGrantedForCurrentPuzzle = false;
        }

        public void ClearPendingGenerationRollback()
        {
            _hasPendingGenerationRollback = false;
            _pendingGenerationRollback = default;
        }

        public void RollbackPendingGeneration()
        {
            if (_run == null || !_hasPendingGenerationRollback) return;
            _run.RestoreNavigationSnapshot(_pendingGenerationRollback);
            ClearPendingGenerationRollback();
        }

        private void StorePendingGenerationRollback(RunNavigationSnapshot rollback)
        {
            _pendingGenerationRollback = rollback;
            _hasPendingGenerationRollback = true;
        }

        private void CancelPreBakeForInteractiveGeneration()
        {
            _preBakeCancel.Cancel();
            _preBakeCancel = new CancellationTokenSource();
        }

        private static bool RequiresPuzzleNode(NodeType type)
        {
            return type == NodeType.Puzzle || type == NodeType.ElitePuzzle
                || type == NodeType.PreBoss || type == NodeType.Boss;
        }

        public RunEvent OpenEventNode()
        {
            return _run?.BuildCurrentEvent();
        }

        public string ChooseEventOption(int optionIndex)
        {
            return _run?.ResolveCurrentEventChoice(optionIndex) ?? string.Empty;
        }

        public RunResult BuildRunResult(bool victory, int bossPhaseReached, int secondsPlayed) =>
            RunResultFactory.Build(_run, victory, bossPhaseReached, secondsPlayed);

        public void AdvanceToNextFloor()
        {
            if (_run == null) return;
            _run.AdvanceToNextFloor();
            _rewardsGrantedForCurrentPuzzle = false;
            _fixedNodeConfigs.Clear();
            // Cancel background tasks from the previous floor and start fresh.
            _preBakeCancel.Cancel();
            _preBakeCancel = new CancellationTokenSource();
            _run.ClearPreBakedNodePuzzles();
            PrepareFixedNodeConfigs();
            SaveNow();
        }

        public RunDirector Run => _run;
        public ProfileService Profile => _profile;

        /// <summary>Returns a short preview string for the boss node shown on the map,
        /// e.g. "3 mods — High intensity — Floor modifier: GermanWhispers".</summary>
        public string GetBossNodePreviewHint()
        {
            if (_run?.State == null) return "";
            var floor = _run.State.CurrentFloor;
            var floorMods = _run.State.ActiveFloorModifiers;
            var sb = new System.Text.StringBuilder();
            sb.Append($"Floor {floor + 1} Boss");
            if (floorMods != null && floorMods.Count > 0)
            {
                sb.Append(" | Floor mod");
                if (floorMods.Count > 1) sb.Append('s');
                sb.Append(": ");
                for (var i = 0; i < floorMods.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatModName(floorMods[i]));
                }
            }
            var intensity = BossService.IntensityForRunNumber(_run.State.RunNumber);
            sb.Append($" | Intensity: {intensity}");
            // DimLantern: reveal the modifier pool that will appear at the boss gate
            if (_run.State.DimLanternUsed)
            {
                var choices = _run.RollBossModifierChoices();
                if (choices != null && choices.Count > 0)
                {
                    sb.Append(" | Gate pool: ");
                    for (var i = 0; i < choices.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(FormatModName(choices[i]));
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>Short label for the active positive floor effect shown on the path overview.</summary>
        // [REQ: MAP-FX-004] GetPositiveFloorEffectLabel: returns display text for the active floor bonus
        public string GetPositiveFloorEffectLabel()
        {
            if (_run?.State == null || !_run.State.HasPositiveFloorEffect) return "";
            return _run.State.ActivePositiveFloorEffect switch
            {
                PositiveFloorEffect.Bounty       => "★ Bounty Floor (+40% gold)",
                PositiveFloorEffect.LuckyItems   => "★ Lucky Items (+1 reward slot)",
                PositiveFloorEffect.PencilRefill => "★ Pencil Refill (+2 per puzzle)",
                PositiveFloorEffect.HealingPath  => "★ Healing Path (+1 HP if perfect)",
                _ => ""
            };
        }

        private static string FormatModName(BossModifierId m) => m switch
        {
            BossModifierId.GermanWhispers   => "G.Whispers",
            BossModifierId.DutchWhispers    => "D.Whispers",
            BossModifierId.ParityLines      => "Parity",
            BossModifierId.RenbanLines      => "Renban",
            BossModifierId.KillerCages      => "Killer",
            BossModifierId.DifferenceKropki => "Diff Dot",
            BossModifierId.RatioKropki      => "Ratio Dot",
            BossModifierId.ArrowSums        => "Arrow",
            BossModifierId.FogOfWar         => "Fog",
            BossModifierId.Palindrome       => "Palindrome",
            BossModifierId.Thermo           => "Thermo",
            BossModifierId.BetweenLines     => "Between",
            BossModifierId.EvenOdd          => "Even/Odd",
            BossModifierId.Nonconsecutive   => "Noncons.",
            BossModifierId.Antiknight       => "Anti-Knight",
            _ => m.ToString()
        };

        public void SaveNow() => _autoSave?.SaveBound();

        // [REQ: SAVE-AUTO-001] Bind on every BindRun, even for the same RunDirector: GameBootstrap reuses one
        // director for every launch and BindRun(run, saveFile) has just dropped the old coordinator, so skipping
        // here left every later run in the session without autosave (GAP-001). The coordinator reloads the save
        // before each write, so profile data saved in between is never overwritten (GAP-037).
        private void BindAutoSave()
        {
            if (_run == null) return;
            _autoSave = _autoSave ?? new RunAutoSaveCoordinator(_saveFile, _profile);
            _autoSave.Bind(_run);
        }

        private void PrepareFixedNodeConfigs()
        {
            _fixedNodeConfigs.Clear();
            if (_run == null || _run.CurrentFloorGraph == null) return;

            // Configs built at floor entry are saved with the run, so a resume shows and plays the same puzzles (GAP-018).
            var saved = _run.GetSavedNodeConfigs();
            var built = new List<LevelConfig>(_run.CurrentFloorGraph.Count);
            for (var i = 0; i < _run.CurrentFloorGraph.Count; i++)
            {
                var node = _run.CurrentFloorGraph[i];
                if (node == null) { built.Add(new LevelConfig()); continue; }

                var isBoss = node.Type == NodeType.Boss;
                var config = saved != null ? saved[i].Clone() : _run.BuildNodeLevelConfig(node, i);
                built.Add(config.Clone());

                if (isBoss)
                {
                    // Pre-bake the boss board on a background thread so floor entry doesn't stutter.
                    // StartLevelAsync reads volatile _preBakedBossBoard; falls back to sync if not ready.
                    var bakeConfig = config.Clone();
                    var token = _preBakeCancel.Token;
                    QueuePreBake(() => _run.BakeBossBoard(bakeConfig, token), token, $"boss node {i}");
                }

                _fixedNodeConfigs[i] = config.Clone();

                // Pre-bake the full puzzle (board + overlay) for all non-boss puzzle nodes so
                // TryAdvanceToNodeAndStartPuzzle can skip synchronous generation entirely.
                if (!isBoss && RequiresPuzzleNode(node.Type))
                {
                    var capturedConfig = config.Clone();
                    var capturedIndex  = i;
                    var token          = _preBakeCancel.Token;
                    QueuePreBake(
                        () => _run.BakeNodePuzzle(capturedConfig, capturedIndex, token),
                        token,
                        $"node {capturedIndex}");
                }
            }

            if (saved == null)
                _run.SaveNodeConfigs(built);
        }

        private void QueuePreBake(Action action, CancellationToken token, string label)
        {
            Task.Run(() =>
            {
                var entered = false;
                try
                {
                    _preBakeGate.Wait(token);
                    entered = true;
                    token.ThrowIfCancellationRequested();
                    action();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RunMapController] Pre-bake failed for {label}: {ex.Message}");
                }
                finally
                {
                    if (entered)
                        _preBakeGate.Release();
                }
            }, token);
        }

        public LevelConfig GetFixedLevelConfig(RunNode node)
        {
            if (_run == null || node == null || _run.CurrentFloorGraph == null)
                return null;

            var index = -1;
            for (var i = 0; i < _run.CurrentFloorGraph.Count; i++)
            {
                if (ReferenceEquals(_run.CurrentFloorGraph[i], node))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return null;

            if (!_fixedNodeConfigs.TryGetValue(index, out var config) || config == null)
            {
                config = _run.BuildNodeLevelConfig(node, index);
                _fixedNodeConfigs[index] = config.Clone();
            }

            var result = config.Clone();

            // Apply chosen boss modifiers (plural) at call time (after boss gate panel)
            if (node.Type == NodeType.Boss && _run.State != null && _run.State.ChosenBossModifiers != null)
            {
                for (var i = 0; i < _run.State.ChosenBossModifiers.Count; i++)
                {
                    if (!result.ActiveModifiers.Contains(_run.State.ChosenBossModifiers[i]))
                        result.ActiveModifiers.Add(_run.State.ChosenBossModifiers[i]);
                }
                _run.NormalizeLevelConfig(result);
            }

            return result;
        }
    }
}
