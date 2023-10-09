

using System;
using System.IO;

using System.Collections;

using System.Collections.Generic;

namespace ArcSWAT3
{
        public class Parameters
        {

            public static bool useSlowPolygonize = false;

            public  bool isBatch;

            public  string mpiexecDir;

            public  string numProcesses;

            public  string SWATEditorDir;

            public GlobalVars _gv;

            public ParamForm _dlg;

            public static string _SWATEDITOR = "SwatEditor.exe";

            public static string _SWATEDITORDEFAULTDIR = "C:\\SWAT\\SWATEditor";

            public static string _SWATEDITORVERSION = "2012.10.19";

            public static string _MPIEXEC = "mpiexec.exe";

            public static string _MPIEXECDEFAULTDIR = "C:\\Program Files\\Microsoft MPI\\Bin";

            public static string _TAUDEMDIR = "TauDEM5Bin";

            public static string _TAUDEM539DIR = "TauDEM539Bin";

            public static string _TAUDEMHELP = "TauDEM_Tools.chm";

            public static string _SWATGRAPH = "SWATGraph";

            public static string _DBDIR = "Databases";

            public static string _DBPROJ = "ArcSWATProj2012.mdb";

            public static string _DBREF = "QSWATRef2012.mdb";

            public static string _TNCDBPROJ = "QSWATProj2012_TNC.sqlite";

            public static string _TNCDBREF = "QSWATRef2012_TNC.sqlite";

            public static string _TNCFAOLOOKUP = "FAO_soils_TNC";

            public static string _TNCFAOUSERSOIL = "usersoil_FAO";

            public static string _TNCHWSDLOOKUP = "HWSD_soils_TNC";

            public static string _TNCHWSDUSERSOIL = "usersoil_HWSD";

            public static string _ACCESSSTRING = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=";

            public static string _TABLESOUT = "TablesOut";

            public static string _ANIMATION = "Animation";

            public static string _PNG = "Png";

            public static string _STILLBASE = "still.gif";

            public static string _STILLJPG = "still.jpg";

            public static string _STILLPNG = "still.png";

            public static string _TXTINOUT = "TxtInOut";

            public static string _SSURGODB = "SWAT_US_SSURGO_Soils.mdb";

            public static string _SSURGODB_HUC = "SSURGO_Soils_HUC.sqlite";

            public static int _SSURGOWater = 377988;

            public static string _WATERBODIES = "WaterBodies.sqlite";

            public static string _USSOILDB = "SWAT_US_Soils.mdb";

            public static string _CIO = "file.cio";

            public static string _OUTPUTDB = "SWATOutput.mdb";

            public static string _SUBS = "subs";

            public static string _RIVS = "rivs";

            public static string _HRUS = "hrus";

            public static string _SUBS1 = "subs1";

            public static string _RIV1 = "riv1";

            public static string _HRU0 = "hru0";

            public static string _HRUS1 = "hrus1";

            public static string _HRUS2 = "hrus2";

            public static string _HRUSRASTER = "hrus.tif";

            public static string _HRUSCSV = "hrus.csv";

            public static string _TOPOREPORT = "TopoRep.txt";

            public static string _TOPOITEM = "Elevation";

            public static string _BASINREPORT = "LanduseSoilSlopeRepSwat.txt";

            public static string _BASINITEM = "Landuse and Soil";

            public static string _HRUSREPORT = "HruLanduseSoilSlopeRepSwat.txt";

            public static string _ARCHRUSREPORT = "HRULandUseSoilsReport.txt";

            public static string _ARCBASINREPORT = "LandUseSoilsReport.txt";

            public static string _HRUSITEM = "HRUs";

            public static string _USECSV = "Use csv file";

            public static string _LANDUSE = "LANDUSE";

            public static string _SOIL = "SOIL";

            public static string _SLOPEBAND = "SLOPE_BAND";

            public static string _AREA = "AREA (ha)";

            public static string _PERCENT = "%SUBBASIN";

            public static string _SQKM = "sq. km";

            public static string _HECTARES = "hectares";

            public static string _SQMETRES = "sq. metres";

            public static string _SQMILES = "sq. miles";

            public static string _ACRES = "acres";

            public static string _SQFEET = "sq. feet";

            public static string _METRES = "metres";

            public static string _FEET = "feet";

            public static string _CM = "centimetres";

            public static string _MM = "millimetres";

            public static string _INCHES = "inches";

            public static string _YARDS = "yards";

            public static string _DEGREES = "degrees";

            public static string _UNKNOWN = "unknown";

            public static double _FEETTOMETRES = 0.3048;

            public static double _CMTOMETRES = 0.01;

            public static double _MMTOMETRES = 0.001;

            public static double _INCHESTOMETRES = 0.0254;

            public static double _YARDSTOMETRES = 0.91441;

            public static double _SQMILESTOSQMETRES = 2589988.1;

            public static double _ACRESTOSQMETRES = 4046.8564;

            public static double _SQMETRESTOSQFEET = 10.76391;

            public static int _RIV1SUBS1MAX = 1000;

            public static double _NEARNESSTHRESHOLD = 0.1;

            public static HashSet<string> _WATERLANDUSES = new HashSet<string> {
            "WATR",
            "WETN",
            "WETF",
            "RIWF",
            "UPWF",
            "RIWN",
            "UPWN"
        };

            public static double _WATERMAXSLOPE = 1E-05;

            public static HashSet<string> _TNCWATERLANDUSES = new HashSet<string> {
            "WEWO",
            "WETL",
            "WEHB",
            "WATR",
            "TWEW",
            "TWET",
            "TWEH"
        };

            public static int _TNCFAOWATERSOIL = 6997;

            public static HashSet<int> _TNCFAOWATERSOILS = new HashSet<int> {
            6997,
            1972
        };

            public static int _TNCHWSDWATERSOIL = 39997;

            public static HashSet<int> _TNCHWSDWATERSOILS = new HashSet<int> {
            39997,
            34972
        };

            public static double _GRIDDEFAULTSLOPE = 0.005;

            public static double _RICEMAXSLOPE = 0.005;

            public static Dictionary<string, ValueTuple<double, double, double, double>> TNCExtents = new Dictionary<string, ValueTuple<double, double, double, double>> {
            {
                "CentralAmerica",
                (-92.3, 7.2, -59.4, 23.2)},
            {
                "NorthAmerica",
                (-178.3, 0, -29.9, 60.0)},
            {
                "SouthAmerica",
                (-109.7, -56.2, -34.5, 12.9)},
            {
                "Australia",
                (89.9, -55.0, 180, 0)},
            {
                "Africa",
                (-25.6, -35.1, 60.1, 37.6)},
            {
                "Europe",
                (-31.6, 34.3, 69.9, 60.0)},
            {
                "Asia",
                (-0.1, -11.1, 180, 60.0)}};

            public static string wgnDb = "swatplus_wgn.sqlite";

            public static string CHIRPSDir = "CHIRPS";

            public static Dictionary<string, List<string>> CHIRPSStationsCsv = new Dictionary<string, List<string>> {
            {
                "CentralAmerica",
                new List<string> {
                    "north_america_grids.csv",
                    "south_america_grids.csv"
                }},
            {
                "NorthAmerica",
                new List<string> {
                    "north_america_grids.csv"
                }},
            {
                "SouthAmerica",
                new List<string> {
                    "south_america_grids.csv"
                }},
            {
                "Australia",
                new List<string> {
                    "australia_grids.csv",
                    "asia_02_grids.csv"
                }},
            {
                "Africa",
                new List<string> {
                    "africa_grids.csv"
                }},
            {
                "Europe",
                new List<string> {
                    "europe_grids.csv",
                    "asia_01_grids.csv",
                    "asia_02_grids.csv"
                }},
            {
                "Asia",
                new List<string> {
                    "europe_grids.csv",
                    "asia_01_grids.csv",
                    "asia_02_grids.csv"
                }}};

            public static string ERA5Dir = "ERA5";

            public static string ERA5GridsDir = "Weather";

            public static Dictionary<string, List<string>> ERA5StationsCsv = new Dictionary<string, List<string>> {
            {
                "CentralAmerica",
                new List<string> {
                    "north_america_grids.csv",
                    "south_america_grids.csv"
                }},
            {
                "NorthAmerica",
                new List<string> {
                    "north_america_grids.csv"
                }},
            {
                "SouthAmerica",
                new List<string> {
                    "south_america_grids.csv"
                }},
            {
                "Australia",
                new List<string> {
                    "australia_grids.csv",
                    "asia_grids.csv"
                }},
            {
                "Africa",
                new List<string> {
                    "africa_grids.csv"
                }},
            {
                "Europe",
                new List<string> {
                    "europe_grids.csv"
                }},
            {
                "Asia",
                new List<string> {
                    "europe_grids.csv",
                    "asia_grids.csv"
                }}};


            public Parameters(GlobalVars gv)
            {
                //var settings = QSettings();
                //# SWAT Editor directory
                //this.SWATEditorDir = settings.value("/QSWAT/SWATEditorDir", _SWATEDITORDEFAULTDIR);
                this.SWATEditorDir = _SWATEDITORDEFAULTDIR;
                //# mpiexec directory
                //this.mpiexecDir = settings.value("/QSWAT/mpiexecDir", _MPIEXECDEFAULTDIR);
                this.mpiexecDir = _MPIEXECDEFAULTDIR;
                //# number of MPI processes
                //this.numProcesses = settings.value("/QSWAT/NumProcesses", "");
                this.numProcesses = "";
                this._gv = gv;
                this._dlg = new ParamForm(gv, this.SWATEditorDir, this.mpiexecDir);
                //this._dlg.setWindowFlags(this._dlg.windowFlags() & ~Qt.WindowContextHelpButtonHint);
                if (this._gv is not null)
                {
                    //this._dlg.move(this._gv.parametersPos);
                    //# flag showing if batch run
                    this.isBatch = this._gv.isBatch;
                }
                else
                {
                    this.isBatch = false;
                }
            }

            public void run() {
                this._dlg.ShowDialog();
            }
        }
    }