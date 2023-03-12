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
    /// 新建shp，数据IO
    /// </summary>
    public class CopyShp
    {
        string orishppath;
        string outputshppath;
        public List<FieldDefn> fieldlist { get; }
        public CopyShp(string orishp,string outputshp,int layerindex=0,wkbGeometryType geomtype=wkbGeometryType.wkbNone) {
            Gdal.AllRegister();
            Ogr.RegisterAll();
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");
            this.orishppath = orishp;
            this.outputshppath = outputshp;
            DataSource dataSource = openDs(orishp);
            Layer orilayer = dataSource.GetLayerByIndex(layerindex);
            List<FieldDefn> fieldDefns;
            getFieldList(orilayer, out fieldDefns);
            DataSource outputds = null;
            if (geomtype == wkbGeometryType.wkbNone)
            {
                outputds = CreateSameShp(outputshp, orilayer.GetName(), orilayer.GetSpatialRef(), orilayer.GetGeomType(), fieldDefns);
            }
            else {
                outputds = CreateSameShp(outputshp, orilayer.GetName(), orilayer.GetSpatialRef(), geomtype, fieldDefns);
            }
            this.fieldlist = fieldDefns;
            orilayer.Dispose();
            outputds.Dispose();
            dataSource.Dispose();
        }
        public DataSource getOutputDataSource() {
            DataSource ds= openDs(this.outputshppath);
            return ds;
        }
        public DataSource getOriDataSource() {
            DataSource ds = openDs(this.orishppath);
            return ds;
        }
        private DataSource openDs(string path) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            return dataSource;
        }
        private void getFieldList(Layer layer, out List<FieldDefn> fieldlist)
        {
            fieldlist = new List<FieldDefn>();
            //  Feature feature = new Feature(layer.GetLayerDefn());
            Feature feature = layer.GetFeature(0);
            int fieldcount = feature.GetFieldCount();
            for (int i = 0; i < fieldcount; i++)
            {
                FieldDefn fieldDefn = feature.GetFieldDefnRef(i);
                fieldlist.Add(fieldDefn);
            }
        }
        private DataSource CreateSameShp(string outpath,string layername, SpatialReference spatialReference, wkbGeometryType geometryType, List<FieldDefn> fieldDefns) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(outpath, null);
            Layer layer = dataSource.CreateLayer(layername, spatialReference, geometryType, null);
            int fieldcount = fieldDefns.Count;
            for (int i = 0; i < fieldcount; i++) {
                layer.CreateField(fieldDefns[i],1);
            }
            layer.Dispose();
            return dataSource;
        }

    }
}
