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
    /// 匹配地质图与剖面线交点和剖面图上表面的点
    /// 这是个大问题，草，我想想，首先从剖面图开始，应该首先获取上面的点，
    /// 然后，获取完了然后就，默认数据肯定是能对上的，对不少就改数据
    /// 这样的话，就是求出这些点的三维位置，然后找一个最近的就是了，而且有个阈值，大于阈值就是错的
    /// </summary>
    public class SurfacePointMatch
    {
        /// <summary>
        /// 输入剖面图上三维点，一条线，地质图，
        /// 返回剖面图上点的id与地质图上的点的geometry对应的dictionary，
        /// </summary>
        /// <param name="points3d"></param>
        /// <param name="polysgeo"></param>
        /// <param name="line"></param>
        /// <returns>返回的是三维的点，地质图上坐标，Z从dem双线性内插得到</returns>
        public Dictionary<int, Geometry> PointMatch(Dictionary<int, Geometry> points3d, Dictionary<int, Geometry> polysgeo, Geometry line,DemIO demIO) {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            Dictionary<int, Geometry> interlines = new Dictionary<int, Geometry>();//通过intersection调整的这个线
            //按照geopoly的id制作
            foreach (var vk in polysgeo) {
                bool inters = vk.Value.Intersect(line);
                if (inters) {
                    interlines.Add(vk.Key, vk.Value.Intersection(line));
                }
            }
            List<Geometry> pointsSurfaces = new List<Geometry>();
            List<double[]> pointxy = new List<double[]>();
            //然后就是黏在一起，
            foreach (var vk in interlines) {
                double pointx = vk.Value.GetX(0);
                double pointy = vk.Value.GetY(0);
                bool unique = true;
                foreach (double[] xy in pointxy) {
                    if (GetDistance2D(xy[0], xy[1], pointx, pointy) < 0.000001) {
                        unique = false;
                    }
                }
                if (unique) {
                    double[] xy = { pointx, pointy };
                    pointxy.Add(xy);
                }
            }
            //把独立的点加进去
            foreach (double[] xy in pointxy)
            {
                Geometry point = new Geometry(wkbGeometryType.wkbPoint25D);
                double z= demIO.BilinearInterpolation(xy[0], xy[1], demIO.gt);
                point.AddPoint(xy[0], xy[1],z);
                pointsSurfaces.Add(point);
            }
            //做匹配
            foreach (var vk in points3d) {
                Geometry point3d1 = vk.Value;
                double x1 = point3d1.GetX(0);
                double y1 = point3d1.GetY(0);
                foreach (Geometry point2 in pointsSurfaces) {
                    double x2 = point2.GetX(0);
                    double y2 = point2.GetY(0);
                    double dis = GetDistance2D(x1, y1, x2, y2);
                    if (dis < 0.000001) {
                        result.Add(vk.Key, point2);
                        pointsSurfaces.Remove(point2);
                        break;
                    }
                }
            }
            return result;
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
    /// <summary>
    /// 这个类主要是负责，在所有的处理之前，给表面自动加上一层polygon用以区分地上和地下
    /// 输入2D剖面section，输入，三维参数，输入，dem，sectionline，
    /// 返回一个polygon，是与上表面touches的
    /// </summary>
    public class ExtractSectionSurface
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sectionpolys"></param>
        /// <param name="par1dic"></param>
        /// <param name="dem"></param>
        /// <param name="sectionline"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static Geometry ExtractSurface(Dictionary<int,Geometry>sectionpolys,Dictionary<string, double> par1dic, DemIO dem,Geometry sectionline,double threshold=1)
        {
            double xstart = sectionline.GetX(0);
            double ystart = sectionline.GetY(0);
            int linepointscount = sectionline.GetPointCount();
            double xend = sectionline.GetX(linepointscount - 1);
            double yend = sectionline.GetY(linepointscount - 1);
            double zstart = dem.BilinearInterpolation(xstart, ystart, dem.gt);
            double zend = dem.BilinearInterpolation(xend, yend, dem.gt);
            Geometry sectionUnion = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in sectionpolys) 
            {
               sectionUnion=sectionUnion.Union(vk.Value);
            }
            //par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"]
            Geometry boundary = sectionUnion.Boundary();
            List<double> pointsx = new List<double>();
            List<double> pointsy = new List<double>();
            //边界线转成坐标list
            int boundarycount = boundary.GetPointCount();
            for (int i = 0; i < boundarycount; i++) 
            {
                double x = boundary.GetX(i);
                double y = boundary.GetY(i);
                pointsx.Add(x);
                pointsy.Add(y);
            }
            int indexstartnearist=-1, indexendnearist=-1;
            indexendnearist = getnearistindex(xstart, ystart, zstart);
            indexendnearist = getnearistindex(xend, yend, zend);
            List<double> pointxline1 = new List<double>();
            List<double> pointyline1 = new List<double>();
            List<double> pointxline2 = new List<double>();
            List<double> pointyline2 = new List<double>();
            int p1 = indexstartnearist;
            double avez1 = 0, avez2 = 0;
            while (p1 != indexendnearist) {
                double[] XYZ;
                getRealXYZ(out XYZ, pointsx[p1], pointsy[p1], par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"]);
                avez1 += XYZ[2];
                p1 = (p1 + 1) % boundarycount;
            }
            p1 = indexendnearist;
            while (p1 != indexstartnearist)
            {
                double[] XYZ;
                getRealXYZ(out XYZ, pointsx[p1], pointsy[p1], par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"]);
                avez2 += XYZ[2];
                p1 = (p1 + 1) % boundarycount;
            }
            Geometry resultline = new Geometry(wkbGeometryType.wkbLineString);
            //判断哪条线是需要的
            if (avez1 >= avez2) 
            {//start到end之间是需要的
                p1 = indexstartnearist;
                    while (p1 != indexendnearist)
                {
                    resultline.AddPoint_2D(pointsx[p1], pointsy[p1]);
                    p1 = (p1 + 1) % boundarycount;
                }
            } else 
            { //end到start之间是需要的
                p1 = indexendnearist;
                while (p1 != indexstartnearist)
                {
                    resultline.AddPoint_2D(pointsx[p1], pointsy[p1]);
                    p1 = (p1 + 1) % boundarycount;
                }
            }
            Geometry buffer = resultline.Buffer(3, 30);
            Geometry result = buffer.Difference(sectionUnion);
            return result;

            int getnearistindex(double aimx,double aimy,double aimz){
                int counttemp = pointsx.Count;
                double dismin = double.MaxValue;
                int resultindex=-1;
                for (int ii = 0; ii < counttemp; ii++) 
                {
                    double xt = pointsx[ii];
                    double yt = pointsy[ii];
                    double[] XYZ;
                    getRealXYZ(out XYZ, xt, yt, par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"]);
                    double dist = GetDistance3D(aimx, aimy, aimz, XYZ[0], XYZ[1], XYZ[2]);
                    if (dist < dismin) 
                    {
                        dismin = dist;
                        resultindex = ii;
                    }
                }
                return resultindex;
            }

        }
        public static Geometry ExtractSurfaceModel2(Dictionary<int, Geometry> sectionpolys,double startx,double starty, double endx, double endy, double startz, DemIO dem, Geometry sectionline, double threshold = 1)
        {
            double xstart = sectionline.GetX(0);
            double ystart = sectionline.GetY(0);
            int linepointscount = sectionline.GetPointCount();
            double xend = sectionline.GetX(linepointscount - 1);
            double yend = sectionline.GetY(linepointscount - 1);
            double zstart = dem.BilinearInterpolation(xstart, ystart, dem.gt);
            double zend = dem.BilinearInterpolation(xend, yend, dem.gt);
            BaseLineTransformWorder2D transWorker = new BaseLineTransformWorder2D(startx, starty, endx, endy);
            Geometry sectionUnion = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in sectionpolys)
            {
                sectionUnion = sectionUnion.Union(vk.Value);
            }
            //par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"]
            Geometry boundary = sectionUnion.Boundary();
            List<double> pointsx = new List<double>();
            List<double> pointsy = new List<double>();
            //边界线转成坐标list
            int boundarycount = boundary.GetPointCount();
            for (int i = 0; i < boundarycount; i++)
            {
                double x = boundary.GetX(i);
                double y = boundary.GetY(i);
                pointsx.Add(x);
                pointsy.Add(y);
            }
            int indexstartnearist = -1, indexendnearist = -1;
            indexendnearist = getnearistindex(xstart, ystart, zstart);
            indexendnearist = getnearistindex(xend, yend, zend);
            List<double> pointxline1 = new List<double>();
            List<double> pointyline1 = new List<double>();
            List<double> pointxline2 = new List<double>();
            List<double> pointyline2 = new List<double>();
            int p1 = indexstartnearist;
            double avez1 = 0, avez2 = 0;
            while (p1 != indexendnearist)
            {
                
                double x, y, z;
                GetRealXYZModel2(pointsx[p1], pointsy[p1], transWorker, out x, out y, out z);
                avez1 += z;
                p1 = (p1 + 1) % boundarycount;
            }
            p1 = indexendnearist;
            while (p1 != indexstartnearist)
            {
                double x, y, z;
                GetRealXYZModel2(pointsx[p1], pointsy[p1], transWorker, out x, out y, out z);
                avez2 += z;
                p1 = (p1 + 1) % boundarycount;
            }
            Geometry resultline = new Geometry(wkbGeometryType.wkbLineString);
            //判断哪条线是需要的
            if (avez1 >= avez2)
            {//start到end之间是需要的
                p1 = indexstartnearist;
                while (p1 != indexendnearist)
                {
                    resultline.AddPoint_2D(pointsx[p1], pointsy[p1]);
                    p1 = (p1 + 1) % boundarycount;
                }
            }
            else
            { //end到start之间是需要的
                p1 = indexendnearist;
                while (p1 != indexstartnearist)
                {
                    resultline.AddPoint_2D(pointsx[p1], pointsy[p1]);
                    p1 = (p1 + 1) % boundarycount;
                }
            }
            Geometry buffer = resultline.Buffer(3, 30);
            Geometry result = buffer.Difference(sectionUnion);
            return result;

            int getnearistindex(double aimx, double aimy, double aimz)
            {
                int counttemp = pointsx.Count;
                double dismin = double.MaxValue;
                int resultindex = -1;
                for (int ii = 0; ii < counttemp; ii++)
                {
                    double xt = pointsx[ii];
                    double yt = pointsy[ii];
                    double x, y, z;
                    GetRealXYZModel2(xt, yt, transWorker, out x, out y, out z);
                    double dist = GetDistance3D(aimx, aimy, aimz, x, y, z);
                    if (dist < dismin)
                    {
                        dismin = dist;
                        resultindex = ii;
                    }
                }
                return resultindex;
            }

        }
        public static void GetRealXYZModel2(double xo,double yo,BaseLineTransformWorder2D transformworker,out double x,out double y,out double z) 
        {
            // zo = zlist[j];
            double[] xy;
            double[] xyz = new double[3];
            transformworker.transXY(xo, yo, out xy);
            xyz[0] = xy[0];
            xyz[1] = 0;
            xyz[2] = xy[1];
            double[] xy2;
            transformworker.transBackXY(xyz[0], xyz[1], out xy2);
            x=xy2[0];//把转完的坐标拿到
            y=xy2[1];
            z=xyz[2];
        }
        public static double GetDistance3D(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2) + Math.Pow(z1 - z2, 2));
        }
        public static double GetDistance2D(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }
        private static void getRealXYZ(out double[] XYZ, double x, double y, double startX, double startY, double startZ, double endX, double endY, double firstX, double firstY)
        {
            //这些参数分别是，XYZ输出的该点正确坐标，x该点在地层shp中的x坐标，y该点在地层shp中的y坐标,startX startY startZ剖面线起始点坐标，endX endY剖面线末端点坐标，fisrtX firstY 地层剖面的首端点坐标
            XYZ = new double[3];
            double X = x - firstX;
            double dz = y - firstY;
            double distance = Math.Sqrt((startX - endX) * (startX - endX) + (startY - endY) * (startY - endY));
            double dx = endX - startX;
            double dy = endY - startY;
            XYZ[2] = dz + startZ;//把高程求出来，即shp中y与左上y的差值加上高程startz
            XYZ[0] = startX + dx * X / distance;//求出该点X
            XYZ[1] = startY + dy * X / distance;//求出该点Y
        }
    }
    public enum ArcClassWtihSurface
    {
        OnSurface=2,//表面弧段
        TouchSurfaceOneHand=1,//一段延展到表面的弧段
        TouchSurfaceTwoHand=0,
        UnderGround=-1//地下的弧段
    }
    /// <summary>
    /// 弧段分类，为了能够进行融合，要把剖面上弧段分个类，
    /// </summary>
    public class ArcClassify
    {

        public int SurfaceMark;
        Topology topology;
        Dictionary<int, Geometry> pointsSurface;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topology">输入一个做好的拓扑对象</param>
        /// <param name="surfaceid">这个是标志表层线的id</param>
        /// <param name="pointsSurface">这个是标志表层点的，它的keys是点id列表，geometry是对应地质图上的点</param>
        public ArcClassify(Topology topology, int surfaceid, Dictionary<int, Geometry> pointsSurface) {
            this.topology = topology;
            this.SurfaceMark = surfaceid;
            this.pointsSurface = pointsSurface;
        }
        public Dictionary<int, ArcClassWtihSurface> classifyArcs() 
        {
            Dictionary<int, ArcClassWtihSurface> result = new Dictionary<int, ArcClassWtihSurface>();//这个结果是,定义三类弧段，key是line的id，value是分类结果
            int[] surfacepointsid = this.pointsSurface.Keys.ToArray();
            foreach (var vk in topology.arcs_points_Pairs) 
            {
                int[] endpoints = vk.Value;
                int lineid = vk.Key;
                int[] polyids = topology.arcs_poly_Pairs[lineid];
                bool onsurface1 = false, onsurface2 = false;
                if (surfacepointsid.Contains<int>(endpoints[0])) 
                {
                    onsurface1 = true;
                }
                if (surfacepointsid.Contains<int>(endpoints[1]))
                {
                    onsurface2 = true;
                }
                //分门别类建立类型
                if (onsurface1 && onsurface2)
                {
                    //当一条边两个点都是外露的，且，本身就与surfacepoly是touches，那么肯定是表面弧段
                    if (polyids.Contains(SurfaceMark))
                    {
                        result.Add(vk.Key, ArcClassWtihSurface.OnSurface);
                    }
                    //否则就是两端出露中间埋藏的弧段
                    else {
                        result.Add(vk.Key, ArcClassWtihSurface.TouchSurfaceTwoHand);
                    }
                }
                else 
                {
                    //如果其中一端点出露，那么这个必然就是弧段埋藏，一端出露
                    if (onsurface1 || onsurface2)
                    {
                        result.Add(vk.Key, ArcClassWtihSurface.TouchSurfaceOneHand);
                    }
                    //完全不出露，那么就是地下弧段
                    else { result.Add(vk.Key, ArcClassWtihSurface.UnderGround); }
                }
            }
            return result;
        }
    }
}
