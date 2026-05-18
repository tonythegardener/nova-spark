using System.Collections.Generic;
using UnityEngine;

namespace NovaSpark.Game.Level
{
    public sealed class PathSampler
    {
        private readonly Vector2[] _points;
        private readonly float[] _cumulativeLength;

        public float TotalLength { get; }

        public PathSampler(IReadOnlyList<Vector2> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2)
                throw new System.ArgumentException("Path needs at least 2 waypoints");

            _points = new Vector2[waypoints.Count];
            _cumulativeLength = new float[waypoints.Count];
            _points[0] = waypoints[0];
            _cumulativeLength[0] = 0f;
            for (int i = 1; i < waypoints.Count; i++)
            {
                _points[i] = waypoints[i];
                _cumulativeLength[i] = _cumulativeLength[i - 1]
                    + Vector2.Distance(_points[i - 1], _points[i]);
            }
            TotalLength = _cumulativeLength[_points.Length - 1];
        }

        public Vector2 SampleByDistance(float arcLength)
        {
            if (arcLength <= 0f) return _points[0];
            if (arcLength >= TotalLength) return _points[_points.Length - 1];

            int i = 1;
            while (i < _points.Length && _cumulativeLength[i] < arcLength) i++;

            float segStart = _cumulativeLength[i - 1];
            float segLen = _cumulativeLength[i] - segStart;
            float t = segLen <= Mathf.Epsilon ? 0f : (arcLength - segStart) / segLen;
            return Vector2.Lerp(_points[i - 1], _points[i], t);
        }

        public Vector2 SampleTangent(float arcLength)
        {
            arcLength = Mathf.Clamp(arcLength, 0f, TotalLength);
            int i = 1;
            while (i < _points.Length - 1 && _cumulativeLength[i] < arcLength) i++;
            var dir = _points[i] - _points[i - 1];
            return dir.sqrMagnitude > Mathf.Epsilon ? dir.normalized : Vector2.right;
        }

        public IReadOnlyList<Vector2> Points => _points;
    }
}
