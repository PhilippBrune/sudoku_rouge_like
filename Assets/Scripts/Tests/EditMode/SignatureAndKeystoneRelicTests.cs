using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // ItemsAndRelicsSystem 2.4: a signature relic per class (RELIC-SIG-001/002) and four keystones after the first boss
    // (RELIC-KEY-001/002), relics that bend a rule of the puzzle.
    [TestFixture]
    public sealed class SignatureAndKeystoneRelicTests
    {
        private static RunState StateWith(params RelicId[] relics) => new RunState
        {
            MaxHP = 10, CurrentHP = 10, MaxPencil = 10, CurrentPencil = 5, ItemSlots = 3,
            HeldRelics = relics.Select(id => new RelicInstance { Id = id, UsesRemaining = -1 }).ToList()
        };

        // A 4×4 board with 2×2 boxes; the solution 1234/3412/2143/4321, all cells empty.
        private static SudokuBoard Board()
        {
            var solution = new[,] { { 1, 2, 3, 4 }, { 3, 4, 1, 2 }, { 2, 1, 4, 3 }, { 4, 3, 2, 1 } };
            var regions = new int[4, 4];
            for (var r = 0; r < 4; r++)
                for (var c = 0; c < 4; c++) regions[r, c] = r / 2 * 2 + c / 2;
            return new SudokuBoard(4, solution, new int[4, 4], regions);
        }

        [Test]
        [Requirement("RELIC-SIG-001")]
        public void EveryClass_HasOneSignatureRelic_AndItIsOfferedOnlyToThatClass_AndNotWhenHeld()
        {
            Assert.AreEqual(8, RelicService.SignatureRelics.Count);
            Assert.AreEqual(8, RelicService.SignatureRelics.Values.Distinct().Count());

            int offered = 0, wrongClass = 0, twoClassRelics = 0;
            for (var seed = 0; seed < 200; seed++)
            {
                var choices = new RelicService(seed).RollRelicChoices(1, 3, 0, ClassId.ReedDuelist, 30);
                if (choices.Any(c => c.Id == RelicId.SingleStroke)) offered++;
                if (choices.Any(c => RelicService.IsSignature(c.Id) && c.Id != RelicId.SingleStroke)) wrongClass++;
                if (choices.Count(c => RelicService.IsSignature(c.Id) || RelicService.IsRelicExclusive(c.Id)) > 1) twoClassRelics++;
                Assert.AreEqual(3, choices.Select(c => c.Id).Distinct().Count(), "three different relics");
            }
            Assert.That(offered, Is.InRange(50, 95), "about 35 % of offerings");
            Assert.AreEqual(0, wrongClass, "another class's signature never shows");
            Assert.AreEqual(0, twoClassRelics, "at most one class relic per offering");

            var held = new List<RelicInstance> { new RelicInstance { Id = RelicId.SingleStroke } };
            for (var seed = 0; seed < 100; seed++)
                Assert.IsFalse(new RelicService(seed).RollRelicChoices(1, 3, 0, ClassId.ReedDuelist, 1, held: held)
                    .Any(c => c.Id == RelicId.SingleStroke), "not when held");
        }

        [Test]
        [Requirement("RELIC-KEY-001")]
        public void TheKeystoneOffering_IsThreeDifferentKeystones()
        {
            var choices = new RelicService(5).RollKeystoneChoices();
            Assert.AreEqual(3, choices.Select(c => c.Id).Distinct().Count());
            Assert.IsTrue(choices.All(c => RelicService.IsKeystone(c.Id) && c.Tier == RelicTier.Keystone));
            Assert.AreEqual(RelicTier.Keystone, RelicService.GetBaseTier(RelicId.CageWhisper));
            Assert.AreEqual(RelicTier.Tier2, RelicService.GetBaseTier(RelicId.InkStone));
        }

        [Test]
        [Requirement("RELIC-KEY-001")]
        public void AfterTheFirstBoss_TheTableOffersKeystones_LaterRelics()
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9133);
            Assert.IsTrue(run.RollBossOfferings().All(c => RelicService.IsKeystone(c.Id)), "Floor 1");
            run.State.CurrentFloor = 1;
            Assert.IsFalse(run.RollBossOfferings().Any(c => RelicService.IsKeystone(c.Id)), "Floor 2");
        }

        [Test]
        [Requirement("RELIC-SIG-002", "RELIC-KEY-002")]
        public void SteadyHandAndPrayerBeads_ShieldThePuzzlesFirstMistake_Once()
        {
            var steady = StateWith(RelicId.SteadyHand);
            Assert.IsTrue(RelicService.TryFirstMistakeShield(steady));
            Assert.IsFalse(RelicService.TryFirstMistakeShield(steady), "once per puzzle");
            RelicService.OnPuzzleStart(steady, Board());
            Assert.IsTrue(RelicService.TryFirstMistakeShield(steady), "again in the next puzzle");

            var beads = StateWith(RelicId.PrayerBeads);
            beads.CurrentHP = 9;
            Assert.IsFalse(RelicService.TryFirstMistakeShield(beads), "Prayer Beads only at full HP");
            beads.CurrentHP = 10;
            Assert.IsTrue(RelicService.TryFirstMistakeShield(beads));
        }

        [Test]
        [Requirement("RELIC-SIG-002")]
        public void SingleStroke_TakesTheFirstWrongDigitBack_OnlyWithoutPencilMarks()
        {
            var state = StateWith(RelicId.SingleStroke);
            var board = Board();
            board.TogglePencilMark(0, 0, 1);
            Assert.IsFalse(RelicService.TrySingleStroke(state, board), "a pencil mark on the board");
            board.TogglePencilMark(0, 0, 1);
            Assert.IsTrue(RelicService.TrySingleStroke(state, board));
            Assert.IsFalse(RelicService.TrySingleStroke(state, board), "once per puzzle");
        }

        [Test]
        [Requirement("RELIC-SIG-002", "RELIC-KEY-002")]
        public void CountingBeadsAndGildedBox_PayForAFinishedDigitAndBox()
        {
            var state = StateWith(RelicId.CountingBeads, RelicId.GildedBox);
            var board = Board();
            var gold = state.CurrentGold;
            foreach (var (r, c) in new[] { (0, 0), (1, 2), (2, 1) }) board.PlaceValue(r, c, 1);
            RelicService.OnDigitPlaced(state, board, 2, 1, 1);
            Assert.AreEqual(5, state.CurrentPencil, "three of four 1s: nothing yet");
            board.PlaceValue(3, 3, 1);
            RelicService.OnDigitPlaced(state, board, 3, 3, 1);
            Assert.AreEqual(6, state.CurrentPencil, "the last 1: +1 Pencil");

            board.PlaceValue(0, 1, 2);
            board.PlaceValue(1, 0, 3);
            board.PlaceValue(1, 1, 4);
            RelicService.OnDigitPlaced(state, board, 1, 1, 4);
            Assert.AreEqual(gold + 1, state.CurrentGold, "the top-left box is full: +1 gold");
        }

        [Test]
        [Requirement("RELIC-SIG-002", "RELIC-KEY-002")]
        public void LuckyScaleMossPouchCompassNeedleOpenMargin()
        {
            var state = StateWith(RelicId.LuckyScale, RelicId.CompassNeedle, RelicId.OpenMargin);
            var gold = 20;
            RelicService.OnPuzzleComplete(state, new LevelState { Mistakes = 1 }, ref gold);
            Assert.AreEqual(40, gold, "exactly one mistake: double gold");
            gold = 20;
            RelicService.OnPuzzleComplete(state, new LevelState { Mistakes = 2 }, ref gold);
            Assert.AreEqual(20, gold);

            RelicService.ApplyPickupPassives(state, new RelicInstance { Id = RelicId.MossPouch });
            Assert.AreEqual(4, state.ItemSlots, "Moss Pouch: +1 bag slot");

            RelicService.OnFloorStart(state);
            Assert.IsTrue(state.AllNodesRevealed, "Compass Needle: every tile's size and stars");

            Assert.IsTrue(RelicService.PencilFreeOnBoard(state, 6));
            Assert.IsFalse(RelicService.PencilFreeOnBoard(state, 7));
        }

        [Test]
        [Requirement("RELIC-KEY-002")]
        public void CageWhisper_ShowsTheLastDigitOfAHiddenSum()
        {
            var cage = new KillerCage { CageType = KillerCageType.HiddenSum, Sum = 17 };
            try
            {
                BoardViewController.WhisperHiddenSums = false;
                Assert.AreEqual("?", BoardViewController.CageLabel(cage));
                BoardViewController.WhisperHiddenSums = true;
                Assert.AreEqual("?7", BoardViewController.CageLabel(cage));
            }
            finally
            {
                BoardViewController.WhisperHiddenSums = false;
            }
        }

        [Test]
        public void EveryNewRelic_HasANameDescriptionAndIcon()
        {
            foreach (var id in RelicService.SignatureRelics.Values.Concat(RelicService.Keystones))
            {
                Assert.AreNotEqual(id.ToString(), RelicService.GetRelicName(id), id + " name");
                Assert.IsNotEmpty(RelicService.GetRelicDescription(id), id + " description");
                // The game falls back to the whole texture when no sprite is cut (InRunUiFactory.LoadResourceSprite).
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Texture2D>(RelicService.GetIconFolder(id) + "/icon_" + RelicService.GetIconName(id)),
                    id + " icon");
            }
        }
    }
}
