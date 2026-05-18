using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Level;
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

        private MarbleColor _currentColor;
        private MarbleColor _nextColor;
        private float _cooldownRemaining;
        private bool _firingEnabled;

        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _currentRenderer;
        private Transform _barrelPivot;
        private SpriteRenderer _nextPreviewRenderer;

        public void Configure(LevelDefinition level, MarbleChain chain, Camera cam, Sprite sprite)
        {
            _level = level;
            _chain = chain;
            _camera = cam;
            _sprite = sprite;
            BuildVisuals();
            _currentColor = RandomColor();
            _nextColor = RandomColor();
            RefreshChamberColors();
            _firingEnabled = true;
        }

        public void SetFiringEnabled(bool enabled) => _firingEnabled = enabled;

        private void Update()
        {
            if (_level == null) return;
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 aimWorld = ReadMouseWorld(mouse);
            Vector2 dir = (aimWorld - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            _barrelPivot.rotation = Quaternion.Euler(0f, 0f, angle);

            if (_firingEnabled && mouse.leftButton.wasPressedThisFrame && _cooldownRemaining <= 0f)
            {
                Fire(dir);
            }
        }

        private Vector2 ReadMouseWorld(Mouse mouse)
        {
            var screen = mouse.position.ReadValue();
            var v = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return new Vector2(v.x, v.y);
        }

        private void Fire(Vector2 dir)
        {
            _cooldownRemaining = _level.ShooterCooldown;

            var muzzle = (Vector2)_barrelPivot.position + dir * (_level.ProjectileRadius + 0.05f);
            var go = new GameObject($"Projectile_{_currentColor}");
            go.transform.position = muzzle;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            var p = go.AddComponent<Projectile>();
            p.Launch(_currentColor, dir * _level.ShotVelocity, _level.ProjectileRadius, _sprite, _chain);

            _currentColor = _nextColor;
            _nextColor = RandomColor();
            RefreshChamberColors();
        }

        private MarbleColor RandomColor()
        {
            return _level.AllowedColors[Random.Range(0, _level.AllowedColors.Length)];
        }

        private void RefreshChamberColors()
        {
            _currentRenderer.color = _currentColor.ToRGB();
            _nextPreviewRenderer.color = _nextColor.ToRGB();
        }

        private void BuildVisuals()
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

            var preview = new GameObject("NextPreview");
            preview.transform.SetParent(transform, false);
            preview.transform.localPosition = new Vector3(-1.1f, -0.1f, 0f);
            _nextPreviewRenderer = NewSprite("NextSprite", preview.transform, _sprite, Color.white,
                scale: _level.ProjectileRadius * 1.4f, sortingOrder: 55);
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
