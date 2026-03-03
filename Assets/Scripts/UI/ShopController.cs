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
            return _run.BuildShopOffers();
        }

        public bool BuyOffer(string offerId)
        {
            return _run.TryPurchaseShopOffer(offerId);
        }

        public bool BuyEmergencyHeal()
        {
            return _run.TryBuyEmergencyHeal();
        }
    }
}
