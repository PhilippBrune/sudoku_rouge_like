using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tutorial
{
    public sealed class TutorialProgressService
    {
        public bool IsCompleted(TutorialProgressState state, string key)
        {
            return state?.CompletedKeys != null && state.CompletedKeys.Contains(key);
        }

        public void MarkCompleted(TutorialProgressState state, string key)
        {
            if (state == null) return;
            if (state.CompletedKeys == null) state.CompletedKeys = new List<string>();
            if (!state.CompletedKeys.Contains(key))
                state.CompletedKeys.Add(key);
        }

        public static readonly string[] TutorialKeys =
        {
            "basics_row_col",
            "basics_region",
            "pencil_marks",
            "items_intro",
            "shop_intro",
            "boss_modifier_intro",
            "relic_intro",
            "path_choice",
            "star_difficulty",
            "class_passive"
        };
    }
}
