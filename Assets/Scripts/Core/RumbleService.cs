using System.Runtime.InteropServices;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    /// <summary>
    /// XInput gamepad rumble for Windows. Silently disabled if XInput is unavailable or on other platforms.
    /// Left motor = low-frequency (heavy thud). Right motor = high-frequency (sharp buzz).
    /// </summary>
    public static class RumbleService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_VIBRATION
        {
            public ushort wLeftMotorSpeed;   // 0..65535
            public ushort wRightMotorSpeed;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern int XInputSetState_14(int dwUserIndex, ref XINPUT_VIBRATION pVibration);

        private static bool _available = true;

        /// <summary>Set to false to disable all rumble (accessibility toggle).</summary>
        public static bool Enabled = true;

        private static void SetMotors(float left, float right, int controllerIndex = 0)
        {
            if (!_available || !Enabled) return;
            try
            {
                var v = new XINPUT_VIBRATION
                {
                    wLeftMotorSpeed  = (ushort)(Mathf.Clamp01(left)  * 65535),
                    wRightMotorSpeed = (ushort)(Mathf.Clamp01(right) * 65535)
                };
                XInputSetState_14(controllerIndex, ref v);
            }
            catch { _available = false; }
        }

        public static void Stop(int controllerIndex = 0)
            => SetMotors(0f, 0f, controllerIndex);

        /// <summary>Heavy left-motor thud on wrong digit (0.20 s).</summary>
        public static void PulseMistake(MonoBehaviour host, int controllerIndex = 0)
            => host.StartCoroutine(PulseCoroutine(0.85f, 0.40f, 0.20f, controllerIndex));

        /// <summary>Micro right-motor tick on correct digit (0.05 s) — subtle tactile click.</summary>
        public static void PulseCorrect(MonoBehaviour host, int controllerIndex = 0)
            => host.StartCoroutine(PulseCoroutine(0f, 0.12f, 0.05f, controllerIndex));

        /// <summary>Medium burst when a puzzle is solved (0.30 s).</summary>
        public static void PulsePuzzleSolved(MonoBehaviour host, int controllerIndex = 0)
            => host.StartCoroutine(PulseCoroutine(0.50f, 0.50f, 0.30f, controllerIndex));

        /// <summary>Crescendo swell for run victory — three-stage ramp up then fade.</summary>
        public static void PulseVictory(MonoBehaviour host, int controllerIndex = 0)
            => host.StartCoroutine(VictoryCoroutine(controllerIndex));

        private static System.Collections.IEnumerator PulseCoroutine(
            float left, float right, float duration, int idx)
        {
            SetMotors(left, right, idx);
            yield return new WaitForSecondsRealtime(duration);
            SetMotors(0f, 0f, idx);
        }

        private static System.Collections.IEnumerator VictoryCoroutine(int idx)
        {
            SetMotors(0.25f, 0.25f, idx);
            yield return new WaitForSecondsRealtime(0.25f);
            SetMotors(0.70f, 0.70f, idx);
            yield return new WaitForSecondsRealtime(0.35f);
            SetMotors(1.00f, 1.00f, idx);
            yield return new WaitForSecondsRealtime(0.40f);
            SetMotors(0.40f, 0.40f, idx);
            yield return new WaitForSecondsRealtime(0.55f);
            SetMotors(0f, 0f, idx);
        }
    }
}
