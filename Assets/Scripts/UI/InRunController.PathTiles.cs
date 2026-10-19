using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // Path map tile decorators (rings, halos, cross-link badge, player marker).
    // Split out of InRunController.cs to keep it within its size budget (docs/controller-extraction-plan.md).
    public sealed partial class InRunController
    {
        // UI-STYLE-001: decorators for the ink-and-paper path. Reachable tiles get a warm halo and a second brush ring,
        // so they stand out by shape as well as colour (colour-blind players).
        private static void AddReachableHalo(RectTransform parent, string name, Color ink)
        {
            var halo = ScrollUiKit.Fill(parent, name, new Vector2(-0.22f, -0.22f), new Vector2(1.22f, 1.22f),
                new Color(1f, 0.74f, 0.36f, 0.42f), ScrollUiKit.Glow());
            halo.transform.SetAsFirstSibling();
            var second = ScrollUiKit.Fill(halo.transform, "SecondStroke", new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f),
                new Color(ink.r, ink.g, ink.b, 0.65f), ScrollUiKit.Enso());
            second.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 150f);
        }

        /// <summary>Reachable bridge tile: the reachable halo in ink (the bridge route is dotted ink).</summary>
        private static void AddBridgeRing(RectTransform parent) =>
            AddReachableHalo(parent, "BridgeRing", new Color(0.16f, 0.13f, 0.10f, 1f));

        /// <summary>[REQ: MAP-CROSS-001] A small vermilion "⇄" seal on a cross-link tile.</summary>
        private static void AddCrossLinkBadge(RectTransform parent)
        {
            var badge = ScrollUiKit.Spot(parent, "CrossLinkBadge", new Vector2(0.12f, 0.88f), new Vector2(26f, 26f),
                new Color(0.62f, 0.16f, 0.17f, 1f), ScrollUiKit.Disc());
            // Drawn, not a glyph: the game font has no ⇄.
            ScrollUiKit.Fill(badge.transform, "Arrows", new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f),
                new Color(0.98f, 0.94f, 0.86f, 1f), ScrollUiKit.SwapArrows());
        }

        /// <summary>[REQ: REQ-PATH-100] Player marker: the class portrait above the tile the player stands on (GAP-022).</summary>
        private static void AddPlayerMarker(RectTransform parent, ClassId classId)
        {
            var go = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(34f, 34f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var sprite = InRunUiFactory.LoadResourceSprite("class/icon_" + ClassCatalog.GetIconName(classId));
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(1f, 1f, 1f, 0f);
                var txt = InRunUiFactory.CreateText(go.transform, "Arrow", "\u25BC", 20, TextAnchor.MiddleCenter,
                    new Color(1f, 0.88f, 0.30f, 1f));
                txt.rectTransform.anchorMin = Vector2.zero;
                txt.rectTransform.anchorMax = Vector2.one;
                txt.rectTransform.offsetMin = txt.rectTransform.offsetMax = Vector2.zero;
                txt.raycastTarget = false;
            }
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        /// <summary>The Start tile: a faint vermilion glow (the route begins in vermilion ink).</summary>
        private static void AddStartHalo(RectTransform parent)
        {
            var halo = ScrollUiKit.Fill(parent, "StartHalo", new Vector2(-0.15f, -0.15f), new Vector2(1.15f, 1.15f),
                new Color(0.62f, 0.16f, 0.17f, 0.18f), ScrollUiKit.Glow());
            halo.transform.SetAsFirstSibling();
        }

        /// <summary>The boss seal sits on a soft vermilion glow.</summary>
        private static void AddBossRing(RectTransform parent)
        {
            var glow = ScrollUiKit.Fill(parent, "BossRing", new Vector2(-0.18f, -0.18f), new Vector2(1.18f, 1.18f),
                new Color(0.62f, 0.16f, 0.17f, 0.22f), ScrollUiKit.Glow());
            glow.transform.SetAsFirstSibling();
        }

        private static void AddAvailableRing(RectTransform parent) =>
            AddReachableHalo(parent, "AvailableRing", new Color(0.62f, 0.16f, 0.17f, 1f));
    }
}
