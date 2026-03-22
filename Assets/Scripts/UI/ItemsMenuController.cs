using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class ItemsMenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private Text completionText;
        [SerializeField] private Text gridText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text tooltipText;
        [SerializeField] private Text filterText;
        [SerializeField] private Image[] iconSlots;
        [SerializeField] private Text[] iconSlotLabels;
        [SerializeField] private RectTransform listContentRoot;
        [SerializeField] private ScrollRect listScrollRect;

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private string _filter = "All";
        private string _sort = "Rarity";
        private int _selectedIndex;
        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private Sprite _fallbackSprite;

        public void Configure(MainMenuController controller, Text completion, Text grid, Text detail, Text tooltip, Text filter, Image[] icons = null, Text[] iconLabels = null, RectTransform listContent = null, ScrollRect listScroll = null)
        {
            mainMenuController = controller;
            completionText = completion;
            gridText = grid;
            detailText = detail;
            tooltipText = tooltip;
            filterText = filter;
            if (icons != null) iconSlots = icons;
            if (iconLabels != null) iconSlotLabels = iconLabels;
            if (listContent != null) listContentRoot = listContent;
            if (listScroll != null) listScrollRect = listScroll;
            RefreshView();
        }

        public void RefreshView()
        {
            EnsureProfileLoaded();
            EnsureSeedEntries();
            if (mainMenuController != null && mainMenuController.DebugEnableAllFeatures)
            {
                MarkAllEntriesDiscovered();
                if (_filter == "Unseen") _filter = "All";
            }

            var filtered = BuildFiltered();
            if (_selectedIndex >= filtered.Count)
                _selectedIndex = Mathf.Max(0, filtered.Count - 1);

            if (completionText != null)
            {
                var discovered = 0;
                var all = _profile.Meta.ItemCodex.Entries.Count;
                for (var i = 0; i < all; i++)
                    if (_profile.Meta.ItemCodex.Entries[i].Discovered) discovered++;
                completionText.text = $"Completion: {discovered} / {all}";
            }

            if (filterText != null)
                filterText.text = $"Filter: {_filter} | Sort: {_sort}";

            if (gridText != null)
                gridText.text = listContentRoot == null ? BuildGridText(filtered) : string.Empty;

            if (detailText != null)
                detailText.text = BuildDetailText(filtered);

            RebuildInteractiveList(filtered);
            RefreshIconGrid(filtered);

            if (tooltipText != null)
            {
                tooltipText.text =
                    "Item Roll: 2-5 slots by star difficulty.\n" +
                    "Nothing-slot grants gold.\n" +
                    "Single relic slot — choose wisely.";
            }

            Persist();
        }

        public void FilterAll() => SetFilter("All");
        public void FilterRelics() => SetFilter("Relics");
        public void FilterConsumables() => SetFilter("Consumables");
        public void FilterLegendary() => SetFilter("Legendary");
        public void FilterUnseen() => SetFilter("Unseen");

        public void SortByRarity()
        {
            _sort = "Rarity";
            RefreshView();
        }

        public void SortByMostUsed()
        {
            _sort = "MostUsed";
            RefreshView();
        }

        public void SortByWinRate()
        {
            _sort = "WinRate";
            RefreshView();
        }

        public void ResetFiltersAndSort()
        {
            _filter = "All";
            _sort = "Rarity";
            _selectedIndex = 0;
            RefreshView();
        }

        public void SelectNext()
        {
            var filtered = BuildFiltered();
            if (filtered.Count == 0) return;
            _selectedIndex = (_selectedIndex + 1) % filtered.Count;
            RefreshView();
        }

        public void SelectPrev()
        {
            var filtered = BuildFiltered();
            if (filtered.Count == 0) return;
            _selectedIndex = (_selectedIndex - 1 + filtered.Count) % filtered.Count;
            RefreshView();
        }

        public void MarkRandomDiscoveredForPrototype()
        {
            EnsureProfileLoaded();
            EnsureSeedEntries();
            for (var i = 0; i < _profile.Meta.ItemCodex.Entries.Count; i++)
            {
                if (!_profile.Meta.ItemCodex.Entries[i].Discovered)
                {
                    _profile.Meta.ItemCodex.Entries[i].Discovered = true;
                    _profile.Meta.ItemCodex.Entries[i].DiscoveredDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    break;
                }
            }
            RefreshView();
        }

        private void SetFilter(string filter)
        {
            _filter = filter;
            _selectedIndex = 0;
            RefreshView();
        }

        private List<ItemCodexEntry> BuildFiltered()
        {
            var output = new List<ItemCodexEntry>();
            var entries = _profile.Meta.ItemCodex.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var item = entries[i];
                if (_filter == "All" || MatchesFilter(item, _filter))
                    output.Add(item);
            }
            ApplySort(output);
            return output;
        }

        private static bool MatchesFilter(ItemCodexEntry item, string filter)
        {
            if (item == null) return false;
            if (filter == "Relics") return string.Equals(item.Type, "Relic", StringComparison.OrdinalIgnoreCase);
            if (filter == "Consumables") return string.Equals(item.Type, "Item", StringComparison.OrdinalIgnoreCase);
            if (filter == "Legendary") return string.Equals(item.RarityTier, "Legendary", StringComparison.OrdinalIgnoreCase);
            if (filter == "Unseen") return !item.Discovered;
            return true;
        }

        private void ApplySort(List<ItemCodexEntry> output)
        {
            if (_sort == "MostUsed")
            {
                output.Sort((a, b) => b.TimesUsed.CompareTo(a.TimesUsed));
                return;
            }
            if (_sort == "WinRate")
            {
                output.Sort((a, b) => WinRate(b).CompareTo(WinRate(a)));
                return;
            }
            output.Sort((a, b) => RarityScore(b.RarityTier).CompareTo(RarityScore(a.RarityTier)));
        }

        private static float WinRate(ItemCodexEntry item)
        {
            if (item == null || item.TimesPicked <= 0) return 0f;
            return (float)item.TimesWon / item.TimesPicked;
        }

        private static int RarityScore(string rarity)
        {
            if (string.Equals(rarity, "Legendary", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(rarity, "Normal", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private void RebuildInteractiveList(List<ItemCodexEntry> filtered)
        {
            if (listContentRoot == null) return;

            for (var i = listContentRoot.childCount - 1; i >= 0; i--)
            {
                var child = listContentRoot.GetChild(i);
                if (child != null && child.name.StartsWith("ItemRow_", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }

            for (var i = 0; i < filtered.Count; i++)
            {
                var entry = filtered[i];
                var row = new GameObject($"ItemRow_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(listContentRoot, false);

                var image = row.GetComponent<Image>();
                var selected = i == _selectedIndex;
                image.color = selected ? new Color(0.27f, 0.36f, 0.43f, 0.94f) : new Color(0.12f, 0.14f, 0.18f, 0.85f);

                var rect = row.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 42f);

                var button = row.GetComponent<Button>();
                var captured = i;
                button.onClick.AddListener(() => { _selectedIndex = captured; RefreshView(); });

                var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(row.transform, false);
                label.rectTransform.anchorMin = new Vector2(0.03f, 0.08f);
                label.rectTransform.anchorMax = new Vector2(0.97f, 0.92f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 13;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;

                var state = entry.Mastered ? "[M]" : entry.Discovered ? "[D]" : "[?]";
                var name = entry.Discovered ? entry.Name : "???";
                label.text = $"{state} {name}   [{entry.RarityTier}]";
            }

            if (listScrollRect != null)
                listScrollRect.verticalNormalizedPosition = Mathf.Clamp01(listScrollRect.verticalNormalizedPosition);
        }

        private void RefreshIconGrid(List<ItemCodexEntry> filtered)
        {
            if (iconSlots == null || iconSlots.Length == 0) return;

            for (var i = 0; i < iconSlots.Length; i++)
            {
                var img = iconSlots[i];
                if (img == null) continue;

                if (i >= filtered.Count)
                {
                    img.enabled = false;
                    if (iconSlotLabels != null && i < iconSlotLabels.Length && iconSlotLabels[i] != null)
                        iconSlotLabels[i].text = string.Empty;
                    continue;
                }

                var item = filtered[i];
                img.enabled = true;
                img.color = item.Discovered ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
                img.sprite = ResolveItemSprite(item);
                img.type = Image.Type.Sliced;
                img.preserveAspect = true;

                if (iconSlotLabels != null && i < iconSlotLabels.Length && iconSlotLabels[i] != null)
                    iconSlotLabels[i].text = item.Discovered ? BuildShortLabel(item.Name) : "???";
            }
        }

        private static string BuildShortLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= 14 ? value : value.Substring(0, 13) + "...";
        }

        private string BuildGridText(List<ItemCodexEntry> filtered)
        {
            if (filtered.Count == 0) return "No items match this filter.";
            var rows = new System.Text.StringBuilder();
            for (var i = 0; i < filtered.Count; i++)
            {
                var item = filtered[i];
                var selected = i == _selectedIndex ? "> " : "  ";
                var state = item.Mastered ? "*" : item.Discovered ? "+" : "?";
                var name = item.Discovered ? item.Name : "???";
                rows.AppendLine($"{selected}[{state}] {name} [{item.RarityTier}]");
            }
            return rows.ToString().TrimEnd();
        }

        private string BuildDetailText(List<ItemCodexEntry> filtered)
        {
            if (filtered.Count == 0) return "Select an item to view details.";
            var item = filtered[Mathf.Clamp(_selectedIndex, 0, filtered.Count - 1)];
            if (!item.Discovered)
            {
                return "Name: ???\nType: ???\nRarity: ???\n" +
                       $"Unlock hint: {item.UnlockCondition}";
            }
            return $"Name: {item.Name}\n" +
                   $"Type: {item.Type}\n" +
                   $"Rarity: {item.RarityTier}\n" +
                   $"Description: {item.Description}\n" +
                   $"Effect: {item.EffectFormula}\n" +
                   $"Synergy: {item.SynergyTags}\n" +
                   $"Discovered: {item.DiscoveredDate}\n" +
                   $"Times used: {item.TimesUsed}\n" +
                   $"Wins with item: {item.TimesWon}\n" +
                   $"Best depth: {item.BestRunDepth}";
        }

        private void EnsureProfileLoaded()
        {
            if (_save.TryLoadProfile(out var envelope))
                _profile.ApplyEnvelope(envelope);
        }

        private void EnsureSeedEntries()
        {
            var codex = _profile.Meta.ItemCodex;
            if (codex.Entries.Count > 0) return;

            // ── Tiered consumable items (8 items × 3 rarities = shown as single entries) ──
            SeedItem(codex, "item_solver", "Solver", "Item", "Normal", "Available from start.",
                "Fill correct cells.", "Tiered: N=1, R=1+1, E=1+2", "Tempo");
            SeedItem(codex, "item_finder", "Finder", "Item", "Normal", "Available from start.",
                "Highlight matching cells.", "Tiered: N=1, R=3, E=2", "Information");
            SeedItem(codex, "item_inkwell", "Ink Well", "Item", "Normal", "Available from start.",
                "Restores Pencil charges.", "Tiered: N=+3, R=+6, E=+10", "Recovery");
            SeedItem(codex, "item_meditation_stone", "Meditation Stone", "Item", "Normal", "Available from start.",
                "Restores HP.", "Tiered: N=+1, R=+2, E=+3", "Recovery");
            SeedItem(codex, "item_wind_chime", "Wind Chime", "Item", "Normal", "Available from start.",
                "Undo last mistake.", "Tiered: undo + HP + reveal", "Recovery");
            SeedItem(codex, "item_pattern_scroll", "Pattern Scroll", "Item", "Normal", "Available from start.",
                "Highlights constraint conflicts.", "Tiered: 1/2/all zones", "Information");
            SeedItem(codex, "item_koi_reflection", "Koi Reflection", "Item", "Normal", "Available from start.",
                "Reveals candidates without Pencil cost.", "Tiered: 1/2/3 cells", "Information");
            SeedItem(codex, "item_lantern_of_clarity", "Lantern of Clarity", "Item", "Normal", "Available from start.",
                "Disables Fog of War.", "Tiered: 3/6/10 moves", "Constraint-control");

            // ── Unique items (10) ──
            SeedItem(codex, "item_garden_rake", "Garden Rake", "Item", "Normal", "Available from start.",
                "Highlights cells with 2 candidates in row/col.", "2-candidate highlight", "Information");
            SeedItem(codex, "item_offering_bowl", "Offering Bowl", "Item", "Normal", "Complete 3 puzzles.",
                "Spend 5 HP to reveal correct cell.", "5 HP cost, 1 cell", "Risk-conversion");
            SeedItem(codex, "item_pruning_shears", "Pruning Shears", "Item", "Normal", "Available from start.",
                "Removes 1 impossible candidate from box.", "1 candidate removed", "Information");
            SeedItem(codex, "item_zen_sand_sifter", "Zen Sand Sifter", "Item", "Normal", "Complete 5 puzzles.",
                "Highlights Hidden Pairs in row.", "Hidden pair highlight", "Information");
            SeedItem(codex, "item_ginkgo_leaf", "Ginkgo Leaf", "Item", "Rare", "Find 5 unique items.",
                "Highlights all instances of a number.", "Number tracking", "Information");
            SeedItem(codex, "item_rice_paper_umbrella", "Rice Paper Umbrella", "Item", "Rare", "Survive with 1 HP.",
                "Protects from next 2 mistakes.", "2 mistake shield", "Recovery");
            SeedItem(codex, "item_temple_incense", "Temple Incense", "Item", "Rare", "Complete a boss.",
                "Correct cells pulse for 5 moves.", "5-move pulse", "Information");
            SeedItem(codex, "item_koi_dragon_scale", "Koi Dragon Scale", "Item", "Epic", "Class Level 15+.",
                "Completes most-filled line or box.", "Auto-complete line", "Tempo");
            SeedItem(codex, "item_golden_kintsugi_jar", "Golden Kintsugi Jar", "Item", "Epic", "Class Level 15+.",
                "Highlights all mistakes (no HP cost).", "Mistake reveal", "Information");
            SeedItem(codex, "item_silk_fan", "Silk Fan", "Item", "Epic", "Class Level 15+.",
                "Swap two placed numbers.", "Number swap", "Tempo");

            // ── Relics (23) ──
            foreach (RelicId id in Enum.GetValues(typeof(RelicId)))
            {
                var tier = RelicService.GetTier(id);
                var name = RelicService.GetName(id);
                var desc = RelicService.GetDescription(id);
                var rarityLabel = tier == RelicTier.Legendary ? "Legendary" : tier.ToString();
                var synergy = ClassifyRelicSynergy(id);
                SeedItem(codex, $"relic_{id}", name, "Relic", rarityLabel,
                    $"Discover via relic nodes, elites, or shops.",
                    desc, desc, synergy);
            }
        }

        private static string ClassifyRelicSynergy(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble or RelicId.CrackedTeacup or RelicId.WisteriaBranch or
                RelicId.PhoenixFeather or RelicId.SilentGrid or RelicId.SakuraSeal
                    => "Recovery",
                RelicId.WoodenComb or RelicId.CopperTortoise or RelicId.GoldenRoot or
                RelicId.SpiritLantern or RelicId.MossToken
                    => "Risk-conversion",
                RelicId.CrimsonFan or RelicId.PorcelainMask or RelicId.MoonstoneCompass or
                RelicId.ShiftingGarden
                    => "Constraint-control",
                RelicId.MonkCharm or RelicId.KoiReflectionRelic or RelicId.StoneSundial or
                RelicId.EternalLotus or RelicId.DragonsEye or RelicId.JadeHairpin
                    => "Tempo",
                _ => "Information"
            };
        }

        private static void SeedItem(ItemCodexState codex, string id, string name, string type, string rarity,
            string unlockCondition, string description, string effect, string synergy)
        {
            codex.Entries.Add(new ItemCodexEntry
            {
                ItemID = id,
                Name = name,
                Type = type,
                RarityTier = rarity,
                UnlockCondition = unlockCondition,
                Description = description,
                EffectFormula = effect,
                SynergyTags = synergy,
                Discovered = false,
                Mastered = false
            });
        }

        private void MarkAllEntriesDiscovered()
        {
            var codex = _profile.Meta.ItemCodex;
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            for (var i = 0; i < codex.Entries.Count; i++)
            {
                codex.Entries[i].Discovered = true;
                codex.Entries[i].Mastered = true;
                if (string.IsNullOrWhiteSpace(codex.Entries[i].DiscoveredDate))
                    codex.Entries[i].DiscoveredDate = date;
            }
        }

        private void Persist()
        {
            var envelope = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = _profile.Options },
                MetaProgress = _profile.Meta,
                TutorialProgress = _profile.TutorialProgress,
                Statistics = _profile.Stats,
                Mastery = _profile.Mastery,
                Completion = _profile.Completion
            };
            _save.SaveProfile(envelope);
        }

        private Sprite ResolveItemSprite(ItemCodexEntry item)
        {
            var iconName = ItemIdToIconName(item);
            if (_spriteCache.TryGetValue(iconName, out var cached)) return cached;

            var loaded = Resources.Load<Sprite>("GeneratedIcons/" + iconName);
            if (loaded == null) loaded = Resources.Load<Sprite>("GeneratedIcons/icon_pebble");
            if (loaded == null) loaded = GetFallbackSprite();

            _spriteCache[iconName] = loaded;
            return loaded;
        }

        private static string ItemIdToIconName(ItemCodexEntry item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemID)) return "icon_pebble";

            // Relic icons
            if (item.ItemID.StartsWith("relic_", StringComparison.Ordinal))
            {
                if (item.ItemID.Contains("GoldenRoot")) return "icon_golden_bloom";
                if (item.ItemID.Contains("SilentGrid")) return "icon_fog_stone";
                if (item.ItemID.Contains("ShiftingGarden")) return "icon_enlightenment_tree";
                if (item.ItemID.Contains("EternalLotus")) return "icon_infinite_lotus";
                if (item.ItemID.Contains("DragonsEye")) return "icon_golden_koi";
                if (item.ItemID.Contains("PhoenixFeather")) return "icon_golden_bloom";
                return "icon_jade_amulet";
            }

            // Item icons
            if (item.ItemID.Contains("solver")) return "icon_item_solver";
            if (item.ItemID.Contains("finder")) return "icon_item_finder";
            if (item.ItemID.Contains("inkwell")) return "icon_item_ink_well";
            if (item.ItemID.Contains("meditation")) return "icon_item_meditation_stone";
            if (item.ItemID.Contains("wind_chime")) return "icon_item_wind_chime";
            if (item.ItemID.Contains("pattern")) return "icon_item_pattern_scroll";
            if (item.ItemID.Contains("koi_reflection")) return "icon_item_koi_reflection";
            if (item.ItemID.Contains("lantern")) return "icon_item_lantern_of_clarity";
            if (item.ItemID.Contains("garden_rake")) return "icon_pebble";
            if (item.ItemID.Contains("offering")) return "icon_rice_bowl";
            if (item.ItemID.Contains("pruning")) return "icon_pebble";
            if (item.ItemID.Contains("zen_sand")) return "icon_pebble";
            if (item.ItemID.Contains("ginkgo")) return "icon_golden_bloom";
            if (item.ItemID.Contains("umbrella")) return "icon_pebble";
            if (item.ItemID.Contains("incense")) return "icon_sacred_bell";
            if (item.ItemID.Contains("dragon_scale")) return "icon_golden_koi";
            if (item.ItemID.Contains("kintsugi")) return "icon_broken_mask";
            if (item.ItemID.Contains("silk_fan")) return "icon_pebble";

            return "icon_pebble";
        }

        private Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var dark = new Color(0.12f, 0.17f, 0.22f, 1f);
            var light = new Color(0.32f, 0.45f, 0.58f, 1f);
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                    tex.SetPixel(x, y, (x < 2 || y < 2 || x > 29 || y > 29) ? light : dark);

            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _fallbackSprite;
        }
    }
}
