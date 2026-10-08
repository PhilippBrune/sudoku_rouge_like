using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.UI
{
    public sealed class MetaProgressionPanelController : MonoBehaviour
    {
        private RectTransform _contentRoot;
        private Text _summaryText;
        private ClassId? _pendingPrestige;
        private Text _pendingLabel;
        private readonly ClassGardenProgressionService _garden = new ClassGardenProgressionService();

        /// <summary>Prestiges the class and refreshes the panel. Set by MainMenuController (GAP-017).</summary>
        public Action<ClassId> PrestigeRequested;

        private static readonly Color UnlockedColor = new Color(0.92f, 0.96f, 0.89f, 1f);
        private static readonly Color LockedColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        private static readonly Color PrestigeButtonColor = new Color(0.45f, 0.33f, 0.12f, 0.92f);

        public void Configure(RectTransform contentRoot, Text summaryText)
        {
            _contentRoot = contentRoot;
            _summaryText = summaryText;
        }

        public void Refresh(MetaProgressionState meta)
        {
            if (meta == null) meta = new MetaProgressionState();
            _pendingPrestige = null;
            _pendingLabel = null;

            if (_summaryText != null)
            {
                var totalClasses = meta.UnlockedClasses?.Count ?? 0;
                var totalRelics = meta.DiscoveredRelics?.Count ?? 0;
                _summaryText.text = F(
                    "MetaProgression.Summary",
                    "Classes: {0}/{1}  |  Relics: {2}/{3}  |  Prestige: {4}",
                    totalClasses,
                    ClassCatalog.GetAll().Length,
                    totalRelics,
                    Enum.GetValues(typeof(RelicId)).Length,
                    TotalPrestige(meta));
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
                    var prestigeLine = PrestigeStatus(meta, entry, level);
                    if (prestigeLine.Length > 0) text.text += "\n" + prestigeLine;
                    if (_garden.CanPrestige(meta, classId)) AddPrestigeButton(go.transform, classId);
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
                layout.preferredHeight = entry != null && XpTable.DeriveLevel(entry.TotalXp) >= XpTable.MaxLevel ? 80
                    : entry != null || isUnlocked ? 60 : 24;
            }
        }

        // [REQ: META-PRESTIGE-002] [REQ: META-PRESTIGE-003] Level-40 classes show what prestige still needs.
        private static string PrestigeStatus(MetaProgressionState meta, ClassGardenProgressEntry entry, int level)
        {
            if (level < XpTable.MaxLevel) return string.Empty;
            if (entry.PrestigeTier >= ClassGardenProgressionService.MaxPrestigeTier)
                return T("MetaProgression.Prestige.Max", "    Highest prestige reached.");
            var required = ClassGardenProgressionService.RequiredBossesForPrestige(entry.PrestigeTier);
            var defeated = meta.ClassUnlocks?.BossesDefeated ?? 0;
            return defeated >= required
                ? T("MetaProgression.Prestige.Ready", "    Prestige ready: the level resets to 1 and the prestige tier rises.")
                : F("MetaProgression.Prestige.NeedsBosses", "    Prestige needs {0} boss defeats ({1}/{0}).", required, defeated);
        }

        // [REQ: META-PRESTIGE-004] Two-step confirmation: the first press asks, the second prestiges (GAP-017).
        private void AddPrestigeButton(Transform row, ClassId classId)
        {
            var go = new GameObject("BtnPrestige_" + classId, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(row, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.70f, 0.02f);
            rt.anchorMax = new Vector2(1.00f, 0.30f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = PrestigeButtonColor;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.font = FontAssetService.GetFont();
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnlockedColor;
            label.text = T("MetaProgression.Prestige.Button", "Prestige");

            // The label changes in place so a gamepad keeps its selection between the two presses.
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_pendingPrestige != classId)
                {
                    if (_pendingLabel != null) _pendingLabel.text = T("MetaProgression.Prestige.Button", "Prestige");
                    _pendingPrestige = classId;
                    _pendingLabel = label;
                    label.text = T("MetaProgression.Prestige.Confirm", "Confirm: reset to Level 1?");
                    return;
                }
                _pendingPrestige = null;
                _pendingLabel = null;
                PrestigeRequested?.Invoke(classId);
            });
        }

        private static int TotalPrestige(MetaProgressionState meta)
        {
            var total = 0;
            if (meta?.GardenProgression?.ClassEntries == null) return total;
            foreach (var entry in meta.GardenProgression.ClassEntries)
                total += entry.PrestigeTier;
            return total;
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
