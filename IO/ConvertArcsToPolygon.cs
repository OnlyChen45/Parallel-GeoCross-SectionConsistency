using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{
    public class ConvertArcsToPolygon
    {
        //线转面算法，数据IO
        //这个类就是把已经标记好的arcs给变成polygon，这就比较简单了
        //做法就是，读取所有的arcs，然后按照polygonid给编个组，弄个Dictionary<int,List<geometry>>
        //然后就是那个osr.buildPolygonFromEages就出一个geometry
        //然后一保存的
        SpatialReference SpatialReference;
        public ConvertArcsToPolygon() { 
        
        }
        public Dictionary<int, Geometry> ReadAndConvert(string arcpath,string polyfieldname1,string polyfieldname2,double minTolerance =5) {
            //CopyShp copyShp = new CopyShp(arcpath, outputpath, geomtype: wkbGeometryType.wkbPolygon);
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource orids = driver.Open(arcpath,1);
            //DataSource outds = copyShp.getOutputDataSource();
            Layer arclayer = orids.GetLayerByIndex(0);
           // Layer outlayer = outds.GetLayerByIndex(0);
            Dictionary<int, List<Geometry>> polyBoundrylist = new Dictionary<int, List<Geometry>>();
            long arcscount = arclayer.GetFeatureCount(0);
            for (int i = 0; i < arcscount; i++) {
                Feature feature = arclayer.GetFeature(i);
                int polygon1 = feature.GetFieldAsInteger(polyfieldname1);
                int polygon2 = feature.GetFieldAsInteger(polyfieldname2);
                Geometry arc = feature.GetGeometryRef();
                if (!polyBoundrylist.ContainsKey(polygon1)) {
                    List<Geometry> geoms = new List<Geometry>();
                    polyBoundrylist.Add(polygon1, geoms);
                }
                if (!polyBoundrylist.ContainsKey(polygon2))
                {
                    List<Geometry> geoms = new List<Geometry>();
                    polyBoundrylist.Add(polygon2, geoms);
                }
                polyBoundrylist[polygon1].Add(arc);
                polyBoundrylist[polygon2].Add(arc);
            }
            Dictionary<int, Geometry> polys = new Dictionary<int, Geometry>();
            foreach (var vk in polyBoundrylist) {
                int polyid = vk.Key;
                List<Geometry> arclist = vk.Value;
                Geometry multilines = new Geometry(wkbGeometryType.wkbMultiLineString);
                List<string> wktlist = new List<string>();
                foreach (Geometry line in arclist) {
                    multilines.AddGeometry(line);
                    string wkt;
                    line.ExportToWkt(out wkt);
                    wktlist.Add(wkt);
                }
                //string folder = @"D:\研究生项目\走通流程\3dTo2dworkspace\middata\";
                //savelinelist(folder, polyid, arclist);
                Geometry polygeom = new Geometry(wkbGeometryType.wkbPolygon);
                int trytimes = 0;
            TryBuildAgain:
                trytimes++;
                try
                {
                    polygeom = Ogr.BuildPolygonFromEdges(multilines, 5, 0, minTolerance );
                }
                catch {
                    minTolerance  += 1;
                    Console.WriteLine("Geom(" + polyid.ToString() + ") Can not fix ring by Tolorance" + minTolerance.ToString());
                    if (trytimes > 100) {
                        Console.WriteLine("Build Fail");
                        Console.ReadLine();
                        return null;
                    }
                    goto TryBuildAgain;
                }
                if (polyid != -1)
                {
                    Geometry ring = polygeom.GetGeometryRef(0);//检查一下输出的geometry是不是首尾相接的
                    int pointcountinring = ring.GetPointCount();
                    double x1 = ring.GetX(0);
                    double y1 = ring.GetY(0);
                    double xe = ring.GetX(pointcountinring - 1);
                    double ye = ring.GetX(pointcountinring - 1);
                    if (!((Math.Abs(x1 - xe) <= 0.0000001) && (Math.Abs(y1 - ye) <= 0.0000001))) {
                        ring.AddPoint_2D(x1, y1);
                        polygeom = new Geometry(wkbGeometryType.wkbPolygon);
                        polygeom.AddGeometry(ring);
                    }
                    Geometry resultgeom = makePolyValid(polygeom);
                    polys.Add(polyid, resultgeom);
                }
            }
            this.SpatialReference = arclayer.GetSpatialRef();
            return polys;
        }
        private Geometry makePolyValid(Geometry poly) {
            //合成的polygon会有一堆乱七八糟的问题，比如自相交，比如坏掉的multipolygon，给它处理一下，
            //首先看看是不是好的
            //如果是好的，那么就把multipolygon拆开，拿到中间面积最大的就行了
            Geometry result = null;
            bool polyvalid = poly.IsValid();
            if (polyvalid) {//如果这个poly是好的，那么就直接获取
                int geomcount = poly.GetGeometryCount();
                if (geomcount == 1)
                {
                    if (poly.GetGeometryType() == wkbGeometryType.wkbPolygon)
                    {
                        return poly;
                    }
                    else if(poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygon) {
                        return poly.GetGeometryRef(0);
                    }
                }
                else {//是一个polygon含有多个环，还是如何，反正都给变成multipolygon
                    Geometry multip = Ogr.ForceToMultiPolygon(poly);
                    // int countmu = multip.GetGeometryCount();
                    return getMaxPolyInMultipoly(multip);
                }
            }
            else//这个面不是好的，这个面有毛病,怎么处理呢，就直接给它做了
            {
                Geometry multipoly = poly.MakeValid();//做直接给它用这个函数给做出一个结果
                Geometry geom11 = getMaxPolyInMultipoly(multipoly);
                result=geom11;
            }
            return result;
        }
        private Geometry getMaxPolyInMultipoly(Geometry multipoly) {
            Geometry result = null;
            double maxarea = double.MinValue;
            int geomcount = multipoly.GetGeometryCount();
            for(int i = 0; i < geomcount; i++) {
                Geometry poly = multipoly.GetGeometryRef(i);
                double area1 = poly.Area();
                if (area1 > maxarea) {
                    maxarea = area1;
                    result = poly;
                }
            }
            return result;
        }
        public void savePolys(string outputpath,string idFieldName,Dictionary<int ,Geometry>polys) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(outputpath, null);
            Layer layer = dataSource.CreateLayer("polygon", this.SpatialReference, wkbGeometryType.wkbPolygon, null);
            FieldDefn fieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(fieldDefn,1);
            foreach (var vk in polys) {
                int id = vk.Key;
                Geometry poly = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(poly);
                feature.SetField(idFieldName, id);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public void savePolys(string outputpath, string idFieldName, Dictionary<int, Geometry> polys,double[] transformAttribute)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(outputpath, null);
            Layer layer = dataSource.CreateLayer("polygon", this.SpatialReference, wkbGeometryType.wkbPolygon, null);
            FieldDefn fieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(fieldDefn, 1);
            FieldDefn fieldDefn1 = new FieldDefn("startx", FieldType.OFTReal);
            FieldDefn fieldDefn2 = new FieldDefn("starty", FieldType.OFTReal);
            FieldDefn fieldDefn3 = new FieldDefn("endx", FieldType.OFTReal);
            FieldDefn fieldDefn4 = new FieldDefn("endy", FieldType.OFTReal);
            FieldDefn fieldDefnz = new FieldDefn("startz", FieldType.OFTReal);
            layer.CreateField(fieldDefn1, 1);
            layer.CreateField(fieldDefn2, 1);
            layer.CreateField(fieldDefn3, 1);
            layer.CreateField(fieldDefn4, 1);
            layer.CreateField(fieldDefnz, 1);
            double startx, starty, endx, endy, startz, endz;
            startx = transformAttribute[0];
            starty = transformAttribute[1];
            startz = transformAttribute[2];
            endx = transformAttribute[3];
            endy = transformAttribute[4];
            endz = transformAttribute[5];
            foreach (var vk in polys)
            {
                int id = vk.Key;
                Geometry poly = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(poly);
                feature.SetField(idFieldName, id);
                feature.SetField("startx", startx);
                feature.SetField("starty", starty);
                feature.SetField("startz", startz);
                feature.SetField("endx", endx);
                feature.SetField("endy", endy);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        private void savelinelist(string outputfolder,int id,List<Geometry>lines) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            string outputpath = outputfolder + "linelist" + id.ToString() + ".shp";
            DataSource dataSource = driver.CreateDataSource(outputpath, null);
            Layer layer = dataSource.CreateLayer("polygon", this.SpatialReference, wkbGeometryType.wkbLineString, null);
            foreach (Geometry line1 in lines) {
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(line1);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
    }
}
