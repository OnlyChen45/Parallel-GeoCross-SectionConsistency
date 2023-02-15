using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.IO;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 输入数据时候的点转3D，数据IO
    /// </summary>
    public class ConvertPointsTo3D
    {
        private string shpname;
        private Layer pointslayer;
        public double startX, startY, startZ, endX, endY;
        public double firstX, firstY;
        public string idfieldname;
        public Dictionary<long, int> ids;
        private Dictionary<long, double[]> pointsdic;
        public ConvertPointsTo3D(string shpname, double startX, double startY, double startZ, double endX, double endY, double firstX, double firstY,string idfieldname)
        {
            this.shpname = shpname;
            Ogr.RegisterAll();
            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(shpname,1);
            this.pointslayer = dataSource.GetLayerByIndex(0);
            this.startX = startX;
            this.startY = startY;
            this.startZ = startZ;
            this.endX = endX;
            this.endY = endY;
            this.firstX = firstX;
            this.firstY = firstY;
            this.pointsdic = new Dictionary<long, double[]>();
            this.idfieldname = idfieldname;
            this.ids = new Dictionary<long, int>();
        }
        public void convertCoord() {
            int pointcount =(int) this.pointslayer.GetFeatureCount(1);
            for (int i = 0; i < pointcount; i++) {
                Feature feature = this.pointslayer.GetFeature(i);
                long fid = feature.GetFID();
                Geometry point = feature.GetGeometryRef();
                double[] xy = new double[2];
                xy[0] = point.GetX(0);
                xy[1] = point.GetY(0);
                double[] XYZ;
                getRealXYZ(out XYZ, xy[0], xy[1], startX, startY, startZ, endX, endY, firstX, firstY);
                this.pointsdic.Add(fid, XYZ);
                int id = feature.GetFieldAsInteger(this.idfieldname);
                this.ids.Add(fid, id);
            }
        }
        public void saveToTXT(string path) {
            FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(fileStream);
            int count = this.pointsdic.Count;
            string line;
            foreach (var kv in this.pointsdic) {
                long key = kv.Key;
                double[] xyz = kv.Value;
                line = key.ToString() + ',' + xyz[0].ToString() + ',' + xyz[1].ToString() + ',' + xyz[2].ToString();
                writer.WriteLine(line);
            }
            writer.Close();
            fileStream.Close();
        }
        public void saveToShp(string path,string layername) {
            SpatialReference spatialReference = this.pointslayer .GetSpatialRef();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(path, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPoint25D, null);
            //int count = this.linesdic.Count();
            FieldDefn fieldDefn = new FieldDefn(this.idfieldname, FieldType.OFTInteger);
            layer.CreateField(fieldDefn,1);
            Feature feature = new Feature(layer.GetLayerDefn());
            foreach (var vk in this.pointsdic)
            {
                long key = vk.Key;
                double[] pointsc = vk.Value;
                Geometry geometry = new Geometry(wkbGeometryType.wkbPoint25D);
                geometry.AddPoint(pointsc[0], pointsc[1], pointsc[2]);
             
                feature.SetGeometry(geometry);
                feature.SetField(this.idfieldname, ids[key]);
                layer.CreateFeature(feature);
            }
        }
        private void getRealXYZ(out double[] XYZ, double x, double y, double startX, double startY, double startZ, double endX, double endY, double firstX, double firstY)
        {
            //这些参数分别是，XYZ输出的该点正确坐标，x该点在地层shp中的x坐标，y该点在地层shp中的y坐标,startX startY startZ剖面线起始点坐标，endX endY剖面线末端点坐标，fisrtX firstY 地层剖面的首端点坐标
            XYZ = new double[3];
            double X = x - firstX;
            double dz = y - firstY;
            double distance = Math.Sqrt((startX - endX) * (startX - endX) + (startY - endY) * (startY - endY));
            double dx = endX - startX;
            double dy = endY - startY;
            XYZ[2] = dz + startZ;//把高程求出来，即shp中y与左上y的差值加上高程startz
            XYZ[0] = startX + dx * X / distance;//求出该点X
            XYZ[1] = startY + dy * X / distance;//求出该点Y
        }
    }
}
