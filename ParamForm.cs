using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArcSWAT3
{
    public partial class ParamForm : Form
    {
        private GlobalVars gv;

        public ParamForm(GlobalVars gv, string SWATEditorDir, string mpiexecDir)
        {
            InitializeComponent();
            this.gv = gv;
            this.checkUseMPI.Checked = Directory.Exists(mpiexecDir);
            this.MPIBox.Text = mpiexecDir;
            this.editorBox.Text = SWATEditorDir;
            if (gv is not null)
            {
                gv.parametersPos = this.Location;
            }
        }

        private void editorButton_Click(object sender, EventArgs e)
        {
            var title = Utils.trans("Select SWAT Editor directory");
            string startDir;
            if (editorBox.Text != "")
            {
                startDir = Path.GetDirectoryName(editorBox.Text);
            }
            else if (Directory.Exists(gv.SWATExeDir))
            {
                startDir = Path.GetDirectoryName(gv.SWATExeDir);
            }
            else
            {
                startDir = null;
            }
            folderBrowserDialog1.ShowNewFolderButton = false;
            folderBrowserDialog1.Description = title;
            if (startDir is not null)
            {
                folderBrowserDialog1.SelectedPath = startDir;
            }
            // Show the FolderBrowserDialog.  
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                editorBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void MPIButton_Click(object sender, EventArgs e)
        {
            var title = Utils.trans("Select MPI bin directory");
            string startDir;
            if (MPIBox.Text != "")
            {
                startDir = Path.GetDirectoryName(MPIBox.Text);
            }
            else if (File.Exists(gv.mpiexecPath))
            {
                startDir = Path.GetDirectoryName(gv.mpiexecPath);
            }
            else
            {
                startDir = null;
            }
            folderBrowserDialog1.ShowNewFolderButton = false;
            folderBrowserDialog1.Description = title;
            if (startDir is not null)
            {
                folderBrowserDialog1.SelectedPath = startDir;
            }
            // Show the FolderBrowserDialog.  
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                MPIBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var SWATEditorDir = this.editorBox.Text;
            if (SWATEditorDir == "" || !Directory.Exists(SWATEditorDir))
            {
                Utils.error("Please select the SWAT editor directory", gv.isBatch);
                return;
            }
            var editor = Parameters._SWATEDITOR;
            var SWATEditorPath = Utils.join(SWATEditorDir, editor);
            if (!File.Exists(SWATEditorPath))
            {
                Utils.error(String.Format("Cannot find the SWAT editor {0}", SWATEditorPath), gv.isBatch);
                return;
            }
            var dbProjTemplate = Utils.join(Utils.join(SWATEditorDir, Parameters._DBDIR), Parameters._DBPROJ);
            if (!File.Exists(dbProjTemplate))
            {
                Utils.error(String.Format("Cannot find the default project database {0}", dbProjTemplate), gv.isBatch);
                return;
            }
            var dbRefTemplate = Utils.join(Utils.join(SWATEditorDir, Parameters._DBDIR), Parameters._DBREF);
            if (!File.Exists(dbRefTemplate))
            {
                Utils.error(String.Format("Cannot find the SWAT parameter database {0}", dbRefTemplate), gv.isBatch);
                return;
            }
            var TauDEMDir = Utils.join(SWATEditorDir, Parameters._TAUDEM539DIR);
            if (!Directory.Exists(TauDEMDir))
            {
                Utils.error(String.Format("Cannot find the TauDEM directory {0}", TauDEMDir), gv.isBatch);
                return;
            }
            string mpiexecPath = "";
            if (this.checkUseMPI.Checked)
            {
                var mpiexecDir = this.MPIBox.Text;
                if (mpiexecDir == "" || !Directory.Exists(mpiexecDir))
                {
                    Utils.error("Please select the MPI bin directory", gv.isBatch);
                    return;
                }
                var mpiexec = Parameters._MPIEXEC;
                mpiexecPath = Utils.join(mpiexecDir, mpiexec);
                if (!File.Exists(mpiexecPath))
                {
                    Utils.error(String.Format("Cannot find mpiexec program {0}", mpiexecPath), gv.isBatch);
                    return;
                }
            }
            // no problems - save parameters
            if (this.gv is not null)
            {
                this.gv.dbProjTemplate = dbProjTemplate;
                this.gv.dbRefTemplate = dbRefTemplate;
                this.gv.TauDEMDir = TauDEMDir;
                this.gv.mpiexecPath = mpiexecPath;
                this.gv.SWATEditorPath = SWATEditorPath;
            }
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkUseMPI_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkUseMPI.Checked)
            {
                this.MPIBox.Enabled = true;
                this.MPIButton.Enabled = true;
                this.MPILabel.Enabled = true;
            }
            else
            {
                this.MPIBox.Enabled = false;
                this.MPIButton.Enabled = false;
                this.MPILabel.Enabled = false;
            }

        }
    }
}
