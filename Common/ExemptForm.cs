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
    public partial class ExemptForm : Form
    {

        private GlobalVars _gv;

        private List<string> landuses;

        private List<string> exemptLanduses;

        public ExemptForm(GlobalVars gv) {
            InitializeComponent();
            this._gv = gv;
            //# landuse codes occurring in landuse map, or used for a split, and not exempt
            this.landuses = new List<string>();
            //# landuse codes marked for exemption
            this.exemptLanduses = new List<string>();
        }

        // Run exempt dialog.
        public virtual void run() {
            foreach (var landuseVal in this._gv.db.landuseVals) {
                var landuse = this._gv.db.getLanduseCode(landuseVal);
                ListFuns.insertIntoSortedStringList(landuse, this.landuses, true);
            }
            foreach (var subs in this._gv.splitLanduses.Values) {
                foreach (var landuse in subs.Keys) {
                    ListFuns.insertIntoSortedStringList(landuse, this.landuses, true);
                }
            }
            foreach (var landuse in this._gv.exemptLanduses) {
                ListFuns.insertIntoSortedStringList(landuse, this.exemptLanduses, true);
                // defensive coding
                if (this.landuses.Contains(landuse)) {
                    this.landuses.Remove(landuse);
                }
            }
            this.fillBoxes();
            this.ShowDialog();
        }

        // Initialise dialog combo boxes.
        public virtual void fillBoxes() {
            this.chooseBox.Items.Clear();
            foreach (var landuse in this.landuses) {
                this.chooseBox.Items.Add(landuse);
            }
            this.chooseBox.SelectedIndex = -1;
            this.exemptBox.Items.Clear();
            foreach (var landuse in this.exemptLanduses) {
                this.exemptBox.Items.Add(landuse);
            }
            
        }

        // Add an exemption.
        public virtual void addExempt() {
            var landuse = this.chooseBox.SelectedItem.ToString();
            // should be present but better safe than sorry
            if (this.landuses.Contains(landuse)) {
                this.landuses.Remove(landuse);
            }
            ListFuns.insertIntoSortedStringList(landuse, this.exemptLanduses, true);
            this.fillBoxes();
        }

        // Remove an exemption.
        public virtual void delExempt() {
            if (this.exemptBox.SelectedItem is null) {
                Utils.information("Please select an exempt landuse for its exemption to be cancelled", this._gv.isBatch);
                return;
            }
            var landuse = this.exemptBox.SelectedItem.ToString();
            // should be present but better safe than sorry
            if (this.exemptLanduses.Contains(landuse)) {
                this.exemptLanduses.Remove(landuse);
            }
            ListFuns.insertIntoSortedStringList(landuse, this.landuses, true);
            this.fillBoxes();
        }

        private void chooseBox_SelectionChangeCommitted(object sender, EventArgs e) {
            this.addExempt();
        }

        private void cancelExemptionButton_Click(object sender, EventArgs e) {
            this.delExempt();
        }

        private void okButton_Click(object sender, EventArgs e) {
            this._gv.exemptLanduses = this.exemptLanduses;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
