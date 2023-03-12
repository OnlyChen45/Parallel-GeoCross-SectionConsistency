using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using TriangleNet;
using MathNet.Numerics.LinearAlgebra;//这个玩意儿是牛牛的矩阵计算类
namespace ThreeDModelSystemForSection
{
    class BifucationWorker
    {
        public static SpatialReference spatialReference1;
        public static int tempid=0;
        public BifucationWorker()
        {
        }
        public static Dictionary<int, List<Geometry>> loadShp(string path,string idFieldName,out SpatialReference spatialReference){
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
             
            long featurecount = layer.GetFeatureCount(1);
            Dictionary<int, List<Geometry>> result = new Dictionary<int, List<Geometry>>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature feature = layer.GetFeature(i);
                int id= feature.GetFieldAsInteger(idFieldName);
                bool containinresult = result.Keys.Contains<int>(id);
                if (containinresult == false)
                {
                    List<Geometry> geomlist = new List<Geometry>();
                    geomlist.Add(feature.GetGeometryRef());
                    result.Add(id, geomlist);
                }
                else {
                    result[id].Add(feature.GetGeometryRef());
                }
            }
            spatialReference = layer.GetSpatialRef();
            return result;
        }
        public static Dictionary<String, List<Geometry>> loadShpByStringId(string path, string idFieldName)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);

            long featurecount = layer.GetFeatureCount(1);
            Dictionary<string, List<Geometry>> result = new Dictionary<string, List<Geometry>>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature feature = layer.GetFeature(i);
                string id = feature.GetFieldAsString(idFieldName);
                bool containinresult = result.Keys.Contains<string>(id);
                if (containinresult == false)
                {
                    List<Geometry> geomlist = new List<Geometry>();
                    geomlist.Add(feature.GetGeometryRef());
                    result.Add(id, geomlist);
                }
                else
                {
                    result[id].Add(feature.GetGeometryRef());
                }
            }
            return result;
        }
       
        /// <summary>
        /// 这个就是把一个geom给按照一个list分成几份
        /// </summary>
        /// <param name="geom1"></param>
        /// <param name="geomlist"></param>
        /// <returns></returns>
        public static Dictionary<int, Geometry> dealOnePairPlural(Geometry geom1,List<Geometry> geomlist) {
            //把geom上的所有点都转成点
            List<TriangleNet.Geometry.Vertex> vertexlist = GetGeomsVertexs(geomlist);
            //构建三角网
            TriangleNet.Mesh mesh = GetTriMesh(vertexlist);
            //构建voronoi图
            double vxt, vyt;
            TriangleNet.Geometry.Rectangle box = getVoronoiRectBox(geom1, geomlist,out vxt,out vyt);
            //TriangleNet.Voronoi.Legacy.SimpleVoronoi sv = new TriangleNet.Voronoi.Legacy.SimpleVoronoi(mesh);
            TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh,box);
            //TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh);
            //获取voronoi图上的所有多边形
            //ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion = sv.Regions;
            var faces= sv.Faces;
            List<TriangleNet.Geometry.Vertex> pVertexs = new List<TriangleNet.Geometry.Vertex>();
            foreach (var mV in mesh.Vertices)
            {
                pVertexs.Add(mV);
            }
            //List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>> pClassficationVoronoi = ClassificationVoronoiRegion(geomlist, pRegion, pVertexs);
            // List<List<TriangleNet.Topology.DCEL.Face>> pClassficationFaces = ClassificationFaces(geomlist, faces, pVertexs);
            List<List<TriangleNet.Topology.DCEL.Face>> pClassficationFaces = ClassificationFacesByGeom(geomlist, faces, pVertexs);//用多边形相交判断哪个面在哪
            //合并voronoi中的三角形
            Dictionary<int, Geometry> pDictVoronoi = new Dictionary<int, Geometry>();
            for (int i = 0; i < pClassficationFaces.Count; i++)
            {
                List<TriangleNet.Topology.DCEL.Face> pvor = pClassficationFaces[i];

                List<Geometry> polygonlist = new List<Geometry>();
                for (int j = 0; j < pvor.Count; j++)
                {
                    Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
                    List<TriangleNet.Topology.DCEL.Vertex> pvs = getVerticesListFromFace(pvor[j]);
                   // List <TriangleNet.Geometry.Point> pvs =vertices as List<TriangleNet.Geometry.Point>;
                    for (int k = 0; k < pvs.Count; k++)
                    {
                        linev.AddPoint_2D(pvs[k].X, pvs[k].Y);
                    }
                    if (pvs.Count > 0)
                    {
                        linev.AddPoint_2D(pvs[0].X, pvs[0].Y);
                        Geometry polygonv = new Geometry(wkbGeometryType.wkbPolygon);
                        polygonv.AddGeometry(linev);
                        polygonlist.Add(polygonv);
                    }
                   
                }
               //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voipiecet" +i.ToString()+ tempid.ToString() + ".shp", polygonlist, spatialReference1);
                
                //合并三角面
                Geometry gp = new Geometry(wkbGeometryType.wkbPolygon);
                for (int j = 0; j < polygonlist.Count; j++)
                {
                    double pArea = polygonlist[j].GetArea();
                    gp = gp.Union(polygonlist[j]);
                }

                List<Geometry> gs = new List<Geometry>();
                gs.Add(gp);

                pDictVoronoi.Add(i, gp);
            }
            //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voittt" + tempid.ToString() + ".shp", pDictVoronoi.Values.ToList<Geometry>(), spatialReference1);
            tempid++;
            string wkt1, wkt2, wkt3,wkt4,wkt5;
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            Geometry geomunion = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in pDictVoronoi)
            {
                geomunion = geomunion.Union(vk.Value);
            }
            geom1.ExportToWkt(out wkt1);
            geomunion.ExportToWkt(out wkt2);

            double VX, VY;
            //Geometry tempgeom1 = moveCenterToWithRect(geom1, geomunion, out VX, out VY);
            Geometry tempgeom1 = moveto(geom1,   vxt,  vyt);
            tempgeom1.ExportToWkt(out wkt3);
            foreach (var vk in pDictVoronoi)
            {
                Geometry geominter = tempgeom1.Intersection(vk.Value);
                //Geometry geomresult = moveto(geominter, -VX, -VY);
                geominter.ExportToWkt(out wkt4);
                Geometry geomresult = moveto(geominter, -vxt, -vyt);

                geomresult.ExportToWkt(out wkt5);
                result.Add(vk.Key, geomresult);
            }
            /*            Dictionary<int, Geometry> tosplit = getGeomlistToSplit(geom1, pDictVoronoi);
                        foreach (var vk in tosplit)
                        {
                            Geometry geominter = geom1.Intersection(vk.Value);

                            result.Add(vk.Key, geominter);
                        }*/
            return result;

        }
        public static Dictionary<int, Geometry> dealOnePairPlural(Geometry geom1, List<Geometry> geomlist,double[] maxminxyForVor)
        {
            double vxt, vyt;
            TriangleNet.Geometry.Rectangle boxtemp = getVoronoiRectBox(geom1, geomlist, out vxt, out vyt);
            TriangleNet.Geometry.Rectangle box = maxminBox(maxminxyForVor);
            //Convert all points on geom to points

            List<TriangleNet.Geometry.Vertex> vertexlist = GetGeomsVertexs(geomlist,box);
            //Constructing triangulation network
            TriangleNet.Mesh mesh = GetTriMesh(vertexlist);
            //Build the voronoi diagram 


            //TriangleNet.Voronoi.Legacy.SimpleVoronoi sv = new TriangleNet.Voronoi.Legacy.SimpleVoronoi(mesh);
            TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh);
            //TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh, box);
            //Gets all polygons on a voronoi diagram
            //ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion = sv.Regions;
            var faces = sv.Faces;
            List<TriangleNet.Geometry.Vertex> pVertexs = new List<TriangleNet.Geometry.Vertex>();
            foreach (var mV in mesh.Vertices)
            {
                pVertexs.Add(mV);
            }
            //List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>> pClassficationVoronoi = ClassificationVoronoiRegion(geomlist, pRegion, pVertexs);
           // List<List<TriangleNet.Topology.DCEL.Face>> pClassficationFaces = ClassificationFaces(geomlist, faces, pVertexs);
            List<List<TriangleNet.Topology.DCEL.Face>> pClassficationFaces = ClassificationFacesByGeom(geomlist, faces, pVertexs);
            //合并voronoi中的三角形
            Dictionary<int, Geometry> pDictVoronoi = new Dictionary<int, Geometry>();
            for (int i = 0; i < pClassficationFaces.Count; i++)
            {
                List<TriangleNet.Topology.DCEL.Face> pvor = pClassficationFaces[i];

                List<Geometry> polygonlist = new List<Geometry>();
                for (int j = 0; j < pvor.Count; j++)
                {
                    Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
                    List<TriangleNet.Topology.DCEL.Vertex> pvs = getVerticesListFromFace(pvor[j]);
                    // List <TriangleNet.Geometry.Point> pvs =vertices as List<TriangleNet.Geometry.Point>;
                    for (int k = 0; k < pvs.Count; k++)
                    {
                        linev.AddPoint_2D(pvs[k].X, pvs[k].Y);
                    }
                    if (pvs.Count > 0)
                    {
                        linev.AddPoint_2D(pvs[0].X, pvs[0].Y);
                        Geometry polygonv = new Geometry(wkbGeometryType.wkbPolygon);
                        polygonv.AddGeometry(linev);
                        polygonlist.Add(polygonv);
                    }
                }


                //Confluent triangular plane
                Geometry gp = new Geometry(wkbGeometryType.wkbPolygon);
                for (int j = 0; j < polygonlist.Count; j++)
                {
                    double pArea = polygonlist[j].GetArea();
                    gp = gp.Union(polygonlist[j]);
                }

                List<Geometry> gs = new List<Geometry>();
                gs.Add(gp);

                pDictVoronoi.Add(i, gp);
            }
            
            tempid++;
            string wkt1, wkt2, wkt3, wkt4, wkt5;
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            Geometry geomunion = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in pDictVoronoi)
            {
                geomunion = geomunion.Union(vk.Value);
            }
            geom1.ExportToWkt(out wkt1);
            geomunion.ExportToWkt(out wkt2);

            double VX, VY;
            //Geometry tempgeom1 = moveCenterToWithRect(geom1, geomunion, out VX, out VY);
            Geometry tempgeom1 = moveto(geom1, vxt, vyt);
            tempgeom1.ExportToWkt(out wkt3);
            foreach (var vk in pDictVoronoi)
            {
                Geometry geominter = tempgeom1.Intersection(vk.Value);
                //Geometry geomresult = moveto(geominter, -VX, -VY);
                geominter.ExportToWkt(out wkt4);
                Geometry geomresult = moveto(geominter, -vxt, -vyt);

                geomresult.ExportToWkt(out wkt5);
                result.Add(vk.Key, geomresult);
            }

            return result;

        }
        private static TriangleNet.Geometry.Rectangle maxminBox(double[] minmaxxy) {
            double maxx1 = minmaxxy[0] ;
            double minx1 = minmaxxy[1] ;
            double maxy1 = minmaxxy[2];
            double miny1 = minmaxxy[3];
            TriangleNet.Geometry.Rectangle result = new TriangleNet.Geometry.Rectangle(minx1, miny1, maxx1 - minx1, maxy1 - miny1);
            return result;
        }
        private static TriangleNet.Geometry.Rectangle getVoronoiRectBox(Geometry geom1,List<Geometry> geomlist,out double VX,out double VY,double xbuffer=1000,double ybuffer=500) {
            double[] geomlistbox = getBoxByPolyGeomlist(geomlist);
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            double maxx = geomlistbox[0];
            double minx = geomlistbox[1];
            double maxy = geomlistbox[2];
            double miny = geomlistbox[3];
            ring.AddPoint_2D(maxx, maxy);
            ring.AddPoint_2D(maxx, miny);
            ring.AddPoint_2D(minx, miny);
            ring.AddPoint_2D(minx, maxy);
            ring.AddPoint_2D(maxx, maxy);
            Geometry recttemp = new Geometry(wkbGeometryType.wkbPolygon);
            recttemp.AddGeometry(ring);
            double vx, vy;
            Geometry moved = moveCenterToWithRect(geom1, recttemp, out vx, out vy);
            List<Geometry> templist = new List<Geometry>();
            templist.Add(moved);
            double[] boxt = getBoxByPolyGeomlist(templist);
            double maxx1 = boxt[0]+xbuffer;
            double minx1 = boxt[1]-xbuffer;
            double maxy1 = boxt[2]+ybuffer;
            double miny1 = boxt[3]-ybuffer;
            TriangleNet.Geometry.Rectangle result = new TriangleNet.Geometry.Rectangle(minx1, miny1, maxx1 - minx1, maxy1 - miny1);
            VX = vx;
            VY = vy;
            return result;
        }
        private static double[] getBoxByPolyGeomlist(List<Geometry> polys) {
            double maxx = double.MinValue;
            double minx = double.MaxValue;
            double maxy = double.MinValue;
            double miny = double.MaxValue;
            foreach (Geometry poly in polys) {
                Geometry boundary = poly.GetBoundary();
                int pointcount = boundary.GetPointCount();
                for (int i = 0; i < pointcount; i++) {
                    double x = boundary.GetX(i);
                    double y = boundary.GetY(i);
                    if (maxx < x) maxx = x;
                    if (minx > x) minx = x;
                    if (maxy < y) maxy = y;
                    if (miny > y) miny = y;
                }
            }
            double[] result = { maxx, minx, maxy, miny };
            return result;
        }
        private static List<TriangleNet.Topology.DCEL.Vertex> getVerticesListFromFace(TriangleNet.Topology.DCEL.Face face) {
            List < TriangleNet.Topology.DCEL.HalfEdge > halfEdges = face.EnumerateEdgesDealNull().ToList();
            //List<TriangleNet.Topology.DCEL.HalfEdge> halfEdges = face.EnumerateEdgesByList();
            
            List<TriangleNet.Topology.DCEL.Vertex> vertecies = new List<TriangleNet.Topology.DCEL.Vertex>();
            //if (halfEdges == null) return vertecies;
            int eagescount = halfEdges.Count;
            for (int i = 0; i < eagescount; i++) {
                TriangleNet.Topology.DCEL.Vertex vertex = halfEdges[i].Origin;
                //vertex.Name=
                vertecies.Add(vertex);
            }
            return vertecies;
        }
        public static Geometry moveCenterTo(Geometry origeom,Geometry targetGeom,out double VX,out double VY) {
            Geometry boundaryline = origeom.GetBoundary();
            Geometry boundarytarget = targetGeom.GetBoundary();
            int pointcount1 = boundaryline.GetPointCount();
            int pointcount2 = boundarytarget.GetPointCount();
            double centerxori = 0, centeryori = 0;
            for (int i = 0; i < pointcount1; i++) {
                centerxori += boundaryline.GetX(i);
                centeryori += boundaryline.GetY(i);
            }
            centerxori = centerxori / pointcount1;
            centeryori = centeryori / pointcount2;
            double centerxtar = 0, centerytar = 0;
            for (int i = 0; i < pointcount2; i++) {
                centerxtar += boundarytarget.GetX(i);
                centerytar += boundarytarget.GetY(i);
            }
            centerxtar = centerxtar / pointcount2;
            centerytar = centerytar / pointcount2;
            double vx, vy;
            vx = centerxtar - centerxori;
            vy = centerytar - centeryori;
            Geometry ringori = origeom.GetGeometryRef(0);
            Geometry ringresult = new Geometry(wkbGeometryType.wkbLinearRing);
            for (int i = 0; i < pointcount1; i++) {
                double x = vx + ringori.GetX(i);
                double y = vy + ringori.GetY(i);
                ringresult.AddPoint_2D(x, y);
            }
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            result.AddGeometry(ringresult);
            VX = vx;
            VY = vy;
            return result;
        }
        /// <summary>
        /// 把origeom转到和targetGeom重合
        /// </summary>
        /// <param name="origeom"></param>
        /// <param name="targetGeom"></param>
        /// <param name="VX"></param>
        /// <param name="VY"></param>
        /// <returns></returns>
        public static Geometry moveCenterToWithRect(Geometry origeom, Geometry targetGeom, out double VX, out double VY)
        {
            Geometry boundaryline = origeom.GetBoundary();
            Geometry boundarytarget = targetGeom.GetBoundary();
            int pointcount1 = boundaryline.GetPointCount();
            int pointcount2 = boundarytarget.GetPointCount();
            List<double[]> xylist1 = new List<double[]>();
            List<double[]> xylist2 = new List<double[]>();
            for (int i = 0; i < pointcount1; i++)
            {
                double x= boundaryline.GetX(i);
                double y= boundaryline.GetY(i);
                double[] xy = { x, y };
                xylist1.Add(xy);
            }
            for (int i = 0; i < pointcount2; i++)
            {
                double x= boundarytarget.GetX(i);
                double y= boundarytarget.GetY(i);
                double[] xy = { x, y };
                xylist2.Add(xy);
            }
            MinOutsourceRect rect1= MinOutRectBuilder.buildMinOutRect(xylist1);
            MinOutsourceRect rect2 = MinOutRectBuilder.buildMinOutRect(xylist2);
            double centerxtar, centerytar, centerxori, centeryori;
            centerxori = rect1.centerx;
            centeryori = rect1.centery;
            centerxtar = rect2.centerx;
            centerytar = rect2.centery;
            double vx, vy;
            vx = centerxtar - centerxori;
            vy = centerytar - centeryori;
            Geometry ringori = origeom.GetGeometryRef(0);
            Geometry ringresult = new Geometry(wkbGeometryType.wkbLinearRing);
            int pointcount3 = ringori.GetPointCount();
            for (int i = 0; i < pointcount3; i++)
            {
                double x = vx + ringori.GetX(i);
                double y = vy + ringori.GetY(i);
                ringresult.AddPoint_2D(x, y);
            }
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            result.AddGeometry(ringresult);
            VX = vx;
            VY = vy;
            return result;
        }
        public static Dictionary<int,Geometry> getGeomlistToSplit(Geometry origeom,Dictionary<int,Geometry> pDictVoronoi)
        {
            Geometry targetGeom = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in pDictVoronoi)
            {
                targetGeom = targetGeom.Union(vk.Value);
            }
            Geometry boundaryline = origeom.GetBoundary();
            Geometry boundarytarget = targetGeom.GetBoundary();
            int pointcount1 = boundaryline.GetPointCount();
            int pointcount2 = boundarytarget.GetPointCount();
            List<double[]> xylist1 = new List<double[]>();
            List<double[]> xylist2 = new List<double[]>();
            for (int i = 0; i < pointcount1; i++)
            {
                double x = boundaryline.GetX(i);
                double y = boundaryline.GetY(i);
                double[] xy = { x, y };
                xylist1.Add(xy);
            }
            for (int i = 0; i < pointcount2; i++)
            {
                double x = boundarytarget.GetX(i);
                double y = boundarytarget.GetY(i);
                double[] xy = { x, y };
                xylist2.Add(xy);
            }
            MinOutsourceRect rect1 = MinOutRectBuilder.buildMinOutRect(xylist1);
            MinOutsourceRect rect2 = MinOutRectBuilder.buildMinOutRect(xylist2);
            double centerxtar, centerytar, centerxori, centeryori;
            Matrix<double> transmatrix = MinOutsourceRect.getTransMat(rect2, rect1);
            Dictionary<int, Geometry> fullpoly = MinOutsourceRect.geomDicTrans(pDictVoronoi, transmatrix);
            return fullpoly;
        }
        public static Geometry moveto(Geometry poly, double vx, double vy) {
            Geometry ringori = poly.GetGeometryRef(0);
            double pointcount1 = ringori.GetPointCount();
            Geometry ringresult = new Geometry(wkbGeometryType.wkbLinearRing);
            for (int i = 0; i < pointcount1; i++)
            {
                double x = vx + ringori.GetX(i);
                double y = vy + ringori.GetY(i);
                ringresult.AddPoint_2D(x, y);
            }
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            result.AddGeometry(ringresult);
            return result;
        }
        private static List<TriangleNet.Geometry.Vertex> GetGeomsVertexs(List<Geometry>geomlist) {
            List<TriangleNet.Geometry.Vertex> vertexlist = new List<TriangleNet.Geometry.Vertex>();
            int count = geomlist.Count;
            for (int i = 0; i < count; i++) {
                Geometry geomtemp = geomlist[i];
                Geometry ring = geomtemp.GetGeometryRef(0);
                int pointcount = ring.GetPointCount();
                for (int j = 0; j < pointcount; j++) {
                    TriangleNet.Geometry.Vertex vr = new TriangleNet.Geometry.Vertex();
                    double x = ring.GetX(j);
                    double y = ring.GetY(j);
                    vr.X = x;
                    vr.Y = y;
                    vr.NAME = i.ToString() + '#' + j.ToString();
                    vertexlist.Add(vr);
                }
            }
            return vertexlist;
        }
        private static List<TriangleNet.Geometry.Vertex> GetGeomsVertexs(List<Geometry> geomlist,TriangleNet.Geometry.Rectangle box)
        {
            List<TriangleNet.Geometry.Vertex> vertexlist = new List<TriangleNet.Geometry.Vertex>();
            int count = geomlist.Count;
            for (int i = 0; i < count; i++)
            {
                Geometry geomtemp = geomlist[i];
                Geometry ring = geomtemp.GetGeometryRef(0);
                int pointcount = ring.GetPointCount();
                for (int j = 0; j < pointcount; j++)
                {
                    TriangleNet.Geometry.Vertex vr = new TriangleNet.Geometry.Vertex();
                    double x = ring.GetX(j);
                    double y = ring.GetY(j);
                    vr.X = x;
                    vr.Y = y;
                    vr.NAME = i.ToString() + '#' + j.ToString();
                    vertexlist.Add(vr);
                }
            }
            TriangleNet.Geometry.Vertex vr1 = new TriangleNet.Geometry.Vertex();
            vr1.X = box.Bottom;
            vr1.Y = box.Left;
            TriangleNet.Geometry.Vertex vr2 = new TriangleNet.Geometry.Vertex();
            vr1.X = box.Top;
            vr1.Y = box.Left;
            TriangleNet.Geometry.Vertex vr3 = new TriangleNet.Geometry.Vertex();
            vr1.X = box.Top;
            vr1.Y = box.Right;
            TriangleNet.Geometry.Vertex vr4 = new TriangleNet.Geometry.Vertex();
            vr1.X = box.Bottom;
            vr1.Y = box.Right;
            return vertexlist;
        }
        private static TriangleNet.Mesh GetTriMesh(List<TriangleNet.Geometry.Vertex> pA)
        {
            #region 三角剖分模块
            //1. 
            //约束选项（约束类）
            var options = new TriangleNet.Meshing.ConstraintOptions();
            options.SegmentSplitting = 1;
            options.ConformingDelaunay = false;
            options.Convex = false;

            //质量选项（质量类）
            var quality = new TriangleNet.Meshing.QualityOptions();
            TriangleNet.Geometry.IPolygon input = GetPolygon(pA);
            //TriangleNet.Geometry.Contour con = GetContourByTriangle(pA);
            //input.Add(con, false);


            TriangleNet.Mesh mesh = null;
            if (input != null)
            {
                mesh = (TriangleNet.Mesh)TriangleNet.Geometry.ExtensionMethods.Triangulate(input, options);

            }

            return mesh;
            #endregion

        }
        private static TriangleNet.Geometry.IPolygon GetPolygon(List<TriangleNet.Geometry.Vertex> pA)
        {
            TriangleNet.Geometry.IPolygon data = new TriangleNet.Geometry.Polygon();

            foreach (var vt in pA)
            {
                TriangleNet.Geometry.Vertex triVertex = new TriangleNet.Geometry.Vertex(vt.X, vt.Y);
                triVertex.NAME = vt.NAME;
                ////vt.Label = 0;
                //vt.ID =data.Points.Count;
                data.Add(triVertex);

            }
            return data;
        }
        public static List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>> ClassificationVoronoiRegion(List<Geometry> geomlist,
    ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion
    , List<TriangleNet.Geometry.Vertex> pVertexList)
        {
            List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>> pListVoronoi = new List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>>();
            int count = geomlist.Count;
            for (int i=0;i<count;i++)
            {
                List<TriangleNet.Voronoi.Legacy.VoronoiRegion> voronoiReg = new List<TriangleNet.Voronoi.Legacy.VoronoiRegion>();
                foreach (var vregion in pRegion)
                {
                    TriangleNet.Geometry.Vertex vt = pVertexList[vregion.ID];
                    string gid = vt.NAME.Split('#')[0];
                    if (int.Parse(gid)==i)
                    {
                        voronoiReg.Add(vregion);
                    }
                }
                pListVoronoi.Add(voronoiReg);
            }
            return pListVoronoi;
        }/// <summary>
         /// This is to classify the voronoi polygon until it belongs to someone
         /// </summary>
         /// <param name="geomlist"></param>
         /// <param name="pFaces"></param>
         /// <param name="pVertexList"></param>
         /// <returns></returns>
        public static List<List<TriangleNet.Topology.DCEL.Face>> ClassificationFaces(List<Geometry> geomlist,
List<TriangleNet.Topology.DCEL.Face> pFaces
, List<TriangleNet.Geometry.Vertex> pVertexList)
        {
            List<List<TriangleNet.Topology.DCEL.Face>> pListVoronoi = new List<List<TriangleNet.Topology.DCEL.Face>>();
            int count = geomlist.Count;
            for (int i = 0; i < count; i++)
            {
                List<TriangleNet.Topology.DCEL.Face> voronoiReg = new List<TriangleNet.Topology.DCEL.Face>();
                //Geometry geomi = geomlist[i];
                foreach (var vregion in pFaces)
                {
                    TriangleNet.Geometry.Vertex vt = pVertexList[vregion.ID];
                    string gid = vt.NAME.Split('#')[0];
                    if (int.Parse(gid) == i)
                    {
                        voronoiReg.Add(vregion);
                    }
                }
                pListVoronoi.Add(voronoiReg);
            }
            return pListVoronoi;
        }
        public static List<List<TriangleNet.Topology.DCEL.Face>> ClassificationFacesByGeom(List<Geometry> geomlist,
List<TriangleNet.Topology.DCEL.Face> pFaces
, List<TriangleNet.Geometry.Vertex> pVertexList)
        {
            List<List<TriangleNet.Topology.DCEL.Face>> pListVoronoi = new List<List<TriangleNet.Topology.DCEL.Face>>();
            int count = geomlist.Count;
           
            for (int i = 0; i < count; i++)
            {
                List<TriangleNet.Topology.DCEL.Face> voronoiReg = new List<TriangleNet.Topology.DCEL.Face>();
                Geometry geomi = geomlist[i];
                foreach (var vregion in pFaces)
                {
                    List<TriangleNet.Topology.DCEL.Vertex> pvs = getVerticesListFromFace(vregion);
                    Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
                    // List <TriangleNet.Geometry.Point> pvs =vertices as List<TriangleNet.Geometry.Point>;
                    for (int k = 0; k < pvs.Count; k++)
                    {
                        linev.AddPoint_2D(pvs[k].X, pvs[k].Y);
                    }
                    if(pvs.Count>0) linev.AddPoint_2D(pvs[0].X, pvs[0].Y);
                    Geometry polygonv = new Geometry(wkbGeometryType.wkbPolygon);
                    string wkt;
                    linev.ExportToWkt(out wkt);
                    int pointcountoflinev = linev.GetPointCount();
                    polygonv.AddGeometry(linev);
                    if (pointcountoflinev < 4) continue;
                    if (geomi.Intersect(polygonv) == true) {
                        voronoiReg.Add(vregion);
                    }
                }
                pListVoronoi.Add(voronoiReg);
            }
            return pListVoronoi;
        }
        public static void savePolys(string outputpath,  List<Geometry> polys, SpatialReference spatialReference)//, double[] transformAttribute
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(outputpath, null);
            Layer layer = dataSource.CreateLayer("polygon", spatialReference, wkbGeometryType.wkbPolygon, null);
            foreach (Geometry vk in polys)
            {
                
                Geometry poly = vk;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(poly);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
    }
}
