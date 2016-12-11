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
        public int TabWidth;
        public bool UseTabs;
        public string EolMode;
        public bool IsSelection;
    }

    public class JsonFormatter
    {
        public static string PrettyJson(StringBuilder sIn, ViewSettings view, bool sorted = false)
        {
            string indent = "\t";
            if (!view.UseTabs)
                indent = "".PadRight(view.TabWidth);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sIn.ToString())))
            using (var sr = new StreamReader(ms))
                if (sorted)
                    return new ijsonParser().PrettySorted(sr, indent, view.EolMode);
                else
                    return new ijsonParser().Pretty(sr, indent, view.EolMode);
        }

        public static string PrettyJsonSorted(StringBuilder sIn, ViewSettings view)
        {
            return PrettyJson(sIn, view, true);
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
                foreach (var evt in new ijsonParser().Parse(sr))
                    continue;
        }
    }

    public class XmlFormatter
    {
        public static string PrettyXml(StringBuilder sIn, ViewSettings view)
        {
            return DoFormatXml(sIn, ref view, true);
        }

        public static string MiniXml(StringBuilder sIn, ViewSettings view)
        {
            return DoFormatXml(sIn, ref view, false);
        }

        private static string DoFormatXml(StringBuilder sIn, ref ViewSettings view, bool doPretty)
        {
            var doc = XDocument.Parse(sIn.ToString());
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = (view.IsSelection || (doc.Declaration == null));
            if (!view.IsSelection && doc.Declaration != null)
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
                if (!view.UseTabs)
                    indent = "".PadRight(view.TabWidth);

                settings.IndentChars = indent;
                settings.NewLineChars = view.EolMode;
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
