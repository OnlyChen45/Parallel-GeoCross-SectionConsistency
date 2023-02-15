using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;//这个玩意儿是牛牛的矩阵计算类
using GeoCommon;
using TriangleNet;

namespace SolidModel
{
    public class Sectiontransform
    {//这个类主要是实现把一个竖直向下的地层剖面通过矩阵变换给弄到平行于XOY
        /*第一步，把剖面采样线的起点挪到坐标轴原点
    第二步，求出剖面采样线的XOY平面方向向量，把这个向量与y轴负方向夹角求出来
    第三步，整个剖面绕Z轴顺时针旋转与y轴负方向夹角那么大角度，即整个剖面变成从坐标原点出发，延y轴负方向延展，处于YOZ平面上
    第四步，整个剖面绕y轴逆时针旋转90度，就转到了XOY平面上
    注意，这个方法最好是两个剖面线共用一个变换矩阵。反正它们都是平行的，只要有一个面转对了，剩下的也都平行XOY的。

         */
        double startX;
        double startY;
        double startZ;
        double endX;
        double endY;
        public Matrix<double> transMat;
        public Matrix<double> transInverseMat;
       public Sectiontransform(double startX,double startY,double startZ,double endX ,double endY) {
            this.startX = startX;
            this.startY = startY;
            this.startZ = startZ;
            this.endX = endX;
            this.endY = endY;//有了这五个点就可以确定剖面对应的采样线在空间中的位置,毕竟地层都是垂直向下的
            calcuTransMatrix();
        }
        public void calcuTransMatrix() {
            double[,] translateTo000 = {   { 1,0,0,-startX},
                                            {0,1,0,-startY },
                                            {0,0,1,-startZ },
                                            {0,0,0,1 } };
            double[] vactorP = new Double[2];
            vactorP[0] = endX - startX;
            vactorP[1] = endY - startY;
            double lengthP = Math.Sqrt(Math.Pow(vactorP[0], 2)+ Math.Pow(vactorP[1], 2));
            vactorP[0] = vactorP[0] / lengthP;
            vactorP[1] = vactorP[1] / lengthP;
            double costht = -vactorP[1];//实际上是vactorP(x,y)*(0,-1)乘以y轴负方向向量
            double angle= Math.Acos(costht);//角度以弧度为单位
            if (vactorP[0] < 0) {//如果x小于0，说明这个角度是从一三象限来的，
                angle = Math.PI * 2 - angle;
            }
            angle = -angle;//旋转矩阵的角度是逆时针的，而从P到-Y是顺时针
            double cosforz = Math.Cos(angle);
            double sinforz = Math.Sin(angle);
            double[,] revolvez = { {cosforz,-sinforz,0,0 },//把剖面线旋转到与y轴重合
                                    {sinforz,cosforz,0,0 },
                                    {0,0,1,0 },
                                    { 0,0,0,1} };
            double cos90 = Math.Cos(Math.PI / 2);
            double sin90 = Math.Sin(Math.PI / 2);
            double[,] revolvey = {      { cos90,0,sin90,0},//直接绕y轴旋转90度，成为平行于XOY的模型
                                        {0,1,0,0 },
                                        {-sin90,0,cos90,0 },
                                        {0,0,0,1 } };
            var mb = Matrix<double>.Build;
            var translateTo000M = mb.DenseOfArray(translateTo000);
            var revolvezM = mb.DenseOfArray(revolvez);
            var revolveyM = mb.DenseOfArray(revolvey);
            var transM = revolveyM * revolvezM * translateTo000M;
            var transInverseM = transM.Inverse();
            this.transMat = transM;
            this.transInverseMat = transInverseM;
        }
        public void transEage(ref Eage eage ,bool trans) { //这个函数是把eage中的所有点都用矩阵转一下，当然也可以用矩阵转回去，全看bool是否为true，true则转到XOY面上去，false则转回原来
            int count = eage.vertexList.Count();
            for (int i = 0; i < count; i++) {
                Vertex vertex = eage.vertexList[i];
                double[] XYZ = new double[3];
                if (trans) { transXYZ(vertex.x, vertex.y, vertex.z, out XYZ); } 
                else { transBackXYZ(vertex.x, vertex.y, vertex.z, out XYZ); }
                eage.vertexList[i].x = XYZ[0];
                eage.vertexList[i].y = XYZ[1];
                eage.vertexList[i].z = XYZ[2];
            }  
        }
        public void transVertex(ref Vertex  vertex,bool trans ) {
            double[] XYZ = new double[3];
            if (trans) { transXYZ(vertex.x, vertex.y, vertex.z, out XYZ); }
            else { transBackXYZ(vertex.x, vertex.y, vertex.z, out XYZ); }
            vertex.x = XYZ[0];
            vertex.y = XYZ[1];
            vertex.z = XYZ[2];
        }
        public void transVertexList(ref List<Vertex> vertexList,bool trans) {
            int count = vertexList.Count();
            for (int i = 0; i < count; i++) {
                Vertex vertex = vertexList[i];
                transVertex(ref vertex , trans);
                vertexList[i] = vertex;
            }
        }
        /*public void transBrepModel( ref BrepModel brepModel,bool trans) {
            double[] XYZ = new double[3];
            int count = brepModel.vertexTable.Count;
            for(int i = 0; i < count; i++) {
                Vertex vertex = brepModel.vertexTable[i] as Vertex;
                transVertex(ref vertex, trans);
                brepModel.vertexTable[i] = vertex;
            }
            Mesh mesh = brepModel.mesh;

        }*/
        public void transXYZ(double x,double y,double z,out double[] XYZ) {//通过变换矩阵把x y z变为XOY平面上的坐标
            double[,] xyzsite = { { x }, { y }, { z }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyzM = mb.DenseOfArray(xyzsite);
            var XYZM = this.transMat * xyzM;
            double[,] columnXYZ = XYZM.ToArray();
            XYZ = new double[3];
            XYZ[0] = columnXYZ[0, 0];
            XYZ[1] = columnXYZ[1, 0];
            XYZ[2] = columnXYZ[2, 0];
        }
        public void transBackXYZ(double x, double y, double z, out double[] XYZ) {//通过变换矩阵的逆矩阵把x y z变回正确位置
            double[,] xyzsite = { { x }, { y }, { z }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyzM = mb.DenseOfArray(xyzsite);
            var XYZM = this.transInverseMat * xyzM;
            double[,] columnXYZ = XYZM.ToArray();
            XYZ = new double[3];
            XYZ[0] = columnXYZ[0, 0];
            XYZ[1] = columnXYZ[1, 0];
            XYZ[2] = columnXYZ[2, 0];
        }
    }
}
