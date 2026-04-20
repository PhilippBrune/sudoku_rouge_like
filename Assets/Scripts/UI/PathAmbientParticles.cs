using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Adds a handful of tiny slow-drifting UI circles to the path panel background.
    /// Each circle's colour matches the floor theme (petals, ripples, embers).
    /// No physics — pure UI Image transforms updated in Update().
    /// Created and destroyed by EnsurePathOverlayRoot / panel teardown.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PathAmbientParticles : MonoBehaviour
    {
        private const int Count = 7;

        private struct Particle
        {
            public RectTransform Rt;
            public float SpeedY;   // px / s (negative = upward in UI space)
            public float SpeedX;   // px / s (gentle horizontal drift)
            public float Phase;    // initial position offset [0,1]
        }

        private Particle[] _particles;
        private RectTransform _panelRt;
        private float _panelW, _panelH;

        /// <summary>
        /// Call once after the panel layout is known.
        /// <paramref name="floor"/> 0–4 determines particle colour and direction.
        /// </summary>
        public void Initialize(RectTransform panel, int floor)
        {
            _panelRt = panel;
            _panelW  = panel.rect.width  > 10f ? panel.rect.width  : 400f;
            _panelH  = panel.rect.height > 10f ? panel.rect.height : 300f;

            // Floor-specific particle properties
            // floor 0: pale-green cherry-petal fragments (gentle upward drift)
            // floor 1: dark-green moss spores (slow downward sink)
            // floor 2: light-blue water droplets (diagonal drift)
            // floor 3: warm amber lantern sparks (float upward faster)
            // floor 4: dark-orange embers (erratic upward rise)
            var (col, speedY, speedX, size) = floor switch
            {
                0 => (new Color(0.55f, 0.82f, 0.38f, 0.45f),  -14f,   3f, 4f),
                1 => (new Color(0.12f, 0.32f, 0.09f, 0.40f),   10f,   1f, 3f),
                2 => (new Color(0.38f, 0.65f, 0.90f, 0.38f),  -10f,  -4f, 3f),
                3 => (new Color(0.92f, 0.70f, 0.22f, 0.50f),  -20f,   2f, 3f),
                4 => (new Color(0.90f, 0.30f, 0.08f, 0.55f),  -22f,   5f, 4f),
                _ => (new Color(0.55f, 0.82f, 0.38f, 0.45f),  -14f,   3f, 4f),
            };

            _particles = new Particle[Count];
            for (var p = 0; p < Count; p++)
            {
                var go = new GameObject($"AmbientParticle_{p}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(panel, false);
                go.transform.SetSiblingIndex(2); // just above the two background layers, below content

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);

                // Random-ish starting position spread across the full panel
                var phase   = p / (float)Count;
                var startX  = phase * _panelW;
                var startY  = GardenTextureFactory.Hash((int)(phase * 997), p * 13) * _panelH;
                rt.anchoredPosition = new Vector2(startX, startY);

                var img = go.GetComponent<Image>();
                img.color         = col;
                img.raycastTarget = false;

                // Add slight alpha variation per particle
                var variedCol    = col;
                variedCol.a      = col.a * (0.65f + GardenTextureFactory.Hash(p * 7, p * 3) * 0.35f);
                img.color        = variedCol;

                // Speed variation (±25%)
                var speedVar = 0.75f + GardenTextureFactory.Hash(p * 5, p + 11) * 0.50f;

                _particles[p] = new Particle
                {
                    Rt     = rt,
                    SpeedY = speedY * speedVar,
                    SpeedX = speedX * speedVar,
                    Phase  = phase
                };
            }
        }

        private void Update()
        {
            if (_particles == null || _panelRt == null) return;

            // Re-read panel size if it changed (first few frames after layout)
            if (_panelRt.rect.width > 10f)  _panelW = _panelRt.rect.width;
            if (_panelRt.rect.height > 10f) _panelH = _panelRt.rect.height;

            var dt = Time.deltaTime;
            for (var p = 0; p < _particles.Length; p++)
            {
                var rt = _particles[p].Rt;
                if (rt == null) continue;

                var pos = rt.anchoredPosition;
                pos.x += _particles[p].SpeedX * dt;
                pos.y += _particles[p].SpeedY * dt;

                // Wrap: when a particle exits the panel it re-enters from the opposite edge
                if (pos.y > _panelH + 6f)  pos.y = -6f;
                if (pos.y < -6f)            pos.y = _panelH + 6f;
                if (pos.x > _panelW + 6f)  pos.x = 0f;
                if (pos.x < -6f)            pos.x = _panelW;

                rt.anchoredPosition = pos;
            }
        }
    }
}
