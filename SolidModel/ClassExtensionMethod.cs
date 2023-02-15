using GeoCommon;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public static class ClassExtensionMethod
    {
        /// <summary>
        /// 将边界点输出到shapefile
        /// </summary>
        /// <param name="PNewEage"></param>
        /// <param name="pSavePath"></param>
        /// <param name="pName"></param>
        public static void ExportTrianglePointToShapefile(this List<TriangleNet.Geometry.Vertex> PNewEage, string pSavePath, string pName)
        {
            List<Geometry> ListPT = new List<Geometry>();
            for (int i = 0; i < PNewEage.Count; i++)
            {
                Geometry pt = new Geometry(wkbGeometryType.wkbPoint);
                pt.AddPoint_2D(PNewEage[i].X, PNewEage[i].Y);
                ListPT.Add(pt);
            }

            ListPT.ExportGeometryToShapfile(pSavePath, pName);
        }



        /// <summary>
        /// 将边界点输出到shapefile
        /// </summary>
        /// <param name="PNewEage"></param>
        /// <param name="pSavePath"></param>
        /// <param name="pName"></param>
        public static void ExportEagePointToShapefile(this Eage PNewEage,string pSavePath,string pName)
        {
            List<Geometry> ListPT = new List<Geometry>();
            for (int i = 0; i < PNewEage.vertexList.Count; i++)
            {
                Geometry pt = new Geometry(wkbGeometryType.wkbPoint);
                pt.AddPoint_2D(PNewEage.vertexList[i].x, PNewEage.vertexList[i].y);
                ListPT.Add(pt);
            }

            ListPT.ExportGeometryToShapfile(pSavePath, pName);
        }

        /// <summary>
        /// 将边界点输出到shapefile
        /// </summary>
        /// <param name="PNewEage"></param>
        /// <param name="pSavePath"></param>
        /// <param name="pName"></param>
        public static void ExportEagePointToShapefile3D(this Eage PNewEage, string pSavePath, string pName)
        {
            List<Geometry> ListPT = new List<Geometry>();
            Geometry pLine =new Geometry(wkbGeometryType.wkbLineString);
            for (int i = 0; i < PNewEage.vertexList.Count; i++)
            {

                pLine.AddPoint(PNewEage.vertexList[i].x, PNewEage.vertexList[i].y, PNewEage.vertexList[i].z);
                
            }
            pLine.AddPoint(PNewEage.vertexList[0].x, PNewEage.vertexList[0].y, PNewEage.vertexList[0].z);
            ListPT.Add(pLine);
            ListPT.ExportGeometryToShapfile(pSavePath, pName);
        }

        /// <summary>
        /// 将边界点输出到面要素
        /// </summary>
        /// <param name="PNewEage"></param>
        /// <param name="pSavePath"></param>
        /// <param name="pName"></param>
        public static void ExportEagePointToPolygon(this Eage PNewEage, string pSavePath, string pName)
        {
            List<Geometry> ListPT = new List<Geometry>();
            Geometry pRing = new Geometry(wkbGeometryType.wkbLinearRing);
            for (int i = 0; i <= PNewEage.vertexList.Count; i++)
            {
                if (i == PNewEage.vertexList.Count)
                {
                    pRing.AddPoint_2D(PNewEage.vertexList[0].x, PNewEage.vertexList[0].y);
                    continue;
                }
                pRing.AddPoint_2D(PNewEage.vertexList[i].x, PNewEage.vertexList[i].y);
                
            }

            Geometry pPolygon = new Geometry(wkbGeometryType.wkbPolygon);
            pPolygon.AddGeometry(pRing);

            List<Geometry> polygons = new List<Geometry>();
            polygons.Add(pPolygon);
            polygons.ExportGeometryToShapfile(pSavePath, pName);

        }
        //将边界作为线输出
        public static void ExportEagePointToLine2D(this Eage PNewEage, string pSavePath, string pName) {
            Geometry polyline = new Geometry(wkbGeometryType.wkbLineString);
            List<Vertex> vertices = PNewEage.vertexList;
            int count = vertices.Count();
            for (int i = 0; i < count; i++) {
                polyline.AddPoint_2D(vertices[i].x,vertices[i].y);
            }
            List<Geometry> lines = new List<Geometry>();
            lines.Add(polyline);
            lines.ExportGeometryToShapfile(pSavePath, pName);


        }

        /// <summary>
        /// 将triMesh输出到要素类
        /// </summary>
        /// <param name="_triMesh"></param>
        /// <param name="_workSpacePath"></param>
        /// <param name="_fileName"></param>
        public static void ExportGeometryToShapfile(this List<Geometry> geometryCollection, string _workSpacePath, string _fileName)
        {
            //注册Ogr库
            string pszDriverName = "ESRI Shapefile";
            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                throw new Exception("Driver Error");

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "NO");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");


            //1、创建数据源
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(_workSpacePath + "\\" + _fileName + ".shp", null);//如果原始文件夹内有该要素数据，会覆盖。
            if (poDS == null)
                throw new Exception("DataSource Creation Error");

            //3、创建层Layer
            Layer poLayer = poDS.CreateLayer(_fileName, null, geometryCollection[0].GetGeometryType(), null);
            if (poLayer == null)
                throw new Exception("Layer Creation Failed");

            FieldDefn oFieldID = new FieldDefn("FieldID", FieldType.OFTInteger);
            poLayer.CreateField(oFieldID, 1);

            //创建一个Feature,一个Polygon
            Feature poFeature = new Feature(poLayer.GetLayerDefn());

            for (int i = 0; i < geometryCollection.Count; i++)
            {

                poFeature.SetField(0, i);

                poFeature.SetGeometry(geometryCollection[i]);

                poLayer.CreateFeature(poFeature);

            }
            poDS.Dispose();
            poLayer.Dispose();
            poFeature.Dispose();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="geometryCollection"></param>
        /// <param name="_workSpacePath"></param>
        /// <param name="_fileName"></param>
        /// <param name="fieldName">字段</param>
        public static void ExportGeometryToShapfileByfield(this List<Geometry> geometryCollection, string _workSpacePath, string _fileName,string fieldName)
        {
            //注册Ogr库
            string pszDriverName = "ESRI Shapefile";
            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                throw new Exception("Driver Error");

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "NO");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");


            //1、创建数据源
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(_workSpacePath + "\\" + _fileName + ".shp", null);//如果原始文件夹内有该要素数据，会覆盖。
            if (poDS == null)
                throw new Exception("DataSource Creation Error");

            //3、创建层Layer
            Layer poLayer = poDS.CreateLayer(_fileName, null, geometryCollection[0].GetGeometryType(), null);
            if (poLayer == null)
                throw new Exception("Layer Creation Failed");

            FieldDefn oFieldID = new FieldDefn("FieldID", FieldType.OFTInteger);
            poLayer.CreateField(oFieldID, 1);

            FieldDefn oFieldID2 = new FieldDefn(fieldName, FieldType.OFTString);
            poLayer.CreateField(oFieldID2, 1);

            FieldDefn oFieldID3 = new FieldDefn("area", FieldType.OFTReal);
            poLayer.CreateField(oFieldID3, 1);

            //创建一个Feature,一个Polygon
            Feature poFeature = new Feature(poLayer.GetLayerDefn());

            for (int i = 0; i < geometryCollection.Count; i++)
            {

                poFeature.SetField(0, i);

                poFeature.SetField(1, _fileName+1);
                poFeature.SetField(2, 0.0);

                poFeature.SetGeometry(geometryCollection[i]);

                poLayer.CreateFeature(poFeature);

            }
            poDS.Dispose();
            poLayer.Dispose();
            poFeature.Dispose();
        }




        /// <summary>
        /// 将几何输出到要素类
        /// </summary>
        /// <param name="_triMesh"></param>
        /// <param name="_workSpacePath"></param>
        /// <param name="_fileName"></param>
        public static void ExportSimpleGeometryToShapfile(this Geometry geometryCollection, string _workSpacePath, string _fileName)
        {
            //注册Ogr库
            string pszDriverName = "ESRI Shapefile";
            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                throw new Exception("Driver Error");

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "NO");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");


            //1、创建数据源
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(_workSpacePath + "\\" + _fileName + ".shp", null);//如果原始文件夹内有该要素数据，会覆盖。
            if (poDS == null)
                throw new Exception("DataSource Creation Error");

            //3、创建层Layer
            Layer poLayer = poDS.CreateLayer(_fileName, null, geometryCollection.GetGeometryType(), null);
            if (poLayer == null)
                throw new Exception("Layer Creation Failed");



            FieldDefn oFieldID = new FieldDefn("FieldID", FieldType.OFTInteger);
            poLayer.CreateField(oFieldID, 1);

            //创建一个Feature,一个Polygon
            Feature poFeature = new Feature(poLayer.GetLayerDefn());


            poFeature.SetField(0, 0);

            poFeature.SetGeometry(geometryCollection);

            poLayer.CreateFeature(poFeature);

            
            poDS.Dispose();
            poLayer.Dispose();
            poFeature.Dispose();
        }

        /// <summary>
        /// 将triMesh输出到要素类
        /// </summary>
        /// <param name="_triMesh"></param>
        /// <param name="_workSpacePath"></param>
        /// <param name="_fileName"></param>
        public static void ExportTriMeshToShapfile(this TriMesh _triMesh, string _workSpacePath, string _fileName)
        {
            //注册Ogr库
            string pszDriverName = "ESRI Shapefile";
            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                throw new Exception("Driver Error");

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "NO");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");


            //1、创建数据源
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(_workSpacePath + "\\" + _fileName + ".shp", null);//如果原始文件夹内有该要素数据，会覆盖。
            if (poDS == null)
                throw new Exception("DataSource Creation Error");

            //3、创建层Layer
            Layer poLayer = poDS.CreateLayer(_fileName, null, wkbGeometryType.wkbPolygon, null);
            if (poLayer == null)
                throw new Exception("Layer Creation Failed");

            FieldDefn oFieldID = new FieldDefn("FieldID", FieldType.OFTInteger);
            poLayer.CreateField(oFieldID, 1);

            //创建一个Feature,一个Polygon
            Feature poFeature = new Feature(poLayer.GetLayerDefn());

            for (int i = 0; i < _triMesh.triangleList.Count; i++)
            {
                Triangle tri = _triMesh.triangleList[i];
                Vertex vTri0 = _triMesh.vertexList[tri.v0];
                Vertex vTri1 = _triMesh.vertexList[tri.v1];
                Vertex vTri2 = _triMesh.vertexList[tri.v2];


                string polygonTri = "POLYGON((" + vTri0.x + " " + vTri0.y + "," + vTri1.x + " " + vTri1.y + "," + vTri2.x + " " + vTri2.y + "," + vTri0.x + " " + vTri0.y + "))";
                poFeature.SetField(0, i);
                Geometry polyTri = Geometry.CreateFromWkt(polygonTri);
                poFeature.SetGeometry(polyTri);

                poLayer.CreateFeature(poFeature);

            }
            poDS.Dispose();
            poLayer.Dispose();
            poFeature.Dispose();
        }

       


    }
}
