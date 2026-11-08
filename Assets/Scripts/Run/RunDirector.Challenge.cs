using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using UnityEngine;

namespace SudokuRoguelike.Run
{
    // [REQ: EVENT-CHALLENGE-001] The puzzle challenge (owner, 0.4.3 playtest): a timed puzzle without rules for an item or a relic. The
    // relic asks for one size more and one star more on a tighter clock. Out of time: back on the path, no reward, no further cost.
    public sealed partial class RunDirector
    {
        public const string ChallengeEventKey = "puzzle_challenge";
        public const int ChallengeItem = 1, ChallengeRelic = 2;

        private LevelConfig _pendingChallenge;

        /// <summary>The board and clock of a challenge on this floor: an item is the floor's smallest board.</summary>
        public static (int size, int stars, int seconds) ChallengeTerms(int kind, int floorMinSize, int floor)
        {
            var size = Math.Clamp(floorMinSize, 5, 9);
            var stars = floor <= 0 ? 2 : 3;
            var perCell = 2.5f;
            if (kind == ChallengeRelic)
            {
                size = Math.Min(9, size + 1);
                stars++;
                perCell = 2f;
            }
            return (size, stars, Mathf.RoundToInt(size * size * (perCell + stars)));
        }

        private (int size, int stars, int seconds) ChallengeTermsNow(int kind) =>
            ChallengeTerms(kind, BoardSizesFor(State.CurrentFloor, State.ThreeFloors).min, State.CurrentFloor);

        /// <summary>The event's two challenge options name this floor's board and clock.</summary>
        private void FillChallengeTerms(RunEvent runEvent)
        {
            if (runEvent?.Key != ChallengeEventKey) return;
            for (var i = 0; i < runEvent.Options.Count; i++)
            {
                var option = runEvent.Options[i];
                if (option.Challenge == 0) continue;
                var (size, stars, seconds) = ChallengeTermsNow(option.Challenge);
                option.EffectDescription = LocalizationService.Format(
                    option.Challenge == ChallengeRelic ? "RunEvent.puzzle_challenge.TermsRelic" : "RunEvent.puzzle_challenge.TermsItem",
                    "{0}×{0}, {1}★, {2}:{3:00}", size, stars, seconds / 60, seconds % 60);
            }
        }

        /// <summary>Called when a challenge option is taken: the puzzle waits for the screen to start it.</summary>
        private void PrepareChallenge(int kind)
        {
            var (size, stars, seconds) = ChallengeTermsNow(kind);
            var config = BuildLevelConfig(false, false, State.CurrentNodeIndex);
            config.BoardSize = size;
            config.Stars = stars;
            config.MissingPercent = StarDensityService.MissingPercentForStars(stars);
            config.ActiveModifiers.Clear();
            config.IsElite = config.IsPreBoss = config.IsBoss = false;
            _pendingChallenge = config;
            State.ChallengeKind = kind;
            State.ChallengeSecondsLeft = seconds;
        }

        public LevelConfig TakePendingChallenge()
        {
            var config = _pendingChallenge;
            _pendingChallenge = null;
            return config;
        }

        /// <summary>Ends the challenge puzzle: the prize when it was solved, nothing when the time ran out. Returns the line for the path.</summary>
        public string FinishChallenge(bool solved)
        {
            var kind = State.ChallengeKind;
            State.ChallengeKind = 0;
            State.ChallengeSecondsLeft = 0f;
            CurrentBoard = null;
            CurrentLevelState = null;
            CurrentOverlay = null;
            CurrentLevelConfig = null;
            if (!solved) return LocalizationService.T("RunEvent.puzzle_challenge.TimeUp");
            if (kind == ChallengeRelic)
            {
                var relic = TakeRandomRelic();
                return relic == null ? string.Empty : LocalizationService.Format("RunEvent.Result.RelicGain", "+{0} (relic)", RelicService.GetRelicName(relic.Id));
            }
            var prize = new RunEvent();
            prize.Options.Add(new EventOption { GrantRandomItem = true });
            return _eventService.ResolveChoice(State, prize, 0);
        }
    }
}
