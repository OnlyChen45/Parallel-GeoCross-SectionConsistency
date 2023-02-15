using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.IO;
namespace ThreeDModelSystemForSection
{
   public class TopologyOfPoly
    {
        public Dictionary<int, List<int>> poly_arcs_Pairs;//面到弧段的索引
        public Dictionary<int, List<int>> points_arcs_Pairs;//点到弧段的连接
        public Dictionary<int, int[]> arcs_points_Pairs;//弧段的端点index
        public Dictionary<int, int[]> arcs_poly_Pairs;//弧段到面
        public Dictionary<int, Geometry> index_arcs_Pairs;//弧段的index到实体
        public Dictionary<int, Geometry> index_points_Pairs;//点的index的实体
        private List<int> idlist;
        private Dictionary<int, Geometry> polys;
        private Dictionary<int, List<int>> touches;
        /// <summary>
        /// 构造函数，输入多边形的id的表，以及字典存储的多边形
        /// </summary>
        /// <param name="idList"></param>
        /// <param name="polys"></param>
        public TopologyOfPoly( List<int> idList,Dictionary<int, Geometry> polys) {
            this.idlist = idList;
            this.polys = polys;
            poly_arcs_Pairs = new Dictionary<int, List<int>>();
            points_arcs_Pairs = new Dictionary<int, List<int>>();
            arcs_points_Pairs = new Dictionary<int, int[]>();
            arcs_poly_Pairs = new Dictionary<int, int[]>();
            index_arcs_Pairs = new Dictionary<int, Geometry>();
            index_points_Pairs = new Dictionary<int, Geometry>();
            touches = new Dictionary<int, List<int>>();
            for (int i = 0; i < idlist.Count; i++) {//初始化一下
                List<int> newlist = new List<int>();
                touches.Add(idlist[i], newlist);
            }
            for (int i = 0; i < idlist.Count; i++)
            {//初始化一下
                List<int> newlist = new List<int>();
                poly_arcs_Pairs.Add(idlist[i], newlist);
            }
            /*这个初始化是没用的，因为我并不知道有多少个端点
             * for (int i = 0; i < idlist.Count; i++)
            {//初始化一下
                List<int> newlist = new List<int>();
                points_arcs_Pairs.Add(idlist[i], newlist);
            }*/
        }
        /// <summary>
        /// 执行拓扑构建的函数
        /// </summary>
        public void makeTopology() {
            //主要任务是把这些polygongeometry给弄出弧段来，然后编好面到弧段的索引，还有弧段索引表，
            //第一步，先把所有的两面之间的线给弄出来
            //需要考虑两面之间夹得线是一条还是多条，
            int geomCount = polys.Count();

            for (int i = 0; i < geomCount; i++) {//把所有的geometry的touch关系给取得
                int id = idlist[i];
                Geometry geometry = polys[id];
                foreach (var vk in polys) {
                    if (vk.Key != id) {
                        Geometry geom2 = vk.Value;
                        bool b = geom2.Touches(geometry);
                        if (b) {
                            touches[id].Add(vk.Key);
                        }
                    }
                }
            }
            //下面直接把所有touche都做了，获得所有两两相交的多边形之间的线
            foreach (var vk in touches) {
                int id1 = vk.Key;
                Geometry geom1 = polys[id1];
                List<int> touchidlist = vk.Value;
                int touchcount = touchidlist.Count();
                for (int j = 0; j < touchcount; j++) {

                    int id2 = touchidlist[j];
                    bool isFinish = isTwoPolyGotTouchesLines(id1, id2);//判断一下是不是做过这两个多边形的touch，如果做过，就下一个
                    if (isFinish) continue;
                    Geometry geom2 = polys[id2];
  
                    List<Geometry> newlines = getToucheLine(geom1, geom2);
                    #region
                    /*if (id1 == 5 && id2 == 4) {
                        string wkt;
                        foreach (Geometry geometry in newlines) {
                            string wkt11;
                            geometry.ExportToWkt(out wkt11);
                            Console.WriteLine(wkt11);
                        }
                        Console.ReadLine();
                    }*/
                        #endregion
                        int countlines1 = newlines.Count();
                    for (int k = 0; k < countlines1; k++) {
                        Geometry singline1 = newlines[k];
                        int lineindexcount = this.index_arcs_Pairs.Count();
                        this.index_arcs_Pairs.Add(lineindexcount, singline1);//把新生成的弧段加入弧段index表中
                        int[] lrgeomid = new int[2];
                        lrgeomid[0] = id1;
                        lrgeomid[1] = id2;
                        this.arcs_poly_Pairs.Add(lineindexcount , lrgeomid);//把两个面的id加入弧段到面的表中  PS：这里加一个叉积的判断，就可以获得左右关系
                        this.poly_arcs_Pairs[id1].Add(lineindexcount );//在面到弧段的表中把弧段加进去
                        this.poly_arcs_Pairs[id2].Add(lineindexcount);
                    }
                }
            }
            //下面把所有的多边形合并成一个大多边形，然后找他们的外围的线
            Geometry uniongeom = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in polys) {
                Geometry geomtemp = vk.Value;
                uniongeom = uniongeom.Union(geomtemp);
            }
            Geometry outLine = uniongeom.Boundary();
            foreach (var vk in polys) {
                Geometry geomtemp = vk.Value;
                int id = vk.Key;
                bool inter = geomtemp.Intersect(outLine);
                if (inter == false) continue;
                if (id == 9) {
                    Console.WriteLine("测试1");
                }
                List<Geometry> lines = getToucheLine(geomtemp, outLine);
                int countlines1 = lines.Count();
                for (int j = 0; j < countlines1; j++) {
                    Geometry singleline = lines[j];
                    int lineindexcount = this.index_arcs_Pairs.Count();
                    this.index_arcs_Pairs.Add(lineindexcount , singleline);
                    int[] lrgeomid = new int[2];
                    lrgeomid[0] = id;
                    lrgeomid[1] = -1;
                    this.arcs_poly_Pairs.Add(lineindexcount, lrgeomid);
                    this.poly_arcs_Pairs[id].Add(lineindexcount);//在面到弧段的表中把弧段加进去
                }
            }
            //到这，弧段的处理就做完了，应该是获得了完整的arcs_poly_Pairs,poly_arcs_Pairs,以及最基础的index_arcs_Pairs
            //现在要生成index_points_Pairs,points_arcs_Pairs, arcs_points_Pairs
            foreach (var vk in index_arcs_Pairs) {
                //干嘛呢，第一就是遍历所有的线段，
                //取出两端点，
                //查询两个端点是否已经加入了index_points
                //如果加入了，那么就让这个线段关联上这个point的index 再让这个point关联上这个line的index
                //如果没加入，那么就创建新的点，然后再创建新的关联关系，就好了
                Geometry line = vk.Value;
                Geometry pointstart = new Geometry(wkbGeometryType.wkbPoint);
                Geometry pointend = new Geometry(wkbGeometryType.wkbPoint);
                int pointCountInLine = line.GetPointCount();
                double[] pointxy=new double[2];
                pointxy[0] = line.GetX(0);
                pointxy[1] = line.GetY(0);
                pointstart.AddPoint_2D(pointxy[0], pointxy[1]);
                pointxy[0] = line.GetX(pointCountInLine - 1);
                pointxy[1] = line.GetY(pointCountInLine - 1);
                pointend.AddPoint_2D(pointxy[0], pointxy[1]);
                int startid = getPointindex(pointstart);
                int endid = getPointindex(pointend);
                if (startid == -1) {
                    int indexcount = this.index_points_Pairs.Count();
                    this.index_points_Pairs.Add(indexcount, pointstart);
                    List<int> newlist = new List<int>();
                    this.points_arcs_Pairs.Add(indexcount, newlist);
                    startid = indexcount;
                }
                if (endid == -1) {
                    int indexcount = this.index_points_Pairs.Count();
                    this.index_points_Pairs.Add(indexcount, pointend);
                    List<int> newlist = new List<int>();
                    this.points_arcs_Pairs.Add(indexcount, newlist);
                    endid = indexcount;
                }
                int[] pointstartendid = {startid,endid };
                this.arcs_points_Pairs.Add(vk.Key, pointstartendid);//把开始和结束的点id加入线到点表
                this.points_arcs_Pairs[startid].Add(vk.Key);
                this.points_arcs_Pairs[endid].Add(vk.Key);
                //到此为止，拓扑关系就算构建完了
            }
        }
        /// <summary>
        /// 把拓扑关系输出成一个txt，主要是方便查看
        /// </summary>
        /// <param name="txtpath"></param>
        public void outTopologyToText(string txtpath) {
            FileStream fileStream = new FileStream(txtpath, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(fileStream);
            //顺序是，面-弧段，弧段-面，结点-弧段，弧段-结点
            string txtline = "Poly-Arcs";
            writer.WriteLine(txtline);
            Dictionary<int, List<int>> poly_arcs = this.poly_arcs_Pairs;
            foreach (var vk in poly_arcs) {
                int polyindex = vk.Key;
                List<int> arcsindex = vk.Value;
                txtline = "";
                txtline = 'P' + polyindex.ToString();
                int listcount = arcsindex.Count();
                for (int j = 0; j < listcount; j++) {
                    int arcindex = arcsindex[j];
                    txtline = txtline + ",A"+arcindex.ToString();
                }
                writer.WriteLine(txtline);
            }
            txtline = "Arc-Poly";
            writer.WriteLine(txtline);
            Dictionary<int, int[]> arc_poly = this.arcs_poly_Pairs;
            foreach (var vk in arc_poly) {
                int arcindex = vk.Key;
                int[] polyindex = vk.Value;
                txtline = 'A' + arcindex.ToString() + ",P" + polyindex[0].ToString() + ",P" + polyindex[1].ToString();
                writer.WriteLine(txtline);
            }
            txtline = "Node-Arcs";
            writer.WriteLine(txtline);
            Dictionary<int, List<int>> node_Arcs = this.points_arcs_Pairs;
            foreach (var vk in node_Arcs) {
                int nodeindex = vk.Key;
                List<int> arcsindex = vk.Value;
                txtline = 'N' + nodeindex.ToString();
                for (int j = 0; j < arcsindex.Count; j++) {
                    int arcindex = arcsindex[j];
                    txtline = txtline + ",A" + arcindex.ToString();
                }
                writer.WriteLine(txtline);
            }
            txtline = "Arc-Node";
            writer.WriteLine(txtline);
            Dictionary<int, int[]> arcs_Nodes = this.arcs_points_Pairs;
            foreach (var vk in arcs_Nodes) {
                int arcindex = vk.Key;
                int[] nodeindex = vk.Value;
                txtline = 'A' + arcindex.ToString() + ",N" + nodeindex[0].ToString() + ",N" + nodeindex[1].ToString();
                writer.WriteLine(txtline);
            }
            writer.Close();
            fileStream.Close();
            
        }
        /// <summary>
        /// 输出线
        /// </summary>
        /// <param name="shppath"></param>
        /// <param name="layername"></param>
        /// <param name="spatialReference"></param>
        public void saveArcsInshp(string shppath,string layername,SpatialReference spatialReference) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(shppath, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbLineString, null);
            FieldDefn fieldDefn = new FieldDefn("lineid", FieldType.OFTInteger);
            layer.CreateField(fieldDefn,1);
            FieldDefn field1 = new FieldDefn("leftPoly", FieldType.OFTInteger);
            FieldDefn field2 = new FieldDefn("rightPoly", FieldType.OFTInteger);
            FieldDefn field3 = new FieldDefn("point1", FieldType.OFTInteger);
            FieldDefn field4 = new FieldDefn("point2", FieldType.OFTInteger);
            layer.CreateField(field1,1);
            layer.CreateField(field2,1);
            layer.CreateField(field3, 1);
            layer.CreateField(field4, 1);
            Dictionary<int, int[]> arc_poly = this.arcs_poly_Pairs;
            Dictionary<int, int[]> arc_point = this.arcs_points_Pairs;
            Feature feature = new Feature(layer.GetLayerDefn());
            foreach (var vk in arc_poly) {
                int arcid = vk.Key;
                int[] polyid = vk.Value;
                int[] pointid = arc_point[arcid];
                feature.SetField("lineid", arcid);
                feature.SetField("leftPoly", polyid[0]);
                feature.SetField("rightPoly", polyid[1]);
                feature.SetField("point1", pointid[0]);
                feature.SetField("point2", pointid[1]);
                Geometry line = this.index_arcs_Pairs[arcid];
                feature.SetGeometry(line);
                layer.CreateFeature(feature);
            }
            feature.Dispose();
            layer.Dispose();
            ds.Dispose();
        }
        /// <summary>
        /// 打造一个多边形对象出去方便使用
        /// </summary>
        /// <param name="topology"></param>
        public void exportToTopology(out Topology topology) {
            topology = new Topology(poly_arcs_Pairs, points_arcs_Pairs, arcs_points_Pairs, arcs_poly_Pairs, index_arcs_Pairs, index_points_Pairs, idlist, polys);
        }
        /// <summary>
        /// 输出点
        /// </summary>
        /// <param name="shppath"></param>
        /// <param name="layername"></param>
        /// <param name="spatialReference"></param>
        public void savePointsInShp(string shppath, string layername, SpatialReference spatialReference) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(shppath, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPoint, null);
            FieldDefn field1 = new FieldDefn("id", FieldType.OFTInteger);
            layer.CreateField(field1,1);
            Feature feature = new Feature(layer.GetLayerDefn());
            Dictionary<int, Geometry> points = this.index_points_Pairs;
            foreach (var vk in points) {
                int pointid = vk.Key;
                Geometry pointgeom = vk.Value;
                feature.SetField("id", pointid);
                feature.SetGeometry(pointgeom);
                layer.CreateFeature(feature);
            }
            feature.Dispose();
            layer.Dispose();
            ds.Dispose();
        }
        public void savePointsInShp(string shppath, string layername, SpatialReference spatialReference,List<FieldDefn> fields)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(shppath, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPoint, null);
            FieldDefn field1 = new FieldDefn("id", FieldType.OFTInteger);
            layer.CreateField(field1, 1);
            int fieldcount = fields.Count;
            for (int i = 0; i < fieldcount; i++) {
                layer.CreateField(fields[i], 1);
            }
            Feature feature = new Feature(layer.GetLayerDefn());
            Dictionary<int, Geometry> points = this.index_points_Pairs;
            foreach (var vk in points)
            {
                int pointid = vk.Key;
                Geometry pointgeom = vk.Value;
                feature.SetField("id", pointid);
                feature.SetGeometry(pointgeom);
                layer.CreateFeature(feature);
            }
            feature.Dispose();
            layer.Dispose();
            ds.Dispose();
        }
        private int getPointindex(Geometry point) {
            foreach (var vk in index_points_Pairs) {
                Geometry geom = vk.Value;
                if (point.Equal(geom)) {
                    return vk.Key;
                }
            }
            return -1;
        }
        private List<Geometry> getToucheLine(Geometry geom1,Geometry geom2) {
           // bool touchesbool = geom1.Touches(geom2);
            //string wkt1, wkt2;
            //geom1.ExportToWkt(out wkt1);
           // geom2.ExportToWkt(out wkt2);

           // if (touchesbool == false)  return null;
            List<Geometry> result=new List<Geometry>();
            Geometry segments=geom1.Intersection(geom2);
            string wkt;
            //segments.ExportToWkt(out wkt);
            //Console.WriteLine(wkt);
            Geometry lines = switchGeomToMultiline(segments);

            if (lines == null)
            {
                return result;
            }
            lines = Ogr.ForceToLineString(lines);
            lines = Ogr.ForceToLineString(lines);
            wkbGeometryType linesType = lines.GetGeometryType();
            if (linesType == wkbGeometryType.wkbMultiLineString) {
                int geomcount = lines.GetGeometryCount();
                for (int i = 0; i < geomcount; i++) {
                    Geometry singleline = lines.GetGeometryRef(i);
                    result.Add(singleline);
                }
            }
            if (linesType == wkbGeometryType.wkbLineString) {
                result.Add(lines);
            }
            return result;
        }
        private Geometry switchGeomToMultiline(Geometry geom)
        {
            Geometry multiline = new Geometry(wkbGeometryType.wkbMultiLineString);
            wkbGeometryType geomtype = geom.GetGeometryType();
            if (geomtype == wkbGeometryType.wkbLineString) return geom;
            if (geomtype == wkbGeometryType.wkbMultiLineString) return geom;
            if (geomtype == wkbGeometryType.wkbGeometryCollection)
            {
                int count = geom.GetGeometryCount();
                for (int i = 0; i < count; i++)
                {
                    Geometry geomsingle = geom.GetGeometryRef(i);
                    wkbGeometryType geomtypet = geomsingle.GetGeometryType();
                    switch (geomtypet)
                    {
                        case wkbGeometryType.wkbLineString:
                            {//把里边的线拿到
                                multiline.AddGeometry(geomsingle);
                                break;
                            }
                        case wkbGeometryType.wkbMultiLineString:
                            {//把里边的multiline给拆开加进去
                                int linecount2 = geomsingle.GetGeometryCount();
                                for (int j = 0; j < linecount2; j++)
                                {
                                    Geometry geomtt = geomsingle.GetGeometryRef(j);
                                    multiline.AddGeometry(geomtt);
                                }
                                break;
                            }
                    }
                    //其他的情况，比如point，polyline，就都略过
                }
                return multiline;
            }
            return null;
        }
        private bool isTwoPolyGotTouchesLines(int id1,int id2) {
            Dictionary<int, int[]> arcWithGeom = this.arcs_poly_Pairs;
            foreach(var vk in arcWithGeom) {
                int[] geomsid = vk.Value;
                if (geomsid[0] == id1 && geomsid[1] == id2) return true;
                if (geomsid[0] == id2 && geomsid[1] == id1) return true;
            }

            return false;
        }
    }
}
