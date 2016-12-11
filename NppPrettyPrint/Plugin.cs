﻿using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using Formatters;
using Converters;
using System.Collections.Generic;
using Configuration;

namespace NppPrettyPrint
{
    class Plugin
    {
        #region " Fields "
        internal const string PluginName = "NppPrettyPrint";
        static string IniFilePath = null;
        static AutoSetting<BoolSetting, bool> EnableAutoDetect = new AutoSetting<BoolSetting, bool>(new BoolSetting("enableAutoDetect"));
        static AutoSetting<IntSetting, int> AutodetectMinLinesToRead = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMinLinesToRead"));
        static AutoSetting<IntSetting, int> AutodetectMaxLinesToRead = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMaxLinesToRead"));
        static AutoSetting<IntSetting, int> AutodetectMinWhitespaceLines = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMinWhitespaceLines"));
        static AutoSetting<IntSetting, int> AutodetectMaxCharsToReadPerLine = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMaxCharsToReadPerLine"));
        static int AutodetectCmdId = 0;
        //static Bitmap tbBmp = Properties.Resources.star;
        //static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        //static Icon tbIcon = null;
        #endregion

        internal enum FormatType
        {
            PrettyJson,
            PrettyJsonSorted,
            MiniJson,
            ValidateJson,
            PrettyXml,
            MiniXml,
            ValidateXml,
            B64GzipString,
            B64GzipPayload
        }

        internal struct BufferInfo
        {
            internal int Id;
            internal string Path;
            internal int UseTabs;
        }

        internal static Dictionary<int, BufferInfo> FileCache = new Dictionary<int, BufferInfo>();

        public sealed class EolMode
        {
            public readonly int Value;
            public readonly string Str;
            public readonly string Name;

            private static readonly Dictionary<int, EolMode> Instance = new Dictionary<int, EolMode>();

            public static readonly EolMode EOL_CRLF = new EolMode(0, "\r\n", "EOL_CRLF");
            public static readonly EolMode EOL_CR = new EolMode(1, "\r", "EOL_CR");
            public static readonly EolMode EOL_LF = new EolMode(2, "\n", "EOL_LF");

            private EolMode(int value, string str, string name)
            {
                this.Value = value;
                this.Str = str;
                this.Name = name;
                Instance[value] = this;
            }

            public override string ToString()
            {
                return Name;
            }

            public static explicit operator EolMode(int val)
            {
                EolMode result;
                if (Instance.TryGetValue(val, out result))
                    return result;
                else
                    throw new InvalidCastException();
            }
        }

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            IniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(IniFilePath)) Directory.CreateDirectory(IniFilePath);
            IniFilePath = Path.Combine(IniFilePath, PluginName + ".ini");

            ReadSettings();
            if (!File.Exists(IniFilePath))
                WriteSettings();

            int cmdIdx = 0;

            PluginBase.SetCommand(cmdIdx++, "Json: Pretty", FormatPrettyJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "Json: Pretty (sorted)", FormatPrettyJsonSortedMenu);
            PluginBase.SetCommand(cmdIdx++, "Json: Minify", FormatMiniJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "Json: Validate", ValidateJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Xml: Pretty", FormatPrettyXmlMenu);
            PluginBase.SetCommand(cmdIdx++, "Xml: Minify", FormatMiniXmlMenu);
            PluginBase.SetCommand(cmdIdx++, "Xml: Validate", ValidateXMLMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Base64/Gzip -> String", B64GzipStringMenu);
            PluginBase.SetCommand(cmdIdx++, "String -> Base64/Gzip", B64GzipPayloadMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Detect Indentation", DetectMenu);
            PluginBase.SetCommand(cmdIdx++, "Set Tabs", SetTabsMenu);
            PluginBase.SetCommand(cmdIdx++, "Set Spaces", SetSpacesMenu);
            AutodetectCmdId = cmdIdx++;
            PluginBase.SetCommand(AutodetectCmdId, "Enable Autodetect", AutodetectEnableMenu, EnableAutoDetect);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Settings...", SettingsMenu);
            //PluginBase.SetCommand(1, "Pretty Json: Format", prettyJson, new ShortcutKey(false, false, false, Keys.None));
        }

        //internal static void SetToolBarIcon()
        //{
        //    toolbarIcons tbIcons = new toolbarIcons();
        //    tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
        //    IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
        //    Marshal.StructureToPtr(tbIcons, pTbIcons, false);
        //    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
        //    Marshal.FreeHGlobal(pTbIcons);
        //}

        internal static void PluginCleanUp()
        {
        }
        #endregion

        #region " Menu functions "
        internal static void FormatPrettyJsonMenu()
        {
            FormatData(FormatType.PrettyJson);
        }

        internal static void FormatPrettyJsonSortedMenu()
        {
            FormatData(FormatType.PrettyJsonSorted);
        }

        internal static void FormatMiniJsonMenu()
        {
            FormatData(FormatType.MiniJson);
        }

        internal static void ValidateJsonMenu()
        {
            FormatData(FormatType.ValidateJson);
        }

        internal static void FormatPrettyXmlMenu()
        {
            FormatData(FormatType.PrettyXml);
        }

        internal static void FormatMiniXmlMenu()
        {
            FormatData(FormatType.MiniXml);
        }

        internal static void ValidateXMLMenu()
        {
            FormatData(FormatType.ValidateXml);
        }

        internal static void B64GzipStringMenu()
        {
            FormatData(FormatType.B64GzipString);
        }

        internal static void B64GzipPayloadMenu()
        {
            FormatData(FormatType.B64GzipPayload);
        }

        internal static void DetectMenu()
        {
            int id = GetActiveBuffer();
            RemoveFileFromCache(id);
            GuessIndentation(id, true);

            if (FileCache.ContainsKey(id))
                MessageBox.Show(string.Format("Set indentation settings to: {0}", (FileCache[id].UseTabs == 1) ? "Tabs" : "Spaces"), "Info");
            else
                MessageBox.Show("Unable to determine indentation settings.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static void SetTabsMenu()
        {
            int id = GetActiveBuffer();
            var buff = GetBufferInfo(id, 1);
            setUseTabs(1);
            FileCache[id] = buff;
        }

        internal static void SetSpacesMenu()
        {
            int id = GetActiveBuffer();
            var buff = GetBufferInfo(id, 0);
            setUseTabs(0);
            FileCache[id] = buff;
        }

        internal static void AutodetectEnableMenu()
        {
            EnableAutoDetect = !EnableAutoDetect;
            ApplySettings();
            WriteSettings();
        }

        internal static void SettingsMenu()
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, IniFilePath);
        }
        #endregion

        #region " Worker Functions "
        internal static void FormatData(FormatType fType)
        {
            StringBuilder npText;
            var curScintilla = PluginBase.GetCurrentScintilla();
            var isSel = false;

            var selLen = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETSELTEXT, 0, 0);
            // len is text + terminating null
            if (selLen > 1)
            {
                isSel = true;
                npText = new StringBuilder(selLen + 1);
                Win32.SendMessage(curScintilla, SciMsg.SCI_GETSELTEXT, 0, npText);
            }
            else
            {
                var nLen = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETLENGTH, 0, 0);
                npText = new StringBuilder(nLen + 1);
                Win32.SendMessage(curScintilla, SciMsg.SCI_GETTEXT, nLen + 1, npText);
            }
            
            try
            {
                if (npText.Length < 1)
                {
                    throw new Exception("No text selected.");
                }

                string npOut = "";
                if (fType == FormatType.PrettyJson)
                {
                    npOut = JsonFormatter.PrettyJson(npText, GetViewSettings(isSel));
                    SetLangType((int)LangType.L_JSON);
                }
                else if (fType == FormatType.PrettyJsonSorted)
                {
                    npOut = JsonFormatter.PrettyJsonSorted(npText, GetViewSettings(isSel));
                    SetLangType((int)LangType.L_JSON);
                }
                else if (fType == FormatType.MiniJson)
                {
                    npOut = JsonFormatter.MiniJson(npText);
                    SetLangType((int)LangType.L_JSON);
                }
                else if (fType == FormatType.ValidateJson)
                {
                    JsonFormatter.ValidateJson(npText);
                    MessageBox.Show("JSON successfully validated  :-)");
                    return;
                }
                else if (fType == FormatType.PrettyXml)
                {
                    npOut = XmlFormatter.PrettyXml(npText, GetViewSettings(isSel));
                    SetLangType((int)LangType.L_XML);
                }
                else if (fType == FormatType.MiniXml)
                {
                    npOut = XmlFormatter.MiniXml(npText, GetViewSettings(isSel));
                    SetLangType((int)LangType.L_XML);
                }
                else if (fType == FormatType.ValidateXml)
                {
                    XmlFormatter.ValidateXml(npText);
                    MessageBox.Show("XML successfully validated  :-)");
                    return;
                }
                else if (fType == FormatType.B64GzipString)
                {
                    npOut = Base64GzipConverter.ConvertToString(npText);
                }
                else if (fType == FormatType.B64GzipPayload)
                {
                    npOut = Base64GzipConverter.ConvertToPayload(npText);
                }
                else
                {
                    throw new Exception("Invalid command.");
                }

                if (isSel)
                {
                    Win32.SendMessage(curScintilla, SciMsg.SCI_REPLACESEL, 0, npOut);
                }
                else
                {
                    Win32.SendMessage(curScintilla, SciMsg.SCI_SETTEXT, 0, npOut);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("{0}", e.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region " Utility Functions "
        internal static ViewSettings GetViewSettings(bool isSelection = false)
        {
            var curScintilla = PluginBase.GetCurrentScintilla();
            int tabWidth = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETTABWIDTH, 0, 0);
            var eolMode = (EolMode)(int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETEOLMODE, 0, 0);

            int id = GetActiveBuffer();
            bool useTabs;
            if (FileCache.ContainsKey(id))
                useTabs = Convert.ToBoolean(FileCache[id].UseTabs);
            else
                useTabs = Convert.ToBoolean((int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETUSETABS, 0, 0));

            return new ViewSettings() { TabWidth = tabWidth, UseTabs = useTabs, EolMode = eolMode.Str, IsSelection = isSelection };
        }

        internal static void ReadSettings()
        {
            EnableAutoDetect.Value = Win32.GetPrivateProfileInt("Settings", EnableAutoDetect, 1, IniFilePath);
            AutodetectMinLinesToRead.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMinLinesToRead, 10, IniFilePath);
            AutodetectMaxLinesToRead.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMaxLinesToRead, 20, IniFilePath);
            AutodetectMinWhitespaceLines.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMinWhitespaceLines, 5, IniFilePath);
            AutodetectMaxCharsToReadPerLine.Value = Win32.GetPrivateProfileInt("Settings", AutodetectMaxCharsToReadPerLine, 100, IniFilePath);
        }

        internal static void WriteSettings()
        {
            Win32.WritePrivateProfileString("Settings", EnableAutoDetect, EnableAutoDetect.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMinLinesToRead, AutodetectMinLinesToRead.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMaxLinesToRead, AutodetectMaxLinesToRead.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMinWhitespaceLines, AutodetectMinWhitespaceLines.ValToString(), IniFilePath);
            Win32.WritePrivateProfileString("Settings", AutodetectMaxCharsToReadPerLine, AutodetectMaxCharsToReadPerLine.ValToString(), IniFilePath);
        }

        internal static void ApplySettings()
        {
            Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[AutodetectCmdId]._cmdID,
                Win32.MF_BYCOMMAND | (EnableAutoDetect ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
        }

        internal static BufferInfo GetBufferInfo(int id, int useTabs = 0)
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            if ((int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, id, path) == -1)
            {
                throw new Exception("Invalid buffer ID.");
            }

            return new BufferInfo() { Id = id, Path = path.ToString(), UseTabs = useTabs };
        }

        internal static int GetActiveBuffer()
        {
            return (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
        }

        internal static void setUseTabs(int useTabs)
        {
            Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETUSETABS, useTabs, 0);
        }

        internal static void SetLangType(int langType)
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_SETCURRENTLANGTYPE, 0, langType);
        }

        internal static void GuessIndentation(int id, bool force = false)
        {
            var curScintilla = PluginBase.GetCurrentScintilla();
            BufferInfo buff;
            if (FileCache.TryGetValue(id, out buff))
            {
                setUseTabs(buff.UseTabs);
                return;
            }

            if (!EnableAutoDetect && !force)
                return;

            int numLines = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETLINECOUNT, 0, 0);
            if (numLines >= AutodetectMinLinesToRead)
            {
                int wsLines = 0;
                int tabLines = 0;
                var ttf = new Sci_TextToFind(0, 0, @"^\s+");
                for (var i = 0; i < Math.Min(AutodetectMaxLinesToRead, numLines); i++)
                {
                    int startPos = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_POSITIONFROMLINE, i, 0);
                    int endPos = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETLINEENDPOSITION, i, 0); // excl EOL chars
                    int lineLen = endPos - startPos;
                    if (lineLen > 0)
                    {
                        ttf.chrg = new Sci_CharacterRange(startPos, startPos + Math.Min(lineLen, AutodetectMaxCharsToReadPerLine));
                        int find = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_FINDTEXT, (int)(SciMsg.SCFIND_REGEXP | SciMsg.SCFIND_CXX11REGEX), ttf.NativePointer);
                        if (find != -1)
                        {
                            wsLines++;
                            var rgFind = ttf.chrgText;
                            var tr = new Sci_TextRange(rgFind, rgFind.cpMax - rgFind.cpMin + 1);
                            Win32.SendMessage(curScintilla, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                            if (tr.lpstrText.Contains("\t"))
                                tabLines++;

                            //MessageBox.Show(string.Format("Line: {3}\nFind start: {0}\nFind end: {1}\nFind len: {2}",
                            //    rgFind.cpMin, rgFind.cpMax, rgFind.cpMax - rgFind.cpMin, i + 1));
                        }
                    }
                }
                
                if (wsLines >= AutodetectMinWhitespaceLines)
                {
                    buff = GetBufferInfo(id);
                    if (tabLines >= (wsLines - tabLines))
                        buff.UseTabs = 1;
                    else
                        buff.UseTabs = 0;

                    FileCache[id] = buff;
                    setUseTabs(buff.UseTabs);
                }

                //MessageBox.Show(string.Format("Lines: {0}, count: {1}, tabs: {2}\nFile: {3}", numLines, wsLines, tabLines, buff.path));
            }
        }

        internal static void FileSaved(int id)
        {
            var buff = GetBufferInfo(id);
            if (string.Equals(Path.GetFullPath(buff.Path), Path.GetFullPath(IniFilePath), StringComparison.OrdinalIgnoreCase))
            {
                ReadSettings();
                ApplySettings();
            }

            if (!EnableAutoDetect)
                return;

            RemoveFileFromCache(id);
            int activeBuf = GetActiveBuffer();
            if (activeBuf == id)
                GuessIndentation(id);
        }

        internal static void RemoveFileFromCache(int id)
        {
            FileCache.Remove(id);
        }
        #endregion

        #region " Events "
        internal static void OnBufferActivated(int id)
        {
            GuessIndentation(id);
        }

        internal static void OnFileSaved(int id)
        {
            FileSaved(id);
        }

        internal static void OnFileClosed(int id)
        {
            RemoveFileFromCache(id);
        }

        internal static void OnLangChanged(int id)
        {
            if (FileCache.ContainsKey(id))
                setUseTabs(FileCache[id].UseTabs);
        }

        internal static void OnNppShutdown()
        {
            PluginCleanUp();
        }
        #endregion
    }
}