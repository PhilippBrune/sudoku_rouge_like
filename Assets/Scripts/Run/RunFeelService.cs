using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunFeelService
    {
        public RunFeelState State { get; } = new();

        public void OnCorrectPlacement(int currentHp, bool isBoss)
        {
            State.CurrentCorrectStreak++;
            if (State.CurrentCorrectStreak > State.PeakCorrectStreak)
            {
                State.PeakCorrectStreak = State.CurrentCorrectStreak;
            }

            if (isBoss)
            {
                State.CurrentMusicLayer = MusicLayer.BossPercussion;
            }
            else if (State.CurrentCorrectStreak >= 5)
            {
                State.CurrentMusicLayer = MusicLayer.Focus;
            }

            State.IsNearDeath = currentHp <= 2;
            if (State.IsNearDeath)
            {
                State.CurrentMusicLayer = MusicLayer.Tension;
            }
        }

        public void OnMistake(int currentHp)
        {
            State.MadeMistake = true;
            State.CurrentCorrectStreak = 0;
            State.IsNearDeath = currentHp <= 2;
            if (State.IsNearDeath)
            {
                State.CurrentMusicLayer = MusicLayer.Tension;
            }
        }

        public int GetComboGoldBonus()
        {
            return State.CurrentCorrectStreak switch
            {
                >= 20 => 6,
                >= 10 => 3,
                >= 5 => 1,
                _ => 0
            };
        }

        public void OnHpLoss()
        {
            State.LostHp = true;
        }

        public void MarkSolverItemUsed()
        {
            State.UsedSolverItem = true;
        }

        public bool TryApplyClearMindBonus(bool puzzleComplete, bool noMistakes, bool noHpLoss, bool noSolverItemUse)
        {
            if (!puzzleComplete || !noMistakes || !noHpLoss || !noSolverItemUse)
            {
                State.ClearMindAwarded = false;
                return false;
            }

            State.ClearMindAwarded = true;
            return true;
        }
    }
}
