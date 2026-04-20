using System.Text.RegularExpressions;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    public static class RunArchetypeService
    {
        // Matches stat-bonus fragments in ClassCatalog unlock strings, e.g.
        // "+2 HP", "+1 Pencil", "+10 Starting Gold", "+1 Item Slot", "+1 Reroll Token", "+1 Starting Relic"
        private static readonly Regex StatRx = new Regex(
            @"\+(\d+)\s+(HP|Pencil|Starting Gold|Item Slot|Reroll Token|Starting Relic)",
            RegexOptions.Compiled);

        public static RunState CreateRunState(ClassId classId, int seed,
            bool allowIrregular = true, int classLevel = 1,
            RelicService relicService = null)
        {
            var def = ClassCatalog.GetDefinition(classId);

            // Accumulate all stat bonuses from level milestones earned up to classLevel.
            int bonusHp = 0, bonusPencil = 0, bonusGold = 0, bonusSlots = 0, bonusRerolls = 0, bonusRelics = 0;
            var unlocks = ClassCatalog.GetAllUnlocks(classId);
            if (unlocks != null)
            {
                foreach (var (level, text) in unlocks)
                {
                    if (level > classLevel) break; // unlocks are sorted ascending by level
                    foreach (Match m in StatRx.Matches(text))
                    {
                        var n = int.Parse(m.Groups[1].Value);
                        switch (m.Groups[2].Value)
                        {
                            case "HP":              bonusHp      += n; break;
                            case "Pencil":          bonusPencil  += n; break;
                            case "Starting Gold":   bonusGold    += n; break;
                            case "Item Slot":       bonusSlots   += n; break;
                            case "Reroll Token":    bonusRerolls += n; break;
                            case "Starting Relic":  bonusRelics  += n; break;
                        }
                    }
                }
            }

            var state = new RunState
            {
                ClassId   = classId,
                ClassLevel = classLevel,
                Mode      = GameMode.GardenRun,
                Seed      = seed,
                RunNumber = 1,
                Depth     = 0,
                CurrentFloor = 0,
                TotalFloors  = 5,
                CurrentHP    = def.BaseHP      + bonusHp,
                MaxHP        = def.BaseHP      + bonusHp,
                CurrentPencil = def.BasePencil + bonusPencil,
                MaxPencil     = def.BasePencil + bonusPencil,
                CurrentGold  = bonusGold,
                ItemSlots    = def.BaseItemSlots + bonusSlots,
                RerollTokens = bonusRerolls,
                AllowIrregularPuzzles = allowIrregular
            };

            relicService?.AssignStartingRelics(state, def.BaseStartingRelics + bonusRelics);

            return state;
        }
    }
}
