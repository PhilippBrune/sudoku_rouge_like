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
        /// <summary>Gold double-ring for reachable bridge nodes — more prominent than the standard ring.</summary>
        private static void AddBridgeRing(RectTransform parent)
        {
            // Outer gold ring
            var outer = new GameObject("BridgeRingOuter", typeof(RectTransform), typeof(Image));
            outer.transform.SetParent(parent, false);
            outer.transform.SetAsFirstSibling();
            var outerRt = outer.GetComponent<RectTransform>();
            outerRt.anchorMin = new Vector2(-0.22f, -0.22f);
            outerRt.anchorMax = new Vector2(1.22f,  1.22f);
            outerRt.offsetMin = outerRt.offsetMax = Vector2.zero;
            var outerImg = outer.GetComponent<Image>();
            outerImg.color = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.05f);
            outerImg.raycastTarget = false;
            var outerOl = outer.AddComponent<Outline>();
            outerOl.effectColor = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.70f);
            outerOl.effectDistance = new Vector2(4f, -4f);
            // Inner ring (tighter)
            var inner = new GameObject("BridgeRingInner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(parent, false);
            inner.transform.SetAsFirstSibling();
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(-0.11f, -0.11f);
            innerRt.anchorMax = new Vector2(1.11f,  1.11f);
            innerRt.offsetMin = innerRt.offsetMax = Vector2.zero;
            var innerImg = inner.GetComponent<Image>();
            innerImg.color = new Color(1f, 0.85f, 0.20f, 0.04f);
            innerImg.raycastTarget = false;
            var innerOl = inner.AddComponent<Outline>();
            innerOl.effectColor = new Color(1f, 0.85f, 0.20f, 0.55f);
            innerOl.effectDistance = new Vector2(2f, -2f);
        }

        /// <summary>Always-visible green halo for the Start node — marks the entry point.</summary>
        /// <summary>[REQ: MAP-CROSS-001] Small gold "⇄" badge in the top-left corner of a cross-link tile.</summary>
        private static void AddCrossLinkBadge(RectTransform parent)
        {
            var go = new GameObject("CrossLinkBadge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(20f, 20f);
            rt.anchoredPosition = new Vector2(4f, -4f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.95f, 0.78f, 0.20f, 0.95f);
            img.raycastTarget = false;
            var txt = InRunUiFactory.CreateText(go.transform, "Arrows", "\u21C4", 14, TextAnchor.MiddleCenter,
                new Color(0.12f, 0.10f, 0.05f, 1f));
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = txt.rectTransform.offsetMax = Vector2.zero;
            txt.raycastTarget = false;
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

        private static void AddStartHalo(RectTransform parent)
        {
            var go = new GameObject("StartHalo", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.20f, -0.20f);
            rt.anchorMax = new Vector2(1.20f,  1.20f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.85f, 0.30f, 0.05f);
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.28f, 0.82f, 0.28f, 0.68f);
            ol.effectDistance = new Vector2(3f, -3f);
        }

        /// <summary>Always-visible red ring for the Boss gate node — marks the destination.</summary>
        private static void AddBossRing(RectTransform parent)
        {
            var go = new GameObject("BossRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.10f, -0.10f);
            rt.anchorMax = new Vector2(1.10f,  1.10f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(GamePalette.DangerRed.r, GamePalette.DangerRed.g, GamePalette.DangerRed.b, 0.06f);
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(GamePalette.DangerRed.r, GamePalette.DangerRed.g, GamePalette.DangerRed.b, 0.72f);
            ol.effectDistance = new Vector2(4f, -4f);
        }

        private static void AddAvailableRing(RectTransform parent)
        {
            var go = new GameObject("AvailableRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.13f, -0.13f);
            rt.anchorMax = new Vector2(1.13f,  1.13f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color         = new Color(1.0f, 0.85f, 0.20f, 0.05f); // near-transparent fill
            img.raycastTarget = false;
            // C: outline-only ring — bright gold border, minimal interior fill
            var ol = go.AddComponent<Outline>();
            ol.effectColor    = new Color(1.0f, 0.85f, 0.20f, 0.88f);
            ol.effectDistance = new Vector2(3f, -3f);
        }
    }
}
