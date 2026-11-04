using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;
using Cue = SudokuRoguelike.UI.RunSoundCues.Cue;

namespace SudokuRoguelike.Tests
{
    // [REQ: AUDIO-SFX-001] The run's sounds read from the run state (GAP-073 round 2).
    [TestFixture]
    [Requirement("AUDIO-SFX-001")]
    public sealed class RunSoundCuesTests
    {
        private static RunState Run() => new RunState
        {
            CurrentHP = 10, MaxHP = 12, CurrentPencil = 3, CurrentGold = 50, ComboStreak = 4, CurrentFloor = 0,
            HeldRelics = new List<RelicInstance>()
        };

        [Test]
        public void TheFirstLook_OnlyRecords_ThenEachChangeHasItsCue()
        {
            var cues = new RunSoundCues();
            var s = Run();
            var fog = new ModifierOverlayData();
            fog.FogCells.Add(new CellCoord(0, 0));
            fog.FogCells.Add(new CellCoord(0, 1));
            CollectionAssert.IsEmpty(cues.Tick(s, fog), "a new run or a loaded save makes no sound");

            s.ItemsUsedThisRun++;
            s.HeldRelics.Add(new RelicInstance());
            s.CurrentPencil = 0;
            s.CurrentHP = 3;
            s.ComboStreak = 5;
            s.CurrentGold = 60;
            s.CurrentFloor = 1;
            fog.FogCells.RemoveAt(0);
            CollectionAssert.AreEquivalent(new[] { Cue.ItemUse, Cue.RelicPickup, Cue.PencilDepleted, Cue.HpCritical, Cue.Combo,
                Cue.GoldGain, Cue.FogReveal, Cue.FloorTransition }, cues.Tick(s, fog));
            CollectionAssert.IsEmpty(cues.Tick(s, fog), "nothing changed");
        }

        [Test]
        public void QuietChanges_MakeNoSound()
        {
            var cues = new RunSoundCues();
            var s = Run();
            cues.Tick(s, null);
            s.CurrentGold = 20;     // spent
            s.CurrentHP = 0;        // the run's end has its own sound
            s.ComboStreak = 6;      // not a milestone
            s.CurrentPencil = 2;
            CollectionAssert.IsEmpty(cues.Tick(s, new ModifierOverlayData()), "a new board's fog is not a reveal either");

            var other = Run();
            other.CurrentGold = 999;
            CollectionAssert.IsEmpty(cues.Tick(other, null), "another run starts fresh");
        }

        [Test]
        public void ComboBreak_OnlyAfterAStreakOfFive()
        {
            var cues = new RunSoundCues();
            var s = Run();
            s.ComboStreak = 7;
            cues.Tick(s, null);
            s.ComboStreak = 0;
            CollectionAssert.AreEqual(new[] { Cue.ComboBreak }, cues.Tick(s, null));
            s.ComboStreak = 4;
            cues.Tick(s, null);
            s.ComboStreak = 0;
            CollectionAssert.IsEmpty(cues.Tick(s, null), "a streak under five breaks quietly");
            s.ComboStreak = 10;
            CollectionAssert.AreEqual(new[] { Cue.Combo }, cues.Tick(s, null));
        }
    }
}
