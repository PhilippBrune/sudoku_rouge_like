using SudokuRoguelike.Classes;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: REQ-ECON-100] Buy-pencil button on the puzzle HUD (GDS §5.2/§5.3, GAP-016).
    public sealed partial class InRunController
    {
        private Button _buyPencilButton;
        private Text _buyPencilLabel;
        private int _buyPencilShownCost = -1;
        private int _buyPencilShownState = -1;   // -1 unknown, 0 hidden, 1 disabled, 2 enabled
        private Text _goldTagText;
        private int _goldTagShown = -1;

        private void WireBuyPencilButton(RectTransform sudokuPanel)
        {
            _buyPencilButton = sudokuPanel != null ? sudokuPanel.Find("BtnBuyPencil")?.GetComponent<Button>() : null;
            if (_buyPencilButton == null) return;
            _buyPencilLabel = _buyPencilButton.GetComponentInChildren<Text>(true);
            _buyPencilButton.onClick.RemoveAllListeners();
            _buyPencilButton.onClick.AddListener(BuyPencil);
            _buyPencilShownCost = -1;
            _buyPencilShownState = -1;
            _goldTagShown = -1;
            EnsureGoldTag();
        }

        // [REQ: HUD-GOLD-001] RM-208: the gold sits beside the Pencil it buys, under the HP lantern (UI-STYLE-036).
        private void EnsureGoldTag()
        {
            if (_goldTagText != null) return;
            var tag = ScrollUiKit.Fill(_buyPencilButton.transform.parent, "GoldTag", new Vector2(0.040f, 0.563f), new Vector2(0.1177f, 0.603f),
                new Color(0.23f, 0.16f, 0.11f, 0.92f), ScrollUiKit.Rounded(8f));
            ScrollUiKit.Icon(tag.transform, "Coin", "economy/icon_sakura_coin", new Vector2(0.05f, 0.14f), new Vector2(0.24f, 0.86f), Color.white);
            _goldTagText = ScrollUiKit.Label(tag.transform, "Gold", "", 17, TextAnchor.MiddleLeft, new Color(0.95f, 0.87f, 0.65f, 1f),
                new Vector2(0.28f, 0f), new Vector2(0.98f, 1f), 11);
            tag.gameObject.SetActive(false);
        }

        /// <summary>Called with the HUD refresh: shows the cost and whether a purchase is possible right now.</summary>
        private void RefreshBuyPencilButton(RunDirector run)
        {
            if (_buyPencilButton == null || run?.State == null) return;
            var state = run.State;
            var offered = ClassPassiveService.CanBuyPencilMidLevel(state)
                && !state.TutorialMode && !state.IsSeasonalChallenge && !state.DisableProgressionRewards
                && state.Mode != SudokuRoguelike.Core.GameMode.IncenseWalk;   // GAP-085: marks are free there; it covered the time
            var shown = !offered ? 0 : run.CanBuyPencil() ? 2 : 1;
            if (shown != _buyPencilShownState)
            {
                _buyPencilShownState = shown;
                _buyPencilButton.gameObject.SetActive(shown != 0);
                if (_goldTagText != null) _goldTagText.transform.parent.gameObject.SetActive(shown != 0);
                _buyPencilButton.interactable = shown == 2;
                // UI-STYLE-002: dark ink on the wooden tile; light text on the faded tile when a purchase isn't possible.
                if (_buyPencilLabel != null)
                    _buyPencilLabel.color = shown == 2 ? ScrollUiKit.WoodTileText : new Color(0.95f, 0.91f, 0.82f, 0.85f);
            }
            if (shown == 0) return;
            if (_goldTagText != null && state.CurrentGold != _goldTagShown)
            {
                _goldTagShown = state.CurrentGold;
                _goldTagText.text = Tf("InRun.Hud.GoldTag", state.CurrentGold);
            }
            var cost = run.GetPencilBuyCost();
            if (cost != _buyPencilShownCost && _buyPencilLabel != null)
            {
                _buyPencilShownCost = cost;
                _buyPencilLabel.text = Tf("InRun.BuyPencil.Cost", RunDirector.PencilPerPurchase, cost);
            }
        }

        private void BuyPencil()
        {
            var run = _map?.Run;
            if (run == null) return;
            if (!run.TryBuyPencil())
            {
                SetStatus(T("InRun.BuyPencil.Unavailable"));
                return;
            }
            _audio?.PlayShopPurchase();
            SetStatus(Tf("InRun.BuyPencil.Bought", RunDirector.PencilPerPurchase, run.State.CurrentPencil));
            _map?.SaveNow();
        }
    }
}
