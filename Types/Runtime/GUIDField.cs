using System;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace nickeltin.Core.Runtime
{
#if ODIN_INSPECTOR
    [DrawWithUnity, DisableContextMenu()]
#endif
    [Serializable]
    public class GUIDField
    {
        [SerializeField] internal string _guid;
        [SerializeField] internal string _userDefinedId;
        [SerializeField] internal bool _usingUserDefinedId;

        internal bool _hashCached;
        private int _hash;

        public void DirtyHash()
        {
            _hashCached = false;
        }

        public GUIDField()
        {
            _guid = NewGUID();
            _userDefinedId = "NO_ID";
            _usingUserDefinedId = false;
        }

        public GUIDField(GUIDField field)
        {
            this._guid = field._guid;
            this._userDefinedId = field._userDefinedId;
            this._usingUserDefinedId = field._usingUserDefinedId;
        }

        public string Value => _usingUserDefinedId ? _userDefinedId : _guid;

        /// <summary>
        /// Sets mode to used id, and sets its value <paramref name="value"/>
        /// </summary>
        /// <param name="value"></param>
        public void SetUserDefinedID(string value)
        {
            _userDefinedId = value;
            _usingUserDefinedId = true;
            _hashCached = false;
        }


        public override string ToString() => Value;

        /// <summary>
        /// Call this from behaviour Validate method to ensure guid filed state
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(_userDefinedId))
            {
                _userDefinedId = "NO_ID";
                _hashCached = false;
            }

            if (string.IsNullOrEmpty(_guid))
            {
                _guid = NewGUID();
                _hashCached = false;
            }
        }

        internal static string NewGUID() => Guid.NewGuid().ToString();

        public static implicit operator string(GUIDField field) => field.Value;

        /// <summary>
        /// Generates new guid, therefore not determenistic for comparison
        /// </summary>
        public static GUIDField Default => new GUIDField();

        public int GetHashedGUID()
        {
            if (!_hashCached)
            {
                _hashCached = true;
                _hash = StringToHash(Value);
            }
            
            return _hash;
        }
        
        public static int StringToHash(string str) => str.GetHashCode();
    }
}