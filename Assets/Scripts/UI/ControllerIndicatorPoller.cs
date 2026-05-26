using System;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class ControllerIndicatorPoller
    {
        private const float DefaultPollIntervalSeconds = 2f;

        private readonly float _pollIntervalSeconds;
        private float _timerSeconds;

        public ControllerIndicatorPoller(float pollIntervalSeconds = DefaultPollIntervalSeconds)
        {
            _pollIntervalSeconds = Math.Max(0.01f, pollIntervalSeconds);
        }

        public int PreviousJoystickCount { get; private set; } = -1;

        public bool Tick(float deltaTimeSeconds, out string label)
        {
            return Tick(deltaTimeSeconds, ReadJoystickCount, ControllerGlyphService.GetIndicatorLabel, out label);
        }

        public bool Tick(
            float deltaTimeSeconds,
            Func<int> joystickCountProvider,
            Func<string> labelProvider,
            out string label)
        {
            label = null;
            _timerSeconds -= Math.Max(0f, deltaTimeSeconds);
            if (_timerSeconds > 0f)
                return false;

            _timerSeconds = _pollIntervalSeconds;
            PreviousJoystickCount = Math.Max(0, joystickCountProvider?.Invoke() ?? 0);
            label = labelProvider?.Invoke() ?? string.Empty;
            return true;
        }

        public void ForceNextPoll()
        {
            _timerSeconds = 0f;
        }

        private static int ReadJoystickCount()
        {
            return Input.GetJoystickNames().Length;
        }
    }
}
