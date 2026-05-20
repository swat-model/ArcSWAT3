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
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void newButton_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
