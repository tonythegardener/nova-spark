using NovaSpark.Core;
using NovaSpark.Powerups;
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

        // Set by MarbleChain when a destruction creates a gap that would NOT
        // form a same-color run if the front retreated. Frozen marbles refuse
        // to retreat in AdvanceChain — the back catches up instead, leaving a
        // gap the player can shoot through. Cleared automatically when the
        // marble's current arc reaches its natural position (gap closed).
        public bool FrozenAfterGap { get; set; }

        // If non-null, this marble is a path-effect "supply package" — when a
        // player projectile hits it, the effect activates and the marble is
        // destroyed without a normal insert. Direct hit only — collateral
        // destruction (matches, explosive radius) does NOT activate.
        public PowerupDefinition SpecialPowerup { get; private set; }
        public bool IsSpecial => SpecialPowerup != null;

        private SpriteRenderer _sr;
        private SpriteRenderer _haloRenderer;
        private float _haloPulseTime;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Initialize(
            MarbleColor color,
            float arcLength,
            float radius,
            Sprite sprite,
            int sortingOrder,
            PowerupDefinition specialPowerup = null)
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

            SpecialPowerup = specialPowerup;
            if (specialPowerup != null) BuildHalo(sprite, sortingOrder);
        }

        public void SetColor(MarbleColor color)
        {
            Color = color;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = color.ToRGB();
        }

        // Halo telegraphs "this marble is a path-effect package — direct hit
        // activates." A larger semi-transparent ring behind the marble, pulsing.
        private void BuildHalo(Sprite sprite, int sortingOrder)
        {
            var go = new GameObject("Halo");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 1.8f;
            _haloRenderer = go.AddComponent<SpriteRenderer>();
            _haloRenderer.sprite = sprite;
            _haloRenderer.color = new Color(1f, 1f, 1f, 0.35f);
            _haloRenderer.sortingOrder = sortingOrder - 1;
        }

        private void Update()
        {
            if (_haloRenderer == null) return;
            _haloPulseTime += Time.deltaTime;
            float pulse = 0.30f + 0.18f * Mathf.Sin(_haloPulseTime * 5f);
            var c = _haloRenderer.color;
            c.a = pulse;
            _haloRenderer.color = c;
        }
    }
}
