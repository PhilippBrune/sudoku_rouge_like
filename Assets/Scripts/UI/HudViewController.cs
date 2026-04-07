using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the in-puzzle HUD (HP/pencil bars, modifier banner, combo counter, passive label)
    /// and the bag panel (item + relic slots).
    /// </summary>
    public sealed class HudViewController : MonoBehaviour
    {
        // ── Injected ──
        private RunMapController _map;
        private GameObject _sudokuPanel;
        private Text _hpText;
        private Text _pencilText;

        // ── HUD bar images ──
        private Image _hpBarFill;
        private Image _pencilBarFill;

        // ── Floor modifier display ──
        private GameObject _floorModBanner;
        private Text _floorModText;
        private GameObject _modifierInfoBox;
        private Text _modifierInfoText;

        // ── Combo / Passive ──
        private Text _comboText;
        private Text _passiveText;

        // ── Bag panel ──
        private GameObject _bagPanel;
        private List<Button> _bagItemButtons;
        private Button _bagRelicButton;
        private int _selectedBagSlot = -1;
        private HashSet<(int row, int col)> _bagHighlightCells;
        private float _bagHighlightEndTime;

        // ── Callbacks ──
        public Action<string> OnStatusChanged;
        public Action<int, ItemInstance> OnItemEffectRequested;
        public Action<HashSet<(int, int)>, float> OnBagHighlightRequested;

        public HashSet<(int row, int col)> BagHighlightCells => _bagHighlightCells;
        public float BagHighlightEndTime => _bagHighlightEndTime;

        public void Configure(RunMapController map, GameObject sudokuPanel,
            Text hpText, Text pencilText)
        {
            _map = map;
            _sudokuPanel = sudokuPanel;
            _hpText = hpText;
            _pencilText = pencilText;
            _bagItemButtons = new List<Button>();
            _bagHighlightCells = new HashSet<(int, int)>();
        }

        public void ClearBagHighlight()
        {
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;
        }

        public void Refresh(RunState state, LevelConfig levelConfig)
        {
            if (state == null) return;
            if (_hpText != null) _hpText.text = $"HP: {state.CurrentHP}/{state.MaxHP}";
            if (_pencilText != null) _pencilText.text = $"Pencil: {state.CurrentPencil}/{state.MaxPencil}";

            if (_hpBarFill == null && _sudokuPanel != null)
            {
                var pr = _sudokuPanel.GetComponent<RectTransform>();
                var hpBg = pr?.Find("HpBarBg");
                if (hpBg != null) _hpBarFill = hpBg.Find("HpBarFill")?.GetComponent<Image>();
                var pBg = pr?.Find("PencilBarBg");
                if (pBg != null) _pencilBarFill = pBg.Find("PencilBarFill")?.GetComponent<Image>();
            }

            if (_hpBarFill != null)
            {
                InRunUiFactory.EnsureBarSprite(_hpBarFill);
                _hpBarFill.fillAmount = state.MaxHP > 0 ? Mathf.Clamp01(state.CurrentHP / (float)state.MaxHP) : 0;
            }
            if (_pencilBarFill != null)
            {
                InRunUiFactory.EnsureBarSprite(_pencilBarFill);
                _pencilBarFill.fillAmount = state.MaxPencil > 0 ? Mathf.Clamp01(state.CurrentPencil / (float)state.MaxPencil) : 0;
            }

            EnsureFloorModBanner();
            EnsureModifierInfoBox();
            EnsureComboCounter();
            EnsurePassiveLabel();
            EnsureBagPanel();
            RefreshBag();
            if (_bagHighlightEndTime > 0f && Time.time > _bagHighlightEndTime)
            {
                _bagHighlightCells.Clear();
                _bagHighlightEndTime = 0f;
                OnBagHighlightRequested?.Invoke(_bagHighlightCells, _bagHighlightEndTime);
            }
        }

        // ────────────────────── Floor modifier banner ──────────────────────

        private void EnsureFloorModBanner()
        {
            var run = _map?.Run;
            if (run?.State == null) return;
            var mods = run.State.ActiveFloorModifiers;

            if (mods == null || mods.Count == 0)
            {
                if (_floorModBanner != null) _floorModBanner.SetActive(false);
                return;
            }

            if (_floorModBanner == null && _sudokuPanel != null)
            {
                _floorModBanner = new GameObject("FloorModBanner", typeof(RectTransform), typeof(Image));
                _floorModBanner.transform.SetParent(_sudokuPanel.transform, false);
                var rt = _floorModBanner.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.27f, 0.835f);
                rt.anchorMax = new Vector2(0.73f, 0.865f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _floorModBanner.GetComponent<Image>().color = new Color(0.15f, 0.10f, 0.25f, 0.85f);

                _floorModText = InRunUiFactory.CreateText(_floorModBanner.transform, "FloorModText", "",
                    12, TextAnchor.MiddleCenter, InRunUiFactory.AccentGold);
                _floorModText.rectTransform.anchorMin = Vector2.zero;
                _floorModText.rectTransform.anchorMax = Vector2.one;
                _floorModText.rectTransform.offsetMin = new Vector2(4, 0);
                _floorModText.rectTransform.offsetMax = new Vector2(-4, 0);
            }

            if (_floorModBanner != null) _floorModBanner.SetActive(true);

            if (_floorModText != null)
            {
                var sb = new StringBuilder("Floor Modifiers: ");
                for (var i = 0; i < mods.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    sb.Append(InRunUiFactory.FormatModName(mods[i]));
                }
                _floorModText.text = sb.ToString();
            }
        }

        private void EnsureModifierInfoBox()
        {
            var run = _map?.Run;
            if (run?.State == null || run.CurrentLevelConfig == null) return;
            var activeMods = run.CurrentLevelConfig.ActiveModifiers;

            if (activeMods == null || activeMods.Count == 0)
            {
                if (_modifierInfoBox != null) _modifierInfoBox.SetActive(false);
                return;
            }

            if (_modifierInfoBox == null && _sudokuPanel != null)
            {
                _modifierInfoBox = new GameObject("ModInfoBox", typeof(RectTransform), typeof(Image));
                _modifierInfoBox.transform.SetParent(_sudokuPanel.transform, false);
                var rt = _modifierInfoBox.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.74f, 0.08f);
                rt.anchorMax = new Vector2(0.97f, 0.27f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _modifierInfoBox.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.18f, 0.90f);

                _modifierInfoText = InRunUiFactory.CreateText(_modifierInfoBox.transform, "ModInfoText", "",
                    10, TextAnchor.UpperLeft, InRunUiFactory.TextColor);
                _modifierInfoText.rectTransform.anchorMin = Vector2.zero;
                _modifierInfoText.rectTransform.anchorMax = Vector2.one;
                _modifierInfoText.rectTransform.offsetMin = new Vector2(4, 4);
                _modifierInfoText.rectTransform.offsetMax = new Vector2(-4, -4);
            }

            if (_modifierInfoBox != null) _modifierInfoBox.SetActive(true);

            if (_modifierInfoText != null)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < activeMods.Count; i++)
                {
                    if (i > 0) sb.Append('\n');
                    sb.Append(InRunUiFactory.FormatModName(activeMods[i]));
                    sb.Append(": ");
                    sb.Append(InRunUiFactory.GetModDesc(activeMods[i]));
                }
                _modifierInfoText.text = sb.ToString();
            }
        }

        private void EnsureComboCounter()
        {
            var run = _map?.Run;
            if (run?.State == null || _sudokuPanel == null) return;

            if (_comboText == null)
            {
                var go = new GameObject("ComboCounter", typeof(RectTransform));
                go.transform.SetParent(_sudokuPanel.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.74f, 0.28f);
                rt.anchorMax = new Vector2(0.97f, 0.35f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _comboText = InRunUiFactory.CreateText(go.transform, "ComboText", "", 13, TextAnchor.MiddleCenter,
                    new Color(0.98f, 0.83f, 0.26f, 1f));
            }

            var streak = run.State.ComboStreak;
            if (streak >= 2)
            {
                _comboText.text = $"Combo \u00d7{streak}!";
                _comboText.gameObject.SetActive(true);
            }
            else
            {
                _comboText.gameObject.SetActive(false);
            }
        }

        private void EnsurePassiveLabel()
        {
            var run = _map?.Run;
            if (run?.State == null || _sudokuPanel == null) return;

            if (_passiveText == null)
            {
                var go = new GameObject("PassiveLabel", typeof(RectTransform));
                go.transform.SetParent(_sudokuPanel.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.01f, 0.01f);
                rt.anchorMax = new Vector2(0.46f, 0.06f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _passiveText = InRunUiFactory.CreateText(go.transform, "PassiveText", "", 9, TextAnchor.MiddleLeft,
                    new Color(0.78f, 0.85f, 0.75f, 0.80f));
                _passiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var def = SudokuRoguelike.Classes.ClassCatalog.GetDefinition(run.State.ClassId);
            _passiveText.text = def != null ? $"Passive: {def.PassiveDescription}" : "";
        }

        // ────────────────────── Bag panel ──────────────────────

        private void EnsureBagPanel()
        {
            if (_bagPanel != null || _sudokuPanel == null) return;
            var pr = _sudokuPanel.GetComponent<RectTransform>();
            if (pr == null) return;

            var bagGo = new GameObject("BagPanel", typeof(RectTransform), typeof(Image));
            bagGo.transform.SetParent(pr, false);
            var bagRt = bagGo.GetComponent<RectTransform>();
            bagRt.anchorMin = new Vector2(0.01f, 0.03f);
            bagRt.anchorMax = new Vector2(0.21f, 0.74f);
            bagRt.offsetMin = Vector2.zero;
            bagRt.offsetMax = Vector2.zero;
            bagGo.GetComponent<Image>().color = new Color(0.06f, 0.10f, 0.12f, 0.70f);
            _bagPanel = bagGo;

            var title = InRunUiFactory.CreateText(bagGo.transform, "BagTitle", "BAG", 13, TextAnchor.MiddleCenter, InRunUiFactory.AccentGold);
            title.rectTransform.anchorMin = new Vector2(0.02f, 0.93f);
            title.rectTransform.anchorMax = new Vector2(0.98f, 1.00f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            // Three item slots
            _bagItemButtons.Clear();
            var slotYs = new[] { (0.68f, 0.90f), (0.45f, 0.67f), (0.22f, 0.44f) };
            for (var i = 0; i < 3; i++)
            {
                var (yMin, yMax) = slotYs[i];
                var btn = InRunUiFactory.CreateBagSlotButton(bagGo.transform, $"BagSlot_{i}",
                    new Vector2(0.03f, yMin), new Vector2(0.97f, yMax));
                _bagItemButtons.Add(btn);
                var idx = i;
                btn.onClick.AddListener(() => OnBagItemClicked(idx));
            }

            // Thin divider between items and relic
            var divGo = new GameObject("BagDivider", typeof(RectTransform), typeof(Image));
            divGo.transform.SetParent(bagGo.transform, false);
            var divRt = divGo.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.05f, 0.19f);
            divRt.anchorMax = new Vector2(0.95f, 0.21f);
            divRt.offsetMin = Vector2.zero;
            divRt.offsetMax = Vector2.zero;
            divGo.GetComponent<Image>().color = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.35f);

            // Relic slot
            _bagRelicButton = InRunUiFactory.CreateBagSlotButton(bagGo.transform, "BagRelic",
                new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.17f));
            _bagRelicButton.onClick.RemoveAllListeners();
            _bagRelicButton.onClick.AddListener(OnBagRelicClicked);
        }

        private void RefreshBag()
        {
            if (_bagPanel == null) return;
            var state = _map?.Run?.State;

            for (var i = 0; i < _bagItemButtons.Count; i++)
            {
                var btn = _bagItemButtons[i];
                if (btn == null) continue;
                var iconImg = btn.transform.Find("Icon")?.GetComponent<Image>();
                var nameText = btn.transform.Find("NameText")?.GetComponent<Text>();
                var item = (state != null && i < state.HeldItems.Count) ? state.HeldItems[i] : null;

                if (item != null)
                {
                    btn.interactable = true;
                    if (iconImg != null)
                    {
                        var spr = Resources.Load<Sprite>("GeneratedIcons/icon_" + ItemService.GetIconName(item.Type));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                        iconImg.preserveAspect = true;
                    }
                    if (nameText != null)
                    {
                        var chargeTxt = item.Charges > 1 ? $" \u00d7{item.Charges}" : "";
                        nameText.text = $"{ItemService.GetItemName(item.Type)}{chargeTxt}\n<size=8>{item.Rarity}</size>";
                        nameText.color = InRunUiFactory.TextColor;
                    }
                }
                else
                {
                    btn.interactable = false;
                    if (iconImg != null) { iconImg.sprite = null; iconImg.color = new Color(1f, 1f, 1f, 0.08f); }
                    if (nameText != null)
                    {
                        nameText.text = (state != null && i < state.ItemSlots) ? "Empty" : "\u2014";
                        nameText.color = new Color(0.45f, 0.45f, 0.45f, 0.55f);
                    }
                }
            }

            if (_bagRelicButton != null)
            {
                var iconImg = _bagRelicButton.transform.Find("Icon")?.GetComponent<Image>();
                var nameText = _bagRelicButton.transform.Find("NameText")?.GetComponent<Text>();
                if (state != null && state.HasRelic)
                {
                    var relic = state.HeldRelic;
                    _bagRelicButton.interactable = false;
                    if (iconImg != null)
                    {
                        var spr = Resources.Load<Sprite>("GeneratedIcons/icon_" + RelicService.GetIconName(relic.Id));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                        iconImg.preserveAspect = true;
                    }
                    if (nameText != null)
                    {
                        var useTxt = relic.UsesRemaining < 0 ? "Passive"
                            : relic.UsesRemaining == 0 ? "Spent" : $"\u00d7{relic.UsesRemaining}";
                        nameText.text = $"{RelicService.GetRelicName(relic.Id)}\n<size=8>{useTxt}</size>";
                        nameText.color = new Color(0.98f, 0.83f, 0.26f, 1f);
                    }
                    _bagRelicButton.interactable = true;
                }
                else
                {
                    _bagRelicButton.interactable = false;
                    if (iconImg != null) { iconImg.sprite = null; iconImg.color = new Color(1f, 1f, 1f, 0.08f); }
                    if (nameText != null) { nameText.text = "No Relic"; nameText.color = new Color(0.45f, 0.45f, 0.45f, 0.55f); }
                }
            }
        }

        private void OnBagItemClicked(int idx)
        {
            var run = _map?.Run;
            if (run == null || idx < 0 || idx >= run.State.HeldItems.Count) return;
            var item = run.State.HeldItems[idx];
            if (item == null) return;

            if (_selectedBagSlot == idx)
            {
                // Second click on same slot → use the item
                _selectedBagSlot = -1;
                RefreshBagHighlights();
                OnItemEffectRequested?.Invoke(idx, item);
            }
            else
            {
                // First click → show description
                _selectedBagSlot = idx;
                RefreshBagHighlights();
                var desc = ItemService.GetItemDescription(item.Type, item.Rarity);
                OnStatusChanged?.Invoke($"{ItemService.GetItemName(item.Type)}: {desc}  [click again to use]");
            }
        }

        private void RefreshBagHighlights()
        {
            for (var i = 0; i < _bagItemButtons.Count; i++)
            {
                var btn = _bagItemButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img == null) continue;
                img.color = (i == _selectedBagSlot)
                    ? new Color(InRunUiFactory.AccentGold.r * 0.5f, InRunUiFactory.AccentGold.g * 0.5f, 0.10f, 0.90f)
                    : new Color(0.10f, 0.16f, 0.20f, 0.80f);
            }
        }

        private void OnBagRelicClicked()
        {
            var state = _map?.Run?.State;
            if (state == null || !state.HasRelic) return;
            var relic = state.HeldRelic;
            OnStatusChanged?.Invoke($"{RelicService.GetRelicName(relic.Id)}: {RelicService.GetRelicDescription(relic.Id)}");
        }
    }
}
