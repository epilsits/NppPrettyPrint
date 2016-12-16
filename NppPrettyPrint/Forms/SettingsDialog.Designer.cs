namespace NppPrettyPrint
{
    partial class SettingsDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsDialog));
            this.ButtonSave = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.maxCharsPerLine = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.minWhitespaceLines = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.maxLinesToRead = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.minLinesToRead = new System.Windows.Forms.NumericUpDown();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.excludeValueDelimiter = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.excludeAttributeValues = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.settingsToolTips = new System.Windows.Forms.ToolTip(this.components);
            this.settingsDialogBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maxCharsPerLine)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minWhitespaceLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxLinesToRead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minLinesToRead)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.settingsDialogBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // ButtonSave
            // 
            this.ButtonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ButtonSave.Location = new System.Drawing.Point(260, 233);
            this.ButtonSave.Name = "ButtonSave";
            this.ButtonSave.Size = new System.Drawing.Size(75, 23);
            this.ButtonSave.TabIndex = 0;
            this.ButtonSave.Text = "Save";
            this.ButtonSave.UseVisualStyleBackColor = true;
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(359, 233);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 1;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.maxCharsPerLine);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.minWhitespaceLines);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.maxLinesToRead);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.minLinesToRead);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(421, 82);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Indent Autodetect";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(262, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(129, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Max chars to read per line";
            // 
            // maxCharsPerLine
            // 
            this.maxCharsPerLine.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.settingsDialogBindingSource, "valMaxCharsPerLine", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.maxCharsPerLine.Location = new System.Drawing.Point(208, 46);
            this.maxCharsPerLine.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.maxCharsPerLine.Name = "maxCharsPerLine";
            this.maxCharsPerLine.Size = new System.Drawing.Size(48, 20);
            this.maxCharsPerLine.TabIndex = 6;
            this.settingsToolTips.SetToolTip(this.maxCharsPerLine, "Maximum number of characters to read from each line.");
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(262, 27);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(123, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Min req whitespace lines";
            // 
            // minWhitespaceLines
            // 
            this.minWhitespaceLines.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.settingsDialogBindingSource, "valMinWhitespaceLines", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.minWhitespaceLines.Location = new System.Drawing.Point(208, 20);
            this.minWhitespaceLines.Name = "minWhitespaceLines";
            this.minWhitespaceLines.Size = new System.Drawing.Size(48, 20);
            this.minWhitespaceLines.TabIndex = 4;
            this.settingsToolTips.SetToolTip(this.minWhitespaceLines, "Minimum number of lines that begin with whitespace to enable indent autodetect.");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(61, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Max lines to read";
            // 
            // maxLinesToRead
            // 
            this.maxLinesToRead.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.settingsDialogBindingSource, "valMaxLinesToRead", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.maxLinesToRead.Location = new System.Drawing.Point(7, 46);
            this.maxLinesToRead.Name = "maxLinesToRead";
            this.maxLinesToRead.Size = new System.Drawing.Size(48, 20);
            this.maxLinesToRead.TabIndex = 2;
            this.settingsToolTips.SetToolTip(this.maxLinesToRead, "Maximum number of lines that will be read when autodetecting indent settings.");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(61, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Min lines to read";
            // 
            // minLinesToRead
            // 
            this.minLinesToRead.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.settingsDialogBindingSource, "valMinLinesToRead", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.minLinesToRead.Location = new System.Drawing.Point(7, 20);
            this.minLinesToRead.Name = "minLinesToRead";
            this.minLinesToRead.Size = new System.Drawing.Size(48, 20);
            this.minLinesToRead.TabIndex = 0;
            this.settingsToolTips.SetToolTip(this.minLinesToRead, "Minimum number of lines in the document to enable indent autodetect.");
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.excludeValueDelimiter);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.excludeAttributeValues);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(13, 102);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(421, 121);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Xml Sorting";
            // 
            // excludeValueDelimiter
            // 
            this.excludeValueDelimiter.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.settingsDialogBindingSource, "valExcludeValueDelimiter", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.excludeValueDelimiter.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.excludeValueDelimiter.Location = new System.Drawing.Point(10, 85);
            this.excludeValueDelimiter.MaxLength = 1;
            this.excludeValueDelimiter.Name = "excludeValueDelimiter";
            this.excludeValueDelimiter.Size = new System.Drawing.Size(24, 23);
            this.excludeValueDelimiter.TabIndex = 3;
            this.excludeValueDelimiter.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.settingsToolTips.SetToolTip(this.excludeValueDelimiter, "The delimiter character for the exclusion list.");
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 68);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(136, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Excluded value list delimiter";
            // 
            // excludeAttributeValues
            // 
            this.excludeAttributeValues.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.settingsDialogBindingSource, "valExcludeAttributeValues", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.excludeAttributeValues.Location = new System.Drawing.Point(10, 37);
            this.excludeAttributeValues.Name = "excludeAttributeValues";
            this.excludeAttributeValues.Size = new System.Drawing.Size(405, 20);
            this.excludeAttributeValues.TabIndex = 1;
            this.settingsToolTips.SetToolTip(this.excludeAttributeValues, resources.GetString("excludeAttributeValues.ToolTip"));
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 20);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(185, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Exclude attribute values (delimited list)";
            // 
            // settingsToolTips
            // 
            this.settingsToolTips.AutoPopDelay = 10000;
            this.settingsToolTips.InitialDelay = 500;
            this.settingsToolTips.ReshowDelay = 100;
            // 
            // settingsDialogBindingSource
            // 
            this.settingsDialogBindingSource.DataSource = typeof(NppPrettyPrint.SettingsDialog);
            // 
            // SettingsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 268);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SettingsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.TopMost = true;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maxCharsPerLine)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minWhitespaceLines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxLinesToRead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minLinesToRead)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.settingsDialogBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonSave;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown minLinesToRead;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown maxLinesToRead;
        private System.Windows.Forms.NumericUpDown maxCharsPerLine;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown minWhitespaceLines;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox excludeAttributeValues;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox excludeValueDelimiter;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.BindingSource settingsDialogBindingSource;
        private System.Windows.Forms.ToolTip settingsToolTips;
    }
}