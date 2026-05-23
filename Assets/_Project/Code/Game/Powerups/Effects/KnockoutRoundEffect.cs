using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Knockout Round: on impact, dislodges the hit marble from the chain
    // (without destroying it) and immediately re-launches it as a flying marble
    // carrying its own color, in the original shot's direction at boosted speed
    // for a billiard-ball feel. If THAT marble hits another chain marble it
    // inserts normally there; if it flies off-screen, lifetime expires.
    //
    // The secondary projectile is launched with NO resolver, so it can't
    // recurse into Knockout again — one dislodge per shot.
    public sealed class KnockoutRoundEffect : PowerupEffect
    {
        private const float DislodgeSpeedMultiplier = 1.8f;

        private static readonly Color s_amber = new Color(1.0f, 0.78f, 0.30f, 1f);
        public override Color? BarrelTint => s_amber;
        public override Sprite ChamberOverrideSprite => SpriteFactory.GetWhiteArrow();
        public override Color? ChamberOverrideColor => s_amber;

        public override bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity)
        {
            if (Context == null || Context.Chain == null || hit == null) return false;

            var dislodgedColor = hit.Color;
            var hitPos = (Vector2)hit.transform.position;

            // Remove from chain WITHOUT firing OnMarbleDestroyed (we're relaunching,
            // not destroying). DislodgeMarble registers a seam so cascades still chain.
            Context.Chain.DislodgeMarble(hit);

            // Spawn a secondary projectile carrying the dislodged color along the
            // original velocity vector, sped up for snappier billiard feel.
            var dir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.up;
            var radius = Context.Level != null ? Context.Level.MarbleRadius : 0.3f;
            var sprite = SpriteFactory.GetWhiteCircle();
            var dislodgeVelocity = velocity * DislodgeSpeedMultiplier;

            var go = new GameObject($"Dislodged_{dislodgedColor}");
            go.transform.position = hitPos + dir * (radius + 0.05f);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            var p = go.AddComponent<Projectile>();
            p.Launch(dislodgedColor, dislodgeVelocity, radius, sprite, Context.Chain, resolver: null);
            // Grace window so this secondary doesn't insta-collide with the
            // chain marble immediately neighboring the dislodge point. At ~32
            // u/s (player shot 18 * 1.8) we clear ~2.5 world units in 0.08s,
            // safely past any adjacent chain marble.
            p.SetCollisionDelay(0.08f);

            NotifyShotFired();
            return true;
        }
    }
}
