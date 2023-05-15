using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Runtime
{
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver
    {
        [SerializeField] internal string _type;
        [SerializeField] internal string _assembly;

        private Type _cachedType;
        
        internal bool _isDirty = true;

        private SerializableType(Type type) => Set(type);

        private SerializableType Set(Type t)
        {
            _assembly = t.Assembly.FullName;
            _type = t.FullName;
            _isDirty = true;
            return this;
        }

        public Type Get()
        {
            if (_isDirty)
            {
                _cachedType = Type.GetType(_type + "," + _assembly);
                _isDirty = false;
            }
            return _cachedType;
        }
        
        
        public static SerializableType Component => new SerializableType(typeof(Component));
        public static SerializableType ScriptableObject => new SerializableType(typeof(ScriptableObject));
        public static SerializableType Object => new SerializableType(typeof(Object));

        public string TypeString => _type;

        public string AssemblyString => _assembly;

        public static implicit operator Type(SerializableType source) => source.Get();

        public void OnBeforeSerialize() => _isDirty = true;

        public void OnAfterDeserialize() { }

        public override string ToString()
        {
            return nameof(SerializableType) + " asm " + _assembly + " t " + _type;
        }
    }
}