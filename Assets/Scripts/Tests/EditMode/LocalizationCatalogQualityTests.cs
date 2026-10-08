using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    [Requirement("REQ-UI-101")]
    public sealed class LocalizationCatalogQualityTests
    {
        // GAP-019: 60 German strings (and one key in code) were UTF-8 read as cp1252 ("ZurÃ¼ck", "Â·").
        // Test sources are not scanned because they name the markers.
        private static readonly string[] MojibakeMarkers =
        {
            "Ã¤", "Ã¶", "Ã¼", "ÃŸ", "Ã©", // ä ö ü ß é read as cp1252
            "Â·", "Â ",                                              // · and no-break space
            "â€"                                                               // – — “ ” ’ …
        };

        [Test]
        public void CatalogsAndSources_ContainNoDoubleEncodedText()
        {
            var files = new[] { "en", "de", "fr", "es", "pt" }
                .Select(lang => Path.Combine(Application.dataPath, "Resources", "Localization", lang + ".json"))
                .Concat(Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs", SearchOption.AllDirectories)
                    .Where(path => !path.Replace('\\', '/').Contains("/Tests/")));

            var hits = new List<string>();
            foreach (var path in files)
            {
                var lines = File.ReadAllLines(path);
                for (var i = 0; i < lines.Length; i++)
                    if (MojibakeMarkers.Any(marker => lines[i].Contains(marker)))
                        hits.Add($"{Path.GetFileName(path)}:{i + 1}: {lines[i].Trim()}");
            }
            CollectionAssert.IsEmpty(hits);
        }

        // 1.1 build smoke (B01): the profile screen showed "Profile.EmptySlot" because the key had German text only.
        // Every dotted key looked up without an English fallback must resolve in English.
        private static readonly Regex KeyOnlyLookup = new Regex(
            @"\b(?:LocalizationService\.)?(?:T|Tf)\(\s*""([A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+)""\s*(\)|,)");

        [Test]
        public void KeyOnlyLookups_HaveEnglishText()
        {
            var previous = SudokuRoguelike.Core.LocalizationService.Current;
            SudokuRoguelike.Core.LocalizationService.SetLanguage(SudokuRoguelike.Core.LanguageOption.English);
            try
            {
                var raw = new SortedSet<string>();
                foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs", SearchOption.AllDirectories)
                             .Where(path => !path.Replace('\\', '/').Contains("/Tests/")))
                {
                    var source = File.ReadAllText(path);
                    foreach (Match match in KeyOnlyLookup.Matches(source))
                    {
                        var isTf = source.Substring(match.Index, Math.Min(4, source.Length - match.Index)).StartsWith("Tf(")
                                   || match.Value.Contains(".Tf(");
                        if (match.Groups[2].Value == "," && !isTf) continue; // T(key, englishFallback)
                        var key = match.Groups[1].Value;
                        if (SudokuRoguelike.Core.LocalizationService.T(key) == key)
                            raw.Add($"{Path.GetFileName(path)}: {key}");
                    }
                }
                CollectionAssert.IsEmpty(raw, "these keys would be shown raw to English players");
            }
            finally
            {
                SudokuRoguelike.Core.LocalizationService.SetLanguage(previous);
            }
        }

        [Test]
        public void Catalogs_DoNotContainDuplicateKeys()
        {
            AssertNoDuplicateKeys(ReadCatalogEntries("en"), "en");
            AssertNoDuplicateKeys(ReadCatalogEntries("de"), "de");
        }

        [Test]
        public void GermanCatalog_PreservesEnglishFormatPlaceholders()
        {
            var english = ReadCatalogEntries("en")
                .GroupBy(entry => entry.Key)
                .ToDictionary(group => group.Key, group => group.First().Value);
            var german = ReadCatalogEntries("de")
                .GroupBy(entry => entry.Key)
                .ToDictionary(group => group.Key, group => group.First().Value);

            var mismatches = new List<string>();
            foreach (var pair in english.OrderBy(pair => pair.Key))
            {
                if (!german.TryGetValue(pair.Key, out var germanValue))
                    continue;

                var enPlaceholders = ExtractFormatPlaceholders(pair.Value);
                var dePlaceholders = ExtractFormatPlaceholders(germanValue);
                if (!enPlaceholders.SetEquals(dePlaceholders))
                {
                    mismatches.Add(
                        $"{pair.Key}: en=[{string.Join(", ", enPlaceholders.OrderBy(v => v))}] " +
                        $"de=[{string.Join(", ", dePlaceholders.OrderBy(v => v))}]");
                }
            }

            CollectionAssert.IsEmpty(
                mismatches,
                "German translations must preserve every numeric string.Format placeholder used by the English catalog.");
        }

        [Test]
        public void LocalizationCatalogWorkflow_DocumentsTranslatorQualityChecks()
        {
            var workflow = File.ReadAllText("docs/localization-catalog.md");

            StringAssert.Contains("duplicate keys", workflow);
            StringAssert.Contains("placeholder parity", workflow);
            StringAssert.Contains("LocalizationCatalogQualityTests", workflow);
            StringAssert.Contains("translator-facing", workflow);
        }

        private static IReadOnlyList<CatalogEntry> ReadCatalogEntries(string language)
        {
            var path = Path.Combine(Application.dataPath, "Resources", "Localization", language + ".json");
            var text = File.ReadAllText(path);
            var entries = new List<CatalogEntry>();

            foreach (Match match in Regex.Matches(
                text,
                "\"(?<key>(?:\\\\.|[^\"\\\\])*)\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.Singleline))
            {
                entries.Add(new CatalogEntry(
                    Regex.Unescape(match.Groups["key"].Value),
                    Regex.Unescape(match.Groups["value"].Value)));
            }

            return entries;
        }

        private static HashSet<string> ExtractFormatPlaceholders(string value)
        {
            var placeholders = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(value ?? string.Empty, "\\{[0-9]+(?::[^}]*)?\\}"))
                placeholders.Add(match.Value);
            return placeholders;
        }

        private static void AssertNoDuplicateKeys(IReadOnlyList<CatalogEntry> entries, string language)
        {
            var duplicates = entries
                .GroupBy(entry => entry.Key)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(key => key)
                .ToArray();

            CollectionAssert.IsEmpty(duplicates, $"Localization catalog '{language}' contains duplicate keys.");
        }

        private readonly struct CatalogEntry
        {
            public CatalogEntry(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }
            public string Value { get; }
        }
    }
}
