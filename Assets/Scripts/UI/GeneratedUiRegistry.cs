using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public enum GeneratedUiControlType
    {
        Button,
        Toggle,
        Slider,
        Dropdown,
        Panel,
        Text
    }

    public readonly struct GeneratedUiControlRequirement
    {
        public GeneratedUiControlRequirement(
            string screenId,
            string controlName,
            GeneratedUiControlType controlType,
            string actionId)
        {
            ScreenId = screenId ?? string.Empty;
            ControlName = controlName ?? string.Empty;
            ControlType = controlType;
            ActionId = actionId ?? string.Empty;
        }

        public string ScreenId { get; }
        public string ControlName { get; }
        public GeneratedUiControlType ControlType { get; }
        public string ActionId { get; }

        public static GeneratedUiControlRequirement Button(string screenId, string controlName, string actionId)
        {
            return new GeneratedUiControlRequirement(screenId, controlName, GeneratedUiControlType.Button, actionId);
        }

        public static GeneratedUiControlRequirement Toggle(string screenId, string controlName, string actionId)
        {
            return new GeneratedUiControlRequirement(screenId, controlName, GeneratedUiControlType.Toggle, actionId);
        }

        public static GeneratedUiControlRequirement Slider(string screenId, string controlName, string actionId)
        {
            return new GeneratedUiControlRequirement(screenId, controlName, GeneratedUiControlType.Slider, actionId);
        }

        public static GeneratedUiControlRequirement Dropdown(string screenId, string controlName, string actionId)
        {
            return new GeneratedUiControlRequirement(screenId, controlName, GeneratedUiControlType.Dropdown, actionId);
        }

        public string Key => GeneratedUiRegistry.BuildKey(ScreenId, ControlName);
    }

    public sealed class GeneratedUiControlRecord
    {
        public GeneratedUiControlRecord(
            string screenId,
            string controlName,
            GeneratedUiControlType controlType,
            string actionId,
            UnityEngine.Object target,
            string hierarchyPath,
            bool hasRegisteredAction)
        {
            ScreenId = screenId ?? string.Empty;
            ControlName = controlName ?? string.Empty;
            ControlType = controlType;
            ActionId = actionId ?? string.Empty;
            Target = target;
            HierarchyPath = hierarchyPath ?? string.Empty;
            HasRegisteredAction = hasRegisteredAction;
        }

        public string ScreenId { get; }
        public string ControlName { get; }
        public GeneratedUiControlType ControlType { get; }
        public string ActionId { get; }
        public UnityEngine.Object Target { get; }
        public string HierarchyPath { get; }
        public bool HasRegisteredAction { get; }
        public string Key => GeneratedUiRegistry.BuildKey(ScreenId, ControlName);
    }

    public sealed class GeneratedUiValidationResult
    {
        public GeneratedUiValidationResult(
            IReadOnlyList<string> errors,
            IReadOnlyList<string> warnings,
            IReadOnlyList<GeneratedUiControlRecord> records)
        {
            Errors = errors ?? Array.Empty<string>();
            Warnings = warnings ?? Array.Empty<string>();
            Records = records ?? Array.Empty<GeneratedUiControlRecord>();
        }

        public static GeneratedUiValidationResult Empty { get; } =
            new GeneratedUiValidationResult(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<GeneratedUiControlRecord>());

        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<GeneratedUiControlRecord> Records { get; }
        public bool IsValid => Errors.Count == 0;

        public string ToLogString()
        {
            if (IsValid && Warnings.Count == 0)
                return "Generated UI validation passed.";

            var sb = new StringBuilder();
            foreach (var error in Errors)
                sb.AppendLine("ERROR: " + error);
            foreach (var warning in Warnings)
                sb.AppendLine("WARN: " + warning);
            return sb.ToString().TrimEnd();
        }
    }

    public sealed class GeneratedUiRegistry
    {
        private readonly Dictionary<string, GeneratedUiControlRequirement> _requirements =
            new Dictionary<string, GeneratedUiControlRequirement>();

        private readonly Dictionary<string, GeneratedUiControlRecord> _records =
            new Dictionary<string, GeneratedUiControlRecord>();

        private readonly List<string> _duplicateRegistrations = new List<string>();

        public int RebuildVersion { get; private set; }

        public IReadOnlyCollection<GeneratedUiControlRecord> Records => _records.Values.ToArray();

        public void BeginRebuild(IEnumerable<GeneratedUiControlRequirement> requirements)
        {
            RebuildVersion++;
            _records.Clear();
            _requirements.Clear();
            _duplicateRegistrations.Clear();

            if (requirements == null)
                return;

            foreach (var requirement in requirements)
            {
                if (string.IsNullOrWhiteSpace(requirement.ScreenId) ||
                    string.IsNullOrWhiteSpace(requirement.ControlName))
                {
                    continue;
                }

                _requirements[requirement.Key] = requirement;
            }
        }

        public void RegisterButton(string screenId, string controlName, Button button, string actionId)
        {
            RegisterControl(
                screenId,
                controlName,
                GeneratedUiControlType.Button,
                actionId,
                button,
                !string.IsNullOrWhiteSpace(actionId));
        }

        public void RegisterControl(
            string screenId,
            string controlName,
            GeneratedUiControlType controlType,
            string actionId,
            UnityEngine.Object target,
            bool hasRegisteredAction)
        {
            var key = BuildKey(screenId, controlName);
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (_records.ContainsKey(key))
                _duplicateRegistrations.Add(key);

            _records[key] = new GeneratedUiControlRecord(
                screenId,
                controlName,
                controlType,
                actionId,
                target,
                GetHierarchyPath(target),
                hasRegisteredAction);
        }

        public GeneratedUiValidationResult Validate()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            foreach (var duplicateKey in _duplicateRegistrations.Distinct().OrderBy(k => k))
                errors.Add($"Duplicate generated UI registration for {duplicateKey}.");

            foreach (var requirement in _requirements.Values)
            {
                if (!_records.TryGetValue(requirement.Key, out var record))
                {
                    errors.Add($"Missing required {requirement.ControlType} {requirement.ScreenId}/{requirement.ControlName}.");
                    continue;
                }

                if (record.ControlType != requirement.ControlType)
                {
                    errors.Add(
                        $"Control {record.ScreenId}/{record.ControlName} has type {record.ControlType}, expected {requirement.ControlType}.");
                }

                if (record.Target == null)
                {
                    errors.Add($"Control {record.ScreenId}/{record.ControlName} has no live Unity target.");
                }

                if (IsInteractiveControl(requirement.ControlType) &&
                    !string.IsNullOrWhiteSpace(requirement.ActionId) &&
                    !record.HasRegisteredAction)
                {
                    errors.Add($"{requirement.ControlType} {record.ScreenId}/{record.ControlName} is missing action {requirement.ActionId}.");
                }

                if (!string.Equals(record.ActionId, requirement.ActionId, StringComparison.Ordinal))
                {
                    warnings.Add(
                        $"Control {record.ScreenId}/{record.ControlName} registered action '{record.ActionId}', expected '{requirement.ActionId}'.");
                }
            }

            return new GeneratedUiValidationResult(
                errors,
                warnings,
                _records.Values.OrderBy(r => r.ScreenId).ThenBy(r => r.ControlName).ToArray());
        }

        public static string BuildKey(string screenId, string controlName)
        {
            if (string.IsNullOrWhiteSpace(screenId) || string.IsNullOrWhiteSpace(controlName))
                return string.Empty;

            return screenId.Trim() + "/" + controlName.Trim();
        }

        private static string GetHierarchyPath(UnityEngine.Object target)
        {
            if (target == null)
                return string.Empty;

            var component = target as Component;
            var transform = component != null ? component.transform : (target as GameObject)?.transform;
            if (transform == null)
                return target.name;

            var parts = new Stack<string>();
            while (transform != null)
            {
                parts.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", parts);
        }

        private static bool IsInteractiveControl(GeneratedUiControlType controlType)
        {
            return controlType == GeneratedUiControlType.Button
                || controlType == GeneratedUiControlType.Toggle
                || controlType == GeneratedUiControlType.Slider
                || controlType == GeneratedUiControlType.Dropdown;
        }
    }
}
