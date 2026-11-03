using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: HUD-DETAIL-001] [REQ: UI-STYLE-036] RM-208 (0.3.0 playtest): a paper slip at the foot of the left column shows
    // the class passive, or the item or relic under the pointer, in focus or picked. The item and relic texts used to go
    // to the status line above the board, where a long one ran behind it on a wide screen.
    public sealed partial class HudViewController
    {
        private Image _detailSlip;
        private Text _detailEyebrow, _detailTitle, _detailBody, _detailFoot;
        private int _hoverItem = -1, _hoverRelic = -1;
        private string _detailShown;
        private int _detailKey;

        private void EnsureDetailSlip()
        {
            if (_detailSlip != null || _sudokuPanel == null) return;
            _detailSlip = ScrollUiKit.PaperTag(_sudokuPanel.transform, "DetailSlip", new Vector2(0.035f, 0.015f),
                new Vector2(0.25f, 0.175f), -0.5f);
            _detailSlip.raycastTarget = false;
            var band = ScrollUiKit.Fill(_detailSlip.transform, "Band", new Vector2(0f, 0.95f), Vector2.one, ScrollUiKit.Moss);
            band.raycastTarget = false;
            // [REQ: DECK-TEXT-001] The run's canvas scales by 0.667 on the Steam Deck's 1280×800: size 14 is its 9 px.
            _detailEyebrow = ScrollUiKit.Label(_detailSlip.transform, "DetailEyebrow", "", 14, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.76f), new Vector2(0.96f, 0.93f), 14);
            _detailTitle = ScrollUiKit.Label(_detailSlip.transform, "DetailTitle", "", 22, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                new Vector2(0.05f, 0.55f), new Vector2(0.96f, 0.77f), 13);
            _detailBody = ScrollUiKit.Label(_detailSlip.transform, "DetailBody", "", 16, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.21f), new Vector2(0.96f, 0.55f), 10);
            _detailFoot = ScrollUiKit.Label(_detailSlip.transform, "DetailFoot", "", 14, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.02f), new Vector2(0.96f, 0.21f), 14);   // two lines for the longest hint (German)
        }

        /// <summary>Per HUD refresh (every frame): rebuilds the slip only when what it shows has changed.</summary>
        private void RefreshDetailIfChanged()
        {
            var state = _map?.Run?.State;
            if (state == null) return;
            var item = FocusedItem(state);
            var relic = item == null ? FocusedRelic(state) : null;
            unchecked
            {
                var key = (int)state.ClassId * 31 + (state.TutorialMode ? 7 : 3) + (_incenseShown ? 11 : 5)
                          + (int)LocalizationService.Current * 101 + (InputDeviceTracker.UsingPad ? 1009 : 0);
                if (item != null) key = key * 397 + ((int)item.Type * 13 + (int)item.Rarity) * 17 + item.Charges + 1;
                if (relic != null) key = key * 389 + (int)relic.Id * 19 + relic.UsesRemaining + 2;
                if (key == _detailKey && _detailSlip != null) return;
                _detailKey = key;
            }
            RefreshDetail();
        }

        private ItemInstance FocusedItem(RunState state) => ItemAt(state, _hoverItem >= 0 ? _hoverItem : _selectedBagSlot);

        private RelicInstance FocusedRelic(RunState state)
        {
            var index = _hoverItem < 0 && _hoverRelic >= 0 ? _hoverRelic : _selectedRelicSlot;
            return index >= 0 && state.HeldRelics != null && index < state.HeldRelics.Count ? state.HeldRelics[index] : null;
        }

        /// <summary>Shows what has the attention: an item or relic under the pointer, then a picked one, else the passive.</summary>
        private void RefreshDetail(bool announce = false)
        {
            var state = _map?.Run?.State;
            EnsureDetailSlip();
            if (_detailSlip == null || state == null) return;

            string eyebrow, title, body, foot;
            var item = FocusedItem(state);
            var relic = item == null ? FocusedRelic(state) : null;
            if (item != null)
            {
                eyebrow = F("InRun.Hud.Detail.ItemEyebrow", RarityLabel(item.Rarity));
                title = ItemService.GetItemName(item.Type) + (item.Charges > 1 ? $" ×{item.Charges}" : "");
                body = ItemService.GetItemDescription(item.Type, item.Rarity);
                foot = DeviceHint.For("InRun.Hud.Detail.ItemUse");
            }
            else if (relic != null)
            {
                eyebrow = T("InRun.Hud.Detail.RelicEyebrow");
                title = RelicService.GetRelicName(relic.Id);
                body = RelicService.GetRelicDescription(relic.Id);
                foot = relic.UsesRemaining < 0 ? DeviceHint.For("InRun.Hud.Detail.RelicAlways")
                    : relic.UsesRemaining == 0 ? T("InRun.Hud.RelicSpent")
                    : DeviceHint.For("InRun.Hud.Detail.RelicUses", relic.UsesRemaining);
            }
            else if (ClassCatalog.IsClass(state.ClassId))
            {
                var def = ClassCatalog.GetDefinition(state.ClassId);
                eyebrow = T("InRun.Hud.Detail.PassiveEyebrow");
                title = ClassCatalog.DisplayName(state.ClassId);
                body = LocalizationService.T($"Class.Passive.{def.Id}", def.PassiveDescription);
                foot = DeviceHint.For("InRun.Hud.Detail.Hover");
            }
            else if (!state.TutorialMode)
            {
                eyebrow = T("InRun.Hud.Detail.PassiveEyebrow");   // [REQ: DAILY-WALK-002] the day's walker
                title = T("DailyWalk.Walker");
                body = T("DailyWalk.Walker.Passive");
                foot = DeviceHint.For("InRun.Hud.Detail.Hover");
            }
            else
            {
                // CUSTOM-CLASS-001: a Custom Puzzle without a class has no passive; the slip waits for an item or relic.
                eyebrow = title = body = foot = string.Empty;
            }

            var shown = eyebrow + "|" + title + "|" + body + "|" + foot;
            _detailSlip.gameObject.SetActive(title.Length > 0 && !_incenseShown);
            if (shown == _detailShown) return;
            _detailShown = shown;
            _detailEyebrow.text = eyebrow.ToUpperInvariant();
            _detailTitle.text = title;
            _detailBody.text = body;
            _detailFoot.text = foot;
            if (announce && OptionsController.LiveAccessibility.ScreenReaderEnabled)
                AccessibilityAnnouncementOverlay.Announce(title + ". " + body);
        }

        private static ItemInstance ItemAt(RunState state, int slot) =>
            slot >= 0 && state.HeldItems != null && slot < state.HeldItems.Count ? state.HeldItems[slot] : null;

        /// <summary>The slip follows the pointer and the keyboard or pad focus on a bag or relic slot.</summary>
        private void WatchForDetail(Button button, int index, bool relic)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();   // not ??: Unity's fake null
            void On(EventTriggerType type, bool enter)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ =>
                {
                    if (relic) _hoverRelic = enter ? index : (_hoverRelic == index ? -1 : _hoverRelic);
                    else _hoverItem = enter ? index : (_hoverItem == index ? -1 : _hoverItem);
                    RefreshDetail(enter && (type == EventTriggerType.Select));
                });
                trigger.triggers.Add(entry);
            }
            On(EventTriggerType.PointerEnter, true);
            On(EventTriggerType.PointerExit, false);
            On(EventTriggerType.Select, true);
            On(EventTriggerType.Deselect, false);
        }
    }
}
