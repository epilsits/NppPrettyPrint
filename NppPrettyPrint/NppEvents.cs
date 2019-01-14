using Kbg.NppPluginNET.PluginInfrastructure;
using System;

namespace NppPrettyPrint
{
    static class NppEvents
    {
        internal static void OnPluginReady()
        {
            NppSettings.CurScintilla = PluginBase.GetCurrentScintilla();

            // menu modifications
            //IntPtr submenu = Win32Extensions.CreatePopupMenu();
            //Win32Extensions.ModifyMenu(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SubmenuCmdId]._cmdID,
            //    Win32.MF_BYCOMMAND | (int)Win32Extensions.MenuFlags.MF_POPUP, submenu, "Sub menu2");
            //Win32Extensions.DeleteMenu(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SubmenuItem]._cmdID, Win32.MF_BYCOMMAND);
            //Win32Extensions.AppendMenu(submenu, Win32Extensions.MenuFlags.MF_STRING, PluginBase._funcItems.Items[SubmenuItem]._cmdID, "Sub menu item2");
        }

        internal static void OnBeforeOpen(IntPtr id)
        {
            if (NppCommands.IsLargeFile(id))
                NppCommands.SetBufferLangType(id, (int)LangType.L_TEXT);
        }

        internal static void OnBufferActivated(IntPtr id)
        {
            NppSettings.CurScintilla = PluginBase.GetCurrentScintilla();
            NppCommands.GuessIndentation(id);
        }

        internal static void OnFileSaved(IntPtr id)
        {
            NppCommands.FileSaved(id);
        }

        internal static void OnFileClosed(IntPtr id)
        {
            NppCommands.RemoveFileFromCache(id);
        }

        internal static void OnLangChanged(IntPtr id)
        {
            if (Main.FileCache.ContainsKey(id))
                NppCommands.SetUseTabs(Main.FileCache[id].UseTabs);
        }
    }
}
