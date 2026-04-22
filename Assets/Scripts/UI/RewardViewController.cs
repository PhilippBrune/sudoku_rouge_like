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
    /// Owns reward screen, bag-swap panel, rest choice panel, relic choice panel, and event handling.
    /// </summary>
    public sealed class RewardViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _pathPanel;

        private GameObject _rewardPanel;
        private Text _rewardSummary;
        private bool _awaitingReward;
        private List<ItemInstance> _rewardSlots;

        // Bag-swap
        private GameObject _bagSwapPanel;
        private ItemInstance _pendingSwapItem;
        private Action<int> _onSwapSlotChosen;
        private Action _onSwapAborted;

        public bool IsAwaitingReward => _awaitingReward;

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

        // ────────────────────── Reward screen ──────────────────────

        public void ShowRewardScreen()
        {
            BuildRewardPanel();

            if (!_map.TryClaimCurrentPuzzleRewards(out var gold, out var slots))
            {
                _awaitingReward = false;
                OnStatusChanged?.Invoke("No rewards.");
                InRunUiFactory.HidePanel(_rewardPanel);
                OnPostRewardReady?.Invoke();
                return;
            }

            _awaitingReward = true;
            _rewardSlots.Clear();
            _rewardSlots.AddRange(slots);

            if (_rewardSummary != null)
            {
                var run   = _map?.Run;
                var s     = run?.State;
                var level = run?.CurrentLevelState;

                var hp     = s != null ? $"{s.CurrentHP}/{s.MaxHP}" : "—";
                var pencil = s != null ? $"{s.CurrentPencil}/{s.MaxPencil}" : "—";

                var mistakes = level?.Mistakes ?? 0;
                var perfect  = level?.PerfectSoFar ?? false;
                var noPencil = level?.NoPencilUsed ?? false;

                var xp = 0;
                if (run?.TileXpLog != null && run.TileXpLog.Count > 0)
                    xp = run.TileXpLog[run.TileXpLog.Count - 1].TotalXp;

                _rewardSummary.text =
                    $"Rewards:  +{gold}g  ·  HP {hp}  ·  Pencil {pencil}\n" +
                    $"Unlocks:  +{xp} XP\n" +
                    $"Stats:    {mistakes} mistake{(mistakes == 1 ? "" : "s")}  ·  {(perfect ? "Perfect" : "Not perfect")}  ·  {(noPencil ? "No pencil" : "Pencil used")}\n" +
                    $"Choose a reward  ({slots.Count} item{(slots.Count == 1 ? "" : "s")}):";
            }

            if (_rewardPanel != null)
            {
                _rewardPanel.SetActive(true);
                ActivePanel = _rewardPanel;
                FadeInPanel(_rewardPanel);
            }
            StartCoroutine(RebuildRewardButtonsStaggered());
        }

        private void BuildRewardPanel()
        {
            if (_rewardPanel != null || _pathPanel == null) return;
            _rewardPanel = InRunUiFactory.CreateOverlayPanel(_pathPanel.transform, "RewardPanel", "Rewards");
            var rewardBase = InRunUiFactory.PanelBg;
            _rewardPanel.GetComponent<Image>().color = new Color(rewardBase.r, rewardBase.g, rewardBase.b, 0.72f);
            InRunUiFactory.AddPanelBackground(_rewardPanel.transform, "bg_reward");
            _rewardSummary = _rewardPanel.transform.Find("Summary")?.GetComponent<Text>();
            if (_rewardSummary != null)
            {
                _rewardSummary.fontSize = 12;
                _rewardSummary.alignment = TextAnchor.UpperLeft;
                _rewardSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
                _rewardSummary.verticalOverflow = VerticalWrapMode.Overflow;

                _rewardSummary.rectTransform.anchorMin = new Vector2(0.08f, 0.62f);
                _rewardSummary.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
                _rewardSummary.rectTransform.offsetMin = Vector2.zero;
                _rewardSummary.rectTransform.offsetMax = Vector2.zero;
            }
            _rewardPanel.SetActive(false);
        }

        private IEnumerator RebuildRewardButtonsStaggered()
        {
            if (_rewardPanel == null) yield break;
            InRunUiFactory.ClearNamedChildren(_rewardPanel.transform, "Slot_");

            var cols = Mathf.Clamp(_rewardSlots.Count, 1, 3);
            var totalRows = Mathf.CeilToInt(_rewardSlots.Count / (float)cols);

            const float left = 0.06f;
            const float right = 0.94f;
            const float colGap = 0.03f;
            var btnW = (right - left - colGap * (cols - 1)) / cols;

            const float top = 0.60f;
            const float btnH = 0.18f;
            const float rowGap = 0.04f;

            var slotBtns = new Button[_rewardSlots.Count];
            for (var i = 0; i < _rewardSlots.Count; i++)
            {
                var item = _rewardSlots[i];
                var col = i % cols;
                var row = i / cols;

                var itemsInRow = row == totalRows - 1 ? _rewardSlots.Count - row * cols : cols;
                var rowOffset = (cols - itemsInRow) * 0.5f * (btnW + colGap);

                var xMin = left + rowOffset + col * (btnW + colGap);
                var xMax = xMin + btnW;
                var yMax = top - row * (btnH + rowGap);
                var yMin = yMax - btnH;

                var itemLabel = item != null
                    ? $"{ItemService.GetItemName(item.Type)} ({item.Rarity})\n{ItemService.GetItemDescription(item.Type, item.Rarity)}"
                    : "Nothing";

                var btn = InRunUiFactory.CreatePanelButton(_rewardPanel.transform, $"Slot_{i}",
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax), itemLabel);
                // Tint button by rarity so items are immediately distinguishable at a glance
                if (item != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null)
                        img.color = item.Rarity switch
                        {
                            ItemRarity.Epic => new Color(0.80f, 0.58f, 0.08f, 0.92f),  // gold
                            ItemRarity.Rare => new Color(0.22f, 0.38f, 0.78f, 0.92f),  // blue
                            _              => img.color                                 // Normal: keep default
                        };
                }
                if (item != null)
                    InRunUiFactory.SetButtonIcon(btn, ItemService.GetIconName(item.Type), false, "items");
                var idx = i;
                btn.onClick.AddListener(() => ClaimReward(idx));
                slotBtns[i] = btn;

                // Staggered reveal: snap to full size then punch-scale for feedback
                btn.transform.localScale = Vector3.one;
                StartCoroutine(AnimationHelper.PulseScale(
                    btn.transform, 1f, 1.15f, AnimationHelper.RewardSlotDuration));
                yield return new WaitForSecondsRealtime(AnimationHelper.RewardSlotDuration * 0.6f);
            }

            // Skip button
            var skip = InRunUiFactory.CreatePanelButton(_rewardPanel.transform, "Slot_skip",
                new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.16f), "Skip");
            skip.onClick.AddListener(() =>
            {
                _awaitingReward = false;
                ActivePanel = null;
                InRunUiFactory.HidePanel(_rewardPanel);
                OnPostRewardReady?.Invoke();
            });

            // Explicit D-pad navigation: grid left/right, row up/down, last row → skip
            for (var i = 0; i < slotBtns.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                var col = i % cols;
                var row = i / cols;
                if (col > 0)            nav.selectOnLeft  = slotBtns[i - 1];
                if (col < cols - 1 && i + 1 < slotBtns.Length) nav.selectOnRight = slotBtns[i + 1];
                if (i + cols < slotBtns.Length) nav.selectOnDown = slotBtns[i + cols];
                else                            nav.selectOnDown = skip;
                if (i - cols >= 0)      nav.selectOnUp    = slotBtns[i - cols];
                slotBtns[i].navigation = nav;
            }
            // Skip: up goes to first slot in the last row
            var lastRowFirst = ((slotBtns.Length - 1) / cols) * cols;
            var skipNav = skip.navigation;
            skipNav.mode = Navigation.Mode.Explicit;
            skipNav.selectOnUp = slotBtns.Length > 0 ? slotBtns[lastRowFirst] : null;
            skip.navigation = skipNav;

            // All buttons now exist — set controller focus to first reward button
            InRunUiFactory.SelectFirstInteractable(_rewardPanel);
        }

        private void ClaimReward(int index)
        {
            var run = _map?.Run;
            if (run == null) return;

            if (index >= 0 && index < _rewardSlots.Count && _rewardSlots[index] != null)
            {
                var claimed = _rewardSlots[index];

                if (run.IsBagFull())
                {
                    ShowBagSwapPanel(
                        claimed,
                        slotToReplace =>
                        {
                            run.ReplaceItemInInventory(slotToReplace, claimed);
                            new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordItemDiscovery(claimed.Type);
                            _awaitingReward = false;
                            _rewardSlots.Clear();
                            InRunUiFactory.HidePanel(_rewardPanel);
                            InRunUiFactory.HidePanel(_bagSwapPanel);
                            OnPostRewardReady?.Invoke();
                        },
                        () =>
                        {
                            _awaitingReward = false;
                            _rewardSlots.Clear();
                            InRunUiFactory.HidePanel(_rewardPanel);
                            InRunUiFactory.HidePanel(_bagSwapPanel);
                            OnPostRewardReady?.Invoke();
                        });
                    return;
                }

                run.PickRewardItem(index);
                new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordItemDiscovery(claimed.Type);
            }

            _awaitingReward = false;
            _rewardSlots.Clear();
            ActivePanel = null;
            InRunUiFactory.HidePanel(_rewardPanel);
            OnPostRewardReady?.Invoke();
        }

        public void RerollRewardSlots(List<ItemInstance> newSlots)
        {
            if (newSlots == null || !_awaitingReward) return;
            _rewardSlots.Clear();
            _rewardSlots.AddRange(newSlots);
            StartCoroutine(RebuildRewardButtonsStaggered());
        }

        // ────────────────────── Bag-full swap panel ──────────────────────

        public void ShowBagSwapPanel(ItemInstance newItem, Action<int> onReplace, Action onAbort)
        {
            _pendingSwapItem = newItem;

            // Wrap callbacks so the swap panel is always dismissed before the
            // caller's logic runs, and neither callback can fire twice even on
            // rapid double-taps.
            _onSwapSlotChosen = slotIdx =>
            {
                _onSwapSlotChosen = null;
                _onSwapAborted    = null;
                ActivePanel = null;
                InRunUiFactory.HidePanel(_bagSwapPanel);
                onReplace?.Invoke(slotIdx);
            };
            _onSwapAborted = () =>
            {
                _onSwapSlotChosen = null;
                _onSwapAborted    = null;
                ActivePanel = null;
                InRunUiFactory.HidePanel(_bagSwapPanel);
                onAbort?.Invoke();
            };

            var parent = _pathPanel?.transform ?? Object.FindFirstObjectByType<Canvas>()?.transform;
            if (parent == null) return;

            if (_bagSwapPanel != null) Object.Destroy(_bagSwapPanel);
            _bagSwapPanel = new GameObject("BagSwapPanel", typeof(RectTransform), typeof(Image));
            _bagSwapPanel.transform.SetParent(parent, false);
            var rt = _bagSwapPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.75f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _bagSwapPanel.GetComponent<Image>().color = InRunUiFactory.SwapPanelBg;
            InRunUiFactory.AddPanelBackground(_bagSwapPanel.transform, "bg_swap");

            var outline = _bagSwapPanel.AddComponent<Outline>();
            outline.effectColor = GamePalette.AccentGoldMid;
            outline.effectDistance = new Vector2(2f, -2f);

            var title = InRunUiFactory.CreateText(_bagSwapPanel.transform, "SwapTitle",
                $"Bag Full \u2014 Choose a slot to replace with:\n{ItemService.GetItemName(newItem.Type)} ({newItem.Rarity})\n{ItemService.GetItemDescription(newItem.Type, newItem.Rarity)}",
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
                    ? $"{ItemService.GetItemName(item.Type)}\n{item.Rarity}"
                    : "Empty";
                var slotBtn = InRunUiFactory.CreatePanelButton(_bagSwapPanel.transform, $"SwapSlot_{i}",
                    new Vector2(xMin, yMax - 0.20f), new Vector2(xMin + 0.29f, yMax), label);
                if (item != null)
                    InRunUiFactory.SetButtonIcon(slotBtn, ItemService.GetIconName(item.Type), false, "items");
                var capturedIdx = i;
                slotBtn.onClick.AddListener(() => _onSwapSlotChosen?.Invoke(capturedIdx));
                swapBtns[i] = slotBtn;
            }

            var abortBtn = InRunUiFactory.CreatePanelButton(_bagSwapPanel.transform, "SwapAbort",
                new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.14f), "Keep Bag (Abort)");
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

        // ────────────────────── Rest choice panel ──────────────────────

        public void ShowRestChoicePanel()
        {
            var run = _map?.Run;
            if (run?.State == null || _pathPanel == null) return;

            var panel = new GameObject("RestChoicePanel", typeof(RectTransform));
            panel.transform.SetParent(_pathPanel.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.20f, 0.25f);
            rt.anchorMax = new Vector2(0.80f, 0.75f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = InRunUiFactory.RestRelicPanelBg;
            InRunUiFactory.AddPanelBackground(panel.transform, "bg_rest");

            var restTitle = InRunUiFactory.CreateText(panel.transform, "Title", "Rest \u2014 Choose one:", 16, TextAnchor.UpperCenter,
                GamePalette.AccentGold);
            restTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.75f);
            restTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            restTitle.rectTransform.offsetMin = Vector2.zero;
            restTitle.rectTransform.offsetMax = Vector2.zero;

            var healAmt = run.GetRestHealAmount();
            var btnHeal = InRunUiFactory.CreatePanelButton(panel.transform, "BtnHeal",
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.70f),
                $"Heal +{healAmt} HP");
            btnHeal.onClick.AddListener(() =>
            {
                run.AcceptRestHeal();
                OnStatusChanged?.Invoke($"Rested. +{healAmt} HP");
                ActivePanel = null;
                Object.Destroy(panel);
            });

            var btnPencil = InRunUiFactory.CreatePanelButton(panel.transform, "BtnPencil",
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.48f),
                "+4 Pencil Marks");
            btnPencil.onClick.AddListener(() =>
            {
                run.AcceptRestPencilBoost();
                OnStatusChanged?.Invoke("Rested. +4 Pencil");
                ActivePanel = null;
                Object.Destroy(panel);
            });

            var btnReroll = InRunUiFactory.CreatePanelButton(panel.transform, "BtnReroll",
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.26f),
                "+1 Shop Reroll Token");
            btnReroll.onClick.AddListener(() =>
            {
                run.AcceptRestRerollShop();
                OnStatusChanged?.Invoke("Rested. +1 Reroll");
                ActivePanel = null;
                Object.Destroy(panel);
            });

            // 4th option: cleanse oldest curse — only shown when at least one curse is active
            if (run.HasAnyCurse())
            {
                // Shift existing buttons up to make room
                var shift = 0.18f;
                btnHeal.GetComponent<RectTransform>().anchorMin    += new Vector2(0, shift);
                btnHeal.GetComponent<RectTransform>().anchorMax    += new Vector2(0, shift);
                btnPencil.GetComponent<RectTransform>().anchorMin  += new Vector2(0, shift);
                btnPencil.GetComponent<RectTransform>().anchorMax  += new Vector2(0, shift);
                btnReroll.GetComponent<RectTransform>().anchorMin  += new Vector2(0, shift);
                btnReroll.GetComponent<RectTransform>().anchorMax  += new Vector2(0, shift);

                var activeCurses = run.GetActiveCurses();
                var oldestName = activeCurses.Count > 0 ? activeCurses[0].Name : "curse";
                var btnCleanse = InRunUiFactory.CreatePanelButton(panel.transform, "BtnCleanse",
                    new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.26f),
                    $"Cleanse \"{oldestName}\"");
                btnCleanse.onClick.AddListener(() =>
                {
                    run.AcceptRestCurseRemoval();
                    OnStatusChanged?.Invoke($"Cleansed: {oldestName}");
                    ActivePanel = null;
                    Object.Destroy(panel);
                });
            }

            ActivePanel = panel;
            FadeInPanel(panel);
            InRunUiFactory.SelectFirstInteractable(panel);
        }

        // ────────────────────── Relic choice panel ──────────────────────

        public void ShowRelicChoicePanel(List<RelicInstance> choices, System.Action onDismissed = null)
        {
            var run = _map?.Run;
            if (run == null || _pathPanel == null) return;

            var panel = new GameObject("RelicChoicePanel", typeof(RectTransform));
            panel.transform.SetParent(_pathPanel.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.15f);
            rt.anchorMax = new Vector2(0.95f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = InRunUiFactory.RestRelicPanelBg;
            InRunUiFactory.AddPanelBackground(panel.transform, "bg_relic");

            var relicTitle = InRunUiFactory.CreateText(panel.transform, "Title", "Relic Node \u2014 Choose one:", 16, TextAnchor.UpperCenter,
                GamePalette.AccentGold);
            relicTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.88f);
            relicTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            relicTitle.rectTransform.offsetMin = Vector2.zero;
            relicTitle.rectTransform.offsetMax = Vector2.zero;

            // Tracks which relic index is currently staged for confirmation (-1 = none)
            var stagedIndex = -1;
            Button acceptBtn = null;

            var colW = 1f / choices.Count;
            var relicBtns = new Button[choices.Count];

            for (var i = 0; i < choices.Count; i++)
            {
                var relic = choices[i];
                var xMin = 0.04f + i * colW;
                var xMax = xMin + colW - 0.04f;

                var relicDesc = $"{RelicService.GetRelicName(relic.Id)}\n" +
                                $"[{relic.Tier}]\n\n" +
                                RelicService.GetRelicDescription(relic.Id);

                var btn = InRunUiFactory.CreatePanelButton(panel.transform, $"RelicBtn_{i}",
                    new Vector2(xMin, 0.10f), new Vector2(xMax, 0.82f), relicDesc);
                InRunUiFactory.SetButtonIcon(btn, RelicService.GetIconName(relic.Id), false, RelicService.GetIconFolder(relic.Id));
                relicBtns[i] = btn;
                var idx = i;
                btn.onClick.AddListener(() =>
                {
                    // First click: stage the relic and show the Accept button
                    stagedIndex = idx;
                    // Tint all buttons: highlight selected, dim others
                    for (var b = 0; b < relicBtns.Length; b++)
                    {
                        var img = relicBtns[b].GetComponent<Image>();
                        if (img != null)
                            img.color = b == idx
                                ? new Color(0.95f, 0.80f, 0.25f, 0.95f)   // gold highlight
                                : new Color(0.25f, 0.25f, 0.22f, 0.70f);  // dimmed
                    }
                    if (acceptBtn != null) acceptBtn.gameObject.SetActive(true);
                });
            }

            // Accept button — hidden until a relic is staged
            acceptBtn = InRunUiFactory.CreatePanelButton(panel.transform, "BtnAccept",
                new Vector2(0.25f, 0.01f), new Vector2(0.55f, 0.08f), "Accept");
            acceptBtn.gameObject.SetActive(false);
            acceptBtn.GetComponent<Image>().color = new Color(0.30f, 0.65f, 0.30f, 0.92f);
            acceptBtn.onClick.AddListener(() =>
            {
                if (stagedIndex < 0 || stagedIndex >= choices.Count) return;
                var chosen = choices[stagedIndex];
                run.AcceptRelicChoice(stagedIndex);
                new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordRelicDiscovery(chosen.Id);
                OnStatusChanged?.Invoke($"Relic: {RelicService.GetRelicName(chosen.Id)}");
                ActivePanel = null;
                Object.Destroy(panel);
                onDismissed?.Invoke();
            });

            // Skip / Leave button
            var skip = InRunUiFactory.CreatePanelButton(panel.transform, "BtnSkip",
                new Vector2(0.60f, 0.01f), new Vector2(0.96f, 0.08f), "Leave");
            skip.onClick.AddListener(() =>
            {
                OnStatusChanged?.Invoke("Left the relic.");
                ActivePanel = null;
                Object.Destroy(panel);
                onDismissed?.Invoke();
            });

            ActivePanel = panel;
            FadeInPanel(panel);
            InRunUiFactory.SelectFirstInteractable(panel);
        }

        // ────────────────────── Shared helpers ──────────────────────

        private void FadeInPanel(GameObject panel)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(AnimationHelper.FadeIn(cg, AnimationHelper.MenuPanelDuration));
        }

        // ────────────────────── Event handling ──────────────────────

        public void HandleEvent()
        {
            var evt = _map.OpenEventNode();
            if (evt == null || evt.Options.Count == 0)
            {
                OnStatusChanged?.Invoke("Event: nothing happened.");
                return;
            }
            _map.ChooseEventOption(0);
            OnStatusChanged?.Invoke($"Event: {evt.Title}\nChose: {evt.Options[0].Label}");
        }
    }
}
