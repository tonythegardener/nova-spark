using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Powerups;
using UnityEngine;

namespace NovaSpark.Game.Shooter
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        // The resolver, when non-null, is given full responsibility for what
        // happens on impact (insert, destroy radius, dislodge, etc.). When null,
        // the projectile falls back to the chain's default insertion behavior —
        // used by powerup effects like Knockout Round that re-launch a secondary
        // projectile and don't want it to recurse through the cannon's effects.
        public delegate void HitResolver(ChainMarble hit, MarbleColor color, Vector2 velocity);

        private MarbleColor _color;
        private Rigidbody2D _rb;
        private MarbleChain _chain;
        private HitResolver _resolver;
        private float _maxLifetime;
        private float _age;
        private bool _consumed;

        public MarbleColor Color => _color;

        public void Launch(
            MarbleColor color,
            Vector2 velocity,
            float radius,
            Sprite sprite,
            MarbleChain chain,
            HitResolver resolver = null,
            float maxLifetime = 4f)
        {
            _color = color;
            _chain = chain;
            _resolver = resolver;
            _maxLifetime = maxLifetime;

            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.linearVelocity = velocity;

            var col = GetComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = false;

            var sr = GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color.ToRGB();
            sr.sortingOrder = 1000;

            transform.localScale = Vector3.one * (radius * 2f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _maxLifetime) Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // _consumed guard — Unity can deliver two collision events before
            // Destroy(gameObject) takes effect at end-of-frame.
            if (_consumed) return;

            // Powerup drop has priority — if the projectile hits one, that
            // consumes the shot. No chain insertion happens even if a chain
            // marble is also part of the collision.
            var drop = collision.collider.GetComponentInParent<PowerupDrop>();
            if (drop != null)
            {
                _consumed = true;
                drop.OnHitByProjectile();
                DisableAndDestroy();
                return;
            }

            var hit = collision.collider.GetComponentInParent<ChainMarble>();
            if (hit == null) return;

            _consumed = true;
            var velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;

            if (_resolver != null)
            {
                _resolver.Invoke(hit, _color, velocity);
            }
            else if (_chain != null)
            {
                _chain.TryInsertAt(hit, velocity, _color);
            }

            DisableAndDestroy();
        }

        private void DisableAndDestroy()
        {
            // Disable collider + zero velocity so no further physics step this
            // frame can re-fire collision before Destroy() lands.
            var col = GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            Destroy(gameObject);
        }
    }
}
