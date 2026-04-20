using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Economy
{
    public sealed class RelicService
    {
        private readonly Random _random;

        // Tier weights per floor: [floor][T1, T2, T3, T4, Legendary]
        private static readonly float[][] TierWeights =
        {
            new[] { 0.50f, 0.30f, 0.15f, 0.05f, 0.00f },
            new[] { 0.30f, 0.35f, 0.25f, 0.08f, 0.02f },
            new[] { 0.15f, 0.30f, 0.30f, 0.18f, 0.07f },
            new[] { 0.05f, 0.20f, 0.30f, 0.30f, 0.15f },
            new[] { 0.00f, 0.10f, 0.25f, 0.35f, 0.30f }
        };

        private static readonly Dictionary<RelicTier, RelicId[]> TierPool = new()
        {
            [RelicTier.Tier1] = new[] { RelicId.SmoothPebble, RelicId.CrackedTeacup, RelicId.WoodenComb, RelicId.MossToken },
            [RelicTier.Tier2] = new[] { RelicId.KoiReflectionRelic, RelicId.MonkCharm, RelicId.CopperTortoise, RelicId.JadeHairpin, RelicId.StoneSundial },
            [RelicTier.Tier3] = new[] { RelicId.SakuraSeal, RelicId.CrimsonFan, RelicId.WisteriaBranch, RelicId.PaperCrane, RelicId.PorcelainMask },
            [RelicTier.Tier4] = new[] { RelicId.TransmutedSigil, RelicId.PhoenixFeather, RelicId.SpiritLantern, RelicId.MoonstoneCompass },
            [RelicTier.Legendary] = new[] { RelicId.GoldenRoot, RelicId.SilentGrid, RelicId.ShiftingGarden, RelicId.EternalLotus, RelicId.DragonsEye }
        };

        // ── Class-Exclusive Relic Definitions ──

        private sealed class ExclusiveRelicDef
        {
            public ClassId ClassId;
            public int UnlockLevel;
        }

        private static readonly Dictionary<RelicId, ExclusiveRelicDef> ClassExclusiveRelics = new()
        {
            [RelicId.FortunesLedger]  = new ExclusiveRelicDef { ClassId = ClassId.NumberFreak,       UnlockLevel = 30 },
            [RelicId.TempleVow]       = new ExclusiveRelicDef { ClassId = ClassId.GardenMonk,        UnlockLevel = 30 },
            [RelicId.EndlessArchive]  = new ExclusiveRelicDef { ClassId = ClassId.ShrineArchivist,   UnlockLevel = 30 },
            [RelicId.GildedKoiScale]  = new ExclusiveRelicDef { ClassId = ClassId.KoiGambler,        UnlockLevel = 30 },
            [RelicId.LoadBearingStone]= new ExclusiveRelicDef { ClassId = ClassId.StoneGardener,     UnlockLevel = 30 },
            [RelicId.WardingFlame]    = new ExclusiveRelicDef { ClassId = ClassId.LanternSeer,       UnlockLevel = 30 },
            [RelicId.DuelingReed]     = new ExclusiveRelicDef { ClassId = ClassId.ReedDuelist,       UnlockLevel = 30 },
            [RelicId.AccurateMap]     = new ExclusiveRelicDef { ClassId = ClassId.QuietCartographer, UnlockLevel = 30 },
        };

        public static bool IsRelicExclusive(RelicId id) => ClassExclusiveRelics.ContainsKey(id);

        public static ClassId GetExclusiveRelicClass(RelicId id) =>
            ClassExclusiveRelics.TryGetValue(id, out var d) ? d.ClassId : (ClassId)0;

        public RelicService(int seed)
        {
            _random = new Random(seed);
        }

        // [REQ: RELIC-ROLL-001] Relic tier weights: floor-based 5×5 table; tierBonus shifts weights up (risk route)
        public RelicInstance RollRelic(int floorIndex, int tierBonus = 0)
        {
            var tier = RollTier(floorIndex + tierBonus);
            var pool = TierPool[tier];
            var id = pool[_random.Next(pool.Length)];

            return new RelicInstance
            {
                Id = id,
                Tier = tier,
                UsesRemaining = GetDefaultUses(id)
            };
        }

        /// <summary>Roll <paramref name="count"/> distinct relics for a choice panel.
        /// Pass classId + classLevel to allow class-exclusive relic injection (25% chance if unlocked).</summary>
        public List<RelicInstance> RollRelicChoices(int floorIndex, int count = 3, int tierBonus = 0,
            ClassId classId = (ClassId)0, int classLevel = 0)
        {
            var choices = new List<RelicInstance>(count);
            var usedIds = new HashSet<RelicId>();

            // 25% chance to inject the class-exclusive relic if unlocked
            if (classId != (ClassId)0 && _random.NextDouble() < 0.25)
            {
                foreach (var kvp in ClassExclusiveRelics)
                {
                    if (kvp.Value.ClassId == classId && classLevel >= kvp.Value.UnlockLevel)
                    {
                        usedIds.Add(kvp.Key);
                        choices.Add(new RelicInstance { Id = kvp.Key, Tier = RelicTier.Legendary, UsesRemaining = GetDefaultUses(kvp.Key) });
                        break;
                    }
                }
            }

            for (var attempt = 0; attempt < count * 4 && choices.Count < count; attempt++)
            {
                var tier = RollTier(floorIndex + tierBonus);
                var pool = TierPool[tier];
                var id = pool[_random.Next(pool.Length)];
                if (usedIds.Contains(id)) continue;
                usedIds.Add(id);
                choices.Add(new RelicInstance { Id = id, Tier = tier, UsesRemaining = GetDefaultUses(id) });
            }

            return choices;
        }

        /// <summary>Populates HeldRelics with <paramref name="count"/> random T1 starting relics at run start.
        /// Class-exclusive relics (L30) are NOT pre-loaded — they remain pool-injection-only (found during the run).</summary>
        public void AssignStartingRelics(RunState state, int count)
        {
            if (count <= 0) return;

            var t1Pool = TierPool[RelicTier.Tier1];
            var usedIds = new HashSet<RelicId>();

            for (var i = 0; i < count; i++)
            {
                for (var attempt = 0; attempt < t1Pool.Length * 2; attempt++)
                {
                    var id = t1Pool[_random.Next(t1Pool.Length)];
                    if (usedIds.Contains(id)) continue;
                    usedIds.Add(id);
                    state.HeldRelics.Add(new RelicInstance { Id = id, Tier = RelicTier.Tier1, UsesRemaining = GetDefaultUses(id) });
                    break;
                }
            }

            // Keep legacy compat fields in sync
            if (state.HeldRelics.Count > 0)
            {
                state.HasRelic = true;
                state.HeldRelic = state.HeldRelics[0];
            }
        }

        public bool RollEliteRelicDrop(int floorIndex)
        {
            var chance = 0.15f + floorIndex * 0.0625f;
            return _random.NextDouble() < chance;
        }

        private RelicTier RollTier(int effectiveFloor)
        {
            var fi = Math.Clamp(effectiveFloor, 0, TierWeights.Length - 1);
            var weights = TierWeights[fi];
            var roll = (float)_random.NextDouble();
            var cumulative = 0f;

            for (var i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return (RelicTier)i;
            }

            return RelicTier.Tier1;
        }

        private static int GetDefaultUses(RelicId id)
        {
            return id switch
            {
                RelicId.CrackedTeacup => 1,
                RelicId.SilentGrid => 3,
                RelicId.PhoenixFeather => 1,
                RelicId.WardingFlame => 1, // once per run
                _ => -1 // passive
            };
        }

        // ── Relic list helpers ──

        private static bool HasRelicOfType(RunState state, RelicId id) =>
            state.HeldRelics != null && state.HeldRelics.Exists(r => r.Id == id);

        private static RelicInstance FindRelicByType(RunState state, RelicId id) =>
            state.HeldRelics?.Find(r => r.Id == id);

        // ── Relic Queries ──

        public static float GetShopRerollCostMultiplier(RunState state) =>
            HasRelicOfType(state, RelicId.MossToken) ? 0.75f : 1.0f;

        public static float GetShopPriceMultiplier(RunState state) =>
            HasRelicOfType(state, RelicId.SpiritLantern) ? 0.80f : 1.0f;

        public static int GetBonusRewardSlots(RunState state) =>
            HasRelicOfType(state, RelicId.StoneSundial) ? 1 : 0;

        public static int GetRelicNodeTierBonus(RunState state) =>
            HasRelicOfType(state, RelicId.MoonstoneCompass) ? 1 : 0;

        public static bool HasInfiniteItems(RunState state) =>
            HasRelicOfType(state, RelicId.EternalLotus);

        /// <summary>First item use each puzzle is free (PaperCrane).</summary>
        public static bool HasFirstItemFreeRelic(RunState state, LevelState level) =>
            HasRelicOfType(state, RelicId.PaperCrane) && level.ItemsUsedThisLevel == 0;

        /// <summary>Called on relic pickup: apply one-time stat buffs for the newly acquired relic.</summary>
        public static void ApplyPickupPassives(RunState state, RelicInstance relic)
        {
            if (relic == null) return;
            switch (relic.Id)
            {
                case RelicId.WoodenComb:
                    state.MaxHP++;
                    state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 1);
                    break;
                case RelicId.JadeHairpin:
                    state.ItemSlots++;
                    break;
                case RelicId.SilentGrid:
                    state.MistakeShieldCharges += relic.UsesRemaining > 0 ? relic.UsesRemaining : 3;
                    break;
            }
        }

        /// <summary>Called at puzzle start: apply per-puzzle relic bonuses for all held relics.</summary>
        public static void OnPuzzleStart(RunState state, SudokuBoard board)
        {
            if (state.HeldRelics == null || state.HeldRelics.Count == 0 || board == null) return;
            foreach (var relic in state.HeldRelics)
            {
                switch (relic.Id)
                {
                    case RelicId.SmoothPebble:
                        state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + 2);
                        break;
                    case RelicId.KoiReflectionRelic:
                        RevealOneCandidate(board);
                        break;
                    case RelicId.DragonsEye:
                        SolveOneBox(board, state.Seed ^ state.CurrentFloor);
                        break;
                    case RelicId.TempleVow:
                        state.TempleVowReady = true;
                        break;
                }
            }
        }

        /// <summary>Check TempleVow trigger on HP change; heals 3 HP once per puzzle if HP ≤ 25%.
        /// Call after any HP deduction.</summary>
        public static void OnHpChanged(RunState state)
        {
            if (!HasRelicOfType(state, RelicId.TempleVow)) return;
            if (!state.TempleVowReady) return;
            if (state.MaxHP > 0 && (float)state.CurrentHP / state.MaxHP <= 0.25f)
            {
                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 3);
                state.TempleVowReady = false;
            }
        }

        /// <summary>Called at puzzle completion: apply per-puzzle relic rewards for all held relics.</summary>
        public static void OnPuzzleComplete(RunState state, LevelState level, ref int gold)
        {
            if (state.HeldRelics == null || state.HeldRelics.Count == 0) return;
            foreach (var relic in state.HeldRelics)
            {
                switch (relic.Id)
                {
                    case RelicId.CopperTortoise:
                        if (level.PerfectSoFar) gold += 15;
                        break;
                    case RelicId.TransmutedSigil:
                        gold = (int)(gold * 1.25f);
                        break;
                    case RelicId.SakuraSeal:
                        if (level.PerfectSoFar)
                        {
                            state.PerfectPuzzleStreak++;
                            if (state.PerfectPuzzleStreak >= 3)
                            {
                                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 1);
                                state.PerfectPuzzleStreak = 0;
                            }
                        }
                        else
                        {
                            state.PerfectPuzzleStreak = 0;
                        }
                        break;
                    case RelicId.DuelingReed:
                        if (level.NoPencilUsed) gold += 20;
                        break;
                }
            }
        }

        /// <summary>Called on run victory: apply end-of-run relic bonuses.</summary>
        public static void OnRunVictory(RunState state)
        {
            if (HasRelicOfType(state, RelicId.GildedKoiScale))
                state.CurrentGold += state.CurrentGold;
        }

        /// <summary>Called after each correct placement: apply streak-based relic rewards.</summary>
        public static void OnCorrectPlacement(RunState state)
        {
            if (state.HeldRelics == null || state.HeldRelics.Count == 0) return;
            foreach (var relic in state.HeldRelics)
            {
                switch (relic.Id)
                {
                    case RelicId.MonkCharm:
                        if (state.ComboStreak > 0 && state.ComboStreak % 5 == 0)
                            state.CurrentGold += 2;
                        break;
                    case RelicId.FortunesLedger:
                        state.FortunesLedgerCounter++;
                        if (state.FortunesLedgerCounter >= 10)
                        {
                            state.RerollTokens++;
                            state.FortunesLedgerCounter = 0;
                        }
                        break;
                }
            }
        }

        /// <summary>Called at floor start: apply per-floor relic effects for all held relics.</summary>
        public static void OnFloorStart(RunState state)
        {
            if (state.HeldRelics == null || state.HeldRelics.Count == 0) return;
            foreach (var relic in state.HeldRelics)
            {
                switch (relic.Id)
                {
                    case RelicId.WisteriaBranch:
                        state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 2);
                        break;
                    case RelicId.GoldenRoot:
                        var interest = Math.Max(1, (int)(state.CurrentGold * 0.5f));
                        state.CurrentGold += interest;
                        break;
                    case RelicId.AccurateMap:
                        state.AllNodesRevealed = true;
                        break;
                }
            }
        }

        /// <summary>Called after a correct placement with board access (for EndlessArchive).</summary>
        public static void OnCorrectPlacementWithBoard(RunState state, int row, SudokuBoard board)
        {
            if (!HasRelicOfType(state, RelicId.EndlessArchive) || board == null) return;
            // Reveal correct candidate for first empty cell in same row
            var size = board.Size;
            for (var c = 0; c < size; c++)
            {
                if (board.Cells[row, c] != 0 || board.IsGiven(row, c)) continue;
                var sol = board.Solution[row, c];
                if (sol > 0) { board.AddPencilMark(row, c, sol); return; }
            }
        }

        // Reveal the correct digit as a pencil mark in the first empty non-given cell
        private static void RevealOneCandidate(SudokuBoard board)
        {
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (board.IsGiven(r, c) || board.Cells[r, c] != 0) continue;
                var sol = board.Solution[r, c];
                if (sol > 0) { board.AddPencilMark(r, c, sol); return; }
            }
        }

        // Fill all empty cells in one randomly chosen region with their solution values.
        // Chosen region must have at least one empty cell; solved cells are locked as given.
        private static void SolveOneBox(SudokuBoard board, int seed)
        {
            var size = board.Size;
            // Collect cells per region
            var regionCells = new Dictionary<int, List<(int r, int c)>>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var reg = board.RegionMap[r, c];
                if (!regionCells.ContainsKey(reg))
                    regionCells[reg] = new List<(int, int)>();
                regionCells[reg].Add((r, c));
            }
            // Regions that still have at least one empty cell
            var candidates = new List<int>();
            foreach (var kv in regionCells)
            {
                foreach (var (r, c) in kv.Value)
                    if (board.Cells[r, c] == 0) { candidates.Add(kv.Key); break; }
            }
            if (candidates.Count == 0) return;
            var rng = new Random(seed);
            var chosen = candidates[rng.Next(candidates.Count)];
            foreach (var (r, c) in regionCells[chosen])
            {
                if (board.Cells[r, c] == 0 && board.Solution[r, c] > 0)
                {
                    board.PlaceValue(r, c, board.Solution[r, c]);
                    board.GivenMask[r, c] = true; // lock as pre-filled
                }
            }
        }

        /// <summary>Called on any mistake: WardingFlame fires once per run to negate the HP loss.</summary>
        public static bool TryWardingFlame(RunState state)
        {
            var relic = FindRelicByType(state, RelicId.WardingFlame);
            if (relic == null) return false;
            if (state.WardingFlameUsed || relic.UsesRemaining <= 0) return false;
            state.WardingFlameUsed = true;
            relic.UsesRemaining--;
            return true; // absorb the HP loss
        }

        public static bool TryPreventDeath(RunState state)
        {
            var relic = FindRelicByType(state, RelicId.PhoenixFeather);
            if (relic == null) return false;
            if (relic.UsesRemaining <= 0) return false;
            relic.UsesRemaining--;
            state.CurrentHP = 1;
            return true;
        }

        public static bool TryAbsorbMistake(RunState state)
        {
            var relic = FindRelicByType(state, RelicId.CrackedTeacup);
            if (relic == null || relic.UsesRemaining <= 0) return false;
            relic.UsesRemaining--;
            return true;
        }

        // ── Name & Description ──

        public static string GetRelicName(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "Smooth Pebble",
                RelicId.CrackedTeacup => "Cracked Teacup",
                RelicId.WoodenComb => "Wooden Comb",
                RelicId.MossToken => "Moss Token",
                RelicId.KoiReflectionRelic => "Koi Reflection",
                RelicId.MonkCharm => "Monk's Charm",
                RelicId.CopperTortoise => "Copper Tortoise",
                RelicId.JadeHairpin => "Jade Hairpin",
                RelicId.StoneSundial => "Stone Sundial",
                RelicId.SakuraSeal => "Sakura Seal",
                RelicId.CrimsonFan => "Crimson Fan",
                RelicId.WisteriaBranch => "Wisteria Branch",
                RelicId.PaperCrane => "Paper Crane",
                RelicId.PorcelainMask => "Porcelain Mask",
                RelicId.TransmutedSigil => "Transmuted Sigil",
                RelicId.PhoenixFeather => "Phoenix Feather",
                RelicId.SpiritLantern => "Spirit Lantern",
                RelicId.MoonstoneCompass => "Moonstone Compass",
                RelicId.GoldenRoot => "Golden Root",
                RelicId.SilentGrid => "Silent Grid",
                RelicId.ShiftingGarden => "Shifting Garden",
                RelicId.EternalLotus => "Eternal Lotus",
                RelicId.DragonsEye => "Dragon's Eye",
                // Class-exclusive relics (L30)
                RelicId.FortunesLedger   => "Fortune's Ledger",
                RelicId.TempleVow        => "Temple Vow",
                RelicId.EndlessArchive   => "Endless Archive",
                RelicId.GildedKoiScale   => "Gilded Koi Scale",
                RelicId.LoadBearingStone => "Load-Bearing Stone",
                RelicId.WardingFlame     => "Warding Flame",
                RelicId.DuelingReed      => "Dueling Reed",
                RelicId.AccurateMap      => "Accurate Map",
                _ => id.ToString()
            };
        }

        public static string GetIconName(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble      => "smooth_pebble",
                RelicId.CrackedTeacup     => "cracked_teacup",
                RelicId.WoodenComb        => "wooden_comb",
                RelicId.MossToken         => "moss_token",
                RelicId.KoiReflectionRelic=> "koi_reflection_relic",
                RelicId.MonkCharm         => "monk_charm",
                RelicId.CopperTortoise    => "copper_tortoise",
                RelicId.JadeHairpin       => "jade_hairpin",
                RelicId.StoneSundial      => "stone_sundial",
                RelicId.SakuraSeal        => "sakura_seal",
                RelicId.CrimsonFan        => "crimson_fan",
                RelicId.WisteriaBranch    => "wisteria_branch",
                RelicId.PaperCrane        => "paper_crane",
                RelicId.PorcelainMask     => "porcelain_mask",
                RelicId.TransmutedSigil   => "transmuted_sigil",
                RelicId.PhoenixFeather    => "phoenix_feather",
                RelicId.SpiritLantern     => "spirit_lantern",
                RelicId.MoonstoneCompass  => "moonstone_compass",
                RelicId.GoldenRoot        => "golden_root",
                RelicId.SilentGrid        => "silent_grid",
                RelicId.ShiftingGarden    => "shifting_garden",
                RelicId.EternalLotus      => "eternal_lotus",
                RelicId.DragonsEye        => "dragons_eye",
                // Class-exclusive relics (L30)
                RelicId.FortunesLedger    => "fortunes_ledger",
                RelicId.TempleVow         => "temple_vow",
                RelicId.EndlessArchive    => "endless_archive",
                RelicId.GildedKoiScale    => "gilded_koi_scale",
                RelicId.LoadBearingStone  => "load_bearing_stone",
                RelicId.WardingFlame      => "warding_flame",
                RelicId.DuelingReed       => "dueling_reed",
                RelicId.AccurateMap       => "accurate_map",
                _ => ""
            };
        }

        public static string GetRelicDescription(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "Start each puzzle with +2 pencil marks.",
                RelicId.CrackedTeacup => "Absorb the first mistake each puzzle (no HP loss).",
                RelicId.WoodenComb => "+1 Max HP.",
                RelicId.MossToken => "Shop rerolls cost 25% less.",
                RelicId.KoiReflectionRelic => "Reveal one candidate per puzzle start.",
                RelicId.MonkCharm => "5 correct placements in a row grants +2 gold.",
                RelicId.CopperTortoise => "Perfect puzzle completion grants +15 gold.",
                RelicId.JadeHairpin => "+1 item slot capacity.",
                RelicId.StoneSundial => "+1 reward slot after each puzzle.",
                RelicId.SakuraSeal => "No-hit puzzle streak: heal 1 HP after 3 puzzles.",
                RelicId.CrimsonFan => "Reduce boss modifier intensity by one step.",
                RelicId.WisteriaBranch => "Heal 2 HP at the start of each floor.",
                RelicId.PaperCrane => "First item use each puzzle is free (no charge cost).",
                RelicId.PorcelainMask => "Elite reward rarity upgraded by one tier.",
                RelicId.TransmutedSigil => "+25% gold from puzzle completion.",
                RelicId.PhoenixFeather => "Prevent death once per run (heal to 1 HP).",
                RelicId.SpiritLantern => "Shop prices reduced by 20%.",
                RelicId.MoonstoneCompass => "Relic node tier boosted by +1.",
                RelicId.GoldenRoot => "+50% gold interest at floor end.",
                RelicId.SilentGrid => "Next 3 mistakes are silent (no HP loss).",
                RelicId.ShiftingGarden => "Puzzles occasionally reshuffle given digits.",
                RelicId.EternalLotus => "Items are never consumed.",
                RelicId.DragonsEye => "Start each puzzle with one fully solved box.",
                // Class-exclusive relics (L30)
                RelicId.FortunesLedger   => "Every 10 correct placements grant +1 Reroll Token. (NumberFreak exclusive)",
                RelicId.TempleVow        => "When HP drops to 25% or below, heal 3 HP once per puzzle. (GardenMonk exclusive)",
                RelicId.EndlessArchive   => "After each correct placement, reveal one candidate in the same row. (ShrineArchivist exclusive)",
                RelicId.GildedKoiScale   => "On run victory, your current gold is doubled. (KoiGambler exclusive)",
                RelicId.LoadBearingStone => "Each mistake also deducts from the boss penalty pool. (StoneGardener exclusive)",
                RelicId.WardingFlame     => "Once per run, automatically absorb one mistake (no HP loss). (LanternSeer exclusive)",
                RelicId.DuelingReed      => "Completing a puzzle with zero pencil marks grants +20 bonus gold. (ReedDuelist exclusive)",
                RelicId.AccurateMap      => "All floor node types are permanently revealed. (QuietCartographer exclusive)",
                _ => ""
            };
        }
    }
}
