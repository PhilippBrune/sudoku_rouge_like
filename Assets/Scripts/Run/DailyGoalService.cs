using System;
using System.Collections.Generic;
using System.Globalization;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Manages the Daily Walk — 3 deterministic date-seeded goals per day, met inside the day's walk (DailyGoalSystem 0.3).
    /// Goals are identical for all players on the same date (no network required).
    /// </summary>
    public static class DailyGoalService
    {
        // ── Goal pool (21 goals, 3 tiers) ────────────────────────────────────

        public enum GoalTier { Easy, Medium, Hard }

        public sealed class GoalDefinition
        {
            public string Id;
            public GoalTier Tier;
            public string Description;
            public string RewardDescription;
        }

        // [REQ: DAILY-WALK-004] The pool for a walk of two floors (owner 2026-10-26: the old 21 with four replaced, so the
        // Discord posts keep their 7/7/7 indices). tools/discord/walk_calendar.py reads these lines.
        private static readonly GoalDefinition[] AllGoals = new GoalDefinition[]
        {
            // ── Tier 1 (Easy) ────────────────────────────────
            new GoalDefinition { Id = "t1_any_puzzle", Tier = GoalTier.Easy, Description = "Complete any puzzle", RewardDescription = "+50 XP" },
            new GoalDefinition { Id = "t1_visit_rest", Tier = GoalTier.Easy, Description = "Visit a Rest node", RewardDescription = "+50 XP" },
            new GoalDefinition { Id = "t1_spend_gold", Tier = GoalTier.Easy, Description = "Spend 50 gold in a single run", RewardDescription = "+50 XP" },
            new GoalDefinition { Id = "t1_collect_item", Tier = GoalTier.Easy, Description = "Collect any item", RewardDescription = "+50 XP" },
            new GoalDefinition { Id = "t1_use_pencil", Tier = GoalTier.Easy, Description = "Use pencil marks in a puzzle", RewardDescription = "+50 XP" },
            new GoalDefinition { Id = "t1_visit_shop", Tier = GoalTier.Easy, Description = "Visit the Coffee & Ice Cream cart", RewardDescription = "+50 XP" },
            new GoalDefinition { Id = "t1_choose_risk", Tier = GoalTier.Easy, Description = "Take the Risk route on any floor", RewardDescription = "+50 XP" },

            // ── Tier 2 (Medium) ──────────────────────────────
            new GoalDefinition { Id = "t2_no_mistake", Tier = GoalTier.Medium, Description = "Complete a puzzle with no mistakes", RewardDescription = "+100 XP" },
            new GoalDefinition { Id = "t2_defeat_boss", Tier = GoalTier.Medium, Description = "Defeat any boss", RewardDescription = "+100 XP" },
            new GoalDefinition { Id = "t2_collect_150g", Tier = GoalTier.Medium, Description = "Collect 150 gold in a single run", RewardDescription = "+100 XP" },
            new GoalDefinition { Id = "t2_use_2_items", Tier = GoalTier.Medium, Description = "Use 2 items in a single run", RewardDescription = "+100 XP" },
            new GoalDefinition { Id = "t2_floor2", Tier = GoalTier.Medium, Description = "Reach Floor 2", RewardDescription = "+100 XP" },
            new GoalDefinition { Id = "t2_accept_cursed", Tier = GoalTier.Medium, Description = "Accept a Cursed node", RewardDescription = "+100 XP" },
            new GoalDefinition { Id = "t2_collect_relic", Tier = GoalTier.Medium, Description = "Collect a relic", RewardDescription = "+100 XP" },

            // ── Tier 3 (Hard) ────────────────────────────────
            new GoalDefinition { Id = "t3_full_hp_floor", Tier = GoalTier.Hard, Description = "Complete any floor without losing HP", RewardDescription = "+200 XP" },
            new GoalDefinition { Id = "t3_no_pencil_puzzle", Tier = GoalTier.Hard, Description = "Complete a puzzle using zero pencil marks", RewardDescription = "+200 XP" },
            new GoalDefinition { Id = "t3_finish_hp3", Tier = GoalTier.Hard, Description = "Finish the walk with 3 HP or more", RewardDescription = "+200 XP" },
            new GoalDefinition { Id = "t3_finish_no_item", Tier = GoalTier.Hard, Description = "Finish the walk without using an item", RewardDescription = "+200 XP" },
            new GoalDefinition { Id = "t3_finish_walk", Tier = GoalTier.Hard, Description = "Finish the walk (defeat the Floor 2 boss)", RewardDescription = "+200 XP" },
            new GoalDefinition { Id = "t3_no_mistake_boss", Tier = GoalTier.Hard, Description = "Defeat a boss with no mistakes", RewardDescription = "+200 XP" },
            new GoalDefinition { Id = "t3_three_perfect", Tier = GoalTier.Hard, Description = "Solve three puzzles without a mistake in one walk", RewardDescription = "+200 XP" },
        };

        private static readonly int[] Tier1Indices = { 0, 1, 2, 3, 4, 5, 6 };
        private static readonly int[] Tier2Indices = { 7, 8, 9, 10, 11, 12, 13 };
        private static readonly int[] Tier3Indices = { 14, 15, 16, 17, 18, 19, 20 };

        // ── Public API ───────────────────────────────────────────────────────

        public static GoalDefinition[] GetAllGoals() => AllGoals;

        public static GoalDefinition GetGoal(int index) => AllGoals[index];

        public static string GetGoalDescription(GoalDefinition goal)
        {
            return goal == null
                ? string.Empty
                : LocalizationService.T($"Challenge.Daily.Goal.{goal.Id}.Description", goal.Description);
        }

        public static string GetRewardDescription(GoalDefinition goal)
        {
            return goal == null ? string.Empty : LocalizationService.Format("Challenge.Daily.GoalXp", "+{0} XP", GoalXp(goal.Tier));
        }

        public static IEnumerable<string> GetLocalizationKeys()
        {
            yield return "Challenge.Daily.GoalXp";
            for (var i = 0; i < AllGoals.Length; i++)
                yield return $"Challenge.Daily.Goal.{AllGoals[i].Id}.Description";
        }

        /// <summary>Returns today's 3 goal definitions from the state.</summary>
        public static GoalDefinition[] GetTodayGoals(DailyGoalState state)
        {
            return new[]
            {
                AllGoals[state.TodayGoalIds[0]],
                AllGoals[state.TodayGoalIds[1]],
                AllGoals[state.TodayGoalIds[2]]
            };
        }

        /// <summary>
        /// Called on game start / main menu open.
        /// Rolls new goals if the date has changed; evaluates streak on rollover.
        /// </summary>
        public static void RefreshIfNewDay(DailyGoalState state, DateTime today)
        {
            var todayStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (state.LastGoalDate == todayStr) return;

            // Evaluate yesterday's streak before rolling new goals
            if (!string.IsNullOrEmpty(state.LastGoalDate))
            {
                var allDone = state.TodayGoalCompleted[0]
                           && state.TodayGoalCompleted[1]
                           && state.TodayGoalCompleted[2];

                if (allDone)
                {
                    state.CurrentStreak++;
                    state.LastStreakDate = state.LastGoalDate;
                    GrantStreakMilestone(state, today);
                }
                else
                {
                    state.CurrentStreak = 0;
                }

                // [REQ: DAILY-GOAL-003] A day without play completes no goals, so it breaks the streak (GAP-028).
                if (DateTime.TryParseExact(state.LastGoalDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var lastDay)
                    && lastDay.Date < today.Date.AddDays(-1))
                    state.CurrentStreak = 0;
            }

            // Generate new goals from date seed
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var rng = new Random(seed);

            var t1 = Tier1Indices[rng.Next(Tier1Indices.Length)];
            var t2 = PickDistinct(rng, Tier2Indices, -1);
            // Goal 3: Tier2 or Tier3 (50/50 by day seed)
            var t3 = (seed % 2 == 0)
                ? PickDistinct(rng, Tier2Indices, t2)
                : PickDistinct(rng, Tier3Indices, -1);

            state.TodayGoalIds[0] = t1;
            state.TodayGoalIds[1] = t2;
            state.TodayGoalIds[2] = t3;

            state.TodayGoalCompleted[0] = false;
            state.TodayGoalCompleted[1] = false;
            state.TodayGoalCompleted[2] = false;

            state.LastGoalDate = todayStr;
        }

        /// <summary>
        /// Check goals against a completed puzzle or run event.
        /// Call after: puzzle complete, node arrive, gold change, boss defeat, item use, etc.
        /// </summary>
        public static void EvaluateInRun(DailyGoalState state, RunState run, LevelState level,
            string trigger, int floorIndex = 0)
        {
            var goals = GetTodayGoals(state);
            for (var i = 0; i < 3; i++)
            {
                if (state.TodayGoalCompleted[i]) continue;
                if (CheckGoal(goals[i].Id, trigger, run, level, floorIndex))
                    CompleteGoal(state, i, run);
            }
        }

        /// <summary>Marks goal[index] complete; [REQ: DAILY-WALK-006] all three bring an ink stamp.</summary>
        public static void CompleteGoal(DailyGoalState state, int index, RunState run)
        {
            if (state.TodayGoalCompleted[index]) return;
            state.TodayGoalCompleted[index] = true;
            if (state.TodayGoalCompleted[0] && state.TodayGoalCompleted[1] && state.TodayGoalCompleted[2])
                EarnStamps(state, 1, DateTime.Today);
        }

        // ── Ink stamps and the weekly scroll ────────────────────────────────

        public const int WeeklyScrollStamps = 7, WeeklyScrollXp = 500;

        /// <summary>[REQ: DAILY-WALK-006] Ink stamps earned (the stamp shop spends them). The seventh earned in one ISO week
        /// brings the weekly scroll: 500 XP that wait, like the walk's, for a class (owner 2026-10-26).</summary>
        public static void EarnStamps(DailyGoalState state, int count, DateTime day)
        {
            if (count <= 0) return;
            state.TotalInkStamps += count;
            var week = $"{ISOWeek.GetYear(day):0000}-W{ISOWeek.GetWeekOfYear(day):00}";
            if (state.StampWeek != week)
            {
                state.StampWeek = week;
                state.StampsThisWeek = 0;
            }
            var before = state.StampsThisWeek;
            state.StampsThisWeek += count;
            if (before < WeeklyScrollStamps && state.StampsThisWeek >= WeeklyScrollStamps)
                state.PendingWalkXp += WeeklyScrollXp;
        }

        // ── The day's walk ──────────────────────────────────────────────────

        public const int WalkFloors = 2;

        /// <summary>[REQ: DAILY-WALK-001] The day's seed, the same for every player (FNV-1a of the date, like the week's seed).</summary>
        public static int WalkSeed(DateTime day)
        {
            var h = 2166136261u;
            foreach (var b in System.Text.Encoding.ASCII.GetBytes("walk:" + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            {
                h ^= b;
                h *= 16777619u;
            }
            return 100000 + (int)(h % 900000u);
        }

        /// <summary>[REQ: DAILY-WALK-005] True for the day's first walk, which is then marked: later walks pay nothing.</summary>
        public static bool TryStartScoredWalk(DailyGoalState state, DateTime day)
        {
            var key = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (state.LastScoredWalkDate == key) return false;
            state.LastScoredWalkDate = key;
            return true;
        }

        /// <summary>The tiers of the goals met today.</summary>
        public static List<GoalTier> GoalsMet(DailyGoalState state)
        {
            var met = new List<GoalTier>();
            var goals = GetTodayGoals(state);
            for (var i = 0; i < 3; i++)
                if (state.TodayGoalCompleted[i]) met.Add(goals[i].Tier);
            return met;
        }

        /// <summary>[REQ: DAILY-WALK-003] The streak day today's walk counts as (the days before it, then today).</summary>
        public static int StreakDay(DailyGoalState state) => state.CurrentStreak + 1;

        /// <summary>[REQ: DAILY-WALK-003] The day's scored walk is over: its XP joins the XP waiting for a class.</summary>
        public static int SettleWalk(DailyGoalState state, int walkXp)
        {
            var xp = WalkXp(walkXp, GoalsMet(state), StreakDay(state));
            state.PendingWalkXp += xp;
            return xp;
        }

        // ── Daily Walk XP ───────────────────────────────────────────────────

        public const double DawnBonus = 1.5;

        public static int GoalXp(GoalTier tier) => tier switch { GoalTier.Easy => 50, GoalTier.Medium => 100, _ => 200 };

        public static int StreakBonusPercent(int streakDays) => streakDays >= 7 ? 25 : streakDays >= 3 ? 10 : 0;

        /// <summary>[REQ: DAILY-WALK-003] The XP a daily walk gives the class the player picks: the walk's XP ×1.5 (dawn
        /// bonus) plus the goals met, raised by the streak bonus.</summary>
        public static int WalkXp(int walkXp, IEnumerable<GoalTier> goalsMet, int streakDays)
        {
            var goals = 0;
            foreach (var tier in goalsMet) goals += GoalXp(tier);
            return (int)Math.Round((walkXp * DawnBonus + goals) * (100 + StreakBonusPercent(streakDays)) / 100.0,
                MidpointRounding.AwayFromZero);
        }

        // ── Trigger strings (used by RunDirector) ────────────────────────────

        public const string TriggerPuzzleComplete    = "puzzle_complete";
        public const string TriggerNoMistakePuzzle   = "no_mistake_puzzle";
        public const string TriggerBossDefeated       = "boss_defeated";
        public const string TriggerNoMistakeBoss      = "no_mistake_boss";
        public const string TriggerRestVisited        = "rest_visited";
        public const string TriggerShopVisited        = "shop_visited";
        public const string TriggerRiskRouteChosen    = "risk_route_chosen";
        public const string TriggerItemCollected      = "item_collected";
        public const string TriggerItemUsed           = "item_used";
        public const string TriggerPencilMarkUsed     = "pencil_mark_used";
        public const string TriggerSpentGold50        = "spent_gold_50";
        public const string TriggerCollected150Gold   = "collected_150_gold";
        public const string TriggerUsed2Items         = "used_2_items";
        public const string TriggerReachedFloor2      = "reached_floor_2";
        public const string TriggerAcceptedCursed     = "accepted_cursed";
        public const string TriggerRelicCollected     = "relic_collected";
        public const string TriggerFullHpFloor        = "full_hp_floor";
        public const string TriggerNoPencilPuzzle     = "no_pencil_puzzle";
        public const string TriggerRunWon             = "run_won";
        public const string TriggerFinishedHp3        = "finished_hp3";
        public const string TriggerFinishedNoItem     = "finished_no_item";
        public const string TriggerThreePerfect       = "three_perfect";

        // ── Internal ─────────────────────────────────────────────────────────

        private static bool CheckGoal(string goalId, string trigger, RunState run, LevelState level, int floorIndex)
        {
            switch (goalId)
            {
                case "t1_any_puzzle":    return trigger == TriggerPuzzleComplete;
                case "t1_visit_rest":    return trigger == TriggerRestVisited;
                case "t1_spend_gold":    return trigger == TriggerSpentGold50;
                case "t1_collect_item":  return trigger == TriggerItemCollected;
                case "t1_use_pencil":    return trigger == TriggerPencilMarkUsed;
                case "t1_visit_shop":    return trigger == TriggerShopVisited;
                case "t1_choose_risk":   return trigger == TriggerRiskRouteChosen;

                case "t2_no_mistake":    return trigger == TriggerNoMistakePuzzle;
                case "t2_defeat_boss":   return trigger == TriggerBossDefeated;
                case "t2_collect_150g":  return trigger == TriggerCollected150Gold;
                case "t2_use_2_items":   return trigger == TriggerUsed2Items;
                case "t2_floor2":        return trigger == TriggerReachedFloor2;
                case "t2_accept_cursed": return trigger == TriggerAcceptedCursed;
                case "t2_collect_relic": return trigger == TriggerRelicCollected;

                case "t3_full_hp_floor":    return trigger == TriggerFullHpFloor;
                case "t3_no_pencil_puzzle": return trigger == TriggerNoPencilPuzzle;
                case "t3_finish_hp3":       return trigger == TriggerFinishedHp3;
                case "t3_finish_no_item":   return trigger == TriggerFinishedNoItem;
                case "t3_finish_walk":      return trigger == TriggerRunWon;
                case "t3_no_mistake_boss":  return trigger == TriggerNoMistakeBoss;
                case "t3_three_perfect":    return trigger == TriggerThreePerfect;
                default: return false;
            }
        }

        private static int PickDistinct(Random rng, int[] pool, int exclude)
        {
            int pick;
            do { pick = pool[rng.Next(pool.Length)]; } while (pick == exclude);
            return pick;
        }

        private static void GrantStreakMilestone(DailyGoalState state, DateTime today)
        {
            // 7-day streak: +3 stamps (repeating every 7 days); 30 days: +10 once; 100 days: +25 once
            if (state.CurrentStreak % 7 == 0) EarnStamps(state, 3, today);
            if (state.CurrentStreak == 30) EarnStamps(state, 10, today);
            if (state.CurrentStreak == 100) EarnStamps(state, 25, today);
        }
    }
}
