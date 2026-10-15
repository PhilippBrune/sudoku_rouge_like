using System;
using System.Text;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Seed of the week: one Garden Run seed per ISO week (Monday to Sunday), the same for every player, because it is
    /// computed from the week alone (no server). tools/discord/walk_posts.py computes the same value for the Discord post.
    /// </summary>
    // [REQ: RUN-WEEK-001]
    public static class WeeklySeedService
    {
        /// <summary>ISO 8601 week-numbering year and week (weeks start on Monday; week 1 holds the year's first Thursday).</summary>
        public static (int Year, int Week) IsoWeek(DateTime date)
        {
            var d = date.Date;
            var dayFromMonday = ((int)d.DayOfWeek + 6) % 7;
            var thursday = d.AddDays(3 - dayFromMonday);
            return (thursday.Year, (thursday.DayOfYear - 1) / 7 + 1);
        }

        /// <summary>Monday of the week that contains <paramref name="date"/>.</summary>
        public static DateTime WeekStart(DateTime date) => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        /// <summary>A six-digit seed (100000–999999) from FNV-1a over "week:YYYY-Www".</summary>
        public static int BuildSeed(int isoYear, int isoWeek)
        {
            var key = $"week:{isoYear:D4}-W{isoWeek:D2}";
            var hash = 2166136261u;
            foreach (var b in Encoding.ASCII.GetBytes(key))
            {
                hash ^= b;
                hash *= 16777619u;
            }
            return 100000 + (int)(hash % 900000u);
        }

        public static int SeedFor(DateTime date)
        {
            var (year, week) = IsoWeek(date);
            return BuildSeed(year, week);
        }
    }
}
