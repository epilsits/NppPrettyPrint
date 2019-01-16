using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NppPrettyPrint
{
    static class Main
    {
        internal const string PluginName = "NppPrettyPrint";
        internal static NppSettings nps;
        internal static NppCommands npc;
        internal static NppEvents npe;
        internal static FormatCommands fc;
        internal static Dictionary<IntPtr, BufferInfo> FileCache;

        static Main()
        {
            string pluginPath = typeof(Main).Assembly.Location;
            string pluginName = Path.GetFileNameWithoutExtension(pluginPath);
            string pluginSubFolder = Path.Combine(Path.GetDirectoryName(pluginPath), pluginName);
            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
            {
                string assemblyPath = Path.Combine(pluginSubFolder, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath))
                    return Assembly.LoadFrom(assemblyPath);
                else
                    return null;
            };
            nps = new NppSettings();
            npc = new NppCommands(nps);
            npe = new NppEvents(nps, npc);
            fc = new FormatCommands(nps, npc);
            FileCache = new Dictionary<IntPtr, BufferInfo>();
        }

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
                npe.OnPluginReady();
            }
            else if (code == (uint)NppMsg.NPPN_FILEBEFOREOPEN)
            {
                npe.OnBeforeOpen(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
            {
                npe.OnBufferActivated(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_FILESAVED)
            {
                npe.OnFileSaved(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_FILECLOSED)
            {
                npe.OnFileClosed(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_LANGCHANGED)
            {
                npe.OnLangChanged(idFrom);
            }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            nps.IniFilePath = Path.GetFullPath(sbIniFilePath.ToString());
            if (!Directory.Exists(nps.IniFilePath)) Directory.CreateDirectory(nps.IniFilePath);
            nps.IniFilePath = Path.Combine(nps.IniFilePath, Main.PluginName + ".ini");

            nps.ReadSettings();
            nps.WriteSettings();

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
            nps.AutodetectCmdId = cmdIdx++;
            PluginBase.SetCommand(nps.AutodetectCmdId, "Enable Indent Autodetect", AutodetectEnableMenu, nps.EnableAutoDetect);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            nps.SizeDetectCmdId = cmdIdx++;
            PluginBase.SetCommand(nps.SizeDetectCmdId, "Enable File Size Autodetect", SizeDetectEnableMenu, nps.EnableSizeDetect);
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

        #region " Menu functions "
        internal static void FormatPrettyJsonMenu()
        {
            fc.FormatData(FormatType.PrettyJson);
        }

        internal static void FormatPrettyJsonSortedMenu()
        {
            fc.FormatData(FormatType.PrettyJsonSorted);
        }

        internal static void FormatMiniJsonMenu()
        {
            fc.FormatData(FormatType.MiniJson);
        }

        internal static void ValidateJsonMenu()
        {
            fc.FormatData(FormatType.ValidateJson);
        }

        internal static void FormatPrettyXmlMenu()
        {
            fc.FormatData(FormatType.PrettyXml);
        }

        internal static void FormatPrettyXmlSortedMenu()
        {
            fc.FormatData(FormatType.PrettyXmlSorted);
        }

        internal static void FormatMiniXmlMenu()
        {
            fc.FormatData(FormatType.MiniXml);
        }

        internal static void ValidateXMLMenu()
        {
            fc.FormatData(FormatType.ValidateXml);
        }

        internal static void B64GzipStringMenu()
        {
            fc.FormatData(FormatType.B64GzipString);
        }
        internal static void B64GzipPrettyJsonMenu()
        {
            fc.FormatData(FormatType.B64GzipPrettyJson);
        }

        internal static void B64GzipPayloadMenu()
        {
            fc.FormatData(FormatType.B64GzipPayload);
        }

        internal static void BlobStringMenu()
        {
            fc.FormatData(FormatType.BlobString);
        }

        internal static void BlobPayloadMenu()
        {
            fc.FormatData(FormatType.BlobPayload);
        }

        internal static void DetectMenu()
        {
            IntPtr id = npc.GetActiveBuffer();
            npc.RemoveFileFromCache(id);
            npc.GuessIndentation(id, true);

            if (FileCache.ContainsKey(id))
                MessageBox.Show(string.Format("Set indentation settings to: {0}", (FileCache[id].UseTabs == 1) ? "Tabs" : "Spaces"), "Info");
            else
                MessageBox.Show("Unable to determine indentation settings.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static void SetTabsMenu()
        {
            IntPtr id = npc.GetActiveBuffer();
            var buff = npc.GetBufferInfo(id, 1);
            npc.SetUseTabs(1);
            FileCache[id] = buff;
        }

        internal static void SetSpacesMenu()
        {
            IntPtr id = npc.GetActiveBuffer();
            var buff = npc.GetBufferInfo(id, 0);
            npc.SetUseTabs(0);
            FileCache[id] = buff;
        }

        internal static void AutodetectEnableMenu()
        {
            nps.EnableAutoDetect = !nps.EnableAutoDetect;
            nps.ApplySettings();
            nps.WriteSettings();
        }

        internal static void SizeDetectEnableMenu()
        {
            nps.EnableSizeDetect = !nps.EnableSizeDetect;
            nps.ApplySettings();
            nps.WriteSettings();
        }

        internal static void SettingsMenu()
        {
            //Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DOOPEN, 0, IniFilePath);
            using (var settings = new SettingsDialog())
            {
                settings.ValMinLinesToRead = nps.AutodetectMinLinesToRead;
                settings.ValMaxLinesToRead = nps.AutodetectMaxLinesToRead;
                settings.ValMinWhitespaceLines = nps.AutodetectMinWhitespaceLines;
                settings.ValMaxCharsPerLine = nps.AutodetectMaxCharsToReadPerLine;
                settings.ValSizeDetectThreshold = nps.SizeDetectThreshold;
                settings.ValExcludeAttributeValues = nps.XmlSortExcludeAttributeValues.ValToString();
                settings.ValExcludeValueDelimiter = nps.XmlSortExcludeValueDelimiter.ValToString();

                if (DialogResult.OK == settings.ShowDialog())
                {
                    nps.AutodetectMinLinesToRead.Value = settings.ValMinLinesToRead;
                    nps.AutodetectMaxLinesToRead.Value = settings.ValMaxLinesToRead;
                    nps.AutodetectMinWhitespaceLines.Value = settings.ValMinWhitespaceLines;
                    nps.AutodetectMaxCharsToReadPerLine.Value = settings.ValMaxCharsPerLine;
                    nps.SizeDetectThreshold.Value = settings.ValSizeDetectThreshold;
                    nps.XmlSortExcludeAttributeValues.Value = settings.ValExcludeAttributeValues;
                    nps.XmlSortExcludeValueDelimiter.Value = settings.ValExcludeValueDelimiter;

                    nps.ApplySettings();
                    nps.WriteSettings();
                }
            }
        }

        //internal static void NoOpMenu()
        //{ MessageBox.Show("Clicked menu NoOp"); }
        #endregion
    }
}