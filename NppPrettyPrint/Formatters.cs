using ijsonDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Formatters
{
    public struct JsonFormatSettings
    {
        public int TabWidth;
        public bool UseTabs;
        public string EolMode;
    }

    public class JsonFormatter
    {
        public static string PrettyJson(StringBuilder sIn, JsonFormatSettings formatSettings, bool sorted = false)
        {
            string indent = "\t";
            if (!formatSettings.UseTabs)
                indent = "".PadRight(formatSettings.TabWidth);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                if (sorted)
                    return new ijsonParser().PrettySorted(sr, indent, formatSettings.EolMode);
                else
                    return new ijsonParser().Pretty(sr, indent, formatSettings.EolMode);
        }

        public static string PrettyJsonSorted(StringBuilder sIn, JsonFormatSettings formatSettings)
        {
            return PrettyJson(sIn, formatSettings, true);
        }

        public static string MiniJson(StringBuilder sIn)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                return new ijsonParser().Minify(sr);
        }

        public static void ValidateJson(StringBuilder sIn)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                foreach (var evt in new ijsonParser().BasicParse(sr))
                    continue;
        }
    }

    public struct XmlFormatSettings
    {
        public int TabWidth;
        public bool UseTabs;
        public string EolMode;
        public bool IsSelection;
    }

    public class XmlFormatter
    {
        private static readonly LogicalComparer LogicalCompare = new LogicalComparer();
        private static List<string> exclude;

        private class LogicalComparer : System.Collections.Generic.IComparer<string>
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int StrCmpLogicalW(string x, string y);

            public int Compare(string s1, string s2)
            {
                return StrCmpLogicalW(s1, s2);
            }
        }

        public static string PrettyXml(StringBuilder sIn, XmlFormatSettings formatSettings)
        {
            return DoFormatXml(sIn, ref formatSettings, true, false);
        }

        public static string PrettyXmlSorted(StringBuilder sIn, XmlFormatSettings formatSettings, string[] exclusions)
        {
            exclude = new List<string>(exclusions) { "" };
            return DoFormatXml(sIn, ref formatSettings, true, true);
        }

        public static string MiniXml(StringBuilder sIn, XmlFormatSettings formatSettings)
        {
            return DoFormatXml(sIn, ref formatSettings, false, false);
        }

        private static string DoFormatXml(StringBuilder sIn, ref XmlFormatSettings formatSettings, bool doPretty, bool sorted)
        {
            var doc = XDocument.Parse(sIn.ToString());
            var settings = new XmlWriterSettings()
            {
                OmitXmlDeclaration = formatSettings.IsSelection || (doc.Declaration == null)
            };
            if (!formatSettings.IsSelection && doc.Declaration != null)
            {
                string enc = doc.Declaration.Encoding;
                if (enc != "")
                {
                    try
                    {
                        settings.Encoding = Encoding.GetEncoding(doc.Declaration.Encoding);
                    }
                    catch { }
                }
            }

            settings.Indent = doPretty;
            if (doPretty)
            {
                string indent = "\t";
                if (!formatSettings.UseTabs)
                    indent = "".PadRight(formatSettings.TabWidth);

                settings.IndentChars = indent;
                settings.NewLineChars = formatSettings.EolMode;
            }

            if (sorted)
                doc = new XDocument(SortXmlElement(doc.Root));

            using (var ms = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(ms, settings))
                    doc.Save(xmlWriter);

                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                    return sr.ReadToEnd();
            }
        }

        private static XElement SortXmlElement(XElement xelement)
        {
            if (xelement.HasElements)
            {
                XElement newElement = new XElement("t");
                foreach (var ele in xelement.Elements())
                {
                    if (ele.HasElements)
                        newElement.Add(SortXmlElement(ele));
                    else
                    {
                        ele.ReplaceAttributes(ele.Attributes().OrderBy(a => a.Name.LocalName, LogicalCompare));
                        newElement.Add(ele);
                    }
                }

                return new XElement(xelement.Name,
                    xelement.Attributes()
                        .OrderBy(a => a.Name.LocalName, LogicalCompare),
                    newElement.Elements()
                        .OrderBy(e => e.Name.LocalName, LogicalCompare)
                        .ThenBy(e => e.Attributes()
                            .DefaultIfEmpty(new XAttribute("d", "!"))
                            .Where(a => !exclude.Contains(a.Value, StringComparer.OrdinalIgnoreCase))
                            .DefaultIfEmpty(new XAttribute("d", "!"))
                            .First().Value, LogicalCompare)
                        .ThenBy(e => e.Value, LogicalCompare)
                    );
            }
            else
            {
                xelement.ReplaceAttributes(xelement.Attributes().OrderBy(a => a.Name.LocalName, LogicalCompare));
                return xelement;
            }
        }

        public static void ValidateXml(StringBuilder sIn)
        {
            XDocument.Parse(sIn.ToString());
        }
    }
}
