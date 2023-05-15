using UnityEngine;

namespace nickeltin.CodeGeneration.Editor
{
    internal static class CodeGeneratorUtil
    {
        public const string EditorNamespace = CodeGenerator.NICKELTIN_GENERATED + ".Editor";
         
        public const string RuntimeNamespace = CodeGenerator.NICKELTIN_GENERATED + ".Runtime";
        
        public static string CreateAssetAttribute(string filePath)
        {
            return "[" + nameof(CreateAssetMenuAttribute) + "(" + nameof(CreateAssetMenuAttribute.menuName) + " = \"" + filePath + "\")]";
        }
    }
}