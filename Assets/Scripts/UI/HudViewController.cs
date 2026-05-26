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
        private Image _classBadgeIcon;
        private Text  _classBadgeText;

        // ── HUD bar images ──
        private Image _hpBarFill;
        private Image _pencilBarFill;
        // Shield badge: overlaid on the HP bar fill when ShieldPoints > 0
        private Text _shieldBadgeText;
        // Cached normal HP bar colour to restore when shield expires
        private static readonly Color HpBarNormal = new Color(0.80f, 0.22f, 0.22f, 1f);
        private static readonly Color HpBarShielded = new Color(0.28f, 0.58f, 0.90f, 1f);

        // ── Floor modifier display ──
        private GameObject _floorModBanner;
        private Text _floorModText;
        private GameObject _modifierInfoBox;
        private Text _modifierInfoText;

        // ── Combo / Passive ──
        private GameObject _comboBox;
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

        // ── Dirty-flag cache: avoid per-frame string allocations when values haven't changed ──
        private int _cachedHp = -1, _cachedMaxHp = -1, _cachedShield = -1;
        private int _cachedPencil = -1, _cachedMaxPencil = -1;
        private int _cachedHpBarHp = -1, _cachedHpBarMax = -1;
        private int _cachedPencilBarPencil = -1, _cachedPencilBarMax = -1;
        private int _cachedTimerSec = -2, _cachedRunTimerSec = -2;

        // ── Bag icon cache: sprites preloaded on first use, reused on every subsequent frame ──
        private readonly Dictionary<string, Sprite> _bagIconCache = new Dictionary<string, Sprite>();
        private Sprite _emptySlotSprite;
        // Pip/badge dirty check: -1 = empty slot; >=0 = enum cast to int
        private int[] _lastItemSlotTypes;
        private int[] _lastItemSlotRarities;
        // Class badge cache: computed once per puzzle start; level/prestige don't change mid-run
        private string _cachedClassBadgeText;
        private Color  _cachedClassBadgeColor = Color.white;
        private bool   _classBadgeCacheValid;
        // Floor mod banner cache: mod list is constant for the duration of a puzzle
        private object _cachedFloorModsRef;
        private string _cachedFloorModText;
        private bool   _cachedFloorModIsSpecial;
        // Modifier info box cache: mod list and listener are constant for the duration of a puzzle
        private object _cachedModInfoModsRef;
        private string _cachedModInfoText;


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
            RemoveLegacyTimerBacking();
        }

        private void RemoveLegacyTimerBacking()
        {
            var timerBg = _sudokuPanel != null ? _sudokuPanel.transform.Find("TimerBg") : null;
            if (timerBg == null) return;

            if (Application.isPlaying)
                Destroy(timerBg.gameObject);
            else
                DestroyImmediate(timerBg.gameObject);
        }

        // Cached sprite lookup — loads from Resources on first access, returns cached on subsequent calls.
        private Sprite GetBagIcon(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_bagIconCache.TryGetValue(path, out var cached)) return cached;
            var s = InRunUiFactory.LoadResourceSprite(path);
            _bagIconCache[path] = s;
            return s;
        }

        private Sprite GetFullTextureBagIcon(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var cacheKey = path + "#full";
            if (_bagIconCache.TryGetValue(cacheKey, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>(path);
            var sprite = texture != null
                ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                : GetBagIcon(path);

            _bagIconCache[cacheKey] = sprite;
            return sprite;
        }

        private static void SetBagItemIconLayout(Image iconImg, bool empty)
        {
            if (iconImg == null) return;

            var rt = iconImg.rectTransform;
            if (empty)
            {
                rt.anchorMin = new Vector2(0.05f, 0.16f);
                rt.anchorMax = new Vector2(0.31f, 0.84f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.04f, 0.12f);
                rt.anchorMax = new Vector2(0.36f, 0.88f);
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        public void StartPuzzleTimer()
        {
            _puzzleStartTime = Time.realtimeSinceStartup;
            _classBadgeCacheValid = false; // re-read level/prestige at puzzle start
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
                OnStatusChanged?.Invoke(F("InRun.Hud.Status.ItemControllerHint", ItemService.GetItemName(item.Type), desc));
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
            OnStatusChanged?.Invoke(F("InRun.Hud.Status.ItemInspect", name, RarityLabel(item.Rarity), desc));
        }

        public void Refresh(RunState state, LevelConfig levelConfig)
        {
            if (state == null) return;

            if (_hpText != null && (state.CurrentHP != _cachedHp || state.MaxHP != _cachedMaxHp))
            {
                _cachedHp    = state.CurrentHP;
                _cachedMaxHp = state.MaxHP;
                _hpText.text = F("InRun.Hud.Hp", state.CurrentHP, state.MaxHP);
            }
            if (_pencilText != null && (state.CurrentPencil != _cachedPencil || state.MaxPencil != _cachedMaxPencil))
            {
                _cachedPencil    = state.CurrentPencil;
                _cachedMaxPencil = state.MaxPencil;
                _pencilText.text = F("InRun.Hud.Pencil", state.CurrentPencil, state.MaxPencil);
            }

            if (_hpBarFill == null && _sudokuPanel != null)
            {
                var pr = _sudokuPanel.GetComponent<RectTransform>();
                var hpBg = pr?.Find("HpBarBg");
                if (hpBg != null) _hpBarFill = hpBg.Find("HpBarFill")?.GetComponent<Image>();
                var pBg = pr?.Find("PencilBarBg");
                if (pBg != null) _pencilBarFill = pBg.Find("PencilBarFill")?.GetComponent<Image>();
            }

            RemoveLegacyTimerBacking();

            // Timer display — lazy-created the first time
            if (_timerText == null && _sudokuPanel != null)
            {
                _timerText = InRunUiFactory.CreateText(_sudokuPanel.transform, "PuzzleTimer",
                    "", 15, TextAnchor.MiddleRight, new Color(1.00f, 0.96f, 0.74f, 1.00f));
                var rt = _timerText.rectTransform;
                rt.anchorMin = new Vector2(0.74f, 0.94f);
                rt.anchorMax = new Vector2(0.93f, 1.00f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _timerText.fontStyle = FontStyle.Bold;
                _timerText.raycastTarget = false;
                var shadow = _timerText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
                shadow.effectDistance = new Vector2(1f, -1f);
                var outline = _timerText.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.90f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            // Run total timer — positioned just below puzzle timer
            if (_runTimerText == null && _sudokuPanel != null)
            {
                _runTimerText = InRunUiFactory.CreateText(_sudokuPanel.transform, "RunTimer",
                    "", 12, TextAnchor.MiddleRight, new Color(0.96f, 0.91f, 0.68f, 0.98f));
                var rt = _runTimerText.rectTransform;
                rt.anchorMin = new Vector2(0.74f, 0.88f);
                rt.anchorMax = new Vector2(0.93f, 0.94f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _runTimerText.fontStyle = FontStyle.Bold;
                _runTimerText.raycastTarget = false;
                var shadow = _runTimerText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
                shadow.effectDistance = new Vector2(1f, -1f);
                var outline = _runTimerText.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.90f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            if (_timerText != null)
            {
                if (state.Mode == GameMode.EndlessZen)
                {
                    if (_cachedTimerSec != -1) { _cachedTimerSec = -1; _timerText.text = string.Empty; }
                }
                else if (_puzzleStartTime >= 0f)
                {
                    var elapsed = (int)(Time.realtimeSinceStartup - _puzzleStartTime);
                    if (elapsed != _cachedTimerSec)
                    {
                        _cachedTimerSec = elapsed;
                        _timerText.text = $"{elapsed / 60:00}:{elapsed % 60:00}";
                    }
                }
                else
                {
                    if (_cachedTimerSec != -1) { _cachedTimerSec = -1; _timerText.text = string.Empty; }
                }
            }

            if (_runTimerText != null && state != null)
            {
                var totalSec = (int)state.TotalRunSeconds;
                if (_puzzleStartTime >= 0f)
                    totalSec += (int)(Time.realtimeSinceStartup - _puzzleStartTime);
                if (state.Mode == GameMode.EndlessZen)
                {
                    if (_cachedRunTimerSec != -1) { _cachedRunTimerSec = -1; _runTimerText.text = string.Empty; }
                }
                else if (totalSec > 0)
                {
                    if (totalSec != _cachedRunTimerSec)
                    {
                        _cachedRunTimerSec = totalSec;
                        _runTimerText.text = F("InRun.Hud.RunTimer", $"{totalSec / 60:00}:{totalSec % 60:00}");
                    }
                }
                else
                {
                    if (_cachedRunTimerSec != 0) { _cachedRunTimerSec = 0; _runTimerText.text = string.Empty; }
                }
            }

            if (_hpBarFill != null && (state.CurrentHP != _cachedHpBarHp || state.MaxHP != _cachedHpBarMax || state.ShieldPoints != _cachedShield))
            {
                _cachedHpBarHp = state.CurrentHP;
                _cachedHpBarMax = state.MaxHP;
                _cachedShield = state.ShieldPoints;
                InRunUiFactory.EnsureBarSprite(_hpBarFill);
                _hpBarFill.fillAmount = state.MaxHP > 0 ? Mathf.Clamp01(state.CurrentHP / (float)state.MaxHP) : 0;
                _hpBarFill.color = state.ShieldPoints > 0 ? HpBarShielded : HpBarNormal;
                // Lazy-create shield badge text overlaid on the HP bar
                if (_shieldBadgeText == null)
                {
                    var badgeGo = new GameObject("ShieldBadge", typeof(RectTransform), typeof(Text));
                    badgeGo.transform.SetParent(_hpBarFill.transform, false);
                    var badgeRt = badgeGo.GetComponent<RectTransform>();
                    badgeRt.anchorMin = Vector2.zero;
                    badgeRt.anchorMax = Vector2.one;
                    badgeRt.offsetMin = Vector2.zero;
                    badgeRt.offsetMax = Vector2.zero;
                    _shieldBadgeText = badgeGo.GetComponent<Text>();
                    _shieldBadgeText.font = FontAssetService.GetFont();
                    _shieldBadgeText.fontSize = 10;
                    _shieldBadgeText.fontStyle = FontStyle.Bold;
                    _shieldBadgeText.alignment = TextAnchor.MiddleCenter;
                    _shieldBadgeText.color = new Color(0.92f, 0.97f, 1.00f, 0.95f);
                    _shieldBadgeText.raycastTarget = false;
                }
                _shieldBadgeText.text = state.ShieldPoints > 0 ? $"\u26CA {state.ShieldPoints}" : string.Empty;
            }
            if (_pencilBarFill != null && (state.CurrentPencil != _cachedPencilBarPencil || state.MaxPencil != _cachedPencilBarMax))
            {
                _cachedPencilBarPencil = state.CurrentPencil;
                _cachedPencilBarMax = state.MaxPencil;
                InRunUiFactory.EnsureBarSprite(_pencilBarFill);
                _pencilBarFill.fillAmount = state.MaxPencil > 0 ? Mathf.Clamp01(state.CurrentPencil / (float)state.MaxPencil) : 0;
            }

            EnsureFloorModBanner();
            EnsureModifierInfoBox();
            EnsureComboCounter();
            EnsurePassiveLabel();
            EnsureClassBadge();
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
            var specialMode = run.State.Mode == GameMode.EndlessZen || run.State.Mode == GameMode.SpiritTrials;
            var mods = specialMode ? run.CurrentLevelConfig?.ActiveModifiers : run.State.ActiveFloorModifiers;

            // Hide the banner during boss puzzles: floor modifiers don't apply there,
            // so showing them would be misleading. Boss modifiers appear in the info box.
            if ((!specialMode && run.CurrentLevelConfig?.IsBoss == true) || mods == null || mods.Count == 0)
            {
                if (_floorModBanner != null) _floorModBanner.SetActive(false);
                return;
            }

            if (_floorModBanner == null && _sudokuPanel != null)
            {
                _floorModBanner = new GameObject("FloorModBanner", typeof(RectTransform), typeof(Image));
                _floorModBanner.transform.SetParent(_sudokuPanel.transform, false);
                var rt = _floorModBanner.GetComponent<RectTransform>();
                // Dedicated lane between title (0.93-1.00) and level info (0.83-0.875).
                rt.anchorMin = new Vector2(0.24f, 0.885f);
                rt.anchorMax = new Vector2(0.68f, 0.920f);
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
                if (!ReferenceEquals(mods, _cachedFloorModsRef) || specialMode != _cachedFloorModIsSpecial || _cachedFloorModText == null)
                {
                    _cachedFloorModsRef      = mods;
                    _cachedFloorModIsSpecial = specialMode;
                    var sb = new StringBuilder(T(specialMode ? "InRun.Hud.ActiveModifiers" : "InRun.Hud.FloorModifiers"));
                    for (var i = 0; i < mods.Count; i++)
                    {
                        if (i > 0) sb.Append(" | ");
                        sb.Append(BossService.GetModifierName(mods[i]));
                    }
                    _cachedFloorModText = sb.ToString();
                }
                _floorModText.text = _cachedFloorModText;
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
                rt.anchorMin = new Vector2(0.74f, 0.09f);
                rt.anchorMax = new Vector2(0.93f, 0.33f);
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
                // Only rebuild text and re-wire the listener when the mod list reference changes
                if (!ReferenceEquals(activeMods, _cachedModInfoModsRef) || _cachedModInfoText == null)
                {
                    _cachedModInfoModsRef = activeMods;
                    var sb = new StringBuilder();
                    for (var i = 0; i < activeMods.Count; i++)
                    {
                        if (i > 0) sb.Append('\n');
                        sb.Append(BossService.GetModifierName(activeMods[i]));
                        sb.Append(": ");
                        sb.Append(BossService.GetModifierDescription(activeMods[i]));
                    }
                    _cachedModInfoText = sb.ToString();

                    // Re-register listener only when the mod list changes
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
                _modifierInfoText.text = _cachedModInfoText;
            }
        }

        private void EnsureComboCounter()
        {
            var run = _map?.Run;
            if (run?.State == null || _sudokuPanel == null) return;

            if (_comboText == null)
            {
                var go = new GameObject("ComboCounter", typeof(RectTransform), typeof(Image));
                _comboBox = go;
                go.transform.SetParent(_sudokuPanel.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.24f, 0.885f);
                rt.anchorMax = new Vector2(0.68f, 0.920f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = InRunUiFactory.SudokuPuzzleBoxBg;
                go.GetComponent<Image>().raycastTarget = false;
                _comboText = InRunUiFactory.CreateText(go.transform, "ComboText", "", 18, TextAnchor.MiddleCenter,
                    GamePalette.AccentGold);
            }

            var streak = run.State.ComboStreak;
            if (streak >= 2)
            {
                var wasHidden = !_comboText.gameObject.activeSelf;
                _comboText.text = F("InRun.Hud.Combo", streak);
                if (_comboBox != null) _comboBox.SetActive(true);
                _comboText.gameObject.SetActive(true);
                if (_floorModBanner != null) _floorModBanner.SetActive(false);
                if (wasHidden && _comboText.rectTransform != null)
                    StartCoroutine(AnimationHelper.PulseScale(
                        _comboText.rectTransform, 1f, 1.18f, AnimationHelper.ComboPulseDuration));
            }
            else
            {
                if (_comboBox != null) _comboBox.SetActive(false);
                _comboText.gameObject.SetActive(false);
            }
        }

        private void EnsurePassiveLabel()
        {
            var run = _map?.Run;
            if (run?.State == null || _sudokuPanel == null) return;

            if (_passiveText == null)
            {
                var go = new GameObject("PassiveLabel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_sudokuPanel.transform, false);
                var rt = go.GetComponent<RectTransform>();
                // Compact lane below the bag panel; text size adapts instead of widening across the board.
                rt.anchorMin = new Vector2(0.01f, 0.01f);
                rt.anchorMax = new Vector2(0.21f, 0.09f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = InRunUiFactory.SudokuPuzzleBoxBg;
                go.GetComponent<Image>().raycastTarget = false;
                _passiveText = InRunUiFactory.CreateText(go.transform, "PassiveText", "", 11, TextAnchor.MiddleLeft,
                    InRunUiFactory.PassiveLabelColor);
                _passiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _passiveText.verticalOverflow = VerticalWrapMode.Truncate;
                _passiveText.resizeTextForBestFit = true;
                _passiveText.resizeTextMinSize = 8;
                _passiveText.resizeTextMaxSize = 11;
                _passiveText.lineSpacing = 0.90f;
                _passiveText.rectTransform.anchorMin = Vector2.zero;
                _passiveText.rectTransform.anchorMax = Vector2.one;
                _passiveText.rectTransform.offsetMin = new Vector2(6f, 2f);
                _passiveText.rectTransform.offsetMax = new Vector2(-6f, -2f);
            }

            var def = SudokuRoguelike.Classes.ClassCatalog.GetDefinition(run.State.ClassId);
            _passiveText.text = def != null
                ? F("InRun.Hud.Passive", LocalizationService.T($"Class.Passive.{def.Id}", def.PassiveDescription))
                : "";
        }

        // ────────────────────── Class XP badge ──────────────────────

        private void EnsureClassBadge()
        {
            var run = _map?.Run;
            if (run?.State == null || _sudokuPanel == null) return;

            if (_classBadgeIcon == null)
            {
                // HpPencilScrim already provides the shared backing for class, HP, and pencil.
                var badgeGo = new GameObject("ClassBadge", typeof(RectTransform));
                badgeGo.transform.SetParent(_sudokuPanel.transform, false);
                var rt = badgeGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.01f, 0.89f);
                rt.anchorMax = new Vector2(0.21f, 1.00f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var iconGo = new GameObject("ClassIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(badgeGo.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.04f, 0.10f);
                iconRt.anchorMax = new Vector2(0.36f, 0.90f);
                iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
                _classBadgeIcon = iconGo.GetComponent<Image>();
                _classBadgeIcon.preserveAspect = true;
                _classBadgeIcon.raycastTarget  = false;

                _classBadgeText = InRunUiFactory.CreateText(badgeGo.transform, "ClassBadgeText",
                    "", 9, TextAnchor.MiddleLeft, InRunUiFactory.TextColor);
                _classBadgeText.rectTransform.anchorMin = new Vector2(0.38f, 0.04f);
                _classBadgeText.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);
                _classBadgeText.rectTransform.offsetMin = _classBadgeText.rectTransform.offsetMax = Vector2.zero;
                _classBadgeText.raycastTarget = false;
            }

            // Refresh icon (only load once per class since ClassId doesn't change in-run)
            var classId = run.State.ClassId;
            var classDef = SudokuRoguelike.Classes.ClassCatalog.GetDefinition(classId);
            if (classDef != null)
            {
                var iconName = SudokuRoguelike.Classes.ClassCatalog.GetIconName(classId);
                if (_classBadgeIcon.sprite == null)
                    _classBadgeIcon.sprite = Resources.Load<Sprite>("class/icon_" + iconName);

                // Populate badge cache once per puzzle; level/prestige don't change mid-run
                if (!_classBadgeCacheValid)
                {
                    _classBadgeCacheValid = true;
                    var garden = new SudokuRoguelike.Meta.ClassGardenProgressionService();
                    var meta   = _map.Profile?.Meta;
                    if (meta != null)
                    {
                        var level    = garden.GetLevel(meta, classId);
                        var prestige = garden.GetPrestigeTier(meta, classId);
                        _cachedClassBadgeText  = prestige > 0
                            ? $"{classDef.Name}\nLv {level} ★{prestige}"
                            : $"{LocalizationService.T(classDef.Name)}\nLv {level}";
                        if (prestige > 0)
                            _cachedClassBadgeText = F(
                                "InRun.Hud.ClassBadge.Prestige",
                                LocalizationService.T(classDef.Name),
                                level,
                                prestige);
                        else
                            _cachedClassBadgeText = F(
                                "InRun.Hud.ClassBadge",
                                LocalizationService.T(classDef.Name),
                                level);
                        _cachedClassBadgeColor = prestige >= 5
                            ? new Color(1.00f, 0.80f, 0.25f, 1f)
                            : prestige >= 1
                                ? new Color(0.85f, 0.70f, 0.50f, 1f)
                                : Color.white;
                    }
                    else
                    {
                        _cachedClassBadgeText  = LocalizationService.T(classDef.Name);
                        _cachedClassBadgeColor = Color.white;
                    }
                }
                _classBadgeText.text  = _cachedClassBadgeText ?? "";
                _classBadgeIcon.color = _cachedClassBadgeColor;
            }
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
                // L5: bag bottom raised to y=0.10 so its lower edge aligns with the right-column description bottom
                bagRt.anchorMin = new Vector2(0.01f, 0.10f);
                bagRt.anchorMax = new Vector2(0.21f, 0.74f);
                bagRt.offsetMin = Vector2.zero;
                bagRt.offsetMax = Vector2.zero;
                bagGo.GetComponent<Image>().color = InRunUiFactory.BagPanelBg;
                _bagPanel = bagGo;

                var title = InRunUiFactory.CreateText(bagGo.transform, "BagTitle", T("InRun.Hud.BagTitle"), 13,
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
                var relicTitle = InRunUiFactory.CreateText(bagGo.transform, "RelicTitle", T("InRun.Hud.RelicsTitle"), 13,
                    TextAnchor.MiddleCenter, InRunUiFactory.AccentGold);
                relicTitle.rectTransform.anchorMin = new Vector2(0.02f, 0.43f);
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

            // Relic area: 0.02-0.43 (bottom half of bag, below divider).
            // Pack up to 4 mini-icons per row; each slot is square, with width controlling height.
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
                var aspect = go.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                aspect.aspectRatio = 1f;
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

            // Ensure pip/badge tracking arrays are large enough (lazy-sized to actual button count)
            var slotCount = _bagItemButtons.Count;
            if (_lastItemSlotTypes == null || _lastItemSlotTypes.Length < slotCount)
            {
                _lastItemSlotTypes    = new int[slotCount];
                _lastItemSlotRarities = new int[slotCount];
                for (var j = 0; j < slotCount; j++) { _lastItemSlotTypes[j] = -1; _lastItemSlotRarities[j] = -1; }
            }

            for (var i = 0; i < slotCount; i++)
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
                        SetBagItemIconLayout(iconImg, false);
                        var spr = GetBagIcon("items/icon_" + ItemService.GetIconName(item.Type));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : InRunUiFactory.IconNoSprite;
                        iconImg.preserveAspect = true;
                        // Only rebuild pips/badge when slot contents changed
                        var newType   = (int)item.Type;
                        var newRarity = (int)item.Rarity;
                        if (newType != _lastItemSlotTypes[i] || newRarity != _lastItemSlotRarities[i])
                        {
                            _lastItemSlotTypes[i]    = newType;
                            _lastItemSlotRarities[i] = newRarity;
                            InRunUiFactory.ClearNamedChildren(iconImg.transform, "RarityPip");
                            InRunUiFactory.ClearNamedChildren(iconImg.transform, "ClassBadge");
                            InRunUiFactory.AddRarityPip(iconImg.transform, item.Rarity);
                            if (ItemService.IsClassExclusive(item.Type))
                            {
                                var exclusiveClass = ItemService.GetExclusiveClass(item.Type);
                                var badgeColor = GamePalette.GetClassColor(exclusiveClass);
                                var badge = new GameObject("ClassBadge", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                                badge.transform.SetParent(iconImg.transform, false);
                                var bRt = badge.GetComponent<RectTransform>();
                                bRt.anchorMin = Vector2.zero;
                                bRt.anchorMax = new Vector2(0.38f, 0.38f);
                                bRt.offsetMin = Vector2.zero;
                                bRt.offsetMax = Vector2.zero;
                                var bImg = badge.GetComponent<UnityEngine.UI.Image>();
                                var classIconName = SudokuRoguelike.Classes.ClassCatalog.GetIconName(exclusiveClass);
                                if (!string.IsNullOrEmpty(classIconName))
                                {
                                    var classSprite = GetBagIcon("class/icon_" + classIconName);
                                    if (classSprite != null) { bImg.sprite = classSprite; bImg.preserveAspect = true; }
                                }
                                bImg.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.30f);
                                bImg.raycastTarget = false;
                            }
                        }
                    }
                    if (nameText != null)
                    {
                        var chargeTxt = item.Charges > 1 ? $" \u00d7{item.Charges}" : "";
                        nameText.text = F("InRun.Hud.ItemSlot", ItemService.GetItemName(item.Type), chargeTxt, RarityLabel(item.Rarity));
                        nameText.color = InRunUiFactory.TextColor;
                    }
                }
                else
                {
                    btn.interactable = false;
                    // If slot just became empty, clear any stale pip/badge overlays left from the previous item
                    if (_lastItemSlotTypes[i] >= 0)
                    {
                        _lastItemSlotTypes[i]    = -1;
                        _lastItemSlotRarities[i] = -1;
                        if (iconImg != null)
                        {
                            InRunUiFactory.ClearNamedChildren(iconImg.transform, "RarityPip");
                            InRunUiFactory.ClearNamedChildren(iconImg.transform, "ClassBadge");
                        }
                    }
                    if (iconImg != null)
                    {
                        SetBagItemIconLayout(iconImg, true);
                        if (_emptySlotSprite == null) _emptySlotSprite = GetFullTextureBagIcon("economy/icon_empty_slot");
                        iconImg.sprite = _emptySlotSprite;
                        iconImg.color = _emptySlotSprite != null ? InRunUiFactory.IconEmpty : Color.clear;
                        iconImg.preserveAspect = true;
                    }
                    if (nameText != null)
                    {
                        nameText.text = T("InRun.Common.Empty");
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
                        var spr = GetBagIcon(RelicService.GetIconFolder(relic.Id) + "/icon_" + RelicService.GetIconName(relic.Id));
                        iconImg.sprite = spr;
                        iconImg.color = spr != null ? Color.white : InRunUiFactory.IconNoSprite;
                        iconImg.preserveAspect = true;
                        // F4: add rarity pip to relic bag slot (maps RelicTier → ItemRarity pip color)
                        InRunUiFactory.ClearNamedChildren(iconImg.transform, "RarityPip");
                        InRunUiFactory.ClearNamedChildren(iconImg.transform, "RelicTier");
                        InRunUiFactory.AddRelicTierPip(iconImg.transform, relic.Tier);
                    }
                    if (nameText != null)
                    {
                        var useTxt = relic.UsesRemaining < 0 ? T("InRun.Hud.RelicPassive")
                            : relic.UsesRemaining == 0 ? T("InRun.Hud.RelicSpent") : $"\u00d7{relic.UsesRemaining}";
                        nameText.text = F("InRun.Hud.RelicSlot", RelicService.GetRelicName(relic.Id), useTxt);
                        nameText.color = GamePalette.AccentGold;
                    }
                }
                else
                {
                    _bagRelicButton.interactable = false;
                    if (iconImg != null)
                    {
                        iconImg.sprite = null;
                        iconImg.color = InRunUiFactory.IconEmpty;
                        InRunUiFactory.ClearNamedChildren(iconImg.transform, "RelicTier");
                    }
                    if (nameText != null) { nameText.text = T("InRun.Hud.NoRelic"); nameText.color = InRunUiFactory.SlotEmptyText; }
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
                    var spr = GetBagIcon(RelicService.GetIconFolder(relic.Id) + "/icon_" + RelicService.GetIconName(relic.Id));
                    iconImg.sprite = spr;
                    iconImg.color = spr != null ? Color.white : InRunUiFactory.IconNoSprite;
                    iconImg.preserveAspect = true;
                    InRunUiFactory.ClearNamedChildren(iconImg.transform, "RelicTier");
                    InRunUiFactory.AddRelicTierPip(iconImg.transform, relic.Tier);
                }
                else if (iconImg != null)
                {
                    iconImg.sprite = null;
                    iconImg.color = InRunUiFactory.IconEmpty;
                    InRunUiFactory.ClearNamedChildren(iconImg.transform, "RelicTier");
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
                OnStatusChanged?.Invoke(F("InRun.Hud.Status.ItemClickHint", ItemService.GetItemName(item.Type), desc));
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
            OnStatusChanged?.Invoke(F("InRun.Hud.Status.RelicInspect", RelicService.GetRelicName(relic.Id), RelicService.GetRelicDescription(relic.Id)));
        }

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);

        private static string RarityLabel(ItemRarity rarity) =>
            LocalizationService.T($"Item.Rarity.{rarity}", rarity.ToString());
    }
}
