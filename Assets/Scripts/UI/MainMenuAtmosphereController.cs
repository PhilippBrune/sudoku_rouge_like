using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuAtmosphereController : MonoBehaviour
    {
        [SerializeField] private RectTransform farLayer;
        [SerializeField] private RectTransform midLayer;
        [SerializeField] private RectTransform nearLayer;
        [SerializeField] private RectTransform petalRoot;
        [SerializeField] private RectTransform mistRoot;
        [SerializeField] private int petalCount = 18;
        [SerializeField] private int mistCount = 3;

        private readonly List<RectTransform> _petals = new();
        private readonly List<float> _petalSpeed = new();
        private readonly List<RectTransform> _mist = new();
        private readonly List<float> _mistSpeed = new();

        public void Configure(RectTransform far, RectTransform mid, RectTransform near, RectTransform petals, RectTransform mist)
        {
            farLayer = far;
            midLayer = mid;
            nearLayer = near;
            petalRoot = petals;
            mistRoot = mist;

            EnsurePetals();
            EnsureMist();
        }

        private void Awake()
        {
            EnsurePetals();
            EnsureMist();
        }

        private void Update()
        {
            AnimateParallax();
            AnimatePetals();
            AnimateMist();
        }

        private void EnsurePetals()
        {
            if (petalRoot == null)
            {
                return;
            }

            while (_petals.Count < petalCount)
            {
                var i = _petals.Count;
                var go = new GameObject($"Petal_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(petalRoot, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(Random.value, Random.value);
                rect.anchorMax = rect.anchorMin;
                rect.sizeDelta = new Vector2(8f + Random.value * 8f, 8f + Random.value * 8f);
                var image = go.GetComponent<Image>();
                image.color = new Color(1f, 0.86f, 0.90f, 0.45f + Random.value * 0.35f);

                _petals.Add(rect);
                _petalSpeed.Add(10f + Random.value * 25f);
            }
        }

        private void EnsureMist()
        {
            if (mistRoot == null)
            {
                return;
            }

            while (_mist.Count < mistCount)
            {
                var i = _mist.Count;
                var go = new GameObject($"Mist_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(mistRoot, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.20f + (i * 0.20f));
                rect.anchorMax = new Vector2(1f, 0.40f + (i * 0.20f));
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var image = go.GetComponent<Image>();
                image.color = new Color(0.85f, 0.92f, 0.95f, 0.06f + i * 0.02f);

                _mist.Add(rect);
                _mistSpeed.Add(4f + i * 2f);
            }
        }

        private void AnimateParallax()
        {
            var t = Time.unscaledTime;
            if (farLayer != null)
            {
                farLayer.anchoredPosition = new Vector2(Mathf.Sin(t * 0.05f) * 8f, 0f);
            }

            if (midLayer != null)
            {
                midLayer.anchoredPosition = new Vector2(Mathf.Sin(t * 0.08f) * 12f, Mathf.Cos(t * 0.06f) * 4f);
            }

            if (nearLayer != null)
            {
                nearLayer.anchoredPosition = new Vector2(Mathf.Sin(t * 0.12f) * 16f, Mathf.Sin(t * 0.07f) * 5f);
            }
        }

        private void AnimatePetals()
        {
            for (var i = 0; i < _petals.Count; i++)
            {
                var rect = _petals[i];
                if (rect == null)
                {
                    continue;
                }

                var anchor = rect.anchorMin;
                anchor.y -= (_petalSpeed[i] * Time.unscaledDeltaTime) / 1080f;
                anchor.x += Mathf.Sin((Time.unscaledTime + i) * 0.7f) * 0.0005f;

                if (anchor.y < -0.05f)
                {
                    anchor.y = 1.05f;
                    anchor.x = Random.value;
                }

                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.unscaledTime + i) * 2f) * 20f);
            }
        }

        private void AnimateMist()
        {
            for (var i = 0; i < _mist.Count; i++)
            {
                var rect = _mist[i];
                if (rect == null)
                {
                    continue;
                }

                var x = Mathf.Sin((Time.unscaledTime * 0.1f) + i) * (20f + 10f * i);
                rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            }
        }
    }
}
