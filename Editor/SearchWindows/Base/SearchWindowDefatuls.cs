using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    internal static class SearchWindowDefatuls
    {
        public static readonly Texture2D noneIcon;
        public static readonly GUIContent noneContnet;
        public static readonly GUIContent noItemsFoundContent;
            
        static SearchWindowDefatuls()
        {
            noItemsFoundContent = new GUIContent("List is empty");
            noneIcon = (Texture2D)EditorGUIUtility.IconContent("Invalid").image;
            noneContnet = new GUIContent("<none>", noneIcon);
        }
    }
}