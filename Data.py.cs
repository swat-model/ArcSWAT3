
using System;
using System.Diagnostics;
using System.Data.Common;

using System.Collections.Generic;

namespace ArcSWAT3 {
    
    
    // Data collected about cells in watershed grid that make an HRU.
    public class CellData {
        
        public double area;
        
        public int cellCount;
        
        public int crop;
        
        public double totalSlope;
        
        public CellData(int count, double area, double slope, int crop) {
            //# Cell count
            this.cellCount = count;
            //# Total area in square metres
            this.area = area;
            //# Total slope (for calculating mean slope)
            this.totalSlope = slope;
            //# Original crop number (for use with split landuses)
            this.crop = crop;
        }
        
        // Add data for 1 cell.
        public void addCell(double area, double slope) {
            this.cellCount += 1;
            this.area += area;
            this.totalSlope += slope;
        }
        
        // Add a cell data to this one.
        public void addCells(CellData cd) {
            this.cellCount += cd.cellCount;
            this.area += cd.area;
            this.totalSlope += cd.totalSlope;
        }
        
        // Multiply cell values by factor.
        public void multiply(double factor) {
            this.cellCount = (Int32)Math.Round(this.cellCount * factor);
            this.area *= factor;
            this.totalSlope *= factor;
        }
    }
    
    // Data held about subbasin.
    public class BasinData {
        
        public double area;
        
        public int cellCount;
        
        public Dictionary<int, double> cropAreas;
        
        public double cropSoilSlopeArea;
        
        public Dictionary<int, Dictionary<int, Dictionary<int, int>>> cropSoilSlopeNumbers;
        
        public double definedArea;
        
        public double drainArea;
        
        public int farCol;
        
        public double farDistance;
        
        public double farElevation;
        
        public int farRow;
        
        public int farthest;
        
        public Dictionary<int, CellData> hruMap;
        
        public double lakeArea;
        
        public double maxElevation;
        
        public Dictionary<int, double> originalCropAreas;
        
        public Dictionary<int, double> originalSlopeAreas;
        
        public Dictionary<int, double> originalSoilAreas;
        
        public int outletCol;
        
        public double outletElevation;
        
        public object outletRow;
        
        public double playaArea;
        
        public double polyArea;
        
        public double pondArea;
        
        public int relHru;
        
        public double reservoirArea;
        
        public string reservoirObjectids;
        
        public Dictionary<int, double> slopeAreas;
        
        public Dictionary<int, double> soilAreas;
        
        public int startCol;
        
        public int startRow;
        
        public double startToOutletDistance;
        
        public double startToOutletDrop;
        
        public double streamArea;
        
        public double totalElevation;
        
        public double totalSlope;
        
        public double WATRInStreamArea;
        
        public double wetlandArea;
        
        public BasinData(
            int outletCol,
            int outletRow,
            double outletElevation,
            int startCol,
            int startRow,
            double length,
            double drop,
            double minDist) {
            //# Number of cells in subbasin
            this.cellCount = 0;
            //# area of basin polygon
            this.polyArea = 0.0;
            //# Area of subbasin in square metres.  Equals cropSoilSlopeArea plus reservoirArea plus pondArea plus lakeArea plus nodata area
            this.area = 0.0;
            //# Area draining through outlet of subbasin in square metres
            this.drainArea = 0.0;
            //# pond area in square metres
            this.pondArea = 0.0;
            //# reservoir area in square metres
            this.reservoirArea = 0.0;
            //# objectids of reservoirs in basin (list of integer ids in a string)
            this.reservoirObjectids = "";
            //# playa area in square metres according to NHD data (only used with HUC)
            this.playaArea = 0.0;
            //# lake area in square metres  before any adjustment for total area according to NHD data (only used with HUC)
            this.lakeArea = 0.0;
            //# wetland area in square metres according to NHD data (only used with HUC)
            this.wetlandArea = 0.0;
            //# area of buffered stream in square metres
            this.streamArea = 0.0;
            //# area of WATR pixels in buffered stream in square metres
            this.WATRInStreamArea = 0.0;
            //# Total of elevation values in the subbasin (to compute mean)
            this.totalElevation = 0.0;
            //# Total of slope values for the subbasin (to compute mean)
            this.totalSlope = 0.0;
            //# Column in DEM of outlet point of the subbasin
            this.outletCol = outletCol;
            //# Row in DEM of outlet point of the subbasin
            this.outletRow = outletRow;
            //# Elevation in metres of outlet point of the subbasin
            this.outletElevation = outletElevation;
            //# Elevation in metres of highest point of the subbasin
            this.maxElevation = 0.0;
            //# Column in DEM of start point of the main channel of the subbasin
            this.startCol = startCol;
            //# Row in DEM of start point of the main channel of the subbasin
            this.startRow = startRow;
            //# Channel distance in metres from main channel start to outlet
            this.startToOutletDistance = length;
            //# Drop in metres from main channel start to outlet
            this.startToOutletDrop = drop;
            //# No longer used 
            this.farCol = 0;
            //# No longer used
            this.farRow = 0;
            //# No longer used
            this.farthest = 0;
            //# Elevation in metres of farthest (longest channel length) point from the outlet
            // defaults to source elevation
            this.farElevation = outletElevation + drop;
            //# Longest channel length in metres.  
            //
            // Make it initially min of x and y resolutions of DEM so cannot be zero.
            this.farDistance = minDist;
            //# Area with not-Nodata crop, soil, and slope values (equals sum of hruMap areas).
            // reduced by water bodies
            this.cropSoilSlopeArea = 0.0;
            //# same as cropSoilSlope area, but not reduced by water bodies
            this.definedArea = 0.0;
            //# Map hru (relative) number -> CellData.
            this.hruMap = new Dictionary<int, CellData>();
            //# Nested map crop -> soil -> slope -> hru number.
            // Range of cropSoilSlopeNumbers must be same as domain of hruMap
            this.cropSoilSlopeNumbers = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
            //# Latest created relative HRU number for this subbasin.
            this.relHru = 0;
            //# Map of crop to area of crop in subbasin.
            //
            // This and the similar maps for soil and slope are duplicated:
            // an original version created after basin data is calculated and 
            // before HRUs are created, and another after HRUs are created.
            this.cropAreas = new Dictionary<int, double>();
            //# Original crop area map
            this.originalCropAreas = new Dictionary<int, double>();
            //# Map of soil to area of soil in subbasin.
            this.soilAreas = new Dictionary<int, double>();
            //# Original soil area map
            this.originalSoilAreas = new Dictionary<int, double>();
            //# Map of slope to area of slope in subbasin.
            this.slopeAreas = new Dictionary<int, double>();
            //# Original slope area map
            this.originalSlopeAreas = new Dictionary<int, double>();
        }
        
        // Add data for 1 cell in watershed raster.
        public void addCell(
            int crop,
            int soil,
            int slope,
            double area,
            double elevation,
            double slopeValue,
            double dist,
            GlobalVars _gv) {
            CellData cellData;
            var hru = 0;
            this.cellCount += 1;
            this.area += area;
            this.polyArea += area;
            // drain area calculated separately
            if (slopeValue != _gv.slopeNoData) {
                this.totalSlope += slopeValue;
            }
            if (elevation != _gv.elevationNoData) {
                this.totalElevation += elevation;
                if (dist != _gv.distNoData && dist > this.farDistance) {
                    // We have found a new  (by flow distance) point from the outlet, store distance and its elevation
                    this.farDistance = dist;
                    this.farElevation = elevation;
                }
                if (elevation > this.maxElevation) {
                    this.maxElevation = elevation;
                }
            }
            if (crop != _gv.cropNoData && soil != _gv.soilNoData && slopeValue != _gv.slopeNoData) {
                this.cropSoilSlopeArea += area;
                hru = BasinData.getHruNumber(this.cropSoilSlopeNumbers, this.relHru, crop, soil, slope);
                if (this.hruMap.ContainsKey(hru)) {
                    cellData = this.hruMap[hru];
                    cellData.addCell(area, slopeValue);
                    this.hruMap[hru] = cellData;
                } else {
                    // new hru
                    cellData = new CellData(1, area, slopeValue, crop);
                    this.hruMap[hru] = cellData;
                    this.relHru = hru;
                }
            }
        }
        
        // Return HRU number (new if necessary, adding one to input hru number) 
        //         for the crop/soil/slope combination.
        //         
        public static int getHruNumber(
            Dictionary<int, Dictionary<int, Dictionary<int, int>>> cropSoilSlopeNumbers,
            int hru,
            int crop,
            int soil,
            int slope) {
            Dictionary<int, int> slopeNumbers;
            Dictionary<int, Dictionary<int, int>> soilSlopeNumbers;
            var resultHru = hru;
            if (cropSoilSlopeNumbers.ContainsKey(crop)) {
                soilSlopeNumbers = cropSoilSlopeNumbers[crop];
                if (soilSlopeNumbers.ContainsKey(soil)) {
                    slopeNumbers = soilSlopeNumbers[soil];
                    if (slopeNumbers.ContainsKey(slope)) {
                        return slopeNumbers[slope];
                    } else {
                        // new slope for existing crop and soil
                        resultHru += 1;
                        slopeNumbers[slope] = resultHru;
                    }
                } else {
                    // new soil for existing crop
                    resultHru += 1;
                    slopeNumbers = new Dictionary<int, int>();
                    slopeNumbers[slope] = resultHru;
                    soilSlopeNumbers[soil] = slopeNumbers;
                    cropSoilSlopeNumbers[crop] = soilSlopeNumbers;
                }
            } else {
                // new crop
                resultHru += 1;
                slopeNumbers = new Dictionary<int, int>();
                slopeNumbers[slope] = resultHru;
                soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
                soilSlopeNumbers[soil] = slopeNumbers;
                cropSoilSlopeNumbers[crop] = soilSlopeNumbers;
            }
            return resultHru;
        }
        
        // Set area maps for crop, soil and slope.
        //         Add nodata area to HRUs if redistributeNodata, else reduce basin cellCount, area and totalSlope to total of defined HRUs.
        public void setAreas(bool isOriginal, bool isBatch, bool redistributeNodata = true) {
            if (isOriginal) {
                if (redistributeNodata) {
                    // nodata area is included in final areas: need to add to original
                    // so final and original tally
                    this.redistributeNodata();
                } else {
                    // if we are not redistributing nodata, need to correct the basin area, cell count and totalSlope, which may be reduced 
                    // as we are removing nodata area from the model
                    this.area = this.cropSoilSlopeArea + this.pondArea + this.reservoirArea + this.lakeArea;
                    this.cellCount = this.totalHRUCellCount();
                    this.totalSlope = this.totalHRUSlopes();
                }
            }
            this.setCropAreas(isOriginal, isBatch);
            this.setSoilAreas(isOriginal, isBatch);
            this.setSlopeAreas(isOriginal, isBatch);
        }
        
        // Redistribute nodata area in each HRU.
        public void redistributeNodata() {
            var nonWaterbodyArea = this.area - (this.pondArea + this.reservoirArea + this.lakeArea);
            var areaToRedistribute = nonWaterbodyArea - this.cropSoilSlopeArea;
            if (nonWaterbodyArea > areaToRedistribute && areaToRedistribute > 0) {
                var redistributeFactor = nonWaterbodyArea / (nonWaterbodyArea - areaToRedistribute);
                this.redistribute(redistributeFactor);
            }
            // adjust cropSoilSlopeArea so remains equal to sum of HRU areas
            // also allows this function to be called again with no effect
            this.cropSoilSlopeArea = nonWaterbodyArea;
        }
        
        // Total cell count of HRUs in this subbasin.
        public virtual int totalHRUCellCount() {
            var totalCellCount = 0;
            foreach (var hruData in this.hruMap.Values) {
                totalCellCount += hruData.cellCount;
            }
            return totalCellCount;
        }
        
        // Total area in square metres of HRUs in this subbasin.
        public virtual double totalHRUAreas() {
            var totalArea = 0.0;
            foreach (var hruData in this.hruMap.Values) {
                totalArea += hruData.area;
            }
            return totalArea;
        }
        
        // Total slope values of HRUs in this subbasin.
        public virtual double totalHRUSlopes() {
            var totalSlope = 0.0;
            foreach (var hruData in this.hruMap.Values) {
                totalSlope += hruData.totalSlope;
            }
            return totalSlope;
        }
        
        // Make map crop -> area from hruMap and cropSoilSlopeNumbers.
        public void setCropAreas(bool isOriginal, bool isBatch) {
            var cmap = isOriginal ? this.originalCropAreas : this.cropAreas;
            cmap.Clear();
            foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, int>>> kvp in this.cropSoilSlopeNumbers) {
                var crop = kvp.Key;
                var soilSlopeNumbers = kvp.Value;
                var area = 0.0;
                foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                    foreach (var hru in slopeNumbers.Values) {
                        CellData cellData;
                        try {
                            cellData = this.hruMap[hru];
                        } catch (Exception) {
                            Utils.error(String.Format("Hru {0} not in hruMap", hru), isBatch);
                            continue;
                        }
                        area += cellData.area;
                    }
                }
                cmap[crop] = area;
            }
        }
        
        // Make map soil -> area from hruMap and cropSoilSlopeNumbers.
        public void setSoilAreas(bool isOriginal, bool isBatch) {
            var smap = isOriginal ? this.originalSoilAreas : this.soilAreas;
            smap.Clear();
            foreach (var soilSlopeNumbers in this.cropSoilSlopeNumbers.Values) {
                foreach (var kvp in soilSlopeNumbers) {
                    var soil = kvp.Key;
                    var slopeNumbers = kvp.Value;
                    foreach (var hru in slopeNumbers.Values) {
                        CellData cellData;
                        try {
                            cellData = this.hruMap[hru];
                        } catch (Exception) {
                            Utils.error(String.Format("Hru {0} not in hruMap", hru), isBatch);
                            continue;
                        }
                        if (smap.ContainsKey(soil)) {
                            var area = smap[soil];
                            smap[soil] = area + cellData.area;
                        } else {
                            smap[soil] = cellData.area;
                        }
                    }
                }
            }
        }
        
        // Make map slope -> area from hruMap and cropSoilSlopeNumbers.
        public void setSlopeAreas(bool isOriginal, bool isBatch) {
            var smap = isOriginal ? this.originalSlopeAreas : this.slopeAreas;
            smap.Clear();
            foreach (var soilSlopeNumbers in this.cropSoilSlopeNumbers.Values) {
                foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                    foreach (var kvp in slopeNumbers) {
                        var slope = kvp.Key;
                        var hru = kvp.Value;
                        CellData cellData;
                        try {
                            cellData = this.hruMap[hru];
                        } catch (Exception) {
                            Utils.error(String.Format("Hru {0} not in hruMap", hru), isBatch);
                            continue;
                        }
                        if (smap.ContainsKey(slope)) {
                            var area = smap[slope];
                            smap[slope] = area + cellData.area;
                        } else {
                            smap[slope] = cellData.area;
                        }
                    }
                }
            }
        }
        
        // Map of soil -> area in square metres for this crop.
        public Dictionary<int, double> cropSoilAreas(int crop, bool isBatch) {
            var csmap = new Dictionary<int, double>();
            var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
            this.cropSoilSlopeNumbers.TryGetValue(crop, out soilSlopeNumbers);
            foreach (var soil in soilSlopeNumbers.Keys) {
                csmap[soil] = this.cropSoilArea(crop, soil, isBatch);
            }
            return csmap;
        }
        
        // Area in square metres for crop.
        public double cropArea(int crop, bool isBatch) {
            // use when cropAreas may not be set
            var area = 0.0;
            var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
            this.cropSoilSlopeNumbers.TryGetValue(crop, out soilSlopeNumbers);
            foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                foreach (var hru in slopeNumbers.Values)
                {
                    CellData cellData;
                    try {
                        cellData = this.hruMap[hru];
                    } catch (Exception) {
                        Utils.error(String.Format("Hru {0} not in hruMap", hru), isBatch);
                        continue;
                    }
                    area += cellData.area;
                }
            }
            return area;
        }
        
        // Area in square metres for crop-soil combination.
        public double cropSoilArea(int crop, int soil, bool isBatch) {
            var area = 0.0;
            var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
            this.cropSoilSlopeNumbers.TryGetValue(crop, out soilSlopeNumbers);
            var slopeNumbers = new Dictionary<int, int>();
            soilSlopeNumbers.TryGetValue(soil, out slopeNumbers);
            foreach (var hru in slopeNumbers.Values)
            {
                CellData cellData;
                try {
                    cellData = this.hruMap[hru];
                } catch (Exception) {
                    Utils.error(String.Format("Hru {0} not in hruMap", hru), isBatch);
                    continue;
                }
                area += cellData.area;
            }
            return area;
        }
        
        // Map of slope -> area in square metres for this crop and soil.
        public virtual Dictionary<int, double> cropSoilSlopeAreas(int crop, int soil, bool isBatch) {
            var cssmap = new Dictionary<int, double>();
            var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
            this.cropSoilSlopeNumbers.TryGetValue(crop, out soilSlopeNumbers);
            var slopeNumbers = new Dictionary<int, int>();
            soilSlopeNumbers.TryGetValue(soil, out slopeNumbers);
            foreach (var kvp in slopeNumbers) {
                var slope = kvp.Key;
                var hru = kvp.Value;
                CellData cellData;
                try {
                    cellData = this.hruMap[hru];
                } catch (Exception) {
                    Utils.error(String.Format("Hru {0} not in hruMap", hru), isBatch);
                    continue;
                }
                cssmap[slope] = cellData.area;
            }
            return cssmap;
        }
        
        // Find the dominant key for a dictionary table of numeric values, 
        //         i.e. the key to the largest value.
        //         
        public static int dominantKey(Dictionary<int, double> table) {
            var maxKey = -1;
            var maxVal = 0.0;
            foreach (var kvp in table) {
                var key = kvp.Key;
                var val = kvp.Value;
                if (val > maxVal) {
                    maxKey = key;
                    maxVal = val;
                }
            }
            return maxKey;
        }
        
        // Find the HRU with the largest area, 
        //         and return its crop, soil and slope.
        //         
        public Tuple<int, int, int> getDominantHRU() {
            var maxArea = 0.0;
            var maxCrop = 0;
            var maxSoil = 0;
            var maxSlope = 0;
            foreach (var kvp1 in this.cropSoilSlopeNumbers) {
                foreach (var kvp2 in kvp1.Value) {
                    foreach (var kvp3 in kvp2.Value) {
                        var cellData = this.hruMap[kvp3.Value];
                        var area = cellData.area;
                        if (area > maxArea) {
                            maxArea = area;
                            maxCrop = kvp1.Key;
                            maxSoil = kvp2.Key;
                            maxSlope = kvp3.Key;
                        }
                    }
                }
            }
            return new Tuple<int, int, int>(maxCrop, maxSoil, maxSlope);
        }
        
        // Multiply all the HRU areas by factor.
        public void redistribute(double factor) {
            foreach (var kvp in this.hruMap) {
                kvp.Value.multiply(factor);
                this.hruMap[kvp.Key] = kvp.Value;
            }
        }
        
        // Remove an HRU from the hruMap and the cropSoilSlopeNumbers map.
        public void removeHRU(int hru, int crop, int soil, int slope) {
            //Debug.Assert(this.cropSoilSlopeNumbers.ContainsKey(crop) && this.cropSoilSlopeNumbers[crop].ContainsKey(soil) && this.cropSoilSlopeNumbers[crop][soil].ContainsKey(slope) && hru == this.cropSoilSlopeNumbers[crop][soil][slope]);
            this.hruMap.Remove(hru);
            this.cropSoilSlopeNumbers[crop][soil].Remove(slope);
            if (this.cropSoilSlopeNumbers[crop][soil].Count == 0) {
                this.cropSoilSlopeNumbers[crop].Remove(soil);
                if (this.cropSoilSlopeNumbers[crop].Count == 0) {
                    this.cropSoilSlopeNumbers.Remove(crop);
                }
            }
        }
        
        // Reduce areas to allow for reservoir, pond, lake and playa areas.  Set WATR HRU to WATRInStream.
        //         Return reduction in WATR area in square metres
        public virtual double removeWaterBodiesArea(double WATRInStream, int basin, DbConnection conn, GlobalVars gv) {
            double factor;

            void setMinimalReach() {
                int SWATBasin = gv.topo.basinToSWATBasin[basin];
                string sql1 = String.Format("SELECT MinEl FROM Reach WHERE Subbasin={0}", SWATBasin);
                using (var reader = DBUtils.getReader(conn, sql1)) {
                    reader.Read();
                    double minEl = reader.GetDouble(0);
                    double maxEl = minEl + Parameters._WATERMAXSLOPE * 100;
                    string sql2 = String.Format("UPDATE Reach SET Len2=100, MaxEl={0}, Shape_Length=100 WHERE Subbasin={1}", maxEl, SWATBasin);
                    gv.db.execNonQuery(sql2);
                }
            }

            double getWaterHRUArea(int waterLanduse) {
                var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
                this.cropSoilSlopeNumbers.TryGetValue(waterLanduse, out soilSlopeNumbers);
                var slopeNumbers = new Dictionary<int, int>();
                soilSlopeNumbers.TryGetValue(Parameters._SSURGOWater, out slopeNumbers);
                int hru = -1;
                if (slopeNumbers.TryGetValue(0, out hru)) {
                    // have existing WATR HRU
                    return this.hruMap[hru].area;
                } else {
                    return 0;
                }
            }

            void setWaterHRUArea(int waterLanduse, double area) {
                var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>();
                this.cropSoilSlopeNumbers.TryGetValue(waterLanduse, out soilSlopeNumbers);
                var slopeNumbers = new Dictionary<int, int>();
                soilSlopeNumbers.TryGetValue(Parameters._SSURGOWater, out slopeNumbers);
                int hru = -1;
                if (slopeNumbers.TryGetValue(0, out hru))
                {
                    // have existing WATR HRU
                    var hruData = this.hruMap[hru];
                    hruData.area = area;
                    hruData.cellCount = (int)Math.Round(area / gv.cellArea);
                } else {
                    // create a water HRU
                    cellCount = (int)Math.Round(area / gv.cellArea);
                    var hruData = new CellData(cellCount, area, Parameters._WATERMAXSLOPE * cellCount, waterLanduse);
                    this.relHru += 1;
                    this.hruMap[this.relHru] = hruData;
                    slopeNumbers[0] = this.relHru;
                    soilSlopeNumbers[Parameters._SSURGOWater] = slopeNumbers;
                    this.cropSoilSlopeNumbers[waterLanduse] = soilSlopeNumbers;
                }
            }

            void removeWater(int waterLanduse) {
                double removedArea = 0.0;
                var soilSlopeNumbers = new Dictionary<int, Dictionary<int, int>>(); 
                if (this.cropSoilSlopeNumbers.TryGetValue(waterLanduse, out soilSlopeNumbers)) {
                    foreach (var slopeNumbers in soilSlopeNumbers.Values) {
                        foreach (var hru in slopeNumbers.Values) {
                            removedArea += this.hruMap[hru].area;
                            this.hruMap.Remove(hru);
                        }
                    }
                    this.cropSoilSlopeNumbers.Remove(waterLanduse);
                }
            }

            bool allWATR(int waterLanduse) {
                foreach (var crop in this.cropSoilSlopeNumbers.Keys) {
                    if (crop != waterLanduse) {
                        return false;
                    }
                }
                return true;
            }

            // first store defined area as cropSoilSlopeArea
            this.definedArea = this.cropSoilSlopeArea;
            var areaToRemove = this.reservoirArea + this.pondArea + this.lakeArea + this.playaArea;
            var availableForHRUs = this.cropSoilSlopeArea - areaToRemove;
            var waterLanduse = gv.db.getLanduseCat("WATR");
            if (availableForHRUs <= 0) {
                //# remove all HRUs
                this.hruMap = new Dictionary<int, CellData>();
                this.relHru = 0;
                this.cropSoilSlopeNumbers = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
                this.cropSoilSlopeArea = 0;
                if (areaToRemove > this.area) {
                    factor = this.area / areaToRemove;
                    this.reservoirArea *= factor;
                    this.pondArea *= factor;
                    this.lakeArea *= factor;
                    this.playaArea *= factor;
                }
                // add a dummy 1 ha WATR HRU to avoid no HRUs in a subbasin
                var area = Math.Min(10000.0, this.area);
                setWaterHRUArea(waterLanduse, area);
                // create a dummy 100m stream to replace existing one in Reach table
                setMinimalReach();
                return 0;
            }
            if (allWATR(waterLanduse)) {
                // just use all the non-reservoir, pond, lake and playa area as water: cannot redistribute anything as no crop HRUs to change
                setWaterHRUArea(waterLanduse, availableForHRUs);
                return 0;
            }
            var oldWaterArea = getWaterHRUArea(waterLanduse);
            var waterOutsideWaterbody = oldWaterArea - areaToRemove;
            // only need to have streams and playas as water HRU
            // note difference between what we have as water and the minimum we need
            // large difference suggests we missed a pond, lake or reservoir
            var waterReduction = waterOutsideWaterbody - WATRInStream;
            var useForWater = Math.Min(waterOutsideWaterbody, WATRInStream);
            if (useForWater > 0) {
                Utils.loginfo(String.Format( "Water area changed from {0} to {1}: {2:F1}%", waterOutsideWaterbody, useForWater, useForWater * 100 / waterOutsideWaterbody));
                this.playaArea = Math.Min(useForWater, this.playaArea);
                setWaterHRUArea(waterLanduse, useForWater);
            } else {
                removeWater(waterLanduse);
                useForWater = 0;
            }
            var availableForCropHRUs = availableForHRUs - useForWater;
            if (availableForCropHRUs == 0) {
                // remove non WATR HRUs
                var cropsToDelete = new List<int>();
                foreach (var kvp in this.cropSoilSlopeNumbers) {
                    var crop = kvp.Key;
                    if (crop != waterLanduse) {
                        cropsToDelete.Add(crop);
                        foreach (var slopeNumbers in kvp.Value.Values) {
                            foreach (var hru in slopeNumbers.Values) {
                                this.hruMap.Remove(hru);
                            }
                        }
                    }
                }
                foreach (var crop in cropsToDelete) {
                    this.cropSoilSlopeNumbers.Remove(crop);
                }
            } else {
                var oldCropArea = this.cropSoilSlopeArea - oldWaterArea;
                factor = availableForCropHRUs / oldCropArea;
                // multiply non-water HRUs by factor
                foreach (var kvp in this.cropSoilSlopeNumbers) {
                    var crop = kvp.Key;
                    if (crop != waterLanduse) {
                        foreach (var slopeNumbers in kvp.Value.Values) {
                            foreach (var hru in slopeNumbers.Values) {
                                this.hruMap[hru].multiply(factor);
                            }
                        }
                    }
                }
            }
            this.cropSoilSlopeArea = availableForHRUs;
            return waterReduction;
        }
    }
    
    // Data about an HRU.
    public class HRUData {
        
        public double area;
        
        public int basin;
        
        public int cellCount;
        
        public int crop;
        
        public double meanSlope;
        
        public int origCrop;
        
        public int relHru;
        
        public int slope;
        
        public int soil;
        
        public HRUData(
            int basin,
            int crop,
            int origCrop,
            int soil,
            int slope,
            int cellCount,
            double area,
            double totalSlope,
            double cellArea,
            int relHru) {
            //# Basin number
            this.basin = basin;
            //# Landuse number
            this.crop = crop;
            //# Original landuse number (for split landuses)
            this.origCrop = origCrop;
            //# Soil number
            this.soil = soil;
            //# Slope index
            this.slope = slope;
            //# Number of DEM cells
            this.cellCount = cellCount;
            //# Area in square metres
            this.area = area;
            //# Originally used cellCount for mean slope, 
            // but cellCounts (which are integer) are inaccurate when small,
            // and may even round to zero because of split and exempt landuses.
            this.meanSlope = area == 0 ? 0 : totalSlope * cellArea / area;
            //# HRU number within the subbasin
            this.relHru = relHru;
        }
    }
}
