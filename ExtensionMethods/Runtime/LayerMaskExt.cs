using UnityEngine;

namespace nickeltin.Core.Runtime
{
    public static class LayerMaskExt
    {
        public static bool ContainsLayer(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static int GetSingleLayer(this LayerMask mask)
        {
            return (int) Mathf.Log(mask.value, 2);
        }
    }
}