using UnityEngine;

namespace nickeltin.Core.Runtime
{
    public static class ColorExt
    {
        public static Color With(this Color color, float? r = null, float? g = null, float? b = null, float? a = null)
        {
            return new Color(r ?? color.r, g ?? color.g, b ?? color.b, a ?? color.a);
        }

        public static Color Mult(this Color color, float? r = null, float? g = null, float? b = null)
        {
            return new Color(
                r.HasValue ? color.r * (float)r : color.r, 
                g.HasValue ? color.g * (float)g : color.g, 
                b.HasValue ? color.b * (float)b : color.b);
        }

        public static Color Mult(this Color color, float by) => color.Mult(@by, @by, @by);
    }
}