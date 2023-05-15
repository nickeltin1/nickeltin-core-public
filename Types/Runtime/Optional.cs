using System;
using UnityEngine.Serialization;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace nickeltin.Core.Runtime
{
#if ODIN_INSPECTOR
    [DrawWithUnity]
#endif
    [Serializable]
    public class Optional<T>
    {
        [FormerlySerializedAs("enabled")] 
        public bool Enabled;
        
        [FormerlySerializedAs("value")] 
        public T Value;

        public Optional(T value)
        {
            this.Value = value;
            Enabled = true;
        }

        public void IfEnabled(Action<T> action)
        {
            if (Enabled) action.Invoke(Value);
        }
        
        public static implicit operator T(Optional<T> source) => source.Value;
        public static implicit operator bool(Optional<T> source) => source.Enabled;
    }
}