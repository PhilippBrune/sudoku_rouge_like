using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class ModifierDiscoveryPersistenceTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private string _savePath;
        private string _backupPath;
        private string _tempPath;
        private string _originalSaveJson;
        private string _originalBackupJson;
        private string _originalTempJson;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();

            _savePath = Path.Combine(Application.persistentDataPath, $"save_profile_{TestSlot}.json");
            _backupPath = _savePath + ".bak";
            _tempPath = _savePath + ".tmp";

            Directory.CreateDirectory(Application.persistentDataPath);

            _originalSaveJson = File.Exists(_savePath) ? File.ReadAllText(_savePath) : null;
            _originalBackupJson = File.Exists(_backupPath) ? File.ReadAllText(_backupPath) : null;
            _originalTempJson = File.Exists(_tempPath) ? File.ReadAllText(_tempPath) : null;

            DeleteIfExists(_savePath);
            DeleteIfExists(_backupPath);
            DeleteIfExists(_tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();

            RestoreFile(_savePath, _originalSaveJson);
            RestoreFile(_backupPath, _originalBackupJson);
            RestoreFile(_tempPath, _originalTempJson);
        }

        [Test]
        public void MergeIntoMeta_DeduplicatesAndKeepsExistingDiscoveries()
        {
            var meta = new MetaProgressionState
            {
                DiscoveredBossModifiers = new List<BossModifierId>
                {
                    BossModifierId.FogOfWar
                }
            };

            var added = ModifierDiscoveryService.MergeIntoMeta(meta, new[]
            {
                BossModifierId.FogOfWar,
                BossModifierId.Antiknight,
                BossModifierId.Antiknight
            });

            Assert.AreEqual(1, added);
            CollectionAssert.AreEquivalent(
                new[] { BossModifierId.FogOfWar, BossModifierId.Antiknight },
                meta.DiscoveredBossModifiers);
        }

        [Test]
        public void RecordRunAndGetNewUnlocks_PersistsLiveRunDiscoveryWithoutAutosave()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());

            var liveRunState = new RunState();
            liveRunState.SeenBossModifiers.Add(BossModifierId.FogOfWar);
            liveRunState.SeenBossModifiers.Add(BossModifierId.Antiknight);

            var profile = new ProfileService(save);
            profile.RecordRunAndGetNewUnlocks(new RunResult
            {
                PlayedClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                Victory = false
            }, liveRunState);

            var loaded = save.Load();
            CollectionAssert.AreEquivalent(
                new[] { BossModifierId.FogOfWar, BossModifierId.Antiknight },
                loaded.MetaProgress.DiscoveredBossModifiers);
        }

        [Test]
        public void RecordRunAndGetNewUnlocks_WinningCurrentHarmonyWithBossUnlocksNextLevel()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());

            var profile = new ProfileService(save);
            profile.RecordRunAndGetNewUnlocks(new RunResult
            {
                PlayedClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                Victory = true,
                HarmonyLevel = 0,
                BossesDefeatedThisRun = 1
            }, new RunState());

            var loaded = save.Load();
            Assert.AreEqual(1, loaded.MetaProgress.MaxUnlockedHarmonyLevel);
        }

        [Test]
        public void RunAutoSave_MergesSeenModifiersIntoMetaProgress()
        {
            var save = new SaveFileService(TestSlot);
            save.Save(new SaveFileEnvelope());

            var coordinator = new RunAutoSaveCoordinator(save);
            var runState = new RunState();
            runState.Mode = GameMode.GardenRun;
            runState.SeenBossModifiers.Add(BossModifierId.GermanWhispers);

            coordinator.Save(runState, null);

            var loaded = save.Load();
            CollectionAssert.AreEqual(
                new[] { BossModifierId.GermanWhispers },
                loaded.MetaProgress.DiscoveredBossModifiers);
        }

        [Test]
        public void Load_SanitizesMissingDiscoveredBossModifiersFromLegacyJson()
        {
            var legacyJson = "{\"SaveVersion\":\"1.0.0\",\"MetaProgress\":{\"SpiritTrialsUnlocked\":true}}";
            File.WriteAllText(_savePath, legacyJson);

            var loaded = new SaveFileService(TestSlot).Load();

            Assert.IsNotNull(loaded.MetaProgress.DiscoveredBossModifiers);
            Assert.AreEqual(0, loaded.MetaProgress.DiscoveredBossModifiers.Count);
        }

        [Test]
        public void SeedRunSeenModifiers_UnionsMetaDiscoveryWithExistingRunState()
        {
            var runState = new RunState();
            runState.SeenBossModifiers.Add(BossModifierId.FogOfWar);

            var added = ModifierDiscoveryService.SeedRunSeenModifiers(runState, new[]
            {
                BossModifierId.FogOfWar,
                BossModifierId.Antiknight
            });

            Assert.AreEqual(1, added);
            CollectionAssert.AreEquivalent(
                new[] { BossModifierId.FogOfWar, BossModifierId.Antiknight },
                runState.SeenBossModifiers);
            CollectionAssert.AreEquivalent(
                runState.SeenBossModifiers,
                runState.SeenBossModifierList);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void RestoreFile(string path, string contents)
        {
            if (contents == null)
            {
                DeleteIfExists(path);
                return;
            }

            File.WriteAllText(path, contents);
        }
    }

    [TestFixture]
    public class SaveWriteQueueTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private string _savePath;
        private string _backupPath;
        private string _tempPath;
        private string _originalSaveJson;
        private string _originalBackupJson;
        private string _originalTempJson;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();

            _savePath = Path.Combine(Application.persistentDataPath, $"save_profile_{TestSlot}.json");
            _backupPath = _savePath + ".bak";
            _tempPath = _savePath + ".tmp";

            Directory.CreateDirectory(Application.persistentDataPath);

            _originalSaveJson = File.Exists(_savePath) ? File.ReadAllText(_savePath) : null;
            _originalBackupJson = File.Exists(_backupPath) ? File.ReadAllText(_backupPath) : null;
            _originalTempJson = File.Exists(_tempPath) ? File.ReadAllText(_tempPath) : null;

            DeleteIfExists(_savePath);
            DeleteIfExists(_backupPath);
            DeleteIfExists(_tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();

            RestoreFile(_savePath, _originalSaveJson);
            RestoreFile(_backupPath, _originalBackupJson);
            RestoreFile(_tempPath, _originalTempJson);
        }

        [Test]
        public void Flush_ExecutesQueuedWritesInOrder()
        {
            var queue = new SaveWriteQueue();
            var order = new List<int>();

            queue.Enqueue(() => order.Add(1));
            queue.Enqueue(() => order.Add(2));
            queue.Flush();

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
            Assert.AreEqual(2, queue.EnqueuedWrites);
            Assert.AreEqual(2, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
            Assert.IsFalse(queue.HasPendingWrites);
        }

        [Test]
        public void Flush_RecordsFailuresAndContinuesLaterWrites()
        {
            var queue = new SaveWriteQueue();
            var order = new List<int>();

            queue.Enqueue(() => throw new IOException("expected test failure"));
            queue.Enqueue(() => order.Add(2));
            queue.Flush();

            CollectionAssert.AreEqual(new[] { 2 }, order);
            Assert.AreEqual(2, queue.EnqueuedWrites);
            Assert.AreEqual(1, queue.CompletedWrites);
            Assert.AreEqual(1, queue.FailedWrites);
            Assert.IsNotNull(queue.LastError);
            Assert.IsFalse(queue.HasPendingWrites);
        }

        [Test]
        public void Load_FlushesInjectedQueueAndReadsLatestSave()
        {
            var queue = new SaveWriteQueue();
            var save = new SaveFileService(TestSlot, queue);

            save.Save(new SaveFileEnvelope
            {
                MetaProgress = new MetaProgressionState { PrestigeCount = 1 }
            });
            save.Save(new SaveFileEnvelope
            {
                MetaProgress = new MetaProgressionState { PrestigeCount = 2 }
            });

            var loaded = save.Load();

            Assert.AreEqual(2, loaded.MetaProgress.PrestigeCount);
            Assert.AreEqual(2, queue.EnqueuedWrites);
            Assert.AreEqual(2, queue.CompletedWrites);
            Assert.AreEqual(0, queue.FailedWrites);
            Assert.IsFalse(queue.HasPendingWrites);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void RestoreFile(string path, string contents)
        {
            if (contents == null)
            {
                DeleteIfExists(path);
                return;
            }

            File.WriteAllText(path, contents);
        }
    }

    [TestFixture]
    public class LocalizationServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            LocalizationService.ResetDiagnostics();
        }

        [Test]
        public void StableCreditKey_ResolvesEnglishAndGermanText()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            Assert.AreEqual(
                "Third-Party Development Tools",
                LocalizationService.T("Credits.Role.ThirdPartyDevelopmentTools"));

            LocalizationService.SetLanguage(LanguageOption.German);
            Assert.AreEqual(
                "Drittanbieter-Entwicklungswerkzeuge",
                LocalizationService.T("Credits.Role.ThirdPartyDevelopmentTools"));
        }

        [Test]
        public void MissingGermanKey_IsRecordedAndFallsBack()
        {
            LocalizationService.SetLanguage(LanguageOption.German);
            LocalizationService.ResetDiagnostics();

            var resolved = LocalizationService.T("Credits.TestMissingKey");

            Assert.AreEqual("Credits.TestMissingKey", resolved);
            CollectionAssert.Contains(
                LocalizationService.MissingGermanKeys,
                "Credits.TestMissingKey");
        }

        [Test]
        public void PluralHelpers_SelectLocalizedSingularAndPluralForms()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            Assert.AreEqual(
                "Reroll (1 token)",
                LocalizationService.FormatPlural(
                    1,
                    "InRun.Shop.Reroll.Token.One",
                    "InRun.Shop.Reroll.Token.Many",
                    "Reroll ({0} token)",
                    "Reroll ({0} tokens)"));
            Assert.AreEqual(
                "Reroll (2 tokens)",
                LocalizationService.FormatPlural(
                    2,
                    "InRun.Shop.Reroll.Token.One",
                    "InRun.Shop.Reroll.Token.Many",
                    "Reroll ({0} token)",
                    "Reroll ({0} tokens)"));

            LocalizationService.SetLanguage(LanguageOption.German);
            LocalizationService.ResetDiagnostics();
            Assert.AreEqual(
                "Neuwurf (1 Marke)",
                LocalizationService.FormatPlural(
                    1,
                    "InRun.Shop.Reroll.Token.One",
                    "InRun.Shop.Reroll.Token.Many",
                    "Reroll ({0} token)",
                    "Reroll ({0} tokens)"));
            Assert.AreEqual(
                "Neuwurf (2 Marken)",
                LocalizationService.FormatPlural(
                    2,
                    "InRun.Shop.Reroll.Token.One",
                    "InRun.Shop.Reroll.Token.Many",
                    "Reroll ({0} token)",
                    "Reroll ({0} tokens)"));
            Assert.AreEqual(
                "Gegenst\u00e4nde",
                LocalizationService.Plural(
                    2,
                    "InRun.Reward.Item.One",
                    "InRun.Reward.Item.Many",
                    "item",
                    "items"));
            CollectionAssert.IsEmpty(LocalizationService.MissingGermanKeys);
        }

        [Test]
        public void RequiredCreditKeys_HaveGermanTranslations()
        {
            var missing = LocalizationService.FindMissingGermanTranslations(
                LocalizationService.RequiredCreditKeys);

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void FirstRunSetupKeys_HaveGermanTranslations()
        {
            var missing = LocalizationService.FindMissingGermanTranslations(GetFirstRunSetupKeys());

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void ExternalLocalizationCatalogFiles_ArePresentAndLoaded()
        {
            var english = LoadCatalogFileText("en");
            var german = LoadCatalogFileText("de");

            StringAssert.Contains("\"Credits.Role.ThirdPartyDevelopmentTools\"", english);
            StringAssert.Contains("\"Credits.Role.ThirdPartyDevelopmentTools\"", german);
            StringAssert.Contains("\"Item.Name.Solver\"", german);
            StringAssert.Contains("\"Narrative.FloorEntry.0\"", german);
            CollectionAssert.IsEmpty(LocalizationService.ExternalCatalogDiagnostics);
        }

        [Test]
        public void LocalizationService_LoadsExternalCatalogsBeforeCodeFallback()
        {
            Assert.GreaterOrEqual(LocalizationService.GetExternalCatalogEntryCount(LanguageOption.English), 40);
            Assert.GreaterOrEqual(LocalizationService.GetExternalCatalogEntryCount(LanguageOption.German), 500);
            Assert.IsTrue(LocalizationService.HasExternalCatalogEntry(
                LanguageOption.English,
                "Credits.Role.ThirdPartyDevelopmentTools"));
            Assert.IsTrue(LocalizationService.HasExternalCatalogEntry(
                LanguageOption.German,
                "Credits.Role.ThirdPartyDevelopmentTools"));
            CollectionAssert.IsEmpty(LocalizationService.ExternalCatalogDiagnostics);
        }

        [Test]
        public void GameplayContentKeys_HaveGermanTranslations()
        {
            var keys = new List<string>();
            keys.AddRange(ItemService.GetLocalizationKeys());
            keys.AddRange(RelicService.GetLocalizationKeys());
            keys.AddRange(BossService.GetLocalizationKeys());
            keys.AddRange(CurseService.GetLocalizationKeys());
            keys.AddRange(ParkNarrativeService.GetLocalizationKeys());

            var missing = LocalizationService.FindMissingGermanTranslations(keys);

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void MainMenuGeneratedUiKeys_HaveGermanTranslations()
        {
            var missing = LocalizationService.FindMissingGermanTranslations(GetMainMenuGeneratedUiKeys());

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void InRunGeneratedUiKeys_HaveGermanTranslations()
        {
            var missing = LocalizationService.FindMissingGermanTranslations(GetInRunGeneratedUiKeys());

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void ReleaseFacingGermanLocalizationReport_IsComplete()
        {
            var keys = BuildReleaseFacingLocalizationKeys();
            var missing = LocalizationService.FindMissingGermanTranslations(keys);

            Assert.IsEmpty(
                missing,
                "Missing German release-facing localization keys:\n" + string.Join("\n", missing));
            Assert.Greater(keys.Count, 650, "Release-facing localization coverage gate should include all known catalog providers.");
        }

        [Test]
        public void GameplayContentServices_ReturnGermanTextWithoutMissingDiagnostics()
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

            CollectionAssert.IsEmpty(LocalizationService.MissingGermanKeys);
        }

        [Test]
        public void NarrativeFlavor_UsesJapaneseGardenIdentityInEnglishAndGerman()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            StringAssert.Contains("bamboo", ParkNarrativeService.GetFloorEntryFlavor(0).ToLowerInvariant());
            StringAssert.Contains("torii", ParkNarrativeService.GetRunEndFlavor(true).ToLowerInvariant());
            StringAssert.Contains("cursed route", ParkNarrativeService.GetNodeFlavor(NodeType.Cursed, 0).ToLowerInvariant());
            StringAssert.Contains("garden gate", LocalizationService.T("SudokuBasicsPrompt.Body").ToLowerInvariant());

            LocalizationService.SetLanguage(LanguageOption.German);
            StringAssert.Contains("Bambustor", ParkNarrativeService.GetFloorEntryFlavor(0));
            StringAssert.Contains("Torii", ParkNarrativeService.GetRunEndFlavor(true));
            StringAssert.Contains("verfluchte Route", ParkNarrativeService.GetNodeFlavor(NodeType.Cursed, 0));
            StringAssert.Contains("Gartentor", LocalizationService.T("SudokuBasicsPrompt.Body"));
        }

        [Test]
        public void ClassNarrative_HasLocalizedIntroAndBranchingEndings()
        {
            var keys = ParkNarrativeService.GetLocalizationKeys().ToList();
            CollectionAssert.Contains(keys, "Narrative.ClassIntro.KoiGambler");
            CollectionAssert.Contains(keys, "Narrative.ClassRunEnd.Victory.KoiGambler");
            CollectionAssert.Contains(keys, "Narrative.ClassRunEnd.Defeat.KoiGambler");

            LocalizationService.SetLanguage(LanguageOption.English);
            StringAssert.Contains("gold koi", ParkNarrativeService.GetClassIntroFlavor(ClassId.KoiGambler).ToLowerInvariant());
            StringAssert.Contains("wager", ParkNarrativeService.GetRunEndFlavor(true, ClassId.KoiGambler).ToLowerInvariant());

            LocalizationService.SetLanguage(LanguageOption.German);
            LocalizationService.ResetDiagnostics();
            StringAssert.Contains("Goldkoi", ParkNarrativeService.GetClassIntroFlavor(ClassId.KoiGambler));
            StringAssert.Contains("Wette", ParkNarrativeService.GetRunEndFlavor(true, ClassId.KoiGambler));
            StringAssert.Contains("Torii", ParkNarrativeService.GetRunEndFlavor(true, ClassId.KoiGambler));
            CollectionAssert.IsEmpty(LocalizationService.MissingGermanKeys);

            var introSource = File.ReadAllText("Assets/Scripts/UI/InRunController.cs");
            StringAssert.Contains("ParkNarrativeService.GetClassIntroFlavor", introSource);

            var endSource = File.ReadAllText("Assets/Scripts/UI/EndScreenViewController.cs");
            StringAssert.Contains("ParkNarrativeService.GetRunEndFlavor(victory, classId)", endSource);
        }

        [Test]
        public void StartupFlow_NoProfiles_RoutesToProfileSelect()
        {
            var flow = new StartupFlowController();

            var step = flow.GetNextStep(new StartupFlowContext
            {
                AllowProfileSelection = true,
                HasAnyProfile = false,
                Envelope = new SaveFileEnvelope(),
                SudokuBasicsComplete = false
            });

            Assert.AreEqual(StartupFlowStep.ProfileSelect, step);
        }

        [Test]
        public void StartupFlow_IncompleteFirstRun_RoutesToFirstRunSetupBeforeTutorial()
        {
            var flow = new StartupFlowController();

            var step = flow.GetNextStep(new StartupFlowContext
            {
                AllowProfileSelection = true,
                HasAnyProfile = true,
                Envelope = new SaveFileEnvelope(),
                SudokuBasicsComplete = false
            });

            Assert.AreEqual(StartupFlowStep.FirstRunSetup, step);
        }

        [Test]
        public void StartupFlow_CompletedFirstRun_RoutesToSudokuBasicsPrompt()
        {
            var flow = new StartupFlowController();
            var envelope = new SaveFileEnvelope();
            StartupFlowController.MarkFirstRunSetupComplete(envelope, "2026-05-10T00:00:00.0000000Z");

            var step = flow.GetNextStep(new StartupFlowContext
            {
                AllowProfileSelection = true,
                HasAnyProfile = true,
                Envelope = envelope,
                SudokuBasicsComplete = false
            });

            Assert.AreEqual(StartupFlowStep.SudokuBasicsPrompt, step);
        }

        [Test]
        public void StartupFlow_CompletedFirstRunAndTutorial_RoutesToMainMenu()
        {
            var flow = new StartupFlowController();
            var envelope = new SaveFileEnvelope();
            StartupFlowController.MarkFirstRunSetupComplete(envelope, "2026-05-10T00:00:00.0000000Z");

            var step = flow.GetNextStep(new StartupFlowContext
            {
                AllowProfileSelection = true,
                HasAnyProfile = true,
                Envelope = envelope,
                SudokuBasicsComplete = true
            });

            Assert.AreEqual(StartupFlowStep.MainMenu, step);
        }

        private static string LoadCatalogFileText(string language)
        {
            var path = Path.Combine(
                Application.dataPath,
                "Resources",
                "Localization",
                language + ".json");

            Assert.IsTrue(File.Exists(path), $"Missing localization catalog: {path}");

            return File.ReadAllText(path);
        }

        private static IReadOnlyList<string> BuildReleaseFacingLocalizationKeys()
        {
            var keys = new List<string>();
            keys.AddRange(LocalizationService.RequiredCreditKeys);
            keys.AddRange(GetFirstRunSetupKeys());
            keys.AddRange(GetMenuFlowKeys());
            keys.AddRange(GetOptionsAndAccessibilityKeys());
            keys.AddRange(GetSudokuBasicsKeys());
            keys.AddRange(GetMainMenuGeneratedUiKeys());
            keys.AddRange(GetInRunGeneratedUiKeys());
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

        private static IEnumerable<string> GetFirstRunSetupKeys()
        {
            return new[]
            {
                "FirstRun.Title",
                "FirstRun.Body",
                "FirstRun.LanguageHeader",
                "FirstRun.Language.English",
                "FirstRun.Language.German",
                "FirstRun.AccessibilityHeader",
                "FirstRun.Accessibility.Colorblind",
                "FirstRun.Accessibility.HighContrast",
                "FirstRun.Accessibility.ReduceMotion",
                "FirstRun.Accessibility.ScreenReader",
                "FirstRun.MoreOptionsHint",
                "FirstRun.LegalHeader",
                "FirstRun.LegalBody",
                "FirstRun.Continue"
            };
        }

        private static IEnumerable<string> GetMenuFlowKeys()
        {
            return new[]
            {
                "Start Game",
                "Resume Game",
                "Tutorial",
                "Meta Progression",
                "Game Modes",
                "Items",
                "Options",
                "Credits",
                "Quit",
                "Ready.",
                "Select Class",
                "Continue",
                "Back",
                "Allow Irregular Puzzles",
                "Class not yet unlocked.",
                "MainMenu.Status.ClassLocked",
                "Custom Puzzle",
                "Board Size",
                "Difficulty",
                "Region Layout",
                "Resource Mode",
                "Class:",
                "Sudoku Modes",
                "Start Puzzle",
                "Start Garden Run",
                "Start Endless Zen",
                "Start Spirit Trials",
                "Records",
                "Spirit Trials",
                "Daily Walk",
                "Monthly Walk",
                "Begin Monthly Walk",
                "Items & Relic Codex",
                "Relics",
                "Modes",
                "Boss Modifiers",
                "Load",
                "Delete",
                "Resume",
                "Profile.EmptySlot",
                "run in progress",
                "Never",
                "Profile.SlotInfo",
                "Resume.Button.Active",
                "MainMenu.Status.SelectGardenClass",
                "MainMenu.Status.NoRunToResume",
                "MainMenu.Status.EndlessZenLocked",
                "MainMenu.Status.SelectEndlessZenClass",
                "MainMenu.Status.SpiritTrialsLocked",
                "MainMenu.Status.SelectSpiritTrialsClass",
                "MainMenu.Status.DebugEnabled",
                "MainMenu.Status.DebugDisabled"
            };
        }

        private static IEnumerable<string> GetOptionsAndAccessibilityKeys()
        {
            return new[]
            {
                "Audio",
                "Master Volume",
                "Music Volume",
                "SFX Volume",
                "UI Volume",
                "Mute when unfocused",
                "Mute all",
                "Display",
                "Fullscreen",
                "VSync",
                "Screen Shake",
                "Resolution",
                "Language",
                "Gameplay",
                "Highlight Errors",
                "Confirm Wrong Placement",
                "Auto Pencil Cleanup",
                "Show Candidate Count",
                "Cursor Snap",
                "Double-Tap to Confirm",
                "Controller Rumble",
                "Controls & Accessibility",
                "Keybindings...",
                "Accessibility...",
                "Sudoku Tutorial",
                "Reset",
                "Apply",
                "Cancel",
                "Reset to Defaults",
                "Text Size",
                "Accessibility",
                "Colorblind Mode",
                "High Contrast",
                "Reduce Motion",
                "Alt Constraint Symbols",
                "Controls",
                "Particles",
                "Brightness",
                "Contrast",
                "DisplayMode",
                "DisplayMode.Fullscreen",
                "DisplayMode.Borderless",
                "DisplayMode.Windowed",
                "Screen Reader",
                "Rebind",
                "Reset All"
            };
        }

        private static IEnumerable<string> GetSudokuBasicsKeys()
        {
            var keys = new List<string>
            {
                "Welcome to Run of the Nine.",
                "SudokuBasicsPrompt.Body",
                "Yes, show me",
                "Skip",
                "SudokuBasics.Hint.AddOtherCandidate",
                "SudokuBasics.Hint.MoreMarks",
                "SudokuBasics.Hint.CheckRowColumn",
                "SudokuBasics.Hint.RowConflict",
                "SudokuBasics.Hint.ColumnConflict",
                "SudokuBasics.Hint.BoxConflict",
                "SudokuBasics.Hint.AnswerIs",
                "SudokuBasics.Button.Next",
                "SudokuBasics.Button.Play"
            };

            for (var i = 0; i <= 14; i++)
            {
                keys.Add($"SudokuBasics.Step{i}.Title");
                keys.Add($"SudokuBasics.Step{i}.Body");
            }

            return keys;
        }

        private static IEnumerable<string> GetMainMenuGeneratedUiKeys()
        {
            return new[]
            {
                "Begin",
                "MAX",
                "Endless Zen (Locked)",
                "Spirit Trials (Locked)",
                "Challenge.Daily.Title",
                "Challenge.Daily.StreakPlaceholder",
                "Challenge.Daily.Goal1Placeholder",
                "Challenge.Daily.Goal2Placeholder",
                "Challenge.Daily.Goal3Placeholder",
                "Challenge.Daily.InkStampsPlaceholder",
                "Challenge.Daily.ResetIn",
                "Challenge.Daily.Streak.One",
                "Challenge.Daily.Streak.Many",
                "Challenge.Daily.StampsSummary",
                "Challenge.Daily.Tier.Easy",
                "Challenge.Daily.Tier.Medium",
                "Challenge.Daily.Tier.Hard",
                "Challenge.Daily.GoalRewardSuffix",
                "Challenge.Monthly.Title",
                "Challenge.Monthly.LoadingTheme",
                "Challenge.Monthly.PersonalBestPlaceholder",
                "Challenge.Monthly.PersonalBest",
                "Challenge.Monthly.PersonalBest.None",
                "Challenge.Monthly.Best.Hp",
                "Challenge.Monthly.Best.Hp.None",
                "Challenge.Monthly.Best.PencilUsed",
                "Challenge.Monthly.Best.PencilUsed.None",
                "Challenge.Monthly.Best.Time",
                "Challenge.Monthly.Best.Time.None",
                "Challenge.Monthly.BestBreakdown",
                "ProfileSelect.Title",
                "ProfileSelect.Subtitle",
                "ProfileSelect.SlotLabel",
                "ProfileSelect.Loading",
                "ProfileSelect.Status.Deleted",
                "GeneratedUi.Dropdown.Select",
                "GeneratedUi.Dropdown.Option",
                "MainMenu.Subtitle",
                "Accessibility.Preview",
                "Accessibility.Preview.SampleTitle",
                "Accessibility.Preview.SampleBody",
                "Accessibility.Preview.SampleHint",
                "Keybindings.Title",
                "Keybindings.Hint",
                "Keybindings.Header.Action",
                "Keybindings.Header.Key",
                "Keybindings.Header.Gamepad",
                "Keybindings.UiOnly",
                "Keybindings.Status.Conflict",
                "Keybindings.Status.SavedWithUnbind",
                "Keybindings.Status.Saved",
                "Keybindings.Status.Listen",
                "Keybindings.Status.Cancelled",
                "Keybindings.Status.ResetAll",
                "Keybindings.Action.MoveUp",
                "Keybindings.Action.MoveDown",
                "Keybindings.Action.MoveLeft",
                "Keybindings.Action.MoveRight",
                "Keybindings.Action.TogglePencil",
                "Keybindings.Action.ClearCell",
                "Keybindings.Action.UndoLastDigit",
                "Keybindings.Action.HighlightDigit",
                "Keybindings.Action.BagNext",
                "Keybindings.Action.BagPrevious",
                "Keybindings.Action.UseItem",
                "Keybindings.Action.InspectItem",
                "Keybindings.Action.Confirm",
                "Keybindings.Action.Cancel",
                "Keybindings.Action.SkipAnimation",
                "Keybindings.Action.TabNext",
                "Keybindings.Action.TabPrevious",
                "GameModes.HarmonyHeader",
                "GameModes.Description",
                "SpiritTrials.TierSelectSubtitle",
                "SpiritTrials.TierStats",
                "SpiritTrials.Tier.Apprentice",
                "SpiritTrials.Tier.Adept",
                "SpiritTrials.Tier.Master",
                "SpiritTrials.Tier.Grandmaster",
                "SpiritTrials.BestPlaceholder",
                "SpiritTrials.BestScore",
                "EndlessZen.Title",
                "EndlessZen.Subtitle",
                "EndlessZen.PersonalBestPlaceholder",
                "EndlessZen.PersonalBestDepth",
                "EndlessZen.TotalSessions",
                "RunConfirm.StatsLine",
                "RunConfirm.Mode.EndlessZen",
                "RunConfirm.Mode.SpiritTrials",
                "RunConfirm.Mode.GardenRun",
                "RunConfirm.Irregular.On",
                "RunConfirm.Irregular.Off",
                "Harmony.Readout",
                "Harmony.SelectedLabel",
                "Harmony.SubLabel",
                "Harmony.Perk.Button",
                "Perk: Locked until H4",
                "MainMenu.Perks.UnlockHint",
                "Class.LevelShort",
                "Class.XpLine",
                "Class.XpMaxLine",
                "Class.SummaryLine",
                "Class.LevelUnlocksHeader",
                "Class.LevelUnlocksPreviewHeader",
                "Class.ExclusiveUnlocksHeader",
                "Class.UnlocksAtLevel15Unknown",
                "Class.UnlocksAtLevel30Unknown",
                "Class.LockedHeader",
                "Class.UnlockConditionLine",
                "Class.UnlockProgress.Bosses2",
                "Class.UnlockProgress.Items15",
                "Class.UnlockProgress.Relics10",
                "Class.UnlockProgress.Bosses10",
                "Class.UnlockProgress.Gold50000",
                "Class.UnlockProgress.ItemCodexComplete",
                "Class.UnlockProgress.ItemCodexIncomplete",
                "Class.UnlockProgress.PerfectRunComplete",
                "Class.UnlockProgress.PerfectRunIncomplete",
                "Harmony.Name.0",
                "Harmony.Name.1",
                "Harmony.Name.2",
                "Harmony.Name.3",
                "Harmony.Name.4",
                "Harmony.Name.5",
                "Harmony.Name.6",
                "Harmony.Name.7",
                "Harmony.Name.8",
                "Harmony.Name.9",
                "Harmony.Name.10",
                "Harmony.Perk.Name.None",
                "Harmony.Perk.Name.MoonshadeOffering",
                "Harmony.Perk.Name.ScholarsBurden",
                "Harmony.Perk.Name.VoidWard",
                "Harmony.Perk.Name.EmptyCanvas",
                "Harmony.Perk.Description.None",
                "Harmony.Perk.Description.MoonshadeOffering",
                "Harmony.Perk.Description.ScholarsBurden",
                "Harmony.Perk.Description.VoidWard",
                "Harmony.Perk.Description.EmptyCanvas",
                "Class.Passive.NumberFreak",
                "Class.Passive.GardenMonk",
                "Class.Passive.ShrineArchivist",
                "Class.Passive.KoiGambler",
                "Class.Passive.StoneGardener",
                "Class.Passive.LanternSeer",
                "Class.Passive.ReedDuelist",
                "Class.Passive.QuietCartographer",
                "Class.UnlockCondition.NumberFreak",
                "Class.UnlockCondition.GardenMonk",
                "Class.UnlockCondition.ShrineArchivist",
                "Class.UnlockCondition.KoiGambler",
                "Class.UnlockCondition.StoneGardener",
                "Class.UnlockCondition.LanternSeer",
                "Class.UnlockCondition.ReedDuelist",
                "Class.UnlockCondition.QuietCartographer"
            };
        }

        private static IEnumerable<string> GetInRunGeneratedUiKeys()
        {
            return new[]
            {
                "InRun.ClassIntro.Floor",
                "InRun.ClassIntro.BeginHint",
                "InRun.Path.LegendTitle",
                "InRun.Path.Node.Start",
                "InRun.Path.Node.Puzzle",
                "InRun.Path.Node.Elite",
                "InRun.Path.Node.PreBoss",
                "InRun.Path.Node.Boss",
                "InRun.Path.Node.Shop",
                "InRun.Path.Node.Rest",
                "InRun.Path.Node.Relic",
                "InRun.Path.Node.Cursed",
                "InRun.Path.Node.Event",
                "InRun.Path.Node.BossGateShort",
                "InRun.Path.Node.BossGate",
                "InRun.Path.Node.Bridge",
                "InRun.Path.SaveAndQuit",
                "InRun.Path.FirstPickHint",
                "InRun.Path.CrossLaneTip",
                "InRun.Path.Lane.GardenWalk",
                "InRun.Path.Lane.BriarCrossing",
                "InRun.Path.Lane.KoiShore",
                "InRun.Path.Lane.VinePath",
                "InRun.Path.Lane.StoneBridge",
                "InRun.Path.Lane.DeepCurrent",
                "InRun.Path.Lane.TempleWalk",
                "InRun.Path.Lane.DemonTrail",
                "InRun.Path.Lane.SummitPath",
                "InRun.Path.Lane.AbyssEdge",
                "InRun.Path.Lane.CalmPath",
                "InRun.Path.Lane.RiskPath",
                "InRun.Path.LaneLabel.One",
                "InRun.Path.LaneLabel.Many",
                "InRun.Path.QuitPrompt",
                "InRun.Path.Quit",
                "InRun.Path.Stay",
                "InRun.PatternScroll.Title",
                "InRun.PatternScroll.Empty",
                "InRun.PatternScroll.ConfirmSelection",
                "InRun.Status.CursedPuzzle",
                "InRun.Status.PuzzleNode",
                "InRun.Status.ArrivedPuzzle",
                "InRun.BoardPreview.NotInitialized",
                "InRun.BoardPreview.StartRunHint",
                "InRun.BoardPreview.Status",
                "InRun.TutorialBanner",
                "Common.Yes",
                "Common.No",
                "Accept",
                "Confirm",
                "Decline",
                "Leave",
                "Item.Rarity.Normal",
                "Item.Rarity.Rare",
                "Item.Rarity.Epic",
                "Relic.Tier.Tier1",
                "Relic.Tier.Tier2",
                "Relic.Tier.Tier3",
                "Relic.Tier.Tier4",
                "Relic.Tier.Legendary",
                "InRun.Cursed.Title",
                "InRun.Cursed.Description",
                "InRun.Cursed.Accept",
                "InRun.BossGate.Title",
                "InRun.BossGate.WardingFlame",
                "InRun.Reward.Title",
                "InRun.Reward.NoRewards",
                "InRun.Reward.Summary",
                "InRun.Reward.Mistake.One",
                "InRun.Reward.Mistake.Many",
                "InRun.Reward.Perfect",
                "InRun.Reward.NotPerfect",
                "InRun.Reward.NoPencil",
                "InRun.Reward.PencilUsed",
                "InRun.Reward.Item.One",
                "InRun.Reward.Item.Many",
                "InRun.Reward.ItemChoice",
                "InRun.Reward.Nothing",
                "InRun.Common.Empty",
                "InRun.BagSwap.Title",
                "InRun.BagSwap.Slot",
                "InRun.BagSwap.KeepBag",
                "InRun.Rest.Title",
                "InRun.Rest.Heal",
                "InRun.Rest.Status.Healed",
                "InRun.Rest.Pencil",
                "InRun.Rest.Status.Pencil",
                "InRun.Rest.Reroll",
                "InRun.Rest.Status.Reroll",
                "InRun.Curse.Generic",
                "InRun.Rest.Cleanse",
                "InRun.Rest.Status.Cleansed",
                "InRun.Relic.Title",
                "InRun.Relic.Choice",
                "InRun.Relic.Status.Accepted",
                "InRun.Relic.Status.Left",
                "InRun.Event.Status.Nothing",
                "InRun.Event.Status.Chose",
                "InRun.Shop.Title",
                "InRun.Shop.SummaryHint",
                "InRun.Shop.Offer.Item",
                "InRun.Shop.Offer.Generic",
                "InRun.Shop.Buy",
                "InRun.Shop.Reroll.Token.One",
                "InRun.Shop.Reroll.Token.Many",
                "InRun.Shop.Reroll.Gold",
                "InRun.Shop.NotEnoughGold",
                "InRun.Shop.Skip",
                "InRun.Shop.Preview.Item",
                "InRun.Shop.Preview.Generic",
                "InRun.Shop.ResourceHeader",
                "InRun.Curse.Title",
                "InRun.Curse.None",
                "InRun.Curse.Line",
                "InRun.End.BossCleared.Continue",
                "InRun.End.BossCleared.Title",
                "InRun.End.BossCleared.Stats",
                "InRun.End.ClaimReward",
                "InRun.End.Title.Victory",
                "InRun.End.Title.Defeat",
                "InRun.End.Detail.ClassFloor",
                "InRun.End.Detail.HpGold",
                "InRun.End.Detail.XpEarned",
                "InRun.End.Detail.LevelUp",
                "InRun.End.Detail.ClassLevel",
                "InRun.End.Detail.XpBar",
                "InRun.End.Detail.ToLevel",
                "InRun.End.Detail.LevelMax",
                "InRun.End.Detail.NewUnlockHeader",
                "InRun.End.Detail.ClassUnlocked",
                "InRun.End.Seasonal.Title",
                "InRun.End.Seasonal.Grade",
                "InRun.End.Seasonal.Score",
                "InRun.End.Seasonal.CellsCorrect",
                "InRun.End.Seasonal.Mistakes",
                "InRun.End.Seasonal.PencilLeft",
                "InRun.End.Seasonal.NewPersonalBest",
                "InRun.End.Seasonal.PersonalBest",
                "InRun.End.Seasonal.FirstAttempt",
                "InRun.End.Seasonal.InkStampFirst",
                "InRun.End.Seasonal.InkStampS",
                "InRun.End.Trials.TitleComplete",
                "InRun.End.Trials.TitleFailed",
                "InRun.End.Trials.Score",
                "InRun.End.Trials.Time",
                "InRun.End.Trials.CellsFilled",
                "InRun.End.Trials.PencilMarks",
                "InRun.End.Trials.Mistakes",
                "InRun.End.Trials.Modifiers",
                "InRun.End.Trials.NewPersonalBest",
                "InRun.End.Trials.PersonalBest",
                "InRun.End.Trials.FirstCompletion",
                "InRun.End.Trials.AwaitAgain",
                "InRun.End.Trials.BestScore",
                "InRun.End.Zen.Title",
                "InRun.End.Zen.DepthReached",
                "InRun.End.Zen.SessionTime",
                "InRun.End.Zen.Mistakes",
                "InRun.End.Zen.NewPersonalBest",
                "InRun.End.Zen.PersonalBest",
                "InRun.End.Zen.FirstSession",
                "InRun.End.Zen.TotalSessions",
                "InRun.Hud.Status.ItemControllerHint",
                "InRun.Hud.Status.ItemInspect",
                "InRun.Hud.Hp",
                "InRun.Hud.Pencil",
                "InRun.Hud.RunTimer",
                "InRun.Hud.ActiveModifiers",
                "InRun.Hud.FloorModifiers",
                "InRun.Hud.Combo",
                "InRun.Hud.Passive",
                "InRun.Hud.ClassBadge",
                "InRun.Hud.ClassBadge.Prestige",
                "InRun.Hud.BagTitle",
                "InRun.Hud.RelicsTitle",
                "InRun.Hud.ItemSlot",
                "InRun.Hud.RelicPassive",
                "InRun.Hud.RelicSpent",
                "InRun.Hud.RelicSlot",
                "InRun.Hud.NoRelic",
                "InRun.Hud.Status.ItemClickHint",
                "InRun.Hud.Status.RelicInspect"
            };
        }
    }

    [TestFixture]
    public class MainMenuClassInfoPresenterTests
    {
        [TearDown]
        public void TearDown()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            LocalizationService.ResetDiagnostics();
        }

        [Test]
        public void Build_LockedGermanClassInfo_IncludesLocalizedProgressAndCopy()
        {
            LocalizationService.SetLanguage(LanguageOption.German);
            var meta = new MetaProgressionState();
            meta.ClassUnlocks.BossesDefeated = 1;

            var view = MainMenuClassInfoPresenter.Build(ClassId.GardenMonk, meta, false);

            StringAssert.Contains("GESPERRT", view.Text);
            StringAssert.Contains("Bosse besiegt: 1/2", view.Text);
            StringAssert.Contains("Stufenfreischaltungen", view.Text);
            Assert.AreEqual(new Color(0.80f, 0.55f, 0.35f, 1f), view.Color);
        }

        [Test]
        public void Build_UnlockedGermanClassInfo_IncludesLocalizedSummary()
        {
            LocalizationService.SetLanguage(LanguageOption.German);
            var meta = new MetaProgressionState();

            var view = MainMenuClassInfoPresenter.Build(ClassId.NumberFreak, meta, true);

            StringAssert.Contains("Stufe", view.Text);
            StringAssert.Contains("LP:10", view.Text);
            StringAssert.Contains("Neuwurf-Marke", view.Text);
            Assert.AreEqual(new Color(0.96f, 0.93f, 0.82f, 1f), view.Color);
        }
    }
}
