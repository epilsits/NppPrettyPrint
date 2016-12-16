using System.IO;
using System.Text;

namespace ijsonDotNet
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
}