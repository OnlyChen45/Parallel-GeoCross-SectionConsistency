using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace ThreeDModelSystemForSection
{ /// <summary>
/// 栅格的像素坐标P/L原点为图像左上角，x轴正方向为水平向右，y轴正方向为垂直向下
/// </summary>
    public class DemIO
    {
        public double[] gt;
        public int Xsize, Ysize;
        public Band band1;
        public DemIO(string dempath) {
            Dataset ds = Gdal.Open(dempath, Access.GA_ReadOnly);
            Band band = ds.GetRasterBand(1);
            this.band1 = band;
            this.gt = new double[6];
            ds.GetGeoTransform(this.gt);
            this.Xsize = ds.RasterXSize;
            this.Ysize = ds.RasterXSize;
            ds.Dispose();
        }
        /// <summary>
        /// 输入地理坐标，获得像素值
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public double getDemValueByXY(double x, double y)
        {//根据地理坐标获取dem上的对应值
            int[] XY = geoToPixel(x, y, this.gt);
            double[] buf = new double[1];
            this.band1.ReadRaster(XY[0], XY[1], 1, 1, buf, 1, 1, 0, 0);
            return buf[0];
        }
        public double getDemValueByPL(int P,int L) {
            double[] buf = new double[1];
            this.band1.ReadRaster(P, L, 1, 1, buf, 1, 1, 0, 0);
            return buf[0];
        }

        /// <summary>
        /// 为一个地理坐标在dem上获取双线性内插的高程值
        /// 具体过程如下
        /// 1.确定目标位置在其所在格子中心位置的哪个方向
        /// 2.获取该方向所属周围的四个格子的中心点地理位置，高程
        /// 3.判断是否越界，越界了就用最近邻法
        /// 4.双线性内插
        /// 当这个点在图形边缘时，直接用最近邻法。
        /// </summary>
        /// <param name="geox"></param>
        /// <param name="geoy"></param>
        /// <returns></returns>
        public double BilinearInterpolation(double geox,double geoy,double[] gt) 
        {
            int[] PL = geoToPixel(geox, geoy, gt);
            int P = PL[0];
            int L = PL[1];
            int[] plr = { P - 1, P, P + 1 };//获取到可能有关像素点的横坐标
            int[] llr = { L - 1, L, L + 1 };//获取到可能有关像素点的纵坐标
            double[] xy = getGeoCenterXYFromPL(plr[1], llr[1], gt);
            double centerz = getDemValueByXY(geox, geoy);
            bool xPosOrNeg, yPosOrNeg;//目标点与其落在格子的中心点，x y方向偏移
            if (geox >= xy[0]) { xPosOrNeg = true; } else { xPosOrNeg = false; }
            if (geoy >= xy[1]) { yPosOrNeg = true; } else { yPosOrNeg = false; }
            int[] p12 = new int[2];
            int[] l12 = new int[2];
            if (xPosOrNeg)
            {
                p12[0] = P;
                p12[1] = P + 1;
            }
            else {
                p12[0] = P - 1;
                p12[1] = P;
            }
            if (yPosOrNeg)//Y轴方向是翻转的，所以当目标点地理坐标大于所在格子中心点坐标时，对应的栅格的坐标要-1，反之+1
            {
                p12[0] = P-1;
                p12[1] = P;
            }
            else
            {
                p12[0] = P;
                p12[1] = P+1;
            }
            //处于边缘直接用最邻近法，返回中心点的z
            if (p12[0] <= 0 || p12[1] > Xsize) return centerz;
            if (l12[0] <= 0 || l12[1] > Ysize) return centerz;
            //双线性内插
            double z00 = getDemValueByPL(p12[0], l12[0]);
            double z10 = getDemValueByPL(p12[1], l12[0]);
            double z01 = getDemValueByPL(p12[0], l12[1]);
            double z11 = getDemValueByPL(p12[1], l12[1]);
            double midz0 = getcenterinter(p12[0], z00, p12[1], z10, geox);
            double midz1 = getcenterinter(p12[0], z01, p12[1], z11, geox);
            double resultz = getcenterinter(l12[0], midz0, l12[1], midz1, geoy);
            return resultz;
            /* Dictionary<char, double[]> points = new Dictionary<char, double[]>();
            List<double> xlist = new List<double>();
            List<double> ylist = new List<double>();
            List<double> zlist = new List<double>();
            List<double[]> oripoints = new List<double[]>();
            for (int i = 0; i < 3; i++) {
                if (plr[i]>0&&plr[i]<=Xsize) {
                    for (int j = 0; j < 3; j++) {
                        if (llr[j] > 0 && llr[1] <= Ysize) {
                            double[] xy = getGeoCenterXYFromPL(plr[i], llr[j], gt);
                            double z = getDemValueByPL(plr[i], llr[j]);
                            xlist.Add(xy[0]);
                            ylist.Add(xy[1]);
                            zlist.Add(z);
                        }
                    }
                }
            }
            points.Add('x', xlist.ToArray());
            points.Add('y', ylist.ToArray());
            points.Add('z', zlist.ToArray());*/
        }
        /// <summary>
        /// 获取一个geometry范围内所有的点和高程，如果落在外部，则舍弃，存到Dic里 x y z
        /// 1.求出geometry的外接矩形
        /// 2.获取外接矩形内所有的像素点 中心地理坐标和高程
        /// 3.验证地理坐标是否在geometry内，如果在则留下该点，不在则舍弃
        /// </summary>
        /// <param name="polygeom"></param>
        /// <returns></returns>
        public Dictionary<char,double[]> getCenterPointInGeom(Geometry polygeom) {
            Dictionary<char, double[]> result = new Dictionary<char, double[]>();
            List<double> xlist = new List<double>();
            List<double> ylist = new List<double>();
            List<double> zlist = new List<double>();
            Envelope envelope = new Envelope();
            polygeom.GetEnvelope(envelope);
            //获取外包矩形所在栅格位置
            int[] PLmax = geoToPixel(envelope.MaxX, envelope.MaxY, gt);
            int[] PLmin = geoToPixel(envelope.MinX, envelope.MinY, gt);
            //0调整一下最大值最小值
            if (PLmax[0] < PLmin[0]) swapint(ref PLmax[0], ref PLmin[0]); 
            if (PLmax[1] < PLmin[1]) swapint(ref PLmax[1], ref PLmin[1]);
            for (int P = PLmin[0]; P <= PLmax[0]; P++)
            { 
                for (int L = PLmin[1]; L <= PLmax[1]; L++) {
                    double z = getDemValueByPL(P, L);
                    double[] xy = getGeoCenterXYFromPL(P,L,gt);
                    //判断这个点具体在不在这个poly里边
                    Geometry pointtemp = new Geometry(wkbGeometryType.wkbPoint);
                    pointtemp.AddPoint_2D(xy[0], xy[1]);
                    if (polygeom.Intersect(pointtemp)) {
                        xlist.Add(xy[0]);
                        ylist.Add(xy[1]);
                        zlist.Add(z);
                    }
                }
            }
            ///把边界线加进去
            Geometry boundary = polygeom.Boundary();
            int pcount = boundary.GetPointCount();
            for (int i = 0; i < pcount; i++)
            {
                double x = boundary.GetX(i);
                double y = boundary.GetY(i);
                //内插得到这个边界线上所有的点
                double z = BilinearInterpolation(x, y, gt);
                xlist.Add(x);
                ylist.Add(y);
                zlist.Add(z);
            }
            result.Add('x', xlist.ToArray());
            result.Add('y', ylist.ToArray());
            result.Add('z', zlist.ToArray());
            return result;
        }
        /// <summary>
        /// 输入地理坐标，返回它在dem上的像素的行列坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="gt"></param>
        /// <returns></returns>
        private int[] geoToPixel(double x, double y, double[] gt)
        {
            double[] XY = new double[2];
            int[] result = new int[2];
            if (gt[1] == 0)
            {
                XY[0] = (y - gt[3]) / gt[4];
                XY[1] = (x - gt[0]) / gt[2];


            }
            else
            if (gt[2] == 0)
            {
                XY[0] = (x - gt[0]) / gt[1];
                XY[1] = (y - gt[3]) / gt[5];

            }
            else
            {
                XY[1] = ((x - gt[0]) / gt[1] - (y - gt[3]) / gt[4]) / (gt[2] / gt[1] - gt[5] / gt[4]);
                XY[0] = (x - gt[0]) / gt[1] - XY[1] * gt[2] / gt[1];
            }
            result[0] = (int)Math.Round(XY[0]);
            result[1] = (int)Math.Round(XY[1]);
            return result;
        }
        /// <summary>
        /// 返回的是dem栅格左上角地理坐标
        /// </summary>
        /// <param name="P"></param>
        /// <param name="L"></param>
        /// <param name="gt"></param>
        /// <returns></returns>
        private double[] getGeoXYFromPL(int P,int L,double[] gt) 
        {
            double x= gt[0] + P * gt[1] + L * gt[2];
            double y= gt[3] + P * gt[4] + L * gt[5];
            double[] result = { x, y };
            return result;
        }
        /// <summary>
        /// 返回的是dem栅格中心点地理坐标
        /// </summary>
        /// <param name="P"></param>
        /// <param name="L"></param>
        /// <param name="gt"></param>
        /// <returns></returns>
        private double[] getGeoCenterXYFromPL(int P, int L, double[] gt)
        {
            double x = gt[0] + (P + 0.5) * gt[1] + (L + 0.5) * gt[2];
            double y = gt[3] + (P + 0.5) * gt[4] + (L + 0.5) * gt[5];
            double[] result = { x, y };
            return result;
        }
        /// <summary>
        /// 一维插值用，某坐标轴上，第一个位置为site1，高程z1，第二个位置site2，高程z2，目标位置为site
        /// </summary>
        /// <param name="site1"></param>
        /// <param name="z1"></param>
        /// <param name="site2"></param>
        /// <param name="z2"></param>
        /// <param name="midsite"></param>
        /// <returns></returns>
        private double getcenterinter(double site1, double z1, double site2, double z2, double midsite) 
        {
            double minsite = site1;
            double maxsite = site2;
            double minz = z1;
            double maxz = z2;
            if (minsite > maxsite) {
                swapdouble(ref minsite, ref maxsite);
                swapdouble(ref minz, ref maxz);
            }
            double resultz = ((maxz - minz) * (midsite - minsite) / (maxsite - minsite)) + minz;
            return resultz;
        
        }
        private void swapdouble(ref double a1,ref double a2) {
            double t;
            t = a1;
            a1 = a2;
            a2 = t;
        }
        private void swapint(ref int a1, ref int a2)
        {
            int t;
            t = a1;
            a1 = a2;
            a2 = t;
        }
    }
}
