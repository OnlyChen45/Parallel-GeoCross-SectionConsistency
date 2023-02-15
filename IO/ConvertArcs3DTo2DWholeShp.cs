using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Runtime.InteropServices;
using System.IO;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 三维线转二维线，数据IO
    /// </summary>
   public class ConvertArcs3DTo2DWholeShp
    {
        [DllImport("gdal303.dll", EntryPoint = "OGR_F_GetFieldAsBinary", CallingConvention = CallingConvention.Cdecl)]
        public extern static System.IntPtr OGR_F_GetFieldAsBinary(HandleRef handle, int index, out int byteCount);
        public ConvertArcs3DTo2DWholeShp()
        {//原来那个类是所有的线各自做旋转，
            //这次就应当是把旋转统一到一条线上，具体怎么统一比较好呢，我来想想
            //首先计算x y的方向最大最小值的差值，选个差值较大轴的作为标准
            //取确定轴方向的最大最小点作为起始终止点
            //记录这条线作为转过去转回来的基准线

        }
        public void OpenArcsShpAndConvert(string path, string outputpath,out double[] transformAttri)
        {
            //这个就是把三维的线转成二维的线
            //转的规则就是，把每个线段xyz，
            CopyShp copyShp = new CopyShp(path, outputpath, 0, wkbGeometryType.wkbLineString);
            DataSource orids = copyShp.getOriDataSource();
            DataSource outputds = copyShp.getOutputDataSource();
            List<FieldDefn> fieldDefns = copyShp.fieldlist;
            Layer orilayer = orids.GetLayerByIndex(0);
            Layer outlayer = outputds.GetLayerByIndex(0);
            FieldDefn fieldDefn1 = new FieldDefn("startx", FieldType.OFTReal);
            FieldDefn fieldDefn2 = new FieldDefn("starty", FieldType.OFTReal);
            FieldDefn fieldDefn3 = new FieldDefn("endx", FieldType.OFTReal);
            FieldDefn fieldDefn4 = new FieldDefn("endy", FieldType.OFTReal);
            FieldDefn fieldDefnz = new FieldDefn("startz", FieldType.OFTReal);
            outlayer.CreateField(fieldDefn1, 1);
            outlayer.CreateField(fieldDefn2, 1);
            outlayer.CreateField(fieldDefn3, 1);
            outlayer.CreateField(fieldDefn4, 1);
            outlayer.CreateField(fieldDefnz, 1);
            long featurecount = orilayer.GetFeatureCount(1);
            List<Feature> featurelist = new List<Feature>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature featureori = orilayer.GetFeature(i);
                featurelist.Add(featureori);
            }
            double[] minmaxxyz = getStartAndEndPoint(featurelist);
            
            double[] minxyz = { minmaxxyz[0], minmaxxyz[1], minmaxxyz[2] };
            double[] maxxyz = { minmaxxyz[3], minmaxxyz[4], minmaxxyz[5] };
            double startx, starty, endx, endy, startz,endz;
            startx = maxxyz[0];
            starty = maxxyz[1];
            startz = maxxyz[2];
            endx = minxyz[0];
            endy = minxyz[1];
            endz = minxyz[2];
            bool exchangeMark = false;
            if (starty > endy)
            {
                exchangedouble(ref startx, ref endx);
                exchangedouble(ref starty, ref endy);
                exchangedouble(ref startz, ref endz);
            }
            transformAttri = new double[6];
            transformAttri[0] = startx;
            transformAttri[1] = starty;
            transformAttri[2] = startz;
            transformAttri[3] = endx;
            transformAttri[4] = endy;
            transformAttri[5] = endz;
            BaseLineTransformWorder2D transformworker = new BaseLineTransformWorder2D(startx, starty, endx, endy);//为一个面建立一个统一的转换矩阵
            for (int i = 0; i < featurecount; i++)
            {//在这装配所有的这个feature
                Feature featureori = featurelist[i];
                Geometry arc = featureori.GetGeometryRef();
                int pointcount = arc.GetPointCount();

                /*double xmin = double.MaxValue;
                double xmax = double.MinValue;
                double ymin = double.MaxValue;
                double ymax = double.MinValue;*/
                List<double> xlist = new List<double>();
                List<double> ylist = new List<double>();
                List<double> zlist = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double x, y, z;
                    x = arc.GetX(j);
                    y = arc.GetY(j);
                    z = arc.GetZ(j);
                    xlist.Add(x);//首先获得所有的点坐标
                    ylist.Add(y);
                    zlist.Add(z);
                }


                List<double> xlistnew = new List<double>();
                List<double> ylistnew = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double xo = xlist[j];
                    double yo = ylist[j];
                    double zo = zlist[j];
                    double[] xy;
                    transformworker.transXY(xo, yo, out xy);
                    xy[1] = xy[1] + zo;//为每个的y坐标加上他的z坐标，就是它的
                    double[] xy2;
                    transformworker.transBackXY(xy[0], xy[1], out xy2);
                    xlistnew.Add(xy2[0]);//把转完的坐标拿到
                    ylistnew.Add(xy2[1]);
                }
                Feature featurenew = new Feature(outlayer.GetLayerDefn());
                Geometry line;
                createLineStringByxylist(out line, xlistnew, ylistnew);
                copyFeatureAttri(featureori, ref featurenew, fieldDefns);
                featurenew.SetField("startx", startx);
                featurenew.SetField("starty", starty);
                featurenew.SetField("startz", startz);
                featurenew.SetField("endx", endx);
                featurenew.SetField("endy", endy);
                featurenew.SetGeometry(line);
                outlayer.CreateFeature(featurenew);
                featureori.Dispose();
                featurenew.Dispose();
            }
            outlayer.Dispose();
            outputds.Dispose();
        }
        public Dictionary<int, List<double[]>> OpenPolysShpAndConvert(Dictionary<int, Geometry> geomDic, out double[] transformAttri, out Dictionary<int, List<double[]>> geomxyz)
        {
            //这个就是把三维的线转成二维的线
            //转的规则就是，把每个线段xyz，
            Dictionary<int, List<double[]>> result = new Dictionary<int, List<double[]>>();
            geomxyz = new Dictionary<int, List<double[]>>();


            List<Geometry> geomlist = geomDic.Values.ToList<Geometry>();

            double[] minmaxxyz = getStartAndEndPoint(geomlist);

            double[] minxyz = { minmaxxyz[0], minmaxxyz[1], minmaxxyz[2] };
            double[] maxxyz = { minmaxxyz[3], minmaxxyz[4], minmaxxyz[5] };
            double startx, starty, endx, endy, startz, endz;
            startx = maxxyz[0];
            starty = maxxyz[1];
            startz = maxxyz[2];
            endx = minxyz[0];
            endy = minxyz[1];
            endz = minxyz[2];
            bool exchangeMark = false;
            if (starty > endy)
            {
                exchangedouble(ref startx, ref endx);
                exchangedouble(ref starty, ref endy);
                exchangedouble(ref startz, ref endz);
            }
            transformAttri = new double[6];
            transformAttri[0] = startx;
            transformAttri[1] = starty;
            transformAttri[2] = startz;
            transformAttri[3] = endx;
            transformAttri[4] = endy;
            transformAttri[5] = endz;
            BaseLineTransformWorder2D transformworker = new BaseLineTransformWorder2D(startx, starty, endx, endy);//为一个面建立一个统一的转换矩阵
            foreach (var vk in geomDic)
            {//在这装配所有的这个feature
                //Feature featureori = featurelist[i];
                Geometry arc = vk.Value;
                arc = arc.GetGeometryRef(0);
                int pointcount = arc.GetPointCount();

                /*double xmin = double.MaxValue;
                double xmax = double.MinValue;
                double ymin = double.MaxValue;
                double ymax = double.MinValue;*/
                List<double> xlist = new List<double>();
                List<double> ylist = new List<double>();
                List<double> zlist = new List<double>();
                List<double[]> xyzlist = new List<double[]>();
                for (int j = 0; j < pointcount; j++)
                {
                    double x, y, z;
                    x = arc.GetX(j);
                    y = arc.GetY(j);
                    z = arc.GetZ(j);
                    xlist.Add(x);//首先获得所有的点坐标
                    ylist.Add(y);
                    zlist.Add(z);
                    double[] xyz = new double[3];
                    xyz[0] = x;
                    xyz[1] = y;
                    xyz[2] = z;
                    xyzlist.Add(xyz);
                }
                geomxyz.Add(vk.Key, xyzlist);

                List<double[]> xylistnew = new List<double[]>();
                // List<double> ylistnew = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double xo = xlist[j];
                    double yo = ylist[j];
                    double zo = zlist[j];
                    double[] xy;
                    transformworker.transXY(xo, yo, out xy);
                    xy[1] = xy[1] + zo;//为每个的y坐标加上他的z坐标，就是它的
                    double[] xy2;
                    transformworker.transBackXY(xy[0], xy[1], out xy2);
                    //xlistnew.Add(xy2[0]);
                    //ylistnew.Add(xy2[1]);
                    xylistnew.Add(xy2);
                }
                //在这写一下加入结果的
                result.Add(vk.Key, xylistnew);
            }
            return result;
        }
        public void OpenPolysShpAndConvert(string path, string outputpath, out double[] transformAttri)
            //把面要素从3维转到2维
        {
            //这个就是把三维的线转成二维的线
            //转的规则就是，把每个线段xyz，
            CopyShp copyShp = new CopyShp(path, outputpath, 0, wkbGeometryType.wkbPolygon);
            DataSource orids = copyShp.getOriDataSource();
            DataSource outputds = copyShp.getOutputDataSource();
            List<FieldDefn> fieldDefns = copyShp.fieldlist;
            Layer orilayer = orids.GetLayerByIndex(0);
            Layer outlayer = outputds.GetLayerByIndex(0);
            FieldDefn fieldDefn1 = new FieldDefn("startx", FieldType.OFTReal);
            FieldDefn fieldDefn2 = new FieldDefn("starty", FieldType.OFTReal);
            FieldDefn fieldDefn3 = new FieldDefn("endx", FieldType.OFTReal);
            FieldDefn fieldDefn4 = new FieldDefn("endy", FieldType.OFTReal);
            FieldDefn fieldDefnz = new FieldDefn("startz", FieldType.OFTReal);
            outlayer.CreateField(fieldDefn1, 1);
            outlayer.CreateField(fieldDefn2, 1);
            outlayer.CreateField(fieldDefn3, 1);
            outlayer.CreateField(fieldDefn4, 1);
            outlayer.CreateField(fieldDefnz, 1);
            long featurecount = orilayer.GetFeatureCount(1);
            List<Feature> featurelist = new List<Feature>();
            List<Geometry> geomlist = new List<Geometry>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature featureori = orilayer.GetFeature(i);
                Geometry geomori = featureori.GetGeometryRef();
                geomlist.Add(geomori);
                featurelist.Add(featureori);
            }
            double[] minmaxxyz = getStartAndEndPoint(geomlist);

            double[] minxyz = { minmaxxyz[0], minmaxxyz[1], minmaxxyz[2] };
            double[] maxxyz = { minmaxxyz[3], minmaxxyz[4], minmaxxyz[5] };
            double startx, starty, endx, endy, startz, endz;
            startx = maxxyz[0];
            starty = maxxyz[1];
            startz = maxxyz[2];
            endx = minxyz[0];
            endy = minxyz[1];
            endz = minxyz[2];
            bool exchangeMark = false;
            if (starty > endy)
            {
                exchangedouble(ref startx, ref endx);
                exchangedouble(ref starty, ref endy);
                exchangedouble(ref startz, ref endz);
            }
            transformAttri = new double[6];
            transformAttri[0] = startx;
            transformAttri[1] = starty;
            transformAttri[2] = startz;
            transformAttri[3] = endx;
            transformAttri[4] = endy;
            transformAttri[5] = endz;
            BaseLineTransformWorder2D transformworker = new BaseLineTransformWorder2D(startx, starty, endx, endy);//为一个面建立一个统一的转换矩阵
            for (int i = 0; i < featurecount; i++)
            {//在这装配所有的这个feature
                Feature featureori = featurelist[i];
                Geometry arc = featureori.GetGeometryRef();
                arc = arc.GetGeometryRef(0);
                int pointcount = arc.GetPointCount();

                /*double xmin = double.MaxValue;
                double xmax = double.MinValue;
                double ymin = double.MaxValue;
                double ymax = double.MinValue;*/
                List<double> xlist = new List<double>();
                List<double> ylist = new List<double>();
                List<double> zlist = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double x, y, z;
                    x = arc.GetX(j);
                    y = arc.GetY(j);
                    z = arc.GetZ(j);
                    xlist.Add(x);//首先获得所有的点坐标
                    ylist.Add(y);
                    zlist.Add(z);
                }


                List<double> xlistnew = new List<double>();
                List<double> ylistnew = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double xo = xlist[j];
                    double yo = ylist[j];
                    double zo = zlist[j];
                    double[] xy;
                    transformworker.transXY(xo, yo, out xy);
                    xy[1] = xy[1] + zo;//为每个的y坐标加上他的z坐标，就是它的
                    double[] xy2;
                    transformworker.transBackXY(xy[0], xy[1], out xy2);
                    xlistnew.Add(xy2[0]);//把转完的坐标拿到
                    ylistnew.Add(xy2[1]);
                }
                Feature featurenew = new Feature(outlayer.GetLayerDefn());
                Geometry polygeom;
                createPolygonByxylist(out polygeom, xlistnew, ylistnew);
                copyFeatureAttri(featureori, ref featurenew, fieldDefns);
                featurenew.SetField("startx", startx);
                featurenew.SetField("starty", starty);
                featurenew.SetField("startz", startz);
                featurenew.SetField("endx", endx);
                featurenew.SetField("endy", endy);
                featurenew.SetGeometry(polygeom);
                outlayer.CreateFeature(featurenew);
                featureori.Dispose();
                featurenew.Dispose();
            }
            outlayer.Dispose();
            outputds.Dispose();
        }
        private double[] getStartAndEndPoint(List<Feature> features3D) {//注意这里的feature是3D的线，为的是求出x y为极端值的点
            int count = features3D.Count;
            List<double[]> xyzlist = new List<double[]>();
            double minx, maxx, miny, maxy;
            minx = double.MaxValue;
            maxx = double.MinValue;
            miny = double.MaxValue;
            maxy = double.MinValue;
            for (int i = 0; i < count; i++) {
               
                Feature feature = features3D[i];
                Geometry geom = feature.GetGeometryRef();
                int pointcount = geom.GetPointCount();
                for (int j = 0; j < pointcount; j++) {
                    double x, y, z;
                    x = geom.GetX(j);
                    y = geom.GetY(j);
                    z = geom.GetZ(j);
                    double[] xyz = { x, y, z };
                    xyzlist.Add(xyz);
                }
            }
            int xyzcount = xyzlist.Count;
            for (int i = 0; i < xyzcount; i++) {
                double[] xyz = xyzlist[i];
                double x = xyz[0];
                double y = xyz[1];
                double z = xyz[2];
                minx = minx > x ? x : minx;//用三目运算符，更新最大最小的，x，y
                maxx = maxx < x ? x : maxx;
                miny = miny > y ? y : miny;
                maxy = maxy < y ? y : maxy;
            }
            double dx = maxx - minx;
            double dy = maxy - miny;
            bool xOry = false;// true就是按照x 求最大的值，false就是按照y
            if (dx > dy)
            {
                xOry = true;
            }
            else {
                xOry = false;
            }
            double mins = double.MaxValue, maxs = double.MinValue;
            int minindex = -1, maxindex = -1;
            for (int i = 0; i < xyzcount; i++) {//找出最大最小的
                double[] xyz = xyzlist[i];
                double x = xyz[0];
                double y = xyz[1];
                double z = xyz[2];
                if (xOry) {
                    if (x < mins) { mins = x;minindex = i; }
                    if (x > maxs) { maxs = x;maxindex = i; }
                }
                else
                {
                    if (y < mins) { mins = y; minindex = i; }
                    if (y > maxs) { maxs = y; maxindex = i; }
                }
            }
            double[] minxyz = xyzlist[minindex];
            double[] maxxyz = xyzlist[maxindex];
            double[] result = { minxyz[0], minxyz[1], minxyz[2], maxxyz[0], maxxyz[1], maxxyz[2] };
            return result;
        } 
        private void copyFeatureAttri(Feature feature, ref Feature featureout, List<FieldDefn> fieldlist)
        {
            foreach (FieldDefn fieldDefn in fieldlist)
            {
                string fieldname = fieldDefn.GetName();
                FieldType fieldType = fieldDefn.GetFieldType();
                switch (fieldType)
                {
                    case FieldType.OFTInteger:
                        {
                            int fieldvalue = feature.GetFieldAsInteger(fieldname);
                            featureout.SetField(fieldname, fieldvalue);
                            break;
                        }
                    case FieldType.OFTString:
                        {
                            //string fieldvalue = feature.GetFieldAsString(j);
                            //featureout.SetField(j, fieldvalue);
                            int index = feature.GetFieldIndex(fieldname);
                            int byteCount;
                            IntPtr intPtr = OGR_F_GetFieldAsBinary(Feature.getCPtr(feature), index, out byteCount);
                            if (intPtr == IntPtr.Zero)
                            {
                                featureout.SetField(fieldname, "");
                                break;
                            }
                            byte[] bytearray = new byte[byteCount];
                            Marshal.Copy(intPtr, bytearray, 0, byteCount);
                            string s = Encoding.UTF8.GetString(bytearray);
                            //Console.WriteLine(s);

                            featureout.SetField(fieldname, s);
                            break;
                        }
                    case FieldType.OFTReal:
                        {
                            double fieldvalue = feature.GetFieldAsDouble(fieldname);
                            featureout.SetField(fieldname, fieldvalue);
                            break;
                        }
                }
            }
        }
        private void createLineStringByxylist(out Geometry linestring, List<double> xlist, List<double> ylist)
        {
            linestring = new Geometry(wkbGeometryType.wkbLineString);
            int count = xlist.Count;
            for (int i = 0; i < count; i++)
            {
                double x = xlist[i];
                double y = ylist[i];
                linestring.AddPoint_2D(x, y);
            }
        }
        private void createPolygonByxylist(out Geometry polygeom, List<double> xlist, List<double> ylist)
        {
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            polygeom = new Geometry(wkbGeometryType.wkbPolygon);
            int count = xlist.Count;
            for (int i = 0; i < count; i++)
            {
                double x = xlist[i];
                double y = ylist[i];
                ring.AddPoint_2D(x, y);
            }
            ring.AddPoint_2D(xlist[0], ylist[0]);
            polygeom.AddGeometry(ring);
        }
        private double[] getStartAndEndPoint(List<Geometry> features3D)
        {
            int count = features3D.Count;
            List<double[]> xyzlist = new List<double[]>();
            double minx, maxx, miny, maxy;
            minx = double.MaxValue;
            maxx = double.MinValue;
            miny = double.MaxValue;
            maxy = double.MinValue;
            for (int i = 0; i < count; i++)
            {

                Geometry geom = features3D[i];
                geom = geom.GetGeometryRef(0);
                int pointcount = geom.GetPointCount();
                for (int j = 0; j < pointcount; j++)
                {
                    double x, y, z;
                    x = geom.GetX(j);
                    y = geom.GetY(j);
                    z = geom.GetZ(j);
                    double[] xyz = { x, y, z };
                    xyzlist.Add(xyz);
                }
            }
            int xyzcount = xyzlist.Count;
            for (int i = 0; i < xyzcount; i++)
            {
                double[] xyz = xyzlist[i];
                double x = xyz[0];
                double y = xyz[1];
                double z = xyz[2];
                minx = minx > x ? x : minx;//用三目运算符，更新最大最小的，x，y
                maxx = maxx < x ? x : maxx;
                miny = miny > y ? y : miny;
                maxy = maxy < y ? y : maxy;
            }
            double dx = maxx - minx;
            double dy = maxy - miny;
            bool xOry = false;// true就是按照x 求最大的值，false就是按照y
            if (dx > dy)
            {
                xOry = true;
            }
            else
            {
                xOry = false;
            }
            double mins = double.MaxValue, maxs = double.MinValue;
            int minindex = -1, maxindex = -1;
            for (int i = 0; i < xyzcount; i++)
            {//找出最大最小的
                double[] xyz = xyzlist[i];
                double x = xyz[0];
                double y = xyz[1];
                double z = xyz[2];
                if (xOry)
                {
                    if (x < mins) { mins = x; minindex = i; }
                    if (x > maxs) { maxs = x; maxindex = i; }
                }
                else
                {
                    if (y < mins) { mins = y; minindex = i; }
                    if (y > maxs) { maxs = y; maxindex = i; }
                }
            }
            double[] minxyz = xyzlist[minindex];
            double[] maxxyz = xyzlist[maxindex];
            double[] result = { minxyz[0], minxyz[1], minxyz[2], maxxyz[0], maxxyz[1], maxxyz[2] };
            return result;
        }
        private void exchangedouble(ref double a, ref double b)
        {
            double t = a;
            a = b;
            b = t;
        }
        public Dictionary<string, double> OpenArcsShpAndConvertToModel1(string path, string outputpath, out double[] transformAttri,bool moveToori=true)
        {
            //这个就是把三维的线转成二维的线
            //转的规则就是，把每个线段xyz，
            CopyShp copyShp = new CopyShp(path, outputpath, 0, wkbGeometryType.wkbLineString);
            DataSource orids = copyShp.getOriDataSource();
            DataSource outputds = copyShp.getOutputDataSource();
            List<FieldDefn> fieldDefns = copyShp.fieldlist;
            Layer orilayer = orids.GetLayerByIndex(0);
            Layer outlayer = outputds.GetLayerByIndex(0);
            Dictionary<string, double> transattribute = new Dictionary<string, double>();
            long featurecount = orilayer.GetFeatureCount(1);
            List<Feature> featurelist = new List<Feature>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature featureori = orilayer.GetFeature(i);
                featurelist.Add(featureori);
            }
            double[] minmaxxyz = getStartAndEndPoint(featurelist);

            double[] minxyz = { minmaxxyz[0], minmaxxyz[1], minmaxxyz[2] };
            double[] maxxyz = { minmaxxyz[3], minmaxxyz[4], minmaxxyz[5] };

            double startx, starty, endx, endy, startz, endz;
            startx = maxxyz[0];
            starty = maxxyz[1];
            startz = maxxyz[2];
            endx = minxyz[0];
            endy = minxyz[1];
            endz = minxyz[2];
            //考虑到这个旋转过程中，剖面线最下边的xy作为开始startxy，而后被转到右侧，那么还是最上边的endxy反而更适合作为开始的点

            /*            transattribute.Add("startX", endx);
                        transattribute.Add("startY", endy);
                        transattribute.Add("startZ", endz);
                        transattribute.Add("endX", startx);
                        transattribute.Add("endY", starty);*/
            bool exchangeMark = false;
            if (starty > endy)
            {
                exchangedouble(ref startx, ref endx);
                exchangedouble(ref starty, ref endy);
                exchangedouble(ref startz, ref endz);
            }
            transformAttri = new double[6];
            transformAttri[0] = startx;
            transformAttri[1] = starty;
            transformAttri[2] = startz;
            transformAttri[3] = endx;
            transformAttri[4] = endy;
            transformAttri[5] = endz;
            //这里设置模式1model1转换参数有个问题，就是，如果旋转时候越过了中线，那么如果还用end作为参数，就会在从x轴平移到正确位置时出现数据的反转
            //这样就不好了，我看看怎么办，也可以单弄一个不反转的，不过这样也不好，输出一个反转了的参数？
            /*transattribute.Add("startX", startx);
            transattribute.Add("startY", starty);
            transattribute.Add("startZ", startz);
            transattribute.Add("endX", endx);
            transattribute.Add("endY", endy);*/
            transattribute.Add("startX", endx);
            transattribute.Add("startY", endy);
            transattribute.Add("startZ", endz);
            transattribute.Add("endX", startx);
            transattribute.Add("endY", starty);
            BaseLineTransformWorder2D transformworker = new BaseLineTransformWorder2D(startx, starty, endx, endy);//为一个面建立一个统一的转换矩阵
            double[] xyt;
            //transformworker.makeMatFormiddata();
            //transformworker.transXY(startx, starty, out xyt);
            transformworker.transXY(endx, endy, out xyt);
            xyt[1] = xyt[1] + endz;
            int movex = (int)endx;
            int movey = (int)endy;
            if (moveToori) {
                xyt[0] = xyt[0] + movex;
                xyt[1] = xyt[1] + movey;
            }
            transattribute.Add("firstX", xyt[0]);
            transattribute.Add("firstY", xyt[1]);
            for (int i = 0; i < featurecount; i++)
            {//在这装配所有的这个feature
                Feature featureori = featurelist[i];
                Geometry arc = featureori.GetGeometryRef();
                int pointcount = arc.GetPointCount();
                List<double> xlist = new List<double>();
                List<double> ylist = new List<double>();
                List<double> zlist = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double x, y, z;
                    x = arc.GetX(j);
                    y = arc.GetY(j);
                    z = arc.GetZ(j);
                    xlist.Add(x);//首先获得所有的点坐标
                    ylist.Add(y);
                    zlist.Add(z);
                }


                List<double> xlistnew = new List<double>();
                List<double> ylistnew = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double xo = xlist[j];
                    double yo = ylist[j];
                    double zo = zlist[j];
                    double[] xy;
                    transformworker.transXY(xo, yo, out xy);
                    xy[1] = xy[1] + zo;//为每个的y坐标加上他的z坐标，就是它的
                    //double[] xy2;
                    // transformworker.transBackXY(xy[0], xy[1], out xy2);
                    if (moveToori) {
                        xy[0] += movex;
                        xy[1] += movey;
                    }
                    xlistnew.Add(xy[0]);//把转完的坐标拿到
                    ylistnew.Add(xy[1]);
                }
                Feature featurenew = new Feature(outlayer.GetLayerDefn());
                Geometry line;
                createLineStringByxylist(out line, xlistnew, ylistnew);
                copyFeatureAttri(featureori, ref featurenew, fieldDefns);
                featurenew.SetGeometry(line);
                outlayer.CreateFeature(featurenew);
                featureori.Dispose();
                featurenew.Dispose();
            }
            saveAttriTotxt(transattribute, outputpath);
            outlayer.Dispose();
            outputds.Dispose();
            return transattribute;
        }
        private void saveAttriTotxt(Dictionary<string, double> attri, string outshppath)
        {
            string txtpath = get3Dpar(outshppath);
            FileStream file = new FileStream(txtpath, FileMode.Create);
            StreamWriter writer = new StreamWriter(file);
            foreach (var vk in attri)
            {
                writer.WriteLine(vk.Key + ':' + vk.Value.ToString());
            }
            writer.Close();
            file.Close();
            string get3Dpar(string shppath)
            {
                string[] pathsplit = shppath.Split('.');
                string resu = pathsplit[0] + ".txt";
                return resu;
            }
        }

    }
}
