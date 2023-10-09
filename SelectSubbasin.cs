using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using System.Windows.Input;

namespace ArcSWAT3
{
    public partial class SelectSubbasin : Form
    {
        private DelinForm _parent;
        private string currentTool;
        private FeatureLayer _wshedLayer;
        private GlobalVars _gv;
        private double meanArea;
        private int areaIndx;
        public SelectSubbasin(FeatureLayer wshedLayer, DelinForm parent, GlobalVars gv) {
            InitializeComponent();
            this._wshedLayer = wshedLayer;
            this._parent = parent;
            this._gv = gv;
            this.checkBox.Checked = false;
            this.groupBox.Visible = false;
            this.areaButton.Checked = false;
            this.percentButton.Checked = true;
            this.threshold.Text = "5";
            this.setup();
        }

        public async void setup() {
            this.areaIndx = await _gv.topo.getIndex(this._wshedLayer, Topology._AREA);
            double area = 0;
            var count = 0;
            await QueuedTask.Run(() => {
                using (RowCursor rowCursor = this._wshedLayer.Search(null)) {
                    while (rowCursor.MoveNext()) {
                        using (Row polygon = rowCursor.Current) {
                            area += Convert.ToDouble(polygon[this.areaIndx]);
                            count++;
                        }
                    }
                }
            });
            if (count == 0) {
                this.meanArea = 0;
            } else {
                this.meanArea = area / count;
            }
            this.currentTool = FrameworkApplication.CurrentTool;
            //ICommand cmd = FrameworkApplication.GetPlugInWrapper("ArcSWAT3_PolygonTool") as ICommand;
            //if ((cmd != null) && cmd.CanExecute(null))
            //    cmd.Execute(null);
        }

        private void saveButton_Click(object sender, EventArgs e) {
            FrameworkApplication.CurrentTool = this.currentTool;
            Close();
        }

        private async void cancelButton_Click(object sender, EventArgs e) {
            FrameworkApplication.CurrentTool = this.currentTool;
            Close();
            await MapView.Active.ClearSketchAsync();
            await QueuedTask.Run(() => {
                this._wshedLayer.ClearSelection();
            });
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e) {
            this.groupBox.Visible = this.checkBox.Checked;
        }

        private async void pushButton_Click(object sender, EventArgs e) {
            double threshold, thresholdM2;
            var num = this.threshold.Text;
            if (string.IsNullOrEmpty(num)) {
                Utils.error("No threshold is set", this._gv.isBatch);
                return;
            }
            try {
                threshold = Double.Parse(num);
            }
            catch (Exception) {
                Utils.error(string.Format("Cannot parse {0} as a number", num), this._gv.isBatch);
                return;
            }
            if (this.areaButton.Checked) {
                thresholdM2 = threshold * 10000;
            } else {
                thresholdM2 = this.meanArea * threshold / 100;
            }
            Selection selection = null;
            IReadOnlyList<long> selected = new List<long>();
            await QueuedTask.Run(() => {
                selection = this._wshedLayer.GetSelection();
                selected = selection.GetObjectIDs();
                var rows = new List<long>();
                using (RowCursor rowCursor = this._wshedLayer.Search(null)) {
                    while (rowCursor.MoveNext()) {
                        using (Row polygon = rowCursor.Current) {
                            var area = Convert.ToDouble(polygon[this.areaIndx]);
                            if (area < thresholdM2) {
                                var id = polygon.GetObjectID();
                                if (!selected.Contains(id)) {
                                    rows.Add(id);
                                }
                            }
                        }
                    }
                }
                selection.Add(rows);
                this._wshedLayer.SetSelection(selection);
            });
        }
    }
}
