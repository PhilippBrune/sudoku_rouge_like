using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
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

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private string _filter = "All";
        private string _sort = "Rarity";
        private int _selectedIndex;
        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private Sprite _fallbackSprite;

        public void Configure(MainMenuController controller, Text completion, Text grid, Text detail, Text tooltip, Text filter, Image[] icons = null, Text[] iconLabels = null)
        {
            mainMenuController = controller;
            completionText = completion;
            gridText = grid;
            detailText = detail;
            tooltipText = tooltip;
            filterText = filter;
            if (icons != null)
            {
                iconSlots = icons;
            }

            if (iconLabels != null)
            {
                iconSlotLabels = iconLabels;
            }

            RefreshView();
        }

        public void RefreshView()
        {
            EnsureProfileLoaded();
            EnsureSeedEntries();
            if (mainMenuController != null && mainMenuController.DebugEnableAllFeatures)
            {
                EnsureDebugCatalogCoverage();
                MarkAllEntriesDiscovered();
                if (_filter == "Unseen")
                {
                    _filter = "All";
                }
            }

            var filtered = BuildFiltered();

            if (_selectedIndex >= filtered.Count)
            {
                _selectedIndex = Mathf.Max(0, filtered.Count - 1);
            }

            if (completionText != null)
            {
                var discovered = 0;
                var all = _profile.Meta.ItemCodex.Entries.Count;
                for (var i = 0; i < all; i++)
                {
                    if (_profile.Meta.ItemCodex.Entries[i].Discovered)
                    {
                        discovered++;
                    }
                }

                completionText.text = $"Completion: {discovered} / {all}";
            }

            if (filterText != null)
            {
                filterText.text = $"Filter: {_filter} | Sort: {_sort}";
            }

            if (gridText != null)
            {
                gridText.text = BuildGridText(filtered);
            }

            if (detailText != null)
            {
                detailText.text = BuildDetailText(filtered);
            }

            RefreshIconGrid(filtered);

            if (tooltipText != null)
            {
                tooltipText.text =
                    "Item Roll: 2-5 slots by difficulty.\n" +
                    "Nothing-slot can grant gold.\n" +
                    "Picked/Nothing slots cannot be rerolled.";
            }

            Persist();
        }

        public void FilterAll() => SetFilter("All");
        public void FilterRelics() => SetFilter("Relics");
        public void FilterConsumables() => SetFilter("Consumables");
        public void FilterCursed() => SetFilter("Cursed");
        public void FilterLegendary() => SetFilter("Legendary");
        public void FilterBossRewards() => SetFilter("Boss Rewards");
        public void FilterClassSpecific() => SetFilter("Class-Specific");
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
            if (filtered.Count == 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + 1) % filtered.Count;
            RefreshView();
        }

        public void SelectPrev()
        {
            var filtered = BuildFiltered();
            if (filtered.Count == 0)
            {
                return;
            }

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
                {
                    output.Add(item);
                }
            }

            ApplySort(output);

            return output;
        }

        private static bool MatchesFilter(ItemCodexEntry item, string filter)
        {
            if (item == null)
            {
                return false;
            }

            if (filter == "Relics") return string.Equals(item.Type, "Relic", StringComparison.OrdinalIgnoreCase);
            if (filter == "Consumables") return string.Equals(item.Type, "Consumable", StringComparison.OrdinalIgnoreCase);
            if (filter == "Cursed") return string.Equals(item.Type, "Cursed", StringComparison.OrdinalIgnoreCase);
            if (filter == "Legendary") return string.Equals(item.RarityTier, "Legendary", StringComparison.OrdinalIgnoreCase);
            if (filter == "Boss Rewards") return item.UnlockCondition.Contains("Boss", StringComparison.OrdinalIgnoreCase);
            if (filter == "Class-Specific") return item.SynergyTags.Contains("Class", StringComparison.OrdinalIgnoreCase);
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
            if (item == null || item.TimesPicked <= 0)
            {
                return 0f;
            }

            return (float)item.TimesWon / item.TimesPicked;
        }

        private static int RarityScore(string rarity)
        {
            if (string.Equals(rarity, "Legendary", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(rarity, "Common", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private void RefreshIconGrid(List<ItemCodexEntry> filtered)
        {
            if (iconSlots == null || iconSlots.Length == 0)
            {
                return;
            }

            for (var i = 0; i < iconSlots.Length; i++)
            {
                var image = iconSlots[i];
                if (image == null)
                {
                    continue;
                }

                if (i >= filtered.Count)
                {
                    image.enabled = false;
                    if (iconSlotLabels != null && i < iconSlotLabels.Length && iconSlotLabels[i] != null)
                    {
                        iconSlotLabels[i].text = string.Empty;
                    }

                    continue;
                }

                var item = filtered[i];
                image.enabled = true;
                image.color = item.Discovered ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
                image.sprite = ResolveItemSprite(item);
                image.type = Image.Type.Sliced;
                image.preserveAspect = true;

                if (iconSlotLabels != null && i < iconSlotLabels.Length && iconSlotLabels[i] != null)
                {
                    iconSlotLabels[i].text = item.Discovered ? item.Name : "???";
                }
            }
        }

        private string BuildGridText(List<ItemCodexEntry> filtered)
        {
            if (filtered.Count == 0)
            {
                return "No items match this filter.";
            }

            var rows = new System.Text.StringBuilder();
            for (var i = 0; i < filtered.Count; i++)
            {
                var item = filtered[i];
                var selected = i == _selectedIndex ? "> " : "  ";
                var state = item.Mastered ? "🌸" : item.Discovered ? "🌿" : "🔒";
                var name = item.Discovered ? item.Name : "???";
                rows.AppendLine($"{selected}{state} {name} [{item.RarityTier}]");
            }

            return rows.ToString().TrimEnd();
        }

        private string BuildDetailText(List<ItemCodexEntry> filtered)
        {
            if (filtered.Count == 0)
            {
                return "Select an item to view details.";
            }

            var item = filtered[Mathf.Clamp(_selectedIndex, 0, filtered.Count - 1)];
            if (!item.Discovered)
            {
                return
                    "Name: ???\n" +
                    "Type: ???\n" +
                    "Rarity: ???\n" +
                    $"Unlock hint: {item.UnlockCondition}";
            }

            return
                $"Name: {item.Name}\n" +
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
            {
                _profile.ApplyEnvelope(envelope);
            }
        }

        private void EnsureSeedEntries()
        {
            var codex = _profile.Meta.ItemCodex;
            if (codex.Entries.Count > 0)
            {
                return;
            }

            codex.Entries.Add(new ItemCodexEntry { ItemID = "relic_koi", Name = "Koi Reflection", Type = "Relic", RarityTier = "Rare", UnlockCondition = "Complete a Garden run.", Description = "Adds calm combo stability.", EffectFormula = "+1 combo grace", SynergyTags = "Class:GardenMonk", Discovered = false });
            codex.Entries.Add(new ItemCodexEntry { ItemID = "consumable_tea", Name = "Tea of Focus", Type = "Consumable", RarityTier = "Common", UnlockCondition = "Use 3 consumables.", Description = "Boosts accuracy for one puzzle.", EffectFormula = "-1 mistake penalty for 5 moves", SynergyTags = "Utility", Discovered = false });
            codex.Entries.Add(new ItemCodexEntry { ItemID = "curse_blind", Name = "Shrouded Lens", Type = "Cursed", RarityTier = "Epic", UnlockCondition = "Accept a trap event.", Description = "Power for clarity at a price.", EffectFormula = "+Gold, +Heat", SynergyTags = "Curse", Discovered = false });
            codex.Entries.Add(new ItemCodexEntry { ItemID = "legendary_lantern", Name = "Lantern of Nine", Type = "Relic", RarityTier = "Legendary", UnlockCondition = "Defeat a Boss with Heat 5+.", Description = "A sacred relic of the garden.", EffectFormula = "+2 reroll tokens, +10% XP", SynergyTags = "Class:LanternSeer", Discovered = false });
            codex.Entries.Add(new ItemCodexEntry { ItemID = "boss_reward_root", Name = "Ember Root", Type = "Boss Reward", RarityTier = "Epic", UnlockCondition = "Clear first Boss.", Description = "Reward from the guardian.", EffectFormula = "+1 passive tier", SynergyTags = "Boss", Discovered = false });
        }

        private void EnsureDebugCatalogCoverage()
        {
            var codex = _profile.Meta.ItemCodex;
            AddEntryIfMissing(codex.Entries, "relic_legend_shifting_garden", "Shifting Garden", "Relic", "Legendary", "Debug unlock", "Garden mutates route pressure.", "Path variance +", "Boss");
            AddEntryIfMissing(codex.Entries, "relic_legend_silent_grid", "Silent Grid", "Relic", "Legendary", "Debug unlock", "Protective rhythm on mistakes.", "Mistake shield +", "Class:LanternSeer");
            AddEntryIfMissing(codex.Entries, "relic_legend_golden_root", "Golden Root", "Relic", "Legendary", "Debug unlock", "Carries value through the run.", "Gold carryover", "Boss");
            AddEntryIfMissing(codex.Entries, "relic_combo_t2_monk_charm", "Monk Charm", "Relic", "Rare", "Debug unlock", "Rewards clean placement streaks.", "Combo gain +", "Class:GardenMonk");
            AddEntryIfMissing(codex.Entries, "relic_cursed_t4_transmuted", "Transmuted Burden", "Cursed", "Epic", "Debug unlock", "Strong upside with pressure.", "Heat + reward +", "Curse");
            AddEntryIfMissing(codex.Entries, "relic_utility_t4_transmuted", "Transmuted Sigil", "Relic", "Epic", "Debug unlock", "Utility relic from adaptation.", "Adaptive utility", "Utility");
        }

        private static void AddEntryIfMissing(List<ItemCodexEntry> entries, string id, string name, string type, string rarity, string unlockCondition, string description, string effect, string tags)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].ItemID, id, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            entries.Add(new ItemCodexEntry
            {
                ItemID = id,
                Name = name,
                Type = type,
                RarityTier = rarity,
                UnlockCondition = unlockCondition,
                Description = description,
                EffectFormula = effect,
                SynergyTags = tags,
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
                {
                    codex.Entries[i].DiscoveredDate = date;
                }
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
            if (_spriteCache.TryGetValue(iconName, out var cached))
            {
                return cached;
            }

            var loaded = Resources.Load<Sprite>("GeneratedIcons/" + iconName);
            if (loaded == null)
            {
                loaded = Resources.Load<Sprite>("GeneratedIcons/icon_pebble");
            }

            if (loaded == null)
            {
                loaded = GetFallbackSprite();
            }

            _spriteCache[iconName] = loaded;
            return loaded;
        }

        private static string ItemIdToIconName(ItemCodexEntry item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemID))
            {
                return "icon_pebble";
            }

            if (item.ItemID.Contains("lantern", StringComparison.OrdinalIgnoreCase)) return "icon_garden_lantern";
            if (item.ItemID.Contains("tea", StringComparison.OrdinalIgnoreCase)) return "icon_tea_cup";
            if (item.ItemID.Contains("koi", StringComparison.OrdinalIgnoreCase)) return "icon_golden_koi";
            if (item.ItemID.Contains("blind", StringComparison.OrdinalIgnoreCase)) return "icon_fog_stone";
            if (item.ItemID.Contains("root", StringComparison.OrdinalIgnoreCase)) return "icon_moss_stone";

            if (string.Equals(item.Type, "Cursed", StringComparison.OrdinalIgnoreCase)) return "icon_broken_mask";
            if (string.Equals(item.RarityTier, "Legendary", StringComparison.OrdinalIgnoreCase)) return "icon_sacred_bell";
            if (string.Equals(item.Type, "Consumable", StringComparison.OrdinalIgnoreCase)) return "icon_rice_bowl";
            return "icon_jade_amulet";
        }

        private Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null)
            {
                return _fallbackSprite;
            }

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var dark = new Color(0.12f, 0.17f, 0.22f, 1f);
            var light = new Color(0.32f, 0.45f, 0.58f, 1f);
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var border = x < 2 || y < 2 || x > 29 || y > 29;
                    tex.SetPixel(x, y, border ? light : dark);
                }
            }

            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _fallbackSprite;
        }
    }
}
