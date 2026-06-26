using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Meta
{
    public sealed class CompletionService
    {
        public void RecordPuzzleCompletion(CompletionTrackerState state, int boardSize, int stars)
        {
            if (state == null) return;
            var key = $"{boardSize}x{boardSize}_s{stars}";
            if (!state.ClearedSizeStarCombos.Contains(key))
                state.ClearedSizeStarCombos.Add(key);
        }

        public void RecordModifierCleared(CompletionTrackerState state, BossModifierId modifier)
        {
            if (state == null) return;
            if (!state.ClearedModifiers.Contains(modifier))
                state.ClearedModifiers.Add(modifier);
        }

        public void RecordClassLevel30(CompletionTrackerState state, ClassId classId, int level)
        {
            if (state == null || level < 30) return;
            if (!state.ClassesAtLevel30.Contains(classId))
                state.ClassesAtLevel30.Add(classId);
        }

        // [REQ: META-COMPLETE-001] GetCompletionPercent: 4 categories each worth 25% — size×star combos, all modifiers, classes at L30, all relics discovered
        public float GetCompletionPercent(CompletionTrackerState state)
        {
            if (state == null) return 0f;

            // 4 categories, each worth 25%
            const float categoryWeight = 0.25f;

            // Category A: 30 size×star combos (5 sizes × 6 stars)
            var comboPercent = Mathf.Clamp01(state.ClearedSizeStarCombos.Count / 30f);

            // Category B: all 15 modifiers cleared
            var modPercent = Mathf.Clamp01(state.ClearedModifiers.Count / 15f);

            // Category C: all 8 classes at L30+
            var classPercent = Mathf.Clamp01(state.ClassesAtLevel30.Count / 8f);

            // Category D: all relics discovered
            var relicPercent = state.AllRelicsDiscovered ? 1f : 0f;

            return (comboPercent + modPercent + classPercent + relicPercent) * categoryWeight;
        }
    }
}
