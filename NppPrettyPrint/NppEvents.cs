using Kbg.NppPluginNET.PluginInfrastructure;
using System;

namespace NppPrettyPrint
{
    internal class NppEvents
    {
        internal readonly NppSettings nps;
        internal readonly NppCommands npc;

        internal NppEvents(NppSettings s, NppCommands c)
        {
            nps = s;
            npc = c;
        }

        internal void OnPluginReady()
        {
            nps.CurScintilla = PluginBase.GetCurrentScintilla();

            // menu modifications
            //IntPtr submenu = Win32Extensions.CreatePopupMenu();
            //Win32Extensions.ModifyMenu(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SubmenuCmdId]._cmdID,
            //    Win32.MF_BYCOMMAND | (int)Win32Extensions.MenuFlags.MF_POPUP, submenu, "Sub menu2");
            //Win32Extensions.DeleteMenu(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[SubmenuItem]._cmdID, Win32.MF_BYCOMMAND);
            //Win32Extensions.AppendMenu(submenu, Win32Extensions.MenuFlags.MF_STRING, PluginBase._funcItems.Items[SubmenuItem]._cmdID, "Sub menu item2");
        }

        internal void OnBeforeOpen(IntPtr id)
        {
            if (npc.IsLargeFile(id))
                npc.SetBufferLangType(id, (int)LangType.L_TEXT);
        }

        internal void OnBufferActivated(IntPtr id)
        {
            nps.CurScintilla = PluginBase.GetCurrentScintilla();
            npc.GuessIndentation(id);
        }

        internal void OnFileSaved(IntPtr id)
        {
            npc.FileSaved(id);
        }

        internal void OnFileClosed(IntPtr id)
        {
            npc.RemoveFileFromCache(id);
        }

        internal void OnLangChanged(IntPtr id)
        {
            if (Main.FileCache.ContainsKey(id))
                npc.SetUseTabs(Main.FileCache[id].UseTabs);
        }
    }
}
