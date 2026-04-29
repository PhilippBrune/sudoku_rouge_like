using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public enum CurseSeverity { Mild, Medium, Nasty }

    public sealed class CurseDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public CurseSeverity Severity;
        public int MinFloor; // 0 = floor 1, 1 = floor 2, 2 = floor 3+
    }

    public sealed class CurseService
    {
        private readonly Random _random;

        // [REQ: CURSE-POOL-001..015] Full curse pool; IDs match CURSE-POOL-* in REQUIREMENT_MAP
        private static readonly List<CurseDefinition> AllCurses = new List<CurseDefinition>
        {
            // ── Mild (MinFloor 0) ──────────────────────────────────────────────
            new CurseDefinition { Id = "hollow_pencil",     Name = "Hollow Pencil",      Severity = CurseSeverity.Mild,   MinFloor = 0,  // [REQ: CURSE-POOL-001]
                Description = "All pencil marks cost 2 Pencil instead of 1." },
            new CurseDefinition { Id = "fog_of_memory",     Name = "Fog of Memory",       Severity = CurseSeverity.Mild,   MinFloor = 0,  // [REQ: CURSE-POOL-002]
                Description = "Board size and star rating are hidden at puzzle start." },
            new CurseDefinition { Id = "misfortune",        Name = "Misfortune",          Severity = CurseSeverity.Mild,   MinFloor = 0,  // [REQ: CURSE-POOL-003]
                Description = "Item reward screens show one fewer slot (min 1)." },
            new CurseDefinition { Id = "bad_luck",          Name = "Bad Luck",            Severity = CurseSeverity.Mild,   MinFloor = 0,  // [REQ: CURSE-POOL-004]
                Description = "Nothing slot probability +15% on all reward screens." },
            new CurseDefinition { Id = "price_gouge",       Name = "Price Gouge",         Severity = CurseSeverity.Mild,   MinFloor = 0,  // [REQ: CURSE-POOL-005]
                Description = "All shop items cost 35% more." },

            // ── Medium (MinFloor 1) ────────────────────────────────────────────
            new CurseDefinition { Id = "inkblot",           Name = "Inkblot",             Severity = CurseSeverity.Medium, MinFloor = 1,  // [REQ: CURSE-POOL-006]
                Description = "3 random cells are fogged at puzzle start." },
            new CurseDefinition { Id = "crumbling_focus",   Name = "Crumbling Focus",     Severity = CurseSeverity.Medium, MinFloor = 1,  // [REQ: CURSE-POOL-007]
                Description = "Max Pencil reduced by 4 immediately." },
            new CurseDefinition { Id = "sealed_eyes",       Name = "Sealed Eyes",         Severity = CurseSeverity.Medium, MinFloor = 1,  // [REQ: CURSE-POOL-008]
                Description = "Modifier descriptions are hidden during boss puzzles." },
            new CurseDefinition { Id = "blurred_sight",     Name = "Blurred Sight",       Severity = CurseSeverity.Medium, MinFloor = 1,  // [REQ: CURSE-POOL-009]
                Description = "Pencil marks disappear 1.5s after entry." },
            new CurseDefinition { Id = "restless_inventory",Name = "Restless Inventory",  Severity = CurseSeverity.Medium, MinFloor = 1,  // [REQ: CURSE-POOL-010]
                Description = "After each puzzle, a random item loses 1 charge." },

            // ── Nasty (MinFloor 2) ─────────────────────────────────────────────
            new CurseDefinition { Id = "phantom_pain",      Name = "Phantom Pain",        Severity = CurseSeverity.Nasty,  MinFloor = 2,  // [REQ: CURSE-POOL-011]
                Description = "Wrong placements cost 2 HP instead of 1." },
            new CurseDefinition { Id = "weight_of_stone",   Name = "Weight of Stone",     Severity = CurseSeverity.Nasty,  MinFloor = 2,  // [REQ: CURSE-POOL-012]
                Description = "Max HP reduced by 2 immediately (min 1)." },
            new CurseDefinition { Id = "trembling_hand",    Name = "Trembling Hand",      Severity = CurseSeverity.Nasty,  MinFloor = 2,  // [REQ: CURSE-POOL-013]
                Description = "The first placement each puzzle costs 1 HP regardless of correctness." },
            new CurseDefinition { Id = "fraying_thread",    Name = "Fraying Thread",      Severity = CurseSeverity.Nasty,  MinFloor = 2,  // [REQ: CURSE-POOL-014]
                Description = "Lose 1 Max HP at the start of each new floor (min 1)." },
            new CurseDefinition { Id = "double_or_nothing", Name = "Double or Nothing",   Severity = CurseSeverity.Nasty,  MinFloor = 2,  // [REQ: CURSE-POOL-015]
                Description = "Each item reward slot has a 20% chance to be rerolled to Nothing." },

            // ── Pressure-mechanic curses (floor 2+) ────────────────────────────
            new CurseDefinition { Id = "counting_shadow",   Name = "Counting Shadow",     Severity = CurseSeverity.Mild,   MinFloor = 2,
                Description = "At the start of each puzzle a single-cell countdown threat spawns. Counter = floor number + 4." },
            new CurseDefinition { Id = "hollow_eye",        Name = "Hollow Eye",          Severity = CurseSeverity.Medium, MinFloor = 2,
                Description = "A haunted cell is assigned at the start of each puzzle. Mistakes cost extra HP while it remains unfilled." },
        };

        private static readonly Dictionary<string, CurseDefinition> CurseById;

        static CurseService()
        {
            CurseById = new Dictionary<string, CurseDefinition>(AllCurses.Count);
            for (var i = 0; i < AllCurses.Count; i++)
                CurseById[AllCurses[i].Id] = AllCurses[i];
        }

        public CurseService(int seed)
        {
            _random = new Random(seed);
        }

        // ── Query ──────────────────────────────────────────────────────────────

        public static bool HasActiveCurse(RunState state) =>
            state.ActiveCurseIds != null && state.ActiveCurseIds.Count > 0;

        public static bool IsActive(RunState state, string curseId) =>
            state.ActiveCurseIds != null && state.ActiveCurseIds.Contains(curseId);

        public static List<CurseDefinition> GetActiveCurses(RunState state)
        {
            var result = new List<CurseDefinition>();
            if (state.ActiveCurseIds == null) return result;
            for (var i = 0; i < state.ActiveCurseIds.Count; i++)
                if (CurseById.TryGetValue(state.ActiveCurseIds[i], out var def))
                    result.Add(def);
            return result;
        }

        public static CurseDefinition GetDefinition(string id) =>
            CurseById.TryGetValue(id, out var def) ? def : null;

        // Maps each curse ID to its icon in Resources/cursed/.
        // Unique icons (prompts_cursed_2.txt) for the 10 new entries;
        // shared fallbacks kept for the 7 curses without their own art yet.
        private static readonly Dictionary<string, string> CurseIcons = new Dictionary<string, string>
        {
            { "hollow_pencil",      "hollow_pencil"    }, // unique icon (prompts_cursed_2)
            { "fog_of_memory",      "fog_of_memory"    }, // unique icon (prompts_cursed_2)
            { "misfortune",         "misfortune"       }, // unique icon (prompts_cursed_2)
            { "bad_luck",           "bad_luck"         }, // unique icon (prompts_cursed_2)
            { "price_gouge",        "price_gouge"      }, // unique icon (prompts_cursed_2)
            { "inkblot",            "inkblot"          }, // unique icon (prompts_cursed_2)
            { "crumbling_focus",    "crumbling_focus"  }, // unique icon (prompts_cursed_2)
            { "sealed_eyes",        "sealed_eyes"      }, // unique icon (prompts_cursed_2)
            { "phantom_pain",       "phantom_pain"     }, // unique icon (prompts_cursed_2)
            { "weight_of_stone",    "weight_of_stone"  }, // unique icon (prompts_cursed_2)
            // Shared icons — generate unique art from prompts_cursed_2 when ready
            { "blurred_sight",      "fog_stone"        },
            { "fraying_thread",     "cracked_tile"     },
            { "counting_shadow",    "blood_ink_brush"  },
            { "double_or_nothing",  "broken_mask"      },
            // F5: unique icons — pending generation (tbc/tbc_curse_icons.txt)
            { "restless_inventory", "restless_inventory" },
            { "trembling_hand",     "trembling_hand"     },
            { "hollow_eye",         "hollow_eye"         },
        };

        public static string GetIconName(string curseId) =>
            CurseIcons.TryGetValue(curseId, out var icon) ? icon : "withered_flower";

        // F23: bg_curse.png (background/bg_curse.png) is a full-screen darkening overlay applied
        // to the ENTIRE game panel when any curse is active. It is NOT a board replacement —
        // the sudoku board remains visible underneath. The overlay is rendered by CursePanelController
        // behind the side-panel curse list at a low alpha (≈ 0.25) to visually signal a cursed state.
        // Do not use it as a tile background or swap it for any board-level texture.

        // ── Mutation ───────────────────────────────────────────────────────────

        /// <summary>Apply a curse to the run state. Returns false if already active.</summary>
        // [REQ: CURSE-STACK-001] Unlimited curse stacking (no cap)
        public static bool ApplyCurse(RunState state, string curseId)
        {
            if (state.ActiveCurseIds == null) state.ActiveCurseIds = new System.Collections.Generic.List<string>();
            if (state.ActiveCurseIds.Contains(curseId)) return false;
            state.ActiveCurseIds.Add(curseId);

            // Apply immediate stat effects
            if (!CurseById.TryGetValue(curseId, out var def)) return true;
            switch (curseId)
            {
                case "crumbling_focus":
                    state.MaxPencil = Math.Max(1, state.MaxPencil - 4);
                    state.CurrentPencil = Math.Min(state.CurrentPencil, state.MaxPencil);
                    break;
                case "weight_of_stone":
                    state.MaxHP = Math.Max(1, state.MaxHP - 2);
                    state.CurrentHP = Math.Min(state.CurrentHP, state.MaxHP);
                    break;
            }
            return true;
        }

        /// <summary>Remove the oldest active curse, or a specific one by id. Returns false if none active.</summary>
        // [REQ: CURSE-REMOVE-001] Rest node "Cleanse a Curse" removes oldest active curse
        public static bool TryRemoveCurse(RunState state, string curseId = null)
        {
            if (state.ActiveCurseIds == null || state.ActiveCurseIds.Count == 0) return false;
            if (curseId != null)
            {
                var removed = state.ActiveCurseIds.Remove(curseId);
                return removed;
            }
            state.ActiveCurseIds.RemoveAt(0); // oldest = first in list
            return true;
        }

        /// <summary>
        /// Roll a curse for the given floor. Returns null if all floor-eligible curses are already active.
        /// </summary>
        // [REQ: CURSE-STACK-002] No duplicates — eligible pool excludes already-active curses
        public CurseDefinition RollCurse(int floorIndex, RunState state)
        {
            var eligible = new List<CurseDefinition>();
            for (var i = 0; i < AllCurses.Count; i++)
            {
                var c = AllCurses[i];
                if (c.MinFloor > floorIndex) continue;
                if (state.ActiveCurseIds != null && state.ActiveCurseIds.Contains(c.Id)) continue;
                eligible.Add(c);
            }
            if (eligible.Count == 0) return null;
            return eligible[_random.Next(eligible.Count)];
        }

        /// <summary>
        /// After a perfect (zero-mistake) puzzle, roll 25% (50% for GardenMonk) to auto-cleanse the oldest curse.
        /// Returns the name of the cleansed curse if successful, null otherwise.
        /// </summary>
        // [REQ: CURSE-REMOVE-002] Error-free puzzle: 25% chance to auto-clear oldest curse
        // [REQ: CURSE-REMOVE-005] GardenMonk increases auto-clear chance to 50%
        public string TryAutoCleanseOnPerfect(RunState state)
        {
            if (!HasActiveCurse(state)) return null;
            var chance = state.ClassId == ClassId.GardenMonk ? 0.50 : 0.25;
            if (_random.NextDouble() >= chance) return null;

            var oldestId = state.ActiveCurseIds[0];
            state.ActiveCurseIds.RemoveAt(0);
            return CurseById.TryGetValue(oldestId, out var def) ? def.Name : oldestId;
        }

        // ── Shop price modifier ────────────────────────────────────────────────

        // [REQ: CURSE-INT-005] price_gouge curse: shop items ×1.35
        public static float GetShopPriceMultiplier(RunState state) =>
            IsActive(state, "price_gouge") ? 1.35f : 1.0f;

        // ── Item reward slot modifiers ─────────────────────────────────────────

        // [REQ: CURSE-INT-003] misfortune curse: item reward screen shows one fewer slot (min 1)
        public static int GetSlotPenalty(RunState state) =>
            IsActive(state, "misfortune") ? -1 : 0;

        // [REQ: CURSE-INT-008] bad_luck curse: Nothing slot probability +15%
        public static float GetNothingChanceBonus(RunState state) =>
            IsActive(state, "bad_luck") ? 0.15f : 0.0f;

        /// <summary>Checks if a slot should be forcibly rerolled to Nothing (double_or_nothing curse).</summary>
        // [REQ: CURSE-INT-008] double_or_nothing curse: 20% chance to reroll slot to Nothing
        public bool RollDoubleOrNothing(RunState state)
        {
            if (!IsActive(state, "double_or_nothing")) return false;
            return _random.NextDouble() < 0.20;
        }

        // ── Mistake damage modifier ────────────────────────────────────────────

        // [REQ: CURSE-INT-002] phantom_pain curse: wrong placements cost 2 HP instead of 1
        public static int GetMistakeDamage(RunState state) =>
            IsActive(state, "phantom_pain") ? 2 : 1;

        // ── Per-puzzle hooks ───────────────────────────────────────────────────

        /// <summary>Called at puzzle start. Resets puzzle-scoped curse state.</summary>
        // [REQ: CURSE-INT-002] reset trembling_hand flag so first placement of new puzzle triggers cost
        public static void OnPuzzleStart(RunState state)
        {
            state.TremblingHandFired = false;
        }

        /// <summary>
        /// Called after every completed puzzle. Applies restless_inventory drain and auto-cleanse roll.
        /// Returns cleansed curse name if auto-cleanse fired, null otherwise.
        /// </summary>
        // [REQ: CURSE-INT-003] restless_inventory: drain 1 charge from random item after each puzzle
        // [REQ: CURSE-REMOVE-002] auto-cleanse roll fires here on perfect solve
        public string OnPuzzleComplete(RunState state, bool perfect)
        {
            // restless_inventory: drain 1 charge from a random item
            if (IsActive(state, "restless_inventory") && state.HeldItems != null && state.HeldItems.Count > 0)
            {
                var idx = _random.Next(state.HeldItems.Count);
                var item = state.HeldItems[idx];
                if (item != null && !item.IsInfinite)
                {
                    item.Charges--;
                    if (item.Charges <= 0)
                        state.HeldItems.RemoveAt(idx);
                }
            }

            // Auto-cleanse on perfect
            if (perfect)
                return TryAutoCleanseOnPerfect(state);

            return null;
        }

        /// <summary>Called at floor advance. Applies fraying_thread MaxHP drain.</summary>
        // [REQ: CURSE-INT-004] fraying_thread: −1 MaxHP at start of each new floor (min 1)
        public static void OnFloorStart(RunState state)
        {
            if (IsActive(state, "fraying_thread"))
            {
                state.MaxHP = Math.Max(1, state.MaxHP - 1);
                state.CurrentHP = Math.Min(state.CurrentHP, state.MaxHP);
            }
        }

        /// <summary>
        /// Called on each placement. Returns extra HP damage from TremblingHand (first placement),
        /// or 0 if none.
        /// </summary>
        // [REQ: CURSE-INT-002] trembling_hand: first placement costs 1 HP regardless of correctness
        public static int GetTremblingHandDamage(RunState state)
        {
            if (!IsActive(state, "trembling_hand") || state.TremblingHandFired) return 0;
            state.TremblingHandFired = true;
            return 1;
        }
    }
}
