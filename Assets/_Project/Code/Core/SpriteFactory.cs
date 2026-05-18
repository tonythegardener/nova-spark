using UnityEngine;

namespace NovaSpark.Core
{
    public static class SpriteFactory
    {
        private static Sprite _circleSprite;

        public static Sprite GetWhiteCircle()
        {
            if (_circleSprite != null) return _circleSprite;
            _circleSprite = CreateCircleSprite(64);
            return _circleSprite;
        }

        public static Sprite CreateCircleSprite(int diameter)
        {
            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            float r = diameter * 0.5f;
            float rSquared = r * r;
            float edge = 1.5f;

            var pixels = new Color32[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a;
                    if (d <= r - edge) a = 255;
                    else if (d >= r) a = 0;
                    else a = (byte)(Mathf.Clamp01((r - d) / edge) * 255f);
                    pixels[y * diameter + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f),
                diameter);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
