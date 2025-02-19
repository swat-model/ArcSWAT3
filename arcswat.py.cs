


using System.Diagnostics;

using System.Collections.Generic;

using System;
using System.IO;
using Path = System.IO.Path;

using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Microsoft.VisualBasic;

using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using LinearUnit = ArcGIS.Core.Geometry.LinearUnit;
using ArcGIS.Core.Data;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Desktop.Core.Geoprocessing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Shapes;
using ArcGIS.Desktop.Framework.AddIns;
using ArcGIS.Desktop.Internal.Framework;
using System.Runtime.InteropServices;
using System.Data.Entity.Core.Mapping;
using System.IO.Compression;
using System.Xml;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Core.Events;
using System.Windows.Documents;
using System.Text.RegularExpressions;



namespace ArcSWAT3
{

    // ArcGIS Pro plugin to prepare geographic data for SWAT Editor.
    public class ArcSWAT
    {

        public bool _demIsProcessed;

        public GlobalVars _gv;

        public MainForm _odlg;

        public DelinForm delin;

        public HRUs hrus;

        public bool loadFailed;

        public string plugin_dir;

        public object translator;

        public Visualise vis;

        public string _SWATEDITORVERSION = Parameters._SWATEDITORVERSION;

        public static string @__version__ = getVersion();

        public static Map mainMap;

        public static MapProjectItem mainMapItem;

        public ArcSWAT() {
            //object locale;
            // this import is a dependency on a Cython produuced .pyd file which will fail if the wrong architecture
            // and so gives an immediate exit before the plugin is loaded
            //# flag to show if init ran successfully
            this.loadFailed = false;
            // if not TYPE_CHECKING:
            //     try:
            //         from . import polygonizeInC2  # @UnusedImport @UnresolvedImport
            //     except Exception:
            //         Utils.loginfo("Failed to load Cython module: wrong architecture?")
            //         self.loadFailed = True
            //         return
            // uncomment next line for debugging
            // import pydevd; pydevd.settrace()
            // initialize plugin directory
            //# plugin directory
            //this.plugin_dir = os.path.dirname(@__file__);
            //// add to PYTHONPATH
            //sys.path.append(this.plugin_dir);
            //var settings = QSettings();
            // initialize locale
            // in testing with a dummy iface object this settings value can be None
            //try {
            //    locale = settings.value("locale/userLocale")[0:2:];
            //} catch (Exception) {
            //    locale = "en";
            //}
            //var localePath = os.path.join(this.plugin_dir, "i18n", "ArcSWAT_{}.qm".format(locale));
            //// set default behaviour for loading files with no CRS to prompt - the safest option
            //QSettings().setValue("Projections/defaultBehaviour", "prompt");
            ////# translator
            //if (os.path.exists(localePath)) {
            //    this.translator = QTranslator();
            //    this.translator.load(localePath);
            //    if (qVersion() > "4.3.3") {
            //        QCoreApplication.installTranslator(this.translator);
            //    }
            //}
            this._gv = null;
            // font = QFont("MS Shell Dlg 2", 8)
            // Create the dialog (after translation) and keep reference
            this._odlg = new MainForm(this);
            //self._odlg.setWindowFlags(self._odlg.windowFlags() & ~Qt.WindowContextHelpButtonHint & Qt.WindowMinimizeButtonHint)
            // TODO: this._odlg.Move(0, 0);
            //=======================================================================
            // font = self._odlg.font()
            // fm = QFontMetrics(font)
            // txt = "The quick brown fox jumps over the lazy dog."
            // family = font.family()
            // size = font.pointSize()
            // Utils.information("Family: {2}.  Point size: {3}.\nWidth of "{0}" is {1} pixels.".format(txt, fm.width(txt), family, size), False)
            //=======================================================================
            this._odlg.Text = String.Format("ArcSWAT3 {0}", ArcSWAT.@__version__);
            // flag used in initialising delineation form
            this._demIsProcessed = false;
            //# deineation window
            this.delin = null;
            //# create hrus window
            this.hrus = null;
            //# visualise window
            this.vis = null;
            Utils.openLog();
            // report version
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string fileVersion = String.Format("{0}.{1}.{2}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart);
            Utils.loginfo(String.Format("ArcGIS Pro version {0}; ArcSWAT version: {1}", fileVersion, ArcSWAT.@__version__));
        }

        // def initGui(self) -> None:
        //     """Create ArcSWAT button in the toolbar."""
        //     if self.loadFailed:
        //         return
        //     ## Action that will start plugin configuration
        //     self.action = QAction(
        //         QIcon(":/ArcSWAT/SWAT32.png"),
        //         u"ArcSWAT", self._iface.mainWindow())
        //     # connect the action to the run method
        //     self.action.triggered.connect(self.run)
        //
        //     # Add toolbar button and menu item
        //     self._iface.addToolBarIcon(self.action)
        //     self._iface.addPluginToMenu(u"&ArcSWAT", self.action)
        //
        // def unload(self) -> None:
        //     """Remove the ArcSWAT menu item and icon."""
        //     # allow for it not to have been loaded
        //     try:
        //         self._iface.removePluginMenu(u"&ArcSWAT", self.action)
        //         self._iface.removeToolBarIcon(self.action)
        //     except Exception:
        //         pass
        // Run ArcSWAT.

        private static string getVersion() {
            // this does not work: GetAddInInfos has no definition
            //var addInInfo = FrameworkApplication.GetAddInInfos().First(addIn => addIn.Name == "ArcSWAT3");
            //return addInInfo.Version.ToString();

            // this always return 1.0.0.0
            //var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            //var assemblyName = assembly.GetName();
            //return assemblyName.Version.ToString();

            // get version from config.daml.  Note this depends on daml being included in build (eg copy if newer)
            string version = "";
            XmlDocument xDoc = new XmlDocument();
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string configDamlPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "Config.daml");
            using (StreamReader streamReader = new StreamReader(configDamlPath)) {
                var daml = streamReader.ReadToEnd();
                xDoc.LoadXml(daml); // @"<?xml version=""1.0"" encoding=""utf - 8""?>" + 
            }
            XmlNodeList items = xDoc.GetElementsByTagName("AddInInfo");
            foreach (XmlNode xItem in items) {
                version = xItem.Attributes["version"].Value;
            }
            return version;
        }

        public async Task run() {

            // make sure we clear data from previous runs
            this.delin = null;
            this.hrus = null;
            this.vis = null;
            // initially only new/existing project buttons visible if project not set
            this._odlg.initButtons();
            this._odlg.Show();
            if (MapView.Active is not null) {
                await setupProject(false);
            }
        }

        // Create a new project.
        public async Task newProject() {
            var title = Utils.trans("Choose parent directory for project");
            string parentDir = "";
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                dialog.Description = title;
                dialog.UseDescriptionForTitle = true;
                if (dialog.ShowDialog() == DialogResult.OK) {
                    parentDir = dialog.SelectedPath;
                } else { return; }
            }
            title = Utils.trans("Pleae provide a project name");
            string projName = Interaction.InputBox(title, "", "", 10, 20);
            if (string.IsNullOrEmpty(projName)) { return; }
            CreateProjectSettings settings = new CreateProjectSettings() {
                LocationPath = parentDir,
                Name = projName,
                TemplateType = TemplateType.Map
            };
            await Project.CreateAsync(settings);
            while (MapView.Active is null) {
                await Task.Delay(1000);
            }
            this._odlg.initButtons();
            await this.setupProject(false);
            this._gv.writeMasterProgress(0, 0);
        }

        // Open an existing project.
        public async Task existingProject() {
            var title = Utils.trans("Choose project (*.aprx) file");
            var filtr = Utils.trans("Project files (*.aprx)|*.aprx");
            string projPath = "";
            using (OpenFileDialog dlg = new OpenFileDialog()) {
                dlg.Title = title;
                dlg.Filter = filtr;
                dlg.RestoreDirectory = false;
                if (dlg.ShowDialog() == DialogResult.OK) {
                    projPath = dlg.FileName;
                } else { return; }
            }
            if (!string.IsNullOrEmpty(projPath)) {
                await Project.OpenAsync(projPath);
                while (MapView.Active is null) {
                    await Task.Delay(1000);
                }
                this._odlg.initButtons();
                this._odlg.setProject("Restarting project ...");
                await this.setupProject(false);
            }
        }

        // Set up the project.
        public async Task setupProject(
            bool isBatch,
            bool isHUC = false,
            bool isHAWQS = false,
            string logFile = null,
            bool fromGRASS = false,
            string TNCDir = "") {
            //this._odlg.mainBox.setVisible(true);
            //this._odlg.mainBox.setEnabled(false);
            //this._odlg.setCursor(Qt.WaitCursor);
            //this._odlg.projPath.setText("Restarting project ...");
            //Utils.information(string.Format("isHUC initially {0}", isHUC), isBatch);
            // now have project so initiate global vars
            // if we do this earlier we cannot for example find the project database
            this._gv = new GlobalVars(isBatch, isHUC, isHAWQS, logFile, fromGRASS, TNCDir);
            var title = this._gv.projName;
            Proj proj = this._gv.proj;
            bool found;
            // for isHUC etc parameters take precedence if set true
            if (isHUC) {
                proj.writeEntryBool(title, "delin/isHUC", isHUC);
            } else {
                (isHUC, found) = proj.readBoolEntry("", "delin/isHUC", false);
            }
            // # same as isHUC for isHAWQS
            if (isHAWQS) {
                proj.writeEntryBool(title, "delin/isHAWQS", isHAWQS);
            } else {
                (isHAWQS, found) = proj.readBoolEntry("", "delin/isHAWQS", false);
            }
            // # same as isHUC for fromGRASS
            if (fromGRASS) {
                proj.writeEntryBool(title, "delin/fromGRASS", fromGRASS);
            } else {
                (fromGRASS, found) = proj.readBoolEntry("", "delin/fromGRASS", false);
            }
            //TODO
            //if (!string.IsNullOrEmpty(TNCDir)) {
            //    proj.writeEntry(title, "delin/TNCDir", TNCDir);
            //} else { 
            //    (TNCDir, found) = proj.readEntry("", "delin/TNCDir", "");
            //}
            //this._gv.plugin_dir = this.plugin_dir;
            //this._odlg.projPath.repaint();
            this._odlg._gv = this._gv;
            this._odlg.checkReports();
            // identify the main project map
            var mapItems = Project.Current.GetItems<MapProjectItem>();
            int mapCount = mapItems.Count();
            if (mapCount == 1) {
                // maybe new project.  Set map title as "Main" and store link to it
                ArcSWAT.mainMapItem = mapItems.First();
                ArcSWAT.mainMap = await QueuedTask.Run(mainMapItem.GetMap);
            } else {
                foreach (var mapItem in mapItems) {
                    if (mapItem.Name.Equals("Map", StringComparison.CurrentCultureIgnoreCase)) {
                        ArcSWAT.mainMapItem = mapItem;
                        ArcSWAT.mainMap = await QueuedTask.Run(mapItem.GetMap);
                        break;
                    }
                }
            }
            ProjectItemRemovingEvent.Subscribe(OnItemRemoval);
            await setLegendGroups();
            // enable edit button if converted from Arc
            int choice;
            (choice, found) = proj.readNumEntry(title, "delin/fromArc", -1);
            if (found) {
                if (choice >= 0) {   // NB values from convertFromArc.py, 0 for full, 1 for existing, 2 for no gis.
                    this._odlg.allowEdit();
                }
            }
            this._gv.useGridModel = proj.readBoolEntry(title, "delin/useGridModel", false).Item1;
             if (this._gv.useGridModel)
                 this._gv.gridSize = proj.readNumEntry(title, "delin/gridSize", 1).Item1;
            //Add ArcSWAT3 styles to project
            string path = Path.Combine(this._gv.addinPath, "ArcSWAT3.stylx");
            var styleItem = ItemFactory.Instance.Create(path) as IProjectItem;
            await QueuedTask.Run(() => Project.Current.AddItem(styleItem));
            //ARCSWAT3 custom style
            this._gv.arcSWAT3Style =
                Project.Current.GetItems<StyleProjectItem>().FirstOrDefault(s => s.Name == "ArcSWAT3");
            if (await this.demProcessed(proj)) {
                this._demIsProcessed = true;
                this._odlg.allowCreateHRU();
                var hrus = new HRUs(this._gv, this._odlg.reportsCombo, this);
                //result = hrus.tryRun()
                //if result == 1:
                if (hrus.HRUsAreCreated()) {
                    this._odlg.showReports();
                    this._odlg.allowEdit();
                }
            }
            var outputDb = Utils.join(this._gv.tablesOutDir, Parameters._OUTPUTDB);
            if (this._gv.forTNC) {
                outputDb = outputDb.Replace(".mdb", ".sqlite");
            }
            if (File.Exists(outputDb)) {
                this._odlg.allowVisualise();
                // TODO: this.loadVisualisationLayers();
            }
            this._odlg.setProject(this._gv.projDir);
            //this._odlg.mainBox.setEnabled(true);
            //this._odlg.setCursor(Qt.ArrowCursor);
        }

        public Task OnItemRemoval(ProjectItemRemovingEventArgs args) {
            Item[] arg = args.ProjectItems;
            foreach (Item item in arg) {
                if (item.ToString().StartsWith("<MapProjectItem Map@")) {
                    Utils.error("You are trying to delete the main map", false);
                    args.Cancel = true;
                }
            }
            return Task.CompletedTask;
        }

        // Run parameters form.
        public void runParams() {
            var @params = new Parameters(this._gv);
            @params.run();
        }

        // Run the delineation dialog.
        public void doDelineation() {
            //Debug.Assert(this._gv is not null);
            // avoid getting second window
            if (this.delin is not null && this.delin.Enabled) {
                this.delin.Close();
            }
            this.delin = new DelinForm(this._gv, this._demIsProcessed, this);
            //Debug.Assert(this.delin is not null);
            this.delin.Show();
        }

        public void postDelineation(bool result) { 
            if (result == true && this._gv.isDelinDone()) {
                this._odlg.allowCreateHRU();
                // remove old data so cannot be reused
                var basinsdataTable = this._gv.isHUC || this._gv.isHAWQS ? "BASINSDATAHUC1" : "BASINSDATA1";
                this._gv.db.clearTable(basinsdataTable);
                // make sure HRUs starts from scratch
                this.hrus = null;
            } else {
                this._demIsProcessed = false;
                this._odlg.initButtons();
            }
            //this._odlg.BringToFront();
        }

        // Run the HRU creation dialog.
        public void doCreateHRUs() {
            //Debug.Assert(this._gv is not null);
            // avoid getting second window
            if (this.hrus is not null && this.hrus._dlg.Enabled) {
                this.hrus._dlg.Close();
            }
            this.hrus = new HRUs(this._gv, this._odlg.reportsCombo, this);
            //Debug.Assert(this.hrus is not null);
            // non modal
            this.hrus.run();
        }

        public void postCreateHRUs(bool result) { 
            //if (result == true && this._gv.isHRUsDone()) {
            if (result) { 
                this._odlg.allowEdit();
            }
        }

        // 
        //         Return true if we can proceed with HRU creation.
        //         
        //         Return false if any required project setting is not found 
        //         in the project file
        //         Return true if:
        //         Using existing watershed and watershed grid exists and 
        //         is newer than dem
        //         or
        //         Not using existing watershed and filled dem exists and 
        //         is no older than dem, and
        //         watershed shapefile exists and is no older than filled dem
        //         
        public async Task<bool> demProcessed(Proj proj) {
            var title = this._gv.projName;
            //var root = proj.layerTreeRoot();
            string demFile;
            bool found;
            (demFile, found) = proj.readEntry(title, "delin/DEM", "");
            if (!found || string.IsNullOrEmpty(demFile) || !File.Exists(demFile)) {
                Utils.loginfo("demProcessed failed: no DEM");
                return false;
            }
            var demLayer = (await Utils.getLayerByFilename(demFile, FileTypes._DEM, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as RasterLayer;
            if (demLayer is null) {
                Utils.loginfo("demProcessed failed: no DEM layer");
                return false;
            }
            this._gv.demFile = demFile;
            dynamic nodata = null;
            await QueuedTask.Run(() => {
                // set extent to DEM as otherwise defaults to full globe
                var demExtent = demLayer.QueryExtent();
                var map = ArcSWAT.mainMap;
                map.SetCustomFullExtent(demExtent);
                nodata = demLayer.GetRaster().GetNoDataValue();
                if (nodata is not null) { 
                    var typ = nodata.GetType();
                    if (typ.IsArray) {
                        this._gv.elevationNoData = Convert.ToDouble(nodata[0]);
                    } else {
                        this._gv.elevationNoData = Convert.ToDouble(nodata);
                    }
                }
            });

            if (nodata is null) {
                Utils.loginfo("demProcessed failed: dem does not have a nodata value");
                return false;
            }
            var crsProject = await QueuedTask.Run(() => demLayer.GetSpatialReference());
            if (!crsProject.IsProjected) { 
                Utils.loginfo(string.Format("demProcessed failed: DEM is not projected"));
                return false;
            }
            var XYSizes = await QueuedTask.Run<Tuple<double, double>>(() => {
                return demLayer.GetRaster().GetMeanCellSize();
            });
            var units = crsProject.Unit;
            double factor = units.ConversionFactor;
            this._gv.cellArea = XYSizes.Item1 * XYSizes.Item2 * factor * factor;
            // hillshade
            //Delineation.addHillshade(demFile, root, demLayer, this._gv);
            string outletFile;
            (outletFile, found) = proj.readEntry(title, "delin/outlets", "");
            FeatureLayer outletLayer = null;
            if (found && !string.IsNullOrEmpty(outletFile) && File.Exists(outletFile)) {
                var ft = this._gv.isHUC || this._gv.isHAWQS ? FileTypes._OUTLETSHUC : FileTypes._OUTLETS;
                outletLayer = (await Utils.getLayerByFilename(outletFile, ft, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                if (outletLayer is null) {
                    Utils.loginfo("demProcessed failed: no outlet layer");
                    return false;
                }
            }
            this._gv.outletFile = outletFile;
            this._gv.existingWshed = proj.readBoolEntry(title, "delin/existingWshed", false).Item1;
            this._gv.useGridModel = proj.readBoolEntry(title, "delin/useGridModel", false).Item1;
            string streamFile;
            (streamFile, found) = proj.readEntry(title, "delin/net", "");
            if (!found || string.IsNullOrEmpty(streamFile) || !File.Exists(streamFile)) {
                Utils.loginfo("demProcessed failed: no stream reaches shapefile");
                return false;
            }
            this._gv.streamFile = streamFile;
            string wshedFile;
            (wshedFile, found) = proj.readEntry(title, "delin/wshed", "");
            if (!found || string.IsNullOrEmpty(wshedFile) || !File.Exists(wshedFile)) {
                Utils.loginfo("demProcessed failed: no subbasins shapefile");
                return false;
            }
            var wshedTime = File.GetLastWriteTime(wshedFile);
            var wshedLayer = (await Utils.getLayerByFilename(wshedFile, FileTypes._WATERSHED, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            if (wshedLayer is null) {
                Utils.loginfo("demProcessed failed: no watershed layer");
                return false;
            };
            this._gv.wshedFile = wshedFile;
            string extraOutletFile;
            (extraOutletFile, found) = proj.readEntry(title, "delin/extraOutlets", "");
            FeatureLayer extraOutletLayer = null;
            if (found && !string.IsNullOrEmpty(extraOutletFile) && File.Exists(extraOutletFile)) {
                extraOutletLayer = (await Utils.getLayerByFilename(extraOutletFile, FileTypes._OUTLETS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                if (extraOutletLayer is null) {
                    Utils.loginfo("demProcessed failed: no extra outlet layer");
                    return false;
                }
            }
            this._gv.extraOutletFile = extraOutletFile;
            var @base = Path.ChangeExtension(demFile, null);
            this._gv.slopeFile = @base + "slp.tif";
            // GRASS slope file should be based on original DEM
            if (this._gv.fromGRASS && this._gv.slopeFile.EndsWith("_burnedslp.tif")) {
                var unburnedslp = this._gv.slopeFile.Replace("_burnedslp.tif", "slp.tif");
                if (File.Exists(unburnedslp)) {
                    this._gv.slopeFile = unburnedslp;
                }
            }
            // if slope file was based on burned-in dem, replace with one produced without burning in
            var noBurnSlopeFile = @base + "slope.tif";
            if (Utils.isUpToDate(this._gv.slopeFile, noBurnSlopeFile)) {
                this._gv.slopeFile = noBurnSlopeFile;
            }
            if (!File.Exists(this._gv.slopeFile)) {
                Utils.loginfo("demProcessed failed: no slope raster");
                return false;
            }
            this._gv.basinFile = @base + "w.tif";
            //if (this._gv.useGridModel) {
            //    this._gv.isBig = wshedLayer.featureCount() > 100000 || this._gv.forTNC;
            //    Utils.loginfo("isBig is {0}".format(this._gv.isBig));
            //}
            if (this._gv.existingWshed) {
                if (!this._gv.useGridModel) {
                    if (!File.Exists(this._gv.basinFile)) {
                        Utils.loginfo("demProcessed failed: no basins raster");
                        return false;
                        // following checks that basins raster created after shapefile, since this is what TauDEM does
                        // but for existing watershed we should not care how the maps were created
                        // so we removed this check
                        //                 winfo = QFileInfo(self._gv.basinFile)
                        //                 # cannot use last modified times because subbasin field in wshed file changed after wfile is created
                        //                 wCreateTime = winfo.created()
                        //                 wshedCreateTime = wshedInfo.created()
                        //                 if not wshedCreateTime <= wCreateTime:
                        //                     Utils.loginfo("demProcessed failed: wFile not up to date for existing watershed")
                        //                     return False
                    }
                }
            } else {
                this._gv.pFile = @base + "p.tif";
                if (!File.Exists(this._gv.pFile)) {
                    Utils.loginfo("demProcessed failed: no p raster");
                    return false;
                }
                if (!this._gv.fromGRASS) {
                    var felFile = @base + "fel.tif";
                    if (!File.Exists(felFile)) {
                        Utils.loginfo("demProcessed failed: no filled raster");
                        return false;
                    }
                    var demTime = File.GetLastWriteTime(demFile);
                    var felTime = File.GetLastWriteTime(felFile);
                    if (!(demTime <= felTime && felTime <= wshedTime)) {
                        Utils.loginfo("demProcessed failed: not up to date");
                        return false;
                    }
                    if (!this._gv.useGridModel) {
                        this._gv.distFile = @base + "dist.tif";
                        if (!File.Exists(this._gv.distFile)) {
                            Utils.loginfo("demProcessed failed: no distance to outlet raster");
                            return false;
                        }
                    }
                }
            }
            if (!await this._gv.topo.setUp0(demLayer, streamFile, this._gv.verticalFactor)) {
                return false;
            }
            var streamLayer = (await Utils.getLayerByFilename(streamFile, FileTypes._STREAMS, this._gv, null, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            if (streamLayer is null) {
                Utils.loginfo("demProcessed failed: no stream reaches layer");
                return false;
            }
            var basinIndex = await this._gv.topo.getIndex(wshedLayer, Topology._POLYGONID);
            if (basinIndex < 0) {
                Utils.loginfo("demProcessed failed: no PolygonId field in washed file");
                return false;
            }
            await QueuedTask.Run(() => {
                using (RowCursor rowCursor = wshedLayer.Search(null)) {
                    while (rowCursor.MoveNext()) {
                        using (Row polygon = rowCursor.Current) {
                            var basin = Convert.ToInt32(polygon[basinIndex]);
                            var centroid = GeometryEngine.Instance.Centroid(((Feature)polygon).GetShape());
                            this._gv.topo.basinCentroids[basin] = new Coordinate2D(centroid.X, centroid.Y);
                        }
                    }
                }
            });
            // this can go wrong if eg the streams and watershed files exist but are inconsistent
            try {
                if (!await this._gv.topo.setUp(demLayer, streamFile, wshedFile, outletFile, extraOutletFile, this._gv, this._gv.existingWshed, false, this._gv.useGridModel, false)) {
                    // type: ignore
                    Utils.loginfo("demProcessed failed: topo setup failed");
                    return false;
                }
                if (this._gv.topo.inletLinks.Count == 0) {
                    // no inlets, so no need to expand subbasins layer legend
                    var subbasinsLayer = Utils.getLayerByLegend(FileTypes.legend(FileTypes._SUBBASINS));
                    //Debug.Assert(subbasinsLayer is not null);
                    if (subbasinsLayer != null) {
                        await QueuedTask.Run(() => subbasinsLayer.SetExpanded(false));
                    }
                }
            }
            catch (Exception ex) {
                Utils.loginfo(string.Format("demProcessed failed: topo setup raised exception: {0}", ex.Message));
                return false;
            }
            return this._gv.isDelinDone();
        }



        // Legend groups are used to keep legend in reasonable order.  
        //         Create them if necessary.
        //         
        public static async Task setLegendGroups() {
            var map = ArcSWAT.mainMap;
            await QueuedTask.Run(async () => {
                var groups = new List<string> {
                    Utils._SLOPE_GROUP_NAME,
                    Utils._SOIL_GROUP_NAME,
                    Utils._LANDUSE_GROUP_NAME,
                    Utils._WATERSHED_GROUP_NAME
                };
                    //Utils._RESULTS_GROUP_NAME,
                    //Utils._ANIMATION_GROUP_NAME
                foreach (string name in groups) {
                    var layer = Utils.getGroupLayerByName(name);
                    if (layer is null) {
                        LayerFactory.Instance.CreateGroupLayer(map, 0, name);
                        //layer.SetExpanded(true);
                    }
                }
                // clear any animations left accidentally
                await Utils.clearAnimationMaps();
                // don't do this - hides landuse, soil and slope
                //// move world hillshade layer to base of watershed group
                //GroupLayer wshedGroup = Utils.getGroupLayerByName(Utils._WATERSHED_GROUP_NAME);
                //if (wshedGroup is not null) {
                //    List<Layer> layers = MapView.Active.Map.GetLayersAsFlattenedList().Where(layer => layer.Name == "World Hillshade").ToList();
                //    if (layers.Count > 0 ) {
                //        map.MoveLayer(layers[0], wshedGroup, -1);
                //    }
                //}
            });
        }

        // Start the SWAT Editor, first setting its initial parameters.
        public void startEditor() {
            //Debug.Assert(this._gv is not null);
            if (!File.Exists(this._gv.SWATEditorPath)) {
                Utils.error(string.Format("Cannot find SWAT Editor {0}: is it installed?", this._gv.SWATEditorPath), this._gv.isBatch);
                return;
            }
            this._gv.setSWATEditorParams();
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = this._gv.SWATEditorPath
                }
            };
            process.Start();
            process.WaitForExit();
            var outputDb = Utils.join(this._gv.tablesOutDir, Parameters._OUTPUTDB);
            if (this._gv.forTNC) {
                outputDb = outputDb.Replace(".mdb", ".sqlite");
            }
            if (File.Exists(outputDb)) {
                this._odlg.allowVisualise();
            }
        }

        // Close the database connections and subsidiary forms.
        public async Task finish(DialogResult result) {
            if (result == DialogResult.OK) {
                await Project.Current.SaveAsync();
            }
            Utils.loginfo("Closing databases");
            try {
                this.delin = null;
                this.hrus = null;
                this.vis = null;
                if (this._gv is not null && this._gv.db is not null) {
                    if (this._gv.db.conn is not null) {
                        this._gv.db.conn.Close();
                    }
                    if (this._gv.db.connRef is not null) {
                        this._gv.db.connRef.Close();
                        if ((this._gv.isHUC || this._gv.isHAWQS) && this._gv.db.SSURGOConn is not null) {
                            this._gv.db.SSURGOConn.Close();
                        }
                    }
                }
                Utils.loginfo("Databases closed");
            }
            catch (Exception) {
            }
        }

        // Run visualise form.
        public async Task visualise() {
            // avoid getting second window
            if (this.vis is not null) {
                await this.vis.doClose();
            }
            this.vis = new Visualise(this._gv);
            Debug.Assert(this.vis is not null);
            await this.vis.run();
        }

        //// If we have subs1.shp and riv1.shp and an empty watershed group then add these layers.
        ////         
        ////         Intended for use after a no gis conversion from ArcSWAT.
        //public virtual object loadVisualisationLayers() {
        //    Debug.Assert(this._gv is not null);
        //    var root = QgsProject.instance().layerTreeRoot();
        //    var wshedLayers = Utils.getLayersInGroup(Utils._WATERSHED_GROUP_NAME, root);
        //    // ad layers if we have empty Watershed group
        //    var addLayers = wshedLayers.Count == 0;
        //    if (addLayers) {
        //        var wshedFile = os.path.join(this._gv.shapesDir, "subs1.shp");
        //        var streamFile = os.path.join(this._gv.shapesDir, "riv1.shp");
        //        if (os.path.exists(wshedFile) && os.path.exists(streamFile)) {
        //            var group = root.findGroup(Utils._WATERSHED_GROUP_NAME);
        //            if (group is not null) {
        //                var proj = QgsProject.instance();
        //                var wshedLayer = QgsVectorLayer(wshedFile, "Subbasins", "ogr");
        //                wshedLayer = cast(QgsVectorLayer, proj.addMapLayer(wshedLayer, false));
        //                group.insertLayer(0, wshedLayer);
        //                // style file like wshed.qml but does not check for subbasins upstream frm inlets
        //                wshedLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, "wshed2.qml"));
        //                var streamLayer = QgsVectorLayer(streamFile, "Streams", "ogr");
        //                streamLayer = cast(QgsVectorLayer, proj.addMapLayer(streamLayer, false));
        //                group.insertLayer(0, wshedLayer);
        //                streamLayer.loadNamedStyle(Utils.join(this._gv.plugin_dir, "stream.qml"));
        //            }
        //        }
        //    }
        //}
    }
}
    
