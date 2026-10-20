using NUnit.Framework;
using SudokuRoguelike.Core;
using Object = UnityEngine.Object;
using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // Gold at the boss gate (design/gold-and-items-concept.md L2, owner pick after the 0.2.0 playtest): strike one trial
    // for 60 gold × floor number (the boss then takes one trial fewer) and lower the intensity one step for 40 gold ×
    // floor number, each once per gate, kept in the save.
    [TestFixture]
    [Requirement("ECON-GATE-001", "ECON-GATE-002")]
    public sealed class BossGateGoldTests
    {
        private static RunDirector StartRun(int floorIndex, int gold, int runNumber = 1)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9120);
            for (var f = 0; f < floorIndex; f++) run.AdvanceToNextFloor();
            run.State.CurrentGold = gold;
            run.State.RunNumber = runNumber;
            return run;
        }

        // [REQ: ECON-GATE-001]
        [Test]
        [Requirement("ECON-GATE-001")]
        public void StrikeATrial_CostsSixtyPerFloor_AndTheBossTakesOneFewer()
        {
            var run = StartRun(floorIndex: 2, gold: 500);
            Assert.AreEqual(180, run.GetBossStrikeCost(), "Floor 3: 60 × 3");
            run.GetBossModifierCounts(out var shown, out var picks);
            Assert.AreEqual(picks, run.GetBossPicksRequired());
            var trial = run.RollBossModifierChoices()[0];

            Assert.IsTrue(run.TryStrikeBossTrial(trial));

            Assert.AreEqual(320, run.State.CurrentGold);
            Assert.AreEqual(picks - 1, run.GetBossPicksRequired(), "one trial fewer for the boss");
            Assert.AreEqual(trial, run.StruckBossTrial);
            Assert.IsFalse(run.CanStrikeBossTrial(), "once per gate");
            Assert.IsFalse(run.TryStrikeBossTrial(trial));
            Assert.AreEqual(320, run.State.CurrentGold, "a refused strike costs nothing");
            Assert.Greater(shown, picks);

            run.AdvanceToNextFloor();
            Assert.IsNull(run.StruckBossTrial, "the next gate starts fresh");
            Assert.IsTrue(run.CanStrikeBossTrial());
        }

        [Test]
        [Requirement("ECON-GATE-001")]
        public void StrikeATrial_NeedsTheGold_AndABossWithTwoTrials()
        {
            Assert.IsFalse(StartRun(floorIndex: 0, gold: 500).CanStrikeBossTrial(), "Floor 1's boss takes one trial");
            var poor = StartRun(floorIndex: 1, gold: 119);
            Assert.AreEqual(120, poor.GetBossStrikeCost());
            Assert.IsFalse(poor.CanStrikeBossTrial(), "119 gold is not enough on Floor 2");
            poor.State.CurrentGold = 120;
            Assert.IsTrue(poor.CanStrikeBossTrial());
        }

        // [REQ: ECON-GATE-002]
        [Test]
        [Requirement("ECON-GATE-002")]
        public void LowerTheIntensity_CostsFortyPerFloor_AndTheBossPuzzleUsesIt()
        {
            var run = StartRun(floorIndex: 2, gold: 500, runNumber: 6);   // High
            Assert.AreEqual(120, run.GetBossCalmCost(), "Floor 3: 40 × 3");
            Assert.AreEqual(BossModifierIntensity.High, run.GetBossIntensity());

            Assert.IsTrue(run.TryLowerBossIntensity());

            Assert.AreEqual(380, run.State.CurrentGold);
            Assert.AreEqual(BossModifierIntensity.Medium, run.GetBossIntensity());
            Assert.AreEqual(BossModifierIntensity.Medium, run.BuildLevelConfig(true).Intensity, "the boss puzzle");
            Assert.AreEqual(BossModifierIntensity.High, run.BuildLevelConfig(false).Intensity, "other puzzles keep it");
            Assert.IsFalse(run.TryLowerBossIntensity(), "one step per gate");
            Assert.IsFalse(StartRun(floorIndex: 2, gold: 500, runNumber: 1).CanLowerBossIntensity(), "Low cannot go lower");
        }

        // Review finding (HIGH): the boss config is built when the floor is entered, before the gate; the paid intensity
        // must still reach the boss puzzle through the map's fixed config.
        [Test]
        [Requirement("ECON-GATE-002")]
        public void LowerTheIntensity_ReachesTheBossConfigBuiltAtFloorEntry()
        {
            var host = new GameObject("BossGateGoldHost");
            try
            {
                var map = host.AddComponent<SudokuRoguelike.UI.RunMapController>();
                var run = StartRun(floorIndex: 2, gold: 500, runNumber: 6);
                map.BindRun(run, new SudokuRoguelike.Save.SaveFileService(SudokuRoguelike.Save.SaveFileService.MaxSlots - 1));
                var boss = run.CurrentFloorGraph.Find(n => n.Type == NodeType.Boss);
                Assert.NotNull(boss);
                Assert.AreEqual(BossModifierIntensity.High, map.GetFixedLevelConfig(boss).Intensity);

                Assert.IsTrue(run.TryLowerBossIntensity());

                Assert.AreEqual(BossModifierIntensity.Medium, map.GetFixedLevelConfig(boss).Intensity,
                    "the boss puzzle is generated with the lowered intensity");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // Review finding: a save written before these fields existed must not read as "Floor 1 already struck/lowered".
        [Test]
        public void OldSaveWithoutGateFields_HasNothingStruckOrLowered()
        {
            var run = StartRun(floorIndex: 0, gold: 500, runNumber: 6);
            var json = JsonUtility.ToJson(run.State);
            foreach (var field in new[] { "BossTrialStruckOnFloor", "BossTrialStruck", "BossIntensityLoweredOnFloor",
                         "WardingFlameOnFloor", "WardingFlameTrial" })
                json = System.Text.RegularExpressions.Regex.Replace(json, "\"" + field + "\":[^,}]*,?", string.Empty);
            var resumed = new RunDirector();
            resumed.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9120);
            resumed.RestoreState(JsonUtility.FromJson<RunState>(json));

            Assert.IsNull(resumed.StruckBossTrial);
            Assert.IsFalse(resumed.BossIntensityLoweredThisFloor);
        }

        // Review finding (MEDIUM, older bug): the trial the Warding Flame removed came back after Save & Quit at the gate.
        [Test]
        public void GateRemovals_DropTheStruckAndTheWardedTrial_AfterResume()
        {
            var run = StartRun(floorIndex: 2, gold: 500);
            var offered = run.RollBossModifierChoices();
            run.State.HeldRelics.Add(new RelicInstance { Id = RelicId.WardingFlame, UsesRemaining = 1 });
            Assert.IsTrue(run.TryUseWardingFlame(offered[0]));
            Assert.IsTrue(run.TryStrikeBossTrial(offered[1]));

            var resumed = new RunDirector();
            resumed.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9120);
            resumed.RestoreState(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State)));
            var shown = resumed.RollBossModifierChoices();
            resumed.ApplyGateRemovals(shown);

            CollectionAssert.DoesNotContain(shown, offered[0], "the warded trial stays gone");
            CollectionAssert.DoesNotContain(shown, offered[1], "the struck trial stays gone");
            Assert.AreEqual(offered.Count - 2, shown.Count);
        }

        // Review finding: a strike never leaves the gate without a trial (fewer offers than expected, or the Warding
        // Flame already used).
        [Test]
        [Requirement("ECON-GATE-001")]
        public void StrikeATrial_NeedsTwoOffersLeft()
        {
            var run = StartRun(floorIndex: 1, gold: 500);
            Assert.IsFalse(run.CanStrikeBossTrial(offeredCount: 1));
            Assert.IsTrue(run.CanStrikeBossTrial(offeredCount: 2));
        }

        [Test]
        public void GateChoices_SurviveSaveAndQuit()
        {
            var run = StartRun(floorIndex: 2, gold: 500, runNumber: 6);
            var trial = run.RollBossModifierChoices()[1];
            run.TryStrikeBossTrial(trial);
            run.TryLowerBossIntensity();

            var resumed = new RunDirector();
            resumed.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9120);
            resumed.RestoreState(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State)));

            Assert.AreEqual(trial, resumed.StruckBossTrial);
            Assert.AreEqual(BossModifierIntensity.Medium, resumed.GetBossIntensity());
            Assert.AreEqual(run.GetBossPicksRequired(), resumed.GetBossPicksRequired());
        }
    }
}
