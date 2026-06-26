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
    public sealed class LocalizationCatalogQualityTests
    {
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
