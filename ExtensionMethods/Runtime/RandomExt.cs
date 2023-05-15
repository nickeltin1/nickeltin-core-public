using System;

namespace nickeltin.Core.Runtime
{
    public static class RandomExt
    {
        public static float NextFloat(this Random random, float min, float max)
        {
            var val = (random.NextDouble() * (max - min) + min);
            return (float)val;
        }

        public static bool NextBool(this Random random)
        {
            return random.Next() > (int.MaxValue / 2);
        }
    }
}