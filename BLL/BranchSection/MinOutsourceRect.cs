using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;//这个玩意儿是牛牛的矩阵计算类
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{
    public class MinOutsourceRect
    {
        public double angle;
        public double width, heigh;
        public double centerx, centery;
        public double[] oripoints;
        public Matrix<double> transMat;
        public MinOutsourceRect() { 
        
        }
        public double[] points() {
            
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
        public double area() {
            return width * heigh;
        }
        public double[] bufferpoints(double xbuffer,double ybuffer) {
            double[] ppp1 = this.oripoints;
            double[] ppp = { ppp1[0]-xbuffer, ppp1[1]-ybuffer, ppp1[2]+xbuffer, ppp1[3]-ybuffer, ppp1[4]+xbuffer, ppp1[5]+ybuffer, ppp1[6]-xbuffer, ppp1[7]+ybuffer };
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
    public class MinOutRectBuilder {
        /// <summary>
        /// 输入一个xy的列表，可选旋转求解精度，返回一个最小外包矩形对象
        /// </summary>
        /// <param name="xylist"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static MinOutsourceRect buildMinOutRect(List<double[]>xylist,double precision=1) {
            double xcenter, ycenter;
            getcenterCoord(xylist, out xcenter, out ycenter);
            double minarea = double.MaxValue;
            MinOutsourceRect result = new MinOutsourceRect();
            double t = (double)90 / precision;
            int t2 = (int)t;
            int searchtimes =t2;
            for (int i = 0; i <= searchtimes; i++) {//循环找到旋转某角度就获得面积最小的矩形
                double angle = i * precision;
                Matrix<double> transM = getTransMat(xcenter, ycenter, angle);
                MinOutsourceRect minOutsourceRect = getSpecificAByMat(xylist, xcenter, ycenter, angle, transM);
                double area = minOutsourceRect.area();
                if (area < minarea) {
                    minarea = area;
                    result = minOutsourceRect;
                }
            }
            return result;
        }
        

        public static void getcenterCoord(List<double[]> xylist, out double centerx, out double centery) {
            //获取中心点
            double sumx = 0, sumy = 0;
            int count = xylist.Count;
            for (int i = 0; i < count; i++) {
                double[] xy = xylist[i];
                sumx += xy[0];
                sumy += xy[1];
            }
            centerx = sumx / count;
            centery = sumy / count;
        }
        public static MinOutsourceRect getSpecificAByMat(List<double[]>xylist,double centerx,double centery,double angle,Matrix<double> transM) {
            //这个是制作一个当前的最小外包矩形
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            int count = xylist.Count;
            for (int i = 0; i < count; i++) {
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
            double[] oripoints = { envelope.MinX,envelope.MinY,envelope.MaxX,envelope.MinY,envelope.MaxX,envelope.MaxY,envelope.MinX,envelope.MaxY};
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
        public static Matrix<double> getTransMat(double centerx,double centery,double angle) {
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
