using Configuration;
using Converters;
using Formatters;
using ijsonDotNet;
using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NppPrettyPrint
{
    static class Main
    {
        #region " Fields "
        internal const string PluginName = "NppPrettyPrint";
        static string IniFilePath = null;
        static AutoSetting<BoolSetting, bool> EnableAutoDetect = new AutoSetting<BoolSetting, bool>(new BoolSetting("enableAutoDetect"));
        static AutoSetting<BoolSetting, bool> EnableSizeDetect = new AutoSetting<BoolSetting, bool>(new BoolSetting("enableSizeDetect"));
        static AutoSetting<IntSetting, int> AutodetectMinLinesToRead = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMinLinesToRead"));
        static AutoSetting<IntSetting, int> AutodetectMaxLinesToRead = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMaxLinesToRead"));
        static AutoSetting<IntSetting, int> AutodetectMinWhitespaceLines = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMinWhitespaceLines"));
        static AutoSetting<IntSetting, int> AutodetectMaxCharsToReadPerLine = new AutoSetting<IntSetting, int>(new IntSetting("autodetectMaxCharsToReadPerLine"));
        static AutoSetting<IntSetting, int> SizeDetectThreshold = new AutoSetting<IntSetting, int>(new IntSetting("sizeDetectThreshold"));
        static AutoSetting<StringSetting, string> XmlSortExcludeAttributeValues = new AutoSetting<StringSetting, string>(new StringSetting("xmlSortExcludeAttributeValues"));
        static AutoSetting<StringSetting, string> XmlSortExcludeValueDelimiter = new AutoSetting<StringSetting, string>(new StringSetting("xmlSortExcludeValueDelimiter"));
        static int AutodetectCmdId = 0;
        static int SizeDetectCmdId = 0;
        //static int SubmenuCmdId = 0;
        //static int SubmenuItem = 0;
        static IntPtr CurScintilla = (IntPtr)0;
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
            PrettyXmlSorted,
            MiniXml,
            ValidateXml,
            B64GzipString,
            B64GzipPrettyJson,
            B64GzipPayload,
            BlobString,
            BlobPayload
        }

        internal struct BufferInfo
        {
            internal IntPtr Id;
            internal string Path;
            internal int UseTabs;
        }

        internal struct ViewSettings
        {
            public IntPtr Id;
            public int TabWidth;
            public bool UseTabs;
            public string EolMode;
            public bool IsSelection;
        }

        internal static Dictionary<IntPtr, BufferInfo> FileCache = new Dictionary<IntPtr, BufferInfo>();

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
                Value = value;
                Str = str;
                Name = name;
                Instance[value] = this;
            }

            public override string ToString()
            {
                return Name;
            }

            public static explicit operator EolMode(int val)
            {
                if (Instance.TryGetValue(val, out EolMode result))
                    return result;
                else
                    throw new InvalidCastException();
            }
        }

        #region " StartUp/CleanUp "
        public static void OnNotification(ScNotification notification)
        {
            // This method is invoked whenever something is happening in notepad++
            // use eg. as
            // if (notification.Header.Code == (uint)NppMsg.NPPN_xxx)
            // { ... }
            // or
            //
            // if (notification.Header.Code == (uint)SciMsg.SCNxxx)
            // { ... }

            /*
             * before load
             * before open
             * file opened
             */

            uint code = notification.Header.Code;
            IntPtr idFrom = notification.Header.IdFrom;
            if (code == (uint)NppMsg.NPPN_READY)
            {
                OnPluginReady();
            }
            else if (code == (uint)NppMsg.NPPN_FILEBEFOREOPEN)
            {
                OnBeforeOpen(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
            {
                OnBufferActivated(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_FILESAVED)
            {
                OnFileSaved(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_FILECLOSED)
            {
                OnFileClosed(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_LANGCHANGED)
            {
                OnLangChanged(idFrom);
            }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            IniFilePath = Path.GetFullPath(sbIniFilePath.ToString());
            if (!Directory.Exists(IniFilePath)) Directory.CreateDirectory(IniFilePath);
            IniFilePath = Path.Combine(IniFilePath, PluginName + ".ini");

            ReadSettings();
            WriteSettings();

            int cmdIdx = 0;

            PluginBase.SetCommand(cmdIdx++, "Json: Pretty", FormatPrettyJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "Json: Pretty (sorted)", FormatPrettyJsonSortedMenu);
            PluginBase.SetCommand(cmdIdx++, "Json: Minify", FormatMiniJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "Json: Validate", ValidateJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Xml: Pretty", FormatPrettyXmlMenu);
            PluginBase.SetCommand(cmdIdx++, "Xml: Pretty (sorted)", FormatPrettyXmlSortedMenu);
            PluginBase.SetCommand(cmdIdx++, "Xml: Minify", FormatMiniXmlMenu);
            PluginBase.SetCommand(cmdIdx++, "Xml: Validate", ValidateXMLMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Base64/Gzip -> String", B64GzipStringMenu);
            PluginBase.SetCommand(cmdIdx++, "Base64/Gzip -> Pretty Json", B64GzipPrettyJsonMenu);
            PluginBase.SetCommand(cmdIdx++, "String -> Base64/Gzip", B64GzipPayloadMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Blob -> String", BlobStringMenu);
            PluginBase.SetCommand(cmdIdx++, "String -> Blob", BlobPayloadMenu);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Detect Indentation", DetectMenu);
            PluginBase.SetCommand(cmdIdx++, "Set Tabs", SetTabsMenu);
            PluginBase.SetCommand(cmdIdx++, "Set Spaces", SetSpacesMenu);
            AutodetectCmdId = cmdIdx++;
            PluginBase.SetCommand(AutodetectCmdId, "Enable Indent Autodetect", AutodetectEnableMenu, EnableAutoDetect);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            SizeDetectCmdId = cmdIdx++;
            PluginBase.SetCommand(SizeDetectCmdId, "Enable File Size Autodetect", SizeDetectEnableMenu, EnableSizeDetect);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            PluginBase.SetCommand(cmdIdx++, "Settings...", SettingsMenu);
            //SubmenuCmdId = cmdIdx++;
            //PluginBase.SetCommand(SubmenuCmdId, "Sub menu", NoOpMenu);
            //SubmenuItem = cmdIdx++;
            //PluginBase.SetCommand(SubmenuItem, "Sub menu item", NoOpMenu);
            //PluginBase.SetCommand(1, "Pretty Json: Format", prettyJson, new ShortcutKey(false, false, false, Keys.None));
        }

        internal static void SetToolBarIcon()
        {
            //toolbarIcons tbIcons = new toolbarIcons();
            //tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            //IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            //Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            //Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            //Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        { }
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

        internal static void FormatPrettyXmlSortedMenu()
        {
            FormatData(FormatType.PrettyXmlSorted);
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
        internal static void B64GzipPrettyJsonMenu()
        {
            FormatData(FormatType.B64GzipPrettyJson);
        }

        internal static void B64GzipPayloadMenu()
        {
            FormatData(FormatType.B64GzipPayload);
        }

        internal static void BlobStringMenu()
        {
            FormatData(FormatType.BlobString);
        }

        internal static void BlobPayloadMenu()
        {
            FormatData(FormatType.BlobPayload);
        }

        internal static void DetectMenu()
        {
            IntPtr id = GetActiveBuffer();
            RemoveFileFromCache(id);
            GuessIndentation(id, true);

            if (FileCache.ContainsKey(id))
                MessageBox.Show(string.Format("Set indentation settings to: {0}", (FileCache[id].UseTabs == 1) ? "Tabs" : "Spaces"), "Info");
            else
                MessageBox.Show("Unable to determine indentation settings.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static void SetTabsMenu()
        {
            IntPtr id = GetActiveBuffer();
            var buff = GetBufferInfo(id, 1);
            SetUseTabs(1);
            FileCache[id] = buff;
        }

        internal static void SetSpacesMenu()
        {
            IntPtr id = GetActiveBuffer();
            var buff = GetBufferInfo(id, 0);
            SetUseTabs(0);
            FileCache[id] = buff;
        }

        internal static void AutodetectEnableMenu()
        {
            EnableAutoDetect = !EnableAutoDetect;
            ApplySettings();
            WriteSettings();
        }

        internal static void SizeDetectEnableMenu()
        {
            EnableSizeDetect = !EnableSizeDetect;
            ApplySettings();
            WriteSettings();
        }

        internal static void SettingsMenu()
        {
            //Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DOOPEN, 0, IniFilePath);
            using (var settings = new SettingsDialog())
            {
                settings.ValMinLinesToRead = AutodetectMinLinesToRead;
                settings.ValMaxLinesToRead = AutodetectMaxLinesToRead;
                settings.ValMinWhitespaceLines = AutodetectMinWhitespaceLines;
                settings.ValMaxCharsPerLine = AutodetectMaxCharsToReadPerLine;
                settings.ValSizeDetectThreshold = SizeDetectThreshold;
                settings.ValExcludeAttributeValues = XmlSortExcludeAttributeValues.ValToString();
                settings.ValExcludeValueDelimiter = XmlSortExcludeValueDelimiter.ValToString();

                if (DialogResult.OK == settings.ShowDialog())
                {
                    AutodetectMinLinesToRead.Value = settings.ValMinLinesToRead;
                    AutodetectMaxLinesToRead.Value = settings.ValMaxLinesToRead;
                    AutodetectMinWhitespaceLines.Value = settings.ValMinWhitespaceLines;
                    AutodetectMaxCharsToReadPerLine.Value = settings.ValMaxCharsPerLine;
                    SizeDetectThreshold.Value = settings.ValSizeDetectThreshold;
                    XmlSortExcludeAttributeValues.Value = settings.ValExcludeAttributeValues;
                    XmlSortExcludeValueDelimiter.Value = settings.ValExcludeValueDelimiter;

                    ApplySettings();
                    WriteSettings();
                }
            }
        }

        //internal static void NoOpMenu()
        //{ MessageBox.Show("Clicked menu NoOp"); }
        #endregion

        #region " Worker Functions "
        internal static void FormatData(FormatType fType)
        {
            StringBuilder npText;
            var isSel = false;

            int selLen = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETSELTEXT, 0, 0);
            // len is text + terminating null
            if (selLen > 1)
            {
                isSel = true;
                npText = new StringBuilder(selLen + 1);
                Win32.SendMessage(CurScintilla, SciMsg.SCI_GETSELTEXT, 0, npText);
            }
            else
            {
                int nLen = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETLENGTH, 0, 0);
                npText = new StringBuilder(nLen + 1);
                Win32.SendMessage(CurScintilla, SciMsg.SCI_GETTEXT, nLen + 1, npText);
            }
            
            try
            {
                if (npText.Length < 1)
                {
                    throw new Exception("No text selected.");
                }

                string npOut = "";
                if (fType == FormatType.ValidateJson)
                {
                    JsonFormatter.ValidateJson(npText);
                    MessageBox.Show("JSON successfully validated  :-)");
                    return;
                }
                else if (fType == FormatType.ValidateXml)
                {
                    XmlFormatter.ValidateXml(npText);
                    MessageBox.Show("XML successfully validated  :-)");
                    return;
                }
                else
                {
                    npOut = FormatStringData(npText, fType, isSel);
                }

                SciMsg setMsg;
                if (isSel)
                    setMsg = SciMsg.SCI_REPLACESEL;
                else
                    setMsg = SciMsg.SCI_SETTEXT;

                Win32.SendMessage(CurScintilla, setMsg, 0, npOut);

                //unsafe
                //{
                //    fixed (byte* strPtr = Encoding.UTF8.GetBytes(npOut))
                //    {
                //        Win32.SendMessage(CurScintilla, setMsg, 0, (IntPtr)strPtr);
                //    }
                //}
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("{0}", e.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                GC.Collect();
            }
        }

        internal static string FormatStringData(StringBuilder npText, FormatType fType, bool isSel)
        {
            string npOut = "";
            var view = GetViewSettings(isSel);
            if (fType == FormatType.PrettyJson)
            {
                npOut = JsonFormatter.PrettyJson(npText, new JsonFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode });
                CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.PrettyJsonSorted)
            {
                npOut = JsonFormatter.PrettyJsonSorted(npText, new JsonFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode });
                CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.MiniJson)
            {
                npOut = JsonFormatter.MiniJson(npText);
                CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.PrettyXml)
            {
                npOut = XmlFormatter.PrettyXml(npText, new XmlFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode, IsSelection = view.IsSelection });
                CheckSetLangType(view.Id, (int)LangType.L_XML);
            }
            else if (fType == FormatType.PrettyXmlSorted)
            {
                npOut = XmlFormatter.PrettyXmlSorted(npText, new XmlFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode, IsSelection = view.IsSelection },
                    XmlSortExcludeAttributeValues.ValToString().Split(new string[] { XmlSortExcludeValueDelimiter.ValToString() }, StringSplitOptions.RemoveEmptyEntries));
                CheckSetLangType(view.Id, (int)LangType.L_XML);
            }
            else if (fType == FormatType.MiniXml)
            {
                npOut = XmlFormatter.MiniXml(npText, new XmlFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode, IsSelection = view.IsSelection });
                CheckSetLangType(view.Id, (int)LangType.L_XML);
            }
            else if (fType == FormatType.B64GzipString)
            {
                if (npText.ToString(0, 5).StartsWith("Data=", StringComparison.OrdinalIgnoreCase))
                    npText.Remove(0, 5);
                
                npOut = Base64GzipConverter.ConvertToString(npText);
            }
            else if (fType == FormatType.B64GzipPrettyJson)
            {
                if (npText.ToString(0, 5).StartsWith("Data=", StringComparison.OrdinalIgnoreCase))
                    npText.Remove(0, 5);

                npOut = JsonFormatter.PrettyJson(new StringBuilder(Base64GzipConverter.ConvertToString(npText)),
                    new JsonFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode });
                CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.B64GzipPayload)
            {
                npOut = Base64GzipConverter.ConvertToPayload(npText);
            }
            else if (fType == FormatType.BlobString)
            {
                npOut = BlobConverter.ConvertToString(npText);
            }
            else if (fType == FormatType.BlobPayload)
            {
                npOut = BlobConverter.ConvertToPayload(npText);
            }
            else
            {
                throw new Exception("Invalid command.");
            }

            return npOut;
        }
        #endregion

        #region " Utility Functions "
        internal static ViewSettings GetViewSettings(bool isSelection = false)
        {
            int tabWidth = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETTABWIDTH, 0, 0);
            var eolMode = (EolMode)(int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETEOLMODE, 0, 0);

            IntPtr id = GetActiveBuffer();
            bool useTabs;
            if (FileCache.ContainsKey(id))
                useTabs = Convert.ToBoolean(FileCache[id].UseTabs);
            else
                useTabs = Convert.ToBoolean((int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETUSETABS, 0, 0));

            return new ViewSettings() { Id = id, TabWidth = tabWidth, UseTabs = useTabs, EolMode = eolMode.Str, IsSelection = isSelection };
        }

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

        internal static BufferInfo GetBufferInfo(IntPtr id, int useTabs = 0)
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            if ((int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLPATHFROMBUFFERID, id, path) == -1)
            {
                throw new Exception("Invalid buffer ID.");
            }

            return new BufferInfo() { Id = id, Path = Path.GetFullPath(path.ToString()), UseTabs = useTabs };
        }

        internal static IntPtr GetActiveBuffer()
        {
            return Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
        }

        internal static void SetUseTabs(int useTabs)
        {
            Win32.SendMessage(CurScintilla, SciMsg.SCI_SETUSETABS, useTabs, 0);
        }

        internal static void SetLangType(int langType)
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETCURRENTLANGTYPE, 0, langType);
        }

        internal static void CheckSetLangType(IntPtr id, int langType)
        {
            if (!IsLargeBuffer())
                SetLangType(langType);
        }

        internal static void SetBufferLangType(IntPtr id, int langType)
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETBUFFERLANGTYPE, id, langType);
        }

        internal static void GuessIndentation(IntPtr id, bool force = false)
        {
            if (FileCache.TryGetValue(id, out BufferInfo buff))
            {
                SetUseTabs(buff.UseTabs);
                return;
            }

            if (!EnableAutoDetect && !force)
                return;

            int numLines = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETLINECOUNT, 0, 0);
            if (numLines >= AutodetectMinLinesToRead)
            {
                int wsLines = 0;
                int tabLines = 0;
                var ttf = new TextToFind(0, 0, @"^\s+");
                for (var i = 0; i < Math.Min(AutodetectMaxLinesToRead, numLines); i++)
                {
                    int startPos = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_POSITIONFROMLINE, i, 0);
                    int endPos = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETLINEENDPOSITION, i, 0); // excl EOL chars
                    int lineLen = endPos - startPos;
                    if (lineLen > 0)
                    {
                        ttf.chrg = new CharacterRange(startPos, startPos + Math.Min(lineLen, AutodetectMaxCharsToReadPerLine));
                        int find = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_FINDTEXT, (int)(SciMsg.SCFIND_REGEXP | SciMsg.SCFIND_CXX11REGEX), ttf.NativePointer);
                        if (find != -1)
                        {
                            wsLines++;
                            var rgFind = ttf.chrgText;
                            var tr = new TextRange(rgFind, rgFind.cpMax - rgFind.cpMin + 1);
                            Win32.SendMessage(CurScintilla, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
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
                    SetUseTabs(buff.UseTabs);
                }

                //MessageBox.Show(string.Format("Lines: {0}, count: {1}, tabs: {2}\nFile: {3}", numLines, wsLines, tabLines, buff.path));
            }
        }

        internal static void FileSaved(IntPtr id)
        {
            var buff = GetBufferInfo(id);
            if (string.Equals(buff.Path, IniFilePath, StringComparison.OrdinalIgnoreCase))
            {
                ReadSettings();
                ApplySettings();
            }

            if (!EnableAutoDetect)
                return;

            RemoveFileFromCache(id);
            IntPtr activeBuf = GetActiveBuffer();
            if (activeBuf == id)
                GuessIndentation(id);
        }

        internal static void RemoveFileFromCache(IntPtr id)
        {
            FileCache.Remove(id);
        }

        internal static bool IsLargeFile(IntPtr id)
        {
            if (EnableSizeDetect)
            {
                var buff = GetBufferInfo(id);
                try
                {
                    long fileSize = new FileInfo(buff.Path).Length;
                    if (fileSize > SizeDetectThreshold)
                        return true;
                }
                catch { }
            }

            return false;
        }
        
        internal static bool IsLargeBuffer()
        {
            if (EnableSizeDetect)
            {
                int nLen = (int)Win32.SendMessage(CurScintilla, SciMsg.SCI_GETLENGTH, 0, 0);
                if (nLen > SizeDetectThreshold)
                    return true;
            }

            return false;
        }
        #endregion

        #region " Events "
        internal static void OnPluginReady()
        {
            CurScintilla = PluginBase.GetCurrentScintilla();

            // menu modifications
            //IntPtr submenu = Win32Extensions.CreatePopupMenu();
            //Win32Extensions.ModifyMenu(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SubmenuCmdId]._cmdID,
            //    Win32.MF_BYCOMMAND | (int)Win32Extensions.MenuFlags.MF_POPUP, submenu, "Sub menu2");
            //Win32Extensions.DeleteMenu(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SubmenuItem]._cmdID, Win32.MF_BYCOMMAND);
            //Win32Extensions.AppendMenu(submenu, Win32Extensions.MenuFlags.MF_STRING, PluginBase._funcItems.Items[SubmenuItem]._cmdID, "Sub menu item2");
        }

        internal static void OnBeforeOpen(IntPtr id)
        {
            if (IsLargeFile(id))
                SetBufferLangType(id, (int)LangType.L_TEXT);
        }

        internal static void OnBufferActivated(IntPtr id)
        {
            CurScintilla = PluginBase.GetCurrentScintilla();
            GuessIndentation(id);
        }

        internal static void OnFileSaved(IntPtr id)
        {
            FileSaved(id);
        }

        internal static void OnFileClosed(IntPtr id)
        {
            RemoveFileFromCache(id);
        }

        internal static void OnLangChanged(IntPtr id)
        {
            if (FileCache.ContainsKey(id))
                SetUseTabs(FileCache[id].UseTabs);
        }
        #endregion
    }
}