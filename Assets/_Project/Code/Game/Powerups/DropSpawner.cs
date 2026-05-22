using System.Collections.Generic;
using NovaSpark.Game.Level;
using NovaSpark.Game.Shooter;
using NovaSpark.Powerups;
using NovaSpark.Run;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Spawns PowerupDrop instances at off-path positions over the course of a
    // level. Picks each drop's powerup from RunState.RunPool. Collected drops
    // immediately equip on the cannon.
    public sealed class DropSpawner : MonoBehaviour
    {
        private LevelDefinition _level;
        private PathSampler _path;
        private Camera _camera;
        private Cannon _cannon;
        private PowerupContext _ctx;
        private Transform _root;

        private readonly List<PowerupDrop> _active = new();
        private float _spawnTimer;
        private bool _running;

        // Tuning — also exposed on LevelDefinition for per-level override.
        private float _spawnInterval = 25f;
        private int _maxConcurrent = 2;
        private float _dropLifetime = 15f;
        private float _dropRadius = 0.30f;
        private float _minDistanceFromPath = 0.9f;

        public void Configure(
            LevelDefinition level,
            PathSampler path,
            Camera camera,
            Cannon cannon,
            PowerupContext ctx)
        {
            _level = level;
            _path = path;
            _camera = camera;
            _cannon = cannon;
            _ctx = ctx;

            // Pull per-level overrides if set on the LevelDefinition (>0)
            if (level.DropSpawnIntervalSeconds > 0f) _spawnInterval = level.DropSpawnIntervalSeconds;
            if (level.DropMaxConcurrent > 0) _maxConcurrent = level.DropMaxConcurrent;
            if (level.DropLifetimeSeconds > 0f) _dropLifetime = level.DropLifetimeSeconds;

            _dropRadius = Mathf.Max(0.18f, level.MarbleRadius);
            _minDistanceFromPath = Mathf.Max(_dropRadius * 2.4f, _level.MarbleRadius * 2.4f);

            _root = new GameObject("PowerupDrops").transform;
            _root.SetParent(transform, false);

            // First drop lands a bit early so the player sees one promptly.
            _spawnTimer = Mathf.Max(0f, _spawnInterval * 0.4f);
            _running = true;
        }

        public void SetRunning(bool running) => _running = running;

        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null) Destroy(_active[i].gameObject);
            }
            _active.Clear();
        }

        private void Update()
        {
            if (!_running || _level == null) return;

            // Sweep nulls (destroyed externally — e.g., collected)
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] == null) _active.RemoveAt(i);
            }

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval && _active.Count < _maxConcurrent)
            {
                _spawnTimer = 0f;
                TrySpawnOne();
            }
        }

        private void TrySpawnOne()
        {
            // Off-path drops only carry Shooter-kind powerups (Path effects come
            // in as special chain marbles, not drops). Without this filter, a
            // Chrono Field drop would 'collect' but Cannon.EquipPowerup would
            // bail silently because PowerupEffects.Create has no chrono_field
            // case — the player would shoot a drop and see no effect at all.
            var def = RunState.PickRandomShooterFromRunPool();
            if (def == null) return;

            if (!TryPickPosition(out var pos)) return;

            var go = new GameObject($"Drop_{def.ContentId}");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<CircleCollider2D>();
            var drop = go.AddComponent<PowerupDrop>();
            drop.Configure(
                def,
                _dropLifetime,
                _dropRadius,
                ColorForPowerup(def),
                OnDropCollected,
                OnDropExpired);
            _active.Add(drop);
        }

        // Pick a random world position that's well off the path, inside the
        // visible viewport, and not directly under the cannon. Try a few times;
        // if no good spot, skip this spawn.
        private bool TryPickPosition(out Vector2 pos)
        {
            pos = Vector2.zero;
            if (_camera == null) return false;

            float ortho = _camera.orthographicSize;
            float aspect = _camera.aspect;
            float halfW = ortho * aspect;
            float halfH = ortho;
            Vector2 center = _camera.transform.position;

            float marginX = halfW * 0.10f;
            float marginY = halfH * 0.10f;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                float x = Random.Range(center.x - halfW + marginX, center.x + halfW - marginX);
                float y = Random.Range(center.y - halfH + marginY, center.y + halfH - marginY);
                var candidate = new Vector2(x, y);

                if (DistanceToPath(candidate) < _minDistanceFromPath) continue;
                if (Vector2.Distance(candidate, _cannon.transform.position) < 1.2f) continue;

                pos = candidate;
                return true;
            }
            return false;
        }

        private float DistanceToPath(Vector2 p)
        {
            var pts = _path.Points;
            float best = float.MaxValue;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float d = DistancePointToSegment(p, pts[i], pts[i + 1]);
                if (d < best) best = d;
            }
            return best;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude);
            t = Mathf.Clamp01(t);
            var proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }

        // Deterministic warm/cool color per powerup so player can learn-by-color
        // which drop is which (good for telegraph; we'll add an icon later).
        private static Color ColorForPowerup(PowerupDefinition def)
        {
            // Hash content id to a hue
            int hash = 0;
            foreach (var c in def.ContentId) hash = unchecked(hash * 31 + c);
            float hue = ((hash & 0x7fffffff) % 360) / 360f;
            return Color.HSVToRGB(hue, 0.55f, 1f);
        }

        private void OnDropCollected(PowerupDefinition def)
        {
            if (_cannon == null) return;
            _cannon.EquipPowerup(def, _ctx);
            Debug.Log($"[NovaSpark.Drops] Collected '{def.ContentId}' — equipped on cannon.");
        }

        private void OnDropExpired(PowerupDrop drop)
        {
            // Removed from _active by null-sweep on next Update.
            Debug.Log($"[NovaSpark.Drops] Drop '{drop.Definition.ContentId}' expired uncollected.");
        }
    }
}
