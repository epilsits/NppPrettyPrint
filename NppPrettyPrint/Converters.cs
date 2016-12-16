using System;
using System.IO;
using System.IO.Compression;
using System.Text;

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
