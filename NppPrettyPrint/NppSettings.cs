using Configuration;
using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Text;

namespace NppPrettyPrint
{
    class NppSettings
    {
        internal static string IniFilePath = null;
        internal static AutoSetting<BoolSetting, bool> EnableAutoDetect = new AutoSetting<BoolSetting, bool>(new BoolSetting("enableAutoDetect"));
        internal static AutoSetting<BoolSetting, bool> EnableSizeDetect = new AutoSetting<BoolSetting, bool>(new BoolSetting("enableSizeDetect"));
        internal static AutoSetting<IntSetting, int> AutodetectMinLinesToRead = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMinLinesToRead"));
        internal static AutoSetting<IntSetting, int> AutodetectMaxLinesToRead = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMaxLinesToRead"));
        internal static AutoSetting<IntSetting, int> AutodetectMinWhitespaceLines = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMinWhitespaceLines"));
        internal static AutoSetting<IntSetting, int> AutodetectMaxCharsToReadPerLine = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMaxCharsToReadPerLine"));
        internal static AutoSetting<IntSetting, int> SizeDetectThreshold = new AutoSetting<IntSetting, int>(new IntSetting("sizeDetectThreshold"));
        internal static AutoSetting<StringSetting, string> XmlSortExcludeAttributeValues = new AutoSetting<StringSetting, string>(new StringSetting("xmlSortExcludeAttributeValues"));
        internal static AutoSetting<StringSetting, string> XmlSortExcludeValueDelimiter = new AutoSetting<StringSetting, string>(new StringSetting("xmlSortExcludeValueDelimiter"));
        internal static int AutodetectCmdId = 0;
        internal static int SizeDetectCmdId = 0;
        //static int SubmenuCmdId = 0;
        //static int SubmenuItem = 0;
        internal static IntPtr CurScintilla = (IntPtr)0;
        //static Bitmap tbBmp = Properties.Resources.star;
        //static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        //static Icon tbIcon = null;

        internal static void ReadSettings()
        {
            EnableAutoDetect.Value = Win32.GetPrivateProfileInt("Settings", EnableAutoDetect, 1, IniFilePath);
            EnableSizeDetect.Value = Win32.GetPrivateProfileInt("Settings", EnableSizeDetect, 1, IniFilePath);
            AutodetectMinLinesToRead.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMinLinesToRead, 10, IniFilePath);
            AutodetectMaxLinesToRead.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMaxLinesToRead, 20, IniFilePath);
            AutodetectMinWhitespaceLines.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMinWhitespaceLines, 5, IniFilePath);
            AutodetectMaxCharsToReadPerLine.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMaxCharsToReadPerLine, 100, IniFilePath);
            SizeDetectThreshold.Value = Win32.GetPrivateProfileInt("Settings", SizeDetectThreshold, 5242880, IniFilePath);

            var sb = new StringBuilder(4096);
            Win32Extensions.GetPrivateProfileString("Settings", XmlSortExcludeAttributeValues, "true,false,yes,no,on,off", sb, sb.Capacity, IniFilePath);
            XmlSortExcludeAttributeValues.Value = sb.ToString();
            sb.Clear();
            Win32Extensions.GetPrivateProfileString("Settings", XmlSortExcludeValueDelimiter, ",", sb, sb.Capacity, IniFilePath);
            XmlSortExcludeValueDelimiter.Value = sb.ToString();
        }

        internal static void WriteSettings()
        {
            Win32.WritePrivateProfileString("Settings", EnableAutoDetect, EnableAutoDetect.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", EnableSizeDetect, EnableSizeDetect.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMinLinesToRead, AutodetectMinLinesToRead.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMaxLinesToRead, AutodetectMaxLinesToRead.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMinWhitespaceLines, AutodetectMinWhitespaceLines.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMaxCharsToReadPerLine, AutodetectMaxCharsToReadPerLine.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", SizeDetectThreshold, SizeDetectThreshold.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", XmlSortExcludeAttributeValues, XmlSortExcludeAttributeValues.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", XmlSortExcludeValueDelimiter, XmlSortExcludeValueDelimiter.ValToString(), IniFilePath);
        }

        internal static void ApplySettings()
        {
            Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[AutodetectCmdId]._cmdID,
                Win32.MF_BYCOMMAND | (EnableAutoDetect ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));

            Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SizeDetectCmdId]._cmdID,
                Win32.MF_BYCOMMAND | (EnableSizeDetect ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
        }
    }
}
