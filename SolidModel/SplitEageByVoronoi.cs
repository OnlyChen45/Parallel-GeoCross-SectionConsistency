using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using OSGeo.OGR;

namespace SolidModel
{
    class SplitEageByVoronoi//这个是用来采用Voronoi分割eages的，应该是输入一个list<eage>,一个eage，然后返回一个List<Eage[2]>记录对应的
    {
        private bool MyContain(Geometry geom1, Geometry geom2)
        {//测试geom1是否包含geom2，主要是因为Geometry.Contain在边界重叠是返回值为false，不能采用
            double area2 = geom2.Area();
            Geometry geominter = geom2.Intersection(geom1);
            double areaI = geominter.Area();
            if ((areaI / area2) > 0.9999)
            {
                return true;
            }
            else { return false; }
        }
        public List<Eage[]> SplitEageByVor(List<Eage > eages,Eage bigEage) {
            List<Eage[]> result = new List<Eage[]>();
            //eages直接就能用
            //bigEage需要被转成Polygon

            Geometry pPolygon = new Geometry(wkbGeometryType.wkbPolygon);
            Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
            double z = bigEage.vertexList[0].z;
            Console.WriteLine(z);
            bool zconsistency = true;
            foreach (var vt in bigEage .vertexList)
            {
              linev.AddPoint_2D(vt.x, vt.y);
                if (z != vt.z) zconsistency = false;
                z = vt.z;
            }
            linev.AddPoint_2D(bigEage.vertexList[0].x, bigEage.vertexList[0].y);
            pPolygon.AddGeometry(linev);
            VoronoiHelp voronoiHelper = new VoronoiHelp();
            List<Geometry> resultgeoms;
            for (int k = 0; k < eages.Count(); k++) {
                eages[k].ExportEagePointToShapefile3D(@"D:\graduateGIS\water3D\temp", k.ToString());

            }
            voronoiHelper.CreateVoronoi(eages, pPolygon,out resultgeoms);

            //以下需要将resultgeoms给处理成能和eages对应的Eages
            List<Eage> spliteages = new List<Eage>();

            /*DataIO dataIO = new DataIO();
            for(int i=0;i< resultgeoms.Count ();i++) {
                Geometry geometry = resultgeoms[i];
                Contour contour = dataIO.IGeometryToContour(geometry, bigEage.name + i.ToString());
                spliteages.AddRange(contour.eageList);
            }*/
            //ContourMapping contourMapping = new ContourMapping();
            //contourMapping.runMapping(eages, spliteages);
            //contourMapping.oneToOneEageWork(out result);
            /*Driver driver = Ogr.GetDriverByName("ESRI Shapefile");

            //

            DataSource ds = driver.Open(@"D:\graduateGIS\water3D\temp\t1.shp",  1);
            Layer layer = ds.GetLayerByIndex(0);
            Feature feature = new Feature(layer.GetLayerDefn());
            for (int k = 0; k < resultgeoms.Count(); k++) {
               
                feature.SetGeometry(resultgeoms[k]);
                layer.CreateFeature(feature);

            }
            feature.Dispose();
            layer.Dispose();
            ds.Dispose();*/

            //
            for (int i = 0; i < eages.Count(); i++) {
                Eage eage = eages[i];
                Geometry geom1 = new Geometry(wkbGeometryType.wkbLinearRing);
                Geometry geom2 = new Geometry(wkbGeometryType.wkbPolygon);
                double z1 = eage.vertexList[0].z;
                for (int j = 0; j < eage.vertexList.Count(); j++)
                    geom1.AddPoint_2D(eage.vertexList[j].x, eage.vertexList[j].y);
                geom1.AddPoint_2D(eage.vertexList[0].x, eage.vertexList[0].y);
                geom2.AddGeometry(geom1);
                for (int j = 0; j < resultgeoms.Count(); j++) {
                    string wkt;
                    resultgeoms[j].ExportToWkt(out wkt);
                    Console.WriteLine(wkt);
                    if (MyContain(resultgeoms[j], geom2)) {
                        Eage[] eagepair = new Eage[2];
                        eagepair[0] = eages[i];
                        Eage seage = new Eage();
                        seage.name = bigEage.name + j.ToString();
                        int temp = resultgeoms[j].GetPointCount();
                        WKTencoder2D wKTencoder2D = new WKTencoder2D(wkt);
                        double[] points = wKTencoder2D.getpolygon();
                        for (int k = 0; k < points.Length/2; k++) {
                            Vertex vt = new Vertex();
                            vt.name = seage.name + k.ToString();
                            vt.x = points[k*2];
                            vt.y = points[k*2+1];
                            vt.z = z;
                            Console.WriteLine(vt.x.ToString() + " " + vt.y.ToString() + " " + vt.z.ToString());
                            seage.AddVertex(vt);
                        }
                        eagepair[1] = seage;
                        result.Add(eagepair);
                    }
                }

            }

             return result;
            #region 创建Voronoi图部分,使用样例
            //Contour dConB1 = contourListB["B0"];
            //pConB.Add(dConB1);
            //Contour dConB2 = contourListB["B1"];
            //pConB.Add(dConB2);
            //Contour dConB3 = contourListB["B2"];
            //pConB.Add(dConB3);

            //VoronoiHelp voronoiHelper = new VoronoiHelp();
            //List<Eage> pEages = new List<Eage>();
            //foreach (var por in pConB)
            //{
            //    pEages.Add(por.eageList[0]);
            //    por.eageList[0].ExportEagePointToShapefile3D(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\zhuanli", por.eageList[0].name);
            //}

            ////RockC要素

            //pConA[0].eageList[0].ExportEagePointToShapefile3D(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\zhuanli", pConA[0].eageList[0].name);

            //Geometry pPolygon = new Geometry(wkbGeometryType.wkbPolygon);
            //Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
            //foreach (var vt in pConA[0].eageList[0].vertexList)
            //{

            //    linev.AddPoint_2D(vt.x, vt.y);
            //}
            //linev.AddPoint_2D(pConA[0].eageList[0].vertexList[0].x, pConA[0].eageList[0].vertexList[0].y);
            //pPolygon.AddGeometry(linev);
            //List<Geometry> gs = new List<Geometry>();
            //gs.Add(pPolygon);
            //gs.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\ProgramData", "rockC2");

            ////执行函数
            //voronoiHelper.CreateVoronoi(pEages, pPolygon);
            //double s = 0;

            #endregion
        }
    }
}
