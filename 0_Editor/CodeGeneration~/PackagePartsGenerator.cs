using System;
using System.IO;
using System.Text;

namespace nickeltin.CodeGeneration.Editor
{
    internal static class PackagePartsGenerator
    {
        private const string INCLUDE_PLATFORM_KEY = "#INCLUDE_PLATFORM#";
        private const string EXCLUDE_PLATFORM_KEY = "#EXCLUDE_PLATFORM#";
        private const string ASSEMBLY_NAME_KEY = "#ASM_NAME#";
        private const string REFERENCES_KEY = "#REFERENCES#";
        
        private const string ASMDEF =
            "{\n" +
            " \"name\": \"" + ASSEMBLY_NAME_KEY + "\",\n" +
            " \"rootNamespace\": \"\",\n" +
            " \"references\": [\n" +
            " " + REFERENCES_KEY +
            " ],\n" +
            " \"includePlatforms\": [\n" +
            " " + INCLUDE_PLATFORM_KEY +
            " ],\n" +
            " \"excludePlatforms\": [\n" +
            " " + EXCLUDE_PLATFORM_KEY +
            " ],\n" +
            " \"allowUnsafeCode\": false,\n" +
            " \"overrideReferences\": false,\n" +
            " \"precompiledReferences\": [],\n" +
            " \"autoReferenced\": true,\n" +
            " \"defineConstraints\": [],\n" +
            " \"versionDefines\": [],\n" +
            " \"noEngineReferences\": false\n" +
            "}";
        

        private const string NAME_KEY = "#NAME#";
        private const string VERSION_KEY = "#VERSION#";
        private const string DISPLAY_NAME_KEY = "#DISPLAY_NAME#";
        private const string DESCRIPTION_KEY = "#DESCRIPTION#";
        private const string AUTHOR_NAME_KEY = "#AUTHOR#";
        
        private const string PACKAGE_MANIFEST =
            "{\n" +
            " \"name\": \"" + NAME_KEY + "\",\n" +
            " \"version\": \"" + VERSION_KEY + "\",\n" +
            " \"displayName\": \"" + DISPLAY_NAME_KEY + "\",\n" +
            " \"hideInEditor\": false,\n" +
            " \"description\": \"" + DESCRIPTION_KEY + "\",\n" +
            " \"dependencies\": {\n" +
            " }\n," +
            " \"author\":{\n" +
            " \"name\": \"" + AUTHOR_NAME_KEY + "\"\n" +
            " }\n" +
            "}";
        
        private static readonly StringBuilder _stringBuilder;
        
        static PackagePartsGenerator()
        {
            _stringBuilder = new StringBuilder();
        }

        public static void CreateAssemblyDefinition(string path, string[] includePlatforms, string[] excludePlatforms, string[] references)
        {
            includePlatforms ??= Array.Empty<string>();
            excludePlatforms ??= Array.Empty<string>();
            references ??= Array.Empty<string>();
            
            string Join(string[] strs)
            {
                _stringBuilder.Clear();

                for (var i = 0; i < strs.Length; i++)
                {
                    var str = strs[i];
                    if (string.IsNullOrEmpty(str)) continue;
                    _stringBuilder.Append('"');
                    _stringBuilder.Append(str);
                    _stringBuilder.Append('"');
                    if (i < strs.Length - 1)
                    {
                        _stringBuilder.Append(',');
                    }
                    _stringBuilder.AppendLine();
                }

                return _stringBuilder.ToString();
            }

            var content = ASMDEF;
            var name = Path.GetFileNameWithoutExtension(path);
            content = content.Replace(ASSEMBLY_NAME_KEY, name);
            content = content.Replace(INCLUDE_PLATFORM_KEY, Join(includePlatforms));
            content = content.Replace(EXCLUDE_PLATFORM_KEY, Join(excludePlatforms));
            content = content.Replace(REFERENCES_KEY, Join(references));
            File.WriteAllText(path, content);
        }

        public static void CreatePackageManifest(string path, string name, string verision, string displayName, 
            string description, string authorName)
        {
            var content = PACKAGE_MANIFEST;

            content = content.Replace(NAME_KEY, name);
            content = content.Replace(VERSION_KEY, verision);
            content = content.Replace(DISPLAY_NAME_KEY, displayName);
            content = content.Replace(DESCRIPTION_KEY, description);
            content = content.Replace(AUTHOR_NAME_KEY, authorName);
            
            File.WriteAllText(path, content);
        }
    }
}