using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using TriangleNet;
using MathNet.Numerics.LinearAlgebra;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 分支处理程序，业务
    /// </summary>
    public class BifucationWorker
    {

        public static SpatialReference spatialReference1;
        public static int tempid = 0;
        public BifucationWorker()
        {
        }
        public static Dictionary<int, List<Geometry>> loadShp(string path, string idFieldName, out SpatialReference spatialReference)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);

            long featurecount = layer.GetFeatureCount(1);
            Dictionary<int, List<Geometry>> result = new Dictionary<int, List<Geometry>>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature feature = layer.GetFeature(i);
                int id = feature.GetFieldAsInteger(idFieldName);
                bool containinresult = result.Keys.Contains<int>(id);
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
        }/// <summary>
         /// 废弃暂时不用
         /// </summary>
         /// <param name="fromsection"></param>
         /// <param name="tosection"></param>
         /// <param name="result1"></param>
         /// <param name="result2"></param>
        public static void dealBifucation(Dictionary<int, List<Geometry>> fromsection, Dictionary<int, List<Geometry>> tosection, out Dictionary<int, Geometry> result1, out Dictionary<int, Geometry> result2)
        {
            List<int> ids = fromsection.Keys.ToList<int>();
            result1 = new Dictionary<int, Geometry>();
            result2 = new Dictionary<int, Geometry>();
            foreach (int id in ids)
            {
                List<Geometry> geoms1 = fromsection[id];
                List<Geometry> geoms2 = tosection[id];
                int count1 = geoms1.Count;
                int count2 = geoms2.Count;
                if (count1 == 1 && count2 == 1)
                {
                    //一对一就直接存入结果
                    result1.Add(id, geoms1[0]);
                    result2.Add(id, geoms2[0]);
                }
                if (count1 == 1 && count2 > 1)
                {
                    Dictionary<int, Geometry> splitresult = dealOnePairPlural(geoms1[0], geoms2);
                    foreach (var vk in splitresult)
                    {

                    }
                }
                if (count1 > 1 && count2 == 1)
                {
                    Dictionary<int, Geometry> splitresult = dealOnePairPlural(geoms2[0], geoms1);
                }
                if (count1 > 1 && count2 > 1)
                {
                    //这时候就需要进行判断
                }
            }
        }
        /// <summary>
        /// 这个是新版的用Voronoi多边形进行剖分的方法。
        /// 它首先是在目标区域建立了voronoi多边形，然后把目标区域最小外接矩形用voronoi切分成n块，
        /// 然后再把这些块加入应当从属的类别里
        /// </summary>
        /// <param name="geom1"></param>
        /// <param name="geomlist"></param>
        /// <returns></returns>
        public static Dictionary<int, Geometry> dealOnePairPluralnewVersion(Geometry geom1, Dictionary<int,Geometry> geomdic) {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            List<Geometry> geomlist = geomdic.Values.ToList<Geometry>();
            //下面开始直接构建出Voronoi多边形，然后把它合并在了一起
            List<TriangleNet.Geometry.Vertex> vertexlist = GetGeomsVertexs(geomlist);
            //构建三角网
            TriangleNet.Mesh mesh = GetTriMesh(vertexlist);
            //构建voronoi图
            //double vxt, vyt;
           // TriangleNet.Geometry.Rectangle box = getVoronoiRectBox(geom1, geomlist, out vxt, out vyt);

            TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh);
            //获取voronoi图上的所有多边形
            //ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion = sv.Regions;
            var faces = sv.Faces;
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
                //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voipiecet" + i.ToString() + tempid.ToString() + ".shp", polygonlist, spatialReference1);

                //合并三角面
                Geometry gp = new Geometry(wkbGeometryType.wkbPolygon);
                for (int j = 0; j < polygonlist.Count; j++)
                {
                    double pArea = polygonlist[j].GetArea();
                    gp = gp.Union(polygonlist[j]);
                }

                List<Geometry> gs = new List<Geometry>();
                gs.Add(gp);
                int rightid = -1;
                foreach (var vk2 in geomdic) {
                    bool interbool = vk2.Value.Intersect(gp);
                    if (interbool) {
                        rightid = vk2.Key;
                    }
                }
                pDictVoronoi.Add(rightid, gp);
            }
            //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voittt" + tempid.ToString() + ".shp", pDictVoronoi.Values.ToList<Geometry>(), spatialReference1);
            tempid++;
            //string wkt1, wkt2, wkt3, wkt4, wkt5;
           
            Geometry geomunion = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in pDictVoronoi)
            {
                geomunion = geomunion.Union(vk.Value);
            }
            //geom1.ExportToWkt(out wkt1);
            //geomunion.ExportToWkt(out wkt2);
            //到此为止，pDictVoronoi是合并好的voronoi多边形，geomunion是所有的voronoi合并在一起的面
            List<double[]> xylist1 =getxylistFromGeomlist(geomlist);//拿到所有的面的坐标，便于继续进行最小外包矩形的获取。
            List<double[]> getxylistFromGeomlist(List<Geometry> pgeomlist)
            {
                List<double[]> xylistresult = new List<double[]>();
                foreach (Geometry ge in pgeomlist)
                {
                    Geometry boundaryline = ge.GetBoundary();
                    int pointcount1 = boundaryline.GetPointCount();
                    for (int i = 0; i < pointcount1; i++)
                    {
                        double x = boundaryline.GetX(i);
                        double y = boundaryline.GetY(i);
                        double[] xy = { x, y };
                        xylistresult.Add(xy);
                    }
                }
                return xylistresult;
            }
            MinOutsourceRect rect1 = MinOutRectBuilder.buildMinOutRect(xylist1);
            Geometry rectgeom = rect1.getRectOfGeom();
            Dictionary<int, List<Geometry>> geomclass = new Dictionary<int, List<Geometry>>();
            Dictionary<int, Geometry> cutori = new Dictionary<int, Geometry>();//原始裁剪下来的
            foreach (var vk in pDictVoronoi)//把直接拼进去的就拼进去
            {
                Geometry geominter = rectgeom.Intersection(vk.Value);
                List<Geometry> gelist = new List<Geometry>();
                gelist.Add(geominter);
                geomclass.Add(vk.Key, gelist);
                cutori.Add(vk.Key, geominter);
            }

            //把这个
            
            Geometry diffresult= rectgeom.Difference(geomunion);
            //Generates a new geometry which is the region of this geometry with the region of the other geometry removed.
            List<Geometry> diffresultlist = getpolyfromMulti(diffresult);
            List<Geometry> getpolyfromMulti(Geometry geomUnkownType) {//用来获取一个未知的geom中所有的poly
                List<Geometry> polysresult = new List<Geometry>();
                wkbGeometryType geomtypetemp = geomUnkownType.GetGeometryType();
                if (geomtypetemp == wkbGeometryType.wkbPolygon || geomtypetemp == wkbGeometryType.wkbPolygon25D)
                {//如果是一个普通poly，就直接加入结果，然后返回
                    polysresult.Add(geomUnkownType);
                }//如果是multi，不管是集合图形集还是多边形集，，那么就拆分递归，
                else if (geomtypetemp == wkbGeometryType.wkbGeometryCollection|| geomtypetemp == wkbGeometryType.wkbMultiPolygon
                    ||geomtypetemp == wkbGeometryType.wkbGeometryCollection25D || geomtypetemp == wkbGeometryType.wkbMultiPolygon25D) {
                    int geomcountt = geomUnkownType.GetGeometryCount();
                    for (int i = 0; i < geomcountt; i++) {
                        Geometry geometryn = geomUnkownType.GetGeometryRef(i);
                        List<Geometry> templist = getpolyfromMulti(geometryn);
                        polysresult.AddRange(templist);
                    }//这里没有else，其他的什么empty，点，线，都直接扔掉，只要poly
                }
                return polysresult;
            }

            for (int i = 0; i < diffresultlist.Count; i++) {
                Geometry ge = diffresultlist[i];
                foreach (var vk in cutori) {
                    bool interb = vk.Value.Intersect(ge);
                    if (interb) {
                        geomclass[vk.Key].Add(ge);
                        break;
                    }
                }

            }
            Dictionary<int, Geometry> classunionDic = new Dictionary<int, Geometry>();
            foreach (var vk in geomclass) {
                List<Geometry> classlist = vk.Value;
                Geometry resultunion = new Geometry(wkbGeometryType.wkbPolygon);
                for (int j = 0; j < classlist.Count; j++) {
                    resultunion = resultunion.Union(classlist[j]);
                }
                classunionDic.Add(vk.Key, resultunion);
            }
            Geometry ringtt = geom1.Boundary();
            int pointcount2 = ringtt.GetPointCount();
            List<double[]> xylist2 = new List<double[]>();
            for (int i = 0; i < pointcount2; i++)
            {
                double x = ringtt.GetX(i);
                double y = ringtt.GetY(i);
                double[] xy = { x, y };
                xylist2.Add(xy);
            }
            MinOutsourceRect rect2 = MinOutRectBuilder.buildMinOutRect(xylist2);
            Matrix<double> transMat = MinOutsourceRect.getTransMat(rect1, rect2);
            Dictionary<int,Geometry> transedResult= MinOutsourceRect.geomDicTrans(classunionDic, transMat);
            foreach (var vk in transedResult) {
                Geometry intertttt = geom1.Intersection(vk.Value);
                result.Add(vk.Key, intertttt);
            }
            return result;
        }
        /// <summary>
        /// 这个就是把一个geom给按照一个list分成几份
        /// </summary>
        /// <param name="geom1"></param>
        /// <param name="geomlist"></param>
        /// <returns></returns>
        public static Dictionary<int, Geometry> dealOnePairPlural(Geometry geom1, List<Geometry> geomlist)
        {
            //把geom上的所有点都转成点
            List<TriangleNet.Geometry.Vertex> vertexlist = GetGeomsVertexs(geomlist);
            //构建三角网
            TriangleNet.Mesh mesh = GetTriMesh(vertexlist);
            //构建voronoi图
            double vxt, vyt;
            TriangleNet.Geometry.Rectangle box = getVoronoiRectBox(geom1, geomlist, out vxt, out vyt);
            //TriangleNet.Voronoi.Legacy.SimpleVoronoi sv = new TriangleNet.Voronoi.Legacy.SimpleVoronoi(mesh);
            //TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh, box);
            TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh);
            //获取voronoi图上的所有多边形
            //ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion = sv.Regions;
            var faces = sv.Faces;
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
                //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voipiecet" + i.ToString() + tempid.ToString() + ".shp", polygonlist, spatialReference1);

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
            /*            Dictionary<int, Geometry> tosplit = getGeomlistToSplit(geom1, pDictVoronoi);
                        foreach (var vk in tosplit)
                        {
                            Geometry geominter = geom1.Intersection(vk.Value);

                            result.Add(vk.Key, geominter);
                        }*/
            return result;

        }
        public static Dictionary<int, Geometry> dealOnePairPlural(Geometry geom1, List<Geometry> geomlist, double[] maxminxyForVor)
        {
            double vxt, vyt;
            TriangleNet.Geometry.Rectangle boxtemp = getVoronoiRectBox(geom1, geomlist, out vxt, out vyt);
            TriangleNet.Geometry.Rectangle box = maxminBox(maxminxyForVor);
            //把geom上的所有点都转成点

            List<TriangleNet.Geometry.Vertex> vertexlist = GetGeomsVertexs(geomlist, box);
            //构建三角网
            TriangleNet.Mesh mesh = GetTriMesh(vertexlist);
            //构建voronoi图


            //TriangleNet.Voronoi.Legacy.SimpleVoronoi sv = new TriangleNet.Voronoi.Legacy.SimpleVoronoi(mesh);
            TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh);
            //TriangleNet.Voronoi.StandardVoronoi sv = new TriangleNet.Voronoi.StandardVoronoi(mesh, box);
            //获取voronoi图上的所有多边形
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
                //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voipiecettt" + i.ToString() + tempid.ToString() + ".shp", polygonlist, spatialReference1);

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
            //savePolys(@"D:\研究生论文写作\平行地质剖面拓扑一致化\temp\voittttt" + tempid.ToString() + ".shp", pDictVoronoi.Values.ToList<Geometry>(), spatialReference1);
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
            /*            Dictionary<int, Geometry> tosplit = getGeomlistToSplit(geom1, pDictVoronoi);
                        foreach (var vk in tosplit)
                        {
                            Geometry geominter = geom1.Intersection(vk.Value);

                            result.Add(vk.Key, geominter);
                        }*/
            return result;

        }
        private static TriangleNet.Geometry.Rectangle maxminBox(double[] minmaxxy)
        {
            double maxx1 = minmaxxy[0];
            double minx1 = minmaxxy[1];
            double maxy1 = minmaxxy[2];
            double miny1 = minmaxxy[3];
            TriangleNet.Geometry.Rectangle result = new TriangleNet.Geometry.Rectangle(minx1, miny1, maxx1 - minx1, maxy1 - miny1);
            return result;
        }
        private static TriangleNet.Geometry.Rectangle getVoronoiRectBox(Geometry geom1, List<Geometry> geomlist, out double VX, out double VY, double xbuffer = 1000, double ybuffer = 500)
        {
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
            double maxx1 = boxt[0] + xbuffer;
            double minx1 = boxt[1] - xbuffer;
            double maxy1 = boxt[2] + ybuffer;
            double miny1 = boxt[3] - ybuffer;
            TriangleNet.Geometry.Rectangle result = new TriangleNet.Geometry.Rectangle(minx1, miny1, maxx1 - minx1, maxy1 - miny1);
            VX = vx;
            VY = vy;
            return result;
        }
        private static double[] getBoxByPolyGeomlist(List<Geometry> polys)
        {
            double maxx = double.MinValue;
            double minx = double.MaxValue;
            double maxy = double.MinValue;
            double miny = double.MaxValue;
            foreach (Geometry poly in polys)
            {
                Geometry boundary = poly.GetBoundary();
                int pointcount = boundary.GetPointCount();
                for (int i = 0; i < pointcount; i++)
                {
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
        private static List<TriangleNet.Topology.DCEL.Vertex> getVerticesListFromFace(TriangleNet.Topology.DCEL.Face face)
        {
            List<TriangleNet.Topology.DCEL.HalfEdge> halfEdges = face.EnumerateEdgesDealNull().ToList();

            //List<TriangleNet.Topology.DCEL.HalfEdge> halfEdges = face.EnumerateEdgesByList();

            List<TriangleNet.Topology.DCEL.Vertex> vertecies = new List<TriangleNet.Topology.DCEL.Vertex>();
            if (halfEdges.Count == 0) { return vertecies; }
            //if (halfEdges == null) return vertecies;
            int eagescount = halfEdges.Count;
            for (int i = 0; i < eagescount; i++)
            {
                TriangleNet.Topology.DCEL.Vertex vertex = halfEdges[i].Origin;
                //vertex.Name=
                vertecies.Add(vertex);
            }
            return vertecies;
        }
        public static Geometry moveCenterTo(Geometry origeom, Geometry targetGeom, out double VX, out double VY)
        {
            Geometry boundaryline = origeom.GetBoundary();
            Geometry boundarytarget = targetGeom.GetBoundary();
            int pointcount1 = boundaryline.GetPointCount();
            int pointcount2 = boundarytarget.GetPointCount();
            double centerxori = 0, centeryori = 0;
            for (int i = 0; i < pointcount1; i++)
            {
                centerxori += boundaryline.GetX(i);
                centeryori += boundaryline.GetY(i);
            }
            centerxori = centerxori / pointcount1;
            centeryori = centeryori / pointcount2;
            double centerxtar = 0, centerytar = 0;
            for (int i = 0; i < pointcount2; i++)
            {
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
            for (int i = 0; i < pointcount1; i++)
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
        public static Dictionary<int, Geometry> getGeomlistToSplit(Geometry origeom, Dictionary<int, Geometry> pDictVoronoi)
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
        public static Geometry moveto(Geometry poly, double vx, double vy)
        {
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
        private static List<TriangleNet.Geometry.Vertex> GetGeomsVertexs(List<Geometry> geomlist)
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
            return vertexlist;
        }
        private static List<TriangleNet.Geometry.Vertex> GetGeomsVertexs(List<Geometry> geomlist, TriangleNet.Geometry.Rectangle box)
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
            for (int i = 0; i < count; i++)
            {
                List<TriangleNet.Voronoi.Legacy.VoronoiRegion> voronoiReg = new List<TriangleNet.Voronoi.Legacy.VoronoiRegion>();
                foreach (var vregion in pRegion)
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
        }/// <summary>
         /// 这个是给voronoi多边形做分类，让它直到归属给谁
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
                    /*                    TriangleNet.Geometry.Vertex vt = pVertexList[vregion.ID];
                                        string gid = vt.NAME.Split('#')[0];
                                        if (int.Parse(gid) == i)
                                        {
                                            voronoiReg.Add(vregion);
                                        }*/
                    List<TriangleNet.Topology.DCEL.Vertex> pvs = getVerticesListFromFace(vregion);
                    if (pvs.Count == 0) continue;//中间总是会有一些bug，导致一些面的结果是有问题的。
                    Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
                    // List <TriangleNet.Geometry.Point> pvs =vertices as List<TriangleNet.Geometry.Point>;
                    for (int k = 0; k < pvs.Count; k++)
                    {
                        linev.AddPoint_2D(pvs[k].X, pvs[k].Y);
                    }
                    if (pvs.Count > 2) linev.AddPoint_2D(pvs[0].X, pvs[0].Y);//判断一下这个面有没有问题，没问题就存起来。
                    else continue;
                    Geometry polygonv = new Geometry(wkbGeometryType.wkbPolygon);
                    polygonv.AddGeometry(linev);
                    if (geomi.Intersect(polygonv) == true)
                    {
                        voronoiReg.Add(vregion);
                    }
                }
                pListVoronoi.Add(voronoiReg);
            }
            return pListVoronoi;
        }
        public static void savePolys(string outputpath, List<Geometry> polys, SpatialReference spatialReference)//, double[] transformAttribute
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

    public class MinOutsourceRect
    {
        public double angle;
        public double width, heigh;
        public double centerx, centery;
        public double[] oripoints;
        public Matrix<double> transMat;
        public MinOutsourceRect()
        {

        }
        public double[] points()
        {

            //  double widthdiv = this.width / 2;
            // double heighdiv = this.heigh / 2;
            // double[] p1 = { -widthdiv, -heighdiv };
            //  double[] p2 = { widthdiv, -heighdiv };
            // double[] p3 = { widthdiv, heighdiv };
            //   double[] p4 = { -widthdiv, heighdiv };//先设好四个角在变换之前得位置，这时候原点是中心
            double[] p11, p22, p33, p44;
            Matrix<double> matinverse = this.transMat.Inverse();
            double[] ppp = this.oripoints;
            MinOutRectBuilder.transXY(matinverse, ppp[0], ppp[1], out p11);
            MinOutRectBuilder.transXY(matinverse, ppp[2], ppp[3], out p22);
            MinOutRectBuilder.transXY(matinverse, ppp[4], ppp[5], out p33);
            MinOutRectBuilder.transXY(matinverse, ppp[6], ppp[7], out p44);
            double[] result = { p11[0], p11[1], p22[0], p22[1], p33[0], p33[1], p44[0], p44[1] };
            return result;
        }
        public double area()
        {
            return width * heigh;
        }
        public Geometry getRectOfGeom() {
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            double[] rectpoints = this.points();
            ring.AddPoint_2D(rectpoints[0], rectpoints[1]);
            ring.AddPoint_2D(rectpoints[2], rectpoints[3]);
            ring.AddPoint_2D(rectpoints[4], rectpoints[5]);
            ring.AddPoint_2D(rectpoints[6], rectpoints[7]);
            ring.AddPoint_2D(rectpoints[0], rectpoints[1]);
            result.AddGeometry(ring);
            return result;
        }
        public double[] bufferpoints(double xbuffer, double ybuffer)
        {
            double[] ppp1 = this.oripoints;
            double[] ppp = { ppp1[0] - xbuffer, ppp1[1] - ybuffer, ppp1[2] + xbuffer, ppp1[3] - ybuffer, ppp1[4] + xbuffer, ppp1[5] + ybuffer, ppp1[6] - xbuffer, ppp1[7] + ybuffer };
            double[] p11, p22, p33, p44;
            Matrix<double> matinverse = this.transMat.Inverse();
            MinOutRectBuilder.transXY(matinverse, ppp[0], ppp[1], out p11);
            MinOutRectBuilder.transXY(matinverse, ppp[2], ppp[3], out p22);
            MinOutRectBuilder.transXY(matinverse, ppp[4], ppp[5], out p33);
            MinOutRectBuilder.transXY(matinverse, ppp[6], ppp[7], out p44);
            double[] result = { p11[0], p11[1], p22[0], p22[1], p33[0], p33[1], p44[0], p44[1] };
            return result;
        }
        public static Matrix<double> getTransMat(MinOutsourceRect rotateRectOri, MinOutsourceRect rotatedRectTarget, double xbuffer = 0, double ybuffer = 0)
        {
            double[] rectPointsOri, rectPointsTarget;
            if (xbuffer == 0)
            {
                rectPointsOri = getRotateRectPoints(rotateRectOri);
            }
            else
            {
                rectPointsOri = getRotateRectPoints(rotateRectOri, xbuffer, ybuffer);
            }
            if (xbuffer == 0)
            {
                rectPointsTarget = getRotateRectPoints(rotatedRectTarget);
            }
            else { rectPointsTarget = getRotateRectPoints(rotatedRectTarget, xbuffer, ybuffer); }
            double[,] transTo00 = { {1,0,-rectPointsOri[0] },//转到原点
                                    {0,1,-rectPointsOri[1]},
                                    {0,0,1 } };
            double[,] transToTarget = { {1,0,rectPointsTarget[0] },//转到目标位置
                                    {0,1,rectPointsTarget[1]},
                                    {0,0,1 } };
            double angeleori = rotateRectOri.angle;//角度参数angle 是矩形最下面的点（y坐标最大）P[0]发出的平行于x轴的射线，逆时针旋转，与碰到的第一个边的夹角（这个边的边长就作为width），取值范围[-90~0]。
            angeleori = (-angeleori / 180) * Math.PI;//角度转弧度
            double angletarget = rotatedRectTarget.angle;
            angletarget = (-angletarget / 180) * Math.PI;//角度转弧度
            double theta = -angeleori;

            double[,] transRotateToX = { { Math.Cos(theta),-Math.Sin(theta),0},
                                            {Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1} };
            theta = angletarget;
            double[,] transRotateToTarget = { { Math.Cos(theta),-Math.Sin(theta),0},
                                            {Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1} };
            //下面写一个比例变换

            double oriwidth, oriheigh, tarwidth, tarheigh;
            oriwidth = rotateRectOri.width;
            oriheigh = rotateRectOri.heigh;
            tarwidth = rotatedRectTarget.width;
            tarheigh = rotatedRectTarget.heigh;
            double sx = tarwidth / oriwidth;
            double sy = tarheigh / oriheigh;
            double[,] transproportion = { {sx,0,0 },//比例变换
                                        {0,sy,0},
                                        {0,0,1 } };
            //现在就是制作旋转矩阵了
            var mb = Matrix<double>.Build;
            var transTo00M = mb.DenseOfArray(transTo00);
            var transRotateToXM = mb.DenseOfArray(transRotateToX);
            var transproportionM = mb.DenseOfArray(transproportion);
            var transRotateToTargetM = mb.DenseOfArray(transRotateToTarget);
            var transToTargetM = mb.DenseOfArray(transToTarget);
            Matrix<double> result = transToTargetM * transRotateToTargetM * transproportionM * transRotateToXM * transTo00M;
            return result;
        }
        public static Dictionary<int, Geometry> geomDicTrans(Dictionary<int, Geometry> geomdic, Matrix<double> transMat)
        {
            //对于个字典保存geometry类型，让它完成转换
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in geomdic)
            {
                int id = vk.Key;
                Geometry geometry = getTransGeom(vk.Value, transMat);
                result.Add(id, geometry);
            }
            return result;
        }
        public static Geometry getTransGeom(Geometry geom, Matrix<double> transMat)
        {
            //对于单独的一个geom，探查它的类型，然后加以转换
            wkbGeometryType geomType = geom.GetGeometryType();
            Geometry result = new Geometry(geomType);
            Geometry woker = geom;
            Geometry collectGeom = new Geometry(geomType);
            if (geomType == wkbGeometryType.wkbPolygon)
            {
                woker = woker.GetGeometryRef(0);
                collectGeom = new Geometry(wkbGeometryType.wkbLinearRing);
            }
            int pointcount = woker.GetPointCount();
            for (int i = 0; i < pointcount; i++)
            {
                double x = woker.GetX(i);
                double y = woker.GetY(i);
                double[] xy;
                transXY(transMat, x, y, out xy);
                collectGeom.AddPoint_2D(xy[0], xy[1]);
            }
            if (geomType == wkbGeometryType.wkbPolygon)
            {
                result.AddGeometry(collectGeom);
            }
            else { result = collectGeom; }
            return result;
        }
        public static void transXY(Matrix<double> transMat, double x, double y, out double[] XY)
        {//转换
            double[,] xysite = { { x }, { y }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyM = mb.DenseOfArray(xysite);
            var XYM = transMat * xyM;
            double[,] columnXYZ = XYM.ToArray();
            XY = new double[2];
            XY[0] = columnXYZ[0, 0];
            XY[1] = columnXYZ[1, 0];
        }
        private static double[] getRotateRectPoints(MinOutsourceRect rotatedRect)
        {
            double[] result = new double[8];
            result = rotatedRect.points();
            return result;
        }
        private static double[] getRotateRectPoints(MinOutsourceRect rotatedRect, double xbuffer, double ybuffer)
        {
            double[] result = new double[8];
            result = rotatedRect.bufferpoints(xbuffer, ybuffer);
            return result;
        }
    }

    /// <summary>
    /// 制作最小外包矩形的工厂类，
    /// </summary>
    public class MinOutRectBuilder
    {
        /// <summary>
        /// 输入一个xy的列表，可选旋转求解精度，返回一个最小外包矩形对象
        /// </summary>
        /// <param name="xylist"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static MinOutsourceRect buildMinOutRect(List<double[]> xylist, double precision = 1)
        {
            double xcenter, ycenter;
            getcenterCoord(xylist, out xcenter, out ycenter);
            double minarea = double.MaxValue;
            MinOutsourceRect result = new MinOutsourceRect();
            double t = (double)90 / precision;
            int t2 = (int)t;
            int searchtimes = t2;
            for (int i = 0; i <= searchtimes; i++)
            {//循环找到旋转某角度就获得面积最小的矩形
                double angle = i * precision;
                Matrix<double> transM = getTransMat(xcenter, ycenter, angle);
                MinOutsourceRect minOutsourceRect = getSpecificAByMat(xylist, xcenter, ycenter, angle, transM);
                double area = minOutsourceRect.area();
                if (area < minarea)
                {
                    minarea = area;
                    result = minOutsourceRect;
                }
            }
            return result;
        }


        public static void getcenterCoord(List<double[]> xylist, out double centerx, out double centery)
        {
            //获取中心点
            int count = xylist.Count;
            double sumx = 0, sumy = 0;
            for (int i = 0; i < count; i++)
            {
                double[] xy = xylist[i];
                sumx += xy[0];
                sumy += xy[1];
            }
            centerx = sumx / count;
            centery = sumy / count;
            /*      //中心坐标公式参考
                  //https://www.zhihu.com/question/337823261/answer/800546353
                      double area = 0;
                      for (int i = 0; i < count - 1; i++) {
                          double[] xy0 = xylist[i];
                          double[] xy1 = xylist[i + 1];
                          area += xy0[0] * xy1[1] - xy1[0] * xy0[1];
                      }
                      area += xylist[count - 1][0] * xylist[0][1] - xylist[0][0] * xylist[count - 1][1];
                      area = area / 2;
                      double centerxtemp = 0, centerytemp = 0;
                      for (int i = 0; i < count - 1; i++)
                      {
                          double[] xy0 = xylist[i];
                          double[] xy1 = xylist[i + 1];
                          double chaji= xy0[0] * xy1[1] - xy1[0] * xy0[1];
                          double tempx = (xy0[0] + xy1[0]) * chaji;
                          double tempy = (xy0[1] + xy1[1]) * chaji;
                          centerxtemp += tempx;
                          centerytemp += tempy;
                      }
                      centerx = centerxtemp / 6 / area;
                      centery = centerytemp / 6 / area;*/
        }
        public static MinOutsourceRect getSpecificAByMat(List<double[]> xylist, double centerx, double centery, double angle, Matrix<double> transM)
        {
            //这个是制作一个当前的最小外包矩形
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            int count = xylist.Count;
            for (int i = 0; i < count; i++)
            {
                double[] xy = xylist[i];
                double[] xytrans;
                transXY(transM, xy[0], xy[1], out xytrans);
                ring.AddPoint_2D(xytrans[0], xytrans[1]);
            }
            Geometry poly = new Geometry(wkbGeometryType.wkbPolygon);
            poly.AddGeometry(ring);
            Envelope envelope = new Envelope();
            poly.GetEnvelope(envelope);
            MinOutsourceRect result = new MinOutsourceRect();
            result.angle = angle;
            result.centerx = centerx;
            result.centery = centery;
            result.width = envelope.MaxX - envelope.MinX;
            result.heigh = envelope.MaxY - envelope.MinY;
            double[] oripoints = { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MinY, envelope.MaxX, envelope.MaxY, envelope.MinX, envelope.MaxY };
            result.oripoints = oripoints;
            result.transMat = transM;
            return result;
        }
        public static void transXY(Matrix<double> transMat, double x, double y, out double[] XY)
        {//转换
            double[,] xysite = { { x }, { y }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyM = mb.DenseOfArray(xysite);
            var XYM = transMat * xyM;
            double[,] columnXYZ = XYM.ToArray();
            XY = new double[2];
            XY[0] = columnXYZ[0, 0];
            XY[1] = columnXYZ[1, 0];
        }
        public static Matrix<double> getTransMat(double centerx, double centery, double angle)
        {
            //这个angle是角度，取值是[0-90],但是是顺时针旋转，所以要取负值
            double[,] transTo00 = { {1,0,-centerx },//中心点转到原点
                                    {0,1,-centery},
                                    {0,0,1 } };
            double angleori = (-angle / 180) * Math.PI;//角度转弧度
            double theta = -angleori;
            double[,] transRotate = { { Math.Cos(theta),-Math.Sin(theta),0},
                                            {Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1} };
            var mb = Matrix<double>.Build;
            var transTo00M = mb.DenseOfArray(transTo00);
            var transRotateM = mb.DenseOfArray(transRotate);
            var transM = transRotateM * transTo00M;
            return transM;
        }
    }
}
