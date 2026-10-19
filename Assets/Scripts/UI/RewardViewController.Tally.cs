using System;
using System.Collections;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-007] The reward after a puzzle (layout-proposals.md, "Reward after a puzzle"): the puzzle's result
    // on a tally scroll on the left, the item choices as ema plaques hanging from a red cord over the reward painting.
    // Choosing a plaque lifts it and names it on the Take button; choosing it again (click, key or A) takes it.
    // Reroll and Take nothing stay as before. Object names (Slot_i, Slot_reroll, Slot_skip) are kept for wiring.
    public sealed partial class RewardViewController
    {
        private GameObject _rewardPanel;
        private bool _awaitingReward;
        private List<ItemInstance> _rewardSlots;
        private readonly List<Button> _rewardPlaques = new List<Button>();
        private int _selectedReward = -1;
        private Button _rewardTake;
        private Coroutine _plaqueReveal;

        /// <summary>The finished puzzle's time in seconds for the tally (InRunController reads the HUD timer).</summary>
        public Func<float> PuzzleSeconds;

        public bool IsAwaitingReward => _awaitingReward;

        private const float PlaqueTop = 0.7407f;      // apex of a plaque's gable
        private const float PlaqueBottom = 0.3333f;
        private const float PlaqueLift = 0.016f;      // how far the chosen plaque rises
        private const float CordY = 0.7796f;

        public void ShowRewardScreen()
        {
            if (_map == null || _pathPanel == null) return;
            // A second call while the plaques are up (the claim is already made) keeps the open screen working.
            if (_awaitingReward && _rewardPanel != null) return;
            if (!_map.TryClaimCurrentPuzzleRewards(out var gold, out var slots))
            {
                _awaitingReward = false;
                OnStatusChanged?.Invoke(T("InRun.Reward.NoRewards"));
                OnPostRewardReady?.Invoke();
                return;
            }

            _awaitingReward = true;
            _rewardSlots.Clear();
            _rewardSlots.AddRange(slots);
            BuildRewardScene(gold);
            ActivePanel = _rewardPanel;
            FadeInPanel(_rewardPanel);
            RevealPlaques();
        }

        /// <summary>RW-1: keys 1–3 choose a plaque; the same key again takes it.</summary>
        public void SelectRewardByIndex(int i)
        {
            if (_rewardPanel == null || !_rewardPanel.activeSelf || IsSwapOpen || i < 0 || i >= _rewardPlaques.Count) return;
            var plaque = _rewardPlaques[i];
            if (plaque == null || !plaque.IsInteractable()) return;
            EventSystem.current?.SetSelectedGameObject(plaque.gameObject);
            OnPlaqueClicked(i);
        }

        public void RerollRewardSlots(List<ItemInstance> newSlots)
        {
            if (newSlots == null || !_awaitingReward) return;
            _rewardSlots.Clear();
            _rewardSlots.AddRange(newSlots);
            RevealPlaques();
        }

        // ── scene ──

        private void BuildRewardScene(int gold)
        {
            CloseRewardScene();
            var run = _map.Run;
            var state = run.State;
            var screen = ScrollUiKit.Scene(_pathPanel, "RewardPanel", "bg_reward", new Color(0.78f, 0.78f, 0.74f, 1f));
            _rewardPanel = screen.gameObject;
            ScrollUiKit.Fill(screen.transform, "FloorShade", Vector2.zero, new Vector2(1f, 0.28f), new Color(0f, 0f, 0f, 0.35f));

            BuildTally(screen.transform, run, gold);

            var heading = ScrollUiKit.Label(screen.transform, "RewardHeading", T("InRun.Reward.Choose"), 40, TextAnchor.MiddleLeft,
                ScrollUiKit.PaperText, new Vector2(0.359f, 0.842f), new Vector2(0.95f, 0.895f), 24);
            AddShadow(heading);
            var bag = ScrollUiKit.Label(screen.transform, "RewardBagLine",
                F("InRun.Reward.BagSlots", state.HeldItems.Count, state.ItemSlots), 20, TextAnchor.MiddleLeft,
                new Color(0.90f, 0.86f, 0.76f, 1f), new Vector2(0.359f, 0.815f), new Vector2(0.95f, 0.843f), 13);
            AddShadow(bag);
            var cord = ScrollUiKit.Fill(screen.transform, "RewardCord", new Vector2(0.3333f, CordY), new Vector2(0.9583f, CordY),
                ScrollUiKit.Vermilion);
            cord.rectTransform.offsetMin = new Vector2(0f, -1.5f);
            cord.rectTransform.offsetMax = new Vector2(0f, 1.5f);

            _rewardTake = ScrollUiKit.PillButton(screen.transform, "Slot_take", new Vector2(0.3594f, 0.0898f),
                new Vector2(0.5063f, 0.1435f), T("InRun.Reward.PickOne"), 22, ScrollUiKit.Vermilion, Color.white, 24f);
            _rewardTake.interactable = false;
            _rewardTake.onClick.AddListener(() =>
            {
                if (_selectedReward >= 0) ClaimReward(_selectedReward);
            });

            var reroll = BuildRewardRerollButton(screen.transform);
            var nothingGold = state.IsSeasonalChallenge || state.DisableProgressionRewards ? 0 : run.GetNothingRewardGold();
            var nothing = ScrollUiKit.RimButton(screen.transform, "Slot_skip", new Vector2(0.638f, 0.0898f),
                new Vector2(0.7828f, 0.1435f),
                nothingGold > 0 ? F("InRun.Reward.TakeNothing", nothingGold) : T("InRun.Reward.TakeNothingPlain"), 20,
                ScrollUiKit.Gold, ScrollUiKit.OfferingFocus, ScrollUiKit.OfferingText);
            nothing.onClick.AddListener(() =>
            {
                GrantNothingReward();
                FinishReward();
            });

            var hint = ScrollUiKit.Label(screen.transform, "RewardKeysHint", F("InRun.Reward.KeysHint", Mathf.Min(3, _rewardSlots.Count)),
                18, TextAnchor.MiddleRight, new Color(0.86f, 0.82f, 0.70f, 1f), new Vector2(0.79f, 0.0898f),
                new Vector2(0.958f, 0.1435f), 12);
            AddShadow(hint);

            _rewardTake.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = reroll };
            reroll.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _rewardTake, selectOnRight = nothing };
            nothing.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = reroll };
        }

        // The puzzle's result on a hanging scroll: floor and depth, "Puzzle solved", what was gained and the puzzle's
        // numbers, and a vermilion "Perfect" stamp for a puzzle without mistakes.
        private void BuildTally(Transform screen, RunDirector run, int gold)
        {
            var state = run.State;
            var level = run.CurrentLevelState;
            var tally = ScrollUiKit.HangingScroll(screen, "RewardTally", new Vector2(0.0656f, 0.1528f), new Vector2(0.2938f, 0.912f));

            ScrollUiKit.Label(tally, "Eyebrow",
                F("InRun.Reward.Eyebrow", FloorThemeData.GetFloorName(state.CurrentFloor), state.Depth).ToUpperInvariant(), 16,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.105f, 0.900f), new Vector2(0.95f, 0.935f), 11);
            ScrollUiKit.Label(tally, "Title", T("InRun.Reward.Solved"), 50, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.10f, 0.815f), new Vector2(0.95f, 0.905f), 28);

            var xp = run.TileXpLog != null && run.TileXpLog.Count > 0 ? run.TileXpLog[run.TileXpLog.Count - 1].TotalXp : 0;
            var gains = new (string key, string value, Color color, string icon)[]
            {
                ("InRun.Reward.Row.Gold", "+" + gold, ScrollUiKit.Vermilion, "economy/icon_sakura_coin"),
                ("InRun.Reward.Row.Xp", "+" + xp, ScrollUiKit.Vermilion, "ui/icon_golden_bloom"),
                ("InRun.Reward.Row.Hp", $"{state.CurrentHP} / {state.MaxHP}", ScrollUiKit.Ink, "ui/icon_petal_orb"),
                ("InRun.Reward.Row.Pencil", $"{state.CurrentPencil} / {state.MaxPencil}", ScrollUiKit.Ink, "ui/icon_ink_save")
            };
            for (var k = 0; k < gains.Length; k++)
            {
                var centre = 0.749f - k * 0.0757f;
                ScrollUiKit.ValueRow(tally, $"Gain_{k}", new Vector2(0.105f, centre - 0.029f), new Vector2(0.895f, centre + 0.029f),
                    T(gains[k].key), gains[k].value, 26, gains[k].color, gains[k].icon);
            }

            var rule = ScrollUiKit.Fill(tally, "Rule", new Vector2(0.105f, 0.457f), new Vector2(0.895f, 0.457f),
                new Color(ScrollUiKit.Vermilion.r, ScrollUiKit.Vermilion.g, ScrollUiKit.Vermilion.b, 0.55f));
            rule.rectTransform.offsetMin = new Vector2(0f, -1f);
            rule.rectTransform.offsetMax = new Vector2(0f, 1f);

            var seconds = Mathf.Max(0, Mathf.RoundToInt(PuzzleSeconds?.Invoke() ?? 0f));
            var stats = new (string key, string value)[]
            {
                ("InRun.Reward.Row.Mistakes", (level?.Mistakes ?? 0).ToString()),
                ("InRun.Reward.Row.PencilMarks", (level?.PencilMarksUsed ?? 0).ToString()),
                ("InRun.Reward.Row.Time", $"{seconds / 60:00}:{seconds % 60:00}"),
                ("InRun.Reward.Row.Combo", "×" + state.ComboStreak)
            };
            for (var k = 0; k < stats.Length; k++)
            {
                var centre = 0.406f - k * 0.0463f;
                ScrollUiKit.ValueRow(tally, $"Stat_{k}", new Vector2(0.105f, centre - 0.02f), new Vector2(0.895f, centre + 0.02f),
                    T(stats[k].key), stats[k].value, 20, ScrollUiKit.Ink);
            }

            if (level != null && level.PerfectSoFar && level.Mistakes == 0)
            {
                var stamp = ScrollUiKit.Spot(tally, "PerfectStamp", new Vector2(0.77f, 0.159f), new Vector2(124f, 124f),
                    ScrollUiKit.Vermilion, ScrollUiKit.Enso());
                stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                var word = ScrollUiKit.Label(tally, "PerfectWord", T("InRun.Reward.Perfect"), 26, TextAnchor.MiddleCenter,
                    ScrollUiKit.Vermilion, new Vector2(0.57f, 0.125f), new Vector2(0.97f, 0.193f), 16);
                word.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            }
        }

        // ── plaques ──

        private void RevealPlaques()
        {
            if (_plaqueReveal != null) StopCoroutine(_plaqueReveal);
            _plaqueReveal = isActiveAndEnabled ? StartCoroutine(BuildPlaques()) : null;
            if (_plaqueReveal == null) BuildPlaquesNow();
        }

        private IEnumerator BuildPlaques()
        {
            BuildPlaquesNow();
            for (var i = 0; i < _rewardPlaques.Count; i++)
            {
                if (_rewardPlaques[i] == null) continue;
                StartCoroutine(AnimationHelper.PulseScale(_rewardPlaques[i].transform, 1f, 1.06f, AnimationHelper.RewardSlotDuration));
                yield return new WaitForSecondsRealtime(AnimationHelper.RewardSlotDuration * 0.5f);
            }
            _plaqueReveal = null;
        }

        private void BuildPlaquesNow()
        {
            if (_rewardPanel == null) return;
            foreach (var old in _rewardPlaques)
                if (old != null) DestroyNow(old.transform.parent.gameObject);
            _rewardPlaques.Clear();
            _selectedReward = -1;
            UpdateTakeButton();

            var count = _rewardSlots.Count;
            var width = count <= 3 ? 0.1719f : 0.150f;
            var step = count <= 3 ? 0.1875f : 0.160f;
            const float middle = 0.638f;
            for (var i = 0; i < count; i++)
            {
                var centre = middle + (i - (count - 1) * 0.5f) * step;
                _rewardPlaques.Add(BuildPlaque(i, _rewardSlots[i], centre, width));
            }

            for (var i = 0; i < _rewardPlaques.Count; i++)
                _rewardPlaques[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _rewardPlaques[i - 1] : null,
                    selectOnRight = i < _rewardPlaques.Count - 1 ? _rewardPlaques[i + 1] : null,
                    selectOnDown = _rewardTake
                };
            if (_rewardTake != null && _rewardPlaques.Count > 0)
            {
                var nav = _rewardTake.navigation;
                nav.selectOnUp = _rewardPlaques[0];
                _rewardTake.navigation = nav;
            }
            var first = _rewardPlaques.Count > 0 ? _rewardPlaques[0] : _rewardTake;
            if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }

        // One ema plaque on a string from the cord: icon, name, rarity tag, rule and its key. "Nothing" slots show a
        // blank plaque that grants the Take-nothing gold.
        private Button BuildPlaque(int index, ItemInstance item, float centre, float width)
        {
            var holder = ScrollUiKit.Area(_rewardPanel.transform, $"Plaque_{index}", new Vector2(centre - width * 0.5f, PlaqueBottom),
                new Vector2(centre + width * 0.5f, PlaqueTop));
            var str = ScrollUiKit.Fill(holder, "String", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), ScrollUiKit.Vermilion);
            str.rectTransform.offsetMin = new Vector2(-1f, -2f);
            str.rectTransform.offsetMax = new Vector2(1f, (CordY - PlaqueTop) * 1080f + 2f);

            var glow = ScrollUiKit.Fill(holder, "Glow", new Vector2(-0.12f, -0.08f), new Vector2(1.12f, 1.06f),
                new Color(1f, 0.85f, 0.50f, 0.0f), ScrollUiKit.Glow());
            var board = ScrollUiKit.Fill(holder, $"Slot_{index}", Vector2.zero, Vector2.one, Color.white, ScrollUiKit.Ema());
            board.raycastTarget = true;
            var shadow = board.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(4f, -8f);
            var button = board.gameObject.AddComponent<Button>();
            button.targetGraphic = board;
            var colors = ScrollUiKit.StateColors(Color.white);
            colors.highlightedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.selectedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.pressedColor = new Color(0.88f, 0.82f, 0.74f, 1f);
            button.colors = colors;

            if (item != null)
            {
                ScrollUiKit.Icon(board.transform, "Icon", "items/icon_" + ItemService.GetIconName(item.Type),
                    new Vector2(0.30f, 0.57f), new Vector2(0.70f, 0.83f), Color.white);
                ScrollUiKit.Label(board.transform, "Name", ItemService.GetItemName(item.Type), 30, TextAnchor.MiddleCenter,
                    ScrollUiKit.WoodTileText, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.53f), 18);
                ScrollUiKit.Pill(board.transform, "Rarity", new Vector2(0.33f, 0.376f), new Vector2(0.67f, 0.424f),
                    RarityLabel(item.Rarity), ScrollUiKit.RarityColor(item.Rarity), ScrollUiKit.RarityTextColor(item.Rarity), 15);
                ScrollUiKit.Label(board.transform, "Rule", ItemService.GetItemDescription(item.Type, item.Rarity), 20,
                    TextAnchor.UpperCenter, ScrollUiKit.WoodTileText, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.355f), 13);
            }
            else
            {
                ScrollUiKit.Label(board.transform, "Name", T("InRun.Reward.Nothing"), 30, TextAnchor.MiddleCenter,
                    ScrollUiKit.WoodTileText, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.56f), 18);
            }
            if (index < 9)
                ScrollUiKit.KeyBadge(board.transform, "KeyBadge", new Vector2(0.5f, 0.075f), index + 1);

            glow.transform.SetAsFirstSibling();
            var idx = index;
            button.onClick.AddListener(() => OnPlaqueClicked(idx));
            return button;
        }

        private void OnPlaqueClicked(int index)
        {
            if (!_awaitingReward || index < 0 || index >= _rewardSlots.Count) return;
            if (_selectedReward == index)
            {
                ClaimReward(index);
                return;
            }
            _selectedReward = index;
            for (var i = 0; i < _rewardPlaques.Count; i++)
            {
                var plaque = _rewardPlaques[i];
                if (plaque == null) continue;
                var holder = (RectTransform)plaque.transform.parent;
                var lift = i == index ? PlaqueLift : 0f;
                holder.anchorMin = new Vector2(holder.anchorMin.x, PlaqueBottom + lift);
                holder.anchorMax = new Vector2(holder.anchorMax.x, PlaqueTop + lift);
                var glow = holder.Find("Glow")?.GetComponent<Image>();
                if (glow != null) glow.color = new Color(1f, 0.85f, 0.50f, i == index ? 0.55f : 0f);
            }
            UpdateTakeButton();
        }

        private void UpdateTakeButton()
        {
            if (_rewardTake == null) return;
            var item = _selectedReward >= 0 && _selectedReward < _rewardSlots.Count ? _rewardSlots[_selectedReward] : null;
            var label = _rewardTake.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
                label.text = _selectedReward < 0 ? T("InRun.Reward.PickOne")
                    : item != null ? F("InRun.Reward.TakeItem", ItemService.GetItemName(item.Type))
                    : T("InRun.Reward.TakeNothingPlain");
            _rewardTake.interactable = _selectedReward >= 0;
        }

        // ── claiming ──

        private void ClaimReward(int index)
        {
            var run = _map?.Run;
            if (run == null || !_awaitingReward) return;

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
                            RecordItem(claimed);
                            FinishReward();
                        },
                        // Keeping the bag returns to the plaques: the player can still reroll, pick another or take
                        // nothing (the old screen closed and the reward was lost).
                        () => FocusPlaque(index));
                    return;
                }

                run.PickRewardItem(index);
                RecordItem(claimed);
            }
            else
                GrantNothingReward();   // a Nothing slot was chosen

            FinishReward();
        }

        private void FocusPlaque(int index)
        {
            if (index >= 0 && index < _rewardPlaques.Count && _rewardPlaques[index] != null)
                EventSystem.current?.SetSelectedGameObject(_rewardPlaques[index].gameObject);
        }

        private static void RecordItem(ItemInstance item) =>
            new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordItemDiscovery(item.Type);

        private void FinishReward()
        {
            if (!_awaitingReward) return;
            _awaitingReward = false;
            _rewardSlots.Clear();
            CloseRewardScene();
            OnPostRewardReady?.Invoke();
        }

        private void CloseRewardScene()
        {
            if (_plaqueReveal != null) StopCoroutine(_plaqueReveal);
            _plaqueReveal = null;
            _rewardPlaques.Clear();
            _selectedReward = -1;
            _rewardTake = null;
            if (_rewardPanel != null) DestroyNow(_rewardPanel);
            _rewardPanel = null;
            ActivePanel = null;
        }

        private Button BuildRewardRerollButton(Transform screen)
        {
            var run = _map?.Run;
            var label = run != null && run.State.RerollTokens > 0
                ? T("InRun.Reward.RerollToken")
                : F("InRun.Reward.RerollGold", run?.GetRewardRerollCost() ?? 0);
            var reroll = ScrollUiKit.RimButton(screen, "Slot_reroll", new Vector2(0.5182f, 0.0898f), new Vector2(0.626f, 0.1435f),
                label, 20, ScrollUiKit.Gold, ScrollUiKit.OfferingFocus, ScrollUiKit.OfferingText);
            // [REQ: REQ-ITEM-100] Reroll the item slots (GAP-015): a reroll token first, otherwise gold.
            reroll.interactable = run != null && run.CanRerollReward();
            reroll.onClick.AddListener(() =>
            {
                var rerolled = _map?.Run?.RerollRewardSlots();
                if (rerolled == null)
                {
                    OnStatusChanged?.Invoke(T("InRun.Reward.RerollUnavailable"));
                    return;
                }
                OnStatusChanged?.Invoke(T("InRun.Reward.Rerolled"));
                var text = reroll.transform.Find("Label")?.GetComponent<Text>();
                if (text != null)
                    text.text = _map.Run.State.RerollTokens > 0
                        ? T("InRun.Reward.RerollToken")
                        : F("InRun.Reward.RerollGold", _map.Run.GetRewardRerollCost());
                reroll.interactable = _map.Run.CanRerollReward();
                RerollRewardSlots(new List<ItemInstance>(rerolled));
            });
            return reroll;
        }

        // [REQ: REQ-ITEM-100] Choosing Nothing (a Nothing slot or Take nothing) grants 5 gold × floor number (SPEC-AMB-007).
        private void GrantNothingReward()
        {
            var gold = _map?.Run?.TakeNothingReward() ?? 0;
            if (gold > 0) OnStatusChanged?.Invoke(F("InRun.Reward.NothingGold", gold));
        }

        private static void AddShadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // Destroy is deferred to the end of the frame; hide and rename first so a rebuild in the same frame never
        // finds the old objects by name.
        private static void DestroyNow(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.name += "_old";
            Object.Destroy(go);
        }
    }
}
