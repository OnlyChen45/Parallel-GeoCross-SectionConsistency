using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 对一对三维点进行贝塞尔曲线求取，以及增添
    /// </summary>
    public class BezierCurve3D
    {
        Vertex startv, contorl1v, contorl2v, endv;
        /// <summary>
        /// 输入控制点，定义三阶贝塞尔曲线
        /// </summary>
        /// <param name="startv"></param>
        /// <param name="contorl1v"></param>
        /// <param name="contorl2v"></param>
        /// <param name="endv"></param>
        public BezierCurve3D(Vertex startv,Vertex contorl1v,Vertex contorl2v,Vertex endv) {
            this.startv = startv;
            this.contorl1v = contorl1v;
            this.contorl2v = contorl2v;
            this.endv = endv;
        }
        /// <summary>
        /// 根据需要的点的数量获取指定步长的贝塞尔线上点
        /// </summary>
        /// <param name="count"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Vertex[] getVertexsByNumber(int count,string name="") {
            double tstep = ((double)1) / (1 + count);
            List<Vertex> resultvlist = new List<Vertex>();
            Vertex vertex0 = getVertexByTime(0, name+"_0");
            resultvlist.Add(vertex0);
            for (int i = 1; i < count + 1; i++) {
                string tname = name +'_'+ i.ToString();
                double t= tstep* i;
                Vertex vertex = getVertexByTime(t, tname);
                resultvlist.Add(vertex);
            }
            int ttt = count + 1;
            Vertex vertexe = getVertexByTime(1, name + "_"+ttt.ToString());
            resultvlist.Add(vertexe);
            return resultvlist.ToArray();
        } 
        /// <summary>
        /// 根据t返回该位置的贝塞尔曲线上的点
        /// </summary>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Vertex getVertexByTime(double t,string name="")
        {//B(t)=(1−t)^3*P0+3t(1−t)^2*P1+3t^2*(1−t)*P2+t^3*P3, t∈[0,1]
            if (t < 0 || t > 1) { return null; }
            if (t == 0) return startv;
            if (t == 1) return endv;
            double x = Math.Pow(1 - t, 3) * startv.x + 3 * t * Math.Pow(1 - t, 2) * contorl1v.x + 3 * Math.Pow(t, 2) * (1 - t) * contorl2v.x + Math.Pow(t, 3) * endv.x;
            double y = Math.Pow(1 - t, 3) * startv.y + 3 * t * Math.Pow(1 - t, 2) * contorl1v.y + 3 * Math.Pow(t, 2) * (1 - t) * contorl2v.y + Math.Pow(t, 3) * endv.y;
            double z = Math.Pow(1 - t, 3) * startv.z + 3 * t * Math.Pow(1 - t, 2) * contorl1v.z + 3 * Math.Pow(t, 2) * (1 - t) * contorl2v.z + Math.Pow(t, 3) * endv.z;
            Vertex vertex = new Vertex(x, y, z);
            vertex.name = name;
            return vertex;
        }
        
    }
    public class MorphingWorker 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertices1"></param>
        /// <param name="vertices2"></param>
        /// <param name="control1start"></param>
        /// <param name="control1end"></param>
        /// <param name="control2start"></param>
        /// <param name="control2end"></param>
        /// <param name="bessel_num_collected"></param>
        /// <returns></returns>
        public static List<List<Vertex>> GenerateMorphingPoints(List<Vertex> vertices1, List<Vertex> vertices2,Vertex control1start, Vertex control1end, Vertex control2start, Vertex control2end, int bessel_num_collected = 5)
        {
            int morphLinesCount = bessel_num_collected;//morphing过渡线个数
            List<List<Vertex>> morphingPoints = new List<List<Vertex>>();
            List<Vertex> SRC = vertices1, DEST = vertices2, FC, LC;
            BezierCurve3D bezierCurve1 = new BezierCurve3D(vertices1[0], control1start, control2start, vertices2[0]);
            BezierCurve3D bezierCurve2 = new BezierCurve3D(vertices1.Last(), control1end, control2end, vertices2.Last());
            FC = new List<Vertex>(bezierCurve1.getVertexsByNumber(bessel_num_collected));
            LC = new List<Vertex>(bezierCurve2.getVertexsByNumber(bessel_num_collected));

            int morphingPointsCount = vertices1.Count;
            for (int i = 1; i <= morphLinesCount; ++i)
            {
                List<Vertex> morphingLine = new List<Vertex>();
                morphingPointsCount -= (vertices1.Count - vertices2.Count) / (morphLinesCount + 1);
                for (int j = 0; j < morphingPointsCount; ++j)
                {
                    Vertex vertex = Morphing(SRC, DEST, FC, LC, (double)j / (morphingPointsCount - 1), (double)i / (morphLinesCount + 1));
                    morphingLine.Add(vertex);
                }
                morphingPoints.Add(morphingLine);
            }
            return morphingPoints;
        }
        /// <summary>
        /// 用外部的这个约束边界来建模
        /// </summary>
        /// <param name="vertices1"></param>
        /// <param name="vertices2"></param>
        /// <param name="FC"></param>
        /// <param name="LC"></param>
        /// <param name="bessel_num_collected"></param>
        /// <returns></returns>
        public static List<List<Vertex>> GenerateMorphingPointsWithOutsideControlline(List<Vertex> vertices1, List<Vertex> vertices2, 
          List<Vertex>FC,List<Vertex>LC , int bessel_num_collected = 5)
        {
            int morphLinesCount = bessel_num_collected;//morphing过渡线个数
            List<List<Vertex>> morphingPoints = new List<List<Vertex>>();
            List<Vertex> SRC = vertices1, DEST = vertices2;
           // BezierCurve3D bezierCurve1 = new BezierCurve3D(vertices1[0], control1start, control2start, vertices2[0]);
            //BezierCurve3D bezierCurve2 = new BezierCurve3D(vertices1.Last(), control1end, control2end, vertices2.Last());
            //FC = new List<Vertex>(bezierCurve1.getVertexsByNumber(bessel_num_collected));
            //LC = new List<Vertex>(bezierCurve2.getVertexsByNumber(bessel_num_collected));

            int morphingPointsCount = vertices1.Count;
            for (int i = 1; i <= morphLinesCount; ++i)
            {
                List<Vertex> morphingLine = new List<Vertex>();
                morphingPointsCount -= (vertices1.Count - vertices2.Count) / (morphLinesCount + 1);
                for (int j = 0; j < morphingPointsCount; ++j)
                {
                    Vertex vertex = Morphing(SRC, DEST, FC, LC, (double)j / (morphingPointsCount - 1), (double)i / (morphLinesCount + 1));
                    morphingLine.Add(vertex);
                }
                morphingPoints.Add(morphingLine);
            }
            return morphingPoints;
/*            List<Vertex> getVertexListWithStep(List<Vertex> vertices,int stepcount)
            {
                List<Vertex> resultT = new List<Vertex>();



                return resultT;
            }*/
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SRC"></param>
        /// <param name="DEST"></param>
        /// <param name="FC"></param>
        /// <param name="LC"></param>
        /// <param name="u">描述所求点与FC、LC的靠近程度</param>
        /// <param name="v">描述所求点与SRC、DEST的靠近程度</param>
        /// <returns></returns>
        private static Vertex Morphing(List<Vertex> SRC, List<Vertex> DEST, List<Vertex> FC, List<Vertex> LC, double u, double v)
        {
            return H(SRC, DEST, u, v) + L(SRC, DEST, FC, LC, u, v);
        }

        private static Vertex L(List<Vertex> SRC, List<Vertex> DEST, List<Vertex> FC, List<Vertex> LC, double u, double v)
        {
            return (1 - u) * (PolylineParametricFunction(FC, v) - H(SRC, DEST, 0, v)) + u * (PolylineParametricFunction(LC, v) - H(SRC, DEST, 1, v));
        }



        private static Vertex H(List<Vertex> SRC, List<Vertex> DEST, double u, double v)
        {
            return (1 - v) * PolylineParametricFunction(SRC, u) + v * PolylineParametricFunction(DEST, u);
        }

        /// <summary>
        /// 折线参数方程
        /// 参数取0表示获取第一个顶点，取1代表获取最后一个，取v代表获取折线拉直后，对应1/v距离上的那个点
        /// 通过距离求取，就可以避免
        /// </summary>
        /// <param name="polylinePoints">折线点集</param>
        /// <param name="v">参数[0,1]</param>
        /// <returns></returns>
        public static Vertex PolylineParametricFunction(List<Vertex> polylinePoints, double v)
        {
            if (v < 0 || v > 1)
                throw new Exception("折线参数方程参数设置错误！");
            else if (v == 0)
                return polylinePoints[0];
            else if (v == 1)
                return polylinePoints[polylinePoints.Count - 1];
            double Length = 0, distance = 0;
            int count = polylinePoints.Count;
            int i = 0;
            for (; i < count - 1; i++)
            {
                distance = Vertex.Distance(polylinePoints[i], polylinePoints[i + 1]);
                if (distance == 0)
                    throw new Exception("折线参数方程中存在同名点！");
                Length += distance;
            }
            Length *= v;
            i = -1;
            while (Length >= 10e-5)//考虑到距离计算有一定误差
            {
                i++;
                distance = Vertex.Distance(polylinePoints[i], polylinePoints[i + 1]);
                if (distance <= 10e-5)
                    throw new Exception("折线参数方程中存在同名点！");
                Length -= distance;
            }
            return polylinePoints[i] + (Length + distance) / distance * (polylinePoints[i + 1] - polylinePoints[i]);
        }


    }
}
