using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// [H3-B] Centralised run-start perk effect enforcement.
    /// Called once by RunDirector.StartRun (or wherever run setup completes) after relics
    /// and items have been assigned to the RunState.
    /// Each active perk flag is translated into its concrete game-state side-effect here so
    /// no other service needs per-perk conditionals scattered through the codebase.
    /// </summary>
    public static class PerkService
    {
        /// <summary>
        /// Applies all active harmony perk run-start effects to <paramref name="state"/>.
        /// This must be called after the starting inventory is fully populated so item grants
        /// can be placed correctly.
        /// </summary>
        public static void ApplyRunStartEffects(RunState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            // ── MoonshadeOffering ────────────────────────────────────────────────────────────
            // Flag is already set on RunState. RewardService reads MoonshadeOfferingActive and
            // forces slot 1 of the first puzzle reward to Nothing. No additional state needed here.

            // ── ScholarsBurden ────────────────────────────────────────────────────────────────
            // Grant 1 Rare InkWell at run start (2 InkWells at H8+).
            if (state.ScholarsBurdenGrantsInkWell)
            {
                var inkWellCount = state.HarmonyLevel >= 8 ? 2 : 1;
                for (var i = 0; i < inkWellCount; i++)
                {
                    var inkWell = new ItemInstance
                    {
                        Id       = Guid.NewGuid().ToString("N"),
                        Type     = ItemType.InkWell,
                        Rarity   = ItemRarity.Rare,
                        Charges  = 1
                    };

                    // Try to place in an available item slot; if full, add anyway (overflow is
                    // the caller's responsibility — PerkService should not silently drop items).
                    state.HeldItems.Add(inkWell);
                }
                state.ScholarsBurdenGrantsInkWell = false; // consume once
            }

            // ── VoidWard ─────────────────────────────────────────────────────────────────────
            // Flag is already set on RunState. RunDirector.RollFloorModifiers reads VoidWardActive
            // and skips positive-effect rolling when true. No additional state needed here.

            // ── EmptyCanvas ───────────────────────────────────────────────────────────────────
            // Flag is already set on RunState. RewardService reads EmptyCanvasActive and enforces
            // Rare+ rarity on all item reward slots. No additional state needed here.
        }
    }
}
