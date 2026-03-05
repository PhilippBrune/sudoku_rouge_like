using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
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
        private readonly RelicSynergyService _relicSynergyService = new();
        private readonly RunArchetypeService _archetypeService = new();
        private readonly CurseService _curseService = new();
        private readonly RunEventService _eventService;
        private readonly RunVarianceService _varianceService = new();
        private readonly MidRunAdaptationService _adaptationService = new();
        private readonly PostRunAnalyticsService _analyticsService = new();
        private readonly EndlessZenService _endlessZenService = new();
        private readonly SpiritTrialsService _spiritTrialsService = new();
        private readonly RunFeelService _feelService = new();

        public RunState RunState { get; private set; }
        public LevelConfig CurrentLevelConfig { get; private set; }
        public LevelState CurrentLevelState { get; private set; }
        public SudokuBoard CurrentBoard { get; private set; }
        public PuzzleAnalysis CurrentPuzzleAnalysis { get; private set; }
        public RunFeelState FeelState => _feelService.State;
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
            _relicService = new RelicService();
            _eventService = new RunEventService(_curseService);
        }

        public void StartRun(ClassId classId, GameMode mode = GameMode.GardenRun, int runNumber = 1, MetaProgressionState meta = null)
        {
            ActiveTutorialSetup = null;
            LastCompletedTutorialSetup = null;

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
                Level = 1,
                CurrentXP = 0
            };

            if (RunState.TutorialMode)
            {
                RunState.DisableProgressionRewards = true;
                RunState.CurrentGold = 0;
                RunState.RerollTokens = 0;
            }

            RunState.CurrentHeatScore = 1f;
            RunState.PeakHeatScore = 1f;
            RunState.HeatHistory.Clear();
            RunState.HeatHistory.Add(1f);
            RunState.CurrentNodeIndex = 0;
            CurrentRunGraph = _runGraphService.BuildRunGraph(RunNumber);
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
                ActiveModifiers = new List<BossModifierId>(config.ActiveModifiers)
            });

            if (!generation.Success || generation.Board == null)
            {
                CurrentBoard = SudokuGenerator.CreatePuzzle(config.BoardSize, config.MissingPercent, _random.Next());
                CurrentPuzzleAnalysis = SudokuLogicalAnalyzer.Analyze(CurrentBoard, config.ActiveModifiers, allowBruteForce: false);
            }
            else
            {
                CurrentBoard = generation.Board;
                CurrentPuzzleAnalysis = generation.Analysis;
            }

            UpdateCurrentHeatScore();
            CurrentLevelState.StartHeatScore = CurrentLevelState.CurrentHeatScore;
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
                    return _endlessZenService.BuildLevel(depth);
                }

                if (RunState.Mode == GameMode.SpiritTrials)
                {
                    return _spiritTrialsService.BuildDailyTrialLevel(RunState.Seed + depth);
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

            var config = new LevelConfig
            {
                Difficulty = difficulty,
                Stars = stars,
                BoardSize = boardSize,
                MissingPercent = missing,
                IsBoss = false
            };

            if (nodeType == NodeType.ElitePuzzle)
            {
                config.Difficulty = (DifficultyTier)Math.Min((int)DifficultyTier.Diff5, (int)config.Difficulty + 1);
                config.Stars = Math.Min(5, config.Stars + 1);
                config.MissingPercent = Math.Clamp(config.MissingPercent + 0.05f, 0.05f, 0.80f);
            }

            if (RunState.CorruptedGardenPath)
            {
                config.ActiveModifiers.Add(BossModifierId.ParityLines);
            }

            var expectedHeat = Math.Max(1f, RunState.CurrentHeatScore <= 0f ? 1f : RunState.CurrentHeatScore);
            var allowSpike = nodeType == NodeType.ElitePuzzle;
            _varianceService.ApplyVariance(config, expectedHeat, riskPath, _random, allowSpike);

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

                UpdateCurrentHeatScore();

                if (CurrentBoard.IsComplete())
                {
                    CurrentLevelState.PuzzleComplete = true;
                }

                return true;
            }

            CurrentLevelState.Mistakes++;
            ApplyMistakePenalty();
            _feelService.OnMistake(RunState.CurrentHP);
            UpdateCurrentHeatScore();
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

            UpdateCurrentHeatScore();
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
            UpdateCurrentHeatScore();
            return true;
        }

        public List<ItemRollSlot> BuildItemRollPhase()
        {
            if (RunState.TutorialMode)
            {
                return new List<ItemRollSlot>();
            }

            return _itemService.RollSlots(CurrentLevelConfig.Difficulty, CurrentLevelConfig.Stars);
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
            _itemService.RerollEligibleSlots(slots, CurrentLevelConfig.Difficulty, CurrentLevelConfig.Stars);
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
                        RunState.CurrentGold += Math.Max(0, slots[i].NothingGoldBonus);
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
                if (CurrentLevelState.PuzzleComplete && ActiveTutorialSetup != null)
                {
                    LastCompletedTutorialSetup = CloneTutorialSetup(ActiveTutorialSetup);
                }

                RunState.Depth++;
                return;
            }

            var gold = FormulaService.CalculateGold(CurrentLevelConfig.Difficulty, CurrentLevelConfig.Stars);
            var xp = FormulaService.CalculateXp(CurrentLevelConfig.Difficulty, CurrentLevelConfig.Stars);

            var modifierBonus = CurrentLevelConfig.ActiveModifiers.Count >= 2 ? 1.15f : CurrentLevelConfig.ActiveModifiers.Count == 1 ? 1.05f : 1f;
            gold = (int)Math.Round(gold * CurrentGoldMultiplier * RunState.GlobalGoldMultiplier * modifierBonus);
            xp += CurrentBonusXp;
            if (clearMind)
            {
                xp = (int)Math.Round(xp * 1.10f);
            }

            if (RunState.CarryGoldInterest)
            {
                var interest = (int)Math.Round(RunState.CurrentGold * 0.05f);
                RunState.CurrentGold += Math.Max(0, interest);
            }

            RunState.CurrentGold += Math.Max(0, gold);
            RunState.CurrentXP += Math.Max(0, xp);
            RunState.CurrentPencil += Math.Max(0, 2 + CurrentBonusPencilReward);
            RunState.Depth++;
            UpdateCurrentHeatScore();
            RunState.HeatHistory.Add(RunState.CurrentHeatScore);

            ProcessLevelUps();
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
            UpdateCurrentHeatScore();
        }

        public bool CanAcceptNextLevelHeat(LevelConfig nextLevel)
        {
            var hpRatio = RunState.MaxHP <= 0 ? 1f : (float)RunState.CurrentHP / RunState.MaxHP;
            var startPencil = Math.Max(1, RunState.MaxPencil);
            var pencilRatio = (float)Math.Max(0, RunState.CurrentPencil) / startPencil;
            var modifierTier = ResolveConstraintTier(nextLevel.ActiveModifiers);
            var interference = ResolveInterferenceFlags(nextLevel.ActiveModifiers);

            var nextHeat = HeatScoreService.ComputeHeatScore(
                nextLevel.BoardSize,
                nextLevel.MissingPercent,
                modifierTier,
                interference.HasArithmetic,
                interference.HasFog,
                interference.HasDual,
                hpRatio,
                pencilRatio);

            return HeatScoreService.IsValidHeatStep(RunState.CurrentHeatScore, nextHeat, nextLevel.IsBoss);
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
                FinalHeatScore = RunState.CurrentHeatScore,
                PeakHeatScore = RunState.PeakHeatScore,
                GoldEarned = RunState.TutorialMode ? 0 : RunState.CurrentGold,
                XpEarned = RunState.TutorialMode ? 0 : RunState.CurrentXP,
                EssenceEarned = RunState.TutorialMode ? 0 : Math.Max(0, (victory ? 20 : 5) + (RunState.Depth / 2)),
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
                FinalArchetype = RunState.CurrentArchetype
            };

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
            CurrentShopOffers = _shopService.BuildOffers(RunState.Depth, ShopPurchasesThisRun);
            return CurrentShopOffers;
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

                if (offer.IsRelic)
                {
                    if (!string.IsNullOrWhiteSpace(offer.RelicId))
                    {
                        RunState.RelicIds.Add(offer.RelicId);
                        _relicService.ApplyRunRelicEffects(RunState);
                        if (offer.RelicId.Contains("shifting_garden", StringComparison.OrdinalIgnoreCase))
                        {
                            RunState.CorruptedGardenPath = true;
                        }
                        else if (offer.RelicId.Contains("silent_grid", StringComparison.OrdinalIgnoreCase))
                        {
                            RunState.MistakeShieldCharges += 2;
                        }
                        else if (offer.RelicId.Contains("golden_root", StringComparison.OrdinalIgnoreCase))
                        {
                            RunState.CarryGoldInterest = true;
                        }

                        RefreshRunBuildIdentity();
                    }
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
                if (RunState.RelicIds[RunState.RelicIds.Count - 1].Contains("cursed", StringComparison.OrdinalIgnoreCase))
                {
                    _curseService.ApplyCurse(RunState, CurseType.CursedRelicBacklash);
                }

                RefreshRunBuildIdentity();
            }

            return changed;
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
            UpdateCurrentHeatScore();
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

        private void ApplyMistakePenalty()
        {
            if (CurrentLevelState.TeaOfFocusActive && CurrentLevelState.TeaOfFocusRemainingPlacements > 0)
            {
                CurrentLevelState.TeaOfFocusRemainingPlacements--;
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
        }

        private void ProcessLevelUps()
        {
            if (RunState.TutorialMode || RunState.DisableProgressionRewards)
            {
                return;
            }

            while (RunState.Level < 30)
            {
                var needed = FormulaService.XpToNextLevel(RunState.Level);
                if (RunState.CurrentXP < needed)
                {
                    break;
                }

                RunState.CurrentXP -= needed;
                RunState.Level++;
                ApplyLevelReward(RunState.Level);
            }
        }

        private void ApplyLevelReward(int level)
        {
            switch (level)
            {
                case 3:
                    RunState.CurrentPencil += 1;
                    break;
                case 7:
                    RunState.MaxHP += 2;
                    RunState.CurrentHP += 2;
                    break;
                case 10:
                    RunState.CurrentPencil += 2;
                    RunState.MaxHP += 2;
                    RunState.CurrentHP += 2;
                    break;
                case 15:
                    RunState.RerollTokens += 1;
                    break;
                case 20:
                    RunState.ItemSlots += 1;
                    break;
                case 30:
                    AddItemToInventory(new ItemInstance
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Type = ItemType.Solver,
                        Rarity = ItemRarity.Rare,
                        Charges = 1
                    });
                    break;
            }
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

        private void UpdateCurrentHeatScore()
        {
            if (RunState == null || CurrentLevelConfig == null)
            {
                return;
            }

            var hpRatio = RunState.MaxHP <= 0 ? 1f : (float)RunState.CurrentHP / RunState.MaxHP;
            var startPencil = Math.Max(1, RunState.MaxPencil);
            var pencilRatio = (float)Math.Max(0, RunState.CurrentPencil) / startPencil;

            var modifierTier = ResolveConstraintTier(CurrentLevelConfig.ActiveModifiers);
            var flags = ResolveInterferenceFlags(CurrentLevelConfig.ActiveModifiers);

            var heat = HeatScoreService.ComputeHeatScore(
                CurrentLevelConfig.BoardSize,
                CurrentLevelConfig.MissingPercent,
                modifierTier,
                flags.HasArithmetic,
                flags.HasFog,
                flags.HasDual,
                hpRatio,
                pencilRatio);

            heat *= _curseService.GetCurseHeatMultiplier(RunState);

            RunState.CurrentHeatScore = heat;
            RunState.PeakHeatScore = Math.Max(RunState.PeakHeatScore, heat);

            if (CurrentLevelState != null)
            {
                CurrentLevelState.CurrentHeatScore = heat;
            }
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

            var synergy = _relicSynergyService.Build(RunState.RelicIds);
            RunState.GlobalGoldMultiplier = synergy.GoldMultiplier;
            RunState.CarryGoldInterest = RunState.CarryGoldInterest || synergy.CarryGoldInterest;
            RunState.MistakeShieldCharges = Math.Max(RunState.MistakeShieldCharges, synergy.MistakeShieldCharges);
            RunState.ComboMistakeProtectionCharges = Math.Max(RunState.ComboMistakeProtectionCharges, synergy.ComboMistakeProtectionCharges);
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
