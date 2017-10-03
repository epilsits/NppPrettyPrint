using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Converters
{
    public class Base64GzipConverter
    {
        public static string ConvertToString(StringBuilder sIn)
        {
            byte[] bIn = Convert.FromBase64String(sIn.ToString().Trim());
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

    public class BlobConverter
    {
        public static string ConvertToString(StringBuilder sIn)
        {
            byte[] bIn = StringToByteArray(sIn.ToString().Trim());
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

                return ByteArrayToByteString(msOut.ToArray());
            }
        }

        public static byte[] StringToByteArray(string str)
        {
            int start = str.StartsWith("0x", StringComparison.Ordinal) ? 1 : 0;
            return Enumerable.Range(start, (str.Length / 2) - start)
                             .Select(h => Convert.ToByte(str.Substring(h * 2, 2), 16))
                             .ToArray();
        }

        public static string ByteArrayToByteString(byte[] bytes)
        {
            var sb = new StringBuilder("0x");
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}
