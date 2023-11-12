using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace QuickEye.UxmlBridgeGen
{
    internal static class UxmlParser
    {
        public static bool TryGetElementsWithName(string uxml, out UxmlElement[] elements, out string[] styles)
        {
            try
            {
                var xElements = XDocument.Parse(uxml).Descendants();
                var _styles = new HashSet<string>();
                
                foreach (var element in xElements.Where(e => e.Name == "Style"))
                {
                    var src = element.Attribute("src")?.Value;
                    // var classes = element.Attribute("class")?.Value.Split(" ");

                    // if (classes is null)
                    //     continue;

                    if (src is null)
                        continue;

                    string path = ExtractPath(src);

                    if (path is null)
                        continue;

                    string filePath = Application.dataPath + "/" + path;
                    string fileContents = File.ReadAllText(filePath);
                    
                    if (fileContents is null)
                        continue;

                    var s = ExtractStyleNames(fileContents);
                        
                    foreach (var style in s)
                        _styles.Add(style);
                }

                styles = _styles.OrderBy(s => s).ToArray();
                
                elements = (from ele in xElements
                    let name = ele.Attribute("name")?.Value
                    where name != null
                    select new UxmlElement(ele)).ToArray();

                return true;
            }
            catch (XmlException)
            {
                elements = null;
                styles = null;
                return false;
            }
        }
        
        private static string ExtractPath(string input)
        {
            Match match = Regex.Match(input, @"project://database/Assets/(.*?)(\?|&|#)");

            if (match.Success)
                return match.Groups[1].Value.Replace("%20"," ");

            return null; // Path not found in the string
        }
        
        private static string[] ExtractStyleNames(string content)
        {
            var set = new HashSet<string>();
            var matches = Regex.Matches(content, @"(^\..+? {)", RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var style = match.Groups[0].Value.Trim();
                
                if (match.Success && !style.Contains(":"))
                    set.Add(match.Groups[0].Value.Replace(".", "").Replace("{", "").Trim());
            }

            return set.ToArray(); // Path not found in the string
        }
    }
}