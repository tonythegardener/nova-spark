using NovaSpark.Core;
using NovaSpark.Powerups;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // World object representing an unequipped powerup waiting to be collected.
    // Player shoots their chambered marble at it; on collision the projectile
    // is consumed and the powerup is equipped on the cannon.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PowerupDrop : MonoBehaviour
    {
        private PowerupDefinition _definition;
        private System.Action<PowerupDefinition> _onCollected;
        private System.Action<PowerupDrop> _onExpired;
        private float _lifetimeRemaining;
        private bool _collected;
        private SpriteRenderer _sr;
        private float _spinRate = 60f; // deg/sec
        private Color _baseColor;
        private float _pulseTime;

        public PowerupDefinition Definition => _definition;

        public void Configure(
            PowerupDefinition def,
            float lifetimeSeconds,
            float radius,
            Color tint,
            System.Action<PowerupDefinition> onCollected,
            System.Action<PowerupDrop> onExpired)
        {
            _definition = def;
            _lifetimeRemaining = lifetimeSeconds;
            _onCollected = onCollected;
            _onExpired = onExpired;
            _baseColor = tint;

            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = SpriteFactory.GetWhiteDiamond();
            _sr.color = _baseColor;
            _sr.sortingOrder = 40;

            var col = GetComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = false;

            transform.localScale = Vector3.one * (radius * 2f);
        }

        private void Update()
        {
            // Spin + pulse so the drop reads as different from chain marbles.
            transform.Rotate(0f, 0f, _spinRate * Time.deltaTime);
            _pulseTime += Time.deltaTime;
            if (_sr != null)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(_pulseTime * 4f);
                _sr.color = Color.Lerp(_baseColor, Color.white, t * 0.45f);
            }

            _lifetimeRemaining -= Time.deltaTime;
            if (_lifetimeRemaining <= 0f && !_collected) Expire();
        }

        // Called by Projectile when it collides with this drop.
        public void OnHitByProjectile()
        {
            if (_collected) return;
            _collected = true;
            _onCollected?.Invoke(_definition);
            Destroy(gameObject);
        }

        private void Expire()
        {
            if (_collected) return;
            _collected = true;
            _onExpired?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
