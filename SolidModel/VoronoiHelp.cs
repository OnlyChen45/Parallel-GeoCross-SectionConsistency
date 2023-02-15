using GeoCommon;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class VoronoiHelp
    {



        public void CreateVoronoi(List<Eage> pEages,Geometry pOriGeometry ,out List<Geometry> resultgeoms )//resultgeoms是返回的被切分好的geom，都是polygon
        {//整个方法的具体过程就是，靠先构建一个voronoi多边形图，然后根据每个eage把这些多边形合并，最后用这些合并过后的多边形去切pOriGeomtry，生成想要的分割后的大多边形。
            //1.1 获取边界上所有点
            List<TriangleNet.Geometry.Vertex> pVertexList = GetListVetexs(pEages);
            //pVertexList.ExportTrianglePointToShapefile(@"D:\graduateGIS\water3D\temp", "pointBO");

            //1.2 构建三角网
            TriangleNet.Mesh mesh = GetTriMesh(pVertexList);

            #region 暂时不用 获取三角网
            TriMesh trimesh = new TriMesh();
            foreach (var item in mesh.Triangles)
            {
                TriangleNet.Geometry.Vertex p1, p2, p3;

                TriangleNet.Topology.Triangle pTinTri = item;

                p1 = item.GetVertex(0);
                p2 = item.GetVertex(1);
                p3 = item.GetVertex(2);

                //将三角形添加到自定义的三角网中
                trimesh.AddTriangle(p1.X, p1.Y, 0, p2.X, p2.Y, 0, p3.X, p3.Y, 0);

            }
            trimesh.ExportTriMeshToShapfile(@"D:\graduateGIS\water3D\temp", "delaunaryBO");
            #endregion

            //1.3 创建Voronoi
            TriangleNet.Voronoi.Legacy.SimpleVoronoi sv = new TriangleNet.Voronoi.Legacy.SimpleVoronoi(mesh);

            #region 暂时不用
            TriangleNet.Geometry.Point[] voronoiVetex = sv.Points;

            List<Geometry> geos = new List<Geometry>();
            for (int i = 0; i < voronoiVetex.Length; i++)
            {
                Geometry pt = new Geometry(wkbGeometryType.wkbPoint);
                pt.AddPoint_2D(voronoiVetex[i].X, voronoiVetex[i].Y);
                geos.Add(pt);
            }
            //geos.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\zhuanli", "voronoiPoint");

            List<TriangleNet.Geometry.IEdge> eages = sv.Edges as List<TriangleNet.Geometry.IEdge>;
            List<Geometry> linelist = new List<Geometry>();
            foreach (var eage in eages)
            {
                Geometry linev = new Geometry(wkbGeometryType.wkbLineString);
                linev.AddPoint_2D(voronoiVetex[eage.P0].X, voronoiVetex[eage.P0].Y);
                linev.AddPoint_2D(voronoiVetex[eage.P1].X, voronoiVetex[eage.P1].Y);
                linelist.Add(linev);

            }

            //linelist.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\zhuanli", "voronoiline"); 
            #endregion

            //1.4 获取voronoi多边形
            ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion = sv.Regions;
            List<TriangleNet.Geometry.Vertex> pVertexs = new List<TriangleNet.Geometry.Vertex>();
            foreach (var mV in mesh.Vertices)
            {
                pVertexs.Add(mV);
            }
            //1.5 归类Voronoi
            List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>> pClassficationVoronoi = ClassificationVoronoiRegion(pEages, pRegion, pVertexs);
            
            //1.6 将每个Voronoi多边形合并
            Dictionary<string, Geometry> pDictVoronoi = new Dictionary<string, Geometry>();
            for (int i = 0; i < pClassficationVoronoi.Count; i++)
            {
                List<TriangleNet.Voronoi.Legacy.VoronoiRegion> pvor = pClassficationVoronoi[i];

                List<Geometry> polygonlist = new List<Geometry>();
                for (int j = 0; j < pvor.Count; j++)
                {
                    Geometry linev = new Geometry(wkbGeometryType.wkbLinearRing);
                    List<TriangleNet.Geometry.Point> pvs = pvor[j].Vertices as List<TriangleNet.Geometry.Point>;
                    for (int k = 0; k < pvs.Count; k++)
                    {
                        linev.AddPoint_2D(pvs[k].X, pvs[k].Y);
                    }
                    linev.AddPoint_2D(pvs[0].X, pvs[0].Y);
                    Geometry polygonv = new Geometry(wkbGeometryType.wkbPolygon);
                    polygonv.AddGeometry(linev);
                    polygonlist.Add(polygonv);
                }

                polygonlist.ExportGeometryToShapfile(@"D:\graduateGIS\water3D\temp", "vorvint"+i);

                //合并三角面
                Geometry gp = new Geometry(wkbGeometryType.wkbPolygon);
                for (int j = 0; j < polygonlist.Count; j++)
                {
                    double pArea = polygonlist[j].GetArea();
                    gp = gp.Union(polygonlist[j]);
                }

                List<Geometry> gs = new List<Geometry>();
                gs.Add(gp);
               gs.ExportGeometryToShapfile(@"D:\graduateGIS\water3D\temp", "combinge" + i);

                pDictVoronoi.Add(pEages[i].name, gp);
            }

            //1.7 将剖面分隔成不同的几何体
           // Dictionary<string, Geometry> pSection = new Dictionary<string, Geometry>();
        resultgeoms = new List<Geometry>();


             Driver driver = Ogr.GetDriverByName("ESRI Shapefile");


         DataSource ds = driver.Open(@"D:\graduateGIS\water3D\temp\biggeom.shp", 1);
         Layer layer = ds.GetLayerByIndex(0);
         Feature feature = new Feature(layer.GetLayerDefn());

        // feature.SetGeometry(pOriGeometry);
        // layer.CreateFeature(feature);


         feature.Dispose();
         layer.Dispose();
         ds.Dispose();
            foreach (var vr in pDictVoronoi.Keys)
            {
                Geometry pBigGeo = pDictVoronoi[vr];
                Geometry pIntersectGeo = pOriGeometry.Intersection(pBigGeo);
               
                // pSection.Add("C" + vr.Substring(1, 1), pIntersectGeo);
                //从这里提供一个返回值接口
                //pIntersectGeo.ExportSimpleGeometryToShapfile(@"D:\graduateGIS\water3D\temp", "C" + vr.Substring(1,1));       
                resultgeoms.Add(pIntersectGeo);
               ds = driver.Open(@"D:\graduateGIS\water3D\temp\biggeom.shp", 1);
               layer = ds.GetLayerByIndex(0);
                 feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(pBigGeo);
                layer.CreateFeature(feature);
            }

            //1.8 将z值修改
            /* Dictionary<string,List<List<Vertex>>> pVertexSave = new Dictionary<string,List<List<Vertex>>>();

             foreach(var vr in pSection.Keys)
             {
                 pVertexSave.Add(vr,new List<List<Vertex>>());
             }

             List<string> pKeys = new List<string>();
             foreach (var vr in pSection.Keys)
             {
                 pKeys.Add(vr);
             }

             for (int s = 0; s < pKeys.Count; s++)
             {
                 for (int u = s + 1; u < pKeys.Count; u++)
                 {
                     if (pKeys[s] == pKeys[u])
                         continue;
                     if (pSection[pKeys[s]].Intersect(pSection[pKeys[u]]))
                     {
                         Geometry pGeoLine = pSection[pKeys[s]].Intersection(pSection[pKeys[u]]);

                         wkbGeometryType ps = pGeoLine.GetGeometryType();
                         int count = pGeoLine.GetGeometryCount();
                         List<Vertex> pVerList = new List<Vertex>();
                         for (int k = 0; k < count; k++)
                         {

                             for (int i = 0; i < pGeoLine.GetGeometryRef(k).GetPointCount(); i++)
                             {
                                 Vertex vs = new Vertex();
                                 vs.x = pGeoLine.GetGeometryRef(k).GetX(i);
                                 vs.y = pGeoLine.GetGeometryRef(k).GetY(i);
                                 vs.z = 0.0;


                                 pVerList.Add(vs);
                                 for (int j = 0; j < pVerList.Count - 1; j++)
                                 {
                                     if (Math.Round(pVerList[j].x, 3) == Math.Round(vs.x, 3) && Math.Round(pVerList[j].y, 3) == Math.Round(vs.y, 3))
                                         pVerList.Remove(vs);
                                 }


                             }
                         }
                         //改变坐标
                         ChangeVertexZ(ref pVerList, -100, 50);

                         //Geometry pGeoVr = GetNewSection(pSection[vr], pVerList, -200);
                         //Geometry pGeoVt = GetNewSection(pSection[vt], pVerList, -200);


                         pVertexSave[pKeys[s]].Add(pVerList);
                         pVertexSave[pKeys[u]].Add(pVerList);

                     }

                 }
             }


             //1.9 输出要素
             foreach (var vr in pSection.Keys)
             {
                 Geometry pGeoVr = GetNewSection(pSection[vr], pVertexSave[vr], -200);
                 pGeoVr.ExportSimpleGeometryToShapfile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\ProgramData", vr);
             }

             double ooooo = 0;*/

        }

        /// <summary>
        /// 修改轮廓面的信息
        /// </summary>
        /// <param name="pOriginalGeo"></param>
        /// <param name="pOriginalVertex"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Geometry GetNewSection(Geometry pOriginalGeo, List<List<Vertex>> pOriginalVertex, double height)
        {
            List<Vertex> pNewOriginalVertex = new List<Vertex>();

            foreach (var vts in pOriginalVertex)
            {
                foreach (var vt in vts)
                {
                    pNewOriginalVertex.Add(vt);
                }
            }

            int count = pOriginalGeo.GetGeometryCount();

            Geometry pLine = pOriginalGeo.GetGeometryRef(0);
            Geometry pRing = new Geometry(wkbGeometryType.wkbLinearRing);
            for (int i = 0; i < pLine.GetPointCount(); i++)
            {
                bool flag = true;
                for (int j = 0; j < pNewOriginalVertex.Count; j++)
                {
                    if (Math.Round(pLine.GetX(i), 3) == Math.Round(pNewOriginalVertex[j].x, 3) && Math.Round(pLine.GetY(i), 3) == Math.Round(pNewOriginalVertex[j].y, 3))
                    {
                        pRing.AddPoint(pLine.GetX(i), pLine.GetY(i), pNewOriginalVertex[j].z);
                        flag = false;
                    }
                }

                if (flag)
                {
                    pRing.AddPoint(pLine.GetX(i), pLine.GetY(i), height);
                }
            }

            Geometry pPolygon = new Geometry(wkbGeometryType.wkbPolygon);          
            pPolygon.AddGeometry(pRing);
            return pPolygon;

        }

        /// <summary>
        /// 改变点的Z值
        /// </summary>
        /// <param name="vertexList"></param>
        /// <param name="z"></param>
        public void ChangeVertexZ(ref List<Vertex> vertexList, double z,double height)
        {
            //获取总的距离
            double accumulate =0;
            for (int i = 1; i < vertexList.Count; i++)
            {
                double dist = CommonFun.GetDistance2D(vertexList[i], vertexList[i-1]);
                accumulate = accumulate + dist;
            }

            double accumulate_2 = 0.0;
            for (int i = 0; i < vertexList.Count; i++)
            {
                if (i == 0)
                {
                    vertexList[i].z = z;
                    continue;
                }

                double dist = CommonFun.GetDistance2D(vertexList[i], vertexList[i - 1]);
                accumulate_2 = accumulate_2 + dist;
                vertexList[i].z = z + Math.Sin((accumulate_2 / accumulate) * Math.PI) * height;
            }
        }

       

        /// <summary>
        /// 将Voronoi区域归类
        /// </summary>
        /// <param name="pEages"></param>
        /// <param name="pRegion"></param>
        /// <param name="pVertexList"></param>
        /// <returns></returns>
        public List<List< TriangleNet.Voronoi.Legacy.VoronoiRegion>> ClassificationVoronoiRegion(List<Eage> pEages, 
            ICollection<TriangleNet.Voronoi.Legacy.VoronoiRegion> pRegion
            ,List<TriangleNet.Geometry.Vertex> pVertexList)
        {
            List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>> pListVoronoi = new List<List<TriangleNet.Voronoi.Legacy.VoronoiRegion>>();
            foreach(var pEage in pEages)
            {
                List<TriangleNet.Voronoi.Legacy.VoronoiRegion> voronoiReg = new List<TriangleNet.Voronoi.Legacy.VoronoiRegion>();
                foreach(var vregion in pRegion)
                {
                    TriangleNet.Geometry.Vertex vt = pVertexList[vregion.ID];
                   // if(vt.NAME.Substring(0,3)==pEage.name)
                   if (vt.NAME.StartsWith(pEage.name))
                    {
                        voronoiReg.Add(vregion);
                    }
                }

                pListVoronoi.Add(voronoiReg);

            }

            return pListVoronoi;
        }

        /// <summary>
        /// 返回点的集合
        /// </summary>
        /// <param name="pEages"></param>
        /// <returns></returns>
        private List<TriangleNet.Geometry.Vertex> GetListVetexs(List<Eage> pEages)
        {
            List<TriangleNet.Geometry.Vertex> pVertexList = new List<TriangleNet.Geometry.Vertex>();
            foreach (var pEage in pEages)
            {
                foreach (var vt in pEage.vertexList)
                {
                    TriangleNet.Geometry.Vertex vr = new TriangleNet.Geometry.Vertex();
                    vr.X = vt.x;
                    vr.Y = vt.y;
                    vr.NAME = vt.name;
                    pVertexList.Add(vr);
                }
            }
            return pVertexList;
        }


        /// <summary>
        /// 三角剖分(1)
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        private TriangleNet.Mesh GetTriMesh(List<TriangleNet.Geometry.Vertex> pA)
        {
            #region 三角剖分模块
            //1. 
            //约束选项（约束类）
            var options = new TriangleNet.Meshing.ConstraintOptions();
            options.SegmentSplitting = 1;
            options.ConformingDelaunay = false;
            options.Convex = false;

            //质量选项（质量类）
            var quality = new TriangleNet.Meshing.QualityOptions();
            TriangleNet.Geometry.IPolygon input = GetPolygon(pA);
            //TriangleNet.Geometry.Contour con = GetContourByTriangle(pA);
            //input.Add(con, false);


            TriangleNet.Mesh mesh = null;
            if (input != null)
            {
                mesh = (TriangleNet.Mesh)TriangleNet.Geometry.ExtensionMethods.Triangulate(input, options);

            }

            return mesh;
            #endregion

        }

        /// <summary>
        /// 获取三角剖分的边界线
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public TriangleNet.Geometry.Contour GetContourByTriangle(List<TriangleNet.Geometry.Vertex> pA)
        {
            List<TriangleNet.Geometry.Vertex> pv = new List<TriangleNet.Geometry.Vertex>();

            foreach (var vt in pA)
            {
                pv.Add(vt);
            }  
            TriangleNet.Geometry.Contour pNewCon = new TriangleNet.Geometry.Contour(pv);
            return pNewCon;
        }

        /// <summary>
        /// 创建需要构建三角网的POlygon
        /// </summary>
        /// <param name="drillList"></param>
        /// <returns></returns>
        private TriangleNet.Geometry.IPolygon GetPolygon(List<TriangleNet.Geometry.Vertex> pA)
        {
            TriangleNet.Geometry.IPolygon data = new TriangleNet.Geometry.Polygon();

            foreach (var vt in pA)
            {
                TriangleNet.Geometry.Vertex triVertex = new TriangleNet.Geometry.Vertex(vt.X, vt.Y);
                triVertex.NAME = vt.NAME;
                ////vt.Label = 0;
                //vt.ID =data.Points.Count;
                data.Add(triVertex);

            }
            return data;
        }

    }
}
