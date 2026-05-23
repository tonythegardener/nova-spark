using UnityEngine;

namespace NovaSpark.Core
{
    public static class SpriteFactory
    {
        private static Sprite _circleSprite;
        private static Sprite _diamondSprite;
        private static Sprite _snowflakeSprite;
        private static Sprite _crossSprite;
        private static Sprite _ringSprite;
        private static Sprite _arrowSprite;
        private static Sprite _starburstSprite;

        public static Sprite GetWhiteCircle()
        {
            if (_circleSprite != null) return _circleSprite;
            _circleSprite = CreateCircleSprite(64);
            return _circleSprite;
        }

        // 6-fold radial snowflake used as the Chrono Field path-marble icon.
        // Each branch is a thin radial line out to roughly 80% of the radius;
        // antialiased at edges.
        public static Sprite GetWhiteSnowflake()
        {
            if (_snowflakeSprite != null) return _snowflakeSprite;
            _snowflakeSprite = CreateSnowflakeSprite(64);
            return _snowflakeSprite;
        }

        // Bold plus/cross — used as the Vaporizer chambered marker.
        public static Sprite GetWhiteCross()
        {
            if (_crossSprite != null) return _crossSprite;
            _crossSprite = CreateCrossSprite(64);
            return _crossSprite;
        }

        public static Sprite CreateCrossSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            float center = size * 0.5f;
            float armHalfLen = size * 0.40f;
            float armHalfThick = size * 0.11f;
            float edge = 1.0f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x - center + 0.5f);
                    float py = Mathf.Abs(y - center + 0.5f);

                    bool inHoriz = px <= armHalfLen && py <= armHalfThick;
                    bool inVert  = py <= armHalfLen && px <= armHalfThick;
                    byte a = 0;
                    if (inHoriz || inVert)
                    {
                        // Distance to the nearest cross edge (for AA)
                        float dHoriz = Mathf.Min(armHalfLen - px, armHalfThick - py);
                        float dVert  = Mathf.Min(armHalfLen - py, armHalfThick - px);
                        float d = Mathf.Max(dHoriz, dVert);
                        if (d >= edge) a = 255;
                        else a = (byte)(Mathf.Clamp01(d / edge) * 255f);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // Hollow circle outline — used as the Mimic Shot chambered marker
        // ('a loaded shot whose color is determined on impact').
        public static Sprite GetWhiteRing()
        {
            if (_ringSprite != null) return _ringSprite;
            _ringSprite = CreateRingSprite(64, innerRatio: 0.55f);
            return _ringSprite;
        }

        public static Sprite CreateRingSprite(int diameter, float innerRatio)
        {
            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            float r = diameter * 0.5f;
            float outerR = r;
            float innerR = r * innerRatio;
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
                    if (d > outerR || d < innerR - edge) a = 0;
                    else if (d <= outerR - edge && d >= innerR) a = 255;
                    else
                    {
                        float aOuter = Mathf.Clamp01((outerR - d) / edge);
                        float aInner = Mathf.Clamp01((d - innerR + edge) / edge);
                        a = (byte)(Mathf.Min(aOuter, aInner) * 255f);
                    }
                    pixels[y * diameter + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f), diameter);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // Filled rightward triangle / play-button arrow — used as the Knockout
        // Round chambered marker. Rotates with the cannon barrel so it points
        // in the aim direction.
        public static Sprite GetWhiteArrow()
        {
            if (_arrowSprite != null) return _arrowSprite;
            _arrowSprite = CreateArrowSprite(64);
            return _arrowSprite;
        }

        public static Sprite CreateArrowSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            float center = size * 0.5f;
            // Arrow points UP (toward +Y in world space) so it aligns naturally
            // with the cannon barrel's local up-axis (barrel rotates such that
            // its top end points at the aim target).
            float baseY = size * 0.18f;
            float tipY  = size * 0.86f;
            float baseHalfWidth = size * 0.34f;
            float edge = 1.5f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center + 0.5f;
                    float py = y;

                    byte a = 0;
                    if (py >= baseY && py <= tipY)
                    {
                        float taper = (tipY - py) / (tipY - baseY);
                        float allowedHalfWidth = baseHalfWidth * taper;
                        float dist = allowedHalfWidth - Mathf.Abs(px);
                        if (dist >= edge) a = 255;
                        else if (dist > 0f) a = (byte)(Mathf.Clamp01(dist / edge) * 255f);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // Central disc with 8 short radial spikes — used as the Explosive Round
        // chambered marker.
        public static Sprite GetWhiteStarburst()
        {
            if (_starburstSprite != null) return _starburstSprite;
            _starburstSprite = CreateStarburstSprite(64);
            return _starburstSprite;
        }

        public static Sprite CreateStarburstSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            float center = size * 0.5f;
            float innerR = size * 0.20f;          // central disc radius
            float spikeR = size * 0.45f;          // tip radius for spikes
            float spikeHalfThickness = size * 0.07f;
            float edge = 1.0f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    byte a = 0;
                    if (dist <= innerR)
                    {
                        // Solid center disc
                        a = 255;
                    }
                    else if (dist <= spikeR)
                    {
                        // 8 spikes at multiples of 45 degrees
                        float angleDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (angleDeg < 0f) angleDeg += 360f;
                        float spokeMod = angleDeg % 45f;
                        float spokeDist = Mathf.Min(spokeMod, 45f - spokeMod);
                        float perpDist = dist * Mathf.Sin(spokeDist * Mathf.Deg2Rad);

                        if (perpDist <= spikeHalfThickness) a = 255;
                        else if (perpDist < spikeHalfThickness + edge)
                            a = (byte)(Mathf.Clamp01((spikeHalfThickness + edge - perpDist) / edge) * 255f);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static Sprite CreateSnowflakeSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            float center = size * 0.5f;
            float maxRadius = size * 0.45f;
            float thickness = size * 0.05f;
            float edge = 1.0f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center + 0.5f;
                    float py = y - center + 0.5f;
                    float dist = Mathf.Sqrt(px * px + py * py);

                    byte a = 0;
                    if (dist <= maxRadius)
                    {
                        float angleDeg = Mathf.Atan2(py, px) * Mathf.Rad2Deg;
                        if (angleDeg < 0f) angleDeg += 360f;
                        float angleMod = angleDeg % 60f;
                        float angleDist = Mathf.Min(angleMod, 60f - angleMod);
                        float perpDist = dist * Mathf.Sin(angleDist * Mathf.Deg2Rad);

                        if (perpDist <= thickness * 0.5f) a = 255;
                        else if (perpDist >= thickness * 0.5f + edge) a = 0;
                        else a = (byte)(Mathf.Clamp01((thickness * 0.5f + edge - perpDist) / edge) * 255f);

                        // Central node — a small filled circle so the branches join cleanly.
                        if (dist < thickness * 0.9f) a = 255;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // Distinct sprite for powerup drops so the player can tell at a glance
        // it's not a chain marble.
        public static Sprite GetWhiteDiamond()
        {
            if (_diamondSprite != null) return _diamondSprite;
            _diamondSprite = CreateDiamondSprite(64);
            return _diamondSprite;
        }

        public static Sprite CreateDiamondSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            float center = size * 0.5f;
            float halfDiag = size * 0.45f;       // controls diamond size within texture
            float edge = 1.5f;                    // antialiased edge in pixels

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center + 0.5f);
                    float dy = Mathf.Abs(y - center + 0.5f);
                    float manhattan = dx + dy;     // diamond = L1 norm
                    byte a;
                    if (manhattan <= halfDiag - edge) a = 255;
                    else if (manhattan >= halfDiag) a = 0;
                    else a = (byte)(Mathf.Clamp01((halfDiag - manhattan) / edge) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
