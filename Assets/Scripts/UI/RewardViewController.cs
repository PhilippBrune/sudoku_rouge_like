using System;
using System.Collections;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the reward screen (RewardViewController.Tally.cs), the relic shrine (RewardViewController.Shrine.cs), the
    /// bag-swap panel, the rest choice panel and event handling.
    /// </summary>
    public sealed partial class RewardViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _pathPanel;

        // Bag-swap
        private GameObject _bagSwapPanel;
        private ItemInstance _pendingSwapItem;
        private GameObject _panelBeforeSwap;
        private Action<int> _onSwapSlotChosen;
        private Action _onSwapAborted;

        public Action<string> OnStatusChanged;
        public Action OnPostRewardReady;

        /// <summary>Non-null and active while any overlay panel is open; used by InRunController for controller focus.</summary>
        public GameObject ActivePanel { get; private set; }

        public void Configure(RunMapController map, GameObject pathPanel)
        {
            _map = map;
            _pathPanel = pathPanel;
            _rewardSlots = new List<ItemInstance>();
        }

        // ────────────────────── Bag-full swap panel ──────────────────────

        public void ShowBagSwapPanel(ItemInstance newItem, Action<int> onReplace, Action onAbort)
        {
            _pendingSwapItem = newItem;

            // Wrap callbacks so the swap panel is always dismissed before the
            // caller's logic runs, and neither callback can fire twice even on
            // rapid double-taps.
            // The panel that was open before the swap (the reward or the shop) gets the focus back afterwards.
            _panelBeforeSwap = ActivePanel;
            _onSwapSlotChosen = slotIdx =>
            {
                _onSwapSlotChosen = null;
                _onSwapAborted    = null;
                CloseSwapLayer();
                onReplace?.Invoke(slotIdx);
            };
            _onSwapAborted = () =>
            {
                _onSwapSlotChosen = null;
                _onSwapAborted    = null;
                CloseSwapLayer();
                onAbort?.Invoke();
            };

            // A full-screen dimmed layer above the reward and shop scenes (UI-STYLE-007/009): it is modal, so nothing
            // behind it (Reroll, Take nothing, Leave, other offers) can be clicked while the bag is being sorted.
            Transform parent = _pathPanel != null ? _pathPanel.transform.parent : null;
            if (parent == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null) parent = canvas.rootCanvas.transform;
            }
            if (parent == null) return;

            if (_bagSwapPanel != null) Object.Destroy(_bagSwapPanel);
            _bagSwapPanel = new GameObject("BagSwapPanel", typeof(RectTransform), typeof(Image));
            _bagSwapPanel.transform.SetParent(parent, false);
            _bagSwapPanel.transform.SetAsLastSibling();
            var layerRt = _bagSwapPanel.GetComponent<RectTransform>();
            layerRt.anchorMin = Vector2.zero;
            layerRt.anchorMax = Vector2.one;
            layerRt.offsetMin = layerRt.offsetMax = Vector2.zero;
            // Nearly opaque: at 0.82 the reward's own buttons showed through behind Swap and Keep Bag (0.4.3 shots).
            _bagSwapPanel.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.02f, 0.95f);

            BuildBagSwapScene(_bagSwapPanel.transform, newItem);   // [REQ: UI-STYLE-044] RewardViewController.BagSwap.cs
            _bagSwapPanel.SetActive(true);
            ActivePanel = _bagSwapPanel;
            FadeInPanel(_bagSwapPanel);
        }

        /// <summary>True while the bag-full swap layer is open; the scenes behind it ignore keys then.</summary>
        public bool IsSwapOpen => _bagSwapPanel != null && _bagSwapPanel.activeSelf;

        private void CloseSwapLayer()
        {
            ActivePanel = _panelBeforeSwap != null && _panelBeforeSwap.activeSelf ? _panelBeforeSwap : null;
            _panelBeforeSwap = null;
            InRunUiFactory.HidePanel(_bagSwapPanel);
        }

        /// <summary>
        /// Closes every scene this view owns (reward, relic shrine, bag swap) without granting anything; used when the
        /// run ends while one is open (defeat, abandon, victory).
        /// </summary>
        public void CloseScenes()
        {
            _onSwapSlotChosen = null;
            _onSwapAborted = null;
            _panelBeforeSwap = null;
            if (_bagSwapPanel != null) DestroyNow(_bagSwapPanel);
            _bagSwapPanel = null;
            _awaitingReward = false;
            _rewardSlots?.Clear();
            CloseRewardScene();
            CloseRelicShrine();
            CloseRestScene();
            ActivePanel = null;
        }

        private GameObject _restPanel;

        /// <summary>Removes the rest scene (a run that ends while it is open, or a test).</summary>
        public void CloseRestScene()
        {
            if (_restPanel == null) return;
            if (ActivePanel == _restPanel) ActivePanel = null;
            DestroyNow(_restPanel);
            _restPanel = null;
        }

        // The rest scene is in RewardViewController.Rest.cs (UI-STYLE-013).

        private IEnumerator FocusNextFrame(GameObject panel)
        {
            yield return null;
            InRunUiFactory.SelectFirstInteractable(panel);
        }

        /// <summary>
        /// Adds <paramref name="chosen"/> to the run (the very instance that was offered, GAP-021) and records its
        /// discovery. Used by the relic node panel and the boss offering table.
        /// </summary>
        public void AcceptRelic(RelicInstance chosen)
        {
            var run = _map?.Run;
            if (run == null || chosen == null) return;
            run.AcceptRelicChoice(chosen);
            new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordRelicDiscovery(chosen.Id);
            OnStatusChanged?.Invoke(F("InRun.Relic.Status.Accepted", RelicService.GetRelicName(chosen.Id)));
        }

        public void LeaveRelics() => OnStatusChanged?.Invoke(T("InRun.Relic.Status.Left"));

        // ────────────────────── Shared helpers ──────────────────────

        private void FadeInPanel(GameObject panel)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            if (!isActiveAndEnabled) { cg.alpha = 1f; return; }   // no coroutine on an inactive view
            cg.alpha = 0f;
            StartCoroutine(AnimationHelper.FadeIn(cg, AnimationHelper.MenuPanelDuration));
        }

        // ────────────────────── Event handling ──────────────────────

        public void HandleEvent()
        {
            var evt = _map.OpenEventNode();
            if (evt == null || evt.Options.Count == 0)
            {
                OnStatusChanged?.Invoke(T("InRun.Event.Status.Nothing"));
                return;
            }
            _map.ChooseEventOption(0);
            OnStatusChanged?.Invoke(F("InRun.Event.Status.Chose", evt.Title, evt.Options[0].Label));
        }

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);

        private static string RarityLabel(ItemRarity rarity) =>
            LocalizationService.T($"Item.Rarity.{rarity}", rarity.ToString());
    }
}
