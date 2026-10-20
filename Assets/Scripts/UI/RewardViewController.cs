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
            _bagSwapPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var card = new GameObject("SwapCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_bagSwapPanel.transform, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.75f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = InRunUiFactory.SwapPanelBg;
            InRunUiFactory.AddPanelBackground(card.transform, "bg_swap");

            var outline = card.AddComponent<Outline>();
            outline.effectColor = GamePalette.AccentGoldMid;
            outline.effectDistance = new Vector2(2f, -2f);

            var title = InRunUiFactory.CreateText(card.transform, "SwapTitle",
                F("InRun.BagSwap.Title", ItemService.GetItemName(newItem.Type), RarityLabel(newItem.Rarity), ItemService.GetItemDescription(newItem.Type, newItem.Rarity)),
                13, TextAnchor.UpperCenter, InRunUiFactory.AccentGold);
            title.rectTransform.anchorMin = new Vector2(0.04f, 0.72f);
            title.rectTransform.anchorMax = new Vector2(0.96f, 0.98f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var run = _map?.Run;
            var heldItems = run?.State?.HeldItems;
            var slotCount = run?.State?.ItemSlots ?? 0;

            var swapBtns = new Button[slotCount];
            for (var i = 0; i < slotCount; i++)
            {
                var item = (heldItems != null && i < heldItems.Count) ? heldItems[i] : null;
                var col = i % 3;
                var row = i / 3;
                var xMin = 0.05f + col * 0.32f;
                var yMax = 0.68f - row * 0.25f;
                var label = item != null
                    ? F("InRun.BagSwap.Slot", ItemService.GetItemName(item.Type), RarityLabel(item.Rarity))
                    : T("InRun.Common.Empty");
                var slotBtn = InRunUiFactory.CreatePanelButton(card.transform, $"SwapSlot_{i}",
                    new Vector2(xMin, yMax - 0.20f), new Vector2(xMin + 0.29f, yMax), label);
                if (item != null)
                    InRunUiFactory.SetButtonIcon(slotBtn, ItemService.GetIconName(item.Type), false, "items");
                var capturedIdx = i;
                slotBtn.onClick.AddListener(() => _onSwapSlotChosen?.Invoke(capturedIdx));
                swapBtns[i] = slotBtn;
            }

            var abortBtn = InRunUiFactory.CreatePanelButton(card.transform, "SwapAbort",
                new Vector2(0.20f, 0.02f), new Vector2(0.80f, 0.17f), T("InRun.BagSwap.KeepBag"));  // B1: taller for icon visibility
            InRunUiFactory.SetButtonIcon(abortBtn, "swap_arrows", false, "economy");
            abortBtn.onClick.AddListener(() => _onSwapAborted?.Invoke());

            // Explicit 3-column grid D-pad navigation for swap slots
            for (var i = 0; i < slotCount; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % 3 > 0)                      nav.selectOnLeft  = swapBtns[i - 1];
                if (i % 3 < 2 && i + 1 < slotCount) nav.selectOnRight = swapBtns[i + 1];
                nav.selectOnDown = i + 3 < slotCount ? swapBtns[i + 3] : abortBtn;
                if (i >= 3)                          nav.selectOnUp    = swapBtns[i - 3];
                swapBtns[i].navigation = nav;
            }
            var lastSwapRow = slotCount > 0 ? ((slotCount - 1) / 3) * 3 : 0;
            var abortNav = abortBtn.navigation;
            abortNav.mode = Navigation.Mode.Explicit;
            abortNav.selectOnUp = slotCount > 0 ? swapBtns[lastSwapRow] : null;
            abortBtn.navigation = abortNav;

            _bagSwapPanel.SetActive(true);
            ActivePanel = _bagSwapPanel;
            FadeInPanel(_bagSwapPanel);
            InRunUiFactory.SelectFirstInteractable(_bagSwapPanel);
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
