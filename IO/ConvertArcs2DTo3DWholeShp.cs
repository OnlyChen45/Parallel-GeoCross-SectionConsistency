using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Runtime.InteropServices;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 把线转面，数据IO，
    /// </summary>
    public class ConvertArcs2DTo3DWholeShp
    {
        [DllImport("gdal303.dll", EntryPoint = "OGR_F_GetFieldAsBinary", CallingConvention = CallingConvention.Cdecl)]
        public extern static System.IntPtr OGR_F_GetFieldAsBinary(HandleRef handle, int index, out int byteCount);
        public ConvertArcs2DTo3DWholeShp()
        {//原来那个类是所有的线各自做旋转，
            //这次就应当是把旋转统一到一条线上，具体怎么统一比较好呢，我来想想
            //首先计算x y的方向最大最小值的差值，选个差值较大轴的作为标准
            //取确定轴方向的最大最小点作为起始终止点
            //记录这条线作为转过去转回来的基准线

        }
        private void getstartendxyz(ref double startx,ref double starty,ref double endx,ref double endy,ref double startz,List<Feature> featurelist) {
            //做个这个主要是防止默认的第一个feature是不含startx之类信息的
            int count = featurelist.Count;
            int i = 0;
            do
            {
                Feature feature0 = featurelist[i];
                startx = feature0.GetFieldAsDouble("startx");
                starty = feature0.GetFieldAsDouble("starty");
                startz = feature0.GetFieldAsDouble("startz");
                endx = feature0.GetFieldAsDouble("endx");
                endy = feature0.GetFieldAsDouble("endy");
                i++;
            }
            while (startx == 0 && i < count);
        }
        public void OpenPolysShpAndConvert(string path, string outputpath, out double[] transformAttri)
        {
            //这个就是把三维的线转成二维的线
            //转的规则就是，把每个线段xyz，
            CopyShp copyShp = new CopyShp(path, outputpath, 0, wkbGeometryType.wkbPolygon25D);
            DataSource orids = copyShp.getOriDataSource();
            DataSource outputds = copyShp.getOutputDataSource();
            List<FieldDefn> fieldDefns = copyShp.fieldlist;
            Layer orilayer = orids.GetLayerByIndex(0);
            Layer outlayer = outputds.GetLayerByIndex(0);
            long featurecount = orilayer.GetFeatureCount(1);
            List<Feature> featurelist = new List<Feature>();
            for (int i = 0; i < featurecount; i++)
            {
                Feature featureori = orilayer.GetFeature(i);
                featurelist.Add(featureori);
            }
            Feature feature0 = featurelist[0];
            double startx = 0, starty = 0, endx = 0, endy = 0, startz = 0, endz = 0;
            /*startx = feature0.GetFieldAsDouble("startx");
            starty = feature0.GetFieldAsDouble("starty");
            startz = feature0.GetFieldAsDouble("startz");
            endx = feature0.GetFieldAsDouble("endx");
            endy = feature0.GetFieldAsDouble("endy");*/
            // endz = feature0.GetFieldAsDouble("startx");
            getstartendxyz(ref startx, ref starty, ref endx, ref endy, ref startz, featurelist);//做个这个主要是防止默认的第一个feature是不含startx之类信息的
            double[] ttt = { startx, starty, startz, endx, endy };
            List<double> tttt = new List<double>();
            tttt.AddRange(ttt);
            transformAttri =tttt.ToArray();
             BaseLineTransformWorder2D transformworker = new BaseLineTransformWorder2D(startx, starty, endx, endy);//为一个面建立一个统一的转换矩阵
            for (int i = 0; i < featurecount; i++)
            {//在这装配所有的这个feature
                Feature featureori = featurelist[i];
                Geometry poly = featureori.GetGeometryRef();
                Geometry ring = poly.GetGeometryRef(0);
                int pointcount = ring.GetPointCount();

                /*double xmin = double.MaxValue;
                double xmax = double.MinValue;
                double ymin = double.MaxValue;
                double ymax = double.MinValue;*/
                List<double> xlist = new List<double>();
                List<double> ylist = new List<double>();
               // List<double> zlist = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double x, y, z;
                    x = ring.GetX(j);
                    y = ring.GetY(j);
                    //z = poly.GetZ(j);
                    xlist.Add(x);//首先获得所有的点坐标
                    ylist.Add(y);
                    //zlist.Add(z);
                }


                List<double> xlistnew = new List<double>();
                List<double> ylistnew = new List<double>();
                List<double> zlistnew = new List<double>();
                for (int j = 0; j < pointcount; j++)
                {
                    double xo = xlist[j];
                    double yo = ylist[j];
                    // zo = zlist[j];
                    double[] xy;
                    double[] xyz = new double[3];
                    transformworker.transXY(xo, yo, out xy);
                    xyz[0] = xy[0];
                    xyz[1] = 0;
                    xyz[2] = xy[1];
                    double[] xy2;
                    transformworker.transBackXY(xyz[0], xyz[1], out xy2);
                    xlistnew.Add(xy2[0]);//把转完的坐标拿到
                    ylistnew.Add(xy2[1]);
                    zlistnew.Add(xyz[2]);
                }
                Feature featurenew = new Feature(outlayer.GetLayerDefn());
                Geometry ring1;
                createRingByxyzlist(out ring1, xlistnew, ylistnew,zlistnew);
                copyFeatureAttri(featureori, ref featurenew, fieldDefns);
                Geometry npoly = new Geometry(wkbGeometryType.wkbPolygon25D);
                npoly.AddGeometry(ring1);
                featurenew.SetGeometry(npoly);
                outlayer.CreateFeature(featurenew);
                featureori.Dispose();
                featurenew.Dispose();
            }
            outlayer.Dispose();
            outputds.Dispose();
            orilayer.Dispose();
            orids.Dispose();
        }
        public Dictionary<string ,double> getTransformAttri(string path) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            Feature feature0 = layer.GetFeature(0);
            double startx, starty, endx, endy, startz, endz;
            startx = feature0.GetFieldAsDouble("startx");
            starty = feature0.GetFieldAsDouble("starty");
            startz = feature0.GetFieldAsDouble("startz");
            endx = feature0.GetFieldAsDouble("endx");
            endy = feature0.GetFieldAsDouble("endy");
            // endz = feature0.GetFieldAsDouble("startx");
            Dictionary<string, double> result = new Dictionary<string, double>();
            result.Add("startx", startx);
            result.Add("starty", starty);
            result.Add("startz", startz);
            result.Add("endx", endx);
            result.Add("endy", endy);
            return result;
        }
        public void ConvertPoint2DTo3D(string inputPath,string outputPath , Dictionary<string,double> transformAttri) {
            CopyShp copyShp = new CopyShp(inputPath, outputPath, 0, wkbGeometryType.wkbPoint25D);
            DataSource orids = copyShp.getOriDataSource();
            DataSource outputds = copyShp.getOutputDataSource();
            List<FieldDefn> fieldDefns = copyShp.fieldlist;
            Layer orilayer = orids.GetLayerByIndex(0);
            Layer outlayer = outputds.GetLayerByIndex(0);
            BaseLineTransformWorder2D transWorker= new BaseLineTransformWorder2D(transformAttri["startx"], transformAttri["starty"], transformAttri["endx"], transformAttri["endy"]);
            long pointcount = orilayer.GetFeatureCount(1);
            for (int i = 0; i < pointcount; i++) {
                Feature feature = orilayer.GetFeature(i);
                Feature featurenew = new Feature(outlayer.GetLayerDefn());
                Geometry point = feature.GetGeometryRef();
                double x = point.GetX(0);
                double y = point.GetY(0);
                double[] xy1,xy2;
                transWorker.transXY(x, y, out xy1);
                double[] xyz = new double[3];
                xyz[0] = xy1[0];
                xyz[1] = 0;
                xyz[2] = xy1[1];
                transWorker.transBackXY(xyz[0], xyz[1], out xy2);
                xyz[0] = xy2[0];
                xyz[1] = xy2[1];
                xyz[2] = xyz[2];
                Geometry pointnew = new Geometry(wkbGeometryType.wkbPoint25D);
                pointnew.AddPoint(xyz[0], xyz[1], xyz[2]);
                featurenew.SetGeometry(pointnew);
                copyFeatureAttri(feature, ref featurenew, fieldDefns);
                outlayer.CreateFeature(featurenew);
                feature.Dispose();
                featurenew.Dispose();
            }
            outlayer.Dispose();
            orilayer.Dispose();
            orids.Dispose();
            outputds.Dispose();
        }
        public void ConvertLines2DTo3D(string inputPath, string outputPath, Dictionary<string, double> transformAttri)
        {
            CopyShp copyShp = new CopyShp(inputPath, outputPath, 0, wkbGeometryType.wkbLineString25D);
            DataSource orids = copyShp.getOriDataSource();
            DataSource outputds = copyShp.getOutputDataSource();
            List<FieldDefn> fieldDefns = copyShp.fieldlist;
            Layer orilayer = orids.GetLayerByIndex(0);
            Layer outlayer = outputds.GetLayerByIndex(0);
            BaseLineTransformWorder2D transWorker = new BaseLineTransformWorder2D(transformAttri["startx"], transformAttri["starty"], transformAttri["endx"], transformAttri["endy"]);
            long pointcount = orilayer.GetFeatureCount(1);
            for (int i = 0; i < pointcount; i++)
            {
                Feature feature = orilayer.GetFeature(i);
                Feature featurenew = new Feature(outlayer.GetLayerDefn());
                Geometry line = feature.GetGeometryRef();
                int pcount = line.GetPointCount();
                Geometry linenew = new Geometry(wkbGeometryType.wkbLineString25D);
                for (int j = 0; j < pcount; j++)
                {
                    double x = line.GetX(j);
                    double y = line.GetY(j);
                    double[] xy1, xy2;
                    transWorker.transXY(x, y, out xy1);
                    double[] xyz = new double[3];
                    xyz[0] = xy1[0];
                    xyz[1] = 0;
                    xyz[2] = xy1[1];
                    transWorker.transBackXY(xyz[0], xyz[1], out xy2);
                    xyz[0] = xy2[0];
                    xyz[1] = xy2[1];
                    xyz[2] = xyz[2];
                    linenew.AddPoint(xyz[0], xyz[1], xyz[2]);
                }
                featurenew.SetGeometry(linenew);
                copyFeatureAttri(feature, ref featurenew, fieldDefns);
                outlayer.CreateFeature(featurenew);
                feature.Dispose();
                featurenew.Dispose();
            }
            outlayer.Dispose();
            orilayer.Dispose();
            orids.Dispose();
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
        private void createRingByxyzlist(out Geometry linestring, List<double> xlist, List<double> ylist,List<double >zlist)
        {
            linestring = new Geometry(wkbGeometryType.wkbLinearRing);
            int count = xlist.Count;
            for (int i = 0; i < count; i++)
            {
                double x = xlist[i];
                double y = ylist[i];
                double z = zlist[i];
                linestring.AddPoint(x, y, z);
            }
        }
        private void exchangedouble(ref double a, ref double b)
        {
            double t = a;
            a = b;
            b = t;
        }
    }
}
