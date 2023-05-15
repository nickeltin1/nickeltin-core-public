using UnityEngine;

namespace nickeltin.Core.Runtime
{
    public static class SingleExt
    {
        /// <param name="x">root to be extracted form</param>
        /// <param name="n">root power, clamped to minmum of 1</param>
        public static float Root(this float x, int n)
        {
            n = Mathf.Clamp(n, 1, int.MaxValue);
            return Mathf.Pow(x, 1.0f / n);
        }

        public static float Clamp(this ref float x, float min, float max) => x = Mathf.Clamp(x, min, max);
        public static float ClampNoRef(this float x, float min, float max) => Mathf.Clamp(x, min, max);
        
        public static float Clamp01(this ref float x) => x = Mathf.Clamp01(x);
    }
}