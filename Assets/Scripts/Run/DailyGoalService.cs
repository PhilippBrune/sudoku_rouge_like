using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Manages the Daily Walk — 3 deterministic date-seeded goals per day.
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
            public int GoldReward;
            public int RerollTokenReward;
            public int InkStampReward;
        }

        private static readonly GoalDefinition[] AllGoals = new GoalDefinition[]
        {
            // ── Tier 1 (Easy) ─────────────────────────────────────────────
            new GoalDefinition { Id = "t1_any_puzzle",   Tier = GoalTier.Easy,   Description = "Complete any puzzle",                    RewardDescription = "+20 Gold",        GoldReward = 20 },
            new GoalDefinition { Id = "t1_visit_rest",   Tier = GoalTier.Easy,   Description = "Visit a Rest node",                      RewardDescription = "+20 Gold",        GoldReward = 20 },
            new GoalDefinition { Id = "t1_spend_gold",   Tier = GoalTier.Easy,   Description = "Spend 50 gold in a single run",          RewardDescription = "+20 Gold",        GoldReward = 20 },
            new GoalDefinition { Id = "t1_collect_item", Tier = GoalTier.Easy,   Description = "Collect any item",                       RewardDescription = "+20 Gold",        GoldReward = 20 },
            new GoalDefinition { Id = "t1_use_pencil",   Tier = GoalTier.Easy,   Description = "Use pencil marks in a puzzle",           RewardDescription = "+20 Gold",        GoldReward = 20 },
            new GoalDefinition { Id = "t1_visit_shop",   Tier = GoalTier.Easy,   Description = "Visit the Coffee & Ice Cream cart",      RewardDescription = "+20 Gold",        GoldReward = 20 },
            new GoalDefinition { Id = "t1_choose_risk",  Tier = GoalTier.Easy,   Description = "Take the Risk route on any floor",       RewardDescription = "+20 Gold",        GoldReward = 20 },

            // ── Tier 2 (Medium) ───────────────────────────────────────────
            new GoalDefinition { Id = "t2_no_mistake",   Tier = GoalTier.Medium, Description = "Complete a puzzle with no mistakes",     RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },
            new GoalDefinition { Id = "t2_defeat_boss",  Tier = GoalTier.Medium, Description = "Defeat any boss",                        RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },
            new GoalDefinition { Id = "t2_collect_150g", Tier = GoalTier.Medium, Description = "Collect 150 gold in a single run",       RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },
            new GoalDefinition { Id = "t2_use_2_items",  Tier = GoalTier.Medium, Description = "Use 2 items in a single run",            RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },
            new GoalDefinition { Id = "t2_floor2",       Tier = GoalTier.Medium, Description = "Reach Floor 2",                          RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },
            new GoalDefinition { Id = "t2_accept_cursed",Tier = GoalTier.Medium, Description = "Accept a Cursed node",                   RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },
            new GoalDefinition { Id = "t2_collect_relic",Tier = GoalTier.Medium, Description = "Collect a relic",                        RewardDescription = "+1 Reroll Token", RerollTokenReward = 1 },

            // ── Tier 3 (Hard) ─────────────────────────────────────────────
            new GoalDefinition { Id = "t3_full_hp_floor",    Tier = GoalTier.Hard, Description = "Complete any floor without losing HP",              RewardDescription = "+1 Ink Stamp",   InkStampReward = 1 },
            new GoalDefinition { Id = "t3_no_pencil_puzzle", Tier = GoalTier.Hard, Description = "Complete a puzzle using zero pencil marks",          RewardDescription = "+1 Ink Stamp",   InkStampReward = 1 },
            new GoalDefinition { Id = "t3_boss_floor3",      Tier = GoalTier.Hard, Description = "Defeat the Floor 3 boss",                           RewardDescription = "+1 Ink Stamp",   InkStampReward = 1 },
            new GoalDefinition { Id = "t3_floor4",           Tier = GoalTier.Hard, Description = "Reach Floor 4",                                     RewardDescription = "+1 Ink Stamp",   InkStampReward = 1 },
            new GoalDefinition { Id = "t3_win_run",          Tier = GoalTier.Hard, Description = "Win a run (defeat the Floor 5 boss)",               RewardDescription = "+2 Ink Stamps",  InkStampReward = 2 },
            new GoalDefinition { Id = "t3_no_mistake_boss",  Tier = GoalTier.Hard, Description = "Defeat a boss with no mistakes",                    RewardDescription = "+2 Ink Stamps",  InkStampReward = 2 },
            new GoalDefinition { Id = "t3_new_class",        Tier = GoalTier.Hard, Description = "Play as a class you haven't used in 7 days",        RewardDescription = "+1 Ink Stamp",   InkStampReward = 1 },
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
            return goal == null
                ? string.Empty
                : LocalizationService.T($"Challenge.Daily.Goal.{goal.Id}.Reward", goal.RewardDescription);
        }

        public static IEnumerable<string> GetLocalizationKeys()
        {
            for (var i = 0; i < AllGoals.Length; i++)
            {
                yield return $"Challenge.Daily.Goal.{AllGoals[i].Id}.Description";
                yield return $"Challenge.Daily.Goal.{AllGoals[i].Id}.Reward";
            }
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
            var todayStr = today.ToString("yyyy-MM-dd");
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
                    GrantStreakMilestone(state);
                }
                else
                {
                    state.CurrentStreak = 0;
                }
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

        /// <summary>Marks goal[index] complete and grants its reward.</summary>
        public static void CompleteGoal(DailyGoalState state, int index, RunState run)
        {
            if (state.TodayGoalCompleted[index]) return;
            state.TodayGoalCompleted[index] = true;

            var goal = AllGoals[state.TodayGoalIds[index]];

            if (goal.InkStampReward > 0)
                state.TotalInkStamps += goal.InkStampReward;

            if (goal.GoldReward > 0)
            {
                if (run != null) run.CurrentGold += goal.GoldReward;
                else state.PendingStartGold += goal.GoldReward;
            }

            if (goal.RerollTokenReward > 0)
            {
                if (run != null) run.RerollTokens += goal.RerollTokenReward;
                else state.PendingStartRerollTokens += goal.RerollTokenReward;
            }
        }

        /// <summary>Apply pending run-start rewards and clear them.</summary>
        public static void ApplyPendingRewards(DailyGoalState state, RunState run)
        {
            if (state.PendingStartGold > 0)
            {
                run.CurrentGold += state.PendingStartGold;
                state.PendingStartGold = 0;
            }
            if (state.PendingStartRerollTokens > 0)
            {
                run.RerollTokens += state.PendingStartRerollTokens;
                state.PendingStartRerollTokens = 0;
            }
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
        public const string TriggerReachedFloor4      = "reached_floor_4";
        public const string TriggerAcceptedCursed     = "accepted_cursed";
        public const string TriggerRelicCollected     = "relic_collected";
        public const string TriggerFullHpFloor        = "full_hp_floor";
        public const string TriggerNoPencilPuzzle     = "no_pencil_puzzle";
        public const string TriggerBossFloor3         = "boss_floor3";
        public const string TriggerRunWon             = "run_won";
        public const string TriggerNewClassUsed       = "new_class_used";

        // ── Class-play tracking (for t3_new_class) ───────────────────────────

        /// <summary>
        /// Returns true when <paramref name="classId"/> has not been used in any run
        /// for at least <paramref name="days"/> days, or has never been played.
        /// </summary>
        public static bool HasBeenUnusedForDays(DailyGoalState state, ClassId classId, DateTime today, int days = 7)
        {
            var id = (int)classId;
            var idx = state.ClassLastPlayedIds.IndexOf(id);
            if (idx < 0) return true; // never played → counts as unused
            if (!DateTime.TryParse(state.ClassLastPlayedDates[idx], out var lastDate)) return true;
            return (today - lastDate).TotalDays >= days;
        }

        /// <summary>Record that <paramref name="classId"/> was played on <paramref name="today"/>.</summary>
        public static void RecordClassPlayed(DailyGoalState state, ClassId classId, DateTime today)
        {
            var id = (int)classId;
            var idx = state.ClassLastPlayedIds.IndexOf(id);
            var dateStr = today.ToString("yyyy-MM-dd");
            if (idx < 0)
            {
                state.ClassLastPlayedIds.Add(id);
                state.ClassLastPlayedDates.Add(dateStr);
            }
            else
            {
                state.ClassLastPlayedDates[idx] = dateStr;
            }
        }

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
                case "t3_boss_floor3":      return trigger == TriggerBossFloor3;
                case "t3_floor4":           return trigger == TriggerReachedFloor4;
                case "t3_win_run":          return trigger == TriggerRunWon;
                case "t3_no_mistake_boss":  return trigger == TriggerNoMistakeBoss;
                case "t3_new_class":        return trigger == TriggerNewClassUsed;
                default: return false;
            }
        }

        private static int PickDistinct(Random rng, int[] pool, int exclude)
        {
            int pick;
            do { pick = pool[rng.Next(pool.Length)]; } while (pick == exclude);
            return pick;
        }

        private static void GrantStreakMilestone(DailyGoalState state)
        {
            // 7-day streak: +3 stamps (repeating every 7 days)
            if (state.CurrentStreak % 7 == 0)
                state.TotalInkStamps += 3;
            // 30-day streak: +10 stamps (one-time at exactly 30)
            if (state.CurrentStreak == 30)
                state.TotalInkStamps += 10;
            // 100-day streak: +25 stamps
            if (state.CurrentStreak == 100)
                state.TotalInkStamps += 25;
        }
    }
}
