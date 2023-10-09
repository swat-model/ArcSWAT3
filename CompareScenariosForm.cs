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
    public partial class CompareScenariosForm : Form
    {

        private Visualise _parent;
        public CompareScenariosForm(Visualise parent) {
            InitializeComponent();
            this._parent = parent;
        }

        private void okButton_Click(object sender, EventArgs e) {
            this._parent.setupCompareScenarios();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this._parent.closeCompareScenarios();
        }

        public ComboBox Pscenario1 => scenario1;
        public ComboBox Pscenario2 => scenario2;
    }
}
