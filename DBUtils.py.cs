

using System.Collections.Generic;

using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SQLite;

using System.IO;
using System.Linq;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ArcSWAT3
{


    // Functions for interacting with project and reference databases.
    public class DBUtils {
        
        public List<string> _allTableNames;
        
        public string _connRefStr;
        
        public string _connStr;
        
        public Dictionary<int, int> _landuseIDCs;
        
        public Dictionary<int, int> _landuseTranslate;
        
        public List<int> _undefinedLanduseIds;
        
        public List<int> _undefinedSoilIds;

        public bool useSQLite;

        public DbConnection conn;

        public DbConnection connRef;
        
        public string dbFile;
        
        public string dbRefFile;
        
        public int defaultLanduse;
        
        public string defaultLanduseCode;
        
        public int defaultSoil;
        
        public string defaultSoilName;
        
        public bool forTNC;
        
        public bool isBatch;
        
        public bool isHAWQS;
        
        public bool isHUC;
        
        public Dictionary<int, string> landuseCodes;
        
        public bool landuseErrorReported;
        
        public Dictionary<int, int> landuseIds;
        
        public Dictionary<int, double> landuseOVN;
        
        public List<string> landuseTableNames;
        
        public List<int> landuseVals;
        
        public string logFile;
        
        public string projDir;
        
        public string projName;
        
        public List<double> slopeLimits;
        
        public Dictionary<int, string> soilNames;
        
        public List<string> soilTableNames;
        
        public Dictionary<int, int> soilTranslate;
        
        public List<int> soilVals;
        
        public DbConnection SSURGOConn;
        
        public string SSURGODbFile;
        
        public Dictionary<int, int> SSURGOSoils;
        
        public int SSURGOUndefined;
        
        public Dictionary<int, int> urbanIds;
        
        public string usersoil;
        
        public bool useSSURGO;
        
        public bool useSTATSGO;
        
        public string waterBodiesFile;
        
        public DBUtils(
            string projDir,
            string projName,
            string dbProjTemplate,
            string dbRefTemplate,
            bool isHUC,
            bool isHAWQS,
            bool forTNC,
            string logFile,
            bool isBatch) {
            //# Flag showing if batch run
            this.isBatch = isBatch;
            //# flag for HUC projects
            this.isHUC = isHUC;
            //# flag for HAWQS projects
            this.isHAWQS = isHAWQS;
            //# flag for TNC projects
            this.forTNC = forTNC;
            //# message logging file for HUC projects
            this.logFile = logFile;
            // flag to use SQLite databases
            this.useSQLite = false; // isHUC || isHAWQS || forTNC
            //# project directory
            this.projDir = projDir;
            //# project name
            this.projName = projName;
            //# project database
            var dbSuffix = Path.GetExtension(dbProjTemplate);
            this.dbFile = this.useSQLite ? Utils.join(projDir, projName + ".sqlite") : Utils.join(projDir, projName + dbSuffix);
            if (forTNC && !File.Exists(this.dbFile)) {
                File.Copy(dbProjTemplate, this.dbFile);
            }
            if (!(this.useSQLite)) {
                this._connStr = Parameters._ACCESSSTRING + this.dbFile;
                // copy template project database to project folder if not already there
                if (!File.Exists(this.dbFile)) {
                    File.Copy(dbProjTemplate, this.dbFile);
                } 
                //else {
                //    this.updateProjDb(Parameters._SWATEDITORVERSION);
                //}
            }
            //# reference database
            var dbRefName = this.useSQLite ? "QSWATRef2012.sqlite" : Parameters._DBREF;
            this.dbRefFile = Utils.join(projDir, dbRefName);
            if (this.useSQLite) {
                if (isHUC && !File.Exists(this.dbRefFile)) {
                    // look one up from project directory for reference database, so allowing it to be shared
                    this.dbRefFile = Utils.join(projDir + "/..", dbRefName);
                } else if (!File.Exists(this.dbRefFile)) {
                    File.Copy(dbRefTemplate, this.dbRefFile);
                }
                if (!File.Exists(this.dbRefFile)) {
                    Utils.error(String.Format("Failed to find reference database {0}", this.dbRefFile), this.isBatch);
                    return;
                }
                try {
                    this.connRef = new SQLiteConnection(String.Format("Data Source={0}", this.dbRefFile));
                    if (this.connRef is null) {
                        Utils.error(String.Format("Failed to connect to reference database {0}.", this.dbRefFile), this.isBatch);
                    }
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Failed to connect to reference database {0}: {1}", this.dbRefFile, ex.Message), this.isBatch);
                    this.connRef = null;
                }
            } else {
                this._connRefStr = Parameters._ACCESSSTRING + this.dbRefFile;
                // copy template reference database to project folder if not already there
                if (!File.Exists(this.dbRefFile)) {
                    File.Copy(dbRefTemplate, this.dbRefFile);
                }
                //# reference database connection
                try {
                    this.connRef = new OleDbConnection(this._connRefStr);
                    if (this.connRef is null) {
                        Utils.error(String.Format("Failed to connect to reference database {0}", this.dbRefFile), this.isBatch);
                    } 
                    //else {
                    //    this.updateRefDb(Parameters._SWATEDITORVERSION, dbRefTemplate);
                    //}
                } catch (Exception ex) {
                    Utils.error(String.Format("Failed to connect to reference database {0}: {1}", this.dbRefFile, ex.Message), this.isBatch);
                    this.connRef = null;
                }
            }
            //# WaterBodies (HUC and HAWQS only)
            this.waterBodiesFile = isHUC ? Utils.join(projDir + "/..", Parameters._WATERBODIES) : Utils.join(projDir, Parameters._WATERBODIES);
            //# Tables in project database containing 'landuse'
            this.landuseTableNames = new List<string>();
            //# Tables in project database containing 'soil'
            this.soilTableNames = new List<string>();
            //# all tables names in project database
            this._allTableNames = new List<string>();
            //# map of landuse category to SWAT landuse code
            this.landuseCodes = new Dictionary<int, string>();
            //# Landuse categories may not translate 1-1 into SWAT codes.
            //
            // This map is used to map category ids into equivalent ids.
            // Eg if we have [0 +> XXXX, 1 +> YYYY, 2 +> XXXX, 3 +> XXXX] then _landuseTranslate will be
            // [2 +> 0, 3 +> 0] showing that 2 and 3 map to 0, and other categories are not changed.
            // Only landuse categories 0 and 1 are then used to calculate HRUs, i.e. landuses 0, 2 and 3 will 
            // contribute to the same HRUs.
            // There is an invariant that the domains of landuseCodes and _landuseTranslate are disjoint,
            // and that the range of _landuseTranslate is a subset of the domain of landuseCodes.
            this._landuseTranslate = new Dictionary<int, int>();
            //# Map of landuse category to SWAT crop ids (as found in crop.dat,
            // or 0 for urban)
            //
            // There is an invariant that the domains of landuseCodes and landuseIds are identical.
            this.landuseIds = new Dictionary<int, int>();
            //# List of undefined landuse categories.  Retained so each is only reported once as an error in each run.
            this._undefinedLanduseIds = new List<int>();
            //# Map of landuse category to IDC value from crop table
            // There is an invariant that the domains of landuseCodes and _landuseIDCs are identical.
            this._landuseIDCs = new Dictionary<int, int>();
            //# map of landuse categories to Mannings n value for overland flow (only used for HUC models)
            // same domain as landuseCodes
            this.landuseOVN = new Dictionary<int, double>();
            //# Map of landuse category to SWAT urban ids (as found in urban.dat)
            // There is an invariant that the domain of urbanIds is a subset of 
            // the domain of landuseIds, corresponding to those whose crop id is 0
            this.urbanIds = new Dictionary<int, int>();
            //# Sorted list of values occurring in landuse map
            this.landuseVals = new List<int>();
            //# Default landuse
            //# Set to first landuse in lookup table or landuse with 0 landuse id and used to replace landuse nodata when using grid model
            this.defaultLanduse = -1;
            //# defaultLanduse code
            this.defaultLanduseCode = "";
            //# flag to prevent multiple landuse errors being reported as errors: subsequent ones in log
            this.landuseErrorReported = false;
            //# Map of soil id  to soil name
            this.soilNames = new Dictionary<int, string>();
            //# Soil categories may not translate 1-1 into soils.
            //
            // This map is used to map category ids into equivalent ids.
            // Eg if we have [0 +> XXXX, 1 +> YYYY, 2 +> XXXX, 3 +> XXXX] then soilTranslate will be
            // [2 +> 0, 3 +> 0] showing that 2 and 3 map to 0, and other categories are not changed.
            // Only soil ids 0 and 1 are then used to calculate HRUs, i.e. soils 0, 2 and 3 will 
            // contribute to the same HRUs.
            // There is an invariant that the domains of soilNames and soilTranslate are disjoint,
            // and that the range of soilTranslate is a subset of the domain of soilNames.
            this.soilTranslate = new Dictionary<int, int>();
            //# List of undefined soil identifiers.  Retained so each is only reported once as an error in each run.
            this._undefinedSoilIds = new List<int>();
            //# Sorted list of values occurring in soil map
            this.soilVals = new List<int>();
            //# Default soil
            //# Set to first soil in lookup table or soil with 0 soil id and used to replace soil nodata when using grid model
            this.defaultSoil = -1;
            //# name of defaultSoil
            this.defaultSoilName = "";
            //# name of usersoil table: can be changed for TNC projects
            this.usersoil = "usersoil";
            //# List of limits for slopes.
            //
            // A list [a,b] means slopes are in ranges [slopeMin,a), [a,b), [b,slopeMax] 
            // and these ranges would be indexed by slopes 0, 1 and 2.
            this.slopeLimits = new List<double>();
            //# flag indicating STATSGO soil data is being used
            this.useSTATSGO = false;
            //# flag indicating SSURGO or STATSGO2 soil data is being used
            this.useSSURGO = false;
            //# map of SSURGO map values to SSURGO MUID (only used with HUC)
            this.SSURGOSoils = new Dictionary<int, int>();
            //if (isHUC || isHAWQS) {
            //    //# SSURGO soil database (only used with HUC and HAWQS)
            //    if (isHUC) {
            //        // changed to use copy one up frpm projDir
            //        this.SSURGODbFile = Utils.join(this.projDir + "/..", Parameters._SSURGODB_HUC);
            //    } else {
            //        this.SSURGODbFile = Utils.join(Path.GetDirectoryName(dbRefTemplate), Parameters._SSURGODB_HUC);
            //    }
            //    this.SSURGOConn = new SQLiteConnection("Data Source=" + this.SSURGODbFile);
            //    this.SSURGOConn.Open();
            //}
            //# nodata value from soil map to replace undefined SSURGO soils (only used with HUC and HAWQS)
            this.SSURGOUndefined = -1;
            //if (this.isHUC) {
            //    this.writeSubmapping();
            //}
        }
        
        // Connect to project database.
        public void connect() {
            if (!File.Exists(this.dbFile)) {
                Utils.error(String.Format("Cannot find project database {0}.  Have you opened the project?", this.dbFile), this.isBatch);
            }
            try
            {
                if (this.conn is null)
                {
                    if (this.useSQLite) {
                        this.conn = new SQLiteConnection("Data Source=" + this.dbFile);
                    } else {
                        this.conn = new OleDbConnection(this._connStr);
                    }
                    if (this.conn is null)
                    {
                        Utils.error(String.Format("Failed to connect to project database {0}.", this.dbFile), this.isBatch);
                        return;
                    }
                }
                if (this.conn.State != System.Data.ConnectionState.Open) this.conn.Open();
            } catch (Exception ex) {
                Utils.error(String.Format("Failed to connect to project database {0}: {1}.", this.dbFile, ex.Message), this.isBatch);
            }
        }
        
        //===========no longer used================================================================
        // def connectRef(self, readonly=False) -> Any:
        //     
        //     """Connect to reference database."""
        //     
        //     if not File.Exists(self.dbRefFile):
        //         Utils.error('Cannot find reference database {0}', self.dbRefFile), self.isBatch)
        //         return None 
        //     try:
        //         if self.isHUC or self.isHAWQS or self.forTNC:
        //             conn = sqlite3.connect(self.dbRefFile)  # @UndefinedVariable
        //             conn.row_factory = sqlite3.Row  # @UndefinedVariable
        //         elif readonly:
        //             conn = pyodbc.connect(self._connRefStr, readonly=True)
        //         else:
        //             # use autocommit when writing to tables, hoping to save storing rollback data
        //             conn = pyodbc.connect(self._connRefStr, autocommit=True)
        //         if conn:
        //             return conn
        //         else:
        //             Utils.error('Failed to connect to reference database {0}.\n{1}', self.dbRefFile, self.connectionProblem()), self.isBatch)
        //     except Exception:
        //         Utils.error('Failed to connect to reference database {0}: {1}.\n{2}', self.dbRefFile, ex.Message,self.connectionProblem()), self.isBatch)
        //     return None
        //===========================================================================
        // Connect to database db.
        public DbConnection connectDb(string db) {
            DbConnection conn;
            if (!File.Exists(db)) {
                Utils.error(String.Format("Cannot find database {0}", db), this.isBatch);
                return null;
            }
            var refStr = Parameters._ACCESSSTRING + db;
            try {
                conn = new OleDbConnection(refStr);
                if (conn is not null) {
                    return conn;
                } else {
                    Utils.error(String.Format("Failed to connect to database {0}", db), this.isBatch);
                }
            } catch (Exception ex) {
                Utils.error(String.Format("Failed to connect to database {0}: {1}", db, ex.Message), this.isBatch);
            }
            return null;
        }
        

        
        // Return true if project database table exists and has data.
        public bool hasData(string table) {
            try {
                this.connect();
                string sql = sqlSelect(table, "*", "", "");
                using (var reader = getReader(this.conn, sql)) {
                    var result = reader.HasRows;
                    return result;
                }
            } catch (Exception) {
                return false;
            }
        }
        
        // Clear table of data.
        public void clearTable(string table) {
            try {
                execNonQuery("DELETE FROM " + table);
            } catch (Exception) {
                // since purpose is to make sure any data in table is not accessible
                // ignore problems such as table not existing
            }
        }

        public void execNonQuery(string sql) {
            this.connect();
            int res = 0;
            if (this.conn is OleDbConnection)
            {
                var cmd = new OleDbCommand(sql, (OleDbConnection)conn);
                res = cmd.ExecuteNonQuery();
            } else {
                var cmd = new SQLiteCommand(sql, (SQLiteConnection)conn);
                res = cmd.ExecuteNonQuery();
            }
        }

        // Return true if table exists in db
        public bool tableExists(string table, string db) {
            var conn = this.connectDb(db);
            if (conn is null) return false;
            bool needToClose = false;
            if (conn.State != ConnectionState.Open) {
                conn.Open();
                needToClose = true;
            }
            var schema = conn.GetSchema("TABLES");
            var myTable = schema.Select(String.Format("TABLE_NAME='{0}'", table));
            if (needToClose) {
                conn.Close();
            }
            return myTable.Length > 0;
        }
        
        // Create SQL select statement.
        public static string sqlSelect(string table, string selection, string order, string where) {
            var orderby = order == "" ? "" : " ORDER BY " + order;
            var select = "SELECT " + selection + " FROM " + table + orderby;
            var result = where == "" ? select : select + " WHERE " + where;
            return result;
        }
        
        ////  SWAT Editor 2012.10_2.18 renamed ElevationBands to ElevationBand.
        //public virtual object updateProjDb(string SWATEditorVersion) {
        //    if (SWATEditorVersion == "2012.10_2.18" || SWATEditorVersion == "2012.10.19") {
        //        using (var conn = this.connect()) {
        //            try {
        //                cursor = conn.cursor();
        //                hasElevationBand = false;
        //                hasElevationBands = false;
        //                foreach (var row in cursor.tables(tableType: "TABLE")) {
        //                    table = row.table_name;
        //                    if (table == "ElevationBand") {
        //                        hasElevationBand = true;
        //                    } else if (table == "ElevationBands") {
        //                        hasElevationBands = true;
        //                    }
        //                }
        //                if (hasElevationBands && !hasElevationBand) {
        //                    sql = "SELECT * INTO ElevationBand FROM ElevationBands";
        //                    cursor.execute(sql);
        //                    Utils.loginfo("Created ElevationBand");
        //                    sql = "DROP TABLE ElevationBands";
        //                    cursor.execute(sql);
        //                    Utils.loginfo("Deleted ElevationBands");
        //                }
        //            } catch (Exception ex) {
        //                Utils.error(String.Format("Could not update table in project database {0}: {1}", this.dbFile, ex.Message), this.isBatch);
        //                return;
        //            }
        //        }
        //    }
        //}
        
        ////  SWAT Editor 2012.10_2.18 renamed ElevationBandsrng to ElevationBandrng and added tblOutputVars.
        //public virtual object updateRefDb(string SWATEditorVersion, string dbRefTemplate) {
        //    object sql;
        //    if (SWATEditorVersion == "2012.10_2.18" || SWATEditorVersion == "2012.10.19") {
        //        try {
        //            var cursor = this.connRef.cursor();
        //            var hasElevationBandrng = false;
        //            var hasElevationBandsrng = false;
        //            var hasTblOututVars = false;
        //            foreach (var row in cursor.tables(tableType: "TABLE")) {
        //                var table = row.table_name;
        //                if (table == "ElevationBandrng") {
        //                    hasElevationBandrng = true;
        //                } else if (table == "ElevationBandsrng") {
        //                    hasElevationBandsrng = true;
        //                } else if (table == "tblOutputVars") {
        //                    hasTblOututVars = true;
        //                }
        //            }
        //            if (!hasElevationBandrng) {
        //                sql = "SELECT * INTO ElevationBandrng FROM [MS Access;DATABASE=" + dbRefTemplate + "].ElevationBandrng";
        //                cursor.execute(sql);
        //                Utils.loginfo("Created ElevationBandrng");
        //            }
        //            if (hasElevationBandsrng) {
        //                sql = "DROP TABLE ElevationBandsrng";
        //                cursor.execute(sql);
        //                Utils.loginfo("Deleted ElevationBandsrng");
        //            }
        //            if (!hasTblOututVars) {
        //                sql = "SELECT * INTO tblOutputVars FROM [MS Access;DATABASE=" + dbRefTemplate + "].tblOutputVars";
        //                cursor.execute(sql);
        //                Utils.loginfo("Created tblOutputVars");
        //            }
        //        } catch (Exception ex) {
        //            Utils.error(String.Format("Could not update tables in reference database {0}: {1}", this.dbRefFile, ex.Message), this.isBatch);
        //            return;
        //        }
        //    }
        //}

        // return reader for sql based on tye of connection conn
        public static DbDataReader getReader(DbConnection conn, string sql)
        {
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            if (conn is SQLiteConnection) {
                var cmd = new SQLiteCommand(sql, (SQLiteConnection)conn);
                return cmd.ExecuteReader() as SQLiteDataReader;
            }
            { 
                var cmd = new OleDbCommand(sql, (OleDbConnection)conn);
                return cmd.ExecuteReader() as OleDbDataReader;
            }
        }
        
        // Collect table names from project database.
        public void populateTableNames() {
            this.landuseTableNames = new List<string>();
            this.soilTableNames = new List<string>();
            this._allTableNames = new List<string>();
            try
            {
                this.connect();
                if (this.conn is SQLiteConnection) {
                    string sql = "SELECT name FROM sqlite_master WHERE TYPE='table'";
                    var cmd = new SQLiteCommand(sql, (SQLiteConnection)this.conn);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.HasRows) {
                            while (reader.Read()) {
                                string table = reader.GetString(0);
                                if (table.Contains("landuse") && !table.Contains("config_landuse")) {
                                    this.landuseTableNames.Add(table);
                                } else if (table.Contains("soil") && !table.Contains("usersoil") &&
                                            !table.Contains("config_soil")) {
                                    this.soilTableNames.Add(table);
                                }
                                this._allTableNames.Add(table);
                            }
                        }
                    }
                } else {
                    var schema = ((OleDbConnection)this.conn).GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                    foreach (var row in schema.Rows.OfType<DataRow>())
                    {
                        string table = row.ItemArray[2].ToString();
                        if (table.Contains("landuse") && !table.Contains("config_landuse"))
                        {
                            this.landuseTableNames.Add(table);
                        }
                        else if (table.Contains("soil") && !table.Contains("usersoil") &&
                                        !table.Contains("config_soil"))
                        {
                            this.soilTableNames.Add(table);
                        }
                        this._allTableNames.Add(table);
                    }
                }
            } catch (Exception) {
                Utils.error(String.Format("Could not read tables in project database {0}", this.dbFile), this.isBatch);
            }
        }

        // Collect landuse codes from landuseTable and create lookup tables.
        public bool populateLanduseCodes(string landuseTable) { 
            var OK = true;
            this.landuseCodes.Clear();
            var revLanduseCodes = new Dictionary<string, int>();
            this._landuseTranslate.Clear();
            this.landuseIds.Clear();
            this._landuseIDCs.Clear();
            this.landuseOVN.Clear();
            this.urbanIds.Clear();
            this.connect();
            string sql = sqlSelect(landuseTable, "LANDUSE_ID, SWAT_CODE", "", "");
            using (DbDataReader reader = getReader(this.conn, sql)) {
                try {
                    if (reader.HasRows) {
                        while (reader.Read()) {
                            int nxt = Convert.ToInt32(reader.GetValue(0));
                            string landuseCode = reader.GetString(1);
                            if (nxt == 0 || this.defaultLanduse < 0) {
                                this.defaultLanduse = nxt;
                                this.defaultLanduseCode = landuseCode;
                            }
                            // check if code already defined
                            int val = -1;
                            if (revLanduseCodes.TryGetValue(landuseCode, out val)) {
                                this.storeLanduseTranslate(nxt, val);
                            } else {
                                // landuseCode was not already defined
                                if (!this.storeLanduseCode(nxt, landuseCode)) {
                                    OK = false;
                                }
                                revLanduseCodes[landuseCode] = nxt;
                            }
                        }
                        Utils.loginfo(String.Format("Default landuse set to {0}", this.defaultLanduseCode));
                    }
                    return OK;
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Could not read table {0} in project database {1}: {2}", landuseTable, this.dbFile, ex.Message), this.isBatch);
                    return false;
                }
            }
        }
        
        // Make key lid equivalent to key equiv, 
        //         where equiv is a key in landuseCodes.
        public void storeLanduseTranslate(int lid, int equiv) {
            if (!this._landuseTranslate.ContainsKey(lid)) {
                this._landuseTranslate[lid] = equiv;
            }
        }
        
        // Translate a landuse id to its equivalent lid 
        //         in landuseCodes, if any.
        //         
        public int translateLanduse(int lid) {
            ListFuns.insertIntoSortedIntList(lid, this.landuseVals, true);
            int val = -1;
            if (this._landuseTranslate.TryGetValue(lid, out val))
            { return val; } else { return lid; }
        }
        
        // Store landuse codes in lookup tables.
        public virtual bool storeLanduseCode(int landuseCat, string landuseCode)
        {
            if (this.connRef is null)
            {
                return false;
            }
            string sql2;
            string table;
            var landuseIDC = 0;
            var landuseOVN = 0.0;
            var landuseId = 0;
            var urbanId = -1;
            var OK = true;
            var isUrban = landuseCode.StartsWith("U");
            DbDataReader reader = null;
            if (isUrban) {
                table = "urban";
                sql2 = sqlSelect(table, "IUNUM, OV_N", "", String.Format("URBNAME='{0}'", landuseCode));
                try {
                    reader = getReader(this.connRef, sql2);
                    if (reader.HasRows)
                    {
                        reader.Read();
                        urbanId = Convert.ToInt32(reader.GetValue(0));
                        landuseOVN = Convert.ToDouble(reader.GetValue(1));
                    }
                } catch (Exception ex) {
                    Utils.error(String.Format("Could not read table {0} in reference database {1}: {2}", table, this.dbRefFile, ex.Message), this.isBatch);
                    return false;
                } finally {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            if (urbanId < 0) {
                // not tried or not found in urban
                table = "crop";
                sql2 = sqlSelect(table, "ICNUM, IDC, OV_N", "", String.Format("CPNM='{0}'", landuseCode));
                try {
                    reader = getReader(this.connRef, sql2);
                    if (reader.HasRows)
                    {
                        reader.Read();
                        urbanId = Convert.ToInt32(reader.GetValue(0));
                        landuseIDC = Convert.ToInt32(reader.GetValue(1));
                        landuseOVN = reader.GetDouble(2);
                    }
                    else
                    {
                        if (isUrban)
                        {
                            if (this.landuseErrorReported)
                            {
                                Utils.loginfo(String.Format("No data for landuse {0} in reference database tables urban or {1}", landuseCode, table));
                            }
                            else
                            {
                                Utils.error(String.Format("No data for landuse {0} (and perhaps others) in reference database tables urban or {1}", landuseCode, table), this.isBatch);
                                this.landuseErrorReported = true;
                            }
                        }
                        else if (this.landuseErrorReported)
                        {
                            Utils.loginfo(String.Format("No data for landuse {0} in reference database table {1}", landuseCode, table));
                        }
                        else
                        {
                            Utils.error(String.Format("No data for landuse {0} (and perhaps others) in reference database table {1}", landuseCode, table), this.isBatch);
                            this.landuseErrorReported = true;
                            //OK = False
                        }
                    }
                } catch (Exception ex) {
                    Utils.error(String.Format("Could not read table {0} in reference database {1}: {2}", table, this.dbRefFile, ex.Message), this.isBatch);
                    return false;
                } finally {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            this.landuseCodes[landuseCat] = landuseCode;
            this.landuseIds[landuseCat] = landuseId;
            this._landuseIDCs[landuseCat] = landuseIDC;
            this.landuseOVN[landuseCat] = landuseOVN;
            if (urbanId >= 0) {
                this.urbanIds[landuseCat] = urbanId;
            }
            return OK;
        }
        
        // Return landuse code of landuse category lid.
        public virtual string getLanduseCode(int lid) {
            var lid1 = this.translateLanduse(lid);
            string code = null;
            if (this.landuseCodes.TryGetValue(lid1, out code)) return code;
            if (this._undefinedLanduseIds.Contains(lid)) {
                return this.defaultLanduseCode;
            } else {
                this._undefinedLanduseIds.Add(lid);
                var @string = lid.ToString();
                //Utils.error('Unknown landuse value {0}', string), self.isBatch)
                Utils.loginfo(String.Format("Unknown landuse value {0}", @string));
                return this.defaultLanduseCode;
            }
        }
        
        // Return landuse category (value in landuse map) for code, 
        //         adding to lookup tables if necessary.
        //         
        public int getLanduseCat(string landuseCode) {
            foreach (KeyValuePair<int, string> entry in this.landuseCodes) {
                if (entry.Value == landuseCode) {
                    return entry.Key;
                }
            }
            // we have a new landuse from splitting
            // first find a new category: maximum existing ones plus 1
            int cat = 0;
            foreach (var key in this.landuseCodes.Keys) {
                if (key > cat) {
                    cat = key;
                }
            }
            cat += 1;
            this.landuseCodes[cat] = landuseCode;
            // now add to landuseIds or urbanIds table
            this.storeLanduseCode(cat, landuseCode);
            return cat;
        }
        
        // HUC and HAWQS only.  Return True if landuse counts as agriculture.
        public bool isAgriculture(int landuse) {
            return 81 < landuse && landuse < 91 || 99 < landuse && landuse < 567;
        }
        
        // Store names and groups for soil categories.
        public virtual bool populateSoilNames(string soilTable, bool checkSoils) {
            this.soilNames.Clear();
            this.soilTranslate.Clear();
            var revSoilNames = new Dictionary<string, int>();
            this.connect();
            if (this.conn is null) {
                return false;
            }
            string sql = sqlSelect(soilTable, "SOIL_ID, SNAM", "", "");
            using (var reader = getReader(conn, sql)) {
                if (reader.HasRows) {
                    try {
                        while (reader.Read()) {
                            int nxt = Convert.ToInt32(reader.GetValue(0));
                            string soilName = reader.GetString(1);
                            if (nxt == 0 || this.defaultSoil < 0) {
                                this.defaultSoil = nxt;
                                this.defaultSoilName = soilName;
                                Utils.loginfo(String.Format("Default soil set to {0}", this.defaultSoilName));
                            }
                            // check if code already defined
                            int key = -1;
                            if (revSoilNames.TryGetValue(soilName, out key)) {
                                this.storeSoilTranslate(nxt, key);
                            } else {
                                // soilName not found
                                this.soilNames[nxt] = soilName;
                                revSoilNames[soilName] = nxt;
                            }
                        }
                    }
                    catch (Exception ex) {
                        Utils.error(String.Format("Could not read table {0} in project database {1}: {2}", soilTable, this.dbFile, ex.Message), this.isBatch);
                        return false;
                    }
                }
            }
            // only need to check usersoil table if not STATSGO 
            // (or SSURGO, but then we would not be here)
            return !checkSoils || this.useSTATSGO || this.checkSoilsDefined();

            // not currently used        
            //===========================================================================
            // @staticmethod
            // def matchesSTATSGO(name):
            //     pattern = '[A-Z]{2}[0-9]{3}\Z'
            //     return re.match(pattern, name)
            //===========================================================================
        }
        
        // Return name for soil id sid, plus flag indicating lookup success.
        public string getSoilName(int sid, out bool ok) {
            if (this.useSSURGO) {
                ok = true;
                return sid.ToString();
            }
            int sid1 = this.translateSoil(sid, out ok);
            string name = null;
            if (this.soilNames.TryGetValue(sid1, out name))
            {
                ok = true;
                return name;
            }
            if (this._undefinedSoilIds.Contains(sid)) {
                ok = false;
                return this.defaultSoilName;
            } else {
                var @string = sid.ToString();
                this._undefinedSoilIds.Add(sid);
                Utils.error(String.Format("Unknown soil value {0}", @string), this.isBatch);
                ok = false;
                return this.defaultSoilName;
            }
        }
        
        // Check if all soil names in soilNames are in usersoil table in reference database.
        public bool checkSoilsDefined() {
            var sql = sqlSelect(this.usersoil, "SNAM", "", "SNAM='{0}'");
            var errorReported = false;
            DbDataReader reader = null;
            foreach (var soilName in this.soilNames.Values) {
                try {
                    var sql2 = String.Format(sql, soilName);
                    reader = getReader(this.connRef, sql2);
                    if (!reader.HasRows) {
                        if (!errorReported) {
                            Utils.error(String.Format("Soil name {0} (and perhaps others) not defined in {1} table in database {2}.", soilName, this.usersoil, this.dbRefFile), this.isBatch);
                            errorReported = true;
                        } else {
                            Utils.loginfo(String.Format("Soil name {0} not defined.", soilName));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.error(String.Format("Could not read {0} table in database {1}: {2}", this.usersoil, this.dbRefFile, ex.Message), this.isBatch);
                    return false;
                } finally {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            return true;
        }
        
        // no longer used
        //===========================================================================
        // def setUsersoilTable(self, conn, connRef, usersoilTable, parent):
        //     """Find a usersoil table.
        //     
        //     First candidate is the usersoilTable parameter, 
        //     which (if not empty) if 'usersoil' is in the reference database, else the project database.
        //     Second candidate is the default 'usersoil' table in the reference database.
        //     Otherwise try project database tables with 'usersoil' in name, and confirm with user.
        //     """
        //     # if usersoilTable exists start with it: it is one obtained from the project file
        //     if usersoilTable != '':
        //         if usersoilTable == 'usersoil':
        //             if self.checkUsersoilTable(usersoilTable, connRef):
        //                 self.usersoilTableName = usersoilTable
        //                 return
        //         elif self.checkUsersoilTable(usersoilTable, conn):
        //             self.usersoilTableName = usersoilTable
        //             return
        //     # next try default 'usersoil'
        //     if self.checkUsersoilTable('usersoil', connRef):
        //         self.usersoilTableName = 'usersoil'
        //         return
        //     for table in self._usersoilTableNames:
        //         if table == 'usersoil':
        //             continue # old project database
        //         if self.checkUsersoilTable(table, conn):
        //             msg = 'Use {0} as usersoil table?', table)
        //             reply = Utils.question(msg, parent, True)
        //             if reply == QMessageBox.Yes:
        //                 self.usersoilTableName = table
        //                 return
        //     Utils.error('No usersoil table found', self.isBatch)
        //     self.usersoilTableName = ''
        //===========================================================================
        // Make key sid equivalent to key equiv, 
        //         where equiv is a key in soilNames.
        //         
        public void storeSoilTranslate(int sid, int equiv) {
            if (!this.soilTranslate.Keys.Contains(sid)) {
                this.soilTranslate[sid] = equiv;
            }
        }
        
        // Translate a soil id to its equivalent id in soilNames, plus flag indicating lookup success.
        public int translateSoil(int sid, out bool ok) {
            ListFuns.insertIntoSortedIntList(sid, this.soilVals, true);
            if (this.useSSURGO) {
                if (this.isHUC || this.isHAWQS) {
                    return this.translateSSURGOSoil(sid, out ok);
                } else {
                    ok = true;
                    return sid;
                }
            }
            int sid1;
            bool found = this.soilTranslate.TryGetValue(sid, out sid1);
            ok = true;
            return found ? sid1 : sid;
        }
        
        // Use table to convert soil map values to SSURGO muids, plus flag indicating lookup success.  
        //         Replace any soil with sname Water with Parameters._SSURGOWater.  
        //         Report undefined SSURGO soils.  Only used with HUC and HAWQS.
        public int translateSSURGOSoil(int sid, out bool ok) {
            if (this._undefinedSoilIds.Contains(sid)) {
                ok = false;
                return this.SSURGOUndefined;
            }
            int muid = -1;
            if (this.SSURGOSoils.TryGetValue(sid, out muid))
            {
                ok = true;
                return muid;
            }
            var sql = sqlSelect("statsgo_ssurgo_lkey", "Source, MUKEY", "", String.Format("LKEY={0}", sid.ToString()));
            this.connect();
            using (var reader = getReader(conn, sql)) {
                if (!reader.HasRows) {
                    Utils.information(String.Format("WARNING: SSURGO soil map value {0} not defined as lkey in statsgo_ssurgo_lkey", sid), this.isBatch, logFile: this.logFile);
                    this._undefinedSoilIds.Add(sid);
                    ok = false;
                    return sid;
                }
                reader.Read();
                // only an information issue, not an error for now 
                if (reader.GetString(0).ToUpper().Trim() == "STATSGO") {
                    Utils.information(String.Format("WARNING: SSURGO soil map value {0} is a STATSGO soil according to statsgo_ssurgo_lkey", sid), this.isBatch, logFile: this.logFile);
                    // self._undefinedSoilIds.append(sid)
                    // return sid
                }
                sql = sqlSelect("SSURGO_Soils", "SNAM", "", String.Format("MUID='{0}'", reader.GetString(1)));
            }
            using (var reader = getReader(this.SSURGOConn, sql)) {
                if (!reader.HasRows) {
                    Utils.information(String.Format("WARNING: SSURGO soil lkey value {0} and MUID {1} not defined", sid, reader.GetString(1)), this.isBatch, logFile: this.logFile);
                    this._undefinedSoilIds.Add(sid);
                    ok = false;
                    return this.SSURGOUndefined;
                }
                reader.Read();
                if (reader.GetString(0).ToLower().Trim() == "water") {
                    this.SSURGOSoils[Convert.ToInt32(sid)] = Parameters._SSURGOWater;
                    ok = true;
                    return Parameters._SSURGOWater;
                } else {
                    muid = Convert.ToInt32(reader.GetString(1));
                    this.SSURGOSoils[Convert.ToInt32(sid)] = muid;
                    ok = true;
                    return muid;
                }
            }
        }
        
        // Make sorted list of all landuses.
        public List<string> populateAllLanduses() {
            var luses = new List<string>();
            var landuseTable = "crop";
            var urbanTable = "urban";
            var landuseSql = sqlSelect(landuseTable, "CPNM, CROPNAME", "", "");
            var urbanSql = sqlSelect(urbanTable, "URBNAME, URBFLNM", "", "");
            if (this.connRef is null) {
                return luses;
            }
            using (var reader = getReader(this.connRef, landuseSql)) {
                try {
                    while (reader.Read()) {
                        luses.Add(reader.GetString(0) + " (" + reader.GetString(1) + ")");
                    }
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Could not read table {0} in reference database {1}: {2}", landuseTable, this.dbRefFile, ex.Message), this.isBatch);
                    return luses;
                }
            }
            using (var reader = getReader(this.connRef, urbanSql)) {
                try {
                    while (reader.Read()) {
                        luses.Add(reader.GetString(0) + " (" + reader.GetString(1) + ")");
                    }
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Could not read table {0} in reference database {1}: {2}", urbanTable, this.dbRefFile, ex.Message), this.isBatch);
                    return luses;
                }
            }
            luses.Sort();
            return luses;
        }
        
        // Put all landuse codes from landuse values vals in combo box.
        public void populateMapLanduses(List<int> vals, System.Windows.Forms.ComboBox combo) {
            foreach (var i in vals) {
                combo.Items.Add(this.getLanduseCode(i));
            }
        }
        
        // Return index of slopePerecent from slope limits list.
        public int slopeIndex(double slopePercent) {
            var n = this.slopeLimits.Count;
            foreach (var index in Enumerable.Range(0, n)) {
                if (slopePercent < this.slopeLimits[index]) {
                    return index;
                }
            }
            return n;
        }
        
        // Return the slope range for an index.
        public string slopeRange(int slopeIndex) {
            //Debug.Assert(0 <= slopeIndex && slopeIndex <= this.slopeLimits.Count);
            var minimum = slopeIndex == 0 ? 0 : this.slopeLimits[slopeIndex - 1];
            var maximum = slopeIndex == this.slopeLimits.Count ? 9999 : this.slopeLimits[slopeIndex];
            return String.Format("{0}-{1}", minimum, maximum);
        }
        
        public static string _MASTERPROGRESSTABLE = "([WorkDir] TEXT(200), " + "[OutputGDB] TEXT(60), " + "[RasterGDB] TEXT(60), " + "[SwatGDB] TEXT(200), " + "[WshdGrid] TEXT(24), " + "[ClipDemGrid] TEXT(24), " + "[SoilOption] TEXT(16), " + "[NumLuClasses] INTEGER, " + "[DoneWSDDel] INTEGER, " + "[DoneSoilLand] INTEGER, " + "[DoneWeather] INTEGER, " + "[DoneModelSetup] INTEGER, " + "[OID] AUTOINCREMENT(1,1), " + "[MGT1_Checked] INTEGER, " + "[ArcSWAT_V_Create] TEXT(12), " + "[ArcSWAT_V_Curr] TEXT(12), " + "[AccessExePath] TEXT(200), " + "[DoneModelRun] INTEGER)";
        
        public static string _BASINSDATA1 = "BASINSDATA1";
        
        public static string _BASINSDATA1TABLE = @"([basin] INTEGER, " + "[cellCount] INTEGER, " + 
            "[area] DOUBLE, " + "[drainArea] DOUBLE, " + 
            "[pondArea] DOUBLE, " + "[reservoirArea] DOUBLE, " + 
            "[totalElevation] DOUBLE, " + "[totalSlope] DOUBLE, " + "[outletCol] INTEGER, " + 
            "[outletRow] INTEGER, " + "[outletElevation] DOUBLE, " + "[startCol] INTEGER, " + 
            "[startRow] INTEGER, " + "[startToOutletDistance] DOUBLE, " + "[startToOutletDrop] DOUBLE, " + 
            "[farCol] INTEGER, " + "[farRow] INTEGER, " + "[farthest] INTEGER, " + "[farElevation] DOUBLE, " + 
            "[farDistance] DOUBLE, " + "[maxElevation] DOUBLE, " + "[cropSoilSlopeArea] DOUBLE, " + "[hru] INTEGER)";
        
        public static string _BASINSDATA2 = "BASINSDATA2";
        
        public static string _BASINSDATA2TABLE = "([ID] INTEGER, " + "[basin] INTEGER, " + "[crop] INTEGER, " + "[soil] INTEGER, " + "[slope] INTEGER, " + "[hru] INTEGER, " + "[cellcount] INTEGER, " + "[area] DOUBLE, " + "[totalSlope] DOUBLE)";
        
        public static string _BASINSDATAHUC1 = "BASINSDATAHUC1";
        
        public static string _BASINSDATA1TABLEHUC = @"
    (basin INTEGER, 
    cellCount INTEGER, 
    area REAL, 
    drainArea REAL, 
    pondArea REAL, 
    reservoirArea REAL, 
    playaArea REAL,
    lakeArea REAL,
    wetlandArea REAL,
    totalElevation REAL, 
    totalSlope REAL, 
    outletCol INTEGER, 
    outletRow INTEGER, 
    outletElevation REAL, 
    startCol INTEGER, 
    startRow INTEGER, 
    startToOutletDistance REAL, 
    startToOutletDrop REAL, 
    farCol INTEGER, 
    farRow INTEGER, 
    farthest INTEGER, 
    farElevation REAL, 
    farDistance REAL, 
    maxElevation REAL, 
    cropSoilSlopeArea REAL, 
    hru INTEGER,
    streamArea REAL,
    WATRInStreamArea REAL)
    ";
        
        public static string _ELEVATIONBANDTABLEINDEX = "OID";
        
        public static string _ELEVATIONBANDTABLE = "([OID] INTEGER, " + "[SUBBASIN] INTEGER, " + "[ELEVB1] DOUBLE, " + "[ELEVB2] DOUBLE, " + "[ELEVB3] DOUBLE, " + "[ELEVB4] DOUBLE, " + "[ELEVB5] DOUBLE, " + "[ELEVB6] DOUBLE, " + "[ELEVB7] DOUBLE, " + "[ELEVB8] DOUBLE, " + "[ELEVB9] DOUBLE, " + "[ELEVB10] DOUBLE, " + "[ELEVB_FR1] DOUBLE, " + "[ELEVB_FR2] DOUBLE, " + "[ELEVB_FR3] DOUBLE, " + "[ELEVB_FR4] DOUBLE, " + "[ELEVB_FR5] DOUBLE, " + "[ELEVB_FR6] DOUBLE, " + "[ELEVB_FR7] DOUBLE, " + "[ELEVB_FR8] DOUBLE, " + "[ELEVB_FR9] DOUBLE, " + "[ELEVB_FR10] DOUBLE)";
        
        // 
        //         Create MasterProgress table in project database using existing connection conn.
        //         
        //         Return true if successful, else false.
        //         
        public virtual bool createMasterProgressTable() {
            this.connect();
            if (this.conn is SQLiteConnection)
            {
                var sql0 = "DROP TABLE IF EXISTS MasterProgress";
                var sql1 = DBUtils._MASTERPROGRESSCREATESQL;
                var cmd = new SQLiteCommand(sql0, (SQLiteConnection)this.conn);
                cmd.ExecuteNonQuery();
                cmd.CommandText = sql1;
                cmd.ExecuteNonQuery();
            } else {
                var table = "MasterProgress";
                var dropSQL = "DROP TABLE " + table;
                OleDbCommand cmd;
                try {
                    cmd = new OleDbCommand(dropSQL, (OleDbConnection)this.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Could not drop table {0} from project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                    return false;
                }
                var createSQL = "CREATE TABLE " + table + " " + DBUtils._MASTERPROGRESSTABLE;
                try {
                    cmd.CommandText = createSQL;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Could not create table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                    return false;
                }
            }
            return true;
        }
        
        //// Write submapping table for HUC projects.
        //public virtual void writeSubmapping() {
        //    var conn = sqlite3.connect(this.dbFile);
        //    var cursor = conn.cursor();
        //    var sql0 = "DROP TABLE IF EXISTS submapping";
        //    cursor.execute(sql0);
        //    var sql1 = DBUtils._SUBMAPPINGCREATESQL;
        //    cursor.execute(sql1);
        //    var sql2 = "INSERT INTO submapping VALUES(?,?,?)";
        //    var submapping = Utils.join(this.projDir, "submapping.csv");
        //    using (var csvFile = open(submapping, "r")) {
        //        reader = csv.reader(csvFile);
        //        _ = next(reader);
        //        foreach (var line in reader) {
        //            cursor.execute(sql2, (Convert.ToInt32(line[0]), line[1], Convert.ToInt32(line[2])));
        //        }
        //    }
        //    conn.commit();
        //}
        
        // change to clear tables to avoid lock problems
        // Create BASINSDATA1 and 2 tables in project database.
        public (string, string) createBasinsDataTables() {
            string createSQL;
            string dropSQL;
            this.connect();
            SQLiteCommand sqliteCmd = new SQLiteCommand();
            OleDbCommand oleDbCmd = new OleDbCommand();
            var table = this.isHUC || this.isHAWQS ? DBUtils._BASINSDATAHUC1 : DBUtils._BASINSDATA1;
            if (this._allTableNames.Contains(table)) {
                //    this.clearTable(table);
                //}
                dropSQL = "DROP TABLE " + table;
                try {
                    execNonQuery(dropSQL);
                } catch (Exception) {
                    // tends to say already locked, so try closing connection and reopening
                    try {
                        this.conn.Close();
                        execNonQuery(dropSQL);
                    }
                    catch (Exception ex) {
                        Utils.error(String.Format("Could not drop table {0} from project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                        return (null, null);
                    }
                }
            }
            if (this.isHUC || this.isHAWQS) {
                createSQL = "CREATE TABLE " + table + " " + DBUtils._BASINSDATA1TABLEHUC;
            } else {
                createSQL = "CREATE TABLE " + table + " " + DBUtils._BASINSDATA1TABLE;
            }
            try {
                execNonQuery(createSQL);
            }
            catch (Exception ex) {
                Utils.error(String.Format("Could not create table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                return (null, null);
            }
            string sql1, sql2;
            if (this.isHUC || this.isHAWQS) {
                sql1 = "INSERT INTO " + table + " VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
            } else { 
                sql1 = "INSERT INTO " + table + " VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
            }
            table = DBUtils._BASINSDATA2;
            if (this._allTableNames.Contains(table)) {
            //    this.clearTable(table);
            //}
                dropSQL = "DROP TABLE " + table;
                try {
                    execNonQuery(dropSQL);
                }
                catch (Exception ex) {
                    Utils.error(String.Format("Could not drop table {0} from project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                    return (null, null);
                }
            }
            createSQL = "CREATE TABLE " + table + " " + DBUtils._BASINSDATA2TABLE;
            try {
                execNonQuery(createSQL);
            } catch (Exception ex) {
                    Utils.error(String.Format("Could not create table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                    return (null, null);
                }
            var indexSQL = String.Format("CREATE INDEX basinindex ON {0} (basin)", table);
            try {
                execNonQuery(indexSQL);
            } catch (Exception ex) {
                Utils.error(String.Format("Failed to create index on table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                return (null, null);
            }
            sql2 = "INSERT INTO " + table + " VALUES(?,?,?,?,?,?,?,?,?)";
            return (sql1, sql2);
        }
        
        // Write BASINSDATA1 and 2 tables in project database.
        public void writeBasinsData(Dictionary<int, BasinData> basins, string sql1, string sql2) {
            this.connect();
            var index = 0;
            foreach (KeyValuePair<int, BasinData> entry in basins) {
                var basin = entry.Key;
                var data = entry.Value;
                if (data.relHru == 0) {
                    Utils.error(String.Format("There are no HRUs in subbasin with PolygonId {0}.  Check your landuse and soil coverage", basin), this.isBatch);
                }
                index = this.writeBasinsDataItem(basin, data, sql1, sql2, index);
                if (index < 0) {
                    // error occurred - no point in repeating the failure
                    break;
                }
            }
            //if (this.isHUC || this.isHAWQS || this.forTNC) {
            //    conn.commit();
            // } else {
                //this.hashDbTable(DBUtils._BASINSDATA1);
                //this.hashDbTable(DBUtils._BASINSDATA2);
            //}
        }

        // Write data for one basin in BASINSDATA1 and 2 tables in project database.
        public int writeBasinsDataItem(
            int basin,
            BasinData data,
            string sql1,
            string sql2,
            int index) {
            this.connect();
            // note we coerce all double values to float to avoid 'SQLBindParameter' error if an int becomes a long
            string insert;
            try {
                if (this.isHUC || this.isHAWQS || this.forTNC) {
                    insert = "(" +
                        basin.ToString() + "," +
                        data.cellCount.ToString() + "," +
                        data.area.ToString() + "," + 
                        data.drainArea.ToString() + "," + 
                        data.pondArea.ToString() + "," + 
                        data.reservoirArea.ToString() + "," + 
                        data.playaArea.ToString() + "," + 
                        data.lakeArea.ToString() + "," + 
                        data.wetlandArea.ToString() + "," + 
                        data.totalElevation.ToString() + "," + 
                        data.totalSlope.ToString() + "," + 
                        data.outletCol.ToString() + "," + 
                        data.outletRow.ToString() + "," + 
                        data.outletElevation.ToString() + "," + 
                        data.startCol.ToString() + "," + 
                        data.startRow.ToString() + "," + 
                        data.startToOutletDistance.ToString() + "," + 
                        data.startToOutletDrop.ToString() + "," + 
                        data.farCol.ToString() + "," +
                        data.farRow.ToString() + "," + 
                        data.farthest.ToString() + "," + 
                        data.farElevation.ToString() + "," + 
                        data.farDistance.ToString() + "," + 
                        data.maxElevation.ToString() + "," + 
                        data.cropSoilSlopeArea.ToString() + "," + 
                        data.relHru.ToString() + "," + 
                        data.streamArea.ToString() + "," + 
                        data.WATRInStreamArea.ToString() + ")";
                } else {
                    insert = "(" +
                        basin.ToString() + "," + 
                        data.cellCount.ToString() + "," + 
                        data.area.ToString() + "," + 
                        data.drainArea.ToString() + "," + 
                        data.pondArea.ToString() + "," + 
                        data.reservoirArea.ToString() + "," + 
                        data.totalElevation.ToString() + "," + 
                        data.totalSlope.ToString() + "," + 
                        data.outletCol.ToString() + "," + 
                        data.outletRow.ToString() + "," + 
                        data.outletElevation.ToString() + "," + 
                        data.startCol.ToString() + "," + 
                        data.startRow.ToString() + "," + 
                        data.startToOutletDistance.ToString() + "," + 
                        data.startToOutletDrop.ToString() + "," + 
                        data.farCol.ToString() + "," + 
                        data.farRow.ToString() + "," + 
                        data.farthest.ToString() + "," + 
                        data.farElevation.ToString() + "," + 
                        data.farDistance.ToString() + "," + 
                        data.maxElevation.ToString() + "," + 
                        data.cropSoilSlopeArea.ToString() + "," + 
                        data.relHru.ToString() + ")";
                }
                InsertInTable(DBUtils._BASINSDATA1, insert);
            } catch (Exception ex) {
                Utils.error(String.Format("Could not write to table {0} in project database {1}: {2}", DBUtils._BASINSDATA1, this.dbFile, ex.Message), this.isBatch);
                return -1;
            }
            foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, int>>> kvp1 in data.cropSoilSlopeNumbers) {
                var crop = kvp1.Key;
                var soilSlopeNumbers = kvp1.Value;
                foreach (KeyValuePair<int, Dictionary<int, int>> kvp2 in soilSlopeNumbers) {
                    var soil = kvp2.Key;
                    var slopeNumbers = kvp2.Value;
                    foreach (KeyValuePair<int, int> kvp3 in slopeNumbers) {
                        var slope = kvp3.Key;
                        var hru = kvp3.Value;
                        var cd = data.hruMap[hru];
                        index += 1;
                        try {
                            string insert2 = "(" +
                                index.ToString() + "," +
                                basin.ToString() + "," +
                                crop.ToString() + "," +
                                soil.ToString() + "," +
                                slope.ToString() + "," +
                                hru.ToString() + "," +
                                cd.cellCount.ToString() + "," +
                                cd.area.ToString() + "," +
                                cd.totalSlope.ToString() + ")";
                            InsertInTable(DBUtils._BASINSDATA2, insert2);
                        } catch (Exception ex) {
                            Utils.error(String.Format("Could not write to table {0} in project database {1}: {2}", DBUtils._BASINSDATA2, this.dbFile, ex.Message), this.isBatch);
                            return -1;
                        }
                    }
                }
            }
            return index;
        }
        
        // Recreate basins data from BASINSDATA1 and 2 tables in project database.
        public Dictionary<int, BasinData> regenerateBasins(out bool ok, bool ignoreerrors = false) {
            try {
                var basins = new Dictionary<int, BasinData>();
                this.connect();
                if (this.isHUC || this.isHAWQS || this.forTNC) {
                    try
                    {
                        using (var reader = getReader(this.conn, sqlSelect(DBUtils._BASINSDATAHUC1, "*", "", ""))) {
                            while (reader.Read()) {
                                var bd = new BasinData(Convert.ToInt32(reader.GetValue(11)), Convert.ToInt32(reader.GetValue(12)), reader.GetDouble(13), Convert.ToInt32(reader.GetValue(14)), Convert.ToInt32(reader.GetValue(15)), reader.GetDouble(16), reader.GetDouble(17), reader.GetDouble(22));
                                bd.cellCount = Convert.ToInt32(reader.GetValue(1));
                                bd.area = reader.GetDouble(2);
                                bd.drainArea = reader.GetDouble(3);
                                bd.pondArea = reader.GetDouble(4);
                                bd.reservoirArea = reader.GetDouble(5);
                                bd.playaArea = reader.GetDouble(6);
                                bd.lakeArea = reader.GetDouble(7);
                                bd.wetlandArea = reader.GetDouble(8);
                                bd.totalElevation = reader.GetDouble(9);
                                bd.totalSlope = reader.GetDouble(10);
                                bd.maxElevation = reader.GetDouble(23);
                                bd.farCol = Convert.ToInt32(reader.GetValue(18));
                                bd.farRow = Convert.ToInt32(reader.GetValue(19));
                                bd.farthest = Convert.ToInt32(reader.GetValue(20));
                                bd.farElevation = reader.GetDouble(21);
                                bd.cropSoilSlopeArea = reader.GetDouble(24);
                                bd.relHru = Convert.ToInt32(reader.GetValue(25));
                                bd.streamArea = reader.GetDouble(26);
                                bd.WATRInStreamArea = reader.GetDouble(27);
                                int basin = Convert.ToInt32(reader.GetValue(0));
                                basins[basin] = bd;
                                using (var reader2 = getReader(this.conn, sqlSelect(_BASINSDATA2, "*", "", String.Format("basin={0}", basin)))) {
                                    while (reader2.Read()) {
                                        int crop = Convert.ToInt32(reader2.GetValue(2));
                                        int soil = Convert.ToInt32(reader2.GetValue(3));
                                        int slope = Convert.ToInt32(reader2.GetValue(4));
                                        if (!bd.cropSoilSlopeNumbers.ContainsKey(crop)) {
                                            bd.cropSoilSlopeNumbers[crop] = new Dictionary<int, Dictionary<int, int>>();
                                            ListFuns.insertIntoSortedIntList(crop, this.landuseVals, true);
                                        }
                                        if (!bd.cropSoilSlopeNumbers[crop].ContainsKey(soil)) {
                                            bd.cropSoilSlopeNumbers[crop][soil] = new Dictionary<int, int>();
                                        }
                                        bd.cropSoilSlopeNumbers[crop][soil][slope] = Convert.ToInt32(reader2.GetValue(5));
                                        var cellData = new CellData(Convert.ToInt32(reader2.GetValue(6)), reader2.GetDouble(7), reader2.GetDouble(8), crop);
                                        bd.hruMap[Convert.ToInt32(reader2.GetValue(5))] = cellData;
                                    }
                                }
                            }
                        }
                    } catch (Exception ex) {
                        if (!ignoreerrors) {
                            Utils.error(String.Format(@"Could not read basins data from project database {0}: {1}.
                                        Perhaps you need to run fixBasinData.
                                        ", this.dbFile, ex.Message), this.isBatch);
                        }
                        ok = false;
                        return null;
                    }
                } else {
                    try {
                        using (var reader = getReader(this.conn, sqlSelect(DBUtils._BASINSDATA1, "*", "", ""))) {
                            while (reader.Read()) {
                                var bd = new BasinData(Convert.ToInt32(reader.GetValue(8)), Convert.ToInt32(reader.GetValue(9)), reader.GetDouble(10), Convert.ToInt32(reader.GetValue(11)), Convert.ToInt32(reader.GetValue(12)), reader.GetDouble(13), reader.GetDouble(14), reader.GetDouble(19));
                                bd.cellCount = Convert.ToInt32(reader.GetValue(1));
                                bd.area = reader.GetDouble(2);
                                bd.drainArea = reader.GetDouble(3);
                                bd.pondArea = reader.GetDouble(4);
                                bd.reservoirArea = reader.GetDouble(5);
                                bd.totalElevation = reader.GetDouble(6);
                                bd.totalSlope = reader.GetDouble(7);
                                bd.maxElevation = reader.GetDouble(20);
                                bd.farCol = Convert.ToInt32(reader.GetValue(15));
                                bd.farRow = Convert.ToInt32(reader.GetValue(16));
                                bd.farthest = Convert.ToInt32(reader.GetValue(17));
                                bd.farElevation = reader.GetDouble(18);
                                bd.cropSoilSlopeArea = reader.GetDouble(21);
                                bd.relHru = Convert.ToInt32(reader.GetValue(22));
                                int basin = Convert.ToInt32(reader.GetValue(0));
                                basins[basin] = bd;
                                using (var reader2 = getReader(this.conn, sqlSelect(_BASINSDATA2, "*", "", String.Format("basin={0}", basin)))) {
                                    while (reader2.Read()) {
                                        int crop = Convert.ToInt32(reader2.GetValue(2));
                                        int soil = Convert.ToInt32(reader2.GetValue(3));
                                        int slope = Convert.ToInt32(reader2.GetValue(4));
                                        if (!bd.cropSoilSlopeNumbers.ContainsKey(crop)) {
                                            bd.cropSoilSlopeNumbers[crop] = new Dictionary<int, Dictionary<int, int>>();
                                            ListFuns.insertIntoSortedIntList(crop, this.landuseVals, true);
                                        }
                                        if (!bd.cropSoilSlopeNumbers[crop].ContainsKey(soil)) {
                                            bd.cropSoilSlopeNumbers[crop][soil] = new Dictionary<int, int>();
                                        }
                                        bd.cropSoilSlopeNumbers[crop][soil][slope] = Convert.ToInt32(reader2.GetValue(5));
                                        var cellCount = Convert.ToInt32(reader2.GetValue(6));
                                        var area = reader2.GetDouble(7);
                                        var totalSlope = reader2.GetDouble(8);
                                        var cellData = new CellData(cellCount, area, totalSlope, crop);
                                        bd.hruMap[Convert.ToInt32(reader2.GetValue(5))] = cellData;
                                    }
                                }
                            }
                        }
                    } catch (Exception ex) {
                        if (!ignoreerrors) {
                            Utils.error(String.Format("Could not read basins data from project database {0}: {1}", this.dbFile, ex.Message), this.isBatch);
                        }
                        ok = false;
                        return null;
                    }
                }
                ok = true;
                return basins;
            } catch (Exception ex) {
                if (!ignoreerrors) {
                    Utils.error("Failed to reconstruct basin data from database: " + ex.Message, this.isBatch);
                }
                ok = false;
                return null;
            }
        }
        
        //# Write ElevationBand table.
        public void writeElevationBands(Dictionary<int, List<(double, double, double)>> basinElevBands) {
            this.connect();
                if (this.conn is null) {
                    return;
                }
                var table = "ElevationBand";
                if (this.useSQLite) { // (this.isHUC || this.isHAWQS) {
                    
                    try {
                    execNonQuery("DROP TABLE IF EXISTS " + table);
                } catch (Exception ex) {
                        Utils.error(String.Format("Could not drop table {0} from project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                        return;
                    }
                } else {
                    if (this._allTableNames.Contains(table)) {
                        try {
                        execNonQuery("DROP TABLE " + table);
                    } catch (Exception ex) {
                            Utils.error(String.Format("Could not drop table {0} from project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                            return;
                        }
                    } 
                }
                var createSQL = "CREATE TABLE " + table + " " + _ELEVATIONBANDTABLE;
                try {
                    execNonQuery(createSQL);
                } catch (Exception ex) {
                    Utils.error(String.Format("Could not create table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                    return;
                }
                var indexSQL = "CREATE UNIQUE INDEX idx" + _ELEVATIONBANDTABLEINDEX + " ON " + table + "([" + _ELEVATIONBANDTABLEINDEX + "])";
                execNonQuery(indexSQL);
                int oid = 0;
                foreach (var kvp in basinElevBands) {
                var SWATBasin = kvp.Key;
                var bands = kvp.Value;
                oid += 1;
                string row;
                    if (bands is not null) {
                        row = String.Format("({0},{1},", oid, SWATBasin);
                        foreach (var i in Enumerable.Range(0, 10)) {
                            double el;
                            if (i < bands.Count) {
                                el = bands[i].Item2;
                            } else {
                                el = 0;
                            }
                            row += String.Format("{0:F2},", el);
                        }
                        foreach (var i in Enumerable.Range(0, 10)) {
                            double frac;
                            if (i < bands.Count) {
                                frac = bands[i].Item3;
                            } else {
                                frac = 0;
                            }
                            row += String.Format("{0:F4}", frac / 100.0);
                            var sep = i < 9 ? "," : ")";
                            row += sep;
                        }
                    } else {
                        row = String.Format("({0},{1},0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0)", oid, SWATBasin);
                    }
                    try {
                    InsertInTable(table, row);
                    } catch (Exception ex) {
                        Utils.error(String.Format("Could not write to table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                        return;
                    }
                }
                if (this.isHUC || this.isHAWQS) {
                    //this.conn.commit();
                } else {
                    this.hashDbTable(table);
                }
            }
        
        public static string _LANDUSELOOKUPTABLE = "([LANDUSE_ID] INTEGER, [SWAT_CODE] TEXT(4))";
        
        public static string _SOILLOOKUPTABLE = "([SOIL_ID] INTEGER, [SNAM] TEXT(254))";
        
        //# write table of typ either soil or landuse in project database using csv file fil
        public string importCsv(string table, string typ, string fil) {
            this.connect();
            if (this.conn is null) {
                return "";
            }
            // should not happen, but safety first
            if (this._allTableNames.Contains(table)) {
                try {
                    execNonQuery("DROP TABLE " + table);
                } catch (Exception ex) {
                    Utils.error(String.Format("Could not drop table {0} from project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                    return "";
                }
            }
            var design = typ == "soil" ? _SOILLOOKUPTABLE : _LANDUSELOOKUPTABLE;
            var createSQL = "CREATE TABLE " + table + " " + design;
            try {
                execNonQuery(createSQL);
            } catch (Exception ex) {
                Utils.error(String.Format("Could not create table {0} in project database {1}: {2}", table, this.dbFile, ex.Message), this.isBatch);
                return "";
            }
            bool firstLineRead = false;
            var sr = new StreamReader(fil);
            string nxt;
            while ((nxt = sr.ReadLine()) != null) {
                var line = nxt.Split(',');
                try {
                    // allow for headers in first line
                    if (!firstLineRead) {
                        firstLineRead = true;
                        if (!int.TryParse(line[0], out _)) {
                            continue;
                        }
                    }
                    if (line.Length < 2) {
                        // protect against blank lines
                        continue;
                    }
                    string insert = String.Format("({0}, '{1}')", line[0], line[1]);
                    InsertInTable(table, insert);
                } catch (Exception ex) {
                    Utils.error(String.Format("Could not write to table {0} in project database {1} from file {2}: {3}", table, this.dbFile, fil, ex.Message), this.isBatch);
                    return "";
                }
            }
            if (typ == "soil") {
                this.soilTableNames.Add(table);
            } else {
                this.landuseTableNames.Add(table);
            }
            return table;
        }
        
        // Clear Watershed, hrus, uncomb and ElevationBandtables.
        public void initWHUTables(object curs) {
            var table1 = "Watershed";
            // Extra CatchmentId field added 21/4/22, so recreate
            var dropSQL = "DROP TABLE IF EXISTS " + table1;
            execNonQuery(dropSQL);
            execNonQuery(DBUtils._WATERSHEDCREATESQL);
            var table2 = "hrus";
            var clearSQL = "DELETE FROM " + table2;
            execNonQuery(clearSQL);
            var table3 = "uncomb";
            clearSQL = "DELETE FROM " + table3;
            execNonQuery(clearSQL);
            var table4 = "ElevationBand";
            clearSQL = "DELETE FROM " + table4;
            execNonQuery(clearSQL);
            return;
        }
        
        //// Return last update time for table, or None if not available.  Returns a datetime value.
        //public DateTime? lastUpdateTime(string table) {
        //    this.connect();
        //        var sql = sqlSelect("MSysObjects", "DateUpdate", String.Format("NAME={0}", table), "");
        //        try {
        //            var reader = getReader(this.conn, sql);
        //            reader.Read();
        //            var result = reader.GetX(0);
        //            return result.DateUpdate;
        //        } catch {
        //            return null;
        //        }
        //    }
        //}
        
        //// Return true if last update time of table no earlier than last update of file.
        //public bool tableIsUpToDate(string fileName, string table) {
        //    try {
        //        var fileTimeStamp = os.path.getmtime(fileName);
        //        var tableTime = this.lastUpdateTime(table);
        //        Debug.Assert(tableTime is not null);
        //        return tableTime >= datetime.datetime.fromtimestamp(fileTimeStamp);
        //    } catch {
        //        return false;
        //    }
        //}
        
        //# Return an md5 hash value for a database table.  Used in testing.
        public string hashDbTable(string table) {
            // Only calculate and store table hashes when testing, as this is their purpose
            // TODO:
            //if (this.projName.Contains("test")) {
            //    using (MD5 md5 = MD5.Create()) {
            //        var reader = getReader(this.conn, sqlSelect(table, "*", "", "");
            //        while (reader.Read()) {
            //        m.update(row.@__repr__().encode());
            //    }
            //    var result = m.hexdigest();
            //    Utils.loginfo(String.Format("Hash for table {0}: {1}", table, result));
            //    return result;
            //}
            return "";
        }

        /// <summary>
        /// Put quotes round a string (needed when string is to be put into an
        /// INSERT command, eg if it is numeric or includes spaces)
        /// </summary>
        /// <param name="s">string</param>
        /// <returns>"string"</returns>
        public static string quote(string s)
        {
            return "\"" + s + "\"";
        }

        /// <summary>
        /// Inserts a row into a table in the project database
        /// </summary>
        /// <param name="table">table name</param>
        /// <param name="values">values for row, as a string in form "(v1, v2, ..., vn)"</param>
        public void InsertInTable(string table, string values)
        {
            string commandString = "INSERT INTO " + table + " VALUES " + values;
            execNonQuery(commandString);
        }

        public static string _WATERSHEDCREATESQL = @"
    CREATE TABLE Watershed (
        OBJECTID INTEGER,
        Shape    BLOB,
        GRIDCODE INTEGER,
        Subbasin INTEGER,
        Area     REAL,
        Slo1     REAL,
        Len1     REAL,
        Sll      REAL,
        Csl      REAL,
        Wid1     REAL,
        Dep1     REAL,
        Lat      REAL,
        Long_    REAL,
        Elev     REAL,
        ElevMin  REAL,
        ElevMax  REAL,
        Bname    TEXT,
        Shape_Length  REAL,
        Shape_Area    REAL,
        Defined_Area  REAL,
        HRU_Area REAL,
        HydroID  INTEGER,
        OutletID INTEGER,
        CatchmentId INTEGER
    );
    ";
        
        public static string _HRUSCREATESQL = @"
    CREATE TABLE hrus (
        OID      INTEGER,
        SUBBASIN INTEGER,
        ARSUB    REAL,
        LANDUSE  TEXT,
        ARLU     REAL,
        SOIL     TEXT,
        ARSO     REAL,
        SLP      TEXT,
        ARSLP    REAL,
        SLOPE    REAL,
        UNIQUECOMB TEXT,
        HRU_ID   INTEGER,
        HRU_GIS  TEXT
    );
    ";
        
        public static string _UNCOMBCREATESQL = @"
    CREATE TABLE uncomb (
        OID        INTEGER,
        SUBBASIN   INTEGER,
        LU_NUM     INTEGER,
        LU_CODE    TEXT,
        SOIL_NUM   INTEGER,
        SOIL_CODE  TEXT,
        SLOPE_NUM  INTEGER,
        SLOPE_CODE TEXT,
        MEAN_SLOPE REAL,
        AREA       REAL,
        UNCOMB     TEXT
    );
    ";
        
        public static string _HRUCREATESQL = @"
    CREATE TABLE hru ( 
        OID INTEGER,
        SUBBASIN INTEGER,
        HRU INTEGER,
        LANDUSE TEXT,
        SOIL TEXT,
        SLOPE_CD TEXT,
        HRU_FR REAL,
        SLSUBBSN REAL,
        HRU_SLP REAL,
        OV_N REAL,
        LAT_TTIME REAL DEFAULT(0),
        LAT_SED REAL DEFAULT(0),
        SLSOIL REAL DEFAULT(0),
        CANMX REAL DEFAULT(0),
        ESCO REAL DEFAULT(0.95),
        EPCO REAL DEFAULT(1),
        RSDIN REAL DEFAULT(0),
        ERORGN REAL DEFAULT(0),
        ERORGP REAL DEFAULT(0),
        POT_FR REAL DEFAULT(0),
        FLD_FR REAL DEFAULT(0),
        RIP_FR REAL DEFAULT(0),
        POT_TILE REAL DEFAULT(0),
        POT_VOLX REAL DEFAULT(0),
        POT_VOL REAL DEFAULT(0),
        POT_NSED REAL DEFAULT(0),
        POT_NO3L REAL DEFAULT(0),
        DEP_IMP REAL DEFAULT(6000),
        DIS_STREAM REAL DEFAULT(35),
        EVPOT REAL DEFAULT(0.5),
        CF REAL DEFAULT(1),
        CFH REAL DEFAULT(1),
        CFDEC REAL DEFAULT(0.055),
        SED_CON REAL DEFAULT(0),
        ORGN_CON REAL DEFAULT(0),
        ORGP_CON REAL DEFAULT(0),
        SOLN_CON REAL DEFAULT(0),
        SOLP_CON REAL DEFAULT(0),
        RE REAL DEFAULT(50),
        SDRAIN REAL DEFAULT(15000),
        DRAIN_CO REAL DEFAULT(10),
        PC REAL DEFAULT(1),
        LATKSATF REAL DEFAULT(1),
        N_LNCO REAL DEFAULT(2.0),
        R2ADJ REAL DEFAULT(1),
        N_REDUC REAL DEFAULT(300),
        POT_K REAL DEFAULT(0.01),
        N_LN REAL DEFAULT(2.0),
        N_LAG REAL DEFAULT(0.25),
        SURLAG REAL DEFAULT(2.0),
        POT_SOLP REAL DEFAULT(0.01)
    );
    ";
        
        public static string _SUBMAPPINGCREATESQL = @"
    CREATE TABLE submapping (
        SUBBASIN    INTEGER,
        HUC_ID      TEXT,
        IsEnding    INTEGER
        );
    ";
        
        public static string _MASTERPROGRESSCREATESQL = @"
    CREATE TABLE MasterProgress (
        WorkDir            TEXT,
        OutputGDB          TEXT,
        RasterGDB          TEXT,
        SwatGDB            TEXT,
        WshdGrid           TEXT,
        ClipDemGrid        TEXT,
        SoilOption         TEXT,
        NumLuClasses       INTEGER,
        DoneWSDDel         INTEGER,
        DoneSoilLand       INTEGER,
        DoneWeather        INTEGER,
        DoneModelSetup     INTEGER,
        OID                INTEGER,
        MGT1_Checked       INTEGER,
        ArcSWAT_V_Create   TEXT,
        ArcSWAT_V_Curr     TEXT,
        AccessExePath      TEXT,
        DoneModelRun       INTEGER
    );
    ";
        
        public static string _LAKETABLESQL = @"
    (OID INTEGER,
    SUBBASIN INTEGER,
    LAKE_AREA REAL)
    ";
        
        public static string _PLAYATABLESQL = @"
    (OID INTEGER,
    SUBBASIN INTEGER,
    PLAYA_AREA REAL)
    ";
    }

    public class Proj {

        private GlobalVars gv;

        public Proj(GlobalVars gv) { this.gv = gv; }

        // imitation of QGIS readEntry
        // assume result is a relative path and prefix with projDir if title is not empty
        // (a kludge exploiting fact title is not needed)
        public Tuple<string, bool> readEntry(string title, string path, string deflt) {
            gv.db.connect();
            var parts = path.Split('/');
            var table = "config_" + parts[0];
            var field = parts[1];
            if (field == "table") { field = "tabl"; } // table is an SQL reserved word
            var sql = "SELECT " + field + " FROM " + table;
            // code used for debugging
            //var sql0 = "SELECT * FROM " + table;
            //var adapter = new OleDbDataAdapter(sql0, gv.db.conn as OleDbConnection);
            //var values = new DataSet();
            //adapter.Fill(values, "Data");
            //object[] vals = new object[30];
            //foreach (DataRow r in values.Tables["Data"].Rows) {
            //    for (int i = 0; i < r.ItemArray.Length; i++) {
            //        vals[i] = r.ItemArray[i];
            //    }
            //}
            try {
                using (var reader = DBUtils.getReader(gv.db.conn, sql)) {
                    if (reader.HasRows) {
                        reader.Read();
                        string result = null;
                        try {
                            var value = reader.GetValue(0);
                            if (value != null) {
                                var typ = value.GetType();
                                if (typ == typeof(string)) {
                                    result = value.ToString();
                                }
                            }
                        } catch { 
                            var errRes = reader.GetValue(0);
                            var typ = errRes.GetType();
                        }
                        reader.Close();
                        if (string.IsNullOrEmpty(result)) {
                            return Tuple.Create(deflt, false);
                        }
                        if (!string.IsNullOrEmpty(title)) {
                            result = Uri.UnescapeDataString(Path.Combine(gv.projDir, result));
                        }
                        return Tuple.Create(result, true);
                    } else {
                        reader.Close();
                        return Tuple.Create(deflt, false);
                    }
                }
            } catch {
                return Tuple.Create(deflt, false);
            }
        }

        // imitation of QGIS readBoolEntry
        public Tuple<bool, bool> readBoolEntry(string title, string path, bool deflt) {
            gv.db.connect();
            var parts = path.Split('/');
            var table = "config_" + parts[0];
            var field = parts[1];
            var sql = "SELECT " + field + " FROM " + table + ";";
            try {
                using (var reader = DBUtils.getReader(gv.db.conn, sql)) {
                    if (reader.HasRows) {
                        reader.Read();
                        return Tuple.Create(reader.GetBoolean(0), true);
                    } else {
                        return Tuple.Create(deflt, false);
                    }
                }
            } catch {
                return Tuple.Create(deflt, false);
            }
        }

        // imitation of QGIS readNumEntry
        public Tuple<int, bool> readNumEntry(string title, string path, int deflt) {
            gv.db.connect();
            var parts = path.Split('/');
            var table = "config_" + parts[0];
            var field = parts[1];
            var sql = "SELECT " + field + " FROM " + table + ";";
            try {
                using (var reader = DBUtils.getReader(this.gv.db.conn, sql)) {
                    if (reader.HasRows) {
                        reader.Read();
                        return Tuple.Create(Convert.ToInt32(reader.GetValue(0)), true);
                    } else {
                        return Tuple.Create(deflt, false);
                    }
                }
            } catch {
                return Tuple.Create(deflt, false);
            }
        }

        // imitation of QGIS writeEntry
        public void writeEntry(string title, string path, string val) {
            this.gv.db.connect();
            var parts = path.Split('/');
            var table = "config_" + parts[0];
            var field = parts[1];
            if (field == "table") { field = "tabl"; } // table is an SQL reserved word
            var sql = "UPDATE " + table + " SET " + field + String.Format("='{0}';", val);
            try {
                this.gv.db.execNonQuery(sql);
            }
            catch {; }
        }

        // imitation of QGIS writeNumEntry
        public void writeNumEntry(string title, string path, int val) {
            gv.db.connect();
            var parts = path.Split('/');
            var table = "config_" + parts[0];
            var field = parts[1];
            var sql = "UPDATE " + table + " SET " + field + String.Format("={0};", val);
            try {
                gv.db.execNonQuery(sql);
            }
            catch {; }
        }

        // imitation of QGIS writeEntryBool
        public void writeEntryBool(string title, string path, bool val) {
            gv.db.connect();
            var parts = path.Split('/');
            var table = "config_" + parts[0];
            var field = parts[1];
            var sql = "UPDATE " + table + " SET " + field + String.Format("={0};", val);
            try {
                gv.db.execNonQuery(sql);
            }
            catch {; }
        }
    }
}
