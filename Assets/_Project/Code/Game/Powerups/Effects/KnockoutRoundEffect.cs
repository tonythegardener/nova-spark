using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Knockout Round: on impact, dislodges the hit marble from the chain
    // (without destroying it) and immediately re-launches it as a flying marble
    // carrying its own color, in the same velocity vector. If THAT marble hits
    // another chain marble, it inserts normally there; if it flies off-screen,
    // the projectile's lifetime expires and it despawns silently.
    //
    // The secondary projectile is launched with NO resolver, so it can't
    // recurse into Knockout again — one dislodge per shot.
    public sealed class KnockoutRoundEffect : PowerupEffect
    {
        public override bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity)
        {
            if (Context == null || Context.Chain == null || hit == null) return false;

            var dislodgedColor = hit.Color;
            var hitPos = (Vector2)hit.transform.position;

            // Remove from chain WITHOUT firing OnMarbleDestroyed (we're relaunching,
            // not destroying). DislodgeMarble registers a seam so cascades still chain.
            Context.Chain.DislodgeMarble(hit);

            // Spawn a secondary projectile carrying the dislodged color along the
            // original velocity vector. No resolver -> default insert behavior.
            var dir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.up;
            var radius = Context.Level != null ? Context.Level.MarbleRadius : 0.3f;
            var sprite = SpriteFactory.GetWhiteCircle();

            var go = new GameObject($"Dislodged_{dislodgedColor}");
            go.transform.position = hitPos + dir * (radius + 0.05f);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            var p = go.AddComponent<Projectile>();
            p.Launch(dislodgedColor, velocity, radius, sprite, Context.Chain, resolver: null);

            return true;
        }
    }
}
