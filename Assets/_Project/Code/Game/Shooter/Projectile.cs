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
        private MarbleColor _color;
        private Rigidbody2D _rb;
        private MarbleChain _chain;
        private float _maxLifetime;
        private float _age;

        public MarbleColor Color => _color;

        public void Launch(
            MarbleColor color,
            Vector2 velocity,
            float radius,
            Sprite sprite,
            MarbleChain chain,
            float maxLifetime = 4f)
        {
            _color = color;
            _chain = chain;
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
            var hit = collision.collider.GetComponentInParent<ChainMarble>();
            if (hit == null) return;
            if (_chain == null) { Destroy(gameObject); return; }

            var velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
            _chain.TryInsertAt(hit, velocity, _color);
            Destroy(gameObject);
        }
    }
}
