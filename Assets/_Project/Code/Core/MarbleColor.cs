using UnityEngine;

namespace NovaSpark.Core
{
    public enum MarbleColor
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3
    }

    public static class MarbleColors
    {
        public static readonly MarbleColor[] All =
        {
            MarbleColor.Red,
            MarbleColor.Blue,
            MarbleColor.Green,
            MarbleColor.Yellow
        };

        public static Color ToRGB(this MarbleColor c)
        {
            switch (c)
            {
                case MarbleColor.Red:    return new Color(0.93f, 0.27f, 0.27f);
                case MarbleColor.Blue:   return new Color(0.27f, 0.55f, 0.93f);
                case MarbleColor.Green:  return new Color(0.30f, 0.78f, 0.36f);
                case MarbleColor.Yellow: return new Color(0.95f, 0.83f, 0.27f);
                default: return Color.magenta;
            }
        }
    }
}
