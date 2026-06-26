namespace SudokuRoguelike.UI
{
    public sealed class TimedVisualStateController
    {
        public bool HasExpired(float expiryRealtimeSeconds, float currentRealtimeSeconds)
        {
            return expiryRealtimeSeconds > 0f && currentRealtimeSeconds >= expiryRealtimeSeconds;
        }
    }
}
