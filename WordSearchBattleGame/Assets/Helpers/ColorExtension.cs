using UnityEngine;

namespace Assets.Helpers
{
    public static class ColorExtension
    {
        public static Color ToUnityColor(this System.Drawing.Color color)
            => new(color.R / 255f, color.G / 255f, color.B / 255f);

        public static string ToIntString(this System.Drawing.KnownColor color)
            => ((int)color).ToString();
    }
}
