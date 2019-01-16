using Converters;
using Formatters;
using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Text;
using System.Windows.Forms;

namespace NppPrettyPrint
{
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

    internal class FormatCommands
    {
        internal readonly NppSettings nps;
        internal readonly NppCommands npc;

        internal FormatCommands(NppSettings s, NppCommands c)
        {
            nps = s;
            npc = c;
        }

        internal void FormatData(FormatType fType)
        {
            StringBuilder npText;
            var isSel = false;

            int selLen = (int)Win32.SendMessage(nps.CurScintilla, SciMsg.SCI_GETSELTEXT, 0, 0);
            // len is text + terminating null
            if (selLen > 1)
            {
                isSel = true;
                npText = new StringBuilder(selLen + 1);
                Win32.SendMessage(nps.CurScintilla, SciMsg.SCI_GETSELTEXT, 0, npText);
            }
            else
            {
                int nLen = (int)Win32.SendMessage(nps.CurScintilla, SciMsg.SCI_GETLENGTH, 0, 0);
                npText = new StringBuilder(nLen + 1);
                Win32.SendMessage(nps.CurScintilla, SciMsg.SCI_GETTEXT, nLen + 1, npText);
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

                Win32.SendMessage(nps.CurScintilla, setMsg, 0, npOut);

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

        internal string FormatStringData(StringBuilder npText, FormatType fType, bool isSel)
        {
            string npOut = "";
            var view = npc.GetViewSettings(isSel);
            if (fType == FormatType.PrettyJson)
            {
                npOut = JsonFormatter.PrettyJson(npText, new JsonFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode });
                npc.CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.PrettyJsonSorted)
            {
                npOut = JsonFormatter.PrettyJsonSorted(npText, new JsonFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode });
                npc.CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.MiniJson)
            {
                npOut = JsonFormatter.MiniJson(npText);
                npc.CheckSetLangType(view.Id, (int)LangType.L_JSON);
            }
            else if (fType == FormatType.PrettyXml)
            {
                npOut = XmlFormatter.PrettyXml(npText, new XmlFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode, IsSelection = view.IsSelection });
                npc.CheckSetLangType(view.Id, (int)LangType.L_XML);
            }
            else if (fType == FormatType.PrettyXmlSorted)
            {
                npOut = XmlFormatter.PrettyXmlSorted(npText, new XmlFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode, IsSelection = view.IsSelection },
                    nps.XmlSortExcludeAttributeValues.ValToString().Split(new string[] { nps.XmlSortExcludeValueDelimiter.ValToString() }, StringSplitOptions.RemoveEmptyEntries));
                npc.CheckSetLangType(view.Id, (int)LangType.L_XML);
            }
            else if (fType == FormatType.MiniXml)
            {
                npOut = XmlFormatter.MiniXml(npText, new XmlFormatSettings() { TabWidth = view.TabWidth, UseTabs = view.UseTabs, EolMode = view.EolMode, IsSelection = view.IsSelection });
                npc.CheckSetLangType(view.Id, (int)LangType.L_XML);
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
                npc.CheckSetLangType(view.Id, (int)LangType.L_JSON);
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
    }
}
