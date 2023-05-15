using UnityEngine;

namespace nickeltin.Core.Runtime
{
    public static class BoundsExt
    {
        public static bool Encapsulates(this Bounds bounds, Bounds toCheck)
        {
            return bounds.Contains(toCheck.min) && bounds.Contains(toCheck.max);
        }
    }
}