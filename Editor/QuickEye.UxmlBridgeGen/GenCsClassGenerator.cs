using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static QuickEye.UxmlBridgeGen.ScriptTemplateUtility;

namespace QuickEye.UxmlBridgeGen
{
    internal static class GenCsClassGenerator
    {
        private const string ColumnFullName = "UnityEngine.UIElements.Column";
        private const string GenCsScriptTemplatePath = "QuickEye/GenCsScriptTemplate";
        private const string CsScriptTemplatePath = "QuickEye/CsScriptTemplate";
        private const string GenCsLightModeIconGuid = "41141330b8b824474bcbbf2f99e848e2";
        private const string UxmlConventionalSuffix = ".view";

        private static readonly string[] _IgnoredTagFullNames =
        {
            "UnityEngine.UIElements.Template",
            "Style"
        };

        public static void GenerateCs(string uxmlFilePath, bool pingAsset)
        {
            var csFilePath = uxmlFilePath.Replace(".uxml", ".cs");
            if (File.Exists(csFilePath))
            {
                Debug.LogWarning($"Cannot generate file, it already exists: {csFilePath}");
                return;
            }

            var inlineSettings = InlineSettings.FromXmlFile(uxmlFilePath);
            var codeStyle = GetFinalCodeStyleRulesFor(inlineSettings);
            var className = Path.GetFileNameWithoutExtension(uxmlFilePath);
            var classNamespace = CsNamespaceUtils.GetCsNamespace(uxmlFilePath, out _);
            var fileContent = Resources.Load<TextAsset>(CsScriptTemplatePath).text;
            fileContent = ReplaceClassNameTag(fileContent, codeStyle.className.Apply(className));
            fileContent = ReplaceNamespaceTags(fileContent, classNamespace);

            File.WriteAllText(csFilePath, fileContent);
            AssetDatabase.ImportAsset(csFilePath);
            if (pingAsset)
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(csFilePath));
        }

        public static void GenerateGenCs(string uxmlFilePath, bool pingAsset)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlFilePath);
            var uxml = File.ReadAllText(uxmlFilePath);
            var inlineSettings = InlineSettings.FromXml(uxml);
            var codeStyleRules = GetFinalCodeStyleRulesFor(inlineSettings);

            if (!UxmlParser.TryGetElementsWithName(uxml, out var elements, out var styles))
                return;

            var validElements = elements
                .Where(e => !_IgnoredTagFullNames.Contains(e.FullyQualifiedTypeName))
                .ToArray();
            if (!inlineSettings.TryGetGenCsFilePath(out var genCsFilePath, out _))
            {
                genCsFilePath = GetDefaultGenCsFilePath(uxmlFilePath);
            }

            //TODO: add class name as inline setting?
            // var className = uxmlAsset.name.EndsWith(UxmlConventionalSuffix)
            //     ? uxmlAsset.name.Substring(0, uxmlAsset.name.Length - UxmlConventionalSuffix.Length)
            //     : uxmlAsset.name;
            var newScriptContent = CreateScriptContent(
                uxmlAsset.name,
                CsNamespaceUtils.GetCsNamespace(uxmlFilePath, out _),
                validElements,
                styles,
                codeStyleRules);

            if (File.Exists(genCsFilePath) &&
                !IsEqualWithoutComments(File.ReadAllText(genCsFilePath), newScriptContent))
                return;

            File.WriteAllText(genCsFilePath, newScriptContent);
            AssetDatabase.ImportAsset(genCsFilePath);
            TryUpdateGenCsGuid(uxmlFilePath, genCsFilePath, inlineSettings);
            SetIcon(genCsFilePath);
            if (pingAsset)
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(genCsFilePath));
        }

        private static void TryUpdateGenCsGuid(string uxmlFilePath, string genCsFilePath, InlineSettings inlineSettings)
        {
            var genCsGuid = AssetDatabase.AssetPathToGUID(genCsFilePath);
            if (inlineSettings.GenCsGuid != genCsGuid)
            {
                inlineSettings.GenCsGuid = genCsGuid;
                inlineSettings.WriteTo(uxmlFilePath);
            }
        }

        private static void SetIcon(string path)
        {
            var monoImporter = AssetImporter.GetAtPath(path) as MonoImporter;
            if (monoImporter == null)
                return;

#if UNITY_2021_1_OR_NEWER
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(GenCsLightModeIconGuid));
            monoImporter.SetIcon(icon);
#endif
            monoImporter.SaveAndReimport();
        }

        private static string GetFieldDeclaration(UxmlElement element, CodeStyleRules codeStyleRules)
        {
            var type = element.IsUnityEngineType ? element.TypeName : element.FullyQualifiedTypeName;
            var fieldIdentifier = codeStyleRules.privateField.Apply(element.NameAttribute);
            return $"public {type} {fieldIdentifier};";
        }

        private static string GetStyleFieldName(string style)
        {
            StringBuilder output = new StringBuilder();
            bool makeNextCharUppercase = false;
            var s = style.Replace(" ", "_");
            
            foreach (char c in s)
            {
                if (c == '-')
                {
                    makeNextCharUppercase = true;
                }
                else
                {
                    if (makeNextCharUppercase)
                    {
                        output.Append(char.ToUpper(c));
                        makeNextCharUppercase = false;
                    }
                    else
                    {
                        output.Append(c);
                    }
                }
            }

            return Regex.Replace(output.ToString(), @"[^A-Za-z0-9_]", string.Empty);
        }

        private static string GetFieldAssigment(UxmlElement element, CodeStyleRules codeStyleRules)
        {
            var type = element.IsUnityEngineType ? element.TypeName : element.FullyQualifiedTypeName;
            var name = element.NameAttribute;
            var varName = codeStyleRules.privateField.Apply(name);

            if (element.FullyQualifiedTypeName == ColumnFullName)
                return GetFieldAssigmentForColumn(element, varName, name, codeStyleRules);

            return $"{varName} = root.Q<{type}>(\"{name}\");";
        }

        private static string GetFieldAssigmentForColumn(UxmlElement element, string varName, string name,
            CodeStyleRules codeStyleRules)
        {
            var multiColumnElement = element.XElement.Parent?.Parent?.ToUxmlElement();

            if (string.IsNullOrEmpty(multiColumnElement?.NameAttribute))
                return $"// Could not find \"{name}\" MultiColumn parent with a name.";

            var multiColumnEleVarName = codeStyleRules.privateField.Apply(multiColumnElement.NameAttribute);
            return $"{varName} = {multiColumnEleVarName}.columns[\"{name}\"];";
        }

        private static string CreateScriptContent(
            string className, 
            string classNamespace, 
            UxmlElement[] uxmlElements,
            string[] rawStyles,
            CodeStyleRules codeStyle)
        {
            var uniques = new HashSet<string>();
            var fields = uxmlElements.Select(e => GetFieldDeclaration(e, codeStyle)).Where(s => uniques.Add(s));
            uniques.Clear();
            var assignments = uxmlElements.Select(e => GetFieldAssigment(e, codeStyle)).Where(s => uniques.Add(s));
            var styles = rawStyles.Select(style => $"public static readonly string s_{GetStyleFieldName(style)} = \"{style}\";");
        
            var scriptContent = Resources.Load<TextAsset>(GenCsScriptTemplatePath).text;
            scriptContent = ReplaceClassNameTag(scriptContent, codeStyle.className.Apply(className));
            scriptContent = ReplacePackageVersionNameTag(scriptContent, PackageInfo.Version);
            scriptContent = ReplaceTagWithIndentedMultiline(scriptContent, "#FIELDS#", fields);
            scriptContent = ReplaceTagWithIndentedMultiline(scriptContent, "#STYLES#", styles);
            scriptContent = ReplaceTagWithIndentedMultiline(scriptContent, "#ASSIGNMENTS#", assignments);
            scriptContent = ReplaceNamespaceTags(scriptContent, classNamespace);
            return scriptContent;
        }

        private static bool IsEqualWithoutComments(string oldContent, string newContent)
        {
            return TrimStartComments(oldContent) != TrimStartComments(newContent);
        }

        private static string TrimStartComments(string content)
        {
            using var reader = new StringReader(content);
            while ((char)reader.Peek() == '/')
                reader.ReadLine();
            return reader.ReadToEnd();
        }

        public static string GetDefaultGenCsFilePath(string uxmlFilePath)
        {
            var uxmlExtension = ".uxml";
            return uxmlFilePath.Replace(uxmlExtension, ".gen.cs");
        }

        public static CodeStyleRules GetFinalCodeStyleRulesFor(InlineSettings inlineSettings)
        {
            var settings = inlineSettings.GetCodeStyleRules().Override(CodeGenProjectSettings.CodeStyleRules);
            return settings;
        }
    }
}