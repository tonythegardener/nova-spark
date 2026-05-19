using UnityEngine;

namespace NovaSpark.Game.Powerups.Effects
{
    // Magnitude = total laser line length in world units (default ~15).
    public sealed class LaserSightEffect : PowerupEffect
    {
        private GameObject _go;
        private LineRenderer _line;

        protected override void OnEquip()
        {
            _go = new GameObject("PowerupEffect_LaserSight");
            if (Context.BarrelMuzzle != null)
            {
                _go.transform.SetParent(Context.BarrelMuzzle, worldPositionStays: false);
            }
            _line = _go.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = 0.04f;
            _line.endWidth = 0.04f;
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.35f, 0.35f, 0.85f);
            _line.material = mat;
            _line.sortingOrder = 200;
        }

        protected override void OnUnequip()
        {
            if (_go != null) Object.Destroy(_go);
            _go = null;
            _line = null;
        }

        public override void OnAim(Vector2 origin, Vector2 dir)
        {
            if (_line == null) return;
            float length = Definition != null && Definition.Magnitude > 0.1f ? Definition.Magnitude : 15f;
            var end = origin + dir * length;
            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);
        }
    }
}
