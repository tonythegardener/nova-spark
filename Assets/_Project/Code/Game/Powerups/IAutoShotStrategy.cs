using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Picks which marble Auto Shot aims at when it triggers a shot. Pluggable
    // so strategies can be swapped without touching the effect class.
    public interface IAutoShotStrategy
    {
        ChainMarble PickTarget(MarbleChain chain, Cannon cannon, MarbleColor chamberedColor);
    }

    // Tony's call (2026-05-22): every Auto Shot should be beneficial.
    // 1) Prefer existing same-color (as chambered) runs of 2+ — adding one
    //    instantly completes the match.
    // 2) Else any single same-color marble — sets up a future match.
    // 3) Else fall back to the closest marble of any color so we don't stall.
    public sealed class MatchPriorityStrategy : IAutoShotStrategy
    {
        public ChainMarble PickTarget(MarbleChain chain, Cannon cannon, MarbleColor chamberedColor)
        {
            if (chain == null || cannon == null) return null;
            var marbles = chain.Marbles;
            if (marbles.Count == 0) return null;
            Vector2 origin = cannon.transform.position;

            // Pass 1: same-color runs of 2+ (instant-match targets).
            ChainMarble bestRunMember = null;
            float bestRunDist = float.MaxValue;
            int i = 0;
            while (i < marbles.Count)
            {
                if (marbles[i] != null && marbles[i].Color == chamberedColor)
                {
                    int runStart = i;
                    int runEnd = i;
                    while (runEnd + 1 < marbles.Count
                           && marbles[runEnd + 1] != null
                           && marbles[runEnd + 1].Color == chamberedColor)
                    {
                        runEnd++;
                    }
                    int runLen = runEnd - runStart + 1;
                    if (runLen >= 2)
                    {
                        for (int j = runStart; j <= runEnd; j++)
                        {
                            float d = ((Vector2)marbles[j].transform.position - origin).sqrMagnitude;
                            if (d < bestRunDist)
                            {
                                bestRunDist = d;
                                bestRunMember = marbles[j];
                            }
                        }
                    }
                    i = runEnd + 1;
                }
                else
                {
                    i++;
                }
            }
            if (bestRunMember != null) return bestRunMember;

            // Pass 2: any single same-color marble (sets up a future match).
            ChainMarble bestSingle = null;
            float bestSingleDist = float.MaxValue;
            for (int j = 0; j < marbles.Count; j++)
            {
                if (marbles[j] != null && marbles[j].Color == chamberedColor)
                {
                    float d = ((Vector2)marbles[j].transform.position - origin).sqrMagnitude;
                    if (d < bestSingleDist)
                    {
                        bestSingleDist = d;
                        bestSingle = marbles[j];
                    }
                }
            }
            if (bestSingle != null) return bestSingle;

            // Pass 3: closest of any color (fallback so Auto Shot doesn't stall).
            return NearestStrategy.PickNearestAnyColor(marbles, origin);
        }
    }

    // Color-blind nearest strategy. Kept as a baseline alternative + as the
    // fallback helper for MatchPriorityStrategy.
    public sealed class NearestStrategy : IAutoShotStrategy
    {
        public ChainMarble PickTarget(MarbleChain chain, Cannon cannon, MarbleColor chamberedColor)
        {
            if (chain == null || cannon == null) return null;
            return PickNearestAnyColor(chain.Marbles, cannon.transform.position);
        }

        public static ChainMarble PickNearestAnyColor(System.Collections.Generic.IReadOnlyList<ChainMarble> marbles, Vector2 origin)
        {
            ChainMarble best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < marbles.Count; i++)
            {
                var m = marbles[i];
                if (m == null) continue;
                float d = ((Vector2)m.transform.position - origin).sqrMagnitude;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = m;
                }
            }
            return best;
        }
    }
}
