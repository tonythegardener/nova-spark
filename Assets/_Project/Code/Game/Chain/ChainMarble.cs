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
        private SpriteRenderer _iconRenderer;
        private float _iconPulseTime;

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
            if (specialPowerup != null) BuildSpecialIcon(sortingOrder);
        }

        public void SetColor(MarbleColor color)
        {
            Color = color;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = color.ToRGB();
        }

        // Telegraphs "this marble is a path-effect package — direct hit activates."
        // Icon sits ON the marble (front), pulses scale gently. Per effectKind:
        //   chrono_field -> snowflake
        //   (others fall back to a small white ring placeholder until each gets
        //    its own icon — Tony 2026-05-21)
        private void BuildSpecialIcon(int sortingOrder)
        {
            Sprite icon;
            Color iconColor;
            switch (SpecialPowerup.EffectKind)
            {
                case "chrono_field":
                    icon = SpriteFactory.GetWhiteSnowflake();
                    iconColor = new Color(0.78f, 0.92f, 1.0f, 0.95f); // pale cyan
                    break;
                default:
                    icon = SpriteFactory.GetWhiteCircle();
                    iconColor = new Color(1f, 1f, 1f, 0.55f);
                    break;
            }

            var go = new GameObject("SpecialIcon");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 0.7f;
            _iconRenderer = go.AddComponent<SpriteRenderer>();
            _iconRenderer.sprite = icon;
            _iconRenderer.color = iconColor;
            _iconRenderer.sortingOrder = sortingOrder + 5; // on top of marble
        }

        private void Update()
        {
            if (_iconRenderer == null) return;
            _iconPulseTime += Time.deltaTime;
            float scalePulse = 0.95f + 0.10f * Mathf.Sin(_iconPulseTime * 4f);
            _iconRenderer.transform.localScale = Vector3.one * (0.7f * scalePulse);
        }
    }
}
