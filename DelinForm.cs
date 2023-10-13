using ArcGIS.Core.Data;
using QueryFilter = ArcGIS.Core.Data.QueryFilter;
using ArcGIS.Core.Data.DDL;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using Envelope = ArcGIS.Core.Geometry.Envelope;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using Unit = ArcGIS.Core.Geometry.Unit;
using AngularUnit = ArcGIS.Core.Geometry.AngularUnit;
using LinearUnit = ArcGIS.Core.Geometry.LinearUnit;
using Polyline = ArcGIS.Core.Geometry.Polyline;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Security.Policy;
using ArcGIS.Desktop.Editing;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Internal.CIM;
using System.Security.Cryptography;
using System.Windows.Shapes;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Desktop.Internal.GeoProcessing;
using ArcGIS.Core.Data.Realtime;
using ArcGIS.Desktop.Internal.Core;
using ArcGIS.Desktop.Internal.Mapping.Locate.Controls;

using MaxRev.Gdal.Core;
using OSGeo.OGR;
// Disambiguate ArcGIS and GDAL
using Layer = ArcGIS.Desktop.Mapping.Layer;
using Feature = ArcGIS.Core.Data.Feature;

using System.Runtime.InteropServices;
using System.Windows.Diagnostics;
using ArcGIS.Desktop.Core;
using OSGeo.OSR;
using ArcGIS.Desktop.Editing.Attributes;
using System.Windows.Input;
using Microsoft.VisualBasic;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;

namespace ArcSWAT3
{

    public partial class DelinForm : Form {

        private ArcSWAT _parent;

        private GlobalVars _gv;

        public double areaOfCell;

        public bool changing;

        public bool delineationFinishedOK;

        public int demHeight;

        public int demWidth;

        public FeatureLayer drawOutletLayer;

        public string drawOutletFile;

        public bool drawCurrent;

        public FeatureLayer selFromLayer;

        public List<int> dX;

        public List<int> dY;

        public HashSet<int> extraReservoirBasins;

        public bool finishHasRun;

        public bool isDelineated;

        public MapTool mapTool;

        public string currentTool;

        public bool snapErrors;

        public string snapFile;

        public bool thresholdChanged;

        public string drawFile;

        public DelinForm(GlobalVars gv, bool isDelineated, ArcSWAT parent) {
            InitializeComponent();
            this._parent = parent;
            // use GDAL for creating shapefiles
            // GdalBase.ConfigureAll();
            this._gv = gv;
            //# when a snap file is created this is set to the file path
            this.snapFile = "";
            //# when not all points are snapped this is set True so snapping can be rerun
            this.snapErrors = false;
            //# vector layer for drawing inlet/outlet points
            this.drawOutletLayer = null;
            //# depends on DEM height and width and also on choice of area units
            this.areaOfCell = 0.0;
            //# Width of DEM as number of cells
            this.demWidth = 0;
            //# Height of DEM cell as number of cells
            this.demHeight = 0;
            //# flag to prevent infinite recursion between number of cells and area
            this.changing = false;
            //# basins selected for reservoirs
            this.extraReservoirBasins = new HashSet<int>();
            //# flag to show basic delineation is done, so removing subbasins, 
            //# adding reservoirs and point sources may be done
            this.isDelineated = isDelineated;
            //# flag to show delineation completed successfully or not
            this.delineationFinishedOK = true;
            //# flag to show if threshold or outlet file changed since loading form; 
            //# if not can assume any existing watershed is OK
            this.thresholdChanged = false;
            //# flag to show finishDelineation has been run
            this.finishHasRun = false;
            //# mapTool used for drawing outlets etc
            this.mapTool = null;
            //# path of draw outlets shapefile
            this.drawFile = null;
            //# x-offsets for TauDEM D8 flow directions, which run 1-8, so we use dir - 1 as index
            this.dX = new List<int> {
                1,
                1,
                0,
                -1,
                -1,
                -1,
                0,
                1
            };
            //# y-offsets for TauDEM D8 flow directions, which run 1-8, so we use dir - 1 as index
            this.dY = new List<int> {
                0,
                -1,
                -1,
                -1,
                0,
                1,
                1,
                1
            };
        }

        private void selectDemButton_Click(object sender, EventArgs e) {
            this.btnSetDEM();
        }

        private void burnButton_Click(object sender, EventArgs e) {
            this.btnSetBurn();
        }

        private void checkBurn_CheckedChanged(object sender, EventArgs e) {
            if (this.checkBurn.Checked) {
                this.selectBurn.Enabled = true;
                this.burnButton.Enabled = true;
                if (this.selectBurn.Text != "")
                    _gv.burnFile = this.selectBurn.Text;
            } else {
                this.selectBurn.Enabled = false;
                this.burnButton.Enabled = false;
                this._gv.burnFile = "";
            }
        }

        // Set connections to controls; read project delineation data.
        public void init() {
            //var settings = QSettings();
            //try {
            //    this.numProcesses.setValue(Convert.ToInt32(settings.value("/QSWAT/NumProcesses")));
            //}
            //catch (Exception) {
            //    this.numProcesses.setValue(8);
            //}
            this.numProcesses.Value = 8;
            //this.selectDemButton.clicked.connect(this.btnSetDEM);
            //this.checkBurn.stateChanged.connect(this.changeBurn);
            //this.useGrid.stateChanged.connect(this.changeUseGrid);
            //this.burnButton.clicked.connect(this.btnSetBurn);
            //this.selectOutletsButton.clicked.connect(this.btnSetOutlets);
            //this.selectWshedButton.clicked.connect(this.btnSetWatershed);
            //this.selectNetButton.clicked.connect(this.btnSetStreams);
            //this.selectExistOutletsButton.clicked.connect(this.btnSetOutlets);
            //this.delinRunButton1.clicked.connect(this.runTauDEM1);
            //this.delinRunButton2.clicked.connect(this.runTauDEM2);
            //this.tabWidget.currentChanged.connect(this.changeExisting);
            //this.existRunButton.clicked.connect(this.runExisting);
            //this.useOutlets.stateChanged.connect(this.changeUseOutlets);
            //this.drawOutletsButton.clicked.connect(this.drawOutlets);
            //this.selectOutletsInteractiveButton.clicked.connect(this.selectOutlets);
            //this.snapReviewButton.clicked.connect(this.snapReview);
            //this.selectSubButton.clicked.connect(this.selectMergeSubbasins);
            //this.mergeButton.clicked.connect(this.mergeSubbasins);
            //this.selectResButton.clicked.connect(this.selectReservoirs);
            //this.addButton.clicked.connect(this.addReservoirs);
            //this.taudemHelpButton.clicked.connect(TauDEMUtils.taudemHelp);
            //this.OKButton.clicked.connect(this.finishDelineation);
            //this.cancelButton.clicked.connect(this.doClose);
            //this.numCells.setValidator(QIntValidator());
            //this.numCells.textChanged.connect(this.setArea);
            //this.area.textChanged.connect(this.setNumCells);
            //this.area.setValidator(QDoubleValidator());
            this.areaUnitsBox.Items.Add(Parameters._SQKM);
            this.areaUnitsBox.Items.Add(Parameters._HECTARES);
            this.areaUnitsBox.Items.Add(Parameters._SQMETRES);
            this.areaUnitsBox.Items.Add(Parameters._SQMILES);
            this.areaUnitsBox.Items.Add(Parameters._ACRES);
            this.areaUnitsBox.Items.Add(Parameters._SQFEET);
            //this.areaUnitsBox.activated.connect(this.changeAreaOfCell);
            this.horizontalCombo.Items.Add(Parameters._METRES);
            this.horizontalCombo.Items.Add(Parameters._FEET);
            this.horizontalCombo.Items.Add(Parameters._DEGREES);
            this.horizontalCombo.Items.Add(Parameters._UNKNOWN);
            this.verticalCombo.Items.Add(Parameters._METRES);
            this.verticalCombo.Items.Add(Parameters._FEET);
            this.verticalCombo.Items.Add(Parameters._CM);
            this.verticalCombo.Items.Add(Parameters._MM);
            this.verticalCombo.Items.Add(Parameters._INCHES);
            this.verticalCombo.Items.Add(Parameters._YARDS);
            // set vertical unit default to metres
            this.verticalCombo.SelectedIndex = this.verticalCombo.Items.IndexOf(Parameters._METRES);
            //this.verticalCombo.activated.connect(this.setVerticalUnits);
            //this.snapThreshold.setValidator(QIntValidator());
            this.readProj();
            // initally disable numCells, area and areaUnitsBox (enabled only after loading DEM, when cell-area conversion possible)
            this.numCells.Enabled = false;
            this.area.Enabled = false;
            this.areaUnitsBox.Enabled = false;
            // set area units initially to hectares
            this.areaUnitsBox.SelectedItem = Parameters._HECTARES;
            // burn not enabled until use burn checked
            this.selectBurn.Enabled = false;
            this.burnButton.Enabled = false;
            this.setMergeResGroups();
            this.checkMPI();
            // allow for cancellation without being considered an error
            this.delineationFinishedOK = true;
            // Prevent annoying "error 4 .shp not recognised" messages.
            // These should become exceptions but instead just disappear.
            // Safer in any case to raise exceptions if something goes wrong.
            //gdal.UseExceptions();
            //ogr.UseExceptions();
        }

        // Allow merging of subbasins and 
        //         adding of reservoirs and point sources if delineation complete.
        //         
        public void setMergeResGroups() {
            this.mergeGroup.Enabled = this.isDelineated;
            this.addResGroup.Enabled = this.isDelineated;
        }

        // Do delineation; check done and save topology data.  Return OK if delineation done and no errors, 2 if not delineated and nothing done, else 0.
        public void run() {
            this.init();
            //if (this._gv.useGridModel) {
            //    this.useGrid.Checked = true;
            //    this.GridBox.Checked = true;
            //} else {
            //    this.useGrid.Visible = false);
            //    this.GridBox.setVisible(false);
            //    this.GridSize.setVisible(false);
            //    this.GridSizeLabel.setVisible(false);
            //}
            //this.Show();
            //this.Activate();
            //this._gv.delineatePos = this.pos();
        }

        // 
        //         Try to make sure there is just one msmpi.dll, either on the path or in the TauDEM directory.
        //         
        //         TauDEM executables are built on the assumption that MPI is available.
        //         But they can run without MPI if msmpi.dll is placed in their directory.
        //         MPI will fail if there is an msmpi.dll on the path and one in the TauDEM directory 
        //         (unless they happen to be the same version).
        //         QSWAT supplies msmpi_dll in the TauDEM directory that can be renamed to provide msmpi.dll 
        //         if necessary.
        //         This function is called every time delineation is started so that if the user installs MPI
        //         or uninstalls it the appropriate steps are taken.
        //         
        public void checkMPI() {
            var dll = "msmpi.dll";
            var dummy = "msmpi_dll";
            var dllPath = Utils.join(this._gv.TauDEMDir, dll);
            var dummyPath = Utils.join(this._gv.TauDEMDir, dummy);
            // tried various methods here.  
            //'where msmpi.dll' succeeds if it was there and is moved or renamed - cached perhaps?
            // isfile fails similarly
            //'where mpiexec' always fails because when run interactively the path does not include the MPI directory
            // so just check for existence of mpiexec.exe and assume user will not leave msmpi.dll around
            // if MPI is installed and then uninstalled
            if (File.Exists(this._gv.mpiexecPath)) {
                Utils.loginfo("mpiexec found");
                // MPI is on the path; rename the local dll if necessary
                if (File.Exists(dllPath)) {
                    if (File.Exists(dummyPath)) {
                        File.Delete(dllPath);
                        Utils.loginfo("dll removed");
                    } else {
                        File.Move(dllPath, dummyPath);
                        Utils.loginfo("dll renamed");
                    }
                }
            } else {
                Utils.loginfo("mpiexec not found");
                // we don't have MPI on the path; rename the local dummy if necessary
                if (File.Exists(dllPath)) {
                    return;
                } else if (File.Exists(dummyPath)) {
                    File.Move(dummyPath, dllPath);
                    Utils.loginfo("dummy renamed");
                } else {
                    Utils.error(String.Format("Cannot find executable mpiexec in the system or {0} in {1}: TauDEM functions will not run.  Install MPI or reinstall ArcSWAT.", dll, this._gv.TauDEMDir), this._gv.isBatch);
                }
            }
        }

        private void numCells_TextChanged(object sender, EventArgs e) {
            if (this.numCells.Enabled) { this.setArea(); }
        }


        // 
        //         Finish delineation.
        //         
        //         Checks stream reaches and watersheds defined, sets DEM attributes, 
        //         checks delineation is complete, calculates flow distances,
        //         runs topology setup.  Sets delineationFinishedOK to true if all completed successfully.
        //         
        public async void finishDelineation() {
            FeatureLayer extraOutletLayer;
            FeatureLayer outletLayer;
            FeatureLayer wshedLayer;
            this.delineationFinishedOK = false;
            this.finishHasRun = true;
            FeatureLayer streamLayer;
            if (!this._gv.existingWshed && this._gv.useGridModel) {
                streamLayer = (FeatureLayer)Utils.getLayerByLegend(FileTypes.legend(FileTypes._GRIDSTREAMS));
            } else {
                streamLayer = (FeatureLayer)(await Utils.getLayerByFilename(this._gv.streamFile, FileTypes._STREAMS, null, null, null)).Item1;
            }
            if (streamLayer is null) {
                if (this._gv.existingWshed) {
                    Utils.error("Stream reaches layer not found.", this._gv.isBatch);
                } else if (this._gv.useGridModel) {
                    Utils.error("Grid stream reaches layer not found.", this._gv.isBatch);
                } else {
                    Utils.error("Stream reaches layer not found: have you run TauDEM?", this._gv.isBatch);
                }
                return;
            }
            if (!this._gv.existingWshed && this._gv.useGridModel) {
                wshedLayer = (FeatureLayer)Utils.getLayerByLegend(Utils._GRIDLEGEND);
            } else {
                var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
                wshedLayer = (FeatureLayer)(await Utils.getLayerByFilename(this._gv.wshedFile, ft, null, null, null)).Item1;
                if (wshedLayer is null) {
                    if (this._gv.existingWshed) {
                        Utils.error("Watershed layer not found.", this._gv.isBatch);
                    } else {
                        Utils.error("Watershed layer not found: have you run TauDEM?", this._gv.isBatch);
                    }
                    return;
                }
            }
            //Debug.Assert(wshedLayer is not null);
            // this may be None
            if (string.IsNullOrEmpty(this._gv.outletFile)) {
                outletLayer = null;
            } else {
                outletLayer = (FeatureLayer)(await Utils.getLayerByFilename(this._gv.outletFile, FileTypes._OUTLETS, null, null, null)).Item1;
            }
            var demLayer = (RasterLayer)(await Utils.getLayerByFilename(this._gv.demFile, FileTypes._DEM, null, null, null)).Item1;
            if (demLayer is null) {
                Utils.error("DEM layer not found: have you removed it?", this._gv.isBatch);
                return;
            }
            if (!await this.setDimensions(demLayer)) {
                return;
            }
            if (!this._gv.useGridModel && string.IsNullOrEmpty(this._gv.basinFile)) {
                // must have merged some subbasins: recreate the watershed grid
                demLayer = (RasterLayer)(await Utils.getLayerByFilename(this._gv.demFile, FileTypes._DEM, null, null, null)).Item1;
                if (demLayer is null) {
                    Utils.error(String.Format("Cannot find DEM layer for file {0}", this._gv.demFile), this._gv.isBatch);
                    return;
                }
                this._gv.basinFile = await this.createBasinFile(this._gv.wshedFile, demLayer);
                if (string.IsNullOrEmpty(this._gv.basinFile)) {
                    return;
                    // Utils.loginfo('Recreated watershed grid as {0}', self._gv.basinFile))
                }
            }
            this.saveProj();
            if (this.checkDEMProcessed()) {
                if (!string.IsNullOrEmpty(this._gv.extraOutletFile)) {
                    extraOutletLayer = (FeatureLayer)(await Utils.getLayerByFilename(this._gv.extraOutletFile, FileTypes._OUTLETS, null, null, null)).Item1;
                } else {
                    extraOutletLayer = null;
                }
                if (!this._gv.existingWshed && !this._gv.useGridModel) {
                    this.progress("Tributary channel lengths ...");
                    int threshold = await this._gv.topo.makeStreamOutletThresholds(this._gv);
                    if (threshold > 0) {
                        var demBase = Path.ChangeExtension(this._gv.demFile, null);
                        this._gv.distFile = demBase + "dist.tif";
                        // threshold is already double maximum ad8 value, so values anywhere near it can only occur at subbasin outlets; 
                        // use fraction of it to avoid any rounding problems
                        var ok = await TauDEMUtils.runDistanceToStreams(this._gv.pFile, this._gv.hd8File, this._gv.distFile, Convert.ToInt32(threshold * 0.9).ToString(), Convert.ToInt32(this.numProcesses.Value), this.taudemOutput, mustRun: this.thresholdChanged);
                        if (!ok) {
                            this.cleanUp(3);
                            return;
                        }
                    } else {
                        // Probably using existing watershed but switched tabs in delineation form
                        this._gv.existingWshed = true;
                    }
                }
                var recalculate = this._gv.existingWshed && this.recalcButton.Checked;
                this.progress("Constructing topology ...");
                this._gv.isBig = this._gv.useGridModel && wshedLayer.GetFeatureClass().GetCount() > 100000 || this._gv.forTNC;
                Utils.loginfo(String.Format("isBig is {0}", this._gv.isBig));
                if (await this._gv.topo.setUp(demLayer, this._gv.streamFile, this._gv.wshedFile, this._gv.outletFile, this._gv.extraOutletFile, this._gv, this._gv.existingWshed, recalculate, this._gv.useGridModel, true)) {
                    if (this._gv.topo.inletLinks.Count == 0) {
                        // no inlets, so no need to expand subbasins layer legend
                        //Debug.Assert(wshedLayer is not null);
                        await QueuedTask.Run(() => wshedLayer.SetExpanded(false));
                    }
                    this.progress("Writing Reach table ...");
                    streamLayer = await this._gv.topo.writeReachTable(streamLayer, this._gv);
                    if (streamLayer is null) {
                        return;
                    }
                    this.progress("Writing MonitoringPoint table ...");
                    this._gv.topo.writeMonitoringPointTable(demLayer, streamLayer);
                    this.delineationFinishedOK = true;
                    this.doClose(true);
                    this._parent.postDelineation(true);
                    return;
                } else {
                    return;
                }
            }
            return;
        }

        // 
        //         Return true if using grid model or basinFile is newer than wshedFile if using existing watershed,
        //         or wshed file is newer than slopeFile file if using grid model,
        //         or  wshedFile is newer than DEM.
        //         
        public virtual bool checkDEMProcessed() {
            if (this._gv.existingWshed) {
                return this._gv.useGridModel || Utils.isUpToDate(this._gv.wshedFile, this._gv.basinFile);
            }
            if (this._gv.useGridModel) {
                return Utils.isUpToDate(this._gv.slopeFile, this._gv.wshedFile);
            } else {
                return Utils.isUpToDate(this._gv.demFile, this._gv.wshedFile);
            }
        }

        // Open and load DEM; set default threshold.
        public async void btnSetDEM() {
            var pair = await Utils.openAndLoadFile(FileTypes._DEM, this.selectDem, this._gv.sourceDir, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            string demFile = pair.Item1;
            RasterLayer demMapLayer = pair.Item2 as RasterLayer;
            if (demFile is not null && demMapLayer is not null) {
                await QueuedTask.Run(() => {
                    MapView.Active.ZoomTo(demMapLayer);
                    // set extent to DEM as otherwise defaults to full globe
                    var demExtent = demMapLayer.QueryExtent();
                    var map = MapView.Active.Map;
                    map.SetCustomFullExtent(demExtent);
                });
                this._gv.demFile = demFile;
                await this.setDefaultNumCells(demMapLayer);
                // warn if large DEM
                var numCells = this.demWidth * this.demHeight;
                if (numCells > 4000000.0) {
                    var millions = Convert.ToInt32(numCells / 1000000.0);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(String.Format("Large DEM", "This DEM has over {0} million cells and could take some time to process.  Be patient!", millions), Utils._ARCSWATNAME);
                }
                // hillshade done with relief colorizer for DEM
                //// hillshade waste of (a lot of) time for TNC DEMs
                //if (!this._gv.forTNC) {
                //    this.addHillshade(demFile, demMapLayer, this._gv);
            }
        }

        // hillshade in ArcGIS done with relief colorizer for DEM
        ////  Create hillshade layer and load.
        //public async void addHillshade(string demFile, RasterLayer demMapLayer, object gv) {
        //    var hillshadeFile = Path.ChangeExtension(demFile) + "/hillshade.tif";
        //    if (!Utils.isUpToDate(demFile, hillshadeFile)) {
        //        // run gdaldem to generate hillshade.tif
        //        await Utils.removeLayerAndFiles(hillshadeFile);
        //        var command = String.Format("gdaldem.exe hillshade -compute_edges -z 5 \"{0}\" \"{1}\"", demFile, hillshadeFile);
        //        var proc = subprocess.run(command, shell: true, stdout: subprocess.PIPE, stderr: subprocess.STDOUT, universal_newlines: true);
        //        Utils.loginfo("Creating hillshade ...");
        //        Utils.loginfo(command);
        //        Debug.Assert(proc is not null);
        //        Utils.loginfo(proc.stdout);
        //        if (!File.Exists(hillshadeFile)) {
        //            Utils.information("Failed to create hillshade file {0}", hillshadeFile), gv.isBatch);
        //            return;
        //        }
        //        Utils.copyPrj(demFile, hillshadeFile);
        //    }
        //    // make dem active layer and add hillshade above it
        //    // demLayer allowed to be None for batch running
        //    if (demMapLayer) {
        //        var demLayer = root.findLayer(demMapLayer.id());
        //        var hillMapLayer = Utils.getLayerByFilename(root.findLayers(), hillshadeFile, FileTypes._HILLSHADE, gv, demLayer, Utils._WATERSHED_GROUP_NAME)[0];
        //        if (!hillMapLayer) {
        //            Utils.information("Failed to load hillshade file {0}", hillshadeFile), gv.isBatch);
        //            return;
        //        }
        //        Debug.Assert(hillMapLayer is QgsRasterLayer);
        //        // compress legend entry
        //        var hillTreeLayer = root.findLayer(hillMapLayer.id());
        //        Debug.Assert(hillTreeLayer is not null);
        //        hillTreeLayer.setExpanded(false);
        //        hillMapLayer.renderer().setOpacity(0.4);
        //        hillMapLayer.triggerRepaint();
        //    }
        //}

        // Open and load stream network to burn in.
        public async void btnSetBurn() {
            var pair = await Utils.openAndLoadFile(FileTypes._BURN, this.selectBurn, this._gv.sourceDir, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            string burnFile = pair.Item1;
            FeatureLayer burnLayer = pair.Item2 as FeatureLayer;
            if (burnFile is not null && burnLayer is not null) {
                if (burnLayer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolyline) {
                    Utils.error(string.Format("Burn in file {0} is not a line shapefile", burnFile), this._gv.isBatch);
                } else {
                    this._gv.burnFile = burnFile;
                }
            }
        }

        // Open and load inlets/outlets shapefile.
        public async void btnSetOutlets() {
            TextBox box;
            if (this._gv.existingWshed) {
                //Debug.Assert(this.tabWidget.SelectedIndex == 1);
                box = this.selectExistOutlets;
            } else {
                //Debug.Assert(this.tabWidget.SelectedIndex == 0);
                box = this.selectOutlets;
                this.thresholdChanged = true;
            }
            var ft = this._gv.isHUC || this._gv.isHAWQS ? FileTypes._OUTLETSHUC : FileTypes._OUTLETS;
            var pair = await Utils.openAndLoadFile(ft, box, this._gv.shapesDir, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            string outletFile = pair.Item1;
            FeatureLayer outletLayer = pair.Item2 as FeatureLayer;
            if (outletFile is not null && outletLayer is not null) {
                if ((outletLayer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryMultipoint) &&
                    (outletLayer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPoint)) {
                    Utils.error(string.Format("Inlets/outlets file {0} is not a point shapefile", outletFile), this._gv.isBatch);
                } else {
                    this._gv.outletFile = outletFile;
                }
            }
        }

        // Open and load existing watershed shapefile.
        public async void btnSetWatershed() {
            var pair = await Utils.openAndLoadFile(FileTypes._EXISTINGWATERSHED, this.selectWshed, this._gv.sourceDir, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            string wshedFile = pair.Item1;
            FeatureLayer wshedLayer = pair.Item2 as FeatureLayer;
            if (wshedFile is not null && wshedLayer is not null) {
                if (wshedLayer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolygon) {
                    Utils.error(string.Format("Subbasins file {0} is not a polygon shapefile", this.selectWshed.Text), this._gv.isBatch);
                } else {
                    this._gv.wshedFile = wshedFile;
                }
            }
        }

        // Open and load existing stream reach shapefile.
        public async void btnSetStreams() {
            var pair = await Utils.openAndLoadFile(FileTypes._STREAMS, this.selectNet, this._gv.sourceDir, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            string streamFile = pair.Item1;
            FeatureLayer streamLayer = pair.Item2 as FeatureLayer;
            if (streamFile is not null && streamLayer is not null) {
                if (streamLayer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolyline) {
                    Utils.error(string.Format("Streams file {0} is not a line shapefile", this.selectNet.Text), this._gv.isBatch);
                } else {
                    this._gv.streamFile = streamFile;
                }
            }
        }

        // Run Taudem to create stream reach network.
        public void runTauDEM1() {
            this.runTauDEM(null, false);
        }

        // Run TauDEM to create watershed shapefile.
        public async void runTauDEM2() {
            // first remove any existing shapesDir inlets/outlets file as will
            // probably be inconsistent with new subbasins
            await Utils.removeLayerByLegend(Utils._EXTRALEGEND);
            this._gv.extraOutletFile = "";
            this.extraReservoirBasins.Clear();
            if (!this.useOutlets.Checked) {
                this.runTauDEM(null, true);
            } else {
                var outletFile = this.selectOutlets.Text;
                if (outletFile == "" || !File.Exists(outletFile)) {
                    Utils.error("Please select an inlets/outlets file", this._gv.isBatch);
                    return;
                }
                this.runTauDEM(outletFile, true);
            }
        }

        // Change between using existing and delineating watershed.
        public void changeExisting() {
            var tab = this.tabWidget.SelectedIndex;
            if (tab > 1) {
                // DEM properties or TauDEM output
                return;
            }
            this._gv.existingWshed = tab == 1;
        }

        // Run TauDEM.
        public async void runTauDEM(string outletFile, bool makeWshed) {
            Layer subLayer;
            string ad8File;
            string delineationDem;
            this.delineationFinishedOK = false;
            var demFile = this.selectDem.Text;
            if (demFile == "" || !File.Exists(demFile)) {
                Utils.error("Please select a DEM file", this._gv.isBatch);
                return;
            }
            this.isDelineated = false;
            this._gv.writeMasterProgress(0, 0);
            this.setMergeResGroups();
            this._gv.demFile = demFile;
            // find dem layer (or load it)
            RasterLayer demLayer;
            demLayer = (await Utils.getLayerByFilename(this._gv.demFile, FileTypes._DEM, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as RasterLayer;
            if (demLayer is null) {
                Utils.error(string.Format("Cannot load DEM {0}", this._gv.demFile), this._gv.isBatch);
                return;
            }
            // changing default number of cells 
            if (!await this.setDefaultNumCells(demLayer)) {
                return;
            }
            string @base = Path.ChangeExtension(this._gv.demFile, null);
            string suffix = Path.GetExtension(this._gv.demFile);
            // burn in if required
            if (this.checkBurn.Checked) {
                var burnFile = this.selectBurn.Text;
                if (burnFile == "") {
                    Utils.error("Please select a burn in stream network shapefile", this._gv.isBatch);
                    return;
                }
                if (!File.Exists(burnFile)) {
                    Utils.error(string.Format("Cannot find burn in file {0}", burnFile), this._gv.isBatch);
                    return;
                }
                var burnedDemFile = Path.ChangeExtension(this._gv.demFile, "_burned.tif");
                if (!Utils.isUpToDate(demFile, burnedDemFile) || !Utils.isUpToDate(burnFile, burnedDemFile)) {
                    // just in case
                    await Utils.removeLayerAndFiles(burnedDemFile);
                    this.progress("Burning streams ...");
                    //burnRasterFile = self.streamToRaster(demLayer, burnFile)
                    //processing.runalg('saga:burnstreamnetworkintodem', demFile, burnRasterFile, burnMethod, burnEpsilon, burnedFile)
                    var burnDepth = this._gv.fromGRASS ? 25.0 : 50.0;
                    Topology.burnStream(burnFile, demFile, burnedDemFile, this._gv.verticalFactor, burnDepth, Convert.ToInt32(this._gv.topo.dx), this._gv);
                    if (!File.Exists(burnedDemFile)) {
                        this.cleanUp(-1);
                        return;
                    }
                }
                if (this._gv.fromGRASS) {
                    // just running to create burned file
                    this.cleanUp(-1);
                    return;
                }
                this._gv.burnedDemFile = burnedDemFile;
                delineationDem = burnedDemFile;
            } else {
                this._gv.burnedDemFile = "";
                delineationDem = demFile;
            }
            if (this._gv.fromGRASS) {
                this._gv.pFile = @base + "p.tif";
                this._gv.basinFile = @base + "w.tif";
                this._gv.slopeFile = @base + "slp.tif";
                // slope file should be based on original DEM
                if (this._gv.slopeFile.EndsWith("_burnedslp.tif")) {
                    var unburnedslp = this._gv.slopeFile.Replace("_burnedslp.tif", "slp.tif");
                    if (File.Exists(unburnedslp)) {
                        this._gv.slopeFile = unburnedslp;
                    }
                }
                ad8File = @base + "ad8.tif";
                this._gv.outletFile = "";
                this._gv.streamFile = @base + "net.shp";
                this._gv.wshedFile = @base + "wshed.shp";
                //this.createGridShapefile(demLayer, this._gv.pFile, ad8File, this._gv.basinFile);
                //FeatureLayer streamLayer = (await Utils.getLayerByFilename(this._gv.streamFile, FileTypes._STREAMS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                if (!await this._gv.topo.setUp0(demLayer, this._gv.streamFile, this._gv.verticalFactor)) {
                    this.cleanUp(-1);
                    return;
                }
                this.isDelineated = true;
                this.setMergeResGroups();
                this.saveProj();
                this.cleanUp(-1);
                return;
            }
            int numProcesses = Convert.ToInt32(this.numProcesses.Value);
            var mpiexecPath = this._gv.mpiexecPath;
            if (numProcesses > 0 && (mpiexecPath == "" || !File.Exists(mpiexecPath))) {
                Utils.information(string.Format("Cannot find MPI program {0} so running TauDEM with just one process", mpiexecPath), this._gv.isBatch);
                numProcesses = 0;
                this.numProcesses.Value = 0;
            }
            if (this.showTaudem.Checked) {
                this.tabWidget.SelectedIndex = 3;
            }
            using (new CursorWait()) {
                this.taudemOutput.Clear();
                var felFile = @base + "fel" + suffix;
                await Utils.removeLayer(felFile);
                this.progress("PitFill ...");
                var ok = await TauDEMUtils.runPitFill(delineationDem, felFile, numProcesses, this.taudemOutput);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var sd8File = @base + "sd8" + suffix;
                var pFile = @base + "p" + suffix;
                await Utils.removeLayer(sd8File);
                await Utils.removeLayer(pFile);
                this.progress("D8FlowDir ...");
                ok = await TauDEMUtils.runD8FlowDir(felFile, sd8File, pFile, numProcesses, this.taudemOutput);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var slpFile = @base + "slp" + suffix;
                var angFile = @base + "ang" + suffix;
                await Utils.removeLayer(slpFile);
                await Utils.removeLayer(angFile);
                this.progress("DinfFlowDir ...");
                ok = await TauDEMUtils.runDinfFlowDir(felFile, slpFile, angFile, numProcesses, this.taudemOutput);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                ad8File = @base + "ad8" + suffix;
                await Utils.removeLayer(ad8File);
                this.progress("AreaD8 ...");
                ok = await TauDEMUtils.runAreaD8(pFile, ad8File, null, null, numProcesses, this.taudemOutput, mustRun: this.thresholdChanged);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var scaFile = @base + "sca" + suffix;
                await Utils.removeLayer(scaFile);
                this.progress("AreaDinf ...");
                ok = await TauDEMUtils.runAreaDinf(angFile, scaFile, null, numProcesses, this.taudemOutput, mustRun: this.thresholdChanged);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var gordFile = @base + "gord" + suffix;
                var plenFile = @base + "plen" + suffix;
                var tlenFile = @base + "tlen" + suffix;
                await Utils.removeLayer(gordFile);
                await Utils.removeLayer(plenFile);
                await Utils.removeLayer(tlenFile);
                this.progress("GridNet ...");
                ok = await TauDEMUtils.runGridNet(pFile, plenFile, tlenFile, gordFile, null, numProcesses, this.taudemOutput, mustRun: this.thresholdChanged);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var srcFile = @base + "src" + suffix;
                await Utils.removeLayer(srcFile);
                this.progress("Threshold ...");
                if (this._gv.isBatch) {
                    Utils.information(string.Format("Delineation threshold: {0} cells", this.numCells.Text), true);
                }
                ok = await TauDEMUtils.runThreshold(ad8File, srcFile, this.numCells.Text, numProcesses, this.taudemOutput, mustRun: this.thresholdChanged);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var ordFile = @base + "ord" + suffix;
                var streamFile = @base + "net.shp";
                // if stream shapefile already exists and is a directory, set path to .shp
                streamFile = Utils.dirToShapefile(streamFile);
                var treeFile = @base + "tree.dat";
                var coordFile = @base + "coord.dat";
                var wFile = @base + "w" + suffix;
                await Utils.removeLayer(ordFile);
                await Utils.removeLayer(streamFile);
                await Utils.removeLayer(wFile);
                this.progress("StreamNet ...");
                ok = await TauDEMUtils.runStreamNet(felFile, pFile, ad8File, srcFile, null, ordFile, treeFile, coordFile, streamFile, wFile, numProcesses, this.taudemOutput, mustRun: this.thresholdChanged);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                // if stream shapefile is a directory, set path to .shp, since not done earlier if streamFile did not exist then
                streamFile = Utils.dirToShapefile(streamFile);
                // load stream network
                Utils.copyPrj(demFile, wFile);
                Utils.copyPrj(demFile, streamFile);
                // better not to use define projection.  Just generates prj and can differ from original, making new file hard to edit.
                //var parms = Geoprocessing.MakeValueArray(streamFile, Path.ChangeExtension(demFile, ".prj"));
                //Utils.runPython("runDefineProjection.py", parms, this._gv);
                // make demLayer (or hillshade if exists) active so streamLayer loads above it and below outlets
                // (or use Full HRUs layer if there is one)
                var fullHRUsLayer = Utils.getLayerByLegend(Utils._FULLHRUSLEGEND);
                var hillshadeLayer = Utils.getLayerByLegend(Utils._HILLSHADELEGEND);
                if (fullHRUsLayer is not null) {
                    subLayer = fullHRUsLayer;
                } else if (hillshadeLayer is not null) {
                    subLayer = hillshadeLayer;
                } else {
                    subLayer = null;
                }
                var pair = await Utils.getLayerByFilename(streamFile, FileTypes._STREAMS, this._gv, subLayer, Utils._WATERSHED_GROUP_NAME);
                FeatureLayer streamLayer = pair.Item1 as FeatureLayer;
                bool loaded = pair.Item2;
                if (streamLayer is null || !loaded) {
                    Utils.error("Failed to create stream layer", this._gv.isBatch);
                    this.cleanUp(-1);
                    return;
                }
                this._gv.streamFile = streamFile;
                if (!makeWshed) {
                    this.snapFile = "";
                    this.snappedLabel.Text = "";
                    this.selectOutletsInteractiveLabel.Text = "";
                    // initial run to enable placing of outlets, so finishes with load of stream network
                    this.taudemOutput.AppendText("------------------- TauDEM finished -------------------\n");
                    this.saveProj();
                    this.cleanUp(-1);
                    return;
                }
                if (this.useOutlets.Checked) {
                    //Debug.Assert(outletFile is not null);
                    var outletBase = Path.ChangeExtension(outletFile, null);
                    var snapFile = outletBase + "_snap.shp";
                    pair = await Utils.getLayerByFilename(outletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME);
                    FeatureLayer outletLayer = pair.Item1 as FeatureLayer;
                    loaded = pair.Item2;
                    if (outletLayer is null) {
                        this.cleanUp(-1);
                        return;
                    }
                    this.progress("SnapOutletsToStreams ...");
                    ok = await this.createSnapOutletFile(outletLayer, streamLayer, outletFile, snapFile);
                    if (!ok) {
                        this.cleanUp(-1);
                        return;
                    }
                    // replaced by snapping
                    // outletMovedFile = outletBase + '_moved.shp'
                    // Utils.removeLayer(outletMovedFile, li)
                    // self.progress('MoveOutletsToStreams ...')
                    // ok = await TauDEMUtils.runMoveOutlets(pFile, srcFile, outletFile, outletMovedFile, numProcesses, self.taudemOutput, mustRun=self.thresholdChanged)
                    // if not ok:
                    //   self.cleanUp(3)
                    //    return
                    // repeat AreaD8, GridNet, Threshold and StreamNet with snapped outlets
                    var mustRun = this.thresholdChanged || !string.IsNullOrEmpty(this.snapFile);
                    await Utils.removeLayer(ad8File);
                    this.progress("AreaD8 ...");
                    ok = await TauDEMUtils.runAreaD8(pFile, ad8File, this.snapFile, null, numProcesses, this.taudemOutput, mustRun: mustRun);
                    if (!ok) {
                        this.cleanUp(3);
                        return;
                    }
                    await Utils.removeLayer(streamFile);
                    this.progress("GridNet ...");
                    ok = await TauDEMUtils.runGridNet(pFile, plenFile, tlenFile, gordFile, this.snapFile, numProcesses, this.taudemOutput, mustRun: mustRun);
                    if (!ok) {
                        this.cleanUp(3);
                        return;
                    }
                    await Utils.removeLayer(srcFile);
                    this.progress("Threshold ...");
                    ok = await TauDEMUtils.runThreshold(ad8File, srcFile, this.numCells.Text, numProcesses, this.taudemOutput, mustRun: mustRun);
                    if (!ok) {
                        this.cleanUp(3);
                        return;
                    }
                    this.progress("StreamNet ...");
                    ok = await TauDEMUtils.runStreamNet(felFile, pFile, ad8File, srcFile, this.snapFile, ordFile, treeFile, coordFile, streamFile, wFile, numProcesses, this.taudemOutput, mustRun: mustRun);
                    if (!ok) {
                        this.cleanUp(3);
                        return;
                    }
                    Utils.copyPrj(demFile, wFile);
                    Utils.copyPrj(demFile, streamFile);
                    //var parms = Geoprocessing.MakeValueArray(streamFile, Path.ChangeExtension(demFile, ".prj"));
                    //try {
                    //    Utils.runPython("runDefineProjection.py", parms, this._gv);
                    //} catch {; }
                    // make demLayer (or hillshadelayer if exists) active so streamLayer loads above it and below outlets
                    // (or use Full HRUs layer if there is one)
                    if (fullHRUsLayer is not null) {
                        subLayer = fullHRUsLayer;
                    } else if (hillshadeLayer is not null) {
                        subLayer = hillshadeLayer;
                    } else {
                        subLayer = null;
                    }
                    pair = await Utils.getLayerByFilename(streamFile, FileTypes._STREAMS, this._gv, subLayer, Utils._WATERSHED_GROUP_NAME);
                    streamLayer = pair.Item1 as FeatureLayer;
                    loaded = pair.Item2;
                    if (streamLayer is null || !loaded) {
                        this.cleanUp(-1);
                        return;
                    }
                    // check if stream network has only one feature
                    bool hasOneStream = await QueuedTask.Run<bool>(() => {
                        using (var fc = streamLayer.GetFeatureClass()) {
                            return (fc.GetCount() == 1);
                        }
                    });
                    if (hasOneStream) {
                        Utils.error("There is only one stream reach in your watershed, so you will only get one subbasin.  You need to reduce the threshold.", this._gv.isBatch);
                        this.cleanUp(-1);
                        return;
                    }
                }
                this.taudemOutput.AppendText("------------------- TauDEM finished -------------------\n");
                this._gv.pFile = pFile;
                this._gv.basinFile = wFile;
                if (this.checkBurn.Checked) {
                    // need to make slope file from original dem
                    var felNoburn = @base + "felnoburn" + suffix;
                    await Utils.removeLayer(felNoburn);
                    this.progress("PitFill ...");
                    ok = await TauDEMUtils.runPitFill(demFile, felNoburn, numProcesses, this.taudemOutput);
                    if (!ok) {
                        this.cleanUp(3);
                        return;
                    }
                    // use of slope.tif as name of slope file unaffected by burning in used by demProcessed check in main form
                    var slopeFile = @base + "slope" + suffix;
                    var angleFile = @base + "angle" + suffix;
                    await Utils.removeLayer(slopeFile);
                    await Utils.removeLayer(angleFile);
                    this.progress("DinfFlowDir ...");
                    ok = await TauDEMUtils.runDinfFlowDir(felNoburn, slopeFile, angleFile, numProcesses, this.taudemOutput);
                    if (!ok) {
                        this.cleanUp(3);
                        return;
                    }
                    this._gv.slopeFile = slopeFile;
                } else {
                    this._gv.slopeFile = slpFile;
                }
                this._gv.streamFile = streamFile;
                if (this.useOutlets.Checked) {
                    //Debug.Assert(outletFile is not null);
                    this._gv.outletFile = outletFile;
                } else {
                    this._gv.outletFile = "";
                }
                var wshedFile = @base + "wshed.shp";
                this.createWatershedShapefile(wFile, wshedFile);
                this._gv.wshedFile = wshedFile;
                //if (this.GridBox.Checked) {
                //    this.createGridShapefile(demLayer, pFile, ad8File, wFile);
                //}
                if (!await this._gv.topo.setUp0(demLayer, streamFile, this._gv.verticalFactor)) {
                    this.cleanUp(-1);
                    return;
                }
                this.isDelineated = true;
                this.setMergeResGroups();
                this.saveProj();
                this.cleanUp(-1);
            }
        }

        // Do delineation from existing stream network and subbasins.
        public async void runExisting() {
            this.delineationFinishedOK = false;
            var demFile = this.selectDem.Text;
            if (demFile == "" || !File.Exists(demFile)) {
                Utils.error("Please select a DEM file", this._gv.isBatch);
                return;
            }
            this._gv.demFile = demFile;
            var wshedFile = this.selectWshed.Text;
            if (wshedFile == "" || !File.Exists(wshedFile)) {
                Utils.error("Please select a watershed shapefile", this._gv.isBatch);
                return;
            }
            var streamFile = this.selectNet.Text;
            if (streamFile == "" || !File.Exists(streamFile)) {
                Utils.error("Please select a streams shapefile", this._gv.isBatch);
                return;
            }
            var outletFile = this.selectExistOutlets.Text;
            if (outletFile != "") {
                if (!File.Exists(outletFile)) {
                    Utils.error(string.Format("Cannot find inlets/outlets shapefile {0}", outletFile), this._gv.isBatch);
                    return;
                }
            }
            this.isDelineated = false;
            this.setMergeResGroups();
            // find layers (or load them)
            RasterLayer demLayer = (await Utils.getLayerByFilename(this._gv.demFile, FileTypes._DEM, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as RasterLayer;
            if (demLayer is null) {
                Utils.error(string.Format("Cannot load DEM {0}", this._gv.demFile), this._gv.isBatch);
                return;
            }
            //this.addHillshade(this._gv.demFile, demLayer, this._gv);
            FeatureLayer wshedLayer = (await Utils.getLayerByFilename(wshedFile, FileTypes._EXISTINGWATERSHED, this._gv, demLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            if (wshedLayer is null) {
                Utils.error(string.Format("Cannot load watershed shapefile {0}", wshedFile), this._gv.isBatch);
                return;
            }
            FeatureLayer streamLayer = (await Utils.getLayerByFilename(streamFile, FileTypes._STREAMS, this._gv, wshedLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            if (streamLayer is null) {
                Utils.error(string.Format("Cannot load streams shapefile {0}", streamFile), this._gv.isBatch);
                return;
            }
            FeatureLayer outletLayer;
            if (outletFile != "") {
                var ft = this._gv.isHUC || this._gv.isHAWQS ? FileTypes._OUTLETSHUC : FileTypes._OUTLETS;
                outletLayer = (await Utils.getLayerByFilename(outletFile, ft, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                if (outletLayer is null) {
                    Utils.error(string.Format("Cannot load inlets/outlets shapefile {0}", outletFile), this._gv.isBatch);
                    return;
                }
            } else {
                outletLayer = null;
            }
            // ready to start processing
            string @base = Path.ChangeExtension(this._gv.demFile, null);
            string suffix = Path.GetExtension(this._gv.demFile);
            var numProcesses = Convert.ToInt32(this.numProcesses.Value);
            using (new CursorWait()) {
                this.taudemOutput.Clear();
                // create Dinf slopes
                var felFile = @base + "fel" + suffix;
                var slpFile = @base + "slp" + suffix;
                var angFile = @base + "ang" + suffix;
                await Utils.removeLayer(slpFile);
                await Utils.removeLayer(angFile);
                var willRun = !(Utils.isUpToDate(demFile, slpFile) && Utils.isUpToDate(demFile, angFile));
                if (willRun) {
                    this.progress("DinfFlowDir ...");
                    if (this.showTaudem.Checked) {
                        this.tabWidget.SelectedIndex = 3;
                    }
                    var ok = await TauDEMUtils.runPitFill(demFile, felFile, numProcesses, this.taudemOutput);
                    if (!ok) {
                        Utils.error(string.Format("Cannot generate pitfilled file from dem {0}", demFile), this._gv.isBatch);
                        this.cleanUp(3);
                        return;
                    }
                    ok = await TauDEMUtils.runDinfFlowDir(felFile, slpFile, angFile, numProcesses, this.taudemOutput);
                    if (!ok) {
                        Utils.error(string.Format("Cannot generate slope file from pitfilled dem {0}", felFile), this._gv.isBatch);
                        this.cleanUp(3);
                        return;
                    }
                    this.progress("DinfFlowDir done");
                }
                if (this._gv.useGridModel) {
                    // set centroids and catchment outlets
                    int basinIndex = await this._gv.topo.getIndex(wshedLayer, Topology._POLYGONID);
                    int outletIndex = await this._gv.topo.getIndex(wshedLayer, Topology._OUTLET);
                    if (basinIndex < 0) {
                        return;
                    }
                    using (var rc = wshedLayer.Search()) {
                        while (rc.MoveNext()) {
                            var feature = rc.Current as Feature;
                            var basin = Convert.ToInt32(feature[basinIndex]);
                            var outlet = Convert.ToInt32(feature[outletIndex]);
                            var poly = feature.GetShape() as ArcGIS.Core.Geometry.Polygon;
                            MapPoint centroid = GeometryEngine.Instance.Centroid(poly);
                            this._gv.topo.basinCentroids[basin] = new Coordinate2D(centroid.X, centroid.Y);
                            this._gv.topo.catchmentOutlets[basin] = outlet;
                        }
                    }
                } else {
                    // generate watershed raster
                    var wFile = @base + "w" + suffix;
                    if (!(Utils.isUpToDate(demFile, wFile) && Utils.isUpToDate(wshedFile, wFile))) {
                        this.progress("Generating watershed raster ...");
                        wFile = await this.createBasinFile(wshedFile, demLayer);
                        if (wFile == "") {
                            return;
                        }
                    }
                    this._gv.basinFile = wFile;
                }
                this._gv.slopeFile = slpFile;
                this._gv.wshedFile = wshedFile;
                this._gv.streamFile = streamFile;
                this._gv.outletFile = outletFile;
                if (!await this._gv.topo.setUp0(demLayer, streamFile, this._gv.verticalFactor)) {
                    return;
                }
                this.isDelineated = true;
                this.setMergeResGroups();
                this.cleanUp(-1);
            }
        }

        // Set threshold number of cells to default of 1% of number in grid, 
        //         unless already set.
        //         
        public async Task<bool> setDefaultNumCells(RasterLayer demLayer) {
            if (!await this.setDimensions(demLayer)) {
                return false;
            }
            // set to default number of cells unless already set
            if (this.numCells.Text == "") {
                var numCells = this.demWidth * this.demHeight;
                var defaultNumCells = Convert.ToInt32(numCells * 0.01);
                this.numCells.Text = defaultNumCells.ToString();
            } else {
                // already have a setting: keep same area but change number of cells according to dem cell size
                this.setNumCells();
            }
            return true;
        }

        // 
        //         Set dimensions of DEM.
        //         
        //         Also sets DEM properties tab.
        //         
        //         
        public async Task<bool> setDimensions(RasterLayer demLayer) {
            string @string;
            double factor;
            int epsg;
            Unit unit;
            SpatialReference crs = null;
            await QueuedTask.Run(() => {
                crs = demLayer.GetSpatialReference();
            });
            if (crs is null) {
                Utils.information("DEM spatial reference not available", false);
                return false;
            } else {
                //Utils.information("DEM spatial reference stored", false);
                this.numCells.Enabled = true;
                this.area.Enabled = true;
                this.areaUnitsBox.Enabled = true;
            }
            // can fail if demLayer is None or not projected
            try {
                if (this._gv.topo.crsProject is null) {
                    this._gv.topo.crsProject = crs;
                }
                unit = crs.Unit;
            }
            catch (Exception ex) {
                Utils.loginfo(string.Format("Failure to read DEM unit: {0}", ex.Message));
                return false;
            }
            string wkt = crs.Wkt;
            Raster raster = null;
            int height = 0;
            int width = 0;
            await QueuedTask.Run(() => {
                raster = demLayer.GetRaster();
                height = raster.GetHeight();
                width = raster.GetWidth();
                this._gv.topo.demExtent = raster.GetExtent();
            });
            this._gv.xBlockSize = height > 1000 ? 1000 : height;
            this._gv.yBlockSize = width > 1000 ? 1000 : width;
            Utils.loginfo(string.Format("DEM horizontal and vertical block sizes are {0} and {1}", this._gv.xBlockSize, this._gv.yBlockSize));
            var demFile = await Utils.layerFilename(demLayer);
            var demPrj = Path.ChangeExtension(demFile, ".prj");
            string demPrjTxt = demPrj + ".txt";
            if (File.Exists(demPrj) && !File.Exists(demPrjTxt)) {
                using (var f = new StreamWriter(demPrjTxt, true)) {
                    f.Write(wkt);
                }
            }
            this.textBrowser.Text = wkt;
            try {
                epsg = crs.LatestWkid;
                Utils.loginfo(epsg.ToString());
                this.label.Text = string.Format("Spatial reference: EPSG:{0}", epsg);
                if (this._gv.isBatch && epsg > 0) {
                    var demDataFile = Utils.join(this._gv.projDir, "dem_data.xml");
                    if (!File.Exists(demDataFile)) {
                        using (var f = new StreamWriter(demDataFile, true)) {
                            f.WriteLine("<demdata>");
                            f.WriteLine(string.Format("<epsg>{0}</epsg>", epsg));
                            f.WriteLine(string.Format("<minx>{0}</minx>", this._gv.topo.demExtent.XMin));
                            f.WriteLine(string.Format("<maxx>{0}</maxx>", this._gv.topo.demExtent.XMax));
                            f.WriteLine(string.Format("<miny>{0}</miny>", this._gv.topo.demExtent.YMin));
                            f.WriteLine(string.Format("<maxy>{0}</maxy>", this._gv.topo.demExtent.YMax));
                            f.WriteLine("</demdata>");
                        }
                    }
                }
            }
            catch (Exception) {
                // fail gracefully
                epsg = -1;
            }
            int uCode = unit.FactoryCode;
            if (uCode == LinearUnit.Meters.FactoryCode) {
                factor = 1.0;
                this.horizontalCombo.SelectedIndex = this.horizontalCombo.Items.IndexOf(Parameters._METRES);
                this.horizontalCombo.Enabled = false;
            } else if (uCode == LinearUnit.Feet.FactoryCode) {
                factor = 0.3048;
                this.horizontalCombo.SelectedIndex = this.horizontalCombo.Items.IndexOf(Parameters._FEET);
                this.horizontalCombo.Enabled = false;
            } else {
                if (uCode == AngularUnit.Degrees.FactoryCode) {
                    @string = "degrees";
                    this.horizontalCombo.SelectedIndex = this.horizontalCombo.Items.IndexOf(Parameters._DEGREES);
                    this.horizontalCombo.Enabled = false;
                } else {
                    @string = "unknown";
                    this.horizontalCombo.SelectedIndex = this.horizontalCombo.Items.IndexOf(Parameters._DEGREES);
                    this.horizontalCombo.Enabled = true;
                }
                Utils.information("WARNING: DEM does not seem to be projected: its units are " + @string, this._gv.isBatch);
                return false;
            }
            this.demWidth = width;
            this.demHeight = height;
            var XYSizes = await QueuedTask.Run<Tuple<double, double>>(() => {
                return raster.GetMeanCellSize();
            });
            this._gv.topo.dx = XYSizes.Item1 * factor;
            this._gv.topo.dy = XYSizes.Item2 * factor;
            this.sizeEdit.Text = string.Format("{0:G4} x {1:G4}", this._gv.topo.dx, this._gv.topo.dy);
            this.sizeEdit.ReadOnly = true;
            var areaM2 = this._gv.topo.dx * this._gv.topo.dy;
            this.setAreaOfCell(areaM2);
            this.areaEdit.Text = string.Format("{0:G4}", areaM2);
            this.areaEdit.ReadOnly = true;
            var north = this._gv.topo.demExtent.YMax;
            var south = this._gv.topo.demExtent.YMin;
            var east = this._gv.topo.demExtent.XMax;
            var west = this._gv.topo.demExtent.XMin;
            var topLeft = this._gv.topo.pointToLatLong(new Coordinate2D(west, north));
            var bottomRight = this._gv.topo.pointToLatLong(new Coordinate2D(east, south));
            var northll = topLeft.Y;
            var southll = bottomRight.Y;
            var eastll = bottomRight.X;
            var westll = topLeft.X;
            this.northEdit.Text = DelinForm.degreeString(northll);
            this.southEdit.Text = DelinForm.degreeString(southll);
            this.eastEdit.Text = DelinForm.degreeString(eastll);
            this.westEdit.Text = DelinForm.degreeString(westll);
            return true;
        }

        // Generate string showing degrees as decimal and as degrees minutes seconds.
        public static string degreeString(double decDeg) {
            var deg = Convert.ToInt32(decDeg);
            var decMin = Math.Abs(decDeg - deg) * 60;
            var minn = Convert.ToInt32(decMin);
            var sec = Convert.ToInt32((decMin - minn) * 60);
            return string.Format("{0:F2}{1} ({2}{1} {3}\' {4}\")", decDeg, (char)0xB0, deg, minn, sec);
        }

        // Set area of 1 cell according to area units choice.
        public void setAreaOfCell(double areaSqM) {
            if ((string)this.areaUnitsBox.SelectedItem == Parameters._SQKM) {
                this.areaOfCell = areaSqM / 1000000.0;
            } else if ((string)this.areaUnitsBox.SelectedItem == Parameters._HECTARES) {
                this.areaOfCell = areaSqM / 10000.0;
            } else if ((string)this.areaUnitsBox.SelectedItem == Parameters._SQMETRES) {
                this.areaOfCell = areaSqM;
            } else if ((string)this.areaUnitsBox.SelectedItem == Parameters._SQMILES) {
                this.areaOfCell = areaSqM / 2589988.1;
            } else if ((string)this.areaUnitsBox.SelectedItem == Parameters._ACRES) {
                this.areaOfCell = areaSqM / 4046.8564;
            } else if ((string)this.areaUnitsBox.SelectedItem == Parameters._SQFEET) {
                this.areaOfCell = areaSqM * 10.76391;
            }
        }

        // Set area of cell and update area threshold display.
        public void changeAreaOfCell() {
            this.setAreaOfCell(this._gv.topo.dx * this._gv.topo.dy);
            this.setArea();
        }

        // Sets vertical units from combo box; sets corresponding factor to apply to elevations.
        public void setVerticalUnits() {
            this._gv.verticalUnits = this.verticalCombo.SelectedText;
            this._gv.setVerticalFactor();
        }

        // Update area threshold display.
        public void setArea() {
            if (this.changing) {
                return;
            }
            int cellCount;
            try {
                cellCount = Int32.Parse(this.numCells.Text);
            }
            catch (Exception) {
                // not currently parsable - ignore
                return;
            }
            var area = cellCount * this.areaOfCell;
            this.changing = true;
            this.area.Text = string.Format("{0:G4}", area);
            this.changing = false;
            this.thresholdChanged = true;
        }

        // Update number of cells threshold display.
        public void setNumCells() {
            if (this.changing) {
                return;
            }
            // prevent division by zero
            if (this.areaOfCell == 0) {
                return;
            }
            double area;
            try {
                area = Double.Parse(this.area.Text);
            }
            catch (Exception) {
                // not currently parsable - ignore
                return;
            }
            var numCells = Convert.ToInt32(area / this.areaOfCell);
            this.changing = true;
            this.numCells.Text = numCells.ToString();
            this.changing = false;
            this.thresholdChanged = true;
        }

        // Make burn option available or not according to check box state.
        public void changeBurn() {
            if (this.checkBurn.Checked) {
                this.selectBurn.Enabled = true;
                this.burnButton.Enabled = true;
                if (this.selectBurn.Text != "") {
                    this._gv.burnFile = this.selectBurn.Text;
                }
            } else {
                this.selectBurn.Enabled = false;
                this.burnButton.Enabled = false;
                this._gv.burnFile = "";
            }
        }

        //// Change use grid setting according to check box state.
        //public void changeUseGrid() {
        //    this._gv.useGridModel = this.useGrid.Checked;
        //}

        // Make outlets option available or not according to check box state.
        public void changeUseOutlets() {
            if (this.useOutlets.Checked) {
                this.drawOutletsButton.Enabled = true;
                this.selectOutletsInteractiveButton.Enabled = true;
                this.snapLabel.Enabled = true;
                this.snapThreshold.Enabled = true;
                this.snapReviewButton.Enabled = true;
                this.selectOutlets.Enabled = true;
                this.selectOutletsButton.Enabled = true;
                if (this.selectOutlets.Text != "") {
                    this._gv.outletFile = this.selectOutlets.Text;
                }
            } else {
                this.drawOutletsButton.Enabled = false;
                this.selectOutletsInteractiveButton.Enabled = false;
                this.snapLabel.Enabled = false;
                this.snapThreshold.Enabled = false;
                this.snapReviewButton.Enabled = false;
                this.selectOutlets.Enabled = false;
                this.selectOutletsButton.Enabled = false;
                this._gv.outletFile = "";
            }
            this.thresholdChanged = true;
        }

        // Allow user to create inlets/outlets in current shapefile 
        //         or a new one.
        //         
        public async void drawOutlets() {
            DialogResult result;
            //SelectPointViewModel vm = new SelectPointViewModel(null, false);
            //this.mapTool = new PointTool();
            var outletLayer = await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch);
            if (outletLayer is not null) {
                // we have a current outlet layer - give user a choice
                result = new ChoosePointsFile().ShowDialog();
                if (result == DialogResult.Cancel) {
                    return;
                }
                this.drawCurrent = result == DialogResult.OK;  // returns Yes for new file
            } else {
                this.drawCurrent = false;
            }
            // always make a drawoutlets file.
            // if adding points is cancelled we will just discard it.
            // if drawCurrent is true we will add its features to outletLayer
            // if drawCurrent is false we make it the outletlayer
            this.drawOutletFile = Utils.join(this._gv.shapesDir, "drawoutlets.shp");
            // our outlet file may already be called drawoutlets.shp
            if (Utils.samePath(this.drawOutletFile, this._gv.outletFile)) {
                this.drawOutletFile = Utils.join(this._gv.shapesDir, "drawoutlets1.shp");
            }
            if (Project.Current.HasEdits) {
                await Project.Current.DiscardEditsAsync();
            }
            if (!await this.createOutletFile(this.drawOutletFile, this._gv.demFile, false)) {
                return;
            }
            this.drawOutletLayer = (await Utils.getLayerByFilename(this.drawOutletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            if (this.drawOutletLayer is null) {
                Utils.error(string.Format("Unable to load shapefile {0}", this.drawOutletFile), this._gv.isBatch);
                return;
            }
            var outletForm = new OutletForm();
            outletForm.setup(this.drawOutletLayer, this);
            outletForm.Show();
        }

        // return function from OutletForm
        public async void addOutlets(bool ok) {
            if (ok) {
                if (this.drawCurrent) {
                    // add features to current outlet layer
                    await Utils.removeLayer(this.drawOutletFile);
                    // remove outlets layer to enable copying to it
                    await Utils.removeLayer(this._gv.outletFile);
                    //if (Project.Current.HasEdits) { await Project.Current.DiscardEditsAsync(); }
                    //var outletLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch)) as FeatureLayer;
                    //await QueuedTask.Run(() => {
                    //    var fc = outletLayer.GetFeatureClass();
                    //    var parms = Geoprocessing.MakeValueArray(this.drawOutletFile, fc);
                    //    Utils.runPython("runCopyFeatures.py", parms, this._gv);
                    //});
                    //var tempOutlets = Path.Combine(this._gv.shapesDir, "tempoutlets.shp");
                    //Utils.copyShapefile(this.drawOutletFile, "tempoutlets", this._gv.shapesDir);
                    //var parms = Geoprocessing.MakeValueArray(this.drawOutletFile, this._gv.outletFile);
                    //Utils.runPython("runCopyFeatures.py", parms, this._gv);
                    Utils.copyPointFeatures(this.drawOutletFile, this._gv.outletFile);
                    // restore outlets layer
                    var outletLayer = (await Utils.getLayerByFilename(this._gv.outletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                    await MapView.Active.RedrawAsync(true);
                } else {
                    // replaces current outlet layer
                    await Utils.removeLayer(this._gv.outletFile);
                    this._gv.outletFile = this.drawOutletFile;
                }
            } else {
                // discard drawoutlets layer and file
                await Utils.removeLayerAndFiles(this.drawOutletFile);
            }
        }

        // Allow user to select points in inlets/outlets layer.
        public async void doSelectOutlets() {
            this.selFromLayer = null;
            List<Layer> layers = (List<Layer>)MapView.Active.GetSelectedLayers().ToList();
            Layer layer = layers.Count == 1 ? layers[0] : null;
            if (layer is not null) {
                if (layer.Name.Contains("inlets/outlets")) {
                    //if layer.name().startswith(Utils._SELECTEDLEGEND):
                    //    Utils.error('You cannot select from a selected inlets/outlets layer', self._gv.isBatch)
                    //    return
                    this.selFromLayer = layer as FeatureLayer;
                }
            }
            if (this.selFromLayer is null) {
                this.selFromLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch)) as FeatureLayer;
                if (this.selFromLayer is null) {
                    Utils.error("Cannot find inlets/outlets layer.  Please choose the layer you want to select from in the layers panel.", this._gv.isBatch);
                    return;
                } else {
                    layers = new List<Layer>() { this.selFromLayer };
                    MapView.Active.SelectLayers(layers);
                }
            }
            // make only the inlets/outlets layer selectable (else clicking selects all features)
            await QueuedTask.Run(() => {
                foreach (Layer layer in MapView.Active.Map.GetLayersAsFlattenedList().Where(l => l is FeatureLayer).ToList()) {
                    ((FeatureLayer)layer).SetSelectable(false);
                }
                var cimFeatureDefinition = this.selFromLayer.GetDefinition() as ArcGIS.Core.CIM.CIMBasicFeatureLayer;
                // this does not work.  Following line just stops selection, and colour change is ignored
                //cimFeatureDefinition.UseSelectionSymbol = false;
                cimFeatureDefinition.SelectionColor = ColorFactory.Instance.CreateRGBColor(255, 255, 0);  // yellow
                this.selFromLayer.SetDefinition(cimFeatureDefinition);
                this.selFromLayer.SetSelectable(true);
            });
            var selectPoint = new SelectPoint(this);
            selectPoint.Show();
        }

        public async void selectPoints(bool saveSelected) {
            if (!saveSelected) {
                await MapView.Active.ClearSketchAsync();
                await QueuedTask.Run(() => {
                    this.selFromLayer.ClearSelection();
                });
                return;
            }
            IReadOnlyList<long> selected = null;
            await QueuedTask.Run(async () => {
                using (Selection selection = this.selFromLayer.GetSelection()) {
                    selected = selection.GetObjectIDs();
                    await MapView.Active.ClearSketchAsync();
                    this.selFromLayer.ClearSelection();
                    if (selected.Count() == 0) {
                        Utils.error("No points selected", this._gv.isBatch);
                        return;
                    }
                }
            });
            var filename = await Utils.layerFilename(this.selFromLayer);
            string selFile = filename;
            // make a copy of selected layer's file, unless already a selection, then remove non-selected features from it
            if (!filename.EndsWith("_sel.shp")) {
                var basename = Path.GetFileNameWithoutExtension(filename) + "_sel";
                var dir = Path.GetDirectoryName(filename);
                Utils.copyShapefile(filename, basename, dir);
                selFile = Path.Combine(dir, basename + ".shp");
            }
            // remove features not selected from selFile
            using (var selDs = Ogr.Open(selFile, 1)) {
                // get layer - should only be one
                OSGeo.OGR.Layer layer = selDs.GetLayerByIndex(0);
                var fidsToRemove = new List<long>();
                layer.ResetReading();
                OSGeo.OGR.Feature f;
                do {
                    f = layer.GetNextFeature();
                    if (f != null) {
                        var fid = f.GetFID();
                        if (!selected.Contains(fid)) {
                            fidsToRemove.Add(fid);
                        }
                    }
                } while (f != null);
                fidsToRemove.Sort();
                // done in reverse as FIDs may change as features are removed
                for (int i = fidsToRemove.Count - 1; i >= 0; i--) {
                    layer.DeleteFeature(fidsToRemove[i]);
                }
                var numDeleted = fidsToRemove.Count;
                if (numDeleted > 0) {
                    Utils.loginfo(string.Format("{0} points removed", numDeleted));
                }
            }
            this._gv.outletFile = selFile;
            // make old outlet layer invisible
            Utils.setLayerVisibility(selFromLayer, false);
            // remove any existing selected layer
            await Utils.removeLayerByLegend(Utils._SELECTEDLEGEND);
            // load new outletFile
            Layer selOutletLayer;
            bool loaded;
            (selOutletLayer, loaded) = await Utils.getLayerByFilename(this._gv.outletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            if (selOutletLayer is null || !loaded) {
                Utils.error(string.Format("Could not load selected inlets/outlets shapefile {0}", this._gv.outletFile), this._gv.isBatch);
                return;
            }
            this.selectOutlets.Text = this._gv.outletFile;
            this.thresholdChanged = true;
            this.selectOutletsInteractiveLabel.Text = string.Format("{0} selected", selected.Count);
            this.snapFile = "";
            this.snappedLabel.Text = "";
        }

        public bool setActiveLayer(Layer layer) {
            try {
                List<Layer> layersToSelect = new List<Layer>() { layer };
                MapView.Active.SelectLayers(layersToSelect);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        // Allow user to select subbasins to which reservoirs should be added.
        public async void selectReservoirs() {
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = await Utils.getLayerByFilenameOrLegend(this._gv.wshedFile, ft, "", this._gv.isBatch) as FeatureLayer;
            if (wshedLayer is null) {
                Utils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            if (!this.setActiveLayer(wshedLayer)) {
                Utils.error("Could not make watershed layer active", this._gv.isBatch);
                return;
            }
            // make only the watershed layer selectable (else clicking selects all features)
            await QueuedTask.Run(() => {
                foreach (Layer layer in MapView.Active.Map.GetLayersAsFlattenedList().Where(l => l is FeatureLayer).ToList()) {
                    ((FeatureLayer)layer).SetSelectable(false);
                }
                var cimFeatureDefinition = wshedLayer.GetDefinition() as ArcGIS.Core.CIM.CIMBasicFeatureLayer;
                // this does not work.  Following line just stops selection, and colour change is ignored
                //cimFeatureDefinition.UseSelectionSymbol = false;
                cimFeatureDefinition.SelectionColor = ColorFactory.Instance.CreateRGBColor(255, 255, 0);  // yellow
                wshedLayer.SetDefinition(cimFeatureDefinition);
                wshedLayer.SetSelectable(true);
            });
            // set selection to already intended reservoirs, in case called twice
            var basinIndex = await this._gv.topo.getIndex(wshedLayer, Topology._POLYGONID);
            await QueuedTask.Run(() => {
                using (RowCursor cursor = (wshedLayer).Search()) {
                    while (cursor.MoveNext()) {
                        Feature wshed = (Feature)cursor.Current;
                        int basin = Convert.ToInt32(wshed[basinIndex]);
                        if (this.extraReservoirBasins.Contains(basin)) {
                            var qf = new QueryFilter() {
                                WhereClause = String.Format("{0} = {1}", Topology._POLYGONID, basin)
                            };
                            ((FeatureLayer)wshedLayer).Select(qf, SelectionCombinationMethod.Add);
                        }
                    }
                }
            });
            // save normal (probably explore) mode
            this.currentTool = FrameworkApplication.CurrentTool;
            var selectResSubbasins = new SelectResSubbasin(this);
            selectResSubbasins.Show();
        }

        public async void selectResSubbasins(bool save) {
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = await Utils.getLayerByFilenameOrLegend(this._gv.wshedFile, ft, "", this._gv.isBatch) as FeatureLayer;
            if (!save) {
                await QueuedTask.Run(() => { wshedLayer.ClearSelection(); });
                FrameworkApplication.CurrentTool = this.currentTool;
                this.extraReservoirBasins = null;
                return;
            }
            var basinIndex = await this._gv.topo.getIndex(wshedLayer, Topology._POLYGONID);
            await QueuedTask.Run(() => {
                using (var wsheds = wshedLayer.GetSelection()) {
                    // make set of basins intended to have reservoirs
                    this.extraReservoirBasins = new HashSet<int>();
                    using (RowCursor rc = wsheds.Search()) {
                        while (rc.MoveNext()) {
                            var basin = ((Feature)rc.Current)[basinIndex];
                            this.extraReservoirBasins.Add(Convert.ToInt32(basin));
                        }
                    }
                }
                wshedLayer.ClearSelection();
            });
            // revert to explore mode
            FrameworkApplication.CurrentTool = this.currentTool;
        }

        // Create extra inlets/outlets shapefile 
        //         with added reservoirs and, if requested, point sources.
        //         
        public async void addReservoirs() {
            Coordinate2D point;
            int basin;
            double length;
            int nodeid;
            this.delineationFinishedOK = false;
            var reservoirIds = await this.getOutletIds(Topology._RES);
            var ptsourceIds = await this.getOutletIds(Topology._PTSOURCE);
            var extraOutletFile = Utils.join(this._gv.shapesDir, "extra.shp");
            if (!await this.createOutletFile(extraOutletFile, this._gv.demFile, true)) {
                return;
            }
            this._gv.writeMasterProgress(0, 0);
            var pid = 0;
            using (new CursorWait()) {
                using (OSGeo.OGR.DataSource extraDs = Ogr.Open(extraOutletFile, 1))
                using (OSGeo.OGR.DataSource streamDs = Ogr.Open(this._gv.streamFile, 0)) {
                    var eLayer = extraDs.GetLayerByIndex(0);
                    var extraDef = eLayer.GetLayerDefn();
                    var sLayer = streamDs.GetLayerByIndex(0);
                    sLayer.ResetReading();
                    OSGeo.OGR.Feature reach = null;
                    do {
                        reach = sLayer.GetNextFeature();
                        if (reach != null) {
                            try {
                                length = reach.GetFieldAsDouble(Topology._LENGTH);
                            } catch {
                                length = reach.GetGeometryRef().Length();
                            }
                            if (length == 0) {
                                continue;
                            }
                            try {
                                nodeid = reach.GetFieldAsInteger(Topology._DSNODEID);
                            } catch {
                                // no DSNODEID field, so no possible existing point source
                                nodeid = -1;
                            }
                            basin = reach.GetFieldAsInteger(Topology._WSNO);
                            if (this.checkAddPoints.Checked && !ptsourceIds.Contains(nodeid)) {
                                point = this._gv.topo.nearsources[basin];
                                OSGeo.OGR.Feature extraPt = new OSGeo.OGR.Feature(extraDef);
                                extraPt.SetField(Topology._ID, pid);
                                pid++;
                                extraPt.SetField(Topology._INLET, 1);
                                extraPt.SetField(Topology._RES, 0);
                                extraPt.SetField(Topology._PTSOURCE, 1);
                                extraPt.SetField(Topology._SUBBASIN, basin);
                                extraPt.SetGeometry(OSGeo.OGR.Geometry.CreateFromWkt(string.Format("POINT ({0} {1})", point.X, point.Y)));
                                eLayer.CreateFeature(extraPt);
                            }
                            if (this.extraReservoirBasins.Contains(basin) && !reservoirIds.Contains(nodeid)) {
                                point = this._gv.topo.nearoutlets[basin];
                                OSGeo.OGR.Feature extraPt = new OSGeo.OGR.Feature(extraDef);
                                extraPt.SetField(Topology._ID, pid);
                                pid++;
                                extraPt.SetField(Topology._INLET, 0);
                                extraPt.SetField(Topology._RES, 1);
                                extraPt.SetField(Topology._PTSOURCE, 0);
                                extraPt.SetField(Topology._SUBBASIN, basin);
                                extraPt.SetGeometry(OSGeo.OGR.Geometry.CreateFromWkt(string.Format("POINT ({0} {1})", point.X, point.Y)));
                                eLayer.CreateFeature(extraPt);
                            }
                        }
                    } while (reach != null);
                }
            }
            if (pid > 0) {
                Layer extraOutletLayer;
                bool loaded;
                (extraOutletLayer, loaded) = await Utils.getLayerByFilename(extraOutletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME);
                if (extraOutletLayer is null || !loaded) {
                    Utils.error(string.Format("Could not load extra outlets/inlets file {0}", extraOutletFile), this._gv.isBatch);
                    return;
                }
                this._gv.extraOutletFile = extraOutletFile;
                // prevent merging of subbasins as point sources and/or reservoirs have been added
                this.mergeGroup.Enabled = false;
            } else {
                // no extra reservoirs or point sources - clean up
                await Utils.removeLayerAndFiles(extraOutletFile);
                this._gv.extraOutletFile = "";
                // can now merge subbasins
                this.mergeGroup.Enabled = true;
            }
        }

        // Load snapped inlets/outlets points.
        public async void snapReview() {
            var outletLayer = await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch) as FeatureLayer;
            if (outletLayer is null) {
                Utils.error("Cannot find inlets/outlets layer", this._gv.isBatch);
                return;
            }
            if (this.snapFile == "" || this.snapErrors) {
                var streamLayer = await Utils.getLayerByFilenameOrLegend(this._gv.streamFile, FileTypes._STREAMS, "", this._gv.isBatch) as FeatureLayer;
                if (streamLayer is null) {
                    Utils.error("Cannot find stream reaches layer", this._gv.isBatch);
                    return;
                }
                var outletBase = Path.ChangeExtension(this._gv.outletFile, null);
                var snapFile = outletBase + "_snap.shp";
                if (!await this.createSnapOutletFile(outletLayer, streamLayer, this._gv.outletFile, snapFile)) {
                    return;
                }
            }
            // make old outlet layer invisible
            Utils.setLayerVisibility(outletLayer, false);
            // load snapped layer
            var outletSnapLayer = (await Utils.getLayerByFilename(this.snapFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1;
            if (outletSnapLayer is null) {
                // don't worry about loaded flag as may already have the layer loaded
                Utils.error(string.Format("Could not load snapped inlets/outlets shapefile {0}", this.snapFile), this._gv.isBatch);
            }
        }

        // Allow user to select subbasins to be merged.
        public async void selectMergeSubbasins() {
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = await Utils.getLayerByFilenameOrLegend(this._gv.wshedFile, ft, "", this._gv.isBatch) as FeatureLayer;
            if (wshedLayer is null) {
                Utils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            // make only watershed layer selectable
            await QueuedTask.Run(() => {
                foreach (Layer layer in MapView.Active.Map.GetLayersAsFlattenedList().Where(l => l is FeatureLayer).ToList()) {
                    ((FeatureLayer)layer).SetSelectable(false);
                }
                var cimFeatureDefinition = wshedLayer.GetDefinition() as ArcGIS.Core.CIM.CIMBasicFeatureLayer;
                // this does not work.  Following line just stops selection, and colour change is ignored
                //cimFeatureDefinition.UseSelectionSymbol = false;
                cimFeatureDefinition.SelectionColor = ColorFactory.Instance.CreateRGBColor(255, 255, 0);  // yellow
                wshedLayer.SetDefinition(cimFeatureDefinition);
                wshedLayer.SetSelectable(true);
            });
            FrameworkApplication.CurrentTool = "ArcGIS.Desktop.Internal.Mapping.Ribbon.SelectTool";
            var selSubs = new SelectSubbasin(wshedLayer, this, this._gv);
            selSubs.Show();
        }

        // Merged selected subbasin with its parent.
        //public async void mergeSubbasins() {
        //    ReachData dataD;
        //    ReachData dataA;
        //    double dropD = 0.0;
        //    double dropA = 0.0;
        //    double lengthD = 0.0;
        //    double lengthA = 0.0;
        //    Feature pointFeature;
        //    this.delineationFinishedOK = false;
        //    var demLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.demFile, FileTypes._DEM, "", this._gv.isBatch)) as RasterLayer;
        //    if (demLayer is null) {
        //        Utils.error("Cannot find DEM layer", this._gv.isBatch);
        //        return;
        //    }
        //    var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
        //    var wshedLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.wshedFile, ft, "", this._gv.isBatch)) as FeatureLayer;
        //    if (wshedLayer is null) {
        //        Utils.error("Cannot find watershed layer", this._gv.isBatch);
        //        return;
        //    }
        //    Selection selection = null;
        //    await QueuedTask.Run(() => {
        //        selection = wshedLayer.GetSelection();
        //        if (selection.GetCount() == 0) {
        //            Utils.information("Please select at least one subbasin to be merged", this._gv.isBatch);
        //            return;
        //        }
        //        wshedLayer.ClearSelection();
        //    });
        //    await MapView.Active.ClearSketchAsync();
        //    var streamLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.streamFile, FileTypes._STREAMS, "", this._gv.isBatch)) as FeatureLayer;
        //    if (streamLayer is null) {
        //        Utils.error("Cannot find stream reaches layer", this._gv.isBatch);
        //        return;
        //    }
        //    var outletLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch)) as FeatureLayer;
        //    var polygonidField = await this._gv.topo.getIndex(wshedLayer, Topology._POLYGONID);
        //    if (polygonidField < 0) {
        //        return;
        //    }
        //    var areaField = await this._gv.topo.getIndex(wshedLayer, Topology._AREA, ignoreMissing: true);
        //    var streamlinkField = await this._gv.topo.getIndex(wshedLayer, Topology._STREAMLINK, ignoreMissing: true);
        //    var streamlenField = await this._gv.topo.getIndex(wshedLayer, Topology._STREAMLEN, ignoreMissing: true);
        //    var dsnodeidwField = await this._gv.topo.getIndex(wshedLayer, Topology._DSNODEIDW, ignoreMissing: true);
        //    var dswsidField = await this._gv.topo.getIndex(wshedLayer, Topology._DSWSID, ignoreMissing: true);
        //    var us1wsidField = await this._gv.topo.getIndex(wshedLayer, Topology._US1WSID, ignoreMissing: true);
        //    var us2wsidField = await this._gv.topo.getIndex(wshedLayer, Topology._US2WSID, ignoreMissing: true);
        //    var subbasinField = await this._gv.topo.getIndex(wshedLayer, Topology._SUBBASIN, ignoreMissing: true);
        //    var linknoField = await this._gv.topo.getIndex(streamLayer, Topology._LINKNO);
        //    if (linknoField < 0) {
        //        return;
        //    }
        //    var dslinknoField = await this._gv.topo.getIndex(streamLayer, Topology._DSLINKNO);
        //    if (dslinknoField < 0) {
        //        return;
        //    }
        //    var uslinkno1Field = await this._gv.topo.getIndex(streamLayer, Topology._USLINKNO1, ignoreMissing: true);
        //    var uslinkno2Field = await this._gv.topo.getIndex(streamLayer, Topology._USLINKNO2, ignoreMissing: true);
        //    var dsnodeidnField = await this._gv.topo.getIndex(streamLayer, Topology._DSNODEID, ignoreMissing: true);
        //    var orderField = await this._gv.topo.getIndex(streamLayer, Topology._ORDER, ignoreMissing: true);
        //    if (orderField < 0) {
        //        orderField = await this._gv.topo.getIndex(streamLayer, Topology._ORDER2, ignoreMissing: true);
        //    }
        //    var lengthField = await this._gv.topo.getIndex(streamLayer, Topology._LENGTH, ignoreMissing: true);
        //    var magnitudeField = await this._gv.topo.getIndex(streamLayer, Topology._MAGNITUDE, ignoreMissing: true);
        //    var ds_cont_arField = await this._gv.topo.getIndex(streamLayer, Topology._DS_CONT_AR, ignoreMissing: true);
        //    if (ds_cont_arField < 0) {
        //        ds_cont_arField = await this._gv.topo.getIndex(streamLayer, Topology._DS_CONT_AR2, ignoreMissing: true);
        //    }
        //    var dropField = await this._gv.topo.getIndex(streamLayer, Topology._DROP, ignoreMissing: true);
        //    if (dropField < 0) {
        //        dropField = await this._gv.topo.getIndex(streamLayer, Topology._DROP2, ignoreMissing: true);
        //    }
        //    var slopeField = await this._gv.topo.getIndex(streamLayer, Topology._SLOPE, ignoreMissing: true);
        //    var straight_lField = await this._gv.topo.getIndex(streamLayer, Topology._STRAIGHT_L, ignoreMissing: true);
        //    if (straight_lField < 0) {
        //        straight_lField = await this._gv.topo.getIndex(streamLayer, Topology._STRAIGHT_L2, ignoreMissing: true);
        //    }
        //    var us_cont_arField = await this._gv.topo.getIndex(streamLayer, Topology._US_CONT_AR, ignoreMissing: true);
        //    if (us_cont_arField < 0) {
        //        us_cont_arField = await this._gv.topo.getIndex(streamLayer, Topology._US_CONT_AR2, ignoreMissing: true);
        //    }
        //    var wsnoField = await this._gv.topo.getIndex(streamLayer, Topology._WSNO);
        //    if (wsnoField < 0) {
        //        return;
        //    }
        //    var dout_endField = await this._gv.topo.getIndex(streamLayer, Topology._DOUT_END, ignoreMissing: true);
        //    if (dout_endField < 0) {
        //        dout_endField = await this._gv.topo.getIndex(streamLayer, Topology._DOUT_END2, ignoreMissing: true);
        //    }
        //    var dout_startField = await this._gv.topo.getIndex(streamLayer, Topology._DOUT_START, ignoreMissing: true);
        //    if (dout_startField < 0) {
        //        dout_startField = await this._gv.topo.getIndex(streamLayer, Topology._DOUT_START2, ignoreMissing: true);
        //    }
        //    var dout_midField = await this._gv.topo.getIndex(streamLayer, Topology._DOUT_MID, ignoreMissing: true);
        //    if (dout_midField < 0) {
        //        dout_midField = await this._gv.topo.getIndex(streamLayer, Topology._DOUT_MID2, ignoreMissing: true);
        //    }
        //    int nodeidField = 0;
        //    int srcField = 0;
        //    int resField = 0;
        //    int inletField = 0;
        //    if (outletLayer is not null) {
        //        nodeidField = await this._gv.topo.getIndex(outletLayer, Topology._ID, ignoreMissing: true);
        //        srcField = await this._gv.topo.getIndex(outletLayer, Topology._PTSOURCE, ignoreMissing: true);
        //        resField = await this._gv.topo.getIndex(outletLayer, Topology._RES, ignoreMissing: true);
        //        inletField = await this._gv.topo.getIndex(outletLayer, Topology._INLET, ignoreMissing: true);
        //    }
        //    // ids of the features will change as we delete them, so use polygonids, which we know will be unique
        //    var pids = new List<int>();
        //    await QueuedTask.Run(() => {
        //        using (var rc = selection.Search()) {
        //            while (rc.MoveNext()) {
        //                var f = rc.Current as Feature;
        //                var pid = Convert.ToInt32(f[polygonidField]);
        //                pids.Add(pid);
        //            }
        //        }
        //    });
        //    // in the following
        //    // suffix A refers to the subbasin being merged
        //    // suffix UAs refers to the subbasin(s) upstream from A
        //    // suffix D refers to the subbasin downstream from A
        //    // suffix B refers to the othe subbasin(s) upstream from D
        //    // suffix M refers to the merged basin
        //    this._gv.writeMasterProgress(0, 0);
        //    foreach (var polygonidA in pids) {
        //        var wshedA = await Utils.getFeatureByValue(wshedLayer, Topology._POLYGONID, polygonidA);
        //        Debug.Assert(wshedA is not null);
        //        var reachA = await Utils.getFeatureByValue(streamLayer, Topology._WSNO, polygonidA);
        //        if (reachA is null) {
        //            Utils.error(string.Format("Cannot find reach with {0} value {1}", Topology._WSNO, polygonidA), this._gv.isBatch);
        //            continue;
        //        }
        //        Utils.loginfo(string.Format("A is reach {0} polygon {1}", reachA[linknoField], polygonidA));
        //        var AHasOutlet = false;
        //        var AHasInlet = false;
        //        var AHasReservoir = false;
        //        var AHasSrc = false;
        //        if (dsnodeidnField >= 0) {
        //            var dsnodeidA = Convert.ToInt32(reachA[dsnodeidnField]);
        //            if (dsnodeidA >= 0 && outletLayer is not null) {
        //                pointFeature = await Utils.getFeatureByValue(outletLayer, Topology._ID, dsnodeidA);
        //                if (pointFeature is not null) {
        //                    if (inletField >= 0 && Convert.ToInt32(pointFeature[inletField]) == 1) {
        //                        if (srcField >= 0 && Convert.ToInt32(pointFeature[srcField]) == 1) {
        //                            AHasSrc = true;
        //                        } else {
        //                            AHasInlet = true;
        //                        }
        //                    } else if (resField >= 0 && Convert.ToInt32(pointFeature[resField]) == 1) {
        //                        AHasReservoir = true;
        //                    } else {
        //                        AHasOutlet = true;
        //                    }
        //                }
        //            }
        //        }
        //        if (AHasOutlet || AHasInlet || AHasReservoir || AHasSrc) {
        //            Utils.information(string.Format("You cannot merge a subbasin which has an outlet, inlet, reservoir, or point source.  Not merging subbasin with {0} value {1}", Topology._POLYGONID, polygonidA), this._gv.isBatch);
        //            continue;
        //        }
        //        var linknoA = Convert.ToInt32(reachA[linknoField]);
        //        var reachUAs = new List<Feature>();
        //        var qf = new QueryFilter() {
        //            WhereClause = string.Format("{0} = {1}", Topology._DSLINKNO, linknoA)
        //        };
        //        await QueuedTask.Run(() => {
        //            using (var rc = streamLayer.Search(qf)) {
        //                while (rc.MoveNext()) {
        //                    reachUAs.Add(rc.Current as Feature);
        //                }
        //            }
        //        });
        //        // check whether a reach immediately upstream from A has an inlet
        //        var inletUpFromA = false;
        //        if (dsnodeidnField >= 0 && outletLayer is not null) {
        //            foreach (var reachUA in reachUAs) {
        //                var dsnodeidUA = Convert.ToInt32(reachUA[dsnodeidnField]);
        //                pointFeature = await Utils.getFeatureByValue(outletLayer, Topology._ID, dsnodeidUA);
        //                if (pointFeature is not null) {
        //                    if (inletField >= 0 && Convert.ToInt32(pointFeature[inletField]) == 1 && (srcField < 0 || Convert.ToInt32(pointFeature[srcField]) == 0)) {
        //                        inletUpFromA = true;
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        var linknoD = Convert.ToInt32(reachA[dslinknoField]);
        //        var reachD = await Utils.getFeatureByValue(streamLayer, Topology._LINKNO, linknoD);
        //        if (reachD is null) {
        //            Utils.information(string.Format("No downstream subbasin from subbasin with {0} value {1}: nothing to merge", Topology._POLYGONID, polygonidA), this._gv.isBatch);
        //            continue;
        //        }
        //        var polygonidD = Convert.ToInt32(reachD[wsnoField]);
        //        Utils.loginfo(string.Format("D is reach {0} polygon {1}", linknoD, polygonidD));
        //        // reachD may be zero length, with no corresponding subbasin, so search downstream if necessary to find wshedD
        //        // at the same time collect zero-length reaches for later disposal
        //        Feature wshedD = null;
        //        var nextReach = reachD;
        //        var zeroReaches = new List<Feature>();
        //        while (wshedD is null) {
        //            polygonidD = Convert.ToInt32(nextReach[wsnoField]);
        //            wshedD = await Utils.getFeatureByValue(wshedLayer, Topology._POLYGONID, polygonidD);
        //            if (wshedD is not null) {
        //                break;
        //            }
        //            // nextReach has no subbasin (it is a zero length link); step downstream and try again
        //            // first make a check
        //            if (lengthField >= 0 && Convert.ToInt32(nextReach[lengthField]) > 0) {
        //                Utils.error(string.Format("Internal error: stream reach wsno {0} has positive length but no subbasin.  Not merging subbasin with {1} value {2}", polygonidD, Topology._POLYGONID, polygonidA), this._gv.isBatch);
        //                continue;
        //            }
        //            zeroReaches.Add(nextReach);
        //            var nextLink = Convert.ToInt32(nextReach[dslinknoField]);
        //            if (nextLink < 0) {
        //                // reached main outlet
        //                break;
        //            }
        //            nextReach = await Utils.getFeatureByValue(streamLayer, Topology._LINKNO, nextLink);
        //        }
        //        if (wshedD is null) {
        //            Utils.information(string.Format("No downstream subbasin from subbasin with {0} value {1}: nothing to merge", Topology._POLYGONID, polygonidA), this._gv.isBatch);
        //            continue;
        //        }
        //        reachD = nextReach;
        //        linknoD = Convert.ToInt32(reachD[linknoField]);
        //        var zeroLinks = (from reach in zeroReaches
        //                            select Convert.ToInt32(reach[linknoField])).ToList();
        //        if (inletUpFromA) {
        //            var DLinks = new List<int>() { linknoD };
        //            DLinks.AddRange(zeroLinks);
        //            var reachBs = new List<Feature>();
        //            await QueuedTask.Run(() => {
        //                using (var rc = streamLayer.Search()) {
        //                    while (rc.MoveNext()) {
        //                        var reach = rc.Current as Feature;
        //                        if (DLinks.Contains(Convert.ToInt32(reach[dslinknoField])) && reach["FID"] != reachA["FID"]) {
        //                            reachBs.Add(reach);
        //                        }
        //                    }
        //                }
        //            });
        //            if (reachBs.Count > 0) {
        //                Utils.information(string.Format("Subbasin with {0} value {1} has an upstream inlet and the downstream one has another upstream subbasin: cannot merge.", Topology._POLYGONID, polygonidA), this._gv.isBatch);
        //                continue;
        //            }
        //        }
        //        // have reaches and watersheds A, UAs, D
        //        // we are ready to start editing the streamLayer
        //        if (!streamLayer.IsEditable) {
        //            streamLayer.SetEditable(true);
        //            if (!streamLayer.IsEditable) {
        //                Utils.error("Cannot edit stream reaches shapefile", this._gv.isBatch);
        //                return;
        //            }
        //        }
        //        if (true) { // try
        //            // create new merged stream M from D and A and add it to streams
        //            EditOperation mergeReaches = new EditOperation();
        //            var streamInspector = new Inspector();
        //            long oidA = 0;
        //            long oidD = 0;
        //            ArcGIS.Core.Geometry.Geometry geomA = null;
        //            ArcGIS.Core.Geometry.Geometry geomD = null;
        //            await QueuedTask.Run(() => {
        //                oidA = reachA.GetObjectID();
        //                oidD = reachD.GetObjectID();
        //                streamInspector.Load(streamLayer, oidD);
        //                geomA = reachA.GetShape();
        //                geomD = reachD.GetShape();
        //            });
        //            //streamInspector[linknoField] = linknoD;
        //            //streamInspector[dslinknoField] = Convert.ToInt32(reachD[dslinknoField];
        //            if (uslinkno1Field >= 0) {
        //                var Dup1 = Convert.ToInt32(reachD[uslinkno1Field]);
        //                if (Dup1 == linknoA || zeroLinks.Contains(Dup1)) {
        //                    // in general these cannot be relied on, since as we remove zero length links 
        //                    // there may be more than two upstream links from M
        //                    // At least don't leave it referring to a soon to be non-existent reach
        //                    Dup1 = Convert.ToInt32(reachA[uslinkno1Field]);
        //                }
        //                streamInspector[uslinkno1Field] = Dup1;
        //            }
        //            if (uslinkno2Field >= 0) {
        //                var Dup2 = Convert.ToInt32(reachD[uslinkno2Field]);
        //                if (Dup2 == linknoA || zeroLinks.Contains(Dup2)) {
        //                    // in general these cannot be relied on, since as we remove zero length links 
        //                    // there may be more than two upstream links from M
        //                    // At least don't leave it referring to a soon to be non-existent reach
        //                    Dup2 = Convert.ToInt32(reachA[uslinkno2Field]);
        //                }
        //                streamInspector[uslinkno2Field] = Dup2;
        //            }
        //            if (dsnodeidnField >= 0) {
        //                streamInspector[uslinkno1Field] = Convert.ToInt32(reachD[dsnodeidnField]);
        //            }
        //            //if (orderField >= 0) {
        //            //    streamInspector[orderField] = reachD[orderField];
        //            //}
        //            if (lengthField >= 0 || slopeField >= 0 || straight_lField >= 0 || dout_endField >= 0 && dout_midField >= 0) {
        //                // we will need these lengths
        //                lengthA = geomA.Length;
        //                lengthD = geomD.Length;
        //            }
        //            if (lengthField >= 0) {
        //                streamInspector[lengthField] = lengthA + lengthD;  // geomM.Length;
        //            }
        //            //if (magnitudeField >= 0) {
        //            //    streamInspector[magnitudeField] = Convert.ToInt32(reachD[magnitudeField];
        //            //}
        //            //if (ds_cont_arField >= 0) {
        //            //    streamInspector[ds_cont_arField] = (double)reachD[ds_cont_arField];
        //            //}
        //            if (dropField >= 0) {
        //                dropA = (double)reachA[dropField];
        //                dropD = (double)reachD[dropField];
        //                streamInspector[dropField] = (double)(dropA + dropD);
        //            } else if (slopeField >= 0) {
        //                dataA = await this._gv.topo.getReachData(reachA, demLayer);
        //                dropA = dataA.upperZ = dataA.lowerZ;
        //                dataD = await this._gv.topo.getReachData(reachD, demLayer);
        //                dropD = dataD.upperZ = dataD.lowerZ;
        //            }
        //            if (slopeField >= 0) {
        //                streamInspector[slopeField] = lengthA + lengthD == 0 ? 0 : (dropA + dropD) / (lengthA + lengthD);
        //            }
        //            if (straight_lField >= 0) {
        //                dataA = await this._gv.topo.getReachData(reachA, demLayer);
        //                dataD = await this._gv.topo.getReachData(reachD, demLayer);
        //                var dx = dataA.upperX - dataD.lowerX;
        //                var dy = dataA.upperY - dataD.lowerY;
        //                streamInspector[straight_lField] = Math.Sqrt(dx * dx + dy * dy);
        //            }
        //            if (us_cont_arField >= 0) {
        //                streamInspector[us_cont_arField] = (double)reachA[us_cont_arField];
        //            }
        //            streamInspector[wsnoField] = polygonidD;
        //            if (dout_endField >= 0) {
        //                streamInspector[dout_endField] = reachD[dout_endField];
        //            }
        //            if (dout_startField >= 0) {
        //                streamInspector[dout_startField] = reachA[dout_startField];
        //            }
        //            if (dout_endField >= 0 && dout_midField >= 0) {
        //                streamInspector[dout_midField] = (double)reachD[dout_endField] + (lengthA + lengthD) / 2.0;
        //            }
        //            var oids = new List<long>() { oidA, oidD };
        //            //// executing mergeReaches may change oids, so delete D and A first
        //            //var deleteFeatures = new EditOperation();
        //            //deleteFeatures.Delete(streamLayer, oids);
        //            //await QueuedTask.Run(() => {
        //            //    if (!deleteFeatures.Execute()) {
        //            //        Utils.error("Failed to delete streams to be merged", this._gv.isBatch);
        //            //    }
        //            //});
        //            bool result = false;
        //            mergeReaches.Merge(streamLayer, oids, streamInspector);
        //            if (!mergeReaches.IsEmpty) {
        //                await QueuedTask.Run(() => {
        //                    result = mergeReaches.Execute();
        //                });
        //            }
        //            if (!result) {
        //                Utils.error("Failed to merge streams", this._gv.isBatch);
        //            }
        //            // now FIDs have changed through deletion of A and D, but LINKNOs are now unique
        //            var oidsToRemove = new List<long>();
        //            var linksToRemove = (from reach in zeroReaches
        //                                    select Convert.ToInt32(reach[linknoField])).ToList();
        //            foreach (var linkToRemove in linksToRemove) {
        //                qf.WhereClause = string.Format("{0} = {1}", Topology._LINKNO, linkToRemove);
        //                await QueuedTask.Run(() => {
        //                    using (var rc = streamLayer.Search(qf)) {
        //                        while (rc.MoveNext()) {
        //                            oidsToRemove.Add(rc.Current.GetObjectID());
        //                        }
        //                    }
        //                });
        //            }
        //            if (oidsToRemove.Count > 0) {
        //                var deleteFeatures = new EditOperation();
        //                deleteFeatures.Delete(streamLayer, oidsToRemove);
        //                await QueuedTask.Run(() => {
        //                    if (!deleteFeatures.Execute()) {
        //                        Utils.error("Failed to delete zero length streams", this._gv.isBatch);
        //                    }
        //                });
        //            }
        //            var modOp = new EditOperation();
        //            var inspector = new Inspector();
        //            // change dslinks in UAs to D (= M)
        //            foreach (var reach in reachUAs) {
        //                qf.WhereClause = string.Format("{0} = {1}", Topology._LINKNO, reach[linknoField]);
        //                await QueuedTask.Run(() => {
        //                    using (var rc = streamLayer.Search(qf)) {
        //                        while (rc.MoveNext()) {
        //                            var oid = rc.Current.GetObjectID();
        //                            inspector.Load(streamLayer, oid);
        //                            inspector[dslinknoField] = linknoD;
        //                            modOp.Modify(inspector);
        //                            if (!modOp.IsEmpty) {
        //                                if (!modOp.Execute()) {
        //                                    Utils.error("Failed to modify DSLINKNO", this._gv.isBatch);
        //                                }
        //                            }
        //                        }
        //                    }
        //                });
        //            }
        //            // change any dslinks to zeroLinks to D as the zeroReaches have been deleted
        //            if (zeroLinks.Count > 0) {
        //                await QueuedTask.Run(() => {
        //                    using (var rc = streamLayer.Search()) {
        //                        while (rc.MoveNext()) {
        //                            if (zeroLinks.Contains(Convert.ToInt32(rc.Current[dslinknoField]))) {
        //                                var oid = rc.Current.GetObjectID();
        //                                inspector.Load(streamLayer, oid);
        //                                inspector[dslinknoField] = linknoD;
        //                                modOp.Modify(inspector);
        //                                if (!modOp.IsEmpty) {
        //                                    if (!modOp.Execute()) {
        //                                        Utils.error("Failed to modify DSLINKNO", this._gv.isBatch);
        //                                    }
        //                                }
        //                            }
        //                        }

        //                    }
        //                });
        //            }
        //        }
        //        //catch (Exception ex) {
        //        //    Utils.error(string.Format("Exception while updating stream reach shapefile: {0}", ex.Message), this._gv.isBatch);
        //        //    return;
        //        //}
        //        // New watershed shapefile will be inconsistent with watershed grid, so remove grid to be recreated later.
        //        // Do not do it immediately because the user may remove several subbasins, so we wait until the 
        //        // delineation form is closed.
        //        // clear name as flag that it needs to be recreated
        //        this._gv.basinFile = "";
        //        if (!wshedLayer.IsEditable) {
        //            wshedLayer.SetEditable(true);
        //            if (!wshedLayer.IsEditable) {
        //                Utils.error("Cannot edit watershed shapefile", this._gv.isBatch);
        //                return;
        //            }
        //        }
        //        try {
        //            // create new merged subbasin M from D and A and add it to wshed
        //            // prepare reachM
        //            EditOperation mergeOp = new EditOperation();
        //            var wshedInspector = new Inspector();
        //            long oidA = 0;
        //            long oidD = 0;
        //            await QueuedTask.Run(() => {
        //                oidA = wshedA.GetObjectID();
        //                oidD = wshedD.GetObjectID();
        //                wshedInspector.Load(wshedLayer, oidD);
        //            });
        //            //wshedInspector[polygonidField] = polygonidD;
        //            if (areaField >= 0) {
        //                var areaA = (double)wshedA[areaField];
        //                var areaD = (double)wshedD[areaField];
        //                wshedInspector[areaField] = areaA + areaD;
        //            }
        //            //if (streamlinkField >= 0) {
        //            //    wshedInspector[streamlinkField] = (double)wshedD[streamlinkField];
        //            //}
        //            if (streamlenField >= 0) {
        //                var lenA = (double)wshedA[streamlenField];
        //                var lenD = (double)wshedD[streamlenField];
        //                wshedInspector[streamlenField] = lenA + lenD;
        //            }
        //            //if (dsnodeidwField >= 0) {
        //            //    wshedInspector[dsnodeidwField] = wshedD[dsnodeidwField];
        //            //}
        //            //if (dswsidField >= 0) {
        //            //    wshedInspector[dswsidField] = wshedD[dswsidField];
        //            //}
        //            if (us1wsidField >= 0) {
        //                if (Convert.ToInt32(wshedD[us1wsidField]) == polygonidA) {
        //                    wshedInspector[us1wsidField] = wshedA[us1wsidField];
        //                }
        //            }
        //            if (us2wsidField >= 0) {
        //                if (Convert.ToInt32(wshedD[us2wsidField]) == polygonidA) {
        //                    wshedInspector[us2wsidField] = wshedA[us2wsidField];
        //                }
        //            }
        //            if (subbasinField >= 0) {
        //                wshedInspector[subbasinField] = wshedD[subbasinField];
        //            }
        //            var oids = new List<long>() { oidA, oidD };
        //            //// executing mergeOp may change oids, so delete D and A first
        //            //var deleteFeatures = new EditOperation();
        //            //deleteFeatures.Delete(wshedLayer, oids);
        //            //await QueuedTask.Run(() => {
        //            //    if (!deleteFeatures.Execute()) {
        //            //        Utils.error("Failed to delete subbasins to be merged", this._gv.isBatch);
        //            //    }
        //            //});
        //            bool result = false;
        //            mergeOp.Merge(wshedLayer, oids, wshedInspector);
        //            if (!mergeOp.IsEmpty) {
        //                await QueuedTask.Run(() => {
        //                    result = mergeOp.Execute();
        //                });
        //            }
        //            if (!result) {
        //                Utils.error("Failed to merge subbasins", this._gv.isBatch);
        //            }
        //            if (dswsidField >= 0) {
        //                var dsOp = new EditOperation();
        //                var dsInspector = new Inspector();
        //                // change downlinks upstream of A from A to D (= M)
        //                qf.WhereClause = string.Format("{0} = {1}", Topology._DSWSID, polygonidA);
        //                await QueuedTask.Run(() => {
        //                    using (var rc = wshedLayer.Search(qf)) {
        //                        while (rc.MoveNext()) {
        //                            var oid = rc.Current.GetObjectID();
        //                            dsInspector.Load(wshedLayer, oid);
        //                            dsInspector[dswsidField] = polygonidD;
        //                            dsOp.Modify(dsInspector);
        //                            if (!dsOp.IsEmpty) {
        //                                if (!dsOp.Execute()) {
        //                                    Utils.error("Failed to modify DSWSID field", this._gv.isBatch);
        //                                }
        //                            }
        //                        }
        //                    }
        //                });
        //            }
        //        }
        //        catch (Exception ex) {
        //            Utils.error(string.Format("Exception while updating watershed shapefile: {0}", ex.Message), this._gv.isBatch);
        //            return;
        //        }
        //        if (Project.Current.HasEdits) {
        //            Utils.information("Project has edits", this._gv.isBatch);
        //        }
        //        // check if stream layer or wshed layer have outstanding edits
        //        var streamName = Path.GetFileName(this._gv.streamFile);
        //        var streamDir = Path.GetDirectoryName(this._gv.streamFile);
        //        // Create a FileSystemConnectionPath using the folder path
        //        FileSystemConnectionPath connectionPath = new FileSystemConnectionPath(new Uri(streamDir), FileSystemDatastoreType.Shapefile);
        //        // Create a new FileSystemDatastore using the FileSystemConnectionPath.
        //        await QueuedTask.Run(() => {
        //            FileSystemDatastore dataStore = new FileSystemDatastore(connectionPath);
        //            if (dataStore.HasEdits()) {
        //                Utils.information("Datastore " + streamDir + " has edits", this._gv.isBatch);
        //            }
        //        });
        //    }
        //}

        // OGR version Merged selected subbasin with its parent.
        public async void mergeSubbasinsOgr() {
            ReachData dataD;
            ReachData dataA;
            double dropD = 0.0;
            double dropA = 0.0;
            double lengthD = 0.0;
            double lengthA = 0.0;
            Feature pointFeature;
            this.delineationFinishedOK = false;
            var demLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.demFile, FileTypes._DEM, "", this._gv.isBatch)) as RasterLayer;
            if (demLayer is null) {
                Utils.error("Cannot find DEM layer", this._gv.isBatch);
                return;
            }
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.wshedFile, ft, "", this._gv.isBatch)) as FeatureLayer;
            if (wshedLayer is null) {
                Utils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            Selection selection = null;
            await QueuedTask.Run(() => {
                selection = wshedLayer.GetSelection();
                if (selection.GetCount() == 0) {
                    Utils.information("Please select at least one subbasin to be merged", this._gv.isBatch);
                    return;
                }
                wshedLayer.ClearSelection();
            });
            await MapView.Active.ClearSketchAsync();
            // remove the layers we will edit
            await Utils.removeLayerByLegend(FileTypes.legend(ft));
            await Utils.removeLayerByLegend(FileTypes.legend(FileTypes._STREAMS));
            using (var wshedDs = Ogr.Open(this._gv.wshedFile, 1))
            using (var streamDs = Ogr.Open(this._gv.streamFile, 1)) {
                var wLayer = wshedDs.GetLayerByIndex(0);
                var streamLayer = streamDs.GetLayerByIndex(0);

                var outletLayer = (await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch)) as FeatureLayer;
                var polygonidField = wLayer.FindFieldIndex(Topology._POLYGONID, 1);
                if (polygonidField < 0) {
                    return;
                }
                var areaField = wLayer.FindFieldIndex(Topology._AREA, 1);
                var streamlinkField = wLayer.FindFieldIndex(Topology._STREAMLINK, 1);
                var streamlenField = wLayer.FindFieldIndex(Topology._STREAMLEN, 1);
                var dsnodeidwField = wLayer.FindFieldIndex(Topology._DSNODEIDW, 1);
                var dswsidField = wLayer.FindFieldIndex(Topology._DSWSID, 1);
                var us1wsidField = wLayer.FindFieldIndex(Topology._US1WSID, 1);
                var us2wsidField = wLayer.FindFieldIndex(Topology._US2WSID, 1);
                var subbasinField = wLayer.FindFieldIndex(Topology._SUBBASIN, 1);
                var linknoField = streamLayer.FindFieldIndex(Topology._LINKNO, 1);
                if (linknoField < 0) {
                    return;
                }
                var dslinknoField = streamLayer.FindFieldIndex(Topology._DSLINKNO, 1);
                if (dslinknoField < 0) {
                    return;
                }
                var uslinkno1Field = streamLayer.FindFieldIndex(Topology._USLINKNO1, 1);
                var uslinkno2Field = streamLayer.FindFieldIndex(Topology._USLINKNO2, 1);
                var dsnodeidnField = streamLayer.FindFieldIndex(Topology._DSNODEID, 1);
                var orderField = streamLayer.FindFieldIndex(Topology._ORDER, 1);
                if (orderField < 0) {
                    orderField = streamLayer.FindFieldIndex(Topology._ORDER2, 1);
                }
                var lengthField = streamLayer.FindFieldIndex(Topology._LENGTH, 1);
                var magnitudeField = streamLayer.FindFieldIndex(Topology._MAGNITUDE, 1);
                var ds_cont_arField = streamLayer.FindFieldIndex(Topology._DS_CONT_AR, 1);
                if (ds_cont_arField < 0) {
                    ds_cont_arField = streamLayer.FindFieldIndex(Topology._DS_CONT_AR2, 1);
                }
                var dropField = streamLayer.FindFieldIndex(Topology._DROP, 1);
                if (dropField < 0) {
                    dropField = streamLayer.FindFieldIndex(Topology._DROP2, 1);
                }
                var slopeField = streamLayer.FindFieldIndex(Topology._SLOPE, 1);
                var straight_lField = streamLayer.FindFieldIndex(Topology._STRAIGHT_L, 1);
                if (straight_lField < 0) {
                    straight_lField = streamLayer.FindFieldIndex(Topology._STRAIGHT_L2, 1);
                }
                var us_cont_arField = streamLayer.FindFieldIndex(Topology._US_CONT_AR, 1);
                if (us_cont_arField < 0) {
                    us_cont_arField = streamLayer.FindFieldIndex(Topology._US_CONT_AR2, 1);
                }
                var wsnoField = streamLayer.FindFieldIndex(Topology._WSNO, 1);
                if (wsnoField < 0) {
                    return;
                }
                var dout_endField = streamLayer.FindFieldIndex(Topology._DOUT_END, 1);
                if (dout_endField < 0) {
                    dout_endField = streamLayer.FindFieldIndex(Topology._DOUT_END2, 1);
                }
                var dout_startField = streamLayer.FindFieldIndex(Topology._DOUT_START, 1);
                if (dout_startField < 0) {
                    dout_startField = streamLayer.FindFieldIndex(Topology._DOUT_START2, 1);
                }
                var dout_midField = streamLayer.FindFieldIndex(Topology._DOUT_MID, 1);
                if (dout_midField < 0) {
                    dout_midField = streamLayer.FindFieldIndex(Topology._DOUT_MID2, 1);
                }
                int nodeidField = 0;
                int srcField = 0;
                int resField = 0;
                int inletField = 0;
                if (outletLayer is not null) {
                    nodeidField = await this._gv.topo.getIndex(outletLayer, Topology._ID, ignoreMissing: true);
                    srcField = await this._gv.topo.getIndex(outletLayer, Topology._PTSOURCE, ignoreMissing: true);
                    resField = await this._gv.topo.getIndex(outletLayer, Topology._RES, ignoreMissing: true);
                    inletField = await this._gv.topo.getIndex(outletLayer, Topology._INLET, ignoreMissing: true);
                }
                // fids of the features will change as we delete them, so use polygonids, which we know will be unique
                var pids = new List<int>();
                await QueuedTask.Run(() => {
                    foreach (var fid in selection.GetObjectIDs()) {
                        pids.Add(wLayer.GetFeature(fid).GetFieldAsInteger(polygonidField));
                    }
                });
                // in the following
                // suffix A refers to the subbasin being merged
                // suffix UAs refers to the subbasin(s) upstream from A
                // suffix D refers to the subbasin downstream from A
                // suffix B refers to the othe subbasin(s) upstream from D
                // suffix M refers to the merged basin
                this._gv.writeMasterProgress(0, 0);
                foreach (var polygonidA in pids) {
                    var wshedA = Utils.getOgrFeatureByValue(wLayer, Topology._POLYGONID, polygonidA);
                    Debug.Assert(wshedA is not null);
                    var reachA = Utils.getOgrFeatureByValue(streamLayer, Topology._WSNO, polygonidA);
                    if (reachA is null) {
                        Utils.error(string.Format("Cannot find reach with {0} value {1}", Topology._WSNO, polygonidA), this._gv.isBatch);
                        continue;
                    }
                    Utils.loginfo(string.Format("A is reach {0} polygon {1}", reachA.GetFieldAsInteger(linknoField), polygonidA));
                    var AHasOutlet = false;
                    var AHasInlet = false;
                    var AHasReservoir = false;
                    var AHasSrc = false;
                    if (dsnodeidnField >= 0) {
                        var dsnodeidA = reachA.GetFieldAsInteger(dsnodeidnField);
                        if (dsnodeidA >= 0 && outletLayer is not null) {
                            pointFeature = await Utils.getFeatureByValue(outletLayer, Topology._ID, dsnodeidA);
                            if (pointFeature is not null) {
                                if (inletField >= 0 && Convert.ToInt32(pointFeature[inletField]) == 1) {
                                    if (srcField >= 0 && Convert.ToInt32(pointFeature[srcField]) == 1) {
                                        AHasSrc = true;
                                    } else {
                                        AHasInlet = true;
                                    }
                                } else if (resField >= 0 && Convert.ToInt32(pointFeature[resField]) == 1) {
                                    AHasReservoir = true;
                                } else {
                                    AHasOutlet = true;
                                }
                            }
                        }
                    }
                    if (AHasOutlet || AHasInlet || AHasReservoir || AHasSrc) {
                        Utils.information(string.Format("You cannot merge a subbasin which has an outlet, inlet, reservoir, or point source.  Not merging subbasin with {0} value {1}", Topology._POLYGONID, polygonidA), this._gv.isBatch);
                        continue;
                    }
                    var linknoA = reachA.GetFieldAsInteger(linknoField);
                    streamLayer.ResetReading();
                    var reachUAs = new List<OSGeo.OGR.Feature>();
                    OSGeo.OGR.Feature reachUA = null;
                    do {
                        reachUA = streamLayer.GetNextFeature();
                        if (reachUA != null && reachUA.GetFieldAsInteger(Topology._DSLINKNO) == linknoA) {
                            reachUAs.Add(reachUA);
                        }
                    } while (reachUA != null);
                    // check whether a reach immediately upstream from A has an inlet
                    var inletUpFromA = false;
                    if (dsnodeidnField >= 0 && outletLayer is not null) {
                        foreach (var rUA in reachUAs) {
                            var dsnodeidUA = rUA.GetFieldAsInteger(dsnodeidnField);
                            pointFeature = await Utils.getFeatureByValue(outletLayer, Topology._ID, dsnodeidUA);
                            if (pointFeature is not null) {
                                if (inletField >= 0 && Convert.ToInt32(pointFeature[inletField]) == 1 && (srcField < 0 || Convert.ToInt32(pointFeature[srcField]) == 0)) {
                                    inletUpFromA = true;
                                    break;
                                }
                            }
                        }
                    }
                    var linknoD = reachA.GetFieldAsInteger(dslinknoField);
                    var reachD = Utils.getOgrFeatureByValue(streamLayer, Topology._LINKNO, linknoD);
                    if (reachD is null) {
                        Utils.information(string.Format("No downstream subbasin from subbasin with {0} value {1}: nothing to merge", Topology._POLYGONID, polygonidA), this._gv.isBatch);
                        continue;
                    }
                    var polygonidD = reachD.GetFieldAsInteger(wsnoField);
                    Utils.loginfo(string.Format("D is reach {0} polygon {1}", linknoD, polygonidD));
                    // reachD may be zero length, with no corresponding subbasin, so search downstream if necessary to find wshedD
                    // at the same time collect zero-length reaches for later disposal
                    OSGeo.OGR.Feature wshedD = null;
                    var nextReach = reachD;
                    var zeroReaches = new List<OSGeo.OGR.Feature>();
                    while (wshedD is null) {
                        polygonidD = nextReach.GetFieldAsInteger(wsnoField);
                        wshedD = Utils.getOgrFeatureByValue(wLayer, Topology._POLYGONID, polygonidD);
                        if (wshedD is not null) {
                            break;
                        }
                        // nextReach has no subbasin (it is a zero length link); step downstream and try again
                        // first make a check
                        if (lengthField >= 0 && nextReach.GetFieldAsDouble(lengthField) > 0) {
                            Utils.error(string.Format("Internal error: stream reach wsno {0} has positive length but no subbasin.  Not merging subbasin with {1} value {2}", polygonidD, Topology._POLYGONID, polygonidA), this._gv.isBatch);
                            continue;
                        }
                        zeroReaches.Add(nextReach);
                        var nextLink = nextReach.GetFieldAsInteger(dslinknoField);
                        if (nextLink < 0) {
                            // reached main outlet
                            break;
                        }
                        nextReach = Utils.getOgrFeatureByValue(streamLayer, Topology._LINKNO, nextLink);
                    }
                    if (wshedD is null) {
                        Utils.information(string.Format("No downstream subbasin from subbasin with {0} value {1}: nothing to merge", Topology._POLYGONID, polygonidA), this._gv.isBatch);
                        continue;
                    }
                    reachD = nextReach;
                    linknoD = reachD.GetFieldAsInteger(linknoField);
                    var zeroLinks = (from reach in zeroReaches
                                     select reach.GetFieldAsInteger(linknoField)).ToList();
                    if (inletUpFromA) {
                        var DLinks = new List<int>() { linknoD };
                        DLinks.AddRange(zeroLinks);
                        var reachBs = new List<OSGeo.OGR.Feature>();
                        streamLayer.ResetReading();
                        OSGeo.OGR.Feature reach = null;
                        do {
                            reach = streamLayer.GetNextFeature();
                            if (reach != null && DLinks.Contains(reach.GetFieldAsInteger(dslinknoField)) && reach.GetFID() != reachA.GetFID()) {
                                reachBs.Add(reach);
                            }
                        } while (reach != null);
                        if (reachBs.Count > 0) {
                            Utils.information(string.Format("Subbasin with {0} value {1} has an upstream inlet and the downstream one has another upstream subbasin: cannot merge.", Topology._POLYGONID, polygonidA), this._gv.isBatch);
                            continue;
                        }
                    }
                    // have reaches and watersheds A, UAs, D
                    // we are ready to start editing the streamLayer

                    try {
                        // create new merged stream M from D and A and add it to streams
                        var fidA = reachA.GetFID();
                        var fidD = reachD.GetFID();
                        var geomA = reachA.GetGeometryRef();
                        var geomD = reachD.GetGeometryRef();
                        var def = streamLayer.GetLayerDefn();
                        var reachM = new OSGeo.OGR.Feature(def);
                        reachM.SetField(linknoField, linknoD);
                        reachM.SetField(dslinknoField, reachD.GetFieldAsInteger(dslinknoField));
                        if (uslinkno1Field >= 0) {
                            var Dup1 = reachD.GetFieldAsInteger(uslinkno1Field);
                            if (Dup1 == linknoA || zeroLinks.Contains(Dup1)) {
                                // in general these cannot be relied on, since as we remove zero length links 
                                // there may be more than two upstream links from M
                                // At least don't leave it referring to a soon to be non-existent reach
                                Dup1 = reachA.GetFieldAsInteger(uslinkno1Field);
                            }
                            reachM.SetField(uslinkno1Field, Dup1);
                        }
                        if (uslinkno2Field >= 0) {
                            var Dup2 = reachD.GetFieldAsInteger(uslinkno2Field);
                            if (Dup2 == linknoA || zeroLinks.Contains(Dup2)) {
                                // in general these cannot be relied on, since as we remove zero length links 
                                // there may be more than two upstream links from M
                                // At least don't leave it referring to a soon to be non-existent reach
                                Dup2 = reachA.GetFieldAsInteger(uslinkno2Field);
                            }
                            reachM.SetField(uslinkno2Field, Dup2);
                        }
                        if (dsnodeidnField >= 0) {
                            reachM.SetField(dsnodeidnField, reachD.GetFieldAsInteger(dsnodeidnField));
                        }
                        if (orderField >= 0) {
                            reachM.SetField(orderField, reachD.GetFieldAsInteger(orderField));
                        }
                        if (lengthField >= 0 || slopeField >= 0 || straight_lField >= 0 || dout_endField >= 0 && dout_midField >= 0) {
                            // we will need these lengths
                            lengthA = geomA.Length();
                            lengthD = geomD.Length();
                        }
                        if (lengthField >= 0) {
                            reachM.SetField(lengthField, lengthA + lengthD);  // geomM.Length;
                        }
                        if (magnitudeField >= 0) {
                            reachM.SetField(magnitudeField, reachD.GetFieldAsInteger(magnitudeField));
                        }
                        if (ds_cont_arField >= 0) {
                            reachM.SetField(ds_cont_arField, reachD.GetFieldAsDouble(ds_cont_arField));
                        }
                        if (dropField >= 0) {
                            dropA = reachA.GetFieldAsDouble(dropField);
                            dropD = reachD.GetFieldAsDouble(dropField);
                            reachM.SetField(dropField, dropA + dropD);
                        } else if (slopeField >= 0) {
                            dataA = await this._gv.topo.getOgrReachData(reachA, demLayer);
                            dropA = dataA.upperZ = dataA.lowerZ;
                            dataD = await this._gv.topo.getOgrReachData(reachD, demLayer);
                            dropD = dataD.upperZ = dataD.lowerZ;
                        }
                        if (slopeField >= 0) {
                            reachM.SetField(slopeField, lengthA + lengthD == 0 ? 0 : (dropA + dropD) / (lengthA + lengthD));
                        }
                        if (straight_lField >= 0) {
                            dataA = await this._gv.topo.getOgrReachData(reachA, demLayer);
                            dataD = await this._gv.topo.getOgrReachData(reachD, demLayer);
                            var dx = dataA.upperX - dataD.lowerX;
                            var dy = dataA.upperY - dataD.lowerY;
                            reachM.SetField(straight_lField, Math.Sqrt(dx * dx + dy * dy));
                        }
                        if (us_cont_arField >= 0) {
                            reachM.SetField(us_cont_arField, reachA.GetFieldAsDouble(us_cont_arField));
                        }
                        reachM.SetField(wsnoField, polygonidD);
                        if (dout_endField >= 0) {
                            reachM.SetField(dout_endField, reachD.GetFieldAsDouble(dout_endField));
                        }
                        if (dout_startField >= 0) {
                            reachM.SetField(dout_startField, reachA.GetFieldAsDouble(dout_startField));
                        }
                        if (dout_endField >= 0 && dout_midField >= 0) {
                            reachM.SetField(dout_midField, reachD.GetFieldAsDouble(dout_endField) + (lengthA + lengthD) / 2.0);
                        }
                        // executing mergeReaches may change fids, so delete D and A first
                        streamLayer.DeleteFeature(Math.Max(fidA, fidD));
                        streamLayer.DeleteFeature(Math.Min(fidA, fidD));
                        reachM.SetGeometry(geomD.Union(geomA));
                        var result = streamLayer.CreateFeature(reachM);
                        if (result != OSGeo.OGR.Ogr.OGRERR_NONE) {
                            Utils.error("Failed to merge streams", this._gv.isBatch);
                        }
                        // now FIDs have changed through deletion of A and D, but LINKNOs are now unique
                        var fidsToRemove = new List<long>();
                        var linksToRemove = (from reach in zeroReaches
                                             select reach.GetFieldAsInteger(linknoField)).ToList();
                        if (linksToRemove.Count > 0) {
                            OSGeo.OGR.Feature r = null;
                            streamLayer.ResetReading();
                            do {
                                r = streamLayer.GetNextFeature();
                                if (r != null && linksToRemove.Contains(r.GetFieldAsInteger(linknoField))) {
                                    fidsToRemove.Add(r.GetFID());
                                }
                            } while (r != null);
                            fidsToRemove.Sort();
                            fidsToRemove.Reverse();
                            foreach (var fid in fidsToRemove) {
                                streamLayer.DeleteFeature(fid);
                            }
                        }
                        // change dslinks in UAs to D (= M)
                        var linksToChange = (from reach in reachUAs
                                             select reach.GetFieldAsInteger(linknoField)).ToList();
                        if (linksToChange.Count > 0) {
                            OSGeo.OGR.Feature r = null;
                            streamLayer.ResetReading();
                            do {
                                r = streamLayer.GetNextFeature();
                                if (r != null && linksToChange.Contains(r.GetFieldAsInteger(linknoField))) {
                                    Utils.loginfo(string.Format("Changing link number {0} to have link {1} downstream", r.GetFieldAsInteger(linknoField), linknoD));
                                    r.SetField(dslinknoField, linknoD);
                                    streamLayer.SetFeature(r);
                                }
                            } while (r != null);
                        }
                        // change any dslinks to zeroLinks to D as the zeroReaches have been deleted
                        if (zeroLinks.Count > 0) {
                            OSGeo.OGR.Feature r = null;
                            streamLayer.ResetReading();
                            do {
                                r = streamLayer.GetNextFeature();
                                if (r != null && zeroLinks.Contains(r.GetFieldAsInteger(dslinknoField))) {
                                    r.SetField(dslinknoField, linknoD);
                                    streamLayer.SetFeature(r);
                                }
                            } while (r != null);
                        }
                    }
                    catch (Exception ex) {
                        Utils.error(string.Format("Exception while updating stream reach shapefile: {0}", ex.Message), this._gv.isBatch);
                        return;
                    }
                    // New watershed shapefile will be inconsistent with watershed grid, so remove grid to be recreated later.
                    // Do not do it immediately because the user may remove several subbasins, so we wait until the 
                    // delineation form is closed.
                    // clear name as flag that it needs to be recreated
                    this._gv.basinFile = "";
                    try {
                        // create new merged subbasin M from D and A and add it to wshed
                        // prepare reachM
                        long fidA = wshedA.GetFID();
                        long fidD = wshedD.GetFID();
                        var def = wLayer.GetLayerDefn();
                        var wshedM = new OSGeo.OGR.Feature(def);
                        wshedM.SetField(polygonidField, polygonidD);
                        if (areaField >= 0) {
                            var areaA = wshedA.GetFieldAsDouble(areaField);
                            var areaD = wshedD.GetFieldAsDouble(areaField);
                            wshedM.SetField(areaField, areaA + areaD);
                        }
                        if (streamlinkField >= 0) {
                            wshedM.SetField(streamlinkField, wshedD.GetFieldAsInteger(streamlinkField));
                        }
                        if (streamlenField >= 0) {
                            var lenA = wshedA.GetFieldAsDouble(streamlenField);
                            var lenD = wshedD.GetFieldAsDouble(streamlenField);
                            wshedM.SetField(streamlenField, lenA + lenD);
                        }
                        if (dsnodeidwField >= 0) {
                            wshedM.SetField(dsnodeidwField, wshedD.GetFieldAsInteger(dsnodeidwField));
                        }
                        if (dswsidField >= 0) {
                            wshedM.SetField(dswsidField, wshedD.GetFieldAsInteger(dswsidField));
                        }
                        if (us1wsidField >= 0) {
                            if (wshedD.GetFieldAsInteger(us1wsidField) == polygonidA) {
                                wshedM.SetField(us1wsidField, wshedA.GetFieldAsInteger(us1wsidField));
                            } else {
                                wshedM.SetField(us1wsidField, wshedD.GetFieldAsInteger(us1wsidField));
                            }
                        }
                        if (us2wsidField >= 0) {
                            if (wshedD.GetFieldAsInteger(us2wsidField) == polygonidA) {
                                wshedM.SetField(us2wsidField, wshedA.GetFieldAsInteger(us2wsidField));
                            } else {
                                wshedM.SetField(us2wsidField, wshedD.GetFieldAsInteger(us2wsidField));
                            }
                        }
                        if (subbasinField >= 0) {
                            wshedM.SetField(subbasinField, wshedD.GetFieldAsInteger(subbasinField));
                        }
                        //var num = OSGeo.OGR.Ogr.OGRERR_CORRUPT_DATA;
                        //num = OSGeo.OGR.Ogr.OGRERR_FAILURE;
                        //num = OSGeo.OGR.Ogr.OGRERR_INVALID_HANDLE;
                        //num = OSGeo.OGR.Ogr.OGRERR_NONE;
                        //num = OSGeo.OGR.Ogr.OGRERR_NON_EXISTING_FEATURE;
                        //num = OSGeo.OGR.Ogr.OGRERR_NOT_ENOUGH_DATA;
                        //num = OSGeo.OGR.Ogr.OGRERR_NOT_ENOUGH_MEMORY;
                        //num = OSGeo.OGR.Ogr.OGRERR_UNSUPPORTED_GEOMETRY_TYPE;
                        //num = OSGeo.OGR.Ogr.OGRERR_UNSUPPORTED_OPERATION;
                        //num = OSGeo.OGR.Ogr.OGRERR_UNSUPPORTED_SRS;
                        var geomA = wshedA.GetGeometryRef();
                        //var strA = "";
                        //geomA.ExportToWkt(out strA);
                        var geomD = wshedD.GetGeometryRef();
                        //var strD = "";
                        //geomD.ExportToWkt(out strD);
                        // executing merge may change oids, so delete D and A first
                        wLayer.DeleteFeature(Math.Max(fidA, fidD));
                        wLayer.DeleteFeature(Math.Min(fidA, fidD));
                        //geomA.ExportToWkt(out strA);
                        //geomD.ExportToWkt(out strD);
                        var geomM = geomD.Union(geomA);
                        //var strM = "";
                        //geomM.ExportToWkt(out strM);
                        if (!geomM.IsValid()) {
                            //Utils.information("Need to make wshed geometry valid", this._gv.isBatch);
                            geomM = geomM.MakeValid(null);
                            if (!geomM.IsValid()) {
                                Utils.error("Failed to make wshed geometry valid", this._gv.isBatch);
                                return;
                            }
                        }
                        wshedM.SetGeometry(geomM);
                        var result = wLayer.CreateFeature(wshedM);
                        if (result != OSGeo.OGR.Ogr.OGRERR_NONE) {
                            Utils.error("Failed to merge subbasins", this._gv.isBatch);
                        }
                        if (dswsidField >= 0) {
                            OSGeo.OGR.Feature w = null;
                            wLayer.ResetReading();
                            do {
                                w = wLayer.GetNextFeature();
                                if (w != null && w.GetFieldAsInteger(dswsidField) == polygonidA) {
                                    w.SetField(dswsidField, polygonidD);
                                    wLayer.SetFeature(w);
                                }
                            } while (w != null);
                        }
                    }
                    catch (Exception ex) {
                        Utils.error(string.Format("Exception while updating watershed shapefile: {0}", ex.Message), this._gv.isBatch);
                        return;
                    }
                }
            }
            // restore stream and watershed layers
            ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            await Utils.getLayerByFilename(this._gv.wshedFile, ft, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            await Utils.getLayerByFilename(this._gv.streamFile, FileTypes._STREAMS, this._gv, null, Utils._WATERSHED_GROUP_NAME);
        }

        //==========no longer used=================================================================
        // @staticmethod      
        // def reassignStrahler(streamLayer, reach, upLink, upOrder, linknoField, dslinknoField, orderField):
        //     """Reassign Strahler numbers downstream in the network starting from reach.
        //     Stop when the new Strahler number is already stored, or the root of the tree is reached.
        //     If a link draining to reach is the same as upLink, use upOrder as its order (since it is not 
        //     yet stored in streamLayer).
        //     """
        //     if reach is None:
        //         return
        //     link = reach[linknoField]
        //     ups = [up for up in streamLayer.getFeatures() if up[dslinknoField] == link]
        //     def orderOfReach(r): return upOrder if r[linknoField] == upLink else r[orderField]
        //     orders = [orderOfReach(up) for up in ups]
        //     s = Delineation.strahlerOrder(orders)
        //     if s != reach[orderField]:
        //         streamLayer.changeAttributeValue(reach.id(), orderField, s)
        //         downReach = Utils.getFeatureByValue(streamLayer, linknoField, reach[dslinknoField])
        //         Delineation.reassignStrahler(streamLayer, downReach, link, s, linknoField, dslinknoField, orderField)
        //         
        // @staticmethod
        // def calculateStrahler(streamLayer, upLinks, linknoField, orderField):
        //     """Calculate Strahler order from upstream links upLinks."""
        //     orders = [Utils.getFeatureByValue(streamLayer, linknoField, upLink)[orderField] for upLink in upLinks]
        //     return Delineation.strahlerOrder(orders)
        //     
        // @staticmethod
        // def strahlerOrder(orders):
        //     """Calculate Strahler order from a list or orders."""
        //     if len(orders) == 0:
        //         return 1
        //     else:
        //         omax = max(orders)
        //         count = len([o for o in orders if o == omax])
        //         return omax if count == 1 else omax+1
        //===========================================================================
        // Set cursor to Arrow, clear progress label, clear message bar, 
        //         and change tab index if not negative.
        //         
        public void cleanUp(int tabIndex) {
            if (tabIndex >= 0) {
                this.tabWidget.SelectedIndex = tabIndex;
            }
            this.progress("");
            return;
        }

        // Create watershed shapefile wshedFile from watershed grid wFile.
        public async void createWatershedShapefile(string wFile, string wshedFile) {
            Layer subLayer;
            FeatureLayer wshedLayer;
            if (Utils.isUpToDate(wFile, wshedFile)) {
                return;
            }
            //var wFileLayer = (await Utils.getLayerByFilename(wFile, FileTypes._WSHEDRASTER, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as RasterLayer;
            await Utils.removeLayerAndFiles(wshedFile);
            //var parms = Geoprocessing.MakeValueArray(this._gv.sourceDir, Path.GetFileName(wshedFile), "POLYGON");
            //Utils.runPython("runCreateWshedFile.py", parms, this._gv.isBatch);
            var parms = Geoprocessing.MakeValueArray(wFile, wshedFile);
            Utils.runPython("runRasterToPolygon.py", parms, this._gv);
            // use GDAL to rename gridcode to PolygonId and add Area and Subbasin
            using (var wshedDs = Ogr.Open(wshedFile, 1)) {
                Driver drv = wshedDs.GetDriver();
                if (drv is null) {
                    Utils.error("Cannot get GDAL driver for watershed file", this._gv.isBatch);
                    return;
                }
                // get layer - should only be one
                OSGeo.OGR.Layer layer = null;
                for (int iLayer = 0; iLayer < wshedDs.GetLayerCount(); iLayer++) {
                    layer = wshedDs.GetLayerByIndex(iLayer);
                    if (!(layer is null)) { break; }
                }
                if (layer == null) {
                    Utils.error(string.Format("No layers found in wshed file {0}", wshedFile), this._gv.isBatch);
                    return;
                }
                FeatureDefn def = layer.GetLayerDefn();
                // add PolygonId as a copy of Id field rather than trying to cahnge field name
                var gridcodeIndex = def.GetFieldIndex("gridcode");
                var polyFieldDef = new OSGeo.OGR.FieldDefn(Topology._POLYGONID, OSGeo.OGR.FieldType.OFTInteger);
                layer.AlterFieldDefn(gridcodeIndex, polyFieldDef, 1);
                var areaFieldDef = new OSGeo.OGR.FieldDefn(Topology._AREA, OSGeo.OGR.FieldType.OFTReal);
                layer.CreateField(areaFieldDef, 1);
                var subFieldDef = new OSGeo.OGR.FieldDefn(Topology._SUBBASIN, OSGeo.OGR.FieldType.OFTInteger);
                layer.CreateField(subFieldDef, 1);
                // set area field and fill in centroids table
                this._gv.topo.basinCentroids.Clear();
                layer.ResetReading();
                OSGeo.OGR.Feature f;
                do {
                    f = layer.GetNextFeature();
                    if (f != null) { 
                        OSGeo.OGR.Geometry geom = f.GetGeometryRef();
                        var area = geom.GetArea();
                        f.SetField(Topology._AREA, area);
                        int basin = f.GetFieldAsInteger(Topology._POLYGONID);
                        var centroid = geom.Centroid();
                        double[] argout = new double[3];
                        centroid.GetPoint(0, argout);
                        this._gv.topo.basinCentroids[basin] = new Coordinate2D(argout[0], argout[1]);
                        layer.SetFeature(f);
                    }
                } while (f != null);
            } 

            // make DEM active so loads above it and below streams
            // (or use Full HRUs layer if there is one)
            var fullHRUsLayer = Utils.getLayerByLegend(Utils._FULLHRUSLEGEND);
            if (fullHRUsLayer is not null) {
                subLayer = fullHRUsLayer;
            } else {
                var demLayer = await Utils.getLayerByFilenameOrLegend(this._gv.demFile, FileTypes._DEM, "", this._gv.isBatch);
                if (demLayer is not null) {
                    subLayer = demLayer;
                } else {
                    subLayer = null;
                }
            }
            wshedLayer = (await Utils.getLayerByFilename(wshedFile, FileTypes._WATERSHED, this._gv, subLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            if (wshedLayer is null) {
                Utils.error(string.Format("Failed to load watershed shapefile {0}", wshedFile), this._gv.isBatch);
                return;
            }
            // TODO
            //this._iface.setActiveLayer(wshedLayer);
            // labels should be turned off, as may persist from previous run
            // we turn back on when SWAT basin numbers are calculated and stored
            // in the Subbasin field
            await QueuedTask.Run(() => wshedLayer.SetLabelVisibility(false));
        }



        // Create basin file from watershed shapefile.
        public async Task<string> createBasinFile(string wshedFile, RasterLayer demLayer) {
            var demPath = await Utils.layerFilename(demLayer); 
            var @base = Path.ChangeExtension(demPath, null);
            var wFile = @base + "w.tif";
            await Utils.removeLayerAndFiles(wFile);
            //Debug.Assert(!File.Exists(wFile));
            var parms = Geoprocessing.MakeValueArray(wshedFile, Topology._POLYGONID, wFile, this._gv.demFile);
            Utils.runPython("runFeatureToRaster.py", parms, this._gv);
            //var xSize = demLayer.rasterUnitsPerPixelX();
            //var ySize = demLayer.rasterUnitsPerPixelY();
            //var extent = this.extent;
            //// need to use extent to align basin raster cells with DEM
            //var command = "gdal_rasterize -a {0} -tr {1} {2} -te {6} {7} {8} {9} -a_nodata -9999 -ot Int32 -of GTiff -l \"{3}\" \"{4}\" \"{5}\"", Topology._POLYGONID, xSize, ySize, baseName, wshedFile, wFile, extent.xMinimum(), extent.yMinimum(), extent.xMaximum(), extent.yMaximum());
            //Utils.loginfo(command);
            //os.system(command);
            //Debug.Assert(File.Exists(wFile));
            Utils.copyPrj(wshedFile, wFile);
            return wFile;
        }

        //// Create grid shapefile for watershed.
        //public virtual object createGridShapefile(object demLayer, string pFile, string ad8File, string wFile) {
        //    object gridStreamsFile;
        //    object gridFile;
        //    object readInletsFile(string fileName) {
        //        result = new HashSet<object>();
        //        using (var f = open(fileName, "r")) {
        //            foreach (var line in f) {
        //                nums = line.split(",");
        //                foreach (var num in nums) {
        //                    try {
        //                        val = Convert.ToInt32(num);
        //                        if (result.Contains(val)) {
        //                            Utils.error(string.Format("PolygonId {0} appears more than once in {1}", val, fileName));
        //                        } else {
        //                            result.add(val);
        //                        }
        //                    }
        //                    catch {
        //                    }
        //                }
        //            }
        //        }
        //        return result;
        //    }
        //    var gridSize = this.GridSize.value();
        //    var inlets = new HashSet<object>();
        //    if (this._gv.forTNC) {
        //        // store grid and gridstreams with DEM so can be reused for same grid size
        //        gridFile = Utils.join(this._gv.sourceDir, "grid{0}.shp", gridSize));
        //        gridStreamsFile = Utils.join(this._gv.sourceDir, "grid{0}streams.shp", gridSize));
        //        // inletsFile = Utils.join(self._gv.sourceDir, 'inlets.txt')  # inlets now added for TNC models by catchments.py
        //        // if os.path.isfile(inletsFile):
        //        //     inlets = readInletsFile(inletsFile)
        //    } else {
        //        gridFile = Utils.join(this._gv.shapesDir, "grid.shp");
        //        gridStreamsFile = Utils.join(this._gv.shapesDir, "gridstreams.shp");
        //    }
        //    if (Utils.isUpToDate(this._gv.demFile, gridFile) && Utils.isUpToDate(this._gv.demFile, gridStreamsFile)) {
        //        if (!this._gv.forTNC) {
        //            // or Utils.isUpToDate(inletsFile, gridFile):
        //            // restore settings of wshed and streams shapefiles
        //            this._gv.wshedFile = gridFile;
        //            this._gv.streamFile = gridStreamsFile;
        //            // make sure grid layers are loaded
        //            var root = QgsProject.instance().layerTreeRoot();
        //            (gridLayer, _) = Utils.getLayerByFilename(root.findLayers(), gridFile, FileTypes._GRID, this._gv, null, Utils._WATERSHED_GROUP_NAME);
        //            if (!gridLayer) {
        //                Utils.error(string.Format("Failed to load grid shapefile {0}", gridFile), this._gv.isBatch);
        //                return;
        //            }
        //            var gridStreamsLayer = Utils.getLayerByFilename(root.findLayers(), gridStreamsFile, FileTypes._GRIDSTREAMS, this._gv, gridLayer, Utils._WATERSHED_GROUP_NAME)[0];
        //            if (!gridStreamsLayer) {
        //                Utils.error(string.Format("Failed to load grid streams shapefile {0}", gridStreamsFile), this._gv.isBatch);
        //            }
        //            return;
        //        }
        //    }
        //    this.progress("Creating grid ...");
        //    var accFile = ad8File;
        //    var flowFile = pFile;
        //    var time2 = DateTime.Now;
        //    (storeGrid, accTransform, minDrainArea, maxDrainArea) = this.storeGridData(accFile, wFile, gridSize);
        //    var time3 = DateTime.Now;
        //    Utils.loginfo("Storing grid data took {0} seconds", Convert.ToInt32(time3 - time2)));
        //    if (storeGrid) {
        //        Debug.Assert(accTransform is not null);
        //        if (this.addDownstreamData(storeGrid, flowFile, gridSize, accTransform)) {
        //            var time4 = DateTime.Now;
        //            Utils.loginfo("Adding downstream data took {0} seconds", Convert.ToInt32(time4 - time3)));
        //            // inlets: Dict[int, Dict[int, int]] = dict()
        //            // self.addGridOutletsAuto(storeGrid, inlets)  # moved to catchments.py
        //            this.addGridOutlets(storeGrid, inlets);
        //            var time4a = DateTime.Now;
        //            // Utils.loginfo('Adding outlets took {0} seconds', int(time4a - time4)))
        //            Console.WriteLine("Adding outlets took {0} seconds", time4a - time4));
        //            this.writeGridShapefile(storeGrid, gridFile, flowFile, gridSize, accTransform, null);
        //            var time5 = DateTime.Now;
        //            Utils.loginfo("Writing grid shapefile took {0} seconds", Convert.ToInt32(time5 - time4a)));
        //            var numOutlets = this.writeGridStreamsShapefile(storeGrid, gridStreamsFile, flowFile, minDrainArea, maxDrainArea, accTransform);
        //            var time6 = DateTime.Now;
        //            Utils.loginfo("Writing grid streams shapefile took {0} seconds", Convert.ToInt32(time6 - time5)));
        //            // if numOutlets >= 0:
        //            //     msg = 'Grid processing done with delineation threshold {0} sq.km: {1} outlets', self.area.Text, numOutlets)
        //            //     Utils.loginfo(msg)
        //            //     self._iface.messageBar().pushMessage(msg, level=Qgis.Info, duration=10)
        //            //     if self._gv.isBatch:
        //            //         print(msg)
        //        }
        //    }
        //    return;
        //}

        //// Create grid data in array and return it.
        //public virtual object storeGridData(string ad8File, string basinFile, int gridSize) {
        //    // mask accFile with basinFile to exclude small outflowing watersheds
        //    // only do this if result needs updating and if not from GRASS (since ad8 file from GRASS was masked by basinFile)
        //    var @base = os.path.splitext(ad8File)[0];
        //    var accFile = this._gv.fromGRASS ? ad8File : @base + "clip.tif";
        //    if (!(this._gv.fromGRASS || Utils.isUpToDate(ad8File, accFile))) {
        //        var ad8Layer = QgsRasterLayer(ad8File, "P");
        //        var entry1 = QgsRasterCalculatorEntry();
        //        entry1.bandNumber = 1;
        //        entry1.raster = ad8Layer;
        //        entry1.@ref = "P@1";
        //        var basinLayer = QgsRasterLayer(basinFile, "Q");
        //        var entry2 = QgsRasterCalculatorEntry();
        //        entry2.bandNumber = 1;
        //        entry2.raster = basinLayer;
        //        entry2.@ref = "Q@1";
        //        Utils.tryRemoveFiles(accFile);
        //        // The formula is a standard way of masking P with Q, since 
        //        // where Q is nodata Q / Q evaluates to nodata, and elsewhere evaluates to 1.
        //        // We use 'Q+1' instead of Q to avoid problems in first subbasin 
        //        // when PolygonId is zero so Q is zero
        //        var formula = "((Q@1 + 1) / (Q@1 + 1)) * P@1";
        //        var calc = QgsRasterCalculator(formula, accFile, "GTiff", ad8Layer.extent(), ad8Layer.width(), ad8Layer.height(), new List<object> {
        //            entry1,
        //            entry2
        //        }, QgsCoordinateTransformContext());
        //        var result = calc.processCalculation(feedback: null);
        //        if (result == 0) {
        //            Debug.Assert(File.Exists(accFile));
        //            Debug.Assert("QGIS calculator formula {0} failed to write output", formula));
        //            Utils.copyPrj(ad8File, accFile);
        //        } else {
        //            Utils.error(string.Format("QGIS calculator formula {0} failed: returned {1}", formula, result), this._gv.isBatch);
        //            return (null, null, 0, 0);
        //        }
        //    }
        //    var accRaster = gdal.Open(accFile, gdal.GA_ReadOnly);
        //    if (accRaster is null) {
        //        Utils.error(string.Format("Cannot open accumulation file {0}", accFile), this._gv.isBatch);
        //        return (null, null, 0, 0);
        //    }
        //    // for now read whole clipped accumulation file into memory
        //    var accBand = accRaster.GetRasterBand(1);
        //    var accTransform = accRaster.GetGeoTransform();
        //    var accArray = accBand.ReadAsArray(0, 0, accBand.XSize, accBand.YSize);
        //    var accNoData = accBand.GetNoDataValue();
        //    var unitArea = abs(accTransform[1] * accTransform[5]) / 1000000.0;
        //    // create polygons and add to gridFile
        //    var polyId = 0;
        //    // grid cells will be gridSize x gridSize squares
        //    var numGridRows = accBand.YSize / gridSize + 1;
        //    var numGridCols = accBand.XSize / gridSize + 1;
        //    var storeGrid = new dict();
        //    var maxDrainArea = 0;
        //    var minDrainArea = double.PositiveInfinity;
        //    foreach (var gridRow in Enumerable.Range(0, numGridRows)) {
        //        var startAccRow = gridRow * gridSize;
        //        foreach (var gridCol in Enumerable.Range(0, numGridCols)) {
        //            var startAccCol = gridCol * gridSize;
        //            var maxAcc = 0;
        //            var maxRow = -1;
        //            var maxCol = -1;
        //            var valCount = 0;
        //            foreach (var row in Enumerable.Range(0, gridSize)) {
        //                var accRow = startAccRow + row;
        //                foreach (var col in Enumerable.Range(0, gridSize)) {
        //                    var accCol = startAccCol + col;
        //                    if (accRow < accBand.YSize && accCol < accBand.XSize) {
        //                        var accVal = accArray[accRow, accCol];
        //                        if (accVal != accNoData) {
        //                            valCount += 1;
        //                            // can get points with same (rounded) accumulation when values are high.
        //                            // prefer one on edge if possible
        //                            if (accVal > maxAcc || accVal == maxAcc && this.onEdge(row, col, gridSize)) {
        //                                maxAcc = accVal;
        //                                maxRow = accRow;
        //                                maxCol = accCol;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            if (valCount == 0) {
        //                // no data for this grid
        //                continue;
        //            }
        //            polyId += 1;
        //            //if polyId <= 5:
        //            //    x, y = Topology.cellToProj(maxCol, maxRow, accTransform)
        //            //    maxAccPoint = QgsPointXY(x, y)
        //            //    Utils.loginfo('Grid ({0},{1}) id {6} max {4} at ({2},{3}) which is {5}', gridRow, gridCol, maxCol, maxRow, maxAcc, maxAccPoint.toString(), polyId))
        //            var drainArea = maxAcc * unitArea;
        //            if (drainArea < minDrainArea) {
        //                minDrainArea = drainArea;
        //            }
        //            if (drainArea > maxDrainArea) {
        //                maxDrainArea = drainArea;
        //            }
        //            var data = new GridData(polyId, valCount, drainArea, maxAcc, maxRow, maxCol);
        //            if (!storeGrid.Contains(gridRow)) {
        //                storeGrid[gridRow] = new dict();
        //            }
        //            storeGrid[gridRow][gridCol] = data;
        //        }
        //    }
        //    accRaster = null;
        //    accArray = null;
        //    return (storeGrid, accTransform, minDrainArea, maxDrainArea);
        //}

        //// Returns true of (row, col) is on the edge of the cell.
        //public static bool onEdge(int row, int col, int gridSize) {
        //    return row == 0 || row == gridSize - 1 || col == 0 || col == gridSize - 1;
        //}

        //// Use flow direction flowFile to see to which grid cell a D8 step takes you from the max accumulation point and store in array.
        //public virtual bool addDownstreamData(object storeGrid, string flowFile, int gridSize, object accTransform) {
        //    object accToPCol;
        //    object accToPRow;
        //    var pRaster = gdal.Open(flowFile, gdal.GA_ReadOnly);
        //    if (pRaster is null) {
        //        Utils.error(string.Format("Cannot open flow direction file {0}", flowFile), this._gv.isBatch);
        //        return false;
        //    }
        //    // for now read whole D8 flow direction file into memory
        //    var pBand = pRaster.GetRasterBand(1);
        //    //pNoData = pBand.GetNoDataValue()
        //    var pTransform = pRaster.GetGeoTransform();
        //    if (pTransform[1] != accTransform[1] || pTransform[5] != accTransform[5]) {
        //        // problem with comparing floating point numbers
        //        // actually OK if the vertical/horizontal difference times the number of rows/columns
        //        // is less than half the depth/width of a cell
        //        if (abs(pTransform[1] - accTransform[1]) * pBand.XSize > pTransform[1] * 0.5 || abs(pTransform[5] - accTransform[5]) * pBand.YSize > abs(pTransform[5]) * 0.5) {
        //            Utils.error("Flow direction and accumulation files must have same cell size", this._gv.isBatch);
        //            pRaster = null;
        //            return false;
        //        }
        //    }
        //    var pArray = pBand.ReadAsArray(0, 0, pBand.XSize, pBand.YSize);
        //    // we know the cell sizes are sufficiently close;
        //    // accept the origins as the same if they are within a tenth of the cell size
        //    var sameCoords = pTransform == accTransform || abs(pTransform[0] - accTransform[0]) < pTransform[1] * 0.1 && abs(pTransform[3] - accTransform[3]) < abs(pTransform[5]) * 0.1;
        //    foreach (var (gridRow, gridCols) in storeGrid) {
        //        foreach (var (gridCol, gridData) in gridCols) {
        //            // since we have same cell sizes, can simplify conversion from accumulation row, col to direction row, col
        //            if (sameCoords) {
        //                accToPRow = 0;
        //                accToPCol = 0;
        //            } else {
        //                accToPCol = round((accTransform[0] - pTransform[0]) / accTransform[1]);
        //                accToPRow = round((accTransform[3] - pTransform[3]) / accTransform[5]);
        //                //pRow = Topology.yToRow(Topology.rowToY(gridData.maxRow, accTransform), pTransform)
        //                //pCol = Topology.xToCol(Topology.colToX(gridData.maxCol, accTransform), pTransform)
        //            }
        //            var currentPRow = gridData.maxRow + accToPRow;
        //            var currentPCol = gridData.maxCol + accToPCol;
        //            // try to find downstream grid cell.  If we fail downstram number left as -1, which means outlet
        //            // rounding of large accumulation values means that the maximum accumulation point found
        //            // may not be at the outflow point, so we need to move until we find a new grid cell, or hit a map edge
        //            var maxSteps = 2 * gridSize;
        //            var found = false;
        //            while (!found) {
        //                if (0 <= currentPRow && currentPRow < pBand.YSize && (0 <= currentPCol && currentPCol < pBand.XSize)) {
        //                    var direction = pArray[currentPRow, currentPCol];
        //                } else {
        //                    break;
        //                }
        //                // apply a step in direction
        //                if (1 <= direction && direction <= 8) {
        //                    currentPRow = currentPRow + Utils._dY[direction - 1];
        //                    currentPCol = currentPCol + Utils._dX[direction - 1];
        //                } else {
        //                    break;
        //                }
        //                var currentAccRow = currentPRow - accToPRow;
        //                var currentAccCol = currentPCol - accToPCol;
        //                var currentGridRow = currentAccRow / gridSize;
        //                var currentGridCol = currentAccCol / gridSize;
        //                found = currentGridRow != gridRow || currentGridCol != gridCol;
        //                if (!found) {
        //                    maxSteps -= 1;
        //                    if (maxSteps <= 0) {
        //                        (x0, y0) = Topology.cellToProj(gridData.maxCol, gridData.maxRow, accTransform);
        //                        (x, y) = Topology.cellToProj(currentAccCol, currentAccRow, accTransform);
        //                        Utils.error(string.Format("Loop in flow directions in grid id {4} starting from ({0},{1}) and so far reaching ({2},{3})", Convert.ToInt32(x0), Convert.ToInt32(y0), Convert.ToInt32(x), Convert.ToInt32(y), gridData.num), this._gv.isBatch);
        //                        Console.WriteLine("Loop in flow directions in grid id {4} starting from ({0},{1}) and so far reaching ({2},{3})", Convert.ToInt32(x0), Convert.ToInt32(y0), Convert.ToInt32(x), Convert.ToInt32(y), gridData.num));
        //                        break;
        //                    }
        //                }
        //            }
        //            if (found) {
        //                var cols = storeGrid.get(currentGridRow, null);
        //                if (cols is not null) {
        //                    var currentData = cols.get(currentGridCol, null);
        //                    if (currentData is not null) {
        //                        if (currentData.maxAcc < gridData.maxAcc) {
        //                            Utils.loginfo("WARNING: while calculating stream drainage, target grid cell {0} has lower maximum accumulation {1} than source grid cell {2}'s accumulation {3}", currentData.num, currentData.maxAcc, gridData.num, gridData.maxAcc));
        //                        }
        //                        gridData.downNum = currentData.num;
        //                        gridData.downRow = currentGridRow;
        //                        gridData.downCol = currentGridCol;
        //                        currentData.incount += 1;
        //                        //if gridData.num <= 5:
        //                        //    Utils.loginfo('Grid ({0},{1}) drains to acc ({2},{3}) in grid ({4},{5})', gridRow, gridCol, currentAccCol, currentAccRow, currentGridRow, currentGridCol))
        //                        //    Utils.loginfo('{0} at {1},{2} given down id {3}', gridData.num, gridRow, gridCol, gridData.downNum))
        //                        if (gridData.downNum == gridData.num) {
        //                            (x, y) = Topology.cellToProj(gridData.maxCol, gridData.maxRow, accTransform);
        //                            var maxAccPoint = QgsPointXY(x, y);
        //                            Utils.loginfo("Grid ({0},{1}) id {5} at ({2},{3}) which is {4} draining to ({6},{7})", gridCol, gridRow, gridData.maxCol, gridData.maxRow, maxAccPoint.toString(), gridData.num, currentAccCol, currentAccRow));
        //                            gridData.downNum = -1;
        //                        }
        //                        //assert gridData.downNum != gridData.num
        //                        storeGrid[gridRow][gridCol] = gridData;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    pRaster = null;
        //    pArray = null;
        //    return true;
        //}

        //=========No longer used.  Subcatchments defined by catchments module for TNC models==================================================================
        // @staticmethod
        // def addGridOutletsAuto(storeGrid: Dict[int, Dict[int, GridData]], inlets: Dict[int, Dict[int, int]]) -> None:
        //     """Add outlets to grid data, marking inlet points automatically."""
        //     maxChainlength = 30  # length of path without encountering an inlet before one is added
        //     inlets.clear()
        //     for gridRow, gridCols in storeGrid:
        //         for gridCol, gridData in gridCols:
        //             # start from leaf nodes
        //             if gridData.incount == 0:
        //                 current  = gridRow, gridCol
        //                 downChain: List[Tuple[int, int]] = [] 
        //                 while True:
        //                     currentGrid = storeGrid[current[0]][current[1]]
        //                     if currentGrid.outlet > 0:
        //                         # already been here
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.outlet
        //                         break
        //                     if currentGrid.downNum < 0:
        //                         currentGrid.outlet = currentGrid.num
        //                     elif len(downChain) == maxChainlength:
        //                         # before making an inlet here
        //                         # make sure next cell down is not already marked with an outlet
        //                         # to avoid the possibility of multiple inlets sharing a downstream node
        //                         # since there cannot be more than two such inlets
        //                         nextGrid = storeGrid.get(currentGrid.downRow, dict()).get(currentGrid.downCol, None)
        //                         if nextGrid is not None and nextGrid.outlet > 0:
        //                             # make currentGrid part of downstream catchment
        //                             currentGrid.outlet = nextGrid.outlet
        //                             # upstream cell is last in downChain
        //                             prevRow, prevCol = downChain[len(downChain) - 1]
        //                             prevGrid = storeGrid[prevRow][prevCol]
        //                             inlets.setdefault(prevRow, dict())[prevCol] = prevGrid.num
        //                             for row, col in downChain:
        //                                 storeGrid[row][col].outlet = prevGrid.num
        //                             print('Inlet at {0} moved upstream from {1}', prevGrid.num, currentGrid.num))
        //                             break
        //                         else:
        //                             inlets.setdefault(current[0], dict())[current[1]] = currentGrid.num
        //                             print('Inlet at {0}', currentGrid.num))
        //                             currentGrid.outlet = currentGrid.num
        //                         
        //                     if currentGrid.outlet > 0:
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.outlet
        //                         downChain = []
        //                     if currentGrid.downNum < 0:
        //                         break
        //                     if current in downChain:
        //                         Utils.loginfo('Row {0} column {1} links to itself in the grid', current[0], current[1]))
        //                         print('Row {0} column {1} links to itself in the grid', current[0], current[1]))
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.num
        //                         break
        //                     if currentGrid.outlet < 0:
        //                         downChain.append(current)
        //                     current = currentGrid.downRow, currentGrid.downCol
        //                 
        //===========================================================================

        //// Add outlets to grid data.  inlets now always an empty set, as added for TNC modesl in catchments.py
        //public static object addGridOutlets(object storeGrid, object inlets) {
        //    foreach (var (gridRow, gridCols) in storeGrid) {
        //        foreach (var gridCol in gridCols) {
        //            var current = (gridRow, gridCol);
        //            var downChain = new List<object>();
        //            while (true) {
        //                var currentGrid = storeGrid[current[0]][current[1]];
        //                if (currentGrid.downNum < 0 || inlets.Contains(currentGrid.num)) {
        //                    currentGrid.outlet = currentGrid.num;
        //                }
        //                if (currentGrid.outlet >= 0) {
        //                    foreach (var (row, col) in downChain) {
        //                        storeGrid[row][col].outlet = currentGrid.outlet;
        //                    }
        //                    break;
        //                }
        //                if (downChain.Contains(current)) {
        //                    Utils.loginfo("Row {0} column {1} links to itself in the grid", current[0], current[1]));
        //                    foreach (var (row, col) in downChain) {
        //                        storeGrid[row][col].outlet = currentGrid.num;
        //                    }
        //                    break;
        //                }
        //                downChain.append(current);
        //                current = (currentGrid.downRow, currentGrid.downCol);
        //            }
        //        }
        //    }
        //}

        //===========================================================================
        // @staticmethod
        // def addGridOutlets(storeGrid: Dict[int, Dict[int, GridData]], inlets: Set[int]) -> None:
        //     """Add outlets to grid data."""
        //     print('Inlets: {0}', inlets))
        //     for gridRow, gridCols in storeGrid:
        //         for gridCol, gridData in gridCols:
        //             if gridData.num in {5839, 5875}:
        //                 print('PolygonId: {0}, downNum {1}, incount: {2}, outlet {3}', gridData.num, gridData.downNum, gridData.incount, gridData.outlet))
        //             # start from leaf nodes
        //             if gridData.incount == 0:
        //                 current  = gridRow, gridCol
        //                 downChain: List[Tuple[int, int]] = []
        //                 while True:
        //                     currentGrid = storeGrid[current[0]][current[1]]
        //                     if currentGrid.downNum < 0 or currentGrid.num in inlets:
        //                         currentGrid.outlet = currentGrid.num
        //                         # if currentGrid.num in inlets:
        //                         #     print('Inlet at {0}', currentGrid.num))
        //                     if currentGrid.outlet >= 0:
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.outlet
        //                         if currentGrid.downNum < 0:
        //                             break
        //                     if current in downChain:
        //                         Utils.loginfo('Row {0} column {1} links to itself in the grid', current[0], current[1]))
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.num
        //                         break
        //                     if currentGrid.num in inlets:
        //                         downChain = []
        //                     else:
        //                         downChain.append(current)
        //                     current = currentGrid.downRow, currentGrid.downCol
        //===========================================================================

        //// Write grid data to grid shapefile.  Also writes centroids dictionary.
        //public virtual object writeGridShapefile(
        //    object storeGrid,
        //    string gridFile,
        //    string flowFile,
        //    int gridSize,
        //    object accTransform,
        //    void inlets = null) {
        //    object writer2;
        //    this.progress("Writing grid ...");
        //    var fields = QgsFields();
        //    fields.append(QgsField(Topology._POLYGONID, QVariant.Int));
        //    fields.append(QgsField(Topology._DOWNID, QVariant.Int));
        //    fields.append(QgsField(Topology._AREA, QVariant.Int));
        //    fields.append(QgsField(Topology._OUTLET, QVariant.Int));
        //    var root = QgsProject.instance().layerTreeRoot();
        //    Utils.removeLayer(gridFile);
        //    var transform_context = QgsProject.instance().transformContext();
        //    var writer = QgsVectorFileWriter.create(gridFile, fields, QgsWkbTypes.Polygon, this._gv.topo.crsProject, transform_context, this._gv.vectorFileWriterOptions);
        //    if (writer.hasError() != QgsVectorFileWriter.NoError) {
        //        Utils.error(string.Format("Cannot create grid shapefile {0}: {1}", gridFile, writer.errorMessage()), this._gv.isBatch);
        //        return;
        //    }
        //    var idIndex = fields.indexFromName(Topology._POLYGONID);
        //    var downIndex = fields.indexFromName(Topology._DOWNID);
        //    var areaIndex = fields.indexFromName(Topology._AREA);
        //    var outletIndex = fields.indexFromName(Topology._OUTLET);
        //    if (inlets is not null) {
        //        var fields2 = QgsFields();
        //        fields2.append(QgsField("Catchment", QVariant.Int));
        //        var inletsFile = os.path.split(gridFile)[0] + "/inletsshapes.shp";
        //        Utils.removeLayer(inletsFile);
        //        writer2 = QgsVectorFileWriter.create(inletsFile, fields2, QgsWkbTypes.Point, this._gv.topo.crsProject, transform_context, this._gv.vectorFileWriterOptions);
        //        if (writer2.hasError() != QgsVectorFileWriter.NoError) {
        //            Utils.error(string.Format("Cannot create inlets shapefile {0}: {1}", inletsFile, writer2.errorMessage()), this._gv.isBatch);
        //            inlets = null;
        //        }
        //    }
        //    (ul_x, x_size, _, ul_y, _, y_size) = accTransform;
        //    var xDiff = x_size * gridSize * 0.5;
        //    var yDiff = y_size * gridSize * 0.5;
        //    this._gv.topo.basinCentroids = new dict();
        //    this._gv.topo.catchmentOutlets = new dict();
        //    this._gv.topo.downCatchments = new dict();
        //    foreach (var (gridRow, gridCols) in storeGrid) {
        //        // grids can be big so we'll add one row at a time
        //        var centreY = (gridRow + 0.5) * gridSize * y_size + ul_y;
        //        var features = new List<object>();
        //        var features2 = new List<object>();
        //        foreach (var (gridCol, gridData) in gridCols) {
        //            var centreX = (gridCol + 0.5) * gridSize * x_size + ul_x;
        //            // this is strictly not the centroid for incomplete grid squares on the edges,
        //            // but will make little difference.  
        //            // Needs to be centre of grid for correct identification of landuse, soil and slope rows
        //            // when creating HRUs.
        //            this._gv.topo.basinCentroids[gridData.num] = (centreX, centreY);
        //            this._gv.topo.catchmentOutlets[gridData.num] = gridData.outlet;
        //            if (gridData.num == gridData.outlet) {
        //                // this is an outlet of a catchment: see if there is one downstream
        //                var dsGridData = storeGrid.get(gridData.downRow, new dict()).get(gridData.downCol, null);
        //                if (dsGridData is null || gridData.outlet == dsGridData.outlet) {
        //                    this._gv.topo.downCatchments[gridData.outlet] = -1;
        //                } else {
        //                    this._gv.topo.downCatchments[gridData.outlet] = dsGridData.outlet;
        //                }
        //            }
        //            var x1 = centreX - xDiff;
        //            var x2 = centreX + xDiff;
        //            var y1 = centreY - yDiff;
        //            var y2 = centreY + yDiff;
        //            var ring = new List<object> {
        //                QgsPointXY(x1, y1),
        //                QgsPointXY(x2, y1),
        //                QgsPointXY(x2, y2),
        //                QgsPointXY(x1, y2),
        //                QgsPointXY(x1, y1)
        //            };
        //            var feature = QgsFeature();
        //            feature.setFields(fields);
        //            feature.setAttribute(idIndex, gridData.num);
        //            feature.setAttribute(downIndex, gridData.downNum);
        //            feature.setAttribute(areaIndex, gridData.area);
        //            feature.setAttribute(outletIndex, gridData.outlet);
        //            var geometry = QgsGeometry.fromPolygonXY(new List<List<object>> {
        //                ring
        //            });
        //            feature.setGeometry(geometry);
        //            features.append(feature);
        //            if (inlets is not null) {
        //                var inletCatchment = inlets.get(gridRow, new dict()).get(gridCol, -1);
        //                if (inletCatchment > 0) {
        //                    var feature2 = QgsFeature();
        //                    feature2.setFields(fields2);
        //                    feature2.setAttribute(0, inletCatchment);
        //                    var geometry2 = QgsGeometry.fromPointXY(QgsPointXY(centreX, centreY));
        //                    feature2.setGeometry(geometry2);
        //                    features2.append(feature2);
        //                }
        //            }
        //        }
        //        if (!writer.addFeatures(features)) {
        //            Utils.error(string.Format("Unable to add features to grid shapefile {0}", gridFile), this._gv.isBatch);
        //            return;
        //        }
        //        if (features2.Count > 0) {
        //            if (!writer2.addFeatures(features2)) {
        //                Utils.error(string.Format("Unable to add features to inlets shapefile {0}", inletsFile), this._gv.isBatch);
        //            }
        //        }
        //    }
        //    // load grid shapefile
        //    // need to release writer before making layer
        //    writer = null;
        //    Utils.copyPrj(flowFile, gridFile);
        //    if (inlets is not null) {
        //        writer2 = null;
        //        Utils.copyPrj(flowFile, inletsFile);
        //    }
        //    // make wshed layer active so loads above it
        //    var wshedTreeLayer = Utils.getLayerByLegend(Utils._WATERSHEDLEGEND, root.findLayers());
        //    if (wshedTreeLayer) {
        //        var wshedLayer = wshedTreeLayer.layer();
        //        Debug.Assert(wshedLayer is not null);
        //        this._iface.setActiveLayer(wshedLayer);
        //        Utils.setLayerVisibility(wshedLayer, false);
        //    }
        //    (gridLayer, loaded) = Utils.getLayerByFilename(root.findLayers(), gridFile, FileTypes._GRID, this._gv, null, Utils._WATERSHED_GROUP_NAME);
        //    if (!gridLayer || !loaded) {
        //        Utils.error(string.Format("Failed to load grid shapefile {0}", gridFile), this._gv.isBatch);
        //        return;
        //    }
        //    this._gv.wshedFile = gridFile;
        //    var styleFile = FileTypes.styleFile(FileTypes._GRID);
        //    Debug.Assert(styleFile is not null);
        //    gridLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, styleFile));
        //    // make grid active layer so streams layer comes above it.
        //    this._iface.setActiveLayer(gridLayer);
        //}

        //// Write grid data to grid streams shapefile.
        //public virtual int writeGridStreamsShapefile(
        //    object storeGrid,
        //    string gridStreamsFile,
        //    string flowFile,
        //    double minDrainArea,
        //    double maxDrainArea,
        //    object accTransform) {
        //    object areaToPenWidth;
        //    this.progress("Writing grid streams ...");
        //    var root = QgsProject.instance().layerTreeRoot();
        //    var fields = QgsFields();
        //    fields.append(QgsField(Topology._LINKNO, QVariant.Int));
        //    fields.append(QgsField(Topology._DSLINKNO, QVariant.Int));
        //    fields.append(QgsField(Topology._WSNO, QVariant.Int));
        //    fields.append(QgsField(Topology._OUTLET, QVariant.Int));
        //    fields.append(QgsField("Drainage", QVariant.Double, len: 10, prec: 2));
        //    fields.append(QgsField(Topology._PENWIDTH, QVariant.Double));
        //    Utils.removeLayer(gridStreamsFile);
        //    var transform_context = QgsProject.instance().transformContext();
        //    var writer = QgsVectorFileWriter.create(gridStreamsFile, fields, QgsWkbTypes.LineString, this._gv.topo.crsProject, transform_context, this._gv.vectorFileWriterOptions);
        //    if (writer.hasError() != QgsVectorFileWriter.NoError) {
        //        Utils.error(string.Format("Cannot create grid shapefile {0}: {1}", gridStreamsFile, writer.errorMessage()), this._gv.isBatch);
        //        return -1;
        //    }
        //    var linkIndex = fields.indexFromName(Topology._LINKNO);
        //    var downIndex = fields.indexFromName(Topology._DSLINKNO);
        //    var wsnoIndex = fields.indexFromName(Topology._WSNO);
        //    var outletIndex = fields.indexFromName(Topology._OUTLET);
        //    var drainIndex = fields.indexFromName("Drainage");
        //    var penIndex = fields.indexFromName(Topology._PENWIDTH);
        //    if (maxDrainArea > minDrainArea) {
        //        // guard against division by zero
        //        var rng = maxDrainArea - minDrainArea;
        //        areaToPenWidth = x => (x - minDrainArea) * 1.8 / rng + 0.2;
        //    } else {
        //        areaToPenWidth = _ => 1.0;
        //    }
        //    var numOutlets = 0;
        //    foreach (var gridCols in storeGrid.Values) {
        //        // grids can be big so we'll add one row at a time
        //        var features = new List<object>();
        //        foreach (var gridData in gridCols.Values) {
        //            var downNum = gridData.downNum;
        //            (sourceX, sourceY) = Topology.cellToProj(gridData.maxCol, gridData.maxRow, accTransform);
        //            if (downNum > 0) {
        //                var downData = storeGrid[gridData.downRow][gridData.downCol];
        //                (targetX, targetY) = Topology.cellToProj(downData.maxCol, downData.maxRow, accTransform);
        //            } else {
        //                var targetX = sourceX;
        //                var targetY = sourceY;
        //                numOutlets += 1;
        //            }
        //            // respect default 'start at outlet' of TauDEM
        //            var link = new List<object> {
        //                QgsPointXY(targetX, targetY),
        //                QgsPointXY(sourceX, sourceY)
        //            };
        //            var feature = QgsFeature();
        //            feature.setFields(fields);
        //            feature.setAttribute(linkIndex, gridData.num);
        //            feature.setAttribute(downIndex, downNum);
        //            feature.setAttribute(wsnoIndex, gridData.num);
        //            feature.setAttribute(outletIndex, gridData.outlet);
        //            // area needs coercion to float or will not write
        //            feature.setAttribute(drainIndex, float(gridData.drainArea));
        //            // set pen width to value in range 0 .. 2
        //            feature.setAttribute(penIndex, float(areaToPenWidth(gridData.drainArea)));
        //            var geometry = QgsGeometry.fromPolylineXY(link);
        //            feature.setGeometry(geometry);
        //            features.append(feature);
        //        }
        //        if (!writer.addFeatures(features)) {
        //            Utils.error(string.Format("Unable to add features to grid streams shapefile {0}", gridStreamsFile), this._gv.isBatch);
        //            return -1;
        //        }
        //    }
        //    // flush writer
        //    writer.flushBuffer();
        //    WONKO_del(writer);
        //    // load grid streams shapefile
        //    Utils.copyPrj(flowFile, gridStreamsFile);
        //    //styleFile = FileTypes.styleFile(FileTypes._GRIDSTREAMS)
        //    // try to load above grid layer
        //    var gridLayer = Utils.getLayerByLegend(Utils._GRIDLEGEND, root.findLayers());
        //    var gridStreamsLayer = Utils.getLayerByFilename(root.findLayers(), gridStreamsFile, FileTypes._GRIDSTREAMS, this._gv, gridLayer, Utils._WATERSHED_GROUP_NAME)[0];
        //    if (!gridStreamsLayer) {
        //        Utils.error(string.Format("Failed to load grid streams shapefile {0}", gridStreamsFile), this._gv.isBatch);
        //        return -1;
        //    }
        //    Debug.Assert(gridStreamsLayer is QgsVectorLayer);
        //    //gridStreamsLayer.loadNamedStyle(Utils.join(self._gv.plugin_dir, styleFile))
        //    // make stream width dependent on drainage values (drainage is accumulation, ie number of dem cells draining to start of stream)
        //    var numClasses = 5;
        //    var props = new Dictionary<object, object> {
        //        {
        //            "width_expression",
        //            Topology._PENWIDTH}};
        //    var symbol = QgsLineSymbol.createSimple(props);
        //    //style = QgsStyleV2().defaultStyle()
        //    //ramp = style.colorRamp('Blues')
        //    // ramp from light to darkish blue
        //    var color1 = QColor(166, 206, 227, 255);
        //    var color2 = QColor(0, 0, 255, 255);
        //    var ramp = QgsGradientColorRamp(color1, color2);
        //    var labelFmt = QgsRendererRangeLabelFormat("%1 - %2", 0);
        //    var renderer = QgsGraduatedSymbolRenderer.createRenderer(gridStreamsLayer, "Drainage", numClasses, QgsGraduatedSymbolRenderer.Jenks, symbol, ramp, labelFmt);
        //    gridStreamsLayer.setRenderer(renderer);
        //    gridStreamsLayer.setOpacity(1);
        //    gridStreamsLayer.triggerRepaint();
        //    var treeModel = QgsLayerTreeModel(root);
        //    var gridStreamsTreeLayer = root.findLayer(gridStreamsLayer.id());
        //    Debug.Assert(gridStreamsTreeLayer is not null);
        //    treeModel.refreshLayerLegend(gridStreamsTreeLayer);
        //    this._gv.streamFile = gridStreamsFile;
        //    this.progress("");
        //    return numOutlets;
        //}

        // Return row and column after 1 step in D8 direction.
        public static object moveD8(int row, int col, int direction) {
            if (direction == 1) {
                // E
                return (row, col + 1);
            } else if (direction == 2) {
                // NE
                return (row - 1, col + 1);
            } else if (direction == 3) {
                // N
                return (row - 1, col);
            } else if (direction == 4) {
                // NW
                return (row - 1, col - 1);
            } else if (direction == 5) {
                // W
                return (row, col - 1);
            } else if (direction == 6) {
                // SW
                return (row + 1, col - 1);
            } else if (direction == 7) {
                // S
                return (row + 1, col);
            } else if (direction == 8) {
                // SE
                return (row + 1, col + 1);
            } else {
                // we have run off the edge of the direction grid
                return (-1, -1);
            }
        }

        //===========================================================================
        // def streamToRaster(self, demLayer: QgsRasterLayer, streamFile: str, root: QgsLayerTree) -> str:
        //     """Use rasterize to generate a raster for the streams, with a fixed value of 1 along the streams."""
        //     demPath = Utils.layerFileInfo(demLayer).absolutePath()
        //     rasterFile = Utils.join(os.path.splitext(demPath)[0], 'streams.tif')
        //     ok, path = Utils.removeLayerAndFiles(wFile)
        //     if not ok:
        //         Utils.error('Failed to remove {0}: try repeating last click, else remove manually.', path), self._gv.isBatch)
        //         self.setCursor(Qt.ArrowCursor)
        //         return ''
        //     assert not File.Exists(rasterFile)
        //     extent = demLayer.extent()
        //     xMin = extent.xMinimum()
        //     xMax = extent.xMaximum()
        //     yMin = extent.yMinimum()
        //     yMax = extent.yMaximum()
        //     xSize = demLayer.rasterUnitsPerPixelX()
        //     ySize = demLayer.rasterUnitsPerPixelY()
        //     command = 'gdal_rasterize -burn 1 -a_nodata -9999 -te {0} {1} {2} {3} -tr {4} {5} -ot Int32 "{6}" "{7}"', xMin, yMin, xMax, yMax, xSize, ySize, streamFile, rasterFile)
        //     Utils.information(command, self._gv.isBatch)
        //     os.system(command)
        //     assert File.Exists(rasterFile)
        //     Utils.copyPrj(streamFile, rasterFile)
        //     return rasterFile
        //===========================================================================
        // Create inlets/outlets file with points snapped to stream reaches.
        public async Task<bool> createSnapOutletFile(
            FeatureLayer outletLayer,
            FeatureLayer streamLayer,
            string outletFile,
            string snapFile) {
            int countPoints = 0;
            int errorCount = 0;
            int count = 0;
            int outletCount = 0;
            bool result = await QueuedTask.Run<bool>(async () => {
                using (var fc = outletLayer.GetFeatureClass()) {
                    countPoints = Convert.ToInt32(fc.GetCount());
                }
                if (countPoints == 0) {
                    Utils.error(string.Format("The outlet layer {0} has no points", outletLayer.Name), this._gv.isBatch);
                    return false;
                }
                int snapThreshold = 0;
                try {
                    snapThreshold = Convert.ToInt32(this.snapThreshold.Text);
                }
                catch (Exception) {
                    Utils.error(string.Format("Cannot parse snap threshold {0} as integer.", this.snapThreshold.Text), this._gv.isBatch);
                    return false;
                }
                if (!await this.createOutletFile(snapFile, outletFile, false)) {
                    return false;
                }
                if (this._gv.isBatch) {
                    Utils.information(string.Format("Snap threshold: {0} metres", snapThreshold), this._gv.isBatch);
                }
                //var idIndex = await this._gv.topo.getIndex(outletLayer, Topology._ID);
                //var inletIndex = await this._gv.topo.getIndex(outletLayer, Topology._INLET);
                //var resIndex = await this._gv.topo.getIndex(outletLayer, Topology._RES);
                //var ptsourceIndex = await this._gv.topo.getIndex(outletLayer, Topology._PTSOURCE);
                // now using GDAL to create snap file, to avoid 'Field is not editable' error in ArcGIS

                using (DataSource snapDs = Ogr.Open(snapFile, 1)) {
                    if (snapDs is null) {
                        Utils.error(string.Format("Cannot open snap file {0}", snapFile), this._gv.isBatch);
                        return false;
                    }
                    Driver drv = snapDs.GetDriver();
                    if (drv is null) {
                        Utils.error("Cannot get GDAL driver for snap file", this._gv.isBatch);
                        return false;
                    }
                    // get layer - should only be one
                    OSGeo.OGR.Layer snapLayer = null;
                    for (int iLayer = 0; iLayer < snapDs.GetLayerCount(); iLayer++) {
                        snapLayer = snapDs.GetLayerByIndex(iLayer);
                        if (!(snapLayer is null)) { break; }
                    }
                    if (snapLayer == null) {
                        Utils.error(string.Format("No layers found in snap file {0}", snapFile), this._gv.isBatch);
                        return false;
                    }
                    FeatureDefn def = snapLayer.GetLayerDefn();
                    var idSnapIndex = def.GetFieldIndex(Topology._ID);
                    var inletSnapIndex = def.GetFieldIndex(Topology._INLET);
                    var resSnapIndex = def.GetFieldIndex(Topology._RES);
                    var ptsourceSnapIndex = def.GetFieldIndex(Topology._PTSOURCE);

                    //var snapLayer = (await Utils.getLayerByFilename(snapFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                    //var idSnapIndex = await this._gv.topo.getIndex(snapLayer, Topology._ID);
                    //var inletSnapIndex = await this._gv.topo.getIndex(snapLayer, Topology._INLET);
                    //var resSnapIndex = await this._gv.topo.getIndex(snapLayer, Topology._RES);
                    //var ptsourceSnapIndex = await this._gv.topo.getIndex(snapLayer, Topology._PTSOURCE);
                    outletLayer = (await Utils.getLayerByFilename(outletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                    using (var rc = outletLayer.Search()) {
                        while (rc.MoveNext()) {
                            var feature = rc.Current as Feature;
                            var point = feature.GetShape() as MapPoint;
                            var point1 = Topology.snapPointToReach(streamLayer, new Coordinate2D(point.X, point.Y), (double)snapThreshold, this._gv.isBatch);
                            if (point1 is null) {
                                errorCount++;
                                continue;
                            }

                            var pid = Convert.ToInt32(feature[Topology._ID]);
                            var inlet = Convert.ToInt32(feature[Topology._INLET]);
                            var res = Convert.ToInt32(feature[Topology._RES]);
                            var ptsource = Convert.ToInt32(feature[Topology._PTSOURCE]);
                            if (inlet == 0 && res == 0) {
                                outletCount += 1;
                            }
                            OSGeo.OGR.Feature pt1 = new OSGeo.OGR.Feature(def);
                            pt1.SetField(idSnapIndex, pid);
                            pt1.SetField(inletSnapIndex, inlet);
                            pt1.SetField(resSnapIndex, res);
                            pt1.SetField(ptsourceSnapIndex, ptsource);
                            OSGeo.OGR.Geometry ogrPoint = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
                            ogrPoint.SetPoint_2D(0, ((Coordinate2D)point1).X, ((Coordinate2D)point1).Y);
                            pt1.SetGeometry(ogrPoint);
                            var num = snapLayer.CreateFeature(pt1);
                            if (num < 0) { errorCount++; }
                            count++;

                            // Utils.information('Snap point at ({0:F2}, {1:F2})', point1.X, point1.Y), self._gv.isBatch)

                            //bool creationResult;
                            //string message = "";
                            //using (var snapFC = snapLayer.GetFeatureClass())
                            //using (var snapDefn = snapFC.GetDefinition()) {
                            //    EditOperation editOperation = new EditOperation();
                            //    editOperation.Callback(context => {
                            //        using (RowBuffer rowBuffer = snapFC.CreateRowBuffer()) {
                            //            rowBuffer[idSnapIndex] = pid;
                            //            rowBuffer[inletSnapIndex] = inlet;
                            //            rowBuffer[resSnapIndex] = res;
                            //            rowBuffer[ptsourceSnapIndex] = ptsource;
                            //            MapPointBuilderEx mb = new MapPointBuilderEx((Coordinate2D)point1, MapView.Active.Map.SpatialReference);
                            //            rowBuffer[snapDefn.GetShapeField()] = mb.ToGeometry();
                            //            using (Feature feature = snapFC.CreateRow(rowBuffer)) {
                            //                context.Invalidate(feature);
                            //            }
                            //        }
                            //    }, snapFC);
                            //    try {
                            //        creationResult = editOperation.Execute();
                            //        if (!creationResult) message = editOperation.ErrorMessage;
                            //    }
                            //    catch (GeodatabaseException exObj) {
                            //        message = exObj.Message;
                            //    }
                            //    if (!string.IsNullOrEmpty(message)) {
                            //        Utils.error("Cannot add point to snap outlets file: " + message, this._gv.isBatch);
                            //        return false;
                            //    }
                            //    count += 1;
                            //}
                        }
                    }
                }
                return true;
            });
                
                string failMessage = errorCount == 0 ? "" : string.Format(": {0} failed", errorCount);
                    this.snappedLabel.Text = string.Format("{0} snapped{1}", count, failMessage);
                    if (this._gv.isBatch) {
                        Utils.information(string.Format("{0} snapped{1}", count, failMessage), true);
                    }
                    if (count == 0) {
                        Utils.error("Could not snap any points to stream reaches", this._gv.isBatch);
                        return false;
                    }
                    if (outletCount == 0) {
                        Utils.error(string.Format("Your outlet layer {0} contains no outlets", outletLayer.Name), this._gv.isBatch);
                        return false;
                    }
                    // shows we have created a snap file
                    this.snapFile = snapFile;
                    this.snapErrors = errorCount > 0;
                    return result;
        }

        //===========================================================================
        // @staticmethod
        // def createOutletFields(subWanted: bool) -> QgsFields:
        //     """Return felds for inlets/outlets file, adding Subbasin field if wanted."""
        //     fields = QgsFields()
        //     fields.append(QgsField(Topology._ID, QVariant.Int))
        //     fields.append(QgsField(Topology._INLET, QVariant.Int))
        //     fields.append(QgsField(Topology._RES, QVariant.Int))
        //     fields.append(QgsField(Topology._PTSOURCE, QVariant.Int))
        //     if subWanted:
        //         fields.append(QgsField(Topology._SUBBASIN, QVariant.Int))
        //     return fields
        //===========================================================================
        //===========================================================================
        // def createOutletFile(self, filePath: str, sourcePath: str, subWanted: bool, root: QgsLayerTreeGroup) -> Tuple[Optional[QgsVectorFileWriter], QgsFields]:
        //     """Create filePath with fields needed for outlets file, 
        //     copying .prj from sourcePath, and adding Subbasin field if wanted.
        //     """
        //     Utils.tryRemoveLayerAndFiles(filePath)
        //     fields = Delineation.createOutletFields(subWanted)
        //     transform_context = QgsProject.instance().transformContext()
        //     writer = QgsVectorFileWriter.create(filePath, fields, QgsWkbTypes.Point, self._gv.topo.crsProject,
        //                                         transform_context, self._gv.vectorFileWriterOptions)
        //     if writer.hasError() != QgsVectorFileWriter.NoError:
        //         Utils.error('Cannot create outlets shapefile {0}: {1}', filePath, writer.errorMessage()), self._gv.isBatch)
        //         return None, fields
        //     Utils.copyPrj(sourcePath, filePath)
        //     return writer, fields
        //===========================================================================
        // Create filePath with fields needed for outlets file, 
        //         copying .prj from sourcePath, and adding Subbasin field if wanted.
        //         
        //         Uses OGR since QgsVectorFileWriter.create seems to be broken.
        //         
        public async Task<bool> createOutletFile(string filePath, string sourcePath, bool subWanted) {
            bool ok = true; 
            await Utils.removeLayerAndFiles(filePath);
            try {
                string dir = Path.GetDirectoryName(filePath);
                string filename = Path.GetFileName(filePath);
                string withSub = subWanted ? "true" : "false";
                var parms = Geoprocessing.MakeValueArray(dir, filename, "POINT", withSub);
                Utils.runPython("runCreateOutletFile.py", parms, this._gv);
                //await QueuedTask.Run( () => {
                //    FileSystemConnectionPath connectionPath = new FileSystemConnectionPath(new Uri(dir), FileSystemDatastoreType.Shapefile);
                //    FileSystemDatastore shapes = new FileSystemDatastore(connectionPath);
                //    FeatureClassDefinition origFCDef = shapes.GetDefinition<FeatureClassDefinition>(filename);
                //    var origFCDesc = new FeatureClassDescription(origFCDef);
                //    var idField = FieldDescription.CreateIntegerField(Topology._ID);
                //    var inletField = FieldDescription.CreateIntegerField(Topology._INLET);
                //    var resField = FieldDescription.CreateIntegerField(Topology._RES);
                //    var ptsourceField = FieldDescription.CreateIntegerField(Topology._PTSOURCE);
                //    var fieldsToAdd = new List<FieldDescription> { idField, inletField, resField, ptsourceField };
                //    if (subWanted) {
                //        var subField = FieldDescription.CreateIntegerField(Topology._SUBBASIN);
                //        fieldsToAdd.Add(subField);
                //    }
                //    var modFDesc = new List<FieldDescription>(origFCDesc.FieldDescriptions);
                //    modFDesc.AddRange(fieldsToAdd);
                //    var modFCDesc = new FeatureClassDescription(origFCDesc.Name, modFDesc, origFCDesc.ShapeDescription);
                //    using (var ds = shapes as Datastore)
                //    using (var gdb = ds as Geodatabase) { 
                //    //using (var dataset = shapes.OpenDataset<Table>(filename))
                //    //using (var gdb = dataset.GetDatastore() as Geodatabase) {
                //        SchemaBuilder schemaBuilder = new SchemaBuilder(gdb);
                //        schemaBuilder.Modify(modFCDesc);
                //        bool modifyStatus = schemaBuilder.Build();
                //        if (!modifyStatus) {
                //            foreach (string error in schemaBuilder.ErrorMessages) {
                //                Utils.error("Failed to create outlets file " + error, this._gv.isBatch);
                //                ok = false;
                //            }
                //        }
                //    }
                //});
                Utils.copyPrj(sourcePath, filePath);
                ok = true;
            }
            catch (Exception ex) {
                Utils.error(string.Format("Failure to create points file: {0}", ex.Message), this._gv.isBatch);
                ok = false;
            }
            return ok;
        }

        // Get list of ID values from inlets/outlets layer 
        //         for which field has value 1.
        //         
        public async Task<HashSet<int>> getOutletIds(string field) {
            var result = new HashSet<int>();
            if (this._gv.outletFile == "") {
                return result;
            }
            var outletLayer = await Utils.getLayerByFilenameOrLegend(this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch) as FeatureLayer;
            if (outletLayer is null) {
                Utils.error("Cannot find inlets/outlets layer", this._gv.isBatch);
                return result;
            }
            var idIndex = await this._gv.topo.getIndex(outletLayer, Topology._ID);
            var fieldIndex = await this._gv.topo.getIndex(outletLayer, field);
            await QueuedTask.Run(() => {
                using (var rc = outletLayer.Search()) {
                    while (rc.MoveNext()) {
                        var f = rc.Current as Feature;
                        if (Convert.ToInt32(f[fieldIndex]) == 1) {
                            result.Add(Convert.ToInt32(f[idIndex]));
                        }
                    }
                }
            });
            return result;
        }

        // Update progress label with message; emit message for display in testing.
        public void progress(string msg) {
            Utils.progress(msg, this.progressLabel);
            //if (msg != "") {
            //    this.progress_signal.emit(msg);
            //}
        }

        //public object progress_signal = pyqtSignal(str);

        // Close form and return dialog result
        public void doClose(bool ok) {
            this.Close();
            if (ok && this.delineationFinishedOK && this.finishHasRun) {
                this._gv.writeMasterProgress(1, 0);
                this._parent.postDelineation(true);
            } else {
                // incomplete
                this._gv.writeMasterProgress(0, 0);
                this._parent.postDelineation(false);
            }
        }

        private void selectOutletsButton_Click(object sender, EventArgs e) {
            this.btnSetOutlets();
        }

        private void selectWshedButton_Click(object sender, EventArgs e) {
            btnSetWatershed();
        }

        private void selectNetButton_Click(object sender, EventArgs e) {
            btnSetStreams();
        }

        private void selectExistOutletsButton_Click(object sender, EventArgs e) {
            this.btnSetOutlets();
        }

        private void delinRunButton1_Click(object sender, EventArgs e) {
            this.runTauDEM1();
        }

        private void delinRunButton2_Click(object sender, EventArgs e) {
            this.runTauDEM2();
        }

        private void tabWidget_SelectedIndexChanged(object sender, EventArgs e) {
            this.changeExisting();
        }

        private void existRunButton_Click(object sender, EventArgs e) {
            this.runExisting();
        }

        private void useOutlets_CheckedChanged(object sender, EventArgs e) {
            this.changeUseOutlets();
        }

        private void drawOutletsButton_Click(object sender, EventArgs e) {
            this.drawOutlets();
        }

        private void selectOutletsInteractiveButton_Click(object sender, EventArgs e) {
            this.doSelectOutlets();
        }

        private void snapReviewButton_Click(object sender, EventArgs e) {
            this.snapReview();
        }

        private void selectSubButton_Click(object sender, EventArgs e) {
            this.selectMergeSubbasins();
        }

        private void mergeButton_Click(object sender, EventArgs e) {
            this.mergeSubbasinsOgr();
        }

        private void selectResButton_Click(object sender, EventArgs e) {
            this.selectReservoirs();
        }

        private void addButton_Click(object sender, EventArgs e) {
            this.addReservoirs();
        }

        private void taudemHelpButton_Click(object sender, EventArgs e) {
            TauDEMUtils.taudemHelp();
        }

        private void OKButton_Click(object sender, EventArgs e) {
            this.finishDelineation();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.doClose(false);
        }

        private void area_TextChanged(object sender, EventArgs e) {
            if (this.area.Enabled) { this.setNumCells(); }
        }

        private void areaUnitsBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (this.areaUnitsBox.Enabled) { this.changeAreaOfCell(); }
        }

        private void verticalCombo_SelectedIndexChanged(object sender, EventArgs e) {
            this.setVerticalUnits();
        }

        // Read delineation data from project database.
        public async void readProj() {
            string extraOutletFile;
            string outletFile;
            string streamFile;
            string burnFile;
            string wshedFile;
            string possFile;
            Layer layer;
            string demFile;
            Proj proj = this._gv.proj;
            var title = this._gv.projName;
            //var root = QgsProject.instance().layerTreeRoot();
            bool found;
            this.tabWidget.SelectedIndex = 0;
            (this._gv.existingWshed, found) = proj.readBoolEntry(title, "delin/existingWshed", false);
            if (found && this._gv.existingWshed) {
                this.tabWidget.SelectedIndex = 1;
            }
            Utils.loginfo(string.Format("Existing watershed is {0}", this._gv.existingWshed));
            (this._gv.useGridModel, _) = proj.readBoolEntry(title, "delin/useGridModel", false);
            Utils.loginfo(string.Format("Use grid model is {0}", this._gv.useGridModel));
            int gridSize;
            if (this._gv.useGridModel) {
                (gridSize, found) = proj.readNumEntry(title, "delin/gridSize", 1);
                if (found) {
                    //this.GridSize.setValue(gridSize);
                    this._gv.gridSize = gridSize;
                }
            }
            (demFile, found) = proj.readEntry(title, "delin/DEM", "");
            RasterLayer demLayer = null;
            if (found && !string.IsNullOrEmpty(demFile) && File.Exists(demFile)) {
                demLayer = (await Utils.getLayerByFilename(demFile, FileTypes._DEM, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as RasterLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._DEM));
                if (layer is not null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._DEM)), this._gv.isBatch, true) == MessageBoxResult.Yes) {
                        demLayer = layer as RasterLayer;
                        demFile = possFile;
                    }
                }
            }
            if (demLayer is not null) {
                this._gv.demFile = demFile;
                this.selectDem.Text = this._gv.demFile;
                await this.setDefaultNumCells(demLayer);
            } else {
                this._gv.demFile = "";
            }
            string verticalUnits;
            (verticalUnits, found) = proj.readEntry("", "delin/verticalUnits", Parameters._METRES);
            if (found) {
                this._gv.verticalUnits = verticalUnits;
                this._gv.setVerticalFactor();
            }
            int threshold;
            (threshold, found) = proj.readNumEntry(title, "delin/threshold", 0);
            if (found && threshold > 0) {
                try {
                    this.numCells.Text = threshold.ToString();
                }
                catch (Exception) {
                }
            }
            int snapThreshold;
            (snapThreshold, found) = proj.readNumEntry(title, "delin/snapThreshold", 300);
            this.snapThreshold.Text = snapThreshold.ToString();
            (wshedFile, found) = proj.readEntry(title, "delin/wshed", "");
            FeatureLayer wshedLayer = null;
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            if (found && !string.IsNullOrEmpty(wshedFile) && File.Exists(wshedFile)) {
                wshedLayer = (await Utils.getLayerByFilename(wshedFile, ft, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(ft));
                if (layer is not null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(ft)), this._gv.isBatch, true) == MessageBoxResult.Yes) {
                        wshedLayer = layer as FeatureLayer;
                        wshedFile = possFile;
                    }
                }
            }
            if (wshedLayer is not null) {
                this.selectWshed.Text = wshedFile;
                this._gv.wshedFile = wshedFile;
            } else {
                this._gv.wshedFile = "";
            }
            (burnFile, found) = proj.readEntry(title, "delin/burn", "");
            FeatureLayer burnLayer = null;
            if (found && !string.IsNullOrEmpty(burnFile) && File.Exists(burnFile)) {
                burnLayer = (await Utils.getLayerByFilename(burnFile, FileTypes._BURN, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._BURN));
                if (layer is not null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._BURN)), this._gv.isBatch, true) == MessageBoxResult.Yes) {
                        burnLayer = layer as FeatureLayer;
                        burnFile = possFile;
                    }
                }
            }
            if (burnLayer is not null) {
                this._gv.burnFile = burnFile;
                this.checkBurn.Checked = true;
                this.selectBurn.Text = burnFile;
            } else {
                this._gv.burnFile = "";
            }
            (streamFile, found) = proj.readEntry(title, "delin/net", "");
            FeatureLayer streamLayer = null;
            if (found && !string.IsNullOrEmpty(streamFile) && File.Exists(streamFile)) {
                streamLayer = (await Utils.getLayerByFilename(streamFile, FileTypes._STREAMS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._STREAMS));
                if (layer is not null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._STREAMS)), this._gv.isBatch, true) == MessageBoxResult.Yes) {
                        streamLayer = layer as FeatureLayer;
                        streamFile = possFile;
                    }
                }
            }
            if (streamLayer is not null) {
                this.selectNet.Text = streamFile;
                this._gv.streamFile = streamFile;
            } else {
                this._gv.streamFile = "";
            }
            bool useOutlets;
            (useOutlets, found) = proj.readBoolEntry(title, "delin/useOutlets", true);
            if (found) {
                this.useOutlets.Checked = useOutlets;
                this.changeUseOutlets();
            }
            (outletFile, found) = proj.readEntry(title, "delin/outlets", "");
            FeatureLayer outletLayer = null;
            if (found && !string.IsNullOrEmpty(outletFile) && File.Exists(outletFile)) {
                outletLayer = (await Utils.getLayerByFilename(outletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            } else {
                layer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._OUTLETS));
                if (layer is not null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, FileTypes.legend(FileTypes._OUTLETS)), this._gv.isBatch, true) == MessageBoxResult.Yes) {
                        outletLayer = layer as FeatureLayer;
                        outletFile = possFile;
                        useOutlets = true;
                        this.useOutlets.Checked = true;
                        this.changeUseOutlets();
                    }
                }
            }
            if (outletLayer is not null) {
                this._gv.outletFile = outletFile;
                this.selectExistOutlets.Text = this._gv.outletFile;
                this.selectOutlets.Text = this._gv.outletFile;
            } else {
                this._gv.outletFile = "";
            }
            (extraOutletFile, found) = proj.readEntry(title, "delin/extraOutlets", "");
            FeatureLayer extraOutletLayer = null;
            if (found && !string.IsNullOrEmpty(extraOutletFile) && File.Exists(extraOutletFile)) {
                extraOutletLayer = (await Utils.getLayerByFilename(extraOutletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            } else {
                layer = Utils.getLayerByLegend(Utils._EXTRALEGEND);
                if (layer is not null) {
                    possFile = await Utils.layerFilename(layer);
                    if (Utils.question(string.Format("Use {0} as {1} file?", possFile, Utils._EXTRALEGEND), this._gv.isBatch, true) == MessageBoxResult.Yes) {
                        extraOutletLayer = layer as FeatureLayer;
                        extraOutletFile = possFile;
                    }
                }
            }
            if (extraOutletLayer is not null) {
                this._gv.extraOutletFile = extraOutletFile;
                var basinIndex = await this._gv.topo.getIndex(extraOutletLayer, Topology._SUBBASIN);
                var resIndex = await this._gv.topo.getIndex(extraOutletLayer, Topology._RES);
                var ptsrcIndex = await this._gv.topo.getIndex(extraOutletLayer, Topology._PTSOURCE);
                if (basinIndex >= 0 && resIndex >= 0 && ptsrcIndex >= 0) {
                    var extraPointSources = false;
                    await QueuedTask.Run(() => {
                        using (RowCursor cursor = extraOutletLayer.Search()) {
                            while (cursor.MoveNext()) {
                                Feature point = (Feature)cursor.Current;
                                if (Convert.ToInt32(point[resIndex]) == 1) {
                                    this.extraReservoirBasins.Add(Convert.ToInt32(point[basinIndex]));
                                } else if (Convert.ToInt32(point[ptsrcIndex]) == 1) {
                                    extraPointSources = true;
                                }
                            }
                        }
                    });
                    this.checkAddPoints.Checked = extraPointSources;
                }
            } else {
                this._gv.extraOutletFile = "";
            }
        }

        // Write delineation data to project file.
        public void saveProj() {
            int snapThreshold;
            int numCells;
            var proj = this._gv.proj;
            var title = this._gv.projName;
            proj.writeEntryBool(title, "delin/existingWshed", this._gv.existingWshed);
            // grid model not official in version >= 1.4 , so normally keep invisible
            //if (this.useGrid.isVisible()) {
            //    proj.writeEntry(title, "delin/useGridModel", this._gv.useGridModel);
            //    proj.writeEntry(title, "delin/gridSize", this.GridSize.value());
            //}
            proj.writeEntry(title, "delin/net", Utils.relativise(this._gv.streamFile, this._gv.projDir));
            proj.writeEntry(title, "delin/wshed", Utils.relativise(this._gv.wshedFile, this._gv.projDir));
            proj.writeEntry(title, "delin/DEM", Utils.relativise(this._gv.demFile, this._gv.projDir));
            proj.writeEntryBool(title, "delin/useOutlets", this.useOutlets.Checked);
            proj.writeEntry(title, "delin/outlets", Utils.relativise(this._gv.outletFile, this._gv.projDir));
            proj.writeEntry(title, "delin/extraOutlets", Utils.relativise(this._gv.extraOutletFile, this._gv.projDir));
            proj.writeEntry(title, "delin/burn", Utils.relativise(this._gv.burnFile, this._gv.projDir));
            try {
                numCells = Convert.ToInt32(this.numCells.Text);
            }
            catch (Exception) {
                numCells = 0;
            }
            proj.writeEntry(title, "delin/verticalUnits", this._gv.verticalUnits);
            proj.writeNumEntry(title, "delin/threshold", numCells);
            try {
                snapThreshold = Convert.ToInt32(this.snapThreshold.Text);
            }
            catch (Exception) {
                snapThreshold = 300;
            }
            proj.writeNumEntry(title, "delin/snapThreshold", snapThreshold);
        }

        private void DelinForm_Load(object sender, EventArgs e) {
            this.run();
        }


        private void showTaudem_CheckedChanged(object sender, EventArgs e) {
            // removed this as setting focus fails and SendKeys not allowed because 'Not handling Windows messages'
            // does not seem necessary anyway
            //var ok = FrameworkApplication.Current.MainWindow.Focus();
            //if (!ok) {
            //    Utils.error("Failed to set focus", this._gv.isBatch);
            //}
            //if (this.showTaudem.Checked) {
            //    SendKeys.Send("^%(S)");
            //} else {
            //    SendKeys.Send("^%(C)");
            //}
            //var tool = FrameworkApplication.ActiveTool;
            //var tab = FrameworkApplication.ActiveTab;
            //Utils.information("Current tool is " + tool + ".", this._gv.isBatch);
            //Utils.information("Current tab is " + tab + ".", this._gv.isBatch);
        }
    }




    // Data about grid cell.
    public class GridData
    {

        public object area;

        public int downCol;

        public int downNum;

        public int downRow;

        public object drainArea;

        public int incount;

        public object maxAcc;

        public object maxCol;

        public object maxRow;

        public object num;

        public int outlet;

        public GridData(
            int num,
            int area,
            double drainArea,
            int maxAcc,
            int maxRow,
            int maxCol) {
            //# PolygonId of this grid cell
            this.num = num;
            //# PolygonId of downstream grid cell
            this.downNum = -1;
            //# Row in storeGrid of downstream grid cell
            this.downRow = -1;
            //# Column in storeGrid of downstream grid cell
            this.downCol = -1;
            //# area of this grid cell in number cells in accumulation grid
            this.area = area;
            //# area being drained in sq km to start of stream in this grid cell
            this.drainArea = drainArea;
            //# accumulation at maximum accumulation point
            this.maxAcc = maxAcc;
            //# Row in accumulation grid of maximum accumulation point
            this.maxRow = maxRow;
            //# Column in accumulation grid of maximum accumulation point
            this.maxCol = maxCol;
            //# polygonId of outlet cell
            this.outlet = -1;
            //# number of cells draining directly to this one (so zero means a leaf cell)
            this.incount = 0;
        }
    }

}
