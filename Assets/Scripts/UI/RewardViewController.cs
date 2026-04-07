using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

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
                _rewardSummary.text = $"Puzzle complete! Gold +{gold}\nChoose a reward ({slots.Count} slots).";

            RebuildRewardButtons();
            if (_rewardPanel != null) _rewardPanel.SetActive(true);
        }

        private void BuildRewardPanel()
        {
            if (_rewardPanel != null || _pathPanel == null) return;
            _rewardPanel = InRunUiFactory.CreateOverlayPanel(_pathPanel.transform, "RewardPanel", "Reward");
            _rewardSummary = _rewardPanel.transform.Find("Summary")?.GetComponent<Text>();
            _rewardPanel.SetActive(false);
        }

        private void RebuildRewardButtons()
        {
            if (_rewardPanel == null) return;
            InRunUiFactory.ClearNamedChildren(_rewardPanel.transform, "Slot_");

            var cols = Mathf.Clamp(_rewardSlots.Count, 1, 3);
            for (var i = 0; i < _rewardSlots.Count; i++)
            {
                var item = _rewardSlots[i];
                var col = i % cols;
                var row = i / cols;
                var xMin = 0.08f + col * 0.29f;
                var yMax = 0.50f - row * 0.18f;

                var itemLabel = item != null
                    ? $"{ItemService.GetItemName(item.Type)}\n{item.Rarity}\n{ItemService.GetItemDescription(item.Type, item.Rarity)}"
                    : "Nothing";
                var btn = InRunUiFactory.CreatePanelButton(_rewardPanel.transform, $"Slot_{i}",
                    new Vector2(xMin, yMax - 0.14f), new Vector2(xMin + 0.26f, yMax),
                    itemLabel);
                if (item != null)
                    InRunUiFactory.SetButtonIcon(btn, ItemService.GetIconName(item.Type));
                var idx = i;
                btn.onClick.AddListener(() => ClaimReward(idx));
            }

            // Skip button
            var skip = InRunUiFactory.CreatePanelButton(_rewardPanel.transform, "Slot_skip",
                new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.18f), "Skip");
            skip.onClick.AddListener(() =>
            {
                _awaitingReward = false;
                InRunUiFactory.HidePanel(_rewardPanel);
                OnPostRewardReady?.Invoke();
            });
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
                            new ProfileService(new SaveFileService()).RecordItemDiscovery(claimed.Type);
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
                new ProfileService(new SaveFileService()).RecordItemDiscovery(claimed.Type);
            }

            _awaitingReward = false;
            _rewardSlots.Clear();
            InRunUiFactory.HidePanel(_rewardPanel);
            OnPostRewardReady?.Invoke();
        }

        public void RerollRewardSlots(List<ItemInstance> newSlots)
        {
            if (newSlots == null || !_awaitingReward) return;
            _rewardSlots.Clear();
            _rewardSlots.AddRange(newSlots);
            RebuildRewardButtons();
        }

        // ────────────────────── Bag-full swap panel ──────────────────────

        public void ShowBagSwapPanel(ItemInstance newItem, Action<int> onReplace, Action onAbort)
        {
            _pendingSwapItem = newItem;
            _onSwapSlotChosen = onReplace;
            _onSwapAborted = onAbort;

            var parent = _pathPanel?.transform ?? Object.FindFirstObjectByType<Canvas>()?.transform;
            if (parent == null) return;

            if (_bagSwapPanel != null) Object.Destroy(_bagSwapPanel);
            _bagSwapPanel = new GameObject("BagSwapPanel", typeof(RectTransform), typeof(Image));
            _bagSwapPanel.transform.SetParent(parent, false);
            var rt = _bagSwapPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.75f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _bagSwapPanel.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.14f, 0.97f);

            var outline = _bagSwapPanel.AddComponent<Outline>();
            outline.effectColor = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.6f);
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
                    InRunUiFactory.SetButtonIcon(slotBtn, ItemService.GetIconName(item.Type));
                var capturedIdx = i;
                slotBtn.onClick.AddListener(() => _onSwapSlotChosen?.Invoke(capturedIdx));
            }

            var abortBtn = InRunUiFactory.CreatePanelButton(_bagSwapPanel.transform, "SwapAbort",
                new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.14f), "Keep Bag (Abort)");
            abortBtn.onClick.AddListener(() => _onSwapAborted?.Invoke());

            _bagSwapPanel.SetActive(true);
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
            bg.color = new Color(0.08f, 0.12f, 0.10f, 0.95f);

            InRunUiFactory.CreateText(panel.transform, "Title", "Rest \u2014 Choose one:", 16, TextAnchor.UpperCenter,
                new Color(0.98f, 0.83f, 0.26f, 1f)).rectTransform.SetRect(0.05f, 0.75f, 0.95f, 0.95f);

            var healAmt = run.GetRestHealAmount();
            var btnHeal = InRunUiFactory.CreatePanelButton(panel.transform, "BtnHeal",
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.70f),
                $"Heal +{healAmt} HP");
            btnHeal.onClick.AddListener(() =>
            {
                run.AcceptRestHeal();
                OnStatusChanged?.Invoke($"Rested. +{healAmt} HP");
                Object.Destroy(panel);
            });

            var btnPencil = InRunUiFactory.CreatePanelButton(panel.transform, "BtnPencil",
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.48f),
                "+4 Pencil Marks");
            btnPencil.onClick.AddListener(() =>
            {
                run.AcceptRestPencilBoost();
                OnStatusChanged?.Invoke("Rested. +4 Pencil");
                Object.Destroy(panel);
            });

            var btnReroll = InRunUiFactory.CreatePanelButton(panel.transform, "BtnReroll",
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.26f),
                "+1 Shop Reroll Token");
            btnReroll.onClick.AddListener(() =>
            {
                run.AcceptRestRerollShop();
                OnStatusChanged?.Invoke("Rested. +1 Reroll");
                Object.Destroy(panel);
            });
        }

        // ────────────────────── Relic choice panel ──────────────────────

        public void ShowRelicChoicePanel(List<RelicInstance> choices)
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
            panel.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.10f, 0.95f);

            InRunUiFactory.CreateText(panel.transform, "Title", "Relic Node \u2014 Choose one:", 16, TextAnchor.UpperCenter,
                new Color(0.98f, 0.83f, 0.26f, 1f)).rectTransform.SetRect(0.05f, 0.88f, 0.95f, 0.98f);

            var colW = 1f / choices.Count;
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
                InRunUiFactory.SetButtonIcon(btn, RelicService.GetIconName(relic.Id));
                var idx = i;
                btn.onClick.AddListener(() =>
                {
                    run.AcceptRelicChoice(idx);
                    new ProfileService(new SaveFileService()).RecordRelicDiscovery(relic.Id);
                    OnStatusChanged?.Invoke($"Relic: {RelicService.GetRelicName(relic.Id)}");
                    Object.Destroy(panel);
                });
            }

            // Skip / Leave button
            var skip = InRunUiFactory.CreatePanelButton(panel.transform, "BtnSkip",
                new Vector2(0.35f, 0.01f), new Vector2(0.65f, 0.08f), "Leave");
            skip.onClick.AddListener(() =>
            {
                OnStatusChanged?.Invoke("Left the relic.");
                Object.Destroy(panel);
            });
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
