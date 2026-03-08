using System;
using System.Collections.Generic;
using System.Linq;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tutorial
{
    public sealed class TutorialSetupValidation
    {
        public bool IsValid;
        public bool ShowDualModifierWarning;
        public string Message;
    }

    public static class TutorialModeService
    {
        private static readonly HashSet<BossModifierId> ArithmeticModifiers = new()
        {
            BossModifierId.KillerCages,
            BossModifierId.ArrowSums
        };

        public static IReadOnlyList<int> GetBoardSizes() => new[] { 5, 6, 7, 8, 9 };

        public static IReadOnlyList<int> GetStars() => new[] { 1, 2, 3, 4, 5 };

        public static bool IsModifierAvailable(BossModifierId modifier, int boardSize)
        {
            if (boardSize < 7 && (modifier == BossModifierId.GermanWhispers || modifier == BossModifierId.KillerCages))
            {
                return false;
            }

            return true;
        }

        public static TutorialSetupValidation ValidateSetup(TutorialSetupConfig setup)
        {
            if (setup == null)
            {
                return new TutorialSetupValidation { IsValid = false, Message = "Tutorial setup is missing." };
            }

            if (!GetBoardSizes().Contains(setup.BoardSize))
            {
                return new TutorialSetupValidation { IsValid = false, Message = "Board size must be between 5x5 and 9x9." };
            }

            if (!GetStars().Contains(setup.Stars))
            {
                return new TutorialSetupValidation { IsValid = false, Message = "Stars must be between 1 and 5." };
            }

            if (setup.SelectedModifiers.Count > 2)
            {
                return new TutorialSetupValidation { IsValid = false, Message = "Select up to 2 modifiers." };
            }

            for (var i = 0; i < setup.SelectedModifiers.Count; i++)
            {
                if (!IsModifierAvailable(setup.SelectedModifiers[i], setup.BoardSize))
                {
                    return new TutorialSetupValidation
                    {
                        IsValid = false,
                        Message = $"{setup.SelectedModifiers[i]} is disabled for boards smaller than 7x7."
                    };
                }
            }

            return new TutorialSetupValidation
            {
                IsValid = true,
                ShowDualModifierWarning = setup.SelectedModifiers.Count == 2,
                Message = setup.SelectedModifiers.Count == 2
                    ? "Dual modifiers increase cognitive load significantly."
                    : string.Empty
            };
        }

        public static LevelConfig BuildLevelConfig(TutorialSetupConfig setup)
        {
            var difficulty = setup.BoardSize switch
            {
                5 => DifficultyTier.Diff1,
                6 => DifficultyTier.Diff2,
                7 => DifficultyTier.Diff3,
                8 => DifficultyTier.Diff4,
                _ => DifficultyTier.Diff5
            };

            var config = new LevelConfig
            {
                Difficulty = difficulty,
                BoardSize = setup.BoardSize,
                Stars = setup.Stars,
                MissingPercent = StarDensityService.MissingPercentForStars(setup.Stars),
                IsBoss = setup.SelectedModifiers.Count > 0,
                RegionVariant = setup.RegionVariant
            };

            for (var i = 0; i < setup.SelectedModifiers.Count; i++)
            {
                config.ActiveModifiers.Add(setup.SelectedModifiers[i]);
            }

            return config;
        }

        public static string GetTutorialSessionLabel()
        {
            return "TUTORIAL MODE | No Progression Rewards";
        }

        public static string GetModifierDescription(BossModifierId modifier)
        {
            return modifier switch
            {
                BossModifierId.FogOfWar => "Some cells are hidden; nearby correct placements reveal fog.",
                BossModifierId.ArrowSums => "Digits along each arrow sum to the circled total.",
                BossModifierId.GermanWhispers => "Adjacent digits on green lines must differ by at least 5.",
                BossModifierId.DutchWhispers => "Adjacent digits on orange lines must differ by at least 4.",
                BossModifierId.ParityLines => "Digits along red lines alternate odd/even parity.",
                BossModifierId.RenbanLines => "Digits on pink lines form a consecutive set in any order.",
                BossModifierId.KillerCages => "Digits in each cage must sum to the displayed value without repeats.",
                BossModifierId.DifferenceKropki => "White dots connect consecutive digits.",
                BossModifierId.RatioKropki => "Black dots connect digits where one is double the other.",
                _ => "No modifier selected."
            };
        }

        public static bool UsesArithmetic(TutorialSetupConfig setup)
        {
            for (var i = 0; i < setup.SelectedModifiers.Count; i++)
            {
                if (ArithmeticModifiers.Contains(setup.SelectedModifiers[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static string BuildCompletionKey(TutorialSetupConfig setup)
        {
            var sorted = setup.SelectedModifiers
                .OrderBy(x => x.ToString())
                .Select(x => x.ToString())
                .ToArray();

            var modifierPart = sorted.Length == 0 ? "None" : string.Join("+", sorted);
            return $"{setup.BoardSize}|{setup.Stars}|{modifierPart}";
        }
    }
}
