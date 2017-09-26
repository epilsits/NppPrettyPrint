using System.Windows.Forms;

namespace NppPrettyPrint
{
    public partial class SettingsDialog : Form
    {
        public int ValMinLinesToRead { get; set; }
        public int ValMaxLinesToRead { get; set; }
        public int ValMinWhitespaceLines { get; set; }
        public int ValMaxCharsPerLine { get; set; }
        public int ValSizeDetectThreshold { get; set; }
        public string ValExcludeAttributeValues { get; set; }
        public string ValExcludeValueDelimiter { get; set; }

        public SettingsDialog()
        {
            InitializeComponent();
            settingsDialogBindingSource.DataSource = this;
        }
    }
}
