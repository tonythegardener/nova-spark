using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

namespace NovaSpark.Game.Shooter
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        public delegate void HitCallback(ChainMarble hit, MarbleColor color, Vector2 velocity);

        private MarbleColor _color;
        private Rigidbody2D _rb;
        private MarbleChain _chain;
        private HitCallback _onHit;
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
            HitCallback onHit = null,
            float maxLifetime = 4f)
        {
            _color = color;
            _chain = chain;
            _onHit = onHit;
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
            // Guard: Unity can deliver more than one collision event before
            // Destroy(gameObject) takes effect at end-of-frame (penetration into
            // a second collider, multiple contact points on the same marble, or
            // the inserted marble's collider overlapping ours on the next physics
            // step). Without this flag, the projectile could insert twice — the
            // 'ghost marble' bug Tony reported.
            if (_consumed) return;

            var hit = collision.collider.GetComponentInParent<ChainMarble>();
            if (hit == null) return;
            if (_chain == null) { _consumed = true; Destroy(gameObject); return; }

            _consumed = true;
            var velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
            _chain.TryInsertAt(hit, velocity, _color);
            _onHit?.Invoke(hit, _color, velocity);

            // Disable collider immediately so any further physics step this frame
            // can't fire another collision before Destroy lands.
            var col = GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            Destroy(gameObject);
        }
    }
}
