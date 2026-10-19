using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: REQ-BOSS-104] Modifier legend tap-to-isolate (GAP-025, BossMechanicsSystem §Modifier Legend Panel):
    // overlay geometry is drawn into one CanvasGroup per modifier layer, so a legend entry can dim every other layer.
    // Fog of War is drawn on the cells, not in a layer, so it is never dimmed.
    public sealed partial class BoardViewController
    {
        public const float IsolatedOthersAlpha = 0.15f;
        public const float IsolationSeconds = 3f;

        private readonly Dictionary<string, CanvasGroup> _overlayGroups = new Dictionary<string, CanvasGroup>();
        private RectTransform _currentOverlayGroup;
        private Coroutine _isolationRestore;

        private RectTransform OverlayParent => _currentOverlayGroup != null ? _currentOverlayGroup : _gridOverlayRoot;

        /// <summary>The overlay layers a modifier draws. Global rules without geometry have none.</summary>
        public static IReadOnlyList<string> GetOverlayGroupKeys(BossModifierId modifier) => modifier switch
        {
            BossModifierId.GermanWhispers   => new[] { LineKey(LineType.GermanWhispers) },
            BossModifierId.DutchWhispers    => new[] { LineKey(LineType.DutchWhispers) },
            BossModifierId.ParityLines      => new[] { LineKey(LineType.ParityLine) },
            BossModifierId.RenbanLines      => new[] { LineKey(LineType.RenbanLine) },
            BossModifierId.Palindrome       => new[] { LineKey(LineType.Palindrome) },
            BossModifierId.Thermo           => new[] { LineKey(LineType.Thermo) },
            BossModifierId.BetweenLines     => new[] { LineKey(LineType.BetweenLine) },
            BossModifierId.ConsecutiveLine  => new[] { LineKey(LineType.ConsecutiveLine) },
            BossModifierId.SlowThermo       => new[] { LineKey(LineType.SlowThermo) },
            BossModifierId.UniqueSetLine    => new[] { LineKey(LineType.UniqueSetLine) },
            BossModifierId.DifferenceKropki => new[] { KropkiWhiteKey },
            BossModifierId.RatioKropki      => new[] { KropkiBlackKey },
            BossModifierId.FullKropki       => new[] { KropkiWhiteKey, KropkiBlackKey },
            BossModifierId.SumKropki        => new[] { KropkiSumKey },
            BossModifierId.GreaterLessThan  => new[] { GreaterLessKey },
            BossModifierId.XVPairs          => new[] { XvKey },
            BossModifierId.KillerCages      => new[] { CagesKey },
            BossModifierId.ArrowSums        => new[] { ArrowsKey },
            BossModifierId.EvenOdd          => new[] { EvenOddKey },
            BossModifierId.PrimeCells       => new[] { PrimeKey },
            BossModifierId.FortressCells    => new[] { FortressKey },
            BossModifierId.NonconsecLine      => new[] { LineKey(LineType.NonconsecLine) },
            BossModifierId.HighLowAlternating => new[] { LineKey(LineType.HighLowAlternating) },
            BossModifierId.NabnerLine         => new[] { LineKey(LineType.NabnerLine) },
            BossModifierId.EntropicLine       => new[] { LineKey(LineType.EntropicLine) },
            BossModifierId.ModularLine        => new[] { LineKey(LineType.ModularLine) },
            BossModifierId.ZipperLine         => new[] { LineKey(LineType.ZipperLine) },
            BossModifierId.FastThermo         => new[] { LineKey(LineType.FastThermo) },
            BossModifierId.AmbiguousThermo    => new[] { LineKey(LineType.AmbiguousThermo) },
            BossModifierId.FullWhiteKropki    => new[] { KropkiWhiteKey },
            BossModifierId.FullBlackKropki    => new[] { KropkiBlackKey },
            BossModifierId.DifferenceK        => new[] { DifferenceKKey },
            BossModifierId.MultipleCells      => new[] { MultipleKey },
            BossModifierId.MirrorCells        => new[] { MirrorKey },
            _                               => Array.Empty<string>()
        };

        /// <summary>The colour a modifier's overlay is drawn in, for the legend swatch. False for rules without geometry.</summary>
        public static bool TryGetLegendColor(BossModifierId modifier, out Color color)
        {
            color = modifier switch
            {
                BossModifierId.GermanWhispers   => GetLineColor(LineType.GermanWhispers),
                BossModifierId.DutchWhispers    => GetLineColor(LineType.DutchWhispers),
                BossModifierId.ParityLines      => GetLineColor(LineType.ParityLine),
                BossModifierId.RenbanLines      => GetLineColor(LineType.RenbanLine),
                BossModifierId.Palindrome       => GetLineColor(LineType.Palindrome),
                BossModifierId.Thermo           => GetLineColor(LineType.Thermo),
                BossModifierId.BetweenLines     => GetLineColor(LineType.BetweenLine),
                BossModifierId.ConsecutiveLine  => GetLineColor(LineType.ConsecutiveLine),
                BossModifierId.SlowThermo       => GetLineColor(LineType.SlowThermo),
                BossModifierId.UniqueSetLine    => GetLineColor(LineType.UniqueSetLine),
                BossModifierId.DifferenceKropki => GamePalette.OverlayKropkiWhite,
                BossModifierId.RatioKropki      => GamePalette.OverlayKropkiBlack,
                BossModifierId.FullKropki       => GamePalette.OverlayKropkiWhite,
                BossModifierId.SumKropki        => GamePalette.OverlayKropkiSum,
                BossModifierId.GreaterLessThan  => GamePalette.OverlayGreaterLess,
                BossModifierId.XVPairs          => GamePalette.OverlayXVPair,
                BossModifierId.KillerCages      => Color.white,
                BossModifierId.ArrowSums        => GamePalette.OverlayArrow,
                BossModifierId.EvenOdd          => GamePalette.OverlayEvenMarker,
                BossModifierId.PrimeCells       => GamePalette.OverlayPrimeCircle,
                BossModifierId.FortressCells    => GamePalette.OverlayFortress,
                BossModifierId.NonconsecLine      => GetLineColor(LineType.NonconsecLine),
                BossModifierId.HighLowAlternating => GetLineColor(LineType.HighLowAlternating),
                BossModifierId.NabnerLine         => GetLineColor(LineType.NabnerLine),
                BossModifierId.EntropicLine       => GetLineColor(LineType.EntropicLine),
                BossModifierId.ModularLine        => GetLineColor(LineType.ModularLine),
                BossModifierId.ZipperLine         => GetLineColor(LineType.ZipperLine),
                BossModifierId.FastThermo         => GetLineColor(LineType.FastThermo),
                BossModifierId.AmbiguousThermo    => GetLineColor(LineType.AmbiguousThermo),
                BossModifierId.FullWhiteKropki    => GamePalette.OverlayKropkiWhite,
                BossModifierId.FullBlackKropki    => GamePalette.OverlayKropkiBlack,
                BossModifierId.DifferenceK        => GamePalette.OverlayDifferenceK,
                BossModifierId.MultipleCells      => GamePalette.OverlayMultiple,
                BossModifierId.MirrorCells        => GamePalette.OverlayMirror,
                _                               => Color.clear
            };
            if (color.a <= 0f) return false;
            color.a = 1f;
            return true;
        }

        private const string KropkiWhiteKey = "Kropki.White";
        private const string KropkiBlackKey = "Kropki.Black";
        private const string KropkiSumKey = "Kropki.Sum";
        private const string GreaterLessKey = "Pairs.GreaterLess";
        private const string XvKey = "Pairs.XV";
        private const string CagesKey = "Cages";
        private const string ArrowsKey = "Arrows";
        private const string EvenOddKey = "Markers.EvenOdd";
        private const string PrimeKey = "Markers.Prime";
        private const string FortressKey = "Markers.Fortress";
        private static string LineKey(LineType type) => "Line." + type;

        /// <summary>
        /// Dims every overlay layer except the modifier's to 15 % for <paramref name="seconds"/>, then restores them.
        /// Null restores all layers at once.
        /// </summary>
        public void IsolateModifier(BossModifierId? modifier, float seconds = IsolationSeconds)
        {
            if (_isolationRestore != null)
            {
                StopCoroutine(_isolationRestore);
                _isolationRestore = null;
            }

            var keep = modifier.HasValue ? new HashSet<string>(GetOverlayGroupKeys(modifier.Value)) : null;
            foreach (var pair in _overlayGroups)
                if (pair.Value != null)
                    pair.Value.alpha = keep == null || keep.Contains(pair.Key) ? 1f : IsolatedOthersAlpha;

            if (keep != null && seconds > 0f && isActiveAndEnabled)
                _isolationRestore = StartCoroutine(RestoreLayersAfter(seconds));
        }

        /// <summary>Current alpha of an overlay layer, or -1 when the layer was not drawn (for tests and diagnostics).</summary>
        public float GetOverlayGroupAlpha(string key) =>
            _overlayGroups.TryGetValue(key, out var group) && group != null ? group.alpha : -1f;

        private IEnumerator RestoreLayersAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _isolationRestore = null;
            IsolateModifier(null);
        }

        private void UseOverlayGroup(string key)
        {
            if (key == null || _gridOverlayRoot == null)
            {
                _currentOverlayGroup = null;
                return;
            }

            if (!_overlayGroups.TryGetValue(key, out var group) || group == null)
            {
                var go = new GameObject("Group_" + key, typeof(RectTransform), typeof(CanvasGroup));
                go.transform.SetParent(_gridOverlayRoot, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                group = go.GetComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                _overlayGroups[key] = group;
                _overlayObjects.Add(go);
            }
            _currentOverlayGroup = (RectTransform)group.transform;
        }

        private void ClearOverlayGroups()
        {
            if (_isolationRestore != null)
            {
                StopCoroutine(_isolationRestore);
                _isolationRestore = null;
            }
            _overlayGroups.Clear();
            _currentOverlayGroup = null;
        }
    }
}
