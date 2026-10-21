using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // Translation tooling (known-issues plan 2, step 1): many content keys are built at runtime and their English lives
    // in data, so the export for translators collects key -> English by calling every content getter with English
    // active. Writes tools/localization/english_sources.json for tools/localization/export_missing.py.
    // Run on demand: -TestFilter SudokuRoguelike.Tests.TranslationSourceExportDiagnostics
    [TestFixture]
    [Explicit("Writes the English source file for the translation export; run on demand.")]
    public sealed class TranslationSourceExportDiagnostics
    {
        [Test]
        public void DumpEnglishSources()
        {
            var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var previous = LocalizationService.Current;
            LocalizationService.SetLanguage(LanguageOption.English);
            LocalizationService.EnglishLookupObserver = (key, english) => sources[key] = english;
            try
            {
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

                foreach (BossModifierId id in Enum.GetValues(typeof(BossModifierId)))
                {
                    BossService.GetModifierName(id);
                    BossService.GetModifierDescription(id);
                }

                for (var floor = 0; floor < 5; floor++)
                {
                    foreach (NodeType node in Enum.GetValues(typeof(NodeType)))
                        ParkNarrativeService.GetNodeFlavor(node, floor);
                    ParkNarrativeService.GetFloorEntryFlavor(floor);
                    ParkNarrativeService.GetBossGateFlavor(floor);
                }

                foreach (ClassId classId in Enum.GetValues(typeof(ClassId)))
                {
                    ParkNarrativeService.GetClassIntroFlavor(classId);
                    ParkNarrativeService.GetRunEndFlavor(true, classId);
                    ParkNarrativeService.GetRunEndFlavor(false, classId);
                }
            }
            finally
            {
                LocalizationService.EnglishLookupObserver = null;
                LocalizationService.SetLanguage(previous);
            }

            // Keys whose English is passed in by UI code from data rather than looked up by a service.
            foreach (var def in ClassCatalog.GetAll())
                sources[$"Class.Passive.{def.Id}"] = def.PassiveDescription;
            foreach (HarmonyPerkId perk in Enum.GetValues(typeof(HarmonyPerkId)))
            {
                sources[$"Harmony.Perk.Name.{perk}"] = HarmonyDifficultyService.GetPerkDisplayName(perk);
                sources[$"Harmony.Perk.Description.{perk}"] = HarmonyDifficultyService.GetPerkDescription(perk);
            }

            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "localization", "english_sources.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ToJson(sources), new UTF8Encoding(false));
            Debug.Log($"[TranslationSources] {sources.Count} keys -> {path}");
            Assert.Greater(sources.Count, 300);
        }

        private static string ToJson(IDictionary<string, string> map)
        {
            var sb = new StringBuilder("{\n");
            var first = true;
            foreach (var pair in map)
            {
                if (!first) sb.Append(",\n");
                first = false;
                sb.Append("  ").Append(Quote(pair.Key)).Append(": ").Append(Quote(pair.Value ?? string.Empty));
            }
            return sb.Append("\n}\n").ToString();
        }

        private static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
