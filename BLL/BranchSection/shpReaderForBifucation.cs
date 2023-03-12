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
    class shpReaderForBifucation
    {
       
        static public Dictionary<int, Geometry> getGeomListByFile(string inputfile, string idFieldName)
        {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(inputfile, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            long featurecount = layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++)
            {
                Feature feature = layer.GetFeature(i);
                int id = feature.GetFieldAsInteger(idFieldName);
                Geometry geometry = feature.GetGeometryRef();
                result.Add(id, geometry);
            }
            layer.Dispose();
            dataSource.Dispose();
            return result;
        }
        static public wkbGeometryType getShpGeomType(string inputfile)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(inputfile, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            wkbGeometryType geomtype = layer.GetGeomType();
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
}
