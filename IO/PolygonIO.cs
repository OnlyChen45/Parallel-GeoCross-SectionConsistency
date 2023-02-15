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
    /// 多边形读取，数据IO
    /// 
    /// </summary>
    public class PolygonIO
    {
        public string polypath;
        Dictionary<int, Geometry> polygons;
        SpatialReference spatialReference;
        List<int> id;
        public PolygonIO(string path,string idFieldName) {
            this.polypath = path;
            this.polygons = new Dictionary<int, Geometry>();
            
            this.id = new List<int>();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            long featurecount = layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++) {
                Feature feature = layer.GetFeature(i);
                Geometry geometry = feature.GetGeometryRef();
                int featureid = feature.GetFieldAsInteger(idFieldName);
                this.id.Add(featureid);
                this.polygons.Add(featureid, geometry);
            }
            this.spatialReference = layer.GetSpatialRef();
            layer.Dispose();
            dataSource.Dispose();
        }
        public void getGeomAndId(out Dictionary<int, Geometry> polyGeoms,out List<int> idlist) {
            polyGeoms = new Dictionary<int, Geometry>();
            idlist = new List<int>();
            foreach(var vk in this.polygons) {
                polyGeoms.Add(vk.Key, vk.Value);
                idlist.Add(vk.Key);
            }

        
        }
        public int getMaxid()
        {
            List<int> ids = polygons.Keys.ToList();
            return  ids.Max();
        }
        public SpatialReference getSpatialRef() {
            return this.spatialReference;
        }
    }
}
