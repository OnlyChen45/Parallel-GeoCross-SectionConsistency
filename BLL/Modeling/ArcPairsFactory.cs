using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using GeoCommon;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 用来制作弧段对应关系的一个类，业务逻辑层，建模模块
    /// </summary>
    public class ArcPairsFactory
    {
        
        public ArcPairsFactory() { 
        //这个类就是在读取之后，把这个arcpair组装正确便于建模
        }
        //public ArcPairsFactory
        public void CreateArcPairs(Dictionary<int, Geometry> arcs1, Dictionary<int, int[]> arc_polys1, Dictionary<int, int[]> arc_points1, Dictionary<int, Geometry> points1,
            Dictionary<int, Geometry> arcs2,  Dictionary<int, int[]> arc_polys2, Dictionary<int, int[]> arc_points2, Dictionary<int, Geometry> points2,
            Dictionary<int, int> arcparis,Dictionary<int, int> pointpairs,string name1,string name2, out List<ModelWithArc> modelWithArcs) {
            //怎么创建呢，，怎么创建呢
            //理一下思路，首先肯定是从1到2 ，因为dictionary是从1到2，
            //首先就有把arcs1,arcs2都转成eage,这时候需要辅助人为输入的name
            //获得了eage之后，寻找它两端的端点的对应，看看是不是需要逆置，如果需要逆置就逆置
            //逆置完成之后包装成一个arcpair
            modelWithArcs = new List<ModelWithArc>();
            Dictionary<int, Eage> arc_eage1 = GeomsToEages(arcs1, name1);//获取了arc对应的eage
            Dictionary<int, Eage> arc_eage2 = GeomsToEages(arcs2, name2);
            //下面开始检查是否需要把eage逆置
            List<int> arcsid = arcs1.Keys.ToList<int>();//把arcs的id都拿到
            int idcount = arcsid.Count;
            for (int i = 0; i < idcount; i++) { 
                int id1 = arcsid[i];
                Eage eage1 = arc_eage1[id1];
                int id2 = arcparis[id1];
                Eage eage2 = arc_eage2[id2];
                int[] pointid1 = arc_points1[id1];
                int[] pointid2 = arc_points2[id2];
                Geometry point1_1 = points1[pointid1[0]];
                Geometry point1_2 = points1[pointid1[1]];
                Geometry point2_1 = points2[pointid2[0]];
                Geometry point2_2 = points2[pointid2[1]];
                Vertex firstvertex = eage1.vertexList[0];
                bool point_eage_order = comparexyAndGeompoint(point1_1, firstvertex.x, firstvertex.y, firstvertex.z);//看看这个eage是不是和pointid顺序对应，如果不对应的话，就调换一下顺序
                if (point_eage_order == false) {
                    exchangeint(ref pointid1[0], ref pointid1[1]);
                    point1_1 = points1[pointid1[0]];
                    point1_2 = points1[pointid1[1]];
                }
                Vertex firstvertex2 = eage2.vertexList[0];
                bool point_eage_order2 = comparexyAndGeompoint(point2_1, firstvertex2.x, firstvertex2.y, firstvertex2.z);//把这个第二条eage也换成正确顺序的
                if (point_eage_order2 == false) {
                    exchangeint(ref pointid2[0], ref pointid2[1]);
                    point2_1 = points2[pointid2[0]];
                    point2_2 = points2[pointid2[1]];
                }
                int point1_1pair = pointpairs[pointid1[0]];//获取到开头点的对应的点的id
                bool needInvert = !(point1_1pair == pointid2[0]);//判断一下第二个弧段的开头点的id是否和第一个点所应当对应的开头点的id一致，一致为false，不一致为true
                if (needInvert) {
                    Eage tempeage = inverteEage(eage2);
                    eage2 = tempeage;
                }
                //目前为止，找到了对应的eage，并调整了eage的顺序，使得可以直接去建模了。
                ArcSe arcSe1 = new ArcSe(eage1.vertexList[0],eage1.vertexList[eage1.vertexList.Count-1]);
                ArcSe arcSe2= new ArcSe(eage2.vertexList[0], eage2.vertexList[eage2.vertexList.Count - 1]);
                arcSe1.id = id1;
                arcSe2.id = id2;
                arcSe1.setEage(eage1);
                arcSe2.setEage(eage2);
                //在这加一下对于点id的记录，方便后边贝塞尔
                ModelWithArc modelWithArc = new ModelWithArc(arcSe1, arcSe2);
                modelWithArc.arc1firstpoint = pointid1[0];
                modelWithArc.arc1endpoint = pointid1[1];
                modelWithArc.arc2firstpoint = pointid2[0];
                modelWithArc.arc2endpoint = pointid2[1];
                if (needInvert) {
                    modelWithArc.arc2firstpoint = pointid2[1];
                    modelWithArc.arc2endpoint = pointid2[0];
                }
                modelWithArcs.Add(modelWithArc);
            }
        }
      
        private void exchangeint(ref int a,ref int b) {
            int t = a;
            a = b;
            b = t;
        }
        private Dictionary<int, Eage> GeomsToEages(Dictionary<int,Geometry> arcindexs,string eagename) {
            int arcscount = arcindexs.Count;
            Dictionary<int, Eage> result = new Dictionary<int, Eage>();
            foreach (var vk in arcindexs) {
                int id = vk.Key;
                Geometry line = vk.Value;
                Eage eage = new Eage();
                eage.name = eagename + vk.Key.ToString();
                long pointcount = line.GetPointCount();
                for (int i = 0; i < pointcount; i++) {
                    double x = line.GetX(i);
                    double y = line.GetY(i);
                    double z = line.GetZ(i);
                    Vertex vertex = new Vertex(x,y,z);
                    vertex.name = eage.name + i.ToString();
                    eage.AddVertex(vertex);
                }
                result.Add(id, eage);
            }
            return result;
        }
        private Eage inverteEage(Eage eage) {
            Eage result = new Eage();
            result.name = eage.name;
            result.id = eage.id;
            List<Vertex> vertices = eage.vertexList;
            int count = vertices.Count;
            for (int i = count - 1; i >= 0; i--) {
                Vertex vertex = vertices[i];
                result.AddVertex(vertex);
            }
            return result;
        }
        private bool comparexyAndGeompoint(Geometry point,double x,double y,double z) {
            double px = point.GetX(0);
            double py = point.GetY(0);
            double pz = point.GetZ(0);
            if ((Math.Abs(px-x)<0.0000001) && (Math.Abs(py-y)<0.0000001) && (Math.Abs(pz-z)<0.0000001)) {
                return true;
            }
            return false;
        }
    }
}
