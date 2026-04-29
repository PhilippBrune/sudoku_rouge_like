using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
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

        // Base pools (always available at H0).
        private static readonly RelicId[] BaseT1Pool = { RelicId.SmoothPebble, RelicId.CrackedTeacup, RelicId.WoodenComb, RelicId.MossToken };
        private static readonly RelicId[] BaseT2Pool = { RelicId.KoiReflectionRelic, RelicId.MonkCharm, RelicId.CopperTortoise, RelicId.JadeHairpin, RelicId.StoneSundial };
        private static readonly RelicId[] BaseT3Pool = { RelicId.SakuraSeal, RelicId.CrimsonFan, RelicId.WisteriaBranch, RelicId.PaperCrane, RelicId.PorcelainMask };
        private static readonly RelicId[] BaseT4Pool = { RelicId.TransmutedSigil, RelicId.PhoenixFeather, RelicId.SpiritLantern, RelicId.MoonstoneCompass };
        private static readonly RelicId[] BaseLegPool = { RelicId.GoldenRoot, RelicId.SilentGrid, RelicId.ShiftingGarden, RelicId.EternalLotus, RelicId.DragonsEye };

        // [HARMONY] Harmony-gated relic entries: (RelicId, Tier, MinHarmonyLevel)
        private static readonly (RelicId Id, RelicTier Tier, int MinHarmony)[] HarmonyRelics =
        {
            (RelicId.CrackedStone,   RelicTier.Tier1,    2),
            (RelicId.FrostedMirror,  RelicTier.Tier2,    5),
            (RelicId.VoidPetal,      RelicTier.Tier3,    7),
            (RelicId.LanternOfVoid,  RelicTier.Legendary, 9),
        };

        // Kept for backward-compat callers that don't pass harmonyLevel.
        private static readonly Dictionary<RelicTier, RelicId[]> TierPool = new()
        {
            [RelicTier.Tier1]    = BaseT1Pool,
            [RelicTier.Tier2]    = BaseT2Pool,
            [RelicTier.Tier3]    = BaseT3Pool,
            [RelicTier.Tier4]    = BaseT4Pool,
            [RelicTier.Legendary] = BaseLegPool
        };

        // Build a runtime pool dictionary that includes harmony-gated relics.
        private static Dictionary<RelicTier, RelicId[]> BuildHarmonyPool(int harmonyLevel)
        {
            var t1  = new List<RelicId>(BaseT1Pool);
            var t2  = new List<RelicId>(BaseT2Pool);
            var t3  = new List<RelicId>(BaseT3Pool);
            var t4  = new List<RelicId>(BaseT4Pool);
            var leg = new List<RelicId>(BaseLegPool);

            foreach (var (id, tier, minH) in HarmonyRelics)
            {
                if (harmonyLevel < minH) continue;
                switch (tier)
                {
                    case RelicTier.Tier1:    t1.Add(id);  break;
                    case RelicTier.Tier2:    t2.Add(id);  break;
                    case RelicTier.Tier3:    t3.Add(id);  break;
                    case RelicTier.Tier4:    t4.Add(id);  break;
                    case RelicTier.Legendary: leg.Add(id); break;
                }
            }

            return new Dictionary<RelicTier, RelicId[]>
            {
                [RelicTier.Tier1]    = t1.ToArray(),
                [RelicTier.Tier2]    = t2.ToArray(),
                [RelicTier.Tier3]    = t3.ToArray(),
                [RelicTier.Tier4]    = t4.ToArray(),
                [RelicTier.Legendary] = leg.ToArray()
            };
        }

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

        public static RelicId? GetExclusiveRelicForClass(ClassId classId)
        {
            foreach (var kvp in ClassExclusiveRelics)
                if (kvp.Value.ClassId == classId) return kvp.Key;
            return null;
        }

        public RelicService(int seed)
        {
            _random = new Random(seed);
        }

        // [REQ: RELIC-ROLL-001] Relic tier weights: floor-based 5×5 table; tierBonus shifts weights up (risk route)
        // [HARMONY] harmonyLevel gates which relics are available.
        public RelicInstance RollRelic(int floorIndex, int tierBonus = 0, int harmonyLevel = 0)
        {
            var tier = RollTier(floorIndex + tierBonus);
            var pool = BuildHarmonyPool(harmonyLevel)[tier];
            var id = pool[_random.Next(pool.Length)];

            return new RelicInstance
            {
                Id = id,
                Tier = tier,
                UsesRemaining = GetDefaultUses(id)
            };
        }

        /// <summary>Roll <paramref name="count"/> distinct relics for a choice panel.
        /// Pass classId + classLevel to allow class-exclusive relic injection (25% chance if unlocked).
        /// [HARMONY] harmonyLevel gates which relics can appear.</summary>
        public List<RelicInstance> RollRelicChoices(int floorIndex, int count = 3, int tierBonus = 0,
            ClassId classId = (ClassId)0, int classLevel = 0, int harmonyLevel = 0)
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

            var activePool = BuildHarmonyPool(harmonyLevel);
            for (var attempt = 0; attempt < count * 4 && choices.Count < count; attempt++)
            {
                var tier = RollTier(floorIndex + tierBonus);
                var pool = activePool[tier];
                var id = pool[_random.Next(pool.Length)];
                if (usedIds.Contains(id)) continue;
                usedIds.Add(id);
                choices.Add(new RelicInstance { Id = id, Tier = tier, UsesRemaining = GetDefaultUses(id) });
            }

            return choices;
        }

        /// <summary>Populates HeldRelics with <paramref name="count"/> random T1 starting relics at run start.
        /// Class-exclusive relics (L30) are NOT pre-loaded — they remain pool-injection-only (found during the run).
        /// [HARMONY-FLAG-002] At H7+ only T1 relics are offered at run start; harmony relics are still gated by harmonyLevel.</summary>
        public void AssignStartingRelics(RunState state, int count, int harmonyLevel = 0)
        {
            if (count <= 0) return;

            // At H7+ starting relics are T1 only (no higher tiers). Harmony-gated T1 relics included if available.
            // [M1-B] When StartingRelicT1Only is active, class-exclusive relics are also suppressed from the starting pool.
            var t1Pool = BuildHarmonyPool(harmonyLevel)[RelicTier.Tier1];
            var config = SudokuRoguelike.Run.HarmonyDifficultyService.BuildConfig(harmonyLevel);
            if (config.StartingRelicT1Only)
            {
                var filtered = new System.Collections.Generic.List<RelicId>();
                foreach (var id in t1Pool)
                    if (!IsRelicExclusive(id)) filtered.Add(id);
                if (filtered.Count > 0) t1Pool = filtered.ToArray();
            }
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
                RelicId.SilentGrid    => 3,
                RelicId.PhoenixFeather => 1,
                RelicId.WardingFlame  => 1, // once per run
                RelicId.VoidPetal     => 1, // [HARMONY] once per run
                _ => -1 // passive
            };
        }

        // ── Harmony relic helpers ────────────────────────────────────────────────────────────────

        /// <summary>[HARMONY-RELIC-002] CrackedStone: first mistake per puzzle costs HP and increments the counter;
        /// subsequent mistakes in the same puzzle are fully absorbed (no HP loss).
        /// Returns true if the mistake was absorbed (no HP should be deducted).</summary>
        public static bool TryCrackedStoneAbsorb(RunState state)
        {
            if (!HasRelicOfType(state, RelicId.CrackedStone)) return false;
            if (state.CrackedStoneMistakesThisPuzzle > 0) return true; // absorb
            state.CrackedStoneMistakesThisPuzzle++;  // first mistake — count it, let HP loss proceed
            return false;
        }

        /// <summary>[HARMONY-FLAG-002] LanternOfVoid: when held, boss modifier ??? labels are never shown.</summary>
        public static bool ShouldHideBossModifiers(RunState state) =>
            state.BossModifiersAlwaysHidden;

        /// <summary>[HARMONY] FrostedMirror: absorb 1 HP of mistake damage at cost of 3 pencil.
        /// Returns the final HP damage after absorption (0 if fully absorbed).
        /// At H5+ each mistake after the first is partially absorbed (1 HP reduced per proc).</summary>
        public static int ApplyFrostedMirror(RunState state, int hpDamage)
        {
            if (hpDamage <= 0) return hpDamage;
            if (!HasRelicOfType(state, RelicId.FrostedMirror)) return hpDamage;
            if (state.CurrentPencil < 3) return hpDamage;
            state.CurrentPencil -= 3;
            return Math.Max(0, hpDamage - 1);
        }

        /// <summary>[HARMONY] VoidPetal: survive a lethal hit at 1 HP once per run.
        /// Caller is responsible for granting 1 free Normal Solver item if slots allow.</summary>
        public static bool TryVoidPetal(RunState state)
        {
            var relic = FindRelicByType(state, RelicId.VoidPetal);
            if (relic == null || relic.UsesRemaining <= 0) return false;
            relic.UsesRemaining--;
            state.CurrentHP = 1;
            return true;
        }

        // ── Relic list helpers ──

        public static bool HasRelicOfType(RunState state, RelicId id) =>
            state.HeldRelics != null && state.HeldRelics.Exists(r => r.Id == id);

        private static RelicInstance FindRelicByType(RunState state, RelicId id) =>
            state.HeldRelics?.Find(r => r.Id == id);

        /// <summary>
        /// Returns the base pool tier for a relic, used to drive visual rarity pip on UI slots.
        /// Harmony-gated relics return their explicit tier; class-exclusive relics return Tier3.
        /// </summary>
        public static RelicTier GetBaseTier(RelicId id)
        {
            if (Array.IndexOf(BaseLegPool, id) >= 0) return RelicTier.Legendary;
            if (Array.IndexOf(BaseT4Pool, id) >= 0) return RelicTier.Tier4;
            if (Array.IndexOf(BaseT3Pool, id) >= 0) return RelicTier.Tier3;
            if (Array.IndexOf(BaseT2Pool, id) >= 0) return RelicTier.Tier2;
            for (var i = 0; i < HarmonyRelics.Length; i++)
                if (HarmonyRelics[i].Id == id) return HarmonyRelics[i].Tier;
            if (ClassExclusiveRelics.ContainsKey(id)) return RelicTier.Tier3;
            return RelicTier.Tier1;
        }

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
                case RelicId.LanternOfVoid:
                    state.BossModifiersAlwaysHidden = true;
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
                        // [REDESIGN] Reveal candidates (not solve) for 5 empty cells; feels like insight, not a solve.
                        RevealNCandidates(board, 5, state.Seed ^ state.CurrentFloor);
                        break;
                    case RelicId.DragonsEye:
                        SolveOneBox(board, state.Seed ^ state.CurrentFloor);
                        break;
                    case RelicId.TempleVow:
                        state.TempleVowReady = true;
                        break;
                }
            }
            // [C1-B] Reset per-puzzle CrackedStone mistake counter
            state.CrackedStoneMistakesThisPuzzle = 0;
        }

        /// <summary>Check TempleVow trigger on HP change; heals 3 HP once per puzzle if HP ≤ 25%.
        /// Call after any HP deduction.</summary>
        public static void OnHpChanged(RunState state)
        {
            if (!HasRelicOfType(state, RelicId.TempleVow)) return;
            if (!state.TempleVowReady) return;
            if (state.MaxHP > 0 && (float)state.CurrentHP / state.MaxHP <= 0.25f)
            {
                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 2);
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
                        // [REDESIGN] Heal 1 HP at streak ≥ 2; heal 3 HP (total) at streak ≥ 5.
                        if (level.PerfectSoFar)
                        {
                            state.PerfectPuzzleStreak++;
                            if (state.PerfectPuzzleStreak >= 5)
                            {
                                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 3);
                                state.PerfectPuzzleStreak = 0;
                            }
                            else if (state.PerfectPuzzleStreak >= 2)
                            {
                                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 1);
                            }
                        }
                        else
                        {
                            state.PerfectPuzzleStreak = 0;
                        }
                        break;
                    case RelicId.DuelingReed:
                        if (level.NoPencilUsed && level.Mistakes == 0)
                        {
                            state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + 2);
                            state.RerollTokens++;
                        }
                        break;
                    case RelicId.LoadBearingStone:
                        // [REDESIGN] Each non-absorbed mistake generates 5 bonus gold at end of puzzle.
                        if (level.Mistakes > 0)
                            gold += level.Mistakes * 5;
                        break;
                    case RelicId.LanternOfVoid:
                        // [HARMONY] +1 gold per active floor modifier on puzzle completion.
                        gold += state.ActiveFloorModifiers?.Count ?? 0;
                        break;
                }
            }
        }

        /// <summary>Called on run victory: apply end-of-run relic bonuses.</summary>
        public static void OnRunVictory(RunState state) { }

        /// <summary>Called after each correct placement: apply streak-based relic rewards.</summary>
        public static void OnCorrectPlacement(RunState state)
        {
            if (state.HeldRelics == null || state.HeldRelics.Count == 0) return;
            foreach (var relic in state.HeldRelics)
            {
                switch (relic.Id)
                {
                    case RelicId.MonkCharm:
                        // [REDESIGN] Every 5-combo: +5 gold AND +1 Pencil (was +2 gold only)
                        if (state.ComboStreak > 0 && state.ComboStreak % 5 == 0)
                        {
                            state.CurrentGold += 5;
                            state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + 1);
                        }
                        break;
                    case RelicId.FortunesLedger:
                        // [REDESIGN] Keep every-10 path; add perfect-streak burst (every 5 combo → +2 tokens)
                        state.FortunesLedgerCounter++;
                        if (state.FortunesLedgerCounter >= 10)
                        {
                            state.RerollTokens++;
                            state.FortunesLedgerCounter = 0;
                        }
                        if (state.ComboStreak > 0 && state.ComboStreak % 5 == 0)
                            state.RerollTokens += 2;
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
                        // [REDESIGN] Interest = 30% of gold SPENT this floor (not 50% of current gold).
                        // GoldSpentThisFloor is tracked in RunState; reset it after consuming.
                        if (state.GoldSpentThisFloor > 0)
                        {
                            var interest = Math.Max(1, (int)(state.GoldSpentThisFloor * 0.30f));
                            state.CurrentGold += interest;
                        }
                        state.GoldSpentThisFloor = 0;
                        break;
                    case RelicId.SilentGrid:
                        // [REDESIGN] Reset 3 mistake-shield charges at the start of every floor (was once per run).
                        state.MistakeShieldCharges = 3;
                        break;
                    case RelicId.AccurateMap:
                        // Reveal all nodes on the map; also guarantee at least one relic-tier reward node.
                        state.AllNodesRevealed = true;
                        state.BonusRelicNodeThisFloor = true; // RunDirector reads this when building node rewards
                        break;
                }
            }
        }

        /// <summary>Returns true when the player holds EndlessArchive (pencil marks cost 0).</summary>
        public static bool HasEndlessArchive(RunState state) =>
            HasRelicOfType(state, RelicId.EndlessArchive);

        // [REDESIGN] Reveal the correct digit as a pencil mark for <count> randomly chosen empty cells.
        // Unlike SolveNCells, this does NOT fill the cell — it only adds a pencil mark so the player
        // still needs to confirm the placement. Deterministic via seed.
        private static void RevealNCandidates(SudokuBoard board, int count, int seed)
        {
            var size = board.Size;
            var emptyCells = new List<(int r, int c)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (!board.IsGiven(r, c) && board.Cells[r, c] == 0 && board.Solution[r, c] > 0)
                    emptyCells.Add((r, c));

            var rng = new Random(seed);
            for (var i = emptyCells.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (emptyCells[i], emptyCells[j]) = (emptyCells[j], emptyCells[i]);
            }

            for (var i = 0; i < Math.Min(count, emptyCells.Count); i++)
            {
                var (r, c) = emptyCells[i];
                board.AddPencilMark(r, c, board.Solution[r, c]);
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

        // Fill randomly chosen empty cells (up to <paramref name="count"/>) with their solution values,
        // spread across the board using a seeded shuffle so the reveal feels varied each puzzle.
        private static void SolveNCells(SudokuBoard board, int count, int seed)
        {
            var size = board.Size;
            var emptyCells = new List<(int r, int c)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (!board.IsGiven(r, c) && board.Cells[r, c] == 0 && board.Solution[r, c] > 0)
                    emptyCells.Add((r, c));

            // Fisher-Yates shuffle with deterministic seed for reproducibility on resume
            var rng = new Random(seed);
            for (var i = emptyCells.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (emptyCells[i], emptyCells[j]) = (emptyCells[j], emptyCells[i]);
            }

            for (var i = 0; i < Math.Min(count, emptyCells.Count); i++)
            {
                var (r, c) = emptyCells[i];
                board.PlaceValue(r, c, board.Solution[r, c]);
                board.GivenMask[r, c] = true; // lock as pre-filled so it can't be erased
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

        /// <summary>Called at boss gate: WardingFlame fires once per run to remove one modifier from the pool.</summary>
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
                // Harmony-gated relics
                RelicId.CrackedStone     => "Cracked Stone",
                RelicId.FrostedMirror    => "Frosted Mirror",
                RelicId.VoidPetal        => "Void Petal",
                RelicId.LanternOfVoid    => "Lantern of Void",
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
                // Harmony-gated relics
                RelicId.CrackedStone      => "cracked_stone",
                RelicId.FrostedMirror     => "frosted_mirror",
                RelicId.VoidPetal         => "void_petal",
                RelicId.LanternOfVoid     => "lantern_of_void",
                _ => ""
            };
        }

        public static string GetIconFolder(RelicId id) => id switch
        {
            RelicId.GoldenRoot or RelicId.SilentGrid or RelicId.ShiftingGarden or
            RelicId.EternalLotus or RelicId.DragonsEye or
            RelicId.LanternOfVoid => "legendary",  // [HARMONY] legendary-tier harmony relic
            _ => "relic"
        };

        public static string GetRelicDescription(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "Start each puzzle with +2 pencil marks.",
                RelicId.CrackedTeacup => "Absorb the first mistake each puzzle (no HP loss).",
                RelicId.WoodenComb => "+1 Max HP.",
                RelicId.MossToken => "Shop rerolls cost 25% less.",
                RelicId.KoiReflectionRelic => "Reveal the correct candidates for 5 random empty cells at puzzle start (no auto-fill).",
                RelicId.MonkCharm => "Every 5-hit combo: +5 gold AND +1 pencil.",
                RelicId.CopperTortoise => "Perfect puzzle completion grants +15 gold.",
                RelicId.JadeHairpin => "+1 item slot capacity.",
                RelicId.StoneSundial => "+1 reward slot after each puzzle.",
                RelicId.SakuraSeal => "Perfect-puzzle streak: +1 HP at streak >=2; +3 HP (reset) at streak >=5.",
                RelicId.CrimsonFan => "Reduce all boss modifier intensities by one step.",
                RelicId.WisteriaBranch => "Heal 2 HP at the start of each floor.",
                RelicId.PaperCrane => "First item use each puzzle is free (no charge cost).",
                RelicId.PorcelainMask => "Elite reward rarity upgraded by one tier.",
                RelicId.TransmutedSigil => "+25% gold from puzzle completion.",
                RelicId.PhoenixFeather => "Prevent death once per run (heal to 1 HP).",
                RelicId.SpiritLantern => "Shop prices reduced by 20%.",
                RelicId.MoonstoneCompass => "Relic node tier boosted by +1.",
                RelicId.GoldenRoot => "At floor start, gain 30% of the gold spent during the previous floor.",
                RelicId.SilentGrid => "Start each floor with 3 fresh mistake-shield charges (each absorbed mistake costs 0 HP).",
                RelicId.ShiftingGarden => "Once per floor, a random given digit is relocated to another valid cell, keeping the puzzle uniquely solvable.",
                RelicId.EternalLotus => "Items are never consumed.",
                RelicId.DragonsEye => "Start each puzzle with one fully solved box.",
                // Class-exclusive relics (L30)
                RelicId.FortunesLedger   => "Every 10 correct placements: +1 Reroll Token. Every 5-hit combo: +2 Reroll Tokens. (NumberFreak exclusive)",
                RelicId.TempleVow        => "When HP drops to 25% or below, heal 3 HP once per puzzle. (GardenMonk exclusive)",
                RelicId.EndlessArchive   => "After each correct placement, reveal one candidate in the same row. (ShrineArchivist exclusive)",
                RelicId.GildedKoiScale   => "Boosts Koi Gambler procs to 40%: lucky hit grants +5 gold, absorbed mistake grants +1 Reroll Token. (KoiGambler exclusive)",
                RelicId.LoadBearingStone => "Each non-absorbed mistake generates 5 bonus gold at end of puzzle. (StoneGardener exclusive)",
                RelicId.WardingFlame     => "Once per run, automatically absorb one mistake (no HP loss). (LanternSeer exclusive)",
                RelicId.DuelingReed      => "Completing a puzzle with zero pencil marks grants +20 bonus gold. (ReedDuelist exclusive)",
                RelicId.AccurateMap      => "All floor nodes permanently revealed. Guarantees one relic-tier reward node per floor. (QuietCartographer exclusive)",
                // Harmony-gated relics
                RelicId.CrackedStone     => "[H2+] Each mistake after the first in a puzzle deals 0 HP (stone absorbs). Passive; unlimited.",
                RelicId.FrostedMirror    => "[H5+] Once per mistake: spend 3 pencil to absorb 1 HP of damage (auto-triggers if pencil ≥ 3). At H5+ each hit after the first in a puzzle is partially reduced.",
                RelicId.VoidPetal        => "[H7+] Once per run: survive a lethal hit at 1 HP. Grants 1 free Normal Solver upon activation.",
                RelicId.LanternOfVoid    => "[H9+] Boss modifier labels are always hidden (no auto-reveal on repeat). Earn +1 gold per active floor modifier on puzzle completion.",
                _ => ""
            };
        }
    }
}
