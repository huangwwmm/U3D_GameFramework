using UnityEngine;

namespace GF.Common.Utility
{
    public static class ColorUtility
    {
        public static Color ConvertInt32ToColor(int rgba)
        {
            const float INVERT_255 = 1.0f / 255.0f;
            const int FLOAT_TO_COLOR_MASK = 0x000000FF;

            return new Color(((rgba >> 24) & FLOAT_TO_COLOR_MASK) * INVERT_255
                , ((rgba >> 16) & FLOAT_TO_COLOR_MASK) * INVERT_255
                , ((rgba >> 8) & FLOAT_TO_COLOR_MASK) * INVERT_255
                , (rgba & FLOAT_TO_COLOR_MASK) * INVERT_255);
        }

        public static int ConvertColorToInt32(Color rgba)
        {
            return (((int)(rgba.r * 255)) << 24)
                | (((int)(rgba.g * 255)) << 16)
                | (((int)(rgba.b * 255)) << 8)
                | ((int)(rgba.a * 255));
        }
    }
}