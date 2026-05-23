using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Explosive Round: on impact, destroys the hit marble and every other
    // marble within `magnitude` arc-radius of the hit position. No insertion;
    // projectile is consumed. Each contiguous destroyed range registers a
    // pending seam so post-blast cascades still chain.
    // Magnitude = explosion radius in world units (default 1.2).
    public sealed class ExplosiveRoundEffect : PowerupEffect
    {
        private static readonly Color s_orange = new Color(1.0f, 0.55f, 0.15f, 1f);
        public override Color? BarrelTint => s_orange;
        public override Sprite ChamberOverrideSprite => SpriteFactory.GetWhiteStarburst();
        public override Color? ChamberOverrideColor => s_orange;

        public override bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity)
        {
            if (Context == null || Context.Chain == null || hit == null) return false;
            float radius = Definition != null && Definition.Magnitude > 0.05f ? Definition.Magnitude : 1.2f;
            Vector2 center = hit.transform.position;
            Context.Chain.DestroyInRadius(center, radius);
            NotifyShotFired();
            return true;
        }
    }
}
