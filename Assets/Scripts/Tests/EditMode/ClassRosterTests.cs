using System.Text.RegularExpressions;
using NUnit.Framework;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-CLASS-100..103] SPEC-CONF-008 (decided: ClassCatalog is the source, documented in
    // ClassXpProgressionSystem) and GAP-023: base stats, level schedule and the "base (+bonus)" display.
    // Promoted from review diagnostic D07.
    [TestFixture]
    [Requirement("REQ-CLASS-100", "REQ-CLASS-101", "REQ-CLASS-103")]
    public sealed class ClassRosterTests
    {
        [TestCase(ClassId.NumberFreak, 10, 10, 2, 1)]
        [TestCase(ClassId.GardenMonk, 14, 5, 1, 1)]
        [TestCase(ClassId.ShrineArchivist, 8, 15, 2, 1)]
        [TestCase(ClassId.KoiGambler, 9, 8, 2, 2)]
        [TestCase(ClassId.StoneGardener, 11, 8, 3, 1)]
        [TestCase(ClassId.LanternSeer, 7, 12, 2, 1)]
        [TestCase(ClassId.ReedDuelist, 9, 9, 2, 1)]
        [TestCase(ClassId.QuietCartographer, 12, 6, 1, 1)]
        public void BaseStats_MatchTheDocumentedRoster(ClassId id, int hp, int pencil, int slots, int relics)
        {
            var def = ClassCatalog.GetDefinition(id);
            Assert.AreEqual(hp, def.BaseHP, "HP");
            Assert.AreEqual(pencil, def.BasePencil, "Pencil");
            Assert.AreEqual(slots, def.BaseItemSlots, "Slots");
            Assert.AreEqual(relics, def.BaseStartingRelics, "Starting relics");
        }

        [Test]
        [Requirement("REQ-CLASS-102")]
        public void EveryStatBonusInTheSchedule_IsApplied()
        {
            // "+ 1 Pencil" (Number Freak L3) was never granted because the parser expects "+1 Pencil" (D07).
            var anyPlus = new Regex(@"\+\s*\d+\s+[A-Za-z]");
            var parsed = new Regex(@"\+(\d+)\s+(HP|Pencil|Starting Gold|Item Slot|Reroll Token|Starting Relic)\b");
            foreach (var def in ClassCatalog.GetAll())
            foreach (var (level, text) in ClassCatalog.GetAllUnlocks(def.Id))
                if (!text.StartsWith("Passive") && !text.StartsWith("Start:") && !text.StartsWith("Cosmetic"))
                    Assert.AreEqual(anyPlus.Matches(text).Count, parsed.Matches(text).Count,
                        $"{def.Id} L{level}: '{text}' has a bonus the run start does not apply");
        }

        [Test]
        public void NumberFreakLevel10_StartsWith14Hp13Pencil()
        {
            var state = RunArchetypeService.CreateRunState(ClassId.NumberFreak, 1, classLevel: 10);
            Assert.AreEqual(14, state.MaxHP);
            Assert.AreEqual(13, state.MaxPencil);
        }

        [Test]
        public void ClassScreen_ShowsBaseAndLevelBonus()
        {
            var def = ClassCatalog.GetDefinition(ClassId.NumberFreak);
            var line = MainMenuClassInfoPresenter.BuildStatsLine(def, 10);
            StringAssert.Contains("10 (+4)", line, "HP 10 base, +2 at L7 and L10");
            StringAssert.Contains("10 (+3)", line, "Pencil 10 base, +1 at L3, +2 at L10");

            var level1 = MainMenuClassInfoPresenter.BuildStatsLine(def, 1);
            Assert.That(level1, Does.Not.Contain("(+"), "no bonus at level 1");
        }
    }
}
