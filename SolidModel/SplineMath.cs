using GeoCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    class SplineMath
    {

        /**
		 * 光滑不等距插值
		 * 
		 * @param n - 结点的个数
		 * @param x - 一维数组，长度为n，存放给定的n个结点的值x(i)
		 * @param y - 一维数组，长度为n，存放给定的n个结点的函数值y(i)，
		 *            y(i) = f(x(i)), i=0,1,...,n-1
		 * @param t - 存放指定的插值点的值
		 * @param s - 一维数组，长度为5，其中s(0)，s(1)，s(2)，s(3)返回三次多项式的系数，
		 *  		  s(4)返回指定插值点t处的函数近似值f(t)（k<0时）或任意值（k>=0时）
		 * @param k - 控制参数，若k>=0，则只计算第k个子区间[x(k), x(k+1)]上的三次多项式的系数
		 * @return double 型，指定的查指点t的函数近似值f(t)
		 */
        public static double GetValueAkima(int n, double[] x, double[] y, double t, double[] s, int k)
        {
            int kk, m, l;
            double p, q;
            double[] u = new double[5];

            // 初值
            s[4] = 0.0;
            s[0] = 0.0;
            s[1] = 0.0;
            s[2] = 0.0;
            s[3] = 0.0;

            // 特例处理
            if (n < 1)
                return s[4];
            if (n == 1)
            {
                s[0] = y[0];
                s[4] = y[0];
                return s[4];
            }
            if (n == 2)
            {
                s[0] = y[0];
                s[1] = (y[1] - y[0]) / (x[1] - x[0]);
                if (k < 0)
                    s[4] = (y[0] * (t - x[1]) - y[1] * (t - x[0])) / (x[0] - x[1]);
                return s[4];
            }

            // 插值
            if (k < 0)
            {
                if (t <= x[1])
                    kk = 0;
                else if (t >= x[n - 1])
                    kk = n - 2;
                else
                {
                    kk = 1;
                    m = n;
                    while (((kk - m) != 1) && ((kk - m) != -1))
                    {
                        l = (kk + m) / 2;
                        if (t < x[l - 1])
                            m = l;
                        else
                            kk = l;
                    }

                    kk = kk - 1;
                }
            }
            else
                kk = k;

            if (kk >= n - 1)
                kk = n - 2;

            u[2] = (y[kk + 1] - y[kk]) / (x[kk + 1] - x[kk]);
            if (n == 3)
            {
                if (kk == 0)
                {
                    u[3] = (y[2] - y[1]) / (x[2] - x[1]);
                    u[4] = 2.0 * u[3] - u[2];
                    u[1] = 2.0 * u[2] - u[3];
                    u[0] = 2.0 * u[1] - u[2];
                }
                else
                {
                    u[1] = (y[1] - y[0]) / (x[1] - x[0]);
                    u[0] = 2.0 * u[1] - u[2];
                    u[3] = 2.0 * u[2] - u[1];
                    u[4] = 2.0 * u[3] - u[2];
                }
            }
            else
            {
                if (kk <= 1)
                {
                    u[3] = (y[kk + 2] - y[kk + 1]) / (x[kk + 2] - x[kk + 1]);
                    if (kk == 1)
                    {
                        u[1] = (y[1] - y[0]) / (x[1] - x[0]);
                        u[0] = 2.0 * u[1] - u[2];

                        if (n == 4)
                            u[4] = 2.0 * u[3] - u[2];
                        else
                            u[4] = (y[4] - y[3]) / (x[4] - x[3]);
                    }
                    else
                    {
                        u[1] = 2.0 * u[2] - u[3];
                        u[0] = 2.0 * u[1] - u[2];
                        u[4] = (y[3] - y[2]) / (x[3] - x[2]);
                    }
                }
                else if (kk >= (n - 3))
                {
                    u[1] = (y[kk] - y[kk - 1]) / (x[kk] - x[kk - 1]);
                    if (kk == (n - 3))
                    {
                        u[3] = (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);
                        u[4] = 2.0 * u[3] - u[2];
                        if (n == 4)
                            u[0] = 2.0 * u[1] - u[2];
                        else
                            u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    }
                    else
                    {
                        u[3] = 2.0 * u[2] - u[1];
                        u[4] = 2.0 * u[3] - u[2];
                        u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    }
                }
                else
                {
                    u[1] = (y[kk] - y[kk - 1]) / (x[kk] - x[kk - 1]);
                    u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    u[3] = (y[kk + 2] - y[kk + 1]) / (x[kk + 2] - x[kk + 1]);
                    u[4] = (y[kk + 3] - y[kk + 2]) / (x[kk + 3] - x[kk + 2]);
                }
            }

            s[0] = Math.Abs(u[3] - u[2]);
            s[1] = Math.Abs(u[0] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                p = (u[1] + u[2]) / 2.0;
            else
                p = (s[0] * u[1] + s[1] * u[2]) / (s[0] + s[1]);

            s[0] = Math.Abs(u[3] - u[4]);
            s[1] = Math.Abs(u[2] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                q = (u[2] + u[3]) / 2.0;
            else
                q = (s[0] * u[2] + s[1] * u[3]) / (s[0] + s[1]);

            s[0] = y[kk];
            s[1] = p;
            s[3] = x[kk + 1] - x[kk];
            s[2] = (3.0 * u[2] - 2.0 * p - q) / s[3];
            s[3] = (q + p - 2.0 * u[2]) / (s[3] * s[3]);
            if (k < 0)
            {
                p = t - x[kk];
                s[4] = s[0] + s[1] * p + s[2] * p * p + s[3] * p * p * p;
            }

            return s[4];
        }

        /**
     * 埃尔米特不等距插值
     * 
     * @param n - 结点的个数
     * @param x - 一维数组，长度为n，存放给定的n个结点的值x(i)
     * @param y - 一维数组，长度为n，存放给定的n个结点的函数值y(i)，
     *            y(i) = f(x(i)), i=0,1,...,n-1
     * @param dy - 一维数组，长度为n，存放给定的n个结点的函数导数值y'(i)，
     *             y'(i) = f'(x(i)), i=0,1,...,n-1
     * @param t - 存放指定的插值点的值
     * @return double 型，指定的查指点t的函数近似值f(t)
     */
        public static double GetValueHermite(int n, double[] x, double[] y, double[] dy, double t)
        {
            int i, j;
            double z, p, q, s;

            // 初值
            z = 0.0;

            // 循环插值
            for (i = 1; i <= n; i++)
            {
                s = 1.0;

                for (j = 1; j <= n; j++)
                {
                    if (j != i)
                        s = s * (t - x[j - 1]) / (x[i - 1] - x[j - 1]);
                }

                s = s * s;
                p = 0.0;

                for (j = 1; j <= n; j++)
                {
                    if (j != i)
                        p = p + 1.0 / (x[i - 1] - x[j - 1]);
                }

                q = y[i - 1] + (t - x[i - 1]) * (dy[i - 1] - 2.0 * y[i - 1] * p);
                z = z + q * s;
            }

            return (z);
        }


        /// <summary>
        /// 三次样条插值
        /// </summary>
        /// <param name="points">排序好的数</param>
        /// <param name="xs">需要计算的插值点</param>
        /// <param name="chf">写1</param>
        /// <returns>返回计算好的数值</returns>
        public static double[] SplineInsertPoint(Vertex[] points, double[] xs, int chf)
        {
            int plength = points.Length;
            double[] h = new double[plength];
            double[] f = new double[plength];
            double[] l = new double[plength];
            double[] v = new double[plength];
            double[] g = new double[plength];
 
            for (int i = 0; i < plength - 1; i++)
            {
                h[i] = points[i + 1].x - points[i].x;
                f[i] = (points[i + 1].y - points[i].y) / h[i];
            }
 
            for (int i = 1; i < plength - 1; i++)
            {
                l[i] = h[i] / (h[i - 1] + h[i]);
                v[i] = h[i - 1] / (h[i - 1] + h[i]);
                g[i] = 3 * (l[i] * f[i - 1] + v[i] * f[i]);
            }
 
            double[] b = new double[plength];
            double[] tem = new double[plength];
            double[] m = new double[plength];
            double f0 = (points[0].y - points[1].y) / (points[0].x - points[1].x);
            double fn = (points[plength - 1].y - points[plength - 2].y) / (points[plength - 1].x - points[plength - 2].x);
 
            b[1] = v[1] / 2;
            for (int i = 2; i < plength - 2; i++)
            {
                // Console.Write(" " + i);
                b[i] = v[i] / (2 - b[i - 1] * l[i]);
            }
            tem[1] = g[1] / 2;
            for (int i = 2; i < plength - 1; i++)
            {
                //Console.Write(" " + i);
                tem[i] = (g[i] - l[i] * tem[i - 1]) / (2 - l[i] * b[i - 1]);
            }
            m[plength - 2] = tem[plength - 2];
            for (int i = plength - 3; i > 0; i--)
            {
                //Console.Write(" " + i);
                m[i] = tem[i] - b[i] * m[i + 1];
            }
            m[0] = 3 * f[0] / 2.0;
            m[plength - 1] = fn;
            int xlength = xs.Length;
            double[] insertRes = new double[xlength];
            for (int i = 0; i < xlength; i++)
            {
                int j = 0;
                for (j = 0; j < plength; j++)
                {
                    if (xs[i] < points[j].x)
                        break;
                }
                j = j - 1;
                Console.WriteLine(j);
                if (j == -1 || j == points.Length - 1)
                {
                    if (j == -1)
                        throw new Exception("插值下边界超出");
                    if (j == points.Length - 1 && xs[i] == points[j].x)
                        insertRes[i] = points[j].y;
                    else
                        throw new Exception("插值下边界超出");
                }
                else
                {
                    double p1;
                    p1 = (xs[i] - points[j + 1].x) / (points[j].x - points[j + 1].x);
                    p1 = p1 * p1;
                    double p2; p2 = (xs[i] - points[j].x) / (points[j + 1].x - points[j].x);
                    p2 = p2 * p2;
                    double p3; p3 = p1 * (1 + 2 * (xs[i] - points[j].x) / (points[j + 1].x - points[j].x)) * points[j].y + p2 * (1 + 2 * (xs[i] - points[j + 1].x) / (points[j].x - points[j + 1].x)) * points[j + 1].y;
 
                    double p4; p4 = p1 * (xs[i] - points[j].x) * m[j] + p2 * (xs[i] - points[j + 1].x) * m[j + 1];
                    //         Console.WriteLine(m[j] + " " + m[j + 1] + " " + j);
                    p4 = p4 + p3;
                    insertRes[i] = p4;
                    //Console.WriteLine("f(" + xs[i] + ")= " + p4);
                }
 
            }
            //Console.ReadLine();
            return insertRes;
        }


        /// <summary>
        /// 写一个排序函数，使得输入的点按顺序排列，是因为插值算法的要求是，x轴递增有序的
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
         public static Vertex[] DeSortX(Vertex[] points)
        {
            int length = points.Length;
            double temx, temy;
            for (int i = 0; i < length - 1; i++)
            {
                for (int j = 0; j < length - i - 1; j++)
                    if (points[j].x > points[j + 1].x)
                    {
 
                        temx = points[j + 1].x;
                        points[j + 1].x = points[j].x;
                        points[j].x = temx;
                        temy = points[j + 1].y;
                        points[j + 1].y = points[j].y;
                        points[j].y = temy;
                    }
            }
            return points;
        }
    }
}
