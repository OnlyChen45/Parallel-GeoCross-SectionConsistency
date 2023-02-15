using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;//这个玩意儿是牛牛的矩阵计算类
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 用来对二维和三维的面进行仿射变换的基础类，业务逻辑层，通用模块
    /// </summary>
    public class BaseLineTransformWorder2D
    {
        //这个类是用来生成三维的垂直的地层剖面线的XY坐标转到从原点开始X正半轴上去的矩阵，然后提供转回来的矩阵。
        double startx, starty, endx, endy;
        public Matrix<double> transMat;
        public Matrix<double> transInverseMat;
        public BaseLineTransformWorder2D(double startx,double starty,double endx,double endy) {
            this.startx = startx;
            this.starty = starty;
            this.endx = endx;
            this.endy = endy;
            if (endy < starty) {
                //确保让startx y是在下方，这样一来，按照start挪到原点，那么整个线段一定是在第一第二象限
                exchangedouble(ref this.starty, ref this.endy);
                exchangedouble(ref this.startx, ref this.endx);
            }
            makeMat();
        }
        public void makeMat() {
            double[,] transTo00 = { {1,0,-startx },
                                    {0,1,-starty},
                                    {0,0,1 } };
            double dis = distance(startx, starty, endx, endy);
            /*            double dx = endx - startx;
                        double theta = Math.Acos(dx / dis);

                        double[,] transRotateToX = { { Math.Cos(theta),-Math.Sin(theta),0},
                                                        {Math.Sin(theta),Math.Cos(theta),0 },
                                                        { 0,0,1} };*/
            #region 修改过的仿射旋转矩阵
            double dx = endx - startx;
            double dy = endy - starty;
            double theta = Math.Abs(Math.Acos(dy / dis));
            double thetaori = theta;
            theta = Math.PI / 2 - theta;
            //这个是顺时针旋转
            double[,] transRotateToX ={ { Math.Cos(theta),-Math.Sin(theta),0},
                                            { Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1}
            };
            if (dx > 0)
            {
                
                double thetat = thetaori + Math.PI / 2;
                //这个是逆时针旋转
                /*double[,] transRotateToX2 ={ { Math.Cos(theta),Math.Sin(theta),0},
                                            { -Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1} };*/
                double[,] transRotateToX2 ={ { Math.Cos(thetat),-Math.Sin(thetat),0},
                                            { Math.Sin(thetat),Math.Cos(thetat),0 },
                                            { 0,0,1} };
                transRotateToX = transRotateToX2;
            }
            #endregion
            var mb = Matrix<double>.Build;
            var translateTo00M = mb.DenseOfArray(transTo00);
            var transRotateToXM = mb.DenseOfArray(transRotateToX);
            this.transMat = transRotateToXM * translateTo00M;
            this.transInverseMat = transMat.Inverse();
        }
        /// <summary>
        /// 这个不会引发旋转问题，
        /// </summary>
        public void makeMatFormiddata()
        {
            double[,] transTo00 = { {1,0,-startx },
                                    {0,1,-starty},
                                    {0,0,1 } };
            double dis = distance(startx, starty, endx, endy);
            /*            double dx = endx - startx;
                        double theta = Math.Acos(dx / dis);

                        double[,] transRotateToX = { { Math.Cos(theta),-Math.Sin(theta),0},
                                                        {Math.Sin(theta),Math.Cos(theta),0 },
                                                        { 0,0,1} };*/
            #region 修改过的仿射旋转矩阵
            double dx = endx - startx;
            double dy = endy - starty;
            //这个是这条线与Y轴正半轴之间的的角度
            double theta = Math.Abs(Math.Acos(dy / dis));
            double thetaori = theta;
            //这个是与X轴正半轴的角度
            theta = Math.PI / 2 - theta;
            //这个是顺时针旋转
            double[,] transRotateToX ={ { Math.Cos(theta),-Math.Sin(theta),0},
                                            { Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1}
            };
            if (dx > 0)
            {
                //这个是与X轴负半轴角度
                double thetat =   Math.PI / 2 - thetaori;
                //这个是逆时针旋转
                double[,] transRotateToX2 ={ { Math.Cos(thetat),Math.Sin(thetat),0},
                                            { -Math.Sin(thetat),Math.Cos(thetat),0 },
                                            { 0,0,1} };
                /*                double[,] transRotateToX2 ={ { Math.Cos(thetat),-Math.Sin(thetat),0},
                                                            { Math.Sin(thetat),Math.Cos(thetat),0 },
                                                            { 0,0,1} };*/
                transRotateToX = transRotateToX2;
            }
            #endregion
            var mb = Matrix<double>.Build;
            var translateTo00M = mb.DenseOfArray(transTo00);
            var transRotateToXM = mb.DenseOfArray(transRotateToX);
            this.transMat = transRotateToXM * translateTo00M;
            this.transInverseMat = transMat.Inverse();
        }
        public double distance(double x1,double y1,double x2,double y2) {
            double dx = x1 - x2;
            double dy = y1 - y2;
            double result = Math.Sqrt(Math.Pow(dx, 2)+Math.Pow(dy, 2));
            return result;
        }
        public void transXY(double x, double y, out double[] XY)
        {//转换
            double[,] xysite = { { x }, { y }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyM = mb.DenseOfArray(xysite);
            var XYM = this.transMat * xyM;
            double[,] columnXYZ = XYM.ToArray();
            XY = new double[2];
            XY[0] = columnXYZ[0, 0];
            XY[1] = columnXYZ[1, 0];
        }
        public void transBackXY(double x, double y, out double[] XY)
        {//转换
            double[,] xysite = { { x }, { y }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyM = mb.DenseOfArray(xysite);
            var XYM = this.transInverseMat * xyM;
            double[,] columnXYZ = XYM.ToArray();
            XY = new double[2];
            XY[0] = columnXYZ[0, 0];
            XY[1] = columnXYZ[1, 0];
        }
        private void exchangedouble(ref double a,ref double b) {
            double t = a;
            a = b;
            b = t;
        }
    }
}
