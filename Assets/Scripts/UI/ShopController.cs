using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class ShopController : MonoBehaviour
    {
        private RunDirector _run;

        public void Bind(RunDirector run)
        {
            _run = run;
        }

        public List<ShopOffer> OpenShop()
        {
            return _run?.BuildShopOffers();
        }

        public bool BuyOffer(int offerIndex)
        {
            return _run != null && _run.TryPurchaseShopOffer(offerIndex);
        }

        public bool BuyEmergencyHeal()
        {
            return _run != null && _run.TryBuyEmergencyHeal();
        }
    }
}
