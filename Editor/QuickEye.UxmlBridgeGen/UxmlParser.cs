using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

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
                
                foreach (var element in xElements)
                {
                    var classes = element.Attribute("class")?.Value.Split(" ");

                    if (classes is null)
                        continue;

                    foreach (var style in classes)
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
    }
}