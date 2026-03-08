using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class RunMapController : MonoBehaviour
    {
        [SerializeField] private int seed = 9901;

        private readonly SaveFileService _saveFile = new();
        private readonly ProfileService _profile = new();
        private readonly RunResumeService _resume = new();

        private RunAutoSaveCoordinator _autoSave;
        private RunDirector _run;
        private RunDirector _autoSaveBoundRun;
        private bool? _lockedRiskPath;
        private bool _rewardsGrantedForCurrentPuzzle;
        private readonly Dictionary<int, LevelConfig> _fixedNodeConfigs = new();

        public void Initialize(ClassId classId, MetaProgressionState meta)
        {
            _run = new RunDirector(seed);
            _run.StartRun(classId, GameMode.GardenRun, runNumber: 1, meta: meta);
            _lockedRiskPath = null;
            _rewardsGrantedForCurrentPuzzle = false;
            PrepareFixedNodeConfigs();
            var firstNode = _run.CurrentRunGraph != null && _run.CurrentRunGraph.Count > 0 ? _run.CurrentRunGraph[0] : null;
            var levelConfig = firstNode != null ? GetFixedLevelConfig(firstNode) : _run.BuildLevelConfig(runNumber: 1, depth: 1);
            _run.StartLevel(levelConfig);
            BindAutoSave();
        }

        public bool ResumeFromEnvelope(SaveFileEnvelope envelope)
        {
            if (envelope == null)
            {
                return false;
            }

            _profile.ApplyEnvelope(envelope);
            _run = new RunDirector(seed);
            var resumed = _resume.TryResumeFromSave(_run, envelope);
            if (!resumed)
            {
                return false;
            }

            BindAutoSave();
            _lockedRiskPath = null;
            _rewardsGrantedForCurrentPuzzle = _run.CurrentLevelState != null && _run.CurrentLevelState.PuzzleComplete;
            PrepareFixedNodeConfigs();
            return true;
        }

        public void BindRun(RunDirector run)
        {
            if (run == null)
            {
                return;
            }

            _run = run;
            _rewardsGrantedForCurrentPuzzle = _run.CurrentLevelState != null && _run.CurrentLevelState.PuzzleComplete;
            PrepareFixedNodeConfigs();
            BindAutoSave();
        }

        public bool TryClaimCurrentPuzzleRewards(out int goldEarned, out List<ItemRollSlot> slots, out string failureReason)
        {
            goldEarned = 0;
            slots = new List<ItemRollSlot>();
            failureReason = string.Empty;

            if (_run == null || _run.RunState == null || _run.CurrentLevelState == null)
            {
                failureReason = "No active puzzle state.";
                return false;
            }

            if (!_run.CurrentLevelState.PuzzleComplete)
            {
                failureReason = "Puzzle is not complete yet.";
                return false;
            }

            if (_rewardsGrantedForCurrentPuzzle)
            {
                failureReason = "Rewards already granted for this puzzle.";
                return false;
            }

            var beforeGold = _run.RunState.CurrentGold;
            _run.CompleteLevelAndGrantRewards();
            var afterGold = _run.RunState.CurrentGold;
            goldEarned = Mathf.Max(0, afterGold - beforeGold);

            slots = _run.BuildItemRollPhase() ?? new List<ItemRollSlot>();
            _rewardsGrantedForCurrentPuzzle = true;
            return true;
        }

        public List<RunNode> GetVisibleNodes()
        {
            var output = new List<RunNode>();
            var graph = _run.CurrentRunGraph;
            for (var i = 0; i < graph.Count; i++)
            {
                if (graph[i].IsRevealed)
                {
                    output.Add(graph[i]);
                }
            }

            return output;
        }

        public RunNode SelectPath(bool risk)
        {
            if (_run == null)
            {
                Debug.LogWarning("RunMapController.SelectPath called before run initialization.");
                return null;
            }

            return _run.AdvanceToNextNode(risk);
        }

        public bool TryAdvancePathAndStartNextPuzzle(bool risk, out RunNode node, out LevelConfig nextLevel, out string failureReason)
        {
            node = null;
            nextLevel = null;
            failureReason = string.Empty;

            if (_run == null || _run.RunState == null)
            {
                failureReason = "Run is not initialized.";
                return false;
            }

            var isFirstPathChoice = _run.RunState.CurrentNodeIndex == 0 && !_lockedRiskPath.HasValue;

            if (!isFirstPathChoice && (_run.CurrentLevelState == null || !_run.CurrentLevelState.PuzzleComplete))
            {
                failureReason = "Complete the current Sudoku puzzle first.";
                return false;
            }

            if (!isFirstPathChoice && !_rewardsGrantedForCurrentPuzzle)
            {
                _run.CompleteLevelAndGrantRewards();
                _rewardsGrantedForCurrentPuzzle = true;
            }

            if (_lockedRiskPath.HasValue)
            {
                risk = _lockedRiskPath.Value;
            }
            else
            {
                _lockedRiskPath = risk;
            }

            node = _run.AdvanceToNextNode(risk);
            if (node == null)
            {
                failureReason = "No next path node is available.";
                return false;
            }

            if (!RequiresPuzzleNode(node.Type))
            {
                nextLevel = null;
                return true;
            }

            nextLevel = GetFixedLevelConfig(node);
            if (nextLevel == null)
            {
                failureReason = "Failed to resolve level configuration for selected path.";
                return false;
            }

            _run.StartLevel(nextLevel);
            _rewardsGrantedForCurrentPuzzle = false;
            return true;
        }

        private static bool RequiresPuzzleNode(NodeType type)
        {
            return type == NodeType.Puzzle || type == NodeType.ElitePuzzle || type == NodeType.PreBoss || type == NodeType.Boss;
        }

        public PathChoicePreview BuildPathChoicePreview(bool risk)
        {
            if (_run == null || _run.RunState == null || _run.CurrentRunGraph == null || _run.CurrentRunGraph.Count == 0)
            {
                return PathChoicePreview.Unavailable(risk);
            }

            if (!TryGetNextChoiceIndex(risk, out var index))
            {
                return PathChoicePreview.Unavailable(risk);
            }

            var node = _run.CurrentRunGraph[index];
            var previewConfig = GetFixedLevelConfig(node);
            var isBoss = node.Type == NodeType.Boss;

            return new PathChoicePreview
            {
                Available = true,
                RiskPath = risk,
                LockedPath = _lockedRiskPath,
                NodeType = node.Type,
                Depth = node.Depth,
                BoardSize = previewConfig.BoardSize,
                Stars = previewConfig.Stars,
                IsBoss = isBoss
            };
        }

        public bool TryGetFixedLevelForNode(RunNode node, out LevelConfig config)
        {
            config = GetFixedLevelConfig(node);
            return config != null;
        }

        private bool TryGetNextChoiceIndex(bool risk, out int index)
        {
            index = -1;
            if (_run == null || _run.RunState == null || _run.CurrentRunGraph == null || _run.CurrentRunGraph.Count == 0)
            {
                return false;
            }

            var currentIndex = Mathf.Clamp(_run.RunState.CurrentNodeIndex, 0, _run.CurrentRunGraph.Count - 1);
            var currentDepth = _run.CurrentRunGraph[currentIndex].Depth;

            for (var i = 0; i < _run.CurrentRunGraph.Count; i++)
            {
                var candidate = _run.CurrentRunGraph[i];
                if (candidate.Depth <= currentDepth)
                {
                    continue;
                }

                if (candidate.Type == NodeType.Boss || candidate.IsRiskPath == risk)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public RunEvent OpenEventNode()
        {
            return _run?.BuildCurrentEvent();
        }

        public bool ChooseEventOption(string optionId)
        {
            return _run != null && _run.ResolveCurrentEventChoice(optionId);
        }

        public List<CurseType> GetActiveCurses()
        {
            var output = new List<CurseType>();
            if (_run?.RunState == null)
            {
                return output;
            }

            for (var i = 0; i < _run.RunState.ActiveCurses.Count; i++)
            {
                output.Add(_run.RunState.ActiveCurses[i]);
            }

            return output;
        }

        public List<float> GetHeatCurve()
        {
            var output = new List<float>();
            if (_run?.RunState == null)
            {
                return output;
            }

            for (var i = 0; i < _run.RunState.HeatHistory.Count; i++)
            {
                output.Add(_run.RunState.HeatHistory[i]);
            }

            return output;
        }

        public RunResult BuildRunResult(bool victory, int bossPhaseReached, int secondsPlayed)
        {
            if (_run == null)
            {
                return null;
            }

            return _run.BuildRunResult(victory, bossPhaseReached, secondsPlayed);
        }

        public void AdvanceToNextGarden()
        {
            if (_run == null) return;
            _run.AdvanceToNextGarden();
            _lockedRiskPath = null;
            _rewardsGrantedForCurrentPuzzle = false;
            _fixedNodeConfigs.Clear();
            PrepareFixedNodeConfigs();
        }

        public RunDirector Run => _run;

        public sealed class PathChoicePreview
        {
            public bool Available;
            public bool RiskPath;
            public bool? LockedPath;
            public NodeType NodeType;
            public int Depth;
            public int BoardSize;
            public int Stars;
            public bool IsBoss;

            public static PathChoicePreview Unavailable(bool risk)
            {
                return new PathChoicePreview
                {
                    Available = false,
                    RiskPath = risk,
                    LockedPath = null,
                    NodeType = NodeType.Start,
                    Depth = 0,
                    BoardSize = 0,
                    Stars = 0,
                    IsBoss = false
                };
            }
        }

        private void BindAutoSave()
        {
            if (_run == null)
            {
                return;
            }

            if (_autoSaveBoundRun == _run)
            {
                return;
            }

            _autoSave ??= new RunAutoSaveCoordinator(_saveFile, _profile);
            _autoSave.Bind(_run);
            _autoSaveBoundRun = _run;
        }

        private void PrepareFixedNodeConfigs()
        {
            _fixedNodeConfigs.Clear();
            if (_run == null || _run.CurrentRunGraph == null || _run.CurrentRunGraph.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _run.CurrentRunGraph.Count; i++)
            {
                var node = _run.CurrentRunGraph[i];
                if (node == null)
                {
                    continue;
                }

                var config = _run.BuildLevelConfig(runNumber: 1, depth: node.Depth);
                if (node.Type == NodeType.Boss)
                {
                    config.IsBoss = true;
                    config.Stars = Mathf.Max(config.Stars, 4);
                    config.BoardSize = Mathf.Max(config.BoardSize, 8);
                }
                else if (node.IsRiskPath)
                {
                    config.Difficulty = (DifficultyTier)Mathf.Clamp((int)config.Difficulty + 1, (int)DifficultyTier.Diff1, (int)DifficultyTier.Diff5);
                    config.Stars = Mathf.Clamp(config.Stars + 1, 1, 5);
                    config.BoardSize = Mathf.Clamp(config.BoardSize + 1, 4, 9);
                    config.MissingPercent = Mathf.Clamp(config.MissingPercent + 0.06f, 0.08f, 0.85f);
                }

                _fixedNodeConfigs[i] = CloneConfig(config);
            }
        }

        public LevelConfig GetFixedLevelConfig(RunNode node)
        {
            if (_run == null || node == null || _run.CurrentRunGraph == null)
            {
                return null;
            }

            var index = -1;
            for (var i = 0; i < _run.CurrentRunGraph.Count; i++)
            {
                if (ReferenceEquals(_run.CurrentRunGraph[i], node))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return null;
            }

            if (!_fixedNodeConfigs.TryGetValue(index, out var config) || config == null)
            {
                config = _run.BuildLevelConfig(runNumber: 1, depth: node.Depth);
                if (node.Type == NodeType.Boss)
                {
                    config.IsBoss = true;
                    config.Stars = Mathf.Max(config.Stars, 4);
                    config.BoardSize = Mathf.Max(config.BoardSize, 8);
                }
                else if (node.IsRiskPath)
                {
                    config.Difficulty = (DifficultyTier)Mathf.Clamp((int)config.Difficulty + 1, (int)DifficultyTier.Diff1, (int)DifficultyTier.Diff5);
                    config.Stars = Mathf.Clamp(config.Stars + 1, 1, 5);
                    config.BoardSize = Mathf.Clamp(config.BoardSize + 1, 4, 9);
                    config.MissingPercent = Mathf.Clamp(config.MissingPercent + 0.06f, 0.08f, 0.85f);
                }

                _fixedNodeConfigs[index] = CloneConfig(config);
            }

            return CloneConfig(config);
        }

        private static LevelConfig CloneConfig(LevelConfig src)
        {
            if (src == null)
            {
                return null;
            }

            var copy = new LevelConfig
            {
                Difficulty = src.Difficulty,
                Stars = src.Stars,
                BoardSize = src.BoardSize,
                MissingPercent = src.MissingPercent,
                IsBoss = src.IsBoss,
                StressVariant = src.StressVariant,
                ExpectedHeat = src.ExpectedHeat,
                VarianceBand = src.VarianceBand,
                RegionVariant = src.RegionVariant
            };

            for (var i = 0; i < src.ActiveModifiers.Count; i++)
            {
                copy.ActiveModifiers.Add(src.ActiveModifiers[i]);
            }

            return copy;
        }
    }
}
