using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    public static class SerializedPropertyExtentions
    {
        #region Other

        public static IEnumerable<SerializedProperty> GetVisibleChilds(this SerializedProperty property)
        {
            var currentProperty = property.Copy();
            var nextSiblingProperty = property.Copy();
            {
                nextSiblingProperty.NextVisible(false);
            }

            if (currentProperty.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty;
                } while (currentProperty.NextVisible(false));
        }
        

        #endregion

        #region Property Type Access

        private static readonly Type ScriptAttributeUtility;
        
        private delegate FieldInfo PropertyGetFieldInfoAndTypeDelegate(SerializedProperty property, out Type type);
        
        private static readonly PropertyGetFieldInfoAndTypeDelegate _getFieldInfoAndStaticTypeFromProperty;
        private static readonly PropertyGetFieldInfoAndTypeDelegate _getFieldInfoFromProperty;
        
        static SerializedPropertyExtentions()
        {
            ScriptAttributeUtility = Type.GetType("UnityEditor.ScriptAttributeUtility, UnityEditor.dll");
            if (ScriptAttributeUtility == null)
            {
                throw new Exception("ScriptAttributeUtility not found");
            }
            
            _getFieldInfoAndStaticTypeFromProperty = CacheMethod<PropertyGetFieldInfoAndTypeDelegate>("GetFieldInfoAndStaticTypeFromProperty");
            _getFieldInfoFromProperty = CacheMethod<PropertyGetFieldInfoAndTypeDelegate>("GetFieldInfoFromProperty");
        }
        
        private static void ValidateReflectedMember(MemberInfo memberInfo, string name, Type type)
        {
            if (memberInfo == null)
            {
                throw new Exception($"Can't reflect {name} member in {type}, unity might have changed it");
            }
        }
        
        private static T CacheMethod<T>(string methodName) where T : Delegate
        {
            var methodInfo = ScriptAttributeUtility.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            ValidateReflectedMember(methodInfo, methodName, ScriptAttributeUtility);
            return (T)Delegate.CreateDelegate(typeof(T), methodInfo!);
        }
        
        /// <summary>
        /// Unity built-in internal function.
        /// Static type don't inclued <see cref="SerializeReference"/> fields, and will return their base types.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfoAndStaticTypeFromProperty(this SerializedProperty property, out Type type)
        {
            return _getFieldInfoAndStaticTypeFromProperty(property, out type);
        }

        /// <summary>
        /// Same as <see cref="GetFieldInfoAndElementStaticTypeFromProperty"/> but in case of type being <see cref="Array"/> or <see cref="List{T}"/>
        /// will return element type instead.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfoAndElementStaticTypeFromProperty(this SerializedProperty property,
            out Type type)
        {
            var fieldInfo = property.GetFieldInfoAndStaticTypeFromProperty(out type);
            type = GetElementType(type);
            return fieldInfo;
        }

        /// <summary>
        /// Unity built-in internal function.
        /// Will inclued <see cref="SerializeReference"/> types.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfoAndTypeFromProperty(this SerializedProperty property, out Type type)
        {
            return _getFieldInfoFromProperty(property, out type);
        }
        
        /// <summary>
        /// Same as <see cref="GetFieldInfoAndTypeFromProperty"/> but in case of type being <see cref="Array"/> or <see cref="List{T}"/>
        /// will return element type instead.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfoAndElementTypeFromProperty(this SerializedProperty property, out Type type)
        {
            var fieldInfo = property.GetFieldInfoAndTypeFromProperty(out type);
            type = GetElementType(type);
            return fieldInfo;
        }

        /// <summary>
        /// If type <see cref="Array"/> or <see cref="List{T}"/> will return elment type, else 
        /// </summary>
        /// <returns></returns>
        private static Type GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                var genericArgument = type.GetGenericArguments().FirstOrDefault();
                return genericArgument ?? typeof(object);
            }

            return type;
        }


        #endregion

        #region Property Value Access
        
        /// <summary>
        /// Gets boxed value of property with support of Nested Types, Arrays, Lists, SerializedReference
        /// For Unity 2022 used built-in efficient property 'boxedValue' 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static object GetValue(this SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data", "");
            var fieldStructure = path.Split('.');
            for (var i = 0; i < fieldStructure.Length; i++)
            {
                if (fieldStructure[i].Contains("["))
                {
                    var index = Convert.ToInt32(new string(fieldStructure[i].Where(char.IsDigit).ToArray()));
                    obj = GetFieldValueWithIndex(RGX.Replace(fieldStructure[i], ""), obj, index);
                }
                else
                {
                    obj = GetFieldValue(fieldStructure[i], obj);
                }
            }

            return obj;
        }

        /// <summary>
        /// Sets boxed value of property with support of Nested Types, Arrays, Lists, SerializedReference
        /// For Unity 2022 used built-in efficient property 'boxedValue' 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetValue(this SerializedProperty property, object value)
        {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data", "");
            var fieldStructure = path.Split('.');
            for (var i = 0; i < fieldStructure.Length - 1; i++)
            {
                if (fieldStructure[i].Contains("["))
                {
                    var index = Convert.ToInt32(new string(fieldStructure[i].Where(char.IsDigit)
                        .ToArray()));
                    obj = GetFieldValueWithIndex(RGX.Replace(fieldStructure[i], ""), obj, index);
                }
                else
                {
                    obj = GetFieldValue(fieldStructure[i], obj);
                }
            }

            var fieldName = fieldStructure.Last();
            if (fieldName.Contains("["))
            {
                var index = Convert.ToInt32(new string(fieldName.Where(char.IsDigit).ToArray()));
                return SetFieldValueWithIndex(RGX.Replace(fieldName, ""), obj, index, value);
            }

            return SetFieldValue(fieldName, obj, value);
        }
        
        private static readonly Regex RGX = new(@"\[\d+\]", RegexOptions.Compiled);

        private const BindingFlags DEFAULT_BINDING_FLAGS =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic;

        private static object GetFieldValue(string fieldName, object obj, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            return field != null ? field.GetValue(obj) : default;
        }

        private static object GetFieldValueWithIndex(string fieldName, object obj, int index, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field == null) return default;
            var list = field.GetValue(obj);
            if (list.GetType().IsArray)
            {
                return ((Array)list).GetValue(index);
            }
                
            if (list is IList castedList)
            {
                return castedList[index];
            }

            return default;
        }

        private static bool SetFieldValue(string fieldName, object obj, object value, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field == null) return false;
            field.SetValue(obj, value);
            return true;

        }

        private static bool SetFieldValueWithIndex(string fieldName, object obj, int index, object value, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field == null) return false;
            var list = field.GetValue(obj);
            if (list.GetType().IsArray)
            {
                ((object[])list)[index] = value;
                return true;
            }

            if (list is IList castedList)
            {
                castedList[index] = value;
                return true;
            }

            return false;
        }
        
        #endregion
    }
}