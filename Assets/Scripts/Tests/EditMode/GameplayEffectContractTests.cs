using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class GameplayEffectContractTests
    {
        [Test]
        public void GoldenKoi_UseActivatesStarScaledCompletionBonusAndConsumesFlag()
        {
            var run = StartPuzzleRun(93001);
            var stars = run.CurrentLevelConfig.Stars;
            var expectedGold = GoldTable.CalculatePuzzleGold(run.CurrentLevelConfig.BoardSize, stars) + stars * 10;

            run.State.HeldItems.Add(BuildItem(ItemType.GoldenKoi, ItemRarity.Rare));

            Assert.IsTrue(run.TryUseItem(0));
            Assert.IsTrue(run.State.GoldenKoiActive);
            Assert.AreEqual(0, run.State.HeldItems.Count);

            SolveCurrentBoard(run);
            var goldBeforeCompletion = run.State.CurrentGold;
            run.CompleteLevelAndGrantRewards();

            Assert.AreEqual(goldBeforeCompletion + expectedGold, run.State.CurrentGold);
            Assert.IsFalse(run.State.GoldenKoiActive);
        }

        [Test]
        public void TempleStamp_UseLocksHpUntilPuzzleCompletion()
        {
            var run = StartPuzzleRun(93002);
            run.State.HeldItems.Add(BuildItem(ItemType.TempleStamp, ItemRarity.Rare));

            Assert.IsTrue(run.TryUseItem(0));
            Assert.IsTrue(run.State.TempleSealActive);
            Assert.AreEqual(0, run.State.HeldItems.Count);

            var hpBeforeMistake = run.State.CurrentHP;
            run.ApplyMistakePenalty(2);

            Assert.AreEqual(hpBeforeMistake, run.State.CurrentHP);
            Assert.AreEqual(0, run.State.LastMistakeHpLost);

            SolveCurrentBoard(run);
            run.CompleteLevelAndGrantRewards();

            Assert.IsFalse(run.State.TempleSealActive);
        }

        [Test]
        public void GinkgoLeaf_RestoresLastMistakeHpComboAndClearsWrongCell()
        {
            var run = StartPuzzleRun(93003);
            var (row, col, wrongValue) = FindInvalidPlacement(run);
            run.State.ComboStreak = 4;
            var hpBeforeMistake = run.State.CurrentHP;

            Assert.AreEqual(PlaceResult.Invalid, run.PlaceNumber(row, col, wrongValue));
            run.ApplyMistakePenalty(row, col);

            Assert.AreEqual(wrongValue, run.CurrentBoard.Cells[row, col]);
            Assert.AreEqual(hpBeforeMistake - 1, run.State.CurrentHP);
            Assert.AreEqual(1, run.State.LastMistakeHpLost);
            Assert.AreEqual(4, run.State.LastComboBeforeMistake);
            Assert.AreEqual(0, run.State.ComboStreak);
            Assert.AreEqual(1, run.CurrentLevelState.Mistakes);
            Assert.AreEqual(1, run.CurrentLevelState.HpLost);
            Assert.IsFalse(run.CurrentLevelState.PerfectSoFar);

            var effect = run.ApplyItemEffect(BuildItem(ItemType.GinkgoLeaf, ItemRarity.Rare));

            Assert.AreEqual(ItemEffectResult.ResultKind.BoardChanged, effect.Kind);
            Assert.AreEqual(0, run.CurrentBoard.Cells[row, col]);
            Assert.AreEqual(hpBeforeMistake, run.State.CurrentHP);
            Assert.AreEqual(4, run.State.ComboStreak);
            Assert.AreEqual(0, run.CurrentLevelState.Mistakes);
            Assert.AreEqual(0, run.CurrentLevelState.HpLost);
            Assert.IsTrue(run.CurrentLevelState.PerfectSoFar);
            Assert.AreEqual(-1, run.State.LastMistakeRow);
            Assert.AreEqual(-1, run.State.LastMistakeCol);
        }

        [Test]
        public void ClassExclusiveItemAvailability_RequiresMatchingClassAndUnlockLevel()
        {
            Assert.IsTrue(ItemService.IsClassExclusive(ItemType.LoadedCoin));
            Assert.AreEqual(ClassId.NumberFreak, ItemService.GetExclusiveClass(ItemType.LoadedCoin));
            Assert.AreEqual(15, ItemService.GetExclusiveUnlockLevel(ItemType.LoadedCoin));

            Assert.IsFalse(ItemService.IsAvailableForClass(ItemType.LoadedCoin, ClassId.NumberFreak, 14));
            Assert.IsFalse(ItemService.IsAvailableForClass(ItemType.LoadedCoin, ClassId.GardenMonk, 15));
            Assert.IsTrue(ItemService.IsAvailableForClass(ItemType.LoadedCoin, ClassId.NumberFreak, 15));
        }

        [Test]
        public void PuzzleCompleteRelicEffects_MatchRepresentativeRewardDescriptions()
        {
            var level = new LevelState { PerfectSoFar = true, Mistakes = 2, NoPencilUsed = true };

            AssertPuzzleCompleteGold(RelicId.CopperTortoise, level, 100, 115);
            AssertPuzzleCompleteGold(RelicId.TransmutedSigil, level, 100, 125);
            AssertPuzzleCompleteGold(RelicId.LoadBearingStone, level, 100, 110);
        }

        [Test]
        public void StartingWoodenComb_IncreasesMaxHpImmediately()
        {
            var state = new RunState
            {
                CurrentHP = 10,
                MaxHP = 10,
                HeldRelics = new List<RelicInstance>()
            };

            new RelicService(seed: 0).AssignStartingRelics(state, count: 1);

            Assert.AreEqual(RelicId.WoodenComb, state.HeldRelics[0].Id);
            Assert.AreEqual(11, state.MaxHP);
            Assert.AreEqual(11, state.CurrentHP);
        }

        [Test]
        public void RelicChoices_DoNotRepeatItemsWithinOnePanel()
        {
            var choices = new RelicService(seed: 44123).RollRelicChoices(floorIndex: 0, count: 40);

            Assert.GreaterOrEqual(choices.Count, 3);
            Assert.AreEqual(
                choices.Count,
                choices.Select(choice => choice.Id).Distinct().Count(),
                "One relic node must not display the same relic more than once.");
        }

        [Test]
        public void StartingRelics_AllowDuplicateCopies()
        {
            var state = new RunState
            {
                CurrentHP = 10,
                MaxHP = 10,
                HeldRelics = new List<RelicInstance>()
            };

            new RelicService(seed: 44124).AssignStartingRelics(state, count: 5);

            Assert.AreEqual(5, state.HeldRelics.Count);
            Assert.IsTrue(
                state.HeldRelics.GroupBy(relic => relic.Id).Any(group => group.Count() > 1),
                "Starting relic assignment should allow duplicate copies when more relics are awarded than the tier pool contains.");
        }

        [Test]
        public void GardenMonk_Level1HealsOnFifthCorrectPlacement()
        {
            var run = StartPuzzleRun(93004, ClassId.GardenMonk, classLevel: 1);
            ClearPlacementHealInterference(run);
            run.State.MaxHP = 20;
            run.State.CurrentHP = 19;

            Assert.AreEqual(5, ClassPassiveService.GetGardenMonkHealInterval(run.State));

            PlaceCorrectNumbers(run, 4);
            Assert.AreEqual(19, run.State.CurrentHP);

            PlaceCorrectNumbers(run, 1);
            Assert.AreEqual(20, run.State.CurrentHP);
        }

        [Test]
        public void GardenMonk_Level24HealsOnFourthCorrectPlacementFromClassLevel()
        {
            var run = StartPuzzleRun(93005, ClassId.GardenMonk, classLevel: 24);
            ClearPlacementHealInterference(run);
            run.State.RunNumber = 0;
            run.State.MaxHP = 20;
            run.State.CurrentHP = 19;

            Assert.AreEqual(4, ClassPassiveService.GetGardenMonkHealInterval(run.State));

            PlaceCorrectNumbers(run, 3);
            Assert.AreEqual(19, run.State.CurrentHP);

            PlaceCorrectNumbers(run, 1);
            Assert.AreEqual(20, run.State.CurrentHP);
        }

        [Test]
        public void GardenMonk_PassiveHealingCapsAtMaxHp()
        {
            var run = StartPuzzleRun(93006, ClassId.GardenMonk, classLevel: 1);
            ClearPlacementHealInterference(run);
            run.State.MaxHP = 20;
            run.State.CurrentHP = 20;

            PlaceCorrectNumbers(run, 5);

            Assert.AreEqual(20, run.State.CurrentHP);
        }

        [Test]
        public void NonGardenMonk_DoesNotReceiveCorrectPlacementHeal()
        {
            var run = StartPuzzleRun(93007, ClassId.NumberFreak, classLevel: 24);
            ClearPlacementHealInterference(run);
            run.State.MaxHP = 20;
            run.State.CurrentHP = 19;

            PlaceCorrectNumbers(run, 5);

            Assert.AreEqual(19, run.State.CurrentHP);
        }

        [Test]
        public void ItemUse_IncrementsPuzzleAndRunCountersWithoutDailyGoalState()
        {
            var run = StartPuzzleRun(93008);
            run.DailyGoals = null;
            run.State.HeldItems.Add(BuildItem(ItemType.InkWell, ItemRarity.Normal));

            Assert.IsTrue(run.TryUseItem(run.State.HeldItems.Count - 1));

            Assert.AreEqual(1, run.CurrentLevelState.ItemsUsedThisLevel);
            Assert.AreEqual(1, run.State.ItemsUsedThisRun);
            Assert.AreEqual(1, run.GetAnalytics().TotalItemsUsed);
        }

        [Test]
        public void BossClearedStats_UseCurrentLevelCounters()
        {
            var run = StartPuzzleRun(93009);
            run.CurrentLevelState.Mistakes = 2;
            run.CurrentLevelState.HpLost = 3;
            run.CurrentLevelState.ItemsUsedThisLevel = 1;
            run.CurrentLevelState.PencilMarksUsed = 4;
            run.State.CurrentHP = run.State.HpAtFloorStart;

            var stats = EndScreenViewController.BuildBossClearedStats(run);

            Assert.AreEqual(2, stats.Mistakes);
            Assert.AreEqual(3, stats.HpLost);
            Assert.AreEqual(1, stats.ItemsUsed);
            Assert.AreEqual(4, stats.PencilMarks);
        }

        private static RunDirector StartPuzzleRun(int seed, ClassId classId = ClassId.NumberFreak,
            int classLevel = 1)
        {
            var run = new RunDirector();
            run.StartRun(
                new LaunchRequest
                {
                    ClassId = classId,
                    Mode = GameMode.GardenRun,
                    AllowIrregularPuzzles = false,
                    ClassLevel = classLevel
                },
                seed);
            run.State.HasPositiveFloorEffect = false;

            var puzzleNode = run.GetReachableNodes()
                .Select(index => run.CurrentFloorGraph[index])
                .First(node => RequiresPuzzle(node.Type));

            Assert.IsTrue(run.TryAdvanceToNode(puzzleNode.Index));
            var config = run.BuildLevelConfig(
                puzzleNode.Type == NodeType.Boss,
                puzzleNode.Type == NodeType.ElitePuzzle,
                puzzleNode.Index,
                puzzleNode.Type == NodeType.PreBoss);
            run.StartLevel(config);

            Assert.NotNull(run.CurrentBoard);
            return run;
        }

        private static void ClearPlacementHealInterference(RunDirector run)
        {
            run.State.HeldRelics.Clear();
            run.State.HasRelic = false;
            run.State.HeldRelic = null;
            run.State.MonksBeadsCountdown = 0;
        }

        private static bool RequiresPuzzle(NodeType type)
        {
            return type == NodeType.Puzzle
                || type == NodeType.ElitePuzzle
                || type == NodeType.PreBoss
                || type == NodeType.Boss;
        }

        private static ItemInstance BuildItem(ItemType type, ItemRarity rarity)
        {
            return new ItemInstance
            {
                Id = $"test-{type}",
                Type = type,
                Rarity = rarity,
                Charges = ItemService.GetDefaultCharges(type)
            };
        }

        private static (int row, int col, int value) FindInvalidPlacement(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var row = 0; row < board.Size; row++)
            for (var col = 0; col < board.Size; col++)
            {
                if (board.Cells[row, col] != 0 || board.IsGiven(row, col))
                    continue;

                for (var value = 1; value <= board.Size; value++)
                {
                    if (value == board.Solution[row, col])
                        continue;
                    if (!run.ConstraintEngine.ValidateAll(board, row, col, value, run.CurrentOverlay))
                        return (row, col, value);
                }
            }

            Assert.Fail("No invalid placement candidate was available for the generated puzzle.");
            return (-1, -1, -1);
        }

        private static void SolveCurrentBoard(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var row = 0; row < board.Size; row++)
            for (var col = 0; col < board.Size; col++)
            {
                if (board.Cells[row, col] != 0 || board.IsGiven(row, col))
                    continue;

                var result = run.PlaceNumber(row, col, board.Solution[row, col]);
                Assert.AreEqual(PlaceResult.Correct, result, $"Expected correct placement at {row},{col}.");
            }
        }

        private static void PlaceCorrectNumbers(RunDirector run, int count)
        {
            var board = run.CurrentBoard;
            var placed = 0;
            for (var row = 0; row < board.Size; row++)
            for (var col = 0; col < board.Size; col++)
            {
                if (board.Cells[row, col] != 0 || board.IsGiven(row, col))
                    continue;

                var result = run.PlaceNumber(row, col, board.Solution[row, col]);
                Assert.AreEqual(PlaceResult.Correct, result, $"Expected correct placement at {row},{col}.");

                placed++;
                if (placed == count)
                    return;
            }

            Assert.Fail($"Expected at least {count} open cells for manual correct placements.");
        }

        private static void AssertPuzzleCompleteGold(RelicId id, LevelState level, int startingGold, int expectedGold)
        {
            var state = new RunState
            {
                CurrentHP = 3,
                MaxHP = 5,
                HeldRelics = new List<RelicInstance>
                {
                    new RelicInstance { Id = id, Tier = RelicService.GetBaseTier(id), UsesRemaining = -1 }
                }
            };
            var gold = startingGold;

            RelicService.OnPuzzleComplete(state, level, ref gold);

            Assert.AreEqual(expectedGold, gold, id.ToString());
        }
    }
}
