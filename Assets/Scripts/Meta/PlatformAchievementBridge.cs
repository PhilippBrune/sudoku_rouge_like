using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SudokuRoguelike.Meta
{
    public interface IPlatformAchievementBridge
    {
        string ProviderName { get; }
        bool IsAvailable { get; }
        bool TryUnlockAchievement(string achievementId, out string error);
    }

    public sealed class SteamworksReflectionAchievementBridge : IPlatformAchievementBridge
    {
        private readonly MethodInfo _setAchievement;
        private readonly MethodInfo _storeStats;
        private readonly PropertyInfo _initializedProperty;

        public SteamworksReflectionAchievementBridge()
            : this(AppDomain.CurrentDomain.GetAssemblies())
        {
        }

        internal SteamworksReflectionAchievementBridge(Assembly[] assemblies)
        {
            var steamUserStats = FindType(assemblies, "Steamworks.SteamUserStats");
            _setAchievement = FindStaticMethod(steamUserStats, "SetAchievement", typeof(string));
            _storeStats = FindStaticMethod(steamUserStats, "StoreStats");

            var steamManager = FindType(assemblies, "SteamManager");
            _initializedProperty = steamManager?.GetProperty(
                "Initialized",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }

        public string ProviderName => "Steamworks";

        public bool IsAvailable
        {
            get
            {
                if (_setAchievement == null || _storeStats == null)
                    return false;

                if (_initializedProperty == null)
                    return true;

                try
                {
                    return _initializedProperty.PropertyType == typeof(bool)
                        && (bool)_initializedProperty.GetValue(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Achievement] SteamManager.Initialized check failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool TryUnlockAchievement(string achievementId, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                error = "Achievement ID is empty.";
                return false;
            }

            if (!IsAvailable)
            {
                error = "Steamworks bridge is not available.";
                return false;
            }

            try
            {
                if (!InvokeBoolMethod(_setAchievement, new object[] { achievementId }, out error))
                    return false;

                return InvokeBoolMethod(_storeStats, null, out error);
            }
            catch (TargetInvocationException ex)
            {
                error = ex.InnerException?.Message ?? ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool InvokeBoolMethod(MethodInfo method, object[] args, out string error)
        {
            error = null;
            var result = method.Invoke(null, args);
            if (method.ReturnType == typeof(bool) && result is bool success && !success)
            {
                error = $"{method.Name} returned false.";
                return false;
            }

            return true;
        }

        private static Type FindType(Assembly[] assemblies, string fullName)
        {
            return assemblies
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type => type.FullName == fullName || type.Name == fullName);
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).ToArray();
            }
        }

        private static MethodInfo FindStaticMethod(Type type, string name, params Type[] parameterTypes)
        {
            if (type == null)
                return null;

            return type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                parameterTypes,
                null);
        }
    }
}
