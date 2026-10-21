using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: META-XP-008] The Level 40 reward: each class's frame ("Cosmetic: Golden Number frame", …), a band in the class's
    /// colour round its plaque in the garden of classes and its badge in a run. Unlocked once the class reaches Level 40,
    /// and kept after prestige (which resets the level). GAP-063: the frames were listed but never shown.
    /// </summary>
    public static class ClassFrames
    {
        public const int UnlockLevel = XpTable.MaxLevel;

        public static bool IsUnlocked(MetaProgressionState meta, ClassId id)
        {
            var progression = new ClassGardenProgressionService();
            return progression.GetLevel(meta, id) >= UnlockLevel || progression.GetPrestigeTier(meta, id) > 0;
        }

        // One colour per frame, after its name in the schedule.
        public static Color Colour(ClassId id) => id switch
        {
            ClassId.NumberFreak       => new Color(0.85f, 0.66f, 0.23f, 1f),   // Golden Number
            ClassId.GardenMonk        => new Color(0.91f, 0.60f, 0.69f, 1f),   // Blooming Monk
            ClassId.ShrineArchivist   => new Color(0.36f, 0.42f, 0.62f, 1f),   // Scroll Archive
            ClassId.KoiGambler        => new Color(0.92f, 0.47f, 0.18f, 1f),   // Golden Koi
            ClassId.StoneGardener     => new Color(0.43f, 0.55f, 0.31f, 1f),   // Mossy Stone
            ClassId.LanternSeer       => new Color(0.55f, 0.80f, 0.80f, 1f),   // Spirit Lantern
            ClassId.ReedDuelist       => new Color(0.66f, 0.71f, 0.31f, 1f),   // Cutting Reed
            _                         => new Color(0.17f, 0.24f, 0.32f, 1f)    // Ink Map
        };
    }
}
