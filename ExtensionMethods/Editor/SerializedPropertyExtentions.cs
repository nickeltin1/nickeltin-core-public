using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    public static class SerializedPropertyExtentions
    {
        public static readonly BindingFlags PublicOrNotInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // public static Type[] GetGenericParametersTypes(this SerializedProperty property)
        // {
        //     return property.GetObjectType().GetGenericArguments();
        // }

        public static Type GetObjectType(this SerializedProperty property, out FieldInfo info) => property.GetObjectType(0, out info);
        
        public static Type GetObjectType(this SerializedProperty property) => property.GetObjectType(0, out _);

        //public static Type GetParentType(this SerializedProperty property) => property.GetObjectType(1);
        
        ///TODO: remove in future
        /// <param name="depth">use 0 if you want to get object itself object</param>
        /// Does not work with <see cref="SerializeReference"/>, simply return typeof(Object)
        /// <returns></returns>
        public static Type GetObjectType(this SerializedProperty property, int depth, out FieldInfo info)
        {
            // Debug.Log(property.type);
            // Debug.Log(property.propertyType == SerializedPropertyType.Generic);
            // return typeof(Object);
            var parentType = property.serializedObject.targetObject.GetType();
            var path = property.GetFullPropertyPath();
            Type type = null;
            info = null;
            for (var i = 0; i < path.Length - depth; i++)
            {
                FieldInfo fieldInfo = null;
                var name = path[i];
                // Debug.Log(name);
                FormatArrayNaming(ref name);
                fieldInfo = GetFieldInfo(name, parentType);
                // Debug.Log(parentType);
                while (fieldInfo == null && parentType.BaseType != null)
                {
                    parentType = parentType.BaseType;
                    fieldInfo = GetFieldInfo(name, parentType);
                }
                // Debug.Log(fieldInfo?.FieldType);
                if (fieldInfo == null)
                {
                    info = null;
                    return null;
                }
                type = fieldInfo.FieldType;
                if (type.IsArray) type = type.GetElementType();
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments().First();
                }
                parentType = type;
                info = fieldInfo;
            }
            
            return type;
        }
        
        public static object GetObjectValue(this SerializedProperty property)
        {
            var parentType = property.serializedObject.targetObject.GetType();
            object currentObject = property.serializedObject.targetObject;
            var path = property.GetFullPropertyPath();
            for (var i = 0; i < path.Length; i++)
            {
                var nonFormatedName = path[i];
                var name = nonFormatedName;
                FormatArrayNaming(ref name);
                FieldInfo fieldInfo = null;
                fieldInfo = GetFieldInfo(name, parentType);
                while (fieldInfo == null && parentType.BaseType != null)
                {
                    parentType = parentType.BaseType;
                    fieldInfo = GetFieldInfo(name, parentType);
                }
                var type = fieldInfo.FieldType;
                if (type.IsArray)
                {
                    currentObject = ((Array)fieldInfo.GetValue(currentObject)).GetValue(GetIndexFromArray(nonFormatedName));
                    type = type.GetElementType();
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    currentObject = ((IList)fieldInfo.GetValue(currentObject))[GetIndexFromArray(nonFormatedName)];
                    type = type.GetGenericArguments().First();
                }
                else
                {
                    currentObject = fieldInfo.GetValue(currentObject);
                }

                parentType = type;
            }
            return currentObject;
        }

        public static string[] GetFullPropertyPath(this SerializedProperty property)
        {
            return property.propertyPath.Replace(".Array.data[", "[").Split('.');
        }

        private static FieldInfo GetFieldInfo(string fieldName, Type from)
        {
            return from.GetField(fieldName, PublicOrNotInstance);
        }

        private static void FormatArrayNaming(ref string name)
        {
            if (name.Contains("[")) name = name.Substring(0, name.IndexOf("["));
        }

        private static int GetIndexFromArray(string propName)
        {
            int i = propName.IndexOf("[") + 1;
            if (i >= 0)
            {
                var s = propName.Substring(i,propName.IndexOf("]") - i);
                return int.Parse(s);
            }

            return -1;
        }
        

        public static IEnumerable<SerializedProperty> GetVisibleChilds(this SerializedProperty property)
        {
            var currentProperty = property.Copy();
            var nextSiblingProperty = property.Copy();
            {
                nextSiblingProperty.NextVisible(false);
            }
 
            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty;
                }
                while (currentProperty.NextVisible(false));
            }
        }

        public static GUIContent GetContent(this SerializedProperty property)
        {
            return EditorGUIUtility.TrTextContent(property.displayName, property.tooltip);
        }

        #region Internal Reflections

        private static readonly Type ScriptAttributeUtility;

        private delegate FieldInfo PropertyGetFieldInfoAndTypeDelegate(SerializedProperty property, out Type type);

        private static readonly PropertyGetFieldInfoAndTypeDelegate _getFieldInfoAndStaticTypeFromProperty;
        private static readonly PropertyGetFieldInfoAndTypeDelegate _getFieldInfoFromProperty;
        
        static SerializedPropertyExtentions()
        {
            ScriptAttributeUtility = Type.GetType("UnityEditor.ScriptAttributeUtility,UnityEditor.dll");
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

        public static FieldInfo GetFieldInfoAndStaticTypeFromProperty(this SerializedProperty property, out Type type)
        {
            return _getFieldInfoAndStaticTypeFromProperty(property, out type);
        }
        
        public static FieldInfo GetFieldInfoFromProperty(this SerializedProperty property, out Type type)
        {
            return _getFieldInfoFromProperty(property, out type);
        }

        #endregion
    }
}