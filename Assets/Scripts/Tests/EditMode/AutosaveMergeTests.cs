using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.Tests
{
    // [REQ: SAVE-AUTO-001] [REQ: META-CODEX-001] [REQ: META-CODEX-002] GAP-037: other services write the save during
    // a run (codex discoveries, options). The next autosave must keep their changes instead of writing an older copy.
    [TestFixture]
    [Requirement("SAVE-AUTO-001", "META-CODEX-001", "META-CODEX-002")]
    public sealed class AutosaveMergeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        [SetUp]
        [TearDown]
        public void CleanSlot()
        {
            SaveFileService.FlushSharedPendingWrites();
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }

        [Test]
        public void Autosave_KeepsCodexDiscoveriesAndOptionChangesMadeDuringTheRun()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, 9901);
            var autosave = new RunAutoSaveCoordinator(save);
            autosave.Bind(run);
            autosave.SaveBound();

            // During the run: claim an item, accept a relic, change an option (as OptionsController.SaveOptions does).
            new ProfileService(new SaveFileService(TestSlot)).RecordItemDiscovery(ItemType.GardenLantern);
            new ProfileService(new SaveFileService(TestSlot)).RecordRelicDiscovery(RelicId.CopperTortoise);
            var optionsWriter = new SaveFileService(TestSlot);
            var withOptions = optionsWriter.Load();
            withOptions.PlayerProfile.Options.Audio.MusicVolume = 0.123f;
            optionsWriter.Save(withOptions);

            run.State.CurrentHP -= 1;
            autosave.SaveBound();
            SaveFileService.FlushSharedPendingWrites();

            var loaded = new SaveFileService(TestSlot).Load();
            Assert.IsTrue(loaded.MetaProgress.ItemCodex.Entries.Any(e => e.ItemId == "GardenLantern" && e.Discovered), "item codex");
            CollectionAssert.Contains(loaded.MetaProgress.DiscoveredRelics, RelicId.CopperTortoise, "relic codex");
            Assert.AreEqual(0.123f, loaded.PlayerProfile.Options.Audio.MusicVolume, 0.0001f, "options");
            Assert.AreEqual(run.State.CurrentHP, loaded.ActiveRunState.CurrentHP, "the autosave itself was written");
        }
    }
}
