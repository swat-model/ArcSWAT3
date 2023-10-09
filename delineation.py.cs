
using Optional = typing.Optional;

using Tuple = typing.Tuple;

using Dict = typing.Dict;

using Set = typing.Set;

using List = typing.List;

using Any = typing.Any;

using TYPE_CHECKING = typing.TYPE_CHECKING;

using cast = typing.cast;

using Qt = PyQt5.QtCore.Qt;

using pyqtSignal = PyQt5.QtCore.pyqtSignal;

using QFileInfo = PyQt5.QtCore.QFileInfo;

using QObject = PyQt5.QtCore.QObject;

using QSettings = PyQt5.QtCore.QSettings;

using QVariant = PyQt5.QtCore.QVariant;

using QIntValidator = PyQt5.QtGui.QIntValidator;

using QDoubleValidator = PyQt5.QtGui.QDoubleValidator;

using QColor = PyQt5.QtGui.QColor;

using QMessageBox = PyQt5.QtWidgets.QMessageBox;

using os;

using glob;

using shutil;

using math;

using subprocess;

using time;

using gdal = osgeo.gdal;

using ogr = osgeo.ogr;

using traceback;

using DelineationDialog = delineationdialog.DelineationDialog;

using TauDEMUtils = TauDEMUtils.TauDEMUtils;

using Utils = Utils.Utils;

using fileWriter = Utils.fileWriter;

using FileTypes = Utils.FileTypes;

using Topology = topology.Topology;

using OutletsDialog = outletsdialog.OutletsDialog;

using SelectSubbasins = selectsubs.SelectSubbasins;

using Parameters = parameters.Parameters;

using System.Collections.Generic;

using System;

using System.Diagnostics;

using System.Linq;

public static class delineation {
    
    static delineation() {
        @"
/***************************************************************************
 QSWAT
                                 A QGIS plugin
 Create SWAT inputs
                              -------------------
        begin                : 2014-07-18
        copyright            : (C) 2014 by Chris George
        email                : cgeorge@mcmaster.ca
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/
";
    }
    
    public static object QgsLayerTree = Any;
    
    public static object QgsRasterLayer = Any;
    
    public static object QgsMapTool = Any;
    
    public static object QgsPointXY = Any;
    
    public static object QgsVectorLayer = Any;
    
    public static object QgsLayerTreeGroup = Any;
    
    public static object Transform = Dict[@int,float];
    
    // Data about grid cell.
    public class GridData {
        
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
    
    // Do watershed delineation.
    public class Delineation
        : QObject {
        
        public object _dlg;
        
        public object _gv;
        
        public object _iface;
        
        public object _odlg;
        
        public double areaOfCell;
        
        public bool changing;
        
        public bool delineationFinishedOK;
        
        public object demHeight;
        
        public object demWidth;
        
        public object drawOutletLayer;
        
        public List<int> dX;
        
        public List<int> dY;
        
        public object extraReservoirBasins;
        
        public bool finishHasRun;
        
        public bool isDelineated;
        
        public object mapTool;
        
        public object sizeX;
        
        public object sizeY;
        
        public bool snapErrors;
        
        public string snapFile;
        
        public bool thresholdChanged;
        
        public Delineation(object gv, bool isDelineated) {
            this._gv = gv;
            this._iface = gv.iface;
            this._dlg = DelineationDialog();
            this._dlg.setWindowFlags(this._dlg.windowFlags() & ~Qt.WindowContextHelpButtonHint & Qt.WindowMinimizeButtonHint);
            this._dlg.move(this._gv.delineatePos);
            //# when a snap file is created this is set to the file path
            this.snapFile = "";
            //# when not all points are snapped this is set True so snapping can be rerun
            this.snapErrors = false;
            this._odlg = OutletsDialog();
            this._odlg.setWindowFlags(this._odlg.windowFlags() & ~Qt.WindowContextHelpButtonHint);
            this._odlg.move(this._gv.outletsPos);
            //# Qgs vector layer for drawing inlet/outlet points
            this.drawOutletLayer = null;
            //# depends on DEM height and width and also on choice of area units
            this.areaOfCell = 0.0;
            //# Width of DEM as number of cells
            this.demWidth = 0;
            //# Height of DEM cell as number of cells
            this.demHeight = 0;
            //# Width of DEM cell in metres
            this.sizeX = 0.0;
            //# Height of DEM cell in metres
            this.sizeY = 0.0;
            //# flag to prevent infinite recursion between number of cells and area
            this.changing = false;
            //# basins selected for reservoirs
            this.extraReservoirBasins = new HashSet<object>();
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
        
        // Set connections to controls; read project delineation data.
        public virtual object init() {
            var settings = QSettings();
            try {
                this._dlg.numProcesses.setValue(Convert.ToInt32(settings.value("/QSWAT/NumProcesses")));
            } catch (Exception) {
                this._dlg.numProcesses.setValue(8);
            }
            this._dlg.selectDemButton.clicked.connect(this.btnSetDEM);
            this._dlg.checkBurn.stateChanged.connect(this.changeBurn);
            this._dlg.useGrid.stateChanged.connect(this.changeUseGrid);
            this._dlg.burnButton.clicked.connect(this.btnSetBurn);
            this._dlg.selectOutletsButton.clicked.connect(this.btnSetOutlets);
            this._dlg.selectWshedButton.clicked.connect(this.btnSetWatershed);
            this._dlg.selectNetButton.clicked.connect(this.btnSetStreams);
            this._dlg.selectExistOutletsButton.clicked.connect(this.btnSetOutlets);
            this._dlg.delinRunButton1.clicked.connect(this.runTauDEM1);
            this._dlg.delinRunButton2.clicked.connect(this.runTauDEM2);
            this._dlg.tabWidget.currentChanged.connect(this.changeExisting);
            this._dlg.existRunButton.clicked.connect(this.runExisting);
            this._dlg.useOutlets.stateChanged.connect(this.changeUseOutlets);
            this._dlg.drawOutletsButton.clicked.connect(this.drawOutlets);
            this._dlg.selectOutletsInteractiveButton.clicked.connect(this.selectOutlets);
            this._dlg.snapReviewButton.clicked.connect(this.snapReview);
            this._dlg.selectSubButton.clicked.connect(this.selectMergeSubbasins);
            this._dlg.mergeButton.clicked.connect(this.mergeSubbasins);
            this._dlg.selectResButton.clicked.connect(this.selectReservoirs);
            this._dlg.addButton.clicked.connect(this.addReservoirs);
            this._dlg.taudemHelpButton.clicked.connect(TauDEMUtils.taudemHelp);
            this._dlg.OKButton.clicked.connect(this.finishDelineation);
            this._dlg.cancelButton.clicked.connect(this.doClose);
            this._dlg.numCells.setValidator(QIntValidator());
            this._dlg.numCells.textChanged.connect(this.setArea);
            this._dlg.area.textChanged.connect(this.setNumCells);
            this._dlg.area.setValidator(QDoubleValidator());
            this._dlg.areaUnitsBox.addItem(Parameters._SQKM);
            this._dlg.areaUnitsBox.addItem(Parameters._HECTARES);
            this._dlg.areaUnitsBox.addItem(Parameters._SQMETRES);
            this._dlg.areaUnitsBox.addItem(Parameters._SQMILES);
            this._dlg.areaUnitsBox.addItem(Parameters._ACRES);
            this._dlg.areaUnitsBox.addItem(Parameters._SQFEET);
            this._dlg.areaUnitsBox.activated.connect(this.changeAreaOfCell);
            this._dlg.horizontalCombo.addItem(Parameters._METRES);
            this._dlg.horizontalCombo.addItem(Parameters._FEET);
            this._dlg.horizontalCombo.addItem(Parameters._DEGREES);
            this._dlg.horizontalCombo.addItem(Parameters._UNKNOWN);
            this._dlg.verticalCombo.addItem(Parameters._METRES);
            this._dlg.verticalCombo.addItem(Parameters._FEET);
            this._dlg.verticalCombo.addItem(Parameters._CM);
            this._dlg.verticalCombo.addItem(Parameters._MM);
            this._dlg.verticalCombo.addItem(Parameters._INCHES);
            this._dlg.verticalCombo.addItem(Parameters._YARDS);
            // set vertical unit default to metres
            this._dlg.verticalCombo.setCurrentIndex(this._dlg.verticalCombo.findText(Parameters._METRES));
            this._dlg.verticalCombo.activated.connect(this.setVerticalUnits);
            this._dlg.snapThreshold.setValidator(QIntValidator());
            this._odlg.resumeButton.clicked.connect(this.resumeDrawing);
            this.readProj();
            this.setMergeResGroups();
            this.thresholdChanged = false;
            this.checkMPI();
            // allow for cancellation without being considered an error
            this.delineationFinishedOK = true;
            // Prevent annoying "error 4 .shp not recognised" messages.
            // These should become exceptions but instead just disappear.
            // Safer in any case to raise exceptions if something goes wrong.
            gdal.UseExceptions();
            ogr.UseExceptions();
        }
        
        // Allow merging of subbasins and 
        //         adding of reservoirs and point sources if delineation complete.
        //         
        public virtual object setMergeResGroups() {
            this._dlg.mergeGroup.setEnabled(this.isDelineated);
            this._dlg.addResGroup.setEnabled(this.isDelineated);
        }
        
        // Do delineation; check done and save topology data.  Return 1 if delineation done and no errors, 2 if not delineated and nothing done, else 0.
        public virtual int run() {
            this.init();
            this._dlg.show();
            if (this._gv.useGridModel) {
                this._dlg.useGrid.setChecked(true);
                this._dlg.GridBox.setChecked(true);
            } else {
                this._dlg.useGrid.setVisible(false);
                this._dlg.GridBox.setVisible(false);
                this._dlg.GridSize.setVisible(false);
                this._dlg.GridSizeLabel.setVisible(false);
            }
            var result = this._dlg.exec_();
            this._gv.delineatePos = this._dlg.pos();
            if (this.delineationFinishedOK) {
                if (this.finishHasRun) {
                    this._gv.writeMasterProgress(1, 0);
                    return 1;
                } else {
                    // nothing done
                    return 2;
                }
            }
            this._gv.writeMasterProgress(0, 0);
            return 0;
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
        public virtual object checkMPI() {
            var dll = "msmpi.dll";
            var dummy = "msmpi_dll";
            var dllPath = QSWATUtils.join(this._gv.TauDEMDir, dll);
            var dummyPath = QSWATUtils.join(this._gv.TauDEMDir, dummy);
            // tried various methods here.  
            //'where msmpi.dll' succeeds if it was there and is moved or renamed - cached perhaps?
            // isfile fails similarly
            //'where mpiexec' always fails because when run interactively the path does not include the MPI directory
            // so just check for existence of mpiexec.exe and assume user will not leave msmpi.dll around
            // if MPI is installed and then uninstalled
            if (os.path.isfile(this._gv.mpiexecPath)) {
                QSWATUtils.loginfo("mpiexec found");
                // MPI is on the path; rename the local dll if necessary
                if (os.path.exists(dllPath)) {
                    if (os.path.exists(dummyPath)) {
                        os.remove(dllPath);
                        QSWATUtils.loginfo("dll removed");
                    } else {
                        os.rename(dllPath, dummyPath);
                        QSWATUtils.loginfo("dll renamed");
                    }
                }
            } else {
                QSWATUtils.loginfo("mpiexec not found");
                // we don't have MPI on the path; rename the local dummy if necessary
                if (os.path.exists(dllPath)) {
                    return;
                } else if (os.path.exists(dummyPath)) {
                    os.rename(dummyPath, dllPath);
                    QSWATUtils.loginfo("dummy renamed");
                } else {
                    QSWATUtils.error("Cannot find executable mpiexec in the system or {0} in {1}: TauDEM functions will not run.  Install MPI or reinstall QSWAT.".format(dll, this._gv.TauDEMDir), this._gv.isBatch);
                }
            }
        }
        
        // 
        //         Finish delineation.
        //         
        //         Checks stream reaches and watersheds defined, sets DEM attributes, 
        //         checks delineation is complete, calculates flow distances,
        //         runs topology setup.  Sets delineationFinishedOK to true if all completed successfully.
        //         
        public virtual object finishDelineation() {
            object extraOutletLayer;
            object outletLayer;
            object wshedLayer;
            this.delineationFinishedOK = false;
            this.finishHasRun = true;
            var root = QgsProject.instance().layerTreeRoot();
            var layers = root.findLayers();
            object streamLayer = null;
            if (!this._gv.existingWshed && this._gv.useGridModel) {
                var treeLayer = QSWATUtils.getLayerByLegend(FileTypes.legend(FileTypes._GRIDSTREAMS), layers);
                if (treeLayer is not null) {
                    streamLayer = treeLayer.layer();
                }
            } else {
                streamLayer = QSWATUtils.getLayerByFilename(layers, this._gv.streamFile, FileTypes._STREAMS, null, null, null)[0];
            }
            if (streamLayer is null) {
                if (this._gv.existingWshed) {
                    QSWATUtils.error("Stream reaches layer not found.", this._gv.isBatch);
                } else if (this._gv.useGridModel) {
                    QSWATUtils.error("Grid stream reaches layer not found.", this._gv.isBatch);
                } else {
                    QSWATUtils.error("Stream reaches layer not found: have you run TauDEM?", this._gv.isBatch);
                }
                return;
            }
            if (!this._gv.existingWshed && this._gv.useGridModel) {
                var wshedTreeLayer = QSWATUtils.getLayerByLegend(QSWATUtils._GRIDLEGEND, layers);
                if (wshedTreeLayer is null) {
                    QSWATUtils.error("Grid layer not found.", this._gv.isBatch);
                    return;
                }
                wshedLayer = wshedTreeLayer.layer();
            } else {
                var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
                wshedLayer = QSWATUtils.getLayerByFilename(layers, this._gv.wshedFile, ft, null, null, null)[0];
                if (wshedLayer is null) {
                    if (this._gv.existingWshed) {
                        QSWATUtils.error("Watershed layer not found.", this._gv.isBatch);
                    } else {
                        QSWATUtils.error("Watershed layer not found: have you run TauDEM?", this._gv.isBatch);
                    }
                    return;
                }
            }
            Debug.Assert(wshedLayer is not null);
            // this may be None
            if (this._gv.outletFile == "") {
                outletLayer = null;
            } else {
                outletLayer = QSWATUtils.getLayerByFilename(layers, this._gv.outletFile, FileTypes._OUTLETS, null, null, null)[0];
            }
            var demLayer = QSWATUtils.getLayerByFilename(layers, this._gv.demFile, FileTypes._DEM, null, null, null)[0];
            if (demLayer is null) {
                QSWATUtils.error("DEM layer not found: have you removed it?", this._gv.isBatch);
                return;
            }
            if (!this.setDimensions(cast(QgsRasterLayer, demLayer))) {
                return;
            }
            if (!this._gv.useGridModel && this._gv.basinFile == "") {
                // must have merged some subbasins: recreate the watershed grid
                demLayer = QSWATUtils.getLayerByFilename(layers, this._gv.demFile, FileTypes._DEM, null, null, null)[0];
                if (!demLayer) {
                    QSWATUtils.error("Cannot find DEM layer for file {0}".format(this._gv.demFile), this._gv.isBatch);
                    return;
                }
                this._gv.basinFile = this.createBasinFile(this._gv.wshedFile, cast(QgsRasterLayer, demLayer), root);
                if (this._gv.basinFile == "") {
                    return;
                    // QSWATUtils.loginfo('Recreated watershed grid as {0}'.format(self._gv.basinFile))
                }
            }
            this.saveProj();
            if (this.checkDEMProcessed()) {
                if (this._gv.extraOutletFile != "") {
                    extraOutletLayer = QSWATUtils.getLayerByFilename(layers, this._gv.extraOutletFile, FileTypes._OUTLETS, null, null, null)[0];
                } else {
                    extraOutletLayer = null;
                }
                if (!this._gv.existingWshed && !this._gv.useGridModel) {
                    this.progress("Tributary channel lengths ...");
                    var threshold = this._gv.topo.makeStreamOutletThresholds(this._gv, root);
                    if (threshold > 0) {
                        var demBase = os.path.splitext(this._gv.demFile)[0];
                        this._gv.distFile = demBase + "dist.tif";
                        // threshold is already double maximum ad8 value, so values anywhere near it can only occur at subbasin outlets; 
                        // use fraction of it to avoid any rounding problems
                        var ok = TauDEMUtils.runDistanceToStreams(this._gv.pFile, this._gv.hd8File, this._gv.distFile, Convert.ToInt32(threshold * 0.9).ToString(), this._dlg.numProcesses.value(), this._dlg.taudemOutput, mustRun: this.thresholdChanged);
                        if (!ok) {
                            this.cleanUp(3);
                            return;
                        }
                    } else {
                        // Probably using existing watershed but switched tabs in delineation form
                        this._gv.existingWshed = true;
                    }
                }
                var recalculate = this._gv.existingWshed && this._dlg.recalcButton.isChecked();
                this.progress("Constructing topology ...");
                this._gv.isBig = this._gv.useGridModel && cast(QgsVectorLayer, wshedLayer).featureCount() > 100000 || this._gv.forTNC;
                QSWATUtils.loginfo("isBig is {0}".format(this._gv.isBig));
                if (this._gv.topo.setUp(demLayer, streamLayer, wshedLayer, outletLayer, extraOutletLayer, this._gv.db, this._gv.existingWshed, recalculate, this._gv.useGridModel, true)) {
                    if (!this._gv.topo.inletLinks) {
                        // no inlets, so no need to expand subbasins layer legend
                        var treeWshedLayer = root.findLayer(wshedLayer.id());
                        Debug.Assert(treeWshedLayer is not null);
                        treeWshedLayer.setExpanded(false);
                    }
                    this.progress("Writing Reach table ...");
                    streamLayer = this._gv.topo.writeReachTable(streamLayer, this._gv);
                    if (!streamLayer) {
                        return;
                    }
                    this.progress("Writing MonitoringPoint table ...");
                    this._gv.topo.writeMonitoringPointTable(demLayer, streamLayer);
                    this.delineationFinishedOK = true;
                    this.doClose();
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
                return this._gv.useGridModel || QSWATUtils.isUpToDate(this._gv.wshedFile, this._gv.basinFile);
            }
            if (this._gv.useGridModel) {
                return QSWATUtils.isUpToDate(this._gv.slopeFile, this._gv.wshedFile);
            } else {
                return QSWATUtils.isUpToDate(this._gv.demFile, this._gv.wshedFile);
            }
        }
        
        // Open and load DEM; set default threshold.
        public virtual object btnSetDEM() {
            var root = QgsProject.instance().layerTreeRoot();
            (demFile, demMapLayer) = QSWATUtils.openAndLoadFile(root, FileTypes._DEM, this._dlg.selectDem, this._gv.sourceDir, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (demFile && demMapLayer) {
                this._gv.demFile = demFile;
                this.setDefaultNumCells(cast(QgsRasterLayer, demMapLayer));
                // warn if large DEM
                var numCells = this.demWidth * this.demHeight;
                if (numCells > 4000000.0) {
                    var millions = Convert.ToInt32(numCells / 1000000.0);
                    this._iface.messageBar().pushMessage("Large DEM", "This DEM has over {0} million cells and could take some time to process.  Be patient!".format(millions), level: Qgis.Warning, duration: 20);
                }
                // hillshade waste of (a lot of) time for TNC DEMs
                if (!this._gv.forTNC) {
                    this.addHillshade(demFile, root, cast(QgsRasterLayer, demMapLayer), this._gv);
                }
            }
        }
        
        //  Create hillshade layer and load.
        [staticmethod]
        public static object addHillshade(string demFile, object root, object demMapLayer, object gv) {
            var hillshadeFile = os.path.split(demFile)[0] + "/hillshade.tif";
            if (!QSWATUtils.isUpToDate(demFile, hillshadeFile)) {
                // run gdaldem to generate hillshade.tif
                (ok, path) = QSWATUtils.removeLayerAndFiles(hillshadeFile, root);
                if (!ok) {
                    QSWATUtils.error("Failed to remove old hillshade file {0}: try repeating last click, else remove manually.".format(path), gv.isBatch);
                    return;
                }
                var command = "gdaldem.exe hillshade -compute_edges -z 5 \"{0}\" \"{1}\"".format(demFile, hillshadeFile);
                var proc = subprocess.run(command, shell: true, stdout: subprocess.PIPE, stderr: subprocess.STDOUT, universal_newlines: true);
                QSWATUtils.loginfo("Creating hillshade ...");
                QSWATUtils.loginfo(command);
                Debug.Assert(proc is not null);
                QSWATUtils.loginfo(proc.stdout);
                if (!os.path.exists(hillshadeFile)) {
                    QSWATUtils.information("Failed to create hillshade file {0}".format(hillshadeFile), gv.isBatch);
                    return;
                }
                QSWATUtils.copyPrj(demFile, hillshadeFile);
            }
            // make dem active layer and add hillshade above it
            // demLayer allowed to be None for batch running
            if (demMapLayer) {
                var demLayer = root.findLayer(demMapLayer.id());
                var hillMapLayer = QSWATUtils.getLayerByFilename(root.findLayers(), hillshadeFile, FileTypes._HILLSHADE, gv, demLayer, QSWATUtils._WATERSHED_GROUP_NAME)[0];
                if (!hillMapLayer) {
                    QSWATUtils.information("Failed to load hillshade file {0}".format(hillshadeFile), gv.isBatch);
                    return;
                }
                Debug.Assert(hillMapLayer is QgsRasterLayer);
                // compress legend entry
                var hillTreeLayer = root.findLayer(hillMapLayer.id());
                Debug.Assert(hillTreeLayer is not null);
                hillTreeLayer.setExpanded(false);
                hillMapLayer.renderer().setOpacity(0.4);
                hillMapLayer.triggerRepaint();
            }
        }
        
        // Open and load stream network to burn in.
        public virtual object btnSetBurn() {
            var root = QgsProject.instance().layerTreeRoot();
            (burnFile, burnLayer) = QSWATUtils.openAndLoadFile(root, FileTypes._BURN, this._dlg.selectBurn, this._gv.sourceDir, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (burnFile && burnLayer) {
                Debug.Assert(burnLayer is QgsVectorLayer);
                var fileType = QgsWkbTypes.geometryType(burnLayer.dataProvider().wkbType());
                if (fileType != QgsWkbTypes.LineGeometry) {
                    QSWATUtils.error("Burn in file {0} is not a line shapefile".format(burnFile), this._gv.isBatch);
                } else {
                    this._gv.burnFile = burnFile;
                }
            }
        }
        
        // Open and load inlets/outlets shapefile.
        public virtual object btnSetOutlets() {
            object box;
            var root = QgsProject.instance().layerTreeRoot();
            if (this._gv.existingWshed) {
                Debug.Assert(this._dlg.tabWidget.currentIndex() == 1);
                box = this._dlg.selectExistOutlets;
            } else {
                Debug.Assert(this._dlg.tabWidget.currentIndex() == 0);
                box = this._dlg.selectOutlets;
                this.thresholdChanged = true;
            }
            var ft = this._gv.isHUC || this._gv.isHAWQS ? FileTypes._OUTLETSHUC : FileTypes._OUTLETS;
            (outletFile, outletLayer) = QSWATUtils.openAndLoadFile(root, ft, box, this._gv.shapesDir, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (outletFile && outletLayer) {
                Debug.Assert(outletLayer is QgsVectorLayer);
                this.snapFile = "";
                this._dlg.snappedLabel.setText("");
                var fileType = QgsWkbTypes.geometryType(outletLayer.dataProvider().wkbType());
                if (fileType != QgsWkbTypes.PointGeometry) {
                    QSWATUtils.error("Inlets/outlets file {0} is not a point shapefile".format(outletFile), this._gv.isBatch);
                } else {
                    this._gv.outletFile = outletFile;
                }
            }
        }
        
        // Open and load existing watershed shapefile.
        public virtual object btnSetWatershed() {
            var root = QgsProject.instance().layerTreeRoot();
            (wshedFile, wshedLayer) = QSWATUtils.openAndLoadFile(root, FileTypes._EXISTINGWATERSHED, this._dlg.selectWshed, this._gv.sourceDir, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (wshedFile && wshedLayer) {
                Debug.Assert(wshedLayer is QgsVectorLayer);
                var fileType = QgsWkbTypes.geometryType(wshedLayer.dataProvider().wkbType());
                if (fileType != QgsWkbTypes.PolygonGeometry) {
                    QSWATUtils.error("Subbasins file {0} is not a polygon shapefile".format(this._dlg.selectWshed.text()), this._gv.isBatch);
                } else {
                    this._gv.wshedFile = wshedFile;
                }
            }
        }
        
        // Open and load existing stream reach shapefile.
        public virtual object btnSetStreams() {
            var root = QgsProject.instance().layerTreeRoot();
            (streamFile, streamLayer) = QSWATUtils.openAndLoadFile(root, FileTypes._STREAMS, this._dlg.selectNet, this._gv.sourceDir, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (streamFile && streamLayer) {
                Debug.Assert(streamLayer is QgsVectorLayer);
                var fileType = QgsWkbTypes.geometryType(streamLayer.dataProvider().wkbType());
                if (fileType != QgsWkbTypes.LineGeometry) {
                    QSWATUtils.error("Stream reaches file {0} is not a line shapefile".format(this._dlg.selectNet.text()), this._gv.isBatch);
                } else {
                    this._gv.streamFile = streamFile;
                }
            }
        }
        
        // Run Taudem to create stream reach network.
        public virtual object runTauDEM1() {
            this.runTauDEM(null, false);
        }
        
        // Run TauDEM to create watershed shapefile.
        public virtual object runTauDEM2() {
            // first remove any existing shapesDir inlets/outlets file as will
            // probably be inconsistent with new subbasins
            var root = QgsProject.instance().layerTreeRoot();
            QSWATUtils.removeLayerByLegend(QSWATUtils._EXTRALEGEND, root.findLayers());
            this._gv.extraOutletFile = "";
            this.extraReservoirBasins.clear();
            if (!this._dlg.useOutlets.isChecked()) {
                this.runTauDEM(null, true);
            } else {
                var outletFile = this._dlg.selectOutlets.text();
                if (outletFile == "" || !os.path.exists(outletFile)) {
                    QSWATUtils.error("Please select an inlets/outlets file", this._gv.isBatch);
                    return;
                }
                this.runTauDEM(outletFile, true);
            }
        }
        
        // Change between using existing and delineating watershed.
        public virtual object changeExisting() {
            var tab = this._dlg.tabWidget.currentIndex();
            if (tab > 1) {
                // DEM properties or TauDEM output
                return;
            }
            this._gv.existingWshed = tab == 1;
        }
        
        // Run TauDEM.
        public virtual object runTauDEM(object outletFile, bool makeWshed) {
            object subLayer;
            object ad8File;
            object delineationDem;
            this.delineationFinishedOK = false;
            var demFile = this._dlg.selectDem.text();
            if (demFile == "" || !os.path.exists(demFile)) {
                QSWATUtils.error("Please select a DEM file", this._gv.isBatch);
                return;
            }
            this.isDelineated = false;
            this._gv.writeMasterProgress(0, 0);
            this.setMergeResGroups();
            this._gv.demFile = demFile;
            // find dem layer (or load it)
            var root = QgsProject.instance().layerTreeRoot();
            (demLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), this._gv.demFile, FileTypes._DEM, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!demLayer) {
                QSWATUtils.error("Cannot load DEM {0}".format(this._gv.demFile), this._gv.isBatch);
                return;
            }
            Debug.Assert(demLayer is QgsRasterLayer);
            // changing default number of cells 
            if (!this.setDefaultNumCells(demLayer)) {
                return;
            }
            (@base, suffix) = os.path.splitext(this._gv.demFile);
            // burn in if required
            if (this._dlg.checkBurn.isChecked()) {
                var burnFile = this._dlg.selectBurn.text();
                if (burnFile == "") {
                    QSWATUtils.error("Please select a burn in stream network shapefile", this._gv.isBatch);
                    return;
                }
                if (!os.path.exists(burnFile)) {
                    QSWATUtils.error("Cannot find burn in file {0}".format(burnFile), this._gv.isBatch);
                    return;
                }
                var burnedDemFile = os.path.splitext(this._gv.demFile)[0] + "_burned.tif";
                if (!QSWATUtils.isUpToDate(demFile, burnFile) || !QSWATUtils.isUpToDate(burnFile, burnedDemFile)) {
                    // just in case
                    (ok, path) = QSWATUtils.removeLayerAndFiles(burnedDemFile, root);
                    if (!ok) {
                        QSWATUtils.error("Failed to remove old burn file {0}: try repeating last click, else remove manually.".format(path), this._gv.isBatch);
                        this._dlg.setCursor(Qt.ArrowCursor);
                        return;
                    }
                    this.progress("Burning streams ...");
                    //burnRasterFile = self.streamToRaster(demLayer, burnFile, root)
                    //processing.runalg('saga:burnstreamnetworkintodem', demFile, burnRasterFile, burnMethod, burnEpsilon, burnedFile)
                    var burnDepth = this._gv.fromGRASS ? 25.0 : 50.0;
                    QSWATTopology.burnStream(burnFile, demFile, burnedDemFile, this._gv.verticalFactor, burnDepth, this._gv.isBatch);
                    if (!os.path.exists(burnedDemFile)) {
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
                if (this._gv.slopeFile.endswith("_burnedslp.tif")) {
                    var unburnedslp = this._gv.slopeFile.replace("_burnedslp.tif", "slp.tif");
                    if (os.path.isfile(unburnedslp)) {
                        this._gv.slopeFile = unburnedslp;
                    }
                }
                ad8File = @base + "ad8.tif";
                this._gv.outletFile = "";
                this._gv.streamFile = @base + "net.shp";
                this._gv.wshedFile = @base + "wshed.shp";
                this.createGridShapefile(demLayer, this._gv.pFile, ad8File, this._gv.basinFile);
                (streamLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), this._gv.streamFile, FileTypes._STREAMS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                if (!this._gv.topo.setUp0(demLayer, streamLayer, this._gv.verticalFactor)) {
                    this.cleanUp(-1);
                    return;
                }
                this.isDelineated = true;
                this.setMergeResGroups();
                this.saveProj();
                this.cleanUp(-1);
                return;
            }
            var numProcesses = this._dlg.numProcesses.value();
            var mpiexecPath = this._gv.mpiexecPath;
            if (numProcesses > 0 && (mpiexecPath == "" || !os.path.exists(mpiexecPath))) {
                QSWATUtils.information("Cannot find MPI program {0} so running TauDEM with just one process".format(mpiexecPath), this._gv.isBatch);
                numProcesses = 0;
                this._dlg.numProcesses.setValue(0);
            }
            QSettings().setValue("/QSWAT/NumProcesses", numProcesses.ToString());
            if (this._dlg.showTaudem.isChecked()) {
                this._dlg.tabWidget.setCurrentIndex(3);
            }
            this._dlg.setCursor(Qt.WaitCursor);
            this._dlg.taudemOutput.clear();
            var felFile = @base + "fel" + suffix;
            QSWATUtils.removeLayer(felFile, root);
            this.progress("PitFill ...");
            var ok = TauDEMUtils.runPitFill(delineationDem, felFile, numProcesses, this._dlg.taudemOutput);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            var sd8File = @base + "sd8" + suffix;
            var pFile = @base + "p" + suffix;
            QSWATUtils.removeLayer(sd8File, root);
            QSWATUtils.removeLayer(pFile, root);
            this.progress("D8FlowDir ...");
            ok = TauDEMUtils.runD8FlowDir(felFile, sd8File, pFile, numProcesses, this._dlg.taudemOutput);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            var slpFile = @base + "slp" + suffix;
            var angFile = @base + "ang" + suffix;
            QSWATUtils.removeLayer(slpFile, root);
            QSWATUtils.removeLayer(angFile, root);
            this.progress("DinfFlowDir ...");
            ok = TauDEMUtils.runDinfFlowDir(felFile, slpFile, angFile, numProcesses, this._dlg.taudemOutput);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            ad8File = @base + "ad8" + suffix;
            QSWATUtils.removeLayer(ad8File, root);
            this.progress("AreaD8 ...");
            ok = TauDEMUtils.runAreaD8(pFile, ad8File, null, null, numProcesses, this._dlg.taudemOutput, mustRun: this.thresholdChanged);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            var scaFile = @base + "sca" + suffix;
            QSWATUtils.removeLayer(scaFile, root);
            this.progress("AreaDinf ...");
            ok = TauDEMUtils.runAreaDinf(angFile, scaFile, null, numProcesses, this._dlg.taudemOutput, mustRun: this.thresholdChanged);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            var gordFile = @base + "gord" + suffix;
            var plenFile = @base + "plen" + suffix;
            var tlenFile = @base + "tlen" + suffix;
            QSWATUtils.removeLayer(gordFile, root);
            QSWATUtils.removeLayer(plenFile, root);
            QSWATUtils.removeLayer(tlenFile, root);
            this.progress("GridNet ...");
            ok = TauDEMUtils.runGridNet(pFile, plenFile, tlenFile, gordFile, null, numProcesses, this._dlg.taudemOutput, mustRun: this.thresholdChanged);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            var srcFile = @base + "src" + suffix;
            QSWATUtils.removeLayer(srcFile, root);
            this.progress("Threshold ...");
            if (this._gv.isBatch) {
                QSWATUtils.information("Delineation threshold: {0} cells".format(this._dlg.numCells.text()), true);
            }
            ok = TauDEMUtils.runThreshold(ad8File, srcFile, this._dlg.numCells.text(), numProcesses, this._dlg.taudemOutput, mustRun: this.thresholdChanged);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            var ordFile = @base + "ord" + suffix;
            var streamFile = @base + "net.shp";
            // if stream shapefile already exists and is a directory, set path to .shp
            streamFile = QSWATUtils.dirToShapefile(streamFile);
            var treeFile = @base + "tree.dat";
            var coordFile = @base + "coord.dat";
            var wFile = @base + "w" + suffix;
            QSWATUtils.removeLayer(ordFile, root);
            QSWATUtils.removeLayer(streamFile, root);
            QSWATUtils.removeLayer(wFile, root);
            this.progress("StreamNet ...");
            ok = TauDEMUtils.runStreamNet(felFile, pFile, ad8File, srcFile, null, ordFile, treeFile, coordFile, streamFile, wFile, numProcesses, this._dlg.taudemOutput, mustRun: this.thresholdChanged);
            if (!ok) {
                this.cleanUp(3);
                return;
            }
            // if stream shapefile is a directory, set path to .shp, since not done earlier if streamFile did not exist then
            streamFile = QSWATUtils.dirToShapefile(streamFile);
            // load stream network
            QSWATUtils.copyPrj(demFile, wFile);
            QSWATUtils.copyPrj(demFile, streamFile);
            root = QgsProject.instance().layerTreeRoot();
            // make demLayer (or hillshade if exists) active so streamLayer loads above it and below outlets
            // (or use Full HRUs layer if there is one)
            var fullHRUsLayer = QSWATUtils.getLayerByLegend(QSWATUtils._FULLHRUSLEGEND, root.findLayers());
            var hillshadeLayer = QSWATUtils.getLayerByLegend(QSWATUtils._HILLSHADELEGEND, root.findLayers());
            if (fullHRUsLayer is not null) {
                subLayer = fullHRUsLayer;
            } else if (hillshadeLayer is not null) {
                subLayer = hillshadeLayer;
            } else {
                subLayer = root.findLayer(demLayer.id());
            }
            (streamLayer, loaded) = QSWATUtils.getLayerByFilename(root.findLayers(), streamFile, FileTypes._STREAMS, this._gv, subLayer, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!streamLayer || !loaded) {
                this.cleanUp(-1);
                return;
            }
            Debug.Assert(streamLayer is QgsVectorLayer);
            this._gv.streamFile = streamFile;
            if (!makeWshed) {
                this.snapFile = "";
                this._dlg.snappedLabel.setText("");
                // initial run to enable placing of outlets, so finishes with load of stream network
                this._dlg.taudemOutput.append("------------------- TauDEM finished -------------------\n");
                this.saveProj();
                this.cleanUp(-1);
                return;
            }
            if (this._dlg.useOutlets.isChecked()) {
                Debug.Assert(outletFile is not null);
                var outletBase = os.path.splitext(outletFile)[0];
                var snapFile = outletBase + "_snap.shp";
                (outletLayer, loaded) = QSWATUtils.getLayerByFilename(root.findLayers(), outletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                if (!outletLayer) {
                    this.cleanUp(-1);
                    return;
                }
                Debug.Assert(outletLayer is QgsVectorLayer);
                this.progress("SnapOutletsToStreams ...");
                ok = this.createSnapOutletFile(outletLayer, streamLayer, outletFile, snapFile, root);
                if (!ok) {
                    this.cleanUp(-1);
                    return;
                }
                // replaced by snapping
                // outletMovedFile = outletBase + '_moved.shp'
                // QSWATUtils.removeLayer(outletMovedFile, li)
                // self.progress('MoveOutletsToStreams ...')
                // ok = TauDEMUtils.runMoveOutlets(pFile, srcFile, outletFile, outletMovedFile, numProcesses, self._dlg.taudemOutput, mustRun=self.thresholdChanged)
                // if not ok:
                //   self.cleanUp(3)
                //    return
                // repeat AreaD8, GridNet, Threshold and StreamNet with snapped outlets
                var mustRun = this.thresholdChanged || this.snapFile;
                QSWATUtils.removeLayer(ad8File, root);
                this.progress("AreaD8 ...");
                ok = TauDEMUtils.runAreaD8(pFile, ad8File, this.snapFile, null, numProcesses, this._dlg.taudemOutput, mustRun: mustRun);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                QSWATUtils.removeLayer(streamFile, root);
                this.progress("GridNet ...");
                ok = TauDEMUtils.runGridNet(pFile, plenFile, tlenFile, gordFile, this.snapFile, numProcesses, this._dlg.taudemOutput, mustRun: mustRun);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                QSWATUtils.removeLayer(srcFile, root);
                this.progress("Threshold ...");
                ok = TauDEMUtils.runThreshold(ad8File, srcFile, this._dlg.numCells.text(), numProcesses, this._dlg.taudemOutput, mustRun: mustRun);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                this.progress("StreamNet ...");
                ok = TauDEMUtils.runStreamNet(felFile, pFile, ad8File, srcFile, this.snapFile, ordFile, treeFile, coordFile, streamFile, wFile, numProcesses, this._dlg.taudemOutput, mustRun: mustRun);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                QSWATUtils.copyPrj(demFile, wFile);
                QSWATUtils.copyPrj(demFile, streamFile);
                root = QgsProject.instance().layerTreeRoot();
                // make demLayer (or hillshadelayer if exists) active so streamLayer loads above it and below outlets
                // (or use Full HRUs layer if there is one)
                if (fullHRUsLayer is not null) {
                    subLayer = fullHRUsLayer;
                } else if (hillshadeLayer is not null) {
                    subLayer = hillshadeLayer;
                } else {
                    subLayer = root.findLayer(demLayer.id());
                }
                (streamLayer, loaded) = QSWATUtils.getLayerByFilename(root.findLayers(), streamFile, FileTypes._STREAMS, this._gv, subLayer, QSWATUtils._WATERSHED_GROUP_NAME);
                if (!streamLayer || !loaded) {
                    this.cleanUp(-1);
                    return;
                }
                // check if stream network has only one feature
                Debug.Assert(streamLayer is QgsVectorLayer);
                if (streamLayer.featureCount() == 1) {
                    QSWATUtils.error("There is only one stream reach in your watershed, so you will only get one subbasin.  You need to reduce the threshold.", this._gv.isBatch);
                    this.cleanUp(-1);
                    return;
                }
            }
            this._dlg.taudemOutput.append("------------------- TauDEM finished -------------------\n");
            this._gv.pFile = pFile;
            this._gv.basinFile = wFile;
            if (this._dlg.checkBurn.isChecked()) {
                // need to make slope file from original dem
                var felNoburn = @base + "felnoburn" + suffix;
                QSWATUtils.removeLayer(felNoburn, root);
                this.progress("PitFill ...");
                ok = TauDEMUtils.runPitFill(demFile, felNoburn, numProcesses, this._dlg.taudemOutput);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                var slopeFile = @base + "slope" + suffix;
                var angleFile = @base + "angle" + suffix;
                QSWATUtils.removeLayer(slopeFile, root);
                QSWATUtils.removeLayer(angleFile, root);
                this.progress("DinfFlowDir ...");
                ok = TauDEMUtils.runDinfFlowDir(felNoburn, slopeFile, angleFile, numProcesses, this._dlg.taudemOutput);
                if (!ok) {
                    this.cleanUp(3);
                    return;
                }
                this._gv.slopeFile = slopeFile;
            } else {
                this._gv.slopeFile = slpFile;
            }
            this._gv.streamFile = streamFile;
            if (this._dlg.useOutlets.isChecked()) {
                Debug.Assert(outletFile is not null);
                this._gv.outletFile = outletFile;
            } else {
                this._gv.outletFile = "";
            }
            var wshedFile = @base + "wshed.shp";
            this.createWatershedShapefile(wFile, wshedFile, root);
            this._gv.wshedFile = wshedFile;
            if (this._dlg.GridBox.isChecked()) {
                this.createGridShapefile(demLayer, pFile, ad8File, wFile);
            }
            if (!this._gv.topo.setUp0(demLayer, streamLayer, this._gv.verticalFactor)) {
                this.cleanUp(-1);
                return;
            }
            this.isDelineated = true;
            this.setMergeResGroups();
            this.saveProj();
            this.cleanUp(-1);
        }
        
        // Do delineation from existing stream network and subbasins.
        public virtual object runExisting() {
            this.delineationFinishedOK = false;
            var demFile = this._dlg.selectDem.text();
            if (demFile == "" || !os.path.exists(demFile)) {
                QSWATUtils.error("Please select a DEM file", this._gv.isBatch);
                return;
            }
            this._gv.demFile = demFile;
            var wshedFile = this._dlg.selectWshed.text();
            if (wshedFile == "" || !os.path.exists(wshedFile)) {
                QSWATUtils.error("Please select a watershed shapefile", this._gv.isBatch);
                return;
            }
            var streamFile = this._dlg.selectNet.text();
            if (streamFile == "" || !os.path.exists(streamFile)) {
                QSWATUtils.error("Please select a streams shapefile", this._gv.isBatch);
                return;
            }
            var outletFile = this._dlg.selectExistOutlets.text();
            if (outletFile != "") {
                if (!os.path.exists(outletFile)) {
                    QSWATUtils.error("Cannot find inlets/outlets shapefile {0}".format(outletFile), this._gv.isBatch);
                    return;
                }
            }
            this.isDelineated = false;
            this.setMergeResGroups();
            // find layers (or load them)
            var root = QgsProject.instance().layerTreeRoot();
            (demLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), this._gv.demFile, FileTypes._DEM, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!demLayer) {
                QSWATUtils.error("Cannot load DEM {0}".format(this._gv.demFile), this._gv.isBatch);
                return;
            }
            Debug.Assert(demLayer is QgsRasterLayer);
            this.addHillshade(this._gv.demFile, root, demLayer, this._gv);
            (wshedLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), wshedFile, FileTypes._EXISTINGWATERSHED, this._gv, demLayer, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!wshedLayer) {
                QSWATUtils.error("Cannot load watershed shapefile {0}".format(wshedFile), this._gv.isBatch);
                return;
            }
            Debug.Assert(wshedLayer is QgsVectorLayer);
            (streamLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), streamFile, FileTypes._STREAMS, this._gv, wshedLayer, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!streamLayer) {
                QSWATUtils.error("Cannot load streams shapefile {0}".format(streamFile), this._gv.isBatch);
                return;
            }
            if (outletFile != "") {
                var ft = this._gv.isHUC || this._gv.isHAWQS ? FileTypes._OUTLETSHUC : FileTypes._OUTLETS;
                (outletLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), outletFile, ft, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                if (!outletLayer) {
                    QSWATUtils.error("Cannot load inlets/outlets shapefile {0}".format(outletFile), this._gv.isBatch);
                    return;
                }
            } else {
                object outletLayer = null;
            }
            // ready to start processing
            (@base, suffix) = os.path.splitext(this._gv.demFile);
            var numProcesses = this._dlg.numProcesses.value();
            QSettings().setValue("/QSWAT/NumProcesses", numProcesses.ToString());
            this._dlg.setCursor(Qt.WaitCursor);
            this._dlg.taudemOutput.clear();
            // create Dinf slopes
            var felFile = @base + "fel" + suffix;
            var slpFile = @base + "slp" + suffix;
            var angFile = @base + "ang" + suffix;
            QSWATUtils.removeLayer(slpFile, root);
            QSWATUtils.removeLayer(angFile, root);
            var willRun = !(QSWATUtils.isUpToDate(demFile, slpFile) && QSWATUtils.isUpToDate(demFile, angFile));
            if (willRun) {
                this.progress("DinfFlowDir ...");
                if (this._dlg.showTaudem.isChecked()) {
                    this._dlg.tabWidget.setCurrentIndex(3);
                }
                var ok = TauDEMUtils.runPitFill(demFile, felFile, numProcesses, this._dlg.taudemOutput);
                if (!ok) {
                    QSWATUtils.error("Cannot generate pitfilled file from dem {0}".format(demFile), this._gv.isBatch);
                    this.cleanUp(3);
                    return;
                }
                ok = TauDEMUtils.runDinfFlowDir(felFile, slpFile, angFile, numProcesses, this._dlg.taudemOutput);
                if (!ok) {
                    QSWATUtils.error("Cannot generate slope file from pitfilled dem {0}".format(felFile), this._gv.isBatch);
                    this.cleanUp(3);
                    return;
                }
                this.progress("DinfFlowDir done");
            }
            if (this._gv.useGridModel) {
                // set centroids and catchment outlets
                var basinIndex = this._gv.topo.getIndex(wshedLayer, QSWATTopology._POLYGONID);
                var outletIndex = this._gv.topo.getIndex(wshedLayer, QSWATTopology._OUTLET);
                if (basinIndex < 0) {
                    return;
                }
                foreach (var feature in wshedLayer.getFeatures()) {
                    var basin = feature[basinIndex];
                    var outlet = feature[outletIndex];
                    var centroid = feature.geometry().centroid().asPoint();
                    this._gv.topo.basinCentroids[basin] = (centroid.x(), centroid.y());
                    this._gv.topo.catchmentOutlets[basin] = outlet;
                }
            } else {
                // generate watershed raster
                var wFile = @base + "w" + suffix;
                if (!(QSWATUtils.isUpToDate(demFile, wFile) && QSWATUtils.isUpToDate(wshedFile, wFile))) {
                    this.progress("Generating watershed raster ...");
                    wFile = this.createBasinFile(wshedFile, demLayer, root);
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
            if (!this._gv.topo.setUp0(demLayer, streamLayer, this._gv.verticalFactor)) {
                return;
            }
            this.isDelineated = true;
            this.setMergeResGroups();
            this.cleanUp(-1);
        }
        
        // Set threshold number of cells to default of 1% of number in grid, 
        //         unless already set.
        //         
        public virtual bool setDefaultNumCells(object demLayer) {
            if (!this.setDimensions(demLayer)) {
                return false;
            }
            // set to default number of cells unless already set
            if (this._dlg.numCells.text() == "") {
                var numCells = this.demWidth * this.demHeight;
                var defaultNumCells = Convert.ToInt32(numCells * 0.01);
                this._dlg.numCells.setText(defaultNumCells.ToString());
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
        public virtual bool setDimensions(object demLayer) {
            object @string;
            object factor;
            object epsg;
            // can fail if demLayer is None or not projected
            try {
                if (this._gv.topo.crsProject is null) {
                    this._gv.topo.crsProject = demLayer.crs();
                }
                var units = demLayer.crs().mapUnits();
            } catch (Exception) {
                QSWATUtils.loginfo("Failure to read DEM units: {0}".format(traceback.format_exc()));
                return false;
            }
            QgsProject.instance().setCrs(demLayer.crs());
            var provider = demLayer.dataProvider();
            this._gv.xBlockSize = provider.xBlockSize();
            this._gv.yBlockSize = provider.yBlockSize();
            QSWATUtils.loginfo("DEM horizontal and vertical block sizes are {0} and {1}".format(this._gv.xBlockSize, this._gv.yBlockSize));
            var demFile = QSWATUtils.layerFileInfo(demLayer).absoluteFilePath();
            var demPrj = os.path.splitext(demFile)[0] + ".prj";
            var demPrjTxt = demPrj + ".txt";
            if (os.path.exists(demPrj) && !os.path.exists(demPrjTxt)) {
                var command = "gdalsrsinfo -p -o wkt \"{0}\" > \"{1}\"".format(demPrj, demPrjTxt);
                os.system(command);
            }
            if (os.path.exists(demPrjTxt)) {
                using (var txtFile = open(demPrjTxt)) {
                    this._dlg.textBrowser.setText(txtFile.read());
                }
            } else {
                this._dlg.textBrowser.setText(demLayer.crs().toWkt());
            }
            try {
                epsg = demLayer.crs().authid();
                QSWATUtils.loginfo(epsg);
                var rect = demLayer.extent();
                this._dlg.label.setText("Spatial reference: {0}".format(epsg));
                // epsg has format 'EPSG:N' where N is the EPSG number
                var startNum = epsg.find(":") + 1;
                if (this._gv.isBatch && startNum > 0) {
                    var demDataFile = QSWATUtils.join(this._gv.projDir, "dem_data.xml");
                    if (!os.path.exists(demDataFile)) {
                        var f = fileWriter(demDataFile);
                        f.writeLine("<demdata>");
                        f.writeLine("<epsg>{0}</epsg>".format(epsg[startNum]));
                        f.writeLine("<minx>{0}</minx>".format(rect.xMinimum()));
                        f.writeLine("<maxx>{0}</maxx>".format(rect.xMaximum()));
                        f.writeLine("<miny>{0}</miny>".format(rect.yMinimum()));
                        f.writeLine("<maxy>{0}</maxy>".format(rect.yMaximum()));
                        f.writeLine("</demdata>");
                        f.close();
                    }
                }
            } catch (Exception) {
                // fail gracefully
                epsg = "";
            }
            if (units == QgsUnitTypes.DistanceMeters) {
                factor = 1.0;
                this._dlg.horizontalCombo.setCurrentIndex(this._dlg.horizontalCombo.findText(Parameters._METRES));
                this._dlg.horizontalCombo.setEnabled(false);
            } else if (units == QgsUnitTypes.DistanceFeet) {
                factor = 0.3048;
                this._dlg.horizontalCombo.setCurrentIndex(this._dlg.horizontalCombo.findText(Parameters._FEET));
                this._dlg.horizontalCombo.setEnabled(false);
            } else {
                if (units == QgsUnitTypes.AngleDegrees) {
                    @string = "degrees";
                    this._dlg.horizontalCombo.setCurrentIndex(this._dlg.horizontalCombo.findText(Parameters._DEGREES));
                    this._dlg.horizontalCombo.setEnabled(false);
                } else {
                    @string = "unknown";
                    this._dlg.horizontalCombo.setCurrentIndex(this._dlg.horizontalCombo.findText(Parameters._DEGREES));
                    this._dlg.horizontalCombo.setEnabled(true);
                }
                QSWATUtils.information("WARNING: DEM does not seem to be projected: its units are " + @string, this._gv.isBatch);
                return false;
            }
            this.demWidth = demLayer.width();
            this.demHeight = demLayer.height();
            if (round(demLayer.rasterUnitsPerPixelX()) != round(demLayer.rasterUnitsPerPixelY())) {
                QSWATUtils.information("WARNING: DEM cells are not square: {0} x {1}".format(demLayer.rasterUnitsPerPixelX(), demLayer.rasterUnitsPerPixelY()), this._gv.isBatch);
            }
            this.sizeX = demLayer.rasterUnitsPerPixelX() * factor;
            this.sizeY = demLayer.rasterUnitsPerPixelY() * factor;
            this._dlg.sizeEdit.setText("{:.4G} x {:.4G}".format(this.sizeX, this.sizeY));
            this._dlg.sizeEdit.setReadOnly(true);
            this.setAreaOfCell();
            var areaM2 = float(this.sizeX * this.sizeY) / 10000.0;
            this._dlg.areaEdit.setText("{:.4G}".format(areaM2));
            this._dlg.areaEdit.setReadOnly(true);
            var extent = demLayer.extent();
            var north = extent.yMaximum();
            var south = extent.yMinimum();
            var east = extent.xMaximum();
            var west = extent.xMinimum();
            var topLeft = this._gv.topo.pointToLatLong(QgsPointXY(west, north));
            var bottomRight = this._gv.topo.pointToLatLong(QgsPointXY(east, south));
            var northll = topLeft.y();
            var southll = bottomRight.y();
            var eastll = bottomRight.x();
            var westll = topLeft.x();
            this._dlg.northEdit.setText(this.degreeString(northll));
            this._dlg.southEdit.setText(this.degreeString(southll));
            this._dlg.eastEdit.setText(this.degreeString(eastll));
            this._dlg.westEdit.setText(this.degreeString(westll));
            return true;
        }
        
        // Generate string showing degrees as decmal and as degrees minuts seconds.
        [staticmethod]
        public static string degreeString(double decDeg) {
            var deg = Convert.ToInt32(decDeg);
            var decMin = abs(decDeg - deg) * 60;
            var minn = Convert.ToInt32(decMin);
            var sec = Convert.ToInt32((decMin - minn) * 60);
            return "{0:.2F}{1} ({2}{1} {3}\' {4}\")".format(decDeg, chr(176), deg, minn, sec);
        }
        
        // Set area of 1 cell according to area units choice.
        public virtual object setAreaOfCell() {
            var areaSqM = float(this.sizeX * this.sizeY);
            if (this._dlg.areaUnitsBox.currentText() == Parameters._SQKM) {
                this.areaOfCell = areaSqM / 1000000.0;
            } else if (this._dlg.areaUnitsBox.currentText() == Parameters._HECTARES) {
                this.areaOfCell = areaSqM / 10000.0;
            } else if (this._dlg.areaUnitsBox.currentText() == Parameters._SQMETRES) {
                this.areaOfCell = areaSqM;
            } else if (this._dlg.areaUnitsBox.currentText() == Parameters._SQMILES) {
                this.areaOfCell = areaSqM / 2589988.1;
            } else if (this._dlg.areaUnitsBox.currentText() == Parameters._ACRES) {
                this.areaOfCell = areaSqM / 4046.8564;
            } else if (this._dlg.areaUnitsBox.currentText() == Parameters._SQFEET) {
                this.areaOfCell = areaSqM * 10.76391;
            }
        }
        
        // Set area of cell and update area threshold display.
        public virtual object changeAreaOfCell() {
            this.setAreaOfCell();
            this.setArea();
        }
        
        // Sets vertical units from combo box; sets corresponding factor to apply to elevations.
        public virtual object setVerticalUnits() {
            this._gv.verticalUnits = this._dlg.verticalCombo.currentText();
            this._gv.setVerticalFactor();
        }
        
        // Update area threshold display.
        public virtual object setArea() {
            if (this.changing) {
                return;
            }
            try {
                var numCells = float(this._dlg.numCells.text());
            } catch (Exception) {
                // not currently parsable - ignore
                return;
            }
            var area = numCells * this.areaOfCell;
            this.changing = true;
            this._dlg.area.setText("{0:.4G}".format(area));
            this.changing = false;
            this.thresholdChanged = true;
        }
        
        // Update number of cells threshold display.
        public virtual object setNumCells() {
            if (this.changing) {
                return;
            }
            // prevent division by zero
            if (this.areaOfCell == 0) {
                return;
            }
            try {
                var area = float(this._dlg.area.text());
            } catch (Exception) {
                // not currently parsable - ignore
                return;
            }
            var numCells = Convert.ToInt32(area / this.areaOfCell);
            this.changing = true;
            this._dlg.numCells.setText(numCells.ToString());
            this.changing = false;
            this.thresholdChanged = true;
        }
        
        // Make burn option available or not according to check box state.
        public virtual object changeBurn() {
            if (this._dlg.checkBurn.isChecked()) {
                this._dlg.selectBurn.setEnabled(true);
                this._dlg.burnButton.setEnabled(true);
                if (this._dlg.selectBurn.text() != "") {
                    this._gv.burnFile = this._dlg.selectBurn.text();
                }
            } else {
                this._dlg.selectBurn.setEnabled(false);
                this._dlg.burnButton.setEnabled(false);
                this._gv.burnFile = "";
            }
        }
        
        // Change use grid setting according to check box state.
        public virtual object changeUseGrid() {
            this._gv.useGridModel = this._dlg.useGrid.isChecked();
        }
        
        // Make outlets option available or not according to check box state.
        public virtual object changeUseOutlets() {
            if (this._dlg.useOutlets.isChecked()) {
                this._dlg.outletsWidget.setEnabled(true);
                this._dlg.selectOutlets.setEnabled(true);
                this._dlg.selectOutletsButton.setEnabled(true);
                if (this._dlg.selectOutlets.text() != "") {
                    this._gv.outletFile = this._dlg.selectOutlets.text();
                }
            } else {
                this._dlg.outletsWidget.setEnabled(false);
                this._dlg.selectOutlets.setEnabled(false);
                this._dlg.selectOutletsButton.setEnabled(false);
                this._gv.outletFile = "";
            }
            this.thresholdChanged = true;
        }
        
        // Allow user to create inlets/outlets in current shapefile 
        //         or a new one.
        //         
        public virtual object drawOutlets() {
            object drawCurrent;
            object result;
            this._odlg.widget.setEnabled(true);
            var canvas = this._iface.mapCanvas();
            this.mapTool = QgsMapToolEmitPoint(canvas);
            Debug.Assert(this.mapTool is not null);
            this.mapTool.canvasClicked.connect(this.getPoint);
            canvas.setMapTool(this.mapTool);
            // detect maptool change
            canvas.mapToolSet.connect(this.mapToolChanged);
            var root = QgsProject.instance().layerTreeRoot();
            var outletLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch);
            if (outletLayer) {
                // we have a current outlet layer - give user a choice 
                Debug.Assert(outletLayer is QgsVectorLayer);
                var msgBox = QMessageBox();
                msgBox.move(this._gv.selectOutletFilePos);
                msgBox.setWindowTitle("Select inlets/outlets file to draw on");
                var text = @"
            Select ""Current"" if you wish to draw new points in the 
            existing inlets/outlets layer, which is
            {0}.
            Select ""New"" if you wish to make a new inlets/outlets file.
            Select ""Cancel"" to abandon drawing.
            ".format(this._gv.outletFile);
                msgBox.setText(QSWATUtils.trans(text));
                var currentButton = msgBox.addButton(QSWATUtils.trans("Current"), QMessageBox.ActionRole);
                var newButton = msgBox.addButton(QSWATUtils.trans("New"), QMessageBox.ActionRole);
                msgBox.setStandardButtons(QMessageBox.Cancel);
                result = msgBox.exec_();
                this._gv.selectOutletFilePos = msgBox.pos();
                if (result == QMessageBox.Cancel) {
                    return;
                }
                drawCurrent = msgBox.clickedButton() == currentButton;
            } else {
                drawCurrent = false;
            }
            if (drawCurrent) {
                Debug.Assert(outletLayer is QgsVectorLayer);
                if (!this._iface.setActiveLayer(outletLayer)) {
                    QSWATUtils.error("Could not make inlets/outlets layer active", this._gv.isBatch);
                    return;
                }
                this.drawOutletLayer = outletLayer;
                Debug.Assert(this.drawOutletLayer is not null);
                this.drawOutletLayer.startEditing();
            } else {
                var drawOutletFile = QSWATUtils.join(this._gv.shapesDir, "drawoutlets.shp");
                // our outlet file may already be called drawoutlets.shp
                if (QSWATUtils.samePath(drawOutletFile, this._gv.outletFile)) {
                    drawOutletFile = QSWATUtils.join(this._gv.shapesDir, "drawoutlets1.shp");
                }
                if (!this.createOutletFile(drawOutletFile, this._gv.demFile, false, root)) {
                    return;
                }
                (this.drawOutletLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), drawOutletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                if (!this.drawOutletLayer) {
                    QSWATUtils.error("Unable to load shapefile {0}".format(drawOutletFile), this._gv.isBatch);
                    return;
                }
                if (!this._iface.setActiveLayer(this.drawOutletLayer)) {
                    QSWATUtils.error("Could not make drawing inlets/outlets layer active", this._gv.isBatch);
                    return;
                }
                this.drawOutletLayer.startEditing();
            }
            this._dlg.showMinimized();
            this._odlg.show();
            result = this._odlg.exec_();
            this._gv.outletsPos = this._odlg.pos();
            this._dlg.showNormal();
            canvas.setMapTool(null);
            if (result == 1) {
                this.thresholdChanged = true;
                if (!drawCurrent) {
                    // need to save memory layer
                    QgsVectorFileWriter.writeAsVectorFormatV2(this.drawOutletLayer, drawOutletFile, QgsCoordinateTransformContext(), this._gv.vectorFileWriterOptions);
                    (this.drawOutletLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), drawOutletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                }
                // points added by drawing will have ids of -1, so fix them
                this.fixPointIds();
                if (!drawCurrent) {
                    this._gv.outletFile = drawOutletFile;
                    this._dlg.selectOutlets.setText(drawOutletFile);
                }
            } else if (drawCurrent) {
                Debug.Assert(this.drawOutletLayer is not null);
                this.drawOutletLayer.rollBack();
            } else {
                // cancel - destroy drawn shapefile
                (ok, _) = QSWATUtils.removeLayerAndFiles(drawOutletFile, root);
                if (!ok) {
                    // no grat harm
                    return;
                }
            }
        }
        
        // Disable choice of point to be added to show users they must resume adding,
        //         unless changing to self.mapTool.
        public virtual object mapToolChanged(object tool) {
            this._odlg.widget.setEnabled(tool == this.mapTool);
        }
        
        // Reset canvas' mapTool.
        public virtual object resumeDrawing() {
            this._odlg.widget.setEnabled(true);
            this._iface.setActiveLayer(this.drawOutletLayer);
            var canvas = this._iface.mapCanvas();
            canvas.setMapTool(this.mapTool);
        }
        
        // Add point to drawOutletLayer.
        public virtual object getPoint(object point, object button) {
            // @UnusedVariable button
            var isInlet = this._odlg.inletButton.isChecked() || this._odlg.ptsourceButton.isChecked();
            // can't use feature count as they can't be counted until adding is confirmed
            // so set to -1 and fix them later
            var pid = -1;
            var inlet = isInlet ? 1 : 0;
            var res = this._odlg.reservoirButton.isChecked() ? 1 : 0;
            var ptsource = this._odlg.ptsourceButton.isChecked() ? 1 : 0;
            var idIndex = this._gv.topo.getIndex(this.drawOutletLayer, QSWATTopology._ID);
            var inletIndex = this._gv.topo.getIndex(this.drawOutletLayer, QSWATTopology._INLET);
            var resIndex = this._gv.topo.getIndex(this.drawOutletLayer, QSWATTopology._RES);
            var ptsourceIndex = this._gv.topo.getIndex(this.drawOutletLayer, QSWATTopology._PTSOURCE);
            var feature = QgsFeature();
            Debug.Assert(this.drawOutletLayer is not null);
            var fields = this.drawOutletLayer.dataProvider().fields();
            feature.setFields(fields);
            feature.setAttribute(idIndex, pid);
            feature.setAttribute(inletIndex, inlet);
            feature.setAttribute(resIndex, res);
            feature.setAttribute(ptsourceIndex, ptsource);
            feature.setGeometry(QgsGeometry.fromPointXY(point));
            this.drawOutletLayer.addFeature(feature);
            this.drawOutletLayer.triggerRepaint();
            // clicking on map may have hidden the dialog, so make it top
            this._odlg.raise_();
        }
        
        // Give suitable point ids to drawn points.
        public virtual object fixPointIds() {
            // need to commit first or appear to be no features
            Debug.Assert(this.drawOutletLayer is not null);
            this.drawOutletLayer.commitChanges();
            // then start editing again
            this.drawOutletLayer.startEditing();
            var idIndex = this._gv.topo.getIndex(this.drawOutletLayer, QSWATTopology._ID);
            // find maximum existing feature id
            var maxId = 0;
            foreach (var feature in this.drawOutletLayer.getFeatures()) {
                maxId = max(maxId, feature[idIndex]);
            }
            // replace negative feature ids
            foreach (var feature in this.drawOutletLayer.getFeatures()) {
                var pid = feature[idIndex];
                if (pid < 0) {
                    maxId += 1;
                    this.drawOutletLayer.changeAttributeValue(feature.id(), idIndex, maxId);
                }
            }
            this.drawOutletLayer.commitChanges();
        }
        
        // Allow user to select points in inlets/outlets layer.
        public virtual object selectOutlets() {
            var root = QgsProject.instance().layerTreeRoot();
            object selFromLayer = null;
            var layer = this._iface.activeLayer();
            if (layer) {
                if (layer.name().Contains("inlets/outlets")) {
                    //if layer.name().startswith(QSWATUtils._SELECTEDLEGEND):
                    //    QSWATUtils.error('You cannot select from a selected inlets/outlets layer', self._gv.isBatch)
                    //    return
                    selFromLayer = layer;
                }
            }
            if (!selFromLayer) {
                selFromLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch);
                if (!selFromLayer) {
                    QSWATUtils.error("Cannot find inlets/outlets layer.  Please choose the layer you want to select from in the layers panel.", this._gv.isBatch);
                    return;
                }
            }
            Debug.Assert(selFromLayer is QgsVectorLayer);
            if (!this._iface.setActiveLayer(selFromLayer)) {
                QSWATUtils.error("Could not make inlets/outlets layer active", this._gv.isBatch);
                return;
            }
            this._iface.actionSelectRectangle().trigger();
            var msgBox = QMessageBox();
            msgBox.move(this._gv.selectOutletPos);
            msgBox.setWindowTitle("Select inlets/outlets");
            var text = @"
        Hold Ctrl and select the points by clicking on them.
        Selected points will turn yellow, and a count is shown 
        at the bottom left of the main window.
        If you want to start again release Ctrl and click somewhere
        away from any points; then hold Ctrl and resume selection.
        You can pause in the selection to pan or zoom provided 
        you hold Ctrl again when you resume selection.
        When finished click ""Save"" to save your selection, 
        or ""Cancel"" to abandon the selection.
        ";
            msgBox.setText(QSWATUtils.trans(text));
            msgBox.setStandardButtons(QMessageBox.Save | QMessageBox.Cancel);
            msgBox.setWindowModality(Qt.NonModal);
            this._dlg.showMinimized();
            msgBox.show();
            var result = msgBox.exec_();
            this._gv.selectOutletPos = msgBox.pos();
            this._dlg.showNormal();
            if (result != QMessageBox.Save) {
                selFromLayer.removeSelection();
                return;
            }
            var selectedIds = selFromLayer.selectedFeatureIds();
            // QSWATUtils.information('Selected feature ids: {0}'.format(selectedIds), self._gv.isBatch)
            selFromLayer.removeSelection();
            // make a copy of selected layer's file, then remove non-selected features from it
            var info = QSWATUtils.layerFileInfo(selFromLayer);
            Debug.Assert(info is not null);
            var baseName = info.baseName();
            var path = info.absolutePath();
            var pattern = QSWATUtils.join(path, baseName) + ".*";
            foreach (var f in glob.iglob(pattern)) {
                (@base, suffix) = os.path.splitext(f);
                var target = @base + "_sel" + suffix;
                shutil.copyfile(f, target);
                if (suffix == ".shp") {
                    this._gv.outletFile = target;
                }
            }
            Debug.Assert(os.path.exists(this._gv.outletFile) && this._gv.outletFile.endswith("_sel.shp"));
            // make old outlet layer invisible
            root = QgsProject.instance().layerTreeRoot();
            QSWATUtils.setLayerVisibility(selFromLayer, false, root);
            // remove any existing selected layer
            QSWATUtils.removeLayerByLegend(QSWATUtils._SELECTEDLEGEND, root.findLayers());
            // load new outletFile
            (selOutletLayer, loaded) = QSWATUtils.getLayerByFilename(root.findLayers(), this._gv.outletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!selOutletLayer || !loaded) {
                QSWATUtils.error("Could not load selected inlets/outlets shapefile {0}".format(this._gv.outletFile), this._gv.isBatch);
                return;
            }
            Debug.Assert(selOutletLayer is QgsVectorLayer);
            // remove non-selected features
            var featuresToDelete = new List<object>();
            foreach (var feature in selOutletLayer.getFeatures()) {
                var fid = feature.id();
                if (!selectedIds.Contains(fid)) {
                    featuresToDelete.append(fid);
                }
            }
            // QSWATUtils.information('Non-selected feature ids: {0}'.format(featuresToDelete), self._gv.isBatch)
            selOutletLayer.dataProvider().deleteFeatures(featuresToDelete);
            selOutletLayer.triggerRepaint();
            this._dlg.selectOutlets.setText(this._gv.outletFile);
            this.thresholdChanged = true;
            this._dlg.selectOutletsInteractiveLabel.setText("{0} selected".format(selectedIds.Count));
            this.snapFile = "";
            this._dlg.snappedLabel.setText("");
        }
        
        // Allow user to select subbasins to which reservoirs should be added.
        public virtual object selectReservoirs() {
            var root = QgsProject.instance().layerTreeRoot();
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.wshedFile, ft, "", this._gv.isBatch);
            if (!wshedLayer) {
                QSWATUtils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            if (!this._iface.setActiveLayer(wshedLayer)) {
                QSWATUtils.error("Could not make watershed layer active", this._gv.isBatch);
                return;
            }
            Debug.Assert(wshedLayer is QgsVectorLayer);
            // set selection to already intended reservoirs, in case called twice
            var basinIndex = this._gv.topo.getIndex(wshedLayer, QSWATTopology._POLYGONID);
            var reservoirIds = new List<object>();
            foreach (var wshed in wshedLayer.getFeatures()) {
                if (this.extraReservoirBasins.Contains(wshed[basinIndex])) {
                    reservoirIds.append(wshed.id());
                }
            }
            wshedLayer.select(reservoirIds);
            wshedLayer.triggerRepaint();
            this._iface.actionSelect().trigger();
            var msgBox = QMessageBox();
            msgBox.move(this._gv.selectResPos);
            msgBox.setWindowTitle("Select subbasins to have reservoirs at their outlets");
            var text = @"
        Hold Ctrl and click in the subbasins you want to select.
        Selected subbasins will turn yellow, and a count is shown 
        at the bottom left of the main window.
        If you want to start again release Ctrl and click outside
        the watershed; then hold Ctrl and resume selection.
        You can pause in the selection to pan or zoom provided 
        you hold Ctrl again when you resume selection.
        When finished click ""Save"" to save your selection, 
        or ""Cancel"" to abandon the selection.
        ";
            msgBox.setText(QSWATUtils.trans(text));
            msgBox.setStandardButtons(QMessageBox.Save | QMessageBox.Cancel);
            msgBox.setWindowModality(Qt.NonModal);
            this._dlg.showMinimized();
            msgBox.show();
            var result = msgBox.exec_();
            this._gv.selectResPos = msgBox.pos();
            this._dlg.showNormal();
            if (result != QMessageBox.Save) {
                wshedLayer.removeSelection();
                return;
            }
            var wsheds = wshedLayer.selectedFeatures();
            // make set of basins intended to have reservoirs
            this.extraReservoirBasins = new HashSet<object>();
            foreach (var f in wsheds) {
                var basin = f[basinIndex];
                this.extraReservoirBasins.add(basin);
            }
        }
        
        // Create extra inlets/outlets shapefile 
        //         with added reservoirs and, if requested, point sources.
        //         
        public virtual object addReservoirs() {
            object feature;
            object point;
            object basin;
            object length;
            object nodeid;
            object attrs;
            this.delineationFinishedOK = false;
            var root = QgsProject.instance().layerTreeRoot();
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.wshedFile, ft, "", this._gv.isBatch);
            if (!wshedLayer) {
                QSWATUtils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            Debug.Assert(wshedLayer is QgsVectorLayer);
            wshedLayer.removeSelection();
            var streamLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.streamFile, FileTypes._STREAMS, "", this._gv.isBatch);
            if (!streamLayer) {
                QSWATUtils.error("Cannot find streams layer", this._gv.isBatch);
                return;
            }
            Debug.Assert(streamLayer is QgsVectorLayer);
            var linkIndex = this._gv.topo.getIndex(streamLayer, QSWATTopology._LINKNO);
            var wsnoIndex = this._gv.topo.getIndex(streamLayer, QSWATTopology._WSNO);
            var nodeidIndex = this._gv.topo.getIndex(streamLayer, QSWATTopology._DSNODEID, ignoreMissing: true);
            var lengthIndex = this._gv.topo.getIndex(streamLayer, QSWATTopology._LENGTH, ignoreMissing: true);
            var reservoirIds = this.getOutletIds(QSWATTopology._RES);
            var ptsourceIds = this.getOutletIds(QSWATTopology._PTSOURCE);
            // QSWATUtils.information('Reservoir ids are {0} and point source ids are {1}'.format(reservoirIds, ptsourceIds), self._gv.isBatch)
            var extraReservoirLinks = new HashSet<object>();
            foreach (var f in streamLayer.getFeatures()) {
                attrs = f.attributes();
                if (this.extraReservoirBasins.Contains(attrs[wsnoIndex])) {
                    if (nodeidIndex >= 0) {
                        nodeid = attrs[nodeidIndex];
                        if (reservoirIds.Contains(nodeid)) {
                            continue;
                        }
                    }
                    extraReservoirLinks.add(attrs[linkIndex]);
                }
            }
            var extraOutletFile = QSWATUtils.join(this._gv.shapesDir, "extra.shp");
            if (!this.createOutletFile(extraOutletFile, this._gv.demFile, true, root)) {
                return;
            }
            this._dlg.setCursor(Qt.WaitCursor);
            var extraOutletLayer = QgsVectorLayer(extraOutletFile, "snapped points", "ogr");
            var idIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._ID);
            var inletIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._INLET);
            var resIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._RES);
            var ptsourceIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._PTSOURCE);
            var basinIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._SUBBASIN);
            var provider = extraOutletLayer.dataProvider();
            var fields = provider.fields();
            this._gv.writeMasterProgress(0, 0);
            var pid = 0;
            foreach (var reach in streamLayer.getFeatures()) {
                attrs = reach.attributes();
                if (lengthIndex >= 0) {
                    length = attrs[lengthIndex];
                } else {
                    length = reach.geometry().length();
                }
                if (length == 0) {
                    continue;
                }
                if (nodeidIndex >= 0) {
                    nodeid = attrs[nodeidIndex];
                } else {
                    // no DSNODEID field, so no possible existing point source
                    nodeid = -1;
                }
                if (this._dlg.checkAddPoints.isChecked() && !ptsourceIds.Contains(nodeid)) {
                    // does not already have a point source
                    basin = attrs[wsnoIndex];
                    point = this._gv.topo.nearsources[basin];
                    feature = QgsFeature();
                    feature.setFields(fields);
                    feature.setAttribute(idIndex, pid);
                    pid += 1;
                    feature.setAttribute(inletIndex, 1);
                    feature.setAttribute(resIndex, 0);
                    feature.setAttribute(ptsourceIndex, 1);
                    feature.setAttribute(basinIndex, basin);
                    feature.setGeometry(QgsGeometry.fromPointXY(point));
                    provider.addFeatures(new List<object> {
                        feature
                    });
                }
                if (extraReservoirLinks.Contains(attrs[linkIndex])) {
                    basin = attrs[wsnoIndex];
                    point = this._gv.topo.nearoutlets[basin];
                    feature = QgsFeature();
                    feature.setFields(fields);
                    feature.setAttribute(idIndex, pid);
                    pid += 1;
                    feature.setAttribute(inletIndex, 0);
                    feature.setAttribute(resIndex, 1);
                    feature.setAttribute(ptsourceIndex, 0);
                    feature.setAttribute(basinIndex, basin);
                    feature.setGeometry(QgsGeometry.fromPointXY(point));
                    provider.addFeatures(new List<object> {
                        feature
                    });
                }
            }
            if (pid > 0) {
                (extraOutletLayer, loaded) = QSWATUtils.getLayerByFilename(root.findLayers(), extraOutletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                if (!(extraOutletLayer && loaded)) {
                    QSWATUtils.error("Could not load extra outlets/inlets file {0}".format(extraOutletFile), this._gv.isBatch);
                    return;
                }
                this._gv.extraOutletFile = extraOutletFile;
                // prevent merging of subbasins as point sources and/or reservoirs have been added
                this._dlg.mergeGroup.setEnabled(false);
            } else {
                // no extra reservoirs or point sources - clean up
                (ok, _) = QSWATUtils.removeLayerAndFiles(extraOutletFile, root);
                if (!ok) {
                }
                this._gv.extraOutletFile = "";
                // can now merge subbasins
                this._dlg.mergeGroup.setEnabled(true);
            }
            this._dlg.setCursor(Qt.ArrowCursor);
        }
        
        // Load snapped inlets/outlets points.
        public virtual object snapReview() {
            var root = QgsProject.instance().layerTreeRoot();
            var outletLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch);
            if (!outletLayer) {
                QSWATUtils.error("Cannot find inlets/outlets layer", this._gv.isBatch);
                return;
            }
            Debug.Assert(outletLayer is QgsVectorLayer);
            if (this.snapFile == "" || this.snapErrors) {
                var streamLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.streamFile, FileTypes._STREAMS, "", this._gv.isBatch);
                if (!streamLayer) {
                    QSWATUtils.error("Cannot find stream reaches layer", this._gv.isBatch);
                    return;
                }
                Debug.Assert(streamLayer is QgsVectorLayer);
                var outletBase = os.path.splitext(this._gv.outletFile)[0];
                var snapFile = outletBase + "_snap.shp";
                if (!this.createSnapOutletFile(outletLayer, streamLayer, this._gv.outletFile, snapFile, root)) {
                    return;
                }
            }
            // make old outlet layer invisible
            QSWATUtils.setLayerVisibility(outletLayer, false, root);
            // load snapped layer
            var outletSnapLayer = QSWATUtils.getLayerByFilename(root.findLayers(), this.snapFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME)[0];
            if (!outletSnapLayer) {
                // don't worry about loaded flag as may already have the layer loaded
                QSWATUtils.error("Could not load snapped inlets/outlets shapefile {0}".format(this.snapFile), this._gv.isBatch);
            }
        }
        
        // Allow user to select subbasins to be merged.
        public virtual object selectMergeSubbasins() {
            var root = QgsProject.instance().layerTreeRoot();
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.wshedFile, ft, "", this._gv.isBatch);
            if (!wshedLayer) {
                QSWATUtils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            if (!this._iface.setActiveLayer(wshedLayer)) {
                QSWATUtils.error("Could not make watershed layer active", this._gv.isBatch);
                return;
            }
            this._iface.actionSelect().trigger();
            this._dlg.showMinimized();
            var selSubs = SelectSubbasins(this._gv, wshedLayer);
            selSubs.run();
            this._dlg.showNormal();
        }
        
        // Merged selected subbasin with its parent.
        public virtual object mergeSubbasins() {
            object dataD;
            object dataA;
            object dropD;
            object dropA;
            object lengthD;
            object lengthA;
            object idM;
            object attrs;
            object pointFeature;
            this.delineationFinishedOK = false;
            var root = QgsProject.instance().layerTreeRoot();
            var demLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.demFile, FileTypes._DEM, "", this._gv.isBatch);
            if (!demLayer) {
                QSWATUtils.error("Cannot find DEM layer", this._gv.isBatch);
                return;
            }
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            var wshedLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.wshedFile, ft, "", this._gv.isBatch);
            if (!wshedLayer) {
                QSWATUtils.error("Cannot find watershed layer", this._gv.isBatch);
                return;
            }
            Debug.Assert(wshedLayer is QgsVectorLayer);
            var streamLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.streamFile, FileTypes._STREAMS, "", this._gv.isBatch);
            if (!streamLayer) {
                QSWATUtils.error("Cannot find stream reaches layer", this._gv.isBatch);
                wshedLayer.removeSelection();
                return;
            }
            Debug.Assert(streamLayer is QgsVectorLayer);
            var selection = wshedLayer.selectedFeatures();
            if (selection.Count == 0) {
                QSWATUtils.information("Please select at least one subbasin to be merged", this._gv.isBatch);
                return;
            }
            var outletLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch);
            var polygonidField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._POLYGONID);
            if (polygonidField < 0) {
                return;
            }
            var areaField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._AREA, ignoreMissing: true);
            var streamlinkField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._STREAMLINK, ignoreMissing: true);
            var streamlenField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._STREAMLEN, ignoreMissing: true);
            var dsnodeidwField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._DSNODEIDW, ignoreMissing: true);
            var dswsidField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._DSWSID, ignoreMissing: true);
            var us1wsidField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._US1WSID, ignoreMissing: true);
            var us2wsidField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._US2WSID, ignoreMissing: true);
            var subbasinField = this._gv.topo.getIndex(wshedLayer, QSWATTopology._SUBBASIN, ignoreMissing: true);
            var linknoField = this._gv.topo.getIndex(streamLayer, QSWATTopology._LINKNO);
            if (linknoField < 0) {
                return;
            }
            var dslinknoField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DSLINKNO);
            if (dslinknoField < 0) {
                return;
            }
            var uslinkno1Field = this._gv.topo.getIndex(streamLayer, QSWATTopology._USLINKNO1, ignoreMissing: true);
            var uslinkno2Field = this._gv.topo.getIndex(streamLayer, QSWATTopology._USLINKNO2, ignoreMissing: true);
            var dsnodeidnField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DSNODEID, ignoreMissing: true);
            var orderField = this._gv.topo.getIndex(streamLayer, QSWATTopology._ORDER, ignoreMissing: true);
            if (orderField < 0) {
                orderField = this._gv.topo.getIndex(streamLayer, QSWATTopology._ORDER2, ignoreMissing: true);
            }
            var lengthField = this._gv.topo.getIndex(streamLayer, QSWATTopology._LENGTH, ignoreMissing: true);
            var magnitudeField = this._gv.topo.getIndex(streamLayer, QSWATTopology._MAGNITUDE, ignoreMissing: true);
            var ds_cont_arField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DS_CONT_AR, ignoreMissing: true);
            if (ds_cont_arField < 0) {
                ds_cont_arField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DS_CONT_AR2, ignoreMissing: true);
            }
            var dropField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DROP, ignoreMissing: true);
            if (dropField < 0) {
                dropField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DROP2, ignoreMissing: true);
            }
            var slopeField = this._gv.topo.getIndex(streamLayer, QSWATTopology._SLOPE, ignoreMissing: true);
            var straight_lField = this._gv.topo.getIndex(streamLayer, QSWATTopology._STRAIGHT_L, ignoreMissing: true);
            if (straight_lField < 0) {
                straight_lField = this._gv.topo.getIndex(streamLayer, QSWATTopology._STRAIGHT_L2, ignoreMissing: true);
            }
            var us_cont_arField = this._gv.topo.getIndex(streamLayer, QSWATTopology._US_CONT_AR, ignoreMissing: true);
            if (us_cont_arField < 0) {
                us_cont_arField = this._gv.topo.getIndex(streamLayer, QSWATTopology._US_CONT_AR2, ignoreMissing: true);
            }
            var wsnoField = this._gv.topo.getIndex(streamLayer, QSWATTopology._WSNO);
            if (wsnoField < 0) {
                return;
            }
            var dout_endField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DOUT_END, ignoreMissing: true);
            if (dout_endField < 0) {
                dout_endField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DOUT_END2, ignoreMissing: true);
            }
            var dout_startField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DOUT_START, ignoreMissing: true);
            if (dout_startField < 0) {
                dout_startField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DOUT_START2, ignoreMissing: true);
            }
            var dout_midField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DOUT_MID, ignoreMissing: true);
            if (dout_midField < 0) {
                dout_midField = this._gv.topo.getIndex(streamLayer, QSWATTopology._DOUT_MID2, ignoreMissing: true);
            }
            if (outletLayer) {
                var nodeidField = this._gv.topo.getIndex(outletLayer, QSWATTopology._ID, ignoreMissing: true);
                var srcField = this._gv.topo.getIndex(outletLayer, QSWATTopology._PTSOURCE, ignoreMissing: true);
                var resField = this._gv.topo.getIndex(outletLayer, QSWATTopology._RES, ignoreMissing: true);
                var inletField = this._gv.topo.getIndex(outletLayer, QSWATTopology._INLET, ignoreMissing: true);
            }
            // ids of the features will change as we delete them, so use polygonids, which we know will be unique
            var pids = new List<object>();
            foreach (var f in selection) {
                var pid = f[polygonidField];
                pids.append(Convert.ToInt32(pid));
            }
            // in the following
            // suffix A refers to the subbasin being merged
            // suffix UAs refers to the subbasin(s) upstream from A
            // suffix D refers to the subbasin downstream from A
            // suffix B refers to the othe subbasin(s) upstream from D
            // suffix M refers to the merged basin
            this._gv.writeMasterProgress(0, 0);
            foreach (var polygonidA in pids) {
                var wshedA = QSWATUtils.getFeatureByValue(wshedLayer, polygonidField, polygonidA);
                Debug.Assert(wshedA is not null);
                var wshedAattrs = wshedA.attributes();
                var reachA = QSWATUtils.getFeatureByValue(streamLayer, wsnoField, polygonidA);
                if (!reachA) {
                    QSWATUtils.error("Cannot find reach with {0} value {1}".format(QSWATTopology._WSNO, polygonidA), this._gv.isBatch);
                    continue;
                }
                var reachAattrs = reachA.attributes();
                QSWATUtils.loginfo("A is reach {0} polygon {1}".format(reachAattrs[linknoField], polygonidA));
                var AHasOutlet = false;
                var AHasInlet = false;
                var AHasReservoir = false;
                var AHasSrc = false;
                if (dsnodeidnField >= 0) {
                    var dsnodeidA = reachAattrs[dsnodeidnField];
                    if (outletLayer) {
                        Debug.Assert(outletLayer is QgsVectorLayer);
                        pointFeature = QSWATUtils.getFeatureByValue(outletLayer, nodeidField, dsnodeidA);
                        if (pointFeature) {
                            attrs = pointFeature.attributes();
                            if (inletField >= 0 && attrs[inletField] == 1) {
                                if (srcField >= 0 && attrs[srcField] == 1) {
                                    AHasSrc = true;
                                } else {
                                    AHasInlet = true;
                                }
                            } else if (resField >= 0 && attrs[resField] == 1) {
                                AHasReservoir = true;
                            } else {
                                AHasOutlet = true;
                            }
                        }
                    }
                }
                if (AHasOutlet || AHasInlet || AHasReservoir || AHasSrc) {
                    QSWATUtils.information("You cannot merge a subbasin which has an outlet, inlet, reservoir, or point source.  Not merging subbasin with {0} value {1}".format(QSWATTopology._POLYGONID, polygonidA), this._gv.isBatch);
                    continue;
                }
                var linknoA = reachAattrs[linknoField];
                var reachUAs = (from reach in streamLayer.getFeatures()
                    where reach[dslinknoField] == linknoA
                    select reach).ToList();
                // check whether a reach immediately upstream from A has an inlet
                var inletUpFromA = false;
                if (dsnodeidnField >= 0 && outletLayer) {
                    Debug.Assert(outletLayer is QgsVectorLayer);
                    foreach (var reachUA in reachUAs) {
                        var reachUAattrs = reachUA.attributes();
                        var dsnodeidUA = reachUAattrs[dsnodeidnField];
                        pointFeature = QSWATUtils.getFeatureByValue(outletLayer, nodeidField, dsnodeidUA);
                        if (pointFeature) {
                            attrs = pointFeature.attributes();
                            if (inletField >= 0 && attrs[inletField] == 1 && (srcField < 0 || attrs[srcField] == 0)) {
                                inletUpFromA = true;
                                break;
                            }
                        }
                    }
                }
                var linknoD = reachAattrs[dslinknoField];
                var reachD = QSWATUtils.getFeatureByValue(streamLayer, linknoField, linknoD);
                if (!reachD) {
                    QSWATUtils.information("No downstream subbasin from subbasin with {0} value {1}: nothing to merge".format(QSWATTopology._POLYGONID, polygonidA), this._gv.isBatch);
                    continue;
                }
                var reachDattrs = reachD.attributes();
                var polygonidD = reachDattrs[wsnoField];
                QSWATUtils.loginfo("D is reach {0} polygon {1}".format(linknoD, polygonidD));
                // reachD may be zero length, with no corresponding subbasin, so search downstream if necessary to find wshedD
                // at the same time collect zero-length reaches for later disposal
                object wshedD = null;
                var nextReach = reachD;
                var zeroReaches = new List<object>();
                while (!wshedD) {
                    polygonidD = nextReach[wsnoField];
                    wshedD = QSWATUtils.getFeatureByValue(wshedLayer, polygonidField, polygonidD);
                    if (wshedD) {
                        break;
                    }
                    // nextReach has no subbasin (it is a zero length link); step downstream and try again
                    // first make a check
                    if (lengthField >= 0 && nextReach[lengthField] > 0) {
                        QSWATUtils.error("Internal error: stream reach wsno {0} has positive length but no subbasin.  Not merging subbasin with {1} value {2}".format(polygonidD, QSWATTopology._POLYGONID, polygonidA), this._gv.isBatch);
                        continue;
                    }
                    if (zeroReaches) {
                        zeroReaches.append(nextReach);
                    } else {
                        zeroReaches = new List<object> {
                            nextReach
                        };
                    }
                    var nextLink = nextReach[dslinknoField];
                    if (nextLink < 0) {
                        // reached main outlet
                        break;
                    }
                    nextReach = QSWATUtils.getFeatureByValue(streamLayer, linknoField, nextLink);
                }
                if (!wshedD) {
                    QSWATUtils.information("No downstream subbasin from subbasin with {0} value {1}: nothing to merge".format(QSWATTopology._POLYGONID, polygonidA), this._gv.isBatch);
                    continue;
                }
                Debug.Assert(wshedD is not null);
                var wshedDattrs = wshedD.attributes();
                reachD = nextReach;
                reachDattrs = reachD.attributes();
                linknoD = reachDattrs[linknoField];
                var zeroLinks = (from reach in zeroReaches
                    select reach[linknoField]).ToList();
                if (inletUpFromA) {
                    var DLinks = zeroLinks ? new List<object> {
                        linknoD
                    } + zeroLinks : new List<object> {
                        linknoD
                    };
                    var reachBs = (from reach in streamLayer.getFeatures()
                        where DLinks.Contains(reach[dslinknoField]) && reach.id() != reachA.id()
                        select reach).ToList();
                    if (reachBs != new List<object>()) {
                        QSWATUtils.information("Subbasin with {0} value {1} has an upstream inlet and the downstream one has another upstream subbasin: cannot merge.".format(QSWATTopology._POLYGONID, polygonidA), this._gv.isBatch);
                        continue;
                    }
                }
                // have reaches and watersheds A, UAs, D
                // we are ready to start editing the streamLayer
                var OK = true;
                var _success1 = false;
                try {
                    OK = streamLayer.startEditing();
                    if (!OK) {
                        QSWATUtils.error("Cannot edit stream reaches shapefile", this._gv.isBatch);
                        return;
                        //                 if reachUAs == []:
                        //                     # A is a head reach (nothing upstream)
                        //                     # change any dslinks to zeroLinks to D as the zeroReaches will be deleted
                        //                     if zeroLinks:
                        //                         for reach in streamLayer.getFeatures():
                        //                             if reach[dslinknoField] in zeroLinks:
                        //                                 streamLayer.changeAttributeValue(reach.id(), dslinknoField, linknoD)
                        //                     # change USLINK1 or USLINK2 references to A or zeroLinks to -1
                        //                     if uslinkno1Field >= 0:
                        //                         Dup1 = reachDattrs[uslinkno1Field]
                        //                         if Dup1 == linknoA or (zeroLinks and Dup1 in zeroLinks):
                        //                             streamLayer.changeAttributeValue(reachD.id(), uslinkno1Field, -1)
                        //                             Dup1 = -1
                        //                     if uslinkno2Field >= 0:
                        //                         Dup2 = reachDattrs[uslinkno2Field]
                        //                         if Dup2 == linknoA or (zeroLinks and Dup2 in zeroLinks):
                        //                             streamLayer.changeAttributeValue(reachD.id(), uslinkno2Field, -1)
                        //                             Dup2 = -1
                        //                     if magnitudeField >= 0:
                        //                         # Magnitudes of D and below should be reduced by 1
                        //                         nextReach = reachD
                        //                         while nextReach:
                        //                             mag = nextReach[magnitudeField]
                        //                             streamLayer.changeAttributeValue(nextReach.id(), magnitudeField, mag - 1)
                        //                             nextReach = QSWATUtils.getFeatureByValue(streamLayer, linknoField, nextReach[dslinknoField])
                        //                     # do not change Order field, since streams unaffected
                        // #                     if orderField >= 0:
                        // #                         # as subbasins are merged we cannot rely on two uplinks;
                        // #                         # there may be several subbasins draining into D,
                        // #                         # so we collect these, remembering to exclude A itself
                        // #                         upLinks = []
                        // #                         for reach in streamLayer.getFeatures():
                        // #                             downLink = reach[dslinknoField]
                        // #                             reachLink = reach[linknoField] 
                        // #                             if downLink == linknoD and reachLink != linknoA:
                        // #                                 upLinks.append(reach[linknoField])
                        // #                         orderD = Delineation.calculateStrahler(streamLayer, upLinks, linknoField, orderField)
                        // #                         if orderD != reachDattrs[orderField]:
                        // #                             streamLayer.changeAttributeValue(reachD.id(), orderField, orderD)
                        // #                             nextReach = QSWATUtils.getFeatureByValue(streamLayer, linknoField, reachD[dslinknoField])
                        // #                             Delineation.reassignStrahler(streamLayer, nextReach, linknoD, orderD, 
                        // #                                                          linknoField, dslinknoField, orderField)
                        //                     OK = streamLayer.deleteFeature(reachA.id())
                        //                     if not OK:
                        //                         QSWATUtils.error('Cannot edit stream reaches shapefile', self._gv.isBatch)
                        //                         streamLayer.rollBack()
                        //                         return
                        //                     if zeroReaches:
                        //                         for reach in zeroReaches:
                        //                             streamLayer.deleteFeature(reach.id())
                        //                 else:
                        // create new merged stream M from D and A and add it to streams
                        // prepare reachM
                    }
                    var reachM = QgsFeature();
                    var streamFields = streamLayer.dataProvider().fields();
                    reachM.setFields(streamFields);
                    reachM.setGeometry(reachD.geometry().combine(reachA.geometry()));
                    // check if we have single line
                    if (reachM.geometry().isMultipart()) {
                        QSWATUtils.loginfo("Multipart reach");
                    }
                    OK = streamLayer.addFeature(reachM);
                    if (!OK) {
                        QSWATUtils.error("Cannot add shape to stream reaches shapefile", this._gv.isBatch);
                        streamLayer.rollBack();
                        return;
                    }
                    idM = reachM.id();
                    streamLayer.changeAttributeValue(idM, linknoField, linknoD);
                    streamLayer.changeAttributeValue(idM, dslinknoField, reachDattrs[dslinknoField]);
                    // change dslinks in UAs to D (= M)
                    foreach (var reach in reachUAs) {
                        streamLayer.changeAttributeValue(reach.id(), dslinknoField, linknoD);
                    }
                    // change any dslinks to zeroLinks to D as the zeroReaches will be deleted
                    if (zeroLinks) {
                        foreach (var reach in streamLayer.getFeatures()) {
                            if (zeroLinks.Contains(reach[dslinknoField])) {
                                streamLayer.changeAttributeValue(reach.id(), dslinknoField, linknoD);
                            }
                        }
                    }
                    if (uslinkno1Field >= 0) {
                        var Dup1 = reachDattrs[uslinkno1Field];
                        if (Dup1 == linknoA || zeroLinks && zeroLinks.Contains(Dup1)) {
                            // in general these cannot be relied on, since as we remove zero length links 
                            // there may be more than two upstream links from M
                            // At least don't leave it referring to a soon to be non-existent reach
                            Dup1 = reachAattrs[uslinkno1Field];
                        }
                        streamLayer.changeAttributeValue(idM, uslinkno1Field, Dup1);
                    }
                    if (uslinkno2Field >= 0) {
                        var Dup2 = reachDattrs[uslinkno2Field];
                        if (Dup2 == linknoA || zeroLinks && zeroLinks.Contains(Dup2)) {
                            // in general these cannot be relied on, since as we remove zero length links 
                            // there may be more than two upstream links from M
                            // At least don't leave it referring to a soon to be non-existent reach
                            Dup2 = reachAattrs[uslinkno2Field];
                        }
                        streamLayer.changeAttributeValue(idM, uslinkno2Field, Dup2);
                    }
                    if (dsnodeidnField >= 0) {
                        streamLayer.changeAttributeValue(idM, dsnodeidnField, reachDattrs[dsnodeidnField]);
                    }
                    if (orderField >= 0) {
                        streamLayer.changeAttributeValue(idM, orderField, reachDattrs[orderField]);
                        //                     # as subbasins are merged we cannot rely on two uplinks;
                        //                     # there may be several subbasins draining into M, those that drained into A or D
                        //                     # so we collect these, remembering to exclude A itself
                        //                     upLinks = []
                        //                     for reach in streamLayer.getFeatures():
                        //                         downLink = reach[dslinknoField]
                        //                         reachLink = reach[linknoField] 
                        //                         if downLink == linknoA or (downLink == linknoD and reachLink != linknoA):
                        //                             upLinks.append(reach[linknoField])
                        //                     orderM = Delineation.calculateStrahler(streamLayer, upLinks, linknoField, orderField)
                        //                     streamLayer.changeAttributeValue(idM, orderField, orderM)
                        //                     if orderM != reachDattrs[orderField]:
                        //                         nextReach = QSWATUtils.getFeatureByValue(streamLayer, linknoField, reachD[dslinknoField])
                        //                         Delineation.reassignStrahler(streamLayer, nextReach, linknoD, orderM, 
                        //                                                      linknoField, dslinknoField, orderField)
                    }
                    if (lengthField >= 0) {
                        lengthA = reachAattrs[lengthField];
                        lengthD = reachDattrs[lengthField];
                        streamLayer.changeAttributeValue(idM, lengthField, float(lengthA + lengthD));
                    } else if (slopeField >= 0 || straight_lField >= 0 || dout_endField >= 0 && dout_midField >= 0) {
                        // we will need these lengths
                        lengthA = reachA.geometry().length();
                        lengthD = reachD.geometry().length();
                    }
                    if (magnitudeField >= 0) {
                        streamLayer.changeAttributeValue(idM, magnitudeField, reachDattrs[magnitudeField]);
                    }
                    if (ds_cont_arField >= 0) {
                        streamLayer.changeAttributeValue(idM, ds_cont_arField, float(reachDattrs[ds_cont_arField]));
                    }
                    if (dropField >= 0) {
                        dropA = reachAattrs[dropField];
                        dropD = reachDattrs[dropField];
                        streamLayer.changeAttributeValue(idM, dropField, float(dropA + dropD));
                    } else if (slopeField >= 0) {
                        dataA = this._gv.topo.getReachData(reachA, demLayer);
                        dropA = dataA.upperZ = dataA.lowerZ;
                        dataD = this._gv.topo.getReachData(reachD, demLayer);
                        dropD = dataD.upperZ = dataD.lowerZ;
                    }
                    if (slopeField >= 0) {
                        streamLayer.changeAttributeValue(idM, slopeField, float((dropA + dropD) / (lengthA + lengthD)));
                    }
                    if (straight_lField >= 0) {
                        dataA = this._gv.topo.getReachData(reachA, demLayer);
                        dataD = this._gv.topo.getReachData(reachD, demLayer);
                        var dx = dataA.upperX - dataD.lowerX;
                        var dy = dataA.upperY - dataD.lowerY;
                        streamLayer.changeAttributeValue(idM, straight_lField, float(math.sqrt(dx * dx + dy * dy)));
                    }
                    if (us_cont_arField >= 0) {
                        streamLayer.changeAttributeValue(idM, us_cont_arField, float(reachAattrs[us_cont_arField]));
                    }
                    streamLayer.changeAttributeValue(idM, wsnoField, polygonidD);
                    if (dout_endField >= 0) {
                        streamLayer.changeAttributeValue(idM, dout_endField, reachDattrs[dout_endField]);
                    }
                    if (dout_startField >= 0) {
                        streamLayer.changeAttributeValue(idM, dout_startField, reachAattrs[dout_startField]);
                    }
                    if (dout_endField >= 0 && dout_midField >= 0) {
                        streamLayer.changeAttributeValue(idM, dout_midField, float(reachDattrs[dout_endField] + (lengthA + lengthD) / 2.0));
                    }
                    streamLayer.deleteFeature(reachA.id());
                    streamLayer.deleteFeature(reachD.id());
                    if (zeroReaches) {
                        foreach (var reach in zeroReaches) {
                            streamLayer.deleteFeature(reach.id());
                        }
                    }
                    _success1 = true;
                } catch (Exception) {
                    QSWATUtils.error("Exception while updating stream reach shapefile: {0}".format(traceback.format_exc()), this._gv.isBatch);
                    OK = false;
                    streamLayer.rollBack();
                    return;
                }
                if (_success1) {
                    if (streamLayer.isEditable()) {
                        streamLayer.commitChanges();
                        streamLayer.triggerRepaint();
                    }
                }
                if (!OK) {
                    return;
                }
                // New watershed shapefile will be inconsistent with watershed grid, so remove grid to be recreated later.
                // Do not do it immediately because the user may remove several subbasins, so we wait until the 
                // delineation form is closed.
                // clear name as flag that it needs to be recreated
                this._gv.basinFile = "";
                var _success2 = false;
                try {
                    OK = wshedLayer.startEditing();
                    if (!OK) {
                        QSWATUtils.error("Cannot edit watershed shapefile", this._gv.isBatch);
                        return;
                    }
                    // create new merged subbasin M from D and A and add it to wshed
                    // prepare reachM
                    var wshedM = QgsFeature();
                    var wshedFields = wshedLayer.dataProvider().fields();
                    wshedM.setFields(wshedFields);
                    var geomD = wshedD.geometry().makeValid();
                    var geomA = wshedA.geometry().makeValid();
                    var geomM = geomD.combine(geomA);
                    if (geomM.isEmpty()) {
                        geomM = QSWATUtils.polyCombine(geomD, geomA);
                    }
                    wshedM.setGeometry(geomM);
                    OK = wshedLayer.addFeature(wshedM);
                    if (!OK) {
                        QSWATUtils.error("Cannot add shape to watershed shapefile", this._gv.isBatch);
                        wshedLayer.rollBack();
                        return;
                    }
                    idM = wshedM.id();
                    wshedLayer.changeAttributeValue(idM, polygonidField, polygonidD);
                    if (areaField >= 0) {
                        var areaA = wshedAattrs[areaField];
                        var areaD = wshedDattrs[areaField];
                        wshedLayer.changeAttributeValue(idM, areaField, float(areaA + areaD));
                    }
                    if (streamlinkField >= 0) {
                        wshedLayer.changeAttributeValue(idM, streamlinkField, wshedDattrs[streamlinkField]);
                    }
                    if (streamlenField >= 0) {
                        var lenA = wshedAattrs[streamlenField];
                        var lenD = wshedDattrs[streamlenField];
                        wshedLayer.changeAttributeValue(idM, streamlenField, float(lenA + lenD));
                    }
                    if (dsnodeidwField >= 0) {
                        wshedLayer.changeAttributeValue(idM, dsnodeidwField, wshedDattrs[dsnodeidwField]);
                    }
                    if (dswsidField >= 0) {
                        wshedLayer.changeAttributeValue(idM, dswsidField, wshedDattrs[dswsidField]);
                        // change downlinks upstream of A from A to D (= M)
                        var wshedUAs = (from wshed in wshedLayer.getFeatures()
                            where wshed[dswsidField] == polygonidA
                            select wshed).ToList();
                        foreach (var wshedUA in wshedUAs) {
                            wshedLayer.changeAttributeValue(wshedUA.id(), dswsidField, polygonidD);
                        }
                    }
                    if (us1wsidField >= 0) {
                        if (wshedDattrs[us1wsidField] == polygonidA) {
                            wshedLayer.changeAttributeValue(idM, us1wsidField, wshedAattrs[us1wsidField]);
                        } else {
                            wshedLayer.changeAttributeValue(idM, us1wsidField, wshedDattrs[us1wsidField]);
                        }
                    }
                    if (us2wsidField >= 0) {
                        if (wshedDattrs[us2wsidField] == polygonidA) {
                            wshedLayer.changeAttributeValue(idM, us2wsidField, wshedAattrs[us2wsidField]);
                        } else {
                            wshedLayer.changeAttributeValue(idM, us2wsidField, wshedDattrs[us2wsidField]);
                        }
                    }
                    if (subbasinField >= 0) {
                        wshedLayer.changeAttributeValue(idM, subbasinField, wshedDattrs[subbasinField]);
                    }
                    // remove A and D subbasins
                    wshedLayer.deleteFeature(wshedA.id());
                    wshedLayer.deleteFeature(wshedD.id());
                    _success2 = true;
                } catch (Exception) {
                    QSWATUtils.error("Exception while updating watershed shapefile: {0}".format(traceback.format_exc()), this._gv.isBatch);
                    OK = false;
                    wshedLayer.rollBack();
                    return;
                }
                if (_success2) {
                    if (wshedLayer.isEditable()) {
                        wshedLayer.commitChanges();
                        wshedLayer.triggerRepaint();
                    }
                }
            }
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
        //         downReach = QSWATUtils.getFeatureByValue(streamLayer, linknoField, reach[dslinknoField])
        //         Delineation.reassignStrahler(streamLayer, downReach, link, s, linknoField, dslinknoField, orderField)
        //         
        // @staticmethod
        // def calculateStrahler(streamLayer, upLinks, linknoField, orderField):
        //     """Calculate Strahler order from upstream links upLinks."""
        //     orders = [QSWATUtils.getFeatureByValue(streamLayer, linknoField, upLink)[orderField] for upLink in upLinks]
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
        public virtual object cleanUp(int tabIndex) {
            if (tabIndex >= 0) {
                this._dlg.tabWidget.setCurrentIndex(tabIndex);
            }
            this._dlg.setCursor(Qt.ArrowCursor);
            this.progress("");
            return;
        }
        
        // Create watershed shapefile wshedFile from watershed grid wFile.
        public virtual object createWatershedShapefile(string wFile, string wshedFile, object root) {
            object subLayer;
            object wshedLayer;
            object ds;
            if (QSWATUtils.isUpToDate(wFile, wshedFile)) {
                return;
            }
            var driver = ogr.GetDriverByName("ESRI Shapefile");
            if (driver is null) {
                QSWATUtils.error("ESRI Shapefile driver is not available - cannot write watershed shapefile", this._gv.isBatch);
                return;
            }
            if (QSWATUtils.shapefileExists(wshedFile)) {
                ds = driver.Open(wshedFile, 1);
                wshedLayer = ds.GetLayer();
                foreach (var feature in wshedLayer) {
                    wshedLayer.DeleteFeature(feature.GetFID());
                }
            } else {
                (ok, path) = QSWATUtils.removeLayerAndFiles(wshedFile, root);
                if (!ok) {
                    QSWATUtils.error("Failed to remove old watershed file {0}: try repeating last click, else remove manually.".format(path), this._gv.isBatch);
                    this._dlg.setCursor(Qt.ArrowCursor);
                    return;
                }
                ds = driver.CreateDataSource(wshedFile);
                if (ds is null) {
                    QSWATUtils.error("Cannot create watershed shapefile {0}".format(wshedFile), this._gv.isBatch);
                    return;
                }
                var fileInfo = QFileInfo(wshedFile);
                wshedLayer = ds.CreateLayer(fileInfo.baseName().ToString(), geom_type: ogr.wkbPolygon);
                if (wshedLayer is null) {
                    QSWATUtils.error("Cannot create layer for watershed shapefile {0}".format(wshedFile), this._gv.isBatch);
                    return;
                }
                var idFieldDef = ogr.FieldDefn(QSWATTopology._POLYGONID, ogr.OFTInteger);
                if (idFieldDef is null) {
                    QSWATUtils.error("Cannot create field {0}".format(QSWATTopology._POLYGONID), this._gv.isBatch);
                    return;
                }
                var index = wshedLayer.CreateField(idFieldDef);
                if (index != 0) {
                    QSWATUtils.error("Cannot create field {0} in {1}".format(QSWATTopology._POLYGONID, wshedFile), this._gv.isBatch);
                    return;
                }
                var areaFieldDef = ogr.FieldDefn(QSWATTopology._AREA, ogr.OFTReal);
                areaFieldDef.SetWidth(20);
                areaFieldDef.SetPrecision(0);
                if (areaFieldDef is null) {
                    QSWATUtils.error("Cannot create field {0}".format(QSWATTopology._AREA), this._gv.isBatch);
                    return;
                }
                index = wshedLayer.CreateField(areaFieldDef);
                if (index != 0) {
                    QSWATUtils.error("Cannot create field {0} in {1}".format(QSWATTopology._AREA, wshedFile), this._gv.isBatch);
                    return;
                }
                var subbasinFieldDef = ogr.FieldDefn(QSWATTopology._SUBBASIN, ogr.OFTInteger);
                if (subbasinFieldDef is null) {
                    QSWATUtils.error("Cannot create field {0}".format(QSWATTopology._SUBBASIN), this._gv.isBatch);
                    return;
                }
                index = wshedLayer.CreateField(subbasinFieldDef);
                if (index != 0) {
                    QSWATUtils.error("Cannot create field {0} in {1}".format(QSWATTopology._SUBBASIN, wshedFile), this._gv.isBatch);
                    return;
                }
            }
            var sourceRaster = gdal.Open(wFile);
            if (sourceRaster is null) {
                QSWATUtils.error("Cannot open watershed grid {0}".format(wFile), this._gv.isBatch);
                return;
            }
            var band = sourceRaster.GetRasterBand(1);
            var nodata = band.GetNoDataValue();
            var featuresToDelete = new List<object>();
            // We could use band as a mask, but that removes and subbasins with wsno 0
            // so we run with no mask, which produces an unwanted polygon with PolygonId
            // set to the wFile's nodata value.  This we will remove later.
            gdal.Polygonize(band, null, wshedLayer, 0, new List<string> {
                "8CONNECTED=8"
            }, callback: null);
            ds = null;
            QSWATUtils.copyPrj(wFile, wshedFile);
            // load it
            root = QgsProject.instance().layerTreeRoot();
            // make DEM active so loads above it and below streams
            // (or use Full HRUs layer if there is one)
            var fullHRUsLayer = QSWATUtils.getLayerByLegend(QSWATUtils._FULLHRUSLEGEND, root.findLayers());
            if (fullHRUsLayer) {
                subLayer = fullHRUsLayer;
            } else {
                var hillshadeLayer = QSWATUtils.getLayerByLegend(QSWATUtils._HILLSHADELEGEND, root.findLayers());
                if (hillshadeLayer) {
                    subLayer = hillshadeLayer;
                } else {
                    var demLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.demFile, FileTypes._DEM, "", this._gv.isBatch);
                    if (demLayer) {
                        subLayer = root.findLayer(demLayer.id());
                    } else {
                        subLayer = null;
                    }
                }
            }
            (wshedLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), wshedFile, FileTypes._WATERSHED, this._gv, subLayer, QSWATUtils._WATERSHED_GROUP_NAME);
            if (wshedLayer is null) {
                QSWATUtils.error("Failed to load watershed shapefile {0}".format(wshedFile), this._gv.isBatch);
                return;
            }
            this._iface.setActiveLayer(wshedLayer);
            // labels should be turned off, as may persist from previous run
            // we turn back on when SWAT basin numbers are calculated and stored
            // in the Subbasin field
            wshedLayer.setLabelsEnabled(false);
            // get areas and centroids of subbasins
            this._gv.topo.basinCentroids.clear();
            wshedLayer.startEditing();
            var basinIndex = this._gv.topo.getIndex(wshedLayer, QSWATTopology._POLYGONID);
            var areaIndex = this._gv.topo.getIndex(wshedLayer, QSWATTopology._AREA);
            foreach (var feature in wshedLayer.getFeatures()) {
                var basin = feature[basinIndex];
                if (basin == nodata) {
                    featuresToDelete.append(feature.id());
                } else {
                    var area = float(feature.geometry().area());
                    wshedLayer.changeAttributeValue(feature.id(), areaIndex, area);
                    var centroid = feature.geometry().centroid().asPoint();
                    this._gv.topo.basinCentroids[basin] = (centroid.x(), centroid.y());
                }
            }
            // get rid of any basin corresponding to nodata in wFile
            if (featuresToDelete.Count > 0) {
                wshedLayer.dataProvider().deleteFeatures(featuresToDelete);
            }
            wshedLayer.commitChanges();
            wshedLayer.triggerRepaint();
        }
        
        // Create basin file from watershed shapefile.
        public virtual string createBasinFile(string wshedFile, object demLayer, object root) {
            var demPath = QSWATUtils.layerFileInfo(demLayer).canonicalFilePath();
            var wFile = os.path.splitext(demPath)[0] + "w.tif";
            var shapeBase = os.path.splitext(wshedFile)[0];
            // if basename of wFile is used rasterize fails
            var baseName = os.path.basename(shapeBase);
            (ok, path) = QSWATUtils.removeLayerAndFiles(wFile, root);
            if (!ok) {
                QSWATUtils.error("Failed to remove old {0}: try repeating last click, else remove manually.".format(path), this._gv.isBatch);
                this._dlg.setCursor(Qt.ArrowCursor);
                return "";
            }
            Debug.Assert(!os.path.exists(wFile));
            var xSize = demLayer.rasterUnitsPerPixelX();
            var ySize = demLayer.rasterUnitsPerPixelY();
            var extent = demLayer.extent();
            // need to use extent to align basin raster cells with DEM
            var command = "gdal_rasterize -a {0} -tr {1} {2} -te {6} {7} {8} {9} -a_nodata -9999 -ot Int32 -of GTiff -l \"{3}\" \"{4}\" \"{5}\"".format(QSWATTopology._POLYGONID, xSize, ySize, baseName, wshedFile, wFile, extent.xMinimum(), extent.yMinimum(), extent.xMaximum(), extent.yMaximum());
            QSWATUtils.loginfo(command);
            os.system(command);
            Debug.Assert(os.path.exists(wFile));
            QSWATUtils.copyPrj(wshedFile, wFile);
            return wFile;
        }
        
        // Create grid shapefile for watershed.
        public virtual object createGridShapefile(object demLayer, string pFile, string ad8File, string wFile) {
            object gridStreamsFile;
            object gridFile;
            object readInletsFile(string fileName) {
                result = new HashSet<object>();
                using (var f = open(fileName, "r")) {
                    foreach (var line in f) {
                        nums = line.split(",");
                        foreach (var num in nums) {
                            try {
                                val = Convert.ToInt32(num);
                                if (result.Contains(val)) {
                                    QSWATUtils.error("PolygonId {0} appears more than once in {1}".format(val, fileName));
                                } else {
                                    result.add(val);
                                }
                            } catch {
                            }
                        }
                    }
                }
                return result;
            }
            var gridSize = this._dlg.GridSize.value();
            var inlets = new HashSet<object>();
            if (this._gv.forTNC) {
                // store grid and gridstreams with DEM so can be reused for same grid size
                gridFile = QSWATUtils.join(this._gv.sourceDir, "grid{0}.shp".format(gridSize));
                gridStreamsFile = QSWATUtils.join(this._gv.sourceDir, "grid{0}streams.shp".format(gridSize));
                // inletsFile = QSWATUtils.join(self._gv.sourceDir, 'inlets.txt')  # inlets now added for TNC models by catchments.py
                // if os.path.isfile(inletsFile):
                //     inlets = readInletsFile(inletsFile)
            } else {
                gridFile = QSWATUtils.join(this._gv.shapesDir, "grid.shp");
                gridStreamsFile = QSWATUtils.join(this._gv.shapesDir, "gridstreams.shp");
            }
            if (QSWATUtils.isUpToDate(this._gv.demFile, gridFile) && QSWATUtils.isUpToDate(this._gv.demFile, gridStreamsFile)) {
                if (!this._gv.forTNC) {
                    // or QSWATUtils.isUpToDate(inletsFile, gridFile):
                    // restore settings of wshed and streams shapefiles
                    this._gv.wshedFile = gridFile;
                    this._gv.streamFile = gridStreamsFile;
                    // make sure grid layers are loaded
                    var root = QgsProject.instance().layerTreeRoot();
                    (gridLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), gridFile, FileTypes._GRID, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
                    if (!gridLayer) {
                        QSWATUtils.error("Failed to load grid shapefile {0}".format(gridFile), this._gv.isBatch);
                        return;
                    }
                    var gridStreamsLayer = QSWATUtils.getLayerByFilename(root.findLayers(), gridStreamsFile, FileTypes._GRIDSTREAMS, this._gv, gridLayer, QSWATUtils._WATERSHED_GROUP_NAME)[0];
                    if (!gridStreamsLayer) {
                        QSWATUtils.error("Failed to load grid streams shapefile {0}".format(gridStreamsFile), this._gv.isBatch);
                    }
                    return;
                }
            }
            this.progress("Creating grid ...");
            var accFile = ad8File;
            var flowFile = pFile;
            var time2 = time.process_time();
            (storeGrid, accTransform, minDrainArea, maxDrainArea) = this.storeGridData(accFile, wFile, gridSize);
            var time3 = time.process_time();
            QSWATUtils.loginfo("Storing grid data took {0} seconds".format(Convert.ToInt32(time3 - time2)));
            if (storeGrid) {
                Debug.Assert(accTransform is not null);
                if (this.addDownstreamData(storeGrid, flowFile, gridSize, accTransform)) {
                    var time4 = time.process_time();
                    QSWATUtils.loginfo("Adding downstream data took {0} seconds".format(Convert.ToInt32(time4 - time3)));
                    // inlets: Dict[int, Dict[int, int]] = dict()
                    // self.addGridOutletsAuto(storeGrid, inlets)  # moved to catchments.py
                    this.addGridOutlets(storeGrid, inlets);
                    var time4a = time.process_time();
                    // QSWATUtils.loginfo('Adding outlets took {0} seconds'.format(int(time4a - time4)))
                    Console.WriteLine("Adding outlets took {0} seconds".format(time4a - time4));
                    this.writeGridShapefile(storeGrid, gridFile, flowFile, gridSize, accTransform, null);
                    var time5 = time.process_time();
                    QSWATUtils.loginfo("Writing grid shapefile took {0} seconds".format(Convert.ToInt32(time5 - time4a)));
                    var numOutlets = this.writeGridStreamsShapefile(storeGrid, gridStreamsFile, flowFile, minDrainArea, maxDrainArea, accTransform);
                    var time6 = time.process_time();
                    QSWATUtils.loginfo("Writing grid streams shapefile took {0} seconds".format(Convert.ToInt32(time6 - time5)));
                    // if numOutlets >= 0:
                    //     msg = 'Grid processing done with delineation threshold {0} sq.km: {1} outlets'.format(self._dlg.area.text(), numOutlets)
                    //     QSWATUtils.loginfo(msg)
                    //     self._iface.messageBar().pushMessage(msg, level=Qgis.Info, duration=10)
                    //     if self._gv.isBatch:
                    //         print(msg)
                }
            }
            return;
        }
        
        // Create grid data in array and return it.
        public virtual object storeGridData(string ad8File, string basinFile, int gridSize) {
            // mask accFile with basinFile to exclude small outflowing watersheds
            // only do this if result needs updating and if not from GRASS (since ad8 file from GRASS was masked by basinFile)
            var @base = os.path.splitext(ad8File)[0];
            var accFile = this._gv.fromGRASS ? ad8File : @base + "clip.tif";
            if (!(this._gv.fromGRASS || QSWATUtils.isUpToDate(ad8File, accFile))) {
                var ad8Layer = QgsRasterLayer(ad8File, "P");
                var entry1 = QgsRasterCalculatorEntry();
                entry1.bandNumber = 1;
                entry1.raster = ad8Layer;
                entry1.@ref = "P@1";
                var basinLayer = QgsRasterLayer(basinFile, "Q");
                var entry2 = QgsRasterCalculatorEntry();
                entry2.bandNumber = 1;
                entry2.raster = basinLayer;
                entry2.@ref = "Q@1";
                QSWATUtils.tryRemoveFiles(accFile);
                // The formula is a standard way of masking P with Q, since 
                // where Q is nodata Q / Q evaluates to nodata, and elsewhere evaluates to 1.
                // We use 'Q+1' instead of Q to avoid problems in first subbasin 
                // when PolygonId is zero so Q is zero
                var formula = "((Q@1 + 1) / (Q@1 + 1)) * P@1";
                var calc = QgsRasterCalculator(formula, accFile, "GTiff", ad8Layer.extent(), ad8Layer.width(), ad8Layer.height(), new List<object> {
                    entry1,
                    entry2
                }, QgsCoordinateTransformContext());
                var result = calc.processCalculation(feedback: null);
                if (result == 0) {
                    Debug.Assert(os.path.exists(accFile));
                    Debug.Assert("QGIS calculator formula {0} failed to write output".format(formula));
                    QSWATUtils.copyPrj(ad8File, accFile);
                } else {
                    QSWATUtils.error("QGIS calculator formula {0} failed: returned {1}".format(formula, result), this._gv.isBatch);
                    return (null, null, 0, 0);
                }
            }
            var accRaster = gdal.Open(accFile, gdal.GA_ReadOnly);
            if (accRaster is null) {
                QSWATUtils.error("Cannot open accumulation file {0}".format(accFile), this._gv.isBatch);
                return (null, null, 0, 0);
            }
            // for now read whole clipped accumulation file into memory
            var accBand = accRaster.GetRasterBand(1);
            var accTransform = accRaster.GetGeoTransform();
            var accArray = accBand.ReadAsArray(0, 0, accBand.XSize, accBand.YSize);
            var accNoData = accBand.GetNoDataValue();
            var unitArea = abs(accTransform[1] * accTransform[5]) / 1000000.0;
            // create polygons and add to gridFile
            var polyId = 0;
            // grid cells will be gridSize x gridSize squares
            var numGridRows = accBand.YSize / gridSize + 1;
            var numGridCols = accBand.XSize / gridSize + 1;
            var storeGrid = new dict();
            var maxDrainArea = 0;
            var minDrainArea = double.PositiveInfinity;
            foreach (var gridRow in Enumerable.Range(0, numGridRows)) {
                var startAccRow = gridRow * gridSize;
                foreach (var gridCol in Enumerable.Range(0, numGridCols)) {
                    var startAccCol = gridCol * gridSize;
                    var maxAcc = 0;
                    var maxRow = -1;
                    var maxCol = -1;
                    var valCount = 0;
                    foreach (var row in Enumerable.Range(0, gridSize)) {
                        var accRow = startAccRow + row;
                        foreach (var col in Enumerable.Range(0, gridSize)) {
                            var accCol = startAccCol + col;
                            if (accRow < accBand.YSize && accCol < accBand.XSize) {
                                var accVal = accArray[accRow,accCol];
                                if (accVal != accNoData) {
                                    valCount += 1;
                                    // can get points with same (rounded) accumulation when values are high.
                                    // prefer one on edge if possible
                                    if (accVal > maxAcc || accVal == maxAcc && this.onEdge(row, col, gridSize)) {
                                        maxAcc = accVal;
                                        maxRow = accRow;
                                        maxCol = accCol;
                                    }
                                }
                            }
                        }
                    }
                    if (valCount == 0) {
                        // no data for this grid
                        continue;
                    }
                    polyId += 1;
                    //if polyId <= 5:
                    //    x, y = QSWATTopology.cellToProj(maxCol, maxRow, accTransform)
                    //    maxAccPoint = QgsPointXY(x, y)
                    //    QSWATUtils.loginfo('Grid ({0},{1}) id {6} max {4} at ({2},{3}) which is {5}'.format(gridRow, gridCol, maxCol, maxRow, maxAcc, maxAccPoint.toString(), polyId))
                    var drainArea = maxAcc * unitArea;
                    if (drainArea < minDrainArea) {
                        minDrainArea = drainArea;
                    }
                    if (drainArea > maxDrainArea) {
                        maxDrainArea = drainArea;
                    }
                    var data = new GridData(polyId, valCount, drainArea, maxAcc, maxRow, maxCol);
                    if (!storeGrid.Contains(gridRow)) {
                        storeGrid[gridRow] = new dict();
                    }
                    storeGrid[gridRow][gridCol] = data;
                }
            }
            accRaster = null;
            accArray = null;
            return (storeGrid, accTransform, minDrainArea, maxDrainArea);
        }
        
        // Returns true of (row, col) is on the edge of the cell.
        [staticmethod]
        public static bool onEdge(int row, int col, int gridSize) {
            return row == 0 || row == gridSize - 1 || col == 0 || col == gridSize - 1;
        }
        
        // Use flow direction flowFile to see to which grid cell a D8 step takes you from the max accumulation point and store in array.
        public virtual bool addDownstreamData(object storeGrid, string flowFile, int gridSize, object accTransform) {
            object accToPCol;
            object accToPRow;
            var pRaster = gdal.Open(flowFile, gdal.GA_ReadOnly);
            if (pRaster is null) {
                QSWATUtils.error("Cannot open flow direction file {0}".format(flowFile), this._gv.isBatch);
                return false;
            }
            // for now read whole D8 flow direction file into memory
            var pBand = pRaster.GetRasterBand(1);
            //pNoData = pBand.GetNoDataValue()
            var pTransform = pRaster.GetGeoTransform();
            if (pTransform[1] != accTransform[1] || pTransform[5] != accTransform[5]) {
                // problem with comparing floating point numbers
                // actually OK if the vertical/horizontal difference times the number of rows/columns
                // is less than half the depth/width of a cell
                if (abs(pTransform[1] - accTransform[1]) * pBand.XSize > pTransform[1] * 0.5 || abs(pTransform[5] - accTransform[5]) * pBand.YSize > abs(pTransform[5]) * 0.5) {
                    QSWATUtils.error("Flow direction and accumulation files must have same cell size", this._gv.isBatch);
                    pRaster = null;
                    return false;
                }
            }
            var pArray = pBand.ReadAsArray(0, 0, pBand.XSize, pBand.YSize);
            // we know the cell sizes are sufficiently close;
            // accept the origins as the same if they are within a tenth of the cell size
            var sameCoords = pTransform == accTransform || abs(pTransform[0] - accTransform[0]) < pTransform[1] * 0.1 && abs(pTransform[3] - accTransform[3]) < abs(pTransform[5]) * 0.1;
            foreach (var (gridRow, gridCols) in storeGrid.items()) {
                foreach (var (gridCol, gridData) in gridCols.items()) {
                    // since we have same cell sizes, can simplify conversion from accumulation row, col to direction row, col
                    if (sameCoords) {
                        accToPRow = 0;
                        accToPCol = 0;
                    } else {
                        accToPCol = round((accTransform[0] - pTransform[0]) / accTransform[1]);
                        accToPRow = round((accTransform[3] - pTransform[3]) / accTransform[5]);
                        //pRow = QSWATTopology.yToRow(QSWATTopology.rowToY(gridData.maxRow, accTransform), pTransform)
                        //pCol = QSWATTopology.xToCol(QSWATTopology.colToX(gridData.maxCol, accTransform), pTransform)
                    }
                    var currentPRow = gridData.maxRow + accToPRow;
                    var currentPCol = gridData.maxCol + accToPCol;
                    // try to find downstream grid cell.  If we fail downstram number left as -1, which means outlet
                    // rounding of large accumulation values means that the maximum accumulation point found
                    // may not be at the outflow point, so we need to move until we find a new grid cell, or hit a map edge
                    var maxSteps = 2 * gridSize;
                    var found = false;
                    while (!found) {
                        if (0 <= currentPRow && currentPRow < pBand.YSize && (0 <= currentPCol && currentPCol < pBand.XSize)) {
                            var direction = pArray[currentPRow,currentPCol];
                        } else {
                            break;
                        }
                        // apply a step in direction
                        if (1 <= direction && direction <= 8) {
                            currentPRow = currentPRow + QSWATUtils._dY[direction - 1];
                            currentPCol = currentPCol + QSWATUtils._dX[direction - 1];
                        } else {
                            break;
                        }
                        var currentAccRow = currentPRow - accToPRow;
                        var currentAccCol = currentPCol - accToPCol;
                        var currentGridRow = currentAccRow / gridSize;
                        var currentGridCol = currentAccCol / gridSize;
                        found = currentGridRow != gridRow || currentGridCol != gridCol;
                        if (!found) {
                            maxSteps -= 1;
                            if (maxSteps <= 0) {
                                (x0, y0) = QSWATTopology.cellToProj(gridData.maxCol, gridData.maxRow, accTransform);
                                (x, y) = QSWATTopology.cellToProj(currentAccCol, currentAccRow, accTransform);
                                QSWATUtils.error("Loop in flow directions in grid id {4} starting from ({0},{1}) and so far reaching ({2},{3})".format(Convert.ToInt32(x0), Convert.ToInt32(y0), Convert.ToInt32(x), Convert.ToInt32(y), gridData.num), this._gv.isBatch);
                                Console.WriteLine("Loop in flow directions in grid id {4} starting from ({0},{1}) and so far reaching ({2},{3})".format(Convert.ToInt32(x0), Convert.ToInt32(y0), Convert.ToInt32(x), Convert.ToInt32(y), gridData.num));
                                break;
                            }
                        }
                    }
                    if (found) {
                        var cols = storeGrid.get(currentGridRow, null);
                        if (cols is not null) {
                            var currentData = cols.get(currentGridCol, null);
                            if (currentData is not null) {
                                if (currentData.maxAcc < gridData.maxAcc) {
                                    QSWATUtils.loginfo("WARNING: while calculating stream drainage, target grid cell {0} has lower maximum accumulation {1} than source grid cell {2}'s accumulation {3}".format(currentData.num, currentData.maxAcc, gridData.num, gridData.maxAcc));
                                }
                                gridData.downNum = currentData.num;
                                gridData.downRow = currentGridRow;
                                gridData.downCol = currentGridCol;
                                currentData.incount += 1;
                                //if gridData.num <= 5:
                                //    QSWATUtils.loginfo('Grid ({0},{1}) drains to acc ({2},{3}) in grid ({4},{5})'.format(gridRow, gridCol, currentAccCol, currentAccRow, currentGridRow, currentGridCol))
                                //    QSWATUtils.loginfo('{0} at {1},{2} given down id {3}'.format(gridData.num, gridRow, gridCol, gridData.downNum))
                                if (gridData.downNum == gridData.num) {
                                    (x, y) = QSWATTopology.cellToProj(gridData.maxCol, gridData.maxRow, accTransform);
                                    var maxAccPoint = QgsPointXY(x, y);
                                    QSWATUtils.loginfo("Grid ({0},{1}) id {5} at ({2},{3}) which is {4} draining to ({6},{7})".format(gridCol, gridRow, gridData.maxCol, gridData.maxRow, maxAccPoint.toString(), gridData.num, currentAccCol, currentAccRow));
                                    gridData.downNum = -1;
                                }
                                //assert gridData.downNum != gridData.num
                                storeGrid[gridRow][gridCol] = gridData;
                            }
                        }
                    }
                }
            }
            pRaster = null;
            pArray = null;
            return true;
        }
        
        //=========No longer used.  Subcatchments defined by catchments module for TNC models==================================================================
        // @staticmethod
        // def addGridOutletsAuto(storeGrid: Dict[int, Dict[int, GridData]], inlets: Dict[int, Dict[int, int]]) -> None:
        //     """Add outlets to grid data, marking inlet points automatically."""
        //     maxChainlength = 30  # length of path without encountering an inlet before one is added
        //     inlets.clear()
        //     for gridRow, gridCols in storeGrid.items():
        //         for gridCol, gridData in gridCols.items():
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
        //                             print('Inlet at {0} moved upstream from {1}'.format(prevGrid.num, currentGrid.num))
        //                             break
        //                         else:
        //                             inlets.setdefault(current[0], dict())[current[1]] = currentGrid.num
        //                             print('Inlet at {0}'.format(currentGrid.num))
        //                             currentGrid.outlet = currentGrid.num
        //                         
        //                     if currentGrid.outlet > 0:
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.outlet
        //                         downChain = []
        //                     if currentGrid.downNum < 0:
        //                         break
        //                     if current in downChain:
        //                         QSWATUtils.loginfo('Row {0} column {1} links to itself in the grid'.format(current[0], current[1]))
        //                         print('Row {0} column {1} links to itself in the grid'.format(current[0], current[1]))
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.num
        //                         break
        //                     if currentGrid.outlet < 0:
        //                         downChain.append(current)
        //                     current = currentGrid.downRow, currentGrid.downCol
        //                 
        //===========================================================================
        // Add outlets to grid data.  inlets now always an empty set, as added for TNC modesl in catchments.py
        [staticmethod]
        public static object addGridOutlets(object storeGrid, object inlets) {
            foreach (var (gridRow, gridCols) in storeGrid.items()) {
                foreach (var gridCol in gridCols) {
                    var current = (gridRow, gridCol);
                    var downChain = new List<object>();
                    while (true) {
                        var currentGrid = storeGrid[current[0]][current[1]];
                        if (currentGrid.downNum < 0 || inlets.Contains(currentGrid.num)) {
                            currentGrid.outlet = currentGrid.num;
                        }
                        if (currentGrid.outlet >= 0) {
                            foreach (var (row, col) in downChain) {
                                storeGrid[row][col].outlet = currentGrid.outlet;
                            }
                            break;
                        }
                        if (downChain.Contains(current)) {
                            QSWATUtils.loginfo("Row {0} column {1} links to itself in the grid".format(current[0], current[1]));
                            foreach (var (row, col) in downChain) {
                                storeGrid[row][col].outlet = currentGrid.num;
                            }
                            break;
                        }
                        downChain.append(current);
                        current = (currentGrid.downRow, currentGrid.downCol);
                    }
                }
            }
        }
        
        //===========================================================================
        // @staticmethod
        // def addGridOutlets(storeGrid: Dict[int, Dict[int, GridData]], inlets: Set[int]) -> None:
        //     """Add outlets to grid data."""
        //     print('Inlets: {0}'.format(inlets))
        //     for gridRow, gridCols in storeGrid.items():
        //         for gridCol, gridData in gridCols.items():
        //             if gridData.num in {5839, 5875}:
        //                 print('PolygonId: {0}, downNum {1}, incount: {2}, outlet {3}'.format(gridData.num, gridData.downNum, gridData.incount, gridData.outlet))
        //             # start from leaf nodes
        //             if gridData.incount == 0:
        //                 current  = gridRow, gridCol
        //                 downChain: List[Tuple[int, int]] = []
        //                 while True:
        //                     currentGrid = storeGrid[current[0]][current[1]]
        //                     if currentGrid.downNum < 0 or currentGrid.num in inlets:
        //                         currentGrid.outlet = currentGrid.num
        //                         # if currentGrid.num in inlets:
        //                         #     print('Inlet at {0}'.format(currentGrid.num))
        //                     if currentGrid.outlet >= 0:
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.outlet
        //                         if currentGrid.downNum < 0:
        //                             break
        //                     if current in downChain:
        //                         QSWATUtils.loginfo('Row {0} column {1} links to itself in the grid'.format(current[0], current[1]))
        //                         for row, col in downChain:
        //                             storeGrid[row][col].outlet = currentGrid.num
        //                         break
        //                     if currentGrid.num in inlets:
        //                         downChain = []
        //                     else:
        //                         downChain.append(current)
        //                     current = currentGrid.downRow, currentGrid.downCol
        //===========================================================================
        // Write grid data to grid shapefile.  Also writes centroids dictionary.
        public virtual object writeGridShapefile(
            object storeGrid,
            string gridFile,
            string flowFile,
            int gridSize,
            object accTransform,
            void inlets = null) {
            object writer2;
            this.progress("Writing grid ...");
            var fields = QgsFields();
            fields.append(QgsField(QSWATTopology._POLYGONID, QVariant.Int));
            fields.append(QgsField(QSWATTopology._DOWNID, QVariant.Int));
            fields.append(QgsField(QSWATTopology._AREA, QVariant.Int));
            fields.append(QgsField(QSWATTopology._OUTLET, QVariant.Int));
            var root = QgsProject.instance().layerTreeRoot();
            QSWATUtils.removeLayer(gridFile, root);
            var transform_context = QgsProject.instance().transformContext();
            var writer = QgsVectorFileWriter.create(gridFile, fields, QgsWkbTypes.Polygon, this._gv.topo.crsProject, transform_context, this._gv.vectorFileWriterOptions);
            if (writer.hasError() != QgsVectorFileWriter.NoError) {
                QSWATUtils.error("Cannot create grid shapefile {0}: {1}".format(gridFile, writer.errorMessage()), this._gv.isBatch);
                return;
            }
            var idIndex = fields.indexFromName(QSWATTopology._POLYGONID);
            var downIndex = fields.indexFromName(QSWATTopology._DOWNID);
            var areaIndex = fields.indexFromName(QSWATTopology._AREA);
            var outletIndex = fields.indexFromName(QSWATTopology._OUTLET);
            if (inlets is not null) {
                var fields2 = QgsFields();
                fields2.append(QgsField("Catchment", QVariant.Int));
                var inletsFile = os.path.split(gridFile)[0] + "/inletsshapes.shp";
                QSWATUtils.removeLayer(inletsFile, root);
                writer2 = QgsVectorFileWriter.create(inletsFile, fields2, QgsWkbTypes.Point, this._gv.topo.crsProject, transform_context, this._gv.vectorFileWriterOptions);
                if (writer2.hasError() != QgsVectorFileWriter.NoError) {
                    QSWATUtils.error("Cannot create inlets shapefile {0}: {1}".format(inletsFile, writer2.errorMessage()), this._gv.isBatch);
                    inlets = null;
                }
            }
            (ul_x, x_size, _, ul_y, _, y_size) = accTransform;
            var xDiff = x_size * gridSize * 0.5;
            var yDiff = y_size * gridSize * 0.5;
            this._gv.topo.basinCentroids = new dict();
            this._gv.topo.catchmentOutlets = new dict();
            this._gv.topo.downCatchments = new dict();
            foreach (var (gridRow, gridCols) in storeGrid.items()) {
                // grids can be big so we'll add one row at a time
                var centreY = (gridRow + 0.5) * gridSize * y_size + ul_y;
                var features = new List<object>();
                var features2 = new List<object>();
                foreach (var (gridCol, gridData) in gridCols.items()) {
                    var centreX = (gridCol + 0.5) * gridSize * x_size + ul_x;
                    // this is strictly not the centroid for incomplete grid squares on the edges,
                    // but will make little difference.  
                    // Needs to be centre of grid for correct identification of landuse, soil and slope rows
                    // when creating HRUs.
                    this._gv.topo.basinCentroids[gridData.num] = (centreX, centreY);
                    this._gv.topo.catchmentOutlets[gridData.num] = gridData.outlet;
                    if (gridData.num == gridData.outlet) {
                        // this is an outlet of a catchment: see if there is one downstream
                        var dsGridData = storeGrid.get(gridData.downRow, new dict()).get(gridData.downCol, null);
                        if (dsGridData is null || gridData.outlet == dsGridData.outlet) {
                            this._gv.topo.downCatchments[gridData.outlet] = -1;
                        } else {
                            this._gv.topo.downCatchments[gridData.outlet] = dsGridData.outlet;
                        }
                    }
                    var x1 = centreX - xDiff;
                    var x2 = centreX + xDiff;
                    var y1 = centreY - yDiff;
                    var y2 = centreY + yDiff;
                    var ring = new List<object> {
                        QgsPointXY(x1, y1),
                        QgsPointXY(x2, y1),
                        QgsPointXY(x2, y2),
                        QgsPointXY(x1, y2),
                        QgsPointXY(x1, y1)
                    };
                    var feature = QgsFeature();
                    feature.setFields(fields);
                    feature.setAttribute(idIndex, gridData.num);
                    feature.setAttribute(downIndex, gridData.downNum);
                    feature.setAttribute(areaIndex, gridData.area);
                    feature.setAttribute(outletIndex, gridData.outlet);
                    var geometry = QgsGeometry.fromPolygonXY(new List<List<object>> {
                        ring
                    });
                    feature.setGeometry(geometry);
                    features.append(feature);
                    if (inlets is not null) {
                        var inletCatchment = inlets.get(gridRow, new dict()).get(gridCol, -1);
                        if (inletCatchment > 0) {
                            var feature2 = QgsFeature();
                            feature2.setFields(fields2);
                            feature2.setAttribute(0, inletCatchment);
                            var geometry2 = QgsGeometry.fromPointXY(QgsPointXY(centreX, centreY));
                            feature2.setGeometry(geometry2);
                            features2.append(feature2);
                        }
                    }
                }
                if (!writer.addFeatures(features)) {
                    QSWATUtils.error("Unable to add features to grid shapefile {0}".format(gridFile), this._gv.isBatch);
                    return;
                }
                if (features2.Count > 0) {
                    if (!writer2.addFeatures(features2)) {
                        QSWATUtils.error("Unable to add features to inlets shapefile {0}".format(inletsFile), this._gv.isBatch);
                    }
                }
            }
            // load grid shapefile
            // need to release writer before making layer
            writer = null;
            QSWATUtils.copyPrj(flowFile, gridFile);
            if (inlets is not null) {
                writer2 = null;
                QSWATUtils.copyPrj(flowFile, inletsFile);
            }
            // make wshed layer active so loads above it
            var wshedTreeLayer = QSWATUtils.getLayerByLegend(QSWATUtils._WATERSHEDLEGEND, root.findLayers());
            if (wshedTreeLayer) {
                var wshedLayer = wshedTreeLayer.layer();
                Debug.Assert(wshedLayer is not null);
                this._iface.setActiveLayer(wshedLayer);
                QSWATUtils.setLayerVisibility(wshedLayer, false, root);
            }
            (gridLayer, loaded) = QSWATUtils.getLayerByFilename(root.findLayers(), gridFile, FileTypes._GRID, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            if (!gridLayer || !loaded) {
                QSWATUtils.error("Failed to load grid shapefile {0}".format(gridFile), this._gv.isBatch);
                return;
            }
            this._gv.wshedFile = gridFile;
            var styleFile = FileTypes.styleFile(FileTypes._GRID);
            Debug.Assert(styleFile is not null);
            gridLayer.loadNamedStyle(QSWATUtils.join(this._gv.plugin_dir, styleFile));
            // make grid active layer so streams layer comes above it.
            this._iface.setActiveLayer(gridLayer);
        }
        
        // Write grid data to grid streams shapefile.
        public virtual int writeGridStreamsShapefile(
            object storeGrid,
            string gridStreamsFile,
            string flowFile,
            double minDrainArea,
            double maxDrainArea,
            object accTransform) {
            object areaToPenWidth;
            this.progress("Writing grid streams ...");
            var root = QgsProject.instance().layerTreeRoot();
            var fields = QgsFields();
            fields.append(QgsField(QSWATTopology._LINKNO, QVariant.Int));
            fields.append(QgsField(QSWATTopology._DSLINKNO, QVariant.Int));
            fields.append(QgsField(QSWATTopology._WSNO, QVariant.Int));
            fields.append(QgsField(QSWATTopology._OUTLET, QVariant.Int));
            fields.append(QgsField("Drainage", QVariant.Double, len: 10, prec: 2));
            fields.append(QgsField(QSWATTopology._PENWIDTH, QVariant.Double));
            QSWATUtils.removeLayer(gridStreamsFile, root);
            var transform_context = QgsProject.instance().transformContext();
            var writer = QgsVectorFileWriter.create(gridStreamsFile, fields, QgsWkbTypes.LineString, this._gv.topo.crsProject, transform_context, this._gv.vectorFileWriterOptions);
            if (writer.hasError() != QgsVectorFileWriter.NoError) {
                QSWATUtils.error("Cannot create grid shapefile {0}: {1}".format(gridStreamsFile, writer.errorMessage()), this._gv.isBatch);
                return -1;
            }
            var linkIndex = fields.indexFromName(QSWATTopology._LINKNO);
            var downIndex = fields.indexFromName(QSWATTopology._DSLINKNO);
            var wsnoIndex = fields.indexFromName(QSWATTopology._WSNO);
            var outletIndex = fields.indexFromName(QSWATTopology._OUTLET);
            var drainIndex = fields.indexFromName("Drainage");
            var penIndex = fields.indexFromName(QSWATTopology._PENWIDTH);
            if (maxDrainArea > minDrainArea) {
                // guard against division by zero
                var rng = maxDrainArea - minDrainArea;
                areaToPenWidth = x => (x - minDrainArea) * 1.8 / rng + 0.2;
            } else {
                areaToPenWidth = _ => 1.0;
            }
            var numOutlets = 0;
            foreach (var gridCols in storeGrid.values()) {
                // grids can be big so we'll add one row at a time
                var features = new List<object>();
                foreach (var gridData in gridCols.values()) {
                    var downNum = gridData.downNum;
                    (sourceX, sourceY) = QSWATTopology.cellToProj(gridData.maxCol, gridData.maxRow, accTransform);
                    if (downNum > 0) {
                        var downData = storeGrid[gridData.downRow][gridData.downCol];
                        (targetX, targetY) = QSWATTopology.cellToProj(downData.maxCol, downData.maxRow, accTransform);
                    } else {
                        var targetX = sourceX;
                        var targetY = sourceY;
                        numOutlets += 1;
                    }
                    // respect default 'start at outlet' of TauDEM
                    var link = new List<object> {
                        QgsPointXY(targetX, targetY),
                        QgsPointXY(sourceX, sourceY)
                    };
                    var feature = QgsFeature();
                    feature.setFields(fields);
                    feature.setAttribute(linkIndex, gridData.num);
                    feature.setAttribute(downIndex, downNum);
                    feature.setAttribute(wsnoIndex, gridData.num);
                    feature.setAttribute(outletIndex, gridData.outlet);
                    // area needs coercion to float or will not write
                    feature.setAttribute(drainIndex, float(gridData.drainArea));
                    // set pen width to value in range 0 .. 2
                    feature.setAttribute(penIndex, float(areaToPenWidth(gridData.drainArea)));
                    var geometry = QgsGeometry.fromPolylineXY(link);
                    feature.setGeometry(geometry);
                    features.append(feature);
                }
                if (!writer.addFeatures(features)) {
                    QSWATUtils.error("Unable to add features to grid streams shapefile {0}".format(gridStreamsFile), this._gv.isBatch);
                    return -1;
                }
            }
            // flush writer
            writer.flushBuffer();
            WONKO_del(writer);
            // load grid streams shapefile
            QSWATUtils.copyPrj(flowFile, gridStreamsFile);
            //styleFile = FileTypes.styleFile(FileTypes._GRIDSTREAMS)
            // try to load above grid layer
            var gridLayer = QSWATUtils.getLayerByLegend(QSWATUtils._GRIDLEGEND, root.findLayers());
            var gridStreamsLayer = QSWATUtils.getLayerByFilename(root.findLayers(), gridStreamsFile, FileTypes._GRIDSTREAMS, this._gv, gridLayer, QSWATUtils._WATERSHED_GROUP_NAME)[0];
            if (!gridStreamsLayer) {
                QSWATUtils.error("Failed to load grid streams shapefile {0}".format(gridStreamsFile), this._gv.isBatch);
                return -1;
            }
            Debug.Assert(gridStreamsLayer is QgsVectorLayer);
            //gridStreamsLayer.loadNamedStyle(QSWATUtils.join(self._gv.plugin_dir, styleFile))
            // make stream width dependent on drainage values (drainage is accumulation, ie number of dem cells draining to start of stream)
            var numClasses = 5;
            var props = new Dictionary<object, object> {
                {
                    "width_expression",
                    QSWATTopology._PENWIDTH}};
            var symbol = QgsLineSymbol.createSimple(props);
            //style = QgsStyleV2().defaultStyle()
            //ramp = style.colorRamp('Blues')
            // ramp from light to darkish blue
            var color1 = QColor(166, 206, 227, 255);
            var color2 = QColor(0, 0, 255, 255);
            var ramp = QgsGradientColorRamp(color1, color2);
            var labelFmt = QgsRendererRangeLabelFormat("%1 - %2", 0);
            var renderer = QgsGraduatedSymbolRenderer.createRenderer(gridStreamsLayer, "Drainage", numClasses, QgsGraduatedSymbolRenderer.Jenks, symbol, ramp, labelFmt);
            gridStreamsLayer.setRenderer(renderer);
            gridStreamsLayer.setOpacity(1);
            gridStreamsLayer.triggerRepaint();
            var treeModel = QgsLayerTreeModel(root);
            var gridStreamsTreeLayer = root.findLayer(gridStreamsLayer.id());
            Debug.Assert(gridStreamsTreeLayer is not null);
            treeModel.refreshLayerLegend(gridStreamsTreeLayer);
            this._gv.streamFile = gridStreamsFile;
            this.progress("");
            return numOutlets;
        }
        
        // Return row and column after 1 step in D8 direction.
        [staticmethod]
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
        //     demPath = QSWATUtils.layerFileInfo(demLayer).absolutePath()
        //     rasterFile = QSWATUtils.join(os.path.splitext(demPath)[0], 'streams.tif')
        //     ok, path = QSWATUtils.removeLayerAndFiles(wFile, root)
        //     if not ok:
        //         QSWATUtils.error('Failed to remove {0}: try repeating last click, else remove manually.'.format(path), self._gv.isBatch)
        //         self._dlg.setCursor(Qt.ArrowCursor)
        //         return ''
        //     assert not os.path.exists(rasterFile)
        //     extent = demLayer.extent()
        //     xMin = extent.xMinimum()
        //     xMax = extent.xMaximum()
        //     yMin = extent.yMinimum()
        //     yMax = extent.yMaximum()
        //     xSize = demLayer.rasterUnitsPerPixelX()
        //     ySize = demLayer.rasterUnitsPerPixelY()
        //     command = 'gdal_rasterize -burn 1 -a_nodata -9999 -te {0} {1} {2} {3} -tr {4} {5} -ot Int32 "{6}" "{7}"'.format(xMin, yMin, xMax, yMax, xSize, ySize, streamFile, rasterFile)
        //     QSWATUtils.information(command, self._gv.isBatch)
        //     os.system(command)
        //     assert os.path.exists(rasterFile)
        //     QSWATUtils.copyPrj(streamFile, rasterFile)
        //     return rasterFile
        //===========================================================================
        // Create inlets/outlets file with points snapped to stream reaches.
        public virtual bool createSnapOutletFile(
            object outletLayer,
            object streamLayer,
            string outletFile,
            string snapFile,
            object root) {
            if (outletLayer.featureCount() == 0) {
                QSWATUtils.error("The outlet layer {0} has no points".format(outletLayer.name()), this._gv.isBatch);
                return false;
            }
            try {
                var snapThreshold = Convert.ToInt32(this._dlg.snapThreshold.text());
            } catch (Exception) {
                QSWATUtils.error("Cannot parse snap threshold {0} as integer.".format(this._dlg.snapThreshold.text()), this._gv.isBatch);
                return false;
            }
            if (!this.createOutletFile(snapFile, outletFile, false, root)) {
                return false;
            }
            if (this._gv.isBatch) {
                QSWATUtils.information("Snap threshold: {0} metres".format(snapThreshold), this._gv.isBatch);
            }
            var idIndex = this._gv.topo.getIndex(outletLayer, QSWATTopology._ID);
            var inletIndex = this._gv.topo.getIndex(outletLayer, QSWATTopology._INLET);
            var resIndex = this._gv.topo.getIndex(outletLayer, QSWATTopology._RES);
            var ptsourceIndex = this._gv.topo.getIndex(outletLayer, QSWATTopology._PTSOURCE);
            var snapLayer = QgsVectorLayer(snapFile, "snapped points", "ogr");
            var idSnapIndex = this._gv.topo.getIndex(snapLayer, QSWATTopology._ID);
            var inletSnapIndex = this._gv.topo.getIndex(snapLayer, QSWATTopology._INLET);
            var resSnapIndex = this._gv.topo.getIndex(snapLayer, QSWATTopology._RES);
            var ptsourceSnapIndex = this._gv.topo.getIndex(snapLayer, QSWATTopology._PTSOURCE);
            var snapProvider = snapLayer.dataProvider();
            var fields = snapProvider.fields();
            var count = 0;
            var errorCount = 0;
            var outletCount = 0;
            foreach (var feature in outletLayer.getFeatures()) {
                var point = feature.geometry().asPoint();
                var point1 = QSWATTopology.snapPointToReach(streamLayer, point, snapThreshold, this._gv.isBatch);
                if (point1 is null) {
                    errorCount += 1;
                    continue;
                }
                var attrs = feature.attributes();
                var pid = attrs[idIndex];
                var inlet = attrs[inletIndex];
                var res = attrs[resIndex];
                var ptsource = attrs[ptsourceIndex];
                if (inlet == 0 && res == 0) {
                    outletCount += 1;
                }
                // QSWATUtils.information('Snap point at ({0:.2F}, {1:.2F})'.format(point1.x(), point1.y()), self._gv.isBatch)
                var feature1 = QgsFeature();
                feature1.setFields(fields);
                feature1.setAttribute(idSnapIndex, pid);
                feature1.setAttribute(inletSnapIndex, inlet);
                feature1.setAttribute(resSnapIndex, res);
                feature1.setAttribute(ptsourceSnapIndex, ptsource);
                feature1.setGeometry(QgsGeometry.fromPointXY(QgsPointXY(point1.x(), point1.y())));
                (ok, _) = snapProvider.addFeatures(new List<object> {
                    feature1
                });
                if (!ok) {
                    QSWATUtils.error("Failed to add snap point", this._gv.isBatch);
                }
                count += 1;
            }
            var failMessage = errorCount == 0 ? "" : ": {0} failed".format(errorCount);
            this._dlg.snappedLabel.setText("{0} snapped{1}".format(count, failMessage));
            if (this._gv.isBatch) {
                QSWATUtils.information("{0} snapped{1}".format(count, failMessage), true);
            }
            if (count == 0) {
                QSWATUtils.error("Could not snap any points to stream reaches", this._gv.isBatch);
                return false;
            }
            if (outletCount == 0) {
                QSWATUtils.error("Your outlet layer {0} contains no outlets".format(outletLayer.name()), this._gv.isBatch);
                return false;
            }
            // shows we have created a snap file
            this.snapFile = snapFile;
            this.snapErrors = errorCount > 0;
            return true;
        }
        
        //===========================================================================
        // @staticmethod
        // def createOutletFields(subWanted: bool) -> QgsFields:
        //     """Return felds for inlets/outlets file, adding Subbasin field if wanted."""
        //     fields = QgsFields()
        //     fields.append(QgsField(QSWATTopology._ID, QVariant.Int))
        //     fields.append(QgsField(QSWATTopology._INLET, QVariant.Int))
        //     fields.append(QgsField(QSWATTopology._RES, QVariant.Int))
        //     fields.append(QgsField(QSWATTopology._PTSOURCE, QVariant.Int))
        //     if subWanted:
        //         fields.append(QgsField(QSWATTopology._SUBBASIN, QVariant.Int))
        //     return fields
        //===========================================================================
        //===========================================================================
        // def createOutletFile(self, filePath: str, sourcePath: str, subWanted: bool, root: QgsLayerTreeGroup) -> Tuple[Optional[QgsVectorFileWriter], QgsFields]:
        //     """Create filePath with fields needed for outlets file, 
        //     copying .prj from sourcePath, and adding Subbasin field if wanted.
        //     """
        //     QSWATUtils.tryRemoveLayerAndFiles(filePath, root)
        //     fields = Delineation.createOutletFields(subWanted)
        //     transform_context = QgsProject.instance().transformContext()
        //     writer = QgsVectorFileWriter.create(filePath, fields, QgsWkbTypes.Point, self._gv.topo.crsProject,
        //                                         transform_context, self._gv.vectorFileWriterOptions)
        //     if writer.hasError() != QgsVectorFileWriter.NoError:
        //         QSWATUtils.error('Cannot create outlets shapefile {0}: {1}'.format(filePath, writer.errorMessage()), self._gv.isBatch)
        //         return None, fields
        //     QSWATUtils.copyPrj(sourcePath, filePath)
        //     return writer, fields
        //===========================================================================
        // Create filePath with fields needed for outlets file, 
        //         copying .prj from sourcePath, and adding Subbasin field if wanted.
        //         
        //         Uses OGR since QgsVectorFileWriter.create seems to be broken.
        //         
        public virtual bool createOutletFile(string filePath, string sourcePath, bool subWanted, object root) {
            (ok, path) = QSWATUtils.removeLayerAndFiles(filePath, root);
            if (!ok) {
                QSWATUtils.error("Failed to remove old inlet/outlet file {0}: try repeating last click, else remove manually.".format(path), this._gv.isBatch);
                this._dlg.setCursor(Qt.ArrowCursor);
                return false;
            }
            var shpDriver = ogr.GetDriverByName("ESRI Shapefile");
            if (os.path.exists(filePath)) {
                shpDriver.DeleteDataSource(filePath);
            }
            try {
                var outDataSource = shpDriver.CreateDataSource(filePath);
                var outLayer = outDataSource.CreateLayer(filePath, geom_type: ogr.wkbPoint);
                var idField = ogr.FieldDefn(QSWATTopology._ID, ogr.OFTInteger);
                outLayer.CreateField(idField);
                var inletField = ogr.FieldDefn(QSWATTopology._INLET, ogr.OFTInteger);
                outLayer.CreateField(inletField);
                var resField = ogr.FieldDefn(QSWATTopology._RES, ogr.OFTInteger);
                outLayer.CreateField(resField);
                var ptsourceField = ogr.FieldDefn(QSWATTopology._PTSOURCE, ogr.OFTInteger);
                outLayer.CreateField(ptsourceField);
                if (subWanted) {
                    var subField = ogr.FieldDefn(QSWATTopology._SUBBASIN, ogr.OFTInteger);
                    outLayer.CreateField(subField);
                }
                QSWATUtils.copyPrj(sourcePath, filePath);
                return true;
            } catch (Exception) {
                QSWATUtils.error("Failure to create points file: {0}".format(traceback.format_exc()), this._gv.isBatch);
                return false;
            }
        }
        
        // Get list of ID values from inlets/outlets layer 
        //         for which field has value 1.
        //         
        public virtual object getOutletIds(string field) {
            var result = new HashSet<object>();
            if (this._gv.outletFile == "") {
                return result;
            }
            var root = QgsProject.instance().layerTreeRoot();
            var outletLayer = QSWATUtils.getLayerByFilenameOrLegend(root.findLayers(), this._gv.outletFile, FileTypes._OUTLETS, "", this._gv.isBatch);
            if (!outletLayer) {
                QSWATUtils.error("Cannot find inlets/outlets layer", this._gv.isBatch);
                return result;
            }
            Debug.Assert(outletLayer is QgsVectorLayer);
            var idIndex = this._gv.topo.getIndex(outletLayer, QSWATTopology._ID);
            var fieldIndex = this._gv.topo.getIndex(outletLayer, field);
            foreach (var f in outletLayer.getFeatures()) {
                var attrs = f.attributes();
                if (attrs[fieldIndex] == 1) {
                    result.add(attrs[idIndex]);
                }
            }
            return result;
        }
        
        // Update progress label with message; emit message for display in testing.
        public virtual object progress(string msg) {
            QSWATUtils.progress(msg, this._dlg.progressLabel);
            if (msg != "") {
                this.progress_signal.emit(msg);
            }
        }
        
        public object progress_signal = pyqtSignal(str);
        
        // Close form.
        public virtual object doClose() {
            this._dlg.close();
        }
        
        // Read delineation data from project file.
        public virtual object readProj() {
            object extraOutletFile;
            object outletFile;
            object streamFile;
            object burnFile;
            object wshedFile;
            object possFile;
            object layer;
            object treeLayer;
            object demFile;
            var proj = QgsProject.instance();
            var title = proj.title();
            var root = QgsProject.instance().layerTreeRoot();
            this._dlg.tabWidget.setCurrentIndex(0);
            (this._gv.existingWshed, found) = proj.readBoolEntry(title, "delin/existingWshed", false);
            if (found && this._gv.existingWshed) {
                this._dlg.tabWidget.setCurrentIndex(1);
            }
            QSWATUtils.loginfo("Existing watershed is {0}".format(this._gv.existingWshed));
            (this._gv.useGridModel, _) = proj.readBoolEntry(title, "delin/useGridModel", false);
            QSWATUtils.loginfo("Use grid model is {0}".format(this._gv.useGridModel));
            if (this._gv.useGridModel) {
                (gridSize, found) = proj.readNumEntry(title, "delin/gridSize", 1);
                if (found) {
                    this._dlg.GridSize.setValue(gridSize);
                    this._gv.gridSize = gridSize;
                }
            }
            (demFile, found) = proj.readEntry(title, "delin/DEM", "");
            object demLayer = null;
            if (found && demFile != "") {
                demFile = QSWATUtils.join(this._gv.projDir, demFile);
                (demLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), demFile, FileTypes._DEM, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            } else {
                treeLayer = QSWATUtils.getLayerByLegend(FileTypes.legend(FileTypes._DEM), root.findLayers());
                if (treeLayer is not null) {
                    layer = treeLayer.layer();
                    possFile = QSWATUtils.layerFileInfo(layer).absoluteFilePath();
                    if (QSWATUtils.question("Use {0} as {1} file?".format(possFile, FileTypes.legend(FileTypes._DEM)), this._gv.isBatch, true) == QMessageBox.Yes) {
                        demLayer = layer;
                        demFile = possFile;
                    }
                }
            }
            if (demLayer) {
                this._gv.demFile = demFile;
                this._dlg.selectDem.setText(this._gv.demFile);
                Debug.Assert(demLayer is QgsRasterLayer);
                this.setDefaultNumCells(demLayer);
            } else {
                this._gv.demFile = "";
            }
            (verticalUnits, found) = proj.readEntry(title, "delin/verticalUnits", Parameters._METRES);
            if (found) {
                this._gv.verticalUnits = verticalUnits;
                this._gv.setVerticalFactor();
            }
            (threshold, found) = proj.readNumEntry(title, "delin/threshold", 0);
            if (found && threshold > 0) {
                try {
                    this._dlg.numCells.setText(threshold.ToString());
                } catch (Exception) {
                }
            }
            (snapThreshold, found) = proj.readNumEntry(title, "delin/snapThreshold", 300);
            this._dlg.snapThreshold.setText(snapThreshold.ToString());
            (wshedFile, found) = proj.readEntry(title, "delin/wshed", "");
            object wshedLayer = null;
            var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
            if (found && wshedFile != "") {
                wshedFile = QSWATUtils.join(this._gv.projDir, wshedFile);
                (wshedLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), wshedFile, ft, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            } else {
                treeLayer = QSWATUtils.getLayerByLegend(FileTypes.legend(ft), root.findLayers());
                if (treeLayer is not null) {
                    layer = treeLayer.layer();
                    possFile = QSWATUtils.layerFileInfo(layer).absoluteFilePath();
                    if (QSWATUtils.question("Use {0} as {1} file?".format(possFile, FileTypes.legend(ft)), this._gv.isBatch, true) == QMessageBox.Yes) {
                        wshedLayer = layer;
                        wshedFile = possFile;
                    }
                }
            }
            if (wshedLayer) {
                this._dlg.selectWshed.setText(wshedFile);
                this._gv.wshedFile = wshedFile;
            } else {
                this._gv.wshedFile = "";
            }
            (burnFile, found) = proj.readEntry(title, "delin/burn", "");
            object burnLayer = null;
            if (found && burnFile != "") {
                burnFile = QSWATUtils.join(this._gv.projDir, burnFile);
                (burnLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), burnFile, FileTypes._BURN, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            } else {
                treeLayer = QSWATUtils.getLayerByLegend(FileTypes.legend(FileTypes._BURN), root.findLayers());
                if (treeLayer is not null) {
                    layer = treeLayer.layer();
                    possFile = QSWATUtils.layerFileInfo(layer).absoluteFilePath();
                    if (QSWATUtils.question("Use {0} as {1} file?".format(possFile, FileTypes.legend(FileTypes._BURN)), this._gv.isBatch, true) == QMessageBox.Yes) {
                        burnLayer = layer;
                        burnFile = possFile;
                    }
                }
            }
            if (burnLayer) {
                this._gv.burnFile = burnFile;
                this._dlg.checkBurn.setChecked(true);
                this._dlg.selectBurn.setText(burnFile);
            } else {
                this._gv.burnFile = "";
            }
            (streamFile, found) = proj.readEntry(title, "delin/net", "");
            object streamLayer = null;
            if (found && streamFile != "") {
                streamFile = QSWATUtils.join(this._gv.projDir, streamFile);
                (streamLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), streamFile, FileTypes._STREAMS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            } else {
                treeLayer = QSWATUtils.getLayerByLegend(FileTypes.legend(FileTypes._STREAMS), root.findLayers());
                if (treeLayer) {
                    layer = treeLayer.layer();
                    possFile = QSWATUtils.layerFileInfo(layer).absoluteFilePath();
                    if (QSWATUtils.question("Use {0} as {1} file?".format(possFile, FileTypes.legend(FileTypes._STREAMS)), this._gv.isBatch, true) == QMessageBox.Yes) {
                        streamLayer = layer;
                        streamFile = possFile;
                    }
                }
            }
            if (streamLayer) {
                this._dlg.selectNet.setText(streamFile);
                this._gv.streamFile = streamFile;
            } else {
                this._gv.streamFile = "";
            }
            (useOutlets, found) = proj.readBoolEntry(title, "delin/useOutlets", true);
            if (found) {
                this._dlg.useOutlets.setChecked(useOutlets);
                this.changeUseOutlets();
            }
            (outletFile, found) = proj.readEntry(title, "delin/outlets", "");
            object outletLayer = null;
            if (found && outletFile != "") {
                outletFile = QSWATUtils.join(this._gv.projDir, outletFile);
                (outletLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), outletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            } else {
                treeLayer = QSWATUtils.getLayerByLegend(FileTypes.legend(FileTypes._OUTLETS), root.findLayers());
                if (treeLayer) {
                    layer = treeLayer.layer();
                    possFile = QSWATUtils.layerFileInfo(layer).absoluteFilePath();
                    if (QSWATUtils.question("Use {0} as {1} file?".format(possFile, FileTypes.legend(FileTypes._OUTLETS)), this._gv.isBatch, true) == QMessageBox.Yes) {
                        outletLayer = layer;
                        outletFile = possFile;
                    }
                }
            }
            if (outletLayer) {
                this._gv.outletFile = outletFile;
                this._dlg.selectExistOutlets.setText(this._gv.outletFile);
                this._dlg.selectOutlets.setText(this._gv.outletFile);
            } else {
                this._gv.outletFile = "";
            }
            (extraOutletFile, found) = proj.readEntry(title, "delin/extraOutlets", "");
            object extraOutletLayer = null;
            if (found && extraOutletFile != "") {
                extraOutletFile = QSWATUtils.join(this._gv.projDir, extraOutletFile);
                (extraOutletLayer, _) = QSWATUtils.getLayerByFilename(root.findLayers(), extraOutletFile, FileTypes._OUTLETS, this._gv, null, QSWATUtils._WATERSHED_GROUP_NAME);
            } else {
                treeLayer = QSWATUtils.getLayerByLegend(QSWATUtils._EXTRALEGEND, root.findLayers());
                if (treeLayer) {
                    layer = treeLayer.layer();
                    possFile = QSWATUtils.layerFileInfo(layer).absoluteFilePath();
                    if (QSWATUtils.question("Use {0} as {1} file?".format(possFile, QSWATUtils._EXTRALEGEND), this._gv.isBatch, true) == QMessageBox.Yes) {
                        extraOutletLayer = layer;
                        extraOutletFile = possFile;
                    }
                }
            }
            if (extraOutletLayer) {
                Debug.Assert(extraOutletLayer is QgsVectorLayer);
                this._gv.extraOutletFile = extraOutletFile;
                var basinIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._SUBBASIN);
                var resIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._RES);
                var ptsrcIndex = this._gv.topo.getIndex(extraOutletLayer, QSWATTopology._PTSOURCE);
                if (basinIndex >= 0 && resIndex >= 0 && ptsrcIndex >= 0) {
                    var extraPointSources = false;
                    foreach (var point in extraOutletLayer.getFeatures()) {
                        var attrs = point.attributes();
                        if (attrs[resIndex] == 1) {
                            this.extraReservoirBasins.add(attrs[basinIndex]);
                        } else if (attrs[ptsrcIndex] == 1) {
                            extraPointSources = true;
                        }
                    }
                    this._dlg.checkAddPoints.setChecked(extraPointSources);
                }
            } else {
                this._gv.extraOutletFile = "";
            }
        }
        
        // Write delineation data to project file.
        public virtual object saveProj() {
            object snapThreshold;
            object numCells;
            var proj = QgsProject.instance();
            var title = proj.title();
            proj.writeEntry(title, "delin/existingWshed", this._gv.existingWshed);
            // grid model not official in version >= 1.4 , so normally keep invisible
            if (this._dlg.useGrid.isVisible()) {
                proj.writeEntry(title, "delin/useGridModel", this._gv.useGridModel);
                proj.writeEntry(title, "delin/gridSize", this._dlg.GridSize.value());
            }
            proj.writeEntry(title, "delin/net", QSWATUtils.relativise(this._gv.streamFile, this._gv.projDir));
            proj.writeEntry(title, "delin/wshed", QSWATUtils.relativise(this._gv.wshedFile, this._gv.projDir));
            proj.writeEntry(title, "delin/DEM", QSWATUtils.relativise(this._gv.demFile, this._gv.projDir));
            proj.writeEntry(title, "delin/useOutlets", this._dlg.useOutlets.isChecked());
            proj.writeEntry(title, "delin/outlets", QSWATUtils.relativise(this._gv.outletFile, this._gv.projDir));
            proj.writeEntry(title, "delin/extraOutlets", QSWATUtils.relativise(this._gv.extraOutletFile, this._gv.projDir));
            proj.writeEntry(title, "delin/burn", QSWATUtils.relativise(this._gv.burnFile, this._gv.projDir));
            try {
                numCells = Convert.ToInt32(this._dlg.numCells.text());
            } catch (Exception) {
                numCells = 0;
            }
            proj.writeEntry(title, "delin/verticalUnits", this._gv.verticalUnits);
            proj.writeEntry(title, "delin/threshold", numCells);
            try {
                snapThreshold = Convert.ToInt32(this._dlg.snapThreshold.text());
            } catch (Exception) {
                snapThreshold = 300;
            }
            proj.writeEntry(title, "delin/snapThreshold", snapThreshold);
            proj.write();
        }
    }
}
