using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using UnityEngine;

namespace NovaSpark.Game.Powerups
{
    // Result of evaluating an Auto Shot target for one candidate firing color.
    //   Quality 0 — no benefit (would graffiti a no-match color into the chain).
    //   Quality 1 — pair-creation (single same-color marble; firing makes a pair).
    //   Quality 2 — match-completion (same-color run of 2+; firing pops 3+).
    public readonly struct AutoShotTarget
    {
        public readonly ChainMarble Marble;
        public readonly int Quality;
        public AutoShotTarget(ChainMarble marble, int quality)
        {
            Marble = marble;
            Quality = quality;
        }
        public static readonly AutoShotTarget None = new AutoShotTarget(null, 0);
        public bool IsBeneficial => Marble != null && Quality > 0;
    }

    // Picks a target for Auto Shot, given the color that will actually be fired.
    // AutoShotEffect evaluates this against every visible queued color and rotates
    // the queue to the slot with the best result.
    public interface IAutoShotStrategy
    {
        AutoShotTarget Evaluate(MarbleChain chain, Cannon cannon, MarbleColor firingColor);
    }

    // Tony's call (2026-05-22, refined 2026-05-23 after testing): every Auto Shot
    // must be beneficial — never insert a no-match color into the chain. If no
    // queue color has a beneficial target, AutoShotEffect holds the shot rather
    // than spraying random colors.
    public sealed class MatchPriorityStrategy : IAutoShotStrategy
    {
        public AutoShotTarget Evaluate(MarbleChain chain, Cannon cannon, MarbleColor firingColor)
        {
            if (chain == null || cannon == null) return AutoShotTarget.None;
            var marbles = chain.Marbles;
            if (marbles.Count == 0) return AutoShotTarget.None;
            Vector2 origin = cannon.transform.position;

            // Pass 1: same-color runs of 2+ (instant-match targets).
            ChainMarble bestRunMember = null;
            float bestRunDist = float.MaxValue;
            int i = 0;
            while (i < marbles.Count)
            {
                if (marbles[i] != null && marbles[i].Color == firingColor)
                {
                    int runStart = i;
                    int runEnd = i;
                    while (runEnd + 1 < marbles.Count
                           && marbles[runEnd + 1] != null
                           && marbles[runEnd + 1].Color == firingColor)
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
            if (bestRunMember != null) return new AutoShotTarget(bestRunMember, 2);

            // Pass 2: any single same-color marble (sets up a future match).
            ChainMarble bestSingle = null;
            float bestSingleDist = float.MaxValue;
            for (int j = 0; j < marbles.Count; j++)
            {
                if (marbles[j] != null && marbles[j].Color == firingColor)
                {
                    float d = ((Vector2)marbles[j].transform.position - origin).sqrMagnitude;
                    if (d < bestSingleDist)
                    {
                        bestSingleDist = d;
                        bestSingle = marbles[j];
                    }
                }
            }
            if (bestSingle != null) return new AutoShotTarget(bestSingle, 1);

            return AutoShotTarget.None;
        }
    }

    // Color-blind nearest strategy. Baseline alternative — picks the closest
    // marble of any color and reports Quality 0 (not beneficial). Kept for
    // comparison and as a shared helper.
    public sealed class NearestStrategy : IAutoShotStrategy
    {
        public AutoShotTarget Evaluate(MarbleChain chain, Cannon cannon, MarbleColor firingColor)
        {
            if (chain == null || cannon == null) return AutoShotTarget.None;
            var pick = PickNearestAnyColor(chain.Marbles, cannon.transform.position);
            return pick != null ? new AutoShotTarget(pick, 0) : AutoShotTarget.None;
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
