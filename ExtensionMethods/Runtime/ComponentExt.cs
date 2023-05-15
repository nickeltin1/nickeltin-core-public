using UnityEngine;

namespace nickeltin.Core.Runtime
{
    public static class ComponentExt
    {
        /// <summary>
        /// Gets component form <paramref name="component" /> if game isn't running, and component isnt null.
        /// /// Use it in <see cref="MonoBehaviour.OnValidate"/>.
        /// </summary>
        public static void Cache<T>(ref T component, GameObject from) where T : Component
        {
            if (component == null) component = from.GetComponent<T>();
        }
        
        public static void CacheInChildrens<T>(ref T component, GameObject from) where T : Component
        {
            if (component == null) component = from.GetComponentInChildren<T>();
        }
        
        /// <summary>
        /// Gets component form <paramref name="from" /> if component isnt null.
        /// Use it in <see cref="MonoBehaviour.OnValidate"/>.
        /// </summary>
        public static void CacheInChildrens<T>(ref T[] components, GameObject from) where T : Component
        {
            if (components == null || components.Length == 0)
            {
                components = from.GetComponentsInChildren<T>(true);
            }
        }

        public static void CacheInChildrens<T>(this MonoBehaviour behaviour, ref T[] components) where T : Component
        {
            CacheInChildrens(ref components, behaviour.gameObject);
        }
        
        public static void CacheInChildrens<T>(this MonoBehaviour behaviour, ref T component) where T : Component
        {
            CacheInChildrens(ref component, behaviour.gameObject);
        }
        
        public static void Cache<T>(this MonoBehaviour behaviour, ref T component) where T : Component
        {
            Cache(ref component, behaviour.gameObject);
        }

        public static Matrix4x4 GetTRSMatrix(this Transform transform)
        {
            return Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        }
    }
}