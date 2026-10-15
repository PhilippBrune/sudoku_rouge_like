using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Playtesters and translators from the community, named in the credits. The list lives in
    /// Resources/credits_community.json, written by `tools/discord/discord_admin.py export-credits` from the Discord
    /// roles and reviewed by the owner before a build.
    /// </summary>
    // [REQ: CREDITS-COMMUNITY-001]
    [Serializable]
    public sealed class CommunityCredits
    {
        public string discord = "";
        public List<string> playtesters = new List<string>();
        public List<Translator> translators = new List<Translator>();

        [Serializable]
        public sealed class Translator
        {
            public string name = "";
            public string language = "";
        }

        public const string ResourcePath = "credits_community";

        public static CommunityCredits Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            return asset != null ? Parse(asset.text) : new CommunityCredits();
        }

        public static CommunityCredits Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new CommunityCredits();
            var parsed = JsonUtility.FromJson<CommunityCredits>(json) ?? new CommunityCredits();
            parsed.playtesters = (parsed.playtesters ?? new List<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            parsed.translators = (parsed.translators ?? new List<Translator>()).Where(t => t != null && !string.IsNullOrWhiteSpace(t.name)).ToList();
            return parsed;
        }

        public bool HasNames => playtesters.Count > 0 || translators.Count > 0;

        public string PlaytesterLine() => string.Join(", ", playtesters.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

        /// <summary>"Ana (pt-BR), Jean (fr)": a translator without a language is listed by name only.</summary>
        public string TranslatorLine() => string.Join(", ", translators
            .OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase)
            .Select(t => string.IsNullOrWhiteSpace(t.language) ? t.name : $"{t.name} ({t.language})"));
    }
}
