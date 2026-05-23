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
    // Magnitude = seconds between auto-shots (default 0.25).
    public sealed class AutoShotEffect : PowerupEffect
    {
        private const float DefaultIntervalSeconds = 0.25f;
        private static readonly Color s_teal = new Color(0.30f, 0.85f, 0.85f, 1f);

        private IAutoShotStrategy _strategy = new ClosestTargetStrategy();
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

            var target = _strategy.PickTarget(Context.Chain, Context.Cannon);
            if (target == null) return; // chain empty (yet) — wait

            var origin = (Vector2)Context.Cannon.transform.position;
            var dir = ((Vector2)target.transform.position - origin);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            // Rotate barrel toward target (Cannon.Update is in _autoControlled
            // mode so it won't overwrite this with mouse aim).
            if (Context.BarrelMuzzle != null)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Context.BarrelMuzzle.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (Context.Cannon.Fire(dir))
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
