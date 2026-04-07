using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;

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
                _summaryText.text = $"Classes: {totalClasses}/8  |  Relics: {totalRelics}/23  |  Ascension: {meta.AscensionLevel}";
            }

            if (_contentRoot == null) return;

            for (var i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);

            // Show ALL 8 classes, not just those with progress
            var allClasses = (ClassId[])Enum.GetValues(typeof(ClassId));
            for (var i = 0; i < allClasses.Length; i++)
            {
                var classId = allClasses[i];
                var def = ClassCatalog.GetDefinition(classId);
                var entry = FindEntry(meta, classId);
                var isUnlocked = meta.UnlockedClasses != null && meta.UnlockedClasses.Contains(classId);

                var go = new GameObject(classId.ToString());
                go.transform.SetParent(_contentRoot, false);
                var text = go.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 15;

                if (entry != null)
                {
                    var level = XpTable.DeriveLevel(entry.TotalXp);
                    text.color = UnlockedColor;
                    text.text = $"{def.Name}  —  Level {level}  (Prestige {entry.PrestigeTier})  —  {entry.TotalXp} XP\n" +
                        $"    HP: {def.BaseHP}  Pencil: {def.BasePencil}  Slots: {def.BaseItemSlots}  |  {def.PassiveDescription}";
                }
                else if (isUnlocked)
                {
                    text.color = UnlockedColor;
                    text.text = $"{def.Name}  —  Level 1  —  Not Started\n" +
                        $"    HP: {def.BaseHP}  Pencil: {def.BasePencil}  Slots: {def.BaseItemSlots}  |  {def.PassiveDescription}";
                }
                else
                {
                    text.color = LockedColor;
                    text.text = $"{def.Name}  —  Locked";
                }

                var layout = go.AddComponent<LayoutElement>();
                layout.preferredHeight = entry != null || isUnlocked ? 40 : 24;
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
    }
}
