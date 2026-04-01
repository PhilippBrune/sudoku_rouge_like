using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Run
{
    public sealed class RunDirector
    {
        // ── Services ──
        private BossService _bossService;
        private ItemService _itemService;
        private ShopService _shopService;
        private RelicService _relicService;
        private RunGraphService _graphService;
        private RunEventService _eventService;
        private RunVarianceService _varianceService;
        private MidRunAdaptationService _adaptationService;
        private PostRunAnalyticsService _analytics;
        private RouteService _routeService;

        // ── State ──
        public RunState State { get; private set; }
        public LevelConfig CurrentLevelConfig { get; private set; }
        public LevelState CurrentLevelState { get; private set; }
        public SudokuBoard CurrentBoard { get; private set; }
        public ModifierOverlayData CurrentOverlay { get; private set; }
        public SudokuConstraintEngine ConstraintEngine { get; private set; }
        public List<RunNode> CurrentFloorGraph { get; private set; }
        public List<ShopOffer> CurrentShopOffers { get; private set; }
        public RunEvent CurrentEvent { get; private set; }
        public List<TileXpEntry> TileXpLog { get; private set; } = new List<TileXpEntry>();
        public List<ItemInstance> RolledItemSlots { get; private set; }
        public List<BossModifierId> BossModifierChoices { get; private set; }

        // ── Init ──

        /// <summary>Restore state from a save file. Used by RunResumeService.</summary>
        public void RestoreState(RunState state)
        {
            State = state;
        }

        public void StartRun(LaunchRequest request, int seed)
        {
            State = RunArchetypeService.CreateRunState(request.ClassId, seed, request.AllowIrregularPuzzles);
            State.Mode = request.Mode;

            InitServices(seed);
            TileXpLog.Clear();
            _analytics = new PostRunAnalyticsService();

            // Roll floor modifiers for the initial floor (Floor 1 = 0 modifiers)
            RollFloorModifiers();
            RebuildFloorGraph();
        }

        public void StartTutorialRun(TutorialSetupConfig setup, int seed)
        {
            State = Tutorial.TutorialModeService.CreateTutorialRunState(setup, seed);

            InitServices(seed);
            TileXpLog.Clear();
            _analytics = new PostRunAnalyticsService();

            var tutService = new Tutorial.TutorialModeService();
            CurrentLevelConfig = tutService.BuildTutorialLevel(setup, seed);
            StartLevel(CurrentLevelConfig);
        }

        private void InitServices(int seed)
        {
            _bossService = new BossService(seed);
            _itemService = new ItemService(seed + 1);
            _shopService = new ShopService(seed + 2);
            _relicService = new RelicService(seed + 3);
            _graphService = new RunGraphService();
            _eventService = new RunEventService(seed + 4);
            _varianceService = new RunVarianceService(seed + 5);
            _adaptationService = new MidRunAdaptationService();
            _routeService = new RouteService();
        }

        // ── Floor / Graph ──

        public void RebuildFloorGraph()
        {
            CurrentFloorGraph = _graphService.BuildFloorGraph(State.CurrentFloor, State.Seed);
            State.CurrentNodeIndex = 0;
        }

        public void AdvanceToNextFloor()
        {
            State.CurrentFloor++;
            State.Depth = 0;

            // Clear boss modifiers from previous floor
            State.ChosenBossModifiers.Clear();
            State.HasChosenBossModifier = false;

            // Roll floor modifiers for the new floor
            RollFloorModifiers();

            RebuildFloorGraph();
        }

        private void RollFloorModifiers()
        {
            var minSize = FloorThemeData.GetMinBoardSize(State.CurrentFloor);
            State.ActiveFloorModifiers = _bossService.RollFloorModifiers(State.CurrentFloor, minSize);

            // Add floor modifiers to seen set for "???" reveal tracking
            for (var i = 0; i < State.ActiveFloorModifiers.Count; i++)
                State.SeenBossModifiers.Add(State.ActiveFloorModifiers[i]);
        }

        // ── Level Generation ──

        public LevelConfig BuildLevelConfig(bool isBoss, bool isElite = false, int nodeIndex = 0)
        {
            var floor = State.CurrentFloor;
            var minSize = FloorThemeData.GetMinBoardSize(floor);
            var maxSize = FloorThemeData.GetMaxBoardSize(floor);
            var rng = new Random(State.Seed + State.Depth * 31 + floor * 997 + nodeIndex * 13);
            var size = rng.Next(minSize, maxSize + 1);
            var stars = RollStars(rng, floor, isElite);

            var config = new LevelConfig
            {
                BoardSize = size,
                Stars = stars,
                MissingPercent = StarDensityService.MissingPercentForStars(stars),
                RegionVariant = State.AllowIrregularPuzzles ? rng.Next(4) : rng.Next(2),
                IsBoss = isBoss,
                Seed = rng.Next(),
                Intensity = BossService.IntensityForRunNumber(State.RunNumber),
                Difficulty = MapDifficulty(floor, stars)
            };

            // Always add floor modifiers (all puzzles on floors 2–5)
            if (State.ActiveFloorModifiers != null)
            {
                for (var i = 0; i < State.ActiveFloorModifiers.Count; i++)
                {
                    if (!config.ActiveModifiers.Contains(State.ActiveFloorModifiers[i]))
                        config.ActiveModifiers.Add(State.ActiveFloorModifiers[i]);
                }
            }

            // Add boss-chosen modifiers (plural)
            if (isBoss && State.ChosenBossModifiers != null)
            {
                for (var i = 0; i < State.ChosenBossModifiers.Count; i++)
                {
                    if (!config.ActiveModifiers.Contains(State.ChosenBossModifiers[i]))
                        config.ActiveModifiers.Add(State.ChosenBossModifiers[i]);
                }
            }

            _varianceService.ApplyVariance(config, State.Depth, floor);

            return config;
        }

        public void StartLevel(LevelConfig config)
        {
            CurrentLevelConfig = config;
            CurrentLevelState = new LevelState();

            CurrentBoard = SudokuGenerator.CreatePuzzle(
                config.BoardSize, config.MissingPercent, config.Seed, config.RegionVariant);

            // Generate modifier overlay
            CurrentOverlay = new ModifierOverlayData();
            if (config.ActiveModifiers.Count > 0)
            {
                CurrentOverlay = ModifierGeometryGenerator.Generate(
                    CurrentBoard, config.ActiveModifiers, config.Seed, config.Intensity);
                ClearOverlayCellsFromGivenMask();
            }

            // Build constraint engine
            ConstraintEngine = new SudokuConstraintEngine();
            var rules = ModifierFactory.BuildRules(config.ActiveModifiers);
            for (var i = 0; i < rules.Count; i++)
                ConstraintEngine.RegisterRule(rules[i]);
        }

        // ── Gameplay ──

        public PlaceResult PlaceNumber(int row, int col, int value)
        {
            if (CurrentBoard.IsGiven(row, col))
                return PlaceResult.IsGiven;

            var isValid = ConstraintEngine.ValidateAll(CurrentBoard, row, col, value, CurrentOverlay);

            CurrentBoard.PlaceValue(row, col, value);

            if (isValid)
            {
                CurrentLevelState.CorrectPlacements++;
                return PlaceResult.Correct;
            }

            CurrentLevelState.Mistakes++;
            CurrentLevelState.PerfectSoFar = false;
            return PlaceResult.Invalid;
        }

        public bool TryAddPencilMark(int row, int col, int value)
        {
            if (State.CurrentPencil <= 0 && State.Mode != GameMode.Tutorial) return false;
            if (CurrentBoard.IsGiven(row, col)) return false;

            CurrentBoard.TogglePencilMark(row, col, value);
            if (State.Mode != GameMode.Tutorial)
            {
                State.CurrentPencil--;
                CurrentLevelState.PencilMarksUsed++;
                CurrentLevelState.NoPencilUsed = false;
            }
            return true;
        }

        public void ApplyMistakePenalty(int damage = 1)
        {
            if (RelicService.TryAbsorbMistake(State)) return;

            State.CurrentHP = Math.Max(0, State.CurrentHP - damage);
        }

        public bool IsPlayerDead => State.CurrentHP <= 0;

        public bool IsLevelComplete => CurrentBoard != null && CurrentBoard.IsComplete();

        // ── Rewards ──

        public TileXpEntry CompleteLevelAndGrantRewards()
        {
            var config = CurrentLevelConfig;
            var level = CurrentLevelState;

            var goldBase = GoldTable.CalculatePuzzleGold(config.BoardSize, config.Stars);
            State.CurrentGold += goldBase;

            var xpEntry = XpService.CalculateTile(
                config.BoardSize, config.Stars,
                config.ActiveModifiers.Count, config.IsBoss,
                level.PerfectSoFar);

            TileXpLog.Add(xpEntry);
            _analytics?.RecordPuzzleSolved(level.Mistakes, goldBase);
            _analytics?.RecordTileXp(xpEntry);

            return xpEntry;
        }

        // ── Item Reward Rolling ──

        public List<ItemInstance> BuildItemRewardSlots()
        {
            var bonusSlots = RelicService.GetBonusRewardSlots(State);
            var classLevel = GetCurrentClassLevel();
            RolledItemSlots = _itemService.RollSlots(CurrentLevelConfig.Stars, classLevel, bonusSlots);
            return RolledItemSlots;
        }

        public void PickRewardItem(int slotIndex)
        {
            if (RolledItemSlots == null || slotIndex < 0 || slotIndex >= RolledItemSlots.Count) return;
            var item = RolledItemSlots[slotIndex];
            if (item == null) return;

            AddItemToInventory(item);
            RolledItemSlots = null;
        }

        public void AddItemToInventory(ItemInstance item)
        {
            if (State.HeldItems.Count < State.ItemSlots)
                State.HeldItems.Add(item);
        }

        public bool IsBagFull() => State != null && State.HeldItems.Count >= State.ItemSlots;

        /// <summary>Replaces the item at the given bag slot with newItem; clears any pending rolled reward.</summary>
        public void ReplaceItemInInventory(int slotIndex, ItemInstance newItem)
        {
            if (slotIndex < 0 || slotIndex >= State.HeldItems.Count) return;
            State.HeldItems[slotIndex] = newItem;
            RolledItemSlots = null;
        }

        // ── Item Usage ──

        public bool TryUseItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= State.HeldItems.Count) return false;

            var item = State.HeldItems[inventoryIndex];
            if (item == null) return false;

            CurrentLevelState.ItemsUsedThisLevel++;
            _analytics?.RecordItemUsed();

            if (!RelicService.HasInfiniteItems(State))
            {
                item.Charges--;
                if (item.Charges <= 0)
                    State.HeldItems.RemoveAt(inventoryIndex);
            }

            return true;
        }

        // ── Shop ──

        public List<ShopOffer> BuildShopOffers()
        {
            var priceMultiplier = RelicService.GetShopPriceMultiplier(State);
            var classLevel = GetCurrentClassLevel();
            CurrentShopOffers = _shopService.BuildOffers(State.CurrentFloor, classLevel, priceMultiplier);
            return CurrentShopOffers;
        }

        public bool TryPurchaseShopOffer(int offerIndex)
        {
            if (CurrentShopOffers == null || offerIndex < 0 || offerIndex >= CurrentShopOffers.Count) return false;

            var offer = CurrentShopOffers[offerIndex];
            if (offer.IsSold || State.CurrentGold < offer.Price) return false;

            State.CurrentGold -= offer.Price;
            offer.IsSold = true;
            AddItemToInventory(offer.Item);
            return true;
        }

        public bool TryBuyEmergencyHeal()
        {
            var price = ShopService.EmergencyHealPrice(State.PencilPurchaseCount);
            if (State.CurrentGold < price) return false;

            State.CurrentGold -= price;
            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
            State.PencilPurchaseCount++;
            return true;
        }

        // ── Boss Modifier Choice ──

        public List<BossModifierId> RollBossModifierChoices()
        {
            var minSize = FloorThemeData.GetMinBoardSize(State.CurrentFloor);
            BossModifierChoices = _bossService.RollBossChoices(State.CurrentFloor, State.ActiveFloorModifiers, minSize);
            return BossModifierChoices;
        }

        public void GetBossModifierCounts(out int shown, out int picks)
        {
            BossService.GetBossModifierCounts(State.CurrentFloor, out shown, out picks);
        }

        public void ChooseBossModifiers(List<BossModifierId> chosen)
        {
            State.ChosenBossModifiers.Clear();
            if (chosen == null) return;

            for (var i = 0; i < chosen.Count; i++)
            {
                State.ChosenBossModifiers.Add(chosen[i]);
                State.SeenBossModifiers.Add(chosen[i]);
            }

            // Keep legacy singular field in sync for save compat
            if (chosen.Count > 0)
            {
                State.HasChosenBossModifier = true;
                State.ChosenBossModifierId = chosen[0];
            }
        }

        /// <summary>Legacy single-choice shortcut (picks 1 modifier).</summary>
        public void ChooseBossModifier(BossModifierId chosen)
        {
            ChooseBossModifiers(new List<BossModifierId> { chosen });
        }

        // ── Events ──

        public RunEvent BuildCurrentEvent()
        {
            CurrentEvent = _eventService.BuildEvent(State.CurrentFloor, State.Depth);
            return CurrentEvent;
        }

        public void ResolveCurrentEventChoice(int optionIndex)
        {
            if (CurrentEvent == null) return;
            _eventService.ResolveChoice(State, CurrentEvent, optionIndex);
            CurrentEvent = null;
        }

        // ── Relic ──

        public RelicInstance RollRelicReward()
        {
            var tierBonus = RelicService.GetRelicNodeTierBonus(State);
            return _relicService.RollRelic(State.CurrentFloor, tierBonus);
        }

        public void AcceptRelic(RelicInstance relic)
        {
            State.HasRelic = true;
            State.HeldRelic = relic;
        }

        // ── Navigation ──

        public bool TryAdvanceToNode(int nodeIndex)
        {
            if (!_routeService.CanMoveTo(CurrentFloorGraph, State.CurrentNodeIndex, nodeIndex))
                return false;

            _routeService.MarkVisited(CurrentFloorGraph, nodeIndex);
            State.CurrentNodeIndex = nodeIndex;
            State.NodePath.Add(nodeIndex);
            State.Depth++;
            return true;
        }

        public List<int> GetReachableNodes()
        {
            return _routeService.GetReachableNodes(CurrentFloorGraph, State.CurrentNodeIndex);
        }

        public RunNode GetCurrentNode()
        {
            if (CurrentFloorGraph == null || State.CurrentNodeIndex < 0
                || State.CurrentNodeIndex >= CurrentFloorGraph.Count)
                return null;
            return CurrentFloorGraph[State.CurrentNodeIndex];
        }

        // ── Save / Restore ──

        public PuzzleSaveState ExportPuzzleSaveState()
        {
            if (CurrentBoard == null) return null;

            var size = CurrentBoard.Size;
            var totalCells = size * size;
            var save = new PuzzleSaveState
            {
                BoardSize = size,
                Stars = CurrentLevelConfig?.Stars ?? 1,
                IsBoss = CurrentLevelConfig?.IsBoss ?? false,
                Board = new int[totalCells],
                Solution = new int[totalCells],
                GivenMask = new int[totalCells],
                RegionMap = new int[totalCells]
            };

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var idx = r * size + c;
                save.Board[idx] = CurrentBoard.Cells[r, c];
                save.Solution[idx] = CurrentBoard.Solution[r, c];
                save.GivenMask[idx] = CurrentBoard.GivenMask[r, c] ? 1 : 0;
                save.RegionMap[idx] = CurrentBoard.RegionMap[r, c];
            }

            // Save pencil marks
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var marks = CurrentBoard.GetPencilMarks(r, c);
                if (marks.Count > 0)
                {
                    var pms = new PencilMarkSaveData { Row = r, Col = c };
                    pms.Marks.AddRange(marks);
                    save.PencilMarks.Add(pms);
                }
            }

            return save;
        }

        public bool TryRestorePuzzleSaveState(PuzzleSaveState save)
        {
            if (save == null) return false;

            var size = save.BoardSize;
            var solution = new int[size, size];
            var cells = new int[size, size];
            var regionMap = new int[size, size];

            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var idx = r * size + c;
                solution[r, c] = save.Solution[idx];
                cells[r, c] = save.Board[idx];
                regionMap[r, c] = save.RegionMap[idx];
            }

            CurrentBoard = new SudokuBoard(size, solution, cells, regionMap);

            // Overwrite GivenMask
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var idx = r * size + c;
                if (save.GivenMask[idx] == 1)
                    CurrentBoard.GivenMask[r, c] = true;
            }

            // Restore pencil marks
            for (var i = 0; i < save.PencilMarks.Count; i++)
            {
                var pm = save.PencilMarks[i];
                for (var j = 0; j < pm.Marks.Count; j++)
                    CurrentBoard.AddPencilMark(pm.Row, pm.Col, pm.Marks[j]);
            }

            return true;
        }

        // ── Helpers ──

        private int RollStars(Random rng, int floor, bool isElite)
        {
            var baseStar = 1 + floor;
            if (isElite) baseStar++;
            return Math.Clamp(baseStar + rng.Next(-1, 2), 1, 6);
        }

        private static DifficultyTier MapDifficulty(int floor, int stars)
        {
            var score = floor + stars;
            if (score <= 2) return DifficultyTier.Diff1;
            if (score <= 4) return DifficultyTier.Diff2;
            if (score <= 6) return DifficultyTier.Diff3;
            if (score <= 8) return DifficultyTier.Diff4;
            return DifficultyTier.Diff5;
        }

        private int GetCurrentClassLevel()
        {
            // In a full run, class level would come from MetaProgressionState.
            // For now, return 1 as default. UI layer passes this in from profile.
            return 1;
        }

        private void ClearOverlayCellsFromGivenMask()
        {
            if (CurrentOverlay == null || CurrentBoard == null) return;

            // Remove overlay elements whose cells are all givens (player can't solve them)
            for (var i = CurrentOverlay.Lines.Count - 1; i >= 0; i--)
            {
                var allGiven = true;
                var line = CurrentOverlay.Lines[i];
                for (var j = 0; j < line.Cells.Count; j++)
                {
                    if (!CurrentBoard.GivenMask[line.Cells[j].Row, line.Cells[j].Col])
                    {
                        allGiven = false;
                        break;
                    }
                }
                if (allGiven) CurrentOverlay.Lines.RemoveAt(i);
            }
        }

        // ── Analytics Access ──

        public PostRunAnalyticsService GetAnalytics() => _analytics;
        public int GetTotalRunXp() => XpService.SumRunXp(TileXpLog);
    }

    public enum PlaceResult
    {
        Correct,
        Invalid,
        IsGiven
    }
}
