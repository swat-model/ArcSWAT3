using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ArcSWAT3
{
    public partial class SplitForm : Form
    {
        private GlobalVars _gv;

        private Dictionary<string, Dictionary<string, int>> _splitLanduses;

        private DataTable data;

        public SplitForm(GlobalVars gv) {
            InitializeComponent();
            this._gv = gv;
            this._splitLanduses = new Dictionary<string, Dictionary<string, int>>();
            this.data = new DataTable();
            this.data.Columns.Add("Landuse", typeof(string));
            this.data.Columns.Add("Sub-landuse", typeof(string));
            this.data.Columns.Add("Percent", typeof(int));
            //DataColumn column;
            //column = new DataColumn();
            //column.DataType = typeof(string);
            //data.Columns.Add(column);
            //column = new DataColumn();
            //column.DataType = typeof(string);
            //data.Columns.Add(column);
            //column = new DataColumn();
            //column.DataType = typeof(int);
            //data.Columns.Add(column);

        }

        // Setup and run the dialog.
        public virtual void run() {
            this.table.DataSource = this.data;
            this.table.Columns[0].Width = 60;
            this.table.Columns[1].Width = 80;
            this.table.Columns[2].Width = 50;
            this.populateCombos();
            this.newCombo.SelectedIndex = -1;
            this.splitCombo.SelectedIndex = -1;
            this.ShowDialog();
        }

        // Add a new sub-landuse to the table.
        public virtual void add() {
            if (this.data.Rows.Count < 1) {
                return;
            }
            this.addSplitRow((string)this.data.Rows[0][0]);
        }

        // Add a new row to the table.
        public virtual void addSplitRow(string luse) {
            var slu = new SelectLuForm(this._gv);
            var subluse = slu.run();
            if (subluse == "") {
                return;
            }
            foreach (var i in Enumerable.Range(0, this.data.Rows.Count)) {
                if ((string)this.data.Rows[i][1] == subluse) {
                    Utils.information(string.Format("Sub-landuse {0} already used in this split", subluse), this._gv.isBatch);
                    return;
                }
            }
            this.addSplitItems(luse, subluse, 100);
        }

        // Populate last row items.
        public virtual void addSplitItems(string luse, string subluse, int percent) {
            var numRows = this.data.Rows.Count;
            var luse1 = numRows == 0 ? luse : "";
            var row = this.data.NewRow();
            row[0] = luse1;
            row[1] = subluse;
            row[2] = percent;
            this.data.Rows.InsertAt(row, numRows);
            this.data.AcceptChanges();
        }

        // Delete selected row from the table.
        public virtual void deleteRow() {
            var selection = this.table.SelectedRows;
            if (selection == null || selection.Count == 0) {
                Utils.information("Please select a row to delete", this._gv.isBatch);
                return; }
            var row = this.table.SelectedRows[0];
            var index = this.table.Rows.IndexOf(row);
            var numRows = this.data.Rows.Count;
            if (index == 0) {
                if (numRows == 1) {
                    // whole split will be deleted
                    this.deleteSplit();
                    return;
                } else {
                    // need to copy luse being split into second row
                    var luse = (string)this.data.Rows[0][0];
                    this.data.Rows[1][0] = luse;
                }
            }
            this.data.Rows.RemoveAt(index);
            // leaves currentRow set, so make current row negative
            this.table.ClearSelection();
        }

        // Delete a split landuse.
        public virtual void deleteSplit() {
            var count = this.data.Rows.Count;
            if (count < 1) {
                Utils.information("No split to delete", this._gv.isBatch);
                return;
            }
            var luse = (string)this.data.Rows[0][0];
            this.clearTable();
            addItemToCombo(luse, this.newCombo);
            removeItemFromCombo(luse, this.splitCombo);
            this.newCombo.SelectedIndex = -1;
            this.splitCombo.SelectedIndex = -1;
            this._splitLanduses.Remove(luse);
        }

        // Clear the table.
        public virtual void cancelEdit() {
            this.clearTable();
            this.newCombo.SelectedIndex = -1;
            this.splitCombo.SelectedIndex = -1;
        }

        // Check the percentages sum to 100, save the data in the table,
        //         and clear it.  Return True if OK
        //         
        public virtual bool saveEdit() {
            var numRows = this.data.Rows.Count;
            if (numRows == 0) {
                return true;
            }
            // check total percentages
            var totalPercent = 0;
            try {
                foreach (var row in Enumerable.Range(0, numRows)) {
                    totalPercent += Convert.ToInt32(this.data.Rows[row][2]);
                }
            }
            catch (Exception) {
                Utils.error("Cannot parse percentages as integers", this._gv.isBatch);
                return false;
            }
            if (totalPercent != 100) {
                Utils.error("Percentages must sum to 100", this._gv.isBatch);
                return false;
            }
            var luse = (string)this.data.Rows[0][0];
            this._splitLanduses[luse] = new Dictionary<string, int>();
            foreach (var row in Enumerable.Range(0, numRows)) {
                var subluse = (string)this.data.Rows[row][1];
                if (subluse != luse && this._splitLanduses.Keys.Contains(subluse)) {
                    Utils.error(string.Format("Target {0} of a split may not itself be split", subluse), this._gv.isBatch);
                    return false;
                }
                var percent = Convert.ToInt32(this.data.Rows[row][2]);
                this._splitLanduses[luse][subluse] = percent;
            }
            addItemToCombo(luse, this.splitCombo);
            removeItemFromCombo(luse, this.newCombo);
            this.newCombo.SelectedIndex = -1;
            this.splitCombo.SelectedIndex = -1;
            this.clearTable();
            return true;
        }

        // Close the dialog.
        public virtual void cancel() {
            this.Close();
        }

        // Save the split landuses data and close the table.
        public virtual void saveSplits() {
            if (this.data.Rows.Count > 0) {
                var result = Utils.question("Save split currently in table?", this._gv.isBatch, true);
                if (result == MessageBoxResult.Yes) {
                    if (!this.saveEdit()) {
                        return;
                    }
                } else {
                    this.clearTable();
                }
            }
            // copy data to globals
            // clear globals first
            this._gv.splitLanduses.Clear();
            foreach (var (luse, subs) in this._splitLanduses) {
                this._gv.splitLanduses[luse] = new Dictionary<string, int>();
                foreach (var (subluse, percent) in subs) {
                    this._gv.splitLanduses[luse][subluse] = percent;
                }
            }
            this.Close();
        }

        // Start a new landuse split.
        public virtual void addNew() {
            if (this.data.Rows.Count > 0) {
                var result = Utils.question("Save split currently in table?", this._gv.isBatch, true);
                if (result == MessageBoxResult.Yes) {
                    this.saveEdit();
                } else {
                    this.clearTable();
                }
            }
            this.splitCombo.SelectedIndex = -1;
            this.addSplitRow(this.newCombo.SelectedItem.ToString());
        }

        // Populate the table with an existing split to be edited.
        public virtual void selectSplit() {
            var luse = this.splitCombo.SelectedItem.ToString();
            if (this.data.Rows.Count > 0 && (string)this.data.Rows[0][0] != luse) {
                var result = Utils.question("Save split currently in table?", this._gv.isBatch, true);
                if (result == MessageBoxResult.Yes) {
                    this.saveEdit();
                } else {
                    this.clearTable();
                }
            }
            foreach (var (subluse, percent) in this._splitLanduses[luse]) {
                this.addSplitItems(luse, subluse, percent);
            }
            this.splitCombo.SelectedIndex = -1;
        }

        // Populate the combo boxes from global data.
        public virtual void populateCombos() {
            this._gv.db.populateMapLanduses(this._gv.db.landuseVals, this.newCombo);
            this._gv.populateSplitLanduses(this.splitCombo);
            foreach (var i in Enumerable.Range(0, this.splitCombo.Items.Count)) {
                var luse = (string)this.splitCombo.Items[i];
                var j = this.newCombo.Items.IndexOf(luse);
                if (j >= 0) {
                    this.newCombo.Items.RemoveAt(j);
                }
            }
            // copy any data from globals
            foreach (var (luse, subs) in this._gv.splitLanduses) {
                this._splitLanduses[luse] = new Dictionary<string, int>();
                foreach (var (subluse, percent) in subs) {
                    this._splitLanduses[luse][subluse] = percent;
                }
            }
        }

        // Clear the table.
        public virtual void clearTable() {
            this.data.Clear();
        }

        // Add an item to a combo if not already present.
        public static void addItemToCombo(string txt, ComboBox combo) {
            var index = combo.Items.IndexOf(txt);
            if (index < 0) {
                combo.Items.Add(txt);
            }
        }

        // Remove an item from a combo, or do nothing if not present.
        public static void removeItemFromCombo(string txt, ComboBox combo) {
            var index = combo.Items.IndexOf(txt);
            if (index >= 0) {
                combo.Items.RemoveAt(index);
            }
        }

        private void addButton_Click(object sender, EventArgs e) {
            this.add();
        }

        private void deleteButton_Click(object sender, EventArgs e) {
            this.deleteRow();
        }

        private void deleteSplitButton_Click(object sender, EventArgs e) {
            this.deleteSplit();
        }

        private void cancelEditButton_Click(object sender, EventArgs e) {
            this.cancelEdit();
        }

        private void saveEditButton_Click(object sender, EventArgs e) {
            this.saveEdit();
        }

        private void saveSplitsButton_Click(object sender, EventArgs e) {
            this.saveSplits();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.cancel();
        }

        private void newCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            this.addNew();
        }

        private void splitCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            this.selectSplit();
        }
    }
}
