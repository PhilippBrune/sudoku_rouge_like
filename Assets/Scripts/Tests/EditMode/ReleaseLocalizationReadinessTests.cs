using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class ReleaseLocalizationReadinessTests
    {
        private const string GeneratedUiLocalizationCoveragePath = "Assets/Scripts/Tests/EditMode/GeneratedUiLocalizationCoverageTests.cs";

        [TearDown]
        public void TearDown()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            LocalizationService.ResetDiagnostics();
        }

        [Test]
        public void ExternalCatalogs_ArePresentLoadedAndDiagnosticsFree()
        {
            AssertCatalogFileExists("en");
            AssertCatalogFileExists("de");

            Assert.GreaterOrEqual(
                LocalizationService.GetExternalCatalogEntryCount(LanguageOption.English),
                300,
                "English external localization catalog should remain populated for release-facing text.");
            Assert.GreaterOrEqual(
                LocalizationService.GetExternalCatalogEntryCount(LanguageOption.German),
                850,
                "German external localization catalog should remain populated for required German support.");

            CollectionAssert.IsEmpty(
                LocalizationService.ExternalCatalogDiagnostics,
                "External localization catalogs must load cleanly without falling back to code-only strings.");
        }

        [Test]
        public void EnglishCatalogKeys_HaveGermanTranslations()
        {
            var englishKeys = ReadCatalogKeys("en").ToList();
            var missing = englishKeys
                .Where(key => !LocalizationService.HasGermanTranslation(key))
                .OrderBy(key => key)
                .ToList();

            Assert.GreaterOrEqual(
                englishKeys.Count,
                300,
                "English catalog key count is unexpectedly low; release localization coverage may have regressed.");
            CollectionAssert.IsEmpty(
                missing,
                "Every English external catalog key must have a German translation for release readiness.");
        }

        [Test]
        public void ReleaseFacingContentProviders_HaveGermanTranslations()
        {
            var keys = BuildReleaseFacingContentKeys();
            var missing = LocalizationService.FindMissingGermanTranslations(keys);

            Assert.GreaterOrEqual(
                keys.Count,
                150,
                "Release-facing content provider coverage is unexpectedly low.");
            CollectionAssert.IsEmpty(
                missing,
                "Gameplay, narrative, credits, curse, challenge, item, relic, and boss content must have German translations.");
        }

        [Test]
        public void GameplayContentServices_ReturnGermanWithoutMissingDiagnostics()
        {
            LocalizationService.SetLanguage(LanguageOption.German);
            LocalizationService.ResetDiagnostics();

            Assert.AreEqual("L\u00f6ser", ItemService.GetItemName(ItemType.Solver));
            Assert.AreEqual("Bambuskompass", ItemService.GetItemName(ItemType.BambooCompass));
            StringAssert.Contains("Bleistiftmarkierungen", ItemService.GetItemDescription(ItemType.InkWell, ItemRarity.Normal));
            StringAssert.Contains("Bossmodifikator", RelicService.GetRelicDescription(RelicId.LanternOfVoid));
            Assert.AreEqual("Reihenl\u00f6schung", BossService.GetModifierName(BossModifierId.RowWipe));
            StringAssert.Contains("gr\u00fcnen Linien", BossService.GetModifierDescription(BossModifierId.GermanWhispers));
            StringAssert.Contains("Bambustor", ParkNarrativeService.GetFloorEntryFlavor(0));
            StringAssert.Contains(
                "Ginkgoblatt",
                LocalizationService.Format(
                    "Item.Effect.GinkgoLeaf.Restored",
                    "Ginkgo Leaf: mistake undone, +{0} HP, combo restored.",
                    2));
            Assert.AreEqual(
                "Nichts zum R\u00fcckg\u00e4ngigmachen.",
                LocalizationService.T("Item.Effect.GinkgoLeaf.NothingToUndo", "Nothing to undo."));
            StringAssert.Contains("Datenschutz", LocalizationService.T("Credits.Role.PrivacyAnalyticsNotice"));
            StringAssert.Contains("Roboto", LocalizationService.T("Credits.Body.Font"));
            StringAssert.Contains("docs/legal/privacy-analytics-notice.md", LocalizationService.T("Credits.Body.PrivacyAnalyticsNotice"));

            CollectionAssert.IsEmpty(
                LocalizationService.MissingGermanKeys,
                "Release-facing service lookups must not fall back to English while German is active.");
        }

        [Test]
        public void EndScreenPresenter_ReturnsGermanWithoutMissingDiagnostics()
        {
            LocalizationService.SetLanguage(LanguageOption.German);
            LocalizationService.ResetDiagnostics();

            var presenter = new EndScreenPresenter();
            var result = new RunResult
            {
                Victory = true,
                GardenDepthReached = 3,
                GoldEarned = 120,
                XpEarned = 450,
                MistakesMade = 2,
                SecondsPlayed = 95,
                BossPhaseReached = 2,
                PerfectPuzzleCount = 1,
                CursedPuzzlesAccepted = 1,
                RunScore = 700,
                Analytics = new PostRunAnalytics
                {
                    HardestPuzzleStars = 4,
                    HardestPuzzleModifier = BossModifierId.RowWipe,
                    HardestPuzzleTier = PuzzleDifficultyTier.Tier2,
                    TotalMistakes = 2,
                    HighestSinglePuzzleMistakes = 1
                }
            };
            result.TileXpEntries.Add(new TileXpEntry
            {
                BoardSize = 9,
                Stars = 4,
                IsBoss = true,
                TotalXp = 450,
                PerfectBonus = 25,
                ModifierBonus = 15
            });

            StringAssert.Contains("Tiefe 3", presenter.BuildRunOverSummary(result));
            StringAssert.Contains("Sieg!", presenter.BuildVictorySummary(result));
            StringAssert.Contains("EP-Aufschl\u00fcsselung", presenter.BuildXpBreakdown(result));
            StringAssert.Contains("Schwerstes", presenter.BuildAnalyticsSummary(result));
            StringAssert.Contains("Run-Punktzahl", presenter.BuildRunScoreBreakdown(result));

            CollectionAssert.IsEmpty(
                LocalizationService.MissingGermanKeys,
                "End-screen presenter text must not fall back to English while German is active.");
        }

        [Test]
        public void GeneratedUiHardcodedStringGate_RemainsStrict()
        {
            var source = File.ReadAllText(GeneratedUiLocalizationCoveragePath);

            StringAssert.Contains(
                "KnownGeneratedUiStringDebt = new HashSet<string>();",
                source,
                "Generated UI localization debt allowlist must stay empty.");
            StringAssert.Contains("MainMenuBlueprintBuilder.cs", source);
            StringAssert.Contains("MainMenuBlueprintBuilder.ChallengePanels.cs", source);
            StringAssert.Contains("InRunUiBlueprintBuilder.cs", source);
            StringAssert.Contains("MainMenuController.cs", source);
            StringAssert.Contains("InRunController.cs", source);
            StringAssert.Contains("BossGateViewController.cs", source);
            StringAssert.Contains("RewardViewController.cs", source);
            StringAssert.Contains("HudViewController.cs", source);
            StringAssert.Contains("ShopViewController.cs", source);
            StringAssert.Contains("CursePanelController.cs", source);
            StringAssert.Contains("EndScreenViewController.cs", source);
            StringAssert.Contains("ItemsMenuController.cs", source);
            StringAssert.Contains("MetaProgressionPanelController.cs", source);
            StringAssert.Contains("EndScreenPresenter.cs", source);
            StringAssert.Contains("BuildToggle", source);
            StringAssert.Contains("AddIconEntry", source);
            StringAssert.Contains("DirectVisibleText", source);
            StringAssert.Contains("\"Run of the Nine\"", source);
        }

        private static IReadOnlyList<string> BuildReleaseFacingContentKeys()
        {
            var keys = new List<string>();
            keys.AddRange(LocalizationService.RequiredCreditKeys);
            keys.AddRange(LocalizationService.RequiredRuntimeFeedbackKeys);
            keys.AddRange(ItemService.GetLocalizationKeys());
            keys.AddRange(RelicService.GetLocalizationKeys());
            keys.AddRange(BossService.GetLocalizationKeys());
            keys.AddRange(CurseService.GetLocalizationKeys());
            keys.AddRange(DailyGoalService.GetLocalizationKeys());
            keys.AddRange(RunEventService.GetLocalizationKeys());
            keys.AddRange(SeasonalChallengeService.GetLocalizationKeys());
            keys.AddRange(ParkNarrativeService.GetLocalizationKeys());

            return keys
                .Where(key => !string.IsNullOrEmpty(key))
                .Distinct()
                .OrderBy(key => key)
                .ToList();
        }

        private static void AssertCatalogFileExists(string language)
        {
            var path = GetCatalogPath(language);
            Assert.IsTrue(File.Exists(path), $"Missing localization catalog: {path}");
        }

        private static IEnumerable<string> ReadCatalogKeys(string language)
        {
            var path = GetCatalogPath(language);
            var text = File.ReadAllText(path);
            foreach (Match match in Regex.Matches(text, "\"(?<key>(?:\\\\.|[^\"\\\\])*)\"\\s*:"))
            {
                var key = match.Groups["key"].Value;
                if (!string.IsNullOrEmpty(key))
                    yield return Regex.Unescape(key);
            }
        }

        private static string GetCatalogPath(string language)
        {
            return Path.Combine(Application.dataPath, "Resources", "Localization", language + ".json");
        }
    }
}
