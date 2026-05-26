using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class DynamicResourceIconCoverageTests
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string ValidationDocPath = "docs/runtime-resource-validation.md";
        private static readonly Regex LiteralResourcesLoadRegex =
            new Regex(@"Resources\.Load<[^>]+>\(\s*""([^""]+)""\s*\)", RegexOptions.Compiled);

        [Test]
        public void ItemIcons_AllMappedTypesResolveToRuntimeSprites()
        {
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                var iconName = ItemService.GetIconName(type);
                AssertNonEmpty(iconName, $"ItemType.{type}");
                AssertRuntimePngExists($"items/icon_{iconName}", $"ItemType.{type}");
            }
        }

        [Test]
        public void RelicIcons_AllMappedIdsResolveToRuntimeSprites()
        {
            foreach (RelicId id in Enum.GetValues(typeof(RelicId)))
            {
                var iconName = RelicService.GetIconName(id);
                AssertNonEmpty(iconName, $"RelicId.{id}");
                AssertRuntimePngExists($"{RelicService.GetIconFolder(id)}/icon_{iconName}", $"RelicId.{id}");
            }
        }

        [Test]
        public void BossModifierIcons_AllMappedIdsResolveToRuntimeSprites()
        {
            foreach (BossModifierId id in Enum.GetValues(typeof(BossModifierId)))
            {
                var iconName = BossService.GetIconName(id);
                AssertNonEmpty(iconName, $"BossModifierId.{id}");
                AssertRuntimePngExists($"{BossService.GetIconFolder(id)}/icon_{iconName}", $"BossModifierId.{id}");
            }
        }

        [Test]
        public void ClassIcons_AllMappedIdsResolveToRuntimeSprites()
        {
            foreach (ClassId id in Enum.GetValues(typeof(ClassId)))
            {
                var iconName = ClassCatalog.GetIconName(id);
                AssertNonEmpty(iconName, $"ClassId.{id}");
                AssertRuntimePngExists($"class/icon_{iconName}", $"ClassId.{id}");
            }
        }

        [Test]
        public void CurseIcons_AllCatalogIdsResolveToRuntimeSprites()
        {
            foreach (var curseId in GetCurseIds())
            {
                var iconName = CurseService.GetIconName(curseId);
                AssertNonEmpty(iconName, $"Curse.{curseId}");
                AssertRuntimePngExists($"cursed/icon_{iconName}", $"Curse.{curseId}");
            }
        }

        [Test]
        public void RuntimeResourceValidationDocument_TracksDynamicIconCoverage()
        {
            var doc = File.ReadAllText(ValidationDocPath);

            Assert.That(doc, Does.Contain("ASSET-003"));
            Assert.That(doc, Does.Contain("ASSET-007"));
            Assert.That(doc, Does.Contain("ItemService.GetIconName"));
            Assert.That(doc, Does.Contain("RelicService.GetIconName"));
            Assert.That(doc, Does.Contain("BossService.GetIconName"));
            Assert.That(doc, Does.Contain("CurseService.GetIconName"));
            Assert.That(doc, Does.Contain("ClassCatalog.GetIconName"));
        }

        [Test]
        public void LiteralResourcesLoadCalls_DirectStringPathsResolveToRuntimeFiles()
        {
            var literalPaths = Directory
                .GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                .SelectMany(path => LiteralResourcesLoadRegex
                    .Matches(File.ReadAllText(path))
                    .Cast<Match>()
                    .Select(match => new { Source = path, ResourcePath = match.Groups[1].Value }))
                .Distinct()
                .OrderBy(entry => entry.ResourcePath)
                .ToArray();

            Assert.That(literalPaths.Length, Is.GreaterThan(0), "No literal Resources.Load paths were found.");
            foreach (var entry in literalPaths)
                AssertRuntimeResourceExists(entry.ResourcePath, entry.Source);
        }

        private static IEnumerable<string> GetCurseIds()
        {
            const string prefix = "Curse.";
            const string suffix = ".Name";

            return CurseService
                .GetLocalizationKeys()
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal) &&
                              key.EndsWith(suffix, StringComparison.Ordinal))
                .Select(key => key.Substring(prefix.Length, key.Length - prefix.Length - suffix.Length))
                .Distinct()
                .OrderBy(id => id);
        }

        private static void AssertRuntimePngExists(string resourcePath, string owner)
        {
            var normalized = resourcePath.Replace('\\', '/');
            var assetPath = Path.Combine(ResourcesRoot, normalized.Replace('/', Path.DirectorySeparatorChar)) + ".png";
            Assert.IsTrue(
                File.Exists(assetPath),
                $"{owner} maps to missing runtime sprite Resources/{normalized}.png");
            Assert.IsTrue(
                File.Exists(assetPath + ".meta"),
                $"{owner} maps to Resources/{normalized}.png but the Unity .meta file is missing.");
        }

        private static void AssertRuntimeResourceExists(string resourcePath, string source)
        {
            var normalized = resourcePath.Replace('\\', '/');
            var assetBasePath = Path.Combine(ResourcesRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
            var candidates = new[]
            {
                ".png", ".wav", ".mp3", ".ogg", ".aiff", ".flac", ".ttf", ".otf", ".json", ".csv"
            };

            foreach (var extension in candidates)
            {
                var candidate = assetBasePath + extension;
                if (File.Exists(candidate))
                {
                    Assert.IsTrue(
                        File.Exists(candidate + ".meta"),
                        $"{source} loads Resources/{normalized} but {candidate} is missing its Unity .meta file.");
                    return;
                }
            }

            Assert.Fail($"{source} loads missing literal resource path Resources/{normalized}.");
        }

        private static void AssertNonEmpty(string iconName, string owner)
        {
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(iconName),
                $"{owner} has no icon mapping. Add a GetIconName entry or route it to an explicit fallback.");
        }
    }
}
