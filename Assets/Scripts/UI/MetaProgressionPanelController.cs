using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.UI
{
    public sealed class MetaProgressionPanelController : MonoBehaviour
    {
        private RectTransform _contentRoot;
        private Text _summaryText;

        private static readonly Color UnlockedColor = new Color(0.92f, 0.96f, 0.89f, 1f);
        private static readonly Color LockedColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);

        public void Configure(RectTransform contentRoot, Text summaryText)
        {
            _contentRoot = contentRoot;
            _summaryText = summaryText;
        }

        public void Refresh(MetaProgressionState meta)
        {
            if (meta == null) meta = new MetaProgressionState();

            if (_summaryText != null)
            {
                var totalClasses = meta.UnlockedClasses?.Count ?? 0;
                var totalRelics = meta.DiscoveredRelics?.Count ?? 0;
                _summaryText.text = F(
                    "MetaProgression.Summary",
                    "Classes: {0}/{1}  |  Relics: {2}/{3}  |  Ascension: {4}",
                    totalClasses,
                    ClassCatalog.GetAll().Length,
                    totalRelics,
                    Enum.GetValues(typeof(RelicId)).Length,
                    meta.AscensionLevel);
            }

            if (_contentRoot == null) return;

            for (var i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);

            var allClasses = (ClassId[])Enum.GetValues(typeof(ClassId));
            for (var i = 0; i < allClasses.Length; i++)
            {
                var classId = allClasses[i];
                var def = ClassCatalog.GetDefinition(classId);
                var className = T(def.Name, def.Name);
                var passive = T($"Class.Passive.{def.Id}", def.PassiveDescription);
                var entry = FindEntry(meta, classId);
                var isUnlocked = meta.UnlockedClasses != null && meta.UnlockedClasses.Contains(classId);

                var go = new GameObject(classId.ToString());
                go.transform.SetParent(_contentRoot, false);
                var text = go.AddComponent<Text>();
                text.font = FontAssetService.GetFont();
                text.fontSize = 15;

                if (entry != null)
                {
                    var level = XpTable.DeriveLevel(entry.TotalXp);
                    var exItem = ItemService.GetExclusiveItemForClass(classId);
                    var exRelic = RelicService.GetExclusiveRelicForClass(classId);
                    var itemStr = exItem.HasValue
                        ? (level >= 15 ? ItemService.GetItemName(exItem.Value) : T("MetaProgression.Unknown", "???"))
                        : T("MetaProgression.None", "-");
                    var relicStr = exRelic.HasValue
                        ? (level >= 30 ? RelicService.GetRelicName(exRelic.Value) : T("MetaProgression.Unknown", "???"))
                        : T("MetaProgression.None", "-");

                    text.color = UnlockedColor;
                    text.text = F("MetaProgression.Class.Progress", "{0} - Level {1} (Prestige {2}) - {3} XP", className, level, entry.PrestigeTier, entry.TotalXp) + "\n" +
                        F("MetaProgression.Class.Stats", "    HP: {0}  Pencil: {1}  Slots: {2}  |  {3}", def.BaseHP, def.BasePencil, def.BaseItemSlots, passive) + "\n" +
                        F("MetaProgression.Class.Exclusive", "    L15 Exclusive: {0}  |  L30 Exclusive: {1}", itemStr, relicStr);
                }
                else if (isUnlocked)
                {
                    var exItem = ItemService.GetExclusiveItemForClass(classId);
                    var exRelic = RelicService.GetExclusiveRelicForClass(classId);
                    var itemStr = exItem.HasValue ? T("MetaProgression.Unknown", "???") : T("MetaProgression.None", "-");
                    var relicStr = exRelic.HasValue ? T("MetaProgression.Unknown", "???") : T("MetaProgression.None", "-");

                    text.color = UnlockedColor;
                    text.text = F("MetaProgression.Class.NotStarted", "{0} - Level 1 - Not Started", className) + "\n" +
                        F("MetaProgression.Class.Stats", "    HP: {0}  Pencil: {1}  Slots: {2}  |  {3}", def.BaseHP, def.BasePencil, def.BaseItemSlots, passive) + "\n" +
                        F("MetaProgression.Class.Exclusive", "    L15 Exclusive: {0}  |  L30 Exclusive: {1}", itemStr, relicStr);
                }
                else
                {
                    text.color = LockedColor;
                    text.text = F("MetaProgression.Class.Locked", "{0} - Locked", className);
                }

                var layout = go.AddComponent<LayoutElement>();
                layout.preferredHeight = entry != null || isUnlocked ? 60 : 24;
            }
        }

        private static ClassGardenProgressEntry FindEntry(MetaProgressionState meta, ClassId classId)
        {
            if (meta?.GardenProgression?.ClassEntries == null) return null;
            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                if (meta.GardenProgression.ClassEntries[i].ClassId == classId)
                    return meta.GardenProgression.ClassEntries[i];
            }
            return null;
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, string fallback, params object[] args) =>
            LocalizationService.Format(key, fallback, args);
    }
}
