using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Mimic Shot: the projectile inherits the color of the marble it hits before
    // being inserted. So shooting Red into a Blue cluster inserts a Blue —
    // effectively extending the cluster you hit. Magnitude unused (single mode).
    public sealed class MimicShotEffect : PowerupEffect
    {
        // Hollow ring sprite tinted near-white — signals 'no specific color,
        // takes the color of what it hits.'
        private static readonly Color s_ringColor = new Color(0.92f, 0.92f, 0.96f, 1f);
        private static readonly Color s_barrelTint = new Color(0.65f, 0.75f, 0.90f, 1f);

        public override Color? BarrelTint => s_barrelTint;
        public override Sprite ChamberOverrideSprite => SpriteFactory.GetWhiteRing();
        public override Color? ChamberOverrideColor => s_ringColor;

        public override MarbleColor ModifyInsertColor(ChainMarble hit, MarbleColor color)
        {
            return hit != null ? hit.Color : color;
        }
    }
}
