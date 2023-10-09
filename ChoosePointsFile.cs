using ArcGIS.Desktop.Internal.Catalog.PropertyPages.NetworkDataset;
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
    public partial class ChoosePointsFile : Form
    {

        public ChoosePointsFile() {
            InitializeComponent();
        }

        private void currentbutton_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.OK;
            _drawFile = null;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void newButton_Click(object sender, EventArgs e) {
            if (this.inletsOutletsFileDialog.ShowDialog() == DialogResult.OK) {
                _drawFile = this.inletsOutletsFileDialog.FileName;
                DialogResult = DialogResult.Yes;
                Close();
            }
        }

        private string _drawFile;
        public string drawFile {
            get => _drawFile;
        }
    }
}
