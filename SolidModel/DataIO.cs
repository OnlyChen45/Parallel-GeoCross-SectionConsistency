using GeoCommon;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class DataIO
    {
        /// <summary>
        /// 获取横截面上的轮廓线
        /// </summary>
        /// <param name="pLayFile"></param>
        /// <returns></returns>
        public List<Contour> GetContour(string pLayFile)
        {
            List<Contour> pContours = new List<Contour>();

            Layer pContourLayer = LayerHelp.GetLayerByLayerName(System.IO.Path.GetDirectoryName(pLayFile), System.IO.Path.GetFileNameWithoutExtension(pLayFile));
            int PFeatureCount = (int)pContourLayer.GetFeatureCount(0);
            for (int i = 0; i < PFeatureCount; i++)
            {
                //string name = pContourLayer.GetFeature(i).GetFieldAsString("NAME");
                Geometry pg = pContourLayer.GetFeature(i).GetGeometryRef();
                string name = pLayFile + i.ToString();
                Contour pContour = new Contour(name);
                //将几何要素转换为轮廓线
                GeometryToContour(pg,ref pContour);

                pContours.Add(pContour);
            }


            return pContours;
        }
        public List<Contour> GetContour(string pLayFile,out List<int> idList,string fieldName)//重载一下，把每个多边形对象的eage也都拿到
        {
            List<Contour> pContours = new List<Contour>();
            idList = new List<int>();
            Layer pContourLayer = LayerHelp.GetLayerByLayerName(System.IO.Path.GetDirectoryName(pLayFile), System.IO.Path.GetFileNameWithoutExtension(pLayFile));
            int PFeatureCount = (int)pContourLayer.GetFeatureCount(0);
            for (int i = 0; i < PFeatureCount; i++)
            {
                //string name = pContourLayer.GetFeature(i).GetFieldAsString("NAME");
                Geometry pg = pContourLayer.GetFeature(i).GetGeometryRef();
                idList.Add(pContourLayer.GetFeature(i).GetFieldAsInteger(fieldName));
                string name = pLayFile + i.ToString();
                Contour pContour = new Contour(name);
                //将几何要素转换为轮廓线
                GeometryToContour(pg, ref pContour);

                pContours.Add(pContour);
            }

            return pContours;
        }
        //把从geom到contour做一个接口，
        public Contour IGeometryToContour(Geometry pGeo,string name) {
            Contour contour = new Contour(name);
            GeometryToContour(pGeo, ref contour);
            return contour;

        }

        /// <summary>
        /// 将几何要素转换为轮廓线
        /// </summary>
        /// <param name="pGeo"></param>
        /// <param name="pName"></param>
        /// <returns></returns>
        private void GeometryToContour(Geometry pGeo, ref Contour pContour)
        {
            wkbGeometryType PT = pGeo.GetGeometryType();

            if (PT == wkbGeometryType.wkbLineString || PT == wkbGeometryType.wkbLineString25D)
            {          
                    Eage pEage = new Eage();
                    pEage.name = pContour.name + 0;
                    int count = pGeo.GetPointCount();
                    for (int j = 0; j < count; j++)
                    {
                        Vertex vt = new Vertex();
                        vt.name = pEage.name + pEage.vertexList.Count;
                        vt.x = pGeo.GetX(j);
                        vt.y = pGeo.GetY(j);
                        vt.z = pGeo.GetZ(j);

                        //if (pContour.name.Contains("B"))
                        //    vt.z = pGeoEage.GetZ(j)-100;
                        //else
                        //    vt.z = pGeoEage.GetZ(j)-300;

                        pEage.AddVertex(vt);
                    }
                    pContour.AddEage(pEage);
                }

            if (PT== wkbGeometryType.wkbPolygon||PT==wkbGeometryType.wkbPolygon25D)
            {
                //边的数量
                int eageCount = pGeo.GetGeometryCount();


                for (int i = 0; i < eageCount; i++)
                {
                    Eage pEage = new Eage();
                    Geometry pGeoEage = pGeo.GetGeometryRef(i);
                    pEage.name = pContour.name + i;

                    int count = pGeoEage.GetPointCount();

                    for (int j = 0; j < pGeoEage.GetPointCount(); j++)
                    {

                        Vertex vt = new Vertex();
                        vt.name = pEage.name + pEage.vertexList.Count;
                        vt.x = pGeoEage.GetX(j);
                        vt.y = pGeoEage.GetY(j);
                        vt.z = pGeoEage.GetZ(j);
                       // Console.WriteLine(pGeoEage.GetZ(j).ToString());
                        //if (pContour.name.Contains("B"))
                        //    vt.z = pGeoEage.GetZ(j)-100;
                        //else
                        //    vt.z = pGeoEage.GetZ(j)-300;

                        pEage.AddVertex(vt);
                    }
                    pContour.AddEage(pEage);
                }
            }
        }
    }
}
