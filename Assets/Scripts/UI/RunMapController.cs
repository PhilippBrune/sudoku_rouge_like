using System.Collections.Generic;
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
        private RunResumeService _resume;

        private RunAutoSaveCoordinator _autoSave;
        private RunDirector _run;
        private RunDirector _autoSaveBoundRun;
        private bool _rewardsGrantedForCurrentPuzzle;
        private readonly Dictionary<int, LevelConfig> _fixedNodeConfigs = new Dictionary<int, LevelConfig>();

        private void Awake()
        {
            _saveFile = new SaveFileService();
            _profile = new ProfileService();
            _resume = new RunResumeService();
        }

        public void Initialize(LaunchRequest request, int seed)
        {
            _run = new RunDirector();
            _run.StartRun(request, seed);
            _rewardsGrantedForCurrentPuzzle = false;
            PrepareFixedNodeConfigs();

            var node = _run.GetCurrentNode();
            var isBoss = node != null && node.Type == NodeType.Boss;
            var isElite = node != null && node.Type == NodeType.ElitePuzzle;
            var config = GetFixedLevelConfig(node) ?? _run.BuildLevelConfig(isBoss, isElite);
            _run.StartLevel(config);
            BindAutoSave();
        }

        public bool ResumeFromEnvelope(SaveFileEnvelope envelope)
        {
            if (envelope == null) return false;

            _profile.ApplyEnvelope(envelope);
            _run = new RunDirector();
            var resumed = _resume.TryResumeFromSave(_run, envelope);
            if (!resumed) return false;

            BindAutoSave();
            _rewardsGrantedForCurrentPuzzle = _run.CurrentLevelState != null && _run.IsLevelComplete;
            PrepareFixedNodeConfigs();
            return true;
        }

        public void BindRun(RunDirector run)
        {
            if (run == null) return;
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

        public bool TryAdvanceToNodeAndStartPuzzle(int nodeIndex, out RunNode node, out LevelConfig nextLevel)
        {
            node = null;
            nextLevel = null;

            if (_run == null || _run.State == null) return false;

            if (!_run.TryAdvanceToNode(nodeIndex)) return false;

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

        public void ChooseEventOption(int optionIndex)
        {
            _run?.ResolveCurrentEventChoice(optionIndex);
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
                SecondsPlayed = secondsPlayed,
                TutorialMode = _run.State.TutorialMode
            };

            for (var i = 0; i < _run.TileXpLog.Count; i++)
                result.TileXpEntries.Add(_run.TileXpLog[i]);

            var analytics = _run.GetAnalytics();
            if (analytics != null)
                result.Analytics = analytics.Build();

            return result;
        }

        public void AdvanceToNextFloor()
        {
            if (_run == null) return;
            _run.AdvanceToNextFloor();
            _rewardsGrantedForCurrentPuzzle = false;
            _fixedNodeConfigs.Clear();
            PrepareFixedNodeConfigs();
        }

        public RunDirector Run => _run;

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
            var intensity = Economy.BossService.IntensityForRunNumber(_run.State.RunNumber);
            sb.Append($" | Intensity: {intensity}");
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
                    config.Stars = Mathf.Max(config.Stars, 4);
                    config.BoardSize = Mathf.Max(config.BoardSize, 8);
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
                    config.Stars = Mathf.Max(config.Stars, 4);
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
