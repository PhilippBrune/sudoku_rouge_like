using System;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.UI
{
    public enum CodexTabId { Items, Relics, Modes }

    public sealed class ItemsMenuController : MonoBehaviour
    {
        private RectTransform _contentRoot;
        private MetaProgressionState _currentMeta;
        private CodexTabId _currentTab = CodexTabId.Items;

        private Button _tabItems;
        private Button _tabRelics;
        private Button _tabModes;

        private static readonly Color HeaderColor      = new Color(0.98f, 0.83f, 0.26f, 1f);
        private static readonly Color DiscoveredColor  = new Color(0.92f, 0.96f, 0.89f, 1f);
        private static readonly Color UndiscoveredColor= new Color(0.55f, 0.55f, 0.55f, 0.70f);
        private static readonly Color SubtextColor     = new Color(0.70f, 0.72f, 0.68f, 1f);

        private static readonly Color TabActiveColor   = new Color(0.98f, 0.83f, 0.26f, 1f);
        private static readonly Color TabInactiveColor = new Color(0.30f, 0.30f, 0.35f, 0.80f);

        public void Configure(RectTransform contentRoot)
        {
            _contentRoot = contentRoot;
        }

        public void SetTabButtons(Button items, Button relics, Button modes)
        {
            _tabItems  = items;
            _tabRelics = relics;
            _tabModes  = modes;
        }

        public void ShowTab(CodexTabId tab)
        {
            _currentTab = tab;
            RefreshTabHighlights();
            if (_currentMeta != null) Refresh(_currentMeta);
        }

        public void Refresh(MetaProgressionState meta)
        {
            _currentMeta = meta;
            if (_contentRoot == null) return;

            for (var i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);

            switch (_currentTab)
            {
                case CodexTabId.Items:   BuildItemsSection(meta);  break;
                case CodexTabId.Relics:  BuildRelicsSection(meta); break;
                case CodexTabId.Modes:   BuildModesSection(meta);  break;
            }
        }

        // ── Tab sections ──────────────────────────────────────────────────────

        private void BuildItemsSection(MetaProgressionState meta)
        {
            var itemTypes = (ItemType[])Enum.GetValues(typeof(ItemType));
            var discoveredCount = 0;
            for (var j = 0; j < itemTypes.Length; j++)
                if (IsDiscovered(meta, itemTypes[j])) discoveredCount++;

            AddHeader(F("Codex.Header.Items", "Items ({0}/{1})", discoveredCount, itemTypes.Length));

            for (var j = 0; j < itemTypes.Length; j++)
            {
                var type = itemTypes[j];
                var discovered = IsDiscovered(meta, type);
                if (discovered)
                    AddIconEntry(ItemService.GetIconName(type),
                        ItemService.GetItemName(type),
                        ItemService.GetItemDescription(type, ItemRarity.Normal),
                        discovered: true, subfolder: "items");
                else
                    AddIconEntry("", T("Codex.Unknown", "???"), T("Codex.NotYetDiscovered", "Not yet discovered"), discovered: false);
            }
        }

        private void BuildRelicsSection(MetaProgressionState meta)
        {
            var relicIds = (RelicId[])Enum.GetValues(typeof(RelicId));
            var discoveredCount = meta?.DiscoveredRelics?.Count ?? 0;
            AddHeader(F("Codex.Header.Relics", "Relics ({0}/{1})", discoveredCount, relicIds.Length));

            for (var j = 0; j < relicIds.Length; j++)
            {
                var rid = relicIds[j];
                var discovered = meta?.DiscoveredRelics != null && meta.DiscoveredRelics.Contains(rid);
                if (discovered)
                    AddIconEntry(RelicService.GetIconName(rid),
                        RelicService.GetRelicName(rid),
                        RelicService.GetRelicDescription(rid),
                        discovered: true, subfolder: RelicService.GetIconFolder(rid));
                else
                    AddIconEntry("", T("Codex.Unknown", "???"), T("Codex.NotYetDiscovered", "Not yet discovered"), discovered: false);
            }
        }

        private void BuildModesSection(MetaProgressionState meta)
        {
            var modIds = (BossModifierId[])Enum.GetValues(typeof(BossModifierId));
            var discovered = 0;
            var total = 0;
            foreach (var id in modIds)
            {
                if ((int)id >= 94) continue; // skip debuffs/pressure
                total++;
                if (meta?.DiscoveredBossModifiers != null && meta.DiscoveredBossModifiers.Contains(id))
                    discovered++;
            }
            AddHeader(F("Codex.Header.Modes", "Boss Modifiers ({0}/{1})", discovered, total));

            foreach (var id in modIds)
            {
                if ((int)id >= 94) continue; // debuffs are not shown in codex
                var isDiscovered = meta?.DiscoveredBossModifiers != null
                    && meta.DiscoveredBossModifiers.Contains(id);
                if (isDiscovered)
                    AddIconEntry(BossService.GetIconName(id), BossService.GetModifierName(id),
                        BossService.GetModifierDescription(id),
                        discovered: true, subfolder: BossService.GetIconFolder(id));
                else
                    AddIconEntry("", T("Codex.Unknown", "???"), T("Codex.NotYetEncountered", "Not yet encountered"), discovered: false);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshTabHighlights()
        {
            SetTabHighlight(_tabItems,  _currentTab == CodexTabId.Items);
            SetTabHighlight(_tabRelics, _currentTab == CodexTabId.Relics);
            SetTabHighlight(_tabModes,  _currentTab == CodexTabId.Modes);
        }

        private static void SetTabHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? TabActiveColor : TabInactiveColor;
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null) txt.color = active
                ? new Color(0.08f, 0.06f, 0.04f, 1f)
                : new Color(0.80f, 0.78f, 0.68f, 1f);
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, string fallback, params object[] args) =>
            LocalizationService.Format(key, fallback, args);

        private void AddHeader(string text)
        {
            var go = new GameObject("Header_" + text);
            go.transform.SetParent(_contentRoot, false);
            var t = go.AddComponent<Text>();
            t.font = FontAssetService.GetFont();
            t.fontSize = 17;
            t.fontStyle = FontStyle.Bold;
            t.color = HeaderColor;
            t.text = text;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32f;
        }

        private void AddIconEntry(string iconName, string title, string description, bool discovered, string subfolder = "GeneratedIcons")
        {
            var row = new GameObject("Entry_" + title);
            row.transform.SetParent(_contentRoot, false);
            var hGroup = row.AddComponent<HorizontalLayoutGroup>();
            hGroup.childAlignment = TextAnchor.UpperLeft;
            hGroup.spacing = 8f;
            hGroup.padding = new RectOffset(4, 4, 4, 4);
            hGroup.childForceExpandHeight = false;
            hGroup.childForceExpandWidth = false;
            hGroup.childControlHeight = true;
            hGroup.childControlWidth = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 40f;

            // Icon image
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 36f;
            iconLe.preferredHeight = 36f;
            iconLe.minWidth = 36f;
            iconLe.minHeight = 36f;
            iconLe.flexibleHeight = 0f;

            if (discovered && !string.IsNullOrEmpty(iconName))
            {
                var sprite = InRunUiFactory.LoadResourceSprite(subfolder + "/icon_" + iconName);
                if (sprite != null)
                    iconImg.sprite = sprite;
                else
                    iconImg.color = new Color(0.30f, 0.35f, 0.30f, 0.5f);
            }
            else
            {
                iconImg.color = new Color(0.25f, 0.25f, 0.25f, 0.4f);
            }

            // Text column
            var textCol = new GameObject("TextCol");
            textCol.transform.SetParent(row.transform, false);
            var vGroup = textCol.AddComponent<VerticalLayoutGroup>();
            vGroup.childForceExpandWidth = true;
            vGroup.childForceExpandHeight = false;
            vGroup.childControlWidth = true;
            vGroup.childControlHeight = true;
            vGroup.spacing = 2f;
            var textColLe = textCol.AddComponent<LayoutElement>();
            textColLe.flexibleWidth = 1f;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(textCol.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = FontAssetService.GetFont();
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = discovered ? DiscoveredColor : UndiscoveredColor;
            nameText.text = title;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            var nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.minHeight = 18f;

            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(textCol.transform, false);
            var descText = descGo.AddComponent<Text>();
            descText.font = FontAssetService.GetFont();
            descText.fontSize = 12;
            descText.color = discovered ? SubtextColor : UndiscoveredColor;
            descText.text = description;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Overflow;
            var descLe = descGo.AddComponent<LayoutElement>();
            descLe.minHeight = 16f;
        }

        private static bool IsDiscovered(MetaProgressionState meta, ItemType type)
        {
            if (meta?.ItemCodex?.Entries == null) return false;
            var key = type.ToString();
            for (var i = 0; i < meta.ItemCodex.Entries.Count; i++)
            {
                if (meta.ItemCodex.Entries[i].ItemId == key && meta.ItemCodex.Entries[i].Discovered)
                    return true;
            }
            return false;
        }
    }
}
