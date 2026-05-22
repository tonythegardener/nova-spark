using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

// SpriteFactory lives in NovaSpark.Core; ChamberOverrideSprite returns one of
// its procedural sprites.

namespace NovaSpark.Game.Powerups.Effects
{
    // Vaporizer: destroys the single marble it hits — no insertion, no radius.
    // Surgical contrast to Explosive Round. Magnitude unused.
    public sealed class VaporizerEffect : PowerupEffect
    {
        // Strong red barrel + chambered marble swapped to a red cross so the
        // player sees "destruction shot, color doesn't matter."
        private static readonly Color s_tint = new Color(0.95f, 0.25f, 0.25f, 1f);
        public override Color? BarrelTint => s_tint;
        public override Sprite ChamberOverrideSprite => SpriteFactory.GetWhiteCross();
        public override Color? ChamberOverrideColor => s_tint;

        public override bool TryHandleProjectileHit(ChainMarble hit, MarbleColor color, Vector2 velocity)
        {
            if (Context == null || Context.Chain == null || hit == null) return false;
            Context.Chain.DestroyMarble(hit);
            return true;
        }
    }
}
