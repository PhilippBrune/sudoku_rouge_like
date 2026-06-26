using System.Text;
using UnityEngine;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.UI
{
    public readonly struct MainMenuClassInfoView
    {
        public readonly string Text;
        public readonly Color Color;

        public MainMenuClassInfoView(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    public static class MainMenuClassInfoPresenter
    {
        public static MainMenuClassInfoView Build(ClassId classId, MetaProgressionState meta, bool isUnlocked)
        {
            var def = ClassCatalog.GetDefinition(classId);
            var allUnlocks = ClassCatalog.GetAllUnlocks(classId);

            return isUnlocked
                ? BuildUnlocked(classId, def, allUnlocks, meta)
                : BuildLocked(classId, def, allUnlocks, meta);
        }

        public static string GetClassPassive(ClassDefinition def)
        {
            return LocalizationService.T($"Class.Passive.{def.Id}", def.PassiveDescription);
        }

        public static string GetUnlockProgress(ClassId classId, MetaProgressionState meta)
        {
            var p = meta?.ClassUnlocks;
            if (p == null) return null;

            return classId switch
            {
                ClassId.GardenMonk => LocalizationService.Format("Class.UnlockProgress.Bosses2",
                    "Bosses defeated: {0}/2", p.BossesDefeated),
                ClassId.ShrineArchivist => LocalizationService.Format("Class.UnlockProgress.Items15",
                    "Items used: {0}/15", p.ItemsUsed),
                ClassId.KoiGambler => LocalizationService.Format("Class.UnlockProgress.Relics10",
                    "Relics collected: {0}/10", p.RelicsCollected),
                ClassId.StoneGardener => LocalizationService.Format("Class.UnlockProgress.Bosses10",
                    "Bosses defeated: {0}/10", p.BossesDefeated),
                ClassId.LanternSeer => LocalizationService.Format("Class.UnlockProgress.Gold50000",
                    "Gold collected: {0:N0}/50,000", p.GoldCollected),
                ClassId.ReedDuelist => p.ItemCodexComplete
                    ? LocalizationService.T("Class.UnlockProgress.ItemCodexComplete")
                    : LocalizationService.T("Class.UnlockProgress.ItemCodexIncomplete"),
                ClassId.QuietCartographer => p.PerfectFullRunAllFloors
                    ? LocalizationService.T("Class.UnlockProgress.PerfectRunComplete")
                    : LocalizationService.T("Class.UnlockProgress.PerfectRunIncomplete"),
                _ => null
            };
        }

        private static MainMenuClassInfoView BuildUnlocked(
            ClassId classId,
            ClassDefinition def,
            (int level, string unlock)[] allUnlocks,
            MetaProgressionState meta)
        {
            var totalXp = GetTotalXp(classId, meta);
            var level = XpTable.DeriveLevel(totalXp);
            var xpIntoLevel = totalXp - XpTable.CumulativeXpForLevel(level);
            var xpToNext = level < 40 ? XpTable.XpToNextLevel(level) : 0;

            var sb = new StringBuilder();
            sb.Append($"<b>{LocalizationService.T(def.Name)}</b>  {LocalizationService.Format("Class.LevelShort", "Lv{0}", level)}{(level >= 40 ? " " + LocalizationService.T("MAX") : "")}");
            sb.AppendLine(level < 40
                ? LocalizationService.Format("Class.XpLine", "   XP: {0}/{1}", xpIntoLevel, xpToNext)
                : LocalizationService.T("Class.XpMaxLine", "   XP: MAX"));
            AppendClassSummary(sb, def);
            sb.AppendLine(LocalizationService.T("Class.LevelUnlocksHeader", "- Level Unlocks -"));

            AppendLevelUnlocks(sb, allUnlocks, level);
            AppendExclusiveUnlocks(sb, classId, level, false);

            return new MainMenuClassInfoView(
                sb.ToString().TrimEnd(),
                new Color(0.96f, 0.93f, 0.82f, 1f));
        }

        private static MainMenuClassInfoView BuildLocked(
            ClassId classId,
            ClassDefinition def,
            (int level, string unlock)[] allUnlocks,
            MetaProgressionState meta)
        {
            var sb = new StringBuilder();
            sb.AppendLine(LocalizationService.Format("Class.LockedHeader",
                "<b>{0}  -  LOCKED</b>",
                LocalizationService.T(def.Name)));
            sb.Append(LocalizationService.Format("Class.UnlockConditionLine",
                "Unlock: {0}",
                GetClassUnlockCondition(def)));

            var progress = GetUnlockProgress(classId, meta);
            if (!string.IsNullOrEmpty(progress)) sb.AppendLine($"   ({progress})");
            else sb.AppendLine();

            AppendClassSummary(sb, def);
            sb.AppendLine(LocalizationService.T("Class.LevelUnlocksPreviewHeader", "- Level Unlocks (preview) -"));
            AppendLevelUnlocks(sb, allUnlocks, null);
            AppendExclusiveUnlocks(sb, classId, null, true);

            return new MainMenuClassInfoView(
                sb.ToString().TrimEnd(),
                new Color(0.80f, 0.55f, 0.35f, 1f));
        }

        private static int GetTotalXp(ClassId classId, MetaProgressionState meta)
        {
            var garden = meta?.GardenProgression;
            if (garden == null) return 0;

            for (var i = 0; i < garden.ClassEntries.Count; i++)
                if (garden.ClassEntries[i].ClassId == classId)
                    return garden.ClassEntries[i].TotalXp;

            return 0;
        }

        private static string GetClassUnlockCondition(ClassDefinition def)
        {
            return LocalizationService.T($"Class.UnlockCondition.{def.Id}", def.UnlockCondition);
        }

        private static void AppendClassSummary(StringBuilder sb, ClassDefinition def)
        {
            sb.AppendLine(LocalizationService.Format("Class.SummaryLine",
                "HP:{0}  Pencil:{1}  Slots:{2}  |  {3}",
                def.BaseHP,
                def.BasePencil,
                def.BaseItemSlots,
                GetClassPassive(def)));
        }

        private static void AppendLevelUnlocks(
            StringBuilder sb,
            (int level, string unlock)[] allUnlocks,
            int? currentLevel)
        {
            var itemsOnRow = 0;
            for (var ui = 0; ui < allUnlocks.Length; ui++)
            {
                var (unlockLevel, description) = allUnlocks[ui];
                var achieved = currentLevel.HasValue && unlockLevel <= currentLevel.Value;
                if (achieved)
                    sb.Append($"<color=#C8A44A>[L{unlockLevel}\u2713]</color> {description}");
                else
                    sb.Append($"<color=#888888>[L{unlockLevel}]</color> {description}");

                itemsOnRow++;
                if (itemsOnRow == 2 || ui == allUnlocks.Length - 1)
                {
                    sb.AppendLine();
                    itemsOnRow = 0;
                }
                else
                {
                    sb.Append("  ");
                }
            }
        }

        private static void AppendExclusiveUnlocks(
            StringBuilder sb,
            ClassId classId,
            int? currentLevel,
            bool previewOnly)
        {
            sb.AppendLine(LocalizationService.T("Class.ExclusiveUnlocksHeader", "- Exclusive Unlocks -"));

            var exItem = ItemService.GetExclusiveItemForClass(classId);
            var exRelic = RelicService.GetExclusiveRelicForClass(classId);

            if (exItem.HasValue)
            {
                sb.Append(!previewOnly && currentLevel.HasValue && currentLevel.Value >= 15
                    ? $"<color=#C8A44A>[L15\u2713] {ItemService.GetItemName(exItem.Value)}</color>"
                    : $"<color=#888888>{LocalizationService.T("Class.UnlocksAtLevel15Unknown", "[L15] Unlocks at Level 15: ???")}</color>");
                sb.Append("  ");
            }

            if (exRelic.HasValue)
            {
                sb.AppendLine(!previewOnly && currentLevel.HasValue && currentLevel.Value >= 30
                    ? $"<color=#C8A44A>[L30\u2713] {RelicService.GetRelicName(exRelic.Value)}</color>"
                    : $"<color=#888888>{LocalizationService.T("Class.UnlocksAtLevel30Unknown", "[L30] Unlocks at Level 30: ???")}</color>");
            }
        }
    }
}
