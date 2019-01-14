using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NppPrettyPrint
{
    internal struct BufferInfo
    {
        internal IntPtr Id;
        internal string Path;
        internal int UseTabs;
    }

    static class NppCommands
    {
        internal struct ViewSettings
        {
            public IntPtr Id;
            public int TabWidth;
            public bool UseTabs;
            public string EolMode;
            public bool IsSelection;
        }

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

        internal static ViewSettings GetViewSettings(bool isSelection = false)
        {
            int tabWidth = (int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETTABWIDTH, 0, 0);
            var eolMode = (EolMode)(int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETEOLMODE, 0, 0);

            IntPtr id = GetActiveBuffer();
            bool useTabs;
            if (Main.FileCache.ContainsKey(id))
                useTabs = Convert.ToBoolean(Main.FileCache[id].UseTabs);
            else
                useTabs = Convert.ToBoolean((int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETUSETABS, 0, 0));

            return new ViewSettings() { Id = id, TabWidth = tabWidth, UseTabs = useTabs, EolMode = eolMode.Str, IsSelection = isSelection };
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
            Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_SETUSETABS, useTabs, 0);
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
            if (Main.FileCache.TryGetValue(id, out BufferInfo buff))
            {
                SetUseTabs(buff.UseTabs);
                return;
            }

            if (!NppSettings.EnableAutoDetect && !force)
                return;

            int numLines = (int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETLINECOUNT, 0, 0);
            if (numLines >= NppSettings.AutodetectMinLinesToRead)
            {
                int wsLines = 0;
                int tabLines = 0;
                var ttf = new TextToFind(0, 0, @"^\s+");
                for (var i = 0; i < Math.Min(NppSettings.AutodetectMaxLinesToRead, numLines); i++)
                {
                    int startPos = (int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_POSITIONFROMLINE, i, 0);
                    int endPos = (int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETLINEENDPOSITION, i, 0); // excl EOL chars
                    int lineLen = endPos - startPos;
                    if (lineLen > 0)
                    {
                        ttf.chrg = new CharacterRange(startPos, startPos + Math.Min(lineLen, NppSettings.AutodetectMaxCharsToReadPerLine));
                        int find = (int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_FINDTEXT, (int)(SciMsg.SCFIND_REGEXP | SciMsg.SCFIND_CXX11REGEX), ttf.NativePointer);
                        if (find != -1)
                        {
                            wsLines++;
                            var rgFind = ttf.chrgText;
                            var tr = new TextRange(rgFind, rgFind.cpMax - rgFind.cpMin + 1);
                            Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                            if (tr.lpstrText.Contains("\t"))
                                tabLines++;

                            //MessageBox.Show(string.Format("Line: {3}\nFind start: {0}\nFind end: {1}\nFind len: {2}",
                            //    rgFind.cpMin, rgFind.cpMax, rgFind.cpMax - rgFind.cpMin, i + 1));
                        }
                    }
                }

                if (wsLines >= NppSettings.AutodetectMinWhitespaceLines)
                {
                    buff = GetBufferInfo(id);
                    if (tabLines >= (wsLines - tabLines))
                        buff.UseTabs = 1;
                    else
                        buff.UseTabs = 0;

                    Main.FileCache[id] = buff;
                    SetUseTabs(buff.UseTabs);
                }

                //MessageBox.Show(string.Format("Lines: {0}, count: {1}, tabs: {2}\nFile: {3}", numLines, wsLines, tabLines, buff.path));
            }
        }

        internal static void FileSaved(IntPtr id)
        {
            var buff = GetBufferInfo(id);
            if (string.Equals(buff.Path, NppSettings.IniFilePath, StringComparison.OrdinalIgnoreCase))
            {
                NppSettings.ReadSettings();
                NppSettings.ApplySettings();
            }

            if (!NppSettings.EnableAutoDetect)
                return;

            RemoveFileFromCache(id);
            IntPtr activeBuf = GetActiveBuffer();
            if (activeBuf == id)
                GuessIndentation(id);
        }

        internal static void RemoveFileFromCache(IntPtr id)
        {
            Main.FileCache.Remove(id);
        }

        internal static bool IsLargeFile(IntPtr id)
        {
            if (NppSettings.EnableSizeDetect)
            {
                var buff = GetBufferInfo(id);
                try
                {
                    long fileSize = new FileInfo(buff.Path).Length;
                    if (fileSize > NppSettings.SizeDetectThreshold)
                        return true;
                }
                catch { }
            }

            return false;
        }

        internal static bool IsLargeBuffer()
        {
            if (NppSettings.EnableSizeDetect)
            {
                int nLen = (int)Win32.SendMessage(NppSettings.CurScintilla, SciMsg.SCI_GETLENGTH, 0, 0);
                if (nLen > NppSettings.SizeDetectThreshold)
                    return true;
            }

            return false;
        }
    }
}
