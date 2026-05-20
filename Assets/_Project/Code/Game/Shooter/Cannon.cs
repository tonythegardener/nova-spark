using System.Collections.Generic;
using System.Linq;
using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Level;
using NovaSpark.Game.Powerups;
using NovaSpark.Powerups;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaSpark.Game.Shooter
{
    public sealed class Cannon : MonoBehaviour
    {
        private LevelDefinition _level;
        private MarbleChain _chain;
        private Camera _camera;
        private Sprite _sprite;

        // Shot queue: index 0 is chambered (fires next); 1..N are previews.
        private readonly List<MarbleColor> _shotQueue = new();
        private float _cooldownRemaining;
        private bool _firingEnabled;

        // Equipped powerups, in equip order. Order matters: first
        // TryHandleProjectileHit that returns true wins.
        private readonly List<PowerupEffect> _equippedEffects = new();

        // Visuals
        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _currentRenderer;
        private Transform _barrelPivot;
        private Transform _previewRoot;
        private readonly List<SpriteRenderer> _previewRenderers = new();

        // Scratch buffer used by PickColor / OnChainChanged to avoid garbage.
        private readonly List<MarbleColor> _scratchColors = new();

        public Transform BarrelMuzzle => _barrelPivot;

        public void Configure(LevelDefinition level, MarbleChain chain, Camera cam, Sprite sprite)
        {
            _level = level;
            _chain = chain;
            _camera = cam;
            _sprite = sprite;

            BuildBaseVisuals();

            _shotQueue.Clear();
            _shotQueue.Add(PickColor());
            _shotQueue.Add(PickColor());
            RebuildPreviewRow(previewCount: 1);
            RefreshChamberAndPreviewColors();

            _firingEnabled = true;
            if (_chain != null) _chain.OnChainChanged += OnChainChanged;
        }

        private void OnDestroy()
        {
            if (_chain != null) _chain.OnChainChanged -= OnChainChanged;
            for (int i = _equippedEffects.Count - 1; i >= 0; i--) _equippedEffects[i].Unequip();
            _equippedEffects.Clear();
        }

        public void SetFiringEnabled(bool enabled) => _firingEnabled = enabled;

        // -------------------- powerup equip/unequip --------------------

        public void EquipPowerup(PowerupDefinition def, PowerupContext ctx)
        {
            if (def == null) return;
            var effect = PowerupEffects.Create(def.EffectKind);
            if (effect == null) return;
            effect.Equip(def, ctx);
            _equippedEffects.Add(effect);
            RecomputeQueueLength();
            Debug.Log($"[NovaSpark.Cannon] Equipped powerup '{def.ContentId}' (effect '{def.EffectKind}', duration {def.DurationKind} {def.DurationParam}).");
        }

        public void UnequipAll()
        {
            for (int i = _equippedEffects.Count - 1; i >= 0; i--) _equippedEffects[i].Unequip();
            _equippedEffects.Clear();
            RecomputeQueueLength();
        }

        private void PruneExpiredEffects()
        {
            bool anyRemoved = false;
            for (int i = _equippedEffects.Count - 1; i >= 0; i--)
            {
                if (_equippedEffects[i].IsExpired)
                {
                    _equippedEffects[i].Unequip();
                    _equippedEffects.RemoveAt(i);
                    anyRemoved = true;
                }
            }
            if (anyRemoved) RecomputeQueueLength();
        }

        // -------------------- update / input --------------------

        private void Update()
        {
            if (_level == null) return;
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);

            // Effects tick + their Seconds-based expiry
            for (int i = 0; i < _equippedEffects.Count; i++) _equippedEffects[i].TickLifetime(Time.deltaTime);

            var mouse = Mouse.current;
            if (mouse == null) { PruneExpiredEffects(); return; }

            Vector2 origin = _barrelPivot != null ? (Vector2)_barrelPivot.position : (Vector2)transform.position;
            Vector2 aimWorld = ReadMouseWorld(mouse);
            Vector2 dir = (aimWorld - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            _barrelPivot.rotation = Quaternion.Euler(0f, 0f, angle);

            for (int i = 0; i < _equippedEffects.Count; i++) _equippedEffects[i].OnAim(origin, dir);

            if (_firingEnabled && SwapShotPressed()) RotateShotQueue();

            if (_firingEnabled && mouse.leftButton.wasPressedThisFrame && _cooldownRemaining <= 0f)
            {
                Fire(dir);
            }

            PruneExpiredEffects();
        }

        private static bool SwapShotPressed()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) return true;
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) return true;
            return false;
        }

        // 1b.2a: swap is now a ROTATE through the visible queue. Chambered moves
        // to the end of the visible portion; everything else shifts forward one.
        // Player can cycle to any visible color into the chamber with N taps,
        // preserving the rest of the order so planned sequences still hold.
        private void RotateShotQueue()
        {
            int visibleLen = 1 + _previewRenderers.Count; // chambered + previews
            if (visibleLen < 2 || _shotQueue.Count < visibleLen) return;
            var moved = _shotQueue[0];
            _shotQueue.RemoveAt(0);
            _shotQueue.Insert(visibleLen - 1, moved);
            RefreshChamberAndPreviewColors();
        }

        private Vector2 ReadMouseWorld(Mouse mouse)
        {
            var screen = mouse.position.ReadValue();
            var v = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return new Vector2(v.x, v.y);
        }

        // -------------------- fire --------------------

        private void Fire(Vector2 dir)
        {
            float cd = _level.ShooterCooldown;
            for (int i = 0; i < _equippedEffects.Count; i++) cd = _equippedEffects[i].ModifyCooldown(cd);
            _cooldownRemaining = Mathf.Max(0.01f, cd);

            var chamberedColor = _shotQueue[0];
            var origin = (Vector2)_barrelPivot.position + dir * (_level.ProjectileRadius + 0.05f);
            var velocity = dir * _level.ShotVelocity;

            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                _equippedEffects[i].OnBeforeFire(origin, dir, ref chamberedColor);
            }

            var go = new GameObject($"Projectile_{chamberedColor}");
            go.transform.position = origin;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            var p = go.AddComponent<Projectile>();
            p.Launch(chamberedColor, velocity, _level.ProjectileRadius, _sprite, _chain, ResolveProjectileHit);

            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                _equippedEffects[i].OnProjectileLaunched(go, chamberedColor, velocity);
            }

            // Per-shot duration decrement (PerShots / OneUse).
            for (int i = 0; i < _equippedEffects.Count; i++) _equippedEffects[i].NotifyShotFired();

            _shotQueue.RemoveAt(0);
            _shotQueue.Add(PickColor());
            RefreshChamberAndPreviewColors();
        }

        // Orchestrates a projectile hit: walks effects looking for one that
        // fully handles it; if none does, applies the default insert with
        // possibly-mutated color via ModifyInsertColor; finally fires
        // OnProjectileHit notifications.
        private void ResolveProjectileHit(ChainMarble hit, MarbleColor originalColor, Vector2 velocity)
        {
            if (_chain == null) return;

            bool handled = false;
            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                if (_equippedEffects[i].TryHandleProjectileHit(hit, originalColor, velocity))
                {
                    handled = true;
                    break;
                }
            }

            if (!handled)
            {
                var color = originalColor;
                for (int i = 0; i < _equippedEffects.Count; i++)
                {
                    color = _equippedEffects[i].ModifyInsertColor(hit, color);
                }
                _chain.TryInsertAt(hit, velocity, color);
            }

            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                _equippedEffects[i].OnProjectileHit(hit, originalColor, velocity);
            }
        }

        // -------------------- queue length / preview rebuild --------------------

        private void RecomputeQueueLength()
        {
            int extra = 0;
            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                if (_equippedEffects[i].ExtraVisibleQueue > extra)
                    extra = _equippedEffects[i].ExtraVisibleQueue;
            }
            int targetPreviews = 1 + extra;
            int targetTotal = 1 + targetPreviews;

            while (_shotQueue.Count < targetTotal) _shotQueue.Add(PickColor());
            while (_shotQueue.Count > targetTotal) _shotQueue.RemoveAt(_shotQueue.Count - 1);

            RebuildPreviewRow(targetPreviews);
            RefreshChamberAndPreviewColors();
        }

        private void RebuildPreviewRow(int previewCount)
        {
            for (int i = _previewRenderers.Count - 1; i >= 0; i--)
            {
                if (_previewRenderers[i] != null) Destroy(_previewRenderers[i].gameObject);
            }
            _previewRenderers.Clear();

            if (_previewRoot == null)
            {
                var rootGo = new GameObject("PreviewRow");
                _previewRoot = rootGo.transform;
                _previewRoot.SetParent(transform, false);
                _previewRoot.localPosition = new Vector3(-1.1f, -0.1f, 0f);
            }

            for (int i = 0; i < previewCount; i++)
            {
                var go = new GameObject($"Preview_{i}");
                go.transform.SetParent(_previewRoot, false);
                go.transform.localPosition = new Vector3(-0.6f * i, 0f, 0f);
                float scale = _level.ProjectileRadius * (1.4f - 0.12f * i);
                if (scale < _level.ProjectileRadius * 0.7f) scale = _level.ProjectileRadius * 0.7f;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = Color.white;
                sr.sortingOrder = 55 - i;
                _previewRenderers.Add(sr);
            }
        }

        private void RefreshChamberAndPreviewColors()
        {
            if (_currentRenderer != null && _shotQueue.Count > 0)
            {
                _currentRenderer.color = _shotQueue[0].ToRGB();
            }
            for (int i = 0; i < _previewRenderers.Count; i++)
            {
                int qIdx = i + 1;
                if (qIdx < _shotQueue.Count)
                {
                    _previewRenderers[i].color = _shotQueue[qIdx].ToRGB();
                    _previewRenderers[i].enabled = true;
                }
                else
                {
                    _previewRenderers[i].enabled = false;
                }
            }
        }

        // -------------------- chain palette tracking --------------------

        private void OnChainChanged()
        {
            if (_chain == null || _shotQueue.Count == 0) return;
            var active = _chain.ActiveColors();
            if (active.Count == 0) return;

            bool changed = false;
            for (int i = 0; i < _shotQueue.Count; i++)
            {
                if (!active.Contains(_shotQueue[i]))
                {
                    _shotQueue[i] = PickFromActiveSet(active, AdjacentAvoid(i));
                    changed = true;
                }
            }
            if (changed) RefreshChamberAndPreviewColors();
        }

        private MarbleColor AdjacentAvoid(int idx)
        {
            if (idx > 0 && idx - 1 < _shotQueue.Count) return _shotQueue[idx - 1];
            if (idx + 1 < _shotQueue.Count) return _shotQueue[idx + 1];
            return _shotQueue[idx];
        }

        private MarbleColor PickFromActiveSet(IReadOnlyCollection<MarbleColor> active, MarbleColor avoid)
        {
            _scratchColors.Clear();
            foreach (var c in active)
            {
                if (c != avoid) _scratchColors.Add(c);
            }
            if (_scratchColors.Count == 0)
            {
                foreach (var c in active) return c;
            }
            return _scratchColors[Random.Range(0, _scratchColors.Count)];
        }

        private MarbleColor PickColor()
        {
            if (_chain != null)
            {
                var active = _chain.ActiveColors();
                if (active.Count > 0)
                {
                    _scratchColors.Clear();
                    foreach (var c in active) _scratchColors.Add(c);
                    return _scratchColors[Random.Range(0, _scratchColors.Count)];
                }
            }
            return _level.AllowedColors[Random.Range(0, _level.AllowedColors.Length)];
        }

        // -------------------- build base visuals --------------------

        private void BuildBaseVisuals()
        {
            transform.position = _level.ShooterPosition;

            _baseRenderer = NewSprite("Base", transform, _sprite, new Color(0.4f, 0.42f, 0.5f, 1f), scale: 1.1f, sortingOrder: 50);

            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(transform, false);
            _barrelPivot = barrel.transform;

            NewSprite("BarrelStem", _barrelPivot, _sprite, new Color(0.55f, 0.58f, 0.66f, 1f),
                scale: 0.45f, sortingOrder: 51, localPos: new Vector3(0f, 0.7f, 0f), nonUniformY: 2.0f);

            _currentRenderer = NewSprite("Current", _barrelPivot, _sprite, Color.white,
                scale: _level.ProjectileRadius * 2f, sortingOrder: 60, localPos: new Vector3(0f, 0.95f, 0f));
        }

        private static SpriteRenderer NewSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            float scale,
            int sortingOrder,
            Vector3? localPos = null,
            float nonUniformY = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (localPos.HasValue) go.transform.localPosition = localPos.Value;
            go.transform.localScale = new Vector3(scale, scale * nonUniformY, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
