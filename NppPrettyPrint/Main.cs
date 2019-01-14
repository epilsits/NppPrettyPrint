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

        internal static Dictionary<IntPtr, BufferInfo> FileCache = new Dictionary<IntPtr, BufferInfo>();

        static Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromPluginSubFolder);
        }

        private static Assembly LoadFromPluginSubFolder(object sender, ResolveEventArgs args)
        {
            string pluginPath = typeof(Main).Assembly.Location;
            string pluginName = Path.GetFileNameWithoutExtension(pluginPath);
            string pluginSubFolder = Path.Combine(Path.GetDirectoryName(pluginPath), pluginName);
            string assemblyPath = Path.Combine(pluginSubFolder, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath))
                return Assembly.LoadFrom(assemblyPath);
            else
                return null;
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
                NppEvents.OnPluginReady();
            }
            else if (code == (uint)NppMsg.NPPN_FILEBEFOREOPEN)
            {
                NppEvents.OnBeforeOpen(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
            {
                NppEvents.OnBufferActivated(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_FILESAVED)
            {
                NppEvents.OnFileSaved(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_FILECLOSED)
            {
                NppEvents.OnFileClosed(idFrom);
            }
            else if (code == (uint)NppMsg.NPPN_LANGCHANGED)
            {
                NppEvents.OnLangChanged(idFrom);
            }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            NppSettings.IniFilePath = Path.GetFullPath(sbIniFilePath.ToString());
            if (!Directory.Exists(NppSettings.IniFilePath)) Directory.CreateDirectory(NppSettings.IniFilePath);
            NppSettings.IniFilePath = Path.Combine(NppSettings.IniFilePath, Main.PluginName + ".ini");

            NppSettings.ReadSettings();
            NppSettings.WriteSettings();

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
            NppSettings.AutodetectCmdId = cmdIdx++;
            PluginBase.SetCommand(NppSettings.AutodetectCmdId, "Enable Indent Autodetect", AutodetectEnableMenu, NppSettings.EnableAutoDetect);
            PluginBase.SetCommand(cmdIdx++, "---", null);
            NppSettings.SizeDetectCmdId = cmdIdx++;
            PluginBase.SetCommand(NppSettings.SizeDetectCmdId, "Enable File Size Autodetect", SizeDetectEnableMenu, NppSettings.EnableSizeDetect);
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
            FormatCommands.FormatData(FormatType.PrettyJson);
        }

        internal static void FormatPrettyJsonSortedMenu()
        {
            FormatCommands.FormatData(FormatType.PrettyJsonSorted);
        }

        internal static void FormatMiniJsonMenu()
        {
            FormatCommands.FormatData(FormatType.MiniJson);
        }

        internal static void ValidateJsonMenu()
        {
            FormatCommands.FormatData(FormatType.ValidateJson);
        }

        internal static void FormatPrettyXmlMenu()
        {
            FormatCommands.FormatData(FormatType.PrettyXml);
        }

        internal static void FormatPrettyXmlSortedMenu()
        {
            FormatCommands.FormatData(FormatType.PrettyXmlSorted);
        }

        internal static void FormatMiniXmlMenu()
        {
            FormatCommands.FormatData(FormatType.MiniXml);
        }

        internal static void ValidateXMLMenu()
        {
            FormatCommands.FormatData(FormatType.ValidateXml);
        }

        internal static void B64GzipStringMenu()
        {
            FormatCommands.FormatData(FormatType.B64GzipString);
        }
        internal static void B64GzipPrettyJsonMenu()
        {
            FormatCommands.FormatData(FormatType.B64GzipPrettyJson);
        }

        internal static void B64GzipPayloadMenu()
        {
            FormatCommands.FormatData(FormatType.B64GzipPayload);
        }

        internal static void BlobStringMenu()
        {
            FormatCommands.FormatData(FormatType.BlobString);
        }

        internal static void BlobPayloadMenu()
        {
            FormatCommands.FormatData(FormatType.BlobPayload);
        }

        internal static void DetectMenu()
        {
            IntPtr id = NppCommands.GetActiveBuffer();
            NppCommands.RemoveFileFromCache(id);
            NppCommands.GuessIndentation(id, true);

            if (FileCache.ContainsKey(id))
                MessageBox.Show(string.Format("Set indentation settings to: {0}", (FileCache[id].UseTabs == 1) ? "Tabs" : "Spaces"), "Info");
            else
                MessageBox.Show("Unable to determine indentation settings.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static void SetTabsMenu()
        {
            IntPtr id = NppCommands.GetActiveBuffer();
            var buff = NppCommands.GetBufferInfo(id, 1);
            NppCommands.SetUseTabs(1);
            FileCache[id] = buff;
        }

        internal static void SetSpacesMenu()
        {
            IntPtr id = NppCommands.GetActiveBuffer();
            var buff = NppCommands.GetBufferInfo(id, 0);
            NppCommands.SetUseTabs(0);
            FileCache[id] = buff;
        }

        internal static void AutodetectEnableMenu()
        {
            NppSettings.EnableAutoDetect = !NppSettings.EnableAutoDetect;
            NppSettings.ApplySettings();
            NppSettings.WriteSettings();
        }

        internal static void SizeDetectEnableMenu()
        {
            NppSettings.EnableSizeDetect = !NppSettings.EnableSizeDetect;
            NppSettings.ApplySettings();
            NppSettings.WriteSettings();
        }

        internal static void SettingsMenu()
        {
            //Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DOOPEN, 0, IniFilePath);
            using (var settings = new SettingsDialog())
            {
                settings.ValMinLinesToRead = NppSettings.AutodetectMinLinesToRead;
                settings.ValMaxLinesToRead = NppSettings.AutodetectMaxLinesToRead;
                settings.ValMinWhitespaceLines = NppSettings.AutodetectMinWhitespaceLines;
                settings.ValMaxCharsPerLine = NppSettings.AutodetectMaxCharsToReadPerLine;
                settings.ValSizeDetectThreshold = NppSettings.SizeDetectThreshold;
                settings.ValExcludeAttributeValues = NppSettings.XmlSortExcludeAttributeValues.ValToString();
                settings.ValExcludeValueDelimiter = NppSettings.XmlSortExcludeValueDelimiter.ValToString();

                if (DialogResult.OK == settings.ShowDialog())
                {
                    NppSettings.AutodetectMinLinesToRead.Value = settings.ValMinLinesToRead;
                    NppSettings.AutodetectMaxLinesToRead.Value = settings.ValMaxLinesToRead;
                    NppSettings.AutodetectMinWhitespaceLines.Value = settings.ValMinWhitespaceLines;
                    NppSettings.AutodetectMaxCharsToReadPerLine.Value = settings.ValMaxCharsPerLine;
                    NppSettings.SizeDetectThreshold.Value = settings.ValSizeDetectThreshold;
                    NppSettings.XmlSortExcludeAttributeValues.Value = settings.ValExcludeAttributeValues;
                    NppSettings.XmlSortExcludeValueDelimiter.Value = settings.ValExcludeValueDelimiter;

                    NppSettings.ApplySettings();
                    NppSettings.WriteSettings();
                }
            }
        }

        //internal static void NoOpMenu()
        //{ MessageBox.Show("Clicked menu NoOp"); }
        #endregion
    }
}