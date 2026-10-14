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
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // FUTURE-014 / PRG-LOCALIZATION (known-issues plan 2): French, Spanish and Brazilian Portuguese carry every text
    // German has, with the same placeholders, so no gameplay text falls back to English.
    [TestFixture]
    public sealed class LocalizationParityTests
    {
        private static readonly string[] Languages = { "fr", "es", "pt" };
        private static readonly Regex Placeholder = new Regex(@"\{\d+(?::[^}]*)?\}");

        [TearDown]
        public void TearDown()
        {
            LocalizationService.SetLanguage(LanguageOption.English);
            LocalizationService.ResetDiagnostics();
        }

        [Test]
        [Requirement("LOC-PARITY-001")]
        public void EveryGermanKey_ExistsInFrenchSpanishAndPortuguese()
        {
            var german = ReadCatalog("de");
            foreach (var lang in Languages)
            {
                var catalog = ReadCatalog(lang);
                var missing = german.Keys.Where(k => !catalog.ContainsKey(k)).OrderBy(k => k).ToList();
                CollectionAssert.IsEmpty(missing, $"{lang}.json lacks {missing.Count} key(s) German has");
            }
        }

        [Test]
        [Requirement("LOC-PARITY-002")]
        public void Translations_KeepTheGermanPlaceholders_AndAreNotEmpty()
        {
            var german = ReadCatalog("de");
            foreach (var lang in Languages)
            {
                var problems = new List<string>();
                foreach (var pair in ReadCatalog(lang))
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                        problems.Add($"{pair.Key}: empty");
                    if (german.TryGetValue(pair.Key, out var de) && !PlaceholdersOf(pair.Value).SetEquals(PlaceholdersOf(de)))
                        problems.Add($"{pair.Key}: placeholders differ from German");
                }
                CollectionAssert.IsEmpty(problems, $"{lang}.json");
            }
        }

        [Test]
        [Requirement("LOC-PARITY-003")]
        public void GameplayContent_DoesNotFallBackToEnglish([Values(LanguageOption.French, LanguageOption.Spanish, LanguageOption.BrazilianPortuguese)] LanguageOption language)
        {
            LocalizationService.SetLanguage(language);
            LocalizationService.ResetDiagnostics();

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                ItemService.GetItemName(type);
                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                    ItemService.GetItemDescription(type, rarity);
            }
            foreach (RelicId id in Enum.GetValues(typeof(RelicId)))
            {
                RelicService.GetRelicName(id);
                RelicService.GetRelicDescription(id);
            }
            // Only the modifiers the game can roll are localized (the catalog-batch-2 IDs are not in the pool).
            foreach (var key in BossService.GetLocalizationKeys())
                LocalizationService.T(key, key);
            for (var floor = 0; floor < 5; floor++)
            {
                foreach (NodeType node in Enum.GetValues(typeof(NodeType)))
                    ParkNarrativeService.GetNodeFlavor(node, floor);
                ParkNarrativeService.GetFloorEntryFlavor(floor);
            }
            foreach (ClassId classId in Enum.GetValues(typeof(ClassId)))
            {
                ParkNarrativeService.GetClassIntroFlavor(classId);
                ParkNarrativeService.GetRunEndFlavor(true, classId);
                ParkNarrativeService.GetRunEndFlavor(false, classId);
            }

            CollectionAssert.IsEmpty(LocalizationService.MissingGermanKeys,
                $"{language}: these lookups fell back to English");
        }

        [Test]
        [Requirement("LOC-PARITY-003")]
        public void ReleaseFacingContentKeys_AreInEveryCatalog([Values(LanguageOption.French, LanguageOption.Spanish, LanguageOption.BrazilianPortuguese)] LanguageOption language)
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

            var missing = keys.Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .Where(k => LocalizationService.HasGermanTranslation(k) && !LocalizationService.HasExternalCatalogEntry(language, k))
                .OrderBy(k => k)
                .ToList();
            CollectionAssert.IsEmpty(missing, $"{language}: keys German translates but this catalog lacks");
        }

        private static HashSet<string> PlaceholdersOf(string text) =>
            new HashSet<string>(Placeholder.Matches(text ?? string.Empty).Cast<Match>().Select(m => m.Value));

        private static Dictionary<string, string> ReadCatalog(string language)
        {
            var path = Path.Combine(Application.dataPath, "Resources", "Localization", language + ".json");
            var text = File.ReadAllText(path);
            var result = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(text, "\"(?<k>(?:\\\\.|[^\"\\\\])*)\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"\\\\])*)\""))
                result[Regex.Unescape(m.Groups["k"].Value)] = Regex.Unescape(m.Groups["v"].Value);
            return result;
        }
    }
}
