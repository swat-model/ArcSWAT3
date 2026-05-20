using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArcSWAT3
{
    public partial class AboutForm : Form
    {
        public GlobalVars _gv;
        public AboutForm(GlobalVars gv, string version) {
            InitializeComponent();
            this._gv = gv;
            var text = String.Format(@"
ArcSWAT3 version: {0}

Current restrictions:
- runs only in Windows
- requires ArcGIS Pro with Spatial Analaysis", version);
            textBrowser.Text = text;
        }

        private void SWATHomeButton_Click(object sender, EventArgs e) {
            System.Diagnostics.Process.Start("explorer.exe", "http://swat.tamu.edu");
        }

        private void closeButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void AboutForm_Load(object sender, EventArgs e) {
            // prevents text being initially selected
            textBrowser.AppendText(" ");
        }
    }
}
