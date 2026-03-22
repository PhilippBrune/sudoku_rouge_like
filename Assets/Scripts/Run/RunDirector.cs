using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Route;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.Tutorial;

namespace SudokuRoguelike.Run
{
    public sealed class RunDirector
    {
        private readonly Random _random;
        private readonly ItemService _itemService;
        private readonly RouteService _routeService;
        private readonly BossService _bossService;
        private readonly RunGraphService _runGraphService;
        private readonly ShopService _shopService;
        private readonly RelicService _relicService;
        private readonly RunArchetypeService _archetypeService = new();
        private readonly CurseService _curseService = new();
        private readonly RunEventService _eventService;
        private readonly RunVarianceService _varianceService = new();
        private readonly MidRunAdaptationService _adaptationService = new();
        private readonly PostRunAnalyticsService _analyticsService = new();
        private readonly EndlessZenService _endlessZenService = new();
        private readonly SpiritTrialsService _spiritTrialsService = new();
        private readonly RunFeelService _feelService = new();
        private readonly Dictionary<int, List<BossModifierId>> _bossModifiersByDepth = new();
        private readonly List<(int Row, int Col)> _lastFinderHints = new();
        private readonly List<TileXpEntry> _tileXpLog = new();

        public RunState RunState { get; private set; }
        public LevelConfig CurrentLevelConfig { get; private set; }
        public LevelState CurrentLevelState { get; private set; }
        public SudokuBoard CurrentBoard { get; private set; }
        public PuzzleAnalysis CurrentPuzzleAnalysis { get; private set; }
        public ModifierOverlayData CurrentOverlayData { get; private set; }
        public SudokuConstraintEngine CurrentConstraintEngine { get; private set; }
        public RunFeelState FeelState => _feelService.State;
        public BossService BossServiceInstance => _bossService;
        public List<RunNode> CurrentRunGraph { get; private set; } = new();
        public List<ShopOffer> CurrentShopOffers { get; private set; } = new();
        public RunEvent CurrentEvent { get; private set; }
        public TutorialSetupConfig ActiveTutorialSetup { get; private set; }
        public TutorialSetupConfig LastCompletedTutorialSetup { get; private set; }

        public int ShopPurchasesThisRun { get; private set; }
        public int EmergencyHealsThisRun { get; private set; }

        public event Action<RunSaveTrigger> AutoSaveRequested;

        public int CurrentMistakePenalty { get; private set; } = 1;
        public float CurrentGoldMultiplier { get; private set; } = 1f;
        public int CurrentBonusPencilReward { get; private set; }
        public int CurrentBonusXp { get; private set; }
        public int RunNumber { get; private set; } = 1;
        public IReadOnlyList<(int Row, int Col)> LastFinderHints => _lastFinderHints;

        public bool MilestoneClearedBoss { get; private set; }
        public BossModifierTier MilestoneClearedBossTier { get; private set; } = BossModifierTier.Tier1;
        public bool MilestoneSolvedEightByEightFourStar { get; private set; }
        public bool MilestoneCompletedKoiPath { get; private set; }
        public bool MilestoneWonWithUnderThreeHp { get; private set; }
        public bool MilestoneClearedGermanWhispersBoss { get; private set; }
        public bool MilestoneClearedMultiStageBoss { get; private set; }

        public RunDirector(int seed)
        {
            _random = new Random(seed);
            _itemService = new ItemService(seed + 11);
            _routeService = new RouteService(seed + 23);
            _bossService = new BossService(seed + 37);
            _runGraphService = new RunGraphService(seed + 47);
            _shopService = new ShopService(seed + 59);
            _relicService = new RelicService(seed + 67);
            _eventService = new RunEventService(_curseService);
        }

        public void StartRun(ClassId classId, GameMode mode = GameMode.GardenRun, int runNumber = 1, MetaProgressionState meta = null)
        {
            ActiveTutorialSetup = null;
            LastCompletedTutorialSetup = null;
            _bossModifiersByDepth.Clear();

            if (mode != GameMode.Tutorial)
            {
                if (meta == null)
                {
                    if (classId != ClassId.NumberFreak)
                    {
                        throw new InvalidOperationException("Class progression lock: only Number Freak is available without meta state.");
                    }
                }
                else if (!meta.UnlockedClasses.Contains(classId))
                {
                    throw new InvalidOperationException($"Class {classId} is locked by progression.");
                }
            }

            var snapshot = ClassCatalog.Build(classId);
            if (!snapshot.Playable)
            {
                throw new InvalidOperationException($"Class {classId} is locked.");
            }

            RunNumber = Math.Max(1, runNumber);

            RunState = new RunState
            {
                Seed = _random.Next(),
                Depth = 1,
                ClassId = classId,
                Mode = mode,
                TutorialMode = mode == GameMode.Tutorial,
                CurrentHP = snapshot.HP,
                MaxHP = snapshot.HP,
                CurrentPencil = snapshot.Pencil,
                MaxPencil = snapshot.Pencil,
                CurrentGold = 0,
                ItemSlots = snapshot.ItemSlots,
                RerollTokens = snapshot.RerollTokens,
                CurrentXP = 0
            };

            if (RunState.TutorialMode)
            {
                RunState.DisableProgressionRewards = true;
                RunState.CurrentGold = 0;
                RunState.RerollTokens = 0;
            }

            RunState.CurrentNodeIndex = 0;
            RunState.CurrentFloor = 0;
            _tileXpLog.Clear();
            CurrentRunGraph = _runGraphService.BuildFloorGraph(0, RunState.Seed);
            RunState.NodePath.Clear();
            for (var i = 0; i < CurrentRunGraph.Count; i++)
            {
                RunState.NodePath.Add(CurrentRunGraph[i]);
            }

            ShopPurchasesThisRun = 0;
            EmergencyHealsThisRun = 0;

            MilestoneClearedBoss = false;
            MilestoneClearedBossTier = BossModifierTier.Tier1;
            MilestoneSolvedEightByEightFourStar = false;
            MilestoneCompletedKoiPath = false;
            MilestoneWonWithUnderThreeHp = false;
            MilestoneClearedGermanWhispersBoss = false;
            MilestoneClearedMultiStageBoss = false;

            RefreshRunBuildIdentity();
        }

        public void AdvanceToNextGarden()
        {
            RunNumber++;
            RunState.CurrentFloor++;
            RunState.Depth++;
            RunState.CurrentNodeIndex = 0;
            RunState.PreBossPuzzlesCompleted = 0;
            RunState.ChosenBossModifier = null;
            RunState.ChosenBossModifiers.Clear();
            CurrentRunGraph = _runGraphService.BuildFloorGraph(RunState.CurrentFloor, RunState.Seed);
            RunState.NodePath.Clear();
            for (var i = 0; i < CurrentRunGraph.Count; i++)
            {
                RunState.NodePath.Add(CurrentRunGraph[i]);
            }

            RefreshRunBuildIdentity();
        }

        public void RebuildCurrentFloorGraph()
        {
            CurrentRunGraph = _runGraphService.BuildFloorGraph(RunState.CurrentFloor, RunState.Seed);
            RunState.NodePath.Clear();
            for (var i = 0; i < CurrentRunGraph.Count; i++)
                RunState.NodePath.Add(CurrentRunGraph[i]);
        }

        public void StartLevel(LevelConfig config)
        {
            CurrentLevelConfig = config;
            CurrentLevelState = new LevelState();

            var generation = SudokuGenerationService.Generate(new PuzzleGenerationRequest
            {
                BoardSize = config.BoardSize,
                Stars = config.Stars,
                TargetTier = ResolveTargetDifficultyTier(config),
                AllowBruteForceOnly = config.IsBoss && config.ActiveModifiers.Count >= 2,
                Seed = _random.Next(),
                RegionVariant = config.RegionVariant,
                ActiveModifiers = new List<BossModifierId>(config.ActiveModifiers)
            });

            if (!generation.Success || generation.Board == null)
            {
                CurrentBoard = SudokuGenerator.CreatePuzzle(config.BoardSize, config.MissingPercent, _random.Next(), config.RegionVariant);
                CurrentPuzzleAnalysis = SudokuLogicalAnalyzer.Analyze(CurrentBoard, config.ActiveModifiers, allowBruteForce: false);
            }
            else
            {
                CurrentBoard = generation.Board;
                CurrentPuzzleAnalysis = generation.Analysis;
            }

            if (config.ActiveModifiers.Count > 0)
            {
                // Try overlay generation with the current board; if the geometry doesn't fit
                // (e.g. no valid thermo paths, no ratio pairs), regenerate the entire puzzle.
                var overlayValid = false;
                for (var boardRetry = 0; boardRetry < 5 && !overlayValid; boardRetry++)
                {
                    if (boardRetry > 0)
                    {
                        // Regenerate puzzle with a new seed to get a different solution
                        var regen = SudokuGenerationService.Generate(new PuzzleGenerationRequest
                        {
                            BoardSize = config.BoardSize,
                            Stars = config.Stars,
                            TargetTier = ResolveTargetDifficultyTier(config),
                            AllowBruteForceOnly = config.IsBoss && config.ActiveModifiers.Count >= 2,
                            Seed = _random.Next(),
                            RegionVariant = config.RegionVariant,
                            ActiveModifiers = new List<BossModifierId>(config.ActiveModifiers)
                        });

                        if (regen.Success && regen.Board != null)
                        {
                            CurrentBoard = regen.Board;
                            CurrentPuzzleAnalysis = regen.Analysis;
                        }
                        else
                        {
                            CurrentBoard = SudokuGenerator.CreatePuzzle(config.BoardSize, config.MissingPercent, _random.Next(), config.RegionVariant);
                            CurrentPuzzleAnalysis = SudokuLogicalAnalyzer.Analyze(CurrentBoard, config.ActiveModifiers, allowBruteForce: false);
                        }
                    }

                    CurrentOverlayData = ModifierGeometryGenerator.Generate(
                        CurrentBoard, config.ActiveModifiers, _random.Next(), config.ModifierIntensity);

                    // Retry overlay seeds on the same board a few times
                    for (var seedRetry = 0; seedRetry < 12 && !HasAllModifiersPresent(config.ActiveModifiers, CurrentOverlayData); seedRetry++)
                    {
                        CurrentOverlayData = ModifierGeometryGenerator.Generate(
                            CurrentBoard, config.ActiveModifiers, _random.Next(), config.ModifierIntensity);
                    }

                    overlayValid = HasAllModifiersPresent(config.ActiveModifiers, CurrentOverlayData);
                }

                var rules = ModifierFactory.BuildRules(config.ActiveModifiers, CurrentOverlayData);
                CurrentConstraintEngine = new SudokuConstraintEngine();
                CurrentConstraintEngine.SetRulesDeterministic(rules);

                // Cells that are part of modifier overlays (lines, dots, cages, arrows, markers)
                // must never be given at puzzle start — the player must solve them.
                ClearOverlayCellsFromGivenMask(CurrentBoard, CurrentOverlayData);
            }
            else
            {
                CurrentOverlayData = null;
                CurrentConstraintEngine = null;
            }

        }

        public void StartTutorialRun(TutorialSetupConfig tutorialSetup)
        {
            var mode = GameMode.Tutorial;
            StartRun(ClassId.NumberFreak, mode, runNumber: 1, meta: null);
            ActiveTutorialSetup = CloneTutorialSetup(tutorialSetup);

            RunState.TutorialMode = true;
            RunState.DisableProgressionRewards = true;
            RunState.TutorialResourceMode = tutorialSetup.ResourceMode;
            RunState.CurrentGold = 0;
            RunState.RerollTokens = 0;

            if (tutorialSetup.ResourceMode == TutorialResourceMode.Free)
            {
                RunState.MaxHP = int.MaxValue;
                RunState.CurrentHP = int.MaxValue;
                RunState.MaxPencil = int.MaxValue;
                RunState.CurrentPencil = int.MaxValue;
            }
            else if (tutorialSetup.ResourceMode == TutorialResourceMode.ClassBased)
            {
                var snap = Classes.ClassCatalog.Build(tutorialSetup.SimulationClassId);
                RunState.MaxHP = snap.HP;
                RunState.CurrentHP = snap.HP;
                RunState.MaxPencil = snap.Pencil;
                RunState.CurrentPencil = snap.Pencil;
            }
            else
            {
                RunState.MaxHP = 10;
                RunState.CurrentHP = 10;
                RunState.MaxPencil = 10;
                RunState.CurrentPencil = 10;
            }

            var config = TutorialModeService.BuildLevelConfig(tutorialSetup);
            StartLevel(config);
        }

        public LevelConfig BuildLevelConfig(int runNumber, int depth)
        {
            if (RunState != null)
            {
                if (RunState.Mode == GameMode.EndlessZen)
                {
                    return _endlessZenService.BuildLevel(depth, RunState.Seed);
                }

                if (RunState.Mode == GameMode.SpiritTrials)
                {
                    return _spiritTrialsService.BuildTrialLevel(SpiritTrialsTier.Apprentice, RunState.Seed + depth);
                }
            }

            var difficulty = MapDifficulty(runNumber, depth);
            var boardSize = 4 + (int)difficulty;
            var stars = RollStarForRun(runNumber);
            var missing = StarDensityService.MissingPercentForStars(stars);
            var node = FindNodeByDepth(depth);
            var riskPath = node != null && node.IsRiskPath;
            var nodeType = node?.Type ?? NodeType.Puzzle;

            if (CurrentRunGraph != null && CurrentRunGraph.Count > 0)
            {
                _runGraphService.RevealNextTwoLayers(CurrentRunGraph, depth);
            }

            var allowIrregular = RunState == null || RunState.AllowIrregularPuzzles;
            var config = new LevelConfig
            {
                Difficulty = difficulty,
                Stars = stars,
                BoardSize = boardSize,
                MissingPercent = missing,
                IsBoss = false,
                RegionVariant = allowIrregular ? _random.Next(4) : _random.Next(2)
            };

            if (nodeType == NodeType.ElitePuzzle)
            {
                config.Difficulty = (DifficultyTier)Math.Min((int)DifficultyTier.Diff5, (int)config.Difficulty + 1);
                config.Stars = Math.Min(5, config.Stars + 1);
                config.MissingPercent = Math.Clamp(config.MissingPercent + 0.05f, 0.05f, 0.80f);
            }

            if (nodeType == NodeType.Boss)
            {
                config.IsBoss = true;
                config.Stars = Math.Max(config.Stars, 4);
                config.BoardSize = Math.Max(config.BoardSize, 8);

                // Intensity scales with floor: floor 0-1=Low, 2=Medium, 3=High, 4=VeryHigh
                var floor = RunState?.CurrentFloor ?? 0;
                config.ModifierIntensity = floor switch
                {
                    <= 1 => BossModifierIntensity.Low,
                    2    => BossModifierIntensity.Medium,
                    3    => BossModifierIntensity.High,
                    _    => BossModifierIntensity.VeryHigh
                };

                // Modifiers are applied by RunMapController.GetFixedLevelConfig from
                // ChosenBossModifiers after the player confirms at the boss gate panel.
                // Do NOT add modifiers here — they would be cached before the player chooses.
            }

            if (RunState.CorruptedGardenPath)
            {
                if (!config.ActiveModifiers.Contains(BossModifierId.ParityLines))
                {
                    config.ActiveModifiers.Add(BossModifierId.ParityLines);
                }
            }

            var allowSpike = nodeType == NodeType.ElitePuzzle;
            _varianceService.ApplyVariance(config, riskPath, _random, allowSpike);

            if (_random.NextDouble() < 0.1)
            {
                config.StressVariant = (StressVariant)_random.Next(1, 5);
            }

            return config;
        }

        public bool PlaceNumber(int row, int col, int value)
        {
            if (CurrentBoard == null || RunState == null || CurrentLevelState == null)
            {
                return false;
            }

            // Fogged cells: place without validation; wrong placements are penalised when fog is revealed.
            if (CurrentOverlayData != null && CurrentOverlayData.IsFogged(row, col))
            {
                CurrentBoard.SetCell(row, col, value);
                CurrentLevelState.Moves.Add(new MoveRecord
                {
                    Row = row,
                    Col = col,
                    Value = value,
                    WasCorrect = false,
                    WasPencil = false
                });
                return false;
            }

            var isCorrect = CurrentBoard.Solution[row, col] == value;
            CurrentBoard.SetCell(row, col, value);
            CurrentLevelState.Moves.Add(new MoveRecord
            {
                Row = row,
                Col = col,
                Value = value,
                WasCorrect = isCorrect,
                WasPencil = false
            });

            if (isCorrect)
            {
                CurrentLevelState.CorrectPlacements++;
                ApplyClassOnCorrectPlacement();
                _feelService.OnCorrectPlacement(RunState.CurrentHP, CurrentLevelConfig.IsBoss);

                var comboGold = _feelService.GetComboGoldBonus();
                if (!RunState.TutorialMode && comboGold > 0)
                {
                    RunState.CurrentGold += comboGold;
                }

                if (CurrentOverlayData != null && CurrentOverlayData.FogCells.Count > 0)
                {
                    // Capture which neighbours are currently fogged before the reveal.
                    var boardSize = CurrentBoard.Size;
                    var dr = new[] { 0, -1, 1, 0, 0 };
                    var dc = new[] { 0, 0, 0, -1, 1 };
                    var wasFogged = new System.Collections.Generic.List<(int r, int c)>();
                    for (var d = 0; d < 5; d++)
                    {
                        var rr = row + dr[d];
                        var cc = col + dc[d];
                        if (rr >= 0 && rr < boardSize && cc >= 0 && cc < boardSize
                            && CurrentOverlayData.IsFogged(rr, cc))
                        {
                            wasFogged.Add((rr, cc));
                        }
                    }

                    ModifierGeometryGenerator.RevealAdjacentFog(
                        CurrentOverlayData, row, col, boardSize);

                    // Penalise any wrong values that were placed in now-revealed fog cells.
                    for (var fi = 0; fi < wasFogged.Count; fi++)
                    {
                        var (fr, fc) = wasFogged[fi];
                        if (!CurrentOverlayData.IsFogged(fr, fc)
                            && !CurrentBoard.IsEmpty(fr, fc)
                            && !CurrentBoard.IsGiven(fr, fc)
                            && CurrentBoard.GetCell(fr, fc) != CurrentBoard.Solution[fr, fc])
                        {
                            CurrentLevelState.Mistakes++;
                            ApplyMistakePenalty();
                            _feelService.OnMistake(RunState.CurrentHP);
                        }
                    }
                }

                if (CurrentBoard.IsComplete())
                {
                    CurrentLevelState.PuzzleComplete = true;
                    CurrentOverlayData?.FogCells.Clear();
                }

                return true;
            }

            CurrentLevelState.Mistakes++;
            ApplyMistakePenalty();
            _feelService.OnMistake(RunState.CurrentHP);
            return false;
        }

        public bool TryAddPencilMark(int row, int col, int value)
        {
            if (!RunState.TutorialMode || RunState.TutorialResourceMode != TutorialResourceMode.Free)
            {
                if (RunState.CurrentPencil <= 0)
                {
                    return false;
                }
            }

            if (!CurrentBoard.IsEmpty(row, col))
            {
                return false;
            }

            var set = CurrentBoard.GetPencilSet(row, col);
            var added = set.Add(value);
            if (!added)
            {
                set.Remove(value);
                if (!RunState.TutorialMode || RunState.TutorialResourceMode != TutorialResourceMode.Free)
                {
                    RunState.CurrentPencil++;
                }

                return true;
            }

            var pencilCost = 1;
            if (CurrentLevelState.TeaOfFocusActive && CurrentLevelState.TeaOfFocusRemainingPlacements > 0)
            {
                pencilCost++;
            }

            if (!RunState.TutorialMode || RunState.TutorialResourceMode != TutorialResourceMode.Free)
            {
                RunState.CurrentPencil = Math.Max(0, RunState.CurrentPencil - pencilCost);
            }

            return true;
        }

        public bool TryBuyPencilUnits()
        {
            if (RunState.TutorialMode)
            {
                return false;
            }

            var classSnapshot = ClassCatalog.Build(RunState.ClassId);
            if (!classSnapshot.CanBuyPencilMidLevel)
            {
                return false;
            }

            var cost = FormulaService.PencilBuyCost(RunState.PencilPurchasesThisRun);
            if (RunState.CurrentGold < cost)
            {
                return false;
            }

            RunState.CurrentGold -= cost;
            RunState.CurrentPencil += 5;
            RunState.PencilPurchasesThisRun++;
            return true;
        }

        public List<ItemRollSlot> BuildItemRollPhase()
        {
            if (RunState.TutorialMode)
            {
                return new List<ItemRollSlot>();
            }

            var bonusSlots = RelicService.GetBonusRewardSlots(RunState);
            return _itemService.RollSlots(CurrentLevelConfig.Stars, GetClassLevel(), bonusSlots);
        }

        private int GetClassLevel()
        {
            // Level derived from TotalXp at runtime — during run, we don't have meta state,
            // so we use a conservative default of 1. The shop/item service can accept any level.
            return 1;
        }

        public bool TryRerollItemSlots(List<ItemRollSlot> slots)
        {
            if (RunState.TutorialMode)
            {
                return false;
            }

            var cost = FormulaService.RerollCost(RunState.RerollsThisRun);
            if (RunState.CurrentGold < cost)
            {
                return false;
            }

            RunState.CurrentGold -= cost;
            RunState.RerollsThisRun++;
            _itemService.RerollEligibleSlots(slots, CurrentLevelConfig.Stars, GetClassLevel());
            return true;
        }

        public void PickRolledSlot(List<ItemRollSlot> slots, int index)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (i == index)
                {
                    if (slots[i].IsNothing)
                    {
                        // Nothing slot grants no gold — pure sacrifice mechanic
                    }
                    else if (slots[i].RolledItem != null)
                    {
                        AddItemToInventory(slots[i].RolledItem);
                    }

                    slots[i].IsLocked = true;
                }
                else if (slots[i].IsNothing)
                {
                    slots[i].IsLocked = true;
                }
            }
        }

        public void PickRolledSlotReplacingIndex(List<ItemRollSlot> slots, int slotIndex, int replaceIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            var slot = slots[slotIndex];
            if (slot.IsNothing || slot.RolledItem == null) return;
            if (replaceIndex >= 0 && replaceIndex < RunState.Inventory.Count)
            {
                RunState.Inventory[replaceIndex] = slot.RolledItem;
            }
            slot.IsLocked = true;
        }

        public void CompleteLevelAndGrantRewards()
        {
            MarkSolvedBoard(CurrentLevelConfig.BoardSize, CurrentLevelConfig.Stars);

            var clearMind = _feelService.TryApplyClearMindBonus(
                puzzleComplete: CurrentLevelState.PuzzleComplete,
                noMistakes: CurrentLevelState.Mistakes == 0,
                noHpLoss: !_feelService.State.LostHp,
                noSolverItemUse: !_feelService.State.UsedSolverItem);

            if (RunState.TutorialMode || RunState.DisableProgressionRewards)
            {
                RunState.CurrentGold = 0;
                RunState.CurrentXP = 0;
                if (CurrentLevelState.PuzzleComplete)
                {
                    LastCompletedTutorialSetup = ActiveTutorialSetup != null
                        ? CloneTutorialSetup(ActiveTutorialSetup)
                        : BuildSetupFromCurrentLevel();
                }

                RunState.Depth++;
                return;
            }

            if (RunState.Mode == GameMode.EndlessZen)
            {
                var zenGold = (int)Math.Round(5 * RunState.GlobalGoldMultiplier);
                RunState.CurrentGold += Math.Max(0, zenGold);
                var zenTile = XpService.CalculateTile(CurrentLevelConfig.BoardSize, CurrentLevelConfig.Stars, 0, false, (CurrentLevelState?.Mistakes ?? 0) == 0);
                RunState.CurrentXP += zenTile.TotalXp;
                _tileXpLog.Add(zenTile);
                RunState.CurrentPencil += 1;
                RunState.Depth++;
                return;
            }

            if (RunState.Mode == GameMode.SpiritTrials)
            {
                var spiritTile = XpService.CalculateTile(CurrentLevelConfig.BoardSize, CurrentLevelConfig.Stars, 0, false, (CurrentLevelState?.Mistakes ?? 0) == 0);
                RunState.CurrentXP += spiritTile.TotalXp;
                _tileXpLog.Add(spiritTile);
                RunState.CurrentPencil += 1;
                RunState.Depth++;
                return;
            }

            var perfectSolve = (CurrentLevelState?.Mistakes ?? 0) == 0;
            var tileXp = XpService.CalculateTile(
                CurrentLevelConfig.BoardSize,
                CurrentLevelConfig.Stars,
                CurrentLevelConfig.ActiveModifiers.Count,
                CurrentLevelConfig.IsBoss,
                perfectSolve);
            _tileXpLog.Add(tileXp);

            var gold = FormulaService.CalculateGold(CurrentLevelConfig.Difficulty, CurrentLevelConfig.Stars);
            var modifierBonus = CurrentLevelConfig.ActiveModifiers.Count >= 2 ? 1.15f : CurrentLevelConfig.ActiveModifiers.Count == 1 ? 1.05f : 1f;
            gold = (int)Math.Round(gold * CurrentGoldMultiplier * RunState.GlobalGoldMultiplier * modifierBonus);

            RunState.CurrentGold += Math.Max(0, gold);
            RunState.CurrentXP += tileXp.TotalXp;
            RunState.CurrentPencil += Math.Max(0, 2 + CurrentBonusPencilReward);

            // Apply relic puzzle-complete effects
            RelicService.ApplyPuzzleComplete(RunState, perfectSolve);

            RunState.Depth++;
        }

        public RouteChoice RollRouteChoice()
        {
            if (RunState.TutorialMode)
            {
                return null;
            }

            return _routeService.RollChoice();
        }

        public RunNode GetCurrentNode()
        {
            if (CurrentRunGraph == null || CurrentRunGraph.Count == 0)
            {
                return null;
            }

            var index = Math.Clamp(RunState.CurrentNodeIndex, 0, CurrentRunGraph.Count - 1);
            return CurrentRunGraph[index];
        }

        public RunNode AdvanceToNextNode(bool chooseRiskPath)
        {
            if (CurrentRunGraph == null || CurrentRunGraph.Count == 0)
            {
                return null;
            }

            var currentIndex = Math.Clamp(RunState.CurrentNodeIndex, 0, CurrentRunGraph.Count - 1);
            var currentDepth = CurrentRunGraph[currentIndex].Depth;
            var nextIndex = -1;

            for (var i = 0; i < CurrentRunGraph.Count; i++)
            {
                var candidate = CurrentRunGraph[i];
                if (candidate.Depth <= currentDepth)
                {
                    continue;
                }

                if (candidate.Type == NodeType.Boss || candidate.IsRiskPath == chooseRiskPath)
                {
                    nextIndex = i;
                    break;
                }
            }

            if (nextIndex < 0)
            {
                return null;
            }

            RunState.CurrentNodeIndex = nextIndex;
            var node = CurrentRunGraph[nextIndex];
            _runGraphService.RevealNextTwoLayers(CurrentRunGraph, node.Depth);
            _adaptationService.TickMutationNode(RunState);
            return node;
        }

        public void ApplyRoute(RouteType route)
        {
            if (RunState.TutorialMode)
            {
                return;
            }

            RunState.RouteHistory.Add(route);
            MarkRouteCompleted(route);
            var mistakePenalty = CurrentMistakePenalty;
            var goldMultiplier = CurrentGoldMultiplier;
            var bonusPencilReward = CurrentBonusPencilReward;
            var bonusXp = CurrentBonusXp;
            _routeService.ApplyRouteProfile(route, CurrentLevelConfig, ref mistakePenalty, ref goldMultiplier, ref bonusPencilReward, ref bonusXp);
            CurrentMistakePenalty = mistakePenalty;
            CurrentGoldMultiplier = goldMultiplier;
            CurrentBonusPencilReward = bonusPencilReward;
            CurrentBonusXp = bonusXp;
        }

        public RunResult BuildRunResult(bool victory, int bossPhaseReached, int secondsPlayed)
        {
            RefreshRunBuildIdentity();

            var result = new RunResult
            {
                PlayedClassId = RunState.ClassId,
                Mode = RunState.Mode,
                Victory = victory,
                GardenDepthReached = RunState.Depth,
                GoldEarned = RunState.TutorialMode ? 0 : RunState.CurrentGold,
                XpEarned = RunState.TutorialMode ? 0 : RunState.CurrentXP,
                BossPhaseReached = bossPhaseReached,
                MistakesMade = CurrentLevelState?.Mistakes ?? 0,
                SecondsPlayed = Math.Max(1, secondsPlayed),
                TutorialMode = RunState.TutorialMode,
                ClearedBoss = MilestoneClearedBoss,
                ClearedBossTier = MilestoneClearedBossTier,
                SolvedEightByEightFourStar = MilestoneSolvedEightByEightFourStar,
                CompletedKoiPathRoute = MilestoneCompletedKoiPath,
                WonWithUnderThreeHp = victory && MilestoneWonWithUnderThreeHp,
                WonWithOneHp = victory && RunState.CurrentHP == 1,
                ClearedGermanWhispersBoss = MilestoneClearedGermanWhispersBoss,
                ClearedMultiStageBoss = MilestoneClearedMultiStageBoss,
                PerfectClear = FeelState.ClearMindAwarded,
                PeakCombo = FeelState.PeakCorrectStreak,
                FinalArchetype = RunState.CurrentArchetype,
                ItemsUsedThisRun = RunState.ItemsUsedCount,
                RelicsCollectedThisRun = RunState.HasRelic ? 1 : 0,
                ClearedStageNoPencilNoHpLoss = victory && RunState.PencilUsedCount == 0 && (CurrentLevelState?.Mistakes ?? 0) == 0,
                HighestBoardSize = CurrentLevelConfig?.BoardSize ?? 0,
                HighestStarCleared = CurrentLevelConfig?.Stars ?? 0,
                SimultaneousModifiersOnBoss = CurrentLevelConfig?.ActiveModifiers?.Count ?? 0,
                UsedAnyItem = RunState.ItemsUsedCount > 0,
                FlawlessFloor = victory && (CurrentLevelState?.Mistakes ?? 0) == 0
            };

            for (var ti = 0; ti < _tileXpLog.Count; ti++)
                result.TileXpEntries.Add(_tileXpLog[ti]);

            result.Analytics = _analyticsService.Build(RunState, result, CurrentLevelConfig, CurrentLevelState);
            return result;
        }

        public void MarkSolverItemUsed()
        {
            _feelService.MarkSolverItemUsed();
        }

        public void RequestAutoSave(RunSaveTrigger trigger)
        {
            AutoSaveRequested?.Invoke(trigger);
        }

        public void OnPauseRequested()
        {
            RequestAutoSave(RunSaveTrigger.Pause);
        }

        public void OnQuitRequested()
        {
            RequestAutoSave(RunSaveTrigger.Quit);
        }

        public void OnBossPhaseTransition()
        {
            RequestAutoSave(RunSaveTrigger.BossPhaseTransition);
        }

        public List<ShopOffer> BuildShopOffers()
        {
            var priceMult = RelicService.GetShopPriceMultiplier(RunState);
            CurrentShopOffers = _shopService.BuildOffers(RunState.CurrentFloor, GetClassLevel(), priceMult);
            return CurrentShopOffers;
        }

        public bool HasShopRerollTokenAvailable()
        {
            return RunState != null && RunState.RerollTokens > 0;
        }

        public int GetShopRerollGoldCostPreview()
        {
            if (RunState == null)
            {
                return 0;
            }

            return FormulaService.RerollCost(RunState.RerollsThisRun);
        }

        public bool TryRerollShopOffers(out int spentGold, out bool usedToken)
        {
            spentGold = 0;
            usedToken = false;

            if (RunState == null || CurrentShopOffers == null || CurrentShopOffers.Count == 0)
            {
                return false;
            }

            if (RunState.RerollTokens > 0)
            {
                RunState.RerollTokens--;
                usedToken = true;
            }
            else
            {
                spentGold = FormulaService.RerollCost(RunState.RerollsThisRun);
                if (RunState.CurrentGold < spentGold)
                {
                    return false;
                }

                RunState.CurrentGold -= spentGold;
                RunState.RerollsThisRun++;
            }

            // Include reroll count in the shop generation input so rerolled sets diverge.
            var priceMult = RelicService.GetShopPriceMultiplier(RunState);
            CurrentShopOffers = _shopService.BuildOffers(RunState.CurrentFloor, GetClassLevel(), priceMult);
            return true;
        }

        public bool TryPurchaseShopOffer(string offerId)
        {
            for (var i = 0; i < CurrentShopOffers.Count; i++)
            {
                var offer = CurrentShopOffers[i];
                if (offer.OfferId != offerId || RunState.CurrentGold < offer.Price)
                {
                    continue;
                }

                RunState.CurrentGold -= offer.Price;
                ShopPurchasesThisRun++;

                if (offer.IsRelic && offer.RelicOffer != null)
                {
                    // Single relic slot: if player already has one, caller should show choice UI first
                    _relicService.AcceptRelic(RunState, offer.RelicOffer);
                    RefreshRunBuildIdentity();
                }
                else if (offer.Item != null)
                {
                    AddItemToInventory(offer.Item);
                }

                CurrentShopOffers.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool TryPurchaseShopOfferReplacingSlot(string offerId, int replaceIndex)
        {
            for (var i = 0; i < CurrentShopOffers.Count; i++)
            {
                var offer = CurrentShopOffers[i];
                if (offer.OfferId != offerId || RunState.CurrentGold < offer.Price)
                {
                    continue;
                }

                if (offer.IsRelic)
                {
                    return TryPurchaseShopOffer(offerId);
                }

                if (offer.Item == null || replaceIndex < 0 || replaceIndex >= RunState.Inventory.Count)
                {
                    return false;
                }

                RunState.CurrentGold -= offer.Price;
                ShopPurchasesThisRun++;
                RunState.Inventory[replaceIndex] = offer.Item;
                CurrentShopOffers.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool TryUseInventoryItemAt(int inventoryIndex, int row, int col, out string message)
        {
            message = string.Empty;
            _lastFinderHints.Clear();

            if (RunState == null || CurrentBoard == null)
            {
                message = "No active puzzle.";
                return false;
            }

            if (inventoryIndex < 0 || inventoryIndex >= RunState.Inventory.Count)
            {
                message = "Invalid inventory slot.";
                return false;
            }

            var item = RunState.Inventory[inventoryIndex];
            if (item == null)
            {
                message = "Item slot is empty.";
                return false;
            }

            var used = false;
            switch (item.Type)
            {
                case ItemType.Solver:
                    used = ItemService.TryUseSolver(CurrentBoard, item.Rarity, row, col);
                    message = used ? "Solver used." : "Solver requires an empty selected cell.";
                    if (used) MarkSolverItemUsed();
                    break;
                case ItemType.Finder:
                {
                    var matches = ItemService.UseFinder(CurrentBoard, item.Rarity, row, col);
                    used = matches.Count > 0;
                    if (used) _lastFinderHints.AddRange(matches);
                    message = used ? $"Finder highlighted {matches.Count} matching cell(s)." : "Finder needs a selected filled value.";
                    break;
                }
                case ItemType.InkWell:
                {
                    var restore = ItemService.GetInkWellAmount(item.Rarity);
                    RunState.CurrentPencil = Math.Min(RunState.MaxPencil, RunState.CurrentPencil + restore);
                    used = true;
                    message = $"+{restore} Pencil.";
                    break;
                }
                case ItemType.MeditationStone:
                {
                    var heal = ItemService.GetMeditationStoneAmount(item.Rarity);
                    RunState.CurrentHP = Math.Min(RunState.MaxHP, RunState.CurrentHP + heal);
                    used = true;
                    message = $"+{heal} HP.";
                    break;
                }
                case ItemType.WindChime:
                {
                    // Undo last wrong input (within 3 moves)
                    var undone = TryUndoLastMistake();
                    if (undone)
                    {
                        if (item.Rarity >= ItemRarity.Rare)
                            RunState.CurrentHP = Math.Min(RunState.MaxHP, RunState.CurrentHP + 1);
                        if (item.Rarity >= ItemRarity.Epic)
                            RevealSingleCell(row, col, item.Rarity, out _, out _);
                    }
                    used = undone;
                    message = used ? "Wind Chime undid the last mistake." : "No recent mistake to undo.";
                    break;
                }
                case ItemType.PatternScroll:
                {
                    var zones = ItemService.GetPatternScrollZones(item.Rarity);
                    var added = zones == -1 ? HighlightFullConflictWeb() : HighlightConflictZones(row, col, zones);
                    used = added > 0;
                    message = used ? $"Pattern Scroll highlighted {added} conflict(s)." : "No conflicts found.";
                    break;
                }
                case ItemType.KoiReflection:
                {
                    var cells = ItemService.GetKoiReflectionCells(item.Rarity);
                    var revealed = RevealCandidatesForCells(row, col, cells);
                    used = revealed > 0;
                    message = used ? $"Koi Reflection revealed candidates for {revealed} cell(s)." : "No eligible cells.";
                    break;
                }
                case ItemType.LanternOfClarity:
                {
                    var moves = ItemService.GetLanternOfClarityMoves(item.Rarity);
                    CurrentLevelState.TeaOfFocusActive = true; // reuse fog-disable state
                    CurrentLevelState.TeaOfFocusRemainingPlacements = moves;
                    used = true;
                    message = $"Fog disabled for {moves} moves.";
                    break;
                }
                case ItemType.GardenRake:
                {
                    var highlighted = HighlightTwoCandidateCells(row, col);
                    used = highlighted > 0;
                    message = used ? $"Garden Rake highlighted {highlighted} cell(s) with 2 candidates." : "No 2-candidate cells in this row/column.";
                    break;
                }
                case ItemType.OfferingBowl:
                {
                    if (RunState.CurrentHP < 5)
                    {
                        used = false;
                        message = "Need at least 5 HP to use Offering Bowl.";
                    }
                    else
                    {
                        RunState.CurrentHP -= 5;
                        used = RevealSingleCell(row, col, ItemRarity.Normal, out var solvedRow, out var solvedCol);
                        message = used ? $"Offering Bowl revealed cell ({solvedRow + 1},{solvedCol + 1})." : "No empty cell to reveal.";
                    }
                    break;
                }
                case ItemType.PruningShears:
                {
                    used = RemoveImpossibleCandidate(row, col);
                    message = used ? "Pruning Shears removed an impossible candidate." : "No removable candidates in this box.";
                    break;
                }
                case ItemType.ZenSandSifter:
                {
                    var pairs = HighlightHiddenPairsInRow(row);
                    used = pairs > 0;
                    message = used ? $"Zen Sand Sifter found {pairs} hidden pair(s)." : "No hidden pairs in this row.";
                    break;
                }
                case ItemType.GinkgoLeaf:
                {
                    // Highlights all instances of a chosen number — tracked visually by UI
                    used = CurrentBoard.GetCell(row, col) > 0;
                    message = used ? "Ginkgo Leaf active — all instances highlighted." : "Select a filled cell to track.";
                    break;
                }
                case ItemType.RicePaperUmbrella:
                {
                    RunState.UmbrellaShieldCharges += 2;
                    used = true;
                    message = "Rice Paper Umbrella: next 2 mistakes cost 0 HP.";
                    break;
                }
                case ItemType.TempleIncense:
                {
                    // Correct cells for the selected number pulse — tracked by UI
                    used = CurrentBoard.GetCell(row, col) > 0;
                    message = used ? "Temple Incense active — correct cells pulsing." : "Select a filled cell.";
                    break;
                }
                case ItemType.KoiDragonScale:
                {
                    var filled = CompletesMostFilledLineOrBox();
                    used = filled > 0;
                    message = used ? $"Koi Dragon Scale completed {filled} cell(s)!" : "No eligible line or box to complete.";
                    break;
                }
                case ItemType.GoldenKintsugiJar:
                {
                    var mistakes = HighlightAllCurrentMistakes();
                    used = true;
                    message = mistakes > 0 ? $"Golden Kintsugi Jar found {mistakes} mistake(s)." : "No mistakes on the board.";
                    break;
                }
                case ItemType.SilkFan:
                {
                    // Phase 1: player clicked SilkFan — record first cell
                    if (CurrentBoard.IsGiven(row, col) || CurrentBoard.GetCell(row, col) == 0)
                    {
                        used = false;
                        message = "Select a filled (non-given) cell first.";
                    }
                    else
                    {
                        RunState.SilkFanPendingIndex = inventoryIndex;
                        RunState.SilkFanFirstRow = row;
                        RunState.SilkFanFirstCol = col;
                        used = false; // don't consume yet — consumed on phase 2
                        message = "Silk Fan: now select the second cell to swap with.";
                    }
                    break;
                }
                default:
                    message = "Item has no mapped effect.";
                    break;
            }

            if (!used)
            {
                return false;
            }

            RunState.ItemsUsedCount++;

            // Eternal Lotus relic: items have infinite uses
            if (!item.IsInfinite && !RelicService.HasInfiniteItems(RunState))
            {
                item.Charges = Math.Max(0, item.Charges - 1);
                if (item.Charges <= 0)
                {
                    RunState.Inventory.RemoveAt(inventoryIndex);
                }
            }

            return true;
        }

        public bool IsSilkFanPending => RunState != null && RunState.SilkFanPendingIndex >= 0;

        public void CancelSilkFan()
        {
            if (RunState == null) return;
            RunState.SilkFanPendingIndex = -1;
            RunState.SilkFanFirstRow = -1;
            RunState.SilkFanFirstCol = -1;
        }

        public bool TryCompleteSilkFanSwap(int secondRow, int secondCol, out string message)
        {
            message = string.Empty;
            if (RunState == null || CurrentBoard == null || RunState.SilkFanPendingIndex < 0)
            {
                message = "No Silk Fan swap pending.";
                return false;
            }

            var firstRow = RunState.SilkFanFirstRow;
            var firstCol = RunState.SilkFanFirstCol;
            var itemIndex = RunState.SilkFanPendingIndex;

            // Reset pending state regardless of outcome
            RunState.SilkFanPendingIndex = -1;
            RunState.SilkFanFirstRow = -1;
            RunState.SilkFanFirstCol = -1;

            if (firstRow == secondRow && firstCol == secondCol)
            {
                message = "Silk Fan cancelled — same cell.";
                return false;
            }

            if (CurrentBoard.IsGiven(secondRow, secondCol) || CurrentBoard.GetCell(secondRow, secondCol) == 0)
            {
                message = "Second cell must be filled and non-given.";
                return false;
            }

            // Perform the swap
            var valA = CurrentBoard.GetCell(firstRow, firstCol);
            var valB = CurrentBoard.GetCell(secondRow, secondCol);
            CurrentBoard.SetCell(firstRow, firstCol, valB);
            CurrentBoard.SetCell(secondRow, secondCol, valA);

            // Consume the item
            RunState.ItemsUsedCount++;
            if (itemIndex >= 0 && itemIndex < RunState.Inventory.Count)
            {
                var item = RunState.Inventory[itemIndex];
                if (item != null && !item.IsInfinite && !RelicService.HasInfiniteItems(RunState))
                {
                    item.Charges = Math.Max(0, item.Charges - 1);
                    if (item.Charges <= 0)
                    {
                        RunState.Inventory.RemoveAt(itemIndex);
                    }
                }
            }

            message = $"Silk Fan swapped ({firstRow + 1},{firstCol + 1}) and ({secondRow + 1},{secondCol + 1}).";
            return true;
        }

        private int ClearPencilsInRowAndColumn(int row, int col)
        {
            if (CurrentBoard == null)
            {
                return 0;
            }

            var cleared = 0;
            for (var c = 0; c < CurrentBoard.Size; c++)
            {
                cleared += ClearPencilAt(row, c);
            }

            for (var r = 0; r < CurrentBoard.Size; r++)
            {
                if (r == row)
                {
                    continue;
                }

                cleared += ClearPencilAt(r, col);
            }

            return cleared;
        }

        private int ClearPencilAt(int row, int col)
        {
            if (CurrentBoard == null || CurrentBoard.IsGiven(row, col) || !CurrentBoard.IsEmpty(row, col))
            {
                return 0;
            }

            var set = CurrentBoard.GetPencilSet(row, col);
            var count = set.Count;
            set.Clear();
            return count;
        }

        private int AddLegalPencilsToCell(int row, int col)
        {
            if (CurrentBoard == null || CurrentBoard.IsGiven(row, col) || !CurrentBoard.IsEmpty(row, col))
            {
                return 0;
            }

            var set = CurrentBoard.GetPencilSet(row, col);
            var before = set.Count;
            for (var value = 1; value <= CurrentBoard.Size; value++)
            {
                if (IsLegalAt(row, col, value))
                {
                    set.Add(value);
                }
            }

            return set.Count - before;
        }

        private bool AddSingleCorrectCandidate(int row, int col)
        {
            if (CurrentBoard == null || CurrentBoard.IsGiven(row, col) || !CurrentBoard.IsEmpty(row, col))
            {
                return false;
            }

            var solutionValue = CurrentBoard.Solution[row, col];
            if (solutionValue <= 0)
            {
                return false;
            }

            CurrentBoard.GetPencilSet(row, col).Add(solutionValue);
            return true;
        }

        private bool TryUndoLastMistake()
        {
            if (CurrentLevelState == null || CurrentLevelState.Moves.Count == 0)
                return false;

            // Look back up to 3 moves for the most recent wrong placement
            var limit = Math.Min(3, CurrentLevelState.Moves.Count);
            for (var i = CurrentLevelState.Moves.Count - 1; i >= CurrentLevelState.Moves.Count - limit; i--)
            {
                var move = CurrentLevelState.Moves[i];
                if (!move.WasCorrect && !move.WasPencil)
                {
                    CurrentBoard.ClearCell(move.Row, move.Col);
                    CurrentLevelState.Moves.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private int HighlightConflictZones(int row, int col, int zoneCount)
        {
            // Highlights conflicts in row/col/box — returns count of conflicts found
            if (CurrentBoard == null) return 0;
            var conflicts = 0;
            // Check row
            for (var c = 0; c < CurrentBoard.Size && conflicts < zoneCount; c++)
            {
                if (c == col) continue;
                var val = CurrentBoard.GetCell(row, c);
                if (val > 0 && val != CurrentBoard.Solution[row, c]) conflicts++;
            }
            // Check column
            for (var r = 0; r < CurrentBoard.Size && conflicts < zoneCount; r++)
            {
                if (r == row) continue;
                var val = CurrentBoard.GetCell(r, col);
                if (val > 0 && val != CurrentBoard.Solution[r, col]) conflicts++;
            }
            return conflicts;
        }

        private int HighlightFullConflictWeb()
        {
            if (CurrentBoard == null) return 0;
            var conflicts = 0;
            for (var r = 0; r < CurrentBoard.Size; r++)
                for (var c = 0; c < CurrentBoard.Size; c++)
                {
                    var val = CurrentBoard.GetCell(r, c);
                    if (val > 0 && val != CurrentBoard.Solution[r, c]) conflicts++;
                }
            return conflicts;
        }

        private int RevealCandidatesForCells(int row, int col, int count)
        {
            if (CurrentBoard == null) return 0;
            var revealed = 0;

            // Start from selected cell, then scan nearby
            for (var r = 0; r < CurrentBoard.Size && revealed < count; r++)
            {
                for (var c = 0; c < CurrentBoard.Size && revealed < count; c++)
                {
                    if (!CurrentBoard.IsEmpty(r, c) || CurrentBoard.IsGiven(r, c)) continue;
                    var added = AddLegalPencilsToCell(r, c);
                    if (added > 0) revealed++;
                }
            }
            return revealed;
        }

        private int HighlightTwoCandidateCells(int row, int col)
        {
            if (CurrentBoard == null) return 0;
            var count = 0;
            // Check cells in same row and column
            for (var c = 0; c < CurrentBoard.Size; c++)
            {
                if (CurrentBoard.IsEmpty(row, c) && !CurrentBoard.IsGiven(row, c))
                {
                    var candidates = CountCandidates(row, c);
                    if (candidates == 2) count++;
                }
            }
            for (var r = 0; r < CurrentBoard.Size; r++)
            {
                if (r == row) continue;
                if (CurrentBoard.IsEmpty(r, col) && !CurrentBoard.IsGiven(r, col))
                {
                    var candidates = CountCandidates(r, col);
                    if (candidates == 2) count++;
                }
            }
            return count;
        }

        private int CountCandidates(int row, int col)
        {
            var count = 0;
            for (var v = 1; v <= CurrentBoard.Size; v++)
                if (IsLegalAt(row, v == col ? row : row, v)) count++;
            // Simple implementation: count legal values
            count = 0;
            for (var v = 1; v <= CurrentBoard.Size; v++)
                if (IsLegalAt(row, col, v)) count++;
            return count;
        }

        private bool RemoveImpossibleCandidate(int row, int col)
        {
            if (CurrentBoard == null) return false;
            // Find the 3x3 box containing this cell and remove one wrong candidate
            var regionMap = CurrentBoard.RegionMap;
            if (regionMap == null) return false;

            var regionId = regionMap[row, col];
            for (var r = 0; r < CurrentBoard.Size; r++)
            {
                for (var c = 0; c < CurrentBoard.Size; c++)
                {
                    if (regionMap[r, c] != regionId) continue;
                    if (!CurrentBoard.IsEmpty(r, c)) continue;
                    var pencils = CurrentBoard.GetPencilSet(r, c);
                    foreach (var val in pencils)
                    {
                        if (val != CurrentBoard.Solution[r, c])
                        {
                            pencils.Remove(val);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private int HighlightHiddenPairsInRow(int row)
        {
            if (CurrentBoard == null) return 0;
            // Simplified: count cells in this row that have exactly 2 candidates
            var pairs = 0;
            for (var c = 0; c < CurrentBoard.Size; c++)
            {
                if (CurrentBoard.IsEmpty(row, c) && !CurrentBoard.IsGiven(row, c))
                {
                    var count = 0;
                    for (var v = 1; v <= CurrentBoard.Size; v++)
                        if (IsLegalAt(row, c, v)) count++;
                    if (count == 2) pairs++;
                }
            }
            return pairs / 2; // pairs, not cells
        }

        private int CompletesMostFilledLineOrBox()
        {
            if (CurrentBoard == null) return 0;
            var bestEmpty = int.MaxValue;
            var bestType = -1; // 0=row, 1=col, 2=region
            var bestIndex = -1;

            // Find the row/col/region with fewest empty cells (but at least 1)
            for (var i = 0; i < CurrentBoard.Size; i++)
            {
                var rowEmpty = 0;
                var colEmpty = 0;
                for (var j = 0; j < CurrentBoard.Size; j++)
                {
                    if (CurrentBoard.IsEmpty(i, j)) rowEmpty++;
                    if (CurrentBoard.IsEmpty(j, i)) colEmpty++;
                }
                if (rowEmpty > 0 && rowEmpty < bestEmpty) { bestEmpty = rowEmpty; bestType = 0; bestIndex = i; }
                if (colEmpty > 0 && colEmpty < bestEmpty) { bestEmpty = colEmpty; bestType = 1; bestIndex = i; }
            }

            if (bestIndex < 0 || bestEmpty == 0) return 0;

            var filled = 0;
            if (bestType == 0) // row
            {
                for (var c = 0; c < CurrentBoard.Size; c++)
                {
                    if (CurrentBoard.IsEmpty(bestIndex, c))
                    {
                        CurrentBoard.SetCell(bestIndex, c, CurrentBoard.Solution[bestIndex, c]);
                        filled++;
                    }
                }
            }
            else // col
            {
                for (var r = 0; r < CurrentBoard.Size; r++)
                {
                    if (CurrentBoard.IsEmpty(r, bestIndex))
                    {
                        CurrentBoard.SetCell(r, bestIndex, CurrentBoard.Solution[r, bestIndex]);
                        filled++;
                    }
                }
            }
            return filled;
        }

        private int HighlightAllCurrentMistakes()
        {
            if (CurrentBoard == null) return 0;
            var mistakes = 0;
            for (var r = 0; r < CurrentBoard.Size; r++)
                for (var c = 0; c < CurrentBoard.Size; c++)
                {
                    var val = CurrentBoard.GetCell(r, c);
                    if (val > 0 && !CurrentBoard.IsGiven(r, c) && val != CurrentBoard.Solution[r, c])
                        mistakes++;
                }
            return mistakes;
        }

        private bool RevealSingleCell(int row, int col, ItemRarity rarity, out int solvedRow, out int solvedCol)
        {
            solvedRow = -1;
            solvedCol = -1;
            if (CurrentBoard == null)
            {
                return false;
            }

            if (CurrentBoard.IsEmpty(row, col) && !CurrentBoard.IsGiven(row, col))
            {
                CurrentBoard.SetCell(row, col, CurrentBoard.Solution[row, col]);
                solvedRow = row;
                solvedCol = col;
                return true;
            }

            var preferBottom = rarity == ItemRarity.Epic;
            for (var r = 0; r < CurrentBoard.Size; r++)
            {
                var rr = preferBottom ? (CurrentBoard.Size - 1 - r) : r;
                for (var c = 0; c < CurrentBoard.Size; c++)
                {
                    if (!CurrentBoard.IsEmpty(rr, c) || CurrentBoard.IsGiven(rr, c))
                    {
                        continue;
                    }

                    CurrentBoard.SetCell(rr, c, CurrentBoard.Solution[rr, c]);
                    solvedRow = rr;
                    solvedCol = c;
                    return true;
                }
            }

            return false;
        }

        private bool IsLegalAt(int row, int col, int value)
        {
            if (CurrentBoard == null)
            {
                return false;
            }

            for (var i = 0; i < CurrentBoard.Size; i++)
            {
                if (CurrentBoard.GetCell(row, i) == value || CurrentBoard.GetCell(i, col) == value)
                {
                    return false;
                }
            }

            var regionMap = CurrentBoard.RegionMap;
            if (regionMap == null)
            {
                return true;
            }

            var regionId = regionMap[row, col];
            for (var r = 0; r < CurrentBoard.Size; r++)
            {
                for (var c = 0; c < CurrentBoard.Size; c++)
                {
                    if ((r != row || c != col) && regionMap[r, c] == regionId && CurrentBoard.GetCell(r, c) == value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryBuyEmergencyHeal()
        {
            var price = _shopService.EmergencyHealPrice(EmergencyHealsThisRun);
            if (RunState.CurrentGold < price)
            {
                return false;
            }

            RunState.CurrentGold -= price;
            EmergencyHealsThisRun++;
            RunState.CurrentHP = Math.Min(RunState.MaxHP, RunState.CurrentHP + 2);
            return true;
        }

        public RunEvent BuildCurrentEvent()
        {
            CurrentEvent = _eventService.RollEvent(_random, RunState);
            return CurrentEvent;
        }

        public bool ResolveCurrentEventChoice(string optionId)
        {
            var resolved = _eventService.ResolveChoice(RunState, CurrentEvent, optionId);
            if (resolved)
            {
                RefreshRunBuildIdentity();
            }

            return resolved;
        }

        public bool TryTransformRelicsAtNode()
        {
            var changed = _adaptationService.TryTransformRelics(RunState, _random);
            if (changed)
            {
                RefreshRunBuildIdentity();
            }

            return changed;
        }

        /// <summary>Roll and offer a relic at a relic node. Returns the offered relic and whether a choice is needed.</summary>
        public (RelicInstance Offered, bool NeedsChoice) RollRelicNodeReward(bool isRiskRoute)
        {
            var tierBonus = RelicService.GetRelicNodeTierBonus(RunState);
            var effectiveFloor = RunState.CurrentFloor + 1 + tierBonus;
            var offered = _relicService.RollRelic(effectiveFloor, isRiskRoute);
            var needsChoice = _relicService.OfferRelic(RunState, offered);
            return (offered, needsChoice);
        }

        /// <summary>Player chose which relic to keep after a relic node choice.</summary>
        public void AcceptRelicChoice(RelicInstance chosen)
        {
            _relicService.AcceptRelic(RunState, chosen);
            RefreshRunBuildIdentity();
        }

        public void ApplyTemporaryMutation(AdaptationMutationType mutation, int nodes = 3)
        {
            _adaptationService.ApplyTemporaryMutation(RunState, mutation, nodes);
        }

        public bool TryRiskyRebuild()
        {
            var changed = _adaptationService.TryRiskyRebuild(RunState);
            if (changed)
            {
                RefreshRunBuildIdentity();
            }

            return changed;
        }

        public bool TryRerouteModifierMeta(MetaProgressionState meta, BossModifierId remove, BossModifierId add)
        {
            return _adaptationService.TryRerouteModifier(meta, remove, add);
        }

        public PuzzleSaveState ExportPuzzleSaveState()
        {
            if (CurrentBoard == null)
            {
                return null;
            }

            var size = CurrentBoard.Size;
            var cellCount = size * size;
            var save = new PuzzleSaveState
            {
                BoardSize = size,
                SolutionFlat = new int[cellCount],
                RegionMapFlat = new int[cellCount],
                CellsFlat = new int[cellCount],
                GivenFlat = new bool[cellCount],
                PencilSerializedPerCell = new string[cellCount],
                ModifierStateJson = string.Join(",", CurrentLevelConfig.ActiveModifiers),
                CurrentHP = RunState.CurrentHP,
                CurrentPencil = RunState.CurrentPencil,
                CurrentGold = RunState.CurrentGold,
                ComboStreak = FeelState.CurrentCorrectStreak,
                PeakCombo = FeelState.PeakCorrectStreak,
                MusicLayer = FeelState.CurrentMusicLayer,
                Mistakes = CurrentLevelState?.Mistakes ?? 0,
                CorrectPlacements = CurrentLevelState?.CorrectPlacements ?? 0,
                Stars = CurrentLevelConfig.Stars,
                Difficulty = (int)CurrentLevelConfig.Difficulty,
                IsBoss = CurrentLevelConfig.IsBoss
            };

            var index = 0;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    save.SolutionFlat[index] = CurrentBoard.Solution[row, col];
                    save.RegionMapFlat[index] = CurrentBoard.RegionMap[row, col];
                    save.CellsFlat[index] = CurrentBoard.Cells[row, col];
                    save.GivenFlat[index] = CurrentBoard.GivenMask[row, col];
                    save.PencilSerializedPerCell[index] = string.Join(",", CurrentBoard.GetPencilSet(row, col));
                    index++;
                }
            }

            return save;
        }

        public bool TryRestorePuzzleSaveState(PuzzleSaveState save)
        {
            if (save == null || save.BoardSize <= 0)
            {
                return false;
            }

            var size = save.BoardSize;
            var solution = new int[size, size];
            var region = new int[size, size];
            var cells = new int[size, size];
            var given = new bool[size, size];

            var index = 0;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    solution[row, col] = save.SolutionFlat[index];
                    region[row, col] = save.RegionMapFlat[index];
                    cells[row, col] = save.CellsFlat[index];
                    given[row, col] = save.GivenFlat[index];
                    index++;
                }
            }

            CurrentBoard = new SudokuBoard(size, solution, cells, given, region);
            CurrentLevelConfig = new LevelConfig
            {
                BoardSize = size,
                Difficulty = (DifficultyTier)save.Difficulty,
                Stars = save.Stars,
                MissingPercent = StarDensityService.MissingPercentForStars(save.Stars),
                IsBoss = save.IsBoss
            };

            if (!string.IsNullOrWhiteSpace(save.ModifierStateJson))
            {
                var tokens = save.ModifierStateJson.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < tokens.Length; i++)
                {
                    if (Enum.TryParse<BossModifierId>(tokens[i], out var modifier))
                    {
                        CurrentLevelConfig.ActiveModifiers.Add(modifier);
                    }
                }
            }

            CurrentLevelState = new LevelState
            {
                Mistakes = save.Mistakes,
                CorrectPlacements = save.CorrectPlacements
            };

            index = 0;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var pencil = save.PencilSerializedPerCell[index];
                    if (!string.IsNullOrWhiteSpace(pencil))
                    {
                        var values = pencil.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (var i = 0; i < values.Length; i++)
                        {
                            if (int.TryParse(values[i], out var value))
                            {
                                CurrentBoard.GetPencilSet(row, col).Add(value);
                            }
                        }
                    }

                    index++;
                }
            }

            RunState.CurrentHP = save.CurrentHP;
            RunState.CurrentPencil = save.CurrentPencil;
            RunState.CurrentGold = save.CurrentGold;
            FeelState.CurrentCorrectStreak = save.ComboStreak;
            FeelState.PeakCorrectStreak = save.PeakCombo;
            FeelState.CurrentMusicLayer = save.MusicLayer;
            return true;
        }

        public void MarkBossCleared(BossModifierTier tier, bool includedGermanWhispers, bool wasMultiStage)
        {
            MilestoneClearedBoss = true;
            if (tier > MilestoneClearedBossTier)
            {
                MilestoneClearedBossTier = tier;
            }

            if (includedGermanWhispers)
            {
                MilestoneClearedGermanWhispersBoss = true;
            }

            if (wasMultiStage)
            {
                MilestoneClearedMultiStageBoss = true;
            }
        }

        public void MarkSolvedBoard(int boardSize, int stars)
        {
            if (boardSize >= 8 && stars >= 4)
            {
                MilestoneSolvedEightByEightFourStar = true;
            }
        }

        public void MarkRouteCompleted(RouteType route)
        {
            if (route == RouteType.KoiPond)
            {
                MilestoneCompletedKoiPath = true;
            }
        }

        public bool TryConsumeLastCompletedTutorialSetup(out TutorialSetupConfig setup)
        {
            setup = LastCompletedTutorialSetup;
            LastCompletedTutorialSetup = null;
            return setup != null;
        }

        public List<BossModifierId> RollBossModifierChoices(int runNumber)
        {
            return _bossService.RollBossChoices(runNumber, CurrentLevelConfig.Stars);
        }

        public List<BossModifierId> GetBossChoicesForDepth(int depth)
        {
            if (_bossModifiersByDepth.TryGetValue(depth, out var choices))
            {
                return choices;
            }

            var stars = CurrentLevelConfig?.Stars ?? 3;
            var rolled = _bossService.RollBossChoices(RunNumber, stars);
            _bossModifiersByDepth[depth] = rolled;
            return rolled;
        }

        public List<BossPhase> BuildFinalBoss()
        {
            return _bossService.BuildFinalThreePhaseBoss();
        }

        public List<BossPhase> BuildHiddenDualModifierBoss()
        {
            return _bossService.BuildHiddenDualModifierBoss();
        }

        private void AddItemToInventory(ItemInstance item)
        {
            if (RunState.Inventory.Count < RunState.ItemSlots)
            {
                RunState.Inventory.Add(item);
                return;
            }

            RunState.Inventory.RemoveAt(0);
            RunState.Inventory.Add(item);
        }

        private static TutorialSetupConfig CloneTutorialSetup(TutorialSetupConfig source)
        {
            if (source == null)
            {
                return null;
            }

            var copy = new TutorialSetupConfig
            {
                BoardSize = source.BoardSize,
                Stars = source.Stars,
                ResourceMode = source.ResourceMode
            };

            for (var i = 0; i < source.SelectedModifiers.Count; i++)
            {
                copy.SelectedModifiers.Add(source.SelectedModifiers[i]);
            }

            return copy;
        }

        private TutorialSetupConfig BuildSetupFromCurrentLevel()
        {
            if (CurrentLevelConfig == null)
            {
                return null;
            }

            var setup = new TutorialSetupConfig
            {
                BoardSize = CurrentLevelConfig.BoardSize,
                Stars = CurrentLevelConfig.Stars,
                ResourceMode = RunState != null ? RunState.TutorialResourceMode : TutorialResourceMode.Simulation
            };

            for (var i = 0; i < CurrentLevelConfig.ActiveModifiers.Count; i++)
            {
                setup.SelectedModifiers.Add(CurrentLevelConfig.ActiveModifiers[i]);
            }

            return setup;
        }

        private void ApplyMistakePenalty()
        {
            if (CurrentLevelState.TeaOfFocusActive && CurrentLevelState.TeaOfFocusRemainingPlacements > 0)
            {
                CurrentLevelState.TeaOfFocusRemainingPlacements--;
                return;
            }

            // Relic mistake absorption (Cracked Teacup, Rice Paper Umbrella)
            if (RelicService.TryAbsorbMistake(RunState))
            {
                return;
            }

            var hpCost = CurrentMistakePenalty;
            if (RunState.TutorialMode && RunState.TutorialResourceMode == TutorialResourceMode.Free)
            {
                hpCost = 0;
            }

            if (RunState.ClassId == ClassId.KoiGambler && _random.NextDouble() < 0.25)
            {
                hpCost = 0;
            }

            if (RunState.ComboMistakeProtectionCharges > 0)
            {
                RunState.ComboMistakeProtectionCharges--;
                hpCost = 0;
            }

            if (RunState.MistakeShieldCharges > 0)
            {
                RunState.MistakeShieldCharges--;
                hpCost = Math.Max(0, hpCost - 1);
            }

            if (RunState.ActiveCurses.Contains(CurseType.IncreasedMistakePenalty))
            {
                hpCost += 1;
            }

            RunState.CurrentHP -= hpCost;
            RunState.CurrentHP = Math.Max(0, RunState.CurrentHP);

            if (hpCost > 0)
            {
                _feelService.OnHpLoss();
                RunState.LostHpThisRun = true;
                RelicService.OnWrongPlacement(RunState);
            }

            // Phoenix Feather: prevent death
            if (RunState.CurrentHP <= 0 && RelicService.TryPreventDeath(RunState))
            {
                // Death prevented — HP/Pencil fully restored
            }

            if (RunState.CurrentHP > 0 && RunState.CurrentHP < 3)
            {
                MilestoneWonWithUnderThreeHp = true;
            }
        }

        private void ApplyClassOnCorrectPlacement()
        {
            if (RunState.TutorialMode)
            {
                return;
            }

            if (RunState.ClassId == ClassId.KoiGambler && _random.NextDouble() < 0.25)
            {
                RunState.CurrentGold += 1;
            }

            if (RunState.ClassId == ClassId.GardenMonk && CurrentLevelState.CorrectPlacements % 5 == 0)
            {
                RunState.CurrentHP = Math.Min(RunState.MaxHP, RunState.CurrentHP + 1);
            }

            // Monk Charm relic: streak gold bonus
            RelicService.OnCorrectPlacement(RunState);
        }

        private static DifficultyTier MapDifficulty(int runNumber, int depth)
        {
            var progression = Math.Max(runNumber, depth);
            if (progression <= 2)
            {
                return DifficultyTier.Diff2;
            }

            if (progression <= 4)
            {
                return DifficultyTier.Diff3;
            }

            if (progression <= 6)
            {
                return DifficultyTier.Diff4;
            }

            return DifficultyTier.Diff5;
        }

        private int RollStarForRun(int runNumber)
        {
            var min = runNumber switch
            {
                <= 2 => 1,
                <= 4 => 2,
                <= 6 => 2,
                <= 8 => 3,
                9 => 4,
                _ => 5
            };

            var max = runNumber >= 9 ? 5 : Math.Min(5, min + 2);
            return _random.Next(min, max + 1);
        }

        private static PuzzleDifficultyTier ResolveTargetDifficultyTier(LevelConfig config)
        {
            if (config.IsBoss)
            {
                return PuzzleDifficultyTier.Tier4;
            }

            return config.Stars switch
            {
                <= 2 => PuzzleDifficultyTier.Tier1,
                3 => PuzzleDifficultyTier.Tier2,
                4 => PuzzleDifficultyTier.Tier3,
                _ => PuzzleDifficultyTier.Tier4
            };
        }

        private RunNode FindNodeByDepth(int depth)
        {
            if (CurrentRunGraph == null)
            {
                return null;
            }

            for (var i = 0; i < CurrentRunGraph.Count; i++)
            {
                if (CurrentRunGraph[i].Depth == depth)
                {
                    return CurrentRunGraph[i];
                }
            }

            return null;
        }

        private void RefreshRunBuildIdentity()
        {
            if (RunState == null)
            {
                return;
            }

            // Single relic slot — no synergy stacking needed
            RunState.CurrentArchetype = _archetypeService.Evaluate(RunState);
        }

        private static BossModifierTier ResolveConstraintTier(List<BossModifierId> modifiers)
        {
            var tier = BossModifierTier.Tier1;
            for (var i = 0; i < modifiers.Count; i++)
            {
                var current = modifiers[i] switch
                {
                    BossModifierId.ParityLines => BossModifierTier.Tier1,
                    BossModifierId.DifferenceKropki => BossModifierTier.Tier1,
                    BossModifierId.DutchWhispers => BossModifierTier.Tier2,
                    BossModifierId.RenbanLines => BossModifierTier.Tier2,
                    BossModifierId.RatioKropki => BossModifierTier.Tier2,
                    BossModifierId.KillerCages => BossModifierTier.Tier3,
                    BossModifierId.ArrowSums => BossModifierTier.Tier3,
                    BossModifierId.FogOfWar => BossModifierTier.Tier4,
                    BossModifierId.GermanWhispers => BossModifierTier.Tier5,
                    _ => BossModifierTier.Tier1
                };

                if (current > tier)
                {
                    tier = current;
                }
            }

            return tier;
        }

        private static bool HasAllModifiersPresent(List<BossModifierId> modifiers, ModifierOverlayData overlay)
        {
            if (overlay == null) return false;
            for (var i = 0; i < modifiers.Count; i++)
            {
                switch (modifiers[i])
                {
                    case BossModifierId.GermanWhispers:
                    case BossModifierId.DutchWhispers:
                    case BossModifierId.ParityLines:
                    case BossModifierId.RenbanLines:
                    case BossModifierId.Palindrome:
                    case BossModifierId.Thermo:
                    case BossModifierId.BetweenLines:
                        if (overlay.Lines.Count == 0) return false;
                        break;
                    case BossModifierId.DifferenceKropki:
                    case BossModifierId.RatioKropki:
                        if (overlay.Dots.Count == 0) return false;
                        break;
                    case BossModifierId.KillerCages:
                        if (overlay.Cages.Count == 0) return false;
                        break;
                    case BossModifierId.ArrowSums:
                        if (overlay.Arrows.Count == 0) return false;
                        break;
                    case BossModifierId.FogOfWar:
                        if (overlay.FogCells.Count == 0) return false;
                        break;
                    case BossModifierId.EvenOdd:
                        if (overlay.CellMarkers.Count == 0) return false;
                        break;
                }
            }
            return true;
        }

        private static void ClearOverlayCellsFromGivenMask(SudokuBoard board, ModifierOverlayData overlay)
        {
            if (board == null || overlay == null) return;

            for (var li = 0; li < overlay.Lines.Count; li++)
            {
                var line = overlay.Lines[li];
                for (var ci = 0; ci < line.Cells.Count; ci++)
                {
                    var cell = line.Cells[ci];
                    board.GivenMask[cell.Row, cell.Col] = false;
                    board.Cells[cell.Row, cell.Col] = 0;
                }
            }

            for (var di = 0; di < overlay.Dots.Count; di++)
            {
                var dot = overlay.Dots[di];
                board.GivenMask[dot.CellA.Row, dot.CellA.Col] = false;
                board.Cells[dot.CellA.Row, dot.CellA.Col] = 0;
                board.GivenMask[dot.CellB.Row, dot.CellB.Col] = false;
                board.Cells[dot.CellB.Row, dot.CellB.Col] = 0;
            }

            for (var ki = 0; ki < overlay.Cages.Count; ki++)
            {
                var cage = overlay.Cages[ki];
                for (var ci = 0; ci < cage.Cells.Count; ci++)
                {
                    var cell = cage.Cells[ci];
                    board.GivenMask[cell.Row, cell.Col] = false;
                    board.Cells[cell.Row, cell.Col] = 0;
                }
            }

            for (var ai = 0; ai < overlay.Arrows.Count; ai++)
            {
                var arrow = overlay.Arrows[ai];
                board.GivenMask[arrow.Circle.Row, arrow.Circle.Col] = false;
                board.Cells[arrow.Circle.Row, arrow.Circle.Col] = 0;
                for (var pi = 0; pi < arrow.Path.Count; pi++)
                {
                    var cell = arrow.Path[pi];
                    board.GivenMask[cell.Row, cell.Col] = false;
                    board.Cells[cell.Row, cell.Col] = 0;
                }
            }

            for (var mi = 0; mi < overlay.CellMarkers.Count; mi++)
            {
                var marker = overlay.CellMarkers[mi];
                board.GivenMask[marker.Cell.Row, marker.Cell.Col] = false;
                board.Cells[marker.Cell.Row, marker.Cell.Col] = 0;
            }
        }

        private static (bool HasArithmetic, bool HasFog, bool HasDual) ResolveInterferenceFlags(List<BossModifierId> modifiers)
        {
            var hasArithmetic = false;
            var hasFog = false;

            for (var i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] == BossModifierId.KillerCages || modifiers[i] == BossModifierId.ArrowSums)
                {
                    hasArithmetic = true;
                }

                if (modifiers[i] == BossModifierId.FogOfWar)
                {
                    hasFog = true;
                }
            }

            return (hasArithmetic, hasFog, modifiers.Count >= 2);
        }
    }
}
