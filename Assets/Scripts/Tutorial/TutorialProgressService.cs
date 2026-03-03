using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tutorial
{
    public sealed class TutorialProgressService
    {
        private readonly TutorialProgressState _state;

        public TutorialProgressService(TutorialProgressState state)
        {
            _state = state;
        }

        public void MarkCompleted(TutorialSetupConfig setup)
        {
            var key = TutorialModeService.BuildCompletionKey(setup);
            if (!_state.CompletedConfigurationKeys.Contains(key))
            {
                _state.CompletedConfigurationKeys.Add(key);
            }

            if (setup.SelectedModifiers.Count == 1)
            {
                var modifier = setup.SelectedModifiers[0];
                if (!_state.CompletedSingleModifiers.Contains(modifier))
                {
                    _state.CompletedSingleModifiers.Add(modifier);
                }
            }
        }

        public bool IsCompleted(TutorialSetupConfig setup)
        {
            var key = TutorialModeService.BuildCompletionKey(setup);
            return _state.CompletedConfigurationKeys.Contains(key);
        }

        public List<TutorialCellProgress> BuildBoardGridProgress()
        {
            var output = new List<TutorialCellProgress>();
            var sizes = TutorialModeService.GetBoardSizes();
            var stars = TutorialModeService.GetStars();

            for (var s = 0; s < sizes.Count; s++)
            {
                for (var i = 0; i < stars.Count; i++)
                {
                    var setup = new TutorialSetupConfig
                    {
                        BoardSize = sizes[s],
                        Stars = stars[i],
                        ResourceMode = TutorialResourceMode.Simulation
                    };

                    output.Add(new TutorialCellProgress
                    {
                        BoardSize = sizes[s],
                        Stars = stars[i],
                        Completed = IsCompleted(setup)
                    });
                }
            }

            return output;
        }

        public List<TutorialModifierProgress> BuildModifierProgress()
        {
            var output = new List<TutorialModifierProgress>();
            var modifiers = (BossModifierId[])System.Enum.GetValues(typeof(BossModifierId));

            for (var i = 0; i < modifiers.Length; i++)
            {
                output.Add(new TutorialModifierProgress
                {
                    Modifier = modifiers[i],
                    Completed = _state.CompletedSingleModifiers.Contains(modifiers[i])
                });
            }

            return output;
        }

        public float GetCompletionPercent()
        {
            var total = TutorialModeService.GetBoardSizes().Count * TutorialModeService.GetStars().Count;
            if (total <= 0)
            {
                return 0f;
            }

            var completed = 0;
            var grid = BuildBoardGridProgress();
            for (var i = 0; i < grid.Count; i++)
            {
                if (grid[i].Completed)
                {
                    completed++;
                }
            }

            return (float)completed / total;
        }
    }
}
