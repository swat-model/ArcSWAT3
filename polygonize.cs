namespace ArcSWAT3 {


    using System.Collections.Generic;

    using System;

    using System.Linq;

    using System.Diagnostics;
    using ArcGIS.Core.Geometry;
    using System.Security.Cryptography.X509Certificates;
    //using ArcGIS.Core.Internal.CIM;
    using System.Runtime.Intrinsics;
    using System.Net.NetworkInformation;
    using System.Security.Policy;
    using System.Windows.Documents;
    using System.Windows.Shapes;
    using ArcGIS.Desktop.Internal.Mapping.Symbology;
    using System.IO;
    using System.Windows.Forms;
    using ArcGIS.Core.Internal.CIM;

    //using OSGeo.OGR;

    public struct Vertex {
        public int x;
        public int y;
    }

    // A polygon is a list of list of vertices.  During construction these may not be closed.
    //     
    //     The front list is or will become the outer ring.
    public class Polygon {

        public List<List<Vertex>> rings;
        public bool connected4;
        public object fw;

        public Polygon(bool connected4, object fw) {
            this.rings = new List<List<Vertex>>();
            this.connected4 = connected4;
            this.fw = fw;
        }

        public override string ToString() {
            var res = "";
            foreach (var ring in this.rings) {
                if (ring is not null) {
                    res += Polygonize.ringToString(ring);
                } else {
                    res += "empty";
                }
                res += "  ";
            }
            return res;
        }

        public void coalesce() {
            foreach (var iBase in Enumerable.Range(0, this.rings.Count)) {
                var @base = this.rings[iBase];
                if (@base is not null) {
                    // try to merge into base until no mergers have happened
                    var mergeHappened = true;
                    while (mergeHappened) {
                        mergeHappened = false;
                        foreach (var i in Enumerable.Range(iBase + 1, this.rings.Count - (iBase + 1))) {
                            var ring = this.rings[i];
                            if (ring != null) {
                                var lastv = @base.Last();
                                var firstv = ring[0];
                                if (lastv.x == firstv.x && lastv.y == firstv.y) {
                                    //self.fw.writeFlush('Append')
                                    this.join(@base, ring, iBase, false);
                                    // need to update @base
                                    @base = this.rings[iBase];
                                    // use None rather than delete to avoid for loop crashing
                                    this.rings[i] = null;
                                    mergeHappened = true;
                                    //self.fw.writeFlush(str(self))
                                } else {
                                    lastv = ring.Last();
                                    firstv = @base[0];
                                    if (lastv.x == firstv.x && lastv.y == firstv.y) {
                                        //self.fw.writeFlush('Prepend')
                                        this.join(ring, @base, iBase, false);
                                        // base needs redefining to new value
                                        @base = this.rings[iBase];
                                        this.rings[i] = null;
                                        mergeHappened = true;
                                        //self.fw.writeFlush(str(self))
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // remove places in list of rings for merged rings
            this.rings = (from ring in this.rings
                          where ring != null
                          select ring).ToList();
            //foreach (var ring in this.rings) {
            //    var s = Polygonize.ringToString(ring);
            //    if (!Polygonize.isClosed(ring)) {
            //        throw new Exception(string.Format("Failed to close ring {0}", Polygonize.ringToString(ring)));
            //    }
            //}
            // make sure first ring is clockwise
            // todoCount guards against looping if no ring is clockwise
            var todoCount = this.rings.Count;
            while (todoCount > 0) {
                var ring = this.rings[0];
                if (Polygonize.isClockwise(ring)) { break; }
                this.rings.RemoveAt(0);
                this.rings.Add(ring);
                todoCount -= 1;
            }
            if (todoCount == 0) {
                throw new Exception(string.Format("No clockwise ring in polygon {0}", this.ToString()));
            }
        }

        //Append second to front. Store at index i, unless inPlace
        public void join(List<Vertex> firstl, List<Vertex> secondl, int i, bool inPlace) {
            // remove and discard last vertex of first, then extend with second
            firstl.RemoveAt(firstl.Count - 1);
            firstl.AddRange(secondl);
            if (!inPlace) {
                this.rings[i] = firstl;
            }
        }

        // Add a link running from p1 to p2.
        public void addLink(Vertex p1, Vertex p2) {
            List<Vertex> ring = null;
            foreach (var i in Enumerable.Range(0, this.rings.Count)) {
                ring = this.rings[i];
                // with connected4 a closed ring cannot be added to
                if (this.connected4 && Polygonize.isClosed(ring)) { continue; }
                // if a ring currently runs to p1, append p2
                var lastv = ring.Last();
                if (lastv.x == p1.x && lastv.y == p1.y) {
                    // add p2, or replace last with p2 if previous link in same direction
                    // note this direction test assumes links are either horizontal or vertical
                    var penult = ring[ring.Count - 2];
                    if (penult.x == lastv.x && lastv.x == p2.x ||
                        penult.y == lastv.y && lastv.y == p2.y) {
                        ring[ring.Count - 1] = p2;
                    } else {
                        ring.Add(p2);
                    }
                    if (!this.connected4 && p1.x < p2.x && p1.y == p2.y) {
                        // with connected 8 a right link can join two down links in the previous row
                        // look for a ring starting at p2 and if found append to the current ring
                        foreach (var i2 in Enumerable.Range(0, this.rings.Count)) {
                            if (i2 != i) { // probably can't happen but to be safe from self destruction
                                var ring2 = this.rings[i2];
                                var firstv = ring2[0];
                                if (firstv.x == p2.x && firstv.y == p2.y) {
                                    ring.RemoveAt(ring.Count - 1);
                                    this.rings[i] = ring.Concat(ring2).ToList();
                                    this.rings.RemoveAt(i2);
                                    break;
                                    // now important not to resume outer loop based on indexes of this.rings
                                    // as we just deleted an item, so don't remove the return below
                                }
                            }
                        }
                    }
                    return;
                } else {
                    // if a ring starts from p2, prepend with p1
                    var firstv = ring[0];
                    if (firstv.x == p2.x && firstv.y == p2.y) {
                        // prepend p1, or replace first with p1 if ring link in same direction
                        // note this direction test assumes links are either horizontal or vertical
                        var second = ring[1];
                        if (second.x == firstv.x && firstv.x == p1.x ||
                            second.y == firstv.y && firstv.y == p1.y) {
                            ring[0] = p1;
                        } else {
                            var lp1 = new List<Vertex>() { p1 };
                            this.rings[i] = lp1.Concat(ring).ToList();
                        }
                        //this.fw.writeFlush((str(self)))
                        return;
                    }
                }
            }
            // no existing segment found - make a new one
            this.rings.Add(new List<Vertex>() { p1, p2 });
            //self.fw.writeFlush((str(self))) 
        }

        // Add poly's rings, joining with this one's where possible
        public void addPoly(Polygon poly) {
            foreach (var iPoly in Enumerable.Range(0, poly.rings.Count)) {
                var polyRing = poly.rings[iPoly];
                var polyFirst = polyRing[0];
                var polyLast = polyRing.Last();
                if (this.connected4 && polyFirst.x == polyLast.x && polyFirst.y == polyLast.y) {
                    // closed ring when using 4connectdness cannot be joined to another
                    continue;
                }
                foreach (var i in Enumerable.Range(0, this.rings.Count)) {
                    var ring = this.rings[i];
                    var firstv = ring[0];
                    var lastv = ring[ring.Count - 1];
                    if (this.connected4 && firstv.x == lastv.x && firstv.y == lastv.y) {
                        continue;
                    }
                    if (lastv.x == polyFirst.x && lastv.y == polyFirst.y) {
                        this.join(ring, polyRing, i, true);
                        poly.rings[iPoly] = null;
                        break;
                    }
                    if (firstv.x == polyLast.x && firstv.y == polyLast.y) {
                        this.join(polyRing, ring, i, false);
                        poly.rings[iPoly] = null;
                        break;
                    }
                }
            }
            // add what is left as new rings
            foreach (var ring in poly.rings) {
                if (ring is not null) {
                    this.rings.Add(ring);
                }
            }
            // clean up
            poly.rings = null;
        }
    }

    // A shape is a collection of polygons.
    public class Shape {

        public int nextPolyId;
        public Dictionary<int, Polygon> polygons;
        public Dictionary<int, int> polyIdMap;
        public bool connected4;
        public StreamWriter fw;
        public int cellCount;

        public Shape(bool connected4, StreamWriter fw) {
            this.nextPolyId = 0;
            this.polygons = new Dictionary<int, Polygon>();
            this.polyIdMap = new Dictionary<int, int>();
            this.connected4 = connected4;
            this.cellCount = 0;
            this.fw = fw;
        }
        public override string ToString() {
            string res = "";
            foreach (var poly in this.polygons.Values) {
                res += poly.ToString();
                res += "\r\n";
            }
            return res;
        }

        // Add a new empty polygon and return its id
        public int newPoly() {
            int polyId = this.nextPolyId;
            this.polygons[polyId] = new Polygon(this.connected4, this.fw);
            this.polyIdMap[polyId] = polyId;
            this.nextPolyId++;
            return polyId;
        }

        // If dest and src refer to different polygons, add the src polygon to the dest polygon and map src to dest
        public void checkMerge(int src, int dest) {
            // short cut for common situation
            //self.fw.writeFlush('Checking dest {0} and src {1}'.format(dest, src))
            if (dest == src) {
                return;
            }
            var finalDest = this.polyIdMap[dest];
            var finalSrc = this.polyIdMap[src];
            //self.fw.writeFlush('finalDest is {0} and finalSrc is {1}'.format(finalDest, finalSrc))
            if (finalDest == finalSrc) {
                return;
            }
            // we need to map finalSrc to finalDest
            // and all targets of finalSrc must be changed to finalDes
            foreach (var _tup_1 in this.polyIdMap) {
                var nextSrc = _tup_1.Key;
                var nextTarg = _tup_1.Value;
                if (nextTarg == finalSrc) {
                    this.polyIdMap[nextSrc] = finalDest;
                }
            }
            //self.fw.writeFlush('PolyIdMap is {0}'.format(str(self.polyIdMap)))
            //self.fw.writeFlush('Polygons has keys {0}'.format(str(self.polygons.keys())))
            this.polygons[finalDest].addPoly(this.polygons[finalSrc]);
            this.polygons.Remove(finalSrc);
        }

        // Add a link to the polygon identifed by polyId
        public void addLink(int polyId, Vertex v1, Vertex v2) {
            this.polygons[this.polyIdMap[polyId]].addLink(v1, v2);
        }

        // Coalesce all polygons
        public void coalesce() {
            foreach (var poly in this.polygons.Values) {
                // this.fw.writeFlush('Before coalesce: {0}'.format(str(poly)))
                poly.coalesce();
                // this.fw.writeFlush('After coalesce: {0}'.format(str(poly)))
            }
        }
    }

    // Holds data about conversion of grid vertices to geographic points, 
    //     and provides functions to calculate shape areas and geometries
    public class Offset {

        public MapPoint origin;
        public double dx;
        public double dy;
        public double unitArea;

        public Offset(MapPoint p, double dx, double dy) {
            this.origin = p;
            this.dx = dx;
            this.dy = dy;
            this.unitArea = dx * dy;
        }

        //public OSGeo.OGR.Geometry vertexToPoint(Vertex v) {
        //    var m = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPoint);
        //    m.AddPoint(this.origin.X + this.dx * v.x, this.origin.Y - dy * v.y, 0);
        //    return m;
        //}

        // Convert the cell count to an area in square metres
        public double area(Shape shape) {
            return shape.cellCount * this.unitArea;
        }
        public OSGeo.OGR.Geometry ringToLinearRing(List<Vertex> ring) {
            //Convert a ring to a polygon with a single ring
            var r = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbLinearRing);
            foreach (Vertex v in ring) {
                r.AddPoint_2D(this.origin.X + this.dx * v.x, this.origin.Y - dy * v.y);
            }
            return r;
        }

        public OSGeo.OGR.Geometry ringsToPolygon(List<List<Vertex>> inrings) {
            //Convert a list of linear rings to a Polygon
            var p = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPolygon);
            foreach (var ring in inrings) {
                p.AddGeometry(this.ringToLinearRing(ring));
            }
            return p;
        }

        public OSGeo.OGR.Geometry polygonsToGeometry(List<Polygon> inpolys) {
            // Create a multi-polygon geometry from a list of polygons
            var mp = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbMultiPolygon);
            foreach (var inpoly in inpolys) {
                mp.AddGeometry(this.ringsToPolygon(inpoly.rings));
            }
            return mp;
        }

        public OSGeo.OGR.Geometry makeGeometry(Shape shape) {
            //Make the geometry from a shape
            return this.polygonsToGeometry(shape.polygons.Values.ToList());
        }
    }

    public class Polygonize {

        public int[] lastVals;
        public int[] thisVals;
        public int[] lastIds;
        public int[] thisIds;
        public int rowNum;
        public int length;
        public Dictionary<int, Shape> shapes;
        public bool connected4;
        public int noData;
        public Offset offset;
        public StreamWriter fw;
            
        public Polygonize(bool connected4, int numCols, int noData, MapPoint p, double dX, double dY, StreamWriter fw =null) {
            // length of arrays is number of values in one row plus 2: we have noData at each end
            // (not strictly necessary for id rows, but easier to use same indices)
            this.length = numCols + 2;
            this.lastVals = new int[length];
            this.thisVals = new int[length];
            foreach (int i in Enumerable.Range(0, length)) {
                thisVals[i] = noData;
            }
            this.lastIds = new int[length];
            this.thisIds = new int[length];
            foreach (int i in Enumerable.Range(0, length)) {
                thisIds[i] = -1;
            }
            this.rowNum = 0;
            this.shapes = new Dictionary<int, Shape>();
            this.connected4 = connected4;
            this.noData = noData;
            this.offset = new Offset(p, dX, dY);
            this.fw = fw;
        }

        public void addRow(int[] row, int rowNum) {
            Array.Copy(this.thisVals, this.lastVals, this.length);
            Array.Copy(this.thisIds, this.lastIds, this.length);
            this.rowNum = rowNum;
            // copy values from row, leaving noData values at ends
            Array.Copy(row, 0, this.thisVals, 1, this.length - 2);
            foreach (int i in Enumerable.Range(1, this.length - 2)) {
                int colNum = i - 1;
                bool thisIdDone = false;
                int thisVal = this.thisVals[i];
                Shape shape;
                if (thisVal != this.noData) {
                    if (!this.shapes.TryGetValue(thisVal, out shape)) {
                        shape = new Shape(this.connected4, this.fw);
                    }
                    if (thisVal == this.thisVals[i - 1]) {
                        this.thisIds[i] = this.thisIds[i - 1];
                        //this.fw.writeFlush('Id for value {3} at ({0}, {1}) set to {2} from left'.format(colNum, rowNum, this.thisIds[i], thisVal))
                        thisIdDone = true;
                    }
                    if (thisVal == this.lastVals[i]) {
                        if (thisIdDone) {
                            shape.checkMerge(this.thisIds[i], this.lastIds[i]);
                        } else {
                            this.thisIds[i] = this.lastIds[i];
                            //this.fw.writeFlush('Id for value {3} at ({0}, {1}) set to {2} from last'.format(colNum, rowNum, this.thisIds[i], thisVal))
                            thisIdDone = true;
                        }
                    }
                    if (!this.connected4 && thisVal == this.lastVals[i - 1]) {
                        if (thisIdDone) {
                            shape.checkMerge(this.thisIds[i], this.lastIds[i - 1]);
                        } else {
                            this.thisIds[i] = this.lastIds[i - 1];
                            //this.fw.writeFlush('Id for value {3} at ({0}, {1}) set to {2} from last left'.format(colNum, rowNum, this.thisIds[i], thisVal))
                            thisIdDone = true;
                        }
                    }
                    if (!this.connected4 && thisVal == this.lastVals[i + 1]) {
                        if (thisIdDone) {
                            shape.checkMerge(this.thisIds[i], this.lastIds[i + 1]);
                        } else {
                            this.thisIds[i] = this.lastIds[i + 1];
                            //this.fw.writeFlush('Id for value {3} at ({0}, {1}) set to {2} from last right'.format(colNum, rowNum, this.thisIds[i], thisVal))
                            thisIdDone = true;
                        }
                    }
                    if (!thisIdDone) {
                        this.thisIds[i] = shape.newPoly();
                        //this.fw.writeFlush('Id for value {3} at ({0}, {1}) set to new {2}'.format(colNum, rowNum, this.thisIds[i], thisVal))
                    }
                    this.shapes[thisVal] = shape;
                }
            }
            //this.fw.writeFlush('thisIds: {0}'.format(str(this.thisIds)))
            // with 8 connectedness need to delay inserting left links to avoid failure to attach
            // upper left to lower right polygons. so we record them and attach after rest of row
            List<(int, int, Vertex, Vertex)> leftLinks = new List<(int, int, Vertex, Vertex)>();
            // add edges to polygons and count cells
            foreach (int i in Enumerable.Range(0, this.length - 1)) {
                int thisVal = this.thisVals[i];
                int lastVal = this.lastVals[i];
                int nextVal = this.thisVals[i + 1];
                // val and id arrays have an initial noData/-1 value
                // so index into input row is one less than arrays index
                int colNum = i - 1;
                Shape shape = null;
                Vertex v1;
                Vertex v2;
                if (thisVal != this.noData) {
                    shape = this.shapes[thisVal];
                    shape.cellCount += 1;
                }
                if (thisVal != lastVal) {
                    v1.x = colNum;
                    v1.y = rowNum;
                    v2.x = colNum + 1;
                    v2.y = rowNum;
                    if (thisVal != this.noData) {
                        shape.addLink(this.thisIds[i], v1, v2); // r
                    }
                    if (lastVal != this.noData) {
                        if (this.connected4) {
                            this.shapes[lastVal].addLink(this.lastIds[i], v2, v1); // l
                        } else {
                            // defer adding left link 
                            leftLinks.Add((lastVal, this.lastIds[i], v2, v1));
                        }
                    }
                }
                if (thisVal != nextVal) {
                    v1.x = colNum + 1;
                    v1.y = rowNum;
                    v2.x = colNum + 1;
                    v2.y = rowNum + 1;
                    if (thisVal != this.noData) {
                        shape.addLink(this.thisIds[i], v1, v2); // d
                    }
                    if (nextVal != this.noData) {
                        this.shapes[nextVal].addLink(this.thisIds[i + 1], v2, v1); // u
                    }
                }
            }
            foreach (var (val, id, v2, v1) in leftLinks) {
                this.shapes[val].addLink(id, v2, v1);
            }
            leftLinks = new List<(int, int, Vertex, Vertex)>();
            //this.fw.writeFlush('Polygon keys {0}'.format(str(this.polygons.keys())))
            //foreach (var  poly in this.polygons.Values) {
            //  this.fw.writeFlush(str(poly))
            // }
        }

        public void finish() {
            // Coalesce all polygons.  Collect polygons for each value into a shape.

            // add links for final row
            Vertex v1;
            Vertex v2;
            foreach (int i in Enumerable.Range(1, this.length - 2)) {
                int thisVal = this.thisVals[i];
                if (thisVal != this.noData) {
                    int colNum = i - 1;
                    v1.x = colNum + 1;
                    v1.y = this.rowNum + 1;
                    v2.x = colNum;
                    v2.y = this.rowNum + 1;
                    this.shapes[thisVal].addLink(this.thisIds[i], v1, v2);
                }
            }
            foreach (Shape shape in this.shapes.Values) {
                shape.coalesce();
            }
            // this.fw.writeFlush(this.shapesToString())
        }

        public OSGeo.OGR.Geometry getGeometry(int val) {
            // Return geometry for shape for val.
            Shape shape = null;
            if (this.shapes.TryGetValue(val, out shape)) {
                return this.offset.makeGeometry(shape);
            } else { return null; }
        }


        public string shapesToString() {
            // Return string for all shapes.
            string res = "";

            foreach (KeyValuePair<int, Shape> kvp in this.shapes) {
                int val = kvp.Key;
                Shape shape = kvp.Value;
                res += String.Format("Shape for value {0}: ", val);
                res += shape.ToString();
            }
            return res;
        }

        public int cellCount(int val) {
            // Return total cell count for val
            Shape shape;
            if (this.shapes.TryGetValue(val, out shape)) {
                return shape.cellCount;
            } else { return 0; }
        }

        public double area(int val) {
            // Return area for val in square metres.
            Shape shape;
            if (this.shapes.TryGetValue(val, out shape)) {
                return this.offset.area(shape);
            } else { return 0; }
        }



        public static string ringToString(List<Vertex> ring) {
            var res = vertexToString(ring[0]);
            foreach (var i in Enumerable.Range(0, ring.Count - 1)) {
                var v1 = ring[i];
                var v2 = ring[i + 1];
                if (v1.x == v2.x) {
                    if (v1.y > v2.y) {
                        res += new string('u', v1.y - v2.y);
                    } else {
                        res += new string('d', v2.y - v1.y);
                    }
                } else if (v1.x > v2.x) {
                    res += new string('l', v1.x - v2.x);
                } else {
                    res += new string('r', v2.x - v1.x);
                }
            }
            return res;
        }

        // A ring is clockwise if its leftmost vertical edge is directed up
        public static bool isClockwise(List<Vertex> ring) {
            if (ring is null || ring.Count == 0) { return false; }
            int i = 0;
            int y1 = 0;
            int y2 = 0;
            var v1 = ring[i];
            // make v1 the first candidate
            var minx = v1.x + 1;
            while (i < ring.Count - 1) {
                i++;
                if (v1.x < minx) {
                    var v2 = ring[i];
                    if (v2.x == v1.x) {
                        y1 = v1.y;
                        y2 = v2.y;
                        minx = v1.x;
                    }
                    v1 = v2;
                } else {
                    v1 = ring[i];
                }
            }
            return y1 > y2;
        }

        public static bool isClosed(List<Vertex> ring) {
            if (ring is null || ring.Count == 0) { return false; }
            var firstv = ring[0];
            var lastv = ring.Last();
            return firstv.x == lastv.x && firstv.y == lastv.y;
        }

        public static string vertexToString(Vertex v) {
            return string.Format("({0}, {1})", v.x, v.y);
        }
        //        this.rings.extend((from ring in poly.rings
        //            where ring != null
        //            select ring).ToList());
        //poly.rings = null;
        //        this.thisVals [1::(self.length  -  1)] = row;
        //        this.thisIds [i] = this.thisIds [i - 1];
        //shape.checkMerge(this.thisIds [i], this.lastIds [i]);
        //        this.thisIds [i] = this.lastIds [i];
        //shape.checkMerge(this.thisIds [i], this.lastIds [i - 1]);
        //        this.thisIds [i] = this.lastIds [i - 1];
        //shape.checkMerge(this.thisIds [i], this.lastIds [i + 1]);
        //        this.thisIds [i] = this.lastIds [i + 1];
        //        this.thisIds [i] = shape.newPoly();
        //        this.shapes [thisVal] = shape;
        //shape.cellCount = 1;
        //shape.addLink(this.thisIds [i], v1, v2);
        //        this.shapes [lastVal].addLink(this.lastIds [i], v2, v1);
        //leftLinks.append((lastVal, this.lastIds [i], v2, v1));
        //shape.addLink(this.thisIds [i], v1, v2);
        //        this.shapes [nextVal].addLink(this.thisIds [i + 1], v2, v1);
        //        this.shapes [val].addLink(id, v2, v1);
        //        this.shapes [thisVal].addLink(this.thisIds [i], v1, v2);
        //shape.coalesce();
        //}
    }
}
