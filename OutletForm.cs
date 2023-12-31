﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

//using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Editing.Attributes;
using OSGeo.OGR;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Internal.Mapping.Symbology;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace ArcSWAT3
{

    public partial class OutletForm : Form
    {
        private DelinForm _parent;
        private string currentTool;
        //OSGeo.OGR.DataSource ds;
        //OSGeo.OGR.Layer layer;
        public FeatureClass fc;
        public FeatureLayer currentLayer;
        //private string filename;
        public int lastIndex;
        public FeatureClassDefinition def;
        public int idIndex, resIndex, inletIndex, ptsourceIndex;

        private async void okButton_Click(object sender, EventArgs e) {
            FrameworkApplication.CurrentTool = this.currentTool;
            //this.ds = null;
            await Project.Current.SaveEditsAsync();
            //this.fc = null;
            this.Close();
            await this._parent.addOutlets(true);
        }

        private async void cancelButton_Click(object sender, EventArgs e) {
            FrameworkApplication.CurrentTool = this.currentTool;
            //this.ds = null;
            await Project.Current.DiscardEditsAsync();
            //this.fc = null;
            this.Close();
            await this._parent.addOutlets(false);
        }

        public OutletForm() {
            InitializeComponent();
        }

        public async Task setup(FeatureLayer layer, DelinForm parent) {
            this._parent = parent; 
            this.lastIndex = 0;
            this.currentLayer = layer;
            //this.filename = filename;
            //this.ds = OSGeo.OGR.Ogr.Open(filename, 1);
            //this.layer = ds.GetLayerByIndex(0);
            //this.def = layer.GetLayerDefn();
            //this.idIndex = def.GetFieldIndex("ID");
            //this.resIndex = def.GetFieldIndex("RES");
            //this.inletIndex = def.GetFieldIndex("INLET");
            //this.ptsourceIndex = def.GetFieldIndex("PTSOURCE");
            //layer.ResetReading();
            //OSGeo.OGR.Feature f = null;
            //do {
            //    f = layer.GetNextFeature();
            //    if (f != null) {
            //        this.lastIndex = Math.Max(f.GetFieldAsInteger(this.idIndex), this.lastIndex);
            //    }
            //} while (f != null);
            await QueuedTask.Run(() => {
                this.fc = this.currentLayer.GetFeatureClass();
                this.def = fc.GetDefinition();
                this.idIndex = def.FindField("ID");
                this.resIndex = def.FindField("RES");
                this.inletIndex = def.FindField("INLET");
                this.ptsourceIndex = def.FindField("PTSOURCE");
                var rowCursor = fc.Search();
                while (rowCursor.MoveNext()) {
                    using (Row row = rowCursor.Current) {
                        var pt = row as ArcGIS.Core.Data.Feature;
                        var geom = pt.GetShape();
                        Utils.information(String.Format("Outlet geometry: {0}", geom.ToString()), false);
                        this.lastIndex = Math.Max(Convert.ToInt32(row[idIndex]), this.lastIndex);
                    }
                }
            });

            this.currentTool = FrameworkApplication.CurrentTool;
            await FrameworkApplication.SetCurrentToolAsync("ArcSWAT3_PointTool");
            //FrameworkApplication.CurrentTool = "ArcSWAT3_PointTool";
            //ICommand cmd = FrameworkApplication.GetPlugInWrapper("ArcSWAT3_PointTool") as ICommand;
            //if ((cmd != null) && cmd.CanExecute(null))
            //    cmd.Execute(null);
        }

        private void OutletForm_Load(object sender, EventArgs e) {
            ArcGIS.Desktop.Mapping.Events.SketchCompletedEvent.Subscribe(async (args) => {
                //MessageBox.Show("Sketch completed");
                if (args.Sketch?.IsEmpty ?? true)
                    return;
                var message = String.Empty;
                var geom = args.Sketch as MapPoint;
                Utils.information(String.Format("Point is ({0}, {1})", geom.X, geom.Y), false);
                this.lastIndex++;
                //OSGeo.OGR.Feature pt1 = new OSGeo.OGR.Feature(def);
                //pt1.SetField(this.idIndex, this.lastIndex);
                //pt1.SetField(resIndex, this.reservoirButton.Checked ? 1 : this.pondButton.Checked ? 2 : 0);
                //pt1.SetField(inletIndex, (this.inletButton.Checked || this.ptsourceButton.Checked) ? 1 : 0);
                //pt1.SetField(ptsourceIndex, this.ptsourceButton.Checked ? 1 : 0);
                //OSGeo.OGR.Geometry ogrPoint = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
                //ogrPoint.SetPoint_2D(0, geom.X, geom.Y);
                //pt1.SetGeometry(ogrPoint);
                //var num = layer.CreateFeature(pt1);
                //if (num < 0) { message = string.Format("Failed to create point at ({0}, {1})", ogrPoint.GetX(0), ogrPoint.GetY(0)); }
                await QueuedTask.Run(() => {
                    EditOperation editOperation = new EditOperation();
                    editOperation.Callback(context => {
                        using (RowBuffer rowBuffer = this.fc.CreateRowBuffer()) {
                            rowBuffer[this.idIndex] = this.lastIndex;
                            rowBuffer[this.resIndex] = this.reservoirButton.Checked ? 1 : this.pondButton.Checked ? 2 : 0;
                            rowBuffer[this.inletIndex] = (this.inletButton.Checked || this.ptsourceButton.Checked) ? 1 : 0;
                            rowBuffer[this.ptsourceIndex] = this.ptsourceButton.Checked ? 1 : 0;
                            rowBuffer[def.GetShapeField()] = new MapPointBuilderEx(geom.X, geom.Y).ToGeometry();
                            using (ArcGIS.Core.Data.Feature feature = this.fc.CreateRow(rowBuffer)) {
                                //To indicate that the attribute table has to be updated
                                context.Invalidate(feature);
                            }
                        }
                    }, fc);
                    try {
                        var creationResult = editOperation.Execute();
                        if (!creationResult) {
                            message = editOperation.ErrorMessage;
                        }
                    }
                    catch {
                        message = editOperation.ErrorMessage;
                    }
                });
                if (!string.IsNullOrEmpty(message))
                    MessageBox.Show("ERROR: Point creation failed: " + message);
            }, true);
            //MessageBox.Show("Subscription to sketch completion");
        }
    }
}
