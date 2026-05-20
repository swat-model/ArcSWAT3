using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;

using ArcGIS.Desktop.Mapping;

namespace ArcSWAT3
{
    public partial class HRUsForm : Form {
        private HRUs parent;
        public HRUsForm(HRUs parent) {

            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            this.parent = parent;
        }

        private async void selectLanduseButton_Click(object sender, EventArgs e) {
            await Utils.removeLayerByLegend(FileTypes.legend(FileTypes._LANDUSES));
            string landuseFile;
            Layer landuseLayer;
            (landuseFile, landuseLayer) = await Utils.openAndLoadFile(FileTypes._LANDUSES, this.selectLanduse, parent._gv.landuseDir, parent._gv, null, Utils._LANDUSE_GROUP_NAME, clipToDEM: true);
            if (!string.IsNullOrEmpty(landuseFile) && landuseLayer is not null) {
                parent.landuseFile = landuseFile;
                parent.landuseLayer = landuseLayer as RasterLayer;
                // hide landuseLayer
                await Utils.setLayerVisibility(landuseLayer, false);
            }
        }

        private async void selectSoilButton_Click(object sender, EventArgs e) {
            await Utils.removeLayerByLegend(FileTypes.legend(FileTypes._SOILS));
            string soilFile;
            Layer soilLayer;
            (soilFile, soilLayer) = await Utils.openAndLoadFile(FileTypes._SOILS, this.selectSoil, parent._gv.soilDir, parent._gv, null, Utils._SOIL_GROUP_NAME, clipToDEM: true);
            if (!string.IsNullOrEmpty(soilFile) && soilLayer is not null) {
                parent.soilFile = soilFile;
                parent.soilLayer = soilLayer as RasterLayer;
                // hide soilLayer
                await Utils.setLayerVisibility(soilLayer, false);
            }
        }

        private void usersoilButton_CheckedChanged(object sender, EventArgs e) {
            setSoilData();
        }

        private void STATSGOButton_CheckedChanged(object sender, EventArgs e) {
            setSoilData();
        }

        private void SSURGOButton_CheckedChanged(object sender, EventArgs e) {
            setSoilData();
        }

        // Read usersoil/STATSGO/SSURGO choice and set variables.
        public virtual void setSoilData() {
            if (this.usersoilButton.Checked) {
                parent._gv.db.useSTATSGO = false;
                parent._gv.db.useSSURGO = false;
                this.soilTableLabel.Enabled = true;
                this.selectSoilTable.Enabled = true;
            } else if (this.STATSGOButton.Checked) {
                parent._gv.db.useSTATSGO = true;
                parent._gv.db.useSSURGO = false;
                this.soilTableLabel.Enabled = true;
                this.selectSoilTable.Enabled = true;
            } else if (this.SSURGOButton.Checked) {
                parent._gv.db.useSTATSGO = false;
                parent._gv.db.useSSURGO = true;
                this.soilTableLabel.Enabled = false;
                this.selectSoilTable.Enabled = false;
            }
        }

        private void readFromMaps_CheckedChanged(object sender, EventArgs e) {
            setReadChoice();
        }

        private void readFromPrevious_CheckedChanged(object sender, EventArgs e) {
            setReadChoice();
        }

        // Read read choice and set variables.
        public virtual void setReadChoice() {
            if (this.readFromMaps.Checked) {
                this.slopeGroup.Enabled = true;
                this.generateFullHRUs.Enabled = true;
                this.elevBandsButton.Enabled = true;
            } else {
                this.slopeGroup.Enabled = false;
                this.generateFullHRUs.Enabled = false;
                this.elevBandsButton.Enabled = false;
            }
            this.splitButton.Enabled = false;
            this.exemptButton.Enabled = false;
            this.hruChoiceGroup.Enabled = false;
            this.areaPercentChoiceGroup.Enabled = false;
            this.landuseSoilSlopeGroup.Enabled = false;
            this.areaGroup.Enabled = false;
            this.targetGroup.Enabled = false;
            this.createButton.Enabled = false;
            this.fullHRUsLabel.Text = "";
        }

        // Set dialog to read from maps or from previous run.
        public virtual void setRead() {
            bool usePrevious;
            if (parent._gv.isHUC) {
                // for safety always rerun reading files for HUC projects
                usePrevious = false;
            } else {
                usePrevious = parent._gv.isHAWQS ? parent._db.hasData("BASINSDATAHUC1") : parent._db.hasData("BASINSDATA1");
            }
            if (usePrevious) {
                this.readFromPrevious.Enabled = true;
                this.readFromPrevious.Checked = true;
            } else {
                this.readFromMaps.Checked = true;
                this.readFromPrevious.Enabled = false;
            }
            this.setReadChoice();
        }

        private void dominantLanduseButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        private void dominantHRUButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        private void filterLanduseButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        private void filterAreaButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        private void targetButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        private void percentButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        private void areaButton_CheckedChanged(object sender, EventArgs e) {
            setHRUChoice();
        }

        // Set dialog according to choice of multiple/single HRUs.
        public virtual void setHRUChoice() {
            if (!hruChoiceGroup.Enabled) { return; }
            if (this.dominantHRUButton.Checked || this.dominantLanduseButton.Checked) {
                parent.CreateHRUs.isMultiple = false;
                parent.CreateHRUs.isDominantHRU = this.dominantHRUButton.Checked;
                this.landuseSoilSlopeGroup.Visible = false;
                this.areaGroup.Visible = false;
                this.targetGroup.Visible = false;
                this.areaPercentChoiceGroup.Enabled = false;
                this.landuseSoilSlopeGroup.Enabled = false;
                this.areaGroup.Enabled = false;
                this.targetGroup.Enabled = false;
                this.createButton.Enabled = true;
            } else {
                this.areaPercentChoiceGroup.Enabled = true;
                parent.CreateHRUs.isMultiple = true;
                if (this.filterLanduseButton.Checked) {
                    this.landuseSoilSlopeGroup.Visible = true;
                    this.areaGroup.Visible = false;
                    this.targetGroup.Visible = false;
                    this.landuseSoilSlopeGroup.Enabled = true;
                    this.landuseSlider.Enabled = true;
                    this.landuseVal.Enabled = true;
                    this.landuseButton.Enabled = true;
                    this.soilSlider.Enabled = false;
                    this.soilVal.Enabled = false;
                    this.soilButton.Enabled = false;
                    this.slopeSlider.Enabled = false;
                    this.slopeVal.Enabled = false;
                    this.areaGroup.Enabled = false;
                    this.targetGroup.Enabled = false;
                    this.createButton.Enabled = false;
                    parent.CreateHRUs.isArea = false;
                    parent.CreateHRUs.isTarget = false;
                } else if (this.filterAreaButton.Checked) {
                    this.landuseSoilSlopeGroup.Visible = false;
                    this.areaGroup.Visible = true;
                    this.targetGroup.Visible = false;
                    this.landuseSoilSlopeGroup.Enabled = false;
                    this.areaGroup.Enabled = true;
                    this.targetGroup.Enabled = false;
                    this.createButton.Enabled = true;
                    parent.CreateHRUs.isArea = true;
                    parent.CreateHRUs.isTarget = false;
                } else {
                    this.landuseSoilSlopeGroup.Enabled = false;
                    this.areaGroup.Enabled = false;
                    this.landuseSoilSlopeGroup.Visible = false;
                    this.areaGroup.Visible = false;
                    this.targetGroup.Visible = true;
                    this.targetGroup.Enabled = true;
                    this.createButton.Enabled = true;
                    parent.CreateHRUs.isArea = false;
                    parent.CreateHRUs.isTarget = true;
                }
                this.setAreaPercentChoice();
            }
        }

        // Set dialog according to choice of area or percent thresholds.
        public virtual void setAreaPercentChoice() {
            if (!parent.CreateHRUs.isMultiple) {
                return;
            }
            parent.CreateHRUs.useArea = this.areaButton.Checked;
            if (parent.CreateHRUs.useArea) {
                this.landuseLabel.Text = "Landuse (ha)";
                this.soilLabel.Text = "Soil (ha)";
                this.slopeLabel.Text = "Slope (ha)";
                this.areaLabel.Text = "Area (ha)";
            } else {
                this.landuseLabel.Text = "Landuse (%)";
                this.soilLabel.Text = "Soil (%)";
                this.slopeLabel.Text = "Slope (%)";
                this.areaLabel.Text = "Area (%)";
            }
            if (parent.CreateHRUs.isArea) {
                var displayMaxArea = parent.CreateHRUs.useArea ? Convert.ToInt32(parent.CreateHRUs.maxBasinArea()) : 100;
                this.areaMax.Text = displayMaxArea.ToString();
                this.areaSlider.Maximum = displayMaxArea;
                var _tmp_1 = parent.CreateHRUs.areaVal;
                if (0 < _tmp_1 && _tmp_1 <= displayMaxArea) {
                    this.areaSlider.Value = Convert.ToInt32(parent.CreateHRUs.areaVal);
                }
            } else if (parent.CreateHRUs.isTarget) {
                // Setting the minimum for the slider changes the slider value
                // which in turn changes CreateHRUs.targetVal.
                // So we remember the value of CreateHRUs.targetVal and restore it later.
                var target = parent.CreateHRUs.targetVal;
                var numBasins = parent._gv.topo.SWATBasinToBasin.Count;
                this.targetSlider.Minimum = numBasins;
                this.targetMin.Text = numBasins.ToString();
                var numHRUs = parent.CreateHRUs.countFullHRUs();
                this.targetSlider.Maximum = numHRUs;
                this.targetMax.Text = numHRUs.ToString();
                // restore the target and use it to set the slider
                parent.CreateHRUs.targetVal = target;
                var _tmp_2 = parent.CreateHRUs.targetVal;
                if (numBasins <= _tmp_2 && _tmp_2 <= numHRUs) {
                    this.targetSlider.Value = Convert.ToInt32(parent.CreateHRUs.targetVal);
                }
            } else {
                var minCropVal = Convert.ToInt32(parent.CreateHRUs.minMaxCropVal(parent.CreateHRUs.useArea));
                this.landuseMax.Text = minCropVal.ToString();
                this.landuseSlider.Maximum = minCropVal;
                var _tmp_3 = parent.CreateHRUs.landuseVal;
                if (0 <= _tmp_3 && _tmp_3 <= minCropVal) {
                    this.landuseSlider.Value = Convert.ToInt32(parent.CreateHRUs.landuseVal);
                }
            }
        }

        private void insertButton_Click(object sender, EventArgs e) {
            insertSlope();
        }

        private void slopeBrowser_Enter(object sender, EventArgs e) {
            insertSlope();
        }

        // Insert a new slope limit.
        public virtual void insertSlope() {
            var txt = slopeBand.Text;
            if (txt == "") {
                return;
            }
            double num;
            if (!Double.TryParse(txt, out num)) {
                Utils.information(String.Format("Cannot parse {0} as a number", txt), parent._gv.isBatch);
                return;
            }
            ListFuns.insertIntoSortedList(num, parent._gv.db.slopeLimits, true);
            this.slopeBrowser.Text = Utils.slopesToString(parent._gv.db.slopeLimits);
            this.slopeBand.Clear();
        }

        // Reset to no slope bands.
        private void clearButton_Click(object sender, EventArgs e) {
            parent._gv.db.slopeLimits = new List<double>();
            this.slopeBrowser.Text = "[0, 9999]";
            this.slopeBand.Clear();
        }

        private async void readButton_Click(object sender, EventArgs e) {
            await parent.readFiles();
        }

        private async void createButton_Click(object sender, EventArgs e) {
            await parent.calcHRUs();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            parent.cancelHRUs();
        }

        private void areaVal_TextChanged(object sender, EventArgs e) {
            var @string = this.areaVal.Text;
            if (@string == "") {
                return;
            }
            try {
                var val = Convert.ToInt32(@string);
                // allow values outside slider range
                if (this.areaSlider.Minimum <= val && val <= this.areaSlider.Maximum) {
                    this.areaSlider.Value = val;
                }
                parent.CreateHRUs.areaVal = val;
                //this.areaVal.moveCursor(QTextCursor.End);
            }
            catch (Exception) {
                return;
            }
            this.createButton.Enabled = true;
        }

        private void areaSlider_ValueChanged(object sender, EventArgs e) {
            var val = this.areaSlider.Value;
            this.areaVal.Text = val.ToString();
            this.createButton.Enabled = true;
            parent.CreateHRUs.areaVal = val;
        }

        private void landuseVal_TextChanged(object sender, EventArgs e) {
            var @string = this.landuseVal.Text;
            if (@string == "") {
                return;
            }
            try {
                var val = Convert.ToInt32(@string);
                // allow values outside slider range
                if (this.landuseSlider.Minimum <= val && val <= this.landuseSlider.Maximum) {
                    this.landuseSlider.Value = val;
                }
                parent.CreateHRUs.landuseVal = val;
                //this.landuseVal.moveCursor(QTextCursor.End);
            }
            catch (Exception) {
                return;
            }
        }

        private void landuseSlider_ValueChanged(object sender, EventArgs e) {
            var val = this.landuseSlider.Value;
            this.landuseVal.Text = val.ToString();
            parent.CreateHRUs.landuseVal = val;
        }

        private void soilVal_TextChanged(object sender, EventArgs e) {
            var @string = this.soilVal.Text;
            if (@string == "") {
                return;
            }
            try {
                var val = Convert.ToInt32(@string);
                // allow values outside slider range
                if (this.soilSlider.Minimum <= val && val <= this.soilSlider.Maximum) {
                    this.soilSlider.Value = val;
                }
                parent.CreateHRUs.soilVal = val;
                //this.soilVal.moveCursor(QTextCursor.End);
            }
            catch (Exception) {
                return;
            }
        }

        private void soilSlider_ValueChanged(object sender, EventArgs e) {
            var val = this.soilSlider.Value;
            this.soilVal.Text = val.ToString();
            parent.CreateHRUs.soilVal = val;
        }

        private void targetVal_TextChanged(object sender, EventArgs e) {
            var @string = this.targetVal.Text;
            if (@string == "") {
                return;
            }
            try {
                var val = Convert.ToInt32(@string);
                this.targetSlider.Value = val;
                parent.CreateHRUs.targetVal = val;
                //this.targetVal.moveCursor(QTextCursor.End);
            }
            catch (Exception) {
                return;
            }
        }

        private void targetSlider_ValueChanged(object sender, EventArgs e) {
            var val = this.targetSlider.Value;
            this.targetVal.Text = val.ToString();
            parent.CreateHRUs.targetVal = val;
        }

        private void slopeVal_TextChanged(object sender, EventArgs e) {
            var @string = this.slopeVal.Text;
            if (@string == "") {
                return;
            }
            try {
                var val = Convert.ToInt32(@string);
                // allow values outside slider range
                if (this.slopeSlider.Minimum <= val && val <= this.slopeSlider.Maximum) {
                    this.slopeSlider.Value = val;
                }
                parent.CreateHRUs.slopeVal = val;
                //this.slopeVal.moveCursor(QTextCursor.End);
            }
            catch (Exception) {
                return;
            }
        }

        private void slopeSlider_ValueChanged(object sender, EventArgs e) {
            var val = this.slopeSlider.Value;
            this.slopeVal.Text = val.ToString();
            parent.CreateHRUs.slopeVal = val;
        }

        private void landuseButton_Click(object sender, EventArgs e) {
            int minSoilVal;
            if (parent.CreateHRUs.useArea) {
                minSoilVal = Convert.ToInt32(parent.CreateHRUs.minMaxSoilArea());
            } else {
                minSoilVal = Convert.ToInt32(parent.CreateHRUs.minMaxSoilPercent(parent.CreateHRUs.landuseVal));
            }
            this.landuseSlider.Enabled = false;
            this.landuseVal.Enabled = false;
            this.landuseButton.Enabled = false;
            this.soilSlider.Enabled = true;
            this.soilVal.Enabled = true;
            this.soilButton.Enabled = true;
            this.soilSlider.Maximum = minSoilVal;
            this.soilMax.Text = minSoilVal.ToString();
            var _tmp_1 = parent.CreateHRUs.soilVal;
            if (0 <= _tmp_1 && _tmp_1 <= minSoilVal) {
                this.soilSlider.Value = Convert.ToInt32(parent.CreateHRUs.soilVal);
            }
        }

        private void soilButton_Click(object sender, EventArgs e) {
            int minSlopeVal;
            this.soilSlider.Enabled = false;
            this.soilVal.Enabled = false;
            this.soilButton.Enabled = false;
            if (parent._gv.db.slopeLimits.Count > 0) {
                if (parent.CreateHRUs.useArea) {
                    minSlopeVal = Convert.ToInt32(parent.CreateHRUs.minMaxSlopeArea());
                } else {
                    minSlopeVal = Convert.ToInt32(parent.CreateHRUs.minMaxSlopePercent(parent.CreateHRUs.landuseVal, parent.CreateHRUs.soilVal));
                }
                this.slopeSlider.Enabled = true;
                this.slopeVal.Enabled = true;
                this.slopeSlider.Maximum = minSlopeVal;
                this.slopeMax.Text = minSlopeVal.ToString();
                var _tmp_1 = parent.CreateHRUs.slopeVal;
                if (0 <= _tmp_1 && _tmp_1 <= minSlopeVal) {
                    this.slopeSlider.Value = Convert.ToInt32(parent.CreateHRUs.slopeVal);
                }
            }
            this.createButton.Enabled = true;
        }

        private void exemptButton_Click(object sender, EventArgs e) {
            var dlg = new ExemptForm(parent._gv);
            dlg.run();
        }

        private void splitButton_Click(object sender, EventArgs e) {
            var dlg = new SplitForm(parent._gv);
            dlg.run();
        }

        private void elevBandsButton_Click(object sender, EventArgs e) {
            var dlg = new ElevationBandsForm(parent._gv);
            dlg.run();
        }

        private async void HRUsForm_Load(object sender, EventArgs e) {
            parent._db.populateTableNames();
            foreach (string name in parent._db.landuseTableNames) {
                this.selectLanduseTable.Items.Add(name);
            }
            this.selectLanduseTable.Items.Add(Parameters._USECSV);
            foreach (string name in parent._db.soilTableNames) {
                this.selectSoilTable.Items.Add(name);
            }
            this.selectSoilTable.Items.Add(Parameters._USECSV);
            await this.readProj();
            this.setSoilData();
            parent._gv.getExemptSplit();
            this.fullHRUsLabel.Text = "";
            this.optionGroup.Enabled = true;
            this.landuseSoilSlopeGroup.Enabled = false;
            this.areaGroup.Enabled = false;
            this.targetGroup.Enabled = false;
            this.landuseSoilSlopeGroup.Visible = false;
            this.areaGroup.Visible = false;
            this.targetGroup.Visible = false;
            this.createButton.Enabled = false;
            this.progressBar.Visible = false;
            this.setRead();
            this.slopeBrowser.Text = Utils.slopesToString(parent._gv.db.slopeLimits);
        }

        // Read HRU settings from the project file.
        public async Task readProj() {
            string slopeBandsFile;
            string soilTable;
            int index;
            string landuseTable;
            string soilFile;
            string possFile;
            Layer layer;
            string landuseFile;
            Proj proj = parent._gv.proj;
            var title = parent._gv.projName;
            bool found;
            (landuseFile, found) = proj.readEntry(title, "landuse/file", "");
            RasterLayer landuseLayer = null;
            if (found && !string.IsNullOrEmpty(landuseFile) && File.Exists(landuseFile)) {
                landuseLayer = (await Utils.getLayerByFilename(landuseFile, FileTypes._LANDUSES, parent._gv, null, Utils._LANDUSE_GROUP_NAME)).Item1 as RasterLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._LANDUSES));
                if (layer != null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._LANDUSES)), parent._gv.isBatch, true) == MessageBoxResult.Yes) {
                        landuseLayer = layer as RasterLayer;
                        landuseFile = possFile;
                    }
                }
            }
            if (landuseLayer is not null) {
                this.selectLanduse.Text = landuseFile;
                parent.landuseFile = landuseFile;
                parent.landuseLayer = landuseLayer;
                // hide landuseLayer
                await Utils.setLayerVisibility(landuseLayer, false);
            }
            (soilFile, found) = proj.readEntry(title, "soil/file", "");
            RasterLayer soilLayer = null;
            if (found && !string.IsNullOrEmpty(soilFile) && File.Exists(soilFile)) {
                soilLayer = (await Utils.getLayerByFilename(soilFile, FileTypes._SOILS, parent._gv, null, Utils._SOIL_GROUP_NAME)).Item1 as RasterLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._SOILS));
                if (layer != null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._SOILS)), parent._gv.isBatch, true) == MessageBoxResult.Yes) {
                        soilLayer = layer as RasterLayer;
                        soilFile = possFile;
                    }
                }
            }
            if (soilLayer is not null) {
                this.selectSoil.Text = soilFile;
                parent.soilFile = soilFile;
                parent.soilLayer = soilLayer;
                // hide soilLayer
                await Utils.setLayerVisibility(soilLayer, false);
            }
            (parent._gv.db.useSTATSGO, found) = proj.readBoolEntry(title, "soil/useSTATSGO", false);
            if (found && parent._gv.db.useSTATSGO) {
                this.STATSGOButton.Checked = true;
            }
            (parent._gv.db.useSSURGO, found) = proj.readBoolEntry(title, "soil/useSSURGO", false);
            if (found && parent._gv.db.useSSURGO) {
                this.SSURGOButton.Checked = true;
            }
            (landuseTable, found) = proj.readEntry("", "landuse/table", "");
            if (found) {
                if (landuseTable.Contains(".csv")) {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !landuseTable.Contains(":") 
                        || !landuseTable.StartsWith("/")) {
                        // relative name: prefix with project directory
                        landuseTable = Utils.join(parent._gv.projDir, landuseTable);
                    }
                    if (File.Exists(landuseTable)) {
                        landuseTable = this.parent.readCsvFile(landuseTable, "landuse", parent._gv.db.landuseTableNames);
                    } else {
                        Utils.information(string.Format("Landuse setting {0} appears to be a csv file but cannot be found.  Setting will be ignored", landuseTable), parent._gv.isBatch);
                        landuseTable = "";
                    }
                }
                if (landuseTable != "") {
                    index = this.selectLanduseTable.FindStringExact(landuseTable);
                    if (index >= 0) {
                        this.selectLanduseTable.SelectedIndex = index;
                    }
                    parent._gv.landuseTable = landuseTable;
                }
            }
            (soilTable, found) = proj.readEntry("", "soil/table", "");
            if (found) {
                if (soilTable.Contains(".csv")) {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !soilTable.Contains(":") 
                        || !soilTable.StartsWith("/")) {
                        // relative name: prefix with project directory
                        soilTable = Utils.join(parent._gv.projDir, soilTable);
                    }
                    if (File.Exists(soilTable)) {
                        soilTable = this.parent.readCsvFile(soilTable, "soil", parent._gv.db.soilTableNames);
                    } else {
                        Utils.information(string.Format("Soil setting {0} appears to be a csv file but cannot be found.  Setting will be ignored", soilTable), parent._gv.isBatch);
                        soilTable = "";
                    }
                }
                if (soilTable != "") {
                    index = this.selectSoilTable.FindStringExact(soilTable);
                    if (index >= 0) {
                        this.selectSoilTable.SelectedIndex = index;
                    }
                    parent._gv.soilTable = soilTable;
                }
            }
            int elevBandsThreshold;
            (elevBandsThreshold, found) = proj.readNumEntry(title, "hru/elevBandsThreshold", 0);
            if (found) {
                parent._gv.elevBandsThreshold = elevBandsThreshold;
            }
            int numElevBands;
            (numElevBands, found) = proj.readNumEntry(title, "hru/numElevBands", 0);
            if (found) {
                parent._gv.numElevBands = numElevBands;
            }
            string slopeBands;
            (slopeBands, found) = proj.readEntry("", "hru/slopeBands", "");
            if (found && slopeBands != "") {
                parent._gv.db.slopeLimits = Utils.parseSlopes(slopeBands);
                this.slopeBrowser.Text = slopeBands;
            }
            (slopeBandsFile, found) = proj.readEntry(title, "hru/slopeBandsFile", "");
            RasterLayer slopeBandsLayer = null;
            if (found && !string.IsNullOrEmpty(slopeBandsFile) && File.Exists(slopeBandsFile)) {
                slopeBandsLayer = (await Utils.getLayerByFilename(slopeBandsFile, FileTypes._SLOPEBANDS, parent._gv, null, Utils._SLOPE_GROUP_NAME)).Item1 as RasterLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._SLOPEBANDS));
                if (layer != null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._SLOPEBANDS)), parent._gv.isBatch, true) == MessageBoxResult.Yes) {
                        slopeBandsLayer = layer as RasterLayer;
                        slopeBandsFile = possFile;
                    }
                }
            }
            if (slopeBandsLayer is not null) {
                parent._gv.slopeBandsFile = slopeBandsFile;
                // hide slopeBandsLayer
                await Utils.setLayerVisibility(slopeBandsLayer, false);
            } else {
                parent._gv.slopeBandsFile = "";
            }
            (parent.CreateHRUs.isMultiple, found) = proj.readBoolEntry(title, "hru/isMultiple", false);
            (parent.CreateHRUs.isDominantHRU, found) = proj.readBoolEntry(title, "hru/isDominantHRU", true);
            (parent.CreateHRUs.isArea, found) = proj.readBoolEntry(title, "hru/isArea", false);
            (parent.CreateHRUs.isTarget, found) = proj.readBoolEntry(title, "hru/isTarget", false);
            (parent.CreateHRUs.useArea, found) = proj.readBoolEntry(title, "hru/useArea", false);
            if (parent.CreateHRUs.isMultiple) {
                if (parent.CreateHRUs.isArea) {
                    this.filterAreaButton.Checked = true;
                } else if (parent.CreateHRUs.isTarget) {
                    this.targetButton.Checked = true;
                } else {
                    this.filterLanduseButton.Checked = true;
                }
            } else if (parent.CreateHRUs.isDominantHRU) {
                this.dominantHRUButton.Checked = true;
            } else {
                this.dominantLanduseButton.Checked = true;
            }
            if (parent.CreateHRUs.useArea) {
                this.areaButton.Checked = true;
            } else {
                this.percentButton.Checked = true;
            }
            (parent.CreateHRUs.areaVal, found) = proj.readNumEntry(title, "hru/areaVal", 0);
            if (found && parent.CreateHRUs.areaVal > 0) {
                this.areaVal.Text = parent.CreateHRUs.areaVal.ToString();
            }
            (parent.CreateHRUs.landuseVal, found) = proj.readNumEntry(title, "hru/landuseVal", 0);
            if (found && parent.CreateHRUs.landuseVal > 0) {
                this.landuseVal.Text = parent.CreateHRUs.landuseVal.ToString();
            }
            (parent.CreateHRUs.soilVal, found) = proj.readNumEntry(title, "hru/soilVal", 0);
            if (found && parent.CreateHRUs.soilVal > 0) {
                this.soilVal.Text = parent.CreateHRUs.soilVal.ToString();
            }
            (parent.CreateHRUs.slopeVal, found) = proj.readNumEntry(title, "hru/slopeVal", 0);
            if (found && parent.CreateHRUs.slopeVal > 0) {
                this.slopeVal.Text = parent.CreateHRUs.slopeVal.ToString();
            }
            (parent.CreateHRUs.targetVal, found) = proj.readNumEntry(title, "hru/targetVal", 0);
            if (found && parent.CreateHRUs.targetVal > 0) {
                this.targetVal.Text = parent.CreateHRUs.targetVal.ToString();
            }
        }

        // Set up soil lookup tables.
        public virtual bool initSoils(string table, bool checkSoils) {
            if (parent._gv.db.useSSURGO) {
                // no lookup table needed
                return true;
            }
            parent._gv.db.SSURGOSoils = new Dictionary<int, int>();
            parent._gv.db.soilVals = new List<int>();
            if (table == "") {
                parent._gv.soilTable = this.selectSoilTable.Text;
                if (parent._gv.soilTable == Parameters._USECSV) {
                    parent._gv.soilTable = parent.readSoilCsv();
                    if (parent._gv.soilTable != "") {
                        this.selectSoilTable.Items.Insert(0, parent._gv.soilTable);
                        this.selectSoilTable.SelectedIndex = 0;
                    }
                }
                if (!parent._gv.db.soilTableNames.Contains(parent._gv.soilTable)) {
                    Utils.error("Please select a soil table", parent._gv.isBatch);
                    return false;
                }
            } else {
                // doing tryRun and table already read from project file
                parent._gv.soilTable = table;
            }
            if (parent._gv.forTNC) {
                parent._gv.setTNCUsersoil();
            }
            return parent._gv.db.populateSoilNames(parent._gv.soilTable, checkSoils);
        }

        public void setForCalcHRUs() {
            this.slopeSlider.Enabled = false;
            this.slopeVal.Enabled = false;
            this.areaGroup.Enabled = false;
            this.targetGroup.Enabled = false;
            this.landuseSoilSlopeGroup.Enabled = false;
        }

        public bool isDominantHRU {
            get { return this.dominantHRUButton.Checked; }
        }

        public bool isMultiple {
            get {  return !this.dominantHRUButton.Checked && !this.dominantLanduseButton.Checked; }
        }

        public bool useArea {
            get { return this.areaButton.Checked; }
        }

        // Update progress label with message.
        public virtual void progress(string msg) {
            Utils.progress(msg, this.progressLabel);
        }

        public void setForReading() {
            this.slopeGroup.Enabled = false;
            this.generateFullHRUs.Enabled = false;
            this.elevBandsButton.Enabled = false;
        }

        public bool readFromMapsChecked() {
            return this.readFromMaps.Checked;
        }

        public bool readFromPreviousChecked() {
            return this.readFromPrevious.Checked;
        }

        public void setToCreateHRUs(bool withSplitExempt=false) {
            this.fullHRUsLabel.Text = string.Format("Full HRUs count: {0}", parent.CreateHRUs.countFullHRUs());
            this.hruChoiceGroup.Enabled = true;
            this.areaPercentChoiceGroup.Enabled = true;
            if (withSplitExempt) {
                this.splitButton.Enabled = true;
                this.exemptButton.Enabled = true;
            }
            this.setHRUChoice();
        }

        public void setProgressBar(int val) {
            this.progressBar.Value = val;
            this.progressBar.Visible = true;
        }

        public void hideProgressBar() {
            this.progressBar.Visible = false;
        }

        public void addProgressBar(int val) {
            this.progressBar.Value = this.progressBar.Value + val;
        }


        public bool generateFullHRUsChecked() {
            return this.generateFullHRUs.Checked;
        }

        public string currentLanduse {
            get { return this.selectLanduseTable.Text; }
            set { 
                this.selectLanduseTable.Items.Insert(0, value);
                this.selectLanduseTable.SelectedIndex = 0;
            }
        }

        public Label progressionLabel {
            get { return this.progressLabel;  }
        }

        public ProgressBar progressionBar {
            get { return this.progressBar; }
        }

        private void selectLanduseTable_SelectionChangeCommitted(object sender, EventArgs e) {
            parent._gv.landuseTable = this.selectLanduseTable.SelectedItem.ToString();
            // set to read from maps 
            this.readFromPrevious.Enabled = false;
            this.readFromMaps.Checked = true;
        }

        private void selectSoilTable_SelectionChangeCommitted(object sender, EventArgs e) {
            // need to check for TNC usersoil tables
            parent._gv.setSoilTable(this.selectSoilTable.SelectedItem.ToString());
            // set to read from maps 
            this.readFromPrevious.Enabled = false;
            this.readFromMaps.Checked = true;
        }
    }
}

