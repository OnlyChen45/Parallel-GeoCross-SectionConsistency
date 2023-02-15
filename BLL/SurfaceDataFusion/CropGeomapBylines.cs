using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using OSGeo.GDAL;
using OSGeo.OGR;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 裁剪地质图，只留下平行剖面之间的那部分
    /// 这个简单，输入两个剖面线，然后两个拉个矩形，然后直接 裁剪
    /// 这个并不简单，问题在于，问题在于
    /// 需要对这个之间的这些poly坐拓扑模型构建，
    /// 构建拓扑模型就需要那个左转算法，或者用arcpy，那就比较麻烦了反正
    /// </summary>
    public class CropGeomapBylines
    {
        Geometry line1, line2;
        public CropGeomapBylines(Geometry line1, Geometry line2) 
        {
            this.line1 = line1;
            this.line2 = line2;
        }
        /// <summary>
        /// 输入一个用地质图的dic，返回一个被两条线切下来的dic
        /// </summary>
        /// <param name="polys"></param>
        /// <returns></returns>
        public Dictionary<int, Geometry> getGeomapBetweenlines(Dictionary<int,Geometry> polys) 
        {
            int count1 = line1.GetPointCount();
            int count2 = line2.GetPointCount();
            double xline10 = line1.GetX(0);
            double yline10 = line1.GetY(0);
            double xline1n = line1.GetX(count1 - 1);
            double yline1n = line1.GetY(count1 - 1);
            double xline20 = line2.GetX(0);
            double yline20 = line2.GetY(0);
            double xline2n = line2.GetX(count2 - 1);
            double yline2n = line2.GetY(count2 - 1);
            Geometry linetemp1 = new Geometry(wkbGeometryType.wkbLineString);
            Geometry linetemp2 = new Geometry(wkbGeometryType.wkbLineString);
            linetemp1.AddPoint_2D(xline10, yline10);
            linetemp1.AddPoint_2D(xline20, yline20);
            linetemp2.AddPoint_2D(xline10, yline10);
            linetemp2.AddPoint_2D(xline2n, yline2n);
            double length1 = linetemp1.Length();
            double length2 = linetemp2.Length();
            bool linkstate = true;
            if (length1 <= length2) { linkstate = true; }
            else { linkstate = false; }
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            Geometry side1 = new Geometry(wkbGeometryType.wkbLineString);//用来记录平行线之间区域的侧面，方便后续构建拓扑模型
            Geometry side2 = new Geometry(wkbGeometryType.wkbLineString);
            if (linkstate) 
            {
                for (int i = count1 - 1; i >= 0; i--) {
                    double x = line1.GetX(i);
                    double y = line1.GetY(i);
                    ring.AddPoint_2D(x, y);
                }
                side1.AddPoint_2D(line1.GetX(0),line1.GetY(0));
                side1.AddPoint_2D(line2.GetX(0), line2.GetY(0));
                for (int i = 0; i < count2; i++) {
                    double x = line2.GetX(i);
                    double y = line2.GetY(i);
                    ring.AddPoint_2D(x, y);
                }
                double xe = line1.GetX(count1-1);
                double ye = line1.GetY(count1 - 1);
                ring.AddPoint_2D(xe, ye);
                side2.AddPoint_2D(line1.GetX(count1 - 1), line1.GetY(count1 - 1));
                side2.AddPoint_2D(line2.GetX(count2 - 1), line2.GetY(count2 - 1));
            }
            else 
            {
                for (int i = 0; i <count1; i++)
                {
                    double x = line1.GetX(i);
                    double y = line1.GetY(i);
                    ring.AddPoint_2D(x, y);
                }
                side1.AddPoint_2D(line1.GetX(count1 - 1), line1.GetY(count1 - 1));
                side1.AddPoint_2D(line2.GetX(0), line2.GetY(0));
                for (int i = 0; i <count2 ; i++)
                {
                    double x = line2.GetX(i);
                    double y = line2.GetY(i);
                    ring.AddPoint_2D(x, y);
                }
                double xe = line1.GetX(0);
                double ye = line1.GetY(0);
                ring.AddPoint_2D(xe, ye);
                side2.AddPoint_2D(line1.GetX(0), line1.GetY(0));
                side2.AddPoint_2D(line2.GetX(count2 - 1), line2.GetY(count2 - 1));
            }

            Geometry geomRect = new Geometry(wkbGeometryType.wkbPolygon);
            geomRect.AddGeometry(ring);
            Geometry union = new Geometry(wkbGeometryType.wkbPolygon);
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in polys) {
                int id = vk.Key;
                Geometry poly = vk.Value;
                Geometry inter = poly.Intersection(geomRect);
                result.Add(id, inter);
                union = union.Union(inter);
            }
            //把标记为边界线的buffer加进结果，用来保证侧面的结点可以生产出来。
            Geometry b1 = side1.Buffer(3, 30);
            Geometry b2 = side2.Buffer(3, 30);
            Geometry bd1 = b1.Difference(union);
            Geometry bd2 = b2.Difference(union);
            result.Add(-2, bd1);
            result.Add(-3, bd2);
            return result;
        }
    }
    /// <summary>
    /// 一定要输入裁剪过的研究区域，然后在这里坐拓扑
    /// 获取到独特的拓扑结构，随后做两侧线上拓扑点的匹配，一般就是一条直线能拉过去就可以
    /// </summary>
    public class MatchSurfacePointid 
    {
        Dictionary<int, Geometry> geopolys;
        Topology geotopology;
        public MatchSurfacePointid(Dictionary<int,Geometry>geopolys) {
            this.geopolys = geopolys;
            TopologyOfPoly topologyOfPoly = new TopologyOfPoly(geopolys.Keys.ToList(), geopolys);
            topologyOfPoly.makeTopology();
            topologyOfPoly.exportToTopology(out geotopology);
        }
        /// <summary>
        /// 输入经过匹配过后的那个点的dic，然后返回这些点id的匹配关系
        /// 就以line1为基础，搜索所有连接点，找到的就是匹配的
        /// 下面再写一个函数，就是根据这个Dictionary<int, int>返回对应的三维线的xyz，就齐活了
        /// 
        /// </summary>
        /// <param name="line1points"></param>
        /// <param name="line2points"></param>
        /// <returns></returns>
        public Dictionary<int, int> getsurfacepointPair(Dictionary<int,Geometry> line1points,Dictionary<int,Geometry> line2points,out Dictionary<int,Geometry> lines1pointToSurfaceBoundary) 
        {
            //应该先把线上点与topo中的点对应一下
            Dictionary<int, int> line1pointsToTopo = new Dictionary<int, int>();
            Dictionary<int, int> line2pointsToTopo = new Dictionary<int, int>();
            lines1pointToSurfaceBoundary = new Dictionary<int, Geometry>();
            Dictionary<int, int> pointline1To2Pairs = new Dictionary<int, int>();
            foreach (var point in line1points) 
            {
                double x = point.Value.GetX(0);
                double y = point.Value.GetY(0);
                foreach (var point2 in geotopology.index_points_Pairs) 
                {
                    double x2 = point2.Value.GetX(0);
                    double y2 = point2.Value.GetY(0);
                    double dis = GetDistance2D(x, y, x2, y2);
                    if (dis < 0.000001) {
                        line1pointsToTopo.Add(point.Key, point2.Key);
                    }
                }
            }
            foreach (var point in line2points)
            {
                double x = point.Value.GetX(0);
                double y = point.Value.GetY(0);
                foreach (var point2 in geotopology.index_points_Pairs)
                {
                    double x2 = point2.Value.GetX(0);
                    double y2 = point2.Value.GetY(0);
                    double dis = GetDistance2D(x, y, x2, y2);
                    if (dis < 0.000001)
                    {
                        line2pointsToTopo.Add(point.Key, point2.Key);
                    }
                }
            }
            //下面给这些点找到对应点
            //其实过程很简单
            //因为目前设置的都是最简单的情况，就是，就是，两边只可能存在相互连通的
            //先遍历一边上的所有点
            foreach (var vk in line1pointsToTopo) 
            {
                int idp = vk.Key;
                int idtopo = vk.Value;
                List<int> linklines = geotopology.points_arcs_Pairs[idtopo];
                //遍历这个点的所有线
                foreach (var vk2 in linklines) 
                {
                    int[] endpointst = geotopology.arcs_points_Pairs[vk2];
                    int idtemp = endpointst[0];
                    if (idtemp == idtopo) {
                        idtemp = endpointst[1];
                    }
                    //这个线的另一端连通到另一条线上，那么就是我们要找的
                    if (line2pointsToTopo.Values.Contains(idtemp)) 
                    {
                        //拿到这个线的geometry
                        lines1pointToSurfaceBoundary.Add(idp, geotopology.index_arcs_Pairs[vk2]);
                        int point2id=-1;
                        //拿到对应这个线上点的，在剖面上的id
                        foreach (var vk3 in line2pointsToTopo) {
                            if (vk3.Value == idtemp) {
                                point2id = vk3.Key;
                                break;
                            }
                        }
                        //存储为剖面上点id的字典，作为结果
                        pointline1To2Pairs.Add(idp, point2id);
                    }
                }
            }
            return pointline1To2Pairs;
        }
        public double GetDistance3D(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2) + Math.Pow(z1 - z2, 2));
        }
        public double GetDistance2D(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }
    }
}
