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
    /// <summary>
    /// 又是一个读取类，草，草草
    /// 数据IO
    /// 
    /// </summary>
    public class shpReader
    {
        static public Dictionary<int, Geometry> getGeomListByFile(string inputfile,string idFieldName) {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(inputfile, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            long featurecount = layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++) {
                Feature feature = layer.GetFeature(i);
                int id = feature.GetFieldAsInteger(idFieldName);
                Geometry geometry = feature.GetGeometryRef();
                result.Add(id, geometry);
            }
            layer.Dispose();
            dataSource.Dispose();
            return result;
        }
        static public wkbGeometryType getShpGeomType(string inputfile) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(inputfile, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            wkbGeometryType geomtype= layer.GetGeomType();
            layer.Dispose();
            dataSource.Dispose();
            return geomtype;
        }
        static public Dictionary<int, Geometry> getGeomListByFile(string inputfile, string idFieldName, out SpatialReference spatialReference)
        {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(inputfile, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            spatialReference = layer.GetSpatialRef();
            long featurecount = layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++)
            {
                Feature feature = layer.GetFeature(i);
                int id = feature.GetFieldAsInteger(idFieldName);
                Geometry geometry = feature.GetGeometryRef();
                result.Add(id, geometry);
            }
            return result;
        }
        static public Dictionary<int, Geometry> getGeomListByFileWithFID(string inputfile, out SpatialReference spatialReference)
        {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(inputfile, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            spatialReference = layer.GetSpatialRef();
            long featurecount = layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++)
            {
                Feature feature = layer.GetFeature(i);
                int id = (int)feature.GetFID();
                Geometry geometry = feature.GetGeometryRef();
                result.Add(id, geometry);
            }
            return result;
        }
        static public void saveDicOfGeoms(string savepath, Dictionary<int, Geometry> geoms, string idFieldname, SpatialReference spatialReference)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(savepath, null);
            Geometry ggg = geoms.Values.ToArray<Geometry>()[0];
            Layer layer = ds.CreateLayer("geom", spatialReference, ggg.GetGeometryType(), null);
            FieldDefn fieldDefn = new FieldDefn(idFieldname, FieldType.OFTInteger);
            layer.CreateField(fieldDefn, 1);
            foreach (var vk in geoms)
            {
                int id = vk.Key;
                Geometry gege = vk.Value;
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(gege);
                feature.SetField(idFieldname, id);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            ds.Dispose();
        }
    }
    public class attrireader {
        DataSource dataSource;
        Layer layer;
        long featurecount;
        public attrireader(string path) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            dataSource = driver.Open(path, 1);
            layer = dataSource.GetLayerByIndex(0);
            this.featurecount = layer.GetFeatureCount(1);
        }
        public Dictionary<int,int> getattrilistByName(string attriname,string FieldName="") {
            Dictionary<int, int> resultdic = new Dictionary<int, int>();
            for (int i = 0; i < featurecount; i++) {
                int id = -1;
                Feature feature = layer.GetFeature(i);
                if (FieldName.Equals(""))
                {
                    id = (int)feature.GetFID();
                }
                else {
                    id = (int)feature.GetFieldAsInteger(FieldName);
                }
                int tinter = feature.GetFieldAsInteger(attriname);
                resultdic.Add(id, tinter);
                
            }
            return resultdic;
        }
        public Dictionary<int, double> getattridoublelistByName(string attriname,string FieldName = "")
        {
            Dictionary<int, double> resultdic = new Dictionary<int, double>();
            for (int i = 0; i < featurecount; i++)
            {
                int id = -1;
                Feature feature = layer.GetFeature(i);
                if (FieldName.Equals(""))
                {
                    id = (int)feature.GetFID();
                }
                else
                {
                    id = (int)feature.GetFieldAsInteger(FieldName);
                }
                double tinter = feature.GetFieldAsDouble(attriname);
                resultdic.Add(id, tinter);

            }
            return resultdic;
        }
        public void getstartendxyz(out double startx, out double starty, out double endx, out double endy, out double startz)
        {
            //做个这个主要是防止默认的第一个feature是不含startx之类信息的
            long featurecount = layer.GetFeatureCount(1);
            List<Feature> featurelist = new List<Feature>();
            for (int k = 0; k < featurecount; k++) 
            {
                featurelist.Add(layer.GetFeature(k));
            }
            int i = 0;
            int count = featurelist.Count;
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
        public void add3DAttributeField() 
        {
            FieldDefn fieldDefn1 = new FieldDefn("startx", FieldType.OFTReal);
            FieldDefn fieldDefn2 = new FieldDefn("starty", FieldType.OFTReal);
            FieldDefn fieldDefn3 = new FieldDefn("startz", FieldType.OFTReal);
            FieldDefn fieldDefn4 = new FieldDefn("endx", FieldType.OFTReal);
            FieldDefn fieldDefn5 = new FieldDefn("endy", FieldType.OFTReal);
            layer.CreateField(fieldDefn1, 1);
            layer.CreateField(fieldDefn2, 1);
            layer.CreateField(fieldDefn3, 1);
            layer.CreateField(fieldDefn4, 1);
            layer.CreateField(fieldDefn5, 1);
        }
        public void save3DAttribute(double startx,double starty,double endx,double endy,double startz) 
        {
            long count = layer.GetFeatureCount(1);
            for (int i = 0; i < count; i++) 
            {
                Feature feature = layer.GetFeature(i);
                feature.SetField("startx", startx);
                feature.SetField("starty", starty);
                feature.SetField("startz", startz);
                feature.SetField("endx", endx);
                feature.SetField("endy", endy);
                layer.SetFeature(feature);
            }
        }
        public void layerdispose() {
            this.layer.Dispose();
            this.dataSource.Dispose();
        }
    }
}
