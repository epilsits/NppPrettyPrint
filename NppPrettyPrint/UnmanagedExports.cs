using System;
using System.Runtime.InteropServices;
using NppPluginNET;
using NppPlugin.DllExport;

namespace NppPrettyPrint
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention=CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            PluginBase.nppData = notepadPlusData;
            Plugin.CommandMenuInit();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = PluginBase._funcItems.Items.Count;
            return PluginBase._funcItems.NativePointer;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getName()
        {
            if (_ptrPluginName == IntPtr.Zero)
                _ptrPluginName = Marshal.StringToHGlobalUni(Plugin.PluginName);
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));
            if (nc.nmhdr.code == (uint)NppMsg.NPPN_TBMODIFICATION)
            {
                PluginBase._funcItems.RefreshItems();
                //Main.SetToolBarIcon();
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
            {
                Plugin.onBufferActivated((int)nc.nmhdr.idFrom);
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILESAVED)
            {
                Plugin.onFileSaved((int)nc.nmhdr.idFrom);
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILECLOSED)
            {
                Plugin.onFileClosed((int)nc.nmhdr.idFrom);
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_LANGCHANGED)
            {
                Plugin.onLangChanged((int)nc.nmhdr.idFrom);
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_SHUTDOWN)
            {
                Plugin.onNppShutdown();
                Marshal.FreeHGlobal(_ptrPluginName);
            }
        }
    }
}
