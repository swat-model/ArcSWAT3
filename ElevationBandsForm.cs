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
    public partial class ElevationBandsForm : Form
    {

        private GlobalVars _gv;

        public ElevationBandsForm(GlobalVars gv) {
            InitializeComponent();
            this._gv = gv;
            if (this._gv.elevBandsThreshold > 0) {
                this.elevBandsThreshold.Text = this._gv.elevBandsThreshold.ToString();
                var _tmp_1 = this._gv.numElevBands;
                if (2 <= _tmp_1 && _tmp_1 <= 10) {
                    this.numElevBands.Value = this._gv.numElevBands;
                }
            }
        }

        // Run the form.
        public virtual void run() {
            this.ShowDialog();
        }

        // Save bands definition.
        public virtual void setBands() {
            var text = this.elevBandsThreshold.Text;
            if (text == "") {
                // clear elevation bands
                this._gv.elevBandsThreshold = 0;
                this._gv.numElevBands = 0;
                this.Close();
                return;
            }
            try {
                this._gv.elevBandsThreshold = Convert.ToInt32(text);
            }
            catch (Exception) {
                Utils.error(string.Format("Cannot parse threshold {0} as an integer", text), this._gv.isBatch);
                return;
            }
            this._gv.numElevBands = Convert.ToInt32(this.numElevBands.Value);
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e) {
            this.setBands();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
