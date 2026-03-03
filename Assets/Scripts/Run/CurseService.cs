using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class CurseService
    {
        public int GetCurseWeight(RunState runState)
        {
            if (runState == null)
            {
                return 0;
            }

            return runState.ActiveCurses.Count;
        }

        public float GetCurseHeatMultiplier(RunState runState)
        {
            var weight = GetCurseWeight(runState);
            return 1f + (weight * 0.07f);
        }

        public float GetRareEventBonusChance(RunState runState)
        {
            return Math.Clamp(GetCurseWeight(runState) * 0.01f, 0f, 0.10f);
        }

        public void ApplyCurse(RunState runState, CurseType curse)
        {
            if (runState == null)
            {
                return;
            }

            runState.ActiveCurses.Add(curse);

            if (curse == CurseType.LockedItemSlot)
            {
                runState.ItemSlots = Math.Max(1, runState.ItemSlots - 1);
            }
            else if (curse == CurseType.IncreasedMistakePenalty)
            {
                runState.RunNotes.Add("Mistakes deal +1 damage for next 2 puzzles.");
            }
            else if (curse == CurseType.TemporaryBlindness)
            {
                runState.RunNotes.Add("Temporary blindness active for next puzzle.");
            }
        }

        public bool TryPurifyRandomCurse(RunState runState, Random random)
        {
            if (runState == null || runState.ActiveCurses.Count == 0)
            {
                return false;
            }

            var index = random.Next(runState.ActiveCurses.Count);
            runState.ActiveCurses.RemoveAt(index);
            return true;
        }
    }
}
