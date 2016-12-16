using ijsonDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Formatters
{
    public struct XmlFormatSettings
    {
        public int TabWidth;
        public bool UseTabs;
        public string EolMode;
        public bool IsSelection;
    }

    public class XmlFormatter
    {
        private static LogicalComparer LogicalCompare = new LogicalComparer();
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
            exclude = new List<string>(exclusions);
            exclude.Add("");
            return DoFormatXml(sIn, ref formatSettings, true, true);
        }

        public static string MiniXml(StringBuilder sIn, XmlFormatSettings formatSettings)
        {
            return DoFormatXml(sIn, ref formatSettings, false, false);
        }

        private static string DoFormatXml(StringBuilder sIn, ref XmlFormatSettings formatSettings, bool doPretty, bool sorted)
        {
            var doc = XDocument.Parse(sIn.ToString());
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = (formatSettings.IsSelection || (doc.Declaration == null));
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
            var element = XDocument.Parse(sIn.ToString());
        }
    }
}

namespace Converters
{
    public class Base64GzipConverter
    {
        public static string ConvertToString(StringBuilder sIn)
        {
            byte[] bIn = Convert.FromBase64String(sIn.ToString());
            using (var msIn = new MemoryStream(bIn))
            using (var gz = new GZipStream(msIn, CompressionMode.Decompress))
            using (var sr = new StreamReader(gz))
                return sr.ReadToEnd();
        }

        public static string ConvertToPayload(StringBuilder sIn)
        {
            using (var msOut = new MemoryStream())
            {
                using (var msIn = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
                using (var gz = new GZipStream(msOut, CompressionMode.Compress, true))
                    msIn.CopyTo(gz);

                return Convert.ToBase64String(msOut.ToArray(), Base64FormattingOptions.None);
            }

        }
    }
}
