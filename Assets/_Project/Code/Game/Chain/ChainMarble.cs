using NovaSpark.Core;
using UnityEngine;

namespace NovaSpark.Game.Chain
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ChainMarble : MonoBehaviour
    {
        public MarbleColor Color { get; private set; }
        public float ArcLength { get; set; }

        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Initialize(MarbleColor color, float arcLength, float radius, Sprite sprite, int sortingOrder)
        {
            Color = color;
            ArcLength = arcLength;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.color = color.ToRGB();
            _sr.sortingOrder = sortingOrder;
            transform.localScale = Vector3.one * (radius * 2f);

            var collider = GetComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.useFullKinematicContacts = true;
        }

        public void SetColor(MarbleColor color)
        {
            Color = color;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = color.ToRGB();
        }
    }
}
