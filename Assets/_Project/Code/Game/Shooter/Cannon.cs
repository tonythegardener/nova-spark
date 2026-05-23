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
        private SpriteRenderer _barrelStemRenderer; // tinted by effects (e.g., Vaporizer red)
        private SpriteRenderer _currentRenderer;
        private Transform _barrelPivot;
        private Transform _previewRoot;
        private readonly List<SpriteRenderer> _previewRenderers = new();
        private static readonly Color s_barrelStemDefaultColor = new Color(0.55f, 0.58f, 0.66f, 1f);

        // Scratch buffer used by PickColor / OnChainChanged to avoid garbage.
        private readonly List<MarbleColor> _scratchColors = new();

        public Transform BarrelMuzzle => _barrelPivot;

        // Exposed so the LevelController HUD can list active effects + timers.
        public IReadOnlyList<PowerupEffect> EquippedEffects => _equippedEffects;

        // Color currently in the chamber (next-to-fire). Used by Auto Shot's
        // strategy to pick same-color targets.
        public MarbleColor ChamberedColor => _shotQueue.Count > 0 ? _shotQueue[0] : default;

        // When true, player input is suppressed and an equipped effect is
        // driving aim + fire (Auto Shot). The effect calls Fire(dir) directly
        // and rotates BarrelMuzzle each tick.
        private bool _autoControlled;
        public bool IsAutoControlled => _autoControlled;
        public void SetAutoControlled(bool value) => _autoControlled = value;

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

            // Max-1 Shooter rule (Tony 2026-05-21). Picking up a new Shooter-type
            // unequips the current one. Path-type effects stack freely.
            if (def.Kind == PowerupKind.Shooter)
            {
                for (int i = _equippedEffects.Count - 1; i >= 0; i--)
                {
                    if (_equippedEffects[i].Definition != null
                        && _equippedEffects[i].Definition.Kind == PowerupKind.Shooter)
                    {
                        Debug.Log($"[NovaSpark.Cannon] Replacing Shooter '{_equippedEffects[i].Definition.ContentId}' with '{def.ContentId}'.");
                        _equippedEffects[i].Unequip();
                        _equippedEffects.RemoveAt(i);
                    }
                }
            }

            effect.Equip(def, ctx);
            _equippedEffects.Add(effect);
            RecomputeQueueLength();
            RefreshBarrelTint();
            Debug.Log($"[NovaSpark.Cannon] Equipped powerup '{def.ContentId}' (effect '{def.EffectKind}', kind {def.Kind}, duration {def.DurationKind} {def.DurationParam}).");
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
            if (anyRemoved)
            {
                RecomputeQueueLength();
                RefreshBarrelTint();
            }
        }

        // Walks equipped effects; first non-null BarrelTint wins. Used to signal
        // "loaded weapon" effects like Vaporizer (red barrel) where the chambered
        // marble color shouldn't change.
        private void RefreshBarrelTint()
        {
            if (_barrelStemRenderer == null) return;
            Color color = s_barrelStemDefaultColor;
            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                var t = _equippedEffects[i].BarrelTint;
                if (t.HasValue) { color = t.Value; break; }
            }
            _barrelStemRenderer.color = color;
        }

        // -------------------- update / input --------------------

        private void Update()
        {
            if (_level == null) return;
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);

            // Effects tick first so an auto-controlling effect can rotate the
            // barrel and call Fire() during its TickLifetime/OnUpdate. The aim
            // dispatch below picks up whatever rotation it set.
            for (int i = 0; i < _equippedEffects.Count; i++) _equippedEffects[i].TickLifetime(Time.deltaTime);

            Vector2 origin = _barrelPivot != null ? (Vector2)_barrelPivot.position : (Vector2)transform.position;
            Vector2 dir;

            if (_autoControlled)
            {
                // Auto Shot is driving — read aim from the barrel's current rotation.
                // Player input (mouse aim, LMB, swap) is suppressed.
                float zAngle = _barrelPivot.rotation.eulerAngles.z + 90f;
                float rad = zAngle * Mathf.Deg2Rad;
                dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }
            else
            {
                var mouse = Mouse.current;
                if (mouse == null) { PruneExpiredEffects(); return; }

                Vector2 aimWorld = ReadMouseWorld(mouse);
                dir = (aimWorld - (Vector2)transform.position);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                dir.Normalize();
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                _barrelPivot.rotation = Quaternion.Euler(0f, 0f, angle);

                if (_firingEnabled && SwapShotPressed()) RotateShotQueue();

                if (_firingEnabled && mouse.leftButton.wasPressedThisFrame && _cooldownRemaining <= 0f)
                {
                    Fire(dir);
                }
            }

            for (int i = 0; i < _equippedEffects.Count; i++) _equippedEffects[i].OnAim(origin, dir);

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

        // Public so auto-controlling effects (Auto Shot) can fire on their own
        // cadence. Returns true if a projectile was actually launched; false if
        // firing is disabled or cooldown isn't ready yet.
        public bool Fire(Vector2 dir)
        {
            if (!_firingEnabled || _level == null) return false;
            if (_cooldownRemaining > 0f) return false;

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

            // 1b.2d polish (Tony 2026-05-21): PerShots / OneUse charges are
            // consumed on EFFECT ACTIVATION, not on blanket fire. Each handler
            // effect calls NotifyShotFired itself when it actually does
            // something (TryHandleProjectileHit returning true, Mimic's
            // ModifyInsertColor on a real insert). Misses, drop pickups, and
            // path-marble hits no longer waste charges.

            _shotQueue.RemoveAt(0);
            _shotQueue.Add(PickColor());
            RefreshChamberAndPreviewColors();
            return true;
        }

        // Orchestrates a projectile hit: walks effects looking for one that
        // fully handles it; if none does, applies the default insert with
        // possibly-mutated color via ModifyInsertColor; finally fires
        // OnProjectileHit notifications.
        private void ResolveProjectileHit(ChainMarble hit, MarbleColor originalColor, Vector2 velocity)
        {
            if (_chain == null) return;

            // Path-marble bypass — direct hit on a special chain marble activates
            // its Path-kind powerup and destroys the marble. Cannon Shooter
            // effects (Mimic, Explosive, etc.) do NOT run for path-marble hits.
            if (hit != null && hit.IsSpecial
                && hit.SpecialPowerup != null
                && hit.SpecialPowerup.Kind == PowerupKind.Path)
            {
                _chain.OnPathMarbleHit(hit);
                return;
            }

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
            // Equipped effects can override the chambered + preview visuals so
            // the player sees that a non-color-matching effect is loaded
            // (Vaporizer cross, Mimic ring). First non-null override wins.
            Sprite overrideSprite = null;
            Color? overrideColor = null;
            for (int i = 0; i < _equippedEffects.Count; i++)
            {
                if (overrideSprite == null && _equippedEffects[i].ChamberOverrideSprite != null)
                    overrideSprite = _equippedEffects[i].ChamberOverrideSprite;
                if (!overrideColor.HasValue && _equippedEffects[i].ChamberOverrideColor.HasValue)
                    overrideColor = _equippedEffects[i].ChamberOverrideColor;
                if (overrideSprite != null && overrideColor.HasValue) break;
            }

            if (_currentRenderer != null && _shotQueue.Count > 0)
            {
                _currentRenderer.sprite = overrideSprite != null ? overrideSprite : _sprite;
                _currentRenderer.color = overrideColor ?? _shotQueue[0].ToRGB();
            }
            for (int i = 0; i < _previewRenderers.Count; i++)
            {
                int qIdx = i + 1;
                if (qIdx < _shotQueue.Count)
                {
                    _previewRenderers[i].sprite = overrideSprite != null ? overrideSprite : _sprite;
                    _previewRenderers[i].color = overrideColor ?? _shotQueue[qIdx].ToRGB();
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

            _barrelStemRenderer = NewSprite("BarrelStem", _barrelPivot, _sprite, s_barrelStemDefaultColor,
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
