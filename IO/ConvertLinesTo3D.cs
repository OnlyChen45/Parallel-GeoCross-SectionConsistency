using System;
using System.Collections;
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
    /// 输入数据时候的线转3D，数据IO
    /// </summary>
   public class ConvertLinesTo3D
    {
        private string shpname;
        private Dictionary<long, List<double[]>> linesdic;
        private Dictionary<long, ArrayList> attriTable;
        private Dictionary<long, int> linesLithid;
        private Layer lineslayer;
        public double startX, startY, startZ, endX, endY;
        public double firstX, firstY;
        public string idFieldName;
        public ConvertLinesTo3D(string shpname, double startX, double startY, double startZ, double endX, double endY, double firstX, double firstY,string idFieldName)
        {
            this.shpname = shpname;
            Ogr.RegisterAll();
            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(shpname, 1);
            this.lineslayer = dataSource.GetLayerByIndex(0);
            this.startX = startX;
            this.startY = startY;
            this.startZ = startZ;
            this.endX = endX;
            this.endY = endY;
            this.firstX = firstX;
            this.firstY = firstY;
            this.idFieldName = idFieldName;
            this.linesdic = new Dictionary<long, List<double[]>>();
            this.linesLithid = new Dictionary<long, int>();
            this.attriTable = new Dictionary<long, ArrayList>();
        }
        public void convertLines() {
            Feature feature=new Feature(this.lineslayer.GetLayerDefn());
            int linescount = (int)this.lineslayer.GetFeatureCount(1);
            int fieldcount = feature.GetFieldCount();
            for (int i = 0; i < linescount; i++) {
                feature = this.lineslayer.GetFeature(i);
                Geometry line1 = feature.GetGeometryRef();
                long fid = feature.GetFID();
                //这次我要把所有的字段都转移到新对象里边去
                ArrayList arrayList = new ArrayList();
                for (int j = 0; j < fieldcount; j++) {
                    FieldType fieldType = feature.GetFieldType(j);
                    switch (fieldType)
                    {
                        case FieldType.OFTInteger:
                            {
                                int fieldvalue = feature.GetFieldAsInteger(j);
                                arrayList.Add(fieldvalue);
                                break;
                            }
                        case FieldType.OFTString:
                            {
                                string fieldvalue = feature.GetFieldAsString(j);
                                arrayList.Add(fieldvalue);
                                break;
                            }
                        case FieldType.OFTReal:
                            {
                                double fieldvalue = feature.GetFieldAsDouble(j);
                                arrayList.Add(fieldvalue);
                                break;
                            }
                    }
                }
                int lithid = feature.GetFieldAsInteger(this.idFieldName);
                int pointcount = line1.GetPointCount();
                List<double[]> linecoords = new List<double[]>();
                for (int j = 0; j < pointcount; j++) {
                    double x = line1.GetX(j);
                    double y = line1.GetY(j);
                    double[] XYZ;
                    getRealXYZ(out XYZ, x, y, startX, startY, startZ, endX, endY, firstX, firstY);
                    linecoords.Add(XYZ);
                }
                this.linesdic.Add(fid, linecoords);
                this.linesLithid.Add(fid, lithid);
                this.attriTable.Add(fid, arrayList);
            }

        }
        public void saveToShp(string path,string layername) {
            //新创建的存储位置
            SpatialReference spatialReference = this.lineslayer.GetSpatialRef();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(path, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbLineString25D, null);
            //int count = this.linesdic.Count();
            // FieldDefn fieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            //layer.CreateField(fieldDefn,1);
            List<FieldDefn> myfields;
            getFieldList(this.lineslayer, out myfields);
            for (int i = 0; i < myfields.Count; i++) {
                FieldDefn fieldDefnt = myfields[i];
                layer.CreateField(fieldDefnt, 1);
            }
            Feature feature = new Feature(layer.GetLayerDefn());
            foreach (var vk in this.linesdic) {
                long key = vk.Key;
                List<double[]> pointsc = vk.Value;
                int cc = pointsc.Count;
                Geometry geometry = new Geometry(wkbGeometryType.wkbLineString25D);
                for (int j = 0; j < cc; j++) {
                    double[] XYZ = pointsc[j];
                    double x = XYZ[0];
                    double y = XYZ[1];
                    double z = XYZ[2];
                    geometry.AddPoint(x, y, z);
                }
                feature.SetGeometry(geometry);
                //feature.SetField(idFieldName, this.linesLithid[key]);
                ArrayList arrayList = this.attriTable[key];
                for (int j = 0; j < arrayList.Count; j++) {
                    FieldType fieldType = feature.GetFieldType(j);
                    switch (fieldType)
                    {
                        case FieldType.OFTInteger:
                            {
                                int value = (int)arrayList[j];
                                feature.SetField(j, value); 
                                break;
                            }
                        case FieldType.OFTString:
                            {
                                string value = (string)arrayList[j];
                                feature.SetField(j, value);
                                break;
                            }
                        case FieldType.OFTReal:
                            {
                                double value = (double)arrayList[j];
                                feature.SetField(j, value);
                                break;
                            }
                    }
                }
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
        private void getFieldList(Layer layer, out List<FieldDefn> fieldlist)
        {
            fieldlist = new List<FieldDefn>();
            Feature feature = layer.GetFeature(0);
            int fieldcount = feature.GetFieldCount();
            for (int i = 0; i < fieldcount; i++)
            {
                FieldDefn fieldDefn = feature.GetFieldDefnRef(i);
                fieldlist.Add(fieldDefn);

            }
        }
    }
}
