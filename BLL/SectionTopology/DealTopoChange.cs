using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

using System.Runtime.Serialization.Formatters.Binary;

namespace ThreeDModelSystemForSection
{ 
    /// <summary>
    /// 地层相对位置变化，即拓扑变换的代码，业务层
    /// </summary>
   public class DealTopoChange//这个类是用来处理拓扑变换
    {
        //首先是找到中间的这个地层之间的点，这个通过Topu类来做，
        //然后再把每个点与面的topo的表做了
        //寻找一下没有完全对应的点
        //然后把没有必然对应的线找出来
        //两个图分别做一个并查集，把这些不对应的点连起来，连不上的 就抛掉
        //然后两个并查集，根据相接触的polygon做对应
        //对应完了根据并查集的线，生成buffer，根据对应关系赋予新的lithcode
        //裁剪放进结果
        static public void dealTopoChange(string section1,string section2,string idFieldName,string workspace,string outputsection1,string  outputsection2,double buffer) {
           
            PolygonIO polygonIO1 = new PolygonIO(section1, idFieldName);
            PolygonIO polygonIO2 = new PolygonIO(section2, idFieldName);
            Dictionary<int, Geometry> polys1,polys2;
            List<int> idlist1,idlist2;
            polygonIO1.getGeomAndId(out polys1, out idlist1);
            TopologyOfPoly topologyworker1 = new TopologyOfPoly(idlist1, polys1);
            topologyworker1.makeTopology();
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            TopologyOfPoly topologyworker2 = new TopologyOfPoly(idlist2, polys2);
            topologyworker2.makeTopology();
            Topology topology1, topology2;
            topologyworker1.exportToTopology(out topology1);//做两个面的拓扑表，这样可以方便拿到点
            topologyworker2.exportToTopology(out topology2);
/*            string arcpath1 = @"D:\GISworkspace\createModelForPaper\datadeal\section1To1_3\workspace\arcs1_section1.shp";
            string arcpath2 = @"D:\GISworkspace\createModelForPaper\datadeal\section1To1_3\workspace\arcs2_section1_3.shp";
            string pointpath1 = @"D:\GISworkspace\createModelForPaper\datadeal\section1To1_3\workspace\points1_section1.shp";
            string pointpath2 = @"D:\GISworkspace\createModelForPaper\datadeal\section1To1_3\workspace\points2_section1_3.shp";
            topologyworker1.saveArcsInshp(arcpath1, "lines", polygonIO1.getSpatialRef());
            topologyworker2.saveArcsInshp(arcpath2, "lines", polygonIO2.getSpatialRef());
            topologyworker1.savePointsInShp(pointpath1, "points", polygonIO1.getSpatialRef());
            topologyworker2.savePointsInShp(pointpath2, "points", polygonIO2.getSpatialRef());*/
            //Dictionary<int, List<int>> pointid_polyid_Touches1, pointid_polyid_Touches2;//点所touches的面的id
            /*createPointTouchesPolyList(polys1, topology1.index_points_Pairs, out pointid_polyid_Touches1);
            createPointTouchesPolyList(polys2, topology2.index_points_Pairs, out pointid_polyid_Touches2);//获取了两个图的点与面之间的关系
            List<int> pointnoPair1, pointnoPair2;
            getNoPairPoints(pointid_polyid_Touches1, pointid_polyid_Touches2, out pointnoPair1, out pointnoPair2);
            createPointTouchesPolyList(polys1, topology1.index_points_Pairs, out pointid_polyid_Touches1);
            createPointTouchesPolyList(polys2, topology2.index_points_Pairs, out pointid_polyid_Touches2);//因为老是传引用，所以重做一遍
            List<int> arcnoPair1, arcnoPair2;
            getNoPairArcs(topology1, topology2, out arcnoPair1, out arcnoPair2);//获得了有用的线的id
            Dictionary<int, List<int>> pointid_arclist1, pointid_arclist2;
            Dictionary<int, int> pointlink1, pointlink2;
            getConnectedblock(pointnoPair1, arcnoPair1, topology1.arcs_points_Pairs, out pointid_arclist1,out pointlink1);
            getConnectedblock(pointnoPair2, arcnoPair2, topology2.arcs_points_Pairs, out pointid_arclist2,out pointlink2);
            //找到连通块之后，通过相touches的面来做对应。
            List<int> headlink1 = pointid_arclist1.Keys.ToList<int>();
            List<int> headlink2 = pointid_arclist2.Keys.ToList<int>();
            Dictionary<int, int> headpair = new Dictionary<int, int>();
            foreach (int idt in headlink1) {
                List<int> pointids1 = new List<int>();
                List<int> polyids1,polyids2;
              
                foreach (var vk in pointlink1)
                {
                    if (vk.Value == idt) pointids1.Add(vk.Key);
                }
                getCommonPolyid(pointids1, pointid_polyid_Touches1, out polyids1);
                foreach (int idt2 in headlink2) {
                   
                    List<int> pointids2 = new List<int>();

                    foreach (var vk in pointlink2){
                        if (vk.Value == idt2) pointids2.Add(vk.Key);
                    }//求出连通点
                    getCommonPolyid(pointids2, pointid_polyid_Touches2, out polyids2);
                    if (listintEqual(polyids1, polyids2)) {
                        headpair.Add(idt, idt2);//找到了对应的群
                        break;
                    }
                }
            }*/
            List<int[]> topochangeline1, topochangeline2;
            Dictionary<int, List<int>> pointid_arclist1, pointid_arclist2;
            FindTopoChangeByMultiLine.findTopoChange(topology1, topology2, polys1, polys2, out topochangeline1, out topochangeline2);
            maketemptranse(topochangeline1, out pointid_arclist1);
            maketemptranse(topochangeline2, out pointid_arclist2);
            Dictionary<int, Geometry> buffers1, buffers2,buffers1reNumber,buffers2reNumber;
            //到此为止，找到了对应的群了，拿到了全部的数据
            //下面就是给每个连通块都做buffer，然后再搞对应
            createBuffersByTouches(topology1.index_arcs_Pairs, pointid_arclist1, out buffers1, buffer);
            createBuffersByTouches(topology2.index_arcs_Pairs, pointid_arclist2, out buffers2, buffer);
            string bufferpath1 = workspace + "\\buffer1.shp";
            string bufferpath2 = workspace + "\\buffer2.shp";
            //在这缺了一个新建两个buffer重新编号的Dictionary<int,Geometry>的过程
            buffers1reNumber = new Dictionary<int, Geometry>();
            buffers2reNumber = new Dictionary<int, Geometry>();
            /*foreach (var pair in headpair) {
                int idnew = 1000 + pair.Key;
                buffers1reNumber.Add(idnew, buffers1[pair.Key]);
                buffers2reNumber.Add(idnew, buffers2[pair.Value]);
            }*/
            for(int i=0;i<buffers1.Count;i++)
            {
                int idnew = 1000 + i;
                buffers1reNumber.Add(idnew, buffers1[i]);
                buffers2reNumber.Add(idnew, buffers2[i]);
            }
            saveDictionaryGeom(buffers1reNumber, bufferpath1, idFieldName, polygonIO1.getSpatialRef());//为了能够使用erase函数，先要把擦除用的数据新建成一个文件
            saveDictionaryGeom(buffers2reNumber, bufferpath2, idFieldName, polygonIO2.getSpatialRef());
            createEraseBufferData(outputsection1, "section1", idFieldName, bufferpath1, section1, polygonIO1.getSpatialRef());
            createEraseBufferData(outputsection2, "section2", idFieldName, bufferpath2, section2, polygonIO2.getSpatialRef());
           // MatchLithIDForSections.MatchLayer matchLayer1 = new MatchLithIDForSections.MatchLayer(section1, outputsection1, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
            //MatchLithIDForSections.MatchLayer matchLayer2 = new MatchLithIDForSections.MatchLayer(section2, outputsection2, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);

        }
        static public void dealTopoChange(string section1, string section2, string idFieldName, string workspace, string outputsection1, string outputsection2, string orisectionpath1,string orisectionpath2, double buffer)
        {
            PolygonIO polygonIO1 = new PolygonIO(section1, idFieldName);
            PolygonIO polygonIO2 = new PolygonIO(section2, idFieldName);
            Dictionary<int, Geometry> polys1, polys2;
            List<int> idlist1, idlist2;
            polygonIO1.getGeomAndId(out polys1, out idlist1);
            TopologyOfPoly topologyworker1 = new TopologyOfPoly(idlist1, polys1);
            topologyworker1.makeTopology();
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            TopologyOfPoly topologyworker2 = new TopologyOfPoly(idlist2, polys2);
            topologyworker2.makeTopology();
            Topology topology1, topology2;
            topologyworker1.exportToTopology(out topology1);//做两个面的拓扑表，这样可以方便拿到点
            topologyworker2.exportToTopology(out topology2);
            //topologyworker1.saveArcsInshp(@"D:\GISworkspace\QS69\topotemp\topo1.shp","line",polygonIO1.getSpatialRef());
            //topologyworker2.saveArcsInshp(@"D:\GISworkspace\QS69\topotemp\topo2.shp", "line", polygonIO2.getSpatialRef());
            List<int[]> topochangeline1, topochangeline2;
            Dictionary<int, List<int>> pointid_arclist1, pointid_arclist2;
            FindTopoChangeByMultiLine.findTopoChange(topology1, topology2, polys1, polys2, out topochangeline1, out topochangeline2);
            maketemptranse(topochangeline1, out pointid_arclist1);
            maketemptranse(topochangeline2, out pointid_arclist2);
            Dictionary<int, Geometry> buffers1, buffers2, buffers1reNumber, buffers2reNumber;
            //到此为止，找到了对应的群了，拿到了全部的数据
            //下面就是给每个连通块都做buffer，然后再搞对应
            createBuffersByTouches(topology1.index_arcs_Pairs, pointid_arclist1, out buffers1, buffer);
            createBuffersByTouches(topology2.index_arcs_Pairs, pointid_arclist2, out buffers2, buffer);
            string bufferpath1 = workspace + "\\buffer1.shp";
            string bufferpath2 = workspace + "\\buffer2.shp";
            //在这缺了一个新建两个buffer重新编号的Dictionary<int,Geometry>的过程
            buffers1reNumber = new Dictionary<int, Geometry>();
            buffers2reNumber = new Dictionary<int, Geometry>();
            /*foreach (var pair in headpair) {
                int idnew = 1000 + pair.Key;
                buffers1reNumber.Add(idnew, buffers1[pair.Key]);
                buffers2reNumber.Add(idnew, buffers2[pair.Value]);
            }*/
            int maxid1= polygonIO1.getMaxid();//找到最大的id，防止新增id冲突
            int maxid2 = polygonIO2.getMaxid();
            if (maxid1 < maxid2) maxid1 = maxid2;
            for (int i = 0; i < buffers1.Count; i++)
            {
                int idnew = maxid1+1000 + i;
                buffers1reNumber.Add(idnew, buffers1[i]);
                buffers2reNumber.Add(idnew, buffers2[i]);
            }
            saveDictionaryGeom(buffers1reNumber, bufferpath1, idFieldName, polygonIO1.getSpatialRef());//为了能够使用erase函数，先要把擦除用的数据新建成一个文件
            saveDictionaryGeom(buffers2reNumber, bufferpath2, idFieldName, polygonIO2.getSpatialRef());
            createEraseBufferData(outputsection1, "section1", idFieldName, bufferpath1, orisectionpath1, polygonIO1.getSpatialRef());
            createEraseBufferData(outputsection2, "section2", idFieldName, bufferpath2, orisectionpath2, polygonIO2.getSpatialRef());
            // MatchLithIDForSections.MatchLayer matchLayer1 = new MatchLithIDForSections.MatchLayer(section1, outputsection1, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
            //MatchLithIDForSections.MatchLayer matchLayer2 = new MatchLithIDForSections.MatchLayer(section2, outputsection2, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
        }
        static public void dealTopoChange(string section1, string section2, string idFieldName, string workspace, string outputsection1, string outputsection2, string orisectionpath1, string orisectionpath2, double buffer,out string bufferpath1,out string bufferpath2)
        {
            PolygonIO polygonIO1 = new PolygonIO(section1, idFieldName);
            PolygonIO polygonIO2 = new PolygonIO(section2, idFieldName);
            Dictionary<int, Geometry> polys1, polys2;
            List<int> idlist1, idlist2;
            polygonIO1.getGeomAndId(out polys1, out idlist1);
            TopologyOfPoly topologyworker1 = new TopologyOfPoly(idlist1, polys1);
            topologyworker1.makeTopology();
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            TopologyOfPoly topologyworker2 = new TopologyOfPoly(idlist2, polys2);
            topologyworker2.makeTopology();
            Topology topology1, topology2;
            topologyworker1.exportToTopology(out topology1);//做两个面的拓扑表，这样可以方便拿到点
            topologyworker2.exportToTopology(out topology2);
            List<int[]> topochangeline1, topochangeline2;
            Dictionary<int, List<int>> pointid_arclist1, pointid_arclist2;
            FindTopoChangeByMultiLine.findTopoChange(topology1, topology2, polys1, polys2, out topochangeline1, out topochangeline2);
            maketemptranse(topochangeline1, out pointid_arclist1);
            maketemptranse(topochangeline2, out pointid_arclist2);
            Dictionary<int, Geometry> buffers1, buffers2, buffers1reNumber, buffers2reNumber;
            //到此为止，找到了对应的群了，拿到了全部的数据
            //下面就是给每个连通块都做buffer，然后再搞对应
            createBuffersByTouches(topology1.index_arcs_Pairs, pointid_arclist1, out buffers1, buffer);
            createBuffersByTouches(topology2.index_arcs_Pairs, pointid_arclist2, out buffers2, buffer);
            bufferpath1 = workspace + "\\buffer1.shp";
            bufferpath2 = workspace + "\\buffer2.shp";
            //在这缺了一个新建两个buffer重新编号的Dictionary<int,Geometry>的过程
            buffers1reNumber = new Dictionary<int, Geometry>();
            buffers2reNumber = new Dictionary<int, Geometry>();
            /*foreach (var pair in headpair) {
                int idnew = 1000 + pair.Key;
                buffers1reNumber.Add(idnew, buffers1[pair.Key]);
                buffers2reNumber.Add(idnew, buffers2[pair.Value]);
            }*/
            for (int i = 0; i < buffers1.Count; i++)
            {
                int idnew = 1000 + i;
                buffers1reNumber.Add(idnew, buffers1[i]);
                buffers2reNumber.Add(idnew, buffers2[i]);
            }
            saveDictionaryGeom(buffers1reNumber, bufferpath1, idFieldName, polygonIO1.getSpatialRef());//为了能够使用erase函数，先要把擦除用的数据新建成一个文件
            saveDictionaryGeom(buffers2reNumber, bufferpath2, idFieldName, polygonIO2.getSpatialRef());
            createEraseBufferData(outputsection1, "section1", idFieldName, bufferpath1, orisectionpath1, polygonIO1.getSpatialRef());
            createEraseBufferData(outputsection2, "section2", idFieldName, bufferpath2, orisectionpath2, polygonIO2.getSpatialRef());
            // MatchLithIDForSections.MatchLayer matchLayer1 = new MatchLithIDForSections.MatchLayer(section1, outputsection1, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
            //MatchLithIDForSections.MatchLayer matchLayer2 = new MatchLithIDForSections.MatchLayer(section2, outputsection2, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
        }
        public static void maketemptranse(List<int[]> arrlist,out Dictionary<int, List<int>> listdic) {
            //用的一个简单的转换
            listdic = new Dictionary<int, List<int>>();
            for (int i=0;i< arrlist.Count;i++) {
                int[] arr = arrlist[i];
                List<int> line = new List<int>(arr);
                listdic.Add(i, line);
            }
        }
        /// <summary>
        /// 得到一个对象的克隆(二进制的序列化和反序列化)--需要标记可序列化
        /// </summary>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }
        static void saveDictionaryGeom(Dictionary<int, Geometry> buffers, string path, string idname, SpatialReference spatialReference)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Layer layer = dataSource.CreateLayer("buffer", spatialReference, wkbGeometryType.wkbPolygon, null);
            FieldDefn fieldDefn = new FieldDefn(idname, FieldType.OFTInteger);
            layer.CreateField(fieldDefn, 1);
            Feature feature = new Feature(layer.GetLayerDefn());
            foreach (var vk in buffers)
            {
                int id = vk.Key;
                Geometry ge = vk.Value;
                feature.SetField(idname, id);
                feature.SetGeometry(ge);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        static public  void createEraseBufferData(string outputpath, string layername, string idfieldname, string bufferpath, string sectionpath, SpatialReference spatialReference=null)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(bufferpath, 1);  
            Layer bufferlayer = dataSource.GetLayerByIndex(0); 
            DataSource dataSource1=driver.Open(sectionpath, 1);
            Layer sectionlayer = dataSource1.GetLayerByIndex(0);
            //OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(outputpath, null);
            if (spatialReference == null) {
                spatialReference = sectionlayer.GetSpatialRef();
            }
            Layer layer2 = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPolygon, null);
            sectionlayer.Erase(bufferlayer, layer2, null, null, null);
            // int t = (int)layer2.GetFeatureCount(0);
            long buffercount = bufferlayer.GetFeatureCount(1);
            Feature layer2feature = new Feature(layer2.GetLayerDefn());
            for (int i = 0; i < buffercount; i++)
            {
                Feature feature = bufferlayer.GetFeature(i);
                Geometry geometry = feature.GetGeometryRef();
                int lith = feature.GetFieldAsInteger(idfieldname);
                layer2feature.SetGeometry(geometry);
                layer2feature.SetField(idfieldname, lith);
                layer2.CreateFeature(layer2feature);
            }
            bufferlayer.Dispose();
            sectionlayer.Dispose();
            layer2.Dispose();
            dataSource.Dispose();
            dataSource1.Dispose();
            ds.Dispose();
        }
        static void createBuffersByTouches(Dictionary<int ,Geometry> arcs, Dictionary<int, List<int>> pointid_arclist,out Dictionary<int ,Geometry>buffers,double distance, int quadeses=30) {
            buffers = new Dictionary<int, Geometry>();
           
            foreach (var point_arcs in pointid_arclist) {
                List<Geometry> lines = new List<Geometry>();
                int idtemp = point_arcs.Key;
                List<int> lineids = point_arcs.Value;
                foreach (int lineid in lineids) {
                    lines.Add(arcs[lineid]);
                }
                Geometry union = new Geometry(wkbGeometryType.wkbPolygon);
                foreach (Geometry ge in lines)
                {
                    Geometry genew = ge.Buffer(distance, quadeses);
                    union = union.Union(genew);
                }
                buffers.Add(point_arcs.Key, union);
            }
            
        }
        static void getCommonPolyid(List<int>pointids, Dictionary<int, List<int>> pointid_polyid_Touches,out List<int> commonpoly) {
            commonpoly = new List<int>();
            foreach (int id in pointids) {
                List<int> touchepoly = pointid_polyid_Touches[id];
                foreach(int polyid in touchepoly)
                {
                    if (commonpoly.Contains(polyid) == false) {
                        commonpoly.Add(polyid);
                    }
                }
            }
        }
        static void getConnectedblock(List<int> pointnoPair, List<int> arcnoPair1,Dictionary<int,int[]> arcs_points,out Dictionary<int,List<int>> pointid_arclist,out Dictionary<int, int> pointlink) {
            //这个函数求出连通块，然后返回一个用核心点的id（实际上没意义）组织的线的集合
            List<List<int>> arcblock = new List<List<int>>();
            pointid_arclist = new Dictionary<int, List<int>>();
            int n = pointnoPair.Count;
            Dictionary<int,int> fatherarr = new  Dictionary<int, int>();
            for (int i = 0; i < n; i++) {//初始化并查集数组
                fatherarr.Add(pointnoPair[i], pointnoPair[i]);
                List<int> arclist = new List<int>();
                pointid_arclist.Add(pointnoPair[i], arclist);//初始化用点id标志的结果，在使用的时候，如果两个点的id合并了，那么就直接合并它
            }
            // foreach (var line in arcs_points)
            foreach (int lineid in arcnoPair1)
            {
                int[] duandian = arcs_points[lineid];
                if (fatherarr.ContainsKey(duandian[0]) == false) continue;
                if (fatherarr.ContainsKey(duandian[1]) == false) continue;
                int x = find(duandian[0], ref fatherarr, ref pointid_arclist);
                int y = find(duandian[1], ref fatherarr, ref pointid_arclist);
                if (pointid_arclist.ContainsKey(x))
                {
                    pointid_arclist[y].AddRange(pointid_arclist[x]);//先把x的内容加给y
                    pointid_arclist.Remove(x);
                }
                pointid_arclist[y].Add(lineid);//再把这个线加给y
                //把x的独立抹掉
                fatherarr[x] = y;//更新x的father
            }
            List<int> pointid = pointid_arclist.Keys.ToList<int>();
            foreach (int id in pointid) {
                if (pointid_arclist[id].Count == 0) {
                    pointid_arclist.Remove(id);
                }
            }
            pointlink = fatherarr;
        }
        static int find(int x,ref Dictionary<int, int> father,ref Dictionary<int, List<int>> pointid_arclist) {
            if (x == father[x]) return x;
            else {
                int fx = find(father[x], ref father, ref pointid_arclist);
                if (pointid_arclist.ContainsKey(x))
                {
                    pointid_arclist[fx].AddRange(pointid_arclist[x]);//把x代表的结点抹掉
                    pointid_arclist.Remove(x);
                }
                father[x] = fx;
                return father[x];
            }
        }
        static void getNoPairArcs(Topology topo1,Topology topo2,out List<int> arcnoPair1,out List<int> arcnoPair2) {
            //Dictionary<int,Geometry> arcs1,Dictionary<int,int[]>arc_poly1, Dictionary<int, Geometry> arcs2, Dictionary<int, int[]> arc_poly2
           // IndexPairs arcpairs = new IndexPairs();//初始化结果
            Dictionary<int, int[]> arc_Poly1 = topo1.arcs_poly_Pairs;//获取第一个topo的线与面关系
            Dictionary<int, int[]> arc_Poly2 = topo2.arcs_poly_Pairs;//获取第二个topo的线与面关系
            arcnoPair1 = new List<int>(topo1.index_arcs_Pairs.Keys.ToArray<int>());
            arcnoPair2 = new List<int>(topo2.index_arcs_Pairs.Keys.ToArray<int>());
            // MultiKeyDictionary<int, int, int> topo1Pair2 = new MultiKeyDictionary<int, int, int>();
            //MultiKeyDictionary<int, int, int> topo2Pair1 = new MultiKeyDictionary<int, int, int>();
            Dictionary<int, int> paircount1 ;//记录一下每条线都对应几个另外的线
            Dictionary<int, List<int>> arcpair1to2;
            Dictionary<int, int> paircount2 ;
            Dictionary<int, List<int>> arcpair2to1;
            Dictionary<int, int[]> arc_Poly1temp1 = Clone(arc_Poly1) as Dictionary<int, int[]>;
            Dictionary<int, int[]> arc_Poly2temp1 = Clone(arc_Poly2) as Dictionary<int, int[]>;
            Dictionary<int, int[]> arc_Poly1temp2 = Clone(arc_Poly1) as Dictionary<int, int[]>;
            Dictionary<int, int[]> arc_Poly2temp2 = Clone(arc_Poly2) as Dictionary<int, int[]>;
            getIdsPair(arc_Poly1temp1, arc_Poly2temp1, out paircount1, out arcpair1to2);
            getIdsPair(arc_Poly2temp2, arc_Poly1temp2, out paircount2, out arcpair2to1);
            foreach (var linepair1 in paircount1) {
                int lineid = linepair1.Key;
                int countpair = linepair1.Value;
                switch (countpair) {
                    case 0: { 
                            //当完全找不到对应的时候，说明这个边实际上是一定是处在拓扑关系变动状态下的
                            //说明不用动了，直接就确定要在结果的ArcNoPair1里边
                            //同样，图2里边的找不到涉及不到arc，也应该在ArcNoPair2里边
                            //所以这里不用写东西
                            break;
                        }
                    case 1: {
                            //找到一个对应的时候，确认一下那个对应对应这里边几个面，如果只对应一个，那么就去掉
                            int lineid2 = arcpair1to2[lineid][0];
                            int id2count = paircount2[lineid2];
                            if (id2count == 1)
                            {//这两条线一一对应，那就没有不一致的事情了，直接从不一致数组里给抹掉就行了
                                arcnoPair1.Remove(lineid);
                                arcnoPair2.Remove(lineid2);
                            }
                            else {
                                //这个情况是一对多的情况，实际上这些都有不太对应的问题，但是肯定有一个是能对上的吧。。。我想想，我觉得1对多应该是把这个1拿掉吧
                               // arcnoPair1.Remove(lineid);
                            }
                            break;
                        }
                    default: {
                            //这就是一对多状态了，，一对多的话，看看同图里边有没有完全一样的，有几个，对面图中有没有完全一样的，有几个
                            //如果数量相等，说明也没什么大问题，那么就都去掉，如果数量不等，那么就不去掉。
                            int lineid2 = arcpair1to2[lineid][0];
                            int id2count = paircount2[lineid2];
                            if (id2count == 1)
                            {
                                //多对一，
                               // arcnoPair2.Remove(lineid2);
                            }
                            else {//多对多且数量相等
                                if (countpair == lineid2) {
                                    List<int> idlist1 = arcpair2to1[lineid2];//拿到图1中的id
                                    List<int> idlist2 = arcpair1to2[countpair];
                                    foreach (int id in idlist1) {
                                        arcnoPair1.Remove(id);
                                    }
                                    foreach (int id in idlist2) {
                                        arcnoPair2.Remove(id);
                                    }
                                }
                            }
                            //另外有个多对多且数量不等，这时候就不要动了。
                            break;
                        }
                }
            
            }
            /* foreach (var vk in arc_Poly1)
             {//遍历所有的线
                 int[] poly = vk.Value;//获取两侧的面
                 int collectcount = 0;//收集同拓扑关系线的数量
                 int id1, id2;
                 id1 = vk.Key;
                 List<int> pairids = new List<int>();
                 foreach (var vk2 in arc_Poly2)
                 {//遍历第二个topo中的线，找出有几个相同topu关系的线
                     int[] poly2 = vk2.Value;
                     if ((poly2[0] == poly[0] && poly2[1] == poly[1]) || (poly2[1] == poly[0] && poly2[0] == poly[1]))
                     {
                         collectcount += 1;//找到相同关系的的线 
                         id2 = vk2.Key;
                         pairids.Add(id2);
                     }
                 }
                 if (collectcount == 1)
                 {//如果对应关系是唯一的，那么就添加到对应列表
                     arcnoPair1.Remove(id1);
                     arcnoPair2.Remove(id2);
                 }
                 paircount1.Add(id1, collectcount);
                 arcpair1to2.Add(id1, pairids);
             }*/
        }
        static void getIdsPair(Dictionary<int, int[]> arc_Poly1, Dictionary<int, int[]> arc_Poly2, out Dictionary<int, int> paircount1, out Dictionary<int, List<int>> arcpair1to2) {
            //为一图中的线找到它对应的线的列表
            paircount1 = new Dictionary<int, int>();//记录一下
            arcpair1to2 = new Dictionary<int, List<int>>();
            foreach (var vk in arc_Poly1)
            {//遍历所有的线
                int[] poly = vk.Value;//获取两侧的面
                int collectcount = 0;//收集同拓扑关系线的数量
                int id1, id2;
                id1 = vk.Key;
                List<int> pairids = new List<int>();
                foreach (var vk2 in arc_Poly2)
                {//遍历第二个topo中的线，找出有几个相同topu关系的线
                    int[] poly2 = vk2.Value;
                    if ((poly2[0] == poly[0] && poly2[1] == poly[1]) || (poly2[1] == poly[0] && poly2[0] == poly[1]))
                    {
                        collectcount += 1;//找到相同关系的的线 
                        id2 = vk2.Key;
                        pairids.Add(id2);
                    }
                }
                /*if (collectcount == 1)
                {//如果对应关系是唯一的，那么就添加到对应列表
                    arcnoPair1.Remove(id1);
                    arcnoPair2.Remove(id2);
                }*/
                paircount1.Add(id1, collectcount);
                arcpair1to2.Add(id1, pairids);
            }
        }
        static void getNoPairPoints(Dictionary<int, List<int>> pointid_polyid_Touches1, Dictionary<int, List<int>> pointid_polyid_Touches2,out List<int> pointnoPair1,out List<int> pointnoPair2) {
            //找出没有对应的点
            pointnoPair1 = new List<int>(pointid_polyid_Touches1.Keys.ToArray<int>());//初始化结果数组，
            pointnoPair2 = new List<int>(pointid_polyid_Touches2.Keys.ToArray<int>());
            //只有一一对应的点才算找到，其他的不算。
            //线也是，只有一一对应才算找到
            Dictionary<int, int> pointpaircount1, pointpaircount2;
            Dictionary<int, List<int>> pointpairlist1, pointpairlist2;
            //Dictionary<int, List<int>> pointid_polyid_Touches1temp = new Dictionary<int, List<int>>(pointid_polyid_Touches1);
            //Dictionary<int, List<int>> pointid_polyid_Touches2temp = new Dictionary<int, List<int>>(pointid_polyid_Touches2);
            Dictionary<int, List<int>> pointid_polyid_Touches1temp = Clone( pointid_polyid_Touches1) as  Dictionary<int, List<int>>;
            Dictionary<int, List<int>> pointid_polyid_Touches2temp = Clone(pointid_polyid_Touches2) as Dictionary<int, List<int>>;
            Dictionary<int, List<int>> pointid_polyid_Touches1temp2 = Clone(pointid_polyid_Touches1) as Dictionary<int, List<int>>;
            Dictionary<int, List<int>> pointid_polyid_Touches2temp2 = Clone(pointid_polyid_Touches2) as Dictionary<int, List<int>>;
            getpointPair(pointid_polyid_Touches1temp, pointid_polyid_Touches2temp, out pointpaircount1, out pointpairlist1);//找出来点的匹配表
            getpointPair(pointid_polyid_Touches2temp2, pointid_polyid_Touches1temp2, out pointpaircount2, out pointpairlist2);

            foreach (var pointt in pointpaircount1) {//遍历一下这个点的对应的函数
                int pointid = pointt.Key;
                int pointcount = pointt.Value;
                switch (pointcount){
                    case 0:
                        {
                            //对应0个点，说明很有必要放在里边
                            break;
                        }
                    case 1: {
                            //对应一个点，应该检查一下对面的那个点是不是也对应一个点
                            int point2id = pointpairlist1[pointid][0];
                            int point2count = pointpaircount2[point2id];
                            if (point2count == 1) {//如果是1对1，那么就直接给抹下去
                                pointnoPair1.Remove(pointid);
                                pointnoPair2.Remove(point2id);
                            }
                            break;
                        }
                    default: {
                            //这就是一对多的情况，，，额
                            //一对多，还是多对多，就找一下，如果是多对多，就要给它处理一下
                            int point2id = pointpairlist1[pointid][0];
                            int point2count = pointpaircount2[point2id];
                            if (point2count == pointcount) { //如果两个点的对应数量相等，那么都给它们挪走
                                List<int> listid1 = pointpairlist2[point2id];//取出图1里边的id列表
                                List<int> listid2 = pointpairlist1[pointid];//取出图1中的列表
                                foreach (int id in listid1)
                                {
                                    pointnoPair1.Remove(id);
                                }
                                foreach (int id in listid2)
                                {
                                    pointnoPair2.Remove(id);
                                }
                            }
                            break; }
                }
            }
            /*
             * 
             * foreach (var point1 in pointid_polyid_Touches1) {
                int count = 0;
                int findid = -1;
                foreach (var point2 in pointid_polyid_Touches2) {
                    bool equ = listintEqual(point1.Value, point2.Value);
                    if (equ) { count++;findid = point2.Key; }
                }
                if (count == 1) {//找到了一一对应的点之后就给他们去掉
                    pointnoPair1.Remove(point1.Key);
                    pointnoPair2.Remove(findid);
                }
                
            }*/
        }
        static void getpointPair(Dictionary<int, List<int>> pointid_polyid_Touches1, Dictionary<int, List<int>> pointid_polyid_Touches2, out Dictionary<int, int> pointpaircount,out Dictionary<int, List<int>> pointpairlist) {
            //为一图中的点找到它可能是对应的点
            pointpaircount = new Dictionary<int, int>();
            pointpairlist = new Dictionary<int, List<int>>();
            foreach (var point1 in pointid_polyid_Touches1)
            {
                int count = 0;
                int findid = -1;
                List<int> findlist = new List<int>();
                foreach (var point2 in pointid_polyid_Touches2)
                {
                    bool equ = listintEqual(point1.Value, point2.Value);
                    if (equ) { count++; findid = point2.Key;
                        findlist.Add(point2.Key);
                    }
                }
                pointpaircount.Add(point1.Key, count);
                pointpairlist.Add(point1.Key, findlist);
                /*if (count == 1)
                {//找到了一一对应的点之后就给他们去掉
                    pointnoPair1.Remove(point1.Key);
                    pointnoPair2.Remove(findid);
                }*/

            }
        }
        static void createPointTouchesPolyList(Dictionary<int, Geometry> polys, Dictionary<int, Geometry> points, out Dictionary<int, List<int>> pointid_polyid_Touches) {
            pointid_polyid_Touches = new Dictionary<int, List<int>>();
            foreach (var point in points) {
                List<int> toucheslist = new List<int>();
                Geometry p = point.Value;
                foreach (var poly in polys) {
                    Geometry po = poly.Value;
                    if (p.Intersect(po)) {
                        toucheslist.Add(poly.Key);
                    }
                }
                pointid_polyid_Touches.Add(point.Key, toucheslist);
            }
            //下面要把最外开放边界给搞定，标记为-1
            Geometry superSection = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var poly in polys) {
                superSection = superSection.Union(poly.Value);}
            Geometry boundary = superSection.Boundary();
            foreach (var point in points) {
                Geometry p = point.Value;
                if (p.Intersect(boundary))
                {
                    pointid_polyid_Touches[point.Key].Add(-1);
                }
            }
        }
        static bool listintEqual(List<int> list1,List<int> list2) {
            foreach (int t in list1) {
                bool tb = list2.Contains(t);
                if (tb == false) return false;
                list2.Remove(t);
            }
            return true;
        }
    }
}
