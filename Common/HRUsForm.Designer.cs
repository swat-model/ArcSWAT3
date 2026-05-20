using System.Windows.Forms;

namespace ArcSWAT3
{
    partial class HRUsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>

        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HRUsForm));
            this.landuseSlider = new System.Windows.Forms.TrackBar();
            this.selectLanduse = new System.Windows.Forms.TextBox();
            this.selectLanduselabel = new System.Windows.Forms.Label();
            this.selectLanduseButton = new System.Windows.Forms.Button();
            this.selectLanduseTable = new System.Windows.Forms.ComboBox();
            this.landuseTableLabel = new System.Windows.Forms.Label();
            this.selectSoil = new System.Windows.Forms.TextBox();
            this.selectSoilButton = new System.Windows.Forms.Button();
            this.selectSoilTable = new System.Windows.Forms.ComboBox();
            this.soilTableLabel = new System.Windows.Forms.Label();
            this.soilGroup = new System.Windows.Forms.GroupBox();
            this.usersoilButton = new System.Windows.Forms.RadioButton();
            this.STATSGOButton = new System.Windows.Forms.RadioButton();
            this.SSURGOButton = new System.Windows.Forms.RadioButton();
            this.generateFullHRUs = new System.Windows.Forms.CheckBox();
            this.readChoiceGroup = new System.Windows.Forms.GroupBox();
            this.readFromPrevious = new System.Windows.Forms.RadioButton();
            this.readFromMaps = new System.Windows.Forms.RadioButton();
            this.readButton = new System.Windows.Forms.Button();
            this.fullHRUsLabel = new System.Windows.Forms.Label();
            this.slopeGroup = new System.Windows.Forms.GroupBox();
            this.slopeBandsLabel = new System.Windows.Forms.Label();
            this.clearButton = new System.Windows.Forms.Button();
            this.insertButton = new System.Windows.Forms.Button();
            this.slopeBrowser = new System.Windows.Forms.TextBox();
            this.slopeBand = new System.Windows.Forms.TextBox();
            this.hruChoiceGroup = new System.Windows.Forms.GroupBox();
            this.targetButton = new System.Windows.Forms.RadioButton();
            this.filterAreaButton = new System.Windows.Forms.RadioButton();
            this.filterLanduseButton = new System.Windows.Forms.RadioButton();
            this.dominantHRUButton = new System.Windows.Forms.RadioButton();
            this.dominantLanduseButton = new System.Windows.Forms.RadioButton();
            this.landuseSoilSlopeGroup = new System.Windows.Forms.GroupBox();
            this.slopeVal = new System.Windows.Forms.TextBox();
            this.slopeMax = new System.Windows.Forms.Label();
            this.slopeLabel = new System.Windows.Forms.Label();
            this.slopeMin = new System.Windows.Forms.Label();
            this.slopeSlider = new System.Windows.Forms.TrackBar();
            this.soilButton = new System.Windows.Forms.Button();
            this.soilVal = new System.Windows.Forms.TextBox();
            this.soilMax = new System.Windows.Forms.Label();
            this.soilLabel = new System.Windows.Forms.Label();
            this.soilMin = new System.Windows.Forms.Label();
            this.soilSlider = new System.Windows.Forms.TrackBar();
            this.landuseButton = new System.Windows.Forms.Button();
            this.landuseVal = new System.Windows.Forms.TextBox();
            this.landuseMax = new System.Windows.Forms.Label();
            this.landuseLabel = new System.Windows.Forms.Label();
            this.landuseMin = new System.Windows.Forms.Label();
            this.areaPercentChoiceGroup = new System.Windows.Forms.GroupBox();
            this.areaButton = new System.Windows.Forms.RadioButton();
            this.percentButton = new System.Windows.Forms.RadioButton();
            this.optionGroup = new System.Windows.Forms.GroupBox();
            this.elevBandsButton = new System.Windows.Forms.Button();
            this.exemptButton = new System.Windows.Forms.Button();
            this.splitButton = new System.Windows.Forms.Button();
            this.progressLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.areaGroup = new System.Windows.Forms.GroupBox();
            this.areaVal = new System.Windows.Forms.TextBox();
            this.areaMax = new System.Windows.Forms.Label();
            this.areaLabel = new System.Windows.Forms.Label();
            this.areaMin = new System.Windows.Forms.Label();
            this.areaSlider = new System.Windows.Forms.TrackBar();
            this.targetGroup = new System.Windows.Forms.GroupBox();
            this.targetVal = new System.Windows.Forms.TextBox();
            this.targetMax = new System.Windows.Forms.Label();
            this.targetLabel = new System.Windows.Forms.Label();
            this.targetMin = new System.Windows.Forms.Label();
            this.targetSlider = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.landuseSlider)).BeginInit();
            this.soilGroup.SuspendLayout();
            this.readChoiceGroup.SuspendLayout();
            this.slopeGroup.SuspendLayout();
            this.hruChoiceGroup.SuspendLayout();
            this.landuseSoilSlopeGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.slopeSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.soilSlider)).BeginInit();
            this.areaPercentChoiceGroup.SuspendLayout();
            this.optionGroup.SuspendLayout();
            this.areaGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.areaSlider)).BeginInit();
            this.targetGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.targetSlider)).BeginInit();
            this.SuspendLayout();
            // 
            // landuseSlider
            // 
            this.landuseSlider.Location = new System.Drawing.Point(6, 57);
            this.landuseSlider.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landuseSlider.Name = "landuseSlider";
            this.landuseSlider.Size = new System.Drawing.Size(137, 45);
            this.landuseSlider.TabIndex = 0;
            this.landuseSlider.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.landuseSlider.ValueChanged += new System.EventHandler(this.landuseSlider_ValueChanged);
            // 
            // selectLanduse
            // 
            this.selectLanduse.Location = new System.Drawing.Point(11, 25);
            this.selectLanduse.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.selectLanduse.Name = "selectLanduse";
            this.selectLanduse.Size = new System.Drawing.Size(396, 23);
            this.selectLanduse.TabIndex = 0;
            // 
            // selectLanduselabel
            // 
            this.selectLanduselabel.AutoSize = true;
            this.selectLanduselabel.Location = new System.Drawing.Point(11, 7);
            this.selectLanduselabel.Name = "selectLanduselabel";
            this.selectLanduselabel.Size = new System.Drawing.Size(109, 15);
            this.selectLanduselabel.TabIndex = 1;
            this.selectLanduselabel.Text = "Select landuse map";
            // 
            // selectLanduseButton
            // 
            this.selectLanduseButton.Location = new System.Drawing.Point(413, 25);
            this.selectLanduseButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.selectLanduseButton.Name = "selectLanduseButton";
            this.selectLanduseButton.Size = new System.Drawing.Size(51, 24);
            this.selectLanduseButton.TabIndex = 2;
            this.selectLanduseButton.Text = "...";
            this.selectLanduseButton.UseVisualStyleBackColor = true;
            this.selectLanduseButton.Click += new System.EventHandler(this.selectLanduseButton_Click);
            // 
            // selectLanduseTable
            // 
            this.selectLanduseTable.FormattingEnabled = true;
            this.selectLanduseTable.Location = new System.Drawing.Point(358, 54);
            this.selectLanduseTable.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.selectLanduseTable.Name = "selectLanduseTable";
            this.selectLanduseTable.Size = new System.Drawing.Size(106, 23);
            this.selectLanduseTable.TabIndex = 3;
            this.selectLanduseTable.SelectionChangeCommitted += new System.EventHandler(this.selectLanduseTable_SelectionChangeCommitted);
            // 
            // landuseTableLabel
            // 
            this.landuseTableLabel.AutoSize = true;
            this.landuseTableLabel.Location = new System.Drawing.Point(258, 57);
            this.landuseTableLabel.Name = "landuseTableLabel";
            this.landuseTableLabel.Size = new System.Drawing.Size(80, 15);
            this.landuseTableLabel.TabIndex = 4;
            this.landuseTableLabel.Text = "Landuse table";
            // 
            // selectSoil
            // 
            this.selectSoil.Location = new System.Drawing.Point(10, 81);
            this.selectSoil.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.selectSoil.Name = "selectSoil";
            this.selectSoil.Size = new System.Drawing.Size(397, 23);
            this.selectSoil.TabIndex = 5;
            // 
            // selectSoilButton
            // 
            this.selectSoilButton.Location = new System.Drawing.Point(413, 81);
            this.selectSoilButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.selectSoilButton.Name = "selectSoilButton";
            this.selectSoilButton.Size = new System.Drawing.Size(51, 23);
            this.selectSoilButton.TabIndex = 6;
            this.selectSoilButton.Text = "...";
            this.selectSoilButton.UseVisualStyleBackColor = true;
            this.selectSoilButton.Click += new System.EventHandler(this.selectSoilButton_Click);
            // 
            // selectSoilTable
            // 
            this.selectSoilTable.FormattingEnabled = true;
            this.selectSoilTable.Location = new System.Drawing.Point(358, 108);
            this.selectSoilTable.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.selectSoilTable.Name = "selectSoilTable";
            this.selectSoilTable.Size = new System.Drawing.Size(106, 23);
            this.selectSoilTable.TabIndex = 7;
            this.selectSoilTable.SelectionChangeCommitted += new System.EventHandler(this.selectSoilTable_SelectionChangeCommitted);
            // 
            // soilTableLabel
            // 
            this.soilTableLabel.AutoSize = true;
            this.soilTableLabel.Location = new System.Drawing.Point(281, 111);
            this.soilTableLabel.Name = "soilTableLabel";
            this.soilTableLabel.Size = new System.Drawing.Size(55, 15);
            this.soilTableLabel.TabIndex = 8;
            this.soilTableLabel.Text = "Soil table";
            // 
            // soilGroup
            // 
            this.soilGroup.Controls.Add(this.usersoilButton);
            this.soilGroup.Controls.Add(this.STATSGOButton);
            this.soilGroup.Controls.Add(this.SSURGOButton);
            this.soilGroup.Location = new System.Drawing.Point(10, 123);
            this.soilGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.soilGroup.Name = "soilGroup";
            this.soilGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.soilGroup.Size = new System.Drawing.Size(284, 41);
            this.soilGroup.TabIndex = 9;
            this.soilGroup.TabStop = false;
            this.soilGroup.Text = "Soil data";
            // 
            // usersoilButton
            // 
            this.usersoilButton.AutoSize = true;
            this.usersoilButton.Checked = true;
            this.usersoilButton.Location = new System.Drawing.Point(5, 16);
            this.usersoilButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.usersoilButton.Name = "usersoilButton";
            this.usersoilButton.Size = new System.Drawing.Size(65, 19);
            this.usersoilButton.TabIndex = 2;
            this.usersoilButton.TabStop = true;
            this.usersoilButton.Text = "usersoil";
            this.usersoilButton.UseVisualStyleBackColor = true;
            this.usersoilButton.CheckedChanged += new System.EventHandler(this.usersoilButton_CheckedChanged);
            // 
            // STATSGOButton
            // 
            this.STATSGOButton.AutoSize = true;
            this.STATSGOButton.Location = new System.Drawing.Point(76, 16);
            this.STATSGOButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.STATSGOButton.Name = "STATSGOButton";
            this.STATSGOButton.Size = new System.Drawing.Size(72, 19);
            this.STATSGOButton.TabIndex = 1;
            this.STATSGOButton.Text = "STATSGO";
            this.STATSGOButton.UseVisualStyleBackColor = true;
            this.STATSGOButton.CheckedChanged += new System.EventHandler(this.STATSGOButton_CheckedChanged);
            // 
            // SSURGOButton
            // 
            this.SSURGOButton.AutoSize = true;
            this.SSURGOButton.Location = new System.Drawing.Point(157, 16);
            this.SSURGOButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.SSURGOButton.Name = "SSURGOButton";
            this.SSURGOButton.Size = new System.Drawing.Size(127, 19);
            this.SSURGOButton.TabIndex = 0;
            this.SSURGOButton.Text = "SSURGO/STATSGO2";
            this.SSURGOButton.UseVisualStyleBackColor = true;
            this.SSURGOButton.CheckedChanged += new System.EventHandler(this.SSURGOButton_CheckedChanged);
            // 
            // generateFullHRUs
            // 
            this.generateFullHRUs.AutoSize = true;
            this.generateFullHRUs.Location = new System.Drawing.Point(17, 195);
            this.generateFullHRUs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.generateFullHRUs.Name = "generateFullHRUs";
            this.generateFullHRUs.Size = new System.Drawing.Size(76, 49);
            this.generateFullHRUs.TabIndex = 10;
            this.generateFullHRUs.Text = "Generate \r\nFullHRUs\r\nshapefile";
            this.generateFullHRUs.UseVisualStyleBackColor = true;
            // 
            // readChoiceGroup
            // 
            this.readChoiceGroup.Controls.Add(this.readFromPrevious);
            this.readChoiceGroup.Controls.Add(this.readFromMaps);
            this.readChoiceGroup.Location = new System.Drawing.Point(107, 169);
            this.readChoiceGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.readChoiceGroup.Name = "readChoiceGroup";
            this.readChoiceGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.readChoiceGroup.Size = new System.Drawing.Size(144, 75);
            this.readChoiceGroup.TabIndex = 11;
            this.readChoiceGroup.TabStop = false;
            this.readChoiceGroup.Text = "Read choice";
            // 
            // readFromPrevious
            // 
            this.readFromPrevious.AutoSize = true;
            this.readFromPrevious.Location = new System.Drawing.Point(11, 41);
            this.readFromPrevious.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.readFromPrevious.Name = "readFromPrevious";
            this.readFromPrevious.Size = new System.Drawing.Size(91, 34);
            this.readFromPrevious.TabIndex = 1;
            this.readFromPrevious.Text = "Read from\r\nprevious run";
            this.readFromPrevious.UseVisualStyleBackColor = true;
            this.readFromPrevious.CheckedChanged += new System.EventHandler(this.readFromPrevious_CheckedChanged);
            // 
            // readFromMaps
            // 
            this.readFromMaps.AutoSize = true;
            this.readFromMaps.Checked = true;
            this.readFromMaps.Location = new System.Drawing.Point(12, 19);
            this.readFromMaps.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.readFromMaps.Name = "readFromMaps";
            this.readFromMaps.Size = new System.Drawing.Size(112, 19);
            this.readFromMaps.TabIndex = 0;
            this.readFromMaps.TabStop = true;
            this.readFromMaps.Text = "Read from maps";
            this.readFromMaps.UseVisualStyleBackColor = true;
            this.readFromMaps.CheckedChanged += new System.EventHandler(this.readFromMaps_CheckedChanged);
            // 
            // readButton
            // 
            this.readButton.Location = new System.Drawing.Point(385, 166);
            this.readButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.readButton.Name = "readButton";
            this.readButton.Size = new System.Drawing.Size(66, 23);
            this.readButton.TabIndex = 12;
            this.readButton.Text = "Read";
            this.readButton.UseVisualStyleBackColor = true;
            this.readButton.Click += new System.EventHandler(this.readButton_Click);
            // 
            // fullHRUsLabel
            // 
            this.fullHRUsLabel.AutoSize = true;
            this.fullHRUsLabel.Location = new System.Drawing.Point(284, 199);
            this.fullHRUsLabel.Name = "fullHRUsLabel";
            this.fullHRUsLabel.Size = new System.Drawing.Size(98, 15);
            this.fullHRUsLabel.TabIndex = 13;
            this.fullHRUsLabel.Text = "Full HRUs count: ";
            // 
            // slopeGroup
            // 
            this.slopeGroup.Controls.Add(this.slopeBandsLabel);
            this.slopeGroup.Controls.Add(this.clearButton);
            this.slopeGroup.Controls.Add(this.insertButton);
            this.slopeGroup.Controls.Add(this.slopeBrowser);
            this.slopeGroup.Controls.Add(this.slopeBand);
            this.slopeGroup.Location = new System.Drawing.Point(10, 258);
            this.slopeGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slopeGroup.Name = "slopeGroup";
            this.slopeGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slopeGroup.Size = new System.Drawing.Size(127, 133);
            this.slopeGroup.TabIndex = 14;
            this.slopeGroup.TabStop = false;
            this.slopeGroup.Text = "Set bands for slope (%)";
            // 
            // slopeBandsLabel
            // 
            this.slopeBandsLabel.AutoSize = true;
            this.slopeBandsLabel.Location = new System.Drawing.Point(7, 84);
            this.slopeBandsLabel.Name = "slopeBandsLabel";
            this.slopeBandsLabel.Size = new System.Drawing.Size(71, 15);
            this.slopeBandsLabel.TabIndex = 4;
            this.slopeBandsLabel.Text = "Slope bands";
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(49, 60);
            this.clearButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(65, 22);
            this.clearButton.TabIndex = 3;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // insertButton
            // 
            this.insertButton.Location = new System.Drawing.Point(49, 32);
            this.insertButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.insertButton.Name = "insertButton";
            this.insertButton.Size = new System.Drawing.Size(65, 24);
            this.insertButton.TabIndex = 2;
            this.insertButton.Text = "Insert";
            this.insertButton.UseVisualStyleBackColor = true;
            this.insertButton.Click += new System.EventHandler(this.insertButton_Click);
            // 
            // slopeBrowser
            // 
            this.slopeBrowser.Location = new System.Drawing.Point(7, 105);
            this.slopeBrowser.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slopeBrowser.Name = "slopeBrowser";
            this.slopeBrowser.Size = new System.Drawing.Size(104, 23);
            this.slopeBrowser.TabIndex = 1;
            this.slopeBrowser.Enter += new System.EventHandler(this.slopeBrowser_Enter);
            // 
            // slopeBand
            // 
            this.slopeBand.Location = new System.Drawing.Point(9, 35);
            this.slopeBand.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slopeBand.Name = "slopeBand";
            this.slopeBand.Size = new System.Drawing.Size(34, 23);
            this.slopeBand.TabIndex = 0;
            // 
            // hruChoiceGroup
            // 
            this.hruChoiceGroup.Controls.Add(this.targetButton);
            this.hruChoiceGroup.Controls.Add(this.filterAreaButton);
            this.hruChoiceGroup.Controls.Add(this.filterLanduseButton);
            this.hruChoiceGroup.Controls.Add(this.dominantHRUButton);
            this.hruChoiceGroup.Controls.Add(this.dominantLanduseButton);
            this.hruChoiceGroup.Location = new System.Drawing.Point(140, 251);
            this.hruChoiceGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.hruChoiceGroup.Name = "hruChoiceGroup";
            this.hruChoiceGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.hruChoiceGroup.Size = new System.Drawing.Size(135, 193);
            this.hruChoiceGroup.TabIndex = 15;
            this.hruChoiceGroup.TabStop = false;
            this.hruChoiceGroup.Text = "Single/Multiple HRUs";
            // 
            // targetButton
            // 
            this.targetButton.AutoSize = true;
            this.targetButton.Location = new System.Drawing.Point(8, 152);
            this.targetButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.targetButton.Name = "targetButton";
            this.targetButton.Size = new System.Drawing.Size(105, 34);
            this.targetButton.TabIndex = 4;
            this.targetButton.Text = "Target number \r\nof HRUs";
            this.targetButton.UseVisualStyleBackColor = true;
            this.targetButton.CheckedChanged += new System.EventHandler(this.targetButton_CheckedChanged);
            // 
            // filterAreaButton
            // 
            this.filterAreaButton.AutoSize = true;
            this.filterAreaButton.Location = new System.Drawing.Point(6, 129);
            this.filterAreaButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.filterAreaButton.Name = "filterAreaButton";
            this.filterAreaButton.Size = new System.Drawing.Size(92, 19);
            this.filterAreaButton.TabIndex = 3;
            this.filterAreaButton.Text = "Filter by area";
            this.filterAreaButton.UseVisualStyleBackColor = true;
            this.filterAreaButton.CheckedChanged += new System.EventHandler(this.filterAreaButton_CheckedChanged);
            // 
            // filterLanduseButton
            // 
            this.filterLanduseButton.AutoSize = true;
            this.filterLanduseButton.Checked = true;
            this.filterLanduseButton.Location = new System.Drawing.Point(6, 94);
            this.filterLanduseButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.filterLanduseButton.Name = "filterLanduseButton";
            this.filterLanduseButton.Size = new System.Drawing.Size(114, 34);
            this.filterLanduseButton.TabIndex = 2;
            this.filterLanduseButton.TabStop = true;
            this.filterLanduseButton.Text = "Filter by landuse,\r\nsoil, slope";
            this.filterLanduseButton.UseVisualStyleBackColor = true;
            this.filterLanduseButton.CheckedChanged += new System.EventHandler(this.filterLanduseButton_CheckedChanged);
            // 
            // dominantHRUButton
            // 
            this.dominantHRUButton.AutoSize = true;
            this.dominantHRUButton.Location = new System.Drawing.Point(6, 71);
            this.dominantHRUButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dominantHRUButton.Name = "dominantHRUButton";
            this.dominantHRUButton.Size = new System.Drawing.Size(105, 19);
            this.dominantHRUButton.TabIndex = 1;
            this.dominantHRUButton.Text = "Dominant HRU";
            this.dominantHRUButton.UseVisualStyleBackColor = true;
            this.dominantHRUButton.CheckedChanged += new System.EventHandler(this.dominantHRUButton_CheckedChanged);
            // 
            // dominantLanduseButton
            // 
            this.dominantLanduseButton.AutoSize = true;
            this.dominantLanduseButton.Location = new System.Drawing.Point(5, 35);
            this.dominantLanduseButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dominantLanduseButton.Name = "dominantLanduseButton";
            this.dominantLanduseButton.Size = new System.Drawing.Size(125, 34);
            this.dominantLanduseButton.TabIndex = 0;
            this.dominantLanduseButton.Text = "Dominant landuse,\r\nsoil, slope";
            this.dominantLanduseButton.UseVisualStyleBackColor = true;
            this.dominantLanduseButton.CheckedChanged += new System.EventHandler(this.dominantLanduseButton_CheckedChanged);
            // 
            // landuseSoilSlopeGroup
            // 
            this.landuseSoilSlopeGroup.Controls.Add(this.slopeVal);
            this.landuseSoilSlopeGroup.Controls.Add(this.slopeMax);
            this.landuseSoilSlopeGroup.Controls.Add(this.slopeLabel);
            this.landuseSoilSlopeGroup.Controls.Add(this.slopeMin);
            this.landuseSoilSlopeGroup.Controls.Add(this.slopeSlider);
            this.landuseSoilSlopeGroup.Controls.Add(this.soilButton);
            this.landuseSoilSlopeGroup.Controls.Add(this.soilVal);
            this.landuseSoilSlopeGroup.Controls.Add(this.soilMax);
            this.landuseSoilSlopeGroup.Controls.Add(this.soilLabel);
            this.landuseSoilSlopeGroup.Controls.Add(this.soilMin);
            this.landuseSoilSlopeGroup.Controls.Add(this.soilSlider);
            this.landuseSoilSlopeGroup.Controls.Add(this.landuseButton);
            this.landuseSoilSlopeGroup.Controls.Add(this.landuseVal);
            this.landuseSoilSlopeGroup.Controls.Add(this.landuseMax);
            this.landuseSoilSlopeGroup.Controls.Add(this.landuseLabel);
            this.landuseSoilSlopeGroup.Controls.Add(this.landuseMin);
            this.landuseSoilSlopeGroup.Controls.Add(this.landuseSlider);
            this.landuseSoilSlopeGroup.Location = new System.Drawing.Point(279, 251);
            this.landuseSoilSlopeGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landuseSoilSlopeGroup.Name = "landuseSoilSlopeGroup";
            this.landuseSoilSlopeGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landuseSoilSlopeGroup.Size = new System.Drawing.Size(185, 239);
            this.landuseSoilSlopeGroup.TabIndex = 16;
            this.landuseSoilSlopeGroup.TabStop = false;
            this.landuseSoilSlopeGroup.Text = "Set landuse, soil, slope thresholds";
            // 
            // slopeVal
            // 
            this.slopeVal.Location = new System.Drawing.Point(148, 166);
            this.slopeVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slopeVal.Name = "slopeVal";
            this.slopeVal.Size = new System.Drawing.Size(32, 23);
            this.slopeVal.TabIndex = 16;
            this.slopeVal.TextChanged += new System.EventHandler(this.slopeVal_TextChanged);
            // 
            // slopeMax
            // 
            this.slopeMax.AutoSize = true;
            this.slopeMax.Location = new System.Drawing.Point(121, 168);
            this.slopeMax.Name = "slopeMax";
            this.slopeMax.Size = new System.Drawing.Size(25, 15);
            this.slopeMax.TabIndex = 15;
            this.slopeMax.Text = "100";
            // 
            // slopeLabel
            // 
            this.slopeLabel.AutoSize = true;
            this.slopeLabel.Location = new System.Drawing.Point(52, 168);
            this.slopeLabel.Name = "slopeLabel";
            this.slopeLabel.Size = new System.Drawing.Size(57, 15);
            this.slopeLabel.TabIndex = 14;
            this.slopeLabel.Text = "Slope (%)";
            // 
            // slopeMin
            // 
            this.slopeMin.AutoSize = true;
            this.slopeMin.Location = new System.Drawing.Point(16, 168);
            this.slopeMin.Name = "slopeMin";
            this.slopeMin.Size = new System.Drawing.Size(13, 15);
            this.slopeMin.TabIndex = 13;
            this.slopeMin.Text = "0";
            // 
            // slopeSlider
            // 
            this.slopeSlider.Location = new System.Drawing.Point(5, 184);
            this.slopeSlider.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slopeSlider.Name = "slopeSlider";
            this.slopeSlider.Size = new System.Drawing.Size(137, 45);
            this.slopeSlider.TabIndex = 12;
            this.slopeSlider.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.slopeSlider.ValueChanged += new System.EventHandler(this.slopeSlider_ValueChanged);
            // 
            // soilButton
            // 
            this.soilButton.Location = new System.Drawing.Point(148, 129);
            this.soilButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.soilButton.Name = "soilButton";
            this.soilButton.Size = new System.Drawing.Size(32, 23);
            this.soilButton.TabIndex = 11;
            this.soilButton.Text = "Go";
            this.soilButton.UseVisualStyleBackColor = true;
            this.soilButton.Click += new System.EventHandler(this.soilButton_Click);
            // 
            // soilVal
            // 
            this.soilVal.Location = new System.Drawing.Point(148, 102);
            this.soilVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.soilVal.Name = "soilVal";
            this.soilVal.Size = new System.Drawing.Size(32, 23);
            this.soilVal.TabIndex = 10;
            this.soilVal.TextChanged += new System.EventHandler(this.soilVal_TextChanged);
            // 
            // soilMax
            // 
            this.soilMax.AutoSize = true;
            this.soilMax.Location = new System.Drawing.Point(121, 104);
            this.soilMax.Name = "soilMax";
            this.soilMax.Size = new System.Drawing.Size(25, 15);
            this.soilMax.TabIndex = 9;
            this.soilMax.Text = "100";
            // 
            // soilLabel
            // 
            this.soilLabel.AutoSize = true;
            this.soilLabel.Location = new System.Drawing.Point(52, 104);
            this.soilLabel.Name = "soilLabel";
            this.soilLabel.Size = new System.Drawing.Size(47, 15);
            this.soilLabel.TabIndex = 8;
            this.soilLabel.Text = "Soil (%)";
            // 
            // soilMin
            // 
            this.soilMin.AutoSize = true;
            this.soilMin.Location = new System.Drawing.Point(16, 104);
            this.soilMin.Name = "soilMin";
            this.soilMin.Size = new System.Drawing.Size(13, 15);
            this.soilMin.TabIndex = 7;
            this.soilMin.Text = "0";
            // 
            // soilSlider
            // 
            this.soilSlider.Location = new System.Drawing.Point(6, 121);
            this.soilSlider.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.soilSlider.Name = "soilSlider";
            this.soilSlider.Size = new System.Drawing.Size(137, 45);
            this.soilSlider.TabIndex = 6;
            this.soilSlider.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.soilSlider.ValueChanged += new System.EventHandler(this.soilSlider_ValueChanged);
            // 
            // landuseButton
            // 
            this.landuseButton.Location = new System.Drawing.Point(148, 65);
            this.landuseButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landuseButton.Name = "landuseButton";
            this.landuseButton.Size = new System.Drawing.Size(32, 23);
            this.landuseButton.TabIndex = 5;
            this.landuseButton.Text = "Go";
            this.landuseButton.UseVisualStyleBackColor = true;
            this.landuseButton.Click += new System.EventHandler(this.landuseButton_Click);
            // 
            // landuseVal
            // 
            this.landuseVal.Location = new System.Drawing.Point(148, 38);
            this.landuseVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landuseVal.Name = "landuseVal";
            this.landuseVal.Size = new System.Drawing.Size(32, 23);
            this.landuseVal.TabIndex = 4;
            this.landuseVal.TextChanged += new System.EventHandler(this.landuseVal_TextChanged);
            // 
            // landuseMax
            // 
            this.landuseMax.AutoSize = true;
            this.landuseMax.Location = new System.Drawing.Point(121, 40);
            this.landuseMax.Name = "landuseMax";
            this.landuseMax.Size = new System.Drawing.Size(25, 15);
            this.landuseMax.TabIndex = 3;
            this.landuseMax.Text = "100";
            // 
            // landuseLabel
            // 
            this.landuseLabel.AutoSize = true;
            this.landuseLabel.Location = new System.Drawing.Point(43, 40);
            this.landuseLabel.Name = "landuseLabel";
            this.landuseLabel.Size = new System.Drawing.Size(72, 15);
            this.landuseLabel.TabIndex = 2;
            this.landuseLabel.Text = "Landuse (%)";
            // 
            // landuseMin
            // 
            this.landuseMin.AutoSize = true;
            this.landuseMin.Location = new System.Drawing.Point(16, 40);
            this.landuseMin.Name = "landuseMin";
            this.landuseMin.Size = new System.Drawing.Size(13, 15);
            this.landuseMin.TabIndex = 1;
            this.landuseMin.Text = "0";
            // 
            // areaPercentChoiceGroup
            // 
            this.areaPercentChoiceGroup.Controls.Add(this.areaButton);
            this.areaPercentChoiceGroup.Controls.Add(this.percentButton);
            this.areaPercentChoiceGroup.Location = new System.Drawing.Point(140, 455);
            this.areaPercentChoiceGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaPercentChoiceGroup.Name = "areaPercentChoiceGroup";
            this.areaPercentChoiceGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaPercentChoiceGroup.Size = new System.Drawing.Size(135, 71);
            this.areaPercentChoiceGroup.TabIndex = 17;
            this.areaPercentChoiceGroup.TabStop = false;
            this.areaPercentChoiceGroup.Text = "Threshold method";
            // 
            // areaButton
            // 
            this.areaButton.AutoSize = true;
            this.areaButton.Location = new System.Drawing.Point(11, 48);
            this.areaButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaButton.Name = "areaButton";
            this.areaButton.Size = new System.Drawing.Size(73, 19);
            this.areaButton.TabIndex = 1;
            this.areaButton.Text = "Area (ha)";
            this.areaButton.UseVisualStyleBackColor = true;
            this.areaButton.CheckedChanged += new System.EventHandler(this.areaButton_CheckedChanged);
            // 
            // percentButton
            // 
            this.percentButton.AutoSize = true;
            this.percentButton.Checked = true;
            this.percentButton.Location = new System.Drawing.Point(11, 15);
            this.percentButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.percentButton.Name = "percentButton";
            this.percentButton.Size = new System.Drawing.Size(82, 34);
            this.percentButton.TabIndex = 0;
            this.percentButton.TabStop = true;
            this.percentButton.Text = "Percent of \r\nsubbasin";
            this.percentButton.UseVisualStyleBackColor = true;
            this.percentButton.CheckedChanged += new System.EventHandler(this.percentButton_CheckedChanged);
            // 
            // optionGroup
            // 
            this.optionGroup.Controls.Add(this.elevBandsButton);
            this.optionGroup.Controls.Add(this.exemptButton);
            this.optionGroup.Controls.Add(this.splitButton);
            this.optionGroup.Location = new System.Drawing.Point(10, 395);
            this.optionGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.optionGroup.Name = "optionGroup";
            this.optionGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.optionGroup.Size = new System.Drawing.Size(124, 131);
            this.optionGroup.TabIndex = 18;
            this.optionGroup.TabStop = false;
            this.optionGroup.Text = "Optional";
            // 
            // elevBandsButton
            // 
            this.elevBandsButton.Location = new System.Drawing.Point(5, 90);
            this.elevBandsButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.elevBandsButton.Name = "elevBandsButton";
            this.elevBandsButton.Size = new System.Drawing.Size(113, 27);
            this.elevBandsButton.TabIndex = 2;
            this.elevBandsButton.Text = "Elevation bands";
            this.elevBandsButton.UseVisualStyleBackColor = true;
            this.elevBandsButton.Click += new System.EventHandler(this.elevBandsButton_Click);
            // 
            // exemptButton
            // 
            this.exemptButton.Location = new System.Drawing.Point(5, 57);
            this.exemptButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.exemptButton.Name = "exemptButton";
            this.exemptButton.Size = new System.Drawing.Size(113, 25);
            this.exemptButton.TabIndex = 1;
            this.exemptButton.Text = "Exempt landuses";
            this.exemptButton.UseVisualStyleBackColor = true;
            this.exemptButton.Click += new System.EventHandler(this.exemptButton_Click);
            // 
            // splitButton
            // 
            this.splitButton.Location = new System.Drawing.Point(6, 24);
            this.splitButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitButton.Name = "splitButton";
            this.splitButton.Size = new System.Drawing.Size(112, 25);
            this.splitButton.TabIndex = 0;
            this.splitButton.Text = "Split landuses";
            this.splitButton.UseVisualStyleBackColor = true;
            this.splitButton.Click += new System.EventHandler(this.splitButton_Click);
            // 
            // progressLabel
            // 
            this.progressLabel.AutoSize = true;
            this.progressLabel.Location = new System.Drawing.Point(15, 534);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(72, 15);
            this.progressLabel.TabIndex = 19;
            this.progressLabel.Text = "dummy text";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(181, 530);
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(94, 23);
            this.progressBar.TabIndex = 20;
            // 
            // createButton
            // 
            this.createButton.Location = new System.Drawing.Point(285, 530);
            this.createButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(97, 24);
            this.createButton.TabIndex = 21;
            this.createButton.Text = "Create HRUs";
            this.createButton.UseVisualStyleBackColor = true;
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(388, 530);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(78, 24);
            this.cancelButton.TabIndex = 22;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // areaGroup
            // 
            this.areaGroup.Controls.Add(this.areaVal);
            this.areaGroup.Controls.Add(this.areaMax);
            this.areaGroup.Controls.Add(this.areaLabel);
            this.areaGroup.Controls.Add(this.areaMin);
            this.areaGroup.Controls.Add(this.areaSlider);
            this.areaGroup.Location = new System.Drawing.Point(279, 341);
            this.areaGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaGroup.Name = "areaGroup";
            this.areaGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaGroup.Size = new System.Drawing.Size(185, 75);
            this.areaGroup.TabIndex = 23;
            this.areaGroup.TabStop = false;
            this.areaGroup.Text = "Set area threshold";
            // 
            // areaVal
            // 
            this.areaVal.Location = new System.Drawing.Point(147, 45);
            this.areaVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaVal.Name = "areaVal";
            this.areaVal.Size = new System.Drawing.Size(32, 23);
            this.areaVal.TabIndex = 10;
            this.areaVal.TextChanged += new System.EventHandler(this.areaVal_TextChanged);
            // 
            // areaMax
            // 
            this.areaMax.AutoSize = true;
            this.areaMax.Location = new System.Drawing.Point(121, 23);
            this.areaMax.Name = "areaMax";
            this.areaMax.Size = new System.Drawing.Size(31, 15);
            this.areaMax.TabIndex = 9;
            this.areaMax.Text = "9999";
            // 
            // areaLabel
            // 
            this.areaLabel.AutoSize = true;
            this.areaLabel.Location = new System.Drawing.Point(51, 23);
            this.areaLabel.Name = "areaLabel";
            this.areaLabel.Size = new System.Drawing.Size(55, 15);
            this.areaLabel.TabIndex = 8;
            this.areaLabel.Text = "Area (ha)";
            // 
            // areaMin
            // 
            this.areaMin.AutoSize = true;
            this.areaMin.Location = new System.Drawing.Point(5, 23);
            this.areaMin.Name = "areaMin";
            this.areaMin.Size = new System.Drawing.Size(13, 15);
            this.areaMin.TabIndex = 7;
            this.areaMin.Text = "0";
            // 
            // areaSlider
            // 
            this.areaSlider.Location = new System.Drawing.Point(5, 37);
            this.areaSlider.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.areaSlider.Name = "areaSlider";
            this.areaSlider.Size = new System.Drawing.Size(137, 45);
            this.areaSlider.TabIndex = 6;
            this.areaSlider.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.areaSlider.ValueChanged += new System.EventHandler(this.areaSlider_ValueChanged);
            // 
            // targetGroup
            // 
            this.targetGroup.Controls.Add(this.targetVal);
            this.targetGroup.Controls.Add(this.targetMax);
            this.targetGroup.Controls.Add(this.targetLabel);
            this.targetGroup.Controls.Add(this.targetMin);
            this.targetGroup.Controls.Add(this.targetSlider);
            this.targetGroup.Location = new System.Drawing.Point(279, 341);
            this.targetGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.targetGroup.Name = "targetGroup";
            this.targetGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.targetGroup.Size = new System.Drawing.Size(185, 75);
            this.targetGroup.TabIndex = 24;
            this.targetGroup.TabStop = false;
            this.targetGroup.Text = "Set target";
            // 
            // targetVal
            // 
            this.targetVal.Location = new System.Drawing.Point(148, 45);
            this.targetVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.targetVal.Name = "targetVal";
            this.targetVal.Size = new System.Drawing.Size(32, 23);
            this.targetVal.TabIndex = 10;
            this.targetVal.TextChanged += new System.EventHandler(this.targetVal_TextChanged);
            // 
            // targetMax
            // 
            this.targetMax.AutoSize = true;
            this.targetMax.Location = new System.Drawing.Point(121, 23);
            this.targetMax.Name = "targetMax";
            this.targetMax.Size = new System.Drawing.Size(31, 15);
            this.targetMax.TabIndex = 9;
            this.targetMax.Text = "9999";
            // 
            // targetLabel
            // 
            this.targetLabel.AutoSize = true;
            this.targetLabel.Location = new System.Drawing.Point(28, 23);
            this.targetLabel.Name = "targetLabel";
            this.targetLabel.Size = new System.Drawing.Size(97, 15);
            this.targetLabel.TabIndex = 8;
            this.targetLabel.Text = "Number of HRUs";
            // 
            // targetMin
            // 
            this.targetMin.AutoSize = true;
            this.targetMin.Location = new System.Drawing.Point(5, 23);
            this.targetMin.Name = "targetMin";
            this.targetMin.Size = new System.Drawing.Size(13, 15);
            this.targetMin.TabIndex = 7;
            this.targetMin.Text = "0";
            // 
            // targetSlider
            // 
            this.targetSlider.Location = new System.Drawing.Point(5, 37);
            this.targetSlider.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.targetSlider.Name = "targetSlider";
            this.targetSlider.Size = new System.Drawing.Size(137, 45);
            this.targetSlider.TabIndex = 6;
            this.targetSlider.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.targetSlider.ValueChanged += new System.EventHandler(this.targetSlider_ValueChanged);
            // 
            // HRUsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(475, 564);
            this.Controls.Add(this.targetGroup);
            this.Controls.Add(this.areaGroup);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.createButton);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.optionGroup);
            this.Controls.Add(this.areaPercentChoiceGroup);
            this.Controls.Add(this.landuseSoilSlopeGroup);
            this.Controls.Add(this.hruChoiceGroup);
            this.Controls.Add(this.slopeGroup);
            this.Controls.Add(this.fullHRUsLabel);
            this.Controls.Add(this.readButton);
            this.Controls.Add(this.readChoiceGroup);
            this.Controls.Add(this.generateFullHRUs);
            this.Controls.Add(this.soilGroup);
            this.Controls.Add(this.soilTableLabel);
            this.Controls.Add(this.selectSoilTable);
            this.Controls.Add(this.selectSoilButton);
            this.Controls.Add(this.selectSoil);
            this.Controls.Add(this.landuseTableLabel);
            this.Controls.Add(this.selectLanduseTable);
            this.Controls.Add(this.selectLanduseButton);
            this.Controls.Add(this.selectLanduselabel);
            this.Controls.Add(this.selectLanduse);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "HRUsForm";
            this.Text = "Create HRUs";
            this.Load += new System.EventHandler(this.HRUsForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.landuseSlider)).EndInit();
            this.soilGroup.ResumeLayout(false);
            this.soilGroup.PerformLayout();
            this.readChoiceGroup.ResumeLayout(false);
            this.readChoiceGroup.PerformLayout();
            this.slopeGroup.ResumeLayout(false);
            this.slopeGroup.PerformLayout();
            this.hruChoiceGroup.ResumeLayout(false);
            this.hruChoiceGroup.PerformLayout();
            this.landuseSoilSlopeGroup.ResumeLayout(false);
            this.landuseSoilSlopeGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.slopeSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.soilSlider)).EndInit();
            this.areaPercentChoiceGroup.ResumeLayout(false);
            this.areaPercentChoiceGroup.PerformLayout();
            this.optionGroup.ResumeLayout(false);
            this.areaGroup.ResumeLayout(false);
            this.areaGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.areaSlider)).EndInit();
            this.targetGroup.ResumeLayout(false);
            this.targetGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.targetSlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private TextBox selectLanduse;
        private Label selectLanduselabel;
        private Button selectLanduseButton;
        private ComboBox selectLanduseTable;
        private Label landuseTableLabel;
        private TextBox selectSoil;
        private Button selectSoilButton;
        private ComboBox selectSoilTable;
        private Label soilTableLabel;
        private GroupBox soilGroup;
        private RadioButton usersoilButton;
        private RadioButton STATSGOButton;
        private RadioButton SSURGOButton;
        private CheckBox generateFullHRUs;
        private GroupBox readChoiceGroup;
        private RadioButton readFromPrevious;
        private RadioButton readFromMaps;
        private Button readButton;
        private Label fullHRUsLabel;
        private GroupBox slopeGroup;
        private Label slopeBandsLabel;
        private Button clearButton;
        private Button insertButton;
        private TextBox slopeBrowser;
        private TextBox slopeBand;
        private GroupBox hruChoiceGroup;
        private RadioButton targetButton;
        private RadioButton filterAreaButton;
        private RadioButton filterLanduseButton;
        private RadioButton dominantHRUButton;
        private GroupBox landuseSoilSlopeGroup;
        private TextBox slopeVal;
        private Label slopeMax;
        private Label slopeLabel;
        private Label slopeMin;
        private TrackBar slopeSlider;
        private Button soilButton;
        private TextBox soilVal;
        private Label soilMax;
        private Label soilLabel;
        private Label soilMin;
        private TrackBar soilSlider;
        private Button landuseButton;
        private TextBox landuseVal;
        private Label landuseMax;
        private Label landuseLabel;
        private Label landuseMin;
        private TrackBar landuseSlider;
        private GroupBox areaPercentChoiceGroup;
        private RadioButton areaButton;
        private RadioButton percentButton;
        private GroupBox optionGroup;
        private Button elevBandsButton;
        private Button exemptButton;
        private Button splitButton;
        private Label progressLabel;
        private ProgressBar progressBar;
        private Button createButton;
        private Button cancelButton;
        private GroupBox areaGroup;
        private TextBox areaVal;
        private Label areaMax;
        private Label areaLabel;
        private Label areaMin;
        private TrackBar areaSlider;
        private GroupBox targetGroup;
        private TextBox targetVal;
        private Label targetMax;
        private Label targetLabel;
        private Label targetMin;
        private TrackBar targetSlider;
        private RadioButton dominantLanduseButton;

        #endregion
    }
}