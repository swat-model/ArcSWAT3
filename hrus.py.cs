using System;
using System.IO;
using Range = System.Range;
using System.Linq;
using System.Windows.Forms;
using ComboBox = System.Windows.Forms.ComboBox;
using ProgressBar = System.Windows.Forms.ProgressBar;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections;

using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Internal.Catalog.PropertyPages.NetworkDataset;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Framework.Dialogs;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

using MaxRev.Gdal.Core;
using OSGeo.OGR;
using OSGeo.GDAL;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;
using OSGeo.OSR;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Editing;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using System.Windows.Controls;
using System.Reflection.Emit;

namespace ArcSWAT3 {

    // Data and functions for creating HRUs.
    public class HRUs {

        private ArcSWAT _parent;

        public DBUtils _db;

        public HRUsForm _dlg;

        public GlobalVars _gv;

        public ComboBox _reportsCombo;

        public Dictionary<int, Dictionary<int, Tuple<int, string, double>>> CHIRPSStations;

        public bool completed;

        public CreateHRUs CreateHRUs;

        public Dictionary<int, Dictionary<int, Tuple<int, string, double>>> ERA5Stations;

        public string landuseFile;

        public RasterLayer landuseLayer;

        public string soilFile;

        public RasterLayer soilLayer;

        public string weatherSource;

        public Dictionary<int, Dictionary<int, int>> wgnStations;

        public HRUs(GlobalVars gv, ComboBox reportsCombo, ArcSWAT parent) {
            this._gv = gv;
            this._db = this._gv.db;
            this._parent = parent;
            this._dlg = new HRUsForm(this);
            //this._dlg.setWindowFlags(this._dlg.windowFlags() & ~Qt.WindowContextHelpButtonHint & Qt.WindowMinimizeButtonHint);
            //this._dlg.move(this._gv.hrusPos);
            this._reportsCombo = reportsCombo;
            //# Landuse grid
            this.landuseFile = "";
            //# Soil grid
            this.soilFile = "";
            //# Landuse grid layer
            this.landuseLayer = null;
            //# Soil grid layer
            this.soilLayer = null;
            //# CreateHRUs object
            this.CreateHRUs = new CreateHRUs(this._gv, reportsCombo, _dlg);
            //# Flag to indicate completion
            this.completed = false;
            //# map lat -> long -> wgn station id
            this.wgnStations = new Dictionary<int, Dictionary<int, int>>();
            //# weather data source for TNC projects: CHIRPS or ERA5
            this.weatherSource = "";
            //# map lat -> long -> CHIRPS station and elevation
            this.CHIRPSStations = new Dictionary<int, Dictionary<int, Tuple<int, string, double>>>();
            //# map lat -> long -> ERA5 station and elevation
            this.ERA5Stations = new Dictionary<int, Dictionary<int, Tuple<int, string, double>>>();
        }



        // Run HRUs dialog.
        public virtual void run() {
            this._dlg.progress("");
            this._dlg.Show();
        }

        // Return true if HRUs are up to date, else false.
        //         
        //         Requires:
        //         - subs.shp used by visualize no earlier than watershed shapefile
        //         - Watershed table in project database no earlier than watershed shapefile
        //         - hrus table in project database no earlier than watershed shapefile
        //         but in fact last update times from Access are not reliable, so just use
        //         susb.shp is up to date and Watershed and hrus have data
        //         
        public virtual bool HRUsAreCreated(string basinFile = "") {
            try {
                if (this._gv.forTNC || !this._gv.useGridModel) {
                    // TODO: currently grid model does not Write the subs.shp file
                    // first check subsFile is up to date
                    var subsFile = Utils.join(this._gv.tablesOutDir, Parameters._SUBS + ".shp");
                    basinFile = this._gv.forTNC ? basinFile : this._gv.basinFile;
                    //===================================================================
                    // return Utils.isUpToDate(self._gv.wshedFile, subsFile) and \
                    //         self._gv.db.tableIsUpToDate(self._gv.wshedFile, 'Watershed') and \
                    //         self._gv.db.tableIsUpToDate(self._gv.wshedFile, 'hrus')
                    //===================================================================
                    //print('Comparing {0} with {1}', basinFile, subsFile))
                    if (!Utils.isUpToDate(basinFile, subsFile)) {
                        //print('HRUSAreCreated failed: subs.shp not up to date')
                        Utils.loginfo("HRUSAreCreated failed: subs.shp not up to date");
                        if (this._gv.logFile is not null) {
                            using (StreamWriter w = new(this._gv.logFile, append: true)) {
                                w.WriteLine("HRUSAreCreated failed: subs.shp not up to date");
                            }
                        }
                        return false;
                    }
                }
                if (!this._gv.db.hasData("Watershed")) {
                    //print('HRUSAreCreated failed: Watershed table missing or empty')
                    Utils.loginfo("HRUSAreCreated failed: Watershed table missing or empty");
                    if (this._gv.logFile is not null) {
                        using (StreamWriter w = new(this._gv.logFile, append: true)) {
                            w.WriteLine("HRUSAreCreated failed: Watershed table missing or empty");
                        }
                    }
                    return false;
                }
                // fails if all water
                // if not self._gv.db.hasData('hrus'):
                //     Utils.loginfo(u'HRUSAreCreated failed: hrus table missing or empty')
                //     return False
                if (this._gv.isHRUsDone()) {
                    //print('HRUSAreCreated OK: MasterProgress says HRUs done')
                    return true;
                } else {
                    //print('HRUSAreCreated failed: MasterProgress says HRUs not done')
                    if (this._gv.logFile is not null) {
                        using (StreamWriter w = new(this._gv.logFile, append: true)) {
                            w.WriteLine("MasterProgress says HRUs not done");
                        }
                    }
                    return false;
                }
            } catch (Exception) {
                return false;
            }
        }

        // no longer used - too slow for large BASINSDATA files   
        //===========================================================================
        // def tryRun(self):
        //     """Try rerunning with existing data and choices.  Fail quietly and return false if necessary, else return true."""
        //     try:
        //         self.init()
        //         if not self._db.hasData('BASINSDATA1'): 
        //             Utils.loginfo('HRUs tryRun failed: no basins data')
        //             return False
        //         if not self.initLanduses(self._gv.landuseTable):
        //             Utils.loginfo('HRUs tryRun failed: cannot initialise landuses')
        //             return False
        //         if not self.initSoils(self._gv._gv.soilTable, False):
        //             Utils.loginfo('HRUs tryRun failed: cannot initialise soils')
        //             return False
        //         time1 = DateTime.Now
        //         self.CreateHRUs.basins, OK = self._gv.db.regenerateBasins(True) 
        //         if not OK:
        //             Utils.loginfo('HRUs tryRun failed: could not regenerate basins')
        //             return False
        //         time2 = DateTime.Now
        //         Utils.loginfo('Reading from database took {0} seconds', int(time2 - time1)))
        //         self.CreateHRUs.saveAreas(True)
        //         if self._gv.useGridModel and self._gv.isBig:
        //             self.rewriteWHUTables()
        //         else:
        //             self._reportsCombo.setVisible(True)
        //             if self._reportsCombo.findText(Parameters._TOPOITEM) < 0:
        //                 self._reportsCombo.addItem(Parameters._TOPOITEM)
        //             if self._reportsCombo.findText(Parameters._BASINITEM) < 0:
        //                 self._reportsCombo.addItem(Parameters._BASINITEM)
        //             if self.CreateHRUs.isMultiple:
        //                 if self.CreateHRUs.isArea:
        //                     self.CreateHRUs.removeSmallHRUsByArea()
        //                 elif self.CreateHRUs.isTarget:
        //                     self.CreateHRUs.removeSmallHRUsbyTarget()
        //                 else:
        //                     if len(self._gv.db.slopeLimits) == 0: self.CreateHRUs.slopeVal = 0
        //                     # allow too tight thresholds, since we guard against removing all HRUs from a subbasin
        //                     # if not self.CreateHRUs.cropSoilAndSlopeThresholdsAreOK():
        //                     #     Utils.error('Internal error: problem with tight thresholds', self._gv.isBatch)
        //                     #     return
        //                     if self.CreateHRUs.useArea:
        //                         self.CreateHRUs.removeSmallHRUsByThresholdArea()
        //                     else:
        //                         self.CreateHRUs.removeSmallHRUsByThresholdPercent()
        //                 if not self.CreateHRUs.splitHRUs():
        //                     return False
        //             self.CreateHRUs.saveAreas(False)
        //             self.CreateHRUs.basinsToHRUs()
        //             if self._reportsCombo.findText(Parameters._HRUSITEM) < 0:
        //                 self._reportsCombo.addItem(Parameters._HRUSITEM)
        //             time1 = DateTime.Now
        //             self.CreateHRUs.writeWatershedTable()
        //             time2 = DateTime.Now
        //             Utils.loginfo('Writing Watershed table took {0} seconds', int(time2 - time1)))
        //         self._gv.writeMasterProgress(-1, 1)
        //         return True
        //     except Exception:
        //         Utils.loginfo('HRUs tryRun failed: {0}', traceback.format_exc()))
        //         return False
        //===========================================================================
        // Recreate Watershed, hrus and uncomb tables from basins map.  Used with grid model.
        //public virtual object rewriteWHUTables() {
        //    using (var conn = this._gv.db.connect()) {
        //        cursor = conn.cursor();
        //        (sql1, sql2, sql3, sql4) = this._gv.db.initWHUTables(cursor);
        //        oid = 0;
        //        elevBandId = 0;
        //        foreach (var (basin, basinData) in this.CreateHRUs.basins) {
        //            SWATBasin = this._gv.topo.basinToSWATBasin.get(basin, 0);
        //            if (SWATBasin == 0) {
        //                continue;
        //            }
        //            (centreX, centreY) = this._gv.topo.basinCentroids[basin];
        //            centroidll = this._gv.topo.pointToLatLong(QgsPointXY(centreX, centreY));
        //            (oid, elevBandId) = this.writeWHUTables(oid, elevBandId, SWATBasin, basin, basinData, cursor, sql1, sql2, sql3, sql4, centroidll);
        //        }
        //    }
        //}

        // Read landuse and soil data from files 
        //         or from previous run stored in project database.
        //         
        public async void readFiles() {
            string soil;
            string luse;
            this._gv.writeMasterProgress(-1, 0);
            // don't hide undefined soil and landuse errors from previous run
            this._gv.db._undefinedLanduseIds = new List<int>();
            this._gv.db._undefinedSoilIds = new List<int>();
            this._dlg.setForReading();
            //ESRI grid has directory as file name
            if (!File.Exists(this.landuseFile) && !Directory.Exists(this.landuseFile)) {
                Utils.error("Please select a landuse file", this._gv.isBatch);
                return;
            }
            //ESRI grid has directory as file name
            if (!File.Exists(this.soilFile) && !Directory.Exists(this.soilFile)) {
                Utils.error("Please select a soil file", this._gv.isBatch);
                return;
            }
            this._gv.landuseFile = this.landuseFile;
            this._gv.soilFile = this.soilFile;
            if (this._gv.isBatch) {
                Utils.information(string.Format("Landuse file: {0}", Path.GetFileName(this.landuseFile)), true);
                Utils.information(string.Format("Soil file: {0}", Path.GetFileName(this.soilFile)), true);
            }
            if (this._gv.isBatch) {
                // use names from project file settings
                luse = this._gv.landuseTable;
                Utils.information(string.Format("Landuse lookup table: {0}", this._gv.landuseTable), true);
                soil = this._gv.soilTable;
                Utils.information(string.Format("Soil lookup table: {0}", this._gv.soilTable), true);
            } else {
                // allow user to choose
                luse = "";
                soil = "";
            }
            this._dlg.progress("Checking landuses ...");
            Cursor.Current = Cursors.WaitCursor;
            if (!this.initLanduses(luse)) {
                Cursor.Current = Cursors.Default;
                this._dlg.progress("");
                return;
            }
            //Utils.information('Using {0} as landuse table', self._gv.landuseTable), self._gv.isBatch)
            this._dlg.progress("Checking soils ...");
            if (!this._dlg.initSoils(soil, this._dlg.readFromMapsChecked())) {
                Cursor.Current = Cursors.Default;
                this._dlg.progress("");
                return;
            }
            //Utils.information('Using {0} as soil table', self._gv.soilTable), self._gv.isBatch)
            if (this._dlg.readFromPreviousChecked()) {
                // read from database
                this._dlg.progress("Reading basin data from database ...");
                bool OK;
                this.CreateHRUs.basins = this._gv.db.regenerateBasins(out OK);
                //TODO
                //if (this._gv.isHUC || this._gv.isHAWQS) {
                //    this.CreateHRUs.addWaterBodies();
                //}
                this._dlg.progress("");
                this.CreateHRUs.saveAreas(true);
                if (OK) {
                    if (this._gv.useGridModel && this._gv.isBig) {
                        // TODO: this.rewriteWHUTables();
                    } else {
                        this._dlg.setToCreateHRUs();
                        this._reportsCombo.Visible = true;
                        var index = this._reportsCombo.Items.IndexOf(Parameters._TOPOITEM);
                        if (index < 0) {
                            this._reportsCombo.Items.Add(Parameters._TOPOITEM);
                        }
                        index = this._reportsCombo.Items.IndexOf(Parameters._BASINITEM);
                        if (index < 0) {
                            this._reportsCombo.Items.Add(Parameters._BASINITEM);
                        }
                    }
                }
            } else {
                this._dlg.progress("Reading rasters ...");
                this._dlg.setProgressBar(0);
                if (this._dlg.generateFullHRUsChecked()) {
                    this.CreateHRUs.fullHRUsWanted = true;
                    await Utils.removeLayer(this._gv.fullHRUsFile);
                    await Utils.removeLayer(this._gv.actHRUsFile);
                } else {
                    // remove any full and actual HRUs layers and files
                    this.CreateHRUs.fullHRUsWanted = false;
                    FeatureLayer fullHRUsLayer = Utils.getLayerByLegend(Utils._FULLHRUSLEGEND) as FeatureLayer;
                    if (fullHRUsLayer is not null) {
                        var fullHRUsFile = await Utils.layerFilename(fullHRUsLayer);
                        await Utils.removeLayer(fullHRUsFile);
                        FeatureLayer actHRUsLayer = Utils.getLayerByLegend(Utils._ACTHRUSLEGEND) as FeatureLayer;
                        if (actHRUsLayer is not null) {
                            var actHRUsFile = await Utils.layerFilename(actHRUsLayer);
                            await Utils.removeLayer(actHRUsFile);
                        }
                    }
                }
                DateTime time1 = DateTime.Now;
                var OK = await this.CreateHRUs.generateBasins(this._dlg);
                DateTime time2 = DateTime.Now;
                Utils.loginfo(string.Format("Generating basins took {0} seconds", Convert.ToInt32(time2.Subtract(time1).TotalSeconds)));
                this._dlg.progress("");
                if (!OK) {
                    this._dlg.hideProgressBar();
                    Cursor.Current = Cursors.Default;
                    return;
                }
                // now have occurrences of landuses and soils, so can make proper colour schemes and legend entries
                // causes problems for HUC models, and no point in any case when batch
                if (!this._gv.isBatch) {
                    FileTypes.colourLanduses(this.landuseLayer, this._gv);
                    FileTypes.colourSoils(this.soilLayer, this._gv);
                    // TODO (if necessary)
                    //var treeModel = QgsLayerTreeModel(root);
                    //var landuseTreeLayer = root.findLayer(this.landuseLayer.id());
                    //treeModel.refreshLayerLegend(landuseTreeLayer);
                    //var soilTreeLayer = root.findLayer(this.soilLayer.id());
                    //treeModel.refreshLayerLegend(soilTreeLayer);
                    // this seems to be too early: slope bands file not yet written
                    //if (!this._gv.useGridModel && this._gv.db.slopeLimits.Count > 0) {
                    //    RasterLayer slopeBandsLayer = (await Utils.getLayerByFilename(this._gv.slopeBandsFile, FileTypes._SLOPEBANDS, this._gv, null, Utils._SLOPE_GROUP_NAME)).Item1 as RasterLayer;
                        //    if (slopeBandsLayer is not null) {
                        //        var slopeBandsTreeLayer = root.findLayer(slopeBandsLayer.id());
                        //        treeModel.refreshLayerLegend(slopeBandsTreeLayer);
                        //    }
                    //    }
                    }
                if (!this._gv.useGridModel || !this._gv.isBig) {
                    if (this._gv.isBatch) {
                        Utils.information("Writing landuse and soil report ...", true);
                    }
                    this.CreateHRUs.printBasins(false);
                }
                this._dlg.hideProgressBar();
            }
            this._dlg.setToCreateHRUs(true);
            this.saveProj();
            if (this._gv.useGridModel && this._gv.isBig) {
                this._gv.writeMasterProgress(-1, 1);
                //var msg = "HRUs done";
                this.completed = true;
                this._dlg.Close();
            }
            //if (this._gv.forTNC) {
            //    this.addWeather();
            //}
            Cursor.Current = Cursors.Default;
            return;
        }

        //// Add weather data (for TNC project only)
        //public virtual void addWeather() {
        //    object continent;
        //    var continent1 = os.path.basename(os.path.normpath(os.path.join(this._gv.projDir, "../..")));
        //    // remove underscore if any and anything after 
        //    var underscoreIndex = continent1.find("_");
        //    if (underscoreIndex < 0) {
        //        continent = continent1;
        //    } else {
        //        continent = continent1[:underscoreIndex:];
        //    }
        //    var extent = Parameters.TNCExtents.get(continent, (-180, -60, 180, 60));
        //    this.addWgn(extent);
        //    if (this.weatherSource == "CHIRPS") {
        //        this.addCHIRPS(extent, continent);
        //    } else if (this.weatherSource == "ERA5") {
        //        this.addERA5(extent, continent);
        //    } else {
        //        Utils.error(string.Format("Unknown weather source for TNC project: {0}", this.weatherSource), this._gv.isBatch);
        //    }
        //}

        //// Make table lat -> long -> station id.
        //public virtual object addWgn(object extent) {
        //    object nearestWgn(object point) {
        //        object bestWgnId(List<object> candidates, object point, double latitudeFactor) {
        //            (bestId, bestLat, bestLon) = candidates.pop(0);
        //            px = point.x();
        //            py = point.y();
        //            dy = bestLat - py;
        //            dx = (bestLon - px) * latitudeFactor;
        //            measure = dx * dx + dy * dy;
        //            foreach (var (id1, lat1, lon1) in candidates) {
        //                dy1 = lat1 - py;
        //                dx1 = (lon1 - px) * latitudeFactor;
        //                measure1 = dx1 * dx1 + dy1 * dy1;
        //                if (measure1 < measure) {
        //                    measure = measure1;
        //                    bestId = id1;
        //                    bestLat = lat1;
        //                    bestLon = lon1;
        //                }
        //            }
        //            return (bestId, Topology.distance(py, px, bestLat, bestLon));
        //        }
        //        // fraction to reduce E-W distances to allow for latitude    
        //        latitudeFactor = math.cos(math.radians(point.y()));
        //        x = round(point.x());
        //        y = round(point.y());
        //        offset = 0;
        //        candidates = new List<object>();
        //        // search in an expanding square centred on (x, y)
        //        while (true) {
        //            foreach (var offsetY in Enumerable.Range(-offset, offset + 1 - -offset)) {
        //                tbl = this.wgnStations.get(y + offsetY, null);
        //                if (tbl is not null) {
        //                    foreach (var offsetX in Enumerable.Range(-offset, offset + 1 - -offset)) {
        //                        // check we are on perimeter, since inner checked on previous iterations
        //                        if (abs(offsetY) == offset || abs(offsetX) == offset) {
        //                            candidates.extend(tbl.get(x + offsetX, new List<object>()));
        //                        }
        //                    }
        //                    if (candidates.Count > 0) {
        //                        return bestWgnId(candidates, point, latitudeFactor);
        //                    } else {
        //                        offset += 1;
        //                        if (offset >= 1000) {
        //                            Utils.error(string.Format("Failed to find wgn station for point ({0},{1})", point.x(), point.y()), this._gv.isBatch);
        //                            //Utils.loginfo('Failed to find wgn station for point ({0},{1})', point.x(), point.y()))
        //                            return (-1, 0);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    var wgnDb = os.path.join(this._gv.TNCDir, Parameters.wgnDb);
        //    this.wgnStations = new dict();
        //    var sql = "SELECT id, lat, lon FROM wgn_cfsr_world";
        //    (minLon, minLat, maxLon, maxLat) = extent;
        //    var oid = 0;
        //    var wOid = 0;
        //    using (var wgnConn = sqlite3.connect(wgnDb), conn = this._db.connect()) {
        //        wgnCursor = wgnConn.cursor();
        //        foreach (var row in wgnCursor.execute(sql)) {
        //            lat = float(row[1]);
        //            lon = float(row[2]);
        //            if (minLon <= lon && lon <= maxLon && (minLat <= lat && lat <= maxLat)) {
        //                intLat = round(lat);
        //                intLon = round(lon);
        //                tbl = this.wgnStations.get(intLat, new dict());
        //                tbl.setdefault(intLon, new List<object>()).append((Convert.ToInt32(row[0]), lat, lon));
        //                this.wgnStations[intLat] = tbl;
        //            }
        //        }
        //        sql0 = "DELETE FROM wgn";
        //        conn.execute(sql0);
        //        sql1 = "DELETE FROM SubWgn";
        //        conn.execute(sql1);
        //        sql2 = @"INSERT INTO wgn VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
        //            ?,?,?,?,?,?,?,?,?,?)";
        //        sql3 = "INSERT INTO SubWgn VALUES(?,?,?,?,?,?,?)";
        //        sql1r = "SELECT name, lat, lon, elev, rain_yrs FROM wgn_cfsr_world WHERE id=?";
        //        sql2r = "SELECT * FROM wgn_cfsr_world_mon WHERE wgn_id=?";
        //        wgnIds = new HashSet<object>();
        //        foreach (var (basin, (centreX, centreY)) in this._gv.topo.basinCentroids) {
        //            SWATBasin = this._gv.topo.basinToSWATBasin.get(basin, 0);
        //            if (SWATBasin > 0) {
        //                centroidll = this._gv.topo.pointToLatLong(QgsPointXY(centreX, centreY));
        //                (wgnId, minDist) = nearestWgn(centroidll);
        //                if (wgnId >= 0) {
        //                    if (true) {
        //                        // wgnId not in wgnIds: changed to include all subbsins in wgn table, even if data repeated
        //                        wgnIds.add(wgnId);
        //                        row1 = wgnCursor.execute(sql1r, ValueTuple.Create(wgnId)).fetchone();
        //                        tmpmx = new dict();
        //                        tmpmn = new dict();
        //                        tmpsdmx = new dict();
        //                        tmpsdmn = new dict();
        //                        pcpmm = new dict();
        //                        pcpsd = new dict();
        //                        pcpskw = new dict();
        //                        prw1 = new dict();
        //                        prw2 = new dict();
        //                        pcpd = new dict();
        //                        pcphh = new dict();
        //                        slrav = new dict();
        //                        dewpt = new dict();
        //                        wndav = new dict();
        //                        foreach (var data in wgnCursor.execute(sql2r, ValueTuple.Create(wgnId))) {
        //                            month = Convert.ToInt32(data[1]);
        //                            tmpmx[month] = float(data[2]);
        //                            tmpmn[month] = float(data[3]);
        //                            tmpsdmx[month] = float(data[4]);
        //                            tmpsdmn[month] = float(data[5]);
        //                            pcpmm[month] = float(data[6]);
        //                            pcpsd[month] = float(data[7]);
        //                            pcpskw[month] = float(data[8]);
        //                            prw1[month] = float(data[9]);
        //                            prw2[month] = float(data[10]);
        //                            pcpd[month] = float(data[11]);
        //                            pcphh[month] = float(data[12]);
        //                            slrav[month] = float(data[13]);
        //                            dewpt[month] = float(data[14]);
        //                            wndav[month] = float(data[15]);
        //                        }
        //                        oid += 1;
        //                        conn.execute(sql2, (oid, SWATBasin, row1[0], float(row1[1]), float(row1[2]), float(row1[3]), float(row1[4]), tmpmx[1], tmpmx[2], tmpmx[3], tmpmx[4], tmpmx[5], tmpmx[6], tmpmx[7], tmpmx[8], tmpmx[9], tmpmx[10], tmpmx[11], tmpmx[12], tmpmn[1], tmpmn[2], tmpmn[3], tmpmn[4], tmpmn[5], tmpmn[6], tmpmn[7], tmpmn[8], tmpmn[9], tmpmn[10], tmpmn[11], tmpmn[12], tmpsdmx[1], tmpsdmx[2], tmpsdmx[3], tmpsdmx[4], tmpsdmx[5], tmpsdmx[6], tmpsdmx[7], tmpsdmx[8], tmpsdmx[9], tmpsdmx[10], tmpsdmx[11], tmpsdmx[12], tmpsdmn[1], tmpsdmn[2], tmpsdmn[3], tmpsdmn[4], tmpsdmn[5], tmpsdmn[6], tmpsdmn[7], tmpsdmn[8], tmpsdmn[9], tmpsdmn[10], tmpsdmn[11], tmpsdmn[12], pcpmm[1], pcpmm[2], pcpmm[3], pcpmm[4], pcpmm[5], pcpmm[6], pcpmm[7], pcpmm[8], pcpmm[9], pcpmm[10], pcpmm[11], pcpmm[12], pcpsd[1], pcpsd[2], pcpsd[3], pcpsd[4], pcpsd[5], pcpsd[6], pcpsd[7], pcpsd[8], pcpsd[9], pcpsd[10], pcpsd[11], pcpsd[12], pcpskw[1], pcpskw[2], pcpskw[3], pcpskw[4], pcpskw[5], pcpskw[6], pcpskw[7], pcpskw[8], pcpskw[9], pcpskw[10], pcpskw[11], pcpskw[12], prw1[1], prw1[2], prw1[3], prw1[4], prw1[5], prw1[6], prw1[7], prw1[8], prw1[9], prw1[10], prw1[11], prw1[12], prw2[1], prw2[2], prw2[3], prw2[4], prw2[5], prw2[6], prw2[7], prw2[8], prw2[9], prw2[10], prw2[11], prw2[12], pcpd[1], pcpd[2], pcpd[3], pcpd[4], pcpd[5], pcpd[6], pcpd[7], pcpd[8], pcpd[9], pcpd[10], pcpd[11], pcpd[12], pcphh[1], pcphh[2], pcphh[3], pcphh[4], pcphh[5], pcphh[6], pcphh[7], pcphh[8], pcphh[9], pcphh[10], pcphh[11], pcphh[12], slrav[1], slrav[2], slrav[3], slrav[4], slrav[5], slrav[6], slrav[7], slrav[8], slrav[9], slrav[10], slrav[11], slrav[12], dewpt[1], dewpt[2], dewpt[3], dewpt[4], dewpt[5], dewpt[6], dewpt[7], dewpt[8], dewpt[9], dewpt[10], dewpt[11], dewpt[12], wndav[1], wndav[2], wndav[3], wndav[4], wndav[5], wndav[6], wndav[7], wndav[8], wndav[9], wndav[10], wndav[11], wndav[12]));
        //                    }
        //                    wOid += 1;
        //                    conn.execute(sql3, (wOid, SWATBasin, minDist, wgnId, row1[0], null, "wgn_cfsr_world"));
        //                }
        //            }
        //        }
        //        conn.commit();
        //    }
        //}

        //======Replaced with newer pcp and tmp data=====================================================================
        // def addCHIRPSpcp(self, extent: Tuple[float, float, float, float], continent: str) -> None:
        //     """Make table row -> col -> station data, create pcp and SubPcp tables."""
        //     # CHIRPS pcp data implicitly uses a grid of width and depth 0.05 degrees, and the stations are situated at the centre points.
        //     # Dividing centre points' latitude and longitude by 0.5 and rounding gives row and column numbers of the CHIRPS grid.
        //     # So doing the same for grid cell centroid gives the position in the grid, if any.  
        //     # If this fails we search for the nearest.
        //     
        //     #========currently not used===============================================================
        //     # def indexToLL(index: int) -> float: 
        //     #     """Convert row or column index to latitude or longitude."""
        //     #     if index < 0:
        //     #         return index * 0.05 - 0.025
        //     #     else:
        //     #         return index * 0.05 + 0.025
        //     #=======================================================================
        //     
        //     def nearestCHIRPS(point: QgsPointXY) -> Tuple[Tuple[int, str, float, float, float], float]:
        //         """Return data of nearest CHIRPS station to point, plus distance in km"""
        //     
        //         def bestCHIRPS(candidates: List[Tuple[int, str, float, float, float]], point: QgsPointXY, latitudeFactor: float) -> Tuple[Tuple[int, str, float, float, float], float]:
        //             """Return nearest candidate to point."""
        //             px = point.x()
        //             py = point.y()   
        //             best = candidates.pop(0)
        //             dy = best[2] - py
        //             dx = (best[3] - px) * latitudeFactor
        //             measure = dx * dx + dy * dy
        //             for nxt in candidates:
        //                 dy1 = nxt[2] - py
        //                 dx1 = (nxt[3] - px) * latitudeFactor 
        //                 measure1 = dx1 * dx1 + dy1 * dy1
        //                 if measure1 < measure:
        //                     best = nxt
        //                     dy = best[2] - py
        //                     dx = best[3] - px
        //             return best, Topology.distance(py, px, best[2], best[3])    
        //                         
        //         cx = point.x()
        //         cy = point.y()
        //         # fraction to reduce E-W distances to allow for latitude 
        //         latitudeFactor = math.cos(math.radians(cy))
        //         centreRow = round(cy / 0.05)
        //         centreCol = round(cx / 0.05)
        //         offset = 0
        //         candidates: List[Tuple[int, str, float, float, float]] = []
        //         # search in an expanding square centred on centreRow, centreCol
        //         while True:
        //             for row in range(centreRow - offset, centreRow + offset + 1):
        //                 tbl = self.CHIRPSpcpStations.get(row, None)
        //                 if tbl is not None:
        //                     for col in range(centreCol - offset, centreCol + offset + 1):
        //                         # check we are on perimeter, since inner checked on previous iterations
        //                         if row in {centreRow - offset, centreRow + offset} or col in {centreCol - offset, centreCol + offset}:
        //                             data = tbl.get(col, None)
        //                             if data is not None:
        //                                 candidates.append(data)
        //             if len(candidates) > 0:
        //                 return bestCHIRPS(candidates, point, latitudeFactor)
        //             offset += 1
        //             if offset >= 1000:
        //                 Utils.error('Failed to find CHIRPS precipitation station for point ({0},{1})', cy, cx), self._gv.isBatch)
        //                 #Utils.loginfo('Failed to find CHIRPS precipitation station for point ({0},{1})', cy, cx))
        //                 return None, 0  
        //         
        //     CHIRPSGrids = os.path.join(self._gv.globaldata, os.path.join(Parameters.CHIRPSpcpDir, Parameters.CHIRPSGridsDir))
        //     #print('CHIRPSGrids: {0}', CHIRPSGrids))
        //     self.CHIRPSpcpStations = dict()
        //     minLon, minLat, maxLon, maxLat = extent
        //     for f in Parameters.CHIRPSpcpStationsCsv.get(continent, []):
        //         inFile = os.path.join(CHIRPSGrids, f)
        //         with open(inFile,'r') as csvFile:
        //             reader= csv.reader(csvFile)
        //             _ = next(reader)  # skip header
        //             for line in reader:  # ID, NAME, LAT, LONG, ELEVATION
        //                 lat = float(line[2])
        //                 lon = float(line[3])
        //                 if minLon <= lon <= maxLon and minLat <= lat <= maxLat:
        //                     row = round(lat / 0.05)
        //                     col = round(lon / 0.05)
        //                     tbl = self.CHIRPSpcpStations.get(row, dict())
        //                     tbl[col] = (int(line[0]), line[1], lat, lon, float(line[4]))   #ID, NAME, LAT, LONG, ELEVATION
        //                     self.CHIRPSpcpStations[row] = tbl 
        //     with self._db.connect() as conn:          
        //         sql0 = 'DELETE FROM pcp'
        //         conn.execute(sql0)
        //         sql0 = 'DELETE FROM SubPcp'
        //         conn.execute(sql0)
        //         sql1 = 'INSERT INTO pcp VALUES(?,?,?,?,?)'
        //         sql2 = 'INSERT INTO SubPcp VALUES(?,?,?,?,?,?,0)'
        //         # map of CHIRPS station name to column in data txt file and position in pcp table
        //         # don't use id as will not be unique if more than one set of CHIRPS data: eg Europe also uses Asia
        //         pcpIds: Dict[str, Tuple[int, int]] = dict()
        //         minRec = 0
        //         orderId = 0
        //         oid = 0
        //         poid = 0
        //         for basin, (centreX, centreY) in self._gv.topo.basinCentroids:
        //             SWATBasin = self._gv.topo.basinToSWATBasin.get(basin, 0)
        //             if SWATBasin > 0:
        //                 centroidll = self._gv.topo.pointToLatLong(QgsPointXY(centreX, centreY))
        //                 data, distance = nearestCHIRPS(centroidll)
        //                 if data is not None:
        //                     pcpId = data[1]
        //                     minRec1, orderId1 = pcpIds.get(pcpId, (0,0))
        //                     if minRec1 == 0:
        //                         minRec += 1
        //                         minRec1 = minRec
        //                         orderId += 1
        //                         orderId1 = orderId
        //                         poid += 1
        //                         conn.execute(sql1, (poid, pcpId, data[2], data[3], data[4]))
        //                         pcpIds[pcpId] = (minRec, orderId)
        //                     oid += 1
        //                     conn.execute(sql2, (oid, SWATBasin, distance, minRec1, pcpId, orderId1))
        //         conn.commit()                
        //===========================================================================
        // Make table row -> col -> station data, create pcp, tmp, SubPcp and SubTmp tables.
        //public virtual object addCHIRPS(object extent, string continent) {
        //    object nearestCHIRPS(object point) {
        //        object bestCHIRPS(List<object> candidates, object point, double latitudeFactor) {
        //            px = point.x();
        //            py = point.y();
        //            best = candidates.pop(0);
        //            dy = best[2] - py;
        //            dx = (best[3] - px) * latitudeFactor;
        //            measure = dx * dx + dy * dy;
        //            foreach (var nxt in candidates) {
        //                dy1 = nxt[2] - py;
        //                dx1 = (nxt[3] - px) * latitudeFactor;
        //                measure1 = dx1 * dx1 + dy1 * dy1;
        //                if (measure1 < measure) {
        //                    best = nxt;
        //                    dy = best[2] - py;
        //                    dx = best[3] - px;
        //                }
        //            }
        //            return (best, Topology.distance(py, px, best[2], best[3]));
        //        }
        //        // fraction to reduce E-W distances to allow for latitude 
        //        latitudeFactor = math.cos(math.radians(point.y()));
        //        x = round(point.x());
        //        y = round(point.y());
        //        offset = 0;
        //        candidates = new List<object>();
        //        // search in an expanding square centred on (x, y)
        //        while (true) {
        //            foreach (var offsetY in Enumerable.Range(-offset, offset + 1 - -offset)) {
        //                tbl = this.CHIRPSStations.get(y + offsetY, null);
        //                if (tbl is not null) {
        //                    foreach (var offsetX in Enumerable.Range(-offset, offset + 1 - -offset)) {
        //                        // check we are on perimeter, since inner checked on previous iterations
        //                        if (abs(offsetY) == offset || abs(offsetX) == offset) {
        //                            candidates.extend(tbl.get(x + offsetX, new List<object>()));
        //                        }
        //                    }
        //                }
        //            }
        //            if (candidates.Count > 0) {
        //                return bestCHIRPS(candidates, point, latitudeFactor);
        //            }
        //            offset += 1;
        //            if (offset >= 1000) {
        //                Utils.error(string.Format("Failed to find CHIRPS station for point ({0},{1})", point.x(), point.y()), this._gv.isBatch);
        //                //Utils.loginfo('Failed to find CHIRPS station for point ({0},{1})', cy, cx))
        //                return (null, 0);
        //            }
        //        }
        //    }
        //    var CHIRPSGrids = os.path.join(this._gv.globaldata, Parameters.CHIRPSDir);
        //    this.CHIRPSStations = new dict();
        //    (minLon, minLat, maxLon, maxLat) = extent;
        //    foreach (var f in Parameters.CHIRPSStationsCsv.get(continent, new List<object>())) {
        //        var inFile = os.path.join(CHIRPSGrids, f);
        //        using (var csvFile = open(inFile, "r")) {
        //            reader = csv.reader(csvFile);
        //            _ = next(reader);
        //            foreach (var line in reader) {
        //                // ID, NAME, LAT, LONG, ELEVATION
        //                lat = float(line[2]);
        //                lon = float(line[3]);
        //                if (minLon <= lon && lon <= maxLon && (minLat <= lat && lat <= maxLat)) {
        //                    intLat = round(lat);
        //                    intLon = round(lon);
        //                    tbl = this.CHIRPSStations.get(intLat, new dict());
        //                    tbl.setdefault(intLon, new List<object>()).append((Convert.ToInt32(line[0]), line[1], lat, lon, float(line[4])));
        //                    this.CHIRPSStations[intLat] = tbl;
        //                }
        //            }
        //        }
        //    }
        //    using (var conn = this._db.connect()) {
        //        sql0 = "DELETE FROM pcp";
        //        conn.execute(sql0);
        //        sql0 = "DELETE FROM SubPcp";
        //        conn.execute(sql0);
        //        sql0 = "DELETE FROM tmp";
        //        conn.execute(sql0);
        //        sql0 = "DELETE FROM SubTmp";
        //        conn.execute(sql0);
        //        sql1 = "INSERT INTO pcp VALUES(?,?,?,?,?)";
        //        sql2 = "INSERT INTO SubPcp VALUES(?,?,?,?,?,?,0)";
        //        sql3 = "INSERT INTO tmp VALUES(?,?,?,?,?)";
        //        sql4 = "INSERT INTO SubTmp VALUES(?,?,?,?,?,?,0)";
        //        //map of CHIRPS station name to column in data txt file and position in pcp table.  tmp uses same data
        //        pcpIds = new dict();
        //        minRec = 0;
        //        orderId = 0;
        //        oid = 0;
        //        poid = 0;
        //        foreach (var (basin, (centreX, centreY)) in this._gv.topo.basinCentroids) {
        //            SWATBasin = this._gv.topo.basinToSWATBasin.get(basin, 0);
        //            if (SWATBasin > 0) {
        //                centroidll = this._gv.topo.pointToLatLong(QgsPointXY(centreX, centreY));
        //                (data, distance) = nearestCHIRPS(centroidll);
        //                if (data is not null) {
        //                    pcpId = data[1];
        //                    (minRec1, orderId1) = pcpIds.get(pcpId, (0, 0));
        //                    if (minRec1 == 0) {
        //                        minRec += 1;
        //                        minRec1 = minRec;
        //                        orderId += 1;
        //                        orderId1 = orderId;
        //                        poid += 1;
        //                        conn.execute(sql1, (poid, pcpId, data[2], data[3], data[4]));
        //                        conn.execute(sql3, (poid, pcpId, data[2], data[3], data[4]));
        //                        pcpIds[pcpId] = (minRec, orderId);
        //                    }
        //                    oid += 1;
        //                    conn.execute(sql2, (oid, SWATBasin, distance, minRec1, pcpId, orderId1));
        //                    conn.execute(sql4, (oid, SWATBasin, distance, minRec1, pcpId, orderId1));
        //                }
        //            }
        //        }
        //        conn.commit();
        //    }
        //}

        //// Make table row -> col -> station data, create pcp and SubPcp tables, plus tmp and SubTmp tables.
        //public virtual object addERA5(object extent, string continent) {
        //    object nearestERA5(object point) {
        //        object bestERA5(List<object> candidates, object point, double latitudeFactor) {
        //            px = point.x();
        //            py = point.y();
        //            best = candidates.pop(0);
        //            dy = best[2] - py;
        //            dx = (best[3] - px) * latitudeFactor;
        //            measure = dx * dx + dy * dy;
        //            foreach (var nxt in candidates) {
        //                dy1 = nxt[2] - py;
        //                dx1 = (nxt[3] - px) * latitudeFactor;
        //                measure1 = dx1 * dx1 + dy1 * dy1;
        //                if (measure1 < measure) {
        //                    best = nxt;
        //                    dy = best[2] - py;
        //                    dx = best[3] - px;
        //                }
        //            }
        //            return (best, Topology.distance(py, px, best[2], best[3]));
        //        }
        //        // fraction to reduce E-W distances to allow for latitude 
        //        latitudeFactor = math.cos(math.radians(point.y()));
        //        x = round(point.x());
        //        y = round(point.y());
        //        offset = 0;
        //        candidates = new List<object>();
        //        // search in an expanding square centred on (x, y)
        //        while (true) {
        //            foreach (var offsetY in Enumerable.Range(-offset, offset + 1 - -offset)) {
        //                tbl = this.ERA5Stations.get(y + offsetY, null);
        //                if (tbl is not null) {
        //                    foreach (var offsetX in Enumerable.Range(-offset, offset + 1 - -offset)) {
        //                        // check we are on perimeter, since inner checked on previous iterations
        //                        if (abs(offsetY) == offset || abs(offsetX) == offset) {
        //                            candidates.extend(tbl.get(x + offsetX, new List<object>()));
        //                        }
        //                    }
        //                }
        //            }
        //            if (candidates.Count > 0) {
        //                return bestERA5(candidates, point, latitudeFactor);
        //            }
        //            offset += 1;
        //            if (offset >= 1000) {
        //                Utils.error(string.Format("Failed to find ERA5 station for point ({0},{1})", point.x(), point.y()), this._gv.isBatch);
        //                //Utils.loginfo('Failed to find ERA5 station for point ({0},{1})', cy, cx))
        //                return (null, 0);
        //            }
        //        }
        //    }
        //    var ERA5Grids = os.path.join(this._gv.globaldata, os.path.join(Parameters.ERA5Dir, Parameters.ERA5GridsDir));
        //    this.ERA5Stations = new dict();
        //    (minLon, minLat, maxLon, maxLat) = extent;
        //    foreach (var f in Parameters.ERA5StationsCsv.get(continent, new List<object>())) {
        //        var inFile = os.path.join(ERA5Grids, f);
        //        using (var csvFile = open(inFile, "r")) {
        //            reader = csv.reader(csvFile);
        //            _ = next(reader);
        //            foreach (var line in reader) {
        //                // ID, NAME, LAT, LONG, ELEVATION
        //                lat = float(line[2]);
        //                lon = float(line[3]);
        //                if (minLon <= lon && lon <= maxLon && (minLat <= lat && lat <= maxLat)) {
        //                    intLat = round(lat);
        //                    intLon = round(lon);
        //                    tbl = this.ERA5Stations.get(intLat, new dict());
        //                    tbl.setdefault(intLon, new List<object>()).append((Convert.ToInt32(line[0]), line[1], lat, lon, float(line[4])));
        //                    this.ERA5Stations[intLat] = tbl;
        //                }
        //            }
        //        }
        //    }
        //    using (var conn = this._db.connect()) {
        //        sql0 = "DELETE FROM pcp";
        //        conn.execute(sql0);
        //        sql0 = "DELETE FROM SubPcp";
        //        conn.execute(sql0);
        //        sql0 = "DELETE FROM tmp";
        //        conn.execute(sql0);
        //        sql0 = "DELETE FROM SubTmp";
        //        conn.execute(sql0);
        //        sql1 = "INSERT INTO pcp VALUES(?,?,?,?,?)";
        //        sql2 = "INSERT INTO SubPcp VALUES(?,?,?,?,?,?,0)";
        //        sql3 = "INSERT INTO tmp VALUES(?,?,?,?,?)";
        //        sql4 = "INSERT INTO Subtmp VALUES(?,?,?,?,?,?,0)";
        //        //map of ERA5 station name to column in data txt file and position in pcp table.  tmp uses same data
        //        pcpIds = new dict();
        //        minRec = 0;
        //        orderId = 0;
        //        oid = 0;
        //        poid = 0;
        //        foreach (var (basin, (centreX, centreY)) in this._gv.topo.basinCentroids) {
        //            SWATBasin = this._gv.topo.basinToSWATBasin.get(basin, 0);
        //            if (SWATBasin > 0) {
        //                centroidll = this._gv.topo.pointToLatLong(QgsPointXY(centreX, centreY));
        //                (data, distance) = nearestERA5(centroidll);
        //                if (data is not null) {
        //                    pcpId = data[1];
        //                    (minRec1, orderId1) = pcpIds.get(pcpId, (0, 0));
        //                    if (minRec1 == 0) {
        //                        minRec += 1;
        //                        minRec1 = minRec;
        //                        orderId += 1;
        //                        orderId1 = orderId;
        //                        poid += 1;
        //                        conn.execute(sql1, (poid, pcpId, data[2], data[3], data[4]));
        //                        conn.execute(sql3, (poid, pcpId, data[2], data[3], data[4]));
        //                        pcpIds[pcpId] = (minRec, orderId);
        //                    }
        //                    oid += 1;
        //                    conn.execute(sql2, (oid, SWATBasin, distance, minRec1, pcpId, orderId1));
        //                    conn.execute(sql4, (oid, SWATBasin, distance, minRec1, pcpId, orderId1));
        //                }
        //            }
        //        }
        //        conn.commit();
        //    }
        //}

        // Set up landuse lookup tables.
        public virtual bool initLanduses(string table) {
            this._gv.db.landuseVals = new List<int>();
            if (table == "") {
                this._gv.landuseTable = this._dlg.currentLanduse;
                if (this._gv.landuseTable == Parameters._USECSV) {
                    this._gv.landuseTable = this.readLanduseCsv();
                    if (this._gv.landuseTable != "") {
                        this._dlg.currentLanduse = this._gv.landuseTable;
                    }
                }
                if (!this._gv.db.landuseTableNames.Contains(this._gv.landuseTable)) {
                    Utils.error("Please select a landuse table", this._gv.isBatch);
                    return false;
                }
            } else {
                // doing tryRun and table already read from project file
                this._gv.landuseTable = table;
            }
            return this._gv.db.populateLanduseCodes(this._gv.landuseTable);
        }

        // Read landuse csv file.
        public virtual string readLanduseCsv() {
            return this.readCsv("landuse", this._gv.db.landuseTableNames);
        }

        // Read soil csv file.
        public virtual string readSoilCsv() {
            return this.readCsv("soil", this._gv.db.soilTableNames);
        }

        // Invite reader to choose csv file and read it.
        public virtual string readCsv(string typ, List<string> names) {
            string path = "";
            //var settings = QSettings();
            //if (settings.contains("/QSWAT/LastInputPath")) {
            //    path = settings.value("/QSWAT/LastInputPath");
            //} else {
            //    path = "";
            //}
            var title = Utils.trans(string.Format("Choose {0} lookup csv file", typ));
            var filtr = Utils.trans("CSV files (*.csv)|*.csv|All files (*.*)|*.*");
            string csvFile = "";
            using (OpenFileDialog dlg = new OpenFileDialog()) {
                dlg.InitialDirectory = path;
                dlg.Title = title;
                dlg.Filter = filtr;
                dlg.RestoreDirectory = false;
                if (dlg.ShowDialog() == DialogResult.OK) {
                    csvFile = dlg.FileName;
                }
            }
            if (!string.IsNullOrEmpty(csvFile)) {
                // settings.Value = "/QSWAT/LastInputPath", os.path.dirname(csvFile);
                return this.readCsvFile(csvFile, typ, names);
            } else {
                return "";
            }
        }

        // Read csv file.
        public virtual string readCsvFile(string csvFile, string typ, List<string> names) {
            var table = Path.GetFileNameWithoutExtension(csvFile);
            if (!table.Contains(typ)) {
                table = string.Format("{0}_lookup", typ);
            }
            var @base = table;
            var i = 0;
            while (names.Contains(table)) {
                table = @base + i.ToString();
                i = i + 1;
            }
            return this._gv.db.importCsv(table, typ, csvFile);
        }

        // Create HRUs.
        public async void calcHRUs() {
            // if this is done in task readFiles seems to be called too early
            if (!this._gv.useGridModel && this._gv.db.slopeLimits.Count > 0) {
                RasterLayer slopeBandsLayer = (await Utils.getLayerByFilename(this._gv.slopeBandsFile, FileTypes._SLOPEBANDS, this._gv, null, Utils._SLOPE_GROUP_NAME)).Item1 as RasterLayer;
            }
            DateTime time2;
            DateTime time1;
            this._gv.writeMasterProgress(-1, 0);
            try {
                Cursor.Current = Cursors.WaitCursor;
                this._dlg.setForCalcHRUs();
                this.CreateHRUs.isDominantHRU = this._dlg.isDominantHRU;
                this.CreateHRUs.isMultiple = this._dlg.isMultiple;
                this.CreateHRUs.useArea = this._dlg.useArea;
                if (!this._gv.saveExemptSplit()) {
                    Cursor.Current = Cursors.Default;
                    return;
                }
                if (this._gv.forTNC) {
                    this.CreateHRUs.removeSmallHRUsbySubbasinTarget();
                } else if (this.CreateHRUs.isMultiple) {
                    if (this.CreateHRUs.isArea) {
                        this.CreateHRUs.removeSmallHRUsByArea();
                    } else if (this.CreateHRUs.isTarget) {
                        this.CreateHRUs.removeSmallHRUsbyTarget();
                    } else {
                        if (this._gv.db.slopeLimits.Count == 0) {
                            this.CreateHRUs.slopeVal = 0;
                        }
                        // allow too tight thresholds, since we guard against removing all HRUs from a subbasin
                        // if not self.CreateHRUs.cropSoilAndSlopeThresholdsAreOK():
                        //     Utils.error('Internal error: problem with tight thresholds', self._gv.isBatch)
                        //     return
                        if (this.CreateHRUs.useArea) {
                            this.CreateHRUs.removeSmallHRUsByThresholdArea();
                        } else {
                            this.CreateHRUs.removeSmallHRUsByThresholdPercent();
                        }
                    }
                    if (!this.CreateHRUs.splitHRUs()) {
                        Cursor.Current = Cursors.Default;
                        return;
                    }
                }
                this.CreateHRUs.saveAreas(false);
                this.CreateHRUs.basinsToHRUs();
                //var fullHRUsLayer = (await Utils.getLayerByFilename(this._gv.fullHRUsFile, FileTypes._HRUS, null, null, null)).Item1 as FeatureLayer;
                if (this._gv.isBatch) {
                    Utils.information("Writing HRUs report ...", true);
                }
                if (this._gv.useGridModel && this._gv.isBig) {
                    time1 = DateTime.Now;
                    this.CreateHRUs.writeHRUsAndUncombTables();
                    time2 = DateTime.Now;
                    Utils.loginfo(string.Format("Writing hrus and uncomb tables took {0} seconds", Convert.ToInt32(time2.Subtract(time1).TotalSeconds)));
                } else {
                    this.CreateHRUs.printBasins(true);
                }
                time1 = DateTime.Now;
                this.CreateHRUs.writeWatershedTable();
                time2 = DateTime.Now;
                Utils.loginfo(string.Format("Writing Watershed table took {0} seconds", Convert.ToInt32(time2.Subtract(time1).TotalSeconds)));
                this._gv.writeMasterProgress(-1, 1);
                var msg = string.Format("HRUs done: {0} HRUs formed in {1} subbasins.", this.CreateHRUs.hrus.Count, this._gv.topo.basinToSWATBasin.Count);
                //Utils.information(msg, this._gv.isBatch);
                if (this._gv.isBatch) {
                    Console.WriteLine(msg);
                }
                this.saveProj();
                this.completed = true;
            } catch (Exception ex) {
                Utils.error(string.Format("Failed to create HRUs: {0}", ex.Message), this._gv.isBatch);
            } finally {
                Cursor.Current = Cursors.Default;
                if (this.completed) {
                    this._dlg.Close();
                    this._parent.postCreateHRUs(true);
                }
            }
        }

        public virtual void cancelHRUs() {
            this.completed = false;
            this._dlg.Close();
            this._parent.postCreateHRUs(false);
        }

        // Write HRU settings to the project file.
        public virtual void saveProj() {
            var proj = this._gv.proj;
            var title = this._gv.projName;
            proj.writeEntry(title, "landuse/file", Utils.relativise(this.landuseFile, this._gv.projDir));
            proj.writeEntry(title, "soil/file", Utils.relativise(this.soilFile, this._gv.projDir));
            proj.writeEntry(title, "landuse/table", this._gv.landuseTable);
            proj.writeEntry(title, "soil/table", this._gv.soilTable);
            proj.writeEntryBool(title, "soil/useSTATSGO", this._gv.db.useSTATSGO);
            proj.writeEntryBool(title, "soil/useSSURGO", this._gv.db.useSSURGO);
            proj.writeNumEntry(title, "hru/elevBandsThreshold", this._gv.elevBandsThreshold);
            proj.writeNumEntry(title, "hru/numElevBands", this._gv.numElevBands);
            proj.writeEntry(title, "hru/slopeBands", Utils.slopesToString(this._gv.db.slopeLimits));
            proj.writeEntry(title, "hru/slopeBandsFile", Utils.relativise(this._gv.slopeBandsFile, this._gv.projDir));
            proj.writeEntryBool(title, "hru/isMultiple", this.CreateHRUs.isMultiple);
            proj.writeEntryBool(title, "hru/isDominantHRU", this.CreateHRUs.isDominantHRU);
            proj.writeEntryBool(title, "hru/isArea", this.CreateHRUs.isArea);
            proj.writeEntryBool(title, "hru/isTarget", this.CreateHRUs.isTarget);
            proj.writeEntryBool(title, "hru/useArea", this.CreateHRUs.useArea);
            proj.writeNumEntry(title, "hru/areaVal", this.CreateHRUs.areaVal);
            proj.writeNumEntry(title, "hru/landuseVal", this.CreateHRUs.landuseVal);
            proj.writeNumEntry(title, "hru/soilVal", this.CreateHRUs.soilVal);
            proj.writeNumEntry(title, "hru/slopeVal", this.CreateHRUs.slopeVal);
            proj.writeNumEntry(title, "hru/targetVal", this.CreateHRUs.targetVal);
        }
    }

    //  Generate HRU data for SWAT.  Inputs are basins, landuse, soil and slope grids.
    public class CreateHRUs {

        public GlobalVars _gv;

        public ComboBox _reportsCombo;

        public HRUsForm _dlg;

        public int areaVal;

        public Dictionary<int, List<(double, double, double)>> basinElevBands;

        public Dictionary<int, List<int>> basinElevMap;

        public Dictionary<int, BasinData> basins;

        public int defaultNoData;

        public List<int> elevMap;

        public bool emptyHUCProject;

        public bool fullHRUsWanted;

        public Dictionary<int, HRUData> hrus;

        public bool isArea;

        public bool isDominantHRU;

        public bool isMultiple;

        public bool isTarget;

        public int landuseVal;

        public double minElev;

        public int slopeVal;

        public int soilVal;

        public int targetVal;

        public bool useArea;

        public CreateHRUs(GlobalVars gv, ComboBox reportsCombo, HRUsForm _dlg) {
            //# Map of basin number to basin data
            this.basins = new Dictionary<int, BasinData>();
            //# Map of hru number to hru data
            this.hrus = new Dictionary<int, HRUData>();
            this._gv = gv;
            this._reportsCombo = reportsCombo;
            this._dlg = _dlg;
            //# Minimum elevation in watershed
            this.minElev = 0;
            //# Array of elevation frequencies for whole watershed
            // Index i in array corresponds to elevation elevationGrid.Minimum + i.
            // Used to generate elevation report.
            this.elevMap = new List<int>();
            //# Map from basin number to array of elevation frequencies.
            // Index i in array corresponds to elevation minElev + i.
            // Used to generate elevation report.
            this.basinElevMap = new Dictionary<int, List<int>>();
            //# Map from SWAT basin number to list of (start of band elevation, mid point, percent of subbasin area) pairs.
            // List is None if bands not wanted or maximum elevation of subbasin below threshold.
            this.basinElevBands = new Dictionary<int, List<(double, double, double)>>();
            // HRU parameters
            //# Flag indicating multiple/single HRUs per subbasin
            this.isMultiple = false;
            //# For single HRU, flag indicating if dominant
            this.isDominantHRU = true;
            //# Flag indicationg if filtering by area
            this.isArea = false;
            //# Flag indicating if filtering  by target number of HRUs
            this.isTarget = false;
            //# Flag indicating selection by area (else by percentage)
            this.useArea = false;
            //# Current value of area slider
            this.areaVal = 0;
            //# Current value of landuse slider
            this.landuseVal = 0;
            //# Current value of soil slider
            this.soilVal = 0;
            //# Current value of slope slider
            this.slopeVal = 0;
            //# Current value of target slider
            this.targetVal = 0;
            //# Flag indicating if full HRUs file to be generated
            this.fullHRUsWanted = false;
            //# value to use when landuse and soil maps have no noData value
            this.defaultNoData = -32768;
            //# flag used with HUC projects to indicate empty project: not  an error as can be caused by lack of soil coverage
            this.emptyHUCProject = false;
        }


        // Generate basin data from watershed, landuse, soil and slope grids.
        public async Task<bool> generateBasins(Form hrusForm) {
            double soilDefinedPercent = 0;
            double landusePercent;
            int hru;
            int slope;
            double slopeValue;
            int slopeCol;
            string cropCode;
            bool soilIsNoData;
            int soil;
            int soilCol;
            int crop;
            int cropCol;
            double dist;
            double x;
            Dictionary<int, Dictionary<int, Dictionary<int, int>>> cropSoilSlopeNumbers = null;
            int extra;
            int index;
            double elevation;
            int slopeRow;
            int soilRow;
            int cropRow;
            double y;
            BasinData data;
            double length;
            double drop;
            double outletElev;
            //Range colRange;
            //Range rowRange;
            //int elevationTopRow;
            ReachData reachData;
            double[] transform;
            int lastHru = 0;
            int basinReadRows;
            int slopeActReadRows;
            int slopeReadRows;
            int soilActReadRows;
            int soilReadRows;
            int cropActReadRows;
            int cropReadRows;
            int elevationReadRows;
            double minDist;
            OSGeo.GDAL.Dataset hrusRasterDs;
            OSGeo.GDAL.Dataset slopeBandsDs;
            OSGeo.GDAL.Driver driver;
            string proj;
            int basin;
            int link;
            OSGeo.GDAL.Dataset distDs = null;
            //GdalBase.ConfigureAll();
            // in case this is a rerun
            this.basins.Clear();
            var elevationDs = Gdal.Open(this._gv.demFile, Access.GA_ReadOnly);
            if (elevationDs is null) {
                Utils.error(string.Format("Cannot open DEM {0}", this._gv.demFile), this._gv.isBatch);
                return false;
            }
            var basinDs = Gdal.Open(this._gv.basinFile, Access.GA_ReadOnly);
            if (basinDs is null) {
                Utils.error(string.Format("Cannot open watershed grid {0}", this._gv.basinFile), this._gv.isBatch);
                return false;
            }
            var basinNumberRows = basinDs.RasterYSize;
            var basinNumberCols = basinDs.RasterXSize;
            var fivePercent = Convert.ToInt32(basinNumberRows / 20);
            double[] basinTransform = new double[6];
            basinDs.GetGeoTransform(basinTransform);
            var basinBand = basinDs.GetRasterBand(1);
            double basinNoData;
            int basinHasNoData;
            basinBand.GetNoDataValue(out basinNoData, out basinHasNoData);
            // can have -128 as the nodata value while map read returns 128
            basinNoData = Math.Abs(basinNoData);
            if (!this._gv.existingWshed && !this._gv.useGridModel) {
                distDs = Gdal.Open(this._gv.distFile, Access.GA_ReadOnly);
                if (distDs is null) {
                    Utils.error(string.Format("Cannot open distance to outlets file {0}", this._gv.distFile), this._gv.isBatch);
                    return false;
                }
            }
            var cropDs = Gdal.Open(this._gv.landuseFile, Access.GA_ReadOnly);
            if (cropDs is null) {
                Utils.error(string.Format("Cannot open landuse file {0}", this._gv.landuseFile), this._gv.isBatch);
                return false;
            }
            var soilDs = Gdal.Open(this._gv.soilFile, Access.GA_ReadOnly);
            if (soilDs is null) {
                Utils.error(string.Format("Cannot open soil file {0}", this._gv.soilFile), this._gv.isBatch);
                return false;
            }
            var slopeDs = Gdal.Open(this._gv.slopeFile, Access.GA_ReadOnly);
            if (slopeDs is null) {
                Utils.error(string.Format("Cannot open slope file {0}", this._gv.slopeFile), this._gv.isBatch);
                return false;
            }
            var distNumberRows = 0;
            var distNumberCols = 0;
            // Loop reading grids is MUCH slower if these are not stored locally
            if (!this._gv.existingWshed && !this._gv.useGridModel) {
                distNumberRows = distDs.RasterYSize;
                distNumberCols = distDs.RasterXSize;
            }
            var cropNumberRows = cropDs.RasterYSize;
            var cropNumberCols = cropDs.RasterXSize;
            var soilNumberRows = soilDs.RasterYSize;
            var soilNumberCols = soilDs.RasterXSize;
            var slopeNumberRows = slopeDs.RasterYSize;
            var slopeNumberCols = slopeDs.RasterXSize;
            var elevationNumberRows = elevationDs.RasterYSize;
            var elevationNumberCols = elevationDs.RasterXSize;
            var distTransform = new double[6];
            if (!this._gv.existingWshed && !this._gv.useGridModel) {
                distDs.GetGeoTransform(distTransform);
            }
            var cropTransform = new double[6];
            cropDs.GetGeoTransform(cropTransform);
            var soilTransform = new double[6];
            soilDs.GetGeoTransform(soilTransform);
            var slopeTransform = new double[6];
            slopeDs.GetGeoTransform(slopeTransform);
            var elevationTransform = new double[6];
            elevationDs.GetGeoTransform(elevationTransform);
            // if rasters have same coords we can use (col, row) from one in another
            Func<int, double, int> cropRowFun = null;
            Func<int, double, int> cropColFun = null;
            Func<int, double, int> soilRowFun = null;
            Func<int, double, int> soilColFun = null;
            Func<int, double, int> slopeRowFun = null;
            Func<int, double, int> slopeColFun = null;
            Func<int, double, int> distRowFun = null;
            Func<int, double, int> distColFun = null;
            Func<int, double, int> elevationRowFun = null;
            Func<int, double, int> elevationColFun = null;
            if (this._gv.useGridModel) {
                (cropRowFun, cropColFun) = Topology.translateCoords(elevationTransform, cropTransform, elevationNumberRows, elevationNumberCols);
                (soilRowFun, soilColFun) = Topology.translateCoords(elevationTransform, soilTransform, elevationNumberRows, elevationNumberCols);
                (slopeRowFun, slopeColFun) = Topology.translateCoords(elevationTransform, slopeTransform, elevationNumberRows, elevationNumberCols);
            } else {
                if (!this._gv.existingWshed) {
                    (distRowFun, distColFun) = Topology.translateCoords(basinTransform, distTransform, basinNumberRows, basinNumberCols);
                }
                (cropRowFun, cropColFun) = Topology.translateCoords(basinTransform, cropTransform, basinNumberRows, basinNumberCols);
                (soilRowFun, soilColFun) = Topology.translateCoords(basinTransform, soilTransform, basinNumberRows, basinNumberCols);
                (slopeRowFun, slopeColFun) = Topology.translateCoords(basinTransform, slopeTransform, basinNumberRows, basinNumberCols);
                (elevationRowFun, elevationColFun) = Topology.translateCoords(basinTransform, elevationTransform, basinNumberRows, basinNumberCols);
            }
            Band distBand = null;
            if (!this._gv.existingWshed && !this._gv.useGridModel) {
                distBand = distDs.GetRasterBand(1);
            }
            var cropBand = cropDs.GetRasterBand(1);
            var soilBand = soilDs.GetRasterBand(1);
            var slopeBand = slopeDs.GetRasterBand(1);
            var elevationBand = elevationDs.GetRasterBand(1);
            double elevationNoData;
            int elevationHasNoData;
            elevationBand.GetNoDataValue(out elevationNoData, out elevationHasNoData);
            if (elevationHasNoData == 0) {
                elevationNoData = this.defaultNoData;
            }
            double distNoData;
            int distHasNoData;
            if (this._gv.existingWshed || this._gv.useGridModel) {
                distNoData = elevationNoData;
            } else {
                distBand.GetNoDataValue(out distNoData, out distHasNoData);
            }
            double cropNoData;
            int cropHasNoData;
            cropBand.GetNoDataValue(out cropNoData, out cropHasNoData);
            if (cropHasNoData == 0) {
                cropNoData = this.defaultNoData;
            }
            double soilNoData;
            int soilHasNoData;
            soilBand.GetNoDataValue(out soilNoData, out soilHasNoData);
            if (soilHasNoData == 0) {
                soilNoData = this.defaultNoData;
            }
            var streamGeoms = new Dictionary<int, OSGeo.OGR.Geometry>();
            var basinStreamWaterData = new Dictionary<int, (OSGeo.OGR.Geometry, double, double)>();
            if (this._gv.isHUC || this._gv.isHAWQS) {
                this._gv.db.SSURGOUndefined = (int)soilNoData;
                // collect data basin -> stream buffer shape, buffer area, WATR area 
                // map basin -> stream geometry
                var streamDs = Ogr.Open(this._gv.streamFile, 0);
                var streamLayer = streamDs.GetLayerByIndex(0);
                var linkIndex = this._gv.topo.getIndex(streamLayer, Topology._LINKNO);
                for (int i = 0; i < streamLayer.GetFeatureCount(1); i++) {
                    var stream = streamLayer.GetFeature(i);
                    link = stream.GetFieldAsInteger(linkIndex);
                    basin = this._gv.topo.linkToBasin[link];
                    streamGeoms[basin] = stream.GetGeometryRef();
                }
            }
            //if (this._gv.useGridModel && this._gv.topo.basinCentroids.Count == 0) {
            //    // need to calculate centroids from wshedFile
            //    var gridLayer = QgsVectorLayer(this._gv.wshedFile, "grid", "ogr");
            //    var basinIndex = this._gv.topo.getIndex(gridLayer, Topology._POLYGONID);
            //    foreach (var feature in gridLayer.getFeatures()) {
            //        basin = feature[basinIndex];
            //        var centroid = Utils.centreGridCell(feature);
            //        this._gv.topo.basinCentroids[basin] = (centroid.x(), centroid.y());
            //    }
            //}
            double slopeNoData;
            int slopeHasNoData;
            slopeBand.GetNoDataValue(out slopeNoData, out slopeHasNoData);
            if (slopeHasNoData == 0) {
                slopeNoData = this.defaultNoData;
            }
            if (!this._gv.useGridModel) {
                this._gv.basinNoData = (int)basinNoData;
            }
            this._gv.distNoData = (int)distNoData;
            this._gv.cropNoData = (int)cropNoData;
            this._gv.soilNoData = (int)soilNoData;
            this._gv.slopeNoData = (int)slopeNoData;
            this._gv.elevationNoData = elevationNoData;
            // counts to calculate landuse and soil overlaps with basins grid or watershed grid
            var landuseCount = 0;
            var landuseNoDataCount = 0;
            var soilDefinedCount = 0;
            var soilUndefinedCount = 0;
            var soilNoDataCount = 0;
            var slopeBandsNoData = -1;
            Band slopeBandsBand = null;
            var hrusRasterNoData = -1;
            Band hrusRasterBand = null;
            // prepare slope bands grid
            // remove old one since may not be wanted: will be recalculated if it is needed
            this._gv.slopeBandsFile = Path.ChangeExtension(this._gv.demFile, null) + "slope_bands.tif";
            await Utils.removeLayerAndFiles(this._gv.slopeBandsFile);
            if (!this._gv.useGridModel && this._gv.db.slopeLimits.Count > 0) {
                proj = slopeDs.GetProjection();
                driver = Gdal.GetDriverByName("GTiff");
                if (File.Exists(this._gv.slopeBandsFile)) {
                    // failed to remove it - already open in ArcGIS
                    slopeBandsDs = Gdal.Open(this._gv.slopeBandsFile, Access.GA_Update);
                } else {
                    slopeBandsDs = driver.Create(this._gv.slopeBandsFile, slopeNumberCols, slopeNumberRows, 1, DataType.GDT_Byte, null);
                }
                slopeBandsBand = slopeBandsDs.GetRasterBand(1);
                slopeBandsBand.SetNoDataValue(slopeBandsNoData);
                slopeBandsDs.SetGeoTransform(slopeTransform);
                slopeBandsDs.SetProjection(proj);
                Utils.copyPrj(this._gv.slopeFile, this._gv.slopeBandsFile);
            }
            // prepare HRUs raster
            if (!this._gv.useGridModel) {
                proj = basinDs.GetProjection();
                driver = Gdal.GetDriverByName("GTiff");
                var hrusRasterFile = Utils.join(this._gv.gridDir, Parameters._HRUSRASTER);
                await Utils.removeLayerAndFiles(hrusRasterFile);
                if (File.Exists(hrusRasterFile)) {
                    // ArcGIS does not let go
                    hrusRasterDs = Gdal.Open(hrusRasterFile, Access.GA_Update);
                } else { 
                    hrusRasterDs = driver.Create(hrusRasterFile, basinNumberCols, basinNumberRows, 1, DataType.GDT_Int32, null);
                } 
                hrusRasterBand = hrusRasterDs.GetRasterBand(1);
                hrusRasterBand.SetNoDataValue(hrusRasterNoData);
                hrusRasterDs.SetGeoTransform(basinTransform);
                hrusRasterDs.SetProjection(proj);
                Utils.copyPrj(this._gv.basinFile, hrusRasterFile);
            }
            int hasMinVal;
            elevationBand.GetMinimum(out this.minElev, out hasMinVal);
            double maxElev;
            int hasMaxVal;
            elevationBand.GetMaximum(out maxElev, out hasMaxVal);
            if (hasMinVal == 0 || hasMaxVal == 0) {
                try {
                    double[] extrema = new double[2];
                    elevationBand.ComputeRasterMinMax(extrema, 1);
                    this.minElev = extrema[0];
                    maxElev = extrema[1];
                } catch {
                    Utils.error("Failed to calculate Math.Min/Math.Max values of your DEM.  Is it too small?", this._gv.isBatch);
                    return false;
                }
            }
            // convert to metres
            this.minElev *= this._gv.verticalFactor;
            maxElev *= this._gv.verticalFactor;
            // have seen minInt for minElev, so let's assume metres and play safe
            // else will get absurdly large list of elevations
            var globalMinElev = -419;
            var globalMaxElev = 8849;
            if (this.minElev < globalMinElev) {
                this.minElev = globalMinElev;
            } else {
                // make sure it is an integer
                this.minElev = Convert.ToInt32(this.minElev);
            }
            if (maxElev > globalMaxElev) {
                maxElev = globalMaxElev;
            } else {
                maxElev = Convert.ToInt32(maxElev);
            }
            var elevMapSize = Convert.ToInt32(1 + maxElev - this.minElev);
            this.elevMap = new List<int>();
            for (var i = 0; i < elevMapSize; i++) {
                this.elevMap.Add(0);
            }
            // We read raster data in complete rows, using several rows for the grid model if necessary.
            // Complete rows should be reasonably efficient, and for the grid model
            // reading all rows necessary for each row of grid cells avoids rereading any row
            if (this._gv.useGridModel) {
                // cell dimensions may be negative!
                this._gv.cellArea = Math.Abs(elevationTransform[1] * elevationTransform[5]);
                // minimum flow distance is minimum of x and y cell dimensions
                minDist = Math.Min(Math.Abs(elevationTransform[1]), Math.Abs(elevationTransform[5])) * this._gv.topo.gridRows;
                elevationReadRows = this._gv.topo.gridRows;
                var elevationRowDepth = elevationReadRows * elevationTransform[5];
                // we add an extra 2 rows since edges of rows may not
                // line up with elevation map.
                cropReadRows = Math.Max(1, Convert.ToInt32(elevationRowDepth / cropTransform[5] + 2));
                cropActReadRows = cropReadRows;
                soilReadRows = Math.Max(1, Convert.ToInt32(elevationRowDepth / soilTransform[5] + 2));
                soilActReadRows = soilReadRows;
                slopeReadRows = Math.Max(1, Convert.ToInt32(elevationRowDepth / slopeTransform[5] + 2));
                slopeActReadRows = slopeReadRows;
                basinReadRows = 1;
                Utils.loginfo(string.Format("{0}, {1}, {2} rows of landuse, soil and slope for each grid cell", cropReadRows, soilReadRows, slopeReadRows));
            } else {
                // cell dimensions may be negative!
                this._gv.cellArea = Math.Abs(basinTransform[1] * basinTransform[5]);
                // minimum flow distance is minimum of x and y cell dimensions
                minDist = Math.Min(Math.Abs(basinTransform[1]), Math.Abs(basinTransform[5]));
                elevationReadRows = 1;
                cropReadRows = 1;
                soilReadRows = 1;
                slopeReadRows = 1;
                basinReadRows = 1;
                //var distReadRows = 1;
                // create empty arrays to hold raster data when read
                // to avoid danger of allocating and deallocating with main loop
                // currentRow is the top row when using grid model
            }
            int[] hruRow = null;
            int[] hruRows = null;
            int[] hrusData = null;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, int>>>> basinCropSoilSlopeNumbers = null;
            var hrusRasterWanted = !this._gv.isHUC && !this._gv.forTNC;
            Polygonize shapes = null;
            if (this.fullHRUsWanted || hrusRasterWanted) {
                // last HRU number used
                lastHru = 0;
                basinCropSoilSlopeNumbers = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, int>>>>();
                // grid models are based on the DEM raster, and non-grid models on the basins grid
                if (this._gv.useGridModel) {
                    transform = elevationTransform;
                    hruRows = new int[elevationNumberCols * elevationReadRows];
                    for (int i = 0; i < elevationReadRows; i++) {
                        for (int j = 0; j < elevationNumberCols; j++) {
                            hruRows[i * elevationNumberCols + j] = -1;
                        }
                    }
                } else {
                    transform = basinTransform;
                    hruRow = new int[basinNumberCols];
                }
                var pt = ArcGIS.Core.Geometry.MapPointBuilderEx.CreateMapPoint(transform[0], transform[3]);
                shapes = new Polygonize(true, basinNumberCols, -1, pt, transform[1], Math.Abs(transform[5]));
                hrusData = new int[basinNumberCols * basinReadRows];
            }
            var cropCurrentRow = -1;
            var cropData = new int[cropNumberCols * cropReadRows];
            var soilCurrentRow = -1;
            var soilData = new int[soilNumberCols * soilReadRows];
            var slopeCurrentRow = -1;
            var slopeData = new double[slopeNumberCols * slopeReadRows];
            var elevationCurrentRow = -1;
            var elevationData = new double[elevationNumberCols * elevationReadRows];
            var progressCount = 0;
            //if (this._gv.useGridModel) {
            //    if (this._gv.soilTable == Parameters._TNCFAOLOOKUP) {
            //        var waterSoil = Parameters._TNCFAOWATERSOIL;
            //        var waterSoils = Parameters._TNCFAOWATERSOILS;
            //    } else if (this._gv.soilTable == Parameters._TNCHWSDLOOKUP) {
            //        waterSoil = Parameters._TNCHWSDWATERSOIL;
            //        waterSoils = Parameters._TNCHWSDWATERSOILS;
            //    } else {
            //        waterSoil = -1;
            //        waterSoils = new HashSet<object>();
            //    }
            //    if (this._gv.isBig) {
            //        var conn = this._gv.db.connect();
            //        var cursor = conn.cursor();
            //        (sql1, sql2, sql3, sql4) = this._gv.db.initWHUTables(cursor);
            //        var oid = 0;
            //        var elevBandId = 0;
            //    }
            //    fivePercent = Convert.ToInt32(this._gv.topo.basinToSWATBasin.Count / 20);
            //    var gridCount = 0;
            //    foreach (var (link, basin) in this._gv.topo.linkToBasin) {
            //        this.basinElevMap[basin] = new List<int> {
            //            0
            //        } * elevMapSize;
            //        var SWATBasin = this._gv.topo.basinToSWATBasin.get(basin, 0);
            //        if (SWATBasin == 0) {
            //            continue;
            //        }
            //        if (progressCount == fivePercent) {
            //            progressBar.setValue(progressBar.Value + 5);
            //            if (this._gv.forTNC) {
            //                Console.WriteLine(string.Format("Percentage of rasters read: {0} at {1}", progressBar.Value, datetime.now().ToString()));
            //            }
            //            progressCount = 1;
            //        } else {
            //            progressCount += 1;
            //        }
            //        gridCount += 1;
            //        reachData = this._gv.topo.reachesData[link];
            //        // centroid was taken from accumulation grid, but does not matter since in projected units
            //        (centreX, centreY) = this._gv.topo.basinCentroids[basin];
            //        var centroidll = this._gv.topo.pointToLatLong(QgsPointXY(centreX, centreY));
            //        var n = elevationReadRows;
            //        // each grid subbasin contains n x n DEM cells
            //        if (n % 2 == 0) {
            //            // even number of rows and columns - start half a row and column NW of centre
            //            (centreCol, centreRow) = Topology.projToCell(centreX - elevationTransform[1] / 2.0, centreY - elevationTransform[5] / 2.0, elevationTransform);
            //            elevationTopRow = centreRow - (n - 2) / 2;
            //            // beware of rows or columns not dividing by n:
            //            // last grid row or column may be short
            //            rowRange = Enumerable.Range(elevationTopRow, Math.Min(centreRow + (n + 2) / 2, elevationNumberRows) - elevationTopRow);
            //            colRange = Enumerable.Range(centreCol - (n - 2) / 2, Math.Min(centreCol + (n + 2) / 2, elevationNumberCols) - (centreCol - (n - 2) / 2));
            //        } else {
            //            // odd number of rows and columns
            //            (centreCol, centreRow) = Topology.projToCell(centreX, centreY, elevationTransform);
            //            elevationTopRow = centreRow - (n - 1) / 2;
            //            // beware of rows or columns not dividing by n:
            //            // last grid row or column may be short
            //            rowRange = Enumerable.Range(elevationTopRow, Math.Min(centreRow + (n + 1) / 2, elevationNumberRows) - elevationTopRow);
            //            colRange = Enumerable.Range(centreCol - (n - 1) / 2, Math.Min(centreCol + (n + 1) / 2, elevationNumberCols) - (centreCol - (n - 1) / 2));
            //        }
            //        (outletCol, outletRow) = Topology.projToCell(reachData.lowerX, reachData.lowerY, elevationTransform);
            //        (sourceCol, sourceRow) = Topology.projToCell(reachData.upperX, reachData.upperY, elevationTransform);
            //        // Utils.loginfo('Outlet at ({0:.0F},{1:.0F}) for source at ({2:.0F},{3:.0F})', reachData.lowerX, reachData.lowerY, reachData.upperX, reachData.upperY))
            //        outletElev = reachData.lowerZ;
            //        // allow for upper < lower in case unfilled dem is used
            //        drop = reachData.upperZ < outletElev ? 0 : reachData.upperZ - outletElev;
            //        length = this._gv.topo.streamLengths[link];
            //        if (length == 0) {
            //            // is zero for outlet grid cells
            //            length = elevationTransform[1];
            //        }
            //        data = BasinData(outletCol, outletRow, outletElev, sourceCol, sourceRow, length, drop, minDist, this._gv.isBatch);
            //        // add drainage areas
            //        data.drainArea = this._gv.topo.drainAreas[link];
            //        var maxGridElev = -419;
            //        var minGridElev = 8849;
            //        // read data if necessary
            //        if (elevationTopRow != elevationCurrentRow) {
            //            if (this.fullHRUsWanted && lastHru > 0) {
            //                // something has been written to hruRows
            //                foreach (var rowNum in Enumerable.Range(0, n)) {
            //                    shapes.addRow(hruRows[rowNum], elevationCurrentRow + rowNum);
            //                }
            //                hruRows.fill(-1);
            //            }
            //            elevationData = elevationBand.ReadAsArray(0, elevationTopRow, elevationNumberCols, Math.Min(elevationReadRows, elevationNumberRows - elevationTopRow));
            //            elevationCurrentRow = elevationTopRow;
            //        }
            //        var topY = Topology.rowToY(elevationTopRow, elevationTransform);
            //        var cropTopRow = cropRowFun(elevationTopRow, topY);
            //        if (cropTopRow != cropCurrentRow) {
            //            if (0 <= cropTopRow && cropTopRow <= cropNumberRows - cropReadRows) {
            //                cropData = cropBand.ReadAsArray(0, cropTopRow, cropNumberCols, cropReadRows);
            //                cropActReadRows = cropReadRows;
            //                cropCurrentRow = cropTopRow;
            //            } else if (cropNumberRows - cropTopRow < cropReadRows) {
            //                // runnning off the bottom of crop map
            //                cropActReadRows = cropNumberRows - cropTopRow;
            //                if (cropActReadRows >= 1) {
            //                    cropData = cropBand.ReadAsArray(0, cropTopRow, cropNumberCols, cropActReadRows);
            //                    cropCurrentRow = cropTopRow;
            //                }
            //            } else {
            //                cropActReadRows = 0;
            //            }
            //        }
            //        var soilTopRow = soilRowFun(elevationTopRow, topY);
            //        if (soilTopRow != soilCurrentRow) {
            //            if (0 <= soilTopRow && soilTopRow <= soilNumberRows - soilReadRows) {
            //                soilData = soilBand.ReadAsArray(0, soilTopRow, soilNumberCols, soilReadRows);
            //                soilActReadRows = soilReadRows;
            //                soilCurrentRow = soilTopRow;
            //            } else if (soilNumberRows - soilTopRow < soilReadRows) {
            //                // runnning off the bottom of soil map
            //                soilActReadRows = soilNumberRows - soilTopRow;
            //                if (soilActReadRows >= 1) {
            //                    soilData = soilBand.ReadAsArray(0, soilTopRow, soilNumberCols, soilActReadRows);
            //                    soilCurrentRow = soilTopRow;
            //                }
            //            } else {
            //                soilActReadRows = 0;
            //            }
            //        }
            //        var slopeTopRow = slopeRowFun(elevationTopRow, topY);
            //        if (slopeTopRow != slopeCurrentRow) {
            //            if (0 <= slopeTopRow && slopeTopRow <= slopeNumberRows - slopeReadRows) {
            //                slopeData = slopeBand.ReadAsArray(0, slopeTopRow, slopeNumberCols, slopeReadRows);
            //                slopeActReadRows = slopeReadRows;
            //                slopeCurrentRow = slopeTopRow;
            //            } else if (slopeNumberRows - slopeTopRow < slopeReadRows) {
            //                // runnning off the bottom of slope map
            //                slopeActReadRows = slopeNumberRows - slopeTopRow;
            //                if (slopeActReadRows >= 1) {
            //                    slopeData = slopeBand.ReadAsArray(0, slopeTopRow, slopeNumberCols, slopeActReadRows);
            //                    slopeCurrentRow = slopeTopRow;
            //                }
            //            } else {
            //                slopeActReadRows = 0;
            //            }
            //        }
            //        foreach (var row in rowRange) {
            //            y = Topology.rowToY(row, elevationTransform);
            //            cropRow = cropRowFun(row, y);
            //            soilRow = soilRowFun(row, y);
            //            slopeRow = slopeRowFun(row, y);
            //            foreach (var col in colRange) {
            //                elevation = cast(float, elevationData[row - elevationTopRow,col]);
            //                if (elevation != elevationNoData) {
            //                    elevation = Convert.ToInt32(elevation * this._gv.verticalFactor);
            //                    maxGridElev = Math.Max(maxGridElev, elevation);
            //                    minGridElev = Math.Min(minGridElev, elevation);
            //                    index = elevation - this.minElev;
            //                    // can have index too large because Math.Max not calculated properly by gdal
            //                    if (index >= elevMapSize) {
            //                        extra = 1 + index - elevMapSize;
            //                        this.elevMap += new List<int> {
            //                            0
            //                        } * extra;
            //                        elevMapSize += extra;
            //                    }
            //                    this.elevMap[index] += 1;
            //                    this.basinElevMap[basin][index] += 1;
            //                }
            //                if (this.fullHRUsWanted) {
            //                    if (basinCropSoilSlopeNumbers.Contains(basin)) {
            //                        cropSoilSlopeNumbers = basinCropSoilSlopeNumbers[basin];
            //                    } else {
            //                        cropSoilSlopeNumbers = new dict();
            //                        basinCropSoilSlopeNumbers[basin] = cropSoilSlopeNumbers;
            //                    }
            //                }
            //                x = Topology.colToX(col, elevationTransform);
            //                dist = distNoData;
            //                var _tmp_1 = cropRow - cropTopRow;
            //                if (0 <= _tmp_1 && _tmp_1 < cropActReadRows) {
            //                    cropCol = cropColFun(col, x);
            //                    if (0 <= cropCol && cropCol < cropNumberCols) {
            //                        crop = cast(@int, cropData[cropRow - cropTopRow,cropCol]);
            //                        if (crop is null || math.isnan(crop)) {
            //                            crop = cropNoData;
            //                        }
            //                    } else {
            //                        crop = cropNoData;
            //                    }
            //                } else {
            //                    crop = cropNoData;
            //                }
            //                if (crop == cropNoData) {
            //                    landuseNoDataCount += 1;
            //                    // when using grid model small amounts of
            //                    // no data for crop, soil or slope could lose subbasin
            //                    crop = this._gv.db.defaultLanduse;
            //                } else {
            //                    landuseCount += 1;
            //                }
            //                // use an equivalent landuse if any
            //                crop = this._gv.db.translateLanduse(Convert.ToInt32(crop));
            //                var _tmp_2 = soilRow - soilTopRow;
            //                if (0 <= _tmp_2 && _tmp_2 < soilActReadRows) {
            //                    soilCol = soilColFun(col, x);
            //                    if (0 <= soilCol && soilCol < soilNumberCols) {
            //                        soil = cast(@int, soilData[soilRow - soilTopRow,soilCol]);
            //                        if (soil is null || math.isnan(soil)) {
            //                            soil = soilNoData;
            //                        }
            //                    } else {
            //                        soil = soilNoData;
            //                    }
            //                } else {
            //                    soil = soilNoData;
            //                }
            //                if (soil == soilNoData) {
            //                    soilIsNoData = true;
            //                    // when using grid model small amounts of
            //                    // no data for crop, soil or slope could lose subbasin
            //                    soil = this._gv.db.defaultSoil;
            //                } else {
            //                    soilIsNoData = false;
            //                }
            //                // use an equivalent soil if any
            //                (soil, OK) = this._gv.db.translateSoil(Convert.ToInt32(soil));
            //                if (soilIsNoData) {
            //                    soilNoDataCount += 1;
            //                } else if (OK) {
            //                    soilDefinedCount += 1;
            //                } else {
            //                    soilUndefinedCount += 1;
            //                }
            //                var isWater = false;
            //                if (crop != cropNoData) {
            //                    cropCode = this._gv.db.getLanduseCode(crop);
            //                    isWater = cropCode == "WATR";
            //                }
            //                if (waterSoil > 0) {
            //                    if (isWater) {
            //                        soil = waterSoil;
            //                    } else if (waterSoils.Contains(soil)) {
            //                        isWater = true;
            //                        soil = waterSoil;
            //                        if (crop == cropNoData || !Parameters._TNCWATERLANDUSES.Contains(cropCode)) {
            //                            crop = this._gv.db.getLanduseCat("WATR");
            //                        }
            //                    }
            //                }
            //                var _tmp_3 = slopeRow - slopeTopRow;
            //                if (0 <= _tmp_3 && _tmp_3 < slopeActReadRows) {
            //                    slopeCol = slopeColFun(col, x);
            //                    if (0 <= slopeCol && slopeCol < slopeNumberCols) {
            //                        slopeValue = cast(float, slopeData[slopeRow - slopeTopRow,slopeCol]);
            //                    } else {
            //                        slopeValue = slopeNoData;
            //                    }
            //                } else {
            //                    slopeValue = slopeNoData;
            //                }
            //                if (slopeValue == slopeNoData) {
            //                    // when using grid model small amounts of
            //                    // no data for crop, soil or slope could lose subbasin
            //                    slopeValue = Parameters._GRIDDEFAULTSLOPE;
            //                } else if (this._gv.fromGRASS) {
            //                    // GRASS slopes are percentages
            //                    slopeValue /= 100;
            //                }
            //                if (crop == this._gv.db.getLanduseCat("RICE")) {
            //                    slopeValue = Math.Min(slopeValue, Parameters._RICEMAXSLOPE);
            //                }
            //                slope = this._gv.db.slopeIndex(slopeValue * 100);
            //                // set water or wetland pixels to have slope at most WATERMAXSLOPE
            //                if (isWater || Parameters._TNCWATERLANDUSES.Contains(cropCode)) {
            //                    slopeValue = Math.Min(slopeValue, Parameters._WATERMAXSLOPE);
            //                    slope = 0;
            //                }
            //                data.addCell(crop, soil, slope, this._gv.cellArea, elevation, slopeValue, dist, this._gv);
            //                if (!this._gv.isBig) {
            //                    this.basins[basin] = data;
            //                }
            //                if (this.fullHRUsWanted) {
            //                    if (crop != cropNoData && soil != soilNoData && slope != slopeNoData) {
            //                        hru = BasinData.getHruNumber(cropSoilSlopeNumbers, lastHru, crop, soil, slope);
            //                        if (hru > lastHru) {
            //                            // new HRU number: store it
            //                            lastHru = hru;
            //                        }
            //                        hruRows[row - elevationTopRow,col] = hru;
            //                    }
            //                }
            //            }
            //        }
            //        data.setAreas(true);
            //        if (this._gv.isBig) {
            //            (oid, elevBandId) = this.writeWHUTables(oid, elevBandId, SWATBasin, basin, data, cursor, sql1, sql2, sql3, sql4, centroidll, this.basinElevMap[basin], minGridElev, maxGridElev);
            //        }
            //    }
            //    if (this._gv.isBig) {
            //        conn.commit();
            //        WONKO_del(cursor);
            //        WONKO_del(conn);
            //        this.writeGridSubsFile();
            //    }
            //} else {
            if (true) {
                double[] basinData = new double[basinNumberCols];
                int basinCurrentRow = -1;
                double[] distData = new double[distNumberCols];
                int distCurrentRow = -1;
                // not grid model  
                // tic = time.perf_counter()            
                foreach (var row in Enumerable.Range(0, basinNumberRows)) {
                    if (progressCount == fivePercent) {
                        this._dlg.addProgressBar(5);
                        progressCount = 1;
                        // toc = time.perf_counter()
                        // Utils.loginfo('Time to row {0}: {1:F1} seconds', row, toc-tic))
                        // tic = toc
                    } else {
                        progressCount += 1;
                    }
                    if (row != basinCurrentRow) {
                        basinBand.ReadRaster(0, row, basinNumberCols, 1, basinData, basinNumberCols, 1, 0, 0);
                    }
                    y = Topology.rowToY(row, basinTransform);
                    var distRow = 0;
                    if (!this._gv.existingWshed) {
                        distRow = distRowFun(row, y);
                        if (0 <= distRow && distRow < distNumberRows && distRow != distCurrentRow) {
                            distCurrentRow = distRow;
                            distBand.ReadRaster(0, distRow, distNumberCols, 1, distData, distNumberCols, 1, 0, 0);
                        }
                    }
                    cropRow = cropRowFun(row, y);
                    if (0 <= cropRow && cropRow < cropNumberRows && cropRow != cropCurrentRow) {
                        cropCurrentRow = cropRow;
                        cropBand.ReadRaster(0, cropRow, cropNumberCols, 1, cropData, cropNumberCols, 1, 0, 0);
                    }
                    soilRow = soilRowFun(row, y);
                    if (0 <= soilRow && soilRow < soilNumberRows && soilRow != soilCurrentRow) {
                        soilCurrentRow = soilRow;
                        soilBand.ReadRaster(0, soilRow, soilNumberCols, 1, soilData, soilNumberCols, 1, 0, 0);
                    }
                    slopeRow = slopeRowFun(row, y);
                    if (0 <= slopeRow && slopeRow < slopeNumberRows && slopeRow != slopeCurrentRow) {
                        if (this._gv.db.slopeLimits.Count > 0 && (0 <= slopeCurrentRow && slopeCurrentRow < slopeNumberRows)) {
                            // generate slope bands data and Write it before reading next row
                            foreach (var i in Enumerable.Range(0, slopeNumberCols)) {
                                slopeValue = slopeData[i];
                                slopeData[i] = slopeValue != slopeNoData ? this._gv.db.slopeIndex(slopeValue * 100) : slopeBandsNoData;
                            }
                            slopeBandsBand.WriteRaster(0, slopeCurrentRow, slopeNumberCols, 1, slopeData, slopeNumberCols, 1, 0, 0);
                        }
                        slopeCurrentRow = slopeRow;
                        slopeBand.ReadRaster(0, slopeRow, slopeNumberCols, 1, slopeData, slopeNumberCols, 1, 0, 0);
                    }
                    var elevationRow = elevationRowFun(row, y);
                    if (0 <= elevationRow && elevationRow < elevationNumberRows && elevationRow != elevationCurrentRow) {
                        elevationCurrentRow = elevationRow;
                        elevationBand.ReadRaster(0, elevationRow, elevationNumberCols, 1, elevationData, elevationNumberCols, 1, 0, 0);
                    }
                    foreach (var col in Enumerable.Range(0, basinNumberCols)) {
                        basin = (int)basinData[col];
                        //basinNoData was made absolute earlier
                        if (Math.Abs(basin) != basinNoData && !this._gv.topo.isUpstreamBasin(basin)) {
                            if (this.fullHRUsWanted || hrusRasterWanted) {
                                if (basinCropSoilSlopeNumbers.Keys.Contains(basin)) {
                                    cropSoilSlopeNumbers = basinCropSoilSlopeNumbers[basin];
                                } else {
                                    cropSoilSlopeNumbers = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
                                    basinCropSoilSlopeNumbers[basin] = cropSoilSlopeNumbers;
                                }
                            }
                            x = Topology.colToX(col, basinTransform);
                            if (!this._gv.existingWshed) {
                                var distCol = distColFun(col, x);
                                if (0 <= distCol && distCol < distNumberCols && (0 <= distRow && distRow < distNumberRows)) {
                                    // coerce dist to float else considered by Access to be a numpy float
                                    dist = distData[distCol];
                                } else {
                                    dist = distNoData;
                                }
                            } else {
                                dist = distNoData;
                            }
                            cropCol = cropColFun(col, x);
                            if (0 <= cropCol && cropCol < cropNumberCols && (0 <= cropRow && cropRow < cropNumberRows)) {
                                crop = cropData[cropCol];
                            } else {
                                crop = (int)cropNoData;
                            }
                            // landuse maps used for HUC models have 0 in Canada
                            // so to prevent messages about 0 not recognised as a landuse
                            if ((this._gv.isHUC || this._gv.isHAWQS) && crop == 0) {
                                crop = (int)cropNoData;
                            }
                            if (crop == cropNoData) {
                                landuseNoDataCount += 1;
                            } else {
                                landuseCount += 1;
                                // use an equivalent landuse if any
                                crop = this._gv.db.translateLanduse(Convert.ToInt32(crop));
                            }
                            soilCol = soilColFun(col, x);
                            bool OK = true;
                            if (0 <= soilCol && soilCol < soilNumberCols && (0 <= soilRow && soilRow < soilNumberRows)) {
                                soil = soilData[soilCol];
                            } else {
                                soil = (int)soilNoData;
                            }
                            if (soil == soilNoData) {
                                soilIsNoData = true;
                            } else {
                                soilIsNoData = false;
                                // use an equivalent soil if any
                                soil = this._gv.db.translateSoil(soil, out OK);
                            }
                            if (soilIsNoData) {
                                soilNoDataCount += 1;
                            } else if (OK) {
                                soilDefinedCount += 1;
                            } else {
                                soilUndefinedCount += 1;
                            }
                            // make sure crop and soil do not conflict about water
                            var isWet = false;
                            cropCode = "";
                            if (crop != cropNoData) {
                                cropCode = this._gv.db.getLanduseCode(crop);
                                isWet = Parameters._WATERLANDUSES.Contains(cropCode);
                            }
                            if (this._gv.db.useSSURGO) {
                                if (isWet) {
                                    soil = Parameters._SSURGOWater;
                                } else if (soil == Parameters._SSURGOWater) {
                                    isWet = true;
                                    if (crop == cropNoData || !Parameters._WATERLANDUSES.Contains(cropCode)) {
                                        crop = this._gv.db.getLanduseCat("WATR");
                                    }
                                }
                            }
                            slopeCol = slopeColFun(col, x);
                            if (0 <= slopeCol && slopeCol < slopeNumberCols && (0 <= slopeRow && slopeRow < slopeNumberRows)) {
                                slopeValue = slopeData[slopeCol];
                            } else {
                                slopeValue = slopeNoData;
                            }
                            // GRASS slopes are percentages
                            if (this._gv.fromGRASS && slopeValue != slopeNoData) {
                                slopeValue /= 100;
                            }
                            // set water or wetland pixels to have slope at most WATERMAXSLOPE
                            if (isWet) {
                                if (slopeValue == slopeNoData) {
                                    slopeValue = Parameters._WATERMAXSLOPE;
                                } else {
                                    slopeValue = Math.Min(slopeValue, Parameters._WATERMAXSLOPE);
                                }
                            }
                            if (slopeValue != slopeNoData) {
                                // for HUC, slope bands only used for agriculture
                                if (!(this._gv.isHUC || this._gv.isHAWQS) || this._gv.db.isAgriculture(crop)) {
                                    slope = this._gv.db.slopeIndex(slopeValue * 100);
                                } else {
                                    slope = 0;
                                }
                            } else {
                                slope = -1;
                            }
                            var elevationCol = elevationColFun(col, x);
                            if (0 <= elevationCol && elevationCol < elevationNumberCols && (0 <= elevationRow && elevationRow < elevationNumberRows)) {
                                elevation = elevationData[elevationCol];
                            } else {
                                elevation = elevationNoData;
                            }
                            if (elevation != elevationNoData) {
                                elevation = Convert.ToInt32(elevation * this._gv.verticalFactor);
                            }
                            if (this.basins.Keys.Contains(basin)) {
                                data = this.basins[basin];
                            } else {
                                // new basin
                                this.basinElevMap[basin] = new List<int>();
                                for (int i = 0; i < elevMapSize; i++) {
                                    this.basinElevMap[basin].Add(0);
                                }
                                try {
                                    link = this._gv.topo.basinToLink[basin];
                                } catch (Exception e) {
                                    Utils.error("Error: " + e.Message, this._gv.isBatch);
                                    continue;
                                }
                                reachData = this._gv.topo.reachesData[link];
                                // could, eg, be outside DEM.  Invent some default values
                                var outletCol = elevationCol;
                                var outletRow = elevationRow;
                                var sourceCol = elevationCol;
                                var sourceRow = elevationRow;
                                outletElev = elevation;
                                drop = 0;
                                if (reachData is not null) {
                                    (outletCol, outletRow) = Topology.projToCell(reachData.lowerX, reachData.lowerY, elevationTransform);
                                    (sourceCol, sourceRow) = Topology.projToCell(reachData.upperX, reachData.upperY, elevationTransform);
                                    // Utils.loginfo('Outlet at ({0:.0F},{1:.0F}) for source at ({2:.0F},{3:.0F})', reachData.lowerX, reachData.lowerY,reachData.upperX, reachData.upperY))
                                    outletElev = reachData.lowerZ;
                                    // allow for upper < lower in case unfilled dem is used
                                    drop = reachData.upperZ < outletElev ? 0 : reachData.upperZ - outletElev;
                                }
                                length = this._gv.topo.streamLengths[link];
                                data = new BasinData(outletCol, outletRow, outletElev, sourceCol, sourceRow, length, drop, minDist);
                                // add drainage areas
                                data.drainArea = this._gv.topo.drainAreas[link];
                                if (this._gv.isHUC || this._gv.isHAWQS) {
                                    var drainAreaKm = data.drainArea / 1000000.0;
                                    var semiChannelWidth = 0.645 * Math.Pow(drainAreaKm, 0.6);
                                    var streamGeom = streamGeoms[basin];
                                    var streamBuffer = streamGeom.Buffer(semiChannelWidth, 30);
                                    var streamArea = streamBuffer.Area();
                                    basinStreamWaterData[basin] = (streamBuffer, streamArea, 0.0);
                                }
                                this.basins[basin] = data;
                            }
                            data.addCell(crop, soil, slope, this._gv.cellArea, elevation, slopeValue, dist, this._gv);
                            this.basins[basin] = data;
                            if ((this._gv.isHUC || this._gv.isHAWQS) && crop == this._gv.db.getLanduseCat("WATR")) {
                                var pt = OSGeo.OGR.Geometry.CreateFromWkt(string.Format("POINT({0} {1})", x, y));
                                OSGeo.OGR.Geometry streamBuffer;
                                double streamArea;
                                double WATRInStreamArea;
                                (streamBuffer, streamArea, WATRInStreamArea) = basinStreamWaterData[basin];
                                if (streamBuffer is not null && streamBuffer.Contains(pt)) {
                                    WATRInStreamArea += this._gv.cellArea;
                                    basinStreamWaterData[basin] = (streamBuffer, streamArea, WATRInStreamArea);
                                }
                            }
                            if (elevation != elevationNoData) {
                                index = Convert.ToInt32(elevation - this.minElev);
                                // can have index too large because Math.Max not calculated properly by gdal
                                if (index >= elevMapSize) {
                                    extra = 1 + index - elevMapSize;
                                    for (int i = 0; i < extra; i++) {
                                        foreach (var b in this.basinElevMap.Keys) {
                                            this.basinElevMap[b].Add(0);
                                        }
                                        this.elevMap.Add(0);
                                    }
                                    elevMapSize += extra;
                                }
                                try {
                                    this.basinElevMap[basin][index] += 1;
                                } catch (Exception) {
                                    Utils.error(string.Format("Problem in basin {0} reading elevation {1} at ({5}, {6}).  Minimum: {2}, maximum: {3}, index: {4}", basin, elevation, this.minElev, maxElev, index, x, y), this._gv.isBatch);
                                    break;
                                }
                                this.elevMap[index] += 1;
                            }
                            if (this.fullHRUsWanted || hrusRasterWanted) {
                                if (crop != cropNoData && soil != soilNoData && slope != slopeNoData) {
                                    hru = BasinData.getHruNumber(cropSoilSlopeNumbers, lastHru, crop, soil, slope);
                                    if (hru > lastHru) {
                                        // new HRU number: store it
                                        lastHru = hru;
                                    }
                                    hruRow[col] = hru;
                                    hrusData[col] = hru;
                                } else {
                                    hruRow[col] = -1;
                                    hrusData[col] = -1;
                                }
                            }
                        } else if (this.fullHRUsWanted || hrusRasterWanted) {
                            hruRow[col] = -1;
                            hrusData[col] = -1;
                        }
                    }
                    if (this.fullHRUsWanted) {
                        shapes.addRow(hruRow, row);
                    }
                    if (hrusRasterWanted) {
                        hrusRasterBand.WriteRaster(0, row, basinNumberCols, 1, hrusData, basinNumberCols, 1, 0, 0);
                    }
                }
                if (!this._gv.useGridModel && this._gv.db.slopeLimits.Count > 0 && (0 <= slopeCurrentRow && slopeCurrentRow < slopeNumberRows)) {
                    // Write final slope bands row
                    foreach (var i in Enumerable.Range(0, slopeNumberCols)) {
                        slopeValue = slopeData[i];
                        slopeData[i] = slopeValue != slopeNoData ? this._gv.db.slopeIndex(slopeValue * 100) : slopeBandsNoData;
                    }
                    slopeBandsBand.WriteRaster(0, slopeCurrentRow, slopeNumberCols, 1, slopeData, slopeNumberCols, 1, 0, 0);
                    // flush and release memor
                    slopeBandsBand = null;
                    slopeBandsDs = null;
                }
            }
            if (hrusRasterWanted) {
                hrusRasterDs = null;
            }
            // clear some memory
            elevationDs = null;
            if (!this._gv.existingWshed && !this._gv.useGridModel) {
                distDs = null;
            }
            slopeDs = null;
            soilDs = null;
            cropDs = null;
            // check landuse and soil overlaps
            if (landuseCount + landuseNoDataCount == 0) {
                landusePercent = 0.0;
            } else {
                // need cast to double or integer arithmetic is used, and result is 0, since a/b is zero with integer arithmetic if b > a
                landusePercent = (double)landuseCount / (landuseCount + landuseNoDataCount) * 100;
            }
            Utils.loginfo(string.Format("Landuse cover percent: {0:F1}", landusePercent));
            if (landusePercent < 95) {
                Utils.information(string.Format("WARNING: only {0:F1} percent of the watershed has defined landuse values.\n If this percentage is zero check your landuse map has the same projection as your DEM.", landusePercent), this._gv.isBatch);
            }
            var soilMapPercent = ((double)soilDefinedCount + soilUndefinedCount) / (soilDefinedCount + soilUndefinedCount + soilNoDataCount) * 100;
            Utils.loginfo(string.Format("Soil cover percent: {0:F1}", soilMapPercent));
            if (this._gv.isHUC || this._gv.isHAWQS) {
                // always 100% for other, since undefined mapped to default
                if (soilDefinedCount + soilUndefinedCount > 0) {
                    soilDefinedPercent = (double)soilDefinedCount / (soilDefinedCount + soilUndefinedCount) * 100;
                } else {
                    soilDefinedPercent = 0;
                }
                Utils.loginfo(string.Format("Soil defined percent: {0:F1}", soilDefinedPercent));
            }
            var under95 = false;
            if (this._gv.isHUC) {
                var huc12 = this._gv.projName[3];
                var logFile = this._gv.logFile;
                if (soilMapPercent < 1) {
                    // start of message is key phrase for HUC12Models
                    Utils.information(string.Format("EMPTY PROJECT: only {0:F4} percent of the watershed in project huc{1} is inside the soil map", soilMapPercent, huc12), this._gv.isBatch, logFile: logFile);
                    this.emptyHUCProject = true;
                    return false;
                } else if (soilMapPercent < 95) {
                    // start of message is key phrase for HUC12Models
                    Utils.information(string.Format("UNDER95 WARNING: only {0:F1} percent of the watershed in project huc{1} is inside the soil map.", soilMapPercent, huc12), this._gv.isBatch, logFile: logFile);
                    under95 = true;
                } else if (soilMapPercent < 99.95) {
                    // always give statistic for HUC models; avoid saying 100.0 when rounded to 1dp
                    // start of message is key word for HUC12Models
                    Utils.information(string.Format("WARNING: only {0:F1} percent of the watershed in project huc{1} is inside the soil map.", soilMapPercent, huc12), this._gv.isBatch, logFile: logFile);
                }
                if (soilDefinedPercent < 80) {
                    // start of message is key word for HUC12Models
                    Utils.information(string.Format("WARNING: only {0:F1} percent of the watershed in project huc{1} has defined soil.", soilDefinedPercent, huc12), this._gv.isBatch, logFile: logFile);
                }
            } else if (soilMapPercent < 95) {
                Utils.information(string.Format("WARNING: only {0:F1} percent of the watershed has defined soil values.\n If this percentage is zero check your soil map has the same projection as your DEM.", soilMapPercent), this._gv.isBatch);
                under95 = true;
            }
            if (this.fullHRUsWanted) {
                // for TestingFullHRUs add these instead of addRow few lines above
                // shapes.addRow([1,1,1], 0, 3, -1)
                // shapes.addRow([1,2,1], 1, 3, -1)
                // shapes.addRow([1,1,1], 2, 3, -1)
                // Utils.loginfo(shapes.reportBoxes())
                Utils.progress("Creating FullHRUs shapes ...", _dlg.progressionLabel);
                if (Parameters.useSlowPolygonize) {
                    //shapes.finishShapes(_dlg.progressionLabel);
                    //print('{0} shapes in shapesTable' , len(shapes.shapesTable)))
                } else {
                    shapes.finish();
                }
                //Utils.loginfo(shapes.makeString())
                Utils.progress("Writing FullHRUs shapes ...", _dlg.progressionLabel);
                if (!await this.createFullHRUsShapefile(shapes, basinCropSoilSlopeNumbers, this.basins, _dlg.progressionBar, lastHru)) {
                    Utils.information("Unable to create FullHRUs shapefile", this._gv.isBatch);
                    Utils.progress("", _dlg.progressionLabel);
                } else {
                    Utils.progress("FullHRUs shapefile finished", _dlg.progressionLabel);
                }
            }
            // Add farthest points
            if (this._gv.existingWshed) {
                // approximate as length of main stream
                foreach (var basinData in this.basins.Values) {
                    // type: ignore
                    basinData.farDistance = Math.Max(basinData.startToOutletDistance, minDist);
                }
            }
            // now use distance to outlets stored in distFile
            //=======================================================================
            // else:
            //     pDs = gdal.Open(self._gv.pFile, gdal.GA_ReadOnly)
            //     if not pDs:
            //         Utils.error('Cannot open D8 slope grid {0}', self._gv.pFile), self._gv.isBatch)
            //         return False
            //     Utils.progress('Calculating channel lengths ...', this._dlg.progressionLabel)
            //     self.progress_signal.emit('Calculating channel lengths ...')
            //     pTransform = pDs.GetGeoTransform()
            //     pBand = pDs.GetRasterBand(1)
            //     for basinData in self.basins.Values:
            //         basinData.farDistance = self.channelLengthToOutlet(basinData, pTransform, pBand, basinTransform, self._gv.isBatch)
            //         if basinData.farDistance == 0: # there was an error; use the stream length
            //             basinData.farDistance = basinData.startToOutletDistance
            //=======================================================================
            // clear memory
            if (!this._gv.useGridModel) {
                basinDs = null;
            }
            if (!(this._gv.isBig)) {
                Utils.progress("Writing HRU data to database ...", this._dlg.progressionLabel);
                // Write raw data to tables before adding water bodies
                // first for HUC and HAWQS save stream water data for basin
                if (this._gv.isHUC || this._gv.isHAWQS) {
                    foreach (var kvp in this.basins) {
                        // type: ignore
                        basin = kvp.Key;
                        BasinData basinData = kvp.Value;
                        double streamArea, WATRInStreamArea;
                        (_, streamArea, WATRInStreamArea) = basinStreamWaterData[basin];
                        basinData.streamArea = streamArea;
                        basinData.WATRInStreamArea = WATRInStreamArea;
                    }
                }
                string sql1, sql2;
                (sql1, sql2) = this._gv.db.createBasinsDataTables();
                if (sql1 is null || sql2 is null) {
                    return false;
                }
                this._gv.db.writeBasinsData(this.basins, sql1, sql2);
                //TODO
                //if (this._gv.isHUC || this._gv.isHAWQS) {
                //    this.addWaterBodies();
                //}
                this.saveAreas(true, redistributeNodata: !(under95 && (this._gv.isHUC || this._gv.isHAWQS)));
                Utils.progress("Writing topographic report ...", this._dlg.progressionLabel);
                this.writeTopoReport();
            }
            return true;
        }

        //TODO
        //// For HUC and HAWQS projects only.  Write res, pnd, lake and playa tables.  Store reservoir, pond, lake and playa areas in basin data.
        //public virtual object addWaterBodies() {
        //    object where;
        //    object collectHUCs() {
        //        // find HUCnn index
        //        wshedFile = this._gv.wshedFile;
        //        wshedLayer = QgsVectorLayer(wshedFile, "wshed", "ogr");
        //        provider = wshedLayer.dataProvider();
        //        (hucIndex, _) = Topology.getHUCIndex(wshedLayer);
        //        hucs = "( ";
        //        foreach (var feature in provider.getFeatures()) {
        //            hucs += "\"" + feature[hucIndex].ToString() + "\",";
        //        }
        //        hucs = hucs[: - 1:] + ")";
        //        return hucs;
        //    }
        //    if (!os.path.isfile(this._gv.db.waterBodiesFile)) {
        //        Utils.error(string.Format("Cannot find water bodies file {0}", this._gv.db.waterBodiesFile), this._gv.isBatch);
        //        return;
        //    }
        //    if (this._gv.isHUC) {
        //        var huc12_10_8_6 = this._gv.projName[3];
        //        where = "HUC12_10_8_6 LIKE \"" + huc12_10_8_6 + "\"";
        //    } else {
        //        // HAWQS
        //        var hucs = collectHUCs();
        //        where = "HUC14_12_10_8 IN " + hucs;
        //    }
        //    var logFile = this._gv.logFile;
        //    using (var conn = this._gv.db.connect(), waterConn = sqlite3.connect(this._gv.db.waterBodiesFile)) {
        //        // first two may not exist in old projects: added November 2012
        //        conn.execute("CREATE TABLE IF NOT EXISTS lake " + DBUtils._LAKETABLESQL);
        //        conn.execute("CREATE TABLE IF NOT EXISTS playa " + DBUtils._PLAYATABLESQL);
        //        conn.execute("DELETE FROM pnd");
        //        conn.execute("DELETE FROM res");
        //        conn.execute("DELETE FROM lake");
        //        conn.execute("DELETE FROM playa");
        //        sql0 = this._gv.db.sqlSelect("reservoirs", "SUBBASIN, IYRES, RES_ESA, RES_EVOL, RES_PSA, RES_PVOL, RES_VOL, RES_DRAIN, RES_NAME, OBJECTID, HUC12_10_8_6, HUC14_12_10_8", "", where);
        //        sql1 = "INSERT INTO res (OID, SUBBASIN, IYRES, RES_ESA, RES_EVOL, RES_PSA, RES_PVOL, RES_VOL) VALUES(?,?,?,?,?,?,?,?);";
        //        sql2 = this._gv.db.sqlSelect("ponds", "SUBBASIN, PND_ESA, PND_EVOL, PND_PSA, PND_PVOL, PND_VOL, PND_DRAIN, PND_NAME, HUC12_10_8_6", "", where);
        //        sql3 = this._gv.db.sqlSelect("wetlands", "SUBBASIN, WET_AREA", "", where);
        //        sql4 = this._gv.db.sqlSelect("playas", "SUBBASIN, PLA_AREA, HUC12_10_8_6", "", where);
        //        sql6 = this._gv.db.sqlSelect("lakes", "SUBBASIN, LAKE_AREA, LAKE_NAME, OBJECTID, HUC12_10_8_6, HUC14_12_10_8", "", where);
        //        sql5 = "INSERT INTO pnd (OID, SUBBASIN, PND_FR, PND_PSA, PND_PVOL, PND_ESA, PND_EVOL, PND_VOL) VALUES(?,?,?,?,?,?,?,?);";
        //        sql7 = "INSERT INTO lake (OID, SUBBASIN, LAKE_AREA) VALUES(?,?,?);";
        //        sql8 = "INSERT INTO playa (OID, SUBBASIN, PLAYA_AREA) VALUES(?,?,?);";
        //        // assigning to each subbasin the reservoir areas that intersect with it
        //        // free areas in m^2 keep track of area available for water bodies
        //        freeAreas = new dict();
        //        foreach (var (basin, basinData) in this.basins) {
        //            freeAreas[basin] = basinData.area;
        //        }
        //        oid = 0;
        //        foreach (var row in waterConn.execute(sql0)) {
        //            oids = (from strng in row[9].split(",")
        //                select Convert.ToInt32(strng)).ToList();
        //            areaHa = float(row[4]);
        //            (reduction, usedBasin) = this.distributeWaterPolygons(oids, areaHa * 10000.0, freeAreas, true);
        //            if (row[10] == row[11][: - 2:]) {
        //                SWATBasin = Convert.ToInt32(row[0]);
        //                basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
        //            } else {
        //                // reservoir was assigned to a different huc12 from original, so SUBBASIN field not reliable
        //                basin = usedBasin;
        //                SWATBasin = this._gv.topo.basinToSWATBasin[basin];
        //            }
        //            if (reduction > 0) {
        //                basinData = this.basins[basin];
        //                Utils.information(string.Format("WARNING: Reservoir {4} in huc{0} subbasin {1} area {2} ha reduced to {3}", row[10], SWATBasin, areaHa, areaHa - reduction / 10000.0, row[8]), this._gv.isBatch, logFile: logFile);
        //            }
        //            // res table stores actual areas
        //            oid += 1;
        //            conn.execute(sql1, (oid, SWATBasin, row[1], row[2], row[3], row[4], row[5], row[6]));
        //        }
        //        oid = 0;
        //        foreach (var row in waterConn.execute(sql2)) {
        //            SWATBasin = Convert.ToInt32(row[0]);
        //            basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
        //            basinData = this.basins[basin];
        //            pnd_fr = float(row[6]) / (basinData.area / 1000000.0);
        //            pnd_fr = Math.Min(0.95, pnd_fr);
        //            areaHa = float(row[3]);
        //            basinData.pondArea = Math.Min(freeAreas[basin], areaHa * 10000.0);
        //            freeAreas[basin] = freeAreas[basin] - basinData.pondArea;
        //            if (basinData.pondArea < areaHa * 10000.0) {
        //                Utils.information(string.Format("WARNING: Pond {4} in huc{0} subbasin {1} area {2} ha reduced to {3}", row[8], SWATBasin, areaHa, basinData.pondArea / 10000.0, row[7]), this._gv.isBatch, logFile: logFile);
        //            }
        //            // pnd table stores actual areas
        //            oid += 1;
        //            conn.execute(sql5, (oid, SWATBasin, pnd_fr, row[3], row[4], row[1], row[2], row[5]));
        //        }
        //        oid = 0;
        //        foreach (var row in waterConn.execute(sql6)) {
        //            oids = (from strng in row[3].split(",")
        //                select Convert.ToInt32(strng)).ToList();
        //            areaHa = float(row[1]);
        //            (reduction, usedBasin) = this.distributeWaterPolygons(oids, areaHa * 10000.0, freeAreas, false);
        //            if (row[4] == row[5][: - 2:]) {
        //                SWATBasin = Convert.ToInt32(row[0]);
        //                basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
        //            } else {
        //                // lake was assigned to a different huc12 from original, so SUBBASIN field not reliable
        //                basin = usedBasin;
        //                SWATBasin = this._gv.topo.basinToSWATBasin[basin];
        //            }
        //            if (reduction > 0) {
        //                basinData = this.basins[basin];
        //                Utils.information(string.Format("WARNING: Lake {4} in huc{0} subbasin {1} area {2} ha reduced to {3}", row[4], SWATBasin, areaHa, areaHa - reduction / 10000.0, row[2]), this._gv.isBatch, logFile: logFile);
        //            }
        //            oid += 1;
        //            conn.execute(sql7, (oid, SWATBasin, row[1]));
        //        }
        //        foreach (var row in waterConn.execute(sql3)) {
        //            SWATBasin = Convert.ToInt32(row[0]);
        //            basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
        //            basinData = this.basins[basin];
        //            // area is in hectares
        //            basinData.wetlandArea = float(row[1]) * 10000.0;
        //        }
        //        oid = 0;
        //        foreach (var row in waterConn.execute(sql4)) {
        //            SWATBasin = Convert.ToInt32(row[0]);
        //            basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
        //            basinData = this.basins[basin];
        //            areaHa = float(row[1]);
        //            // area is in hectares
        //            basinData.playaArea = Math.Min(freeAreas[basin], areaHa * 10000.0);
        //            freeAreas[basin] = freeAreas[basin] - basinData.playaArea;
        //            if (basinData.playaArea < areaHa * 10000.0) {
        //                Utils.information(string.Format("WARNING: Playa in huc{0} subbasin {1} area {2} ha reduced to {3}", row[2], SWATBasin, areaHa, basinData.playaArea / 10000.0), this._gv.isBatch, logFile: logFile);
        //            }
        //            oid += 1;
        //            conn.execute(sql8, (oid, SWATBasin, row[1]));
        //        }
        //        // collect water statistics before WATR areas reduced for reservoirs, ponds, lakes and playa
        //        waterStats = this.writeWaterStats1();
        //        // area of WATR removed in square metres
        //        totalWaterReduction = 0.0;
        //        totalArea = 0.0;
        //        reductions = new dict();
        //        foreach (var (basin, basinData) in this.basins) {
        //            waterReduction = 0.0;
        //            area = basinData.cropSoilSlopeArea;
        //            waterData = waterStats.get(basin, null);
        //            if (waterData is not null) {
        //                waterReduction = basinData.removeWaterBodiesArea(waterData[6] * 10000.0, basin, conn, this._gv);
        //            }
        //            totalWaterReduction += waterReduction;
        //            totalArea += area;
        //            reductions[basin] = area == 0 ? (0, 0) : (waterReduction / 10000.0, waterReduction * 100 / area);
        //        }
        //        this.writeWaterStats2(waterStats, reductions);
        //        if (totalWaterReduction > 1000000.0) {
        //            // 1 square km
        //            percent = totalWaterReduction * 100 / totalArea;
        //            Utils.information(string.Format("WARNING: Water area reduction of {0:F1} sq km ({1:F1}%)", totalWaterReduction / 1000000.0, percent), this._gv.isBatch, logFile: logFile);
        //        }
        //        conn.commit();
        //    }
        //}

        // Assign area of water polygon(s), either reservoirs or lakes, to the subbasin they intersect with, as far as possible.
        //         Return unassigned area (or zero) and smallest used basin number.
        //public virtual object distributeWaterPolygons(List<int> objectIds, double waterArea, object freeAreas, bool isReservoir) {
        //    object assignArea;
        //    object interArea2;
        //    object topoSort(object basins) {
        //        result = new List<object>();
        //        todo = basins.ToList()[":"];
        //        while (todo.Count > 0) {
        //            basin = todo.pop(0);
        //            channel = this._gv.topo.basinToLink[basin];
        //            dsChannel = this._gv.topo.downLinks.get(channel, -1);
        //            if (dsChannel == -1) {
        //                result.insert(0, basin);
        //            } else {
        //                dsBasin = this._gv.topo.linkToBasin[dsChannel];
        //                try {
        //                    pos = result.index(dsBasin);
        //                    result.insert(pos + 1, basin);
        //                } catch {
        //                    todo.append(basin);
        //                }
        //            }
        //        }
        //        return result;
        //    }
        //    object firstBasin(List<int> sortedBasins, object basins) {
        //        foreach (var basin in sortedBasins) {
        //            if (basins.Contains(basin)) {
        //                return basin;
        //            }
        //        }
        //        return -1;
        //    }
        //    var subbasinsFile = this._gv.wshedFile;
        //    var waterPolygonsFile = Utils.join(this._gv.HUCDataDir, "NHDLake5072Fixed.shp");
        //    var subbasinsLayer = QgsVectorLayer(subbasinsFile, "subbasins", "ogr");
        //    var polyIndex = subbasinsLayer.fields().lookupField("PolygonId");
        //    var waterbodyLayer = QgsVectorLayer(waterPolygonsFile, "water", "ogr");
        //    var interAreas = new dict();
        //    foreach (var oid in objectIds) {
        //        var exp = QgsExpression(string.Format("\"OBJECTID\" = {0}", oid));
        //        var waterBody = next(from feature in waterbodyLayer.getFeatures(QgsFeatureRequest(exp))
        //            select feature);
        //        var waterGeom = waterBody.geometry();
        //        foreach (var subbasin in subbasinsLayer.getFeatures()) {
        //            var basin = subbasin[polyIndex];
        //            var intersect = subbasin.geometry().intersection(waterGeom);
        //            if (!QgsGeometry.isEmpty(intersect)) {
        //                var interArea = intersect.area();
        //                if (interArea >= 50) {
        //                    // otherwise will round to 0.00 ha
        //                    interAreas[basin] = interAreas.get(basin, 0) + interArea;
        //                }
        //            }
        //        }
        //    }
        //    var sortedBasins = topoSort(this.basins.Keys);
        //    Utils.loginfo(string.Format("Sorted basins: {0}", sortedBasins));
        //    var totalInterArea = 0.0;
        //    var OK = true;
        //    foreach (var (basin, interArea) in interAreas) {
        //        totalInterArea += interArea;
        //        if (interArea > freeAreas[basin]) {
        //            OK = false;
        //        }
        //    }
        //    var ident = isReservoir ? "Reservoir" : "Lake";
        //    Utils.loginfo(string.Format("{0}(s) {1} intersect with basins {2}", ident, objectIds, interAreas.Keys));
        //    if (OK) {
        //        if (totalInterArea >= waterArea) {
        //            foreach (var (basin, interArea) in interAreas) {
        //                if (isReservoir) {
        //                    this.basins[basin].reservoirArea += interArea;
        //                } else {
        //                    this.basins[basin].lakeArea += interArea;
        //                }
        //                freeAreas[basin] -= interArea;
        //            }
        //            return (0, firstBasin(sortedBasins, interAreas.Keys));
        //        } else if (totalInterArea > 0) {
        //            // current intersection areas within free space but inadequate: lake presumably extends beyond watershed
        //            // try increasing proprtionately
        //            var factor = waterArea / totalInterArea;
        //            var totalInterArea2 = 0.0;
        //            var OK2 = true;
        //            foreach (var (basin, interArea) in interAreas) {
        //                interArea2 = interArea * factor;
        //                totalInterArea2 += interArea2;
        //                if (interArea2 > freeAreas[basin]) {
        //                    OK2 = false;
        //                }
        //            }
        //            if (OK2) {
        //                foreach (var (basin, interArea) in interAreas) {
        //                    interArea2 = interArea * factor;
        //                    if (isReservoir) {
        //                        this.basins[basin].reservoirArea += interArea2;
        //                    } else {
        //                        this.basins[basin].lakeArea += interArea2;
        //                    }
        //                    freeAreas[basin] -= interArea2;
        //                }
        //                return (0, firstBasin(sortedBasins, interAreas.Keys));
        //            }
        //        }
        //    }
        //    var requiredArea = waterArea;
        //    // use maximum area in each subbasin.  Start with subbasins with an intersection
        //    var basinsUsed = new List<object>();
        //    foreach (var basin in sortedBasins) {
        //        if (interAreas.Keys.Contains(basin)) {
        //            assignArea = Math.Min(requiredArea, freeAreas[basin]);
        //            requiredArea -= assignArea;
        //            freeAreas[basin] -= assignArea;
        //            if (isReservoir) {
        //                this.basins[basin].reservoirArea += assignArea;
        //            } else {
        //                this.basins[basin].lakeArea += assignArea;
        //            }
        //            basinsUsed.append(basin);
        //            if (requiredArea <= 0) {
        //                return (0, firstBasin(sortedBasins, basinsUsed));
        //            }
        //        }
        //    }
        //    // now use other basins if necessary
        //    foreach (var basin in sortedBasins) {
        //        if (!basinsUsed.Contains(basin)) {
        //            assignArea = Math.Min(requiredArea, freeAreas[basin]);
        //            requiredArea -= assignArea;
        //            freeAreas[basin] -= assignArea;
        //            if (isReservoir) {
        //                this.basins[basin].reservoirArea += assignArea;
        //            } else {
        //                this.basins[basin].lakeArea += assignArea;
        //            }
        //            basinsUsed.append(basin);
        //            if (requiredArea <= 0) {
        //                return (0, firstBasin(sortedBasins, basinsUsed));
        //            }
        //        }
        //    }
        //    return (requiredArea, firstBasin(sortedBasins, basinsUsed));
        //}

        //  Create and add features to FullHRUs shapefile.  Return True if OK.
        public virtual bool insertFeatures(
            OSGeo.OGR.Layer layer,
            Polygonize shapes,
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, int>>>> basinCropSoilSlopeNumbers,
            Dictionary<int, BasinData> basins,
            ProgressBar progressBar,
            int lastHru) {
            var subIndx = layer.FindFieldIndex(Topology._SUBBASIN, 1);
            if (subIndx < 0) {
                return false;
            }
            var luseIndx = layer.FindFieldIndex(Parameters._LANDUSE, 1);
            if (luseIndx < 0) {
                return false;
            }
            var soilIndx = layer.FindFieldIndex(Parameters._SOIL, 1);
            if (soilIndx < 0) {
                return false;
            }
            var slopeIndx = layer.FindFieldIndex(Parameters._SLOPEBAND, 1);
            if (slopeIndx < 0) {
                return false;
            }
            var areaIndx = layer.FindFieldIndex(Parameters._AREA, 1);
            if (areaIndx < 0) {
                return false;
            }
            var percentIndx = layer.FindFieldIndex(Parameters._PERCENT, 1);
            if (percentIndx < 0) {
                return false;
            }
            var hrugisIndx = layer.FindFieldIndex(Topology._HRUGIS, 1);
            if (hrugisIndx < 0) {
                return false;
            }
            progressBar.Value = 0;
            var fivePercent = Convert.ToInt32(lastHru / 20);
            var progressCount = 0;
            foreach (var kvp in basinCropSoilSlopeNumbers) {
                int basin = kvp.Key;
                Dictionary<int, Dictionary<int, Dictionary<int, int>>> cropSoilSlopeNumbers = kvp.Value;
                var basinCells = basins[basin].cellCount;
                int SWATBasin;
                if (!this._gv.topo.basinToSWATBasin.TryGetValue(basin, out SWATBasin)) {
                    SWATBasin = 0;
                }
                if (SWATBasin > 0) {
                    foreach (var kvp1 in cropSoilSlopeNumbers) {
                        int crop = kvp1.Key;
                        Dictionary<int, Dictionary<int, int>> soilSlopeNumbers = kvp1.Value;
                        foreach (var kvp2 in soilSlopeNumbers) {
                            int soil = kvp2.Key;
                            Dictionary<int, int> slopeNumbers = kvp2.Value;
                            foreach (var kvp3 in slopeNumbers) {
                                int slope = kvp3.Key;
                                int hru = kvp3.Value;
                                OSGeo.OGR.Geometry geometry = shapes.getGeometry(hru);
                                //if (geometry is null) {
                                //    return false;
                                    //                            errors = geometry.validateGeometry()
                                    //                            if len(errors) > 0:
                                    //                                Utils.error('Internal error: FullHRUs geometry invalid', self._gv.isBatch)
                                    //                                for error in errors:
                                    //                                    Utils.loginfo(str(error))
                                    //                                return False
                                    // make polygons available to garbage collection
                                    //shapes.shapesTable[hru].polygons = None
                                //}
                                FeatureDefn layerDef = layer.GetLayerDefn();
                                OSGeo.OGR.Feature feature = new OSGeo.OGR.Feature(layerDef);
                                feature.SetField(subIndx, SWATBasin);
                                feature.SetField(luseIndx, this._gv.db.getLanduseCode(crop));
                                bool OK;
                                feature.SetField(soilIndx, this._gv.db.getSoilName(soil, out OK));
                                feature.SetField(slopeIndx, this._gv.db.slopeRange(slope));
                                feature.SetField(areaIndx, shapes.area(hru) / 10000.0);
                                var percent = (double)shapes.cellCount(hru) / basinCells * 100;
                                feature.SetField(percentIndx, percent);
                                feature.SetField(hrugisIndx, "NA");
                                feature.SetGeometry(geometry);
                                var err = layer.CreateFeature(feature);
                                if (err != 0) {
                                    Utils.error(string.Format("Unable to add feature to FullHRUs shapefile {0}", this._gv.fullHRUsFile), this._gv.isBatch);
                                    progressBar.Visible = false;
                                    return false;
                                }
                                if (progressCount == fivePercent) {
                                    progressBar.Value = progressBar.Value + 5;
                                    progressCount = 0;
                                } else {
                                    progressCount += 1;
                                }
                            }
                        }
                    }
                }
            }
            progressBar.Visible = false;
            return true;
        }

        // Create FullHRUs shapefile.
        public async Task<bool> createFullHRUsShapefile(
            Polygonize shapes,
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, int>>>> basinCropSoilSlopeNumbers,
            Dictionary<int, BasinData> basins,
            ProgressBar progressBar,
            int lastHru) {
            OSGeo.OGR.Layer layer;
            var ft = FileTypes._HRUS;
            var legend = Utils._FULLHRUSLEGEND;
            if (Utils.shapefileExists(this._gv.fullHRUsFile)) {
                await Utils.removeLayer(this._gv.fullHRUsFile);
                DataSource ds = Ogr.Open(this._gv.fullHRUsFile, 1);
                layer = ds.GetLayerByIndex(0);
                for (long i = layer.GetFeatureCount(1) - 1; i >= 0; i--) {
                    layer.DeleteFeature(i);
                }
            } else {
                await Utils.removeLayer(this._gv.fullHRUsFile);
                var driver = Ogr.GetDriverByName("ESRI Shapefile");
                DataSource ds = driver.CreateDataSource(this._gv.fullHRUsFile, null);
                var wkt = this._gv.topo.crsProject.Wkt;
                OSGeo.OSR.SpatialReference srs = new OSGeo.OSR.SpatialReference(wkt);
                layer = ds.CreateLayer("Full HRUs", srs, wkbGeometryType.wkbMultiPolygon, null);
                layer.CreateField(new FieldDefn(Topology._SUBBASIN, OSGeo.OGR.FieldType.OFTInteger), 1);
                layer.CreateField(new FieldDefn(Parameters._LANDUSE, OSGeo.OGR.FieldType.OFTString), 1);
                layer.CreateField(new FieldDefn(Parameters._SOIL,   OSGeo.OGR.FieldType.OFTString), 1);
                layer.CreateField(new FieldDefn(Parameters._SLOPEBAND, OSGeo.OGR.FieldType.OFTString), 1);
                layer.CreateField(new FieldDefn(Parameters._AREA, OSGeo.OGR.FieldType.OFTReal), 1);
                layer.CreateField(new FieldDefn(Parameters._PERCENT, OSGeo.OGR.FieldType.OFTReal), 1);
                layer.CreateField(new FieldDefn(Topology._HRUGIS, OSGeo.OGR.FieldType.OFTString), 1);
                Utils.copyPrj(this._gv.demFile, this._gv.fullHRUsFile);
                //var parms = Geoprocessing.MakeValueArray(this._gv.fullHRUsFile, Path.ChangeExtension(this._gv.demFile, ".prj"));
                //Utils.runPython("runDefineProjection.py", parms, this._gv);
            }
            if (this.insertFeatures(layer, shapes, basinCropSoilSlopeNumbers, basins, progressBar, lastHru)) {
                legend = Utils._FULLHRUSLEGEND;
                //var styleFile = "fullhrus.qml";
                //layer = QgsVectorLayer(this._gv.fullHRUsFile, "{0} ({1})", legend, QFileInfo(this._gv.fullHRUsFile).baseName()), "ogr");
                // insert above dem (or hillshade if exists) in legend, so streams and watershed still visible
                var demLayer = (await Utils.getLayerByFilename(this._gv.demFile, FileTypes._DEM, null, null, null)).Item1 as RasterLayer;
                var hillshadeLayer = Utils.getLayerByLegend(Utils._HILLSHADELEGEND);
                ArcGIS.Desktop.Mapping.Layer subLayer;
                if (hillshadeLayer is not null) {
                    subLayer = hillshadeLayer;
                } else {
                    subLayer = demLayer;
                }
                var group = MapView.Active.Map.Layers.OfType<GroupLayer>().FirstOrDefault(layer => string.Equals(layer.Name, Utils._WATERSHED_GROUP_NAME));
                var index = Utils.groupIndex(group, subLayer);
                await Utils.removeLayerByLegend(legend);
                if (group is not null) {
                    var baseName = Path.ChangeExtension(Path.GetFileName(this._gv.fullHRUsFile), null);
                    var mapLayer = await QueuedTask.Run(() => LayerFactory.Instance.CreateLayer(new Uri(this._gv.fullHRUsFile), group, index, String.Format("{0} ({1})", legend, baseName)));
                    FileTypes.ApplySymbolToFeatureLayerAsync((FeatureLayer)mapLayer, ft, this._gv);
                    Utils.setMapTip((FeatureLayer)mapLayer, ft);
                }
                return true;
            } else {
                return false;
            }
        }

        // Count possible HRUs in watershed.
        public virtual int countFullHRUs() {
            var count = 0;
            foreach (var data in this.basins.Values) {
                count += data.hruMap.Count;
            }
            return count;
        }

        // Create area maps for each subbasin.
        public virtual void saveAreas(bool isOriginal, bool redistributeNodata = true) {

            void deleteSubbasinsFromTables(List<int> basinsToDelete, Dictionary<int, int> newBasinToSWATBasin) {
                List<int> SWATBasinsToDelete = (from basin in this._gv.topo.basinToSWATBasin.Keys
                                                where basinsToDelete.Contains(basin)
                                                select this._gv.topo.basinToSWATBasin[basin]).ToList();
                string sql0 = "DELETE FROM {0} WHERE Subbasin={1}";
                string sql1 = "UPDATE Reach SET SubbasinR=0 WHERE SubbasinR={0}";
                string sql2 = "UPDATE {0} SET Subbasin={1} WHERE Subbasin={2}";
                string sql3 = "UPDATE Reach SET SubbasinR={0} WHERE SubbasinR={1}";
                string sql4 = "INSERT INTO SubbasinChanges VALUES({0},{1})";
                List<string> tables = new List<string>() {
                    "Reach",
                    "MonitoringPoint",
                    "lake",
                    "playa",
                    "pnd",
                    "res"
                };
                foreach (var SWATBasin in SWATBasinsToDelete) {
                    foreach (var table in tables) {
                        this._gv.db.execNonQuery(string.Format(sql0, table, SWATBasin));
                    }
                }
                // renumber other subbasins
                var renumber = new Dictionary<int, int>();
                foreach (var kvp in this._gv.topo.basinToSWATBasin) {
                    var basin = kvp.Key;
                    var oldSub = kvp.Value;
                    if (basinsToDelete.Contains(basin)) {
                        this._gv.db.execNonQuery(string.Format(sql1, oldSub));
                        this._gv.db.execNonQuery(string.Format(sql4, oldSub, -1));
                        continue;
                    }
                    var newSub = newBasinToSWATBasin[basin];
                    renumber[oldSub] = newSub;
                }
                // need to use renumber in ascending order to avoid renumbering twice
                // e.g if we have 7 -> 6 and we renumber 7 to 6 and then 6 to 5 we end up with 5 -> 5
                foreach (var oldSub in renumber.Keys.OrderBy(_p_1 => _p_1).ToList()) {
                    var newSub = renumber[oldSub];
                    if (oldSub != newSub) {
                        foreach (var table in tables) {
                            this._gv.db.execNonQuery(string.Format(sql2, table, newSub, oldSub));
                        }
                        this._gv.db.execNonQuery(string.Format(sql3, newSub, oldSub));
                        this._gv.db.execNonQuery(string.Format(sql4, oldSub, newSub));
                    }
                }
            }
            foreach (var data in this.basins.Values) {
                data.setAreas(isOriginal, this._gv.isBatch, redistributeNodata: redistributeNodata);
            }
            if ((this._gv.isHUC || this._gv.isHAWQS) && isOriginal) {
                // remove empty basins, which can occur for subbasins outside soil and/or landuse maps
                List<int> basinsToDelete = (from _tup_1 in this.basins
                                            let basin = _tup_1.Key
                                            let data = _tup_1.Value
                                            where data.area == 0
                                            select basin).ToList();
                if (basinsToDelete.Count > 0) {
                    var sql0 = "DROP TABLE IF EXISTS SubbasinChanges";
                    this._gv.db.execNonQuery(sql0);
                    var sql1 = "CREATE TABLE SubbasinChanges (OldSub INTEGER, NewSub INTEGER)";
                    this._gv.db.execNonQuery(sql1);
                    foreach (var basin in basinsToDelete) {
                        this.basins.Remove(basin);
                    }
                    // rewrite basinToSWATBasin and SWATBasinToBasin maps
                    var newBasinToSWATBasin = new Dictionary<int, int>();
                    var newSWATBasinToBasin = new Dictionary<int, int>();
                    var newSWATBasin = 0;
                    foreach (var nextNum in this._gv.topo.SWATBasinToBasin.Keys.OrderBy(_p_1 => _p_1).ToList()) {
                        var basin = this._gv.topo.SWATBasinToBasin[nextNum];
                        if (basinsToDelete.Contains(basin)) {
                            continue;
                        } else {
                            newSWATBasin += 1;
                            newSWATBasinToBasin[newSWATBasin] = basin;
                            newBasinToSWATBasin[basin] = newSWATBasin;
                        }
                    }
                    deleteSubbasinsFromTables(basinsToDelete, newBasinToSWATBasin);
                    this._gv.topo.SWATBasinToBasin = newSWATBasinToBasin;
                    this._gv.topo.basinToSWATBasin = newBasinToSWATBasin;
                }
            }
            if (!redistributeNodata) {
                // need to correct the drain areas of the basins, using the defined area of each
                this.defineDrainAreas();
            }
        }

        // Reset drain areas map according to defined area value of each basin and update Reach table.  
        //         For use with HUC models, so we can assume number of basins is small and use recursion.
        //         Also assumes (because HUC model) there are no basins above inlets.
        public virtual void defineDrainAreas() {

            double drainArea(Dictionary<int, List<int>> us, int link) {
                double result;
                double drainAreaDone;
                bool ok = this._gv.topo.drainAreas.TryGetValue(link, out drainAreaDone);
                if (ok) {
                    return drainAreaDone;
                }
                int basin;
                ok = this._gv.topo.linkToBasin.TryGetValue(link, out basin);
                if (!ok) {
                    result = 0.0;
                } else {
                    BasinData basinData;
                    ok = this.basins.TryGetValue(basin, out basinData);
                    if (!ok) {
                        result = 0.0;
                    } else {
                        List<int> ups;
                        ok = us.TryGetValue(link, out ups);
                        if (!ok) {
                            ups = new List<int>();
                        }
                        result = basinData.cropSoilSlopeArea + (from l in ups
                                                                select drainArea(us, l)).ToList().Sum();
                    }
                }
                this._gv.topo.drainAreas[link] = result;
                return result;
            }
            // build us relation from downlinks map
            //Debug.Assert(this._gv.isHUC || this._gv.isHAWQS);
            //Debug.Assert("Internal error: definedDrainAreas called for non-HUC model");
            var us = new Dictionary<int, List<int>>();
            foreach (var kvp in this._gv.topo.downLinks) {
                var link = kvp.Key;
                var dsLink = kvp.Value;
                if (dsLink >= 0) {
                    if (us.ContainsKey(dsLink)) {
                        us[dsLink].Add(link);
                    } else {
                        us[dsLink] = new List<int>() { link };
                    }
                }
            }
            // redefine link drain areas and update Reach table
            this._gv.topo.drainAreas = new Dictionary<int, double>();
            var sql = "UPDATE Reach SET AreaC={0}, Wid2={1}, Dep2={2} WHERE Subbasin={3}";
            foreach (var kvp in this._gv.topo.linkToBasin) {
                var link = kvp.Key;
                var basin = kvp.Value;
                BasinData basinData;
                bool ok = this.basins.TryGetValue(basin, out basinData);
                if (ok) {
                    basinData.drainArea = drainArea(us, link);
                    var drainAreaHa = basinData.drainArea / 10000.0;
                    var drainAreaKm = drainAreaHa / 100.0;
                    var SWATBasin = this._gv.topo.basinToSWATBasin[basin];
                    var channelWidth = 1.29 * Math.Pow(drainAreaKm, 0.6);
                    var channelDepth = 0.13 * Math.Pow(drainAreaKm, 0.4);
                    this._gv.db.execNonQuery(string.Format(sql, drainAreaHa, channelWidth, channelDepth, SWATBasin));
                }
            }
        }

        // Convert basin data to HRU data.
        public virtual void basinsToHRUs() {
            HRUData hruData;
            int origCrop;
            Double totalSlope;
            int slope;
            int soil;
            int crop;
            int relHru;
            int basin;
            IEnumerable<int> iterator;
            // First clear in case this is a rerun
            this.hrus.Clear();
            // hru number across watershed
            var hru = 0;
            if (this._gv.useGridModel) {
                iterator = this.basins.Keys;
            } else {
                iterator = Enumerable.Range(0, this._gv.topo.SWATBasinToBasin.Count);
            }
            // deal with basins in SWATBasin order so that HRU numbers look logical
            bool ok;
            foreach (var i in iterator) {
                if (this._gv.useGridModel) {
                    basin = i;
                    ok = true;
                } else {
                    // i will range from 0 to n-1, SWATBasin from 1 to n
                    ok = this._gv.topo.SWATBasinToBasin.TryGetValue(i + 1, out basin);
                }
                if (ok) {
                    BasinData basinData;
                    ok = this.basins.TryGetValue(basin, out basinData);
                    if (!ok) {
                        Utils.error(string.Format("SWAT basin {0} not defined", i + 1), this._gv.isBatch);
                        return;
                    }
                    if (!this.isMultiple) {
                        hru += 1;
                        relHru = 1;
                        if (this.isDominantHRU) {
                            (crop, soil, slope) = basinData.getDominantHRU();
                        } else {
                            crop = BasinData.dominantKey(basinData.originalCropAreas);
                            if (crop < 0) {
                                throw new Exception(string.Format("No landuse data for basin {0}", basin));
                            }
                            soil = BasinData.dominantKey(basinData.originalSoilAreas);
                            if (soil < 0) {
                                throw new Exception(string.Format("No soil data for basin {0}", basin));
                            }
                            slope = BasinData.dominantKey(basinData.originalSlopeAreas);
                            if (slope < 0) {
                                throw new Exception(string.Format("No slope data for basin {0}", basin));
                            }
                        }
                        var area = basinData.area;
                        var cellCount = basinData.cellCount;
                        totalSlope = basinData.totalSlope;
                        origCrop = crop;
                        hruData = new HRUData(basin, crop, origCrop, soil, slope, cellCount, area, totalSlope, this._gv.cellArea, 1);
                        this.hrus[hru] = hruData;
                    } else {
                        // multiple
                        if (this._gv.forTNC) {
                            // Add dummy HRU for TNC projects, 0.1% of grid cell.
                            // Easiest method is to use basinData.addCell for each cell: there will not be many. 
                            // Before we add a dummy HRU of 0.1% we should reduce all the existing HRUs by that amount.
                            var pointOnePercent = 0.001;
                            basinData.redistribute(1 - pointOnePercent);
                            // alse reduce basin area
                            basinData.area *= 1 - pointOnePercent;
                            var cellCount = Math.Max(1, Convert.ToInt32(basinData.cellCount * pointOnePercent));
                            crop = this._gv.db.getLanduseCat("DUMY");
                            soil = BasinData.dominantKey(basinData.originalSoilAreas);
                            double area = this._gv.cellArea;
                            var meanElevation = basinData.totalElevation / basinData.cellCount;
                            var meanSlopeValue = basinData.totalSlope / basinData.cellCount;
                            slope = this._gv.db.slopeIndex(meanSlopeValue);
                            foreach (var _ in Enumerable.Range(0, cellCount)) {
                                basinData.addCell(crop, soil, slope, area, meanElevation, meanSlopeValue, this._gv.distNoData, this._gv);
                            }
                            // bring areas up to date (including adding DUMY crop).  Not original since we have already removed small HRUs.
                            basinData.setAreas(false, this._gv.isBatch);
                        }
                        // hru number within subbasin
                        relHru = 0;
                        foreach (var kvp0 in basinData.cropSoilSlopeNumbers) {
                            foreach (var kvp1 in kvp0.Value) {
                                foreach (var kvp2 in kvp1.Value) {
                                    var cellData = basinData.hruMap[kvp2.Value];
                                    hru += 1;
                                    relHru += 1;
                                    var area = cellData.area;
                                    var cellCount = cellData.cellCount;
                                    totalSlope = cellData.totalSlope;
                                    origCrop = cellData.crop;
                                    hruData = new HRUData(basin, kvp0.Key, origCrop, kvp1.Key, kvp2.Key, cellCount, area, totalSlope, this._gv.cellArea, relHru);
                                    this.hrus[hru] = hruData;
                                }
                            }
                        }
                    }
                }
                // ensure dummy HRU not eliminated
                if (this._gv.forTNC) {
                    this._gv.exemptLanduses.Add("DUMY");
                }
            }
        }

        // Return the maximum subbasin area in hectares.
        public virtual double maxBasinArea() {
            var maximum = 0.0;
            foreach (var basinData in this.basins.Values) {
                var area = basinData.area;
                if (area > maximum) {
                    maximum = area;
                }
            }
            return maximum / 10000;
        }

        // 
        //         Return the minimum across the watershed of the largest percentage (or area in hectares)
        //         of a crop within each subbasin.
        //         
        //         Finds the least percentage (or area) across the subbasins of the percentages 
        //         (or areas) of the dominant crop in the subbasins.  This is the maximum percentage (or area)
        //         acceptable for the minuimum crop percentage (or area) to be used to form multiple HRUs.  
        //         If the user could choose a percentage (or area) above this figure then at
        //         least one subbasin would have no HRU.
        //         This figure is only advisory since limits are checked during removal.
        //         
        public virtual double minMaxCropVal(bool useArea) {
            double val;
            var minMax = useArea ? double.PositiveInfinity : 100.0;
            foreach (var kvp in this.basins) {
                var basin = kvp.Key;
                var basinData = kvp.Value;
                var cropAreas = basinData.originalCropAreas;
                var crop = BasinData.dominantKey(cropAreas);
                if (crop < 0) {
                    if (this._gv.isHUC || this._gv.isHAWQS) {
                        val = 0.0;
                    } else {
                        throw new Exception(string.Format("No landuse data for basin {0}", basin));
                    }
                } else {
                    val = useArea ? cropAreas[crop] / 10000 : cropAreas[crop] / basinData.cropSoilSlopeArea * 100;
                }
                // Utils.loginfo('Max crop value {0} for basin {1}', int(val), self._gv.topo.basinToSWATBasin[basin]))
                if (val < minMax) {
                    minMax = val;
                }
            }
            return minMax;
        }

        // 
        //         Return the minimum across the watershed of the largest area in hectares
        //         of a soil within each subbasin.
        //         
        //         Finds the least area across the subbasins of the areas of the dominant soil
        //         in the subbasins.  This is the maximum area
        //         acceptable for the minuimum soil area to be used to form multiple HRUs.  
        //         If the user could choose an area above this figure then at
        //         least one subbasin would have no HRU.
        //         This figure is only advisory since limits are checked during removal.
        //         
        public virtual double minMaxSoilArea() {
            var minMax = double.PositiveInfinity;
            foreach (var kvp in this.basins) {
                var basin = kvp.Key;
                var basinData = kvp.Value;
                var soilAreas = basinData.originalSoilAreas;
                var soil = BasinData.dominantKey(soilAreas);
                if (soil < 0) {
                    throw new Exception(string.Format("No soil data for basin {0}", basin));
                }
                var val = soilAreas[soil] / 10000;
                // Utils.loginfo('Max soil area {0} for basin {1}', int(val), self._gv.topo.basinToSWATBasin[basin]))
                if (val < minMax) {
                    minMax = val;
                }
            }
            return minMax;
        }

        // 
        //         Return the minimum across the watershed of the largest area in hectares
        //         of a slope within each subbasin.
        //         
        //         Finds the least area across the subbasins of the areas of the dominant slope
        //         in the subbasins.  This is the maximum area
        //         acceptable for the minuimum slope area to be used to form multiple HRUs.  
        //         If the user could choose an area above this figure then at
        //         least one subbasin would have no HRU.
        //         This figure is only advisory since limits are checked during removal.
        //         
        public virtual double minMaxSlopeArea() {
            var minMax = double.PositiveInfinity;
            foreach (var kvp in this.basins) {
                var basin = kvp.Key;
                var basinData = kvp.Value;
                var slopeAreas = basinData.originalSlopeAreas;
                var slope = BasinData.dominantKey(slopeAreas);
                if (slope < 0) {
                    throw new Exception(string.Format("No slope data for basin {0}", basin));
                }
                var val = slopeAreas[slope] / 10000;
                // Utils.loginfo('Max slope area {0} for basin {1}', int(val), self._gv.topo.basinToSWATBasin[basin]))
                if (val < minMax) {
                    minMax = val;
                }
            }
            return minMax;
        }

        // 
        //         Return the minimum across the watershed of the percentages
        //         of the dominant soil in the crops included by minCropVal.
        // 
        //         Finds the least percentage across the watershed of the percentages 
        //         of the dominant soil in the crops included by minCropVal.  
        //         This is the maximum percentage acceptable for the minimum soil
        //         percentage to be used to form multiple HRUs.  
        //         If the user could choose a percentage above this figure then
        //         at least one soil in one subbasin would have no HRU.
        //         This figure is only advisory since limits are checked during removal.
        //         
        public virtual double minMaxSoilPercent(double minCropVal) {
            var minMax = 100.0;
            foreach (var basinData in this.basins.Values) {
                var cropAreas = basinData.originalCropAreas;
                foreach (var kvp in cropAreas) {
                    var crop = kvp.Key;
                    var cropArea = kvp.Value;
                    var cropVal = cropArea / basinData.cropSoilSlopeArea * 100;
                    if (cropVal >= minCropVal) {
                        // crop will be included.  Find the maximum area or percentage for soils for this crop.
                        var maximum = 0.0;
                        var soilSlopeNumbers = basinData.cropSoilSlopeNumbers[crop];
                        foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                            var area = 0.0;
                            foreach (var hru in slopeNumbers.Values) {
                                var cellData = basinData.hruMap[hru];
                                area += cellData.area;
                            }
                            var soilVal = area / cropArea * 100;
                            if (soilVal > maximum) {
                                maximum = soilVal;
                            }
                        }
                        if (maximum < minMax) {
                            minMax = maximum;
                        }
                    }
                }
            }
            return minMax;
        }

        // 
        //         Return the minimum across the watershed of the percentages 
        //         of the dominant slope in the crops and soils included by 
        //         minCropPercent and minSoilPercent.
        //         
        //         Finds the least percentage across the subbasins of the percentages 
        //         of the dominant slope in the crops and soils included by 
        //         minCropVal and minSoilVal.
        //         This is the maximum percentage  acceptable for the minimum slope
        //         percentage to be used to form multiple HRUs.  
        //         If the user could choose a percentage above this figure then
        //         at least one slope in one subbasin would have no HRU.
        //         This figure is only advisory since limits are checked during removal.
        //         
        public virtual double minMaxSlopePercent(double minCropVal, double minSoilVal) {
            CellData cellData;
            var minMax = 100.0;
            foreach (var basinData in this.basins.Values) {
                var cropAreas = basinData.originalCropAreas;
                foreach (var kvp in cropAreas) {
                    var crop = kvp.Key;
                    var cropArea = kvp.Value;
                    var cropVal = cropArea / basinData.cropSoilSlopeArea * 100;
                    if (cropVal >= minCropVal) {
                        // crop will be included.
                        var soilSlopeNumbers = basinData.cropSoilSlopeNumbers[crop];
                        foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                            // first find if this soil is to be included
                            var soilArea = 0.0;
                            foreach (var hru in slopeNumbers.Values) {
                                cellData = basinData.hruMap[hru];
                                soilArea += cellData.area;
                            }
                            var soilVal = soilArea / cropArea * 100;
                            if (soilVal >= minSoilVal) {
                                // soil will be included.
                                // Find the maximum percentage area for slopes for this soil.
                                var maximum = 0.0;
                                foreach (var hru in slopeNumbers.Values) {
                                    cellData = basinData.hruMap[hru];
                                    var slopeVal = cellData.area / soilArea * 100;
                                    if (slopeVal > maximum) {
                                        maximum = slopeVal;
                                    }
                                }
                                if (maximum < minMax) {
                                    minMax = maximum;
                                }
                            }
                        }
                    }
                }
            }
            return minMax;
            // beware = this function is no longer used and is also out of date because 
            // it has not been revised to allow for both area and percentages as thresholds
            //===============================================================================
            //     def cropSoilAndSlopeThresholdsAreOK(self):
            //         """
            //         Check that at least one hru will be left in each subbasin 
            //         after applying thresholds.
            //         
            //         This is really a precondition for removeSmallHRUsByThreshold.
            //         It checks that at least one crop will be left
            //         in each subbasin, that at least one soil will be left for each crop,
            //         and that at least one slope will be left for each included crop and 
            //         soil combination.
            //         """
            //         minCropVal = self.landuseVal
            //         minSoilVal = self.soilVal
            //         minSlopeVal = self.slopeVal
            // 
            //         for basinData in self.basins.Values:
            //             cropAreas = basinData.originalCropAreas
            //             cropFound = False
            //             minCropArea = minCropVal * 10000 if self.useArea else (float(basinData.cropSoilSlopeArea) * minCropVal) / 100
            //             for (crop, area) in cropAreas:
            //                 cropFound = cropFound or (area >= minCropArea)
            //                 if area >= minCropArea:
            //                     # now check soils for this crop
            //                     soilFound = False
            //                     minSoilArea = minSoilVal * 10000 if self.useArea else (float(area) * minSoilVal) / 100
            //                     soilSlopeNumbers = basinData.cropSoilSlopeNumbers[crop]
            //                     for slopeNumbers in soilSlopeNumbers.Values:
            //                         soilArea = 0
            //                         for hru in slopeNumbers.Values:
            //                             cellData = basinData.hruMap[hru]
            //                             soilArea += cellData.area
            //                         soilFound = soilFound or (soilArea >= minSoilArea)
            //                         if soilArea >= minSoilArea:
            //                             # now sheck for slopes for this soil
            //                             slopeFound = False
            //                             minSlopeArea = minSlopeVal * 10000 if self.useArea else (float(soilArea) * minSlopeVal) / 100
            //                             for hru in slopeNumbers.Values:
            //                                 cellData = basinData.hruMap[hru]
            //                                 slopeFound = (cellData.area >= minSlopeArea)
            //                                 if slopeFound: break
            //                             if not slopeFound: return False
            //                     if not soilFound: return False
            //             if not cropFound: return False
            //         return True
            //===============================================================================
        }

        // 
        //         Remove from basins data HRUs that are below the minimum area or minumum percent.
        //         
        //         Removes from basins data HRUs that are below areaVal 
        //         (which is in hectares if useArea is true, else is a percentage) 
        //         and redistributes their areas and slope 
        //         totals in proportion to the other HRUs.
        //         Crop, soil, and slope nodata cells are also redistributed, 
        //         so the total area of the retained HRUs should eventually be the 
        //         total area of the subbasin.
        //         
        //         The algorithm removes one HRU at a time, the smallest, 
        //         redistributing its area to the others, until all are above the 
        //         threshold.  So an HRU that was initially below the
        //         threshold may be retained because redistribution from smaller 
        //         ones lifts its area above the threshold.
        //         
        //         The area of the whole subbasin can be below the minimum area, 
        //         in which case the dominant HRU will finally be left.
        //         
        public virtual void removeSmallHRUsByArea() {
            foreach (var kvp in this.basins) {
                var basin = kvp.Key;
                var basinData = kvp.Value;
                var count = basinData.hruMap.Count;
                // self.areaVal is either an area in hectares or a percentage of the subbasin
                // in either case convert to square metres
                var basinThreshold = this.useArea ? this.areaVal * 10000 : basinData.cropSoilSlopeArea * this.areaVal / 100;
                var areaToRedistribute = 0.0;
                var unfinished = true;
                while (unfinished) {
                    // find smallest non-exempt HRU
                    var minCrop = 0;
                    var minSoil = 0;
                    var minSlope = 0;
                    var minHru = 0;
                    var minArea = basinThreshold;
                    foreach (var kvp0 in basinData.cropSoilSlopeNumbers) {
                        var crop = kvp0.Key;
                        var soilSlopeNumbers = kvp0.Value;
                        if (!this._gv.isExempt(crop)) {
                            foreach (var kvp1 in soilSlopeNumbers) {
                                var soil = kvp1.Key;
                                var slopeNumbers = kvp1.Value;
                                foreach (var kvp2 in slopeNumbers) {
                                    var slope = kvp2.Key;
                                    var hru = kvp2.Value;
                                    var cellData = basinData.hruMap[hru];
                                    var hruArea = cellData.area;
                                    if (hruArea < minArea) {
                                        minArea = hruArea;
                                        minHru = hru;
                                        minCrop = crop;
                                        minSoil = soil;
                                        minSlope = slope;
                                    }
                                }
                            }
                        }
                    }
                    if (minArea < basinThreshold) {
                        // Don't remove last hru.
                        // This happens when the subbasin area is below the area threshold
                        if (count > 1) {
                            basinData.removeHRU(minHru, minCrop, minSoil, minSlope);
                            count -= 1;
                            areaToRedistribute += minArea;
                        } else {
                            // count is 1; ensure termination after redistributing
                            unfinished = false;
                        }
                        if (areaToRedistribute > 0) {
                            // make sure we don't divide by zero
                            if (basinData.cropSoilSlopeArea - areaToRedistribute == 0) {
                                throw new Exception(string.Format("No HRUs for basin {0}", basin));
                            }
                            var redistributeFactor = basinData.cropSoilSlopeArea / (basinData.cropSoilSlopeArea - areaToRedistribute);
                            basinData.redistribute(redistributeFactor);
                            areaToRedistribute = 0;
                        }
                    } else {
                        unfinished = false;
                    }
                }
            }
        }

        // 
        //         Remove HRUs that are below the minCropVal, minSoilVal, 
        //         or minSlopeVal, where the values are percentages.
        // 
        //         Remove from basins data HRUs that are below the minCropVal,
        //         minSoilVal, or minSlopeVal, where the values are percentages, and 
        //         redistribute their areas in proportion to the other HRUs.  
        //         Crop, soil, and slope nodata cells are also redistributed, 
        //         so the total area of the retained HRUs should eventually be the total
        //         area of the subbasin.
        //         
        public virtual void removeSmallHRUsByThresholdPercent() {
            CellData cellData;
            double soilArea;
            double minArea;
            var minCropPercent = this.landuseVal;
            var minSoilPercent = this.soilVal;
            var minSlopePercent = this.slopeVal;
            foreach (var kvp in this.basins) {
                var basin = kvp.Key;
                var basinData = kvp.Value;
                var cropAreas = basinData.originalCropAreas;
                var areaToRedistribute = 0.0;
                var minCropArea = basinData.cropSoilSlopeArea * minCropPercent / 100;
                // reduce area if necessary to avoid removing all crops
                if (!this.hasExemptCrop(basinData)) {
                    minCropArea = Math.Min(minCropArea, CreateHRUs.maxValue(cropAreas));
                }
                foreach (var kvp0 in cropAreas) {
                    var crop = kvp0.Key;
                    var area = kvp0.Value;
                    if (!this._gv.isExempt(crop)) {
                        if (area < minCropArea) {
                            areaToRedistribute += area;
                            // remove this crop
                            // going to change maps so use lists
                            var soilSlopeNumbers = basinData.cropSoilSlopeNumbers[crop];
                            foreach (var (soil, slopeNumbers) in soilSlopeNumbers.ToList()) {
                                foreach (var (slope, hru) in slopeNumbers.ToList()) {
                                    basinData.removeHRU(hru, crop, soil, slope);
                                }
                            }
                        }
                    }
                }
                if (areaToRedistribute > 0) {
                    // just to make sure we don't divide by zero
                    if (basinData.cropSoilSlopeArea - areaToRedistribute == 0) {
                        throw new Exception(string.Format("No landuse data for basin {0}", basin));
                    }
                    var redistributeFactor = basinData.cropSoilSlopeArea / (basinData.cropSoilSlopeArea - areaToRedistribute);
                    basinData.redistribute(redistributeFactor);
                }
                // Now have to remove soil areas within each crop area that are
                // less than minSoilVal for that crop.
                // First create crop areas map (not overwriting the original)
                basinData.setCropAreas(false, this._gv.isBatch);
                cropAreas = basinData.cropAreas;
                foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers) {
                    var cropArea = cropAreas[crop];
                    minArea = cropArea * minSoilPercent / 100;
                    var soilAreas = basinData.cropSoilAreas(crop, this._gv.isBatch);
                    // reduce area if necessary to avoid removing all soils for this crop
                    minArea = Math.Min(minArea, CreateHRUs.maxValue(soilAreas));
                    var soilAreaToRedistribute = 0.0;
                    // Cannot use original soilSlopeNumbers as we will remove domain elements, so iterate with items()
                    foreach (var (soil, slopeNumbersCopy) in soilSlopeNumbers.ToList()) {
                        // first calculate area for this soil
                        soilArea = soilAreas[soil];
                        if (soilArea < minArea) {
                            // add to area to redistribute
                            soilAreaToRedistribute += soilArea;
                            // remove hrus
                            foreach (var (slope, hru) in slopeNumbersCopy.ToList()) {
                                basinData.removeHRU(hru, crop, soil, slope);
                            }
                        }
                    }
                    if (soilAreaToRedistribute > 0) {
                        // now redistribute
                        // just to make sure we don't divide by zero
                        if (cropArea - soilAreaToRedistribute == 0) {
                            throw new Exception(string.Format("No soil data for landuse {1} in basin {0}", basin, crop));
                        }
                        var soilRedistributeFactor = cropArea / (cropArea - soilAreaToRedistribute);
                        foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                            foreach (var hru in slopeNumbers.Values) {
                                cellData = basinData.hruMap[hru];
                                cellData.multiply(soilRedistributeFactor);
                                basinData.hruMap[hru] = cellData;
                            }
                        }
                    }
                }
                // Now we remove the slopes for each remaining crop/soil combination
                // that fall below minSlopePercent.
                foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers) {
                    foreach (var (soil, slopeNumbers) in soilSlopeNumbers) {
                        // first calculate area for the soil
                        soilArea = 0;
                        foreach (var hru in slopeNumbers.Values) {
                            cellData = basinData.hruMap[hru];
                            soilArea += cellData.area;
                        }
                        minArea = soilArea * minSlopePercent / 100;
                        var slopeAreas = basinData.cropSoilSlopeAreas(crop, soil, this._gv.isBatch);
                        // reduce minArea if necessary to avoid removing all slopes for this crop and soil
                        minArea = Math.Min(minArea, CreateHRUs.maxValue(slopeAreas));
                        var slopeAreaToRedistribute = 0.0;
                        // Use list as we will remove domain elements from original
                        foreach (var (slope, hru) in slopeNumbers.ToList()) {
                            // first calculate the area for this slope
                            var slopeArea = slopeAreas[slope];
                            if (slopeArea < minArea) {
                                // add to area to redistribute
                                slopeAreaToRedistribute += slopeArea;
                                // remove hru
                                basinData.removeHRU(hru, crop, soil, slope);
                            }
                        }
                        if (slopeAreaToRedistribute > 0) {
                            // Now redistribute removed slope areas
                            // just to make sure we don't divide by zero
                            if (soilArea - slopeAreaToRedistribute == 0) {
                                throw new Exception(string.Format("No slope data for landuse {1} and soil {2} in basin {0}", basin, crop, soil));
                            }
                            var slopeRedistributeFactor = soilArea / (soilArea - slopeAreaToRedistribute);
                            foreach (var hru in slopeNumbers.Values) {
                                cellData = basinData.hruMap[hru];
                                cellData.multiply(slopeRedistributeFactor);
                                basinData.hruMap[hru] = cellData;
                            }
                        }
                    }
                }
            }
        }

        // 
        //         Remove HRUs that are below the minCropVal, minSoilVal, 
        //         or minSlopeVal, where the values are areas in hectares.
        // 
        //         Remove from basins data HRUs that are below the minCropVal,
        //         minSoilVal, or minSlopeVal, where the values are areas, and 
        //         redistribute their areas in proportion to the other HRUs.  
        //         Crop, soil, and slope nodata cells are also redistributed, 
        //         so the total area of the retained HRUs should eventually be the total
        //         area of the subbasin.
        //         
        public virtual void removeSmallHRUsByThresholdArea() {
            double minCropArea;
            // convert threshold areas to square metres
            var minCropAreaBasin = this.landuseVal * 10000;
            var minSoilAreaBasin = this.soilVal * 10000;
            var minSlopeAreaBasin = this.slopeVal * 10000;
            foreach (var (basin, basinData) in this.basins) {
                var cropAreas = basinData.originalCropAreas;
                // reduce area if necessary to avoid removing all crops
                if (!this.hasExemptCrop(basinData)) {
                    minCropArea = Math.Min(minCropAreaBasin, CreateHRUs.maxValue(cropAreas));
                } else {
                    minCropArea = minCropAreaBasin;
                }
                var areaToRedistribute = 0.0;
                foreach (var (crop, area) in cropAreas) {
                    if (!this._gv.isExempt(crop)) {
                        if (area < minCropArea) {
                            // remove this crop
                            // going to change maps so use lists
                            var soilSlopeNumbers = basinData.cropSoilSlopeNumbers[crop];
                            foreach (var (soil, slopeNumbers) in soilSlopeNumbers.ToList()) {
                                foreach (var (slope, hru) in slopeNumbers.ToList()) {
                                    areaToRedistribute += basinData.hruMap[hru].area;
                                    basinData.removeHRU(hru, crop, soil, slope);
                                }
                            }
                        }
                    }
                }
                // Now have to remove soil areas that are
                // less than minSoilArea
                var soilAreas = basinData.originalSoilAreas;
                // reduce area if necessary to avoid removing all soils
                var minSoilArea = Math.Min(minSoilAreaBasin, CreateHRUs.maxValue(soilAreas));
                foreach (var (soil, area) in soilAreas) {
                    if (area < minSoilArea) {
                        // remove this soil
                        // going to change maps so use lists
                        foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers.ToList()) {
                            // possible that soil has been removed
                            Dictionary<int, int> slopeNumbers2;
                            bool ok = soilSlopeNumbers.TryGetValue(soil, out slopeNumbers2);
                            if (ok) {
                                foreach (var (slope, hru) in slopeNumbers2.ToList()) {
                                    areaToRedistribute += basinData.hruMap[hru].area;
                                    basinData.removeHRU(hru, crop, soil, slope);
                                }
                            }
                        }
                    }
                }
                // Now we remove the slopes that are less than minSlopeArea
                var slopeAreas = basinData.originalSlopeAreas;
                // reduce area if necessary to avoid removing all slopes
                var minSlopeArea = Math.Min(minSlopeAreaBasin, CreateHRUs.maxValue(slopeAreas));
                foreach (var (slope, area) in slopeAreas) {
                    if (area < minSlopeArea) {
                        // remove this slope
                        // going to change maps so use lists
                        foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers.ToList()) {
                            foreach (var (soil, slopeNumbers) in soilSlopeNumbers.ToList()) {
                                // possible that slope has been removed
                                int hru;
                                if (!slopeNumbers.TryGetValue(slope, out hru)) {
                                    hru = -1;
                                }
                                if (hru != -1) {
                                    areaToRedistribute += basinData.hruMap[hru].area;
                                    basinData.removeHRU(hru, crop, soil, slope);
                                }
                            }
                        }
                    }
                }
                if (areaToRedistribute > 0) {
                    // Now redistribute removed slope areas
                    // just to make sure we don't divide by zero
                    if (basinData.cropSoilSlopeArea - areaToRedistribute == 0) {
                        throw new Exception(string.Format("Cannot redistribute {1:F2} ha for basin {0}", basin, areaToRedistribute / 10000));
                    }
                    var redistributeFactor = basinData.cropSoilSlopeArea / (basinData.cropSoilSlopeArea - areaToRedistribute);
                    basinData.redistribute(redistributeFactor);
                }
            }
        }

        // Return true if basindata has an exempt crop.
        public virtual bool hasExemptCrop(BasinData basinData) {
            foreach (var crop in basinData.cropSoilSlopeNumbers.Keys) {
                if (this._gv.isExempt(crop)) {
                    return true;
                }
            }
            return false;
        }

        // Return maximum value in map.
        public static double maxValue(Dictionary<int, double> mapv) {
            var maxm = 0.0;
            foreach (var val in mapv.Values) {
                if (val > maxm) {
                    maxm = val;
                }
            }
            return maxm;
        }

        // Try to reduce the number of HRUs to targetVal, 
        //         removing them in increasing order of size.
        //         
        //         Size is measured by area (if useArea is true) or by fraction
        //         of subbasin.
        //         The target may not be met if the order is by area and it would cause
        //         one or more subbasins to have no HRUs.
        //         The strategy is to make a list of all potential HRUs and their sizes 
        //         for which the landuses are not exempt, sort this list by increasing size, 
        //         and remove HRUs according to this list until the target is met.
        //         
        public virtual void removeSmallHRUsbyTarget() {
            // first make a list of (basin, crop, soil, slope, size) tuples
            var removals = new List<(int, int, int, int, int, double)>();
            foreach (var (basin, basinData) in this.basins) {
                var basinArea = basinData.cropSoilSlopeArea;
                foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers) {
                    if (!this._gv.isExempt(crop)) {
                        foreach (var (soil, slopeNumbers) in soilSlopeNumbers) {
                            foreach (var (slope, hru) in slopeNumbers) {
                                var hruArea = basinData.hruMap[hru].area;
                                var size = this.useArea ? hruArea : hruArea / basinArea;
                                removals.Add((basin, hru, crop, soil, slope, size));
                            }
                        }
                    }
                }
            }
            // sort by increasing size
            removals = removals.OrderBy(item => item.Item6).ToList();
            // remove HRUs
            // if some are exempt and target is small, can try to remove more than all in removals, so check for this
            var numToRemove = Math.Min(this.countFullHRUs() - this.targetVal, removals.Count);
            foreach (var i in Enumerable.Range(0, numToRemove)) {
                var nextItem = removals[i];
                var basinData = this.basins[nextItem.Item1];
                this.removeHru(nextItem.Item1, basinData, nextItem.Item2, nextItem.Item3, nextItem.Item4, nextItem.Item5);
            }
        }

        // Only used for TNC projects (forTNC is true).
        //         Impose maximum number of HRUs per subbasin (which is grid cell).
        //         
        public virtual void removeSmallHRUsbySubbasinTarget() {
            foreach (var (basin, basinData) in this.basins) {
                this.makeSubbasinTargetHRUs(basin, basinData);
            }
        }

        // Only used for TNC projects, which use grids.
        //         Impose maximum number of HRUs per subbasin (which is grid cell).
        //         The strategy is for each subbasin to make a list of all potential HRUs and their sizes 
        //         for which the landuses are not exempt, sort this list by increasing size, 
        //         and remove HRUs according to this list until the target is met.
        public virtual void makeSubbasinTargetHRUs(int basin, BasinData basinData) {
            var target = this.targetVal;
            var removals = new List<(int, int, int, int, double)>();
            var hruCount = basinData.hruMap.Count;
            // first make a list of (hru, crop, soil, slope, size) tuples
            foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers) {
                if (!this._gv.isExempt(crop)) {
                    foreach (var (soil, slopeNumbers) in soilSlopeNumbers) {
                        foreach (var (slope, hru) in slopeNumbers) {
                            var hruArea = basinData.hruMap[hru].area;
                            removals.Add((hru, crop, soil, slope, hruArea));
                        }
                    }
                }
            }
            // sort by increasing size
            removals = removals.OrderBy(item => item.Item5).ToList();
            // remove HRUs
            // if some are exempt and target is small, can try to remove more than all in removals, so check for this
            var numToRemove = Math.Min(hruCount - target, removals.Count);
            //print('Target is {0}; count is {1}; removing {2}', target, hruCount, numToRemove))
            foreach (var i in Enumerable.Range(0, numToRemove)) {
                var nextItem = removals[i];
                this.removeHru(basin, basinData, nextItem.Item1, nextItem.Item2, nextItem.Item3, nextItem.Item4);
            }
        }

        // Remove an HRU and redistribute its area within its subbasin.
        public virtual void removeHru(
            int basin,
            BasinData basinData,
            int hru,
            int crop,
            int soil,
            int slope) {
            if (basinData.hruMap.Count == 1) {
                // last HRU - do not remove
                return;
            }
            var areaToRedistribute = basinData.hruMap[hru].area;
            basinData.removeHRU(hru, crop, soil, slope);
            if (areaToRedistribute > 0) {
                // make sure we don't divide by zero
                if (basinData.cropSoilSlopeArea - areaToRedistribute == 0) {
                    throw new Exception(string.Format("No HRUs for basin {0}", basin));
                }
                var redistributeFactor = basinData.cropSoilSlopeArea / (basinData.cropSoilSlopeArea - areaToRedistribute);
                basinData.redistribute(redistributeFactor);
            }
        }

        // 
        //         Write basin data to Watershed, hrus and uncomb tables.  Also Write ElevationBand entry if Math.Max elevation above threshold.
        //         
        //         This is used when using grid model.  Makes at most self.targetVal HRUs in each grid cell.
        //
        //TODO
        //public virtual int writeWHUTables(
        //    int oid,
        //    int elevBandId,
        //    int SWATBasin,
        //    int basin,
        //    BasinData basinData,
        //    object cursor,
        //    string sql1,
        //    string sql2,
        //    string sql3,
        //    string sql4,
        //    object centroidll,
        //    List<int> mapp,
        //    int minElev,
        //    int maxElev) {
        //    var areaKm = (basinData.area / 1000000.0;
        //    var areaHa = areaKm * 100;
        //    var meanSlope = basinData.totalSlope / basinData.cellCount == 0 ? 1 : basinData.cellCount;
        //    var meanSlopePercent = meanSlope * 100;
        //    var farDistance = basinData.farDistance;
        //    var slsubbsn = Utils.getSlsubbsn(meanSlope);
        //    //Debug.Assert(farDistance > 0);
        //    //Debug.Assert(string.Format("Longest flow length is zero for basin {0}", SWATBasin));
        //    var farSlopePercent = basinData.farElevation - basinData.outletElevation / basinData.farDistance * 100;
        //    // formula from Srinivasan 11/01/06
        //    var tribChannelWidth = 1.29 * Math.Pow(areaKm, 0.6);
        //    var tribChannelDepth = 0.13 * Math.Pow(areaKm, 0.4);
        //    var lon = centroidll.x();
        //    var lat = centroidll.y();
        //    var catchmentId = this._gv.topo.catchmentOutlets.get(basin, -1);
        //    var SWATOutletBasin = this._gv.topo.basinToSWATBasin.get(catchmentId, 0);
        //    //Debug.Assert(basinData.cellCount > 0);
        //    //Debug.Assert(string.Format("Basin {0} has zero cell count", SWATBasin));
        //    var meanElevation = basinData.totalElevation / basinData.cellCount;
        //    var elevMin = basinData.outletElevation;
        //    var elevMax = basinData.maxElevation;
        //    cursor.execute(sql1, (SWATBasin, 0, SWATBasin, SWATBasin, float(areaHa), float(meanSlopePercent), float(farDistance), float(slsubbsn), float(farSlopePercent), float(tribChannelWidth), float(tribChannelDepth), float(lat), float(lon), float(meanElevation), float(elevMin), float(elevMax), "", 0, float(basinData.polyArea), float(basinData.definedArea), float(basinData.totalHRUAreas()), SWATBasin + 300000, SWATBasin + 100000, SWATOutletBasin));
        //    // TNC models use minimum threshold of 500 and fixed bandwidth of 500
        //    if (this._gv.forTNC && maxElev > 500) {
        //        var bandWidth = 500;
        //        this._gv.numElevBands = Convert.ToInt32(1 + (maxElev - minElev) / bandWidth);
        //        var thisWidth = Math.Min(maxElev + 1 - minElev, bandWidth);
        //        var midPoint = minElev + (thisWidth - 1) / 2.0;
        //        var bands = new List<object[]> {
        //            (minElev, midPoint, 0.0)
        //        };
        //        var nextBand = minElev + thisWidth;
        //        var totalFreq = mapp.Sum();
        //        foreach (var elev in Enumerable.Range(minElev, maxElev + 1 - minElev)) {
        //            var i = elev - this.minElev;
        //            var freq = mapp[i];
        //            var percent = freq / totalFreq * 100.0;
        //            if (elev >= nextBand) {
        //                // start a new band
        //                var nextWidth = Math.Min(maxElev + 1 - elev, bandWidth);
        //                var nextMidPoint = elev + (nextWidth - 1) / 2.0;
        //                bands.append((nextBand, nextMidPoint, percent));
        //                nextBand = Math.Min(nextBand + bandWidth, maxElev + 1);
        //            } else {
        //                (el, mid, frac) = bands[^1];
        //                bands[^1] = (el, mid, frac + percent);
        //            }
        //        }
        //        elevBandId += 1;
        //        var row = new List<int> {
        //            elevBandId,
        //            SWATBasin
        //        };
        //        foreach (var i in Enumerable.Range(0, 10)) {
        //            if (i < bands.Count) {
        //                row.append(bands[i][1]);
        //            } else {
        //                row.append(0);
        //            }
        //        }
        //        foreach (var i in Enumerable.Range(0, 10)) {
        //            if (i < bands.Count) {
        //                row.append(bands[i][2] / 100);
        //            } else {
        //                row.append(0);
        //            }
        //        }
        //        cursor.execute(sql4, tuple(row));
        //    }
        //=======================================================================
        // # original code for 1 HRU per grid cell
        // luNum = BasinData.dominantKey(basinData.originalCropAreas)
        // soilNum = BasinData.dominantKey(basinData.originalSoilAreas)
        // slpNum = BasinData.dominantKey(basinData.originalSlopeAreas)
        // lu = self._gv.db.getLanduseCode(luNum)
        // soil, _ = self._gv.db.getSoilName(soilNum)
        // slp = self._gv.db.slopeRange(slpNum)
        // meanSlopePercent = meanSlope * 100
        // uc = lu + '_' + soil + '_' + slp
        // filebase = Utils.fileBase(SWATBasin, 1)
        // oid += 1
        // cursor.execute(sql2, (oid, SWATBasin, float(areaHa), lu, float(areaHa), soil, float(areaHa), slp, \
        //                        float(areaHa), float(meanSlopePercent), uc, 1, filebase))
        // cursor.execute(sql3, (oid, SWATBasin, luNum, lu, soilNum, soil, slpNum, slp, \
        //                            float(meanSlopePercent), float(areaHa), uc))
        // return oid
        //=======================================================================
        // code for multiple HRUs
        // creates self.target HRUs plus a dummy (landuse DUMY) if forTNC
        //    this.makeSubbasinTargetHRUs(basin, basinData);
        //    if (this._gv.forTNC) {
        //        // Add dummy HRU for TNC projects, 0.1% of grid cell.
        //        // Easiest method is to use basinData.addCell for each cell: there will not be many. 
        //        // Before we add a dummy HRU of 0.1% we should reduce all the existing HRUs by that amount.
        //        var pointOnePercent = 0.001;
        //        basinData.redistribute(1 - pointOnePercent);
        //        // alse reduce basin area
        //        basinData.area *= 1 - pointOnePercent;
        //        var cellCount = Math.Max(1, Convert.ToInt32(basinData.cellCount * pointOnePercent + 0.5));
        //        var crop = this._gv.db.getLanduseCat("DUMY");
        //        var soil = BasinData.dominantKey(basinData.originalSoilAreas);
        //        var area = this._gv.cellArea;
        //        meanElevation = basinData.totalElevation / basinData.cellCount;
        //        var meanSlopeValue = basinData.totalSlope / basinData.cellCount;
        //        var slope = this._gv.db.slopeIndex(meanSlopeValue);
        //        foreach (var _ in Enumerable.Range(0, cellCount)) {
        //            basinData.addCell(crop, soil, slope, area, meanElevation, meanSlopeValue, this._gv.distNoData, this._gv);
        //        }
        //        // bring areas up to date (including adding DUMY crop).  Not original since we have already removed some HRUs.
        //        basinData.setAreas(false);
        //    }
        //    var relHRU = 0;
        //    foreach (var (crop, ssn) in basinData.cropSoilSlopeNumbers) {
        //        foreach (var (soil, sn) in ssn) {
        //            foreach (var (slope, hru) in sn) {
        //                var cellData = basinData.hruMap[hru];
        //                var lu = this._gv.db.getLanduseCode(crop);
        //                (soilName, _) = this._gv.db.getSoilName(soil);
        //                var slp = this._gv.db.slopeRange(slope);
        //                var hruha = float(cellData.area) / 10000;
        //                var arlu = float(basinData.cropArea(crop)) / 10000;
        //                var arso = float(basinData.cropSoilArea(crop, soil)) / 10000;
        //                var uc = lu + "_" + soilName + "_" + slp;
        //                var slopePercent = float(cellData.totalSlope) / cellData.cellCount * 100;
        //                var filebase = Utils.fileBase(SWATBasin, hru, forTNC: this._gv.forTNC);
        //                oid += 1;
        //                relHRU += 1;
        //                filebase = Utils.fileBase(SWATBasin, hru, forTNC: this._gv.forTNC);
        //                cursor.execute(sql2, (oid, SWATBasin, areaHa, lu, arlu, soilName, arso, slp, hruha, slopePercent, uc, oid, filebase));
        //                cursor.execute(sql3, (oid, SWATBasin, crop, lu, soil, soilName, slope, slp, slopePercent, hruha, uc));
        //            }
        //        }
        //    }
        //    return (oid, elevBandId);
        //}

        // Write subs.shp to TablesOut folder for visualisation.  Only used for big grids (isBig is True).
        //public virtual void writeGridSubsFile() {
        //    Utils.copyShapefile(this._gv.wshedFile, Parameters._SUBS, this._gv.tablesOutDir);
        //    var subsFile = Utils.join(this._gv.tablesOutDir, Parameters._SUBS + ".shp");
        //    var subsLayer = QgsVectorLayer(subsFile, "Watershed grid ({0})", Parameters._SUBS), "ogr");
        //    var provider = subsLayer.dataProvider();
        //    // remove fields apart from Subbasin
        //    var toDelete = new List<object>();
        //    var fields = provider.fields();
        //    foreach (var idx in Enumerable.Range(0, fields.count())) {
        //        var name = fields.field(idx).name();
        //        if (name != Topology._SUBBASIN) {
        //            toDelete.append(idx);
        //        }
        //    }
        //    if (toDelete) {
        //        provider.deleteAttributes(toDelete);
        //    }
        //    var OK = subsLayer.startEditing();
        //    if (!OK) {
        //        Utils.error(string.Format("Cannot start editing watershed shapefile {0}", subsFile), this._gv.isBatch);
        //        return;
        //    }
        //    // remove features with 0 subbasin value
        //    var exp = QgsExpression(string.Format("{0} = 0", Topology._SUBBASIN));
        //    var idsToDelete = new List<object>();
        //    foreach (var feature in subsLayer.getFeatures(QgsFeatureRequest(exp).setFlags(QgsFeatureRequest.NoGeometry))) {
        //        idsToDelete.append(feature.id());
        //    }
        //    OK = provider.deleteFeatures(idsToDelete);
        //    if (!OK) {
        //        Utils.error(string.Format("Cannot edit watershed shapefile {0}", subsFile), this._gv.isBatch);
        //        return;
        //    }
        //    OK = subsLayer.commitChanges();
        //    if (!OK) {
        //        Utils.error(string.Format("Cannot finish editing watershed shapefile {0}", subsFile), this._gv.isBatch);
        //        return;
        //    }
        //    var numDeleted = idsToDelete.Count;
        //    if (numDeleted > 0) {
        //        Utils.loginfo(string.Format("{0} subbasins removed from subs.shp", numDeleted));
        //    }
        //}

        // Split HRUs according to split landuses.
        public virtual bool splitHRUs() {
            Dictionary<int, int> oldsn;
            int newhru;
            Dictionary<int, int> newsn;
            foreach (var (landuse, split) in this._gv.splitLanduses) {
                var crop = this._gv.db.getLanduseCat(landuse);
                if (crop < 0) {
                    // error already reported
                    return false;
                }
                foreach (var basinData in this.basins.Values) {
                    var nextHruNo = basinData.relHru + 1;
                    if (basinData.cropSoilSlopeNumbers.ContainsKey(crop)) {
                        // have some hrus to split
                        var soilSlopeNumbers = basinData.cropSoilSlopeNumbers[crop];
                        // Make a new cropSoilSlopeNumbers map for the new crops
                        var newcssn = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
                        foreach (var lu in split.Keys) {
                            var newssn = new Dictionary<int, Dictionary<int, int>>();
                            var crop1 = this._gv.db.getLanduseCat(lu);
                            if (crop1 < 0) {
                                // error already reported
                                return false;
                            }
                            newcssn[crop1] = newssn;
                        }
                        foreach (var (soil, slopeNumbers) in soilSlopeNumbers) {
                            // add soils to new dictionary
                            foreach (var newssn in newcssn.Values) {
                                newsn = new Dictionary<int, int>();
                                newssn[soil] = newsn;
                            }
                            foreach (var (slope, hru) in slopeNumbers) {
                                var cd = basinData.hruMap[hru];
                                // remove hru from hruMap
                                basinData.hruMap.Remove(hru);
                                // first new hru can reuse removed hru number
                                var first = true;
                                foreach (var (sublu, percent) in split) {
                                    var subcrop = this._gv.db.getLanduseCat(sublu);
                                    var oldhru = -1;
                                    if (subcrop != crop && basinData.cropSoilSlopeNumbers.ContainsKey(subcrop)) {
                                        // add to an existing crop
                                        // if have HRU with same soil and slope, add to it
                                        var oldssn = basinData.cropSoilSlopeNumbers[subcrop];
                                        if (oldssn.ContainsKey(soil)) {
                                            if (oldssn[soil].ContainsKey(slope)) {
                                                oldhru = oldssn[soil][slope];
                                                var oldcd = basinData.hruMap[oldhru];
                                                var cd1 = new CellData(cd.cellCount, cd.area, cd.totalSlope, crop);
                                                cd1.multiply(percent / 100);
                                                oldcd.addCells(cd1);
                                            }
                                        }
                                        if (oldhru < 0) {
                                            // have to add new HRU to existing crop
                                            // keep original crop number in cell data
                                            var cd1 = new CellData(cd.cellCount, cd.area, cd.totalSlope, crop);
                                            cd1.multiply(percent / 100);
                                            if (first) {
                                                newhru = hru;
                                                first = false;
                                            } else {
                                                newhru = nextHruNo;
                                                basinData.relHru = newhru;
                                                nextHruNo += 1;
                                            }
                                            // add the new hru to hruMap
                                            basinData.hruMap[newhru] = cd1;
                                            // add hru to existing data for this crop
                                            if (oldssn.ContainsKey(soil)) {
                                                oldsn = oldssn[soil];
                                            } else {
                                                oldsn = new Dictionary<int, int>();
                                                oldssn[soil] = oldsn;
                                            }
                                            oldsn[slope] = newhru;
                                        }
                                    } else {
                                        // the subcrop is new to the basin
                                        // keep original crop number in cell data
                                        var cd1 = new CellData(cd.cellCount, cd.area, cd.totalSlope, crop);
                                        cd1.multiply(percent / 100);
                                        if (first) {
                                            newhru = hru;
                                            first = false;
                                        } else {
                                            newhru = nextHruNo;
                                            basinData.relHru = newhru;
                                            nextHruNo += 1;
                                        }
                                        // add the new hru to hruMap
                                        basinData.hruMap[newhru] = cd1;
                                        // add slope and hru number to new dictionary
                                        var newssn = newcssn[subcrop];
                                        newsn = newssn[soil];
                                        newsn[slope] = newhru;
                                    }
                                }
                            }
                        }
                        // remove crop from cropSoilSlopeNumbers
                        basinData.cropSoilSlopeNumbers.Remove(crop);
                        // add new cropSoilSlopeNumbers to original
                        foreach (var (newcrop, newssn) in newcssn) {
                            // existing subcrops already dealt with
                            if (!basinData.cropSoilSlopeNumbers.ContainsKey(newcrop)) {
                                basinData.cropSoilSlopeNumbers[newcrop] = newssn;
                            }
                        }
                    }
                }
            }
            return true;
        }

        // Write topographic report file.
        public virtual void writeTopoReport() {
            var topoPath = Utils.join(this._gv.textDir, Parameters._TOPOREPORT);
            var line = "------------------------------------------------------------------------------";
            using (var fw = new StreamWriter(topoPath, false)) {
                fw.WriteLine("");
                fw.WriteLine(line);
                fw.WriteLine(Utils.trans("Elevation report for the watershed".PadRight(40) + Utils.date() + " " + Utils.time()));
                fw.WriteLine("");
                fw.WriteLine(line);
                this.writeTopoReportSection(this.elevMap, fw, "watershed");
                fw.WriteLine(line);
                if (!this._gv.useGridModel) {
                    foreach (var i in Enumerable.Range(0, this._gv.topo.SWATBasinToBasin.Count)) {
                        // i will range from 0 to n-1, SWATBasin from 1 to n
                        var SWATBasin = i + 1;
                        fw.WriteLine(string.Format("Subbasin {0}", SWATBasin));
                        var basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
                        List<int> mapp;
                        bool ok = this.basinElevMap.TryGetValue(basin, out mapp);
                        List<(double, double, double)> bands;
                        if (ok) {
                            try {
                                bands = this.writeTopoReportSection(mapp, fw, "subbasin");
                            } catch (Exception) {
                                Utils.exceptionError(string.Format("Internal error: cannot Write topo report for SWAT basin {0} (basin {1})", SWATBasin, basin), this._gv.isBatch);
                                bands = null;
                            }
                        } else {
                            bands = null;
                        }
                        fw.WriteLine(line);
                        this.basinElevBands[SWATBasin] = bands;
                    }
                }
            }
            this._reportsCombo.Visible = true;
            if (this._reportsCombo.FindString(Parameters._TOPOITEM) < 0) {
                this._reportsCombo.Items.Add(Parameters._TOPOITEM);
            }
            this._gv.db.writeElevationBands(this.basinElevBands);
        }

        // Write topographic report file section for 1 subbasin.
        //         
        //         Returns list of (start, midpoint, percent of subbasin at start)
        public virtual List<(double, double, double)> writeTopoReportSection(List<int> mapp, StreamWriter fw, string @string) {
            double nextBand = 0;
            List<(double, double, double)> bands;
            int bandWidth = 0;
            fw.WriteLine("");
            fw.WriteLine(Utils.trans("Statistics: All elevations reported in meters"));
            fw.WriteLine("-----------");
            fw.WriteLine("");
            int minimum, maximum, totalFreq;
            double mean, stdDev;
            (minimum, maximum, totalFreq, mean, stdDev) = this.analyseElevMap(mapp);
            fw.WriteLine(Utils.trans("Minimum elevation: ").PadLeft(21) + (minimum + this.minElev).ToString());
            fw.WriteLine(Utils.trans("Maximum elevation: ").PadLeft(21) + (maximum + this.minElev).ToString());
            fw.WriteLine(Utils.trans("Mean elevation: ").PadLeft(21) + string.Format("{0:F2}", mean));
            fw.WriteLine(Utils.trans("Standard deviation: ").PadLeft(21) + string.Format("{0:F2}", stdDev));
            fw.WriteLine("");
            fw.Write(Utils.trans("Elevation").PadLeft(23));
            fw.Write(Utils.trans("% area up to elevation").PadLeft(32));
            fw.Write(Utils.trans("% area of ").PadLeft(14) + @string);
            fw.WriteLine("");
            var summ = 0.0;
            if (@string == "subbasin" && this._gv.elevBandsThreshold > 0 && this._gv.numElevBands > 0 && maximum + this.minElev > this._gv.elevBandsThreshold) {
                if (this._gv.isHUC || this._gv.isHAWQS) {
                    // HUC models use fixed bandwidth of 500m
                    bandWidth = 500;
                    this._gv.numElevBands = Convert.ToInt32(1 + (maximum - minimum) / bandWidth);
                    var thisWidth = Math.Min(maximum + 1 - minimum, bandWidth);
                    var midPoint = minimum + this.minElev + (thisWidth - 1) / 2.0;
                    bands = new List<(double, double, double)> {
                        (minimum + this.minElev, midPoint, 0.0)
                    };
                    nextBand = minimum + this.minElev + thisWidth;
                } else {
                    bandWidth = (maximum + 1 - minimum) / this._gv.numElevBands;
                    bands = new List<(double, double, double)> {
                        (minimum + this.minElev, minimum + this.minElev + (bandWidth - 1) / 2.0, 0.0)
                    };
                    nextBand = minimum + this.minElev + bandWidth;
                }
            } else {
                bands = null;
            }
            foreach (var i in Enumerable.Range(minimum, maximum + 1 - minimum)) {
                var freq = mapp[i];
                summ += freq;
                var elev = i + this.minElev;
                var upto = summ / totalFreq * 100.0;
                var percent = freq / totalFreq * 100.0;
                if (bands is not null) {
                    if (elev >= nextBand) {
                        // start a new band
                        var nextWidth = Math.Min(maximum + 1 + this.minElev - elev, bandWidth);
                        var nextMidPoint = elev + (nextWidth - 1) / 2.0;
                        bands.Add((nextBand, nextMidPoint, percent));
                        nextBand = Math.Min(nextBand + bandWidth, maximum + 1 + this.minElev);
                    } else {
                        double el, mid, frac;
                        (el, mid, frac) = bands[^1];
                        bands[^1] = (el, mid, frac + percent);
                    }
                }
                fw.Write(elev.ToString().PadLeft(20));
                fw.Write(string.Format("{0:F2}", upto).PadLeft(25));
                fw.WriteLine(string.Format("{0:F2}", percent).PadLeft(25));
            }
            fw.WriteLine("");
            return bands;
        }

        // Calculate statistics from map elevation -> frequency.
        public virtual (int, int, int, double, double) analyseElevMap(List<int> mapp) {
            if (mapp.Count == 0) {
                return (0, 0, 0, 0, 0);
            }
            // find index of first non-zero frequency
            var minimum = 0;
            while (minimum < mapp.Count && mapp[minimum] == 0) {
                minimum += 1;
            }
            // find index of last non-zero frequency
            var maximum = mapp.Count - 1;
            while (maximum >= 0 && mapp[maximum] == 0) {
                maximum -= 1;
            }
            // calculate mean elevation and total of frequencies
            var summ = 0.0;
            var totalFreq = 0;
            foreach (var i in Enumerable.Range(minimum, maximum + 1 - minimum)) {
                // if mapp is all zeros, we get range(len(mapp), 1) which is empty since len(mapp) > 0 after initial check
                var freq = mapp[i];
                summ += i * freq;
                totalFreq += freq;
            }
            // just to avoid dvision by zero
            if (totalFreq == 0) {
                return (minimum, maximum, 0, 0, 0);
            }
            var mapMean = summ / totalFreq;
            var mean = mapMean + this.minElev;
            var variance = 0.0;
            foreach (var i in Enumerable.Range(minimum, maximum + 1 - minimum)) {
                var diff = i - mapMean;
                variance += diff * diff * mapp[i];
            }
            var stdDev = Math.Sqrt(variance / totalFreq);
            return (minimum, maximum, totalFreq, mean, stdDev);
        }

        // 
        //         Print report on crops, soils, and slopes for watershed.
        //         
        //         Also writes hrus and uncomb tables if withHRUs.
        //         
        public void printBasins(bool withHRUs) {
            var fileName = withHRUs ? Parameters._HRUSREPORT : Parameters._BASINREPORT;
            var path = Utils.join(this._gv.textDir, fileName);
            var hrusCsvFile = Utils.join(this._gv.gridDir, Parameters._HRUSCSV);
            string horizLine;
            string line, line1, line2, method, units, st1, st2;
            using (StreamWriter fw = new StreamWriter(path, false))
            using (StreamWriter hrusCsv = new StreamWriter(hrusCsvFile, false)) {

                // Print report on crops, soils, and slopes for subbasin.
                void printBasinsDetails(
                    double basinHa,
                    bool withHRUs) {

                    // Print HRUs for a subbasin.
                    int printbasinHRUs(
                       int basin,
                       BasinData basinData,
                       double wshedArea,
                       double subArea,
                       int oid) {
                        foreach (var (hru, hrudata) in this.hrus) {
                            if (hrudata.basin == basin) {
                                // ignore basins not mapping to SWAT basin (empty, or edge when using grid model)
                                if (this._gv.topo.basinToSWATBasin.ContainsKey(basin)) {
                                    var lu = this._gv.db.getLanduseCode(hrudata.crop);
                                    bool ok;
                                    string soil = this._gv.db.getSoilName(hrudata.soil, out ok);
                                    var slp = this._gv.db.slopeRange(hrudata.slope);
                                    var cropSoilSlope = lu + "/" + soil + "/" + slp;
                                    var meanSlopePercent = hrudata.meanSlope * 100;
                                    var hruha = hrudata.area / 10000;
                                    var arlu = this.isMultiple ? basinData.cropAreas[hrudata.crop] / 10000 : hruha;
                                    var arso = this.isMultiple ? basinData.cropSoilArea(hrudata.crop, hrudata.soil, this._gv.isBatch) / 10000 : hruha;
                                    var arslp = hruha;
                                    var uc = lu + "_" + soil + "_" + slp;
                                    var SWATBasin = this._gv.topo.basinToSWATBasin[basin];
                                    fw.Write(hru.ToString().PadRight(5) + cropSoilSlope.PadLeft(25) + string.Format("{0:F2}", hruha).PadLeft(15));
                                    if (wshedArea > 0) {
                                        var percent1 = hruha / wshedArea * 100;
                                        fw.Write(string.Format("{0:F2}", percent1).PadLeft(30));
                                    }
                                    if (subArea > 0) {
                                        var percent2 = hruha / subArea * 100;
                                        fw.Write(string.Format("{0:F2}", percent2).PadLeft(23));
                                    }
                                    var hrusArea = subArea - (basinData.reservoirArea + basinData.pondArea + basinData.lakeArea) / 10000.0;
                                    if (this._gv.isHUC || this._gv.isHAWQS) {
                                        // allow for extra 1 ha WATR hru inserted if all water body
                                        hrusArea = Math.Max(1, hrusArea);
                                    }
                                    fw.WriteLine("");
                                    var filebase = Utils.fileBase(SWATBasin, hrudata.relHru, forTNC: this._gv.forTNC);
                                    oid += 1;
                                    var table = "hrus";
                                    var sql = "INSERT INTO " + table + " VALUES";
                                    string inserts = "(" +
                                        oid.ToString() + "," +
                                        SWATBasin.ToString() + "," +
                                        hrusArea.ToString() + "," +
                                        DBUtils.quote(lu) + "," +
                                        arlu.ToString() + "," +
                                        DBUtils.quote(soil) + "," +
                                        arso.ToString() + "," +
                                        DBUtils.quote(slp) + "," +
                                        arslp.ToString() + "," +
                                        meanSlopePercent.ToString() + "," +
                                        DBUtils.quote(uc) + "," +
                                        hru.ToString() + "," +
                                        DBUtils.quote(filebase) + ");";
                                    this._gv.db.execNonQuery(sql + inserts);
                                    table = "uncomb";
                                    sql = "INSERT INTO " + table + " VALUES";
                                    inserts = "(" +
                                        oid.ToString() + "," +
                                        SWATBasin.ToString() + "," +
                                        hrudata.crop.ToString() + "," +
                                        DBUtils.quote(lu) + "," +
                                        hrudata.soil.ToString() + "," +
                                        DBUtils.quote(soil) + "," +
                                        hrudata.slope.ToString() + "," +
                                        DBUtils.quote(slp) + "," +
                                        meanSlopePercent.ToString() + "," +
                                        hruha.ToString() + "," +
                                        DBUtils.quote(uc) + ");";
                                    this._gv.db.execNonQuery(sql + inserts);
                                    if (this._gv.isHUC || this._gv.isHAWQS) {
                                        table = "hru";
                                        sql = "INSERT INTO " + table + " (OID, SUBBASIN, HRU, LANDUSE, SOIL, SLOPE_CD, HRU_FR, SLSUBBSN, HRU_SLP, OV_N, POT_FR) VALUES(?,?,?,?,?,?,?,?,?,?,?);";

                                        // assume that if, say, 10% of subbasin is pothole (playa) then 10% of each HRU drains into it.
                                        var pot_fr = basinData.playaArea / basinData.area;
                                        double ovn;
                                        if (!this._gv.db.landuseOVN.TryGetValue(hrudata.crop, out ovn)) {
                                            ovn = 0;
                                        }
                                        inserts = "(" +
                                            oid.ToString() + "," +
                                            SWATBasin.ToString() + "," +
                                            hru.ToString() + "," +
                                            DBUtils.quote(lu) + "," +
                                            DBUtils.quote(soil) + "," +
                                            DBUtils.quote(slp) + "," +
                                            (hrudata.area / basinData.area).ToString() + "," +
                                            Utils.getSlsubbsn(hrudata.meanSlope).ToString() + "," +
                                            hrudata.meanSlope.ToString() + "," +
                                            ovn.ToString() + "," +
                                            pot_fr.ToString() + ");";
                                        this._gv.db.execNonQuery(sql + inserts);
                                    }
                                    hrusCsv.WriteLine(string.Format("{0},{1}", hru, hruha));
                                }
                            }
                        }
                        return oid;
                    }


                    double bPercent;
                    double wPercent;
                    int SWATBasin;
                    int basin;
                    IEnumerable<int> iterator;
                    //bool OK;
                    OSGeo.OGR.DataSource fullHRUsDs = null;
                    OSGeo.OGR.Layer fullHRUsLayer = null;
                    if (withHRUs && this.fullHRUsWanted) {
                        if (File.Exists(this._gv.fullHRUsFile)) { 
                            fullHRUsDs = Ogr.Open(this._gv.fullHRUsFile, 1);
                            fullHRUsLayer = fullHRUsDs.GetLayerByIndex(0);
                        } 
                    }
                    var setHRUGIS = withHRUs && fullHRUsLayer is not null;
                    int subIndx = 0;
                    int luseIndx = 0;
                    int soilIndx = 0;
                    int slopeIndx = 0;
                    int hrugisIndx = 0;
                    if (setHRUGIS) {
                        // use OGR as changing fields with ArcGIS inspector always fails
                        subIndx = fullHRUsLayer.FindFieldIndex(Topology._SUBBASIN, 1);
                        if (subIndx < 0) {
                            setHRUGIS = false;
                        }
                        luseIndx = fullHRUsLayer.FindFieldIndex(Parameters._LANDUSE, 1);
                        if (luseIndx < 0) {
                            setHRUGIS = false;
                        }
                        soilIndx = fullHRUsLayer.FindFieldIndex(Parameters._SOIL, 1);
                        if (soilIndx < 0) {
                            setHRUGIS = false;
                        }
                        slopeIndx = fullHRUsLayer.FindFieldIndex(Parameters._SLOPEBAND, 1);
                        if (slopeIndx < 0) {
                            setHRUGIS = false;
                        }
                        hrugisIndx = fullHRUsLayer.FindFieldIndex(Topology._HRUGIS, 1);
                        if (hrugisIndx < 0) {
                            setHRUGIS = false;
                        }
                        if (setHRUGIS) {
                            //Debug.Assert(fullHRUsLayer is not null);
                            //OK = fullHRUsLayer.startEditing();
                            //if (!OK) {
                            //    Utils.error("Cannot edit FullHRUs shapefile", this._gv.isBatch);
                            //    setHRUGIS = false;
                            //}
                        }
                        // set HRUGIS field for all shapes for this basin to NA
                        // (in case rerun with different HRU settings)
                        if (setHRUGIS) {
                            if (fullHRUsLayer.GetFeatureCount(1) == 0) {
                                Utils.error("Full HRUs layer is empty", this._gv.isBatch);
                                setHRUGIS = false;
                                //Debug.Assert(fullHRUsLayer is not null);
                            } else {
                                this.clearHRUGISNums(fullHRUsLayer, hrugisIndx);
                            }
                        }
                    }
                    var oid = 0;
                    if (this._gv.useGridModel) {
                        iterator = this.basins.Keys;
                    } else {
                        iterator = Enumerable.Range(0, this._gv.topo.SWATBasinToBasin.Count);
                    }
                    foreach (var i in iterator) {
                        if (this._gv.useGridModel) {
                            basin = i;
                            SWATBasin = this._gv.topo.basinToSWATBasin[basin];
                        } else {
                            // i will range from 0 to n-1, SWATBasin from 1 to n
                            SWATBasin = i + 1;
                            basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
                        }
                        BasinData basinData;
                        bool ok = this.basins.TryGetValue(basin, out basinData);
                        if (!ok) {
                            Utils.error(string.Format("No data for SWATBasin {0} (polygon {1})", SWATBasin, basin), this._gv.isBatch);
                            return;
                        }
                        var subHa = basinData.area / 10000;
                        var percent = subHa / basinHa * 100;
                        var st1 = "Area [ha]";
                        var st2 = "%Watershed";
                        var st3 = "%Subbasin";
                        var col2just = withHRUs ? 33 : 18;
                        var col3just = withHRUs ? 23 : 15;
                        fw.WriteLine(st1.PadLeft(45) + st2.PadLeft(col2just) + st3.PadLeft(col3just));
                        fw.WriteLine("");
                        fw.WriteLine(string.Format("Subbasin {0}", SWATBasin).PadRight(30) + string.Format("{0:F2}", subHa).PadLeft(15) + string.Format("{0:F2}", percent).PadLeft(col2just - 3));
                        fw.WriteLine("");
                        fw.WriteLine("Landuse");
                        this.printCropAreas(basinData.cropAreas, basinData.originalCropAreas, basinHa, subHa, fw);
                        fw.WriteLine("");
                        fw.WriteLine("Soil");
                        this.printSoilAreas(basinData.soilAreas, basinData.originalSoilAreas, basinHa, subHa, fw);
                        fw.WriteLine("");
                        fw.WriteLine("Slope");
                        this.printSlopeAreas(basinData.slopeAreas, basinData.originalSlopeAreas, basinHa, subHa, fw);
                        fw.WriteLine("");
                        if (basinData.reservoirArea > 0) {
                            var resHa = basinData.reservoirArea / 10000.0;
                            wPercent = resHa / basinHa * 100;
                            bPercent = resHa / subHa * 100;
                            fw.WriteLine("Reservoir".PadRight(30) + string.Format("{0:F2}", resHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3) + string.Format("{0:F2}", bPercent).PadLeft(col3just));
                            fw.WriteLine("");
                        }
                        if (basinData.pondArea > 0) {
                            var pndHa = basinData.pondArea / 10000.0;
                            wPercent = pndHa / basinHa * 100;
                            bPercent = pndHa / subHa * 100;
                            fw.WriteLine("Pond".PadRight(30) + string.Format("{0:F2}", pndHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3) + string.Format("{0:F2}", bPercent).PadLeft(col3just));
                            fw.WriteLine("");
                        }
                        if (basinData.lakeArea > 0) {
                            var lakeHa = basinData.lakeArea / 10000.0;
                            wPercent = lakeHa / basinHa * 100;
                            bPercent = lakeHa / subHa * 100;
                            fw.WriteLine("Lake".PadRight(30) + string.Format("{0:F2}", lakeHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3) + string.Format("{0:F2}", bPercent).PadLeft(col3just));
                            fw.WriteLine("");
                        }
                        if (basinData.playaArea > 0) {
                            var playaHa = basinData.playaArea / 10000.0;
                            wPercent = playaHa / basinHa * 100;
                            bPercent = playaHa / subHa * 100;
                            fw.WriteLine("Playa".PadRight(30) + string.Format("{0:F2}", playaHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3) + string.Format("{0:F2}", bPercent).PadLeft(col3just));
                            fw.WriteLine("");
                        }
                        if (withHRUs) {
                            if (this.isMultiple) {
                                fw.WriteLine("HRUs:");
                            } else {
                                fw.WriteLine("HRU:");
                            }
                            oid = printbasinHRUs(basin, basinData, basinHa, subHa, oid);
                            if (setHRUGIS) {
                                //Debug.Assert(fullHRUsLayer is not null);
                                this.addHRUGISNums(basin, fullHRUsLayer, subIndx, luseIndx, soilIndx, slopeIndx, hrugisIndx);
                            }
                        }
                        fw.WriteLine(horizLine);
                    }
                    if (setHRUGIS) {
                        fullHRUsLayer.SyncToDisk();
                        fullHRUsDs.FlushCache();
                        //Debug.Assert(fullHRUsLayer is not null);
                        //OK = fullHRUsLayer.commitChanges();
                        //if (!OK) {
                        //    Utils.error("Cannot commit changes to FullHRUs shapefile", this._gv.isBatch);
                        //}
                        this.writeActHRUs(fullHRUsLayer, hrugisIndx);
                    }
                }


                if (withHRUs) {
                    horizLine = "---------------------------------------------------------------------------------------------------------";
                    fw.WriteLine("Landuse/Soil/Slope and HRU Distribution".PadRight(47) + Utils.date() + " " + Utils.time());
                    hrusCsv.WriteLine("hru, area_ha");
                } else {
                    horizLine = "---------------------------------------------------------------------------";
                    fw.WriteLine("Landuse/Soil/Slope Distribution".PadRight(47) + Utils.date() + " " + Utils.time());
                }
                fw.WriteLine("");
                if (withHRUs) {
                    if (this.isDominantHRU) {
                        fw.WriteLine("Dominant HRU option");
                        if (this._gv.isBatch) {
                            Utils.information("Dominant HRU option", true);
                        }
                        fw.WriteLine(string.Format("Number of HRUs: {0}", this.basins.Count));
                    } else if (!this.isMultiple) {
                        fw.WriteLine("Dominant Landuse/Soil/Slope option");
                        if (this._gv.isBatch) {
                            Utils.information("Dominant Landuse/Soil/Slope option", true);
                        }
                        fw.WriteLine(string.Format("Number of HRUs: {0}", this.basins.Count));
                    } else {
                        // multiple
                        if (this._gv.forTNC) {
                            line = "Using target number of HRUs per grid cell".PadRight(47) + string.Format("Target {0}", this.targetVal);
                            fw.WriteLine(line);
                            if (this._gv.isBatch) {
                                Utils.information(line, true);
                            }
                        } else {
                            if (this.useArea) {
                                method = "Using area in hectares";
                                units = "ha";
                            } else {
                                method = "Using percentage of subbasin";
                                units = "%";
                            }
                            if (this.isTarget) {
                                line1 = method + " as a measure of size";
                                line2 = "Target number of HRUs option".PadRight(47) + string.Format("Target {0}", this.targetVal);
                            } else if (this.isArea) {
                                line1 = method + " as threshold";
                                line2 = "Multiple HRUs Area option".PadRight(47) + string.Format("Threshold: {0:d} {1}", this.areaVal, units);
                            } else {
                                line1 = method + " as a threshold";
                                line2 = "Multiple HRUs Landuse/Soil/Slope option".PadRight(47) + string.Format("Thresholds: {0:d}/{1:d}/{2:d} [{3}]", this.landuseVal, this.soilVal, this.slopeVal, units);
                            }
                            fw.WriteLine(line1);
                            if (this._gv.isBatch) {
                                Utils.information(line1, true);
                            }
                            fw.WriteLine(line2);
                            if (this._gv.isBatch) {
                                Utils.information(line2, true);
                            }
                        }
                        fw.WriteLine(string.Format("Number of HRUs: {0}", this.hrus.Count));
                    }
                }
                fw.WriteLine(string.Format("Number of subbasins: {0}", this._gv.topo.basinToSWATBasin.Count));
                if (withHRUs && this.isMultiple) {
                    // don't report DUMY when forTNC (and there will not be others)
                    if (!this._gv.forTNC && this._gv.exemptLanduses.Count > 0) {
                        fw.Write("Landuses exempt from thresholds: ");
                        foreach (var landuse in this._gv.exemptLanduses) {
                            fw.Write(landuse.PadLeft(6));
                        }
                        fw.WriteLine("");
                    }
                    if (this._gv.splitLanduses.Count > 0) {
                        fw.WriteLine("Split landuses: ");
                        foreach (var (landuse, splits) in this._gv.splitLanduses) {
                            fw.Write(landuse.PadLeft(6));
                            fw.Write(" split into ");
                            foreach (var (use, percent) in splits) {
                                fw.Write(string.Format("{0} : {1}%  ", use, percent));
                            }
                            fw.WriteLine("");
                        }
                    }
                }
                if (withHRUs) {
                    fw.WriteLine("");
                    fw.WriteLine("Numbers in parentheses are corresponding values before HRU creation");
                }
                fw.WriteLine("");
                fw.WriteLine(horizLine);
                st1 = "Area [ha]";
                st2 = "%Watershed";
                int col2just = withHRUs ? 33 : 18;
                fw.WriteLine(st1.PadLeft(45));
                double basinHa = this.totalBasinsArea() / 10000;
                fw.WriteLine("Watershed" + string.Format("{0:F2}", basinHa).PadLeft(36));
                fw.WriteLine(horizLine);
                fw.WriteLine(st1.PadLeft(45) + st2.PadLeft(col2just));
                fw.WriteLine("");
                fw.WriteLine("Landuse");
                Dictionary<int, double> cropAreas, originalCropAreas, soilAreas, originalSoilAreas, slopeAreas, originalSlopeAreas;
                (cropAreas, originalCropAreas) = this.totalCropAreas(withHRUs);
                this.printCropAreas(cropAreas, originalCropAreas, basinHa, 0, fw);
                fw.WriteLine("");
                fw.WriteLine("Soil");
                (soilAreas, originalSoilAreas) = this.totalSoilAreas(withHRUs);
                this.printSoilAreas(soilAreas, originalSoilAreas, basinHa, 0, fw);
                fw.WriteLine("");
                fw.WriteLine("Slope");
                (slopeAreas, originalSlopeAreas) = this.totalSlopeAreas(withHRUs);
                this.printSlopeAreas(slopeAreas, originalSlopeAreas, basinHa, 0, fw);
                if (this._gv.isHUC || this._gv.isHAWQS) {
                    double totalReservoirsArea = this.totalReservoirsArea();
                    double totalPondsArea = this.totalPondsArea();
                    double totalLakesArea = this.totalLakesArea();
                    double totalPlayasArea = this.totalPlayasArea();
                    if (totalReservoirsArea > 0) {
                        fw.WriteLine("");
                        double resHa = totalReservoirsArea / 10000.0;
                        double wPercent = resHa / basinHa * 100;
                        fw.WriteLine("Reservoirs".PadRight(30) + string.Format("{0:F2}", resHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3));
                    }
                    if (totalPondsArea > 0) {
                        fw.WriteLine("");
                        double pndHa = totalPondsArea / 10000.0;
                        double wPercent = pndHa / basinHa * 100;
                        fw.WriteLine("Ponds".PadRight(30) + string.Format("{0:F2}", pndHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3));
                    }
                    if (totalLakesArea > 0) {
                        fw.WriteLine("");
                        double lakeHa = totalLakesArea / 10000.0;
                        double wPercent = lakeHa / basinHa * 100;
                        fw.WriteLine("Lakes".PadRight(30) + string.Format("{0:F2}", lakeHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3));
                    }
                    if (totalPlayasArea > 0) {
                        fw.WriteLine("");
                        double playaHa = totalPlayasArea / 10000.0;
                        double wPercent = playaHa / basinHa * 100;
                        fw.WriteLine("Lakes".PadRight(30) + string.Format("{0:F2}", playaHa).PadLeft(15) + string.Format("{0:F2}", wPercent).PadLeft(col2just - 3));
                    }
                }
                fw.WriteLine(horizLine);
                fw.WriteLine(horizLine);
                if (withHRUs) {
                    if (this._gv.isHUC || this._gv.isHAWQS) {
                        var sql0 = "DROP TABLE IF EXISTS hrus";
                        this._gv.db.execNonQuery(sql0);
                        var sql0a = "DROP TABLE IF EXISTS uncomb";
                        this._gv.db.execNonQuery(sql0a);
                        var sql0b = "DROP TABLE IF EXISTS hru";
                        this._gv.db.execNonQuery(sql0b);
                        var sql1 = DBUtils._HRUSCREATESQL;
                        this._gv.db.execNonQuery(sql1);
                        var sql1a = DBUtils._UNCOMBCREATESQL;
                        this._gv.db.execNonQuery(sql1a);
                        var sql1b = DBUtils._HRUCREATESQL;
                        this._gv.db.execNonQuery(sql1b);
                    } else {
                        var table = "hrus";
                        var clearSQL = "DELETE FROM " + table;
                        this._gv.db.execNonQuery(clearSQL);
                        table = "uncomb";
                        clearSQL = "DELETE FROM " + table;
                        this._gv.db.execNonQuery(clearSQL);
                    }
                    printBasinsDetails(basinHa, true);
                    //if (this._gv.isHUC || this._gv.isHAWQS) {
                    //    conn.commit();
                    //} else {
                    //    this._gv.db.hashDbTable(conn, "hrus");
                    //    this._gv.db.hashDbTable(conn, "uncomb");
                    //}
                } else {
                    printBasinsDetails(basinHa, false);
                }
                this._reportsCombo.Visible = true;
                if (withHRUs) {
                    if (this._reportsCombo.FindString(Parameters._HRUSITEM) < 0) {
                        this._reportsCombo.Items.Add(Parameters._HRUSITEM);
                    }
                } else if (this._reportsCombo.FindString(Parameters._BASINITEM) < 0) {
                    this._reportsCombo.Items.Add(Parameters._BASINITEM);
                }
            }
        }

        //TODO
        //// Write water statistics for HUC and HAWQS projects.  Write bodyStats file and return stats data so WATR reduction stats can be added.
        //public virtual object writeWaterStats1() {
        //    object statsName;
        //    //         NHDWaterFile = Utils.join(self._gv.HUCDataDir, 'NHDPlusNationalData/NHDWaterBody5072.sqlite')
        //    //         NHDWaterConn = sqlite3.connect(NHDWaterFile)
        //    //         NHDWaterConn.enable_load_extension(True)
        //    //         NHDWaterConn.execute("SELECT load_extension('mod_spatialite')")
        //    //         sql = """SELECT AsText(GEOMETRY), ftype FROM nhdwaterbody5072
        //    //                     WHERE nhdwaterbody5072.ROWID IN (SELECT ROWID FROM SpatialIndex WHERE
        //    //                         ((f_table_name = 'nhdwaterbody5072') AND (search_frame = GeomFromText(?))));"""
        //    var wshedFile = this._gv.wshedFile;
        //    var wshedLayer = QgsVectorLayer(wshedFile, "watershed", "ogr");
        //    var basinIndex = this._gv.topo.getIndex(wshedLayer, Topology._POLYGONID);
        //    if (this._gv.isHUC) {
        //        var huc = this._gv.projName[3];
        //        var l = huc.Count + 2;
        //        var region = huc[:2:];
        //        statsName = "waterbodyStats{0}_HUC{1}.csv", region, l);
        //        var hucField = "HUC{0}", l);
        //        var hucIndex = this._gv.topo.getIndex(wshedLayer, hucField);
        //    } else {
        //        // HAWQS
        //        statsName = "waterbodyStats.csv";
        //        (hucIndex, _) = Topology.getHUCIndex(wshedLayer);
        //    }
        //    var waterStats = new dict();
        //    var bodyStatsFile = Utils.join(this._gv.projDir, statsName);
        //    using (var bodyStats = open(bodyStatsFile, "w")) {
        //        bodyStats.Write("HUC, Area, Reservoir, Pond, Lake, Playa, Percent\n");
        //        foreach (var basinShape in wshedLayer.getFeatures()) {
        //            basin = basinShape[basinIndex];
        //            basinHUC = basinShape[hucIndex];
        //            basinData = this.basins.get(basin, null);
        //            if (basinData is null) {
        //                Utils.error(string.Format("Polygon {0} in watershed file {1} has no basin data", basin, wshedFile), this._gv.isBatch);
        //                continue;
        //            }
        //            areaHa = basinData.area / 10000.0;
        //            if (areaHa == 0) {
        //                continue;
        //            }
        //            reservoirHa = basinData.reservoirArea / 10000.0;
        //            if (basinData.reservoirArea > 0) {
        //                Utils.loginfo(string.Format("Reservoir area for basin {0} is {1}", basin, basinData.reservoirArea));
        //            }
        //            pondHa = basinData.pondArea / 10000.0;
        //            lakeHa = basinData.lakeArea / 10000.0;
        //            swampMarshHa = basinData.wetlandArea / 10000.0;
        //            playaHa = basinData.playaArea / 10000.0;
        //            percent = (reservoirHa + pondHa + lakeHa + playaHa) * 100 / areaHa;
        //            bodyStats.Write(string.Format("'{0}', {1:F2}, {2:F2}, {3:F2}, {4:F2}, {5:F2}, {6:F2}\n", basinHUC, areaHa, reservoirHa, pondHa, lakeHa, playaHa, percent));
        //            // edge inaccuracies can cause WATRInStreamArea > streamArea
        //            WATRInStreamHa = Math.Min(basinData.WATRInStreamArea, basinData.streamArea) / 10000.0;
        //            streamAreaHa = basinData.streamArea / 10000.0;
        //            WATRHa = 0.0;
        //            wetLanduseHa = 0.0;
        //            foreach (var (crop, soilSlopeNumbers) in basinData.cropSoilSlopeNumbers) {
        //                cropCode = this._gv.db.getLanduseCode(crop);
        //                if (Parameters._WATERLANDUSES.Contains(cropCode)) {
        //                    foreach (var slopeNumbers in soilSlopeNumbers.Values) {
        //                        foreach (var hru in slopeNumbers.Values) {
        //                            hruData = basinData.hruMap[hru];
        //                            if (cropCode == "WATR") {
        //                                WATRHa += hruData.area / 10000.0;
        //                            } else {
        //                                wetLanduseHa += hruData.area / 10000.0;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            waterStats[basin] = (basinHUC, areaHa, reservoirHa, pondHa, lakeHa, WATRHa, WATRInStreamHa, streamAreaHa, swampMarshHa, wetLanduseHa, playaHa);
        //            //Utils.loginfo('WATRha for basin {0} is {1}', basin, WATRHa))
        //        }
        //    }
        //    return waterStats;
        //    //                 NHDWaterHa = 0
        //    //                 NHDWetlandHa = 0
        //    //                 basinGeom = basinShape.geometry()
        //    //                 # basinBoundary = QgsGeometry.fromRect(basinGeom.boundingBox()).asWkt()
        //    //                 basinBoundary = basinGeom.boundingBox().asWktPolygon()
        //    //                 for row in NHDWaterConn.execute(sql, (basinBoundary,)):
        //    //                     waterBody = QgsGeometry.fromWkt(row[0])
        //    //                     waterGeom = basinGeom.intersection(waterBody)
        //    //                     if not waterGeom.isEmpty():
        //    //                         areaHa = waterGeom.area() / 1E4
        //    //                         if row[1]  == 'SwampMarsh':
        //    //                             NHDWetlandHa += areaHa
        //    //                         else:
        //    //                             # IceMass, LakePond, Playa, Reservoir and Inundation all treated as water
        //    //                             NHDWaterHa += areaHa
        //    //                 stats.Write('{0}, {1:F2}, {2:F2}, {3:F2}, {4:F2}, {5:F2}, {6:F2}, {7:F2}\n', basinHUC, NIDWaterHa, NHDWaterHa, WATRHa, WATRInStreamHa, streamAreaHa, NHDWetlandHa, wetLanduseHa))
        //}

        //// Write water stats file.
        //public virtual object writeWaterStats2(object waterStats, object reductions) {
        //    object statsFile;
        //    if (this._gv.isHUC) {
        //        var huc = this._gv.projName[3];
        //        var l = huc.Count + 2;
        //        var region = huc[:2:];
        //        statsFile = Utils.join(this._gv.projDir, "waterStats{0}_HUC{1}.csv", region, l));
        //    } else {
        //        // HAWQS
        //        statsFile = Utils.join(this._gv.projDir, "waterbodyStats.csv");
        //    }
        //    using (var stats = open(statsFile, "w")) {
        //        stats.Write("HUC, Area, Reservoir, Pond, Lake, WATR, WATRInStreams, StreamArea, SwampMarsh, WetLanduse, Playa, WATRReduction, Percent\n");
        //        foreach (var (basin, (basinHUC, areaHa, reservoirHa, pondHa, lakeHa, WATRHa, WATRInStreamHa, streamAreaHa, swampMarshHa, wetLanduseHa, playaHa)) in waterStats) {
        //            (reduction, percent) = reductions[basin];
        //            stats.Write(string.Format("'{0}', {1:F2}, {2:F2}, {3:F2}, {4:F2}, {5:F2}, {6:F2}, {7:F2}, {8:F2}, {9:F2}, {10:F2}, {11:F2}, {12:F2}\n", basinHUC, areaHa, reservoirHa, pondHa, lakeHa, WATRHa, WATRInStreamHa, streamAreaHa, swampMarshHa, wetLanduseHa, playaHa, reduction, percent));
        //        }
        //    }
        //}

        // Write hrus table.
        public virtual void writeHRUsAndUncombTables() {
            var oid = 0;
            var table1 = "hrus";
            var sql1 = "INSERT INTO " + table1 + " VALUES";
            var table2 = "uncomb";
            var sql2 = "INSERT INTO " + table2 + " VALUES";
            var clearSQL = "DELETE FROM " + table1;
            this._gv.db.execNonQuery(clearSQL);
            clearSQL = "DELETE FROM " + table2;
            this._gv.db.execNonQuery(clearSQL);
            foreach (var (hru, hrudata) in this.hrus) {
                var basin = hrudata.basin;
                var basinData = this.basins[basin];
                // ignore basins not mapping to SWAT basin (empty, or edge when using grid model)
                if (this._gv.topo.basinToSWATBasin.ContainsKey(basin)) {
                    var lu = this._gv.db.getLanduseCode(hrudata.crop);
                    bool ok;
                    var soil = this._gv.db.getSoilName(hrudata.soil, out ok);
                    var meanSlopePercent = hrudata.meanSlope * 100;
                    string slp;
                    if (this._gv.isHUC || this._gv.isHAWQS) {
                        slp = CreateHRUs.HUCSlopeClass(meanSlopePercent);
                    } else {
                        slp = this._gv.db.slopeRange(hrudata.slope);
                    }
                    var hruha = hrudata.area / 10000;
                    var arlu = basinData.cropAreas[hrudata.crop] / 10000;
                    var arso = basinData.cropSoilArea(hrudata.crop, hrudata.soil, this._gv.isBatch) / 10000;
                    var arslp = hruha;
                    var uc = lu + "_" + soil + "_" + slp;
                    var SWATBasin = this._gv.topo.basinToSWATBasin[basin];
                    var hrusArea = (basinData.area - (basinData.reservoirArea + basinData.pondArea + basinData.lakeArea)) / 10000;
                    var filebase = Utils.fileBase(SWATBasin, hrudata.relHru, forTNC: this._gv.forTNC);
                    oid += 1;
                    var inserts = "(" +
                            oid.ToString() + "," +
                            SWATBasin.ToString() + "," +
                            hrusArea.ToString() + "," +
                            DBUtils.quote(lu) + "," +
                            arlu.ToString() + "," +
                            DBUtils.quote(soil) + "," +
                            arso.ToString() + "," +
                            DBUtils.quote(slp) + "," +
                            arslp.ToString() + "," +
                            meanSlopePercent.ToString() + "," +
                            DBUtils.quote(uc) + "," +
                            hru.ToString() + "," +
                            DBUtils.quote(filebase) + ");";
                    this._gv.db.execNonQuery(sql1 + inserts);
                    inserts = "(" +
                        oid.ToString() + "," +
                        SWATBasin.ToString() + "," +
                        hrudata.crop.ToString() + "," +
                        DBUtils.quote(lu) + "," +
                        hrudata.soil.ToString() + "," +
                        DBUtils.quote(soil) + "," +
                        hrudata.slope.ToString() + "," +
                        DBUtils.quote(slp) + "," +
                        meanSlopePercent.ToString() + "," +
                        hruha.ToString() + "," +
                        DBUtils.quote(uc) + ");";
                    this._gv.db.execNonQuery(sql2 + inserts);
                }
            }
        }

        // Return slope class as 0-2, 2-8 or 8-100.
        public static string HUCSlopeClass(double slopePercent) {
            if (slopePercent < 2) {
                return "0-2";
            }
            if (slopePercent < 8) {
                return "2-8";
            }
            return "8-100";
        }

        // Set HRUGIS values to NA.
        public void clearHRUGISNums(OSGeo.OGR.Layer fullHRUsLayer, int hrugisIndx) {
            OSGeo.OGR.Feature feature = null;
            do {
                feature = fullHRUsLayer.GetNextFeature();
                if (feature != null) {
                    feature.SetField(hrugisIndx, "NA");
                }
            } while(feature != null);
        }

        // Add HRUGIS values for actual HRUs.
        public void addHRUGISNums(
            int basin,
            OSGeo.OGR.Layer fullHRUsLayer,
            int subIndx,
            int luseIndx,
            int soilIndx,
            int slopeIndx,
            int hrugisIndx) {
            string hrugis;
            // ignore empty basins
            if (this._gv.topo.basinToSWATBasin.ContainsKey(basin)) {
                int SWATBasin = this._gv.topo.basinToSWATBasin[basin];
                foreach (var hruData in this.hrus.Values) {
                    if (hruData.basin == basin) {
                        var found = false;
                        var cropCode = this._gv.db.getLanduseCode(hruData.crop);
                        var origCropCode = this._gv.db.getLanduseCode(hruData.origCrop);
                        bool ok;
                        var soilName = this._gv.db.getSoilName(hruData.soil, out ok);
                        var slopeRange = this._gv.db.slopeRange(hruData.slope);
                        // the inspector method always fails.  Use OGR
                        //await QueuedTask.Run(() => {
                        //    var insp = new Inspector();
                        //    using (var rows = fullHRUsLayer.Search(null)) {
                        //        while (rows.MoveNext()) {
                        //            var row = rows.Current as ArcGIS.Core.Data.Feature;
                        //            insp.Load(row);
                        //            if (SWATBasin == (int)insp[subIndx] && origCropCode == (string)insp[luseIndx] &&
                        //                    soilName == (string)insp[soilIndx] && slopeRange == (string)insp[slopeIndx]) {
                        //                var op = new EditOperation();
                        //                found = true;
                        //                var oldgis = (string)insp[hrugisIndx];
                        //                if (oldgis == "NA") {
                        //                    hrugis = Utils.fileBase(SWATBasin, hruData.relHru, forTNC: this._gv.forTNC);
                        //                } else {
                        //                    hrugis = oldgis + string.Format(", {0}", hruData.relHru);
                        //                }
                        //                insp[hrugisIndx] = hrugis;
                        //                op.Modify(insp);
                        //                ok = true;
                        //                if (op.IsEmpty) {
                        //                    ok = false;
                        //                } else {
                        //                    ok = op.Execute();
                        //                }
                        //                if (!ok) {
                        //                    Utils.error("Cannot Write to FullHRUs attribute table", this._gv.isBatch);
                        //                    return;
                        //                }
                        //                break;
                        //            }
                        //        }
                        //        if (!found) {
                        //            Utils.error(String.Format("Cannot find FullHRUs feature for basin {0}, landuse {1}, soil {2}, slope range {3}",
                        //                SWATBasin, cropCode, soilName, slopeRange), this._gv.isBatch);
                        //            return;
                        //        }
                        //    }
                        //});
                        fullHRUsLayer.ResetReading();
                        OSGeo.OGR.Feature feature = null;
                        do {
                            feature = fullHRUsLayer.GetNextFeature();
                            if (feature != null) {
                                var sub = feature.GetFieldAsInteger(subIndx);
                                var crop = feature.GetFieldAsString(luseIndx);
                                var soil = feature.GetFieldAsString(soilIndx);
                                var slope = feature.GetFieldAsString(slopeIndx);
                                if (SWATBasin == sub && origCropCode == crop && soilName == soil && slopeRange == slope) {
                                    found = true;
                                    var oldgis = feature.GetFieldAsString(hrugisIndx);
                                    if (oldgis == "NA") {
                                        hrugis = Utils.fileBase(SWATBasin, hruData.relHru, forTNC: this._gv.forTNC);
                                    } else {
                                        hrugis = oldgis + string.Format(", {0}", hruData.relHru);
                                    }
                                    feature.SetField(hrugisIndx, hrugis);
                                    fullHRUsLayer.SetFeature(feature);
                                }
                            }
                        } while (!found && feature != null);
                        if (!found) {
                            Utils.error(String.Format("Cannot find FullHRUs feature for basin {0}, landuse {1}, soil {2}, slope range {3}",
                                       SWATBasin, cropCode, soilName, slopeRange), this._gv.isBatch);
                            return;
                        }
                    }
                }
            }
        }


        // Create and load the actual HRUs file.
        public async void writeActHRUs(OSGeo.OGR.Layer fullHRUsLayer, int hrugisIndx) {
            var actHRUsBasename = "hru2";
            var actHRUsFilename = actHRUsBasename + ".shp";
            Utils.copyShapefile(this._gv.fullHRUsFile, actHRUsBasename, this._gv.shapesDir);
            var actHRUsFile = Utils.join(this._gv.shapesDir, actHRUsFilename);
            if (this.removeDeselectedHRUs(actHRUsFile, hrugisIndx)) {
                var legend = Utils._ACTHRUSLEGEND;
                var group = MapView.Active.Map.Layers.OfType<GroupLayer>().FirstOrDefault(layer => string.Equals(layer.Name, Utils._WATERSHED_GROUP_NAME));
                var index = 0;
                await Utils.removeLayerByLegend(legend);
                var ft = FileTypes._HRUS;
                if (group is not null) {
                    var mapLayer = await QueuedTask.Run(() => LayerFactory.Instance.CreateLayer(new Uri(actHRUsFile), group, index, String.Format("{0} ({1})", legend, actHRUsBasename)));
                    FileTypes.ApplySymbolToFeatureLayerAsync((FeatureLayer)mapLayer, ft, this._gv);
                    Utils.setMapTip((FeatureLayer)mapLayer, ft);
                }
                // remove visibility from FullHRUs layer
                var layer = Utils.getLayerByLegend(Utils._FULLHRUSLEGEND);
                Utils.setLayerVisibility(layer, false);
                // copy actual HRUs file as template for visualisation
                Utils.copyShapefile(actHRUsFile, Parameters._HRUS, this._gv.tablesOutDir);
            }
        }

        // Remove non-actual HRUs.
        public virtual bool removeDeselectedHRUs(string actHRUsFile, int hrugisIndx) {
            using (var ds = Ogr.Open(actHRUsFile, 1)) {
                var layer = ds.GetLayerByIndex(0);
                layer.ResetReading();
                var deselectedIds = new List<long>();
                OSGeo.OGR.Feature hru = null;
                do {
                    hru = layer.GetNextFeature();
                    if (hru != null) {
                        if (hru.GetFieldAsString(hrugisIndx) == "NA") {
                            deselectedIds.Add(hru.GetFID());
                        }
                    }
                } while (hru != null);
                foreach (var id in deselectedIds) {
                    var ok = layer.DeleteFeature(id);
                    if (ok != 0) {
                        Utils.error("Cannot delete features from actual HRUs shapefile", this._gv.isBatch);
                        return false;
                    }
                }
            }
            return true;
        }

        // Write Watershed table in project database, make subs1.shp in shapes directory, and copy as results template to TablesOut directory.
        public async void writeWatershedTable() {
            var subsFile = Utils.join(this._gv.tablesOutDir, Parameters._SUBS + ".shp");
            await Utils.removeLayerAndFiles(subsFile);
            var parms = Geoprocessing.MakeValueArray(this._gv.tablesOutDir, Parameters._SUBS + ".shp", "POLYGON");
            Utils.runPython("runCreateSubsFile.py", parms, this._gv);
            Utils.copyPrj(this._gv.wshedFile, subsFile);
            var subs1File = Utils.join(this._gv.shapesDir, Parameters._SUBS1 + ".shp");
            await Utils.removeLayerAndFiles(subs1File);
            var parms1 = Geoprocessing.MakeValueArray(this._gv.shapesDir, Parameters._SUBS1 + ".shp", "POLYGON");
            Utils.runPython("runCreateSubs1File.py", parms1, this._gv);
            Utils.copyPrj(this._gv.wshedFile, subs1File);
            int SWATBasin;
            using (var subs1Ds = Ogr.Open(subs1File, 1)) {
                OSGeo.OGR.Layer layer1 = subs1Ds.GetLayerByIndex(0);
                var subs1Def = layer1.GetLayerDefn();
                using (var subsDs = Ogr.Open(subsFile, 1))
                using (var wshedDs = Ogr.Open(this._gv.wshedFile, 0)) {
                    OSGeo.OGR.Driver drv = subsDs.GetDriver();
                    // get layer - should only be one
                    OSGeo.OGR.Layer layer = subsDs.GetLayerByIndex(0);
                    OSGeo.OGR.Layer wshedLayer = wshedDs.GetLayerByIndex(0);
                    var subsDef = layer.GetLayerDefn();
                    wshedLayer.ResetReading();
                    do {
                        var poly = wshedLayer.GetNextFeature();
                        if (poly == null) {
                            break;
                        }
                        SWATBasin = poly.GetFieldAsInteger(Topology._SUBBASIN);
                        if (SWATBasin == 0) {
                            continue;
                        }
                        var geom = poly.GetGeometryRef();
                        var subsPoly = new OSGeo.OGR.Feature(subsDef);
                        subsPoly.SetField(Topology._SUBBASIN, SWATBasin);
                        subsPoly.SetGeometry(geom);
                        layer.CreateFeature(subsPoly);
                        var subs1Poly = new OSGeo.OGR.Feature(subs1Def);
                        subs1Poly.SetField(Topology._SUBBASIN, SWATBasin);
                        subs1Poly.SetGeometry(geom);
                        layer1.CreateFeature(subs1Poly);
                    } while (true);
                }
                // add fields from Watershed table
                var subIdx = subs1Def.GetFieldIndex(Topology._SUBBASIN);
                var areaIdx = subs1Def.GetFieldIndex("Area");
                var slo1Idx = subs1Def.GetFieldIndex("Slo1");
                var len1Idx = subs1Def.GetFieldIndex("Len1");
                var sllIdx = subs1Def.GetFieldIndex("Sll");
                var cslIdx = subs1Def.GetFieldIndex("Csl");
                var wid1Idx = subs1Def.GetFieldIndex("Wid1");
                var dep1Idx = subs1Def.GetFieldIndex("Dep1");
                var latIdx = subs1Def.GetFieldIndex("Lat");
                var longIdx = subs1Def.GetFieldIndex("Long_");
                var elevIdx = subs1Def.GetFieldIndex("Elev");
                var elevMinIdx = subs1Def.GetFieldIndex("ElevMin");
                var elevMaxIdx = subs1Def.GetFieldIndex("ElevMax");
                var bNameIdx = subs1Def.GetFieldIndex("Bname");
                var shapeLenIdx = subs1Def.GetFieldIndex("Shape_Len");
                var shapeAreaIdx = subs1Def.GetFieldIndex("Shape_Area");
                var hydroIdIdx = subs1Def.GetFieldIndex("HydroID");
                var OutletIdIdx = subs1Def.GetFieldIndex("OutletID");
                var table = "Watershed";
                if (this._gv.isHUC || this._gv.isHAWQS || this._gv.forTNC) {
                    var sql0 = "DROP TABLE IF EXISTS Watershed";
                    this._gv.db.execNonQuery(sql0);
                    var sql1 = DBUtils._WATERSHEDCREATESQL;
                    this._gv.db.execNonQuery(sql1);
                } else {
                    var clearSQL = "DELETE FROM " + table;
                    this._gv.db.execNonQuery(clearSQL);
                }
                IEnumerable<int> iterator;
                if (this._gv.useGridModel) {
                    iterator = this.basins.Keys;
                } else {
                    iterator = Enumerable.Range(0, this._gv.topo.SWATBasinToBasin.Count);
                }
                // deal with basins in SWATBasin order so that HRU numbers look logical
                int basin, SWATOutletBasin;
                foreach (var i in iterator) {
                    if (this._gv.useGridModel) {
                        basin = i;
                        SWATBasin = this._gv.topo.basinToSWATBasin[basin];
                        int outlet;
                        if (!this._gv.topo.catchmentOutlets.TryGetValue(i, out outlet)) {
                            outlet = -1;
                        }
                        if (!this._gv.topo.basinToSWATBasin.TryGetValue(outlet, out SWATOutletBasin)) {
                            SWATOutletBasin = 0;
                        }
                    } else {
                        // i will range from 0 to n-1, SWATBasin from 1 to n
                        SWATBasin = i + 1;
                        basin = this._gv.topo.SWATBasinToBasin[SWATBasin];
                        SWATOutletBasin = 0;
                    }
                    BasinData basinData = this.basins[basin];
                    var areaKm = basinData.area / 1000000.0;
                    var areaHa = areaKm * 100;
                    if (this._gv.isHUC) {
                        if (basinData.cellCount == 0) {
                            var huc12 = this._gv.projName[3];
                            var logFile = this._gv.logFile;
                            Utils.information(string.Format("WARNING: Basin {0} in project huc{1} has zero cell count", SWATBasin, huc12), this._gv.isBatch, logFile: logFile);
                        }
                    } else {
                        //Debug.Assert(basinData.cellCount > 0);
                        //Debug.Assert(string.Format("Basin {0} has zero cell count", SWATBasin));
                    }
                    var meanSlope = basinData.cellCount == 0 ? 0 : basinData.totalSlope / basinData.cellCount;
                    var meanSlopePercent = meanSlope * 100;
                    var farDistance = basinData.farDistance;
                    var slsubbsn = Utils.getSlsubbsn(meanSlope);
                    //Debug.Assert(farDistance > 0);
                    //Debug.Assert(string.Format("Longest flow length is zero for basin {0}", SWATBasin));
                    var farSlopePercent = (basinData.farElevation - basinData.outletElevation) / basinData.farDistance * 100;
                    // formula from Srinivasan 11/01/06
                    var tribChannelWidth = 1.29 * Math.Pow(areaKm, 0.6);
                    var tribChannelDepth = 0.13 * Math.Pow(areaKm, 0.4);
                    var centroid = this._gv.topo.basinCentroids[basin];
                    var centreX = centroid.X;
                    var centreY = centroid.Y;
                    var centroidll = this._gv.topo.pointToLatLong(new ArcGIS.Core.Geometry.Coordinate2D(centreX, centreY));
                    var lat = centroidll.Y;
                    var lon = centroidll.X;
                    var meanElevation = basinData.cellCount == 0 ? basinData.outletElevation : basinData.totalElevation / basinData.cellCount;
                    var elevMin = basinData.outletElevation;
                    var elevMax = basinData.maxElevation;
                    layer1.ResetReading();
                    long fid = -1;
                    OSGeo.OGR.Feature f = null;
                    foreach (int id in Enumerable.Range(0, (int)layer1.GetFeatureCount(1))) {
                        f = layer1.GetFeature(id);
                        if (SWATBasin == f.GetFieldAsInteger(subIdx)) {
                            fid = f.GetFID();
                            break;
                        }
                    }
                    if (fid < 0) {
                        Utils.loginfo(string.Format("Subbasin {0} in {1} has been removed", SWATBasin, subs1File));
                        continue;
                    }
                    f.SetField(areaIdx, areaHa);
                    f.SetField(slo1Idx, meanSlopePercent);
                    f.SetField(len1Idx, farDistance);
                    f.SetField(sllIdx, slsubbsn);
                    f.SetField(cslIdx, farSlopePercent);
                    f.SetField(wid1Idx, tribChannelWidth);
                    f.SetField(dep1Idx, tribChannelDepth);
                    f.SetField(elevIdx, meanElevation);
                    f.SetField(latIdx, lat);
                    f.SetField(longIdx, lon);
                    f.SetField(elevMinIdx, elevMin);
                    f.SetField(elevMaxIdx, elevMax);
                    f.SetField(bNameIdx, "");
                    f.SetField(shapeLenIdx, 0);
                    f.SetField(shapeAreaIdx, basinData.polyArea);
                    f.SetField(hydroIdIdx, SWATBasin + 300000);
                    f.SetField(OutletIdIdx, SWATBasin + 100000);
                    layer1.SetFeature(f);
                    string insert;
                    var sql = "INSERT INTO " + table + " VALUES";
                    if (this._gv.isHUC || this._gv.isHAWQS || this._gv.forTNC) {
                        insert = "(" +
                            SWATBasin.ToString() + ",0," +
                            SWATBasin.ToString() + "," +
                            SWATBasin.ToString() + "," +
                            areaHa.ToString() + "," +
                            meanSlopePercent.ToString() + "," +
                            farDistance.ToString() + "," +
                            slsubbsn.ToString() + "," +
                            farSlopePercent.ToString() + "," +
                            tribChannelWidth.ToString() + "," +
                            tribChannelDepth.ToString() + "," +
                            lat.ToString() + "," +
                            lon.ToString() + "," +
                            meanElevation.ToString() + "," +
                            elevMin.ToString() + "," +
                            elevMax.ToString() + ",'',0," +
                            basinData.polyArea.ToString() + "," +
                            basinData.definedArea.ToString() + "," +
                            basinData.totalHRUAreas().ToString() + "," +
                            SWATBasin + 300000.ToString() + "," +
                            SWATBasin + 100000.ToString() + "," +
                            SWATOutletBasin.ToString() + ");";
                    } else {
                        insert = "(" +
                            SWATBasin.ToString() + ",0," +
                            SWATBasin.ToString() + "," +
                            SWATBasin.ToString() + "," +
                            areaHa.ToString() + "," +
                            meanSlopePercent.ToString() + "," +
                            farDistance.ToString() + "," +
                            slsubbsn.ToString() + "," +
                            farSlopePercent.ToString() + "," +
                            tribChannelWidth.ToString() + "," +
                            tribChannelDepth.ToString() + "," +
                            lat.ToString() + "," +
                            lon.ToString() + "," +
                            meanElevation.ToString() + "," +
                            elevMin.ToString() + "," +
                            elevMax.ToString() + ",'',0," +
                            basinData.area.ToString() + "," +
                            SWATBasin + 300000.ToString() + "," +
                            SWATBasin + 100000.ToString() + ");";
                    }
                    this._gv.db.execNonQuery(sql + insert);
                }
            }
            // add layer in place of watershed layer, unless using grid model
            if (!this._gv.useGridModel) {
                var ft = this._gv.existingWshed ? FileTypes._EXISTINGWATERSHED : FileTypes._WATERSHED;
                var wshedLayer = (await Utils.getLayerByFilename(this._gv.wshedFile, ft, null, null, null)).Item1 as FeatureLayer;
                var ft1 = this._gv.existingWshed ? FileTypes._EXISTINGSUBBASINS : FileTypes._SUBBASINS;
                var subs1Layer = (await Utils.getLayerByFilename(subs1File, ft1, this._gv, wshedLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                var label = subs1Layer.LabelClasses.FirstOrDefault();
                if (label != null) {
                    await QueuedTask.Run(() => {
                        label.SetExpression("$feature.Subbasin");
                        subs1Layer.SetLabelVisibility(true);
                    });
                }
                // no need to expand legend since any subbasins upstream from inlets have been removed
                await QueuedTask.Run(() => {
                    subs1Layer.SetExpanded(false);
                });
                if (wshedLayer is not null) {
                    Utils.setLayerVisibility(wshedLayer, false);
                }
            }
        }

        // Return sum of areas of subbasins in square metres.
        public virtual double totalBasinsArea() {
            var total = 0.0;
            foreach (var bd in this.basins.Values) {
                total += bd.area;
            }
            return total;
        }

        // Return sum of areas of reservoirs in square metres.
        public virtual double totalReservoirsArea() {
            var total = 0.0;
            foreach (var bd in this.basins.Values) {
                total += bd.reservoirArea;
            }
            return total;
        }

        // Return sum of areas of ponds in square metres.
        public virtual double totalPondsArea() {
            var total = 0.0;
            foreach (var bd in this.basins.Values) {
                total += bd.pondArea;
            }
            return total;
        }

        // Return sum of areas of lakes in square metres.
        public virtual double totalLakesArea() {
            var total = 0.0;
            foreach (var bd in this.basins.Values) {
                total += bd.lakeArea;
            }
            return total;
        }

        // Return sum of areas of playas in square metres.
        public virtual double totalPlayasArea() {
            var total = 0.0;
            foreach (var bd in this.basins.Values) {
                total += bd.playaArea;
            }
            return total;
        }

        // 
        //         Return maps of crop -> area in square metres across all subbasins.
        //         
        //         If withHRUs, return updated and original values.
        //         Otherise return the original values, and first map is None
        //         
        //         
        public virtual (Dictionary<int, double>, Dictionary<int, double>) totalCropAreas(bool withHRUs) {
            var result1 = withHRUs ? new Dictionary<int, double>() : null;
            var result2 = new Dictionary<int, double>();
            foreach (var bd in this.basins.Values) {
                var map1 = withHRUs ? bd.cropAreas : null;
                var map2 = bd.originalCropAreas;
                if (map1 is not null) {
                    //Debug.Assert(result1 is not null);
                    foreach (var (crop, area) in map1) {
                        if (result1.ContainsKey(crop)) {
                            result1[crop] += area;
                        } else {
                            result1[crop] = area;
                        }
                    }
                }
                foreach (var (crop, area) in map2) {
                    if (result2.ContainsKey(crop)) {
                        result2[crop] += area;
                    } else {
                        result2[crop] = area;
                    }
                }
            }
            return (result1, result2);
        }

        // Return map of soil -> area in square metres across all subbasins.
        //         
        //         If withHRUs, return updated and original values.
        //         Otherise return the original values, and first map is None
        //         
        //         
        public virtual (Dictionary<int, double>, Dictionary<int, double>) totalSoilAreas(bool withHRUs) {
            var result1 = withHRUs ? new Dictionary<int, double>() : null;
            var result2 = new Dictionary<int, double>();
            foreach (var bd in this.basins.Values) {
                var map1 = withHRUs ? bd.soilAreas : null;
                var map2 = bd.originalSoilAreas;
                if (map1 is not null) {
                    //Debug.Assert(result1 is not null);
                    foreach (var (soil, area) in map1) {
                        if (result1.ContainsKey(soil)) {
                            result1[soil] += area;
                        } else {
                            result1[soil] = area;
                        }
                    }
                }
                foreach (var (soil, area) in map2) {
                    if (result2.ContainsKey(soil)) {
                        result2[soil] += area;
                    } else {
                        result2[soil] = area;
                    }
                }
            }
            return (result1, result2);
        }

        // Return map of slope -> area in square metres across all subbasins.
        //         
        //         If withHRUs, return updated and original values.
        //         Otherise return the original values, and first map is None
        //         
        //         
        public virtual (Dictionary<int, double>, Dictionary<int, double>) totalSlopeAreas(bool withHRUs) {
            var result1 = withHRUs ? new Dictionary<int, double>() : null;
            var result2 = new Dictionary<int, double>();
            foreach (var bd in this.basins.Values) {
                var map1 = withHRUs ? bd.slopeAreas : null;
                var map2 = bd.originalSlopeAreas;
                if (map1 is not null) {
                    //Debug.Assert(result1 is not null);
                    foreach (var (slope, area) in map1) {
                        if (result1.ContainsKey(slope)) {
                            result1[slope] += area;
                        } else {
                            result1[slope] = area;
                        }
                    }
                }
                foreach (var (slope, area) in map2) {
                    if (result2.ContainsKey(slope)) {
                        result2[slope] += area;
                    } else {
                        result2[slope] = area;
                    }
                }
            }
            return (result1, result2);
        }

        //  Print a line containing crop, area in hectares, 
        //         percent of total1, percent of total2.
        //         
        //         If cropAreas is not None, use its figures and add original figures in bracket for comparison.
        //         
        public virtual void printCropAreas(
            Dictionary<int, double> cropAreas,
            Dictionary<int, double> originalCropAreas,
            double total1,
            double total2,
            StreamWriter fw) {
            double opercent2;
            double opercent1;
            double originalArea = 0;
            string landuseCode;
            Dictionary<int, double> original;
            Dictionary<int, double> main;
            if (cropAreas is not null && cropAreas.Count > 0) {
                main = cropAreas;
                original = originalCropAreas;
            } else {
                main = originalCropAreas;
                original = null;
            }
            foreach (var (crop, areaM) in main) {
                landuseCode = this._gv.db.getLanduseCode(crop);
                var area = areaM / 10000;
                var string0 = string.Format("{0:F2}", area).PadLeft(15);
                if (original is not null) {
                    // crop may not have been in original because of splitting
                    if (original.TryGetValue(crop, out originalArea)) {
                        originalArea /= 10000;
                    } else {
                        originalArea = 0;
                    }
                    string0 += string.Format("({0:F2})", originalArea).PadLeft(15);
                }
                fw.Write(landuseCode.PadLeft(30) + string0);
                if (total1 > 0) {
                    var percent1 = area / total1 * 100;
                    var string1 = string.Format("{0:F2}", percent1).PadLeft(15);
                    if (original is not null) {
                        opercent1 = originalArea / total1 * 100;
                        string1 += string.Format("({0:F2})", opercent1).PadLeft(8);
                    }
                    fw.Write(string1);
                    if (total2 > 0) {
                        var percent2 = area / total2 * 100;
                        var string2 = string.Format("{0:F2}", percent2).PadLeft(15);
                        if (original is not null) {
                            opercent2 = originalArea / total2 * 100;
                            string2 += string.Format("({0:F2})", opercent2).PadLeft(8);
                        }
                        fw.Write(string2);
                    }
                }
                fw.WriteLine("");
            }
            // if have original, add entries for originals that have been removed
            if (original is not null) {
                foreach (var (crop, areaM) in original) {
                    if (!main.ContainsKey(crop)) {
                        landuseCode = this._gv.db.getLanduseCode(crop);
                        originalArea = areaM / 10000;
                        fw.Write(landuseCode.PadLeft(30) + string.Format("({0:F2})", originalArea).PadLeft(30));
                        if (total1 > 0) {
                            opercent1 = originalArea / total1 * 100;
                            fw.Write(string.Format("({0:F2})", opercent1).PadLeft(23));
                        }
                        if (total2 > 0) {
                            opercent2 = originalArea / total2 * 100;
                            fw.Write(string.Format("({0:F2})", opercent2).PadLeft(23));
                        }
                        fw.WriteLine("");
                    }
                }
            }
        }

        //  Print a line containing soil, area in hectares, 
        //         percent of total1, percent of total2.
        //         
        //         If soilAreas is not None, use its figures and add original figures in bracket for comparison.
        //         
        public virtual void printSoilAreas(
            Dictionary<int, double> soilAreas,
            Dictionary<int, double> originalSoilAreas,
            double total1,
            double total2,
            StreamWriter fw) {
            object opercent2;
            object opercent1;
            double originalArea = 0;
            Dictionary<int, double> original;
            Dictionary<int, double> main;
            if (soilAreas is not null && soilAreas.Count > 0) {
                main = soilAreas;
                original = originalSoilAreas;
            } else {
                main = originalSoilAreas;
                original = null;
            }
            foreach (var (soil, areaM) in main) {
                bool ok;
                var soilName = this._gv.db.getSoilName(soil, out ok);
                var area = areaM / 10000;
                var string0 = string.Format("{0:F2}", area).PadLeft(15);
                if (original is not null) {
                    originalArea = original[soil] / 10000;
                    string0 += string.Format("({0:F2})", originalArea).PadLeft(15);
                }
                fw.Write(soilName.PadLeft(30) + string0);
                if (total1 > 0) {
                    var percent1 = area / total1 * 100;
                    var string1 = string.Format("{0:F2}", percent1).PadLeft(15);
                    if (original is not null) {
                        opercent1 = originalArea / total1 * 100;
                        string1 += string.Format("({0:F2})", opercent1).PadLeft(8);
                    }
                    fw.Write(string1);
                    if (total2 > 0) {
                        var percent2 = area / total2 * 100;
                        var string2 = string.Format("{0:F2}", percent2).PadLeft(15);
                        if (original is not null) {
                            opercent2 = originalArea / total2 * 100;
                            string2 += string.Format("({0:F2})", opercent2).PadLeft(8);
                        }
                        fw.Write(string2);
                    }
                }
                fw.WriteLine("");
            }
            // if have original, add entries for originals that have been removed
            if (original is not null) {
                foreach (var (soil, areaM) in original) {
                    if (!main.ContainsKey(soil)) {
                        bool ok;
                        var soilName = this._gv.db.getSoilName(soil, out ok);
                        originalArea = areaM / 10000;
                        fw.Write(soilName.PadLeft(30) + string.Format("({0:F2})", originalArea).PadLeft(30));
                        if (total1 > 0) {
                            opercent1 = originalArea / total1 * 100;
                            fw.Write(string.Format("({0:F2})", opercent1).PadLeft(23));
                        }
                        if (total2 > 0) {
                            opercent2 = originalArea / total2 * 100;
                            fw.Write(string.Format("({0:F2})", opercent2).PadLeft(23));
                        }
                        fw.WriteLine("");
                    }
                }
            }
        }

        //  Print a line containing slope, area in hectares, 
        //         percent of total1, percent of total2.
        //         
        //         If soilAreas is not None, use its figures and add original figures in bracket for comparison.
        //         
        public virtual void printSlopeAreas(
            Dictionary<int, double> slopeAreas,
            Dictionary<int, double> originalSlopeAreas,
            double total1,
            double total2,
            StreamWriter fw) {
            object opercent2;
            object opercent1;
            double originalArea = 0;
            string slopeRange;
            Dictionary<int, double> original;
            Dictionary<int, double> main;
            if (slopeAreas is not null && slopeAreas.Count > 0) {
                main = slopeAreas;
                original = originalSlopeAreas;
            } else {
                main = originalSlopeAreas;
                original = null;
            }
            // seems natural to list these in increasing order
            foreach (var i in Enumerable.Range(0, this._gv.db.slopeLimits.Count + 1)) {
                if (main.ContainsKey(i)) {
                    slopeRange = this._gv.db.slopeRange(i);
                    var area = main[i] / 10000;
                    var string0 = string.Format("{0:F2}", area).PadLeft(15);
                    if (original is not null) {
                        originalArea = original[i] / 10000;
                        string0 += string.Format("({0:F2})", originalArea).PadLeft(15);
                    }
                    fw.Write(slopeRange.PadLeft(30) + string0);
                    if (total1 > 0) {
                        var percent1 = area / total1 * 100;
                        var string1 = string.Format("{0:F2}", percent1).PadLeft(15);
                        if (original is not null) {
                            opercent1 = originalArea / total1 * 100;
                            string1 += string.Format("({0:F2})", opercent1).PadLeft(8);
                        }
                        fw.Write(string1);
                        if (total2 > 0) {
                            var percent2 = area / total2 * 100;
                            var string2 = string.Format("{0:F2}", percent2).PadLeft(15);
                            if (original is not null) {
                                opercent2 = originalArea / total2 * 100;
                                string2 += string.Format("({0:F2})", opercent2).PadLeft(8);
                            }
                            fw.Write(string2);
                        }
                    }
                    fw.WriteLine("");
                }
            }
            // if have original, add entries for originals that have been removed
            if (original is not null) {
                foreach (var i in Enumerable.Range(0, this._gv.db.slopeLimits.Count + 1)) {
                    if (original.ContainsKey(i) && !main.ContainsKey(i)) {
                        slopeRange = this._gv.db.slopeRange(i);
                        originalArea = original[i] / 10000;
                        fw.Write(slopeRange.PadLeft(30) + string.Format("({0:F2})", originalArea).PadLeft(30));
                        if (total1 > 0) {
                            opercent1 = originalArea / total1 * 100;
                            fw.Write(string.Format("({0:F2})", opercent1).PadLeft(23));
                        }
                        if (total2 > 0) {
                            opercent2 = originalArea / total2 * 100;
                            fw.Write(string.Format("({0:F2})", opercent2).PadLeft(23));
                        }
                        fw.WriteLine("");

                        // no longer used as we use distFile
                        //===========================================================================
                        // @staticmethod
                        // def channelLengthToOutlet(basinData, pTransform, pBand, basinTransform, isBatch):
                        //     """Return distance in metres from farthest point in subbasin 
                        //     from its outlet to the outlet, along D8 drainage path.
                        //     """
                        //     bcol = basinData.farCol;
                        //     brow = basinData.farRow;
                        //     boutletCol = basinData.outletCol;
                        //     boutletRow = basinData.outletRow;
                        //     (x, y) = Topology.cellToProj(bcol, brow, basinTransform);
                        //     (col, row) = Topology.projToCell(x, y, pTransform);
                        //     (x, y) = Topology.cellToProj(boutletCol, boutletRow, basinTransform);
                        //     (outletCol, outletRow) = Topology.projToCell(x, y, pTransform);
                        //         
                        //     # since we accumulate distance moved, take these as positive
                        //     nsCellDistance = abs(pTransform[5])
                        //     weCellDistance = abs(pTransform[1])
                        //     diagCellDistance = math.sqrt(weCellDistance * weCellDistance + nsCellDistance * nsCellDistance)
                        //     distance = 0
                        //         
                        //     while ((col != outletCol) or (row != outletRow)):
                        //         direction = pBand.ReadAsArray(col, row, 1, 1)[0, 0]
                        //         if direction == 1: # E
                        //             col += 1
                        //             distance += weCellDistance
                        //         elif direction == 2: # NE
                        //             col += 1
                        //             row -= 1
                        //             distance += diagCellDistance
                        //         elif direction == 3: # N
                        //             row -= 1
                        //             distance += nsCellDistance
                        //         elif direction == 4: # NW
                        //             col -= 1
                        //             row -= 1
                        //             distance += diagCellDistance
                        //         elif direction == 5: # W
                        //             col -= 1
                        //             distance += weCellDistance
                        //         elif direction == 6: # SW
                        //             col -= 1
                        //             row += 1
                        //             distance += diagCellDistance
                        //         elif direction == 7: # S
                        //             row += 1
                        //             distance += nsCellDistance
                        //         elif direction == 8: # SE
                        //             col += 1
                        //             row += 1
                        //             distance += diagCellDistance
                        //         else: # we have run off the edge of the direction grid
                        //             (startx, starty) =  Topology.cellToProj(basinData.farCol, basinData.farRow, pTransform)
                        //             (x,y) = Topology.cellToProj(col, row, pTransform)
                        //             Utils.error('Channel length calculation from ({0:.0F}, {1:.0F}) halted at ({2:.0F}, {3:.0F})', startx, starty, x, y), isBatch)
                        //             return 0
                        //     return distance 
                        //===========================================================================
                    }
                }
            }
        }
    }
}
    



