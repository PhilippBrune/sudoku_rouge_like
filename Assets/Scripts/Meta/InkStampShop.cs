using System;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [REQ: DAILY-WALK-007] The ink stamp shop (owner 2026-10-25: "ink stamps also get a shop for looks"): three shelves of
    /// looks bought with ink stamps, looks only. Board ink colours the board's lines and given digits, the seal frame the
    /// ring round a class seal (Select Class, the Daily Walk's result), the path seal the floor seals on the run end. The
    /// first look of each shelf is the game's own and free. Owned looks are kept in DailyGoalState.UnlockedCosmetics
    /// (shelf × 10 + look), the worn ones in DailyGoalState.Worn*.
    /// </summary>
    public static class InkStampShop
    {
        public enum Shelf { BoardInk, SealFrame, PathSeal }

        public readonly struct Look
        {
            public readonly Shelf Shelf;
            public readonly int Index;
            public readonly string Key;
            public readonly int Price;
            public readonly Color Colour;
            public readonly bool Round;

            public Look(Shelf shelf, int index, string key, int price, Color colour, bool round = false)
            {
                Shelf = shelf;
                Index = index;
                Key = key;
                Price = price;
                Colour = colour;
                Round = round;
            }

            public int Code => (int)Shelf * 10 + Index;
        }

        private static readonly Color Sumi = new Color(0.11f, 0.10f, 0.08f, 1f);
        private static readonly Color Vermilion = new Color(0.70f, 0.23f, 0.18f, 1f);

        // Prices are first values (the mockup's), for playtesting.
        public static readonly Look[] Catalog =
        {
            new Look(Shelf.BoardInk, 0, "SumiBlack", 0, Sumi),
            new Look(Shelf.BoardInk, 1, "Indigo", 5, new Color(0.18f, 0.29f, 0.56f, 1f)),
            new Look(Shelf.BoardInk, 2, "Cinnabar", 5, new Color(0.62f, 0.16f, 0.17f, 1f)),
            new Look(Shelf.BoardInk, 3, "Moss", 5, new Color(0.18f, 0.29f, 0.20f, 1f)),
            new Look(Shelf.BoardInk, 4, "GoldLeaf", 12, new Color(0.54f, 0.39f, 0.13f, 1f)),
            new Look(Shelf.SealFrame, 0, "InkRing", 0, new Color(0.16f, 0.14f, 0.11f, 0.85f)),
            new Look(Shelf.SealFrame, 1, "MossRing", 6, new Color(0.44f, 0.61f, 0.40f, 1f)),
            new Look(Shelf.SealFrame, 2, "VermilionRing", 8, Vermilion),
            new Look(Shelf.SealFrame, 3, "GoldRing", 14, new Color(0.91f, 0.77f, 0.42f, 1f)),
            new Look(Shelf.PathSeal, 0, "VermilionSeal", 0, Vermilion),
            new Look(Shelf.PathSeal, 1, "IndigoSeal", 6, new Color(0.18f, 0.29f, 0.56f, 1f)),
            new Look(Shelf.PathSeal, 2, "RoundSumiSeal", 8, Sumi, true),
        };

        /// <summary>The looks worn now, for the screens that draw them (set when a profile loads and in the shop).</summary>
        public static Look WornBoardInk { get; private set; } = Catalog[0];
        public static Look WornSealFrame { get; private set; } = Catalog[5];
        public static Look WornPathSeal { get; private set; } = Catalog[9];

        // The free look of each shelf keeps the game's own colours exactly.
        public static Color GivenDigitColour(Color normal) => WornBoardInk.Index == 0 ? normal : WornBoardInk.Colour;
        public static Color LineColour(Color normal) => WornBoardInk.Index == 0 ? normal : WornBoardInk.Colour;
        public static Color SealRingColour(Color normal) => WornSealFrame.Index == 0 ? normal : WornSealFrame.Colour;
        public static Color PathSealColour(Color normal) => WornPathSeal.Index == 0 ? normal : WornPathSeal.Colour;

        /// <summary>The colour of placed digits: indigo, or sumi black under indigo ink so the two never look alike.</summary>
        public static Color PlacedDigitColour(Color placed) => WornBoardInk.Index == 1 ? Sumi : placed;

        public static bool Owns(DailyGoalState state, Look look) =>
            look.Price == 0 || (state?.UnlockedCosmetics != null && state.UnlockedCosmetics.Contains(look.Code));

        public static bool IsWorn(DailyGoalState state, Look look) => Worn(state, look.Shelf).Code == look.Code;

        public static Look Worn(DailyGoalState state, Shelf shelf)
        {
            var index = state == null ? 0 : shelf switch
            {
                Shelf.BoardInk => state.WornBoardInk,
                Shelf.SealFrame => state.WornSealFrame,
                _ => state.WornPathSeal
            };
            foreach (var look in Catalog)
                if (look.Shelf == shelf && look.Index == index) return look;
            return Array.Find(Catalog, l => l.Shelf == shelf);
        }

        /// <summary>Buys a look the player does not own (spending ink stamps) and wears it; false when it cannot.</summary>
        public static bool TryBuy(DailyGoalState state, Look look)
        {
            if (state == null || Owns(state, look) || state.TotalInkStamps < look.Price) return false;
            state.UnlockedCosmetics ??= new System.Collections.Generic.List<int>();
            state.TotalInkStamps -= look.Price;
            state.UnlockedCosmetics.Add(look.Code);
            Wear(state, look);
            return true;
        }

        /// <summary>Wears an owned look on its shelf.</summary>
        public static void Wear(DailyGoalState state, Look look)
        {
            if (state == null || !Owns(state, look)) return;
            switch (look.Shelf)
            {
                case Shelf.BoardInk: state.WornBoardInk = look.Index; break;
                case Shelf.SealFrame: state.WornSealFrame = look.Index; break;
                default: state.WornPathSeal = look.Index; break;
            }
            Apply(state);
        }

        /// <summary>Makes the profile's worn looks the ones the screens draw.</summary>
        public static void Apply(DailyGoalState state)
        {
            WornBoardInk = Worn(state, Shelf.BoardInk);
            WornSealFrame = Worn(state, Shelf.SealFrame);
            WornPathSeal = Worn(state, Shelf.PathSeal);
        }

        public static string Name(Look look) => LocalizationService.T("StampShop.Look." + look.Key);
        public static string Description(Look look) => LocalizationService.T("StampShop.Look." + look.Key + ".Desc");
    }
}
