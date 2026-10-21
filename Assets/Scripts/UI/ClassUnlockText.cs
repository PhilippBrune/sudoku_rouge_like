using System.Text;
using System.Text.RegularExpressions;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: META-XP-006] A level-schedule entry of ClassCatalog ("+2 Pencil, +2 HP, Start: Rare Solver") in the player's
    /// language. Stat bonuses are translated by kind ("+{0} HP"); every other part is its own catalog key (its English text).
    /// The class screen and the class garden show the schedule this way.
    /// </summary>
    public static class ClassUnlockText
    {
        // The stat kinds RunArchetypeService.GetLevelBonuses applies.
        private static readonly Regex Stat = new Regex(@"^\+(\d+)\s+(HP|Pencil|Starting Gold|Item Slot|Reroll Token|Starting Relic)$",
            RegexOptions.Compiled);

        /// <summary>[REQ: META-UNLOCK-006] A locked class's goal with its progress ("Collect 10 relics · 9/10"): the garden of
        /// classes and Select Class show it.</summary>
        public static string Goal(ClassId id, ClassUnlockService.UnlockProgress p) => id switch
        {
            ClassId.GardenMonk or ClassId.StoneGardener => LocalizationService.Format("MetaProgression.Goal.Bosses",
                "Defeat {1} bosses · {0}/{1}", p.Current, p.Target),
            ClassId.ShrineArchivist => LocalizationService.Format("MetaProgression.Goal.ItemsUsed", "Use {1} items · {0}/{1}",
                p.Current, p.Target),
            ClassId.KoiGambler => LocalizationService.Format("MetaProgression.Goal.Relics", "Collect {1} relics · {0}/{1}",
                p.Current, p.Target),
            ClassId.LanternSeer => LocalizationService.Format("MetaProgression.Goal.Gold", "Collect {1:N0} gold · {0:N0}",
                p.Current, p.Target),
            ClassId.ReedDuelist => LocalizationService.Format("MetaProgression.Goal.Codex", "Find every item · {0}/{1}",
                p.Current, p.Target),
            _ => LocalizationService.T("MetaProgression.Goal.PerfectRun", "A run with no mistakes")
        };

        public static string Localize(string unlock)
        {
            if (string.IsNullOrEmpty(unlock)) return unlock;
            var parts = unlock.Split(new[] { ", " }, System.StringSplitOptions.None);
            var sb = new StringBuilder();
            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(LocalizePart(parts[i]));
            }
            return sb.ToString();
        }

        private static string LocalizePart(string part)
        {
            var match = Stat.Match(part);
            if (!match.Success) return LocalizationService.T(part, part);
            var key = "Class.Reward." + match.Groups[2].Value.Replace(" ", "");
            return LocalizationService.Format(key, "+{0} " + match.Groups[2].Value, match.Groups[1].Value);
        }
    }
}
