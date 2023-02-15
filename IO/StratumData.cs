using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace ThreeDModelSystemForSection { 
  public  class OriSingleStratumPolygon { //用来记录原始的这个图形信息，而且这个类专门为polygon写的，为后边写入xml做准备，当然如果即取即用，那么就直接用这个就行了
        public int LithId;
        public long FID;
        public wkbGeometryType shapeType;
        public Geometry geometry;
        public List<List<double[]>> XYZlist;//这个是本地层剖面的三维坐标，可以直接取出在程序中使用
        public string wkt;//这个是本地层剖面的wkt文本，方便进行后续的存取
        public OriSingleStratumPolygon(Feature feature,int indexofLithId,int indexofFID, wkbGeometryType shapeType) {
            this.geometry = feature.GetGeometryRef();
            this.LithId = feature.GetFieldAsInteger(indexofLithId);
            //this.FID = feature.GetFieldAsInteger64(indexofFID);
            this.FID = feature.GetFID();
            this.shapeType = shapeType;
            XYZlist = new List<List<double[]>>();
        }
        private void getRealXYZ(out double [] XYZ,double x,double y , double startX, double startY, double startZ,double endX,double endY,double firstX, double firstY) {
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
        public void getRealCoordWKT(out string wkt,out  List<List<double[]>> realCoordXYZlist, double startX,double startY,double startZ,double endX,double endY,double firstX,double firstY) {
            wkt = "POLYGON(";//初始化输出的wkt
            realCoordXYZlist = new List<List<double[]>>();//初始化这个输出的坐标列表
            int geomcount = this.geometry.GetGeometryCount();
            List<List<double >> XPoints = new List<List<double>>();
            List<List<double >> YPoints = new List<List<double>>();
            for (int i = 0; i < geomcount; i++) {
                Geometry geom1 = this.geometry.GetGeometryRef(i);
                int count = geom1.GetPointCount();
                List<double > xlist = new List<double >();
                List<double > ylist = new List<double >();
                for (int j = 0; j < count; j++) {
                    double x = geom1.GetX(j);
                    double y = geom1.GetY(j);
                    xlist.Add(x);
                    ylist.Add(y);
                }
                XPoints.Add(xlist);
                YPoints.Add(ylist);
            }
            int countXPoints = XPoints.Count();
            for (int i = 0; i < countXPoints; i++) {
                List<double> xlist = XPoints[i];
                List<double> ylist = YPoints[i];
                int countxlist = xlist.Count();
                wkt = wkt + '(';//为一个新的geometry增加括号
                List<double[]> geomPoints = new List<double[]>();
                for (int j = 0; j < countxlist; j++) {
                    double[] XYZ;
                    getRealXYZ(out XYZ, xlist[j], ylist[j], startX, startY, startZ, endX, endY, firstX, firstY);
                    geomPoints.Add(XYZ);
                    wkt = wkt + XYZ[0].ToString() + ' ' + XYZ[1].ToString() + ' ' + XYZ[2].ToString();
                    if (j != countxlist - 1) wkt = wkt + ',';
                }
                realCoordXYZlist.Add(geomPoints);
                if (i != countXPoints - 1) wkt = wkt + "),";
                else wkt = wkt + ')';
            }
            wkt = wkt + ')';
            this.XYZlist = realCoordXYZlist;
            this.wkt = wkt;
        }
    }
   public class StratumData//这个类是为了完整存取使用,打算加入XML读写，
    {
        public double startX, startY, startZ,endX,endY;
        public double firstX, firstY;
        public string shpFilePath;
        public string idFieldName;
        SpatialReference spatialReference;//记录本剖面的空间参考。考虑到地层剖面与原地层的文件空间参考不同，考虑提供一个修改本参数的函数
        //另外，这个对象可以exportToWKT输出wkt记录的空间参考，也可以importFromWKT输入空间参考
        public List<OriSingleStratumPolygon> stratums;
        public StratumData(string shpFilePath , double startX, double startY, double startZ, double endX, double endY, double firstX, double firstY,string fieldName) {
            this.shpFilePath = shpFilePath;
            this.startX = startX;
            this.startY = startY;
            this.startZ = startZ;
            this.endX = endX;
            this.endY = endY;
            this.firstX = firstX;
            this.firstY = firstY;
            this.idFieldName = fieldName;
            Gdal.AllRegister();
            Ogr.RegisterAll();
            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");
            this.stratums = new List<OriSingleStratumPolygon>();
            createStratums();
        }
        private void createStratums() {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource data = driver.Open(this.shpFilePath,1);
            Layer layer = data.GetLayerByIndex(0);
            this.spatialReference = layer.GetSpatialRef();
            int featurecount = (int) layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++) {//遍历这个图层所有的元素，每个元素都给他创建一个stratum
                Feature feature = layer.GetFeature(i);
               int LithIdindex = feature.GetFieldIndex(idFieldName);
                int Fidindex = feature.GetFieldIndex("FID");
                wkbGeometryType geometryType = feature.GetGeometryRef().GetGeometryType();
                OriSingleStratumPolygon oriSingleStratum = new OriSingleStratumPolygon(feature, LithIdindex, Fidindex, geometryType);
                string wkt;
                List<List<double[]>> XYZList;
                oriSingleStratum.getRealCoordWKT(out wkt, out XYZList, startX, startY, startZ, endX, endY, firstX, firstY);
                this.stratums.Add(oriSingleStratum);
            }
         

        }
        public void saveStratumsToSHP(string shppath,string layername) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource data = driver.CreateDataSource(shppath,null);
            Layer layer = data.CreateLayer(layername,this.spatialReference,wkbGeometryType.wkbPolygon25D,null);
            int count = this.stratums.Count();
            FieldDefn fieldid = new FieldDefn("id", FieldType.OFTInteger);
            FieldDefn fieldLithId = new FieldDefn(idFieldName,FieldType.OFTInteger);
            layer.CreateField(fieldid, 1);
            layer.CreateField(fieldLithId, 1);
            
            for (int i = 0; i < count; i++) {
                Feature feature = new Feature(layer.GetLayerDefn());
                OriSingleStratumPolygon stratumPolygon = stratums[i];
                feature.SetField(idFieldName, stratumPolygon.LithId);
                feature.SetField("id", stratumPolygon.FID);
                Geometry geometry =Geometry.CreateFromWkt(stratumPolygon.wkt);
                //Geometry geomRing = new Geometry(wkbGeometryType.wkbLinearRing);
                feature.SetGeometry(geometry);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            data.Dispose();
        }
        public void importSpatialRefByShpFile(string shppath) {//通过一个图层，修改本对象的空间参考
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource data = driver.Open(shppath, 1);
            Layer layer = data.GetLayerByIndex(0);
            this.spatialReference = layer.GetSpatialRef();
        }
    }
}
