using NovaSpark.Core;
using NovaSpark.Game.Chain;
using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Auto Shot: takes over the cannon for its PerShots duration, auto-aiming
    // at a target chosen by the strategy and firing on a fixed cadence. Player
    // input (mouse aim, LMB fire, swap) is suppressed while active. Returns
    // control to the player when the last charge fires.
    //
    // Each tick we evaluate every visible queued color and pick the (color,
    // target) pair with the highest match quality. If a non-chambered color
    // wins, we rotate the queue to chamber it before firing. If NO queue
    // color has a beneficial target, we hold the shot rather than spray a
    // no-match color into the chain (Tony 2026-05-23).
    //
    // Magnitude = seconds between auto-shots (default 0.25).
    public sealed class AutoShotEffect : PowerupEffect
    {
        private const float DefaultIntervalSeconds = 0.25f;
        private const float HoldRetryLeadSeconds = 0.1f;
        private static readonly Color s_teal = new Color(0.30f, 0.85f, 0.85f, 1f);

        private IAutoShotStrategy _strategy = new MatchPriorityStrategy();
        private float _timeSinceLastShot;

        public override Color? BarrelTint => s_teal;

        protected override void OnEquip()
        {
            if (Context?.Cannon != null) Context.Cannon.SetAutoControlled(true);
            // First shot fires immediately on next OnUpdate (timer starts at interval).
            _timeSinceLastShot = Interval;
        }

        protected override void OnUnequip()
        {
            if (Context?.Cannon != null) Context.Cannon.SetAutoControlled(false);
        }

        public override void OnUpdate(float dt)
        {
            if (Context?.Cannon == null || Context.Chain == null) return;
            _timeSinceLastShot += dt;
            if (_timeSinceLastShot < Interval) return;

            var cannon = Context.Cannon;
            int queueLen = cannon.VisibleShotQueueLength;
            if (queueLen <= 0) return;

            Vector2 origin = cannon.transform.position;
            int bestSlot = -1;
            AutoShotTarget bestTarget = AutoShotTarget.None;
            float bestDistSq = float.MaxValue;

            for (int slot = 0; slot < queueLen; slot++)
            {
                var color = cannon.GetShotQueueColor(slot);
                var candidate = _strategy.Evaluate(Context.Chain, cannon, color);
                if (!candidate.IsBeneficial) continue;

                float distSq = ((Vector2)candidate.Marble.transform.position - origin).sqrMagnitude;
                bool better;
                if (candidate.Quality > bestTarget.Quality) better = true;
                else if (candidate.Quality < bestTarget.Quality) better = false;
                else better = distSq < bestDistSq;

                if (better)
                {
                    bestTarget = candidate;
                    bestSlot = slot;
                    bestDistSq = distSq;
                }
            }

            if (!bestTarget.IsBeneficial)
            {
                // Hold — no beneficial shot available. Retry soon (chain may
                // change between now and full interval).
                _timeSinceLastShot = Mathf.Max(0f, Interval - HoldRetryLeadSeconds);
                return;
            }

            // Rotate queue so the chosen color is chambered before firing.
            for (int r = 0; r < bestSlot; r++) cannon.RotateShotQueue();

            var targetPos = (Vector2)bestTarget.Marble.transform.position;
            var dir = targetPos - origin;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            // Rotate barrel toward target (Cannon.Update is in _autoControlled
            // mode so it won't overwrite this with mouse aim).
            if (Context.BarrelMuzzle != null)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Context.BarrelMuzzle.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (cannon.Fire(dir))
            {
                _timeSinceLastShot = 0f;
                NotifyShotFired();
            }
        }

        private float Interval
        {
            get
            {
                if (Definition != null && Definition.Magnitude > 0.01f) return Definition.Magnitude;
                return DefaultIntervalSeconds;
            }
        }
    }
}
