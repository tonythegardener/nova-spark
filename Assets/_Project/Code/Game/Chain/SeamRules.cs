using System;
using NovaSpark.Core;

namespace NovaSpark.Game.Chain
{
    // Pure rules for the gap ("seam") left when marbles are removed from the chain.
    // Kept free of Unity objects so the logic can be unit tested directly.
    public static class SeamRules
    {
        // Counts same-color marbles walking outward from both flanks of a seam,
        // including the flanks themselves. Indices are positions in the chain,
        // with lower indices closer to the Bastion.
        public static int CountRunAcrossSeam(
            int marbleCount,
            Func<int, MarbleColor> colorAt,
            int headFlankIdx,
            int tailFlankIdx,
            MarbleColor color)
        {
            int count = 0;
            for (int i = headFlankIdx; i >= 0 && colorAt(i) == color; i--) count++;
            for (int i = tailFlankIdx; i < marbleCount && colorAt(i) == color; i++) count++;
            return count;
        }

        // True if the front segment should hold position because closing the gap
        // wouldn't form a run of at least matchThreshold marbles.
        public static bool ShouldFreeze(
            int marbleCount,
            Func<int, MarbleColor> colorAt,
            int headFlankIdx,
            int tailFlankIdx,
            int matchThreshold)
        {
            var color = colorAt(headFlankIdx);
            if (color != colorAt(tailFlankIdx)) return true;
            return CountRunAcrossSeam(marbleCount, colorAt, headFlankIdx, tailFlankIdx, color) < matchThreshold;
        }
    }
}
