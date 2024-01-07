


using System.Collections.Generic;

using System;
using System.Drawing;

using System.Linq;

using System.Diagnostics;
using System.IO;

using ArcGIS.Core.Data;
using Feature = ArcGIS.Core.Data.Feature;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Geometry;
using Envelope = ArcGIS.Core.Geometry.Envelope;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

using ArcGIS.Desktop.Mapping;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Runtime.CompilerServices;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;

using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using ActiproSoftware.Windows.Data;
using System.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using Microsoft.VisualBasic;
using System.Reflection;

namespace ArcSWAT3 {


    // Location and elevation of points at ends of reach, 
    //     draining from upper to lower.
    //     
    public class ReachData {
        
        public double lowerX;
        
        public double lowerY;
        
        public double lowerZ;
        
        public double upperX;
        
        public double upperY;
        
        public double upperZ;
        
        public ReachData(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2) {
            //# x coordinate of upper end
            this.upperX = x1;
            //# y coordinate of upper end
            this.upperY = y1;
            //# elevation of upper end
            this.upperZ = z1;
            //# x coordinate of lower end
            this.lowerX = x2;
            //# y coordinate of lower end
            this.lowerY = y2;
            //# elevation of lower end
            this.lowerZ = z2;
        }
    }
    
    // Module for creating and storing topological data 
    //     derived from watershed delineation.
    //     
    public class Topology {
        
        public Dictionary<int, double> basinAreas;
        
        public Dictionary<int, Coordinate2D> basinCentroids;
        
        public Dictionary<int, int> basinToLink;
        
        public Dictionary<int, int> basinToSWATBasin;
        
        public Dictionary<int, int> catchmentOutlets;
        
        public SpatialReference crsLatLong;
        
        public SpatialReference crsProject;
        
        public DBUtils db;
        
        public Envelope demExtent;
        
        public double demNodata;
        
        public Dictionary<int, int> downCatchments;
        
        public Dictionary<int, int> downLinks;
        
        public Dictionary<int, double> drainAreas;
        
        public int dsLinkIndex;
        
        public double dx;
        
        public double dy;
        
        public HashSet<int> emptyBasins;
        
        public bool forTNC;
        
        public bool fromGRASS;
        
        public int gridRows;
        
        public HashSet<int> inletLinks;
        
        public bool isBatch;
        
        public bool isHAWQS;
        
        public bool isHUC;
        
        public int linkIndex;
        
        public Dictionary<int, int> linkToBasin;
        
        public int MonitoringPointFid;
        
        public Dictionary<int, Coordinate2D> nearoutlets;
        
        public Dictionary<int, Coordinate2D> nearsources;
        
        public bool outletAtStart;
        
        public HashSet<int> outletLinks;
        
        public Dictionary<int, Coordinate2D> outlets;
        
        public Dictionary<int, int> ptSrcLinks;
        
        public Dictionary<int, ReachData> reachesData;
        
        public Dictionary<int, int> reservoirLinks;
        
        public Dictionary<int, double> streamLengths;
        
        public Dictionary<int, double> streamSlopes;
        
        public Dictionary<int, int> SWATBasinToBasin;
        
        public double TNCCatchmentThreshold;
        
        public HashSet<int> upstreamFromInlets;
        
        public double verticalFactor;
        
        public int wsnoIndex;
        
        public static string _LINKNO = "LINKNO";
        
        public static string _DSLINKNO = "DSLINKNO";
        
        public static string _USLINKNO1 = "USLINKNO1";
        
        public static string _USLINKNO2 = "USLINKNO2";
        
        public static string _DSNODEID = "DSNODEID";
        
        public static string _ORDER = "strmOrder";
        
        public static string _ORDER2 = "Order";
        
        public static string _LENGTH = "Length";
        
        public static string _MAGNITUDE = "Magnitude";
        
        public static string _DS_CONT_AR = "DSContArea";
        
        public static string _DS_CONT_AR2 = "DS_Cont_Ar";
        
        public static string _DROP = "strmDrop";
        
        public static string _DROP2 = "Drop";
        
        public static string _SLOPE = "Slope";
        
        public static string _STRAIGHT_L = "StraightL";
        
        public static string _STRAIGHT_L2 = "Straight_L";
        
        public static string _US_CONT_AR = "USContArea";
        
        public static string _US_CONT_AR2 = "US_Cont_Ar";
        
        public static string _WSNO = "WSNO";
        
        public static string _DOUT_END = "DOUTEND";
        
        public static string _DOUT_END2 = "DOUT_END";
        
        public static string _DOUT_START = "DOUTSTART";
        
        public static string _DOUT_START2 = "DOUT_START";
        
        public static string _DOUT_MID = "DOUTMID";
        
        public static string _DOUT_MID2 = "DOUT_MID";
        
        public static string _ID = "ID";
        
        public static string _INLET = "INLET";
        
        public static string _RES = "RES";
        
        public static string _PTSOURCE = "PTSOURCE";
        
        public static string _POLYGONID = "PolygonId";
        
        public static string _DOWNID = "DownId";
        
        public static string _AREA = "Area";
        
        public static string _STREAMLINK = "StreamLink";
        
        public static string _STREAMLEN = "StreamLen";
        
        public static string _DSNODEIDW = "DSNodeID";
        
        public static string _DSWSID = "DSWSID";
        
        public static string _US1WSID = "US1WSID";
        
        public static string _US2WSID = "US2WSID";
        
        public static string _SUBBASIN = "Subbasin";
        
        public static string _PENWIDTH = "PenWidth";
        
        public static string _HRUGIS = "HRUGIS";
        
        public static string _TOTDASQKM = "TotDASqKm";
        
        public static string _OUTLET = "Outlet";
        
        public static string _SOURCEX = "SourceX";
        
        public static string _SOURCEY = "SourceY";
        
        public static string _OUTLETX = "OutletX";
        
        public static string _OUTLETY = "OutletY";
        
        public static int _HUCPointId = 100000;
        
        public Topology(
            bool isBatch,
            bool isHUC,
            bool isHAWQS,
            bool fromGRASS,
            bool forTNC,
            double TNCCatchmentThreshold) {
            //# Link to project database
            this.db = null;
            //# True if outlet end of reach is its first point, i.e. index zero."""
            this.outletAtStart = true;
            //# index to LINKNO in stream shapefile
            this.linkIndex = -1;
            //# index to DSLINKNO in stream shapefile
            this.dsLinkIndex = -1;
            //# index to WSNO in stream shapefile (value commonly called basin)
            this.wsnoIndex = -1;
            //# LINKNO to WSNO in stream shapefile (should be identity but we won't assume it unless isHAWQS)
            // WSNO is same as PolygonId in watershed shapefile, 
            // while LINKNO is used in DSLINKNO in stream shapefile
            this.linkToBasin = new Dictionary<int, int>();
            //# inverse table, possible since link-basin mapping is 1-1
            this.basinToLink = new Dictionary<int, int>();
            //# WSNO does not obey SWAT rules for basin numbers (range 1:n) 
            // so we invent and store SWATBasin numbers
            // also SWAT basins may not be empty
            this.basinToSWATBasin = new Dictionary<int, int>();
            //#inverse map to make it easy to output in numeric order of SWAT Basins
            this.SWATBasinToBasin = new Dictionary<int, int>();
            //# LINKNO to DSLINKNO in stream shapefile
            this.downLinks = new Dictionary<int, int>();
            //# zero area WSNO values
            this.emptyBasins = new HashSet<int>();
            //# centroids of basins as (x, y) pairs in projected units
            this.basinCentroids = new Dictionary<int, Coordinate2D>();
            //# catchment outlets, map of basin to basin, only used with grid meodels
            this.catchmentOutlets = new Dictionary<int, int>();
            //# catchment to downstream catchment or -1
            this.downCatchments = new Dictionary<int, int>();
            //# link to reach length in metres
            this.streamLengths = new Dictionary<int, double>();
            //# reach slopes in m/m
            this.streamSlopes = new Dictionary<int, double>();
            //# numpy array of total area draining to downstream end of link in square metres
            this.drainAreas = new Dictionary<int, double>();
            //# points and elevations at ends of reaches
            this.reachesData = new Dictionary<int, ReachData>();
            //# basin to area in square metres
            this.basinAreas = new Dictionary<int, double>();
            //# links which are user-defined or main outlets
            this.outletLinks = new HashSet<int>();
            //# links with reservoirs mapped to point id
            this.reservoirLinks = new Dictionary<int, int>();
            //# links with inlets
            this.inletLinks = new HashSet<int>();
            //# links with point sources at their outlet points mapped to point id
            this.ptSrcLinks = new Dictionary<int, int>();
            //# links draining to inlets
            this.upstreamFromInlets = new HashSet<int>();
            //# key to MonitoringPoint table
            this.MonitoringPointFid = 0;
            //# width of DEM cell in metres
            this.dx = 0;
            //# depth of DEM cell in metres
            this.dy = 0;
            //# number of elevation grid rows to make a watershed grid cell (only used in grid model)
            this.gridRows = 0;
            //# multiplier to turn DEM elevations to metres
            this.verticalFactor = 1;
            //# DEM nodata value
            this.demNodata = 0;
            //# DEM extent
            this.demExtent = null;
            //# map from basin to outlet point (used for calculating basin flow length)
            this.outlets = new Dictionary<int, Coordinate2D>();
            //# map from basin to near outlet point (used for placing extra reservoirs)
            this.nearoutlets = new Dictionary<int, Coordinate2D>();
            //# map from basin to near source point (use for placing extra point sources)
            this.nearsources = new Dictionary<int, Coordinate2D>();
            //# project projection (set from DEM)
            this.crsProject = null;
            //# lat-long coordinate reference system
            this.crsLatLong = SpatialReferences.WGS84;
            //# Flag to show if batch run
            this.isBatch = isBatch;
            //# flag for HUC projects
            this.isHUC = isHUC;
            //# flage for HAWQS projects
            this.isHAWQS = isHAWQS;
            //# flag for projects using GRASS delineation
            this.fromGRASS = fromGRASS;
            //# flag for TNC projects
            this.forTNC = forTNC;
            //# minimum catchment size for TNC projects in sq km
            this.TNCCatchmentThreshold = TNCCatchmentThreshold;
        }

        // Set DEM size parameters and stream orientation, and store source and outlet points for stream reaches.
        public async Task<bool> setUp0(RasterLayer demLayer, string streamFile, double verticalFactor) {
            double factor;
            Unit unit;
            // can fail if demLayer is None or not projected
            try {
                this.crsProject ??= await QueuedTask.Run(() => demLayer.GetSpatialReference());
                unit = this.crsProject.Unit;
            }
            catch (Exception ex) {
                Utils.loginfo(String.Format("Failure to read DEM unit: {0}", ex.Message));
                return false;
            }
            if (unit.Name == "Meter") {
                factor = 1;
            } else if (unit.Name == "Foot") {
                factor = 0.3048;
            } else {
                // unknown or degrees - will be reported in delineation - just quietly fail here
                Utils.loginfo(String.Format("Failure to read DEM unit: {0}", unit.ToString()));
                return false;
            }
            var XYSizes = await QueuedTask.Run<Tuple<double, double>>(() => {
                return demLayer.GetRaster().GetMeanCellSize();
            });
            this.dx = XYSizes.Item1 * factor;
            this.dy = XYSizes.Item2 * factor;
            Utils.loginfo(String.Format("Factor is {0}, cell width is {1}, cell depth is {2}", factor, this.dx, this.dy));
            this.demExtent = await QueuedTask.Run(() => demLayer.QueryExtent());
            this.verticalFactor = verticalFactor;
            Ogr.RegisterAll();
            var pathProBin = Path.GetDirectoryName(new System.Uri(Assembly.GetEntryAssembly().Location).AbsolutePath);
            var pathPro = Uri.UnescapeDataString(Directory.GetParent(pathProBin).FullName);
            Osr.SetPROJSearchPath(Path.Combine(pathPro, @"Resources\pedata\gdaldata"));
            using (var streamDs = Ogr.Open(streamFile, 0)) {
                if (streamDs is null) {
                    Utils.error(string.Format("Failed to open stream file {0}", streamFile), this.isBatch);
                    return false;
                }
                OSGeo.OGR.Driver drv = streamDs.GetDriver();
                if (drv is null) {
                    Utils.error("Cannot get OGR driver for stream file", this.isBatch);
                    return false;
                }
                // get layer - should only be one
                OSGeo.OGR.Layer layer = null;
                for (int iLayer = 0; iLayer < streamDs.GetLayerCount(); iLayer++) {
                    layer = streamDs.GetLayerByIndex(iLayer);
                    if (!(layer is null)) { break; }
                }
                if (layer == null) {
                    Utils.error(string.Format("No layers found in stream file {0}", streamFile), this.isBatch);
                    return false;
                }
                var numFeatures = layer.GetFeatureCount(1);
                this.outletAtStart = this.hasOutletAtStart(layer);
                Utils.loginfo(String.Format("Outlet at start is {0}", this.outletAtStart));
                if (!this.saveOutletsAndSources(layer)) {
                    return false;
                }
            }
            return true;
        }

        // Create topological data from layers.
        public async Task<bool> setUp(
            RasterLayer demLayer,
            string streamFile,
            string  wshedFile,
            string  outletFile,
            string  extraOutletFile,
            GlobalVars gv,
            bool existing,
            bool recalculate,
            bool useGridModel,
            bool reportErrors) {
            int SWATBasin;
            double area;
            int basin;
            int dsNode;
            double slope;
            double length;
            int dsLink;
            int link;
            int wsno;
            this.db = gv.db;
            this.linkToBasin.Clear();
            this.basinToLink.Clear();
            this.basinToSWATBasin.Clear();
            this.SWATBasinToBasin.Clear();
            this.downLinks.Clear();
            this.emptyBasins.Clear();
            // do not clear centroids unless existing and not using grid model: 
            if (existing && !useGridModel) {
                this.basinCentroids.Clear();
            }
            this.streamLengths.Clear();
            this.streamSlopes.Clear();
            this.reachesData.Clear();
            this.basinAreas.Clear();
            this.outletLinks.Clear();
            this.reservoirLinks.Clear();
            this.inletLinks.Clear();
            this.ptSrcLinks.Clear();
            this.upstreamFromInlets.Clear();
            var dsNodeToLink = new Dictionary<int, int>();
            // upstream array us will get very big for grid, so not used for grid models
            var us = new Dictionary<int, List<int>>();
            var ignoreError = !reportErrors;
            var ignoreWithExisting = existing || !reportErrors;
            var ignoreWithGrid = useGridModel || !reportErrors;
            var ignoreWithGridOrExisting = ignoreWithGrid || ignoreWithExisting;
            int dsNodeIndex = -1;
            using (var streamDs = Ogr.Open(streamFile, 0)) {
                var streamLayer = streamDs.GetLayerByIndex(0);
                this.linkIndex = this.getIndex(streamLayer, Topology._LINKNO, ignoreMissing: ignoreError);
                if (this.linkIndex < 0) {
                    Utils.loginfo("No LINKNO field in stream layer");
                    return false;
                }
                this.dsLinkIndex = this.getIndex(streamLayer, Topology._DSLINKNO, ignoreMissing: ignoreError);
                if (this.dsLinkIndex < 0) {
                    Utils.loginfo("No DSLINKNO field in stream layer");
                    return false;
                }
                dsNodeIndex = this.getIndex(streamLayer, Topology._DSNODEID, ignoreMissing: ignoreWithGridOrExisting);
                this.wsnoIndex = this.getIndex(streamLayer, Topology._WSNO, ignoreMissing: ignoreError);
                if (this.wsnoIndex < 0) {
                    Utils.loginfo(String.Format("No {0} field in stream layer", Topology._WSNO));
                    return false;
                }
                var lengthIndex = this.getIndex(streamLayer, Topology._LENGTH, ignoreMissing: ignoreWithGridOrExisting);
                var dropIndex = this.getIndex(streamLayer, Topology._DROP, ignoreMissing: true);
                if (dropIndex < 0) {
                    dropIndex = this.getIndex(streamLayer, Topology._DROP2, ignoreMissing: ignoreWithGridOrExisting);
                }
                var totDAIndex = this.getIndex(streamLayer, Topology._TOTDASQKM, ignoreMissing: true);
                this.demNodata = gv.elevationNoData;
                var time1 = DateTime.Now;
                var maxLink = 0;
                // drainAreas is a mapping from link number (used as index to array) of grid cell areas in sq m
                if (totDAIndex >= 0) {
                    // make it a dictionary rather than a numpy array because there is a big gap
                    // between most basin numbers and the nunbers for inlets (10000 +)
                    // and we need to do no calculation
                    // create it here for HUC and HAWQS models as we set it up from totDASqKm field in streamLayer
                    this.drainAreas = new Dictionary<int, double>();
                }
                var manyBasins = streamLayer.GetFeatureCount(1) > 950;
                streamLayer.ResetReading();
                var reach = streamLayer.GetNextFeature();
                while (reach is not null) {
                    link = reach.GetFieldAsInteger(this.linkIndex);
                    dsLink = reach.GetFieldAsInteger(this.dsLinkIndex);
                    wsno = reach.GetFieldAsInteger(this.wsnoIndex);
                    if (lengthIndex < 0 || recalculate) {
                        length = reach.GetGeometryRef().Length();
                    } else {
                        length = reach.GetFieldAsDouble(lengthIndex);
                    }
                    var data = await this.getOgrReachData(reach, demLayer);
                    this.reachesData[link] = data;
                    double drop;
                    if (length == 0) {
                        drop = 0;
                        slope = 0;
                    } else {
                        // don't use TauDEM for drop - affected by burn-in and pit filling
                        // if data and (dropIndex < 0 or recalculate):
                        //     drop = data.upperZ - data.lowerZ
                        // elif dropIndex >= 0:
                        //     drop = attrs[dropIndex]
                        // else:
                        //     drop = 0
                        if (data is not null) {
                            drop = data.upperZ - data.lowerZ;
                        } else {
                            drop = 0;
                        }
                        slope = drop < 0 ? 0 : drop / length;
                    }
                    dsNode = dsNodeIndex >= 0 ? reach.GetFieldAsInteger(dsNodeIndex) : -1;
                    this.linkToBasin[link] = wsno;
                    this.basinToLink[wsno] = link;
                    maxLink = Math.Max(maxLink, link);
                    // if the length is zero there will not (for TauDEM) be an entry in the wshed shapefile
                    // unless the zero length is caused by something in the inlets/outlets file
                    // but with HUC models there are empty basins for the zero length links inserted for inlets, and these have positive DSNODEIDs
                    if (!useGridModel && length == 0 && (dsNode < 0 || this.isHUC || this.isHAWQS)) {
                        this.emptyBasins.Add(wsno);
                    }
                    this.downLinks[link] = dsLink;
                    this.streamLengths[link] = length;
                    this.streamSlopes[link] = slope;
                    if (dsNode >= 0) {
                        dsNodeToLink[dsNode] = link;
                    }
                    if (dsLink >= 0 && !((this.isHUC || this.isHAWQS) && link >= Topology._HUCPointId)) {
                        // keep HUC links out of us map
                        if (!useGridModel && !manyBasins) {
                            if (us.ContainsKey(dsLink) && us[dsLink] is not null) {
                                us[dsLink].Add(link);
                            } else {
                                us[dsLink] = new List<int> { link };
                            }
                            // check we haven't just made the us relation circular
                            if (existing) {
                                // probably safe to assume TauDEM won't create a circular network
                                if (Topology.reachable(dsLink, new List<int> { link }, us)) {
                                    Utils.error(String.Format("Circular drainage network from link {0}", dsLink), this.isBatch);
                                    // remove link from upstream of dsLink
                                    us[dsLink].Remove(link);
                                }
                            }
                        }
                    }
                    if (totDAIndex >= 0) {
                        this.drainAreas[link] = reach.GetFieldAsDouble(totDAIndex) * 1000000.0;
                    }
                    reach = streamLayer.GetNextFeature();
                }
                // create drainAreas here for non-HUC models as we now have maxLink value to size the array
                if (totDAIndex < 0) {
                    for (int i = 0; i <= maxLink; i++) {
                        this.drainAreas[i] = 0;
                    }
                }
                var time2 = DateTime.Now;
                Utils.loginfo(String.Format("Topology setup took {0} seconds", Convert.ToInt32(time2.Subtract(time1).TotalSeconds)));
                //Utils.loginfo('Finished setting tables from streams shapefile')

                using (var wshedDs = Ogr.Open(wshedFile, 1)) {
                    var wshedLayer = wshedDs.GetLayerByIndex(0);
                    var subbasinIndex = this.getIndex(wshedLayer, Topology._SUBBASIN, ignoreMissing: ignoreWithGridOrExisting);
                    var polyIndex = this.getIndex(wshedLayer, Topology._POLYGONID, ignoreMissing: ignoreError);
                    if (polyIndex < 0) {
                        Utils.loginfo(String.Format("No {0} field in watershed layer", Topology._POLYGONID));
                        return false;
                    }
                    var areaIndex = this.getIndex(wshedLayer, Topology._AREA, ignoreMissing: ignoreWithGridOrExisting);
                    var wshedOutletIndex = this.getIndex(wshedLayer, Topology._OUTLET, ignoreMissing: true);
                    if (wshedOutletIndex >= 0) {
                        this.catchmentOutlets.Clear();
                    }
                    wshedLayer.ResetReading();
                    var polygon = wshedLayer.GetNextFeature();
                    while (polygon is not null) {
                        basin = polygon.GetFieldAsInteger(polyIndex);
                        if (wshedOutletIndex >= 0) {
                            this.catchmentOutlets[basin] = polygon.GetFieldAsInteger(wshedOutletIndex);
                        }
                        if (areaIndex < 0 || recalculate) {
                            area = polygon.GetGeometryRef().Area();
                        } else {
                            area = polygon.GetFieldAsDouble(areaIndex);
                        }
                        if (useGridModel && this.gridRows == 0) {
                            // all areas the same, so just use first
                            // gridArea = area # not used
                            this.gridRows = Convert.ToInt32(Math.Round(polygon.GetGeometryRef().Length() / (4 * this.dy)));
                            Utils.loginfo(String.Format("Using {0} DEM grid rows per grid cell", this.gridRows));
                        }
                        this.basinAreas[basin] = area;
                        if (manyBasins) {
                            // initialise drainAreas
                            link = this.basinToLink[basin];
                            this.drainAreas[link] = area;
                        }
                        // belt and braces for empty basins: already stored as empty if length of link is zero
                        // in which case TauDEM makes no wshed feature, 
                        // but there may be such a feature in an existing watershed shapefile
                        if (area == 0) {
                            this.emptyBasins.Add(basin);
                        }
                        if (existing) {
                            // need to set centroids
                            var centroid = polygon.GetGeometryRef().Centroid();
                            double[] argout = new double[3];
                            centroid.GetPoint(0, argout);
                            this.basinCentroids[basin] = new Coordinate2D(argout[0], argout[1]);
                        }
                        polygon = wshedLayer.GetNextFeature();
                    }
                    if (File.Exists(outletFile)) {
                        using (var outletDs = Ogr.Open(outletFile, 0)) {
                            var outletLayer = outletDs.GetLayerByIndex(0);
                            int idIndex = -1;
                            int inletIndex = -1;
                            int ptSourceIndex = -1;
                            int resIndex = -1;
                            idIndex = this.getIndex(outletLayer, Topology._ID, ignoreMissing: ignoreError);
                            if (idIndex < 0) {
                                Utils.loginfo("No ID field in outlets layer");
                                return false;
                            }
                            inletIndex = this.getIndex(outletLayer, Topology._INLET, ignoreMissing: ignoreError);
                            if (inletIndex < 0) {
                                Utils.loginfo("No INLET field in outlets layer");
                                return false;
                            }
                            ptSourceIndex = this.getIndex(outletLayer, Topology._PTSOURCE, ignoreMissing: ignoreError);
                            if (ptSourceIndex < 0) {
                                Utils.loginfo("No PTSOURCE field in outlets layer");
                                return false;
                            }
                            resIndex = this.getIndex(outletLayer, Topology._RES, ignoreMissing: ignoreError);
                            if (resIndex < 0) {
                                Utils.loginfo("No RES field in outlets layer");
                                return false;
                            }
                            outletLayer.ResetReading();
                            var point = outletLayer.GetNextFeature();
                            while (point is not null) {
                                if (dsNodeIndex >= 0) {
                                    // ignore HUC reservoir and pond points: only for display
                                    if (this.isHUC && point.GetFieldAsInteger(resIndex) > 0) {
                                        continue;
                                    }
                                    dsNode = point.GetFieldAsInteger(idIndex);
                                    if (!dsNodeToLink.ContainsKey(dsNode)) {
                                        if (reportErrors) {
                                            Utils.error(String.Format("ID value {0} from inlets/outlets file {1} not found as DSNODEID in stream reaches file {2}", dsNode, outletFile, streamFile), this.isBatch);
                                        }
                                    } else {
                                        link = dsNodeToLink[dsNode];
                                        // an outlet upstream from but too close to a junction can cause a basin 
                                        // to not be in the basins file and hence the wshed shapefile, so we check for this
                                        // The link with a wsno number missing from the wshed is downstream from this link
                                        if (!useGridModel) {
                                            dsLink = this.downLinks[link];
                                            if (dsLink >= 0) {
                                                var dsBasin = this.linkToBasin[dsLink];
                                                if (!this.basinAreas.ContainsKey(dsBasin)) {
                                                    // map derived from wshed shapefile
                                                    if (reportErrors) {
                                                        Utils.error(String.Format("ID value {0} from inlets/outlets file {1} has not generated a subbasin: probably too close to a stream junction.  Please move or remove.", dsNode, outletFile), this.isBatch);
                                                    }
                                                    // try to avoid knock-on errors
                                                    this.emptyBasins.Add(dsBasin);
                                                }
                                            }
                                        }
                                        var isInlet = point.GetFieldAsInteger(inletIndex) == 1;
                                        var isPtSource = point.GetFieldAsInteger(ptSourceIndex) == 1;
                                        var isReservoir = point.GetFieldAsInteger(resIndex) == 1;
                                        if (isInlet) {
                                            if (isPtSource) {
                                                this.ptSrcLinks[link] = dsNode;
                                            } else if (this.isHUC || this.isHAWQS) {
                                                // in HUC models inlets are allowed which do not split streams
                                                // so use only the zero length stream added to support the inlet
                                                this.inletLinks.Add(link);
                                            } else {
                                                // inlet links need to be associated with their downstream links
                                                this.inletLinks.Add(this.downLinks[link]);
                                            }
                                        } else if (isReservoir) {
                                            this.reservoirLinks[link] = dsNode;
                                        } else {
                                            this.outletLinks.Add(link);
                                        }
                                    }
                                }
                                point = outletLayer.GetNextFeature();
                            }
                        }
                    }
                    if (!useGridModel && !manyBasins) {
                        foreach (var linki in this.inletLinks) {
                            this.addUpstreamLinks(linki, us);
                        }
                        Utils.loginfo(String.Format("Outlet links: {0}", Utils.DisplaySet(this.outletLinks)));
                        Utils.loginfo(String.Format("Inlet links: {0}", Utils.DisplaySet(this.inletLinks)));
                    }
                    if (File.Exists(extraOutletFile)) {
                        using (var extraOutletDs = Ogr.Open(extraOutletFile, 0)) {
                            var extraOutletLayer = extraOutletDs.GetLayerByIndex(0);
                            int extraIdIndex = -1;
                            int extraPtSourceIndex = -1;
                            int extraResIndex = -1;
                            int extraBasinIndex = -1;
                            extraIdIndex = this.getIndex(extraOutletLayer, Topology._ID, ignoreMissing: ignoreError);
                            if (extraIdIndex < 0) {
                                Utils.loginfo("No ID field in extra outlets layer");
                                return false;
                            }
                            extraPtSourceIndex = this.getIndex(extraOutletLayer, Topology._PTSOURCE, ignoreMissing: ignoreError);
                            if (extraPtSourceIndex < 0) {
                                Utils.loginfo("No PTSOURCE field in extra outlets layer");
                                return false;
                            }
                            extraResIndex = this.getIndex(extraOutletLayer, Topology._RES, ignoreMissing: ignoreError);
                            if (extraResIndex < 0) {
                                Utils.loginfo("No RES field in extra outlets layer");
                                return false;
                            }
                            extraBasinIndex = this.getIndex(extraOutletLayer, Topology._SUBBASIN, ignoreMissing: ignoreError);
                            if (extraBasinIndex < 0) {
                                Utils.loginfo("No SUBBASIN field in extra outlets layer");
                                return false;
                            }
                            // add any extra reservoirs and point sources
                            var point = extraOutletLayer.GetNextFeature();
                            while (point is not null) {
                                int pointId = point.GetFieldAsInteger(extraIdIndex);
                                basin = point.GetFieldAsInteger(extraBasinIndex);
                                link = this.basinToLink[basin];
                                if (!this.emptyBasins.Contains(basin) && !this.upstreamFromInlets.Contains(link)) {
                                    if (point.GetFieldAsInteger(extraResIndex) == 1) {
                                        this.reservoirLinks[link] = pointId;
                                    }
                                    if (point.GetFieldAsInteger(extraPtSourceIndex) == 1) {
                                        this.ptSrcLinks[link] = pointId;
                                    }
                                }
                                point = extraOutletLayer.GetNextFeature();
                            }
                        }
                    }
                    if (!useGridModel) {
                        Utils.loginfo(String.Format("Reservoir links: {0}", Utils.DisplaySet(this.reservoirLinks.Keys)));
                        Utils.loginfo(String.Format("Coordinate2D source links: {0}", Utils.DisplaySet(this.ptSrcLinks.Keys)));
                        Utils.loginfo(String.Format("Empty basins: {0}", Utils.DisplaySet(this.emptyBasins)));
                    }
                    var time4 = DateTime.Now;
                    // set drainAreas
                    if (totDAIndex < 0) {
                        if (useGridModel) {
                            this.setGridDrainageAreas(streamLayer, wshedLayer, maxLink);
                        } else if (manyBasins) {
                            this.setManyDrainageAreas(maxLink);
                        } else {
                            this.setDrainageAreas(us);
                        }
                    }
                    var time5 = DateTime.Now;
                    Utils.loginfo(String.Format("Topology drainage took {0} seconds", Convert.ToInt32(time5.Subtract(time4).TotalSeconds)));
                    if (useGridModel) {
                        // lower limit on drainage area for outlets to be included
                        // 1.5 multiplier guards against rounding errors:
                        // ensures that any cell with drainage area exceeding this cannot be a singleton
                        var minDrainArea = this.dx * this.dy * this.gridRows * this.gridRows * 1.5;
                        // Create SWAT basin numbers for grid
                        // we ignore edge basins which are outlets with nothing upstream, ie they are single cell outlets,
                        // by counting only those which have a downstream link or have an upstream link
                        SWATBasin = 0;
                        foreach (var kvp in this.linkToBasin) {
                            var klink = kvp.Key;
                            var kbasin = kvp.Value;
                            dsLink = this.downLinks[klink];
                            if (dsLink >= 0 || this.drainAreas[klink] > minDrainArea) {
                                if (this.catchmentLargeEnoughForTNC(kbasin)) {
                                    SWATBasin += 1;
                                    this.basinToSWATBasin[kbasin] = SWATBasin;
                                }
                            }
                        }
                    } else {
                        // if not grid, try existing subbasin numbers as SWAT basin numbers
                        var ok = subbasinIndex >= 0 && this.trySubbasinAsSWATBasin(wshedLayer, polyIndex, subbasinIndex);
                        if (!ok) {
                            // failed attempt may have put data in these, so clear them
                            this.basinToSWATBasin.Clear();
                            this.SWATBasinToBasin.Clear();
                            // create SWAT basin numbers
                            SWATBasin = 0;
                            foreach (var kvp in this.linkToBasin) {
                                var vlink = kvp.Key;
                                var vbasin = kvp.Value;
                                if (!this.emptyBasins.Contains(vbasin) && !this.upstreamFromInlets.Contains(vlink)) {
                                    SWATBasin += 1;
                                    this.basinToSWATBasin[vbasin] = SWATBasin;
                                    this.SWATBasinToBasin[SWATBasin] = vbasin;
                                }
                            }
                        }
                    }
                    // put SWAT Basin numbers in subbasin field of watershed shapefile
                    if (subbasinIndex < 0) {
                        // need to add subbasin field
                        var defn = wshedLayer.GetLayerDefn();
                        var subField = new OSGeo.OGR.FieldDefn(Topology._SUBBASIN, OSGeo.OGR.FieldType.OFTInteger);
                        wshedLayer.CreateField(subField, 1);
                    }
                    wshedLayer.ResetReading();
                    polygon = wshedLayer.GetNextFeature();
                    while (polygon != null) {
                        basin = polygon.GetFieldAsInteger(polyIndex);
                        var subbasin = 0;
                        if (this.basinToSWATBasin.TryGetValue(basin, out subbasin)) {
                            polygon.SetField(Topology._SUBBASIN, subbasin);
                        } else {
                            polygon.SetField(Topology._SUBBASIN, 0);
                        }
                        wshedLayer.SetFeature(polygon);
                        polygon = wshedLayer.GetNextFeature();
                    }
                }
            }
            return true;
        }
        
        // If not for TNC, return True.  Else return True if catchment size (drain area at outlet) no less than TNCCatchmentThreshold (in sq km).
        public virtual bool catchmentLargeEnoughForTNC(int basin) {
            if (!this.forTNC) {
                return true;
            }
            var outlet = this.catchmentOutlets[basin];
            if (outlet < 0) {
                Console.WriteLine("Polygon {0} is not in any catchment", basin);
                return false;
            }
            var outletLink = this.basinToLink[outlet];
            var catchmentArea = this.drainAreas[outletLink];
            return catchmentArea / 1000000.0 >= this.TNCCatchmentThreshold;
        }
        
        // Return true if link is in links or reachable from an item in links via the one-many relation us.
        public static bool reachable(int link, List<int> links, Dictionary<int, List<int>> us) {
            if (links.Contains(link)) {
                return true;
            }
            foreach (var nxt in links) {
                var ups = new List<int>();
                if (us.TryGetValue(nxt, out ups)) {
                    if (Topology.reachable(link, ups, us))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        // Add to upstreamFromInlets the links upstream from link.
        public void addUpstreamLinks(int link, Dictionary<int, List<int>> us) {
            var ups = new List<int>();
            if (us.TryGetValue(link, out ups)) {
                foreach (var up in ups) {
                    this.upstreamFromInlets.Add(up);
                    this.addUpstreamLinks(up, us);
                }
            }
        }
        
        // Calculate and save grid drain areas in sq km.
        public void setGridDrainageAreas(OSGeo.OGR.Layer streamLayer, OSGeo.OGR.Layer wshedLayer, int maxLink) {
            int dsLink;
            int link;
            var gridArea = this.dx * this.dy * this.gridRows * this.gridRows;
            foreach (var k in this.drainAreas.Keys) this.drainAreas[k] = gridArea;
            // number of incoming links for each link
            var incount = new int[maxLink + 1];
            for (int i = 0; i <= maxLink; i++) incount[i] = 0;
            foreach (var dsl in this.downLinks.Values) {
                if (dsl >= 0) {
                    incount[dsl] += 1;
                }
            }
            // queue contains all links whose drainage areas have been calculated 
            // i.e. will not increase and can be propagated
            var elements = (from lnk in Enumerable.Range(0, maxLink + 1)
                where incount[lnk] == 0
                select lnk).ToList();
            var queue = new Queue<int>(elements);
            if (queue.TryDequeue(out link)) {
                dsLink = this.downLinks[link];
                if (dsLink >= 0) {
                    this.drainAreas[dsLink] += this.drainAreas[link];
                    incount[dsLink] -= 1;
                    if (incount[dsLink] == 0) {
                        queue.Enqueue(dsLink);
                    }
                }
            }
            // incount values should now all be zero
            var remainder = (from lnk in Enumerable.Range(0, maxLink + 1)
                where incount[lnk] > 0
                select lnk).ToList();
            if (remainder.Count > 0) {
                // Utils.information(u'Drainage areas incomplete.  There is a circularity in links {0}', remainder), self.isBatch)
                // remainder may contain a number of circles.
                var rings = new List<List<int>>();
                var nextRing = new List<int>();
                link = remainder[0];
                remainder.RemoveAt(0);
                while (true) {
                    nextRing.Add(link);
                    dsLink = this.downLinks[link];
                    if (nextRing.Contains(dsLink)) {
                        // complete the ring
                        nextRing.Add(dsLink);
                        rings.Add(nextRing);
                        if (remainder.Count > 0) {
                            nextRing = new List<int>();
                            link = remainder[0];
                            remainder.RemoveAt(0);
                        } else {
                            break;
                        }
                    } else {
                        // continue
                        remainder.Remove(dsLink);
                        link = dsLink;
                    }
                }
                var numRings = rings.Count;
                if (numRings > 0) {
                    Utils.information(String.Format("Drainage areas incomplete.  There are {0} circularities.  Will try to remove them.  See the ArcSWAT log for details", numRings), this.isBatch);
                    foreach (var ring in rings) {
                        Utils.loginfo(String.Format("Circularity in links {0}", ring));
                        // fix the circularity by making the largest drainage link an exit in the downChannels map
                        double maxDrainage = 0;
                        maxLink = -1;
                        foreach (var lnk in ring) {
                            var drainage = this.drainAreas[lnk];
                            if (drainage > maxDrainage) {
                                maxLink = lnk;
                                maxDrainage = drainage;
                            }
                        }
                        if (maxLink < 0) {
                            Utils.error(String.Format("Failed to find link with largest drainage in circle {0}", ring), this.isBatch);
                        } else {
                            this.downLinks[maxLink] = -1;
                            streamLayer.ResetReading();
                            streamLayer.SetAttributeFilter(String.Format("{0} = {1}", Topology._LINKNO, maxLink));
                            var reach = streamLayer.GetNextFeature();
                            streamLayer.SetAttributeFilter(null);
                            reach.SetField(Topology._DSLINKNO, -1);
                            var basin = this.linkToBasin[maxLink];
                            wshedLayer.ResetReading();
                            wshedLayer.SetAttributeFilter(String.Format("{0} = {1}", Topology._POLYGONID, basin));
                            var polygon = wshedLayer.GetNextFeature();
                            wshedLayer.SetAttributeFilter(null);
                            polygon.SetField(Topology._DOWNID, -1);
                            Utils.loginfo(String.Format("Link {0} and polygon {1} made into an exit", maxLink, basin));
                            Console.WriteLine("Link {0} and polygon {1} made into an exit", maxLink, basin);
                        }
                    }
                }
            }
        }
        
        // Calculate and save subbasin drain areas in sq km.
        public void setManyDrainageAreas(int maxLink) {
            // number of incoming links for each link
            var incount = new int[maxLink + 1];
            for (int i = 0; i <= maxLink; i++) incount[i] = 0;
            foreach (var dsLink in this.downLinks.Values) {
                if (dsLink >= 0) {
                    incount[dsLink] += 1;
                }
            }
            // queue contains all links whose drainage areas have been calculated 
            // i.e. will not increase and can be propagated
            var elements = (from lnk in Enumerable.Range(0, maxLink + 1)
                where incount[lnk] == 0
                select lnk).ToList();
            int link;
            var queue = new Queue<int>(elements);
            if (queue.TryDequeue(out link))
            {
                var dsLink = this.downLinks[link];
                if (dsLink >= 0) {
                    this.drainAreas[dsLink] += this.drainAreas[link];
                    incount[dsLink] -= 1;
                    if (incount[dsLink] == 0) {
                        queue.Enqueue(dsLink);
                    }
                }
            }
            // incount values should now all be zero
            var remainder = (from lnk in Enumerable.Range(0, maxLink + 1)
                where incount[lnk] > 0
                select lnk).ToList();
            if (remainder.Count > 0) {
                Utils.error(String.Format("Drainage areas incomplete.  There is a circularity in links {0}", remainder), this.isBatch);
            }
        }
        
        // Calculate and save drainAreas.
        public void setDrainageAreas(Dictionary<int, List<int>> us) {
            foreach (var kvp in this.linkToBasin) {
                this.setLinkDrainageArea(kvp.Key, kvp.Value, us);
            }
        }
        
        // Calculate and save drainArea for link.
        public void setLinkDrainageArea(int link, int basin, Dictionary<int, List<int>> us) {
            if (this.upstreamFromInlets.Contains(link)) {
                this.drainAreas[link] = 0;
                return;
            }
            if (this.drainAreas[link] > 0) {
                // already done in calculating one further downstream
                return;
            }
            var ownArea = 0.0; 
            if (!this.basinAreas.TryGetValue(basin, out ownArea)) { ownArea = 0.0; }
            var upsArea = 0.0;
            var ups = new List<int>();
            us.TryGetValue(link, out ups);
            if (ups is not null) {
                foreach (var up in ups) {
                    this.setLinkDrainageArea(up, this.linkToBasin[up], us);
                    upsArea += this.drainAreas[up];
                }
            }
            this.drainAreas[link] = ownArea + upsArea;
        }

        // Generate ReachData record for reach.
        public async Task<ReachData> getReachData(Feature reach, RasterLayer demLayer) {
            double minElev;
            double maxElev;
            MapPoint pFinish = null;
            MapPoint pStart = null;
            if (this.isHUC || this.isHAWQS) {
                int wsno = Convert.ToInt32(reach[this.wsnoIndex]);
                pStart = MapPointBuilderEx.CreateMapPoint(this.nearsources[wsno].X, this.nearsources[wsno].Y);
                pFinish = MapPointBuilderEx.CreateMapPoint(this.outlets[wsno].X, this.outlets[wsno].Y);
            } else {
                await QueuedTask.Run(() => {
                    var geometry = reach.GetShape();
                    // get the geometry as a point collection
                    var pointCol = ((Multipart)geometry).Points;
                    pStart = pointCol.First();
                    pFinish = pointCol.Last();
                });
            }
            Raster demRas;
            double startVal = 0;
            double finishVal = 0;
            await QueuedTask.Run(() => {
                demRas = demLayer.GetRaster();
                startVal = Topology.valueAtPoint(pStart, demRas);
                finishVal = Topology.valueAtPoint(pFinish, demRas);
            });
            if (startVal == this.demNodata) {
                if (finishVal == this.demNodata) {
                    if (this.isHUC || this.isHAWQS) {
                        // allow for streams outside DEM
                        startVal = 0;
                        finishVal = 0;
                    } else {
                        Utils.error(String.Format("Stream link {4} ({0},{1}) to ({2},{3}) seems to be outside DEM", pStart.X, pStart.Y, pFinish.X, pFinish.Y, Convert.ToInt32(reach[this.linkIndex])), this.isBatch);
                        return null;
                    }
                } else {
                    startVal = finishVal;
                }
            } else if (finishVal == this.demNodata) {
                finishVal = startVal;
            }
            if (this.outletAtStart) {
                maxElev = finishVal * this.verticalFactor;
                minElev = startVal * this.verticalFactor;
                return new ReachData(pFinish.X, pFinish.Y, maxElev, pStart.X, pStart.Y, minElev);
            } else {
                minElev = finishVal * this.verticalFactor;
                maxElev = startVal * this.verticalFactor;
                return new ReachData(pStart.X, pStart.Y, maxElev, pFinish.X, pFinish.Y, minElev);
            }
        }

        // Generate ReachData record for OGR reach.
        public async Task<ReachData> getOgrReachData(OSGeo.OGR.Feature reach, RasterLayer demLayer) {
            double minElev;
            double maxElev;
            double[] pFinish = new double[3];
            double[] pStart = new double[3];
            if (this.isHUC || this.isHAWQS) {
                int wsno = reach.GetFieldAsInteger(this.wsnoIndex);
                pStart[0] = this.nearsources[wsno].X;
                pStart[1] = this.nearsources[wsno].Y;
                pFinish[0] = this.outlets[wsno].X;
                pFinish[1] = this.outlets[wsno].Y;
            } else {
                var firstLine = Topology.reachOgrFirstLine(reach, this.dx, this.dy);
                if (firstLine is null || firstLine.GetPointCount() < 1) {
                    Utils.error("It looks like your stream shapefile does not obey the single direction rule, that all reaches are either upstream or downstream.", this.isBatch);
                    return null;
                }
                var lastLine = Topology.reachOgrLastLine(reach, this.dx, this.dy);
                if (lastLine is null || lastLine.GetPointCount() < 1) {
                    Utils.error("It looks like your stream shapefile does not obey the single direction rule, that all reaches are either upstream or downstream.", this.isBatch);
                    return null;
                }
                firstLine.GetPoint(0, pStart);
                lastLine.GetPoint(lastLine.GetPointCount() - 1, pFinish);
            }
            Raster demRas;
            double startVal = 0;
            double finishVal = 0;
            await QueuedTask.Run(() => {
                demRas = demLayer.GetRaster();
                startVal = Topology.valueAtPoint(pStart, demRas);
                finishVal = Topology.valueAtPoint(pFinish, demRas);
            });
            if (startVal == this.demNodata) {
                if (finishVal == this.demNodata) {
                    if (this.isHUC || this.isHAWQS) {
                        // allow for streams outside DEM
                        startVal = 0;
                        finishVal = 0;
                    } else {
                        Utils.error(String.Format("Stream link {4} ({0},{1}) to ({2},{3}) seems to be outside DEM", pStart[0], pStart[1], pFinish[0], pFinish[1], reach.GetFieldAsInteger(this.linkIndex)), this.isBatch);
                        return null;
                    }
                } else {
                    startVal = finishVal;
                }
            } else if (finishVal == this.demNodata) {
                finishVal = startVal;
            }
            if (this.outletAtStart) {
                maxElev = finishVal * this.verticalFactor;
                minElev = startVal * this.verticalFactor;
                return new ReachData(pFinish[0], pFinish[1], maxElev, pStart[0], pStart[1], minElev);
            } else {
                minElev = finishVal * this.verticalFactor;
                maxElev = startVal * this.verticalFactor;
                return new ReachData(pStart[0], pStart[1], maxElev, pFinish[0], pFinish[1], minElev);
            }
        }
        
        // Length of reach assuming it is a single straight line.
        public static double gridReachLength(ReachData data) {
            var dx = data.upperX - data.lowerX;
            var dy = data.upperY - data.lowerY;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        // Return true if the subbasin field values can be used as SWAT basin numbers.
        //         
        //         The subbasin numbers, if any, can be used if those for non-empty basins downstream from inlets 
        //         run from 1 to N, where  N is the number of non-empty subbasins not upstream from inlets,
        //         and for empty or upstream from inlet subbasins are 0.
        //         Also populate basinToSWATBasin and SWATBasinToBasin.
        //         
        public bool trySubbasinAsSWATBasin(OSGeo.OGR.Layer wshedLayer, int polyIndex, int subIndex) {
            //Debug.Assert(polyIndex >= 0 && subIndex >= 0 && this.basinToSWATBasin.Count == 0 && this.SWATBasinToBasin.Count == 0);
            int numShapes = 0;
            int mmin = int.MaxValue;
            int mmax = 0;
            int ignoreCount = 0;
            wshedLayer.ResetReading();
            var polygon = wshedLayer.GetNextFeature();
            while (polygon is not null) {
                numShapes += 1;
                int nxt = polygon.GetFieldAsInteger(subIndex);
                int basin = polygon.GetFieldAsInteger(polyIndex);
                if (!this.basinToLink.ContainsKey(basin)) {
                    return false;
                }
                var link = this.basinToLink[basin];
                if (!this.upstreamFromInlets.Contains(link) && !this.emptyBasins.Contains(basin)) {
                    if (nxt > 0 && !this.basinToSWATBasin.ContainsKey(basin) && !this.SWATBasinToBasin.ContainsKey(nxt)) {
                        if (nxt < mmin) {
                            mmin = nxt;
                        }
                        if (nxt > mmax) {
                            mmax = nxt;
                        }
                        this.basinToSWATBasin[basin] = nxt;
                        this.SWATBasinToBasin[nxt] = basin;
                    } else {
                        return false;
                    }
                } else if (nxt == 0) {
                    // can be ignored
                    ignoreCount += 1;
                } else {
                    return false;
                }
                polygon = wshedLayer.GetNextFeature();
            }
            var expectedCount = numShapes - ignoreCount;
            return mmin == 1 && mmax == expectedCount && this.basinToSWATBasin.Count == expectedCount;
        }
        
        // Return the nearest point on a stream segment to the input point.
        public static Coordinate2D? snapPointToReach(FeatureLayer streamLayer, Coordinate2D point, double threshold, bool isBatch) {
            Coordinate2D p = Topology.nearestStreamPoint(streamLayer, point);
            // check p is sufficiently near point
            if (Topology.distanceMeasure(p, point) <= threshold * threshold) {
                return p;
            } else {
                Utils.error(String.Format("Cannot snap point ({0:F2}, {1:F2}) to stream network within threshold {2}", point.X, point.Y, threshold), isBatch);
                return null;
            }
        }
        
        // Find nearest point in the stream layer, i.e. one segment tip is the nearest segment tip and the vertical distance
        // to the next segment is greater than the vertical distance to this one.
        // The next segment is one with an increased index if the nearest point is an end,
        // and the one with a decreased index if the nearest point is a start.
        //         
        public static Coordinate2D nearestStreamPoint(FeatureLayer streamLayer, Coordinate2D point) {
            int bestSegmentIndex = -1;
            ReadOnlySegmentCollection bestLine = null;
            bool isStart = true;
            var minMeasure = double.PositiveInfinity;
            double measure;
            using (RowCursor cursor = streamLayer.Search(null)) {
                while (cursor.MoveNext()) {
                    var reach = cursor.Current;
                    var geometry = ((Feature)reach).GetShape() as Polyline;
                    var parts = geometry.Parts;
                    foreach (var line in parts) {
                        foreach (var j in Enumerable.Range(0, line.Count)) {
                            var linej = line[j];
                            measure = Topology.distanceMeasure(linej.StartCoordinate, point);
                            if (measure < minMeasure) {
                                minMeasure = measure;
                                bestSegmentIndex = j;
                                bestLine = line;
                                isStart = true;
                            }
                            measure = Topology.distanceMeasure(linej.EndCoordinate, point);
                            if (measure < minMeasure) {
                                minMeasure = measure;
                                bestSegmentIndex = j;
                                bestLine = line;
                                isStart = false;
                            }
                        }
                    }
                }
                // have best line and best segment tip.  There are two candidates: the intercepts on this segment and the next (if any).
                if (isStart) {
                    if (bestSegmentIndex == 0) return getIntercept(bestLine[0].StartCoordinate, bestLine[0].EndCoordinate, point);
                    var p1 = getIntercept(bestLine[bestSegmentIndex].StartCoordinate, bestLine[bestSegmentIndex].EndCoordinate, point);
                    var p2 = getIntercept(bestLine[bestSegmentIndex - 1].EndCoordinate, bestLine[bestSegmentIndex - 1].StartCoordinate, point);
                    return nearer(p1, p2, point);
                } else {
                    if (bestSegmentIndex == bestLine.Count - 1) return getIntercept(bestLine[bestSegmentIndex].EndCoordinate, bestLine[bestSegmentIndex].StartCoordinate, point);
                    var p1 = getIntercept(bestLine[bestSegmentIndex].EndCoordinate, bestLine[bestSegmentIndex].StartCoordinate, point);
                    var p2 = getIntercept(bestLine[bestSegmentIndex + 1].StartCoordinate, bestLine[bestSegmentIndex + 1].EndCoordinate, point);
                    return nearer(p1, p2, point);
                }
            }
        }
        
        //// Get points on segments on either side of pointIndex where 
        ////         vertical from point meets the segment.
        ////         
        //[staticmethod]
        //public static object intercepts(List<object> line, int pointIndex, object point) {
        //    Debug.Assert(Enumerable.Range(0, line.Count).Contains(pointIndex));
        //    // first try above pointIndex
        //    if (pointIndex == line.Count - 1) {
        //        // We are at the upper end - no upper segment.  
        //        // Return just this point to avoid a tiny subbasin.
        //        return (line[pointIndex], line[pointIndex]);
        //    } else {
        //        var upper = Topology.getIntercept(line[pointIndex], line[pointIndex + 1], point);
        //    }
        //    if (pointIndex == 0) {
        //        // We are at the lower end - no lower segment.  
        //        // Return just this point to avoid a tiny subbasin.
        //        return (line[0], line[0]);
        //    } else {
        //        var lower = Topology.getIntercept(line[pointIndex], line[pointIndex - 1], point);
        //    }
        //    return (lower, upper);
        //}
        
        // Return point in segment with tips p1 and p2 where 
        // vertical from p intercepts it, or p1 if there is no intercept.
        // p1 is known to be nearer to P than p2.
        //         
        public static Coordinate2D getIntercept(Coordinate2D p1, Coordinate2D p2, Coordinate2D p) {
            var x1 = p1.X;
            var x2 = p2.X;
            var xp = p.X;
            var y1 = p1.Y;
            var y2 = p2.Y;
            var yp = p.Y;
            var X = x1 - x2;
            var Y = y1 - y2;
            //Debug.Assert(!(X == 0 && Y == 0));
            var prop = (X * (x1 - xp) + Y * (y1 - yp)) / (X * X + Y * Y);
            if (prop < 0) {
                // intercept is off the line beyond p1.
                // Could check for prop > 1, which means 
                // intercept is off the line beyond p2, but we know p is nearer to p1
                // so return the nearest
                return p1;
            } else {
                //Debug.Assert(0 <= prop && prop < 1);
                return new Coordinate2D(x1 - prop * X, y1 - prop * Y);
            }
        }

        // Return the nearer of p1 and p2 to p.
        public static Coordinate2D nearer(Coordinate2D p1, Coordinate2D p2, Coordinate2D p) {
            if (Topology.distanceMeasure(p1, p) < Topology.distanceMeasure(p2, p)) {
                return p1;
            } else {
                return p2;
            }
        }

        // Return square of distance between p1 and p2.
        public static double distanceMeasure(Coordinate2D p1, Coordinate2D p2) {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }
        
        // Write the monitoring point table in the project database.
        public void writeMonitoringPointTable(RasterLayer demLayer, FeatureLayer streamLayer) {
            this.db.connect();
            if (this.db.conn is null) {
                return;
            }
            var table = "MonitoringPoint";
            if (this.isHUC || this.isHAWQS || this.forTNC) {
                var sql0 = "DROP TABLE IF EXISTS MonitoringPoint";
                this.db.execNonQuery(sql0);
                var sql1 = Topology._MONITORINGPOINTCREATESQL;
                this.db.execNonQuery(sql1);
            } else {
                var clearSQL = "DELETE FROM " + table;
                this.db.execNonQuery(clearSQL);
            }
            this.MonitoringPointFid = 1;
            var time1 = DateTime.Now;
            // Add outlets from subbasins
            foreach (var link in this.linkToBasin.Keys) {
                if (this.outletLinks.Contains(link)) {
                    continue;
                }
                if (this.upstreamFromInlets.Contains(link)) {
                    continue;
                }
                var basin = this.linkToBasin[link];
                if (!this.basinToSWATBasin.ContainsKey(basin)) {
                    continue;
                }
                var data = this.reachesData[link];
                this.addMonitoringPoint(link, data, 0, "L");
            }
            // Add outlets
            foreach (var link in this.outletLinks) {
                // omit basins upstream from inlets
                if (this.upstreamFromInlets.Contains(link)) {
                    continue;
                }
                var basin = this.linkToBasin[link];
                if (!this.basinToSWATBasin.ContainsKey(basin)) {
                    continue;
                }
                var data = this.reachesData[link];
                this.addMonitoringPoint(link, data, 0, "T");
            }
            // Add inlets
            foreach (var link in this.inletLinks) {
                if (this.upstreamFromInlets.Contains(link)) {
                    // shouldn't happen, but users can be stupid
                    continue;
                }
                var data = this.reachesData[link];
                this.addMonitoringPoint(link, data, 0, "W");
            }
            // Add point sources
            foreach (var kvp in this.ptSrcLinks) {
                var link = kvp.Key;
                var pointId = kvp.Value;
                if (this.upstreamFromInlets.Contains(link)) {
                    continue;
                }
                var data = this.reachesData[link];
                this.addMonitoringPoint(link, data, pointId, "P");
            }
            // Add reservoirs
            foreach (var kvp in this.reservoirLinks) {
                var link = kvp.Key;
                var pointId = kvp.Value;
                if (this.upstreamFromInlets.Contains(link)) {
                    continue;
                }
                var data = this.reachesData[link];
                this.addMonitoringPoint(link, data, pointId, "R");
            }
            //// for TNC projects (marked by forTNC True) there should be a CCdams.shp file in the sources directory
            //// and we add these to reservoirLinks and to MonitoringPoint table
            //if (this.forTNC) {
            //    projDir = this.db.projDir;
            //    sourceDir = Utils.join(projDir, "Source");
            //    shapesDir = Utils.join(projDir, "Watershed/Shapes");
            //    damsPattern = Utils.join(sourceDir, "??dams.shp");
            //    dams = null;
            //    foreach (var f in glob.iglob(damsPattern)) {
            //        dams = f;
            //        break;
            //    }
            //    if (dams is not null) {
            //        Processing.initialize();
            //        alg = "native:joinattributesbylocation";
            //        @params = new Dictionary<object, object> {
            //            {
            //                "DISCARD_NONMATCHING",
            //                false},
            //            {
            //                "INPUT",
            //                Utils.join(shapesDir, "grid.shp")},
            //            {
            //                "JOIN",
            //                dams},
            //            {
            //                "JOIN_FIELDS",
            //                new List<string> {
            //                    "GRAND_ID"
            //                }},
            //            {
            //                "METHOD",
            //                1},
            //            {
            //                "OUTPUT",
            //                Utils.join(shapesDir, "grid_res.shp")},
            //            {
            //                "PREDICATE",
            //                new List<int> {
            //                    0
            //                }},
            //            {
            //                "PREFIX",
            //                ""}};
            //        result = processing.run(alg, @params);
            //        gridRes = result["OUTPUT"];
            //        gridResLayer = QgsVectorLayer(gridRes, "grid", "ogr");
            //        subIndex = this.getIndex(gridResLayer, Topology._SUBBASIN);
            //        damIndex = this.getIndex(gridResLayer, "GRAND_ID");
            //        polyIndex = this.getIndex(gridResLayer, Topology._POLYGONID);
            //        foreach (var cell in gridResLayer.getFeatures()) {
            //            res = cell[damIndex];
            //            sub = cell[subIndex];
            //            if (res != NULL && sub > 0) {
            //                resId = Convert.ToInt32(res);
            //                link = cell[polyIndex];
            //                this.reservoirLinks[link] = resId;
            //                data = this.reachesData[link];
            //                this.addMonitoringPoint(curs, demLayer, streamLayer, link, data, resId, "R");
            //            }
            //        }
            //    }
            //}
            var time2 = DateTime.Now;
            Utils.loginfo(String.Format("Writing MonitoringPoint table took {0} seconds", Convert.ToInt32(time2.Subtract(time1).TotalSeconds)));
            //if (this.isHUC || this.isHAWQS || this.forTNC) {
            //    conn.commit();
            //} else {
            //    this.db.hashDbTable(table);
            //}
        }
        
        // Add a point to the MonitoringPoint table.
        public void addMonitoringPoint(
            int link,
            ReachData data,
            int pointId,
            string typ) {
            Coordinate2D pt;
            int basin;
            var table = "MonitoringPoint";
            var POINTID = pointId;
            var HydroID = this.MonitoringPointFid + 400000;
            var OutletID = this.MonitoringPointFid + 100000;
            if ((this.isHUC || this.isHAWQS) && typ == "W") {
                // point is associated with zero length link added for it, which has an empty basin
                // so need to use downstream basin
                var dsLink = this.downLinks[link];
                basin = this.linkToBasin[dsLink];
            } else {
                basin = this.linkToBasin[link];
            }
            // guard against empty basins (included for outlet points)
            var SWATBasin = 0;
            if (!this.basinToSWATBasin.TryGetValue(basin, out SWATBasin)) { SWATBasin = 0; }
            var GRID_CODE = SWATBasin;
            // inlets will be located at the upstream ends of their links
            // since they are attached to their downstream basins
            var isUp = typ == "W" || typ == "I";
            if (data is null) {
                return;
            }
            if (isUp) {
                pt = new Coordinate2D(data.upperX, data.upperY);
            } else {
                pt = new Coordinate2D(data.lowerX, data.lowerY);
            }
            var ptll = this.pointToLatLong(pt);
            var elev = 0;
            var name = "";
            string insert = "(" +
                this.MonitoringPointFid.ToString() + "," +
                "0," +
                POINTID.ToString() + "," +
                GRID_CODE.ToString() + "," +
                pt.X.ToString() + "," +
                pt.Y.ToString() + "," +
                ptll.Y.ToString() + "," +
                ptll.X.ToString() + "," +
                elev.ToString() + "," +
                DBUtils.quote(name) + "," +
                DBUtils.quote(typ) + "," +
                SWATBasin.ToString() + "," +
                HydroID.ToString() + "," +
                OutletID.ToString() + ")";
            this.db.InsertInTable(table, insert);
            this.MonitoringPointFid += 1;
        }

        // 
        //         Write the Reach table in the project database, make riv1.shp in shapes directory, and copy as results template to TablesOut directory.
        //         
        //         Changes the stream layer, so if successful, returns the new one.
        //         
        public async Task<FeatureLayer> writeReachTable(FeatureLayer streamLayer, GlobalVars gv) {
            var riv1File = Utils.join(gv.shapesDir, Parameters._RIV1 + ".shp");
            // make sure no layer as will be overwritten
            await Utils.removeLayer(riv1File);
            Utils.copyShapefile(gv.streamFile, Parameters._RIV1, gv.shapesDir);
            //var riv1Layer = (await Utils.getLayerByFilename(riv1File, FileTypes._REACHES, gv, streamLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            using (var riv1Ds = Ogr.Open(riv1File, 1)) {
                OSGeo.OGR.Driver drv = riv1Ds.GetDriver();
                // get layer - should only be one
                OSGeo.OGR.Layer layer1 = riv1Ds.GetLayerByIndex(0);
                var def = layer1.GetLayerDefn();
                // add Subbasin field unless already has it
                var subIndex = def.GetFieldIndex(Topology._SUBBASIN);
                if (subIndex < 0) {
                    layer1.CreateField(new FieldDefn(Topology._SUBBASIN, OSGeo.OGR.FieldType.OFTInteger), 1);
                }
                // set Subbasin field to SWATBasin
                var riv1FeatureCount = 0;
                var zeroFids = new List<long>();
                layer1.ResetReading();
                OSGeo.OGR.Feature f;
                do {
                    f = layer1.GetNextFeature();
                    if (f != null) {
                        int basin = f.GetFieldAsInteger(Topology._WSNO);
                        int SWATBasin = 0;
                        if (this.basinToSWATBasin.TryGetValue(basin, out SWATBasin)) {
                            if (SWATBasin == 0) {
                                zeroFids.Add(f.GetFID());
                            } else {
                                f.SetField(Topology._SUBBASIN, SWATBasin);
                                layer1.SetFeature(f);
                                riv1FeatureCount++;
                            }
                        } else {
                            zeroFids.Add(f.GetFID());
                        }
                    }
                } while (f != null);
                // remove features with zero SWATBasin
                // done in reverse as FIDs may change as features are removed
                for (int i = zeroFids.Count - 1; i >=0; i--) {
                    layer1.DeleteFeature(zeroFids[i]);
                }
                //var wsnoIdx = this.getIndex(riv1Lyr, Topology._WSNO);
                //await QueuedTask.Run(() => {
                //    using (RowCursor cursor = riv1Layer.Search(null)) {
                //        while (cursor.MoveNext()) {
                //            var row = cursor.Current;
                //            int basin = Convert.ToInt32(row[Topology._WSNO]);
                //            int SWATBasin = 0;
                //            if (this.basinToSWATBasin.TryGetValue(basin, out SWATBasin)) {
                //                row[Topology._SUBBASIN] = SWATBasin;
                //            } else { 
                //                row[Topology._SUBBASIN] = 0; 
                //            }
                //            riv1FeatureCount++;
                //        }
                //    }
                //});
                ////remove layer1 for now 
                //await Utils.removeLayer(riv1File);
                //riv1Layer = null;
                // remove features with zero SWATBasin
                //QueryFilter qf = new QueryFilter() {
                //    WhereClause = String.Format("{0} = 0", Topology._SUBBASIN)
                //};
                //riv1Layer.Select(qf);
                //Utils.removeLocks(riv1File);
                //parms = Geoprocessing.MakeValueArray(riv1File, "NEW_SELECTION", String.Format("{0}=0", Topology._SUBBASIN));
                //Utils.runPython("runMakeSelection.py", parms, gv);
                //parms = Geoprocessing.MakeValueArray(riv1File);
                //Utils.runPython("runDeleteFeatures.py", parms, gv);
                // Add fields from Reach table to riv1File if less than RIV1SUBS1MAX features; otherwise takes too long.
                var addToRiv1 = !gv.useGridModel && riv1FeatureCount <= Parameters._RIV1SUBS1MAX;
                // if we are adding fields we need to
                // 1. remove other fields from riv1
                // 2. copy to make results template
                // and if not we need to 
                // 1. copy to make results template
                // 2. remove other fields from template
                var layer1Def = layer1.GetLayerDefn();
                var todo = layer1Def.GetFieldCount();
                if (addToRiv1) {
                    //Utils.removeLocks(riv1File);
                    //parms = Geoprocessing.MakeValueArray(riv1File, Topology._SUBBASIN);
                    //Utils.runPython("runDeleteField.py", parms, gv);
                    // remove fields apart from Subbasin
                    for (int i = 0; i < todo; i++) {
                        if (layer1Def.GetFieldDefn(i).GetName() == Topology._SUBBASIN) {
                            continue;
                        } else {
                            layer1.DeleteField(i);
                            i--;
                            todo--;
                        }
                    }
                }
                //var remaining = layerDef.GetFieldCount();
                // make copy as template for stream results
                Utils.copyShapefile(riv1File, Parameters._RIVS, gv.tablesOutDir);
                string rivFile = Utils.join(gv.tablesOutDir, Parameters._RIVS + ".shp");
                if (!addToRiv1) {
                    // remove fields apart from Subbasin
                    for (int i = 0; i < todo; i++) {
                        if (layer1Def.GetFieldDefn(i).GetName() == Topology._SUBBASIN) {
                            continue;
                        } else {
                            layer1.DeleteField(i);
                            i--;
                        }
                    }
                }
                if (!gv.useGridModel) {
                    // add PenWidth field to stream results template
                    var penWidthField = new FieldDefn(Topology._PENWIDTH, OSGeo.OGR.FieldType.OFTReal);
                    using (var rivDs = Ogr.Open(rivFile, 1)) {
                        var layer = rivDs.GetLayerByIndex(0);
                        layer.CreateField(penWidthField, 1);
                    }
                }
                //int subIdx = 0;
                int subRIdx = 0;
                int areaCIdx = 0;
                int len2Idx = 0;
                int slo2Idx = 0;
                int wid2Idx = 0;
                int dep2Idx = 0;
                int minElIdx = 0;
                int maxElIdx = 0;
                int shapeLenIdx = 0;
                int hydroIdIdx = 0;
                int outletIdIdx = 0;
                if (addToRiv1) {
                    var subRField = new FieldDefn("SubbasinR", OSGeo.OGR.FieldType.OFTInteger);
                    layer1.CreateField(subRField, 1);
                    var areaCField = new FieldDefn("AreaC", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(areaCField, 1);
                    var len2Field = new FieldDefn("Len2", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(len2Field, 1);
                    var slo2Field = new FieldDefn("Slo2", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(slo2Field, 1);
                    var wid2Field = new FieldDefn("Wid2", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(wid2Field, 1);
                    var dep2Field = new FieldDefn("Dep2", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(dep2Field, 1);
                    var minElField = new FieldDefn("MinEl", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(minElField, 1);
                    var maxElField = new FieldDefn("MaxEl", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(maxElField, 1);
                    var shapeLenField = new FieldDefn("Shape_Len", OSGeo.OGR.FieldType.OFTReal);
                    layer1.CreateField(shapeLenField, 1);
                    var hydroIdField = new FieldDefn("HydroID", OSGeo.OGR.FieldType.OFTInteger);
                    layer1.CreateField(hydroIdField, 1);
                    var outletIdField = new FieldDefn("OutletID", OSGeo.OGR.FieldType.OFTInteger);
                    layer1.CreateField(outletIdField, 1);
                    layer1Def = layer1.GetLayerDefn();
                    subRIdx = layer1Def.GetFieldIndex(Topology._SUBBASIN);
                    subRIdx = layer1Def.GetFieldIndex("SubbasinR");
                    areaCIdx = layer1Def.GetFieldIndex("AreaC");
                    len2Idx = layer1Def.GetFieldIndex("Len2");
                    slo2Idx = layer1Def.GetFieldIndex("Slo2");
                    wid2Idx = layer1Def.GetFieldIndex("Wid2");
                    dep2Idx = layer1Def.GetFieldIndex("Dep2");
                    minElIdx = layer1Def.GetFieldIndex("MinEl");
                    maxElIdx = layer1Def.GetFieldIndex("MaxEl");
                    shapeLenIdx = layer1Def.GetFieldIndex("Shape_Len");
                    hydroIdIdx = layer1Def.GetFieldIndex("HydroID");
                    outletIdIdx = layer1Def.GetFieldIndex("OutletID");

                    ////Utils.removeLocks(riv1File);
                    ////parms = Geoprocessing.MakeValueArray(riv1File);
                    ////Utils.runPython("addRivsFields.py", parms, gv);
                    //subIdx = await this.getIndex(riv1Layer, Topology._SUBBASIN);
                    //subRIdx = await this.getIndex(riv1Layer, "SubbasinR");
                    //areaCIdx = await this.getIndex(riv1Layer, "AreaC");
                    //len2Idx = await this.getIndex(riv1Layer, "Len2");
                    //slo2Idx = await this.getIndex(riv1Layer, "Slo2");
                    //wid2Idx = await this.getIndex(riv1Layer, "Wid2");
                    //dep2Idx = await this.getIndex(riv1Layer, "Dep2");
                    //minElIdx = await this.getIndex(riv1Layer, "MinEl");
                    //maxElIdx = await this.getIndex(riv1Layer, "MaxEl");
                    //shapeLenIdx = await this.getIndex(riv1Layer, "Shape_Len");
                    //hydroIdIdx = await this.getIndex(riv1Layer, "HydroID");
                    //OutletIdIdx = await this.getIndex(riv1Layer, "OutletID");
                }
                this.db.connect();
                if (this.db.conn is null) {
                    return null;
                }
                var table = "Reach";
                if (this.isHUC || this.isHAWQS || this.forTNC) {
                    var sql0 = "DROP TABLE IF EXISTS Reach";
                    this.db.execNonQuery(sql0);
                    var sql1 = Topology._REACHCREATESQL;
                    this.db.execNonQuery(sql1);
                } else {
                    var clearSQL = "DELETE FROM " + table;
                    this.db.execNonQuery(clearSQL);
                }
                var oid = 0;
                var time1 = DateTime.Now;
                var wid2Data = new Dictionary<int, double>();
                //var subsToDelete = new HashSet<int>();
                var fidsToDelete = new List<IntPtr>();
                foreach (var kvp in this.linkToBasin) {
                    var link = kvp.Key;
                    var basin = kvp.Value;
                    var SWATBasin = 0;
                    if (!this.basinToSWATBasin.TryGetValue(basin, out SWATBasin)) { SWATBasin = 0; }
                    if (SWATBasin == 0) {
                        continue;
                    }
                    var downLink = this.downLinks[link];
                    int downSWATBasin = 0;
                    if (downLink >= 0) {
                        var downBasin = this.linkToBasin[downLink];
                        while (downLink >= 0 && this.emptyBasins.Contains(downBasin)) {
                            downLink = this.downLinks[downLink];
                            this.linkToBasin.TryGetValue(downLink, out downBasin);
                        }
                        if (downLink < 0) {
                            downSWATBasin = 0;
                        } else {
                            if (!this.basinToSWATBasin.TryGetValue(downBasin, out downSWATBasin)) { downSWATBasin = 0; }
                        }
                    }
                    var drainAreaHa = this.drainAreas[link] / 10000.0;
                    var drainAreaKm = drainAreaHa / 100;
                    var length = this.streamLengths[link];
                    var slopePercent = this.streamSlopes[link] * 100;
                    // Formulae from Srini 11/01/06
                    var channelWidth = 1.29 * Math.Pow(drainAreaKm, 0.6);
                    wid2Data[SWATBasin] = channelWidth;
                    var channelDepth = 0.13 * Math.Pow(drainAreaKm, 0.4);
                    var reachData = this.reachesData[link];
                    //if (reachData is null) {
                    //    subsToDelete.Add(SWATBasin);
                    //    continue;
                    //}
                    var minEl = reachData.lowerZ;
                    var maxEl = reachData.upperZ;
                    if (addToRiv1) {
                        layer1.SetAttributeFilter(String.Format("{0} = {1}", Topology._SUBBASIN, SWATBasin));
                        layer1.ResetReading();
                        OSGeo.OGR.Feature f1 = layer1.GetNextFeature();
                        if (f1 is null) {
                            Utils.error(String.Format("Cannot find subbasin {0} in {1}", SWATBasin, riv1File), this.isBatch);
                            return null;
                        }
                        layer1.SetAttributeFilter(null);
                        f1.SetField(subRIdx, downSWATBasin);
                        f1.SetField(areaCIdx, drainAreaHa);
                        f1.SetField(len2Idx, length);
                        f1.SetField(slo2Idx, slopePercent);
                        f1.SetField(wid2Idx, channelWidth);
                        f1.SetField(dep2Idx, channelDepth);
                        f1.SetField(minElIdx, minEl);
                        f1.SetField(maxElIdx, maxEl);
                        f1.SetField(shapeLenIdx, length);
                        f1.SetField(hydroIdIdx, SWATBasin + 200000);
                        f1.SetField(outletIdIdx, SWATBasin + 100000);
                        layer1.SetFeature(f1);

                        // find the feature for this subbasin
                        //var qsub = new QueryFilter() { WhereClause = String.Format("{0} = {1}", Topology._SUBBASIN, SWATBasin) };
                        //Row row = null;
                        //await QueuedTask.Run(() => {
                        //    using (RowCursor cursor = riv1Layer.Search(qsub)) {
                        //        try {
                        //            cursor.MoveNext();
                        //            row = cursor.Current;
                        //        }
                        //        catch {
                        //            Utils.error(String.Format("Cannot find subbasin {0} in {1}", SWATBasin, riv1File), this.isBatch);
                        //            //return null;
                        //        }
                        //    }

                        //row[subRIdx] = downSWATBasin;
                        //row[areaCIdx] = drainAreaHa;
                        //row[len2Idx] = length;
                        //row[slo2Idx] = slopePercent;
                        //row[wid2Idx] = channelWidth;
                        //row[dep2Idx] = channelDepth;
                        //row[minElIdx] = minEl;
                        //row[maxElIdx] = maxEl;
                        //row[shapeLenIdx] = length;
                        //row[hydroIdIdx] = SWATBasin + 200000;
                        //row[outletIdIdx] = SWATBasin + 100000;
                        //});
                    }
                    oid += 1;
                    string insert = "(" +
                        oid.ToString() + "," +
                        "0," +
                        SWATBasin.ToString() + "," +
                        SWATBasin.ToString() + "," +
                        SWATBasin.ToString() + "," +
                        downSWATBasin.ToString() + "," +
                        SWATBasin.ToString() + "," +
                        downSWATBasin.ToString() + "," +
                        drainAreaHa.ToString() + "," +
                        length.ToString() + "," +
                        slopePercent.ToString() + "," +
                        channelWidth.ToString() + "," +
                        channelDepth.ToString() + "," +
                        minEl.ToString() + "," +
                        maxEl.ToString() + "," +
                        length.ToString() + "," +
                        (SWATBasin + 200000).ToString() + "," +
                        (SWATBasin + 100000).ToString() + ")";
                    this.db.InsertInTable(table, insert);
                }

                var time2 = DateTime.Now;
                Utils.loginfo(String.Format("Writing Reach table took {0} seconds", Convert.ToInt32(time2.Subtract(time1).TotalSeconds)));
                //if (this.isHUC || this.isHAWQS || this.forTNC) {
                //    conn.commit();
                //} else {
                //    this.db.hashDbTable(table);
                //}

                if (gv.useGridModel) {
                    return streamLayer;
                } else {
                    // add layer1 in place of stream reaches layer
                    var riv1Layer = (await Utils.getLayerByFilename(riv1File, FileTypes._REACHES, gv, streamLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
                    if (streamLayer is not null) {
                        await Utils.setLayerVisibility(streamLayer, false);
                    }
                    this.setPenWidth(wid2Data, rivFile, gv);
                    return riv1Layer;
                }
            }
        }
        
        
        
        // Scale wid2 data to 1 .. 4 and write to layer.
        public void setPenWidth(Dictionary<int, double> data, string rivFile, GlobalVars gv) {
            Func<double, double> fun;
            var minW = double.PositiveInfinity;
            var maxW = 0.0;
            foreach (var val in data.Values) {
                minW = Math.Min(minW, val);
                maxW = Math.Max(maxW, val);
            }
            if (maxW > minW) {
                // guard against division by zero
                var rng = maxW - minW;
                fun = x => (x - minW) * 7 / rng + 1.0;
            } else {
                fun = _ => 1.0;
            }
            using (var rivDs = Ogr.Open(rivFile, 1)) {
                var layer = rivDs.GetLayerByIndex(0);
                var layerDef = layer.GetLayerDefn();
                var subIdx = layerDef.GetFieldIndex(Topology._SUBBASIN);
                var penIdx = layerDef.GetFieldIndex(Topology._PENWIDTH);
                layer.ResetReading();
                for (int i = 0; i < layer.GetFeatureCount(1); i++) {
                    // may have deleted features yet to be actually deleted
                    try {
                        var feature = layer.GetFeature(i);
                        int sub = feature.GetFieldAsInteger(subIdx);
                        feature.SetField(penIdx, fun(data[sub]));
                        layer.SetFeature(feature);
                    } catch { ; }
                }
            }


            //    var rivLayer = (await Utils.getLayerByFilename(rivFile, FileTypes._REACHES, gv, streamLayer, Utils._WATERSHED_GROUP_NAME)).Item1 as FeatureLayer;
            //var subIdx = await this.getIndex(rivLayer, Topology._SUBBASIN);
            //if (subIdx < 0) {
            //    Utils.error(String.Format("Cannot find {0} field in stream reaches results template", Topology._SUBBASIN), gv.isBatch);
            //    return;
            //}
            //var penIdx = await this.getIndex(rivLayer, Topology._PENWIDTH);
            //if (penIdx < 0) {
            //    Utils.error(String.Format("Cannot find {0} field in stream reaches results template", Topology._PENWIDTH), gv.isBatch);
            //    return;
            //}
            //await QueuedTask.Run(() => {
            //    using (RowCursor cursor = rivLayer.Search(null)) {
            //        while (cursor.MoveNext()) {
            //            var row = cursor.Current;
            //            int sub = Convert.ToInt32(row[subIdx]);
            //            row[penIdx] = fun(data[sub]);
            //        }
            //    }
            //});
        }

        // 
        //         Make file like D8 contributing area but with heightened values at subbasin outlets.
        //         
        //         Return -1 if cannot make the file.
        //         
        public async Task<int> makeStreamOutletThresholds(GlobalVars gv) {
            //Debug.Assert(File.Exists(gv.demFile));
            var demBase = Path.ChangeExtension(gv.demFile, null);
            var ad8File = demBase + "ad8.tif";
            if (!File.Exists(ad8File)) {
                // Probably using existing watershed but switched tabs in delineation form
                // At any rate, cannot calculate flow paths
                Utils.loginfo("ad8 file not found");
                return -1;
            }
            if (!Utils.isUpToDate(gv.demFile, ad8File)) {
                // Probably using existing watershed but switched tabs in delineation form
                // At any rate, cannot calculate flow paths
                Utils.loginfo("ad8 file out of date");
                return -1;
            }
            gv.hd8File = demBase + "hd8.tif";
            await Utils.removeLayerAndFiles(gv.hd8File);
            //Debug.Assert(!File.Exists(gv.hd8File));
            //Debug.Assert(this.outlets.Count > 0);
            //var ad8Layer = QgsRasterLayer(ad8File, "D8 contributing area");
            // calculate maximum contributing area at an outlet point

            int maxContrib = 0;
            int threshold = 0;

            var hd8_temp = demBase + "hd8_temp.tif";
            await QueuedTask.Run(() => {
                // Create a FileSystemConnectionPath using the folder path.
                FileSystemConnectionPath connectionPath = new FileSystemConnectionPath(new System.Uri(gv.sourceDir), FileSystemDatastoreType.Raster);
                // Create a new FileSystemDatastore using the FileSystemConnectionPath.
                FileSystemDatastore dataStore = new FileSystemDatastore(connectionPath);
                // Open the raster datasets.
                // calculate statistics for ad8File
                var parms = Geoprocessing.MakeValueArray(ad8File);
                Utils.runPython("runStatistics.py", parms, gv);
                RasterDataset ad8Ds = dataStore.OpenDataset<RasterDataset>(Path.GetFileName(ad8File));
                Raster ad8 = ad8Ds.CreateFullRaster();
                foreach (var outlet in this.outlets.Values) {
                    int contrib = Convert.ToInt32(Math.Round(Topology.valueAtPoint(outlet, ad8)));
                    // assume ad8nodata is negative
                    if (contrib >= 0) {
                        maxContrib = Math.Max(maxContrib, contrib);
                    }
                }
                threshold = Convert.ToInt32(2 * maxContrib);
                //// copy ad8 to hd8_temp and then set outlet point values to threshold
                File.Copy(ad8File, hd8_temp, true);
                Utils.copyPrj(ad8File, gv.hd8File);
                RasterDataset hd8Ds = dataStore.OpenDataset<RasterDataset>(Path.GetFileName(hd8_temp));
                Raster hd8 = hd8Ds.CreateFullRaster();
                // seems like a kludge to create 1x1 pixel blocks, but seems easiest way to write a few points
                PixelBlock block = hd8.CreatePixelBlock(1, 1);
                foreach (var outlet in this.outlets.Values) {
                    var pixel = hd8.MapToPixel(outlet.X, outlet.Y);
                    hd8.Read(pixel.Item1, pixel.Item2, block);
                    // change value to threshold
                    // don't know about what planes there are - should be just one, as one band - but can safely change all
                    for (int plane = 0; plane < block.GetPlaneCount(); plane++) {
                        Array sourcePixels = block.GetPixelData(plane, false);
                        sourcePixels.SetValue(threshold, 0, 0);
                        block.SetPixelData(plane, sourcePixels);
                    }
                    hd8.Write(pixel.Item1, pixel.Item2, block);
                }
                hd8.SaveAs(Path.GetFileName(gv.hd8File), dataStore, "TIFF", new RasterStorageDef());
                hd8.Dispose();
                hd8Ds.Dispose();
            });
            File.Delete(hd8_temp);
            return threshold;
        }
            // Python:
            // in_raster = Raster(gv.hd8File)
            // raster_info = in_raster.getRasterInfo()
            // new_raster = Raster(raster_info)  # new_raster is writeable, but nodata in all cells
            // new_raster[x, y] = val  # modify cell value
            // so better in_raster.setProperty("readOnly", false)
            // new_raster.save(path)   # write to disk
        
        // Create as burnFile a copy of demFile with points on lines streamFile reduced in height by burnDepth metres.
        public static void burnStream(
            string streamFile,
            string demFile,
            string burnFile,
            double verticalFactor,
            double burnDepth,
            int cellSize,
            GlobalVars gv) {
            // use vertical factor to convert from metres to vertical units of DEM
            int demReduction = Convert.ToInt32(Math.Round(burnDepth / verticalFactor));
            //Debug.Assert(!File.Exists(burnFile));
            var start = DateTime.Now;
            var parms = Geoprocessing.MakeValueArray(demFile, streamFile, burnFile, cellSize.ToString(), demReduction.ToString());
            Utils.runPython("runBurnin.py", parms, gv);
            if (!File.Exists(burnFile)) {
                Utils.error(String.Format("Failed to burn {0} into {1}", streamFile, demFile), gv.isBatch);
                return;
            }
            Utils.copyPrj(demFile, burnFile);
            var finish = DateTime.Now;
            Utils.loginfo(String.Format("Created burned-in DEM {0} in {1} milliseconds", burnFile, Convert.ToInt32((finish.Subtract(start)).TotalMilliseconds)));
        }
        
        // 
        //         Get the band 1 value at point in a raster.
        //         
        //         Must be run inside QueuedTask.Run
        //         
        public static double valueAtPoint(MapPoint point, Raster raster) {
            // returns (column, row)
            Tuple<int, int> pixel = raster.MapToPixel(point.X, point.Y);
            // uses band, column, row
            // use band 1, which seems to have index 0 in ArcGIS Pro version 3.1
            //var x1 = raster.GetPixelValue(1, pixel.Item1, pixel.Item2);
            var x0 = raster.GetPixelValue(0, pixel.Item1, pixel.Item2);
            double val = Convert.ToDouble(x0);
            return val;
        }

        // 
        //         Get the band 1 value at point in a raster.
        //         
        //         Must be run inside QueuedTask.Run
        //         
        public static double valueAtPoint(Coordinate2D point, Raster raster) {
            // returns (column, row)
            Tuple<int, int> pixel = raster.MapToPixel(point.X, point.Y);
            // uses band, column, row
            // use band 1, which seems to have index 0 in ArcGIS Pro version 3.1
            //var x1 = raster.GetPixelValue(1, pixel.Item1, pixel.Item2);
            var x0 = raster.GetPixelValue(0, pixel.Item1, pixel.Item2);
            double val = Convert.ToDouble(x0);
            return val;
        }

        // 
        //         Get the band 1 value at point in a raster.
        //         OGR version
        //         Must be run inside QueuedTask.Run
        //         
        public static double valueAtPoint(double[] point, Raster raster) {
            // returns (column, row)
            Tuple<int, int> pixel = raster.MapToPixel(point[0], point[1]);
            // uses band, column, row
            // use band 1, which seems to have index 0 in ArcGIS Pro version 3.1
            //var x1 = raster.GetPixelValue(1, pixel.Item1, pixel.Item2);
            var x0 = raster.GetPixelValue(0, pixel.Item1, pixel.Item2);
            double val = Convert.ToDouble(x0);
            return val;
        }

        // Return true if a basin is upstream from an inlet.
        public virtual bool isUpstreamBasin(int basin) {
            var link = -1;
            if (this.basinToLink.TryGetValue(basin, out link))
            return this.upstreamFromInlets.Contains(link); else return false;
        }

        // Convert a Coordinate2D to latlong coordinates and return it.
        public Coordinate2D pointToLatLong(Coordinate2D point) {
            var crsTransform = ProjectionTransformation.Create(this.crsProject, this.crsLatLong, this.demExtent);
            var inGeom = new MapPointBuilderEx(point.X, point.Y).ToGeometry();
            var newPoint = GeometryEngine.Instance.ProjectEx(inGeom, crsTransform) as MapPoint;
            return new Coordinate2D(newPoint.X, newPoint.Y);
        }
        
        // Get the index of a shapefile layer attribute name, 
        //         reporting error if not found, unless ignoreMissing is true.
        // Must be called in QueuedTask.Run
        //         
        public async Task<int> getIndex(FeatureLayer layer, string name, bool ignoreMissing = false) {
            var fields = new List<FieldDescription>();
            try {
                fields = await QueuedTask.Run<List<FieldDescription>>(() => layer.GetFieldDescriptions());
            } catch {
                ;
            }
            int index = 0;
            foreach (var field in fields) {
                if (name.ToUpper() == field.Name.ToUpper()) return index;
                index++;
            }
            if (!ignoreMissing) {
                Utils.error(String.Format("Cannot find field {0} in {1}", name, await Utils.layerFilename(layer)), this.isBatch);
            }
            return -1;
        }

        // Get the index of a shapefile layer attribute name, 
        //         reporting error if not found, unless ignoreMissing is true.
        // OGR version 
        //         
        public int getIndex(OSGeo.OGR.Layer layer, string name, bool ignoreMissing = false) {
            var defn = layer.GetLayerDefn();
            var index = defn.GetFieldIndex(name);
            if (index < 0 && !ignoreMissing) { 
                Utils.error(String.Format("Cannot find field {0} in layer {1}", name, layer.GetName()), this.isBatch);
            }
            return index;
        }

        // Return index of first HUCnn field plus field name, else (-1, '').
        public static object getHUCIndex(FeatureLayer layer) {
            var fields = layer.GetFieldDescriptions();
            var fieldName = "";
            var hucIndex = -1;
            int index = -1;
            foreach (var field in fields) {
                index ++;
                if (field.Name.StartsWith("HUC")) {
                    hucIndex = index;
                    fieldName = field.Name;
                    break;
                }
            }
            return (hucIndex, fieldName);
        }
        
        // Return a point percent along line from outlet end to next point.
        public Coordinate2D makePointInLine(OSGeo.OGR.Feature reach, double percent) {
            double[] outlet = new double[3];
            double[] nxt = new double[3];
            if (this.outletAtStart) {
                var line = Topology.reachOgrFirstLine(reach, this.dx, this.dy);
                line.GetPoint(0, outlet);
                line.GetPoint(1, nxt);
            } else {
                var line = Topology.reachOgrLastLine(reach, this.dx, this.dy);
                var length = line.GetPointCount();
                line.GetPoint(length - 1, outlet);
                line.GetPoint(length - 2, nxt);
            }
            var x = (outlet[0] * (100 - percent) + nxt[0] * percent) / 100.0;
            var y = (outlet[1] * (100 - percent) + nxt[1] * percent) / 100.0;
            return new Coordinate2D(x, y);
        }
        
        // Returns true iff streamLayer lines have their outlet points at their start points.
        //          
        //         Finds shapes with a downstream connections, and 
        //         determines the orientation by seeing how such a shape is connected to the downstream shape.
        //         If they don't seem to be connected (as my happen after merging subbasins) 
        //         tries other shapes with downstream connections, up to 10.
        //         A line is connected to another if their ends are less than dx and dy apart horizontally and vertically.
        //         Assumes the orientation found for this shape can be used generally for the layer.
        //         For HUC models just returns False immediately as NHD flowlines start from source end.
        //         
        public bool hasOutletAtStart(OSGeo.OGR.Layer streamLayer) {
            if (this.isHUC || this.isHAWQS) {
                return false;
            }
            if (this.fromGRASS) {
                return true;
            }
            this.dsLinkIndex = streamLayer.FindFieldIndex(Topology._DSLINKNO, 1);
            //FeatureDefn def = streamLayer.GetLayerDefn();
            //var gridcodeIndex = def.GetFieldIndex(Topology._DSLINKNO);
            if (this.dsLinkIndex < 0) {
                Utils.error("No DSLINKNO field in stream layer", this.isBatch);
                return true;
            }
            // find candidates: links with a down connection
            var candidates = new List<Tuple<OSGeo.OGR.Feature, OSGeo.OGR.Feature>>();
            streamLayer.ResetReading();
            OSGeo.OGR.Feature reach;
            do {
                reach = streamLayer.GetNextFeature();
                if (reach is null) {
                    break;
                }
                int downLink = reach.GetFieldAsInteger(this.dsLinkIndex);
                if (downLink >= 0) {
                    // find the down reach
                    var downReach = Utils.getOgrFeatureByValue(streamLayer, Topology._LINKNO, downLink);
                    if (downReach is not null) {
                        candidates.Add(new Tuple<OSGeo.OGR.Feature, OSGeo.OGR.Feature>(reach, downReach));
                        if (candidates.Count < 10) {
                            continue;
                        } else {
                            break;
                        }
                    } else {
                        Utils.error(String.Format("Cannot find link {0} in stream file", downLink), this.isBatch);
                        return true;
                    }
                }
            } while (true);
            if (candidates.Count == 0) {
                Utils.information("Cannot find link with a downstream link in stream file.  Do you only have one stream?", this.isBatch);
                return true;
            }
            foreach (var candidate in candidates) {
                var upReach = candidate.Item1;
                var downReach = candidate.Item2;
                var firstLine = reachOgrFirstLine(upReach, this.dx, this.dy);
                var firstPoint = new double[3];
                firstLine.GetPoint(0, firstPoint);
                var lastLine = reachOgrLastLine(upReach, this.dx, this.dy);
                var lastPoint = new double[3];
                lastLine.GetPoint(lastLine.GetPointCount() - 1, lastPoint);
                // look for firstPoint and lastPoint on the downstream geometry
                // if one is found it is the outlet of upReach, so if it is firstPoint then outletAtStart is true,
                // and if it is lastPoint then outletAtStart is false.
                var downGeom = downReach.GetGeometryRef();
                var pt = new double[3];
                for (int i = 0; i < downGeom.GetPointCount(); i++) {
                    downGeom.GetPoint(i, pt);
                    if (Topology.samePoint(firstPoint, pt, this.dx, this.dy)) return true;
                    if (Topology.samePoint(lastPoint, pt, this.dx, this.dy)) return false;
                }
            }
            Utils.information("Cannot find physically connected reaches in stream shapefile.  Try increasing nearness threshold", this.isBatch);
            return true;
        }
        
        // Write outlets, nearoutlets and nearsources tables.
        public bool saveOutletsAndSources(OSGeo.OGR.Layer streamLayer) {
            var numFeatures = streamLayer.GetFeatureCount(0);
            int basin;
            double length;
            bool result = true;
            // in case called twice
            this.outlets.Clear();
            this.nearoutlets.Clear();
            this.nearsources.Clear();
            if (this.fromGRASS) {
                return true;
            }
            var isHUCOrHAWQS = this.isHUC || this.isHAWQS;
            var lengthIndex = this.getIndex(streamLayer, Topology._LENGTH, ignoreMissing: !isHUCOrHAWQS);
            var wsnoIndex = this.getIndex(streamLayer, Topology._WSNO, ignoreMissing: !isHUCOrHAWQS);
            var sourceXIndex = this.getIndex(streamLayer, Topology._SOURCEX, ignoreMissing: !isHUCOrHAWQS);
            var sourceYIndex = this.getIndex(streamLayer, Topology._SOURCEY, ignoreMissing: !isHUCOrHAWQS);
            var outletXIndex =  this.getIndex(streamLayer, Topology._OUTLETX, ignoreMissing: !isHUCOrHAWQS);
            var outletYIndex = this.getIndex(streamLayer, Topology._OUTLETY, ignoreMissing: !isHUCOrHAWQS);
            streamLayer.ResetReading();
            OSGeo.OGR.Feature reach = null;
            do {
                reach = streamLayer.GetNextFeature();
                if (reach != null) {
                    if (lengthIndex < 0) {
                        length = reach.GetGeometryRef().Length();
                    } else {
                        length = reach.GetFieldAsDouble(lengthIndex);
                    }
                    if (sourceXIndex >= 0 && sourceYIndex >= 0 && outletXIndex >= 0 && outletYIndex >= 0) {
                        // includes HUC and HAWQS models:
                        basin = reach.GetFieldAsInteger(wsnoIndex);
                        this.outlets[basin] = new Coordinate2D(reach.GetFieldAsDouble(outletXIndex), reach.GetFieldAsDouble(outletYIndex));
                        this.nearoutlets[basin] = this.outlets[basin];
                        this.nearsources[basin] = new Coordinate2D(reach.GetFieldAsDouble(sourceXIndex), reach.GetFieldAsDouble(sourceYIndex));
                    } else if (length > 0) {
                        // otherwise can ignore
                        basin = reach.GetFieldAsInteger(wsnoIndex);
                        var first = Topology.reachOgrFirstLine(reach, this.dx, this.dy);
                        if (first is null || first.GetPointCount() < 2) {
                            Utils.error("It looks like your stream shapefile does not obey the single direction rule, that all reaches are either upstream or downstream.", this.isBatch);
                            result = false;
                            break;
                        }
                        var p1 = new double[3];
                        first.GetPoint(0, p1);
                        var p2 = new double[3];
                        first.GetPoint(1, p2);
                        var midFirst = new Coordinate2D((p1[0] + p2[0]) / 2.0, (p1[1] + p2[1]) / 2.0);
                        var last = Topology.reachOgrLastLine(reach, this.dx, this.dy);
                        if (last is null || last.GetPointCount() < 2) {
                            Utils.error("It looks like your stream shapefile does not obey the single direction rule, that all reaches are either upstream or downstream.", this.isBatch);
                            result = false;
                            break;
                        }
                        var count = last.GetPointCount();
                        var p3 = new double[3];
                        last.GetPoint(count - 1, p3);
                        var p4 = new double[3];
                        last.GetPoint(count - 2, p4);
                        var midLast = new Coordinate2D((p3[0] + p4[0]) / 2.0, (p3[1] + p4[1]) / 2.0);
                        if (this.outletAtStart) {
                            this.outlets[basin] = new Coordinate2D(p1[0], p1[1]);
                            this.nearoutlets[basin] = midFirst;
                            this.nearsources[basin] = midLast;
                        } else {
                            this.outlets[basin] = new Coordinate2D(p3[0], p3[1]);
                            this.nearoutlets[basin] = midLast;
                            this.nearsources[basin] = midFirst;
                        }
                    }
                }
            } while (reach != null);
            return result;
        }

        // Returns the line of a single polyline, 
        //         or a line in a multipolyline whose first point is not adjacent to a point 
        //         of another line in the multipolyline.
        //         
        public static OSGeo.OGR.Geometry reachOgrFirstLine(OSGeo.OGR.Feature reach, double dx, double dy) {
            OSGeo.OGR.Geometry geometry = reach.GetGeometryRef();
            if (geometry.GetGeometryType() == wkbGeometryType.wkbLineString) {
                return geometry;
            } else {
                int count = geometry.GetGeometryCount();
                for (int i = 0; i < count; i++) {
                    var linei = geometry.GetGeometryRef(i);
                    bool connected = false;
                    if (linei is null || linei.GetPointCount() == 0) { continue; }
                    double[] startPoint = new double[3];
                    linei.GetPoint(0, startPoint);
                    for (int j = 0; j < count; j++) {
                        if (i != j) {
                            var linej = geometry.GetGeometryRef(j);
                            // linei is connected to (ie continues) linej if linei's start point is on linej and 
                            // linej has preceding points, so exclude start point of linej from the search
                            if (Topology.pointOgrOnLine(startPoint, linej, 1, linej.GetPointCount() - 1, dx, dy)) {
                                connected = true;
                                break;
                            }
                        }
                    }
                    if (!connected) {
                        return linei;
                    }
                }
            }
            // should not get here
            return null;
        }


        //    await QueuedTask.Run(() => {
        //        var shape = reach.GetShape();
        //        geometry = shape as Polyline;
        //    });
        //    var parts = geometry.Parts;
        //    if (parts.Count == 1) {
        //        return geometry.Parts[0];
        //    }
        //    var numLines = parts.Count;

        //    foreach (var i in Enumerable.Range(0, numLines)) {
        //        var linei = parts[i];
        //        var connected = false;
        //        if (linei is null || linei.Count == 0) {
        //            continue;
        //        } else {
        //            var start = linei[0].StartCoordinate;
        //            foreach (var j in Enumerable.Range(0, numLines)) {
        //                if (i != j) {
        //                    var linej = parts[j];
        //                    if (Topology.pointOgrOnLine(start, linej, dx, dy)) {
        //                        connected = true;
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        if (!connected) {
        //            return linei;
        //        }
        //    }
        //    // should not get here
        //    return null;
        //}
        
        // Returns the line of a single polyline, 
        //         or a line in a multipolyline whose last point is not adjacent to a point 
        //         of another line in the multipolyline.
        //         
        public static OSGeo.OGR.Geometry reachOgrLastLine(OSGeo.OGR.Feature reach, double dx, double dy) {
            OSGeo.OGR.Geometry geometry = reach.GetGeometryRef();
            if (geometry.GetGeometryType() == wkbGeometryType.wkbLineString) {
                return geometry;
            } else {
                int count = geometry.GetGeometryCount();
                for (int i = 0; i < count; i++) {
                    var linei = geometry.GetGeometryRef(i);
                    bool connected = false;
                    if (linei is null || linei.GetPointCount() == 0) { continue; }
                    double[] lastPoint = new double[3];
                    linei.GetPoint(linei.GetPointCount() - 1, lastPoint);
                    for (int j = 0; j < count; j++) {
                        if (i != j) {
                            var linej = geometry.GetGeometryRef(j);
                            // linei connects to linej if linei's last point is on linej AND
                            // linej continues further, so exclude linej's last point from search
                            if (Topology.pointOgrOnLine(lastPoint, linej, 0, linej.GetPointCount() - 2, dx, dy)) {
                                connected = true;
                                break;
                            }
                        }
                    }
                    if (!connected) {
                        return linei;
                    }
                }
            }
            // should not get here
            return null;

            //var geometry = await QueuedTask.Run(() => reach.GetShape() as Polyline);
            //var parts = geometry.Parts;
            //if (parts.Count == 1) {
            //    return geometry.Parts[0];
            //}
            //var numLines = parts.Count;
            //foreach (var i in Enumerable.Range(0, numLines)) {
            //    var linei = parts[i];
            //    var connected = false;
            //    if (linei is null || linei.Count == 0) {
            //        continue;
            //    } else {
            //        var finish = linei[linei.Count - 1].EndCoordinate;
            //        foreach (var j in Enumerable.Range(0, numLines)) {
            //            if (i != j) {
            //                var linej = parts[j];
            //                if (Topology.pointOgrOnLine(finish, linej, dx, dy)) {
            //                    connected = true;
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //    if (!connected) {
            //        return linei;
            //    }
            //}
            //// should not get here
            //return null;
        }
        
        // Return True if p1 and p2 are within dx and dy horizontally and vertically.
        public static bool samePoint(double[] p1, double[] p2, double dx, double dy) {
            var xThreshold = dx * Parameters._NEARNESSTHRESHOLD;
            var yThreshold = dy * Parameters._NEARNESSTHRESHOLD;
            return Math.Abs(p1[0] - p2[0]) < xThreshold && Math.Abs(p1[1] - p2[1]) < yThreshold;
        }

        // Return true if point is within dx and dy horizontally and vertically
        //         of a point on the line between startIndex and finishIndex inclusive
        //         
        //         Note this only checks if the point is close to a vertex.
        public static bool pointOgrOnLine(double[] point, OSGeo.OGR.Geometry line, int startIndex, int finishIndex, double dx, double dy) {
            if (line is null || line.GetPointCount() == 0) {
                return false;
            }
            var x = point[0];
            var y = point[1];
            var xThreshold = dx * Parameters._NEARNESSTHRESHOLD;
            var yThreshold = dy * Parameters._NEARNESSTHRESHOLD;
            for (int i = startIndex; i <= finishIndex; i++) {
                double[] argout = new double[3];
                line.GetPoint(i, argout);
                if (Math.Abs(x - argout[0]) < xThreshold && Math.Abs(y - argout[1]) < yThreshold) {
                    return true;
                }
            }
            return false;
        }

        //    foreach (var segment in line) {
        //        var pt = segment.StartCoordinate;
        //        if (Math.Abs(x - pt.X) < xThreshold && Math.Abs(y - pt.Y) < yThreshold) {
        //            return true;
        //        }
        //        pt = segment.EndCoordinate;
        //        if (Math.Abs(x - pt.X) < xThreshold && Math.Abs(y - pt.Y) < yThreshold) {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        // transform[0] x-coordinate of the upper-left corner of the upper-left pixel. = rasterinfo.getExtent first item
        // transform[1] w-e pixel resolution / pixel width = rasterinfo.getCellSize().Item1
        // transform[2] 0
        // transform[3] y-coordinate of the upper-left corner of the upper-left pixel. = rasterinfo.getExtent third item
        // transform[4] 0
        // transform[5] n-s pixel resolution / pixel height (negative value for a north-up image). = rasterinfo.getCellSize().Item2 (and negate)

        // Convert column number to X-coordinate.
        public static double colToX(int col, double[] transform) {
            return (col + 0.5) * transform[1] + transform[0];
        }

        // Convert row number to Y-coordinate.
        public static double rowToY(int row, double[] transform) {
            return (row + 0.5) * transform[5] + transform[3];
        }

        // Convert X-coordinate to column number.
        public static int xToCol(double x, double[] transform) {
            return Convert.ToInt32((x - transform[0]) / transform[1]);
        }

        // Convert Y-coordinate to row number.
        public static int yToRow(double y, double[] transform) {
            return Convert.ToInt32((y - transform[3]) / transform[5]);
        }

        // Convert column and row numbers to (X,Y)-coordinates.
        public static (double, double) cellToProj(int col, int row, double[] transform) {
            var x = (col + 0.5) * transform[1] + transform[0];
            var y = (row + 0.5) * transform[5] + transform[3];
            return (x, y);
        }

        // Convert (X,Y)-coordinates to column and row numbers.
        public static (int, int) projToCell(double x, double y, double[] transform) {
            var col = Convert.ToInt32((x - transform[0]) / transform[1]);
            var row = Convert.ToInt32((y - transform[3]) / transform[5]);
            return (col, row);
        }

        // 
        //         Return a pair of functions:
        //         row, latitude -> row and column, longitude -> column
        //         for transforming positions in raster1 to row and column of raster2.
        //         
        //         The functions are:
        //         identities on the first argument if the rasters have (sufficiently) 
        //         the same origins and cell sizes;
        //         a simple shift on the first argument if the rasters have 
        //         the same cell sizes but different origins;
        //         otherwise a full transformation on the second argument.
        //         It is assumed that the first and second arguments are consistent, 
        //         ie they identify the same cell in raster1.

        public static (Func<int, double, int>, Func<int, Double, int>) translateCoords(double[] transform1, double[] transform2, int numRows1, int numCols1) {
            int rowShift;
            Func<int, double, int> yFun;
            int colShift;
            Func<int, double, int> xFun;
            if (Topology.sameTransform(transform1, transform2, numRows1, numCols1)) {
                return ((row, _) => row, (col, _) => col);
            }
            var xOrigin1 = transform1[0];
            var xSize1 = transform1[1];
            var yOrigin1 = transform1[3];
            var ySize1 = transform1[5];
            var xOrigin2 = transform2[0];
            var xSize2 = transform2[1];
            var yOrigin2 = transform2[3];
            var ySize2 = transform2[5];
            // accept the origins as the same if they are within a tenth of the cell size
            var sameXOrigin = Math.Abs(xOrigin1 - xOrigin2) < xSize2 * 0.1;
            var sameYOrigin = Math.Abs(yOrigin1 - yOrigin2) < Math.Abs(ySize2) * 0.1;
            // accept cell sizes as equivalent if  vertical/horizontal difference 
            // in cell size times the number of rows/columns
            // in the first is less than half the depth/width of a cell in the second
            var sameXSize = Math.Abs(xSize1 - xSize2) * numCols1 < xSize2 * 0.5;
            var sameYSize = Math.Abs(ySize1 - ySize2) * numRows1 < Math.Abs(ySize2) * 0.5;
            if (sameXSize) {
                if (sameXOrigin) {
                    xFun = (col, _) => col;
                } else {
                    // just needs origin shift
                    if (xOrigin1 > xOrigin2) {
                        colShift = Convert.ToInt32((xOrigin1 - xOrigin2) / xSize1);
                        xFun = (col, _) => col + colShift;
                    } else {
                        colShift = Convert.ToInt32((xOrigin2 - xOrigin1) / xSize1);
                        xFun = (col, _) => col - colShift;
                    }
                }
            } else {
                // full transformation
                xFun = (_, x) => Convert.ToInt32((x - xOrigin2) / xSize2);
            }
            if (sameYSize) {
                if (sameYOrigin) {
                    yFun = (row, _) => row;
                } else {
                    // just needs origin shift
                    if (yOrigin1 > yOrigin2) {
                        rowShift = Convert.ToInt32((yOrigin2 - yOrigin1) / ySize1);
                        yFun = (row, _) => row - rowShift;
                    } else {
                        rowShift = Convert.ToInt32((yOrigin1 - yOrigin2) / ySize1);
                        yFun = (row, _) => row + rowShift;
                    }
                }
            } else {
                // full transformation
                yFun = (_, y) => Convert.ToInt32((y - yOrigin2) / ySize2);
            }
            // note row, column order of return (same as order of reading rasters)
            return (yFun, xFun);
        }

        // Return true if transforms are sufficiently close to be regarded as the same,
        //         i.e. row and column numbers for the first can be used without transformation to read the second.  
        //         Avoids relying on equality between real numbers.
        public static bool sameTransform(double[] transform1, double[] transform2, int numRows1, int numCols1) {
            // may work, thuough we are comparing real values
            if (transform1 == transform2) {
                return true;
            }
            var xOrigin1 = transform1[0];
            var xSize1 = transform1[1];
            var yOrigin1 = transform1[3];
            var ySize1 = transform1[5];
            var xOrigin2 = transform2[0];
            var xSize2 = transform2[1];
            var yOrigin2 = transform2[3];
            var ySize2 = transform2[5];
            // accept the origins as the same if they are within a tenth of the cell size
            var sameXOrigin = Math.Abs(xOrigin1 - xOrigin2) < xSize2 * 0.1;
            if (sameXOrigin) {
                var sameYOrigin = Math.Abs(yOrigin1 - yOrigin2) < Math.Abs(ySize2) * 0.1;
                if (sameYOrigin) {
                    // accept cell sizes as equivalent if  vertical/horizontal difference 
                    // in cell size times the number of rows/columns
                    // in the first is less than half the depth/width of a cell in the second
                    var sameXSize = Math.Abs(xSize1 - xSize2) * numCols1 < xSize2 * 0.5;
                    if (sameXSize) {
                        var sameYSize = Math.Abs(ySize1 - ySize2) * numRows1 < Math.Abs(ySize2) * 0.5;
                        return sameYSize;
                    }
                }
            }
            return false;
        }

        // Return distance in m between points with latlon coordinates, using the haversine formula.
        public static double distance(double lat1, double lon1, double lat2, double lon2) {
            double frac = Math.PI / 180;  // conversion factor to convert degrees to radians
            var dLat = frac * (lat2 - lat1);
            var dLon = frac * (lon2 - lon1);
            var latrad1 = frac * lat1;
            var latrad2 = frac * lat2;
            var sindLat = Math.Sin(dLat / 2);
            var sindLon = Math.Sin(dLon / 2);
            var a = sindLat * sindLat + sindLon * sindLon * Math.Cos(latrad1) * Math.Cos(latrad2);
            var radius = 6371000;
            var c = 2 * Math.Asin(Math.Sqrt(a));
            return radius * c;
        }
        
        public static string _REACHCREATESQL = @"
    CREATE TABLE Reach (
        OBJECTID INTEGER,
        Shape    BLOB,
        ARCID    INTEGER,
        GRID_CODE INTEGER,
        FROM_NODE INTEGER,
        TO_NODE  INTEGER,
        Subbasin INTEGER,
        SubbasinR INTEGER,
        AreaC    REAL,
        Len2     REAL,
        Slo2     REAL,
        Wid2     REAL,
        Dep2     REAL,
        MinEl    REAL,
        MaxEl    REAL,
        Shape_Length  REAL,
        HydroID  INTEGER,
        OutletID INTEGER
    );
    ";
        
        public static string _MONITORINGPOINTCREATESQL = @"
    CREATE TABLE MonitoringPoint (
        OBJECTID   INTEGER,
        Shape      BLOB,
        POINTID    INTEGER,
        GRID_CODE  INTEGER,
        Xpr        REAL,
        Ypr        REAL,
        Lat        REAL,
        Long_      REAL,
        Elev       REAL,
        Name       TEXT,
        Type       TEXT,
        Subbasin   INTEGER,
        HydroID    INTEGER,
        OutletID   INTEGER
    );
    ";
    }
    
 
}
