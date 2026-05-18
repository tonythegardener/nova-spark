using System.Collections.Generic;
using NovaSpark.Core;
using NovaSpark.Game.Level;
using UnityEngine;

namespace NovaSpark.Game.Chain
{
    public sealed class MarbleChain : MonoBehaviour
    {
        private readonly List<ChainMarble> _marbles = new();
        private LevelDefinition _level;
        private PathSampler _path;
        private Sprite _sprite;

        private int _spawnedCount;
        private float _spawnTimer;
        private bool _spawnComplete;

        public IReadOnlyList<ChainMarble> Marbles => _marbles;
        public bool SpawnComplete => _spawnComplete;
        public bool ReachedBastion { get; private set; }
        public System.Action OnMatchResolved;
        public System.Action OnReachedBastion;
        public System.Action OnCleared;
        public System.Action<Vector2, MarbleColor> OnMarbleDestroyed;

        public void Configure(LevelDefinition level, PathSampler path, Sprite sprite)
        {
            _level = level;
            _path = path;
            _sprite = sprite;
        }

        public void Tick(float dt)
        {
            HandleSpawning(dt);
            AdvanceChain(dt);
            UpdatePositions();
            DetectAndResolveMatches();
            CheckEndConditions();
        }

        private void HandleSpawning(float dt)
        {
            if (_spawnComplete) return;
            _spawnTimer += dt;
            while (!_spawnComplete && _spawnTimer >= _level.SpawnIntervalSeconds)
            {
                _spawnTimer -= _level.SpawnIntervalSeconds;
                SpawnNext();
            }
        }

        private void SpawnNext()
        {
            float arc;
            if (_marbles.Count == 0)
            {
                arc = 0f;
            }
            else
            {
                arc = _marbles[_marbles.Count - 1].ArcLength - _level.MarbleSpacing;
            }
            var color = _level.AllowedColors[Random.Range(0, _level.AllowedColors.Length)];
            var marble = CreateMarble(color, arc, sortingOrder: _spawnedCount);
            _marbles.Add(marble);
            _spawnedCount++;
            if (_spawnedCount >= _level.TotalMarbles) _spawnComplete = true;
        }

        private ChainMarble CreateMarble(MarbleColor color, float arc, int sortingOrder)
        {
            var go = new GameObject($"Marble_{sortingOrder}_{color}");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            var marble = go.AddComponent<ChainMarble>();
            marble.Initialize(color, arc, _level.MarbleRadius, _sprite, sortingOrder);
            marble.transform.position = _path.SampleByDistance(arc);
            return marble;
        }

        private void AdvanceChain(float dt)
        {
            if (_marbles.Count == 0) return;
            _marbles[0].ArcLength += _level.ChainAdvanceSpeed * dt;
            float catchUp = _level.ChainAdvanceSpeed * _level.CatchUpSpeedMultiplier * dt;

            for (int i = 1; i < _marbles.Count; i++)
            {
                float natural = _marbles[i - 1].ArcLength - _level.MarbleSpacing;
                float current = _marbles[i].ArcLength;
                if (current >= natural)
                {
                    _marbles[i].ArcLength = natural;
                }
                else
                {
                    _marbles[i].ArcLength = Mathf.Min(natural, current + catchUp);
                }
            }
        }

        private void UpdatePositions()
        {
            for (int i = 0; i < _marbles.Count; i++)
            {
                var m = _marbles[i];
                m.transform.position = _path.SampleByDistance(m.ArcLength);
            }
        }

        public bool TryInsertAt(ChainMarble hit, Vector2 projectileVelocity, MarbleColor newColor)
        {
            int k = _marbles.IndexOf(hit);
            if (k < 0) return false;

            var tangent = _path.SampleTangent(hit.ArcLength);
            float dot = Vector2.Dot(projectileVelocity.normalized, tangent);
            int insertIndex = dot > 0f ? k : k + 1;

            float newArc;
            if (insertIndex == 0)
            {
                newArc = _marbles[0].ArcLength + _level.MarbleSpacing;
            }
            else if (insertIndex >= _marbles.Count)
            {
                newArc = _marbles[_marbles.Count - 1].ArcLength - _level.MarbleSpacing;
            }
            else
            {
                newArc = (_marbles[insertIndex - 1].ArcLength + _marbles[insertIndex].ArcLength) * 0.5f;
            }

            var inserted = CreateMarble(newColor, newArc, sortingOrder: _spawnedCount);
            _spawnedCount++;
            _marbles.Insert(insertIndex, inserted);

            for (int i = insertIndex + 1; i < _marbles.Count; i++)
            {
                float natural = _marbles[i - 1].ArcLength - _level.MarbleSpacing;
                if (_marbles[i].ArcLength > natural)
                    _marbles[i].ArcLength = natural;
            }

            return true;
        }

        private void DetectAndResolveMatches()
        {
            if (_marbles.Count < _level.MatchThreshold) return;

            float contiguityEpsilon = _level.MarbleSpacing * 0.4f;
            int runStart = 0;
            MarbleColor runColor = _marbles[0].Color;

            var toDestroy = new List<int>();

            for (int i = 1; i <= _marbles.Count; i++)
            {
                bool boundary = i == _marbles.Count;
                bool sameColor = !boundary && _marbles[i].Color == runColor;
                bool contiguous = !boundary &&
                    (_marbles[i - 1].ArcLength - _marbles[i].ArcLength) <= _level.MarbleSpacing + contiguityEpsilon;

                if (sameColor && contiguous) continue;

                int runLen = i - runStart;
                if (runLen >= _level.MatchThreshold)
                {
                    for (int j = runStart; j < i; j++) toDestroy.Add(j);
                }
                if (!boundary)
                {
                    runStart = i;
                    runColor = _marbles[i].Color;
                }
            }

            if (toDestroy.Count == 0) return;

            for (int i = toDestroy.Count - 1; i >= 0; i--)
            {
                var idx = toDestroy[i];
                var m = _marbles[idx];
                OnMarbleDestroyed?.Invoke(m.transform.position, m.Color);
                Destroy(m.gameObject);
                _marbles.RemoveAt(idx);
            }

            OnMatchResolved?.Invoke();
        }

        private void CheckEndConditions()
        {
            if (!ReachedBastion && _marbles.Count > 0 && _marbles[0].ArcLength >= _path.TotalLength)
            {
                ReachedBastion = true;
                OnReachedBastion?.Invoke();
            }
            else if (_spawnComplete && _marbles.Count == 0)
            {
                OnCleared?.Invoke();
            }
        }
    }
}
