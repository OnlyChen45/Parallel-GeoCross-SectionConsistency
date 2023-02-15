using GeoCommon;
using OSGeo.OGR;
using OSGeo.OSR;
using OSGeo.GDAL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class ContourIntePHelp
    {
        /// <summary>
        /// 第一条边
        /// </summary>
        public Eage pEageA;

        /// <summary>
        /// 第二条边
        /// </summary>
        public Eage pEageB;

        /// <summary>
        /// 两条边形成的三角网
        /// </summary>
        public TriangleNet.Mesh pMesh;

        /// <summary>
        /// 点的对应关系
        /// </summary>
        Dictionary<Vertex, List<Vertex>> pEee;

    
        public ContourIntePHelp()
        {

        }

        public ContourIntePHelp(Eage _pEageA,Eage _pEageB,TriangleNet.Mesh _pMesh)
        {
            this.pEageA = _pEageA;
            this.pEageB = _pEageB;
            this.pMesh = _pMesh;
        }
        #region 下边两个函数是用来获取一个按照比例得到插值的
        public Eage ContourInterPolateByRate(double rate) {

            //1获取所有点的信息
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(pEageA.vertexList, pEageB.vertexList);

            //2获取点的对应关系
            pEee = GetCorrespondPoint(pEageVertexDict);
            PointsSore(pEee);

            //3获取插值后的边
            Eage pIntePolateEage = GetIntepolateEage(pEee, rate, pEageA.name.Substring(0, 2) + pEageB.name.Substring(0, 2));

            return pIntePolateEage;

        }
        private Eage GetIntepolateEage(Dictionary<Vertex, List<Vertex>> _pEee, double rate, string name)
        {
                Eage pNewEage = new Eage();
                pNewEage.name = name + rate.ToString();
                int count = 0;
                foreach (var vt in _pEee.Keys)
                {

                    foreach (var vvt in _pEee[vt])
                    {

                        Vertex pVertex = new Vertex();
                        pVertex.name = pNewEage.name + count;
                        pVertex.x = vt.x + (vvt.x - vt.x) * rate;
                        pVertex.y = vt.y + (vvt.y - vt.y) * rate;
                        pVertex.z = vt.z + (vvt.z - vt.z) * rate;
                        pNewEage.AddVertex(pVertex);
                        count++;
                    }
                }
            return pNewEage;
        }
        #endregion
        /// <summary>
        /// 获取插值后的边
        /// </summary>
        /// <returns></returns>
        public List<Eage> ContourInterPolate()
        {

            //1获取所有点的信息
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(pEageA.vertexList, pEageB.vertexList);

            //2获取点的对应关系
            pEee = GetCorrespondPoint(pEageVertexDict);
            PointsSore(pEee);
            
            //3获取插值后的边
            List<Eage> pIntePolateEages = GetIntepolateEages(pEee, 6, pEageA.name.Substring(0, 2) + pEageB.name.Substring(0, 2));
            /*for (int i = 0; i < pIntePolateEages.Count; i++)
            {
                pIntePolateEages[i].ExportEagePointToShapefile3D(@"D:\graduateGIS\temp", "eage" + i);

                pIntePolateEages[i].ExportEagePointToPolygon(@"D:\graduateGIS\temp", "polygon" + i);

            }*/


            int nCount = 7;
            double[] x = { -300.0,-250.0, -200.0,-150.0,-100.0,-50.0,0.0};
            double[] y = { 393500.275,281872.218,171227.968,27892.8761, 12772.7232,5941.18937,1395.390081};
            double[] s = new double[5];


            #region 暂时不用
            //Vertex[] vts = new Vertex[7];
            //Vertex v1 = new Vertex();
            //v1.x = 0.0;
            //v1.y = 1395.390081;
            //vts[0] = v1;

            //Vertex v2 = new Vertex();
            //v2.x = -50.0;
            //v2.y = 5941.18937;
            //vts[1] = v2;

            //Vertex v3 = new Vertex();
            //v3.x = -100.0;
            //v3.y = 12772.7232;
            //vts[2] = v3;

            //Vertex v4 = new Vertex();
            //v4.x = -150.0;
            //v4.y = 27892.8761;
            //vts[3] = v4;

            //Vertex v5 = new Vertex();
            //v5.x = -200.0;
            //v5.y = 171227.968;
            //vts[4] = v5;

            //Vertex v6 = new Vertex();
            //v6.x = -250.0;
            //v6.y = 281872.218;
            //vts[5] = v6;

            //Vertex v7 = new Vertex();
            //v7.x = -300.0;
            //v7.y = 393500.275;
            //vts[6] = v7; 
            #endregion


            double[] areas = new double[pIntePolateEages.Count];
            for (int i = 0; i < pIntePolateEages.Count; i++)
            {
                
                double t = pIntePolateEages[i].vertexList[0].z;
                areas[i] = SplineMath.GetValueAkima(nCount, x, y, t, s, -1);
                
            }


            //SplineMath.DeSortX(vts);
            //double []areas= SplineMath.SplineInsertPoint(vts, xs,1);

            //4获取根据面积变化的边
            List<double> pAreas = new List<double>();
            for (int i = 0; i < areas.Length; i++)
            {
                pAreas.Add(areas[i]);
            }
            GetIntepolateEagesByArea(pIntePolateEages, pAreas);

            //5输出
            for (int i = 0; i < pIntePolateEages.Count; i++)
            {
                pIntePolateEages[i].ExportEagePointToShapefile3D(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\zhuanli\intepolate\after", "eage" + i);

                pIntePolateEages[i].ExportEagePointToPolygon(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\TestData\eages", "polygon" + i);
            }

            pIntePolateEages.Insert(0, this.pEageA);
            pIntePolateEages.Add(this.pEageB);

           

            return pIntePolateEages;
        }

        /// <summary>
        /// 获取一轮廓线点对应的轮廓线对应点
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <param name="pEageVertexDict"></param>
        /// <returns></returns>
        private Dictionary<Vertex, List<Vertex>> GetCorrespondPoint(Dictionary<string, Vertex> pEageVertexDict)
        {
            #region 获取对应关系
            Dictionary<Vertex, List<Vertex>> pCorrespondPoints = new Dictionary<Vertex, List<Vertex>>();

            foreach (var vp in pEageA.vertexList)
            {
                pCorrespondPoints.Add(vp, new List<Vertex>());
            }

            //二维三角网
            TriMesh trimesh = new TriMesh();
           
            foreach (var item in pMesh.Triangles)
            {
                TriangleNet.Geometry.Vertex p1, p2, p3;

                TriangleNet.Topology.Triangle pTinTri = item;

                p1 = item.GetVertex(0);
                p2 = item.GetVertex(1);
                p3 = item.GetVertex(2);

                Vertex pp1 = pEageVertexDict[p1.NAME];
                Vertex pp2 = pEageVertexDict[p2.NAME];
                Vertex pp3 = pEageVertexDict[p3.NAME];

                List<Vertex> pEageAPoints = new List<Vertex>();
                List<Vertex> pEageBPoints = new List<Vertex>();

                //这一段是通过name来区分,因为每次新增Vertex，都是让它在eage.name后边加上一个数字，所以要用startwith
                if (p1.NAME.StartsWith(pEageA.name))
                    pEageAPoints.Add(pp1);
                else
                    pEageBPoints.Add(pp1);

                if (p2.NAME.StartsWith(pEageA.name))
                    pEageAPoints.Add(pp2);
                else
                    pEageBPoints.Add(pp2);

                if (p3.NAME.StartsWith(pEageA.name))
                    pEageAPoints.Add(pp3);
                else
                    pEageBPoints.Add(pp3);
                /*                if (p1.NAME.Substring(0, 1) == pEageA.name.Substring(0, 1))
                    pEageAPoints.Add(pp1);
                else
                    pEageBPoints.Add(pp1);

                if (p2.NAME.Substring(0, 1) == pEageA.name.Substring(0, 1))
                    pEageAPoints.Add(pp2);
                else
                    pEageBPoints.Add(pp2);

                if (p3.NAME.Substring(0, 1) == pEageA.name.Substring(0, 1))
                    pEageAPoints.Add(pp3);
                else
                    pEageBPoints.Add(pp3);*/

                if (pEageAPoints.Count == 1)
                {
                    bool isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[0]])
                    {
                        if (vk.name == pEageBPoints[0].name)
                            isExist = true;

                    }
                    if (!isExist)
                    pCorrespondPoints[pEageAPoints[0]].Add(pEageBPoints[0]);

                    isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[0]])
                    {
                        if (vk.name == pEageBPoints[1].name)
                            isExist = true;

                    }
                    if (!isExist)
                    pCorrespondPoints[pEageAPoints[0]].Add(pEageBPoints[1]);
                }

                if (pEageAPoints.Count == 2)
                {
                    bool isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[0]])
                    {
                        if (vk.name == pEageBPoints[0].name)
                            isExist = true;

                    }
                    if (!isExist)
                        pCorrespondPoints[pEageAPoints[0]].Add(pEageBPoints[0]);

                    isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[1]])
                    {
                        if (vk.name == pEageBPoints[0].name)
                            isExist = true;
                    }
                    if (!isExist)
                        pCorrespondPoints[pEageAPoints[1]].Add(pEageBPoints[0]);
                   
                }          
            }

            Dictionary<Vertex, List<Vertex>> pAllCorrespondPoints = new Dictionary<Vertex,List<Vertex>>();
            //修改点的顺序
            foreach (var vp in pEageA.vertexList)
            {
                foreach (var vk in pCorrespondPoints.Keys)
                {
                    if (vk == vp)
                    {
                        pAllCorrespondPoints.Add(vk, pCorrespondPoints[vk]);
                        pCorrespondPoints.Remove(vk);
                        break;
                    }
                }
            }

            return pAllCorrespondPoints;


            #endregion
        }

        #region 排序
        /// <summary>
        /// 冒泡排序
        /// </summary>
        /// <param name="vertexs"></param>
        /// <param name="len"></param>
        private void BubbleSort(List<Vertex> vertexs, int len)
        {

            int i, j;
            Vertex temp;
            for (j = 0; j < len - 1; j++)
            {
                for (i = 0; i < len - 1 - j; i++)
                {

                    if (vertexs[i].id > vertexs[i + 1].id)
                    {
                        temp = vertexs[i];
                        vertexs[i] = vertexs[i + 1];
                        vertexs[i + 1] = temp;
                    }
                }
            }
        }

        /// <summary>
        /// 特殊位置排序
        /// </summary>
        /// <param name="vertexs"></param>
        /// <returns></returns>
        public void SpecialBubbleSore(List<Vertex> vertexs)
        {
            List<Vertex> newVetex = new List<Vertex>();

            BubbleSort(vertexs, vertexs.Count);

            int n = 0;
            for (int i = 0; i < vertexs.Count - 1; i++)
            {
                if (vertexs[i].id != vertexs[i + 1].id - 1)
                {
                    n = i;
                    break;
                }
            }

            for (int i = n + 1; i < vertexs.Count; i++)
            {
                newVetex.Add(vertexs[i]);

            }

            for (int i = 0; i <= n; i++)
            {
                newVetex.Add(vertexs[i]);

            }


            for (int i = 0; i < newVetex.Count; i++)
            {
                vertexs[i] = newVetex[i];
            }

        }

        /// <summary>
        /// 将轮廓边上点的对应的点保持顺时针排序
        /// </summary>
        /// <param name="vertexs"></param>
        /// <returns></returns>
        public void PointsSore(Dictionary<Vertex, List<Vertex>> pEee)
        {
            foreach (var vt in pEee.Keys)
            {
                bool isExist = false;
                foreach (var vp in pEee[vt])
                {
                    if (vp.id == 0)
                    {
                        isExist = true;
                        break;
                    }
                }

                if (isExist)
                {
                    SpecialBubbleSore(pEee[vt]);
                }
                else
                {
                    BubbleSort(pEee[vt], pEee[vt].Count);
                }

            }
        }
        #endregion
       

        /// <summary>
        /// 将边界上的点集保存到字典上便于查询
        /// </summary>
        /// <param name="pVertexListA"></param>
        /// <returns></returns>
        private Dictionary<string, Vertex> ConvertToDict(List<Vertex> pVertexListA, List<Vertex> pVertexListB)
        {

            Dictionary<string, Vertex> pVertexDict = new Dictionary<string, Vertex>();

            foreach (var vt in pVertexListA)
            {
                pVertexDict.Add(vt.name, vt);
            }

            foreach (var vt in pVertexListB)
            {
                pVertexDict.Add(vt.name, vt);
            }

            return pVertexDict;
        }

        /// <summary>
        /// 插值获取新的边界点
        /// </summary>
        /// <param name="_pEee"></param>
        /// <param name="n"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private List<Eage> GetIntepolateEages(Dictionary<Vertex, List<Vertex>> _pEee,int n,string name)
        {
            List<Eage> pIntePolateEage = new List<Eage>();

            for (int i = 1; i <= n; i++)
            {
                Eage pNewEage = new Eage();
                pNewEage.name = name + i;
                int count=0;
                foreach (var vt in _pEee.Keys)
                {

                    foreach (var vvt in _pEee[vt])
                    {
                        
                        Vertex pVertex = new Vertex();
                        pVertex.name = pNewEage.name + count;
                        
                        pVertex.x = vt.x + (vvt.x - vt.x) * ((i + 0.0) / (n + 1));
                        pVertex.y = vt.y + (vvt.y - vt.y) * ((i + 0.0) / (n + 1));
                        pVertex.z = vt.z + (vvt.z - vt.z) * ((i + 0.0) / (n + 1));
                        pNewEage.AddVertex(pVertex);
                        count++;
                    }
                }

                pIntePolateEage.Add(pNewEage);
            }

            return pIntePolateEage;
        }


        /// <summary>
        /// 根据面积重新确定轮廓线边界
        /// </summary>
        /// <param name="pIntePolateEages"></param>
        /// <param name="areas"></param>
        /// <returns></returns>
        private void GetIntepolateEagesByArea(List<Eage> pIntePolateEages, List<double> areas)
        {
            //List<double> pEageAreas = new List<double>();
            for (int i = 0; i < pIntePolateEages.Count; i++)
            {
                double pEageArea = GetEageArea(pIntePolateEages[i]);

                Vertex pCentralPoint1 = pEageA.Get2DCentralPoint();
                Vertex pCentralPoint = pIntePolateEages[i].Get2DCentralPoint();

                double scale = areas[i]/pEageArea;

                for (int j = 0; j < pIntePolateEages[i].vertexList.Count; j++)
                {
                    pIntePolateEages[i].vertexList[j].x = scale * pIntePolateEages[i].vertexList[j].x - scale * pCentralPoint.x + pCentralPoint.x;

                    pIntePolateEages[i].vertexList[j].y = scale * pIntePolateEages[i].vertexList[j].y - scale * pCentralPoint.y + pCentralPoint.y;

                }
                pIntePolateEages[i].ExportEagePointToShapefile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\TestData\eages", "eageinter" + i);
            }         
        }

        

        /// <summary>
        /// 获取轮廓线构建成面的面积
        /// </summary>
        /// <param name="pEage"></param>
        /// <returns></returns>
        private double GetEageArea(Eage pEage)
        {
            List<Geometry> ListPT = new List<Geometry>();
            Geometry pRing = new Geometry(wkbGeometryType.wkbLinearRing);
            for (int i = 0; i <= pEage.vertexList.Count; i++)
            {
                if (i == pEage.vertexList.Count)
                {
                    pRing.AddPoint_2D(pEage.vertexList[0].x, pEage.vertexList[0].y);
                    continue;
                }
                pRing.AddPoint_2D(pEage.vertexList[i].x, pEage.vertexList[i].y);

            }

            Geometry pPolygon = new Geometry(wkbGeometryType.wkbPolygon);
            pPolygon.AddGeometry(pRing);

            return pPolygon.Area();
        }

        /// <summary>
        /// 修改获取插值线的方法，让获得的线更稳定，不会出现错误和交错
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        public Eage ContourInterPolateByRateStable(double rate)
        {

            //1获取所有点的信息
            Dictionary<string, int> pVListAIndex, pVListBIndex;
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDictStable(pEageA.vertexList, pEageB.vertexList, out pVListAIndex, out pVListBIndex);

            //2获取点的对应关系
            Dictionary<int, List<int>> pVertexIndexPair;
            pEee = GetCorrespondPointStable(pEageVertexDict, pVListAIndex, pVListBIndex, out pVertexIndexPair);
            //PointsSore(pEee);

            //3获取插值后的边
            Eage pIntePolateEage = GetIntepolateEageStable(pEee, pVertexIndexPair, pEageA, pEageB, rate, pEageA.name.Substring(0, 2) + pEageB.name.Substring(0, 2));

            return pIntePolateEage;

        }
        /// <summary>
        /// 修改插值的顺序，让获得的边界线更为稳定。
        /// </summary>
        /// <param name="_pEee"></param>
        /// <param name="rate"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private Eage GetIntepolateEageStable(Dictionary<Vertex, List<Vertex>> _pEee, Dictionary<int, List<int>> pVertexIndexPair, Eage eageA, Eage eageB, double rate, string name)
        {
            Eage pNewEage = new Eage();
            pNewEage.name = name + rate.ToString();
            int count = 0;
            /*            foreach (var vt in _pEee.Keys)
                        {

                            foreach (var vvt in _pEee[vt])
                            {

                                Vertex pVertex = new Vertex();
                                pVertex.name = pNewEage.name + count;
                                pVertex.x = vt.x + (vvt.x - vt.x) * rate;
                                pVertex.y = vt.y + (vvt.y - vt.y) * rate;
                                pVertex.z = vt.z + (vvt.z - vt.z) * rate;
                                pNewEage.AddVertex(pVertex);
                                count++;
                            }
                        }*/
            int paircount = pVertexIndexPair.Keys.Count;
            int lastPairid = -1;
            for (int i = 0; i < paircount; i++)
            {//按照顺序来生成边界，这样就可以避免中间的插值出毛病。
                List<int> pairindex = pVertexIndexPair[i];
                pairindex.Sort();
                for (int j = 0; j < pairindex.Count; j++)
                {
                    Vertex vt = eageA.vertexList[i];
                    Vertex vvt = eageB.vertexList[pairindex[j]];
                    Vertex pVertex = new Vertex();
                    pVertex.name = pNewEage.name + count;
                    pVertex.x = vt.x + (vvt.x - vt.x) * rate;
                    pVertex.y = vt.y + (vvt.y - vt.y) * rate;
                    pVertex.z = vt.z + (vvt.z - vt.z) * rate;
                    pNewEage.AddVertex(pVertex);
                    count++;
                }

            }
            return pNewEage;
        }
        private Dictionary<Vertex, List<Vertex>> GetCorrespondPointStable(Dictionary<string, Vertex> pEageVertexDict, Dictionary<string, int> pointsAindex, Dictionary<string, int> pointsBindex, out Dictionary<int, List<int>> pCorrespondPointindexs)
        {
            #region 获取对应关系
            Dictionary<Vertex, List<Vertex>> pCorrespondPoints = new Dictionary<Vertex, List<Vertex>>();
            pCorrespondPointindexs = new Dictionary<int, List<int>>();
            foreach (var vp in pEageA.vertexList)
            {
                pCorrespondPoints.Add(vp, new List<Vertex>());

            }
            for (int i = 0; i < pEageA.vertexList.Count; i++)
            {
                pCorrespondPointindexs.Add(i, new List<int>());
            }
            //二维三角网
            TriMesh trimesh = new TriMesh();

            foreach (var item in pMesh.Triangles)
            {
                TriangleNet.Geometry.Vertex p1, p2, p3;

                TriangleNet.Topology.Triangle pTinTri = item;

                p1 = item.GetVertex(0);
                p2 = item.GetVertex(1);
                p3 = item.GetVertex(2);

                Vertex pp1 = pEageVertexDict[p1.NAME];
                Vertex pp2 = pEageVertexDict[p2.NAME];
                Vertex pp3 = pEageVertexDict[p3.NAME];

                List<Vertex> pEageAPoints = new List<Vertex>();
                List<Vertex> pEageBPoints = new List<Vertex>();
                Vertex[] eageAPoints = new Vertex[pointsAindex.Count];
                Vertex[] eageBPoints = new Vertex[pointsBindex.Count];
                //这一段是通过name来区分,因为每次新增Vertex，都是让它在eage.name后边加上一个数字，所以要用startwith
                if (p1.NAME.StartsWith(pEageA.name))
                {
                    eageAPoints[pointsAindex[pp1.name]] = pp1;
                    pEageAPoints.Add(pp1);
                }
                else
                {
                    eageBPoints[pointsBindex[pp1.name]] = pp1;
                    pEageBPoints.Add(pp1);
                }

                if (p2.NAME.StartsWith(pEageA.name))
                {
                    eageAPoints[pointsAindex[pp2.name]] = pp2;
                    pEageAPoints.Add(pp2);
                }
                else
                {
                    eageBPoints[pointsBindex[pp2.name]] = pp2;
                    pEageBPoints.Add(pp2);
                }
                if (p3.NAME.StartsWith(pEageA.name))
                {
                    eageAPoints[pointsAindex[pp3.name]] = pp3;
                    pEageAPoints.Add(pp3);
                }
                else
                {
                    eageBPoints[pointsBindex[pp3.name]] = pp3;
                    pEageBPoints.Add(pp3);
                }
                /*                if (p1.NAME.Substring(0, 1) == pEageA.name.Substring(0, 1))
                    pEageAPoints.Add(pp1);
                else
                    pEageBPoints.Add(pp1);

                if (p2.NAME.Substring(0, 1) == pEageA.name.Substring(0, 1))
                    pEageAPoints.Add(pp2);
                else
                    pEageBPoints.Add(pp2);

                if (p3.NAME.Substring(0, 1) == pEageA.name.Substring(0, 1))
                    pEageAPoints.Add(pp3);
                else
                    pEageBPoints.Add(pp3);*/

                if (pEageAPoints.Count == 1)
                {
                    bool isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[0]])
                    {
                        if (vk.name == pEageBPoints[0].name)
                            isExist = true;

                    }
                    if (!isExist)
                    {
                        pCorrespondPoints[pEageAPoints[0]].Add(pEageBPoints[0]);
                        pCorrespondPointindexs[pointsAindex[pEageAPoints[0].name]].Add(pointsBindex[pEageBPoints[0].name]);//记录一下index
                    }
                    isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[0]])
                    {
                        if (vk.name == pEageBPoints[1].name)
                            isExist = true;

                    }
                    if (!isExist)
                    {
                        pCorrespondPoints[pEageAPoints[0]].Add(pEageBPoints[1]);
                        pCorrespondPointindexs[pointsAindex[pEageAPoints[0].name]].Add(pointsBindex[pEageBPoints[1].name]);//记录一下index
                    }
                }


                if (pEageAPoints.Count == 2)
                {
                    bool isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[0]])
                    {
                        if (vk.name == pEageBPoints[0].name)
                            isExist = true;

                    }
                    if (!isExist)
                    {
                        pCorrespondPoints[pEageAPoints[0]].Add(pEageBPoints[0]);
                        pCorrespondPointindexs[pointsAindex[pEageAPoints[0].name]].Add(pointsBindex[pEageBPoints[0].name]);//记录一下index
                    }
                    isExist = false;
                    foreach (var vk in pCorrespondPoints[pEageAPoints[1]])
                    {
                        if (vk.name == pEageBPoints[0].name)
                            isExist = true;
                    }
                    if (!isExist)
                    {
                        pCorrespondPoints[pEageAPoints[1]].Add(pEageBPoints[0]);
                        pCorrespondPointindexs[pointsAindex[pEageAPoints[1].name]].Add(pointsBindex[pEageBPoints[0].name]);//记录一下index
                    }

                }
            }

            Dictionary<Vertex, List<Vertex>> pAllCorrespondPoints = new Dictionary<Vertex, List<Vertex>>();
            //修改点的顺序
            foreach (var vp in pEageA.vertexList)
            {
                foreach (var vk in pCorrespondPoints.Keys)
                {
                    if (vk == vp)
                    {
                        pAllCorrespondPoints.Add(vk, pCorrespondPoints[vk]);
                        pCorrespondPoints.Remove(vk);
                        break;
                    }
                }
            }

            return pAllCorrespondPoints;


            #endregion
        }
        /// <summary>
        /// 稳定顺序获得边界线折线点，不要用List，就直接用index
        /// </summary>
        /// <param name="pVertexListA"></param>
        /// <param name="pVertexListB"></param>
        /// <returns></returns>
        private Dictionary<string, Vertex> ConvertToDictStable(List<Vertex> pVertexListA, List<Vertex> pVertexListB, out Dictionary<string, int> pVListAindex, out Dictionary<string, int> pVListBindex)
        {
            pVListAindex = new Dictionary<string, int>();
            pVListBindex = new Dictionary<string, int>();
            Dictionary<string, Vertex> pVertexDict = new Dictionary<string, Vertex>();
            int countA = pVertexListA.Count;
            int countB = pVertexListB.Count;
            for (int i = 0; i < countA; i++)
            {
                Vertex vt = pVertexListA[i];
                pVertexDict.Add(vt.name, vt);
                pVListAindex.Add(vt.name, i);
            }

            for (int i = 0; i < countB; i++)
            {
                Vertex vt = pVertexListB[i];
                pVertexDict.Add(vt.name, vt);
                pVListBindex.Add(vt.name, i);
            }

            return pVertexDict;
        }

    }
}
