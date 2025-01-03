

using System;
using System.Data;

using System.Diagnostics;

using System.Linq;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Path = System.IO.Path;
using System.Data.OleDb;
using System.Threading.Tasks;
using System.Windows.Forms;

using ArcGIS.Desktop.Mapping;
using Layer = ArcGIS.Desktop.Mapping.Layer;
using OSGeo.GDAL;
using OSGeo.OGR;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Windows.Documents;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Core.CommonControls;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Core.CIM;
using System.Runtime.ConstrainedExecution;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Internal.Mapping.Voxel.Controls.Transparency;
using System.DirectoryServices.ActiveDirectory;
using ArcGIS.Desktop.Layouts;
using System.Windows.Media.Animation;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Data.SQLite;

namespace ArcSWAT3 {

    // Support visualisation of SWAT outputs, using data in SWAT output database.
    public class Visualise {

        public CompareScenariosForm _comparedlg;

        public VisualiseForm _dlg;

        public GlobalVars _gv;

        public List<double> allAnimateVals;

        public string animateFile;

        public Dictionary<string, int> animateIndexes;

        public Dictionary<string, string> animateVars;

        public FeatureLayer animateLayer;

        public Timer animateTimer;

        public string animateVar;

        public bool animating;

        public object animationDOM;

        public object animationLayout;

        public bool animationPaused;

        public object animationTemplate;

        public bool animationTemplateDirty;

        public Dictionary<int, double> areas;

        public bool capturing;

        public int compositionCount;

        public DbConnection conn;

        public FeatureLayer currentResultsLayer;

        public int currentStillNumber;

        public string db;

        public int finishDay;

        public int finishMonth;

        public int finishYear;

        public bool hasAreas;

        public Dictionary<int, int> hruNums;

        public FeatureLayer hruResultsLayer;

        public int HRUsSetting;

        public List<string> ignoredVars;

        public bool internalChangeToHRURenderer;

        public bool internalChangeToRivRenderer;

        public bool internalChangeToSubRenderer;

        public bool isAnnual;

        public bool isDaily;

        public int julianFinishDay;

        public int julianStartDay;

        public bool keepHRUColours;

        public bool keepRivColours;

        public bool keepSubColours;

        public string mapTitle;

        public int numSubbasins;

        public int numYears;

        public string observedFileName;

        public int periodDays;

        public double periodMonths;

        public bool periodsUpToDate;

        public double periodYears;

        public Dictionary<int, Dictionary<string, double>> staticResultsData;

        public Dictionary<string, Dictionary<int, Dictionary<int, double>>> animateResultsData;

        public string resultsFile;

        public bool resultsFileUpToDate;

        public FeatureLayer rivResultsLayer;

        public string scenario;

        public string scenario1;

        public string scenario2;

        public int startDay;

        public int startMonth;

        public int startYear;

        public Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<int, Dictionary<int, double>>>>> staticData;

        public string stillFileBase;

        public FeatureLayer subResultsLayer;

        public bool summaryChanged;

        public string table;

        public string title;

        public string videoFile;

        public static string _TOTALS = "Totals";

        public static string _DAILYMEANS = "Daily means";

        public static string _MONTHLYMEANS = "Monthly means";

        public static string _ANNUALMEANS = "Annual means";

        public static string _MAXIMA = "Maxima";

        public static string _MINIMA = "Minima";

        public static string _AREA = "AREAkm2";

        public static List<string> _MONTHS = new List<string> {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };

        public static string _NORTHARROW = "apps/qgis/svg/wind_roses/WindRose_01.svg";

        public static string _CHOOSEPLOT = "Plot type";

        public static string _GRAPHORBAR = "Graph/bar chart";

        public static string _FLOWDURATION = "Duration curve";

        public static string _SCATTER = "Scatter plot";

        public static string _BOX = "Box plot";

        private DataTable plotData;

        public Visualise(GlobalVars gv) {
            this._gv = gv;
            this._dlg = new VisualiseForm(this);
            this._comparedlg = new CompareScenariosForm(this);
            //# variables found in various tables that do not contain values used in results
            this.ignoredVars = new List<string> {
                "LULC",
                "HRU",
                "HRUGIS",
                "",
                "SUB",
                "RCH",
                "YEAR",
                "MON",
                "DAY",
                Visualise._AREA,
                "YYYYDDD",
                "YYYYMM"
            };
            //# current scenario
            this.scenario = "";
            //# Current output database
            this.db = "";
            //# Current connection
            this.conn = null;
            //# Current table
            this.table = "";
            //# Number of subbasins in current watershed
            this.numSubbasins = 0;
            //# Data read from db table
            //
            // data takes the form
            // layerId -> subbasin_number -> variable_name -> year -> month -> value
            this.staticData = new Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<int, Dictionary<int, double>>>>>();
            //# Data to write to shapefile
            //
            // takes the form subbasin number -> variable_name -> value for static use
            // takes the form layerId -> date -> subbasin number -> val for animation
            // where date is YYYY or YYYYMM or YYYYDDD according to period of input
            this.staticResultsData = new Dictionary<int, Dictionary<string, double>>();
            this.animateResultsData = new Dictionary<string, Dictionary<int, Dictionary<int, double>>>();
            //# Areas of subbasins (drainage area for reaches)
            this.areas = new Dictionary<int, double>();
            //# Flag to indicate areas available
            this.hasAreas = false;
            // data from cio file
            //# number of years in simulation
            this.numYears = 0;
            //# true if output is daily
            this.isDaily = false;
            //# true if output is annual
            this.isAnnual = false;
            //# julian start day
            this.julianStartDay = 0;
            //# julian finish day
            this.julianFinishDay = 0;
            //# start year of period (of output: includes any nyskip)
            this.startYear = 0;
            //# start month of period
            this.startMonth = 0;
            //# start day of period
            this.startDay = 0;
            //# finish year of period
            this.finishYear = 0;
            //# finish month of period
            this.finishMonth = 0;
            //# finish day of period
            this.finishDay = 0;
            //# length of simulation in days
            this.periodDays = 0;
            //# length of simulation in months (may be fractional)
            this.periodMonths = 0.0;
            //# length of simulation in years (may be fractional)
            this.periodYears = 0.0;
            //# map canvas title
            this.mapTitle = null;
            //# flag to decide if we need to create a new results file:
            // changes to summary method or result variable don't need a new file
            this.resultsFileUpToDate = false;
            //# flag to decide if we need to reread data because period has changed
            this.periodsUpToDate = false;
            //# current streams results layer
            this.rivResultsLayer = null;
            //# current subbasins results layer
            this.subResultsLayer = null;
            //# current HRUs results layer
            this.hruResultsLayer = null;
            //# current results layer: equal to one of the riv, sub, lsu or hruResultsLayer
            this.currentResultsLayer = null;
            //# current resultsFile
            this.resultsFile = "";
            //# flag to indicate if summary has changed since last write to results file
            this.summaryChanged = true;
            //# current animation layer
            this.animateLayer = null;
            //# current animation file (a temporary file)
            this.animateFile = "";
            //# map layerId -> index of animation variable in results file
            this.animateIndexes = new Dictionary<string, int>();
            //# map layerId -> name of animation variable in results file
            this.animateVars = new Dictionary<string, string>();
            //# all values involved in animation, for calculating Jenks breaks
            this.allAnimateVals = new List<double>();
            //# timer used to run animation
            this.animateTimer = this._dlg.Ptimer;
            //# flag to indicate if animation running
            this.animating = false;
            //# flag to indicate if animation paused
            this.animationPaused = false;
            //# animation variable
            this.animateVar = "";
            //# flag to indicate if capturing video
            this.capturing = false;
            //# base filename of capture stills
            this.stillFileBase = "";
            //# name of latest video file
            this.videoFile = "";
            //# number of next still frame
            this.currentStillNumber = 0;
            //# flag to indicate if stream renderer being changed by code
            this.internalChangeToRivRenderer = false;
            //# flag to indicate if subbasin renderer being changed by code
            this.internalChangeToSubRenderer = false;
            //# flag to indicate if HRU renderer being changed by code
            this.internalChangeToHRURenderer = false;
            //# flag to indicate if colours for rendering streams should be inherited from existing results layer
            this.keepRivColours = false;
            //# flag to indicate if colours for rendering subbasins should be inherited from existing results layer
            this.keepSubColours = false;
            //# flag to indicate if colours for rendering HRUs should be inherited from existing results layer
            this.keepHRUColours = false;
            //# flag for HRU results for current scenario: 0 for limited HRUs or multiple but no hru table; 1 for single HRUs; 2 for multiple
            this.HRUsSetting = 0;
            //# table of numbers of HRU in each subbasin (empty if no hru table)
            this.hruNums = new Dictionary<int, int>();
            //# file with observed data for plotting
            this.observedFileName = "";
            //# project title
            this.title = "";
            //# count to keep composition titles unique
            this.compositionCount = 0;
            //# animation layout
            this.animationLayout = null;
            //# animation template DOM document
            this.animationDOM = null;
            //# animation template file
            this.animationTemplate = "";
            //# flag to show when user has perhaps changed the animation template
            this.animationTemplateDirty = false;
            // layout animation not possible in ArcSWAT
            this._dlg.removeAnimationMode();
            // empty animation and png directories
            this.clearAnimationDir();
            this.clearPngDir();
            //# first scenario to compare
            this.scenario1 = "";
            //# second scenario to compare
            this.scenario2 = "";
        }

        // Initialise the visualise form.
        public virtual async Task init() {
            this.setSummary();
            this.fillScenarios();
            this._dlg.PscenariosCombo.SelectedIndex = this._dlg.PscenariosCombo.Items.IndexOf("Default");
            if (this.db == "") {
                this.setupDb();
            }
            this._dlg.PtabWidget.SelectedIndex = 0;
            this.changeAnimationMode();
            this.setupPlot();
            await this.setBackgroundLayers();
            // check we have streams and watershed
            var group = Utils.getGroupLayerByName(Utils._WATERSHED_GROUP_NAME);
            //if (this._gv.forTNC) {
            //    var subsFile = Utils.join(this._gv.tablesOutDir, Parameters._SUBS + ".shp");
            //    var subsLayer = Utils.getLayerByLegend("Subbasin grid");
            //    var OK = true;
            //    if (subsLayer is null) {
            //        using (var subsDs = Ogr.Open(subsFile, 1)) { 
            //            if (!this._gv.isCatchmentProject) {
            //                var layer = subsDs.GetLayerByIndex(0);
            //                var defn = layer.GetLayerDefn();
            //                var catchmentIndex = defn.GetFieldIndex("Catchment");
            //                if (catchmentIndex < 0) {
            //                    layer.CreateField(new FieldDefn("Catchment", FieldType.OFTInteger), 1);
            //                    defn = layer.GetLayerDefn();
            //                    var subIndex = defn.GetFieldIndex(Topology._SUBBASIN);
            //                    catchmentIndex = defn.GetFieldIndex("Catchment");
            //                    var sql = "SELECT Subbasin, CatchmentId FROM Watershed";
            //                    var subToCatchment = new Dictionary<int, int>();
            //                    using (DbDataReader reader = DBUtils.getReader(this.conn, sql)) {
            //                        if (reader.HasRows) {
            //                            while (reader.Read()) {
            //                                subToCatchment[Convert.ToInt32(reader.GetValue(0))] = Convert.ToInt32(reader.GetValue(1));
            //                            }
            //                        }
            //                    }
            //                    layer.ResetReading();
            //                    do {
            //                        var f = layer.GetNextFeature();
            //                        if (f == null) { break; }
            //                        var subbasin = f.GetFieldAsInteger(subIndex);
            //                        var catchment = subToCatchment[subbasin];
            //                        f.SetField(subIndex, catchment);
            //                    } while (true);
            //                }
            //            }
            //        }
            //        (subsLayer, _) = await Utils.getLayerByFilename(subsFile, FileTypes._SUBBASINS, this._gv, null, Utils._WATERSHED_GROUP_NAME);
            //    }
            //    if (OK && !this._gv.isCatchmentProject) {
            //        var catchmentsTreeLayer = Utils.getLayerByLegend("Catchments", root.findLayers());
            //        if (catchmentsTreeLayer) {
            //            var catchmentsLayer = catchmentsTreeLayer.layer();
            //        } else {
            //            var catchmentsFile = Utils.join(this._gv.tablesOutDir, "catchments.shp");
            //            if (!File.Exists(catchmentsFile)) {
            //                Processing.initialize();
            //                if (!(from p in QgsApplication.processingRegistry().providers()
            //                    select p.id()).ToList().Contains("native")) {
            //                    QgsApplication.processingRegistry().addProvider(QgsNativeAlgorithms());
            //                }
            //                var context = QgsProcessingContext();
            //                processing.run("native:dissolve", new Dictionary<object, object> {
            //                    {
            //                        "INPUT",
            //                        subsFile},
            //                    {
            //                        "FIELD",
            //                        new List<string> {
            //                            "Catchment"
            //                        }},
            //                    {
            //                        "OUTPUT",
            //                        catchmentsFile}}, context: context);
            //            }
            //            catchmentsLayer = QgsVectorLayer(catchmentsFile, "Catchments", "ogr");
            //            catchmentsLayer = cast(QgsVectorLayer, proj.addMapLayer(catchmentsLayer, false));
            //            Debug.Assert(group is not null);
            //            group.insertLayer(0, catchmentsLayer);
            //        }
            //        catchmentsLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, "catchments.qml"));
            //        catchmentsLayer.setMapTipTemplate("<b>Catchment:</b> [% \"Catchment\" %]");
            //    }
            //} else {
            var wshedLayer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._WATERSHED));
            if (wshedLayer is null) {
                var wshedFile = Path.Combine(this._gv.shapesDir, "subs1.shp");
                if (File.Exists(wshedFile)) {
                    (wshedLayer, _) = await Utils.getLayerByFilename(wshedFile, FileTypes._WATERSHED, this._gv, null, Utils._WATERSHED_GROUP_NAME);
                } else {
                    wshedLayer = null;
                }
                //if (wshedLayer is not null) {
                //    // style file like wshed.qml but does not check for subbasins upstream frm inlets
                //    wshedLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, "wshed2.qml"));
                //}
            }
            //}
            var streamLayer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._STREAMS));
            var streamFile = Path.Combine(this._gv.shapesDir, "riv1.shp");
            if (File.Exists(streamFile)) {
                (streamLayer, _) = await Utils.getLayerByFilename(streamFile, FileTypes._STREAMS,
                                                               this._gv, null, Utils._WATERSHED_GROUP_NAME);
            }
            Proj proj = this._gv.proj;
            this.title = this._gv.projName;
            string observedFile;
            bool found;
            (observedFile, found) = proj.readEntry(this.title, "observed/observedFile", "");
            if (found && File.Exists(observedFile)) {
                this.observedFileName = observedFile;
                this._dlg.PobservedFileEdit.Text = observedFileName;
            }
            var animationGroup = Utils.getGroupLayerByName(Utils._ANIMATION_GROUP_NAME);
            Debug.Assert(animationGroup is not null);
            animationGroup.PropertyChanged += new PropertyChangedEventHandler(changeAnimation);
            var resultsGroup = Utils.getGroupLayerByName(Utils._RESULTS_GROUP_NAME);
            Debug.Assert(resultsGroup is not null);
            // make sure results group is expanded and visible
            await QueuedTask.Run(() => {
                resultsGroup.SetExpanded(true);
                resultsGroup.SetVisibility(true);
            });
            resultsGroup.PropertyChanged += new PropertyChangedEventHandler(setResults);
            // in case restart with existing animation layers
            await this.setAnimateLayer();
            // in case restart with existing results layers
            await this.setResultsLayer();
        }

        // Do visualisation.
        public virtual async Task run() {
            Cursor.Current = Cursors.WaitCursor;
            await this.init();
            Cursor.Current = Cursors.Default;
            this._dlg.Show();
        }

        // Put scenarios in PscenariosCombo and months in start and finish month combos.
        public virtual void fillScenarios() {
            foreach (var direc in Directory.GetDirectories(this._gv.scenariosDir)) {
                var db = Utils.join(Utils.join(direc, Parameters._TABLESOUT), Parameters._OUTPUTDB);
                if (this._gv.forTNC) {
                    db = db.Replace(".mdb", ".sqlite");
                }
                if (File.Exists(db)) {
                    this._dlg.PscenariosCombo.Items.Add(Path.GetFileNameWithoutExtension(direc));
                }
            }
            if (this._dlg.PscenariosCombo.Items.Count > 1) {
                foreach (var i in Enumerable.Range(0, this._dlg.PscenariosCombo.Items.Count)) {
                    this._comparedlg.Pscenario1.Items.Add(this._dlg.PscenariosCombo.Items[i]);
                    this._comparedlg.Pscenario2.Items.Add(this._dlg.PscenariosCombo.Items[i]);
                }
                this._dlg.setCompare(true);
            } else {
                this._dlg.setCompare(false);
            }
            foreach (var month in Visualise._MONTHS) {
                var m = Utils.trans(month);
                this._dlg.PstartMonth.Items.Add(m);
                this._dlg.PfinishMonth.Items.Add(m);
            }
            foreach (var i in Enumerable.Range(0, 31)) {
                this._dlg.PstartDay.Items.Add((i + 1).ToString());
                this._dlg.PfinishDay.Items.Add((i + 1).ToString());
            }
        }

        // Reduce visible layers to channels, LSUs, HRUs, aquifers and subbasins by making all others not visible,
        //         loading LSUs, HRUs, aquifers if necessary.
        //         Leave Results group in case we already have some layers there.
        public virtual async Task setBackgroundLayers() {
            Func<string, bool> keepVisible;
            var slopeGroup = Utils.getGroupLayerByName(Utils._SLOPE_GROUP_NAME);
            if (slopeGroup is not null) {
                foreach (var layer in slopeGroup.GetLayersAsFlattenedList()) {
                    await QueuedTask.Run(() => {
                        layer.SetVisibility(false); 
                    });
                }
            }
            var soilGroup = Utils.getGroupLayerByName(Utils._SOIL_GROUP_NAME);
            if (soilGroup is not null) {
                foreach (var layer in soilGroup.GetLayersAsFlattenedList()) {
                    await QueuedTask.Run(() => {
                        layer.SetVisibility(false);
                    });
                }
            }
            var landuseGroup = Utils.getGroupLayerByName(Utils._LANDUSE_GROUP_NAME);
            if (landuseGroup is not null) {
                foreach (var layer in landuseGroup.GetLayersAsFlattenedList()) {
                    await QueuedTask.Run(() => {
                        layer.SetVisibility(false);
                    });
                }
            }
            // laod HRUS if necessary
            var hrusLayer = Utils.getLayerByLegend(Utils._FULLHRUSLEGEND);
            var hrusFile = Utils.join(this._gv.tablesOutDir, Parameters._HRUS + ".shp");
            var hasHRUs = File.Exists(hrusFile);
            if (hrusLayer is null && hasHRUs) {
                // set sublayer as hillshade or DEM
                var hillshadeLayer = Utils.getLayerByLegend(Utils._HILLSHADELEGEND);
                var demLayer = Utils.getLayerByLegend(Utils._DEMLEGEND);
                Layer subLayer = null;
                if (hillshadeLayer is not null) {
                    subLayer = hillshadeLayer;
                } else if (demLayer is not null) {
                    subLayer = demLayer;
                }
                if (hrusLayer is null && hasHRUs) {
                    (hrusLayer, _) = await Utils.getLayerByFilename(hrusFile, FileTypes._HRUS, this._gv, subLayer, Utils._WATERSHED_GROUP_NAME);
                }
            }
            var watershedLayers = Utils.getLayersInGroup(Utils._WATERSHED_GROUP_NAME);
            // make subbasins, channels, LSUs, HRUs and aquifers visible
            if (this._gv.useGridModel) {
                keepVisible = n => n.StartsWith(Utils._GRIDSTREAMSLEGEND) || n.StartsWith(Utils._DRAINSTREAMSLEGEND) || n.StartsWith(Utils._GRIDLEGEND);
            } else {
                keepVisible = n => n.StartsWith(Utils._WATERSHEDLEGEND) || n.StartsWith(Utils._REACHESLEGEND);
            }
            foreach (var layer in watershedLayers) {
                await QueuedTask.Run(() => {
                    layer.SetVisibility(keepVisible(layer.Name));
                });
            }
        }

        // Set current database and connection to it; put table names in PoutputCombo.
        public virtual void setupDb() {
            this.resultsFileUpToDate = false;
            var scen = this._dlg.PscenariosCombo.SelectedItem;
            if (scen is not null) {
                this.scenario = scen.ToString();
            } else { 
                this.scenario = this._dlg.PscenariosCombo.Text;
            }
            this.setConnection(this.scenario);
            var scenDir = Utils.join(this._gv.scenariosDir, this.scenario);
            var txtInOutDir = Utils.join(scenDir, Parameters._TXTINOUT);
            var cioFile = Utils.join(txtInOutDir, Parameters._CIO);
            if (!File.Exists(cioFile) && this._gv.forTNC) {
                // use a catchment cio file: since only dates being read any cio file will suffice
                var catchmentsDir = Utils.join(this._gv.projDir, "Catchments");
                var continentAbbrev = this._gv.projName.Substring(0, 2);
                var catchment = 0;
                while (catchment < 1000) {
                    // backstop against non termination
                    catchment += 1;
                    cioFile = Utils.join(catchmentsDir, Utils.join(continentAbbrev + catchment.ToString(), string.Format("Scenarios/Default/TxtInOut/{0}", Parameters._CIO)));
                    if (File.Exists(cioFile)) {
                        break;
                    }
                }
            }
            if (!File.Exists(cioFile)) {
                Utils.error(string.Format("Cannot find cio file {0}", cioFile), this._gv.isBatch);
                return;
            }
            if (this.conn is null) {
                return;
            }
            this.readCio(cioFile, this.scenario);
            this._dlg.PoutputCombo.Items.Clear();
            //this._dlg.PoutputCombo.Items.Add("");
            var tables = new List<string>();
            var schema = ((OleDbConnection)this.conn).GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            foreach (var row in schema.Rows.OfType<DataRow>()) {
                string tabl = row.ItemArray[2].ToString();
                tables.Add(tabl);
            }
            //if (this._gv.forTNC) {
            //    foreach (var row in this.conn.execute("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1")) {
            //        var name = row[0];
            //        if (name != "hru") {
            //            tables.Add(name);
            //        }
            //    }
            //} else {
            //    DbDataReader reader = DBUtils.getReader(this.conn, "");
            //    var tables = ((OleDbConnection)this.conn).tables
            //    foreach (var row in (OleDbConnection)this.conn.tables(tableType: "TABLE")) {
            //        tables.Add(row.table_name);
            //    }
            //}
            this.setHRUs(tables);
            foreach (var table in tables) {
                if (this.HRUsSetting > 0 && table == "hru" || table == "rch" || table == "sub" || table == "sed" || table == "wql") {
                    this._dlg.PoutputCombo.Items.Add(table);
                }
            }
            //this._dlg.PoutputCombo.SelectedIndex = 0;
            this.table = "";
            this.plotSetSub();
            this._dlg.PvariablePlot.Items.Clear();
            //this._dlg.PvariablePlot.Items.Add("");
            this.updateCurrentPlotRow(0);
        }

        // Set connection to scenario output database.
        public virtual void setConnection(string scenario) {
            var scenDir = Utils.join(this._gv.scenariosDir, scenario);
            var outDir = Utils.join(scenDir, Parameters._TABLESOUT);
            this.db = Utils.join(outDir, Parameters._OUTPUTDB);
            if (this._gv.forTNC) {
                //this.db = this.db.Replace(".mdb", ".sqlite");
                //this.conn = sqlite3.connect("file:{0}?mode=ro", this.db), uri: true);
            } else {
                this.conn = this._gv.db.connectDb(this.db);
            }
            this.conn.Open();
        }

        // Initialise the plot table.
        public virtual void setupPlot() {
            this.plotData = new DataTable();
            this.plotData.Columns.Add("Scenario", typeof(string));
            this.plotData.Columns.Add("Table", typeof(string));
            this.plotData.Columns.Add("Subbasin", typeof(string));
            this.plotData.Columns.Add("HRU", typeof(string));
            this.plotData.Columns.Add("Variable", typeof(string));
            this._dlg.PtableWidget.DataSource = this.plotData;
            this._dlg.PtableWidget.Columns[0].Width = 100;
            this._dlg.PtableWidget.Columns[1].Width = 45;
            this._dlg.PtableWidget.Columns[2].Width = 45;
            this._dlg.PtableWidget.Columns[3].Width = 45;
            this._dlg.PtableWidget.Columns[4].Width = 90;
            this._dlg.PplotType.Items.Clear();
            //this._dlg.PplotType.Items.Add(Visualise._CHOOSEPLOT);
            this._dlg.PplotType.Items.Add(Visualise._GRAPHORBAR);
            this._dlg.PplotType.Items.Add(Visualise._FLOWDURATION);
            this._dlg.PplotType.Items.Add(Visualise._SCATTER);
            this._dlg.PplotType.Items.Add(Visualise._BOX);
        }

        // Fill variables combos from selected table; set default results file name.
        public virtual void setVariables() {
            var table = this._dlg.PoutputCombo.SelectedItem.ToString();
            if (this.table == table) {
                // no change: do nothing
                return;
            }
            this.table = table;
            if (this.table == "") {
                return;
            }
            if (this.conn is null) {
                return;
            }
            var scenDir = Utils.join(this._gv.scenariosDir, this.scenario);
            var outDir = Utils.join(scenDir, Parameters._TABLESOUT);
            var outFile = Utils.join(outDir, this.table + "results.shp");
            this._dlg.PresultsFileEdit.Text = outFile;
            this.resultsFileUpToDate = false;
            this._dlg.PvariableCombo.Items.Clear();
            this._dlg.PanimationVariableCombo.Items.Clear();
            //this._dlg.PanimationVariableCombo.Items.Add("");
            this._dlg.PvariablePlot.Items.Clear();
            //this._dlg.PvariablePlot.Items.Add("");
            this._dlg.PvariableList.Items.Clear();
            if (this._gv.forTNC) {
                //foreach (var row in cursor.execute("SELECT name from pragma_table_info('{0}')", this.table))) {
                //    var = row[0];
                //    if (!this.ignoredVars.Contains(var)) {
                //        this._dlg.PvariableCombo.Items.Add(var);
                //        this._dlg.PanimationVariableCombo.Items.Add(var);
                //        this._dlg.PvariablePlot.Items.Add(var);
                //    }
                //}
            } else {
                var adp = new OleDbDataAdapter();
                var cmd = new OleDbCommand(string.Format("SELECT * FROM {0} WHERE 1=2;", table), this.conn as OleDbConnection);
                adp.SelectCommand = cmd;
                var dset = new DataSet();
                adp.Fill(dset, table);
                var cols = dset.Tables[0].Columns;
                for (int i = 0; i < cols.Count; i++) {
                    var name = cols[i].ColumnName;
                    if (!this.ignoredVars.Contains(name)) {
                        this._dlg.PvariableCombo.Items.Add(name);
                        this._dlg.PanimationVariableCombo.Items.Add(name);
                        this._dlg.PvariablePlot.Items.Add(name);
                    }
                }
            }
            this.updateCurrentPlotRow(1);
        }

        // Return true if plot tab open and plot table has a selected row.
        public virtual bool plotting() {
            if (this._dlg.PtabWidget.SelectedIndex != 2) {
                return false;
            }
            var row = this._dlg.PtableWidget.CurrentRow;
            return row is not null;
        }

        // Fill summary combo.
        public virtual void setSummary() {
            this._dlg.PsummaryCombo.Items.Clear();
            this._dlg.PsummaryCombo.Items.Add(Visualise._TOTALS);
            this._dlg.PsummaryCombo.Items.Add(Visualise._DAILYMEANS);
            this._dlg.PsummaryCombo.Items.Add(Visualise._MONTHLYMEANS);
            this._dlg.PsummaryCombo.Items.Add(Visualise._ANNUALMEANS);
            this._dlg.PsummaryCombo.Items.Add(Visualise._MAXIMA);
            this._dlg.PsummaryCombo.Items.Add(Visualise._MINIMA);
        }

        // Read cio file to get period of run and print frequency.
        public virtual void readCio(string cioFile, string scenario) {
            using (var cio = new StreamReader(cioFile)) {
                // skip 7 lines
                foreach (var _ in Enumerable.Range(0, 7)) {
                    cio.ReadLine();
                }
                var nbyrLine = cio.ReadLine();
                var cioNumYears = Convert.ToInt32(nbyrLine.Substring(0, 20));
                var iyrLine = cio.ReadLine();
                var cioStartYear = Convert.ToInt32(iyrLine.Substring(0, 20));
                var idafLine = cio.ReadLine();
                this.julianStartDay = Convert.ToInt32(idafLine.Substring(0, 20));
                var idalLine = cio.ReadLine();
                this.julianFinishDay = Convert.ToInt32(idalLine.Substring(0, 20));
                // skip 47 lines
                foreach (var _ in Enumerable.Range(0, 47)) {
                    cio.ReadLine();
                }
                var iprintLine = cio.ReadLine();
                var iprint = Convert.ToInt32(iprintLine.Substring(0, 20));
                this.isDaily = iprint == 1;
                this.isAnnual = iprint == 2;
                var nyskipLine = cio.ReadLine();
                var nyskip = Convert.ToInt32(nyskipLine.Substring(0, 20));
                this.startYear = cioStartYear + nyskip;
                this.numYears = cioNumYears - nyskip;
            }
            this.setDates(scenario);
        }

        // Set requested start and finish dates to smaller period of length of scenario and requested dates (if any).
        public virtual void setDates(string scenario) {
            var startDate = this.julianToDate(this.julianStartDay, this.startYear);
            var finishYear = this.startYear + this.numYears - 1;
            var finishDate = this.julianToDate(this.julianFinishDay, finishYear);
            var requestedStartDate = this.readStartDate();
            if (requestedStartDate is null) {
                this.setStartDate(startDate);
            } else {
                DateOnly rsd = (DateOnly)requestedStartDate;
                if (rsd < startDate) {
                    Utils.information(string.Format("Scenario {0} period starts later than current scenario {1} period: changing start", scenario, this.scenario), this._gv.isBatch);
                    this.setStartDate(startDate);
                }
            }
            var requestedFinishDate = this.readFinishDate();
            if (requestedFinishDate is null) {
                this.setFinishDate(finishDate);
            } else {
                DateOnly rfd = (DateOnly)requestedFinishDate;
                if (rfd > finishDate) {
                    Utils.information(string.Format("Scenario {0} period finishes earlier than current scenario {1} period: changing finish", scenario, this.scenario), this._gv.isBatch);
                    this.setFinishDate(finishDate);
                }
            }
        }

        // Define period of current scenario in days, months and years.  Return true if OK.
        public virtual bool setPeriods() {
            var requestedStartDate = this.readStartDate();
            var requestedFinishDate = this.readFinishDate();
            if (requestedStartDate is null || requestedFinishDate is null) {
                Utils.error("Cannot read chosen period", this._gv.isBatch);
                return false;
            }
            DateOnly rsd = (DateOnly)requestedStartDate;
            DateOnly rfd = (DateOnly)requestedFinishDate;
            if (rfd <= rsd) {
                Utils.error("Finish date must be later than start date", this._gv.isBatch);
                return false;
            }
            this.periodsUpToDate = this.startDay == rsd.Day && this.startMonth == rsd.Month && this.startYear == rsd.Year && this.finishDay == rfd.Day && this.finishMonth == rfd.Month && this.finishYear == rfd.Year;
            if (this.periodsUpToDate) {
                return true;
            }
            this.startDay = rsd.Day;
            this.startMonth = rsd.Month;
            this.startYear = rsd.Year;
            this.finishDay = rfd.Day;
            this.finishMonth = rfd.Month;
            this.finishYear = rfd.Year;
            this.julianStartDay = rsd.DayOfYear;
            this.julianFinishDay = rfd.DayOfYear;
            this.periodDays = 0;
            this.periodMonths = 0.0;
            this.periodYears = 0.0;
            foreach (var year in Enumerable.Range(this.startYear, this.finishYear + 1 - this.startYear)) {
                var leapAdjust = Visualise.isLeap(year) ? 1 : 0;
                var yearDays = 365 + leapAdjust;
                var start = year == this.startYear ? this.julianStartDay : 1;
                var finish = year == this.finishYear ? this.julianFinishDay : yearDays;
                var numDays = finish - start + 1;
                this.periodDays += numDays;
                var fracYear = (double)numDays / yearDays;
                this.periodYears += fracYear;
                this.periodMonths += fracYear * 12;
            }
            // Utils.loginfo('Period is {0} days, {1} months, {2} years', self.periodDays, self.periodMonths, self.periodYears))
            return true;
        }

        // Return date from start date from form.  None if any problems.
        public virtual DateOnly? readStartDate() {
            try {
                var day = Convert.ToInt32(this._dlg.PstartDay.SelectedItem);
                var month = this._dlg.PstartMonth.SelectedIndex + 1;
                var year = Convert.ToInt32(this._dlg.PstartYear.Text);
                return new DateOnly(year, month, day);
            } catch (Exception) {
                return null;
            }
        }

        // Return date from finish date from form.  None if any problems.
        public virtual DateOnly? readFinishDate() {
            try {
                var day = Convert.ToInt32(this._dlg.PfinishDay.SelectedItem);
                var month = this._dlg.PfinishMonth.SelectedIndex + 1;
                var year = Convert.ToInt32(this._dlg.PfinishYear.Text);
                return new DateOnly(year, month, day);
            } catch (Exception) {
                return null;
            }
        }

        // Set start date on form.
        public virtual void setStartDate(DateOnly date) {
            this._dlg.PstartDay.SelectedIndex = date.Day - 1;
            this._dlg.PstartYear.Text = date.Year.ToString();
            this._dlg.PstartMonth.SelectedIndex = date.Month - 1;
        }

        // Set finish date on form.
        public virtual void setFinishDate(DateOnly date) {
            this._dlg.PfinishDay.SelectedIndex = date.Day - 1;
            this._dlg.PfinishYear.Text = date.Year.ToString();
            this._dlg.PfinishMonth.SelectedIndex = date.Month - 1;
        }

        // Append item to variableList.
        public virtual void addClick() {
            this.resultsFileUpToDate = false;
            var @var = this._dlg.PvariableCombo.SelectedItem.ToString();
            var index = this._dlg.PvariableList.Items.IndexOf(@var);
            if (index < 0) {
                this._dlg.PvariableList.Items.Add(@var);
            }
        }

        // Clear variableList and insert all items from variableCombo.
        public virtual void allClick() {
            this.resultsFileUpToDate = false;
            this._dlg.PvariableList.Items.Clear();
            foreach (var i in Enumerable.Range(0, this._dlg.PvariableCombo.Items.Count)) {
                var item = this._dlg.PvariableCombo.Items[i].ToString();
                this._dlg.PvariableList.Items.Add(item);
            }
        }

        // Delete item from variableList.
        public virtual void delClick() {
            this.resultsFileUpToDate = false;
            var item = this._dlg.PvariableList.SelectedItem;
            if (item is not null) {
                var index = this._dlg.PvariableList.Items.IndexOf(item.ToString());
                if (index >= 0) {
                    this._dlg.PvariableList.Items.RemoveAt(index);
                }
            }
        }

        // Clear variableList.
        public virtual void clearClick() {
            this.resultsFileUpToDate = false;
            this._dlg.PvariableList.Items.Clear();
        }

        // Close the db connection, timer, clean up from animation, and close the form.
        public virtual async Task doClose() {
            this.animateTimer.Stop();
            // remove animation layers
            await Utils.clearAnimationGroup();
            // empty animation and png directories
            this.clearAnimationDir();
            this.clearPngDir();
            // only close connection after removing animation layers as the map title is affected and recalculation needs connection
            this.conn = null;
            this._dlg.Close();
        }

        // If the current table is 'hru', set the number of HRUs according to the subbasin.  Else set the PhruPlot to '-'.
        public virtual void plotSetSub() {
            this._dlg.PhruPlot.Items.Clear();
            if (this.table != "hru") {
                this._dlg.PhruPlot.Items.Add("-");
                this.updateCurrentPlotRow(2);
                return;
            }
            //this._dlg.PhruPlot.Items.Add("");
            this.updateCurrentPlotRow(2);
            if (this.conn is null) {
                return;
            }
            var substr = this._dlg.PsubPlot.SelectedItem.ToString();
            if (substr == "") {
                return;
            }
            var sub = Convert.ToInt32(substr);
            // find maximum hru number in hru table for this subbasin
            var maxHru = 0;
            // need to avoid WHERE x = n clause
            //sql = self._gv.db.sqlSelect('hru', QSWATTopology._HRUGIS, '', 'SUB=?')
            //for row in self.conn.cursor().execute(sql, (sub,)):
            //    hru = int(row.HRUGIS[6:])
            var varz = string.Format("[SUB], [{0}]", Topology._HRUGIS);
            var sql = DBUtils.sqlSelect("hru", varz, "", "");
            var reader = DBUtils.getReader(this.conn, sql);
            while (reader.Read()) {
                if (reader.GetInt32(0) != sub) {
                    continue;
                }
                var hru = Convert.ToInt32(reader.GetString(0).Substring(6));
                maxHru = Math.Max(maxHru, hru);
            }
            foreach (var i in Enumerable.Range(0, maxHru)) {
                this._dlg.PhruPlot.Items.Add((i + 1).ToString());
            }
        }

        // Update the HRU value in current plot row.
        public virtual void plotSetHRU() {
            this.updateCurrentPlotRow(3);
        }

        // Update the variable in the current plot row.
        public virtual void plotSetVar() {
            this.updateCurrentPlotRow(4);
        }

        // Write data for plot rows to csv file.
        public virtual void writePlotData() {
            object @ref;
            if (this.conn is null) {
                return;
            }
            if (!this.setPeriods()) {
                return;
            }
            if (this._dlg.PplotType.SelectedIndex < 0) {
                Utils.information("Please select a plot type", this._gv.isBatch);
                return;
            }
            var numRows = this.plotData.Rows.Count;
            if (numRows == 0) {
                Utils.information("There are no rows to plot", this._gv.isBatch);
                return;
            }
            if (this._dlg.PplotType.SelectedItem.ToString() == Visualise._SCATTER) {
                if (numRows == 1) {
                    Utils.information("You need two rows for a scatter plot", this._gv.isBatch);
                    return;
                }
                if (numRows > 2) {
                    Utils.information(string.Format("You need 2 rows for a scatter plot, and you have {0}.  Only the first two will be used.", numRows), this._gv.isBatch);
                }
            }
            var dataForPlot = new Dictionary<int, List<string>>();
            var labels = new Dictionary<int, string>();
            var dates = new List<string>();
            var datesDone = false;
            foreach (var i in Enumerable.Range(0, numRows)) {
                dataForPlot[i] = new List<string>();
                DataRow row = this.plotData.Rows[i];
                var scenario = row[0].ToString();
                var table = row[1].ToString();
                var sub = row[2].ToString();
                var hru = row[3].ToString();
                var variable = row[4].ToString();
                if (scenario == "" || table == "" || sub == "" || hru == "" || variable == "") {
                    Utils.information(string.Format("Row {0} is incomplete", i + 1), this._gv.isBatch);
                    return;
                }
                if (scenario == "observed" && table == "-") {
                    if (File.Exists(this.observedFileName)) {
                        labels[i] = "observed-" + variable;
                        dataForPlot[i] = this.readObservedFile(variable);
                    } else {
                        Utils.error(string.Format("Cannot find observed data file {0}", this.observedFileName), this._gv.isBatch);
                        return;
                    }
                } else {
                    string where;
                    int num, whereNum;
                    if (table == "hru") {
                        var hrufactor = this._gv.forTNC ? 100 : 10000;
                        var hrugis = Convert.ToInt32(sub) * hrufactor + Convert.ToInt32(hru);
                        // note that HRUGIS string as stored seems to have preceding space
                        where = string.Format("HRUGIS=' {0:D9}'", hrugis);
                        num = this.HRUsSetting == 2 ? hrugis : Convert.ToInt32(sub);
                        whereNum = 0;
                    } else if (table == "rch" || table == "sub") {
                        where = string.Format("SUB={0}", sub);
                        num = Convert.ToInt32(sub);
                        whereNum = num;
                    } else if (table == "sed" || table == "wql") {
                        where = string.Format("RCH={0}", sub);
                        num = Convert.ToInt32(sub);
                        whereNum = num;
                    } else {
                        Utils.error(string.Format("Unknown table {0} in row {1}", table, i + 1), this._gv.isBatch);
                        return;
                    }
                    labels[i] = string.Format("{0}-{1}-{2}-{3}", scenario, table, num, variable);
                    if (scenario != this.scenario) {
                        // need to change database
                        this.setConnection(scenario);
                        if (!this.readData("", false, table, variable, where, whereNum: whereNum)) {
                            return;
                        }
                        // restore database
                        this.setConnection(this.scenario);
                    } else if (!this.readData("", false, table, variable, where, whereNum: whereNum)) {
                        return;
                    }
                    int year, mon, finishYear, finishMon;
                    (year, mon) = this.startYearMon();
                    (finishYear, finishMon) = this.finishYearMon();
                    var layerData = this.staticData[""];
                    var finished = false;
                    while (!finished) {
                        if (!layerData.ContainsKey(num)) {
                            if (table == "hru") {
                                @ref = string.Format("HRU {0}", sub);
                            } else {
                                @ref = string.Format("subbasin {0}", sub);
                            }
                            Utils.error(string.Format("Insufficient data for {0} for plot {1}", @ref, i + 1), this._gv.isBatch);
                            break;
                        }
                        var subData = layerData[num];
                        if (!subData.ContainsKey(variable)) {
                            Utils.error(string.Format("Insufficient data for variable {0} for plot {1}", variable, i + 1), this._gv.isBatch);
                            break;
                        }
                        var varData = subData[variable];
                        if (!varData.ContainsKey(year)) {
                            Utils.error(string.Format("Insufficient data for year {0} for plot {1}", year, i + 1), this._gv.isBatch);
                            break;
                        }
                        var yearData = varData[year];
                        if (!yearData.ContainsKey(mon)) {
                            if (this.isDaily || this.table == "wql") {
                                @ref = string.Format("day {0}", mon);
                            } else if (this.isAnnual) {
                                @ref = string.Format("year {0}", mon);
                            } else {
                                @ref = string.Format("month {0}", mon);
                            }
                            Utils.error(string.Format("Insufficient data for {0} of year {1} for plot {2}", @ref, year, i + 1), this._gv.isBatch);
                            break;
                        }
                        var val = yearData[mon];
                        dataForPlot[i].Add(string.Format("{0:G3}", val));
                        if (!datesDone) {
                            if (this.isDaily || this.table == "wql") {
                                dates.Add((year * 1000 + mon).ToString());
                            } else if (this.isAnnual) {
                                dates.Add(year.ToString());
                            } else {
                                dates.Add(year.ToString() + "/" + mon.ToString());
                            }
                        }
                        finished = year == finishYear && mon == finishMon;
                        (year, mon) = this.nextDate(year, mon);
                    }
                    datesDone = true;
                }
            }
            if (!datesDone) {
                Utils.error("You must have at least one non-observed plot", this._gv.isBatch);
                return;
            }
            // data all collected: write csv file
            var dlg = new SaveFileDialog() {
                Title = "Choose a csv file",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                InitialDirectory = this._gv.scenariosDir
            };
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK) {
                return;
            }
            var csvFile = dlg.FileName;
            using (var fw = new StreamWriter(csvFile)) {
                fw.Write("Date,");
                foreach (var i in Enumerable.Range(0, numRows - 1)) {
                    fw.Write(labels[i]);
                    fw.Write(",");
                }
                fw.WriteLine(labels[numRows - 1]);
                foreach (var d in Enumerable.Range(0, dates.Count)) {
                    fw.Write(dates[d]);
                    fw.Write(",");
                    foreach (var i in Enumerable.Range(0, numRows)) {
                        if (!dataForPlot.ContainsKey(i)) {
                            Utils.error(string.Format("Missing data for plot {0}", i + 1), this._gv.isBatch);
                            fw.WriteLine("");
                            return;
                        }
                        if (!Enumerable.Range(0, dataForPlot[i].Count).Contains(d)) {
                            Utils.error(string.Format("Missing data for date {0} for plot {1}", dates[d], i + 1), this._gv.isBatch);
                            fw.WriteLine("");
                            return;
                        }
                        fw.Write(dataForPlot[i][d]);
                        if (i < numRows - 1) {
                            fw.Write(",");
                        } else {
                            fw.WriteLine("");
                            //         commands = []
                            //         settings = QSettings()
                            //         commands.Add(Utils.join(Utils.join(settings.value('/QSWAT/SWATEditorDir'), Parameters._SWATGRAPH), Parameters._SWATGRAPH))
                            //         commands.Add(csvFile)
                            //         subprocess.Popen(commands)
                            // above replaced with swatGraph form
                        }
                    }
                }
            }
            Process graph = new Process {
                StartInfo = new ProcessStartInfo() {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    //FileName = "C:\\Users\\Chris\\source\\repos\\ArcSWAT3\\runSWATGraph.bat",
                    FileName = Path.Combine(this._gv.addinPath, "runSWATGraph.bat"),
                    Arguments = string.Format("{0} {1}", csvFile, this._dlg.PplotType.SelectedIndex + 1)
                }
            };
            //var graph = SWATGraph(csvFile, this._dlg.PplotType.SelectedItem.ToString());
            graph.Start();
        }

        // Read data from database table into staticData.  Return True if no error detected.
        //         
        //         whereNum if positive causes the where clause to be replaced by the empty string and a filter index = whereNum used on the 
        //         SELECT results, to avoid the bug that WHERE x = n with n a number causes an Internal OLE Automation error
        public virtual bool readData(
            string layerId,
            bool isStatic,
            string table,
            string var,
            string where,
            int whereNum = 0) {
            int preLen;
            string preString;
            List<string> varz;
            if (this.conn is null) {
                return false;
            }
            // clear existing data for layerId
            this.staticData[layerId] = new Dictionary<int, Dictionary<string, Dictionary<int, Dictionary<int, double>>>>();
            var layerData = this.staticData[layerId];
            this.areas = new Dictionary<int, double>();
            this.hasAreas = false;
            this.animateResultsData[layerId] = new Dictionary<int, Dictionary<int, double>>();
            if (isStatic) {
                varz = this.varList(true);
            } else {
                varz = new List<string> {
                    "[" + var + "]"
                };
            }
            var numVars = varz.Count;
            if (table == "sub" || table == "rch") {
                if (isStatic) {
                    preString = "SUB, YEAR, MON, AREAkm2, ";
                    preLen = 4;
                    this.hasAreas = true;
                } else {
                    preString = "SUB, YEAR, MON, ";
                    preLen = 3;
                }
            } else if (table == "hru") {
                if (isStatic) {
                    if (this.HRUsSetting == 2) {
                        preString = "HRUGIS, YEAR, MON, AREAkm2, ";
                    } else {
                        preString = "SUB, YEAR, MON, AREAkm2, ";
                    }
                    preLen = 4;
                    this.hasAreas = true;
                } else {
                    if (this.HRUsSetting == 2) {
                        preString = "HRUGIS, YEAR, MON, ";
                    } else {
                        preString = "SUB, YEAR, MON, ";
                    }
                    preLen = 3;
                }
            } else if (table == "sed") {
                if (isStatic) {
                    preString = "RCH, YEAR, MON, AREAkm2, ";
                    preLen = 4;
                    this.hasAreas = true;
                } else {
                    preString = "RCH, YEAR, MON, ";
                    preLen = 3;
                }
            } else if (table == "wql") {
                preString = "RCH, YEAR, DAY, ";
                preLen = 3;
                this.hasAreas = false;
            } else {
                // TODO: not yet supported
                return false;
            }
            var selectString = preString + String.Join(',', varz);
            // fudge to deal with WHERE x = n problem 
            if (whereNum > 0) {
                where = "";
            }
            var sql = DBUtils.sqlSelect(table, selectString, "", where);
            //Utils.information('SQL: {0}', sql), self._gv.isBatch)
            // guard against no table or no data
            try {
                using (var reader = DBUtils.getReader(this.conn, sql)) {
                    while (reader.Read()) {
                        // index is subbasin number unless multiple hrus, when it is the integer parsing of HRUGIS
                        int index = 0;
                        if (table == "hru") {
                            index = Convert.ToInt32(reader.GetString(0));
                        } else {
                            index = reader.GetInt32(0);
                        }
                        // fudge to deal with WHERE x = n problem 
                        if (whereNum > 0) {
                            if (index != whereNum) {
                                continue;
                            }
                        }
                        var year = reader.GetInt32(1);
                        var mon = reader.GetInt32(2);
                        if (!this.inPeriod(year, mon)) {
                            continue;
                        }
                        double area = 0;
                        if (this.hasAreas) {
                            area = reader.GetDouble(3);
                        }
                        if (isStatic && this.hasAreas && !this.areas.ContainsKey(index)) {
                            this.areas[index] = area;
                        }
                        if (!layerData.ContainsKey(index)) {
                            layerData[index] = new Dictionary<string, Dictionary<int, Dictionary<int, double>>>();
                        }
                        foreach (var i in Enumerable.Range(0, numVars)) {
                            // remove square brackets from each var
                            var vari = varz[i];
                            vari = vari.Substring(1, vari.Length - 2);
                            var val = reader.GetDouble(i + preLen);
                            if (!layerData[index].ContainsKey(vari)) {
                                layerData[index][vari] = new Dictionary<int, Dictionary<int, double>>();
                            }
                            if (!layerData[index][vari].ContainsKey(year)) {
                                layerData[index][vari][year] = new Dictionary<int, double>();
                            }
                            layerData[index][vari][year][mon] = val;
                        }
                    }
                }
            } catch {
                // get scenario name
                var scenario = this.scenarioFromDb();
                Utils.error(string.Format("Cannot find {0} data for scenario {1}", table, scenario), this._gv.isBatch);
                return false;
            }
            if (layerData.Count == 0) {
                Utils.error("No data has nbeen read.  Perhaps your dates are outside the dates of the table", this._gv.isBatch);
                return false;
            }
            this.summaryChanged = true;
            return true;
        }

        // Return scenario name from current setting of this.db.
        public virtual string scenarioFromDb() {
            var dbDir = Path.GetDirectoryName(this.db);
            var scenDir = Path.GetDirectoryName(dbDir);
            return Path.GetFileName(scenDir);
        }

        // 
        //         Return true if year and mon are within requested period.
        //         
        //         Assumes self.[julian]startYear/Month/Day and self.[julian]finishYear/Month/Day already set.
        //         Assumes mon is within 1..365/6 when daily, and within 1..12 when monthly.
        //         
        public virtual bool inPeriod(int year, int mon) {
            if (year < this.startYear || year > this.finishYear) {
                return false;
            }
            if (this.isAnnual) {
                return true;
            }
            if (this.isDaily || this.table == "wql") {
                if (year == this.startYear) {
                    return mon >= this.julianStartDay;
                }
                if (year == this.finishYear) {
                    return mon <= this.julianFinishDay;
                }
                return true;
            }
            // monthly
            if (year == this.startYear) {
                return mon >= this.startMonth;
            }
            if (year == this.finishYear) {
                return mon <= this.finishMonth;
            }
            return true;
        }

        // if isStatic, summarise data in staticData, else store all data for animate variable, saving in resultsData.
        public virtual void summariseData(string layerId, bool isStatic) {
            var layerData = this.staticData[layerId];
            if (isStatic) {
                foreach (var (index, vym) in layerData) {
                    foreach (var (var, ym) in vym) {
                        var val = this.summarise(ym);
                        if (!this.staticResultsData.ContainsKey(index)) {
                            this.staticResultsData[index] = new Dictionary<string, double>();
                        }
                        this.staticResultsData[index][var] = val;
                    }
                }
            } else {
                this.allAnimateVals = new List<double>();
                if (!this.animateResultsData.ContainsKey(layerId)) {
                    this.animateResultsData[layerId] = new Dictionary<int, Dictionary<int, double>>();
                }
                var results = this.animateResultsData[layerId];
                foreach (var (index, vym) in layerData) {
                    foreach (var ym in vym.Values) {
                        foreach (var (y, mval) in ym) {
                            foreach (var (m, val) in mval) {
                                var dat = this.makeDate(y, m);
                                if (!results.ContainsKey(dat)) {
                                    results[dat] = new Dictionary<int, double>();
                                }
                                results[dat][index] = val;
                                this.allAnimateVals.Add(val);
                            }
                        }
                    }
                }
            }
        }

        // 
        //         Make date number from year and mon according to period.
        //         
        //         mon is MON field, which may be year, month or day according to period.
        //         
        public virtual int makeDate(int year, int mon) {
            if (this.isDaily || this.table == "wql") {
                return year * 1000 + mon;
            } else if (this.isAnnual) {
                return year;
            } else {
                return year * 100 + mon;
            }
        }

        // Return (year, mon) pair for start date according to period.
        public virtual (int, int)  startYearMon() {
            if (this.isDaily || this.table == "wql") {
                return (this.startYear, this.julianStartDay);
            } else if (this.isAnnual) {
                return (this.startYear, this.startYear);
            } else {
                return (this.startYear, this.startMonth);
            }
        }

        // Return (year, mon) pair for finish date according to period.
        public virtual (int, int) finishYearMon() {
            if (this.isDaily || this.table == "wql") {
                return (this.finishYear, this.julianFinishDay);
            } else if (this.isAnnual) {
                return (this.finishYear, this.finishYear);
            } else {
                return (this.finishYear, this.finishMonth);
            }
        }

        // Get next (year, mon) pair according to period.
        public virtual (int, int) nextDate(int year, int mon) {
            if (this.isDaily || this.table == "wql") {
                var leapAdjust = isLeap(year) ? 1 : 0;
                var maxDays = 365 + leapAdjust;
                if (mon < maxDays) {
                    return (year, mon + 1);
                } else {
                    return (year + 1, 1);
                }
            } else if (this.isAnnual) {
                return (year + 1, year + 1);
            } else if (mon < 12) {
                return (year, mon + 1);
            } else {
                return (year + 1, 1);
            }
        }

        // Summarise values according to summary method.
        public virtual double summarise(Dictionary<int, Dictionary<int, double>> data) {
            if (this._dlg.PsummaryCombo.SelectedItem.ToString() == Visualise._TOTALS) {
                return this.summariseTotal(data);
            } else if (this._dlg.PsummaryCombo.SelectedItem.ToString() == Visualise._ANNUALMEANS) {
                return this.summariseAnnual(data);
            } else if (this._dlg.PsummaryCombo.SelectedItem.ToString() == Visualise._MONTHLYMEANS) {
                return this.summariseMonthly(data);
            } else if (this._dlg.PsummaryCombo.SelectedItem.ToString() == Visualise._DAILYMEANS) {
                return this.summariseDaily(data);
            } else if (this._dlg.PsummaryCombo.SelectedItem.ToString() == Visualise._MAXIMA) {
                return this.summariseMaxima(data);
            } else if (this._dlg.PsummaryCombo.SelectedItem.ToString() == Visualise._MINIMA) {
                return this.summariseMinima(data);
            } else {
                Utils.error("Internal error: unknown summary method: please report", this._gv.isBatch);
            }
            return 0;
        }

        // Sum values and return.
        public virtual double summariseTotal(Dictionary<int, Dictionary<int, double>> data) {
            var total = 0.0;
            foreach (var mv in data.Values) {
                foreach (var v in mv.Values) {
                    total += v;
                }
            }
            return total;
        }

        // Return total divided by period in years.
        public virtual double summariseAnnual(Dictionary<int, Dictionary<int, double>> data) {
            return (double)this.summariseTotal(data) / this.periodYears;
        }

        // Return total divided by period in months.
        public virtual double summariseMonthly(Dictionary<int, Dictionary<int, double>> data) {
            return (double)this.summariseTotal(data) / this.periodMonths;
        }

        // Return total divided by period in days.
        public virtual double summariseDaily(Dictionary<int, Dictionary<int, double>> data) {
            return (double)this.summariseTotal(data) / this.periodDays;
        }

        // Return maximum of values.
        public virtual double summariseMaxima(Dictionary<int, Dictionary<int, double>> data) {
            var maxv = 0.0;
            foreach (var mv in data.Values) {
                foreach (var v in mv.Values) {
                    maxv = Math.Max(maxv, v);
                }
            }
            return maxv;
        }

        // Return minimum of values.
        public virtual double summariseMinima(Dictionary<int, Dictionary<int, double>> data) {
            var minv = double.PositiveInfinity;
            foreach (var mv in data.Values) {
                foreach (var v in mv.Values) {
                    minv = Math.Min(minv, v);
                }
            }
            return minv;
        }

        // Return true if year is a leap year.
        public static bool isLeap(int year) {
            if (year % 4 == 0) {
                if (year % 100 == 0) {
                    return year % 400 == 0;
                } else {
                    return true;
                }
            } else {
                return false;
            }
        }

        // Set self.numSubbasins from one of tables.
        public virtual void setNumSubbasins(List<string> tables) {
            string subCol;
            string table;
            if (this.conn is null) {
                return;
            }
            this.numSubbasins = 0;
            if (tables.Contains("sub")) {
                table = "sub";
                subCol = "SUB";
            } else if (tables.Contains("rch")) {
                table = "rch";
                subCol = "SUB";
            } else if (tables.Contains("sed")) {
                table = "sed";
                subCol = "RCH";
            } else if (tables.Contains("wql")) {
                table = "wql";
                subCol = "RCH";
            } else {
                Utils.error("Seem to be no complete tables in this scenario", this._gv.isBatch);
                return;
            }
            var sql = DBUtils.sqlSelect(table, subCol, "", "");
            using (var reader = DBUtils.getReader(this.conn, sql)) { 
                while (reader.Read()) {
                    this.numSubbasins = Math.Max(this.numSubbasins, reader.GetInt32(0));
                }
            }
        }

        // Add subbasin numbers to PsubPlot combo.
        public virtual void setSubPlot() {
            this._dlg.PsubPlot.Items.Clear();
            //this._dlg.PsubPlot.Items.Add("");
            foreach (var i in Enumerable.Range(0, this.numSubbasins)) {
                this._dlg.PsubPlot.Items.Add((i + 1).ToString());
            }
        }

        // 
        //         Set self.hruNums if hru table, plus HRUsSetting.  Also self.numSubbasins, and populate PsubPlot combo.
        //         
        //         HRUsSetting is 1 if only 1 HRU in each subbasin, else 0 if limited HRU output or no hru template shapefile, else 2 (meaning multiple HRUs).
        //         
        public virtual void setHRUs(List<string> tables) {
            if (this.conn is null) {
                return;
            }
            if (!tables.Contains("hru")) {
                this.setNumSubbasins(tables);
                this.setSubPlot();
                return;
            }
            var tablesOutDir = Path.GetDirectoryName(this.db);
            var HRUsFile = Utils.join(tablesOutDir, Parameters._HRUS) + ".shp";
            // find maximum hru number in hru table for each subbasin
            this.hruNums = new Dictionary<int, int>();
            this.numSubbasins = 0;
            var maxHRU = 0;
            var maxSub = 0;
            var sql = DBUtils.sqlSelect("hru", Topology._HRUGIS, "", "");
            using (var reader = DBUtils.getReader(this.conn, sql)) {
                while (reader.Read()) {
                    var hrugis = Int32.Parse(reader.GetString(0));
                    // TNC projects use 7+2 filenames, instead of 5+4
                    var hrufactor = this._gv.forTNC ? 100 : 10000;
                    var hru = hrugis % hrufactor;
                    var sub = hrugis / hrufactor;
                    maxHRU = Math.Max(maxHRU, hru);
                    maxSub = Math.Max(maxSub, sub);
                    int hruNum = 0;
                    this.hruNums[sub] = Math.Max(hru, this.hruNums.TryGetValue(sub, out hruNum) ? hruNum : 0);
                }
            }
            if (maxSub > 1 && maxHRU == 1) {
                this.HRUsSetting = 1;
                this.numSubbasins = maxSub;
            } else if (maxSub == 1 || !File.Exists(HRUsFile)) {
                this.HRUsSetting = 0;
                this.setNumSubbasins(tables);
            } else {
                this.HRUsSetting = 2;
                this.numSubbasins = maxSub;
            }
            this.setSubPlot();
        }

        // Return variables in PvariableList as a list of strings, with square brackets if bracket is true.
        public virtual List<string> varList(bool bracket) {
            var result = new List<string>();
            var numRows = this._dlg.PvariableList.Items.Count;
            foreach (var row in Enumerable.Range(0, numRows)) {
                var vari = this._dlg.PvariableList.Items[row].ToString();
                // bracket variables when using in sql, to avoid reserved words and '/'
                if (bracket) {
                    vari = "[" + vari + "]";
                }
                result.Add(vari);
            }
            return result;
        }

        // Set results file by asking user.
        public virtual void setResultsFile() {
            string direcUpUp;
            string path;
            try {
                path = Path.GetDirectoryName(this._dlg.PresultsFileEdit.Text);
            } catch (Exception) {
                path = "";
            }
            var subsOrRiv = this.useSubs() ? "subs" : this.useHRUs() ? "hrus" : "riv";
            var dlg = new OpenFileDialog() {
                Title = subsOrRiv + "results",
                Filter = "Shapefiles (*.shp)|*.shp|All files (*.*)|*.*",
                InitialDirectory = path
            };
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK) {
                return;
            }
            var resultsFileName = dlg.FileName;
            var direc = Path.GetDirectoryName(resultsFileName);
            var name = Path.GetFileName(resultsFileName);
            var direcUp = Path.GetDirectoryName(direc);
            var direcName = Path.GetFileName(direc);
            if (direcName == Parameters._TABLESOUT) {
                //# check we are not overwriting a template
                direcUpUp = Path.GetDirectoryName(direcUp);
                if (Utils.samePath(direcUpUp, this._gv.scenariosDir)) {
                    var @base = Path.GetFileNameWithoutExtension(name);
                    if (@base == Parameters._SUBS || @base == Parameters._RIVS || @base == Parameters._HRUS) {
                        Utils.information(string.Format("The file {0} should not be overwritten: please choose another file name.", Path.ChangeExtension(resultsFileName, null) + ".shp"), this._gv.isBatch);
                        return;
                    }
                }
            } else if (direcName == Parameters._ANIMATION) {
                //# check we are not using the Animation directory
                direcUpUp = Path.GetDirectoryName(direcUp);
                if (Utils.samePath(direcUpUp, this._gv.tablesOutDir)) {
                    Utils.information(string.Format("Please do not use {0} for results as it can be overwritten by animation.", Path.ChangeExtension(resultsFileName, null) + ".shp"), this._gv.isBatch);
                    return;
                }
            }
            this._dlg.PresultsFileEdit.Text = resultsFileName;
            this.resultsFileUpToDate = false;
        }

        // Get an observed data file from the user.
        public virtual void setObservedFile(string filename) {
            string path;
            try {
                path = Path.GetDirectoryName(filename);
            } catch (Exception) {
                path = "";
            }
            var dlg = new OpenFileDialog() {
                Title = "Choose observed data file",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                InitialDirectory = path
            };
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK) {
                return;
            }
            this.observedFileName = dlg.FileName;
            this._dlg.PobservedFileEdit.Text = observedFileName;
            Proj proj = this._gv.proj;
            this.title = this._gv.projName;
            proj.writeEntry(this.title, "observed/observedFile", this.observedFileName);
        }

        // Return true if should use subbasins map for results (else use streams or HRUs).
        public virtual bool useSubs() {
            return this.table == "sub" || this.table == "hru" && this.HRUsSetting == 1;
        }

        // Return true if should use HRUs map.
        public virtual bool useHRUs() {
            return this.table == "hru" && this.HRUsSetting == 2;
        }

        // 
        //         Create results shapefile.
        //         
        //         Assumes:
        //         - PresultsFileEdit contains suitable text for results file name
        //         - one or more variables is selected in variableList (and uses the first one)
        //         - resultsData is suitably populated
        //         
        public virtual async Task<bool> createResultsFile() {
            var nextResultsFile = this._dlg.PresultsFileEdit.Text;
            if (File.Exists(nextResultsFile)) {
                var reply = Utils.question(string.Format("Results file {0} already exists.  Do you wish to overwrite it?", nextResultsFile), this._gv.isBatch, true);
                if (reply != System.Windows.MessageBoxResult.Yes) {
                    return false;
                }
                if (nextResultsFile == this.resultsFile) {
                    await Utils.removeLayerAndFiles(this.resultsFile);
                } else {
                    // layer removed as a precaution since removing files;  Could be present, eg after a restart.
                    await Utils.removeLayerAndFiles(nextResultsFile);
                    this.resultsFile = nextResultsFile;
                }
            } else {
                this.resultsFile = nextResultsFile;
            }
            var tablesOutDir = Path.GetDirectoryName(this.db);
            var baseName = this.useSubs() ? Parameters._SUBS : this.useHRUs() ? Parameters._HRUS : Parameters._RIVS;
            var resultsBase = Utils.join(tablesOutDir, baseName) + ".shp";
            var outdir = Path.GetDirectoryName(this.resultsFile);
            var outfile = Path.GetFileName(this.resultsFile);
            var outbase = Path.ChangeExtension(outfile, null);
            Utils.copyShapefile(resultsBase, outbase, outdir);
            var selectVar = this._dlg.PvariableList.SelectedItems[0].ToString();
            var legend = string.Format("{0} {1} {2}", this.scenario, selectVar, this._dlg.PsummaryCombo.SelectedItem.ToString());
            if (this.useSubs()) {
                //this.subResultsLayer = QgsVectorLayer(this.resultsFile, legend, "ogr");
                //this.subResultsLayer.RendererChanged.connect(this.changeSubRenderer);
                this.internalChangeToSubRenderer = true;
                this.keepSubColours = false;
                this.currentResultsLayer = this.subResultsLayer;
            } else if (this.useHRUs()) {
                //this.hruResultsLayer = QgsVectorLayer(this.resultsFile, legend, "ogr");
                //this.hruResultsLayer.rendererChanged.connect(this.changeHRURenderer);
                this.internalChangeToHRURenderer = true;
                this.keepHRUColours = false;
                this.currentResultsLayer = this.hruResultsLayer;
            } else {
                //this.rivResultsLayer = QgsVectorLayer(this.resultsFile, legend, "ogr");
                //this.rivResultsLayer.rendererChanged.connect(this.changeRivRenderer);
                this.internalChangeToRivRenderer = true;
                this.keepRivColours = false;
                this.currentResultsLayer = this.rivResultsLayer;
            }
            using (var resultsDs = Ogr.Open(this.resultsFile, 1)) { 
                var resultslayer = resultsDs.GetLayerByIndex(0);
                var resultsDef = resultslayer.GetLayerDefn();
                if (this.hasAreas) {
                    var field = new OSGeo.OGR.FieldDefn(Visualise._AREA, FieldType.OFTReal);
                    resultslayer.CreateField(field, 1);
                }
                var varz = this.varList(false);
                foreach (var vari in varz) {
                    var field = new OSGeo.OGR.FieldDefn(vari, FieldType.OFTReal);
                    resultslayer.CreateField(field, 1);
                }
            }
            this.currentResultsLayer = await QueuedTask.Run(() => 
                LayerFactory.Instance.CreateLayer(new Uri(this.resultsFile),
                        Utils.getGroupLayerByName(Utils._RESULTS_GROUP_NAME), 0, legend) as FeatureLayer);
            if (this.useSubs()) {
                this.subResultsLayer = currentResultsLayer;
            } else if (this.useHRUs()) {
                this.hruResultsLayer = currentResultsLayer;
            } else {
                this.rivResultsLayer = currentResultsLayer;
            }
            await this.updateResultsFile();
            //cast(QgsVectorLayer, QgsProject.instance().addMapLayer(this.currentResultsLayer, false));
            //var resultsGroup = root.findGroup(Utils._RESULTS_GROUP_NAME);
            //Debug.Assert(resultsGroup is not null);
            //resultsGroup.insertLayer(0, this.currentResultsLayer);
            //this._gv.iface.setActiveLayer(this.currentResultsLayer);
            //if (this.useSubs()) {
            //    // add labels
            //    Debug.Assert(this.subResultsLayer is not null);
            //    if (!this._gv.useGridModel) {
            //        this.subResultsLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, "subresults.qml"));
            //    }
            //    this.internalChangeToSubRenderer = false;
            //    var baseMapTip = FileTypes.mapTip(FileTypes._SUBBASINS);
            //} else if (this.useHRUs()) {
            //    this.internalChangeToHRURenderer = false;
            //    baseMapTip = FileTypes.mapTip(FileTypes._HRUS);
            //} else {
            //    this.internalChangeToRivRenderer = false;
            //    baseMapTip = FileTypes.mapTip(FileTypes._REACHES);
            //}
            //this.currentResultsLayer.setMapTipTemplate(baseMapTip + "<br/><b>{0}:</b> [% \"{0}\" %]", selectVar));
            //this.currentResultsLayer.updatedFields.connect(this.addResultsVars);
            return true;
        }

        // Write resultsData to resultsFile.
        public virtual async Task updateResultsFile() {
            string @ref;
            int sub;
            FeatureLayer layer = this.useSubs() ? this.subResultsLayer : this.useHRUs() ? this.hruResultsLayer : this.rivResultsLayer;
            var varz = this.varList(false);
            await QueuedTask.Run(async () => {
                using (var rc = layer.Search()) {
                    while (rc.MoveNext()) {
                        using (var f = rc.Current as ArcGIS.Core.Data.Feature) {
                            bool result = false;
                            var op = new ArcGIS.Desktop.Editing.EditOperation();
                            if (this.useHRUs()) {
                                // May be split HRUs; just use first
                                // This is inadequate for some variables, but no way to know if correct val is sum of vals, mean, etc.
                                sub = Convert.ToInt32(f[Topology._HRUGIS].ToString().Split(',')[0]);
                            } else {
                                sub = Convert.ToInt32(f[Topology._SUBBASIN]);
                            }
                            if (this.hasAreas) {
                                double area = 0;
                                if (!this.areas.TryGetValue(sub, out area)) {
                                    if (this.useHRUs()) {
                                        @ref = string.Format("HRU {0}", sub);
                                    } else {
                                        @ref = string.Format("subbasin {0}", sub);
                                    }
                                    Utils.error(string.Format("Cannot get area for {0}: have you run SWAT and saved data since running QSWAT", @ref), this._gv.isBatch);
                                    return;
                                }
                                op.Modify(f, Visualise._AREA, area);
                            }
                            foreach (var vari in varz) {
                                Dictionary<string, double> subData;
                                double data = 0;
                                bool ok = false;
                                if (this.staticResultsData.TryGetValue(sub, out subData)) {
                                    if (subData.TryGetValue(vari, out data)) {
                                        ok = true;
                                    }
                                }
                                if (!ok) {
                                    if (this.useHRUs()) {
                                        @ref = string.Format("HRU {0}", sub);
                                    } else {
                                        @ref = string.Format("subbasin {0}", sub);
                                    }
                                    Utils.error(string.Format("Cannot get data for variable {0} in {1}: have you run SWAT and saved data since running QSWAT", vari, @ref), this._gv.isBatch);
                                    return;
                                }
                                var variShort = vari.Substring(0, Math.Min(10, vari.Length));
                                op.Modify(f, variShort, data);
                            }
                            if (!op.IsEmpty) {
                                result = await op.ExecuteAsync();
                            }
                            if (!result) {
                                Utils.error(string.Format("Could not set attributes in results file {0}", this.resultsFile), this._gv.isBatch);
                                return;
                            }
                        }
                    }
                }
            });
            this.summaryChanged = false;
            if (Project.Current.HasEdits) {
                await Project.Current.SaveEditsAsync();
            }
        }

        //// Write resultsData to resultsFile.
        //public virtual async Task updateResultsFile() {
        //    string @ref;
        //    int sub;
        //    FeatureLayer layer = this.useSubs() ? this.subResultsLayer : this.useHRUs() ? this.hruResultsLayer : this.rivResultsLayer;
        //    //var resultsFile = await Utils.layerFilename(layer);
        //    //Utils.removeLayer(resultsFile);
        //    var varz = this.varList(false);
        //    using (var resultsDs = Ogr.Open(this.resultsFile, 1)) {
        //        var resultsLayer = resultsDs.GetLayerByIndex(0);
        //        do {
        //            var f = resultsLayer.GetNextFeature();
        //            if (f is null) { break; }
        //            if (this.useHRUs()) {
        //                // May be split HRUs; just use first
        //                // This is inadequate for some variables, but no way to know if correct val is sum of vals, mean, etc.
        //                sub = Convert.ToInt32(f.GetFieldAsString(Topology._HRUGIS).Split(',')[0]);
        //            } else {
        //                sub = Convert.ToInt32(f.GetFieldAsString(Topology._SUBBASIN));
        //            }
        //            if (this.hasAreas) {
        //                double area = 0;
        //                if (!this.areas.TryGetValue(sub, out area)) {
        //                    if (this.useHRUs()) {
        //                        @ref = string.Format("HRU {0}", sub);
        //                    } else {
        //                        @ref = string.Format("subbasin {0}", sub);
        //                    }
        //                    Utils.error(string.Format("Cannot get area for {0}: have you run SWAT and saved data since running QSWAT", @ref), this._gv.isBatch);
        //                    return;
        //                }
        //                f.SetField(Visualise._AREA, area);
        //            }
        //            foreach (var vari in varz) {
        //                Dictionary<string, double> subData;
        //                double data = 0;
        //                bool ok = false;
        //                if (this.staticResultsData.TryGetValue(sub, out subData)) {
        //                    if (subData.TryGetValue(vari, out data)) {
        //                        ok = true;
        //                    }
        //                }
        //                if (!ok) {
        //                    if (this.useHRUs()) {
        //                        @ref = string.Format("HRU {0}", sub);
        //                    } else {
        //                        @ref = string.Format("subbasin {0}", sub);
        //                    }
        //                    Utils.error(string.Format("Cannot get data for variable {0} in {1}: have you run SWAT and saved data since running QSWAT", vari, @ref), this._gv.isBatch);
        //                    return;
        //                }
        //                var ind1 = f.GetFieldIndex(vari);
        //                var shortVari = vari.Substring(0, Math.Min(vari.Length, 10));
        //                var ind2 = f.GetFieldIndex(shortVari);
        //                if (vari.Length > 10) {
        //                    ;
        //                }
        //                f.SetField(shortVari, data);
        //            }
        //            var res = resultsLayer.SetFeature(f);
        //            if (res != Ogr.OGRERR_NONE) {
        //                Utils.error(string.Format("Could not set attributes in results file {0}", this.resultsFile), this._gv.isBatch);
        //                return;
        //            }
        //        } while (true);
        //    }
        //    this.summaryChanged = false;
        //}

        // Make renderer with Jenks algorithm using vals of var, setting colour rampe to ramp, inverted if invert
        public virtual async Task<CIMClassBreaksRenderer> makeJenksRenderer(List<double> vals, CIMColorRamp ramp, string vari, bool useLine) {
            //List<double> val5 = new List<double>() { 1, 2, 4, 5, 6 };
            //var cbs = JenksFisher.CreateJenksFisherBreaksArray(val5, 2);
            var upperBound = vals.Max();
            var count = 5;
            // algoritm provides lower bounds for breaks
            var cbreaks = JenksFisher.CreateJenksFisherBreaksArray(vals, count);
            Utils.loginfo(string.Format("Breaks: {0}", cbreaks.ToString()));
            var colours = await QueuedTask.Run(() => ColorFactory.Instance.GenerateColorsFromColorRamp(ramp, count));
            var classBreaks = new List<CIMClassBreak>();
            foreach (var i in Enumerable.Range(0, count)) {
                // adjust min and max by 1% to avoid rounding errors causing values to be outside the range
                var minVal = i == 0 ? cbreaks[0] * 0.99 : cbreaks[i];
                var maxVal = i == count - 1 ? upperBound * 1.01 : cbreaks[i + 1];
                var colour = colours[i];
                classBreaks.Add(this.makeSymbologyForRange(minVal, maxVal, colour, 3, useLine));
            }
            var renderer = new CIMClassBreaksRenderer() { 
                Breaks = classBreaks.ToArray(),
                ColorRamp = ramp,
                Field = vari.Substring(0, Math.Min(10, vari.Length))
            };
            return renderer;
        }

        // 
        //         Colour results layer according to current results variable and update legend.
        //         
        //         If keepColours is true the existing colour ramp and number of classes can be reused
        //         When layer is supplied (for scenario comparisons) its name is used
        //         
        public virtual async Task colourResultsFile(FeatureLayer layer = null, CIMClassBreaksRenderer renderer = null, CIMColorRamp ramp = null) {
            bool keepColours = false;
            CIMSymbol symbol;
            var haveLayer = layer is not null;
            if (haveLayer) {
                keepColours = false;
            }
            if (this.useSubs()) {
                if (!haveLayer) {
                    layer = this.subResultsLayer;
                    keepColours = this.keepSubColours;
                }
                symbol = SymbolFactory.Instance.ConstructPolygonSymbol();
            } else if (this.useHRUs()) {
                if (!haveLayer) {
                    layer = this.hruResultsLayer;
                    keepColours = this.keepHRUColours;
                }
                symbol = SymbolFactory.Instance.ConstructPolygonSymbol();
            } else {
                if (!haveLayer) {
                    layer = this.rivResultsLayer;
                    keepColours = this.keepRivColours;
                }
                //var props = new Dictionary<object, object> {
                //    {
                //        "width_expression",
                //        QSWATTopology._PENWIDTH}};
                symbol = SymbolFactory.Instance.ConstructLineSymbol();
            }
            var selectVar = this._dlg.PvariableList.SelectedItems[0].ToString();
            var selectVarShort = selectVar.Substring(0, Math.Min(selectVar.Length, 10));
            Debug.Assert(layer is not null);
            if (!haveLayer) {
                var summary = this._dlg.PsummaryCombo.SelectedItem.ToString();
                await QueuedTask.Run(() => {
                    layer.SetName(string.Format("{0} {1} {2}", this.scenario, selectVar, summary));
                });
            }
            int count = 5;
            //double opacity;
            double transparency = 0;
            // TODO for now at least calculate renderer afresh
            //if (!keepColours) {
            //    count = 5;
            //    //opacity = this.useSubs() || this.useHRUs() ? 0.65 : 1.0;
            //    transparency = this.useSubs() || this.useHRUs() ? 35 : 0;
            //} else {
            //    // same layer as currently - try to use same range size and colours, and same opacity
            //    try {
            //        var oldRenderer = layer.GetRenderer();
            //        var oldRanges = oldRenderer.Breaks;
            //        count = oldRanges.Count();
            //        ramp = oldRenderer.ColorRamp;
            //        //opacity = 1.0 - layer.Transparency / (double)100;
            //        transparency = layer.Transparency;
            //    } catch (Exception) {
            //        // don't care if no suitable colours, so no message, just revert to defaults
            //        keepColours = false;
            //        count = 5;
            //        //opacity = this.useSubs() || this.useHRUs() ? 0.65 : 1.0;
            //        transparency = this.useSubs() || this.useHRUs() ? 35 : 0;
            //    }
            //}
            if (true)  {   // was !keepColours) {
                if (ramp is null) {
                    ramp = await this.chooseColorRamp(this.table, selectVar);
                }
                if (ramp is null) {
                    var fileName = await Utils.layerFilename(layer);
                    Utils.error(string.Format("Internal error: Failed to make renderer for {0}", fileName), this._gv.isBatch);
                }
            }
            //var labelFmt = QgsRendererRangeLabelFormat("%1 - %2", 0);
            GraduatedColorsRendererDefinition rendererDefn = null;
            if (renderer is null) {
                rendererDefn = new GraduatedColorsRendererDefinition() {
                    ClassificationField = selectVarShort,
                    ClassificationMethod = ArcGIS.Core.CIM.ClassificationMethod.NaturalBreaks,
                    BreakCount = count,
                    ColorRamp = ramp,
                    SymbolTemplate = symbol.MakeSymbolReference()
                }; 
                //renderer = QgsGraduatedSymbolRenderer.createRenderer(layer, selectVarShort, count, QgsGraduatedSymbolRenderer.Jenks, symbol, ramp, labelFmt);
            } else {
                //renderer..setSourceSymbol(symbol);
                //renderer.setLabelFormat(labelFmt);
            }
            //renderer.calculateLabelPrecision(true);
            //         # previous line should be enough to update precision, but in practice seems we need to recreate renderer
            //         precision = renderer.labelFormat().precision()
            //         Utils.loginfo('Precision: {0}', precision))
            //         # default seems too high
            //         labelFmt = QgsRendererRangeLabelFormat('%1 - %2', precision-1)
            //         # should be enough to update labelFmt, but seems to be necessary to make renderer again to reflect new precision
            //         renderer = QgsGraduatedSymbolRenderer.createRenderer(layer, selectVarShort, count, 
            //                                                              QgsGraduatedSymbolRenderer.Jenks, symbol, 
            //                                                              ramp, labelFmt)
            if (this.useSubs()) {
                this.internalChangeToSubRenderer = true;
            } else if (this.useHRUs()) {
                this.internalChangeToHRURenderer = true;
            } else {
                this.internalChangeToRivRenderer = true;
            }
            await QueuedTask.Run(() => {
                if (renderer is null) {
                    renderer = layer.CreateRenderer(rendererDefn) as CIMClassBreaksRenderer;
                }
                CIMVisualVariable[] visualVars = new CIMVisualVariable[1];
                var expressionInfo = new CIMExpressionInfo() {
                    Expression = "$feature.PenWidth",
                    Title = "Custom"
                };
                var sizeVar = new CIMSizeVisualVariable() {
                    ValueExpressionInfo = expressionInfo,
                    VariableType = SizeVisualVariableType.Expression,
                    MaxSize = 0,
                    MinValue = 1,
                    MaxValue = 8
                };
                visualVars[0] = sizeVar;
                renderer.VisualVariables = visualVars;
                layer.SetRenderer(renderer);
                layer.SetTransparency(transparency);
                // TODO
                //this.clearMapTitle();
                //this.mapTitle = MapTitle(canvas, this.title, layer);
                //MapView.Active.Redraw(true);
            });
            if (this.useSubs()) {
                this.internalChangeToSubRenderer = false;
                this.keepSubColours = keepColours;
            } else if (this.useHRUs()) {
                this.internalChangeToHRURenderer = false;
                this.keepHRUColours = keepColours;
            } else {
                this.internalChangeToRivRenderer = false;
                this.keepRivColours = keepColours;
            }
            var tip = string.Format("\"{0}: \" + Text($feature.{1})", selectVar, selectVarShort);
            await Utils.setMapTip(this.currentResultsLayer, selectVarShort, tip);
        }

        // Add any extra fields to variableList.
        public virtual void addResultsVars() {
            if (!this.resultsLayerExists()) {
                return;
            }
            var newVars = new List<string>();
            Debug.Assert(this.currentResultsLayer is not null);
            var fields = (this.currentResultsLayer.GetDefinition() as CIMBasicFeatureLayer).FeatureTable.FieldDescriptions;
            //var indexes = fields.allAttributesList();
            //foreach (var i in indexes) {
            //    if (fields.fieldOrigin(i) == QgsFields.OriginEdit) {
            //        // added by editing
            //        newVars.Add(fields.at(i).name());
            //    }
            //}
            // TODO check if field added by editing
            foreach (int i in Enumerable.Range(0, fields.Length)) {
                newVars.Add(fields[i].FieldName);
            }
            foreach (var vari in newVars) {
                var i = this._dlg.PvariableList.FindString(vari);
                if (i == ListBox.NoMatches) {
                    // add vari to variableList
                    this._dlg.PvariableList.Items.Add(vari);
                }
            }
        }

        // Return true if current results layer has not been removed.
        public virtual bool resultsLayerExists() {
            //         if self.useSubs():
            //             layer = self.subResultsLayer
            //         elif self.useHRUs():
            //             layer = self.hruResultsLayer
            //         else:
            //             layer = self.rivResultsLayer
            if (this.currentResultsLayer is null) {
                return false;
            }
            var resultsGroup = Utils.getGroupLayerByName(Utils._RESULTS_GROUP_NAME);
            return resultsGroup is not null && resultsGroup.FindLayers(this.currentResultsLayer.Name).Count > 0;
        }

        // 
        //         Create animation with new shapefile or existing one.
        //         
        //         Assumes:
        //         - animation variable is set
        //         
        public virtual async Task<bool> createAnimationLayer() {
            var @base = this.useSubs() ? Parameters._SUBS : this.useHRUs() ? Parameters._HRUS : Parameters._RIVS;
            var resultsBase = Utils.join(this._gv.tablesOutDir, @base) + ".shp";
            var animateFileBase = Utils.join(this._gv.animationDir, @base) + ".shp";
            int num = 0;
            var animateFile = Utils.nextFileName(animateFileBase, ref num);
            int animateIndex = 0;
            Utils.copyShapefile(resultsBase, @base + num.ToString(), this._gv.animationDir);
            if (string.IsNullOrEmpty(this.stillFileBase)) {
                this.stillFileBase = Utils.join(this._gv.pngDir, Parameters._STILLPNG);
            }
            this.currentStillNumber = 0;
            using (var animateDs = Ogr.Open(animateFile, 1)) {
                //var animateLayer = QgsVectorLayer(animateFile, "{0} {1}", this.scenario, this.animateVar), "ogr");
                var animateLayer = animateDs.GetLayerByIndex(0);
                var defn = animateLayer.GetLayerDefn();
                //var provider = animateLayer.dataProvider();
                var field = new OSGeo.OGR.FieldDefn(this.animateVar, FieldType.OFTReal);
                // TODO this gives wrong number because Id field added later (where?)
                animateIndex = animateLayer.CreateField(field, 1);
                //if (!provider.addAttributes(new List<object> {
                //    field
                //})) {
                //    Utils.error("Could not add field {0} to animation file {1}", this.animateVar, animateFile), this._gv.isBatch);
                //    return false;
                //}
                //animateLayer.updateFields();
                //var animateIndex = this._gv.topo.getProviderIndex(provider, this.animateVar);
            }
            // place layer at top of animation group if new,
            // else above current animation layer, and remove that
            var animationGroup = Utils.getGroupLayerByName(Utils._ANIMATION_GROUP_NAME);
            Debug.Assert(animationGroup is not null);
            var index = 0;
            if (this._dlg.PcurrentAnimation.Checked) {
                var animations = animationGroup.Layers;
                if (animations.Count == 1) {
                    await QueuedTask.Run(() => MapView.Active.Map.RemoveLayer(animations[0]));
                index = 0;
                } else {
                    var currentLayers = MapView.Active.GetSelectedLayers();
                    if (currentLayers.Count > 0) {
                        var currentLayerName = currentLayers[0].Name;
                        foreach (var i in Enumerable.Range(0, animations.Count)) {
                            if (animations[i].Name == currentLayerName) {
                                index = i;
                                await QueuedTask.Run(() => MapView.Active.Map.RemoveLayer(animations[i]));
                                break;
                            }
                        }
                    }
                }
            }
            this.animateLayer = await QueuedTask.Run(() => LayerFactory.Instance.CreateLayer(new Uri(animateFile), 
                animationGroup, index, string.Format("{0} {1}", this.scenario, this.animateVar))) as FeatureLayer;
            //Debug.Assert(this.animateLayer is not null);
            this.animateIndexes[this.animateLayer.Name] = animateIndex;
            this.animateVars[this.animateLayer.Name] = this.animateVar;
            // add labels if based on subbasins
            // TODO
            //if (this.useSubs()) {
            //    this.animateLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, "subsresults.qml"));
            //}
            return true;
        }

        // Colour animation layer.
        //         
        //         Assumes allAnimateVals is suitably populated.
        //         
        public virtual async Task colourAnimationLayer(bool useLine) {
            double transparency = useLine ? 0 : 35;
            CIMColorRamp ramp;
            ramp = await this.chooseColorRamp(this.table, this.animateVar);
            var renderer = await this.makeJenksRenderer(this.allAnimateVals, ramp, this.animateVar, useLine);
            //         renderer.setMode(QgsGraduatedSymbolRenderer.Custom)
            //         renderer.calculateLabelPrecision()
            //         precision = renderer.labelFormat().precision()
            //         Utils.loginfo('Animation precision: {0}', precision))
            //         # repeat with calculated precision
            //         rangeList = []
            //         for i in range(count):
            //             # adjust min and max by 1% to avoid rounding errors causing values to be outside the range
            //             minVal = cbreaks[0] * 0.99 if i == 0 else cbreaks[i]
            //             maxVal = cbreaks[count] * 1.01 if i == count-1 else cbreaks[i+1]
            //             f = float(i)
            //             colourVal = (count - f) / (count - 1) if invert else f / (count - 1)
            //             colour = ramp.color(colourVal)
            //             # default precision too high
            //             rangeList.Add(self.makeSymbologyForRange(minVal, maxVal, colour, precision-1))
            //         renderer = QgsGraduatedSymbolRenderer(self.animateVar[:10], rangeList)
            //         renderer.setMode(QgsGraduatedSymbolRenderer.Custom)
            Debug.Assert(this.animateLayer is not null);
            await QueuedTask.Run(() => {
                if (useLine) {
                    CIMVisualVariable[] visualVars = new CIMVisualVariable[1];
                    var expressionInfo = new CIMExpressionInfo() {
                        Expression = "$feature.PenWidth",
                        Title = "Custom"
                    };
                    var sizeVar = new CIMSizeVisualVariable() {
                        ValueExpressionInfo = expressionInfo,
                        VariableType = SizeVisualVariableType.Expression,
                        MaxSize = 0,
                        MinValue = 1,
                        MaxValue = 8
                    };
                    visualVars[0] = sizeVar;
                    renderer.VisualVariables = visualVars;
                }
                this.animateLayer.SetRenderer(renderer);
                this.animateLayer.SetTransparency(transparency);
            });
            //         animations = Utils.getLayersInGroup(Utils._ANIMATION_GROUP_NAME, li, visible=True)
            //         if len(animations) > 0:
            //             canvas = self._iface.mapCanvas()
            //             if self.mapTitle is not None:
            //                 canvas.scene().removeItem(self.mapTitle)
            //             self.mapTitle = MapTitle(canvas, self.title, animations[0])
            //             canvas.refresh()
        }

        //TODO
        //// Create print composer to capture each animation step.
        //public virtual object createAnimationComposition() {
        //    object height;
        //    object width;
        //    object templ;
        //    var proj = QgsProject.instance();
        //    var root = proj.layerTreeRoot();
        //    var animationLayers = Utils.getLayersInGroup(Utils._ANIMATION_GROUP_NAME, root);
        //    var watershedLayers = Utils.getLayersInGroup(Utils._WATERSHED_GROUP_NAME, root, visible: true);
        //    // choose template file and set its width and height
        //    // width and height here need to be updated if template file is changed
        //    var count = this._dlg.composeCount.Value;
        //    var isLandscape = this._dlg.composeLandscape.Checked;
        //    if (count == 1) {
        //        if (isLandscape) {
        //            templ = "1Landscape.pagx";
        //            width = 230.0;
        //            height = 160.0;
        //        } else {
        //            templ = "1Portrait.pagx";
        //            width = 190.0;
        //            height = 200.0;
        //        }
        //    } else if (count == 2) {
        //        if (isLandscape) {
        //            templ = "2Landscape.pagx";
        //            width = 125.0;
        //            height = 120.0;
        //        } else {
        //            templ = "2Portrait.pagx";
        //            width = 150.0;
        //            height = 120.0;
        //        }
        //    } else if (count == 3) {
        //        if (isLandscape) {
        //            templ = "3Landscape.pagx";
        //            width = 90.0;
        //            height = 110.0;
        //        } else {
        //            templ = "3Portrait.pagx";
        //            width = 150.0;
        //            height = 80.0;
        //        }
        //    } else if (count == 4) {
        //        if (isLandscape) {
        //            templ = "4Landscape.pagx";
        //            width = 95.0;
        //            height = 80.0;
        //        } else {
        //            templ = "4Portrait.pagx";
        //            width = 85.0;
        //            height = 85.0;
        //        }
        //    } else if (count == 6) {
        //        if (isLandscape) {
        //            templ = "6Landscape.pagx";
        //            width = 90.0;
        //            height = 40.0;
        //        } else {
        //            templ = "6Portrait.pagx";
        //            width = 55.0;
        //            height = 80.0;
        //        }
        //    } else {
        //        Utils.error("There are composition templates only for 1, 2, 3, 4 or 6 result maps, not for {0}", count), this._gv.isBatch);
        //        return;
        //    }
        //    var templateIn = Utils.join(this._gv.plugin_dir, "PrintTemplate" + templ);
        //    this.animationTemplate = Utils.join(this._gv.tablesOutDir, "AnimationTemplate.pagx");
        //    // make substitution table
        //    var subs = new dict();
        //    var northArrow = Utils.join(os.getenv("OSGEO4W_ROOT"), Visualise._NORTHARROW);
        //    if (!File.Exists(northArrow)) {
        //        // may be qgis-ltr for example
        //        var northArrowRel = Visualise._NORTHARROW.replace("qgis", Utils.qgisName(), 1);
        //        northArrow = Utils.join(os.getenv("OSGEO4W_ROOT"), northArrowRel);
        //    }
        //    if (!File.Exists(northArrow)) {
        //        Utils.error("Failed to find north arrow {0}.  You will need to repair the layout.", northArrow), this._gv.isBatch);
        //    }
        //    subs["%%NorthArrow%%"] = northArrow;
        //    subs["%%ProjectName%%"] = this.title;
        //    var numLayers = animationLayers.Count;
        //    if (count > numLayers) {
        //        Utils.error("You want to make a print of {0} maps, but you only have {1} animation layers", count, numLayers), this._gv.isBatch);
        //        return;
        //    }
        //    var extent = this._iface.mapCanvas().extent();
        //    var xmax = extent.xMaximum();
        //    var xmin = extent.xMinimum();
        //    var ymin = extent.yMinimum();
        //    var ymax = extent.yMaximum();
        //    Utils.loginfo("Map canvas extent {0}, {1}, {2}, {3}", round(xmin).ToString(), round(ymin).ToString(), round(xmax).ToString(), round(ymax).ToString()));
        //    // need to expand either x or y extent to fit map shape
        //    var xdiff = (ymax - ymin) / height * width - (xmax - xmin);
        //    if (xdiff > 0) {
        //        // need to expand x extent
        //        xmin = xmin - xdiff / 2;
        //        xmax = xmax + xdiff / 2;
        //    } else {
        //        // expand y extent
        //        var ydiff = (xmax - xmin) / width * height - (ymax - ymin);
        //        ymin = ymin - ydiff / 2;
        //        ymax = ymax + ydiff / 2;
        //    }
        //    Utils.loginfo("Map extent set to {0}, {1}, {2}, {3}", round(xmin).ToString(), round(ymin).ToString(), round(xmax).ToString(), round(ymax).ToString()));
        //    // estimate of segment size for scale
        //    // aim is approx 10mm for 1 segment
        //    // we make size a power of 10 so that segments are 1km, or 10, or 100, etc.
        //    var segSize = Math.Pow(10, round(math.log10((xmax - xmin) / (width / 10))));
        //    var layerStr = "<Layer source=\"{0}\" provider=\"ogr\" name=\"{1}\">{2}</Layer>";
        //    foreach (var i in Enumerable.Range(0, count)) {
        //        var layer = animationLayers[i].layer();
        //        Debug.Assert(layer is not null);
        //        subs["%%LayerId{0}%%", i)] = layer.id();
        //        subs["%%LayerName{0}%%", i)] = layer.name();
        //        subs["%%YMin{0}%%", i)] = ymin.ToString();
        //        subs["%%XMin{0}%%", i)] = xmin.ToString();
        //        subs["%%YMax{0}%%", i)] = ymax.ToString();
        //        subs["%%XMax{0}%%", i)] = xmax.ToString();
        //        subs["%%ScaleSegSize%%"] = segSize.ToString();
        //        subs["%%Layer{0}%%", i)] = layerStr, Utils.layerFilename(layer), layer.name(), layer.id());
        //    }
        //    foreach (var i in Enumerable.Range(0, 6)) {
        //        // 6 entries in template for background layers
        //        if (i < watershedLayers.Count) {
        //            var wLayer = watershedLayers[i].layer();
        //            Debug.Assert(wLayer is not null);
        //            subs["%%WshedLayer{0}%%", i)] = layerStr, Utils.layerFilename(wLayer), wLayer.name(), wLayer.id());
        //        } else {
        //            // remove unused ones
        //            subs["%%WshedLayer{0}%%", i)] = "";
        //        }
        //    }
        //    // seems to do no harm to leave unused <Layer> items with original pattern, so we don't bother removing them
        //    using (var inFile = open(templateIn, "rU")) {
        //        using (var outFile = open(this.animationTemplate, "w")) {
        //            foreach (var line in inFile) {
        //                outFile.write(Visualise.replaceInLine(line, subs));
        //            }
        //        }
        //    }
        //    Utils.loginfo("Print layout template {0} written", this.animationTemplate));
        //    this.animationDOM = QDomDocument();
        //    var f = QFile(this.animationTemplate);
        //    if (f.open(QIODevice.ReadOnly)) {
        //        var OK = this.animationDOM.setContent(f)[0];
        //        if (!OK) {
        //            Utils.error("Cannot parse template file {0}", this.animationTemplate), this._gv.isBatch);
        //            return;
        //        }
        //    } else {
        //        Utils.error("Cannot open template file {0}", this.animationTemplate), this._gv.isBatch);
        //        return;
        //    }
        //    if (!this._gv.isBatch) {
        //        Utils.information(@"
        //    The layout designer is about to start, showing the current layout for the animation.
            
        //    You can change the layout as you wish, and then you should 'Save as Template' in the designer menu, using {0} as the template file.  
        //    If this file already exists: you will have to confirm overwriting it.
        //    Then close the layout designer.
        //    If you don't change anything you can simply close the layout designer without saving.
            
        //    Then start the animation running.
        //    ", this.animationTemplate), false);
        //        var title = "Animation base";
        //        // remove layout from layout manager, in case still there
        //        try {
        //            Debug.Assert(this.animationLayout is not null);
        //            proj.layoutManager().removeLayout(this.animationLayout);
        //        } catch {
        //        }
        //        this.animationLayout = null;
        //        this.animationLayout = QgsPrintLayout(proj);
        //        this.animationLayout.initializeDefaults();
        //        this.animationLayout.setName(title);
        //        this.setDateInTemplate();
        //        var items = this.animationLayout.loadFromTemplate(this.animationDOM, QgsReadWriteContext());
        //        var ok = proj.layoutManager().addLayout(this.animationLayout);
        //        if (!ok) {
        //            Utils.error("Failed to add animation layout to layout manager.  Try removing some.", this._gv.isBatch);
        //            return;
        //        }
        //        var designer = this._gv.iface.openLayoutDesigner(layout: this.animationLayout);
        //        this.animationTemplateDirty = true;
        //    }
        //}

        // TODO
        //// Reread animation template file.
        //public virtual object rereadAnimationTemplate() {
        //    this.animationTemplateDirty = false;
        //    this.animationDOM = QDomDocument();
        //    var f = QFile(this.animationTemplate);
        //    if (f.open(QIODevice.ReadOnly)) {
        //        var OK = this.animationDOM.setContent(f)[0];
        //        if (!OK) {
        //            Utils.error("Cannot parse template file {0}", this.animationTemplate), this._gv.isBatch);
        //            return;
        //        }
        //    } else {
        //        Utils.error("Cannot open template file {0}", this.animationTemplate), this._gv.isBatch);
        //        return;
        //    }
        //}

        // TODO
        //// Set current animation date in title field.
        //public virtual object setDateInTemplate() {
        //    Debug.Assert(this.animationDOM is not null);
        //    var itms = this.animationDOM.elementsByTagName("LayoutItem");
        //    foreach (var i in Enumerable.Range(0, itms.length())) {
        //        var itm = itms.item(i);
        //        var attr = itm.attributes().namedItem("id").toAttr();
        //        if (attr is not null && attr.Value == "Date") {
        //            var title = itm.attributes().namedItem("labelText").toAttr();
        //            if (title is null) {
        //                Utils.error("Cannot find template date label", this._gv.isBatch);
        //                return;
        //            }
        //            title.setValue(this._dlg.dateLabel.Text);
        //            return;
        //        }
        //    }
        //    Utils.error("Cannot find template date label", this._gv.isBatch);
        //    return;
        //}

        //TODO
        //// Set current animation date in title field.
        //public virtual object setDateInComposer() {
        //    Debug.Assert(this.animationDOM is not null);
        //    var labels = this.animationDOM.elementsByTagName("ComposerLabel");
        //    foreach (var i in Enumerable.Range(0, labels.length())) {
        //        var label = labels.item(i);
        //        var item = label.namedItem("ComposerItem");
        //        var attr = item.attributes().namedItem("id").toAttr();
        //        if (attr is not null && attr.Value == "Date") {
        //            var title = label.attributes().namedItem("labelText").toAttr();
        //            if (title is null) {
        //                Utils.error("Cannot find composer date label", this._gv.isBatch);
        //                return;
        //            }
        //            title.setValue(this._dlg.dateLabel.Text);
        //            return;
        //        }
        //    }
        //    Utils.error("Cannot find composer date label", this._gv.isBatch);
        //    return;
        //}

        // 
        //         Display animation data for current slider value.
        //         
        //         Get date from slider value; read animation data for date; write to animation file; redisplay.
        //         
        public virtual async Task changeAnimate() {
            string @ref;
            int sub;
            List<FeatureLayer> animateLayers;
            //try {
            if (true) { 
                if (this._dlg.PanimationVariableCombo.SelectedItem.ToString() == "") {
                    Utils.information("Please choose an animation variable", this._gv.isBatch);
                    this.doRewind();
                    return;
                }
                if (this.capturing) {
                    await this.capture();
                }
                var dat = this.sliderValToDate();
                var date = this.dateToString(dat);
                this._dlg.PdateLabel.Text = date;
                if (this._dlg.PcanvasAnimation.Checked) {
                    animateLayers = new List<FeatureLayer> {
                        this.animateLayer
                    };
                } else {
                    var layers = Utils.getLayersInGroup(Utils._ANIMATION_GROUP_NAME, visible: false);
                    animateLayers = (from layer in layers
                                     where layer is not null
                                     select layer as FeatureLayer).ToList();
                }
                foreach (var animateLayer in animateLayers) {
                    if (animateLayer is null) {
                        continue;
                    }
                    var layerName = animateLayer.Name;
                    var animateFile = await Utils.layerFilename(this.animateLayer);
                    var animateIndex = this.animateIndexes[layerName];
                    var animateVar = this.animateVars[layerName];
                    var animateVarShort = animateVar.Substring(0, Math.Min(10, animateVar.Length));
                    var data = this.animateResultsData[layerName][dat];
                    // TODO
                    //Debug.Assert(this.mapTitle is not null);
                    //this.mapTitle.updateLine2(date);
                    await QueuedTask.Run(async () => {
                        if (!animateLayer.CanEditData()) {
                            animateLayer.SetEditable(true);
                            if (!animateLayer.CanEditData()) {
                                Utils.error("Cannot edit animation layer " + layerName, false);
                                return;
                            }
                        }
                        var layerDefn = animateLayer.GetFeatureClass().GetDefinition();
                        // cannot use useHRUs as it will only be correct for top layer
                        var subIdx = 0;
                        var hruIdx = layerDefn.FindField(Topology._HRUGIS);
                        if (hruIdx < 0) {
                            // not an HRUs layer
                            subIdx = layerDefn.FindField(Topology._SUBBASIN);
                        }
                        using (var rc = animateLayer.Search()) {
                            while (rc.MoveNext()) {
                                using (var f = rc.Current as ArcGIS.Core.Data.Feature) {
                                    bool result = false;
                                    var op = new ArcGIS.Desktop.Editing.EditOperation();
                                    if (hruIdx >= 0) {
                                        // May be split HRUs; just use first
                                        // This is inadequate for some variables, but no way to know if correct val is sum of vals, mean, etc.
                                        sub = Convert.ToInt32(f[hruIdx].ToString().Split(',')[0]);
                                    } else {
                                        sub = Convert.ToInt32(f[subIdx]);
                                    }
                                    double val;
                                    if (data.ContainsKey(sub)) {
                                        val = data[sub];
                                    } else {
                                        if (hruIdx >= 0) {
                                            @ref = string.Format("HRU {0}", sub);
                                        } else {
                                            @ref = string.Format("subbasin {0}", sub);
                                        }
                                        Utils.error(string.Format("Cannot get data for {0}: have you run SWAT and saved data since running QSWAT", @ref), this._gv.isBatch);
                                        return;
                                    }
                                    op.Modify(f, animateVarShort, val);
                                    if (!op.IsEmpty) {
                                        result = await op.ExecuteAsync();
                                    }
                                    if (!result) {
                                        Utils.error(string.Format("Could not set attributes in results file {0}", this.resultsFile), this._gv.isBatch);
                                        return;
                                    }
                                }
                            }
                        }
                    });
                }
                await Project.Current.SaveEditsAsync();
                //await QueuedTask.Run(() => {
                //    MapView.Active.Redraw(false);
                //});
                this._dlg.PdateLabel.Refresh();
            //} catch (Exception) {
            //    this.animating = false;
            //    throw;
            }
        }

        // Make image file of current canvas.
        public virtual async Task capture() {
            if (this.animateLayer is null) {
                return;
            }
            //await QueuedTask.Run(() => MapView.Active.Redraw(false));
            this.currentStillNumber += 1;
            var @base = Path.ChangeExtension(this.stillFileBase, null);
            var suffix = Path.GetExtension(this.stillFileBase);
            var nextStillFile = @base + string.Format("{0:D5}", this.currentStillNumber) + suffix;
            // this does not capture the title
            //self._iface.mapCanvas().saveAsImage(nextStillFile)
            var composingAnimation = this._dlg.PprintAnimation.Checked;
            if (composingAnimation) {
                // remove layout if any
                // TODO
                //try {
                //    Debug.Assert(this.animationLayout is not null);
                //    proj.layoutManager().removeLayout(this.animationLayout);
                //} catch {
                //}
                //this.animationLayout = null;
                //if (this.animationTemplateDirty) {
                //    this.rereadAnimationTemplate();
                //}
                //var title = "Animation {0}", this.compositionCount);
                //this.compositionCount += 1;
                //this.animationLayout = QgsPrintLayout(proj);
                //this.animationLayout.initializeDefaults();
                //this.animationLayout.setName(title);
                //this.setDateInTemplate();
                //Debug.Assert(this.animationDOM is not null);
                //var _ = this.animationLayout.loadFromTemplate(this.animationDOM, QgsReadWriteContext());
                //var ok = proj.layoutManager().addLayout(this.animationLayout);
                //if (!ok) {
                //    Utils.error("Failed to add animation layout to layout manager.  Try removing some.", this._gv.isBatch);
                //    return;
                //}
                //var exporter = QgsLayoutExporter(this.animationLayout);
                //var settings = QgsLayoutExporter.ImageExportSettings();
                //settings.exportMetadata = false;
                //var res = exporter.exportToImage(nextStillFile, settings);
                //if (res != QgsLayoutExporter.Success) {
                //    Utils.error("Failed with result {1} to save layout as image file {0}", nextStillFile, res), this._gv.isBatch);
                //}
            } else {
                // tempting bot omits canvas title
                // canvas.saveAsImage(nextStillFile)
                await QueuedTask.Run(() => {
                    var PNG = new PNGFormat();
                    PNG.Resolution = 300;
                    PNG.OutputFileName = nextStillFile;
                    if (PNG.ValidateOutputFilePath()) {
                        MapView.Active.Export(PNG);
                    } else {
                        Utils.error(string.Format("Failed to save canvas as image file {0}", nextStillFile), this._gv.isBatch);
                    }
                });


                // no longer used
                //===========================================================================
                // def minMax(self, layer, var):
                //     """
                //     Return minimum and maximum of values for var in layer.
                //     
                //     Subbasin values of 0 indicate subbasins upstream from inlets and are ignored.
                //     """
                //     minv = float('inf')
                //     maxv = 0
                //     for f in layer.getFeatures():
                //         sub = f.attribute(QSWATTopology._SUBBASIN)
                //         if sub == 0:
                //             continue
                //         val = f.attribute(var)
                //         minv = min(minv, val)
                //         maxv = max(maxv, val)
                //     # increase/decrease by 1% to ensure no rounding errors cause values to be outside all ranges
                //     maxv *= 1.01
                //     minv *= 0.99
                //     return minv, maxv
                //===========================================================================
                // no longer used
                //===========================================================================
                // def dataList(self, var):
                //     """Make list of data values for var from resultsData for creating Jenks breaks."""
                //     res = []
                //     for subvals in self.resultsData.Values:
                //         res.Add(subvals[var])
                //     return res
                //===========================================================================
            }
        }

        // Create a range from minv to maxv with the colour.
        public virtual CIMClassBreak makeSymbologyForRange(double minv, double maxv, CIMColor colour, int precision, bool useLine) {
            string title;
            CIMSymbolReference symbolRef;
            if (useLine) {
                var symbol = SymbolFactory.Instance.ConstructLineSymbol(colour);
                symbolRef = symbol.MakeSymbolReference();
            } else {
                var symbol = SymbolFactory.Instance.ConstructPolygonSymbol(colour);
                symbolRef = symbol.MakeSymbolReference();
            }
            if (precision > 0) {
                title = string.Format("{0:G" + precision.ToString() + "} - {1:G" + precision.ToString() + "}", minv, maxv);
            } else {
                var factor = Convert.ToInt32(Math.Pow(10, Math.Abs(precision)));
                var minv1 = Math.Round((double)minv / factor) * factor;
                var maxv1 = Math.Round((double)maxv / factor) * factor;
                title = string.Format("{0} - {1}", minv1, maxv1);
            }
            var brk = new CIMClassBreak() {
                UpperBound = maxv,
                Label = title,
                Symbol = symbolRef
            };
            return brk;
        }

        // Select a colour ramp.
        public virtual async Task<CIMColorRamp> chooseColorRamp(string table, string vari) {
            CIMColorRamp ramp;
            var rchWater = new List<string> {
                "FLOW_INcms",
                "FLOW_OUTcms",
                "EVAPcms",
                "TLOSScms"
            };
            var subPrecip = new List<string> {
                "PRECIPmm",
                "PETmm",
                "ETmm"
            };
            var subWater = new List<string> {
                "SNOMELTmm",
                "SWmm",
                "PERCmm",
                "SURQmm",
                "GW_Qmm",
                "WYLDmm"
            };
            var hruPrecip = new List<string> {
                "PRECIPmm",
                "SNOWFALLmm",
                "PETmm",
                "ETmm"
            };
            var hruWater = new List<string> {
                "SNOWMELTmm",
                "IRRmm",
                "SW_INITmm",
                "SW_ENDmm",
                "PERCmm",
                "GW_RCHGmm",
                "DA_RCHG",
                "REVAP",
                "SA_IRRmm",
                "DA_IRRmm",
                "SA_STmm",
                "DA_STmm",
                "SURQ_GENmm",
                "SURQ_CNTmm",
                "TLOSSmm",
                "LATQ_mm",
                "GW_Qmm",
                "WYLD_Qmm",
                "SNOmm",
                "QTILEmm"
            };
            var hruPollute = new List<string> {
                "SYLDt_ha",
                "USLEt_ha",
                "ORGNkg_ha",
                "ORGPkg_ha",
                "SEDPkg_h",
                "NSURQkg_ha",
                "NLATQkg_ha",
                "NO3Lkg_ha",
                "NO3GWkg_ha",
                "SOLPkg_ha",
                "P_GWkg_ha",
                "BACTPct",
                "BACTLPct",
                "TNO3kg/ha",
                "LNO3kg/ha"
            };
            if (table == "sed" || table == "wql" || table == "rch" && !rchWater.Contains(vari) || table == "sub" && !subPrecip.Contains(vari) && !subWater.Contains(vari) || table == "hru" && hruPollute.Contains(vari)) {
                // sediments and pollutants
                // Condition Number is the reversal of Red-Yellow-Green
                ramp = await FileTypes.SWATRamp("RdYlGn");
                return ramp;
                //return (style.colorRamp("RdYlGn"), true);
            } else if (table == "rch" && rchWater.Contains(vari) || table == "sub" && subWater.Contains(vari) || table == "hru" && hruWater.Contains(vari)) {
                // water
                ramp = await FileTypes.SWATRamp("YlGnBu");
                return ramp;
                //return (style.colorRamp("YlGnBu"), false);
            } else if (table == "sub" && subPrecip.Contains(vari) || table == "hru" && hruPrecip.Contains(vari)) {
                // precipitation and transpiration:
                ramp = await FileTypes.SWATRamp("GnBu");
                return ramp;
                //return (style.colorRamp("GnBu"), false);
            } else {
                ramp = await FileTypes.SWATRamp("YlOrRd");
                return ramp;
                //return (style.colorRamp("YlOrRd"), false);
            }
        }

        // Main tab has changed.  Show/hide Animation group.
        public virtual async Task modeChange() {
            var expandAnimation = this._dlg.PtabWidget.SelectedIndex == 1;
            var animationGroup = Utils.getGroupLayerByName(Utils._ANIMATION_GROUP_NAME);
            Debug.Assert(animationGroup is not null);
            await QueuedTask.Run(() => {
                animationGroup.SetVisibility(expandAnimation);
            });
        }

        // Make single results file or comparison results files if self.scenarios1 is not empty.
        public virtual async Task makeResults0() {
            if (this.scenario1 == "") {
                await this.makeResults();
                return;
            }
            var currentScenario = this.scenario;
            this.scenario = this.scenario1;
            var scenDir = Utils.join(this._gv.scenariosDir, this.scenario);
            var outDir = Utils.join(scenDir, Parameters._TABLESOUT);
            var outFile = Utils.join(outDir, this.table + "results.shp");
            this._dlg.PresultsFileEdit.Text = outFile;
            this.setConnection(this.scenario);
            await this.makeResults();
            this.scenario = this.scenario2;
            scenDir = Utils.join(this._gv.scenariosDir, this.scenario);
            outDir = Utils.join(scenDir, Parameters._TABLESOUT);
            outFile = Utils.join(outDir, this.table + "results.shp");
            this._dlg.PresultsFileEdit.Text = outFile;
            this.setConnection(this.scenario);
            await this.makeResults();
            // restore current scenario and results shapefile
            this.scenario = currentScenario;
            scenDir = Utils.join(this._gv.scenariosDir, this.scenario);
            outDir = Utils.join(scenDir, Parameters._TABLESOUT);
            outFile = Utils.join(outDir, this.table + "results.shp");
            this._dlg.PresultsFileEdit.Text = outFile;
            this.setConnection(this.scenario);
        }

        // 
        //         Create a results file and display.
        //         
        //         Only creates a new file if the variables have changed.
        //         If variables unchanged, only makes and writes summary data if necessary.
        //         
        public virtual async Task makeResults() {
            if (this.table == "") {
                Utils.information("Please choose a SWAT output table", this._gv.isBatch);
                return;
            }
            if (this._dlg.PresultsFileEdit.Text == "") {
                Utils.information("Please choose a results file", this._gv.isBatch);
                return;
            }
            if (this._dlg.PvariableList.Items.Count == 0) {
                Utils.information("Please choose some variables", this._gv.isBatch);
                return;
            }
            if (this._dlg.PvariableList.SelectedItems.Count == 0) {
                Utils.information("Please select a variable for display", this._gv.isBatch);
                return;
            }
            if (!this.setPeriods()) {
                return;
            }
            if (this.scenario1 != "") {
                var _ = await this.makeCompareResults();
                return;
            }
            
            Cursor.Current = Cursors.WaitCursor;
            this.resultsFileUpToDate = this.resultsFileUpToDate && this.resultsFile == this._dlg.PresultsFileEdit.Text;
            if (!this.resultsFileUpToDate || !this.periodsUpToDate) {
                if (!this.readData("", true, this.table, "", "")) {
                    return;
                }
                this.periodsUpToDate = true;
            }
            if (this.summaryChanged) {
                this.summariseData("", true);
            }
            if (this.resultsFileUpToDate && this.resultsLayerExists()) {
                if (this.summaryChanged) {
                    await this.updateResultsFile();
                }
            } else if (await this.createResultsFile()) {
                this.resultsFileUpToDate = true;
            } else {
                return;
            }
            await this.colourResultsFile();
            Cursor.Current = Cursors.Default;
        }

        // Create and add to results 4 shapefiles:
        //         1.  Results for scenario 1
        //         2.  Results for scenario 2
        //         3.  Results for difference scenario 2 - scenario 1
        //         4.  Results for % change from 1 to 2
        //         Assumes self.scenario1 and self.scenario2 are different existing scenarios directories.
        public virtual async Task<bool> makeCompareResults() {
            FeatureLayer layer4 = null;
            FeatureLayer layer3 = null;
            FeatureLayer layer2 = null;
            FeatureLayer layer1 = null;
            void removeComparisonLayers(List<FeatureLayer> layers) {
                QueuedTask.Run(() => {
                    MapView.Active.Map.RemoveLayers(layers);
                });
            }
            // make a directory for the results files
            var dirname = string.Format("{0}_{1}", this.scenario1, this.scenario2);
            var direc = Utils.join(this._gv.tablesOutDir, dirname);
            Directory.CreateDirectory(direc);
            var selectVar = this._dlg.PvariableList.SelectedItems[0].ToString();
            var file1Base = this.scenario1 + "-" + this.table + "-" + selectVar + "-results";
            var file1 = Utils.join(direc, file1Base + ".shp");
            var file1Exists = File.Exists(file1);
            var file2Base = this.scenario2 + "-" + this.table + "-" + selectVar + "-results";
            var file2 = Utils.join(direc, file2Base + ".shp");
            var file2Exists = File.Exists(file2);
            var diffBase = this.table + "-" + selectVar + "-diff";
            var diffFile = Utils.join(direc, diffBase + ".shp");
            var diffFileExists = File.Exists(diffFile);
            var changeBase = this.table + "-" + selectVar + "-%change";
            var changeFile = Utils.join(direc, changeBase + ".shp");
            var changeFileExists = File.Exists(changeFile);
            this.setConnection(this.scenario1);
            // set isStatic False so data for only one variable is read
            if (!this.readData("scenario1", false, this.table, selectVar, "")) {
                return false;
            }
            // assume results shapefiles are the same for scenarios 1 and 2, so use the first scenario for the source shapefiles
            var tablesOutDir = Path.GetDirectoryName(this.db);
            this.setConnection(this.scenario2);
            if (!this.readData("scenario2", false, this.table, selectVar, "")) {
                return false;
            }
            // restore self.db
            this.setConnection(this.scenario);
            var baseName = this.useSubs() ? Parameters._SUBS : this.useHRUs() ? Parameters._HRUS : Parameters._RIVS;
            var ft = this.useSubs() ? FileTypes._SUBBASINS : this.useHRUs() ? FileTypes._HRUS : FileTypes._REACHES;
            var resultsBase = Utils.join(tablesOutDir, baseName) + ".shp";
            var summary = this._dlg.PsummaryCombo.SelectedItem.ToString();
            var needLayer1 = true;
            if (file1Exists) {
                layer1 = (await Utils.getLayerByFilename(file1, ft, null, null, null)).Item1 as FeatureLayer;
                if (layer1 is not null) {
                    needLayer1 = false;
                }
            } else {
                Utils.copyShapefile(resultsBase, file1Base, direc);
            }
            var needLayer2 = true;
            if (file2Exists) {
                layer2 = (await Utils.getLayerByFilename(file2, ft, null, null, null)).Item1 as FeatureLayer;
                if (layer2 is not null) {
                    needLayer2 = false;
                }
            } else {
                Utils.copyShapefile(resultsBase, file2Base, direc);
            }
            var needLayer3 = true;
            if (diffFileExists) {
                layer3 = (await Utils.getLayerByFilename(diffFile, ft, null, null, null)).Item1 as FeatureLayer;
                if (layer3 is not null) {
                    needLayer3 = false;
                }
            } else {
                Utils.copyShapefile(resultsBase, diffBase, direc);
            }
            var needLayer4 = true;
            if (changeFileExists) {
                layer4 = (await Utils.getLayerByFilename(changeFile, ft, null, null, null)).Item1 as FeatureLayer;
                if (layer4 is not null) {
                    needLayer4 = false;
                }
            } else {
                Utils.copyShapefile(resultsBase, changeBase, direc);
            }
            var resultsGroup = Utils.getGroupLayerByName(Utils._RESULTS_GROUP_NAME);
            Debug.Assert(resultsGroup is not null);
            FieldDefn areaField = null;
            if (this.hasAreas) {
                areaField = new OSGeo.OGR.FieldDefn(Visualise._AREA, FieldType.OFTReal);
            }
            var selectVarShort = selectVar.Substring(0, Math.Min(selectVar.Length, 10));
            var varField = new OSGeo.OGR.FieldDefn(selectVarShort, FieldType.OFTReal);
            var addedLayers = new List<FeatureLayer>();
            if (needLayer1) {
                var legend1 = string.Format("{0} {1} {2}", this.scenario1, selectVar, summary);
                using (var layer1Ds = Ogr.Open(file1, 1)) {
                    var glayer1 = layer1Ds.GetLayerByIndex(0);
                    var defn1 = glayer1.GetLayerDefn();
                    if (this.hasAreas && defn1.GetFieldIndex(Visualise._AREA) < 0) {
                        glayer1.CreateField(areaField, 1);
                    }
                    if (defn1.GetFieldIndex(selectVarShort) < 0) {
                        glayer1.CreateField(varField, 1);
                    }
                }
                await QueuedTask.Run(() => {
                    layer1 = LayerFactory.Instance.CreateLayer(new Uri(file1), resultsGroup, 0, legend1) as FeatureLayer;
                });
                addedLayers.Add(layer1);
            }
            if (needLayer2) {
                var legend2 = string.Format("{0} {1} {2}", this.scenario2, selectVar, summary);
                using (var layer2Ds = Ogr.Open(file2, 1)) {
                    var glayer2 = layer2Ds.GetLayerByIndex(0);
                    var defn2 = glayer2.GetLayerDefn();
                    if (this.hasAreas && defn2.GetFieldIndex(Visualise._AREA) < 0) {
                        glayer2.CreateField(areaField, 1);
                    }
                    if (defn2.GetFieldIndex(selectVarShort) < 0) {
                        glayer2.CreateField(varField, 1);
                    }
                }
                await QueuedTask.Run(() => {
                    layer2 = LayerFactory.Instance.CreateLayer(new Uri(file2), resultsGroup, 0, legend2) as FeatureLayer;
                });
                addedLayers.Add(layer2);
            }
            if (needLayer3) {
                // note that string 'difference' is used to find variable name in MapTitle, so change there if you change here
                var legend3 = string.Format("{0} {1} {2} {3} {4}", selectVar, "difference", this.scenario2, "minus", this.scenario1);
                using (var layer3Ds = Ogr.Open(diffFile, 1)) {
                    var glayer3 = layer3Ds.GetLayerByIndex(0);
                    var defn3 = glayer3.GetLayerDefn();
                    if (this.hasAreas && defn3.GetFieldIndex(Visualise._AREA) < 0) {
                        glayer3.CreateField(areaField, 1);
                    }
                    if (defn3.GetFieldIndex(selectVarShort) < 0) {
                        glayer3.CreateField(varField, 1);
                    }
                }
                await QueuedTask.Run(() => {
                    layer3 = LayerFactory.Instance.CreateLayer(new Uri(diffFile), resultsGroup, 0, legend3) as FeatureLayer;
                });
                addedLayers.Add(layer3);
            }
            if (needLayer4) {
                // note that string '%change' is used to find variable name in MapTitle, so change there if you change here
                var legend4 = string.Format("{0} {1} {2} {3} {4}", selectVar, "%change from", this.scenario1, "to", this.scenario2); 
                using (var layer4Ds = Ogr.Open(changeFile, 1)) {
                    var glayer4 = layer4Ds.GetLayerByIndex(0);
                    var defn4 = glayer4.GetLayerDefn();
                    if (this.hasAreas && defn4.GetFieldIndex(Visualise._AREA) < 0) {
                        glayer4.CreateField(areaField, 1);
                    }
                    if (defn4.GetFieldIndex(selectVarShort) < 0) {
                        glayer4.CreateField(varField, 1);
                    }
                }
                await QueuedTask.Run(() => {
                    layer4 = LayerFactory.Instance.CreateLayer(new Uri(changeFile), resultsGroup, 0, legend4) as FeatureLayer;
                });
                addedLayers.Add(layer4);
            }
            bool useLine = false;
            if (this.useSubs()) {
                //TODO
                //layer1.rendererChanged.connect(this.changeSubRenderer);
                //layer2.rendererChanged.connect(this.changeSubRenderer);
                //layer3.rendererChanged.connect(this.changeSubRenderer);
                //layer4.rendererChanged.connect(this.changeSubRenderer);
                this.internalChangeToSubRenderer = true;
                this.keepSubColours = false;
                useLine = false;
            } else if (this.useHRUs()) {
                //layer1.rendererChanged.connect(this.changeHRURenderer);
                //layer2.rendererChanged.connect(this.changeHRURenderer);
                //layer3.rendererChanged.connect(this.changeHRURenderer);
                //layer4.rendererChanged.connect(this.changeHRURenderer);
                this.internalChangeToHRURenderer = true;
                this.keepHRUColours = false;
                useLine = false;
            } else {
                //layer1.rendererChanged.connect(this.changeRivRenderer);
                //layer2.rendererChanged.connect(this.changeRivRenderer);
                //layer3.rendererChanged.connect(this.changeRivRenderer);
                //layer4.rendererChanged.connect(this.changeRivRenderer);
                this.internalChangeToRivRenderer = true;
                this.keepRivColours = false;
                useLine = true;
            }
            List<double> allVals;
            bool invertRamp34;
            (allVals, invertRamp34) = await this.updateCompareLayers(layer1, layer2, layer3, layer4, selectVar, selectVarShort);
            if (allVals is null) {
                removeComparisonLayers(addedLayers);
                return false;
            }
            this.currentResultsLayer = layer4;
            if (this.useSubs()) {
                // add labels
                // TODO
                //if (!this._gv.useGridModel) {
                //    layer1.loadNamedStyle(Utils.join(this._gv.plugin_dir, "subresults.qml"));
                //    layer2.loadNamedStyle(Utils.join(this._gv.plugin_dir, "subresults.qml"));
                //    layer3.loadNamedStyle(Utils.join(this._gv.plugin_dir, "subresults.qml"));
                //    layer4.loadNamedStyle(Utils.join(this._gv.plugin_dir, "subresults.qml"));
                //}
                this.internalChangeToSubRenderer = false;
                // TODO
                //var baseMapTip = FileTypes.mapTip(FileTypes._SUBBASINS);
            } else if (this.useHRUs()) {
                this.internalChangeToHRURenderer = false;
                //baseMapTip = FileTypes.mapTip(FileTypes._HRUS);
            } else {
                this.internalChangeToRivRenderer = false;
                //baseMapTip = FileTypes.mapTip(FileTypes._REACHES);
            }
            //layer1.setMapTipTemplate(baseMapTip + "<br/><b>{0}:</b> [% \"{0}\" %]", selectVarShort));
            //layer2.setMapTipTemplate(baseMapTip + "<br/><b>{0}:</b> [% \"{0}\" %]", selectVarShort));
            //layer3.setMapTipTemplate(baseMapTip + "<br/><b>{0}:</b> [% \"{0}\" %]", selectVarShort));
            //layer4.setMapTipTemplate(baseMapTip + "<br/><b>{0}:</b> [% \"{0}\" %]", selectVarShort));
            CIMColorRamp ramp12;
            ramp12 = await this.chooseColorRamp(this.table, selectVar);
            // need two renderers for two files, else crash when removing layers
            var renderer1 = await this.makeJenksRenderer(allVals, ramp12, selectVar, useLine);
            var renderer2 = CIMClassBreaksRenderer.Clone(renderer1) as CIMClassBreaksRenderer;
            //renderer2 = await self.makeJenksRenderer(allVals, ramp12, selectVar, useLine)
            var ramp34 = await this.makeComparisonRamp();
            await this.colourResultsFile(layer: layer1, renderer: renderer1);
            await this.colourResultsFile(layer: layer2, renderer: renderer2);
            await this.colourResultsFile(layer: layer3, ramp: ramp34);
            await this.colourResultsFile(layer: layer4, ramp: ramp34);
            return true;
        }

        // Return YlwGrnRd colour ramp.
        public async Task<CIMColorRamp> makeComparisonRamp() {
            // this ramp is inverted by SWATRamp
            return await FileTypes.SWATRamp("RdYlGn");
        }

        // Write data to compare layers.  Return list of all values in compared layers, plus flag indicating if colour ramp for diff and %change maps should be inverted.
        public virtual async Task<(List<double>, bool)> updateCompareLayers(
            FeatureLayer layer1,
            FeatureLayer layer2,
            FeatureLayer layer3,
            FeatureLayer layer4,
            string selectVar,
            string selectVarShort) {
            double val2 = 0;
            double val1 = 0;
            double data = 0;
            Dictionary<string, double> subData;
            string @ref;
            int sub;
            // note that all layers are copy of the same template shapefile, so all have the same structure
            if (this.hasAreas) {
                var areaIndex = await this._gv.topo.getIndex(layer1, Visualise._AREA);
            }
            // collect resultsData
            this.summariseData("scenario1", true);
            // summariseData puts data in this.staticResultsData, so save this before running again
            // need to copy data, not just a pointer to it
            var data1 = this.copyStaticResultsData();
            this.summariseData("scenario2", true);
            var allVals = new List<double>();
            var minDiff = double.PositiveInfinity;
            var maxDiff = double.NegativeInfinity;
            foreach (var layer in new HashSet<FeatureLayer> {
                layer1,
                layer2,
                layer3,
                layer4
            }) {
                Debug.Assert(layer is not null);
                var fileName = await Utils.layerFilename(layer);
                using (var ds = Ogr.Open(fileName, 1)) {
                    var glayer = ds.GetLayerByIndex(0);
                    var defn = glayer.GetLayerDefn();
                    var varIndex = defn.GetFieldIndex(selectVarShort);
                    do {
                        var f = glayer.GetNextFeature();
                        if (f is null) { break; }
                        if (this.useHRUs()) {
                            // May be split HRUs; just use first
                            // This is inadequate for some variables, but no way to know of correct val is sum of vals, mean, etc.
                            sub = Convert.ToInt32(f.GetFieldAsString(Topology._HRUGIS).Split(',')[0]);
                        } else {
                            sub = f.GetFieldAsInteger(Topology._SUBBASIN);
                        }
                        if (this.hasAreas) {
                            var areaIndex = defn.GetFieldIndex(Visualise._AREA);
                            var area = 0.0;
                            if (!this.areas.TryGetValue(sub, out area)) {
                                if (this.useHRUs()) {
                                    @ref = string.Format("HRU {0}", sub);
                                } else {
                                    @ref = string.Format("subbasin {0}", sub);
                                }
                                Utils.error(string.Format("Cannot get area for {0}", @ref), this._gv.isBatch);
                                return (null, false);
                            }
                            f.SetField(areaIndex, area);
                        }
                        if (layer == layer1) {
                            bool ok = true;
                            if (!data1.TryGetValue(sub, out subData)) {
                                ok = false;
                            } else if (!subData.TryGetValue(selectVar, out data)) {
                                ok = false;
                            }
                            if (!ok) {
                                if (this.useHRUs()) {
                                    @ref = string.Format("HRU {0}", sub);
                                } else {
                                    @ref = string.Format("subbasin {0}", sub);
                                }
                                Utils.error(string.Format("Cannot get data for variable {0} in {1} in first scenario", selectVar, @ref), this._gv.isBatch);
                                return (null, false);
                            }
                            allVals.Add(data);
                        } else if (layer == layer2) {
                            bool ok = true;
                            if (!this.staticResultsData.TryGetValue(sub, out subData)) {
                                ok = false;
                            } else if (!subData.TryGetValue(selectVar, out data)) {
                                ok = false;
                            }
                            if (!ok) {
                                if (this.useHRUs()) {
                                    @ref = string.Format("HRU {0}", sub);
                                } else {
                                    @ref = string.Format("subbasin {0}", sub);
                                }
                                Utils.error(string.Format("Cannot get data for variable {0} in {1} in second scenario", selectVar, @ref), this._gv.isBatch);
                                return (null, false);
                            }
                            allVals.Add(data);
                        } else {
                            bool ok = true;
                            if (!data1.TryGetValue(sub, out subData)) {
                                ok = false;
                            } else if (!subData.TryGetValue(selectVar, out val1)) {
                                ok = false;
                            }
                            if (!ok) {
                                if (this.useHRUs()) {
                                    @ref = string.Format("HRU {0}", sub);
                                } else {
                                    @ref = string.Format("subbasin {0}", sub);
                                }
                                Utils.error(string.Format("Cannot get data for variable {0} in {1} in first scenario", selectVar, @ref), this._gv.isBatch);
                                return (null, false);
                            }
                            if (!this.staticResultsData.TryGetValue(sub, out subData)) {
                                ok = false;
                            } else if (!subData.TryGetValue(selectVar, out val2)) {
                                ok = false;
                            }
                            if (!ok) {
                                if (this.useHRUs()) {
                                    @ref = string.Format("HRU {0}", sub);
                                } else {
                                    @ref = string.Format("subbasin {0}", sub);
                                }
                                Utils.error(string.Format("Cannot get data for variable {0} in {1} in second scenario", selectVar, @ref), this._gv.isBatch);
                                return (null, false);
                            }
                            if (layer == layer3) {
                                data = val2 - val1;
                                minDiff = Math.Min(minDiff, data);
                                maxDiff = Math.Max(maxDiff, data);
                            } else {
                                // layer 4
                                data = val1 == 0 ? 0 : (double)(val2 - val1) / val1 * 100;
                            }
                        }
                        f.SetField(varIndex, data);
                        glayer.SetFeature(f);

                    } while (true);
                }
            }
            this.summaryChanged = false;
            return (allVals, Math.Abs(minDiff) > maxDiff);
        }

        public Dictionary<int, Dictionary<string, double>> copyStaticResultsData() {
            //Return deep copy of this.staticResultsData.
            var data = new Dictionary<int, Dictionary<string, double>>();
            foreach (var (sub, varData) in this.staticResultsData) {
                var data2 = new Dictionary<string, double>();
                foreach (var (vari, val) in varData) {
                    data2[vari] = val;
                }
                data[sub] = data2;
            }
            return data;
        }

        // Create layout by opening template file.
        public async Task printResults() {
            string templ;
            // choose template file 
            var count = this._dlg.PprintCount.Value;
            if (count == 1) {
                if (this._dlg.PlandscapeButton.Checked) {
                    templ = "1Landscape.pagx";
                } else {
                    templ = "1Portrait.pagx";
                }
            } else if (count == 2) {
                if (this._dlg.PlandscapeButton.Checked) {
                    templ = "2Landscape.pagx";
                } else {
                    templ = "2Portrait.pagx";
                }
            } else if (count == 3) {
                if (this._dlg.PlandscapeButton.Checked) {
                    templ = "3Landscape.pagx";
                } else {
                    templ = "3Portrait.pagx";
                }
            } else if (count == 4) {
                if (this._dlg.PlandscapeButton.Checked) {
                    templ = "4Landscape.pagx";
                } else {
                    templ = "4Portrait.pagx";
                }
            } else if (count == 6) {
                if (this._dlg.PlandscapeButton.Checked) {
                    templ = "6Landscape.pagx";
                } else {
                    templ = "6Portrait.pagx";
                }
            } else {
                Utils.error(string.Format("There are layout templates only for 1, 2, 3, 4 or 6 result maps, not for {0}", count), this._gv.isBatch);
                return;
            }
            var templateIn = Utils.join(this._gv.addinPath, "LayoutTemplates/LayoutTemplate" + templ); 
            await QueuedTask.Run(() =>
            {
                IProjectItem pagx = ItemFactory.Instance.Create(templateIn) as IProjectItem;
                Project.Current.AddItem(pagx);
            });
            //var templateOut = Utils.join(this._gv.tablesOutDir, this.title + templ);
            //Utils.loginfo(String.Format("Print composer template {0} written", templateOut));
        }

        // Use table of replacements to replace keys with items in returned line.
        public static string replaceInLine(string inLine, Dictionary<string, string> table) {
            foreach (var (patt, sub) in table) {
                inLine = inLine.Replace(patt, sub);
            }
            return inLine;
        }

        // Reveal or hide compose options group.
        public virtual void changeAnimationMode() {
            if (this._dlg.PprintAnimation.Checked) {
                this._dlg.PcomposeOptions.Visible = true;
                this._dlg.PcomposeCount.Value = Utils.countLayersInGroup(Utils._ANIMATION_GROUP_NAME);
            } else {
                this._dlg.PcomposeOptions.Visible = false;
            }
        }

        // 
        //         Set up for animation.
        //         
        //         Collect animation data from database table according to animation variable; 
        //         set slider minimum and maximum;
        //         create animation layer;
        //         set speed accoring to spin box;
        //         set slider at minimum and display data for start time.
        //         
        public virtual async Task setupAnimateLayer() {
            if (this._dlg.PanimationVariableCombo.SelectedItem.ToString() == "") {
                return;
            }
            // can take a while so set a wait cursor
            Cursor.Current = Cursors.WaitCursor;
            this.doRewind();
            //this._dlg.calculateLabel.Text = "Calculating breaks ...");
            //this._dlg.repaint();
            try {
                if (!this.setPeriods()) {
                    return;
                }
                this.animateVar = this._dlg.PanimationVariableCombo.SelectedItem.ToString();
                if (!await this.createAnimationLayer()) {
                    return;
                }
                Debug.Assert(this.animateLayer is not null);
                var lid = this.animateLayer.Name;
                if (!this.readData(lid, false, this.table, this.animateVar, "")) {
                    return;
                }
                int animateLength = 0;
                this.summariseData(lid, false);
                if (this.isDaily || this.table == "wql") {
                    animateLength = this.periodDays;
                } else if (this.isAnnual) {
                    animateLength = (int)Math.Round(this.periodYears);
                } else {
                    animateLength = (int)Math.Round(this.periodMonths);
                }
                this._dlg.Pslider.Minimum = 1;
                this._dlg.Pslider.Maximum = animateLength;
                bool useLine = this.table != "sub" && this.table != "hru";
                await this.colourAnimationLayer(useLine);
                this._dlg.Pslider.Value = 1;
                var sleep = this._dlg.PspinBox.Value;
                this.changeSpeed(sleep);
                this.resetSlider();
                await this.changeAnimate();
            } finally {
                //this._dlg.calculateLabel.Text = "");
                Cursor.Current = Cursors.Default;
            }
        }

        //TODO
        //// Save animated GIF if still files found.
        //public virtual object saveVideo() {
        //    // capture final frame
        //    this.capture();
        //    // remove animation layout
        //    try {
        //        Debug.Assert(this.animationLayout is not null);
        //        QgsProject.instance().layoutManager().removeLayout(this.animationLayout);
        //    } catch {
        //    }
        //    var fileNames = (from fn in os.listdir(this._gv.pngDir)
        //                     where fn.endswith(".png")
        //                     select fn).OrderBy(_p_1 => _p_1).ToList();
        //    if (fileNames == new List<object>()) {
        //        return;
        //    }
        //    if (this._dlg.printAnimation.Checked) {
        //        var @base = Utils.join(this._gv.tablesOutDir, "Video.gif");
        //        this.videoFile = Utils.nextFileName(@base, 0)[0];
        //    } else {
        //        var tablesOutDir = os.path.split(this.db)[0];
        //        this.videoFile = Utils.join(tablesOutDir, this.animateVar + "Video.gif");
        //    }
        //    try {
        //        os.remove(this.videoFile);
        //    } catch (Exception) {
        //    }
        //    var period = 1.0 / this._dlg.spinBox.Value;
        //    try {
        //        using (var writer = imageio.get_writer("file://" + this.videoFile, mode: "I", loop: 1, duration: period)) {
        //            // type: ignore
        //            foreach (var filename in fileNames) {
        //                image = imageio.imread(Utils.join(this._gv.pngDir, filename));
        //                writer.append_data(image);
        //            }
        //        }
        //        // clear the png files:
        //        this.clearPngDir();
        //        Utils.information("Animated gif {0} written", this.videoFile), this._gv.isBatch);
        //    } catch (Exception ex) {
        //        Utils.error(string.Format(@"
        //    Failed to generate animated gif: {0}.
        //    The .png files are in {1}: suggest you try using GIMP.
        //    ", ex.Message, this._gv.pngDir), this._gv.isBatch);
        //    }
        //}

        // Set animating and not pause.
        public virtual void doPlay() {
            if (this._dlg.PanimationVariableCombo.SelectedItem.ToString() == "") {
                Utils.information("Please choose an animation variable", this._gv.isBatch);
                return;
            }
            this.animating = true;
            this.animationPaused = false;
        }

        // If animating change pause from on to off, or off to on.
        public virtual void doPause() {
            if (this.animating) {
                this.animationPaused = !this.animationPaused;
            }
        }

        // Turn off animating and pause and set slider to minimum.
        public virtual void doRewind() {
            this.animating = false;
            this.animationPaused = false;
            this.resetSlider();
        }

        // Move slide one step to right unless at maximum.
        public virtual void doStep() {
            if (this.animating && !this.animationPaused) {
                var val = this._dlg.Pslider.Value;
                if (val < this._dlg.Pslider.Maximum) {
                    this._dlg.Pslider.Value = val + 1;
                }
            }
        }

        // Stop any running animation and if possible move the animation slider one step left.
        public virtual void animateStepLeft() {
            if (this._dlg.PtabWidget.SelectedIndex == 1) {
                this.animating = false;
                this.animationPaused = false;
                var val = this._dlg.Pslider.Value;
                if (val > this._dlg.Pslider.Minimum) {
                    this._dlg.Pslider.Value = val - 1;
                }
            }
        }

        // Stop any running animation and if possible move the animation slider one step right.
        public virtual void animateStepRight() {
            if (this._dlg.PtabWidget.SelectedIndex == 1) {
                this.animating = false;
                this.animationPaused = false;
                var val = this._dlg.Pslider.Value;
                if (val < this._dlg.Pslider.Maximum) {
                    this._dlg.Pslider.Value = val + 1;
                }
            }
        }

        // 
        //         Starts or restarts the timer with speed set to val.
        //         
        //         Runs in a try ... except so that timer gets stopped if any exception.
        //         
        public virtual void changeSpeed(decimal val) {
            try {
                this.animateTimer.Interval = (int)(1000 / val);
                this.animateTimer.Start();
            } catch (Exception) {
                this.animating = false;
                this.animateTimer.Stop();
                // raise last exception again
                throw;
            }
        }

        // Turn off animating and pause.
        public virtual void pressSlider() {
            this.animating = false;
            this.animationPaused = false;
        }

        // Move slide to minimum.
        public virtual void resetSlider() {
            this._dlg.Pslider.Value = this._dlg.Pslider.Minimum;
        }

        // Convert slider value to date.
        public virtual int sliderValToDate() {
            if (this.isDaily || this.table == "wql") {
                return this.addDays(this.julianStartDay + this._dlg.Pslider.Value - 1, this.startYear);
            } else if (this.isAnnual) {
                return this.startYear + this._dlg.Pslider.Value - 1;
            } else {
                var totalMonths = this.startMonth + this._dlg.Pslider.Value - 2;
                var year = (double)totalMonths / 12;
                var month = totalMonths % 12 + 1;
                return (int)(this.startYear + year) * 100 + month;
            }
        }

        // Run the compare scenarios form.
        public virtual void startCompareScenarios() {
            this._comparedlg.ShowDialog();
        }

        // Save chosen scenarios and exit form.
        public virtual void setupCompareScenarios() {
            string cioFile;
            string txtInOutDir;
            string tablesOutDir;
            string scenDir;
            var scenario1 = this._comparedlg.Pscenario1.SelectedItem.ToString();
            var scenario2 = this._comparedlg.Pscenario2.SelectedItem.ToString();
            if (scenario1 == "" || scenario2 == "" || scenario1 == scenario2) {
                Utils.information("Please choose two different scenarios to compare", this._gv.isBatch);
                this.closeCompareScenarios();
                return;
            }
            this._dlg.PcompareLabel.Text = string.Format("Compare {0} and {1}", scenario1, scenario2);
            this.scenario1 = scenario1;
            this.scenario2 = scenario2;
            // constrict dates if necessary
            if (this.scenario1 != this.scenario) {
                scenDir = Utils.join(this._gv.scenariosDir, this.scenario1);
                if (this.table != "") {
                    tablesOutDir = Utils.join(scenDir, Parameters._TABLESOUT);
                    if (!this._gv.db.tableExists(this.table, Utils.join(tablesOutDir, Parameters._OUTPUTDB))) {
                        Utils.error(string.Format("There is no {0} output table for scenario {1}", this.table, this.scenario1), this._gv.isBatch);
                        this.closeCompareScenarios();
                        return;
                    }
                }
                txtInOutDir = Utils.join(scenDir, Parameters._TXTINOUT);
                cioFile = Utils.join(txtInOutDir, Parameters._CIO);
                if (!File.Exists(cioFile)) {
                    Utils.error(string.Format("Cannot find cio file {0}", cioFile), this._gv.isBatch);
                    this.closeCompareScenarios();
                    return;
                }
                this.readCio(cioFile, this.scenario1);
            }
            if (this.scenario2 != this.scenario) {
                scenDir = Utils.join(this._gv.scenariosDir, this.scenario2);
                if (this.table != "") {
                    tablesOutDir = Utils.join(scenDir, Parameters._TABLESOUT);
                    if (!this._gv.db.tableExists(this.table, Utils.join(tablesOutDir, Parameters._OUTPUTDB))) {
                        Utils.error(string.Format("There is no {0} output table for scenario {1}", this.table, this.scenario2), this._gv.isBatch);
                        this.closeCompareScenarios();
                        return;
                    }
                }
                txtInOutDir = Utils.join(scenDir, Parameters._TXTINOUT);
                cioFile = Utils.join(txtInOutDir, Parameters._CIO);
                if (!File.Exists(cioFile)) {
                    Utils.error(string.Format("Cannot find cio file {0}", cioFile), this._gv.isBatch);
                    this.closeCompareScenarios();
                    return;
                }
                this.readCio(cioFile, this.scenario1);
            }
            this._comparedlg.Close();
        }

        public virtual void closeCompareScenarios() {
            this.scenario1 = "";
            this.scenario2 = "";
            this._dlg.PcompareLabel.Text = "Compare X and Y";
            this._comparedlg.Close();
        }

        // Make Julian date from year + days.
        public virtual int addDays(int days, int year) {
            var leapAdjust = isLeap(year) ? 1 : 0;
            var lenYear = 365 + leapAdjust;
            if (days <= lenYear) {
                return year * 1000 + days;
            } else {
                return this.addDays(days - lenYear, year + 1);
            }
        }

        // 
        //         Return datetime.date from year and number of days.
        //         
        //         The day may exceed the length of year, in which case a later year
        //         will be returned.
        //         
        public virtual DateOnly julianToDate(int day, int year) {
            if (day <= 31) {
                return new DateOnly(year, 1, day);
            }
            day -= 31;
            var leapAdjust = Visualise.isLeap(year) ? 1 : 0;
            if (day <= 28 + leapAdjust) {
                return new DateOnly(year, 2, day);
            }
            day -= 28 + leapAdjust;
            if (day <= 31) {
                return new DateOnly(year, 3, day);
            }
            day -= 31;
            if (day <= 30) {
                return new DateOnly(year, 4, day);
            }
            day -= 30;
            if (day <= 31) {
                return new DateOnly(year, 5, day);
            }
            day -= 31;
            if (day <= 30) {
                return new DateOnly(year, 6, day);
            }
            day -= 30;
            if (day <= 31) {
                return new DateOnly(year, 7, day);
            }
            day -= 31;
            if (day <= 31) {
                return new DateOnly(year, 8, day);
            }
            day -= 31;
            if (day <= 30) {
                return new DateOnly(year, 9, day);
            }
            day -= 30;
            if (day <= 31) {
                return new DateOnly(year, 10, day);
            }
            day -= 31;
            if (day <= 30) {
                return new DateOnly(year, 11, day);
            }
            day -= 30;
            if (day <= 31) {
                return new DateOnly(year, 12, day);
            } else {
                return this.julianToDate(day - 31, year + 1);
            }
        }

        // Convert integer date to string.
        public virtual string dateToString(int dat) {
            if (this.isDaily || this.table == "wql") {
                return this.julianToDate(dat % 1000, dat / 1000).ToLongDateString();
            }
            if (this.isAnnual) {
                return dat.ToString();
            }
            int year = dat / 100;
            string month = Visualise._MONTHS[dat % 100 - 1];
            return month + " " + year.ToString();
        }

        // Switch between recording and not.
        public virtual void record() {
            this.capturing = !this.capturing;
            if (this.capturing) {
                // clear any existing png files (can be left eg if making gif failed)
                this.clearPngDir();
                if (this._dlg.PprintAnimation.Checked) {
                    // TODO
                    //this.createAnimationComposition();
                }
                this._dlg.PrecordButton.BackColor = System.Drawing.Color.Red;
                this._dlg.PrecordLabel.Text = "Stop recording";
                this._dlg.PplayButton.Enabled = false;
            } else {
                Cursor.Current = Cursors.WaitCursor;
                this._dlg.PrecordButton.BackColor = System.Drawing.Color.Green;
                this._dlg.PrecordLabel.Text = "Start recording";
                //TODO
                //this.saveVideo();
                this._dlg.PplayButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        // Use default application to play video file (an animated gif).
        public virtual void playRecording() {
            // stop recording if necessary
            if (this.capturing) {
                this.record();
            }
            if (!File.Exists(this.videoFile)) {
                Utils.information(string.Format("No video file for {0} exists at present", this.animateVar), this._gv.isBatch);
                return;
            }
            //TODO
            //os.startfile(this.videoFile);
        }

        // Flag change to summary method.
        public virtual void changeSummary() {
            this.summaryChanged = true;
        }

        // If user changes the stream renderer, flag to retain colour scheme.
        public virtual void changeRivRenderer() {
            if (!this.internalChangeToRivRenderer) {
                this.keepRivColours = true;
            }
        }

        // If user changes the subbasin renderer, flag to retain colour scheme.
        public virtual void changeSubRenderer() {
            if (!this.internalChangeToSubRenderer) {
                this.keepSubColours = true;
            }
        }

        // If user changes the subbasin renderer, flag to retain colour scheme.
        public virtual void changeHRURenderer() {
            if (!this.internalChangeToHRURenderer) {
                this.keepHRUColours = true;
            }
        }

        // 
        //         Update current plot row according to the colChanged index.
        //         
        //         If there are no rows, first makes one.
        //         
        public virtual void updateCurrentPlotRow(int colChanged) {
            if (!this.plotting()) {
                return;
            }
            var row = this._dlg.PtableWidget.CurrentRow;
            if (row is null) {
                this.doAddPlot();
                row = this._dlg.PtableWidget.CurrentRow;
            }
            var dataRow = this.plotData.Rows[row.Index];
            if (colChanged == 0) {
                dataRow[0] = this.scenario;
            } else if (colChanged == 1) {
                if ((string)dataRow[1] == "-") {
                    // observed plot: do not change
                    return;
                }
                dataRow[1] = this.table;
                if (this.table == "hru") {
                    dataRow[3] = "";
                } else {
                    dataRow[3] = "-";
                }
                dataRow[4] = "";
            } else if (colChanged == 2) {
                dataRow[2] = this._dlg.PsubPlot.Text;
                if ((string)dataRow[1] == "hru") {
                    dataRow[3] = "";
                } else {
                    dataRow[3] = "-";
                }
            } else if (colChanged == 3) {
                dataRow[3] = this._dlg.PhruPlot.Text;
            } else {
                dataRow[4] = this._dlg.PvariablePlot.Text;
            }
        }

        // used when current tableWidget cell changes.
        // If the cell is a variable cell (column index 4), set variables for observed 
        // if current cell row is observed, else according to current table
        public void setVariablesForRow() {
            try {
                if (this._dlg.PtableWidget.CurrentCell.ColumnIndex != 4) { return; }
                var row = this._dlg.PtableWidget.CurrentCell.OwningRow;
                var rowTable = (string)row.Cells[1].Value;
                if (rowTable == "-") {  // observed row
                    this.setObservedVars();
                } else {
                    this.setPlotVars(rowTable);
                }
            }
            catch {
                // allow to fail quietly
                return;
            }
        }

        // Add a plot row and make it current.
        public virtual void doAddPlot() {
            var sub = this._dlg.PsubPlot.SelectedIndex < 0 ? "" : this._dlg.PsubPlot.SelectedItem.ToString();
            var hru = this._dlg.PhruPlot.SelectedIndex < 0 ? (this.table == "hru" ? "" : "-") : this._dlg.PhruPlot.SelectedItem.ToString();
            var size = this._dlg.PtableWidget.Rows.Count;
            if (size > 1 && (string)this.plotData.Rows[size - 2][1] == "-") {
                // last plot was observed: need to reset variables
                this.table = "";
                this.setVariables();
            }
            var vari = this._dlg.PvariablePlot.SelectedIndex < 0 ? "" : this._dlg.PvariablePlot.SelectedItem.ToString();
            var row = this.plotData.NewRow();
            row[0] = this.scenario;
            row[1] = this.table;
            row[2] = sub;
            row[3] = hru;
            row[4] = vari;
            this.plotData.Rows.InsertAt(row, size - 1);
            //foreach (var col in Enumerable.Range(0, 5)) {
            //    this._dlg.tableWidget.item(size, col).setTextAlignment(Qt.AlignCenter);
            //}
            this._dlg.PtableWidget.CurrentCell = this._dlg.PtableWidget.Rows[size - 1].Cells[0];
        }

        // Delete current plot row.
        public virtual void doDelPlot() {
            var row = this._dlg.PtableWidget.CurrentRow;
            if (row is null) {
                Utils.information("Please select a row for deletion", this._gv.isBatch);
                return;
            }
            // only remove current row
            var dataRow = this.plotData.Rows[row.Index];
            this.plotData.Rows.Remove(dataRow);
        }

        // Add a copy of the current plot row and make it current.
        public virtual void doCopyPlot() {
            var row = this._dlg.PtableWidget.CurrentRow;
            if (row is null) {
                Utils.information("Please select a row to copy", this._gv.isBatch);
                return;
            }
            var size = this.plotData.Rows.Count;
            if (Enumerable.Range(0, size).Contains(row.Index)) {
                var plotRow = this.plotData.Rows[row.Index];
                var copyRow = this.plotData.NewRow();
                foreach (var col in Enumerable.Range(0, 5)) {
                    copyRow[col] = plotRow[col];
                }
                this.plotData.Rows.InsertAt(copyRow, size);
            }
            this._dlg.PtableWidget.CurrentCell = this._dlg.PtableWidget.Rows[size].Cells[0];

        }

        // Move current plot row up 1 place and keep it current.
        public virtual void doUpPlot() {
            var row = this._dlg.PtableWidget.CurrentRow;
            if (row is null) {
                Utils.information("Please select a row to move up", this._gv.isBatch);
                return;
            }
            var index = row.Index;
            if (1 <= index && index < this.plotData.Rows.Count) {
                var upper = this.plotData.Rows[index - 1];
                var lower = this.plotData.Rows[index];
                foreach (var col in Enumerable.Range(0, 5)) {
                    var item = upper[col];
                    upper[col] = lower[col];
                    lower[col] = item; 
                }
            }
            this._dlg.PtableWidget.CurrentCell = this._dlg.PtableWidget.Rows[index - 1].Cells[0];
        }

        // Move current plot row down 1 place and keep it current.
        public virtual void doDownPlot() {
            var row = this._dlg.PtableWidget.CurrentRow;
            if (row is null) {
                Utils.information("Please select a row to move up", this._gv.isBatch);
                return;
            }
            var index = row.Index;
            if (0 <= index && index < this.plotData.Rows.Count - 1) {
                var upper = this.plotData.Rows[index];
                var lower = this.plotData.Rows[index + 1];
                foreach (var col in Enumerable.Range(0, 5)) {
                    var item = upper[col];
                    upper[col] = lower[col];
                    lower[col] = item;
                }
            }
            this._dlg.PtableWidget.CurrentCell = this._dlg.PtableWidget.Rows[index + 1].Cells[0];
        }

        // set plot variables according to table parameter
        public void setPlotVars(string table) {
            this._dlg.PvariablePlot.Items.Clear();
            //this._dlg.PvariablePlot.Items.Add("");
            this._dlg.PvariablePlot.Text = "";
            if (!string.IsNullOrEmpty(table)) {
                var adp = new OleDbDataAdapter();
                var cmd = new OleDbCommand(string.Format("SELECT * FROM {0} WHERE 1=2;", table), this.conn as OleDbConnection);
                adp.SelectCommand = cmd;
                var dset = new DataSet();
                adp.Fill(dset, table);
                var cols = dset.Tables[0].Columns;
                for (int i = 0; i < cols.Count; i++) {
                    var name = cols[i].ColumnName;
                    if (!this.ignoredVars.Contains(name)) {
                        this._dlg.PvariablePlot.Items.Add(name);
                    }
                }
            }
        }

        // Add a row for an observed plot, and make it current.
        public virtual void addObservedPlot() {
            if (!File.Exists(this.observedFileName)) {
                return;
            }
            this.setObservedVars();
            var row = this.plotData.NewRow();
            row[0] = "observed";
            row[1] = "-";
            row[2] = "-";
            row[3] = "-";
            row[4] = this._dlg.PvariablePlot.SelectedIndex < 0 ? 
                (this._dlg.PvariablePlot.Items.Count == 1 ? this._dlg.PvariablePlot.Items[0].ToString() : "") :
                this._dlg.PvariablePlot.SelectedItem.ToString();
            this.plotData.Rows.Add(row);
            this._dlg.PtableWidget.CurrentCell = this._dlg.PtableWidget.Rows[this._dlg.PtableWidget.Rows.Count - 2].Cells[0];
        }

        // Add variables from 1st line of observed data file, ignoring 'date' if it occurs as the first column.
        public virtual void setObservedVars() {
            string line;
            using (var obs = new StreamReader(this.observedFileName)) {
                line = obs.ReadLine();
            }
            var varz = line.Split(',');
            int num = varz.Count();
            if (num == 0) {
                Utils.error(string.Format("Cannot find variables in first line of observed data file {0}", this.observedFileName), this._gv.isBatch);
                return;
            }
            var col1 = varz[0].Trim().ToLower();
            var start = col1 == "date" ? 1 : 0;
            this._dlg.PvariablePlot.Items.Clear();
            foreach (var i in Enumerable.Range(start, num - start)) {
                // need to strip since last variable in csv header comes with newline
                this._dlg.PvariablePlot.Items.Add(varz[i].Trim());
            }
        }

        // 
        //         Read data for var from observed data file, returning a list of data as strings.
        //         
        //         Note that dates are not checked even if present in the observed data file.
        //         
        public virtual List<string> readObservedFile(string vari) {
            var result = new List<string>();
            using (var obs = new StreamReader(this.observedFileName)) {
                var line = obs.ReadLine();
                var varz = (from var1 in line.Split(',')
                        select var1.Trim()).ToList();
                int numVarz = varz.Count;
                if (numVarz == 0) {
                    Utils.error(string.Format("Cannot find variables in first line of observed data file {0}", this.observedFileName), this._gv.isBatch);
                    return result;
                }
                var idx = varz.FindIndex(x => x == vari);
                if (idx < 0) {
                    Utils.error(string.Format("Cannot find variable {0} in first line of observed data file {1}", vari, this.observedFileName), this._gv.isBatch);
                    return result;
                }
                while (true) {
                    line = obs.ReadLine();
                    if (line is null) {
                        break;
                    }
                    var vals = line.Split(',');
                    if (0 <= idx && idx < vals.Count()) {
                        result.Add(vals[idx].Trim());
                    } else {
                        break;
                    }
                }
            }
            return result;
        }

        // code from http://danieljlewis.org/files/2010/06/Jenks.pdf
        // described at http://danieljlewis.org/2010/06/07/jenks-natural-breaks-algorithm-in-python/
        // amended following style of http://www.macwright.org/simple-statistics/docs/simple_statistics.html#section-116
        // no longer used - replaced by Cython
        //===========================================================================
        // @staticmethod
        // def getJenksBreaks( dataList, numClass ):
        //     """Return Jenks breaks for dataList with numClass classes."""
        //     if not dataList:
        //         return [], 0
        //     # Use of sample unfortunate because gives poor animation results.
        //     # Tends to overestimate lower limit and underestimate upper limit, and areas go white in animation.
        //     # But can take a long time to calculate!
        //     # QGIS internal code uses 1000 here but 4000 runs in reasonable time
        //     maxSize = 4000
        //     # use a sample if size exceeds maxSize
        //     size = len(dataList)
        //     if size > maxSize:
        //         origSize = size
        //         size = max(maxSize, size / 10)
        //         Utils.loginfo('Jenks breaks: using a sample of size {0} from {1}', size, origSize))
        //         sample = random.sample(dataList, size)
        //     else:
        //         sample = dataList
        //     sample.sort()
        //     # at most one class: return singleton list
        //     if numClass <= 1:
        //         return [sample.last()]
        //     if numClass >= size:
        //         # nothing useful to do
        //         return sample
        //     lowerClassLimits = []
        //     varianceCombinations = []
        //     variance = 0
        //     for i in range(0,size+1):
        //         temp1 = []
        //         temp2 = []
        //         # initialize with lists of zeroes
        //         for j in range(0,numClass+1):
        //             temp1.Add(0)
        //             temp2.Add(0)
        //         lowerClassLimits.Add(temp1)
        //         varianceCombinations.Add(temp2)
        //     for i in range(1,numClass+1):
        //         lowerClassLimits[1][i] = 1
        //         varianceCombinations[1][i] = 0
        //         for j in range(2,size+1):
        //             varianceCombinations[j][i] = float('inf')
        //     for l in range(2,size+1):
        //         # sum of values seen so far
        //         summ = 0
        //         # sum of squares of values seen so far
        //         sumSquares = 0
        //         # for each potential number of classes. w is the number of data points considered so far
        //         w = 0
        //         i4 = 0
        //         for m in range(1,l+1):
        //             lowerClassLimit = l - m + 1
        //             val = float(sample[lowerClassLimit-1])
        //             w += 1
        //             summ += val
        //             sumSquares += val * val
        //             variance = sumSquares - (summ * summ) / w
        //             i4 = lowerClassLimit - 1
        //             if i4 != 0:
        //                 for j in range(2,numClass+1):
        //                     # if adding this element to an existing class will increase its variance beyond the limit, 
        //                     # break the class at this point, setting the lower_class_limit.
        //                     if varianceCombinations[l][j] >= (variance + varianceCombinations[i4][j - 1]):
        //                         lowerClassLimits[l][j] = lowerClassLimit
        //                         varianceCombinations[l][j] = variance + varianceCombinations[i4][j - 1]
        //         lowerClassLimits[l][1] = 1
        //         varianceCombinations[l][1] = variance
        //     k = size
        //     kclass = []
        //     for i in range(0,numClass+1):
        //         kclass.Add(0)
        //     kclass[numClass] = float(sample[size - 1])
        //     countNum = numClass
        //     while countNum >= 2:#print "rank = " + str(lowerClassLimits[k][countNum])
        //         idx = int((lowerClassLimits[k][countNum]) - 2)
        //         #print "val = " + str(sample[idx])
        //         kclass[countNum - 1] = sample[idx]
        //         k = int((lowerClassLimits[k][countNum] - 1))
        //         countNum -= 1
        //     return kclass, sample[0]
        //===========================================================================
        // copied like above but not used
        //===============================================================================
        //     @staticmethod
        //     def getGVF( sample, numClass ):
        //         """
        //         The Goodness of Variance Fit (GVF) is found by taking the
        //         difference between the squared deviations
        //         from the array mean (SDAM) and the squared deviations from the
        //         class means (SDCM), and dividing by the SDAM
        //         """
        //         breaks = Visualise.getJenksBreaks(sample, numClass)
        //         sample.sort()
        //         size = len(sample)
        //         listMean = sum(sample)/size
        //         print listMean
        //         SDAM = 0.0
        //         for i in range(0,size):
        //             sqDev = (sample[i] - listMean)**2
        //             SDAM += sqDev
        //         SDCM = 0.0
        //         for i in range(0,numClass):
        //             if breaks[i] == 0:
        //                 classStart = 0
        //             else:
        //                 classStart = sample.index(breaks[i])
        //             classStart += 1
        //             classEnd = sample.index(breaks[i+1])
        //             classList = sample[classStart:classEnd+1]
        //         classMean = sum(classList)/len(classList)
        //         print classMean
        //         preSDCM = 0.0
        //         for j in range(0,len(classList)):
        //             sqDev2 = (classList[j] - classMean)**2
        //             preSDCM += sqDev2
        //             SDCM += preSDCM
        //         return (SDAM - SDCM)/SDAM
        // 
        //     # written by Drew
        //     # used after running getJenksBreaks()
        //     @staticmethod
        //     def classify(value, breaks):
        //         """
        //         Return index of value in breaks.
        //         
        //         Returns i such that
        //         breaks = [] and i = -1, or
        //         value < breaks[1] and i = 1, or 
        //         breaks[i-1] <= value < break[i], or
        //         value >= breaks[len(breaks) - 1] and i = len(breaks) - 1
        //         """
        //         for i in range(1, len(breaks)):
        //             if value < breaks[i]:
        //                 return i
        //         return len(breaks) - 1 
        //===============================================================================

        //TODO
        //// Can often end up with more than one map title.  Remove all of them from the canvas, prior to resetting one required.
        //public virtual void clearMapTitle() {
        //    var canvas = this._iface.mapCanvas();
        //    var scene = canvas.scene();
        //    if (this.mapTitle is not null) {
        //        scene.removeItem(this.mapTitle);
        //        this.mapTitle = null;
        //        // scene.items() causes a crash for some users.
        //        // this code seems unnecessary in any case
        //        // for item in scene.items():
        //        // # testing by isinstance is insufficient as a MapTitle item can have a wrappertype
        //        // # and the test returns false
        //        // #if isinstance(item, MapTitle):
        //        // try:
        //        // isMapTitle = item.identifyMapTitle() == 'MapTitle'
        //        // except Exception:
        //        // isMapTitle = False
        //        // if isMapTitle:
        //        // scene.removeItem(item)
        //    }
        //    canvas.refresh();
        //}

        public async void changeAnimation(object sender, PropertyChangedEventArgs e) {
            await this.setAnimateLayer();
        }
        
        // Set self.animateLayer to first visible layer in Animations group, retitle as appropriate.
        public async Task setAnimateLayer() {
            var animationLayers = Utils.getLayersInGroup(Utils._ANIMATION_GROUP_NAME, visible: true);
            if (animationLayers.Count == 0) {
                this.animateLayer = null;
                await this.setResultsLayer();
                return;
            }
            // expand the animation layer and hide the results group maps
            var animationGroup = Utils.getGroupLayerByName(Utils._ANIMATION_GROUP_NAME);
            if (!animationGroup.IsExpanded) {
                await QueuedTask.Run(() => { animationGroup.SetExpanded(true); });
            }
            var resultsGroup = Utils.getGroupLayerByName(Utils._RESULTS_GROUP_NAME);
            if (resultsGroup.IsVisible) {
                await QueuedTask.Run(() => { resultsGroup.SetVisibility(false); });
            }
            // TODO
            this.animateLayer = animationLayers[0] as FeatureLayer;
            //foreach (var mapLayer in animationLayers) {
            //    Debug.Assert(mapLayer is not null);
            //    if (this.mapTitle is null) {
            //        this.mapTitle = new MapTitle(canvas, this.title, mapLayer);
            //        canvas.refresh();
            //        this.animateLayer = cast(QgsVectorLayer, mapLayer);
            //        return;
            //    } else if (mapLayer == this.mapTitle.layer) {
            //        // nothing to do
            //        return;
            //    } else {
            //        // first visible animation layer not current titleLayer
            //        this.clearMapTitle();
            //        var dat = this.sliderValToDate();
            //        var date = this.dateToString(dat);
            //        this.mapTitle = new MapTitle(canvas, this.title, mapLayer, line2: date);
            //        canvas.refresh();
            //        this.animateLayer = cast(QgsVectorLayer, mapLayer);
            //        return;
            //    }
            //}
            //// if we get here, no visible animation layers
            //this.clearMapTitle();
            //this.animateLayer = null;
            return;
        }

        public async void setResults(object sender, PropertyChangedEventArgs e) {
            await this.setResultsLayer();
        }
        
        // Set self.currentResultsLayer to first visible layer in Results group, retitle as appropriate.
        public async Task setResultsLayer() {
            // only change results layer and title if there are no visible animate layers
            var animationLayers = Utils.getLayersInGroup(Utils._ANIMATION_GROUP_NAME, visible: true);
            if (animationLayers.Count > 0) {
                return;
            }
            // make sure results group is visible, since running animations may have made it invisible
            var resultsGroup = Utils.getGroupLayerByName(Utils._RESULTS_GROUP_NAME);
            if (!resultsGroup.IsVisible) {
                await QueuedTask.Run(() => { resultsGroup.SetVisibility(true); });
            }
            //TODO
            //this.clearMapTitle();
            var resultsLayers = Utils.getLayersInGroup(Utils._RESULTS_GROUP_NAME, visible: true);
            if (resultsLayers.Count == 0) {
                this.currentResultsLayer = null;
                return;
            } else {
                //TODO
                this.currentResultsLayer = resultsLayers[0] as FeatureLayer;
                //foreach (var treeLayer in resultsLayers) {
                //    var mapLayer = treeLayer.layer();
                //    Debug.Assert(mapLayer is not null);
                //    this.currentResultsLayer = cast(QgsVectorLayer, mapLayer);
                //    Debug.Assert(this.currentResultsLayer is not null);
                //    this.mapTitle = new MapTitle(canvas, this.title, mapLayer);
                //    canvas.refresh();
                //}
                return;
            }
        }
        
        // Remove shape files (all components) from animation directory.
        public virtual void clearAnimationDir() {
            if (Directory.Exists(this._gv.animationDir)) {
                var pattern = "*.shp";
                Matcher matcher = new Matcher();
                matcher.AddInclude(pattern);
                foreach (var f in matcher.GetResultsInFullPath(this._gv.animationDir)) {
                    try {
                        Utils.removeFiles(f);
                    }
                    catch (Exception) {
                        continue;
                    }
                }
            }
        }

        // Remove .png files from Png directory.
        public virtual void clearPngDir() {
            if (Directory.Exists(this._gv.pngDir)) {
                var pattern = "*.png";
                Matcher matcher = new Matcher();
                matcher.AddInclude(pattern);
                foreach (var f in matcher.GetResultsInFullPath(this._gv.animationDir)) {
                    try {
                        File.Delete(f);
                    }
                    catch (Exception) {
                        continue;
                    }
                }
            }
            this.currentStillNumber = 0;
        }

            // started developing this but incomplete: not clear how to render the annotation
            // also not clear if multiple annotated visible layers would give clashing annotations
            // could continue with MapTitle below, and perhaps use QgsMapCanvasAnnotationItem as the QgsMapCanvasItem,
            // but looks as if it would be more complicated than current version
            // class MapTitle2(QgsTextAnnotation):
            // 
            //     def __init__(self, canvas: QgsMapCanvas, title: str, 
            //                  layer: QgsMapLayer, line2: Optional[str]=None):
            //         super().__init__() 
            //         ## normal font
            //         self.normFont = QFont()
            //         ## normal metrics object
            //         self.metrics = QFontMetricsF(self.normFont)
            //         # bold metrics object
            //         boldFont = QFont()
            //         boldFont.setBold(True)
            //         metricsBold = QFontMetricsF(boldFont)
            //         ## titled layer
            //         self.layer = layer
            //         ## project line of title
            //         self.line0 = 'Project: {0}', title)
            //         ## First line of title
            //         self.line1 = layer.name()
            //         ## second line of title (or None)
            //         self.line2 = line2
            //         rect0 = metricsBold.boundingRect(self.line0)
            //         rect1 = self.metrics.boundingRect(self.line1)
            //         ## bounding rectange of first 2 lines 
            //         self.rect01 = QRectF(0, rect0.top() + rect0.height(),
            //                             max(rect0.width(), rect1.width()),
            //                             rect0.height() + rect1.height())
            //         ## bounding rectangle
            //         self.rect = None
            //         if line2 is None:
            //             self.rect = self.rect01
            //         else:
            //             self.updateLine2(line2)
            //         text = QTextDocument()
            //         text.setDefaultFont(self.normFont)
            //         if self.line2 is None:
            //             text.setHtml('<p><b>{0}</b><br/>{1}</p>', self.line0, self.line1))
            //         else:
            //             text.setHtml('<p><b>{0}</b><br/>{1}<br/>{2}</p>', self.line0, self.line1, self.line2))
            //         canvasRect = canvas.extent()
            //         self.setMapPosition(QgsPointXY(canvasRect.xMinimum(), canvasRect.yMaximum()))
            //         self.setHasFixedMapPosition(True)
            //         self.setFrameSize(self.rect.size())
            //         self.setDocument(text)
            //         self.setMapLayer(layer)
            //     
            //     def updateLine2(self, line2: str) -> None:
            //         """Change second line."""
            //         self.line2 = line2
            //         rect2 = self.metrics.boundingRect(self.line2)
            //         self.rect = QRectF(0, self.rect01.top(), 
            //                             max(self.rect01.width(), rect2.width()), 
            //                             self.rect01.height() + rect2.height())
            //         
            //     def renderAnnotation(self, context, size):
    }
    
    //TODO
    //// Item for displaying title at top left of map canvas.
    //public class MapTitle
    //    : QgsMapCanvasItem {
        
    //    public object layer;
        
    //    public string line0;
        
    //    public object line1;
        
    //    public object line2;
        
    //    public object metrics;
        
    //    public object normFont;
        
    //    public object rect;
        
    //    public object rect01;
        
    //    public MapTitle(object canvas, string title, object layer, object line2 = null) {
    //        //# normal font
    //        this.normFont = QFont();
    //        //# normal metrics object
    //        this.metrics = QFontMetricsF(this.normFont);
    //        // bold metrics object
    //        var boldFont = QFont();
    //        boldFont.setBold(true);
    //        var metricsBold = QFontMetricsF(boldFont);
    //        //# titled layer
    //        this.layer = layer;
    //        //# project line of title
    //        this.line0 = "Project: {0}", title);
    //        //# First line of title
    //        this.line1 = layer.name();
    //        //# second line of title (or None)
    //        this.line2 = line2;
    //        var rect0 = metricsBold.boundingRect(this.line0);
    //        var rect1 = this.metrics.boundingRect(this.line1);
    //        //# bounding rectange of first 2 lines 
    //        this.rect01 = QRectF(0, rect0.top() + rect0.height(), max(rect0.width(), rect1.width()), rect0.height() + rect1.height());
    //        //# bounding rectangle
    //        this.rect = null;
    //        if (line2 is null) {
    //            this.rect = this.rect01;
    //        } else {
    //            this.updateLine2(line2);
    //        }
    //    }
        
    //    // Paint the text.
    //    public virtual object paint(object painter, object option, object widget = null) {
    //        // type: ignore # @UnusedVariable
    //        //         if self.line2 is None:
    //        //             painter.drawText(self.rect, Qt.AlignLeft, '{0}\n{1}', self.line0, self.line1))
    //        //         else:
    //        //             painter.drawText(self.rect, Qt.AlignLeft, '{0}\n{1}\n{2}', self.line0, self.line1, self.line2))
    //        var text = QTextDocument();
    //        text.setDefaultFont(this.normFont);
    //        if (this.line2 is null) {
    //            text.setHtml("<p><b>{0}</b><br/>{1}</p>", this.line0, this.line1));
    //        } else {
    //            text.setHtml("<p><b>{0}</b><br/>{1}<br/>{2}</p>", this.line0, this.line1, this.line2));
    //        }
    //        //Utils.loginfo(text.toPlainText())
    //        //Utils.loginfo(text.toHtml())
    //        text.drawContents(painter);
    //    }
        
    //    // Return the bounding rectangle.
    //    public virtual object boundingRect() {
    //        Debug.Assert(this.rect is not null);
    //        return this.rect;
    //    }
        
    //    // Change second line.
    //    public virtual object updateLine2(string line2) {
    //        this.line2 = line2;
    //        var rect2 = this.metrics.boundingRect(this.line2);
    //        this.rect = QRectF(0, this.rect01.top(), max(this.rect01.width(), rect2.width()), this.rect01.height() + rect2.height());
    //    }
        
    //    // Function used to identify a MapTitle object even when it has a wrapper.
    //    public virtual string identifyMapTitle() {
    //        return "MapTitle";
    //    }
    //}
    

}
