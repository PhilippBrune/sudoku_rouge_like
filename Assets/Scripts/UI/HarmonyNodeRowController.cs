using System;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [HARMONY-UI-002] Horizontal node chain UI for Harmony level selection.
    /// Displays 11 nodes (H0–H10), each in one of three states: locked, unlocked, selected.
    /// Clicking an unlocked node selects it and fires <see cref="OnNodeSelected"/>.
    /// Also drives the summary card beneath the chain.
    /// </summary>
    public sealed class HarmonyNodeRowController : MonoBehaviour
    {
        // ── Inspector wiring ──────────────────────────────────────────────────────────────────
        [Header("Node row")]
        [SerializeField] private HarmonyNodeButton[] _nodes;    // 11 elements, H0..H10 in order

        [Header("Summary card")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _subtitleText;
        [SerializeField] private Text _hudLabelText;
        [SerializeField] private Text _xpMultText;

        // ── Runtime state ─────────────────────────────────────────────────────────────────────
        private int _maxUnlocked;
        private int _selected;

        /// <summary>Fired when the player taps a node. Arg = 0..10.</summary>
        public event Action<int> OnNodeSelected;

        // ── Public API ────────────────────────────────────────────────────────────────────────

        /// <summary>Initialise with the persisted meta state and refresh the entire row.</summary>
        public void BindMeta(MetaProgressionState meta)
        {
            _maxUnlocked = meta?.MaxUnlockedHarmonyLevel ?? 0;
            _selected    = Math.Clamp(meta?.LastSelectedHarmonyLevel ?? 0, 0, _maxUnlocked);
            Refresh();
        }

        /// <summary>Force-set the currently highlighted node (no event fired).</summary>
        public void SetSelectedLevel(int level)
        {
            _selected = Math.Clamp(level, 0, _maxUnlocked);
            Refresh();
        }

        /// <summary>Rebuilds all node visual states and the summary card from cached data.</summary>
        public void Refresh()
        {
            if (_nodes == null) return;

            for (var i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] == null) continue;
                var state = i > _maxUnlocked
                    ? HarmonyNodeState.Locked
                    : i == _selected
                        ? HarmonyNodeState.Selected
                        : HarmonyNodeState.Unlocked;

                _nodes[i].SetState(state);
            }

            // MM-4: wire explicit left/right navigation between unlocked node buttons
            Button prevBtn = null;
            for (var i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] == null) continue;
                var btn = _nodes[i].GetComponent<UnityEngine.UI.Button>();
                if (btn == null || !btn.interactable) continue;

                var nav = btn.navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                nav.selectOnLeft  = prevBtn;
                nav.selectOnRight = null; // filled in by next iteration
                btn.navigation = nav;

                if (prevBtn != null)
                {
                    var prevNav = prevBtn.navigation;
                    prevNav.selectOnRight = btn;
                    prevBtn.navigation = prevNav;
                }
                prevBtn = btn;
            }

            RefreshSummaryCard(_selected);
        }

        // ── Called by each node button on click ───────────────────────────────────────────────

        /// <summary>Unity-message callback from a <see cref="HarmonyNodeButton"/> child.</summary>
        public void HandleNodeClicked(int level)
        {
            if (level > _maxUnlocked) return;
            _selected = level;
            Refresh();
            OnNodeSelected?.Invoke(_selected);
        }

        // ── Private helpers ───────────────────────────────────────────────────────────────────

        private void RefreshSummaryCard(int level)
        {
            if (_titleText   != null) _titleText.text    = HarmonyDifficultyService.GetDisplayName(level);
            if (_hudLabelText != null) _hudLabelText.text = HarmonyDifficultyService.GetHudLabel(level);
            if (_xpMultText  != null) _xpMultText.text   = $"XP ×{HarmonyDifficultyService.GetXpMultiplier(level):0.0}";

            if (_subtitleText != null)
            {
                _subtitleText.text = level == 0
                    ? "Standard difficulty — no penalty."
                    : level == 10
                        ? "Maximum difficulty. No rest heal. All modifiers active."
                        : $"Difficulty level {level}/10";
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Helper enum and stub button component referenced by HarmonyNodeRowController.
    // In the real scene, each node GameObject has a HarmonyNodeButton attached and calls
    // GetComponentInParent<HarmonyNodeRowController>().HandleNodeClicked(levelIndex).
    // ─────────────────────────────────────────────────────────────────────────────────────────

    public enum HarmonyNodeState { Locked, Unlocked, Selected }

    /// <summary>
    /// Single node in the Harmony level chain. Manages its own visual state.
    /// Assign <see cref="LevelIndex"/> in the Inspector.
    /// </summary>
    public sealed class HarmonyNodeButton : MonoBehaviour
    {
        [SerializeField] public int LevelIndex;

        [Header("Sprites")]
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private Sprite _unlockedSprite;
        [SerializeField] private Sprite _selectedSprite;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        public void SetState(HarmonyNodeState state)
        {
            if (_image != null)
            {
                _image.sprite = state switch
                {
                    HarmonyNodeState.Locked   => _lockedSprite,
                    HarmonyNodeState.Selected => _selectedSprite,
                    _                         => _unlockedSprite
                };
            }

            if (_button != null)
                _button.interactable = state != HarmonyNodeState.Locked;
        }

        private void OnClick()
        {
            GetComponentInParent<HarmonyNodeRowController>()?.HandleNodeClicked(LevelIndex);
        }
    }
}
