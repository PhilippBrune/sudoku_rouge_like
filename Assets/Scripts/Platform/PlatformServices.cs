using System.Collections.Generic;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.Platform
{
    public enum PlatformCapabilityState
    {
        Available,
        Unavailable,
        NotConfigured
    }

    public sealed class PlatformCapabilityReport
    {
        public PlatformCapabilityReport(
            string featureName,
            PlatformCapabilityState state,
            string providerName,
            string detail)
        {
            FeatureName = string.IsNullOrWhiteSpace(featureName) ? "Unknown" : featureName;
            State = state;
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? "None" : providerName;
            Detail = detail ?? string.Empty;
        }

        public string FeatureName { get; }
        public PlatformCapabilityState State { get; }
        public string ProviderName { get; }
        public string Detail { get; }
        public bool IsAvailable => State == PlatformCapabilityState.Available;
    }

    public sealed class PlatformReadinessStatus
    {
        private readonly List<string> _releaseBlockers;

        public PlatformReadinessStatus(
            PlatformCapabilityReport achievements,
            PlatformCapabilityReport cloudSave,
            PlatformCapabilityReport privacyNotice,
            IEnumerable<string> releaseBlockers)
        {
            Achievements = achievements;
            CloudSave = cloudSave;
            PrivacyNotice = privacyNotice;
            _releaseBlockers = releaseBlockers != null
                ? new List<string>(releaseBlockers)
                : new List<string>();
        }

        public PlatformCapabilityReport Achievements { get; }
        public PlatformCapabilityReport CloudSave { get; }
        public PlatformCapabilityReport PrivacyNotice { get; }
        public IReadOnlyList<string> ReleaseBlockers => _releaseBlockers;
        public bool HasReleaseBlockers => _releaseBlockers.Count > 0;
    }

    public interface IPlatformService
    {
        string ProviderName { get; }
        PlatformReadinessStatus GetReadinessStatus();
    }

    public interface ICloudSaveProvider
    {
        string ProviderName { get; }
        PlatformCapabilityReport GetCapabilityReport();
    }

    public sealed class LocalOnlyCloudSaveProvider : ICloudSaveProvider
    {
        public string ProviderName => "Local Profiles";

        public PlatformCapabilityReport GetCapabilityReport()
        {
            return new PlatformCapabilityReport(
                "Cloud Save",
                PlatformCapabilityState.NotConfigured,
                ProviderName,
                "The current save system uses local profile slots only. No cloud-save provider or conflict-resolution service is configured.");
        }
    }

    public sealed class DefaultPlatformService : IPlatformService
    {
        private readonly ICloudSaveProvider _cloudSaveProvider;

        public DefaultPlatformService(ICloudSaveProvider cloudSaveProvider = null)
        {
            _cloudSaveProvider = cloudSaveProvider ?? new LocalOnlyCloudSaveProvider();
        }

        public string ProviderName => SteamAchievementService.PlatformProviderName;

        public PlatformReadinessStatus GetReadinessStatus()
        {
            var achievements = SteamAchievementService.GetPlatformAchievementReport();
            var cloudSave = _cloudSaveProvider.GetCapabilityReport();
            var privacyNotice = new PlatformCapabilityReport(
                "Privacy/Analytics Notice",
                PlatformCapabilityState.Available,
                "None",
                "No platform telemetry package is configured. The game only uses local in-session run summaries for end-screen stats.");

            var blockers = new List<string>();
            AddBlocker(blockers, achievements, "Platform achievements are not release-verified.");
            AddBlocker(blockers, cloudSave, "Cloud save is not configured; local saves only.");

            return new PlatformReadinessStatus(achievements, cloudSave, privacyNotice, blockers);
        }

        private static void AddBlocker(
            ICollection<string> blockers,
            PlatformCapabilityReport report,
            string message)
        {
            if (report == null || report.State != PlatformCapabilityState.Available)
                blockers.Add(message);
        }
    }

    public static class PlatformServiceRegistry
    {
        private static IPlatformService _current = new DefaultPlatformService();

        public static IPlatformService Current => _current;

        public static PlatformReadinessStatus CurrentStatus => _current.GetReadinessStatus();

        public static void SetForTests(IPlatformService service)
        {
            _current = service ?? new DefaultPlatformService();
        }

        public static void ResetForTests()
        {
            _current = new DefaultPlatformService();
        }
    }
}
