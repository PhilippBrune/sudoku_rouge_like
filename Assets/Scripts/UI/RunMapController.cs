using System.Collections.Generic;
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
        private RunDirector _autoSaveBoundRun;
        private bool _rewardsGrantedForCurrentPuzzle;
        private readonly Dictionary<int, LevelConfig> _fixedNodeConfigs = new Dictionary<int, LevelConfig>();

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
            _run = run;
            _rewardsGrantedForCurrentPuzzle = _run.CurrentLevelState != null && _run.IsLevelComplete;
            PrepareFixedNodeConfigs();
            BindAutoSave();
        }

        public bool TryClaimCurrentPuzzleRewards(out int goldEarned, out List<ItemInstance> slots)
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
            if (!_run.TryAdvanceToNode(nodeIndex, false)) return false;

            node = _run.GetCurrentNode();
            if (node == null || node.Type != NodeType.Boss) return false;

            // Always rebuild boss config after ChooseBossModifiers so ActiveModifiers includes
            // the player's chosen modifier. The fixed-config cache was built at floor entry
            // before the modifier was selected, so it never contains the chosen modifier.
            nextLevel = _run.BuildLevelConfig(true, false, nodeIndex);
            nextLevel.BoardSize = Mathf.Max(nextLevel.BoardSize, 8);

            _run.StartLevelAsync(nextLevel);
            _rewardsGrantedForCurrentPuzzle = false;
            return true;
        }

        public bool TryAdvanceToNodeAndStartPuzzle(int nodeIndex, out RunNode node, out LevelConfig nextLevel, bool forced = false)
        {
            node = null;
            nextLevel = null;

            if (_run == null || _run.State == null) return false;

            if (!_run.TryAdvanceToNode(nodeIndex, forced)) return false;

            node = _run.GetCurrentNode();
            if (node == null) return false;

            if (!RequiresPuzzleNode(node.Type))
            {
                nextLevel = null;
                return true;
            }

            nextLevel = GetFixedLevelConfig(node);
            if (nextLevel == null)
            {
                var isBoss = node.Type == NodeType.Boss;
                var isElite = node.Type == NodeType.ElitePuzzle;
                nextLevel = _run.BuildLevelConfig(isBoss, isElite);
            }

            _run.StartLevel(nextLevel);
            _rewardsGrantedForCurrentPuzzle = false;
            return true;
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

        public RunResult BuildRunResult(bool victory, int bossPhaseReached, int secondsPlayed)
        {
            if (_run == null || _run.State == null) return null;

            var result = new RunResult
            {
                PlayedClassId = _run.State.ClassId,
                Mode = _run.State.Mode,
                Victory = victory,
                GardenDepthReached = _run.State.Depth,
                GoldEarned = _run.State.CurrentGold,
                XpEarned = _run.GetTotalRunXp(),
                BossPhaseReached = bossPhaseReached,
                BossesDefeatedThisRun = _run.State.BossesDefeatedThisRun,
                MistakesMade = _run.GetAnalytics()?.TotalMistakes ?? _run.CurrentLevelState?.Mistakes ?? 0,
                SecondsPlayed = secondsPlayed > 0 ? secondsPlayed : Mathf.RoundToInt(_run.State.TotalRunSeconds),
                TutorialMode = _run.State.TutorialMode,
                DisableProgressionRewards = _run.State.DisableProgressionRewards,
                PlayedTier = _run.State.SpiritTrialsTier,
                HarmonyLevel = _run.State.HarmonyLevel
            };

            for (var i = 0; i < _run.TileXpLog.Count; i++)
                result.TileXpEntries.Add(_run.TileXpLog[i]);

            var analytics = _run.GetAnalytics();
            if (analytics != null)
            {
                result.Analytics = analytics.Build();
                result.ItemsUsedThisRun = analytics.TotalItemsUsed;
                result.RunScore = result.Analytics.RunScore;
            }

            result.AcquiredRelic = _run.State.HasRelic;
            result.RelicsCollectedThisRun = _run.State.HeldRelics?.Count ?? 0;

            return result;
        }

        public void AdvanceToNextFloor()
        {
            if (_run == null) return;
            _run.AdvanceToNextFloor();
            _rewardsGrantedForCurrentPuzzle = false;
            _fixedNodeConfigs.Clear();
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

        private void BindAutoSave()
        {
            if (_run == null) return;
            if (_autoSaveBoundRun == _run) return;
            _autoSave = _autoSave ?? new RunAutoSaveCoordinator(_saveFile, _profile);
            _autoSave.Bind(_run);
            _autoSaveBoundRun = _run;
        }

        private void PrepareFixedNodeConfigs()
        {
            _fixedNodeConfigs.Clear();
            if (_run == null || _run.CurrentFloorGraph == null) return;

            for (var i = 0; i < _run.CurrentFloorGraph.Count; i++)
            {
                var node = _run.CurrentFloorGraph[i];
                if (node == null) continue;

                var isBoss = node.Type == NodeType.Boss;
                var isElite = node.Type == NodeType.ElitePuzzle;
                var config = _run.BuildLevelConfig(isBoss, isElite, i);

                if (isBoss)
                {
                    // Stars are fixed per floor by RollStars — no override needed here
                    config.BoardSize = Mathf.Max(config.BoardSize, 8);
                    // Pre-bake the boss board on a background thread so floor entry doesn't stutter.
                    // StartLevelAsync reads volatile _preBakedBossBoard; falls back to sync if not ready.
                    var bakeConfig = config.Clone();
                    Task.Run(() => _run.BakeBossBoard(bakeConfig));
                }
                else if (node.Route == RouteType.RiskRoute)
                {
                    config.Difficulty = (DifficultyTier)Mathf.Clamp((int)config.Difficulty + 1, (int)DifficultyTier.Diff1, (int)DifficultyTier.Diff5);
                    config.Stars = Mathf.Clamp(config.Stars + 1, 1, 5);
                    config.BoardSize = Mathf.Clamp(config.BoardSize + 1, 4, 9);
                    config.MissingPercent = Mathf.Clamp(config.MissingPercent + 0.06f, 0.08f, 0.85f);
                }

                _fixedNodeConfigs[i] = config.Clone();
            }
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
                var isBoss = node.Type == NodeType.Boss;
                var isElite = node.Type == NodeType.ElitePuzzle;
                config = _run.BuildLevelConfig(isBoss, isElite, index);

                if (isBoss)
                {
                    // Stars are fixed per floor by RollStars — no override needed here
                    config.BoardSize = Mathf.Max(config.BoardSize, 8);
                }
                else if (node.Route == RouteType.RiskRoute)
                {
                    config.Difficulty = (DifficultyTier)Mathf.Clamp((int)config.Difficulty + 1, (int)DifficultyTier.Diff1, (int)DifficultyTier.Diff5);
                    config.Stars = Mathf.Clamp(config.Stars + 1, 1, 5);
                    config.BoardSize = Mathf.Clamp(config.BoardSize + 1, 4, 9);
                    config.MissingPercent = Mathf.Clamp(config.MissingPercent + 0.06f, 0.08f, 0.85f);
                }

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
            }

            return result;
        }
    }
}
