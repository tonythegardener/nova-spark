using NovaSpark.Core;
using NovaSpark.Game.Chain;

namespace NovaSpark.Game.Powerups.Effects
{
    // Mimic Shot: the projectile inherits the color of the marble it hits before
    // being inserted. So shooting Red into a Blue cluster inserts a Blue —
    // effectively extending the cluster you hit. Magnitude unused (single mode).
    public sealed class MimicShotEffect : PowerupEffect
    {
        public override MarbleColor ModifyInsertColor(ChainMarble hit, MarbleColor color)
        {
            return hit != null ? hit.Color : color;
        }
    }
}
