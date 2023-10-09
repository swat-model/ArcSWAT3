/*
 * Created by SharpDevelop.
 * User: Chris
 * Date: 11/25/2022
 * Time: 5:51 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Path = System.IO.Path;
using System.Windows.Forms;
using System.Threading;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Reports;
using System.Diagnostics;

namespace ArcSWAT3
{

    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
	{

        public GlobalVars _gv;
        private ArcSWAT _parent;
        public ComboBox reportsCombo;
        public MainForm(ArcSWAT parent)
		{
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            this._parent = parent;
            this.reportsCombo = this.reportsBox;
            this.reportsBox.Visible = false;
            this.reportsLabel.Visible = false;
            this.reportsBox.Items.Clear();
            this.reportsBox.Items.Add(Utils.trans("Select report to view"));
            this.reportsBox.SelectedIndex = 0;
            //this.finished.connect(this.finish);
            // connect buttons
            //this.aboutButton.clicked.connect(this.about);
            //this.newButton.clicked.connect(this.newProject);
            //this.existingButton.clicked.connect(this.existingProject);
            //this.delinButton.clicked.connect(this.doDelineation);
            //this.hrusButton.clicked.connect(this.doCreateHRUs);
            //this.editButton.clicked.connect(this.startEditor);
            //this.visualiseButton.clicked.connect(this.visualise);
            //this.paramsButton.clicked.connect(this.runParams);
            //this.reportsBox.activated.connect(this.showReport);
            this.initButtons();
            this.projPath.Text = "";


        }

        // Initial button settings.
        public void initButtons() {
            this.delinLabel.Text = "Step 1";
            this.hrusLabel.Text = "Step 2";
            this.editLabel.Text = "Step 3";
            this.hrusLabel.Enabled = false;
            this.hrusButton.Enabled = false;
            this.editLabel.Enabled = false;
            this.editButton.Enabled = false;
            this.visualiseLabel.Visible = false;
            this.visualiseButton.Visible = false;
            bool haveProject = MapView.Active is not null;
            this.mainBox.Visible = haveProject; 
            this.paramsButton.Visible = haveProject;
            this.reportsBox.Visible = false;
            this.reportsLabel.Visible = false;
            this.projPath.Visible = haveProject;
        }

        public void setProject(string project) {
            this.projPath.Text = project;
        }

        // Mark delineation as Done and make create HRUs option visible.
        public void allowCreateHRU() {
            Utils.progress("Done", this.delinLabel);
            Utils.progress("Step 2", this.hrusLabel);
            this.hrusLabel.Enabled = true;
            this.hrusButton.Enabled = true;
            this.editLabel.Enabled = false;
            this.editButton.Enabled = false;
        }

        public void allowEdit() {
            Utils.progress("Done", this.hrusLabel);
            this.editLabel.Enabled = true;
            this.editButton.Enabled = true;
        }

        public void allowVisualise() {
            this.visualiseLabel.Visible = true;
            this.visualiseButton.Visible = true;
        }

        private void delinButton_Click(object sender, EventArgs e)
        {
            this._parent.doDelineation(); 
        }

        private void MainForm_Load(object sender, EventArgs e) {
        }

        private void aboutButton_Click(object sender, EventArgs e) {
            var dlg = new AboutForm(_gv, ArcSWAT.@__version__);
            dlg.ShowDialog();
        }

        private void reportsBox_SelectedIndexChanged(object sender, EventArgs e) {
            // Display selected report.
            //         
            //         In case project converted from ArcSWAT, also accept ArcSWAT report names.
            if (!this.reportsBox.ContainsFocus) {
                return;
            }
            string  report = "";
            string  arcReport = "";
            var item = this.reportsBox.SelectedItem;
            var itemString = item.ToString();
            if (itemString == Parameters._TOPOITEM) {
                report = Parameters._TOPOREPORT;
                arcReport = "";
            } else if (itemString == Parameters._BASINITEM) {
                report = Parameters._BASINREPORT;
                arcReport = Parameters._ARCBASINREPORT;
            } else if (itemString == Parameters._HRUSITEM) {
                report = Parameters._HRUSREPORT;
                arcReport = Parameters._ARCHRUSREPORT;
            } else {
                return;
            }
            //Debug.Assert(this._gv is not null);
            var rept = Utils.join(this._gv.textDir, report);
            if (!File.Exists(rept)) {
                rept = Utils.join(this._gv.textDir, arcReport);
            }
            Process.Start("notepad.exe", "\"" + rept + "\"");
            this.reportsBox.SelectedIndex = 0;
        }

        // Add existing reports to reports box and if there are some make it visible.
        //         
        //         Include ArcSWAT names for reports in case converted from ArcSWAT.
        public void checkReports() {
            //Debug.Assert(this._gv is not null);
            var makeVisible = false;
            var topoReport = Utils.join(this._gv.textDir, Parameters._TOPOREPORT);
            if (File.Exists(topoReport) && this.reportsBox.FindString(Parameters._TOPOITEM) < 0) {
                makeVisible = true;
                this.reportsBox.Items.Add(Parameters._TOPOITEM);
            }
            var basinReport = Utils.join(this._gv.textDir, Parameters._BASINREPORT);
            if (!File.Exists(basinReport)) {
                basinReport = Utils.join(this._gv.textDir, Parameters._ARCBASINREPORT);
            }
            if (File.Exists(basinReport) && this.reportsBox.FindString(Parameters._BASINITEM) < 0) {
                makeVisible = true;
                this.reportsBox.Items.Add(Parameters._BASINITEM);
            }
            var hrusReport = Utils.join(this._gv.textDir, Parameters._HRUSREPORT);
            if (!File.Exists(hrusReport)) {
                hrusReport = Utils.join(this._gv.textDir, Parameters._ARCHRUSREPORT);
            }
            if (File.Exists(hrusReport) && this.reportsBox.FindString(Parameters._HRUSITEM) < 0) {
                makeVisible = true;
                this.reportsBox.Items.Add(Parameters._HRUSITEM);
            }
            if (makeVisible) {
                this.reportsBox.Visible = true;
                this.reportsLabel.Visible = true;
                this.reportsBox.SelectedIndex = 0;
            }
        }

        // Show reports combo box and add items if necessary.
        public virtual void showReports() {
            this.reportsBox.Visible = true;
            if (this.reportsBox.FindString(Parameters._TOPOITEM) < 0) {
                this.reportsBox.Items.Add(Parameters._TOPOITEM);
            }
            if (this.reportsBox.FindString(Parameters._BASINITEM) < 0) {
                this.reportsBox.Items.Add(Parameters._BASINITEM);
            }
            if (this.reportsBox.FindString(Parameters._HRUSITEM) < 0) {
                this.reportsBox.Items.Add(Parameters._HRUSITEM);
            }
        }

        private void OKButton_Click(object sender, EventArgs e) {
            this._parent.finish(DialogResult.OK);
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this._parent.finish(DialogResult.Cancel);
            this.Close();
        }

        private void paramsButton_Click(object sender, EventArgs e) {
            var pf = new ParamForm(this._gv, Parameters._SWATEDITORDEFAULTDIR, Parameters._MPIEXECDEFAULTDIR);
            pf.ShowDialog(); 
        }

        private void hrusButton_Click(object sender, EventArgs e) {
            this._parent.doCreateHRUs();
        }

        private void editButton_Click(object sender, EventArgs e) {
            this._parent.startEditor();
        }

        private void existingButton_Click(object sender, EventArgs e) {
            this._parent.existingProject();
        }

        private void newButton_Click(object sender, EventArgs e) {
            this._parent.newProject();
        }

        private void visualiseButton_Click(object sender, EventArgs e) {
            this._parent.visualise();
        }
    }
}
