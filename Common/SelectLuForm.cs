using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArcSWAT3
{
    public partial class SelectLuForm : Form
    {
        private GlobalVars _gv;

        private string _luse;

        public SelectLuForm(GlobalVars gv) {
            InitializeComponent();
            this._gv = gv;
            this._luse = "";
        }

        // Run the dialog and return selected landuse.
        public virtual string run() {
            var luses = this._gv.db.populateAllLanduses();
            this.listBox.DataSource = luses;
            var result = this.ShowDialog();
            if (result == DialogResult.OK) {
                return this._luse;
            } else {
                return "";
            }
        }

        // 
        //         A selection has the form 'LUSE (Description)' or 'USE (Description)'.
        //         This function returns 'LUSE' or 'USE': 
        //         namely the line up to the space before '('
        //         
        public virtual void select(string selection) {
            var length = selection.IndexOf("(");
            this._luse = selection.Substring(0, length - 1);
        }

        private void listBox_SelectedValueChanged(object sender, EventArgs e) {
            this.select(this.listBox.SelectedValue.ToString());
        }

        private void okButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
