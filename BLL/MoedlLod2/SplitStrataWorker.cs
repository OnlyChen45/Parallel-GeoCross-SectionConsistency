using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using OpenCvSharp;
using MathNet.Numerics.LinearAlgebra;
using System.IO;
namespace ThreeDModelSystemForSection
{
    public class SplitStrataWorker
    {
        //输入，两个layer，两个layer对应的这个地层的编号
        //然后返回的是，被切好的Dictionary<id,geom>
        public SplitStrataWorker() { 
        }
        public Dictionary<int, Geometry> MakeGeomSplit(Layer layerori,List<int> stratas,Layer layerp,int stratap,string idFieldName,double tolerate) {
            //Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            Dictionary<int, Geometry> oriGeom = getGeomsByIds(layerori, stratas, idFieldName);
            List<int> temp = new List<int>();temp.Add(stratap);
            Dictionary<int, Geometry> tempgeom = getGeomsByIds(layerp, temp, idFieldName);
            Geometry stratapGeom = tempgeom[stratap];
            //到这就拿到了所有内容
            //下面开始制作toolbox
            MinOutsourceRect rotatedRectOri;
            Dictionary<int, Geometry> fullpolys = SplitStrataToolbox.getFullGeoms(oriGeom, out rotatedRectOri,tolerate);
            //现在就开始，求变换矩阵
            Matrix<double> transmatrix = SplitStrataToolbox.getTransMatWorker(rotatedRectOri, stratapGeom);
            //变换矩阵求好了，下面干嘛呢，下面就做出变换好的fullpolys
            Dictionary<int, Geometry> fullpolysTransed = SplitStrataToolbox.geomDicTrans(fullpolys, transmatrix);
            //这次就把所有的面切出来就好了
        //string savetrianglepath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\temp012.shp";
          // string spatialpath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\finalbuffers2Change_LTP.shp";
         // SplitStrataToolbox.saveGeom(fullpolysTransed.Values.ToList<Geometry>(), savetrianglepath, spatialpath);
            Dictionary<int, Geometry> splitResult = SplitStrataToolbox.geomSplit(fullpolysTransed, stratapGeom);
            return splitResult;
        }
        public Dictionary<int, Geometry> MakeGeomSplit(Layer layerori, List<int> stratas, Layer layerp, int stratap, string idFieldName, double tolerate,double xbuffer,double ybuffer)
        {//新增，xbuffer，ybuffer，创造更大的外包的矩形
            //Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            Dictionary<int, Geometry> oriGeom = getGeomsByIds(layerori, stratas, idFieldName);

            List<int> temp = new List<int>(); temp.Add(stratap);
            Dictionary<int, Geometry> tempgeom = getGeomsByIds(layerp, temp, idFieldName);
            Geometry stratapGeom = tempgeom[stratap];
            if (stratas.Count < 2)//如果输入进来的就是一个面，或者0个，直接返回目标的原来没被分割的那个
            {
                Dictionary<int, Geometry> ttt = new Dictionary<int, Geometry>();
                ttt.Add(stratap, stratapGeom);
                return ttt;
            }
            //到这就拿到了所有内容
            //下面开始制作toolbox
            MinOutsourceRect rotatedRectOri;
            Dictionary<int, Geometry> fullpolys = SplitStrataToolbox.getFullGeoms(oriGeom, out rotatedRectOri, tolerate,xbuffer,ybuffer);
            //现在就开始，求变换矩阵
            Matrix<double> transmatrix = SplitStrataToolbox.getTransMatWorker(rotatedRectOri, stratapGeom,xbuffer,ybuffer);
            //变换矩阵求好了，下面干嘛呢，下面就做出变换好的fullpolys
            Dictionary<int, Geometry> fullpolysTransed = SplitStrataToolbox.geomDicTrans(fullpolys, transmatrix);
            //现在的问题来了，我们需要把这个面上下左右挪一挪，然后找到合适的满足条件的
            //我希望的条件是，所有的面都只切出polygon，而不是multipolygon,也不是none
            //首先给个条件，遍历所有的结果面，如果存在multi或者none，就进入挪的过程
            Dictionary<int, Geometry> splitResultt = SplitStrataToolbox.geomSplit(fullpolysTransed, stratapGeom);
            bool needDeal = false;
            foreach (var vk in splitResultt) {//看看切下来的结果合理不
                Geometry geomtemp = vk.Value;
                wkbGeometryType geometryType = geomtemp.GetGeometryType();
                string wkt;
                geomtemp.ExportToWkt(out wkt);
                if (geomtemp.IsEmpty() || geometryType == wkbGeometryType.wkbMultiPolygon) {
                    needDeal = true;
                    break;
                }
            }
            if (needDeal) {
                Dictionary<int, Geometry> movedfullgeom;
                moveAndFindRightFullgeom(stratapGeom,fullpolysTransed,out movedfullgeom,xbuffer,ybuffer);
                fullpolysTransed = movedfullgeom;
            }
            string savetrianglepath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\temp014.shp";
            string spatialpath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\finalbuffers2Change_LTP.shp";
            SplitStrataToolbox.saveGeom(fullpolysTransed.Values.ToList<Geometry>(), savetrianglepath, spatialpath);
            Dictionary<int, Geometry> splitResult = SplitStrataToolbox.geomSplit(fullpolysTransed, stratapGeom);
            return splitResult;
        }
        private void moveAndFindRightFullgeom(Geometry strata,Dictionary<int,Geometry> orifullGeoms,out Dictionary<int ,Geometry> result,double xbuffer,double ybuffer) {
            //参数解释，starta是要分割的目标geom
            double xstep=0.1, ystep=0.1;
            int movex=20, movey=20;//暂时给一个初始值就好，0.1 20 作为左右挪的这个值
            if (xbuffer > xstep * 20) {
                xstep = xbuffer / 20;
            }
            else {
                movex = (int)(xbuffer / xstep);
            }
            if (ybuffer > ystep * 20)
            {
                ystep = ybuffer / 20;
            }
            else
            {
                movey = (int)(ybuffer / ystep);
            }
            //这样就有了，搜索的步长，搜索的数量，数量不大于40*40
            //有个问题，整体移动已有的fullGeoms需要动的点坐标数量比较多，所以，应该是移动目标的单一geoim，就是参数中的strata
            //移动位置是否可用判断条件三级层层递进，
            //第一，所有的裁剪面与被裁减地层的intersect为true，第二，intersection结果均为polygon而非multi，第三，裁剪后的所有面的比例相近。目前考虑执行效率问题，采纳第二层
            //把40*40全做出来，然后弄一个bool[40,40]
            bool[,] usefulsite = new bool[41, 41];
            for (int i = 0; i < movex * 2+1; i++) {
                
                for (int j = 0; j < movey * 2+1; j++) {
                    bool usefulthissite = true;//默认为true，检测到不可用就置为false
                    int px = i - movex;
                    int py = j - movey;
                    double mx = px * xstep;
                    double my = py * ystep;
                    Geometry moveStrata =movePoly (strata, -mx, -my);//为了减小计算量，反向移动strata，然后得到的叠置分析结果应该是和正向移动所有其他地层是一样的
                    foreach (var vk in orifullGeoms) {

                        Geometry cutpoly = vk.Value;
                        bool bbb = cutpoly.Intersect(moveStrata);
                        if (bbb == false) {
                            //如果，存在一个与移动后地层叠置不相交的裁减面，那么这个位置不可用
                            usefulthissite = false;
                            break;
                        }
                        Geometry interGeom = cutpoly.Intersection(moveStrata);
                        wkbGeometryType geomtype = interGeom.GetGeometryType();
                        if (geomtype != wkbGeometryType.wkbPolygon||interGeom.GetGeometryCount()!=1) {
                            //如果存在一个叠置后不是单一polygon,或者poly含有多个不连续部分，就不可用
                            usefulthissite = false;
                            break;
                        }
                        double area = interGeom.GetArea();
                        if (area < 1) {
                            //如果面积太小，不利于后边的继续运算，所以就把它排除
                            usefulthissite = false;
                            break;
                        }
                    }
                    usefulsite[i, j] = usefulthissite;
                }
            }//这样就获得了是否可用的一个数组
            int tari=-1, tarj=-1;
            double mindis = double.MaxValue;
            for (int i = 0; i < movex * 2 + 1; i++)
            {
                for (int j = 0; j < movey * 2 + 1; j++) {
                    if (usefulsite[i, j]) {
                        int px = i - movex;
                        int py = j - movey;
                        double mx = px * xstep;
                        double my = py * ystep;
                        double dis = distance(mx, my, 0, 0);//因为mx,my为偏移量，所以距离是跟0,0算
                        if (dis < mindis) {
                            mindis = dis;
                            tari = i;
                            tarj = j;
                        }
                    }
                }
            }
            int tarx = tari - movex;
            int tary = tarj - movey;
            double movetarx = tarx * xstep;
            double movetary = tary * ystep;
            result = new Dictionary<int, Geometry>();
            foreach (var vk in orifullGeoms) {
                int id = vk.Key;
                Geometry geom1 = vk.Value;
                Geometry geommoved = movePoly(geom1, movetarx, movetary);
                result.Add(id, geommoved);
            }
          //  return result;
        }
        private Geometry movePoly(Geometry poly,double offsetx,double offsety) {
            Geometry ring = poly.GetGeometryRef(0);
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            Geometry ringresult = new Geometry(wkbGeometryType.wkbLinearRing);
            int count = ring.GetPointCount();
            for (int i = 0; i < count; i++) {
                double x, y;
                x = ring.GetX(i);
                y = ring.GetY(i);
                x += offsetx;
                y += offsety;
                ringresult.AddPoint_2D(x, y);
            }
            result.AddGeometry(ringresult);
            return result;
        }
        private Dictionary<int,Geometry> getGeomsByIds(Layer layer,List<int> ids,string idFieldName) {
            long featurecount = layer.GetFeatureCount(0);
            Dictionary<int,Geometry> result = new Dictionary<int, Geometry>();
            for (int i = 0; i < featurecount; i++) {
                Feature feature = layer.GetFeature(i);
                int id = feature.GetFieldAsInteger(idFieldName);
                if (ids.Contains(id)) {
                    Geometry geometry = feature.GetGeometryRef();
                    result.Add(id, geometry);
                    ids.Remove(id);
                }
            }
            return result;
        }
        private double distance(double x1,double y1,double x2,double y2) {
            double dx = Math.Abs(x2 - x1);
            double dy = Math.Abs(y2 - y1);
            double result = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
            return result;
        }
        public static void saveGeom(Dictionary<int,Geometry> geometries, string path, string spatialpath,string idFieldName)
        {

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Geometry geom1 = geometries[0];
            Layer layer = dataSource.CreateLayer("result", getSpatialRef(spatialpath), geom1.GetGeometryType(), null);
            FieldDefn idfieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(idfieldDefn,1);
            int count = geometries.Count;
            foreach(var vk in geometries)
            {
                int id = vk.Key;
                Geometry geom = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(geom);
                feature.SetField(idFieldName, id);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public static void saveGeom(Dictionary<int, Geometry> geometries, string path, SpatialReference spatialReference, string idFieldName)
        {

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Geometry geom1 = geometries.Values.ToArray<Geometry>()[0] ;
            Layer layer = dataSource.CreateLayer("result", spatialReference, geom1.GetGeometryType(), null);
            FieldDefn idfieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(idfieldDefn, 1);
            int count = geometries.Count;
            foreach (var vk in geometries)
            {
                int id = vk.Key;
                Geometry geom = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(geom);
                feature.SetField(idFieldName, id);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public static void saveGeom(Dictionary<int, Geometry> geometries, string path, SpatialReference spatialReference, string idFieldName,Dictionary<string,double> otherAttri )
        {

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Geometry geom1 = geometries.Values.ToArray<Geometry>()[0];
            Layer layer = dataSource.CreateLayer("result", spatialReference, geom1.GetGeometryType(), null);
            FieldDefn idfieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(idfieldDefn, 1);
            foreach (var vk in otherAttri) {
                string name = vk.Key;
                FieldDefn fieldDefn = new FieldDefn(name, FieldType.OFTReal);
                layer.CreateField(fieldDefn,1);
            }
            int count = geometries.Count;
            foreach (var vk in geometries)
            {
                int id = vk.Key;
                Geometry geom = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(geom);
                feature.SetField(idFieldName, id);
                foreach (var vk2 in otherAttri) {
                    feature.SetField(vk2.Key, vk2.Value);
                }
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public static void saveGeom(Dictionary<int, Geometry> geometries, string path, SpatialReference spatialReference, string idFieldName, Dictionary<string, double> otherAttri,bool standardization,string[] pyscriptPath)
        {

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Geometry geom1 = geometries.Values.ToArray<Geometry>()[0];
            Layer layer = dataSource.CreateLayer("result", spatialReference, geom1.GetGeometryType(), null);
            FieldDefn idfieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(idfieldDefn, 1);
            foreach (var vk in otherAttri)
            {
                string name = vk.Key;
                FieldDefn fieldDefn = new FieldDefn(name, FieldType.OFTReal);
                layer.CreateField(fieldDefn, 1);
            }
            int count = geometries.Count;
            foreach (var vk in geometries)
            {
                int id = vk.Key;
                Geometry geom = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(geom);
                feature.SetField(idFieldName, id);
                foreach (var vk2 in otherAttri)
                {
                    feature.SetField(vk2.Key, vk2.Value);
                }
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
            if (standardization == true) {
                string target;
                string folder = Path.GetDirectoryName(path);
                string filename = Path.GetFileName(path);
                string[] nameAndEx = filename.Split('.');
                target = folder + '\\' + nameAndEx[0] + "_Standard.shp";
                string[] paras = { pyscriptPath[0], pyscriptPath[1], path, target, pyscriptPath[2] };
                string commandstr = CMDHandler.makePara(paras);//把cmd命令的所有参数合成一行
                CMDHandler.Processing(commandstr);//执行
                MatchLayer matchLayer = new MatchLayer(path, target, gdalDriverType.SHP, idFieldName);
            }
        }
        public static SpatialReference getSpatialRef(string path)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            SpatialReference spatialReference = layer.GetSpatialRef();
            layer.Dispose();
            dataSource.Dispose();
            return spatialReference;
        }
    }
}
