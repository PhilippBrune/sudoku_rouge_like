using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
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
        private Button _bagRelicButton;       // legacy compat — no longer primary
        private List<Button> _bagRelicButtons = new List<Button>();
        private int _selectedBagSlot = -1;
        private HashSet<(int row, int col)> _bagHighlightCells;
        private float _bagHighlightEndTime;
        private int _builtForSlotCount = -1;
        private int _builtForRelicCount = -1;
        private GameObject _bagScrollContainer;
        private GameObject _bagRelicScrollContainer;

        // ── Callbacks ──
        public Action<string> OnStatusChanged;
        public Action<int, ItemInstance> OnItemEffectRequested;
        public Action<HashSet<(int, int)>, float> OnBagHighlightRequested;
        // ── Timer ──
        private float _puzzleStartTime = -1f;
        private Text  _timerText;
        private Text  _runTimerText;


        public float GetPuzzleElapsed() =>
            _puzzleStartTime >= 0f ? Time.realtimeSinceStartup - _puzzleStartTime : 0f;

        public Action<BossModifierId> OnModifierTapped;

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

        public void StartPuzzleTimer()
        {
            _puzzleStartTime = Time.realtimeSinceStartup;
        }

        public void StopPuzzleTimer()
        {
            _puzzleStartTime = -1f;
        }

        public void ClearBagHighlight()
        {
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;
        }

        /// <summary>
        /// Controller: move the bag slot selection by <paramref name="delta"/> steps.
        /// Clamps within valid item count. If nothing is selected yet, selects the first item.
        /// </summary>
        public void ControllerSelectBagSlot(int delta)
        {
            var run = _map?.Run;
            if (run == null) return;
            var count = run.State?.HeldItems?.Count ?? 0;
            if (count == 0) return;
            _selectedBagSlot = _selectedBagSlot < 0
                ? 0
                : Mathf.Clamp(_selectedBagSlot + delta, 0, count - 1);
            EnsureBagPanel();
            RefreshBagHighlights();
            var item = run.State.HeldItems[_selectedBagSlot];
            if (item != null)
            {
                var desc = ItemService.GetItemDescription(item.Type, item.Rarity);
                OnStatusChanged?.Invoke($"{ItemService.GetItemName(item.Type)}: {desc}  [A to use]");
            }
        }

        /// <summary>
        /// Controller: activate (use) the currently selected bag slot.
        /// </summary>
        public void ControllerActivateBagSlot()
        {
            var run = _map?.Run;
            if (run == null || _selectedBagSlot < 0) return;
            var count = run.State?.HeldItems?.Count ?? 0;
            if (_selectedBagSlot >= count) return;
            var item = run.State.HeldItems[_selectedBagSlot];
            if (item == null) return;
            var idx = _selectedBagSlot;
            _selectedBagSlot = -1;
            RefreshBagHighlights();
            OnItemEffectRequested?.Invoke(idx, item);
        }

        /// <summary>
        /// Keyboard / controller inspect: shows the selected item's full description in the status bar.
        /// </summary>
        public void ControllerInspectBagSlot()
        {
            var run = _map?.Run;
            if (run == null || _selectedBagSlot < 0) return;
            var count = run.State?.HeldItems?.Count ?? 0;
            if (_selectedBagSlot >= count) return;
            var item = run.State.HeldItems[_selectedBagSlot];
            if (item == null) return;
            var name = ItemService.GetItemName(item.Type);
            var desc = ItemService.GetItemDescription(item.Type, item.Rarity);
            OnStatusChanged?.Invoke($"{name} [{item.Rarity}]: {desc}");
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

            // Timer display — lazy-created the first time
            if (_timerText == null && _sudokuPanel != null)
            {
                _timerText = InRunUiFactory.CreateText(_sudokuPanel.transform, "PuzzleTimer",
                    "", 12, TextAnchor.MiddleRight, new Color(0.80f, 0.77f, 0.60f, 0.80f));
                var rt = _timerText.rectTransform;
                rt.anchorMin = new Vector2(0.72f, 0.95f);
                rt.anchorMax = new Vector2(0.99f, 1.00f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _timerText.raycastTarget = false;
            }

            // Run total timer — positioned just below puzzle timer
            if (_runTimerText == null && _sudokuPanel != null)
            {
                _runTimerText = InRunUiFactory.CreateText(_sudokuPanel.transform, "RunTimer",
                    "", 10, TextAnchor.MiddleRight, new Color(0.65f, 0.62f, 0.45f, 0.65f));
                var rt = _runTimerText.rectTransform;
                rt.anchorMin = new Vector2(0.72f, 0.90f);
                rt.anchorMax = new Vector2(0.99f, 0.95f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _runTimerText.raycastTarget = false;
            }

            if (_timerText != null)
            {
                if (_puzzleStartTime >= 0f)
                {
                    var elapsed = (int)(Time.realtimeSinceStartup - _puzzleStartTime);
                    _timerText.text = $"{elapsed / 60:00}:{elapsed % 60:00}";
                }
                else
                {
                    _timerText.text = string.Empty;
                }
            }

            if (_runTimerText != null && state != null)
            {
                var totalSec = (int)state.TotalRunSeconds;
                if (_puzzleStartTime >= 0f)
                    totalSec += (int)(Time.realtimeSinceStartup - _puzzleStartTime);
                if (totalSec > 0)
                    _runTimerText.text = $"Run: {totalSec / 60:00}:{totalSec % 60:00}";
                else
                    _runTimerText.text = string.Empty;
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

            // Hide the banner during boss puzzles: floor modifiers don't apply there,
            // so showing them would be misleading. Boss modifiers appear in the info box.
            if (run.CurrentLevelConfig?.IsBoss == true || mods == null || mods.Count == 0)
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
                _floorModBanner.GetComponent<Image>().color = InRunUiFactory.ModBannerBg;

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

            // sealed_eyes curse: hide modifier descriptions during boss puzzles
            var sealedEyes = SudokuRoguelike.Run.CurseService.IsActive(run.State, "sealed_eyes")
                             && run.CurrentLevelConfig.IsBoss;

            if (sealedEyes || activeMods == null || activeMods.Count == 0)
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
                _modifierInfoBox.GetComponent<Image>().color = InRunUiFactory.ModInfoBoxBg;

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

                // Add a click handler on the info box to cycle through global constraints
                if (_modifierInfoBox != null && OnModifierTapped != null)
                {
                    var btn = _modifierInfoBox.GetComponent<UnityEngine.UI.Button>()
                           ?? _modifierInfoBox.AddComponent<UnityEngine.UI.Button>();
                    btn.onClick.RemoveAllListeners();
                    var mods = activeMods;
                    btn.onClick.AddListener(() =>
                    {
                        for (var j = 0; j < mods.Count; j++)
                        {
                            if (mods[j] == BossModifierId.Nonconsecutive
                             || mods[j] == BossModifierId.Antiknight)
                            {
                                OnModifierTapped?.Invoke(mods[j]);
                                return;
                            }
                        }
                    });
                }
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
                rt.anchorMin = new Vector2(0.30f, 0.900f);
                rt.anchorMax = new Vector2(0.70f, 0.945f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _comboText = InRunUiFactory.CreateText(go.transform, "ComboText", "", 18, TextAnchor.MiddleCenter,
                    GamePalette.AccentGold);
            }

            var streak = run.State.ComboStreak;
            if (streak >= 2)
            {
                var wasHidden = !_comboText.gameObject.activeSelf;
                _comboText.text = $"Combo \u00d7{streak}!";
                _comboText.gameObject.SetActive(true);
                if (wasHidden && _comboText.rectTransform != null)
                    StartCoroutine(AnimationHelper.PulseScale(
                        _comboText.rectTransform, 1f, 1.18f, AnimationHelper.ComboPulseDuration));
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
                    InRunUiFactory.PassiveLabelColor);
                _passiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var def = SudokuRoguelike.Classes.ClassCatalog.GetDefinition(run.State.ClassId);
            _passiveText.text = def != null ? $"Passive: {def.PassiveDescription}" : "";
        }

        // ────────────────────── Bag panel ──────────────────────

        private void EnsureBagPanel()
        {
            var state = _map?.Run?.State;

            // Bag is irrelevant in Tutorial and Seasonal Challenge — hide and bail out.
            if (state?.Mode == GameMode.Tutorial || state?.Mode == GameMode.SeasonalChallenge)
            {
                if (_bagPanel != null) _bagPanel.SetActive(false);
                return;
            }

            if (_bagPanel != null) _bagPanel.SetActive(true);
            var slotCount  = Mathf.Max(1, state?.ItemSlots ?? 3);
            var relicCount = state?.HeldRelics?.Count ?? 0;

            if (_bagPanel == null)
            {
                if (_sudokuPanel == null) return;
                var pr = _sudokuPanel.GetComponent<RectTransform>();
                if (pr == null) return;

                // Build the permanent shell (background, title, dividers) — runs once.
                var bagGo = new GameObject("BagPanel", typeof(RectTransform), typeof(Image));
                bagGo.transform.SetParent(pr, false);
                var bagRt = bagGo.GetComponent<RectTransform>();
                bagRt.anchorMin = new Vector2(0.01f, 0.03f);
                bagRt.anchorMax = new Vector2(0.21f, 0.74f);
                bagRt.offsetMin = Vector2.zero;
                bagRt.offsetMax = Vector2.zero;
                bagGo.GetComponent<Image>().color = InRunUiFactory.BagPanelBg;
                _bagPanel = bagGo;

                var title = InRunUiFactory.CreateText(bagGo.transform, "BagTitle", "BAG", 13,
                    TextAnchor.MiddleCenter, InRunUiFactory.AccentGold);
                title.rectTransform.anchorMin = new Vector2(0.02f, 0.93f);
                title.rectTransform.anchorMax = new Vector2(0.98f, 1.00f);
                title.rectTransform.offsetMin = Vector2.zero;
                title.rectTransform.offsetMax = Vector2.zero;

                // Items/Relics divider
                var divGo = new GameObject("BagDivider", typeof(RectTransform), typeof(Image));
                divGo.transform.SetParent(bagGo.transform, false);
                var divRt = divGo.GetComponent<RectTransform>();
                divRt.anchorMin = new Vector2(0.05f, 0.47f);
                divRt.anchorMax = new Vector2(0.95f, 0.49f);
                divRt.offsetMin = Vector2.zero;
                divRt.offsetMax = Vector2.zero;
                divGo.GetComponent<Image>().color = GamePalette.AccentGoldSubtle;

                // Relics section label
                var relicTitle = InRunUiFactory.CreateText(bagGo.transform, "RelicTitle", "RELICS", 9,
                    TextAnchor.MiddleCenter, new Color(0.75f, 0.65f, 0.40f, 0.90f));
                relicTitle.rectTransform.anchorMin = new Vector2(0.02f, 0.44f);
                relicTitle.rectTransform.anchorMax = new Vector2(0.98f, 0.47f);
                relicTitle.rectTransform.offsetMin = Vector2.zero;
                relicTitle.rectTransform.offsetMax = Vector2.zero;
            }

            if (_bagPanel == null) return;

            if (slotCount != _builtForSlotCount)
                RebuildBagSlots(slotCount);

            if (relicCount != _builtForRelicCount)
                RebuildRelicSlots(relicCount);
        }

        private void RebuildBagSlots(int slotCount)
        {
            // Tear down existing item buttons.
            foreach (var btn in _bagItemButtons)
                if (btn != null) Destroy(btn.gameObject);
            _bagItemButtons.Clear();
            _selectedBagSlot = -1;

            // Tear down the scroll container if one was built for a previous slot count.
            if (_bagScrollContainer != null)
            {
                Destroy(_bagScrollContainer);
                _bagScrollContainer = null;
            }

            if (slotCount <= 3)
                BuildFixedSlots(slotCount);
            else
                BuildScrollableSlots(slotCount);

            _builtForSlotCount = slotCount;
        }

        private void RebuildRelicSlots(int relicCount)
        {
            foreach (var btn in _bagRelicButtons)
                if (btn != null) Destroy(btn.gameObject);
            _bagRelicButtons.Clear();

            if (_bagRelicScrollContainer != null)
            {
                Destroy(_bagRelicScrollContainer);
                _bagRelicScrollContainer = null;
            }

            if (relicCount == 0)
            {
                // Show placeholder "No Relic" label inside the relic area
                _builtForRelicCount = 0;
                return;
            }

            // Relic area: 0.02-0.43 (bottom half of bag, below divider)
            // Pack up to 4 mini-icons per row (each ~25% width)
            const int perRow    = 4;
            const float relicAreaBottom = 0.02f;
            const float relicAreaTop    = 0.43f;
            const float relicAreaH = relicAreaTop - relicAreaBottom;
            var rows   = Mathf.CeilToInt(relicCount / (float)perRow);
            var slotH  = rows > 0 ? relicAreaH / rows : relicAreaH;
            var slotW  = 1f / perRow;

            for (var i = 0; i < relicCount; i++)
            {
                var row = i / perRow;
                var col = i % perRow;
                var xMin = col * slotW + 0.01f;
                var xMax = xMin + slotW - 0.02f;
                var yMax = relicAreaTop - row * slotH;
                var yMin = yMax - slotH + 0.01f;

                var go = new GameObject($"RelicSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_bagPanel.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(xMin, yMin);
                rt.anchorMax = new Vector2(xMax, yMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = InRunUiFactory.BagSlotBg;

                // Icon fills the slot (no text — compact 25% size)
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.05f, 0.15f);
                iconRt.anchorMax = new Vector2(0.95f, 0.85f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                iconGo.GetComponent<Image>().raycastTarget = false;

                var btn = go.GetComponent<Button>();
                var idx = i;
                btn.onClick.AddListener(() => OnBagRelicClicked(idx));
                _bagRelicButtons.Add(btn);
            }

            _builtForRelicCount = relicCount;
        }

        // Item area within the bag panel (local Y, between divider and title).
        // Bag is now split 50/50: items top half, relics bottom half.
        private const float SlotAreaBottom = 0.50f;
        private const float SlotAreaTop    = 0.92f;

        // 1–3 slots: evenly distribute with small gaps inside the item area.
        private void BuildFixedSlots(int n)
        {
            const float gap = 0.01f;
            var slotH = (SlotAreaTop - SlotAreaBottom - gap * (n - 1)) / n;

            for (var i = 0; i < n; i++)
            {
                var yMax = SlotAreaTop - i * (slotH + gap);
                var yMin = yMax - slotH;
                var btn = InRunUiFactory.CreateBagSlotButton(_bagPanel.transform, $"BagSlot_{i}",
                    new Vector2(0.03f, yMin), new Vector2(0.97f, yMax));
                _bagItemButtons.Add(btn);
                var idx = i;
                btn.onClick.AddListener(() => OnBagItemClicked(idx));
            }
        }

        // 4+ slots: ScrollRect with VerticalLayoutGroup so slots are reachable by
        // mouse scroll wheel, drag, and EventSystem focus navigation (controller/keyboard).
        private void BuildScrollableSlots(int n)
        {
            // Container spans the item area and hosts the ScrollRect.
            var containerGo = new GameObject("BagScrollContainer", typeof(RectTransform), typeof(Image));
            containerGo.transform.SetParent(_bagPanel.transform, false);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.03f, SlotAreaBottom);
            containerRt.anchorMax = new Vector2(0.97f, SlotAreaTop);
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;
            containerGo.GetComponent<Image>().color = Color.clear;
            _bagScrollContainer = containerGo;

            // Viewport with Mask so clipping works.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewportGo.transform.SetParent(containerGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = Color.clear;
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content: top-anchored, expands downward as buttons are added.
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.spacing                = 3f;
            vlg.padding                = new RectOffset(0, 0, 1, 1);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = containerGo.AddComponent<ScrollRect>();
            scrollRect.viewport         = viewportRt;
            scrollRect.content          = contentRt;
            scrollRect.horizontal       = false;
            scrollRect.vertical         = true;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.movementType     = ScrollRect.MovementType.Clamped;

            for (var i = 0; i < n; i++)
            {
                var btn = InRunUiFactory.CreateBagSlotButton(contentGo.transform, $"BagSlot_{i}",
                    Vector2.zero, Vector2.one);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight       = 40f;
                le.preferredHeight = 40f;
                _bagItemButtons.Add(btn);
                var idx = i;
                btn.onClick.AddListener(() => OnBagItemClicked(idx));
            }
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
                        iconImg.color = spr != null ? Color.white : InRunUiFactory.IconNoSprite;
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
                    if (iconImg != null) { iconImg.sprite = null; iconImg.color = InRunUiFactory.IconEmpty; }
                    if (nameText != null)
                    {
                        nameText.text = "Empty";
                        nameText.color = InRunUiFactory.SlotEmptyText;
                    }
                }
            }

            if (_bagRelicButton != null)
            {
                var iconImg = _bagRelicButton.transform.Find("Icon")?.GetComponent<Image>();
                var nameText = _bagRelicButton.transform.Find("NameText")?.GetComponent<Text>();
                var relics = state?.HeldRelics;
                var hasAny = relics != null && relics.Count > 0;
                if (hasAny)
                {
                    var relic = relics[0];
                    _bagRelicButton.interactable = true;
                    if (iconImg != null)
                    {
                        var spr = Resources.Load<Sprite>("GeneratedIcons/icon_" + RelicService.GetIconName(relic.Id));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : InRunUiFactory.IconNoSprite;
                        iconImg.preserveAspect = true;
                    }
                    if (nameText != null)
                    {
                        var useTxt = relic.UsesRemaining < 0 ? "Passive"
                            : relic.UsesRemaining == 0 ? "Spent" : $"\u00d7{relic.UsesRemaining}";
                        nameText.text = $"{RelicService.GetRelicName(relic.Id)}\n<size=8>{useTxt}</size>";
                        nameText.color = GamePalette.AccentGold;
                    }
                }
                else
                {
                    _bagRelicButton.interactable = false;
                    if (iconImg != null) { iconImg.sprite = null; iconImg.color = InRunUiFactory.IconEmpty; }
                    if (nameText != null) { nameText.text = "No Relic"; nameText.color = InRunUiFactory.SlotEmptyText; }
                }
            }

            // Refresh compact relic mini-icons
            var heldRelics = state?.HeldRelics;
            for (var ri = 0; ri < _bagRelicButtons.Count; ri++)
            {
                var btn = _bagRelicButtons[ri];
                if (btn == null) continue;
                var iconImg = btn.transform.Find("Icon")?.GetComponent<Image>();
                var relic   = (heldRelics != null && ri < heldRelics.Count) ? heldRelics[ri] : null;
                if (relic != null && iconImg != null)
                {
                    var spr = Resources.Load<Sprite>("GeneratedIcons/icon_" + RelicService.GetIconName(relic.Id));
                    iconImg.sprite = spr;
                    iconImg.color = spr != null ? Color.white : InRunUiFactory.IconNoSprite;
                    iconImg.preserveAspect = true;
                }
                else if (iconImg != null)
                {
                    iconImg.sprite = null;
                    iconImg.color = InRunUiFactory.IconEmpty;
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
                    ? InRunUiFactory.BagHighlightSelected
                    : InRunUiFactory.BagSlotBg;
            }
        }

        private void OnBagRelicClicked(int idx = 0)
        {
            var state = _map?.Run?.State;
            if (state?.HeldRelics == null || idx >= state.HeldRelics.Count) return;
            var relic = state.HeldRelics[idx];
            OnStatusChanged?.Invoke($"{RelicService.GetRelicName(relic.Id)}: {RelicService.GetRelicDescription(relic.Id)}");
        }
    }
}
