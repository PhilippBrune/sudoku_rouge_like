using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    // [REQ: DAILY-WALK-002] The run's intro card: a class, or the Daily Walk's walker (ClassId 0), who has no class: the
    // day's gate as its icon and its own lines.
    public sealed partial class InRunController
    {
        private static Sprite IntroIcon(ClassId id) => ClassCatalog.IsClass(id)
            ? Resources.Load<Sprite>("class/icon_" + ClassCatalog.GetIconName(id))
            : InRunUiFactory.LoadResourceSprite("ui/icon_start_run");

        private static string IntroNarrative(ClassId id) => ClassCatalog.IsClass(id)
            ? ParkNarrativeService.GetClassIntroFlavor(id)
            : LocalizationService.T("DailyWalk.Walker.Intro");

        private static string IntroPassive(ClassId id, ClassDefinition def) => ClassCatalog.IsClass(id)
            ? LocalizationService.T($"Class.Passive.{def.Id}", def.PassiveDescription)
            : LocalizationService.T("DailyWalk.Walker.Passive");
    }
}
