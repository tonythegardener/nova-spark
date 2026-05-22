using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Vaporizer: destroys the single marble it hits — no insertion, no radius.
    // Surgical contrast to Explosive Round. Magnitude unused.
    public sealed class VaporizerEffect : PowerupEffect
    {
        // Strong red barrel tint so the player sees "this is going to vaporize"
        // before they fire. Chambered marble color stays accurate.
        private static readonly Color s_tint = new Color(0.95f, 0.25f, 0.25f, 1f);
        public override Color? BarrelTint => s_tint;

        public override bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity)
        {
            if (Context == null || Context.Chain == null || hit == null) return false;
            Context.Chain.DestroyMarble(hit);
            return true;
        }
    }
}
