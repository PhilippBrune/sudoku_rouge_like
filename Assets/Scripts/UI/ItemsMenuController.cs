using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public enum CodexTabId { Items, Relics, Modes, Curses }

    /// <summary>
    /// [REQ: UI-STYLE-018] The Codex as a collector's book (layout-proposals.md, owner pick 2026-10-20): bookmarks for
    /// Items, Relics, Rules and Curses with their counts; the left page shows every entry as a tile (found ones in colour,
    /// the rest as ink silhouettes) with an All / Found / Not yet found filter; the right page shows the chosen entry with
    /// its icon, kinds, rule, where it comes from and when it was found, the goal of the collection and a "found" stamp.
    /// LB/RB (PageUp/PageDown) change the collection, Y (F) the filter.
    /// </summary>
    public sealed class ItemsMenuController : MonoBehaviour
    {
        private enum Filter { All, Found, Unknown }

        private sealed class Entry
        {
            public string Name;
            public string Description;
            public string IconPath;
            public bool Found;
            public List<(string Label, Color Color)> Pills = new List<(string, Color)>();
            public List<(string Label, string Value)> Facts = new List<(string, string)>();
        }

        private static readonly Color TileFound = new Color(0.97f, 0.94f, 0.86f, 1f);
        private static readonly Color TileUnknown = new Color(0.90f, 0.86f, 0.77f, 1f);
        private static readonly Color Silhouette = new Color(0.36f, 0.31f, 0.25f, 0.35f);

        private RectTransform _grid;
        private RectTransform _detail;
        private ScrollRect _scroll;
        private MetaProgressionState _currentMeta;
        private CodexTabId _currentTab = CodexTabId.Items;
        private Filter _filter = Filter.All;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<Button> _tiles = new List<Button>();

        private Button _tabItems, _tabRelics, _tabModes, _tabCurses;
        private Button[] _filterButtons = Array.Empty<Button>();
        private Button _back;
        private Entry _shown;
        private readonly Vector3[] _tileCorners = new Vector3[4];
        private readonly Vector3[] _viewCorners = new Vector3[4];

        /// <summary>The left page's grid (inside a ScrollRect) that holds the tiles.</summary>
        public void Configure(RectTransform contentRoot)
        {
            _grid = contentRoot;
            _scroll = contentRoot != null ? contentRoot.GetComponentInParent<ScrollRect>(true) : null;
        }

        /// <summary>The right page, filled with the chosen entry.</summary>
        public void ConfigureDetail(RectTransform page) => _detail = page;

        public void SetTabButtons(Button items, Button relics, Button modes, Button curses = null)
        {
            _tabItems = items;
            _tabRelics = relics;
            _tabModes = modes;
            _tabCurses = curses;
        }

        /// <summary>Back under the book: the D-pad reaches it from the last row of tiles.</summary>
        public void SetBackButton(Button back) => _back = back;

        public void SetFilterButtons(Button all, Button found, Button unknown)
        {
            _filterButtons = new[] { all, found, unknown };
            for (var i = 0; i < _filterButtons.Length; i++)
            {
                var filter = (Filter)i;
                _filterButtons[i].onClick.RemoveAllListeners();
                _filterButtons[i].onClick.AddListener(() => SetFilter(filter));
            }
        }

        public void ShowTab(CodexTabId tab)
        {
            _currentTab = tab;
            _filter = Filter.All;
            if (_currentMeta != null) Refresh(_currentMeta);
        }

        public void Refresh(MetaProgressionState meta)
        {
            _currentMeta = meta;
            if (_grid == null) return;
            UpdateTabLabels(meta);
            _entries.Clear();
            switch (_currentTab)
            {
                case CodexTabId.Items: AddItems(meta); break;
                case CodexTabId.Relics: AddRelics(meta); break;
                case CodexTabId.Modes: AddRules(meta); break;
                default: AddCurses(meta); break;
            }
            BuildTiles();
            RefreshFilterMarks();
            var first = _entries.FindIndex(Shown);
            ShowDetail(first >= 0 ? _entries[first] : null);
        }

        private void Update()
        {
            if (_grid == null || !_grid.gameObject.activeInHierarchy) return;
            var step = Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.PageDown) ? 1
                : Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.PageUp) ? -1 : 0;
            if (step != 0) TurnTab(step);
            if (Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.F)) CycleFilter();
            KeepSelectionVisible();
        }

        /// <summary>LB/RB (PageUp/PageDown): the previous or next collection, with the focus on its first tile.</summary>
        public void TurnTab(int step)
        {
            ShowTab((CodexTabId)(((int)_currentTab + step % 4 + 4) % 4));
            FocusFirst();
        }

        /// <summary>Y (F): All, Found, Not yet found in turn.</summary>
        public void CycleFilter() => SetFilter((Filter)(((int)_filter + 1) % 3));

        // ── Collections ──────────────────────────────────────────────────────

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, string fallback, params object[] args) =>
            LocalizationService.Format(key, fallback, args);

        private void AddItems(MetaProgressionState meta)
        {
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                var entry = NewEntry(IsItemFound(meta, type, out var date), ItemService.GetItemName(type),
                    "items/icon_" + ItemService.GetIconName(type));
                var rarities = ItemService.IsTiered(type)
                    ? new[] { ItemRarity.Normal, ItemRarity.Rare, ItemRarity.Epic }
                    : new[] { ItemService.GetFixedRarity(type) };
                foreach (var rarity in rarities)
                    entry.Pills.Add((T($"Item.Rarity.{rarity}", rarity.ToString()), RarityColor(rarity)));
                entry.Description = ItemService.GetItemDescription(type, rarities[0]);
                var counts = CodexEntryOf(meta, type);
                // Found at least once when found at all (entries from before the counters have 0).
                entry.Facts.Add((T("Codex.TimesFound", "Found"), Times(Math.Max(1, counts?.TimesFound ?? 0))));
                entry.Facts.Add((T("Codex.TimesUsed", "Used"), Times(counts?.TimesUsed ?? 0)));
                entry.Facts.Add((T("Codex.ComesFrom", "Comes from"), ItemSource(type)));
                if (!string.IsNullOrEmpty(date)) entry.Facts.Add((T("Codex.FoundOn", "Found on"), date));
                _entries.Add(entry);
            }
        }

        private void AddRelics(MetaProgressionState meta)
        {
            foreach (RelicId id in Enum.GetValues(typeof(RelicId)))
            {
                var found = meta?.DiscoveredRelics != null && meta.DiscoveredRelics.Contains(id);
                var entry = NewEntry(found, RelicService.GetRelicName(id),
                    RelicService.GetIconFolder(id) + "/icon_" + RelicService.GetIconName(id));
                var tier = RelicService.GetBaseTier(id);
                entry.Pills.Add((T($"Relic.Tier.{tier}", tier.ToString()), ScrollUiKit.TierColor(tier)));
                entry.Description = RelicService.GetRelicDescription(id);
                _entries.Add(entry);
            }
        }

        private void AddRules(MetaProgressionState meta)
        {
            foreach (var id in ModifierFamilies.All())
            {
                if (!BossService.IsImplementedModifier(id)) continue;
                var found = meta?.DiscoveredBossModifiers != null && meta.DiscoveredBossModifiers.Contains(id);
                var entry = NewEntry(found, BossService.GetModifierName(id),
                    BossService.GetIconFolder(id) + "/icon_" + BossService.GetIconName(id));
                entry.Pills.Add((ModifierFamilies.LocalizedName(ModifierFamilies.FamilyOf(id)), new Color(0.27f, 0.36f, 0.30f, 1f)));
                entry.Description = BossService.GetModifierDescription(id);
                _entries.Add(entry);
            }
        }

        private void AddCurses(MetaProgressionState meta)
        {
            foreach (var curse in CurseService.All)
            {
                var found = meta?.DiscoveredCurses != null && meta.DiscoveredCurses.Contains(curse.Id);
                var entry = NewEntry(found, CurseService.GetLocalizedName(curse), "cursed/icon_" + CurseService.GetIconName(curse.Id));
                entry.Pills.Add((T($"Codex.Severity.{curse.Severity}", curse.Severity.ToString()), SeverityColor(curse.Severity)));
                entry.Description = CurseService.GetLocalizedDescription(curse);
                entry.Facts.Add((T("Codex.FromFloor", "From floor"), (curse.MinFloor + 1).ToString()));
                _entries.Add(entry);
            }
        }

        private static Entry NewEntry(bool found, string name, string icon) =>
            new Entry { Found = found, Name = name, IconPath = icon };

        private static bool IsItemFound(MetaProgressionState meta, ItemType type, out string date)
        {
            var entry = CodexEntryOf(meta, type);
            date = entry?.DiscoveredDate;
            return entry != null;
        }

        private static ItemCodexEntry CodexEntryOf(MetaProgressionState meta, ItemType type)
        {
            if (meta?.ItemCodex?.Entries == null) return null;
            var key = type.ToString();
            foreach (var entry in meta.ItemCodex.Entries)
                if (entry.ItemId == key && entry.Discovered) return entry;
            return null;
        }

        private static string Times(int count) => Math.Max(0, count) switch
        {
            0 => T("Codex.Times.Zero", "not yet"),
            1 => T("Codex.Times.One", "once"),
            _ => F("Codex.Times", "{0} times", count)
        };

        // Where an item comes from, as the pools decide (ItemService, ShopService).
        private static string ItemSource(ItemType type)
        {
            if (CounterItems.IsCounterItem(type)) return T("Codex.Source.Counter", "shops, for the floor's rules");
            if (ItemService.IsClassExclusive(type))
                return F("Codex.Source.Class", "the {0}, from level {1}",
                    LocalizationService.T(ClassCatalog.GetDefinition(ItemService.GetExclusiveClass(type)).Name),
                    ItemService.GetExclusiveUnlockLevel(type));
            return type switch
            {
                ItemType.GardenRake or ItemType.OfferingBowl or ItemType.PruningShears or ItemType.ZenSandSifter
                    => T("Codex.Source.Events", "events and shops"),
                ItemType.BambooCompass => T("Codex.Source.Harmony", "Harmony 3 and higher"),
                _ => T("Codex.Source.Rewards", "rewards and shops")
            };
        }

        private static Color RarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Epic => new Color(0.56f, 0.36f, 0.62f, 1f),
            ItemRarity.Rare => new Color(0.20f, 0.42f, 0.68f, 1f),
            _ => new Color(0.42f, 0.40f, 0.37f, 1f)
        };

        private static Color SeverityColor(CurseSeverity severity) => severity switch
        {
            CurseSeverity.Nasty => new Color(0.55f, 0.13f, 0.12f, 1f),
            CurseSeverity.Medium => new Color(0.72f, 0.42f, 0.13f, 1f),
            _ => new Color(0.45f, 0.42f, 0.36f, 1f)
        };

        // ── Tiles ────────────────────────────────────────────────────────────

        private bool Shown(Entry entry) => _filter == Filter.All || (_filter == Filter.Found) == entry.Found;

        private void BuildTiles()
        {
            // A rebuild destroys the tile the controller is on: the focus then moves to the new first tile (review HIGH).
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var hadTileFocus = selected != null && selected.transform.parent == _grid;
            ScrollUiKit.DestroyChildrenNow(_grid);
            _tiles.Clear();
            foreach (var entry in _entries)
            {
                if (!Shown(entry)) continue;
                var index = _tiles.Count;
                var tile = ScrollUiKit.Fill(_grid, $"CodexTile_{index}", Vector2.zero, Vector2.one,
                    entry.Found ? TileFound : TileUnknown, ScrollUiKit.Rounded(8f));
                tile.raycastTarget = true;
                ScrollUiKit.InkFrame(tile.transform, new Color(0.33f, 0.29f, 0.24f, 0.45f), 1.5f);
                var icon = ScrollUiKit.Icon(tile.transform, "Icon", entry.IconPath, new Vector2(0.22f, 0.32f),
                    new Vector2(0.78f, 0.90f), entry.Found ? Color.white : Silhouette);
                if (icon != null) icon.preserveAspect = true;
                ScrollUiKit.Label(tile.transform, "Name", entry.Found ? entry.Name : T("Codex.Unknown", "???"), 15,
                    TextAnchor.MiddleCenter, OptionsUiStyle.PaperInk, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.30f), 10);
                var button = tile.gameObject.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.93f, 0.78f, 1f);
                colors.selectedColor = new Color(0.98f, 0.80f, 0.62f, 1f);
                colors.pressedColor = new Color(0.92f, 0.76f, 0.58f, 1f);
                button.colors = colors;
                var shown = entry;
                button.onClick.AddListener(() => ShowDetail(shown));
                tile.gameObject.AddComponent<CodexTileFocus>().OnFocus = () => ShowDetail(shown);
                _tiles.Add(button);
            }
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
            WireNavigation();
            if (hadTileFocus) FocusFirst();
        }

        // Explicit D-pad paths: bookmarks, filters, the tile grid, Back. The grid is taller than its page, so
        // automatic navigation never left it and Back could not be reached (UI-STYLE-018 test).
        private void WireNavigation()
        {
            var layout = _grid.GetComponent<GridLayoutGroup>();
            var columns = layout != null && layout.constraintCount > 0 ? layout.constraintCount : 5;
            var bookmarks = new List<Button>();
            foreach (var tab in new[] { _tabItems, _tabRelics, _tabModes, _tabCurses })
                if (tab != null) bookmarks.Add(tab);
            var activeTab = TabButton(_currentTab);
            var filters = new List<Button>();
            foreach (var filter in _filterButtons)
                if (filter != null) filters.Add(filter);
            Selectable firstTile = _tiles.Count > 0 ? _tiles[0] : _back;
            Selectable activeFilter = (int)_filter < filters.Count ? filters[(int)_filter] : null;
            var aboveTiles = activeFilter != null ? activeFilter : activeTab;

            for (var i = 0; i < bookmarks.Count; i++)
                SetNavigation(bookmarks[i], null, activeFilter != null ? activeFilter : firstTile,
                    i > 0 ? bookmarks[i - 1] : null, i + 1 < bookmarks.Count ? bookmarks[i + 1] : null);
            for (var i = 0; i < filters.Count; i++)
                SetNavigation(filters[i], activeTab, firstTile,
                    i > 0 ? filters[i - 1] : null, i + 1 < filters.Count ? filters[i + 1] : null);

            var lastRowStart = _tiles.Count == 0 ? 0 : (_tiles.Count - 1) / columns * columns;
            for (var i = 0; i < _tiles.Count; i++)
            {
                var column = i % columns;
                Selectable down = i >= lastRowStart ? _back : _tiles[Math.Min(i + columns, _tiles.Count - 1)];
                Selectable up = i < columns ? aboveTiles : _tiles[i - columns];
                SetNavigation(_tiles[i], up, down,
                    column > 0 ? _tiles[i - 1] : null,
                    column + 1 < columns && i + 1 < _tiles.Count ? _tiles[i + 1] : null);
            }

            if (_back != null)
                SetNavigation(_back, _tiles.Count > 0 ? _tiles[lastRowStart] : aboveTiles, null, null, null);
        }

        private Button TabButton(CodexTabId tab) => tab switch
        {
            CodexTabId.Items => _tabItems,
            CodexTabId.Relics => _tabRelics,
            CodexTabId.Modes => _tabModes,
            _ => _tabCurses
        };

        private static void SetNavigation(Selectable selectable, Selectable up, Selectable down, Selectable left,
            Selectable right)
        {
            if (selectable == null) return;
            selectable.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }

        /// <summary>Focuses the first tile (the entry the right page shows); with no tiles (an empty filter) the active
        /// filter, else Back. The Codex opens with it, so a controller starts on the collection, not on a filter.</summary>
        public void FocusFirst()
        {
            var eventSystem = EventSystem.current;   // no ?. on a UnityEngine.Object (fake null)
            if (eventSystem == null || !_grid.gameObject.activeInHierarchy) return;
            Selectable target = _tiles.Count > 0 ? _tiles[0] : null;
            if (target == null && (int)_filter < _filterButtons.Length) target = _filterButtons[(int)_filter];
            if (target == null) target = _back;
            if (target != null) eventSystem.SetSelectedGameObject(target.gameObject);
        }

        private void SetFilter(Filter filter)
        {
            _filter = filter;
            BuildTiles();
            RefreshFilterMarks();
            var first = _entries.FindIndex(Shown);
            ShowDetail(first >= 0 ? _entries[first] : null);
        }

        private void RefreshFilterMarks()
        {
            for (var i = 0; i < _filterButtons.Length; i++)
            {
                var label = _filterButtons[i] != null ? _filterButtons[i].GetComponentInChildren<Text>() : null;
                if (label == null) continue;
                label.color = i == (int)_filter ? OptionsUiStyle.PaperVermilion : OptionsUiStyle.PaperInkSoft;
                label.fontStyle = i == (int)_filter ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        // The grid scrolls so the tile the controller is on stays in view.
        private void KeepSelectionVisible()
        {
            if (_scroll == null || EventSystem.current == null) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || selected.transform.parent != _grid) return;
            var viewport = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
            ((RectTransform)selected.transform).GetWorldCorners(_tileCorners);
            viewport.GetWorldCorners(_viewCorners);
            var scale = _grid.lossyScale.y > 0f ? _grid.lossyScale.y : 1f;
            if (_tileCorners[0].y < _viewCorners[0].y)
                _grid.anchoredPosition += new Vector2(0f, (_viewCorners[0].y - _tileCorners[0].y) / scale);
            else if (_tileCorners[1].y > _viewCorners[1].y)
                _grid.anchoredPosition -= new Vector2(0f, (_tileCorners[1].y - _viewCorners[1].y) / scale);
        }

        // ── Tabs ─────────────────────────────────────────────────────────────

        private void UpdateTabLabels(MetaProgressionState meta)
        {
            var itemTotal = Enum.GetValues(typeof(ItemType)).Length;
            var itemsFound = 0;
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
                if (IsItemFound(meta, type, out _)) itemsFound++;
            var relicTotal = Enum.GetValues(typeof(RelicId)).Length;
            var relicsFound = meta?.DiscoveredRelics?.Count ?? 0;
            var ruleTotal = 0;
            var rulesFound = 0;
            foreach (var id in ModifierFamilies.All())
            {
                if (!BossService.IsImplementedModifier(id)) continue;
                ruleTotal++;
                if (meta?.DiscoveredBossModifiers != null && meta.DiscoveredBossModifiers.Contains(id)) rulesFound++;
            }
            var curseTotal = CurseService.All.Count;
            var cursesFound = 0;
            foreach (var curse in CurseService.All)
                if (meta?.DiscoveredCurses != null && meta.DiscoveredCurses.Contains(curse.Id)) cursesFound++;

            SetTab(_tabItems, F("Codex.Tab.Items", "Items {0}/{1}", itemsFound, itemTotal), _currentTab == CodexTabId.Items);
            SetTab(_tabRelics, F("Codex.Tab.Relics", "Relics {0}/{1}", relicsFound, relicTotal), _currentTab == CodexTabId.Relics);
            SetTab(_tabModes, F("Codex.Tab.Rules", "Rules {0}/{1}", rulesFound, ruleTotal), _currentTab == CodexTabId.Modes);
            SetTab(_tabCurses, F("Codex.Tab.Curses", "Curses {0}/{1}", cursesFound, curseTotal), _currentTab == CodexTabId.Curses);
        }

        private static void SetTab(Button tab, string label, bool active)
        {
            if (tab == null) return;
            var text = tab.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
                text.color = active ? OptionsUiStyle.PaperInk : new Color(0.97f, 0.92f, 0.80f, 1f);
            }
            var colors = tab.colors;
            colors.normalColor = active ? new Color(0.94f, 0.90f, 0.81f, 1f) : new Color(0.47f, 0.31f, 0.17f, 1f);
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.12f);
            colors.selectedColor = Color.Lerp(colors.normalColor, OptionsUiStyle.PaperFocus, 0.6f);
            tab.colors = colors;
            var img = tab.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }

        // ── Right page ───────────────────────────────────────────────────────

        private void ShowDetail(Entry entry)
        {
            if (_detail == null) return;
            // A click selects the tile (OnSelect) and clicks it (onClick): build the page once.
            if (entry != null && ReferenceEquals(entry, _shown) && _detail.childCount > 0) return;
            _shown = entry;
            ScrollUiKit.DestroyChildrenNow(_detail);
            if (entry == null)
            {
                ScrollUiKit.Label(_detail, "CodexEmpty", T("Codex.Empty", "Nothing here yet."), 22, TextAnchor.MiddleCenter,
                    OptionsUiStyle.PaperInkSoft, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f));
                BuildGoal();
                return;
            }

            var disc = ScrollUiKit.Spot(_detail, "CodexIconDisc", new Vector2(0.17f, 0.80f), new Vector2(190f, 190f),
                new Color(0.93f, 0.85f, 0.70f, 1f), ScrollUiKit.Disc());
            var icon = ScrollUiKit.Icon(disc.transform, "Icon", entry.IconPath, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f),
                entry.Found ? Color.white : Silhouette);
            if (icon != null) icon.preserveAspect = true;
            ScrollUiKit.Label(_detail, "CodexName", entry.Found ? entry.Name : T("Codex.Unknown", "???"), 44, TextAnchor.MiddleLeft,
                OptionsUiStyle.PaperInk, new Vector2(0.34f, 0.80f), new Vector2(0.97f, 0.92f), 22);

            var x = 0.34f;
            for (var i = 0; entry.Found && i < entry.Pills.Count; i++)   // an unknown entry keeps its kind hidden too
            {
                var (label, color) = entry.Pills[i];
                var width = Mathf.Clamp(0.03f + label.Length * 0.0105f, 0.08f, 0.30f);
                ScrollUiKit.Pill(_detail, $"CodexPill_{i}", new Vector2(x, 0.735f), new Vector2(x + width, 0.785f),
                    label, entry.Found ? color : new Color(0.55f, 0.51f, 0.45f, 1f), new Color(0.99f, 0.96f, 0.90f, 1f), 14);
                x += width + 0.012f;
            }

            var rule = ScrollUiKit.Label(_detail, "CodexRule",
                entry.Found ? entry.Description : T("Codex.NotYetDiscovered", "Not yet discovered"), 22, TextAnchor.UpperLeft,
                OptionsUiStyle.PaperInk, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.645f), 13);   // under the icon disc
            rule.verticalOverflow = VerticalWrapMode.Truncate;

            if (entry.Found)
                for (var i = 0; i < entry.Facts.Count; i++)
                {
                    var y = 0.46f - i * 0.065f;   // the last of four rows ends above the goal and the stamp
                    ScrollUiKit.ValueRow(_detail, $"CodexFact_{i}", new Vector2(0.05f, y - 0.03f), new Vector2(0.95f, y + 0.03f),
                        entry.Facts[i].Label, entry.Facts[i].Value, 20, OptionsUiStyle.PaperInk);
                }
            BuildGoal();
        }

        // The collection's goal and the "found" stamp.
        private void BuildGoal()
        {
            var total = _entries.Count;
            var found = 0;
            foreach (var entry in _entries)
                if (entry.Found) found++;
            var unknown = total - found;
            string goal;
            if (_currentTab == CodexTabId.Items)
            {
                // The codex unlock leaves out the counter items, which only shops sell (ITEM-COUNTER-007).
                var required = 0;
                var requiredFound = 0;
                foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
                {
                    if (CounterItems.IsCounterItem(type)) continue;
                    required++;
                    if (IsItemFound(_currentMeta, type, out _)) requiredFound++;
                }
                var className = LocalizationService.T(ClassCatalog.GetDefinition(ClassId.ReedDuelist).Name);
                goal = requiredFound >= required
                    ? F("Codex.Goal.ItemsDone", "Every item found: the {0} is yours.", className)
                    : F("Codex.Goal.Items", "Find every item to unlock the {0}. {1} still unknown.", className, required - requiredFound);
            }
            else
                goal = unknown == 0 ? T("Codex.Goal.Complete", "All found.") : F("Codex.Goal.Other", "{0} still unknown.", unknown);
            ScrollUiKit.Label(_detail, "CodexGoal", goal, 18, TextAnchor.UpperLeft, OptionsUiStyle.PaperInkSoft,
                new Vector2(0.05f, 0.04f), new Vector2(0.66f, 0.17f), 12);

            var stamp = ScrollUiKit.Spot(_detail, "CodexStamp", new Vector2(0.83f, 0.12f), new Vector2(150f, 130f),
                OptionsUiStyle.PaperVermilion, ScrollUiKit.Rounded(10f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            ScrollUiKit.Label(stamp.transform, "Word", T("Codex.Stamp", "Found").ToUpperInvariant(), 16, TextAnchor.MiddleCenter,
                new Color(0.99f, 0.93f, 0.85f, 1f), new Vector2(0f, 0.62f), new Vector2(1f, 0.86f));
            ScrollUiKit.Label(stamp.transform, "Percent", (total > 0 ? Mathf.RoundToInt(found * 100f / total) : 0) + "%", 40,
                TextAnchor.MiddleCenter, new Color(0.99f, 0.93f, 0.85f, 1f), new Vector2(0f, 0.12f), new Vector2(1f, 0.64f), 20);
        }
    }

    /// <summary>Shows a Codex tile's entry when the controller's focus lands on it.</summary>
    public sealed class CodexTileFocus : MonoBehaviour, ISelectHandler
    {
        public Action OnFocus;
        public void OnSelect(BaseEventData eventData) => OnFocus?.Invoke();
    }
}
