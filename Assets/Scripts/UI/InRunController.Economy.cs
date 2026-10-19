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

        private void WireBuyPencilButton(RectTransform sudokuPanel)
        {
            _buyPencilButton = sudokuPanel != null ? sudokuPanel.Find("BtnBuyPencil")?.GetComponent<Button>() : null;
            if (_buyPencilButton == null) return;
            _buyPencilLabel = _buyPencilButton.GetComponentInChildren<Text>(true);
            _buyPencilButton.onClick.RemoveAllListeners();
            _buyPencilButton.onClick.AddListener(BuyPencil);
            _buyPencilShownCost = -1;
            _buyPencilShownState = -1;
        }

        /// <summary>Called with the HUD refresh: shows the cost and whether a purchase is possible right now.</summary>
        private void RefreshBuyPencilButton(RunDirector run)
        {
            if (_buyPencilButton == null || run?.State == null) return;
            var state = run.State;
            var offered = ClassPassiveService.CanBuyPencilMidLevel(state)
                && !state.TutorialMode && !state.IsSeasonalChallenge && !state.DisableProgressionRewards;
            var shown = !offered ? 0 : run.CanBuyPencil() ? 2 : 1;
            if (shown != _buyPencilShownState)
            {
                _buyPencilShownState = shown;
                _buyPencilButton.gameObject.SetActive(shown != 0);
                _buyPencilButton.interactable = shown == 2;
                // UI-STYLE-002: dark ink on the wooden tile; light text on the faded tile when a purchase isn't possible.
                if (_buyPencilLabel != null)
                    _buyPencilLabel.color = shown == 2 ? ScrollUiKit.WoodTileText : new Color(0.95f, 0.91f, 0.82f, 0.85f);
            }
            if (shown == 0) return;
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
