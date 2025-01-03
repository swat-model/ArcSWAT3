



using System.Collections.Generic;

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using System.Data.SQLite;
using System.Reflection;
using ArcGIS.Core.Data.UtilityNetwork.Trace;

namespace ArcSWAT3
{

        // Data used across across the plugin, and some utilities on it.
        public class GlobalVars {

            public string addinPath;

            public StyleProjectItem arcSWAT3Style;

            public object aboutPos;

            public string actHRUsFile;

            public string animationDir;

            public string basinFile;

            public int basinNoData;

            public string burnedDemFile;

            public string burnFile;

            public double cellArea;

            public int cropNoData;

            public DBUtils db;

            public string dbProjTemplate;

            public string dbRefTemplate;

            public object delineatePos;

            public string demFile;

            public string distFile;

            public int distNoData;

            public object elevationBandsPos;

            public double elevationNoData;

            public int elevBandsThreshold;

            public List<string> exemptLanduses;

            public object exemptPos;

            public bool existingWshed;

            public string extraOutletFile;

            public bool forTNC;

            public bool fromGRASS;

            public string fullHRUsFile;

            public object globaldata;

            public string gridDir;

            public int gridSize;

            public string hd8File;

            public object hrusPos;

            public string HUCDataDir;

            public bool isBatch;

            public bool isBig;

            public bool isCatchmentProject;

            public bool isHAWQS;

            public bool isHUC;

            public string landuseDir;

            public string landuseFile;

            public string landuseTable;

            public string logFile;

            public string mpiexecPath;

            public int numElevBands;

            public string outletFile;

            public object outletsPos;

            public object parametersPos;

            public string pFile;

            public string plugin_dir;

            public string pngDir;

            public string projDir;

            public string projName;

            public Proj proj;

            //public int QGISSubVersion;

            public string scenariosDir;

            public object selectLuPos;

            public object selectOutletFilePos;

            public object selectOutletPos;

            public object selectResPos;

            public object selectSubsPos;

            public string shapesDir;

            public string slopeBandsFile;

            public string slopeFile;

            public int slopeNoData;

            public string soilDir;

            public string soilFile;

            public int soilNoData;

            public string soilTable;

            public string sourceDir;

            public Dictionary<string, Dictionary<string, int>> splitLanduses;

            public object splitPos;

            public string streamFile;

            public string SWATEditorPath;

            public string SWATExeDir;

            public string tablesInDir;

            public string tablesOutDir;

            public string TauDEMDir;

            public string textDir;

            public string tempDir;

            public int TNCCatchmentThreshold;

            public string TNCDir;

            public Topology topo;

            public bool useGridModel;

            public object vectorFileWriterOptions;

            public double verticalFactor;

            public string verticalUnits;

            public object visualisePos;

            public string wshedFile;

            public int xBlockSize;

            public int yBlockSize;

            public GlobalVars(
                bool isBatch,
                bool isHUC = false,
                bool isHAWQS = false,
                string logFile = null,
                bool fromGRASS = false,
                string TNCDir = "") {
                string SWATEditorDir;
            //# QGIS interface
            //this.iface = iface;
            // set SWAT EDitor, databases, TauDEM executables directory and mpiexec path
            // In Windows values currently stored in registry, in HKEY_CURRENT_USER\Software\QGIS\QGIS2
            // Read values from settings if present, else set to defaults
            // This allows them to be set elsewhere, in particular by Parameters module
            //var settings = QSettings();
            //if (settings.contains("/QSWAT/SWATEditorDir")) {
            //    SWATEditorDir = settings.value("/QSWAT/SWATEditorDir");
            //} else {
            //    settings.setValue("/QSWAT/SWATEditorDir", Parameters._SWATEDITORDEFAULTDIR);
            //    SWATEditorDir = Parameters._SWATEDITORDEFAULTDIR;
            //}
            // TODO
            // location of addIn code (including here or below python scripts, stylx file)
            this.addinPath = System.IO.Path.GetDirectoryName(new System.Uri(typeof(GlobalVars).Assembly.Location).AbsolutePath);
            this.addinPath = Uri.UnescapeDataString(this.addinPath);
            //this.addinPath = Uri.UnescapeDataString(@"C:\Users\Chris\source\repos\ArcSWAT3");
            SWATEditorDir = Parameters._SWATEDITORDEFAULTDIR;
            //# Directory containing SWAT executables
            // SWAT Editor does not like / in paths
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                    SWATEditorDir = SWATEditorDir.Replace("/", "\\");
                    this.SWATExeDir = SWATEditorDir + "\\";
                } else {
                    this.SWATExeDir = SWATEditorDir + "/";
                }
            //# Path of SWAT Editor
            this.SWATEditorPath = Utils.join(SWATEditorDir, Parameters._SWATEDITOR);
            // base directory for projects for The Nature Conservancy, otherwise ''
            this.TNCDir = TNCDir;
            //# flag to show project for TNC
            this.forTNC = TNCDir != "";
            //# minimum catchment size (in sq km)
            this.TNCCatchmentThreshold = 150;
            //# Path of template project database
            if (this.forTNC) {
                this.dbProjTemplate = Utils.join(TNCDir, Parameters._TNCDBPROJ);
            } else {
                this.dbProjTemplate = Utils.join(Utils.join(SWATEditorDir, Parameters._DBDIR), Parameters._DBPROJ);
            }
            //# Path of template reference database
            if (this.forTNC) {
                this.dbRefTemplate = Utils.join(TNCDir, Parameters._TNCDBREF);
            } else {
                this.dbRefTemplate = Utils.join(Utils.join(SWATEditorDir, Parameters._DBDIR), Parameters._DBREF);
            }
            if (this.forTNC) {
                this.globaldata = Utils.join(TNCDir, "../globaldata");
            }
            //# Directory of TauDEM executables, version 5.3.9
            this.TauDEMDir = Utils.join(SWATEditorDir, Parameters._TAUDEM539DIR);
            //# Path of mpiexec
            //if (settings.contains("/QSWAT/mpiexecDir")) {
            //    this.mpiexecPath = Utils.join(settings.value("/QSWAT/mpiexecDir"), Parameters._MPIEXEC);
            //} else {
            //    settings.setValue("/QSWAT/mpiexecDir", Parameters._MPIEXECDEFAULTDIR);
                this.mpiexecPath = Utils.join(Parameters._MPIEXECDEFAULTDIR, Parameters._MPIEXEC);
            //}
            //# QGIS sub version number
            //this.QGISSubVersion = Convert.ToInt32(Qgis.QGIS_VERSION.split(".")[1]);
            //# Flag showing if using existing watershed
            this.existingWshed = false;
            //# Flag showing if using grid model
            this.useGridModel = false;
            //# flag to show large grid - dominant landuse, soil and slope only
            this.isBig = false;
            //# grid size (grid models only)
            this.gridSize = 0;
            //# Directory containing QSWAT plugin
            this.plugin_dir = "";
            //# Path of DEM grid
            this.demFile = "";
            //# Path of stream burn-in shapefile
            this.burnFile = "";
            //# Path of DEM after burning-in
            this.burnedDemFile = "";
            //# Path of D8 flow direction grid
            this.pFile = "";
            //# Path of basins grid
            this.basinFile = "";
            //# Path of outlets shapefile
            this.outletFile = "";
            //# Path of outlets shapefile for extra reservoirs and point sources
            this.extraOutletFile = "";
            //# Path of stream reaches shapefile
            this.streamFile = "";
            //# Path of watershed shapefile
            this.wshedFile = "";
            //# Path of file like D8 contributing area but with heightened values at subbasin outlets
            this.hd8File = "";
            //# Path of distance to outlets grid
            this.distFile = "";
            //# Path of slope grid
            this.slopeFile = "";
            //# Path of slope bands grid
            this.slopeBandsFile = "";
            //# Path of landuse grid
            this.landuseFile = "";
            //# Path of soil grid
            this.soilFile = "";
            //# Landuse lookup table
            this.landuseTable = "";
            //# Soil lookup table
            this.soilTable = "";
            //# Nodata value for DEM
            this.elevationNoData = 0.0;
            //# DEM horizontal block size
            this.xBlockSize = 0;
            //# DEM vertical block size
            this.yBlockSize = 0;
            //# Nodata value for basins grid
            this.basinNoData = 0;
            //# Nodata value for distance to outlets grid
            this.distNoData = 0;
            //# Nodata value for slope grid
            this.slopeNoData = 0;
            //# Nodata value for landuse grid
            this.cropNoData = 0;
            //# Nodata value for soil grid
            this.soilNoData = 0;
            //# Area of DEM cell in square metres
            this.cellArea = 0.0;
            //# list of landuses exempt from HRU removal
            this.exemptLanduses = new List<string>();
            //# table of landuses being split
            this.splitLanduses = new Dictionary<string, Dictionary<string, int>>();
            //# Elevation bands threshold in metres
            this.elevBandsThreshold = 0;
            //# Number of elevation bands
            this.numElevBands = 0;
            //# Topology object
            this.topo = new Topology(isBatch, isHUC, isHAWQS, fromGRASS, this.forTNC, this.TNCCatchmentThreshold);
            var projFile = Project.Current.Name;
            var projPath = Project.Current.Path;
            // avoid / on Windows because of SWAT Editor
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                projPath = projPath.Replace("/", "\\");
            }
            //# Project name
            // Project.Current.Name finishes with .aprx, which we need to remove
            int posn = projFile.LastIndexOf(".");
            this.projName = projFile.Substring(0, posn);
            //# Project directory
            this.projDir = Path.GetDirectoryName(projPath);
            // Proj
            this.proj = new Proj(this);
            //# flag to show if a TNC project is running on a catchment or the whole continent
            this.isCatchmentProject = false;
            if (this.forTNC) {
                var parent = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(this.projDir));
                this.isCatchmentProject = (parent == "Catchments");
            }
            //# Source directory
            this.sourceDir = "";
            //# Landuse directory
            this.landuseDir = "";
            //# Soil directory
            this.soilDir = "";
            //# TablesIn directory
            this.tablesInDir = "";
            //# text directory
            this.textDir = "";
            //# Shapes directory
            this.shapesDir = "";
            //# Grid directory
            this.gridDir = "";
            //# Scenarios directory
            this.scenariosDir = "";
            //# TablesOut directory
            this.tablesOutDir = "";
            //# png directory for storing png images used to create animation videos
            this.pngDir = "";
            //# animation directory for storing animation files
            this.animationDir = "";
            this.createSubDirectories();
            //# Path of FullHRUs shapefile
            this.fullHRUsFile = Utils.join(this.shapesDir, "hru1.shp");
            //# Path of ActHRUs shapefile
            this.actHRUsFile = Utils.join(this.shapesDir, "hru2.shp");
            //# Flag to show if running in batch mode
            this.isBatch = isBatch;
            //# flag for HUC projects
            this.isHUC = isHUC;
            //# flag for HAWQS projects
            this.isHAWQS = isHAWQS;
            //# log file for message output for HUC projects
            this.logFile = logFile;
            //# data directory for HUC projects
            // default for debugging
            this.HUCDataDir = "I:/Data";
            // flag for projects using GRASS to do delineation
            this.fromGRASS = fromGRASS;
            //# Path of project database
            this.db = new DBUtils(this.projDir, this.projName, this.dbProjTemplate, this.dbRefTemplate, this.isHUC, this.isHAWQS, this.forTNC, this.logFile, this.isBatch);
            //# multiplier to turn elevations to metres
            this.verticalFactor = 1.0;
            //# vertical units
            this.verticalUnits = Parameters._METRES;
            // positions of sub windows
            //# Position of delineation form
            this.delineatePos = new Point(0, 100);
            //# Position of HRUs form
            this.hrusPos = new Point(0, 100);
            //# Position of parameters form
            this.parametersPos = new Point(50, 100);
            //# Position of select subbasins form
            this.selectSubsPos = new Point(50, 100);
            //# Position of select reservoirs form
            this.selectResPos = new Point(50, 100);
            //# Position of about form
            this.aboutPos = new Point(50, 100);
            //# Position of elevation bands form
            this.elevationBandsPos = new Point(50, 100);
            //# Position of split landuses form
            this.splitPos = new Point(50, 100);
            //# Position of select landuses form
            this.selectLuPos = new Point(50, 100);
            //# Position of exempt landuses form
            this.exemptPos = new Point(50, 100);
            //# Position of outlets form
            this.outletsPos = new Point(50, 100);
            //# Position of select outlets file form
            this.selectOutletFilePos = new Point(50, 100);
            //# Position of select outlets form
            this.selectOutletPos = new Point(50, 100);
            //# Position of visualise form
            this.visualisePos = new Point(0, 100);
            //# options for creating shapefiles
            //this.vectorFileWriterOptions = QgsVectorFileWriter.SaveVectorOptions();
            //this.vectorFileWriterOptions.ActionOnExistingFile = QgsVectorFileWriter.CreateOrOverwriteFile;
            //this.vectorFileWriterOptions.driverName = "ESRI Shapefile";
            //this.vectorFileWriterOptions.fileEncoding = "UTF-8";
            //# rasters open that need to be closed if memory exception occurs
            // only used with hrus2
            // self.openRasters: Set[Raster] = set()  # type: ignore @UndefinedVariable
        }

        // Create subdirectories under project file's directory.
        public void createSubDirectories() {
            Directory.CreateDirectory(this.projDir);
            this.sourceDir = this.forTNC ? Utils.join(this.projDir, "../../DEM") : Utils.join(this.projDir, "Source");
            Directory.CreateDirectory(this.sourceDir);
            this.soilDir = this.forTNC ? Utils.join(this.projDir, "../../Soil") : Utils.join(this.sourceDir, "soil");
            Directory.CreateDirectory(this.soilDir);
            this.landuseDir = this.forTNC ? Utils.join(this.projDir, "../../Landuse") : Utils.join(this.sourceDir, "crop");
            Directory.CreateDirectory(this.landuseDir);
            this.scenariosDir = Utils.join(this.projDir, "Scenarios");
            Directory.CreateDirectory(this.scenariosDir);
            var defaultDir = Utils.join(this.scenariosDir, "Default");
            Directory.CreateDirectory(defaultDir);
            var txtInOutDir = Utils.join(defaultDir, "TxtInOut");
            Directory.CreateDirectory(txtInOutDir);
            var tablesInDir = Utils.join(defaultDir, "TablesIn");
            Directory.CreateDirectory(tablesInDir);
            this.tablesOutDir = Utils.join(defaultDir, Parameters._TABLESOUT);
            Directory.CreateDirectory(this.tablesOutDir);
            this.animationDir = Utils.join(this.tablesOutDir, Parameters._ANIMATION);
            Directory.CreateDirectory(this.animationDir);
            this.pngDir = Utils.join(this.animationDir, Parameters._PNG);
            Directory.CreateDirectory(this.pngDir);
            var watershedDir = Utils.join(this.projDir, "Watershed");
            Directory.CreateDirectory(watershedDir);
            this.textDir = Utils.join(watershedDir, "Text");
            Directory.CreateDirectory(this.textDir);
            this.shapesDir = Utils.join(watershedDir, "Shapes");
            Directory.CreateDirectory(this.shapesDir);
            this.gridDir = Utils.join(watershedDir, "Grid");
            Directory.CreateDirectory(this.gridDir);
            var tablesDir = Utils.join(watershedDir, "Tables");
            Directory.CreateDirectory(tablesDir);
            this.tempDir = Utils.join(watershedDir, "temp");
            Directory.CreateDirectory(this.tempDir);
        }

        // Set vertical conversion factor according to vertical units.
        public void setVerticalFactor() {
            if (this.verticalUnits == Parameters._METRES) {
                this.verticalFactor = 1.0;
            } else if (this.verticalUnits == Parameters._FEET) {
                this.verticalFactor = Parameters._FEETTOMETRES;
            } else if (this.verticalUnits == Parameters._CM) {
                this.verticalFactor = Parameters._CMTOMETRES;
            } else if (this.verticalUnits == Parameters._MM) {
                this.verticalFactor = Parameters._MMTOMETRES;
            } else if (this.verticalUnits == Parameters._INCHES) {
                this.verticalFactor = Parameters._INCHESTOMETRES;
            } else if (this.verticalUnits == Parameters._YARDS) {
                this.verticalFactor = Parameters._YARDSTOMETRES;
            }
        }

        // Set soil lookup table and also, for TNC projects, appropriate usersoil
        public void setSoilTable(string soilTable) {
            this.soilTable = soilTable;
            if (this.forTNC) {
                this.setTNCUsersoil();
            }
        }

        // Set usersoil table for TNC projects.
        public void setTNCUsersoil() {
            if (this.soilTable == Parameters._TNCFAOLOOKUP) {
                this.db.usersoil = Parameters._TNCFAOUSERSOIL;
            } else if (this.soilTable == Parameters._TNCHWSDLOOKUP) {
                this.db.usersoil = Parameters._TNCHWSDUSERSOIL;
            } else {
                Utils.error(String.Format("Inappropriate lookup table {0} for TNC project", this.soilTable), this.isBatch);
            }
        }

        // Return true if landuse is exempt 
        //         or is part of a split of an exempt landuse.
        //         
        public bool isExempt(int landuseId) {
            var luse = this.db.getLanduseCode(landuseId);
            if (this.exemptLanduses.Contains(luse)) {
                return true;
            }
            foreach (KeyValuePair<string, Dictionary<string, int>> entry in this.splitLanduses) {
                if (this.exemptLanduses.Contains(entry.Key) && entry.Value.Keys.Contains(luse)) {
                    return true;
                }
            }
            return false;
        }

        // Save landuse exempt and split details in project database.
        public bool saveExemptSplit() {
            var exemptTable = "LUExempt";
            var splitTable = "SplitHRUs";
            this.db.connect();
            if (this.db.conn is null) {
                return false;
            }
            string clearSql = "DELETE FROM " + exemptTable;
            this.db.execNonQuery(clearSql);
            int oid = 0;
            foreach (var luse in this.exemptLanduses)
            {
                oid += 1;
                string insert = String.Format("({0}, '{1}')", oid, luse);
                this.db.InsertInTable(exemptTable, insert);
            }
            clearSql = "DELETE FROM " + splitTable;
            this.db.execNonQuery(clearSql);
            oid = 0;
            foreach (KeyValuePair<string, Dictionary<string, int>> entry in this.splitLanduses) {
                foreach (KeyValuePair<string, int> entry1 in entry.Value) {
                    oid += 1;
                    string insert = String.Format("({0}, '{1}', '{2}', {3})", oid, entry.Key, entry1.Key, entry1.Value);
                    this.db.InsertInTable(splitTable, insert);
                }
            }
            //conn.commit();
            //if (!(this.isHUC || this.isHAWQS || this.forTNC)) {
            //    this.db.hashDbTable(exemptTable);
            //    this.db.hashDbTable(splitTable);
            //}
            return true;
        }

        // Get landuse exempt and split details from project database.
        public void getExemptSplit() {
            // in case called twice
            this.exemptLanduses = new List<string>();
            this.splitLanduses = new Dictionary<string, Dictionary<string, int>>();
            var exemptTable = "LUExempt";
            var splitTable = "SplitHRUs";
            this.db.connect();
            if (this.db.conn is null) {
                return;
            }
            string sql = DBUtils.sqlSelect(exemptTable, "LANDUSE", "OID", "");
            var reader = DBUtils.getReader(this.db.conn, sql);
            while (reader.Read()) {
                this.exemptLanduses.Add(reader.GetString(0));
            }
            sql = DBUtils.sqlSelect(splitTable, "LANDUSE, SUBLU, PERCENT", "OID", "");
            reader = DBUtils.getReader(this.db.conn, sql);
            while (reader.Read()) {
                var luse = reader.GetString(0);
                if (!this.splitLanduses.ContainsKey(luse)) {
                    this.splitLanduses[luse] = new Dictionary<string, int>();
                }
                this.splitLanduses[luse][reader.GetString(1)] = Convert.ToInt32(reader.GetValue(2));
            }
        }

        // Put currently split landuse codes into combo.
        public void populateSplitLanduses(ComboBox combo) {
            foreach (var luse in this.splitLanduses.Keys) {
                combo.Items.Add(luse);
            }
        }

        // 
        //         Write information to MasterProgress table.
        //         
        //         done parameters may be -1 (leave as is) 0 (not done, default) or 1 (done)
        //         
        public void writeMasterProgress(int doneDelin, int doneSoilLand) {
            this.db.connect();
            if (this.db.conn is null) {
                return;
            }
            string table = "MasterProgress";
            string workdir = this.projDir;
            string gdb = this.projName;
            string swatgdb = this.db.dbRefFile;
            int numLUs = this.db.landuseIds.Count;
            // TODO: properly
            string swatEditorVersion = Parameters._SWATEDITORVERSION;
            string soilOption;
            if (this.db.useSSURGO) {
                soilOption = "ssurgo";
            } else if (this.db.useSTATSGO) {
                soilOption = "stmuid";
            } else if (this.forTNC) {
                soilOption = this.db.usersoil;
            } else {
                soilOption = "name";
            }
            int doneDelinNum;
            int doneSoilLandNum;
            // allow table not to exist for HUC
            var reader = DBUtils.getReader(this.db.conn, DBUtils.sqlSelect(table, "*", "", ""));
            if (reader.HasRows) {
                reader.Read();
                if (doneDelin == -1) {
                    doneDelinNum = Convert.ToInt32(reader.GetValue(8));
                } else {
                    doneDelinNum = doneDelin;
                }
                if (doneSoilLand == -1) {
                    doneSoilLandNum = Convert.ToInt32(reader.GetValue(9));
                } else {
                    doneSoilLandNum = doneSoilLand;
                }
                string sql = "UPDATE " + table + String.Format(" SET SoilOption='{0}',NumLuClasses={1},DoneWSDDel={2},DoneSoilLand={3}", soilOption, numLUs, doneDelinNum, doneSoilLandNum);
                this.db.execNonQuery(sql);
            } else {
                reader.Close();
                reader.Dispose();
                if (doneDelin == -1) {
                    doneDelinNum = 0;
                } else {
                    doneDelinNum = doneDelin;
                }
                if (doneSoilLand == -1) {
                    doneSoilLandNum = 0;
                } else {
                    doneSoilLandNum = doneSoilLand;
                }
                // SWAT Editor 2012.10.19 added a ModelDoneRun field, and we have no data
                // so easiest to make a new table with this field, so we know how many fields to fill
                if (true) { //(this.db.createMasterProgressTable()) {
                    var insert = "(" +
                        DBUtils.quote(workdir) + "," +
                        DBUtils.quote(gdb) + "," +
                        "\"\"," +
                        DBUtils.quote(swatgdb) + "," +
                        "\"\"," +
                        "\"\"," +
                        DBUtils.quote(soilOption) + "," +
                        numLUs.ToString() + "," +
                        doneDelinNum.ToString() + "," +
                        doneSoilLandNum.ToString() + "," +
                        "0, 0, 1, 0, \"\"," +
                        DBUtils.quote(swatEditorVersion) + "," +
                        "\"\", 0)";
                    this.db.InsertInTable(table, insert);
                }
            }
        }

        // Return true if delineation done according to MasterProgress table.
        public bool isDelinDone() {
            this.db.connect();
            if (this.db.conn is null) {
                return false;
            }
            string table = "MasterProgress";
            var reader = DBUtils.getReader(this.db.conn, DBUtils.sqlSelect(table, "DoneWSDDel", "", ""));
            if (!reader.HasRows) return false;
            //object[] vals = new object[20];
            reader.Read();
            //reader.GetValues(vals);
            //return (short)vals[0] == 1;
            return Convert.ToInt32(reader.GetValue(0)) == 1;
        }

        // Return true if HRU creation is done according to MasterProgress table.
        public bool isHRUsDone() {
            this.db.connect();
            if (this.db.conn is null) {
                    return false;
            }
            string table = "MasterProgress";
            var reader = DBUtils.getReader(this.db.conn, DBUtils.sqlSelect(table, "DoneSoilLand", "", ""));
            if (!reader.HasRows) return false;
            reader.Read();
            return Convert.ToInt32(reader.GetValue(0)) == 1;
        }

        // Save SWAT Editor initial parameters in its configuration file.
        public void setSWATEditorParams() {
            string soilDb;
            var path = this.SWATEditorPath + ".config";
            XElement config = XElement.Load(path);

            var projDbKey = "SwatEditor_ProjGDB";
            var refDbKey = "SwatEditor_SwatGDB";
            var soilDbKey = "SwatEditor_SoilsGDB";
            var exeKey = "SwatEditor_SwatEXE";
            foreach (XElement item in config.Descendants("add"))
            {
                string key = (string)item.Attribute("key");
                if (key == projDbKey)
                {
                    item.SetAttributeValue("value", this.db.dbFile);
                } else if (key == refDbKey) {
                        item.SetAttributeValue("value", this.db.dbRefFile);
                    } else if (key == soilDbKey) {
                    // in case this is a restart need to get SSURGO setting from project config data
                    var found = false;
                    Proj proj = this.proj;
                    var title = this.projName;
                    (this.db.useSSURGO, found) = proj.readBoolEntry(title, "soil/useSSURGO", false);
                    if (this.db.useSSURGO) {
                            soilDb = Parameters._SSURGODB;
                        } else {
                            soilDb = Parameters._USSOILDB;
                        }
                        item.SetAttributeValue("value", Utils.join(this.SWATExeDir + "Databases", soilDb));
                    } else if (key == exeKey) {
                        item.SetAttributeValue("value", this.SWATExeDir);
                    }
                }
            config.Save(path);
        }
    }
}

