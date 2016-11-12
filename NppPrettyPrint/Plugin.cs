using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using Formatters;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NppPrettyPrint
{
    class Plugin
    {
        #region " Fields "
        internal const string PluginName = "NppPrettyPrint";
        static string iniFilePath = null;
        static bool enableAutoDetect = true;
        static int autodetectCmdId = 0;
        //static Bitmap tbBmp = Properties.Resources.star;
        //static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        //static Icon tbIcon = null;
        #endregion

        internal enum FormatType
        {
            PrettyJson,
            MiniJson,
            ValidateJson,
            PrettyXml,
            MiniXml,
            ValidateXml
        }

        internal struct BufferInfo
        {
            internal int id;
            internal string path;
            internal int useTabs;
        }

        internal static Dictionary<int, BufferInfo> fileCache = new Dictionary<int, BufferInfo>();

        public sealed class EolMode
        {
            public readonly int value;
            public readonly string str;
            public readonly string name;

            private static readonly Dictionary<int, EolMode> instance = new Dictionary<int, EolMode>();

            public static readonly EolMode EOL_CRLF = new EolMode(0, "\r\n", "EOL_CRLF");
            public static readonly EolMode EOL_CR = new EolMode(1, "\r", "EOL_CR");
            public static readonly EolMode EOL_LF = new EolMode(2, "\n", "EOL_LF");

            private EolMode(int value, string str, string name)
            {
                this.value = value;
                this.str = str;
                this.name = name;
                instance[value] = this;
            }

            public override string ToString()
            {
                return name;
            }

            public static explicit operator EolMode(int val)
            {
                EolMode result;
                if (instance.TryGetValue(val, out result))
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
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            enableAutoDetect = (Win32.GetPrivateProfileInt("Settings", "enableAutoDetect", 1, iniFilePath) != 0);

            PluginBase.SetCommand(0, "Json: Pretty", formatPrettyJsonMenu);
            PluginBase.SetCommand(1, "Json: Minify", formatMiniJsonMenu);
            PluginBase.SetCommand(2, "Json: Validate", validateJsonMenu);
            PluginBase.SetCommand(3, "---", null);
            PluginBase.SetCommand(4, "Xml: Pretty", formatPrettyXmlMenu);
            PluginBase.SetCommand(5, "Xml: Minify", formatMiniXmlMenu);
            PluginBase.SetCommand(6, "Xml: Validate", validateXMLMenu);
            PluginBase.SetCommand(7, "---", null);
            PluginBase.SetCommand(8, "Detect Indentation", detectMenu);
            PluginBase.SetCommand(9, "Set Tabs", setTabsMenu);
            PluginBase.SetCommand(10, "Set Spaces", setSpacesMenu);
            autodetectCmdId = 11;
            PluginBase.SetCommand(autodetectCmdId, "Enable Autodetect", autodetectEnableMenu, enableAutoDetect);
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
            Win32.WritePrivateProfileString("Settings", "enableAutoDetect", enableAutoDetect ? "1" : "0", iniFilePath);
        }
        #endregion

        #region " Menu functions "
        internal static void formatPrettyJsonMenu()
        {
            formatData(FormatType.PrettyJson);
        }

        internal static void formatMiniJsonMenu()
        {
            formatData(FormatType.MiniJson);
        }

        internal static void validateJsonMenu()
        {
            formatData(FormatType.ValidateJson);
        }

        internal static void formatPrettyXmlMenu()
        {
            formatData(FormatType.PrettyXml);
        }

        internal static void formatMiniXmlMenu()
        {
            formatData(FormatType.MiniXml);
        }

        internal static void validateXMLMenu()
        {
            formatData(FormatType.ValidateXml);
        }

        internal static void detectMenu()
        {
            int id = getActiveBuffer();
            removeFileFromCache(id);
            guessIndentation(id, true);

            if (fileCache.ContainsKey(id))
                MessageBox.Show(string.Format("Set indentation settings to: {0}", (fileCache[id].useTabs == 1) ? "Tabs" : "Spaces"), "Info");
            else
                MessageBox.Show("Unable to determine indentation settings.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static void setTabsMenu()
        {
            int id = getActiveBuffer();
            var buff = getBufferInfo(id, 1);
            setUseTabs(1);
            fileCache[id] = buff;
        }

        internal static void setSpacesMenu()
        {
            int id = getActiveBuffer();
            var buff = getBufferInfo(id, 0);
            setUseTabs(0);
            fileCache[id] = buff;
        }

        internal static void autodetectEnableMenu()
        {
            enableAutoDetect = !enableAutoDetect;
            Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[autodetectCmdId]._cmdID,
                Win32.MF_BYCOMMAND | (enableAutoDetect ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
        }
        #endregion

        #region " Worker Functions "
        internal static void formatData(FormatType fType)
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
                    npOut = JsonFormatter.prettyJson(npText, getViewSettings(isSel));
                    setLangType((int)LangType.L_JSON);
                }
                else if (fType == FormatType.MiniJson)
                {
                    npOut = JsonFormatter.miniJson(npText);
                    setLangType((int)LangType.L_JSON);
                }
                else if (fType == FormatType.ValidateJson)
                {
                    JsonFormatter.validateJson(npText);
                    MessageBox.Show("JSON successfully validated  :-)");
                    return;
                }
                else if (fType == FormatType.PrettyXml)
                {
                    npOut = XmlFormatter.prettyXml(npText, getViewSettings(isSel));
                    setLangType((int)LangType.L_XML);
                }
                else if (fType == FormatType.MiniXml)
                {
                    npOut = XmlFormatter.miniXml(npText, getViewSettings(isSel));
                    setLangType((int)LangType.L_XML);
                }
                else if (fType == FormatType.ValidateXml)
                {
                    XmlFormatter.validateXml(npText);
                    MessageBox.Show("XML successfully validated  :-)");
                    return;
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
        internal static ViewSettings getViewSettings(bool isSelection = false)
        {
            var curScintilla = PluginBase.GetCurrentScintilla();
            int tabWidth = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETTABWIDTH, 0, 0);
            var eolMode = (EolMode)(int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETEOLMODE, 0, 0);

            int id = getActiveBuffer();
            bool useTabs;
            if (fileCache.ContainsKey(id))
                useTabs = Convert.ToBoolean(fileCache[id].useTabs);
            else
                useTabs = Convert.ToBoolean((int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETUSETABS, 0, 0));

            return new ViewSettings() { tabWidth = tabWidth, useTabs = useTabs, eolMode = eolMode.str, isSelection = isSelection };
        }

        internal static BufferInfo getBufferInfo(int id, int useTabs = 0)
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            if ((int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, id, path) == -1)
            {
                throw new Exception("Invalid buffer ID.");
            }

            return new BufferInfo() { id = id, path = path.ToString(), useTabs = useTabs };
        }

        internal static int getActiveBuffer()
        {
            return (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
        }

        internal static void setUseTabs(int useTabs)
        {
            Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETUSETABS, useTabs, 0);
        }

        internal static void setLangType(int langType)
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_SETCURRENTLANGTYPE, 0, langType);
        }

        internal static void guessIndentation(int id, bool force = false)
        {
            var curScintilla = PluginBase.GetCurrentScintilla();
            BufferInfo buff;
            if (fileCache.TryGetValue(id, out buff))
            {
                setUseTabs(buff.useTabs);
                return;
            }

            if (!enableAutoDetect && !force)
                return;

            int numLines = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETLINECOUNT, 0, 0);
            if (numLines >= 10)
            {
                var sb = new StringBuilder();
                int count = 0;
                int tabs = 0;
                int size;
                string ws;
                var re = new Regex(@"^\s+");
                for (var i = 0; i < Math.Min(15, numLines); i++)
                {
                    size = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_LINELENGTH, i, 0);
                    sb.Clear();
                    sb.EnsureCapacity(size);
                    Win32.SendMessage(curScintilla, SciMsg.SCI_GETLINE, i, sb);
                    ws = re.Match(sb.ToString()).Value;
                    if (ws.Length > 0)
                        count++;

                    if (ws.Contains("\t"))
                        tabs++;
                }

                //MessageBox.Show(string.Format("Lines: {0}, count: {1}, tabs: {2}\nFile: {3}", numLines, count, tabs, path));

                if (count >= 5)
                {
                    buff = getBufferInfo(id);
                    if (tabs >= 5)
                        buff.useTabs = 1;
                    else
                        buff.useTabs = 0;

                    fileCache[id] = buff;
                    setUseTabs(buff.useTabs);
                }
            }
        }

        internal static void fileSaved(int id)
        {
            if (!enableAutoDetect)
                return;

            removeFileFromCache(id);
            int activeBuf = getActiveBuffer();
            if (activeBuf == id)
                guessIndentation(id);
        }

        internal static void removeFileFromCache(int id)
        {
            fileCache.Remove(id);
        }
        #endregion

        #region " Events "
        internal static void onBufferActivated(int id)
        {
            guessIndentation(id);
        }

        internal static void onFileSaved(int id)
        {
            fileSaved(id);
        }

        internal static void onFileClosed(int id)
        {
            removeFileFromCache(id);
        }

        internal static void onLangChanged(int id)
        {
            if (fileCache.ContainsKey(id))
                setUseTabs(fileCache[id].useTabs);
        }

        internal static void onNppShutdown()
        {
            PluginCleanUp();
        }
        #endregion
    }
}