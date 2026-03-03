using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class HeatCurveGraphController : MonoBehaviour
    {
        [SerializeField] private RectTransform graphRoot;
        [SerializeField] private Image pointPrefab;
        [SerializeField] private Image segmentPrefab;
        [SerializeField] private Text yAxisLabel;

        private readonly List<GameObject> _instances = new();
        private RunMapController _runMap;

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        public void Configure(RectTransform root, Image point, Image segment, Text axisLabel)
        {
            graphRoot = root;
            pointPrefab = point;
            segmentPrefab = segment;
            yAxisLabel = axisLabel;
        }

        public void RenderCurrentRunCurve()
        {
            if (_runMap == null)
            {
                return;
            }

            RenderCurve(_runMap.GetHeatCurve());
        }

        public void RenderCurve(List<float> heatValues)
        {
            ClearGraph();

            if (graphRoot == null || pointPrefab == null || heatValues == null || heatValues.Count == 0)
            {
                return;
            }

            var width = Math.Max(1f, graphRoot.rect.width);
            var height = Math.Max(1f, graphRoot.rect.height);

            var min = heatValues[0];
            var max = heatValues[0];
            for (var i = 1; i < heatValues.Count; i++)
            {
                min = Math.Min(min, heatValues[i]);
                max = Math.Max(max, heatValues[i]);
            }

            var span = Math.Max(0.01f, max - min);
            if (yAxisLabel != null)
            {
                yAxisLabel.text = $"Heat {min:0.0} - {max:0.0}";
            }

            Vector2? previous = null;
            for (var i = 0; i < heatValues.Count; i++)
            {
                var t = heatValues.Count == 1 ? 0f : i / (float)(heatValues.Count - 1);
                var normalizedY = (heatValues[i] - min) / span;
                var position = new Vector2(t * width, normalizedY * height);

                var point = Instantiate(pointPrefab, graphRoot);
                var pointRect = point.rectTransform;
                pointRect.anchorMin = new Vector2(0f, 0f);
                pointRect.anchorMax = new Vector2(0f, 0f);
                pointRect.anchoredPosition = position;
                _instances.Add(point.gameObject);

                if (previous.HasValue && segmentPrefab != null)
                {
                    DrawSegment(previous.Value, position);
                }

                previous = position;
            }
        }

        private void DrawSegment(Vector2 a, Vector2 b)
        {
            var segment = Instantiate(segmentPrefab, graphRoot);
            var rect = segment.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);

            var delta = b - a;
            var length = delta.magnitude;
            rect.sizeDelta = new Vector2(length, Math.Max(2f, rect.sizeDelta.y));
            rect.anchoredPosition = a + (delta * 0.5f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _instances.Add(segment.gameObject);
        }

        private void ClearGraph()
        {
            for (var i = 0; i < _instances.Count; i++)
            {
                if (_instances[i] != null)
                {
                    Destroy(_instances[i]);
                }
            }

            _instances.Clear();
        }
    }
}
