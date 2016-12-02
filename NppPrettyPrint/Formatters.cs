using ijsonDotNet;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System;
using System.IO.Compression;

namespace Formatters
{
    public struct ViewSettings
    {
        public int tabWidth;
        public bool useTabs;
        public string eolMode;
        public bool isSelection;
    }

    public class JsonFormatter
    {
        public static string prettyJson(StringBuilder sIn, ViewSettings view)
        {
            string indent = "\t";
            if (!view.useTabs)
                indent = "".PadRight(view.tabWidth);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                return new ijsonParser().pretty(sr, indent, view.eolMode);
        }

        public static string miniJson(StringBuilder sIn)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                return new ijsonParser().minify(sr);
        }

        public static void validateJson(StringBuilder sIn)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                foreach (var evt in new ijsonParser().parse(sr))
                    continue;
        }
    }

    public class XmlFormatter
    {
        public static string prettyXml(StringBuilder sIn, ViewSettings view)
        {
            return doFormatXml(sIn, ref view, true);
        }

        public static string miniXml(StringBuilder sIn, ViewSettings view)
        {
            return doFormatXml(sIn, ref view, false);
        }

        private static string doFormatXml(StringBuilder sIn, ref ViewSettings view, bool doPretty)
        {
            var doc = XDocument.Parse(sIn.ToString());
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = (view.isSelection || (doc.Declaration == null));
            if (!view.isSelection && doc.Declaration != null)
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
                if (!view.useTabs)
                    indent = "".PadRight(view.tabWidth);

                settings.IndentChars = indent;
                settings.NewLineChars = view.eolMode;
            }

            using (var ms = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(ms, settings))
                    doc.Save(xmlWriter);

                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                    return sr.ReadToEnd();
            }
        }

        public static void validateXml(StringBuilder sIn)
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
