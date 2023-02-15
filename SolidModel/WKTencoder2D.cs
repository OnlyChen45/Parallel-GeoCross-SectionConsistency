using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class WKTencoder2D//针对于GDAL的解码器
    {
        public string wktstring;
        public int wkttype;
        public int pointnumber;
        private double[] coord1;
        private List<double> xlist;
        private List<double> ylist;
        /*POINT(6 10)
          LINESTRING(3 4,10 50,20 25)
          POLYGON((1 1,5 1,5 5,1 5,1 1),(2 2,2 3,3 3,3 2,2 2))
          MULTIPOINT(3.5 5.6, 4.8 10.5)
          MULTILINESTRING((3 4,10 50,20 25),(-5 -8,-10 -8,-15 -4))
          MULTIPOLYGON(((1 1,5 1,5 5,1 5,1 1),(2 2,2 3,3 3,3 2,2 2)),((6 3,9 2,9 4,6 3)))
         */
        public WKTencoder2D(string wkt)
        {
            this.wktstring = wkt;
            coord1 = new double[2];//初始化中间数据存储物
            xlist = new List<double>();
            ylist = new List<double>();
            int endindexoftype = this.wktstring.IndexOf('(');
            //bool marktype;
            this.wkttype = 0;
            if (this.wktstring.StartsWith("POINT"))
            {
                this.wkttype = 1;
            }
            if (this.wktstring.StartsWith("LINESTRING"))
            {
                this.wkttype = 2;
            }
            if (this.wktstring.StartsWith("POLYGON"))
            {
                this.wkttype = 3;
            }
            if (this.wktstring.StartsWith("MULTIPOINT"))
            {
                this.wkttype = 4;
            }
            if (this.wktstring.StartsWith("MULTILINESTRING"))
            {
                this.wkttype = 5;
            }
            if (this.wktstring.StartsWith("MULTIPOLYGON"))
            {
                this.wkttype = 6;
            }
        }
        private void dealpoint(string pointstr)
        {//处理Point括号内x y坐标，会将其更新到coord1中
            int indexofspace = pointstr.IndexOf(' ');
            int len1 = pointstr.Length;
            string xstr = pointstr.Substring(0, indexofspace);
            string ystr = pointstr.Substring(indexofspace + 1, len1 - 1 - indexofspace);
            this.coord1[0] = double.Parse(xstr);
            this.coord1[1] = double.Parse(ystr);
        }
        private int dealline(string linestr)
        {//处理Line内坐标，会将其更新到coordlist中 
            string[] points;
            xlist.Clear();
            ylist.Clear();
            points = linestr.Split(',');
            int pointcount = points.Length;
            this.pointnumber = pointcount;
            for (int i = 0; i < pointcount; i++)
            {
                dealpoint(points[i]);
                double x1 = this.coord1[0];
                double y1 = this.coord1[1];
                xlist.Add(x1);
                ylist.Add(y1);
            }
            return pointcount;
        }
        public double[] getpoint()
        {
            if (this.wkttype != 1) { return null; }
            //POINT(6 10)
            string contain1 = this.wktstring.Substring(6, this.wktstring.Length - 7);
            dealpoint(contain1);
            double[] result = new double[2];
            result[0] = this.coord1[0];
            result[1] = this.coord1[1];
            return result;
        }
        public double[] getmultpoints()
        {
            if (this.wkttype != 4) { return null; }
            //MULTIPOINT(3.5 5.6, 4.8 10.5)
            string contain1 = this.wktstring.Substring(12, this.wktstring.Length - 13);
            int pointnum = dealline(contain1);
            List<double> xylist = new List<double>();
            for (int i = 0; i < pointnum; i++)
            {
                xylist.Add(this.xlist[i]);
                xylist.Add(this.ylist[i]);
            }
            return xylist.ToArray();
        }
        public double[] getline()
        {
            if (this.wkttype != 2) { return null; }
            //LINESTRING(3 4,10 50,20 25)
            string contain1 = this.wktstring.Substring(12, this.wktstring.Length - 13);
            int pointnum = dealline(contain1);
            List<double> xylist = new List<double>();
            for (int i = 0; i < pointnum; i++)
            {
                xylist.Add(this.xlist[i]);
                xylist.Add(this.ylist[i]);
            }
            return xylist.ToArray();
        }
        public double[] getpolygon(int lineindex = -1)//lineindex是一个指定获取polygon中第几条线上点的参数，如果是-1则是获取全部的点
        {
            if (this.wkttype != 3) { return null; }
            //POLYGON((1 1, 5 1, 5 5, 1 5, 1 1),(2 2,2 3,3 3,3 2,2 2))
            string contain1 = this.wktstring.Substring(9, this.wktstring.Length - 10);
            int rightpoly = this.numberofpoly() - 1;
            string contain2 = null;
            string temp = contain1;
            List<double> xylist = new List<double>();
            int countlinepoint = 0;
            List<int> linepointcount = new List<int>();//记录一下每条线的点在xylist的起始索引
            while (rightpoly >= 0)
            {
                linepointcount.Add(countlinepoint);
                int rightbracket = temp.IndexOf(')');
                contain2 = temp.Substring(1, rightbracket - 1);
                int pointnum = dealline(contain2);
                for (int i = 0; i < pointnum; i++)
                {
                    xylist.Add(this.xlist[i]);
                    xylist.Add(this.ylist[i]);
                    countlinepoint++;
                    countlinepoint++;
                }
                if ((rightbracket + 2) > temp.Length) { break; }
                temp = temp.Substring(rightbracket + 2);
                rightpoly--;
            }
            linepointcount.Add(countlinepoint);
            double[] result = xylist.ToArray();
            if (lineindex == -1) return result;
            else
            {
                int linestart = linepointcount[lineindex];
                int lineend = linepointcount[lineindex + 1];
                double[] realresult = new double[lineend - linestart];
                Array.Copy(result, linestart, realresult, 0, lineend - linestart);
                return realresult;
            }
        }
        public int numberofpoly()
        {
            if (this.wkttype != 3) { return -1; }
            //POLYGON((1 1, 5 1, 5 5, 1 5, 1 1),(2 2,2 3,3 3,3 2,2 2))
            string contain1 = this.wktstring.Substring(9, this.wktstring.Length - 10);
            string contain2 = null;
            string temp = contain1;
            int rightbracket = temp.IndexOf(')');
            int result = 0;
            while (rightbracket >= 0)
            {
                result++;
                if ((rightbracket + 2) > temp.Length) { break; }
                temp = temp.Substring(rightbracket + 2);
                rightbracket = temp.IndexOf(')');
            }
            return result;
        }
        public int getpointnumber()
        {
            if (this.wkttype == 1) { return 1; }
            if (this.wkttype == 0) { return 0; }
            return 0;
        }
    }
}
