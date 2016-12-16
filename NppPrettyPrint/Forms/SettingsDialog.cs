using NppPrettyPrint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NppPrettyPrint
{
    public partial class SettingsDialog : Form
    {
        public int valMinLinesToRead { get; set; }
        public int valMaxLinesToRead { get; set; }
        public int valMinWhitespaceLines { get; set; }
        public int valMaxCharsPerLine { get; set; }
        public string valExcludeAttributeValues { get; set; }
        public string valExcludeValueDelimiter { get; set; }

        public SettingsDialog()
        {
            InitializeComponent();
            settingsDialogBindingSource.DataSource = this;
        }
    }
}
