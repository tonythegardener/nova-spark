using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Vaporizer: destroys the single marble it hits — no insertion, no radius.
    // Surgical contrast to Explosive Round. Magnitude unused.
    public sealed class VaporizerEffect : PowerupEffect
    {
        public override bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity)
        {
            if (Context == null || Context.Chain == null || hit == null) return false;
            Context.Chain.DestroyMarble(hit);
            return true;
        }
    }
}
