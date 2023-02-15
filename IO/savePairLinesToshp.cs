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
    /// 数据输出函数
    /// </summary>
    public class savePairLinesToshp
    {
        static public string  saveSectionLines(double[] startendxy1,double[] startendxy2,string workspace,string spatialpath) {
            //根据两组首尾点数据，输出一个有用的两个采样线的二维线的，便于后续获取采样线
            //这个里边，一定要有start_x,start_y,end_x,end_y,lineID
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            string outputname =getnewFileName(workspace, "sectionlines", "shp");
            DataSource dataSource = driver.CreateDataSource(outputname, null);
            DataSource spds = driver.Open(spatialpath, 1);
            Layer splayer = spds.GetLayerByIndex(0);
            SpatialReference spatialReference = splayer.GetSpatialRef();
            splayer.Dispose();
            spds.Dispose();
            Layer lineslayer = dataSource.CreateLayer("lines", spatialReference, wkbGeometryType.wkbLineString, null);
            FieldDefn field1 = new FieldDefn("start_x", FieldType.OFTReal);
            FieldDefn field2 = new FieldDefn("start_y", FieldType.OFTReal);
            FieldDefn field3 = new FieldDefn("end_x", FieldType.OFTReal);
            FieldDefn field4 = new FieldDefn("end_y", FieldType.OFTReal);
            FieldDefn field5 = new FieldDefn("lineID", FieldType.OFTInteger);
            lineslayer.CreateField(field1,1);
            lineslayer.CreateField(field2, 1);
            lineslayer.CreateField(field3, 1);
            lineslayer.CreateField(field4, 1);
            lineslayer.CreateField(field5, 1);
            Geometry line1 = new Geometry(wkbGeometryType.wkbLineString);
            Geometry line2 = new Geometry(wkbGeometryType.wkbLineString);
            line1.AddPoint_2D(startendxy1[0], startendxy1[1]);
            line1.AddPoint_2D(startendxy1[2], startendxy1[3]);
            line2.AddPoint_2D(startendxy2[0], startendxy2[1]);
            line2.AddPoint_2D(startendxy2[2], startendxy2[3]);
            Feature feature1 = new Feature(lineslayer.GetLayerDefn());
            Feature feature2 = new Feature(lineslayer.GetLayerDefn());
            feature1.SetGeometry(line1);
            feature1.SetField("start_x", startendxy1[0]);
            feature1.SetField("start_y", startendxy1[1]);
            feature1.SetField("end_x", startendxy1[2]);
            feature1.SetField("end_y", startendxy1[3]);
            feature1.SetField("lineID", 1);
            feature2.SetGeometry(line2);
            feature2.SetField("start_x", startendxy2[0]);
            feature2.SetField("start_y", startendxy2[1]);
            feature2.SetField("end_x", startendxy2[2]);
            feature2.SetField("end_y", startendxy2[3]);
            feature2.SetField("lineID", 2);
            lineslayer.CreateFeature(feature1);
            lineslayer.CreateFeature(feature2);
            lineslayer.Dispose();
            dataSource.Dispose();
            return outputname;
        }
        static private string getNowTimeNumString()
        {
            string st = DateTime.Now.ToString("yyyyMMdd_hhmmss");
            return st;
        }
        static private string getnewFileName(string workfolder, string filename, string ex)
        {//给定一个文件夹，一个文件名，一个后缀名，生成特定路径下按照时间标记的文件绝对路径 
            string datest = getNowTimeNumString();
            string fullfilename = filename + datest + '.' + ex;
            //string[] files = Directory.GetFiles(workfolder);
            string fullpath = workfolder + '\\' + fullfilename;
            return fullpath;

        }
    }
}
