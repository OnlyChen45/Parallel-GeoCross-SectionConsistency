using GeoCommon;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OSR;





namespace SolidModel
{
    public class ContourHelp
    {
        /// <summary>
        /// 轮廓线集合A
        /// </summary>
        public List<Contour> contourListA;
        public List<Eage> eageListA;
        /// <summary>
        /// 轮廓线集合B
        /// </summary>
        public List<Contour> contourListB;
        public List<Eage> eageListB;
        /// <summary>
        /// 边界模型
        /// </summary>
        public List<BrepModel> pBrepModelList;

        /// <summary>
        /// 线平均
        /// </summary>
        public bool averageEage;

        /// <summary>
        /// 线增密
        /// </summary>
        public bool densificationEage;
        public Eage eageA;
        public Eage eageB;

        /// <summary>
        /// 两条边上的点对应关系
        /// </summary>
        public Dictionary<string, Dictionary<Vertex, Vertex[]>> EageCorrespondPoints;

        public ContourHelp()
        {
            pBrepModelList = new List<BrepModel>();
            averageEage = true;
            densificationEage = false;
        }

        public ContourHelp(List<Contour> contourListA, List<Contour> contourListB,out int[,] Atype,out int[,] Btype)
            //这个Acontian是用来描述底或顶相关对应关系，,turnover是表明，从底到顶是1对多 false 还是 多对1 true
        {//把两个轮廓线输入到这个建模功能对象里
            this.contourListA = contourListA;//源代码中A为底，B为顶
            this.contourListB = contourListB;
            this.eageListA = new List<Eage>();
            this.eageListB = new List<Eage>();

            List<Contour> pConA = new List<Contour>();
            
            for (int i = 0; i < contourListA.Count(); i++)
            {

                pConA.Add(contourListA[i]);
                List<Eage> edges = contourListA[i].eageList;
                for (int j = 0; j < edges.Count(); j++) {
                    Eage eage = edges[j];
                    this.eageListA.Add(eage);
                }
            }

            List<Contour> pConB = new List<Contour>();
           
            for (int i = 0; i < contourListB.Count(); i++)
            {
                pConB.Add(contourListB[i]);
                List<Eage> edges = contourListB[i].eageList;
                for (int j = 0; j < edges.Count(); j++)
                {
                    Eage eage = edges[j];
                    this.eageListB.Add(eage);
                }
            }
            Atype = new int[eageListA.Count(), 3];
            Btype = new int[eageListB.Count(), 3];
            Geometry[] Ageom = new Geometry[eageListA.Count()];
            Geometry[] Bgeom = new Geometry[eageListB.Count()];
            //这两个主要是记录每个contour与另一个图层的关系的，0 暂时不用，1为inside,2为Intersects,数组内的int指的是这个关系所针对的另一个图层的下标。
            for (int i = 0; i < eageListA.Count(); i++) {
                Geometry geom1 = new Geometry(wkbGeometryType.wkbLinearRing);
                Geometry geom2 = new Geometry(wkbGeometryType.wkbPolygon);
                List<Vertex> vertices = eageListA[i].vertexList;
                for (int j = 0; j < vertices.Count(); j++) {
                    Vertex vertex = vertices[j];
                    geom1.AddPoint_2D(vertex.x, vertex.y);

                }
                geom1.AddPoint_2D(vertices[0].x, vertices[0].y);
                geom2.AddGeometry(geom1);
                Ageom[i] = geom2;
            }
            for (int i = 0; i < eageListB.Count(); i++)
            {
                Geometry geom1 = new Geometry(wkbGeometryType.wkbLinearRing);
                Geometry geom2 = new Geometry(wkbGeometryType.wkbPolygon);
                List<Vertex> vertices = eageListB[i].vertexList;
                for (int j = 0; j < vertices.Count(); j++)
                {
                    Vertex vertex = vertices[j];
                    geom1.AddPoint_2D(vertex.x, vertex.y);

                }
                geom1.AddPoint_2D(vertices[0].x, vertices[0].y);
                geom2.AddGeometry(geom1);
                Bgeom[i] = geom2;
            }
            //给存储A Btype的数组初始化
            for (int i = 0; i < eageListA.Count(); i++) {
                for (int j = 0; j < 3; j++) {
                    Atype[i, j] = -1;
                }
            }
            for (int i = 0; i < eageListB.Count(); i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Btype[i, j] = -1;
                }
            }
            for (int i = 0; i < eageListB.Count(); i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Btype[i, j] = -1;
                }
            }
            //后编写如何判定这个不同情况的叠置问题。
            averageEage = true;
            densificationEage = false;
            pBrepModelList = new List<BrepModel>();
        }
        public void getArcPairs(Eage eageA, Eage eageB, List<Vertex> nodesA, List<Vertex> nodesB,int polygonid, ref List<ModelWithArc> modelArcList, bool _densificationEage = false, bool findcorrespond = false)
        {
            //重构一下这个方法，主要是向把这个方法的线段给输出
            //首先把两个线段进行均匀操作
            List<ArcSe> arcsA;
            List<ArcSe> arcsB;
            Eage oriEageA = eageA;
            Eage oriEageB = eageB;
            getArcsList(out arcsA, out arcsB, ref eageA, ref eageB, ref nodesA, ref nodesB, _densificationEage: _densificationEage, findcorrespond);
            
            #region 输出一下arc的边界
            /*for (int i = 0; i < arcsB.Count(); i++)
            {
                arcsB[i].eage.ExportEagePointToLine2D(@"D:\temp", "arcB" + i.ToString());
            }*/
           
            #endregion
            #region 这部分要搞一下把弧段对与已有的建模完成的弧段对对比，然后如果有相同或者类似的
            //有这么一回事，就是线段增密是自适应的，所以说，我们需要用没有增密过的边界去和结果对比，才能得到比较好的结果
            //而且为什么到这里可以把那些已经建模过的arc去掉而不影响后续程序呢？因为到这里之后原始的eage已经被分配封装进了arc中，后续的建模也是为了arc。ok，么问题
            //这里我们先重新建一下线段，不带线均匀的
           //List<ArcSe> arcsA2, arcsB2;
           // Eage tempeageA = oriEageA;
           // Eage tempeageB = oriEageB;
            //getArcsList(out arcsA2, out arcsB2, ref tempeageA, ref tempeageB, ref nodesA, ref nodesB, _densificationEage: false, findcorrespond: false);//因为nodesA，nodesB都是处理过的了，所以就不必这么搞了
            int finishcount = modelArcList.Count();
            int arccount = arcsA.Count();
            /*if (polygonid == 4)
            {
                Console.WriteLine("debug用");
            }*/
            for (int i = arccount - 1; i >= 0; i--)
            {
                ArcSe arcA = arcsA[i];
                ArcSe arcB = arcsB[i];
               
                for (int j = 0; j < finishcount; j++)
                {
                    ModelWithArc modelWithArc = modelArcList[j];
                    bool ttttttt = modelWithArc.compareArc(arcA, arcB);
                    if (ttttttt)
                    {
                        modelWithArc.polygonids.Add(polygonid);
                        arcsA.RemoveAt(i);
                        arcsB.RemoveAt(i);

                    }
                }
            }
            #endregion

           
            int arcscount = arcsA.Count();
            for (int i = 0; i < arcscount; i++)
            {
                ModelWithArc modelWithArc = new ModelWithArc(arcsA[i], arcsB[i], polygonid);
                modelArcList.Add(modelWithArc);
            }

        }
        public List<BrepModel> buildModelsWithArcsPairs(ref List<ModelWithArc> modelWithArcs,bool save2Dmapping=false,string savespace=null,SpatialReference spatialReference=null) 
        {
            List<BrepModel> result = new List<BrepModel>();

            int count = modelWithArcs.Count();
            for (int i = 0; i < count; i++) {
                ModelWithArc modelWithArc = modelWithArcs[i];
                bool b = modelWithArc.finishModeling;
                if (b == false) {
                    BrepModel brep = null;
                    if (save2Dmapping == false)
                    {
                        brep = getArcModel(modelWithArc.arc1, modelWithArc.arc2);
                    }
                    else {
                        brep = getArcModel(modelWithArc.arc1, modelWithArc.arc2, true, savespace + "\\mapping" + i.ToString() + ".shp", spatialReference);
                    }
                    result.Add(brep);
                    modelWithArcs[i].setModel(brep);
                    
                }
            }
            return result;
        }
        /// <summary>
        /// 弄一个可以自动增加线上点密度的
        /// </summary>
        /// <param name="modelWithArcs"></param>
        /// <param name="save2Dmapping"></param>
        /// <param name="savespace"></param>
        /// <param name="spatialReference"></param>
        /// <returns></returns>
        public List<BrepModel> buildModelsWithArcsPairsIncreaseDensification(ref List<ModelWithArc> modelWithArcs, bool save2Dmapping = false, string savespace = null, SpatialReference spatialReference = null)
        {
            List<BrepModel> result = new List<BrepModel>();

            int count = modelWithArcs.Count();
            for (int i = 0; i < count; i++)
            {
                ModelWithArc modelWithArc = modelWithArcs[i];
                bool b = modelWithArc.finishModeling;
                
                if (b == false)
                {
                    BrepModel brep = null;
                    makeArcModelIncreaseDensi(ref modelWithArc);
                    if (save2Dmapping == false)
                    {
                        brep = getArcModel(modelWithArc.arc1, modelWithArc.arc2);
                    }
                    else
                    {
                        brep = getArcModel(modelWithArc.arc1, modelWithArc.arc2, true, savespace + "\\mapping" + i.ToString() + ".shp", spatialReference);
                    }
                    result.Add(brep);
                    modelWithArcs[i].setModel(brep);

                }
            }
            return result;
        }
        private BrepModel getArcModelIncreaseDensification(ArcSe arc1, ArcSe arc2)
        {
            //先把集合保存到字典中
            Eage eageA = arc1.eage;
            Eage eageB = arc2.eage;
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(eageA.vertexList, eageB.vertexList);
            //再把轮廓线的Vertex转化为TriangleNet的Vertex
            List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(eageA.vertexList, 100);
            List<TriangleNet.Geometry.Vertex> ListVB = GetTraingNetVertexs(eageB.vertexList, 0);

            //构建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(ListVA, ListVB);

            //把三维实体的name与原来Vex的name对应上
            BrepModel pbrep = GetBrepModel(mesh, pEageVertexDict);
            pbrep.mesh = mesh;

            return pbrep;
            //
            //
            //
        }
        /// <summary>
        /// 对一个弧段对进行插值，
        /// </summary>
        /// <param name="modelWithArc"></param>
        /// <param name="threshold">阈值，大于这个就要插点</param>
        private void makeArcModelIncreaseDensi(ref ModelWithArc modelWithArc,double threshold=3) {
            //CommonFun.GetDistance3D
            List<Vertex> vertices1 = modelWithArc.arc1.eage.vertexList;
            List<Vertex> verticesnew1 = new List<Vertex>();
            int count1 = vertices1.Count;
            verticesnew1.Add(vertices1[0]);
            for (int i = 0; i < count1-1; i++) {
                Vertex vertex1 = vertices1[i];
                Vertex vertex2 = vertices1[i];
                double dis = CommonFun.GetDistance3D(vertex1, vertex2);
                if (dis <= threshold)//小于阈值就不添加
                {
                    verticesnew1.Add(vertex2);
                }
                else {
                    int count =(int)(dis / threshold);//得到整数
                    double dx = vertex2.x - vertex1.x;
                    double dy = vertex2.y - vertex1.y;
                    double dz = vertex2.z - vertex1.z;
                    for (int j = 1; j <= count; j++) {//补点
                        double ratet = (double)j / (double)count;
                        Vertex vertextemp = new Vertex();
                        vertextemp.x = vertex1.x + dx * ratet;
                        vertextemp.y = vertex1.y + dy * ratet;
                        vertextemp.z = vertex1.z + dz * ratet;
                        vertextemp.name = modelWithArc.arc1.eage.name + "inter" + i.ToString() + j.ToString();
                        verticesnew1.Add(vertextemp);
                    }
                    verticesnew1.Add(vertex2);//添加进去
                }
            }
            modelWithArc.arc1.eage.vertexList = verticesnew1;
            List<Vertex> vertices2 = modelWithArc.arc2.eage.vertexList;
            List<Vertex> verticesnew2 = new List<Vertex>();
            int count2 = vertices2.Count;
            verticesnew2.Add(vertices2[0]);
            for (int i = 0; i < count2 - 1; i++)
            {
                Vertex vertex1 = vertices2[i];
                Vertex vertex2 = vertices2[i];
                double dis = CommonFun.GetDistance3D(vertex1, vertex2);
                if (dis <= threshold)//小于阈值就不添加
                {
                    verticesnew2.Add(vertex2);
                }
                else
                {
                    int count = (int)(dis / threshold);//得到整数
                    double dx = vertex2.x - vertex1.x;
                    double dy = vertex2.y - vertex1.y;
                    double dz = vertex2.z - vertex1.z;
                    for (int j = 1; j <= count; j++)
                    {//补点
                        double ratet = (double)j / (double)count;
                        Vertex vertextemp = new Vertex();
                        vertextemp.x = vertex1.x + dx * ratet;
                        vertextemp.y = vertex1.y + dy * ratet;
                        vertextemp.z = vertex1.z + dz * ratet;
                        vertextemp.name = modelWithArc.arc1.eage.name + "inter" + i.ToString() + j.ToString();
                        verticesnew2.Add(vertextemp);
                    }
                    verticesnew2.Add(vertex2);//添加进去
                }
            }
            modelWithArc.arc2.eage.vertexList = verticesnew2;
        }
        public List<BrepModel> ArcToModels(Eage eageA, Eage eageB, List<Vertex> nodesA, List<Vertex> nodesB,ref List<ModelWithArc> modelArcList, bool _densificationEage = false, bool findcorrespond = false) {
            //重构一下这个方法，主要是向把这个方法的线段给输出
            //首先把两个线段进行均匀操作
            List<ArcSe> arcsA ;
            List<ArcSe> arcsB ;
            Eage oriEageA = eageA;
            Eage oriEageB = eageB;
            getArcsList(out arcsA, out arcsB, ref eageA, ref eageB,ref nodesA,ref nodesB, _densificationEage: _densificationEage, findcorrespond);
            /*eageA = GetAverageEage(eageA, 1f);
            eageB = GetAverageEage(eageB, 1f);
            if (_densificationEage)
            {
                DensifiPoint(ref eageA, ref eageB);
            }
            //把两个eage都变成顺时针
            Eage tempEage;
            bool tempb = makeEageClockwise(eageA, out tempEage);
            if (tempb == false)
            {
                eageA = tempEage;
            }
            tempb = makeEageClockwise(eageB, out tempEage);
            if (tempb == false)
            {
                eageB = tempEage;
            }
            makeVertexListClockwise(ref nodesA);
            makeVertexListClockwise(ref nodesB);
            //让输入的切割点顺时针，也让边界数据顺时针，可以比较好的进行弧段生成
            //然后按照对应点对距离和最短，找到A，B对应的顺序的顺序，

            if (findcorrespond == false)
            {
                findcorres(ref nodesA, ref nodesB);
            }
            //然后按照A，B给切割开来，建模，返回。
            int nodescount = nodesA.Count();
            List<int> nodesAindex = getCutPointsID(eageA, nodesA);//把切开点的index拿到
            List<int> nodesBindex = getCutPointsID(eageB, nodesB);
            List<ArcSe> arcsA = getArcs(eageA, nodesAindex);
            List<ArcSe> arcsB = getArcs(eageB, nodesBindex);*/
            #region 输出一下arc的边界
            /*for (int i = 0; i < arcsB.Count(); i++)
            {
                arcsB[i].eage.ExportEagePointToLine2D(@"D:\temp", "arcB" + i.ToString());
            }*/
            List<BrepModel> result = new List<BrepModel>();
            #endregion
            #region 这部分要搞一下把弧段对与已有的建模完成的弧段对对比，然后如果有相同或者类似的
            //有这么一回事，就是线段增密是自适应的，所以说，我们需要用没有增密过的边界去和结果对比，才能得到比较好的结果
            //而且为什么到这里可以把那些已经建模过的arc去掉而不影响后续程序呢？因为到这里之后原始的eage已经被分配封装进了arc中，后续的建模也是为了arc。ok，么问题
            //这里我们先重新建一下线段，不带线均匀的
            List<ArcSe> arcsA2,arcsB2;
            Eage tempeageA = oriEageA;
            Eage tempeageB = oriEageB;
            getArcsList(out arcsA2, out arcsB2, ref tempeageA, ref tempeageB, ref nodesA, ref nodesB, _densificationEage: false, findcorrespond:false);//因为nodesA，nodesB都是处理过的了，所以就不必这么搞了
            int finishcount = modelArcList.Count();
            int arccount = arcsA2.Count();
            for (int i = arccount-1; i >=0 ; i--) {
                ArcSe arcA = arcsA2[i];
                ArcSe arcB = arcsB2[i];
                bool duplication = false;
                for (int j = 0; j < finishcount; j++) {
                    ModelWithArc modelWithArc = modelArcList[j];
                    bool ttttttt = modelWithArc.compareArc(arcA, arcB);
                    if (ttttttt) {
                        result.Add(modelWithArc.getModel());
                        arcsA.RemoveAt(i);
                        arcsB.RemoveAt(i);
                        
                    }
                }
            }
            #endregion
           
            int nodescount = nodesA.Count();

            int arcscount = arcsA.Count();
            for (int i = 0; i < arcscount; i++)
            {
                BrepModel brep = getArcModel(arcsA[i], arcsB[i]);
                result.Add(brep);
                modelArcList.Add(new ModelWithArc(arcsA[i], arcsB[i], brep));
            }

            return result;
        }
        public List<BrepModel> ArcToModels(Eage arceage,Eage polyeage,List<Vertex> arcCuts,List<Vertex> nodes,bool specialForArc, bool _densificationEage = false, bool findcorrespond = false) {
            //重载这玩意儿，专门处理弧段给出的Eage和一个正常的Eage
            //由于arc的
            arceage = GetAverageEage(arceage, 1f);
            polyeage = GetAverageEage(polyeage, 1f);
            if (_densificationEage)
            {
                DensifiPoint(ref arceage, ref polyeage);
            }
            //把两个eage都变成顺时针
            Eage tempEage;
            bool arcClockWise = makeEageClockwise(arceage, out tempEage);
            if (arcClockWise == false)
            {
                arceage = tempEage;
                int count1 = arcCuts.Count();
                List<Vertex> verticestemp = new List<Vertex>();
                for (int j = count1 - 1; j >= 0; j--) {
                    verticestemp.Add(arcCuts[j]);
                }
                arcCuts = verticestemp;
            }
            bool tempb = makeEageClockwise(polyeage, out tempEage);
            if (tempb == false)
            {
                polyeage = tempEage;
            }
            //makeVertexListClockwise(ref nodesA);
            makeVertexListClockwise(ref nodes);
            findcorres(ref arcCuts, ref nodes);
            List<int> nodeIndexArc = getCutPointsIDForArc(arceage,arcCuts);
            List<int> nodeIndex2 = getCutPointsID(polyeage, nodes);
            List<ArcSe> arcsA = getArcs(arceage, nodeIndexArc);
            List<ArcSe> arcsB = getArcs(polyeage, nodeIndex2);
            List<BrepModel> result = new List<BrepModel>();
            //Dictionary<string,>
            int nodescount = arcsA.Count();
            for (int i = 0; i < nodescount; i++)
            {
                BrepModel brep = getArcModel(arcsA[i], arcsB[i]);
                result.Add(brep);
            }
            return result;
        }
        public List<BrepModel> ArcToModels(Eage eageA,Eage eageB,List<Vertex> nodesA,List<Vertex>nodesB, bool _densificationEage=false, bool findcorrespond=false) {
            //新键盘到了，很开心
           //首先把两个线段进行均匀操作
           eageA = GetAverageEage(eageA, 1f);
           eageB = GetAverageEage(eageB, 1f);
            if (_densificationEage)
            {
                DensifiPoint(ref eageA, ref eageB);
            }
            //把两个eage都变成顺时针
            Eage tempEage;
            bool tempb = makeEageClockwise(eageA, out tempEage);
            if (tempb == false) {
                eageA = tempEage;
            }
            tempb = makeEageClockwise(eageB, out tempEage);
            if (tempb == false) {
                eageB = tempEage;
            }
            makeVertexListClockwise(ref nodesA);
            makeVertexListClockwise(ref nodesB);
            //让输入的切割点顺时针，也让边界数据顺时针，可以比较好的进行弧段生成
            //然后按照对应点对距离和最短，找到A，B对应的顺序的顺序，

            if (findcorrespond == false) {
                findcorres(ref nodesA,ref nodesB);
                /*double mindistance = double.MaxValue;
                double collection = 0;
                int mini = -1;
                int count1 = nodesA.Count();
                int count2 = nodesB.Count();
                Vertex c1 = getCenter2D(nodesA);
                Vertex c2 = getCenter2D(nodesB);
                double[] vector = new double[2];
                vector[0] = c1.x - c2.x;
                vector[1] = c1.y - c2.y;
                if (count1 != count2) { return null; };
                for (int i=0; i<count1; i++) {
                    collection = 0;
                    for (int j = 0; j < count1; j++) {
                        Vertex vertexA = nodesA[j];
                        Vertex vertexB = nodesB[(j + i) % count1];
                        double t1 = distance(vertexA.x, vertexA.y, vertexB.x+vector[0], vertexB.y+vector[1]);
                        collection += t1;
                    }
                    if (collection<mindistance) {//判断一下，A从0与B从i开始的一一对应得到的平面距离是否最短，如果是，那么
                        mindistance = collection;
                        mini = i;
                    }
                }
                //把b按照与A的点的对应，调整顺序，
                List<Vertex> nodestemp = new List<Vertex>();
                for (int i = mini; i < count1; i++) {
                    nodestemp.Add(nodesB[i]);
                }
                for (int i = 0; i < mini; i++) {
                    nodestemp.Add(nodesB[i]);
                }
                nodesB = nodestemp;*/
            }
            //然后按照A，B给切割开来，建模，返回。
            int nodescount = nodesA.Count();
            List<int> nodesAindex = getCutPointsID(eageA, nodesA);//把切开点的index拿到
            List<int> nodesBindex = getCutPointsID(eageB, nodesB);
            List<ArcSe> arcsA = getArcs(eageA, nodesAindex);
            List<ArcSe> arcsB = getArcs(eageB, nodesBindex);
            #region 输出一下arc的边界
            for (int i = 0; i < arcsB.Count(); i++) {
                arcsB[i].eage.ExportEagePointToLine2D(@"D:\temp","arcB"+i.ToString());
            }
            #endregion
            List<BrepModel> result = new List<BrepModel>();
            //Dictionary<string,>
            for (int i = 0; i < nodescount; i++) {
                BrepModel brep = getArcModel(arcsA[i], arcsB[i]);
                result.Add(brep);
            }

            return result;
        }
        private void getArcsList(out List<ArcSe> arcsA,out  List<ArcSe> arcsB,ref Eage eageA,ref Eage eageB,ref List<Vertex> nodesA,ref List<Vertex> nodesB, bool _densificationEage = false, bool findcorrespond = false)//把从点集和边界到arcs做成一个函数，方便调用
        {
            eageA = GetAverageEage(eageA, 1f);
            eageB = GetAverageEage(eageB, 1f);
            if (_densificationEage)
            {
                DensifiPoint(ref eageA, ref eageB);
            }
            //把两个eage都变成顺时针
            Eage tempEage;
            bool tempb = makeEageClockwise(eageA, out tempEage);
            if (tempb == false)
            {
                eageA = tempEage;
            }
            tempb = makeEageClockwise(eageB, out tempEage);
            if (tempb == false)
            {
                eageB = tempEage;
            }
            makeVertexListClockwise(ref nodesA);
            makeVertexListClockwise(ref nodesB);
            //让输入的切割点顺时针，也让边界数据顺时针，可以比较好的进行弧段生成
            //然后按照对应点对距离和最短，找到A，B对应的顺序的顺序，

            if (findcorrespond == false)
            {
                findcorres(ref nodesA, ref nodesB);
            }
            //然后按照A，B给切割开来，建模，返回。
            int nodescount = nodesA.Count();
            List<int> nodesAindex = getCutPointsID(eageA, nodesA);//把切开点的index拿到
            List<int> nodesBindex = getCutPointsID(eageB, nodesB);
            arcsA = getArcs(eageA, nodesAindex);
            arcsB = getArcs(eageB, nodesBindex);
        }
        private void getArcsList(out List<ArcSe> arcsA, out List<ArcSe> arcsB, ref Eage eageA, ref Eage eageB, ref List<Vertex> nodesA, ref List<Vertex> nodesB, bool sharpenArc,bool _densificationEage = false, bool findcorrespond = false)//把从点集和边界到arcs做成一个函数，方便调用
        {//重载该方法，用于处理尖灭地层和弧段线对应的情况，这个bool sharpenArc就是作为指示的，指示出要不要这么做这方法，同时
            eageA = GetAverageEage(eageA, 1f);
            eageB = GetAverageEage(eageB, 1f);
            if (_densificationEage)
            {
                DensifiPoint(ref eageA, ref eageB);
            }
            //把两个eage都变成顺时针
            Eage tempEage;
            bool tempb = makeEageClockwise(eageA, out tempEage);
            if (tempb == false)
            {
                eageA = tempEage;
            }
            tempb = makeEageClockwise(eageB, out tempEage);
            if (tempb == false)
            {
                eageB = tempEage;
            }
            makeVertexListClockwise(ref nodesA);
            makeVertexListClockwise(ref nodesB);
            //让输入的切割点顺时针，也让边界数据顺时针，可以比较好的进行弧段生成
            //然后按照对应点对距离和最短，找到A，B对应的顺序的顺序，

            if (findcorrespond == false)
            {
                findcorres(ref nodesA, ref nodesB);
            }
            //然后按照A，B给切割开来，建模，返回。
            int nodescount = nodesA.Count();
            List<int> nodesAindex = getCutPointsID(eageA, nodesA);//把切开点的index拿到
            List<int> nodesBindex = getCutPointsID(eageB, nodesB);
            arcsA = getArcs(eageA, nodesAindex);
            arcsB = getArcs(eageB, nodesBindex);
        }
        private BrepModel getArcModel(ArcSe arc1,ArcSe arc2) {
            // 
            //先把集合保存到字典中
            Eage eageA = arc1.eage;
            Eage eageB = arc2.eage;
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(eageA.vertexList, eageB.vertexList);
            //再把轮廓线的Vertex转化为TriangleNet的Vertex
            List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(eageA.vertexList, 100);
            List<TriangleNet.Geometry.Vertex> ListVB = GetTraingNetVertexs(eageB.vertexList, 0);

            //构建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(ListVA, ListVB);

            //把三维实体的name与原来Vex的name对应上
            BrepModel pbrep = GetBrepModel(mesh, pEageVertexDict);
            pbrep.mesh = mesh;

            return pbrep;
        }
        public BrepModel getArcModelNewVersion(Eage eageA, Eage eageB)
        {
            // 
            //先把集合保存到字典中
            //Eage eageA = arc1.eage;
            //Eage eageB = arc2.eage;
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(eageA.vertexList, eageB.vertexList);
            //再把轮廓线的Vertex转化为TriangleNet的Vertex
            List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(eageA.vertexList, 100);
            List<TriangleNet.Geometry.Vertex> ListVB = GetTraingNetVertexs(eageB.vertexList, 0);

            //构建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(ListVA, ListVB);

            //把三维实体的name与原来Vex的name对应上
            BrepModel pbrep = GetBrepModel(mesh, pEageVertexDict);
            pbrep.mesh = mesh;

            return pbrep;
        }
        private BrepModel getArcModelAligntheBegin(ArcSe arc1, ArcSe arc2)
        {
            // 
            //先把集合保存到字典中
            Eage eageA = arc1.eage;
            Eage eageB = arc2.eage;
            double beginlength = getlengthWithlines(eageB.vertexList[0].x, eageB.vertexList[0].y, eageB.vertexList[1].x, eageB.vertexList[1].y, eageA.vertexList[0].x, eageA.vertexList[0].y);
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(eageA.vertexList, eageB.vertexList);
            //再把轮廓线的Vertex转化为TriangleNet的Vertex
            List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(eageA.vertexList, 100,0);
            List<TriangleNet.Geometry.Vertex> ListVB = GetTraingNetVertexs(eageB.vertexList, 0,-beginlength);//因为求的是，eageA第一个点在eageb所在剖面线上的投影点到eageB第一个点的线上距离，而eageA所在位置是0，所以在挪eageB的时候，就要用负的，就对齐了

            //构建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(ListVA, ListVB);

            //把三维实体的name与原来Vex的name对应上
            BrepModel pbrep = GetBrepModel(mesh, pEageVertexDict);
            pbrep.mesh = mesh;

            return pbrep;
            //
            //
            //
        }
        /// <summary>
        /// 通过一个线段和一个点，获取该点在线段所在直线上投影的点的位置距离线段起点的距离
        /// </summary>
        /// <returns></returns>
        private double getlengthWithlines(double startx,double starty,double endx,double endy,double px,double py) {
            double dx1 = endx - startx;
            double dy1 = endy - starty;
            double dx2 = px - startx;
            double dy2 = py - starty;
            double dianji = dx1 * dx2 + dy1 * dy2;
            double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            double length2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
            double costhet = dianji / (length1 * length2);//求出两条向量之间的夹角余弦
            double thet = Math.Acos(costhet);
            double result = Math.Cos(Math.PI - thet) * length2;
            return result;
        }
        private BrepModel getArcModel(ArcSe arc1, ArcSe arc2,bool saveProcessData,string savepath,SpatialReference spatialReference)
        {
            // 
            //先把集合保存到字典中
            Eage eageA = arc1.eage;
            Eage eageB = arc2.eage;
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(eageA.vertexList, eageB.vertexList);
            //再把轮廓线的Vertex转化为TriangleNet的Vertex
            List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(eageA.vertexList, 100);
            List<TriangleNet.Geometry.Vertex> ListVB = GetTraingNetVertexs(eageB.vertexList, 0);



            //构建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(ListVA, ListVB);
            if (saveProcessData)
            {
                OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
                DataSource ds = driver.CreateDataSource(savepath, null);
                Layer layer = ds.CreateLayer("mesh", spatialReference, wkbGeometryType.wkbPolygon, null);
                foreach (var item in mesh.Triangles)
                {
                    TriangleNet.Geometry.Vertex p1, p2, p3;

                    TriangleNet.Topology.Triangle pTinTri = item;

                    p1 = item.GetVertex(0);
                    p2 = item.GetVertex(1);
                    p3 = item.GetVertex(2);
                    Geometry triangelenew = new Geometry(wkbGeometryType.wkbPolygon);
                    Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
                    ring.AddPoint_2D(p1.X, p1.Y);
                    ring.AddPoint_2D(p2.X, p2.Y);
                    ring.AddPoint_2D(p3.X, p3.Y);
                    ring.AddPoint_2D(p1.X, p1.Y);
                    triangelenew.AddGeometry(ring);
                    Feature newfeature = new Feature(layer.GetLayerDefn());
                    newfeature.SetGeometry(triangelenew);
                    layer.CreateFeature(newfeature);
                }
                layer.Dispose();
                ds.Dispose();
                driver.Dispose();
            }
            //把三维实体的name与原来Vex的name对应上
            BrepModel pbrep = GetBrepModel(mesh, pEageVertexDict);
            pbrep.mesh = mesh;

            return pbrep;
            //
            //
            //
        }
        public BrepModel getArcModel(Eage eageA, Eage eageB)
        {
            // 
            //先把集合保存到字典中
            
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(eageA.vertexList, eageB.vertexList);
            //再把轮廓线的Vertex转化为TriangleNet的Vertex
            List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(eageA.vertexList, 100);
            List<TriangleNet.Geometry.Vertex> ListVB = GetTraingNetVertexs(eageB.vertexList, 0);

            //构建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(ListVA, ListVB);

            //把三维实体的name与原来Vex的name对应上
            BrepModel pbrep = GetBrepModel(mesh, pEageVertexDict);
            pbrep.mesh = mesh;
            return pbrep;
        }
        private void findcorres(ref List<Vertex>nodesA,ref List<Vertex> nodesB) {
            double mindistance = double.MaxValue;
            double collection = 0;
            int mini = -1;
            int count1 = nodesA.Count();
            int count2 = nodesB.Count();
            Vertex c1 = getCenter2D(nodesA);
            Vertex c2 = getCenter2D(nodesB);
            double[] vector = new double[2];
            vector[0] = c1.x - c2.x;
            vector[1] = c1.y - c2.y;
            //if (count1 != count2) { return null; };
            for (int i = 0; i < count1; i++)
            {
                collection = 0;
                for (int j = 0; j < count1; j++)
                {
                    Vertex vertexA = nodesA[j];
                    Vertex vertexB = nodesB[(j + i) % count1];
                    double t1 = distance(vertexA.x, vertexA.y, vertexB.x + vector[0], vertexB.y + vector[1]);
                    collection += t1;
                }
                if (collection < mindistance)
                {//判断一下，A从0与B从i开始的一一对应得到的平面距离是否最短，如果是，那么
                    mindistance = collection;
                    mini = i;
                }
            }
            //把b按照与A的点的对应，调整顺序，
            List<Vertex> nodestemp = new List<Vertex>();
            for (int i = mini; i < count1; i++)
            {
                nodestemp.Add(nodesB[i]);
            }
            for (int i = 0; i < mini; i++)
            {
                nodestemp.Add(nodesB[i]);
            }
            nodesB = nodestemp;
        }
        public List<TriangleNet.Geometry.Vertex> GetTraingNetVertexs(List<Vertex> eageVertex,double valueY) {
            List<TriangleNet.Geometry.Vertex> pNewVertexs = new List<TriangleNet.Geometry.Vertex>();
            double ds = 0;
            for (int i = 0; i < eageVertex.Count; i++) {
                TriangleNet.Geometry.Vertex vt = new TriangleNet.Geometry.Vertex();
                vt.ID = eageVertex[i].id;
                vt.NAME = eageVertex[i].name;
                if (i == 0) {
                    vt.X = 0;
                    vt.Y = valueY;
                    pNewVertexs.Add(vt);
                    continue;
                }
                double dis = CommonFun.GetDistance3D(eageVertex[i - 1], eageVertex[i]);//这里改动为GetDistance3D，原为2D版本。
                ds = ds + dis;
                vt.X = ds;
                vt.Y = valueY;
                pNewVertexs.Add(vt);
            }
            return pNewVertexs;
        }
        /// <summary>
        /// 为了能够在建模时候对齐弧段，必须添加一个start参数，这样就可以让两个弧段对的准一点
        /// </summary>
        /// <param name="eageVertex"></param>
        /// <param name="valueY"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public List<TriangleNet.Geometry.Vertex> GetTraingNetVertexs(List<Vertex> eageVertex, double valueY,double start)
        {
            List<TriangleNet.Geometry.Vertex> pNewVertexs = new List<TriangleNet.Geometry.Vertex>();
            double ds = start;
            for (int i = 0; i < eageVertex.Count; i++)
            {
                TriangleNet.Geometry.Vertex vt = new TriangleNet.Geometry.Vertex();
                vt.ID = eageVertex[i].id;
                vt.NAME = eageVertex[i].name;
                if (i == 0)
                {
                    vt.X = start;
                    vt.Y = valueY;
                    pNewVertexs.Add(vt);
                    continue;
                }
                double dis = CommonFun.GetDistance3D(eageVertex[i - 1], eageVertex[i]);//这里改动为GetDistance3D，原为2D版本。
                ds = ds + dis;
                vt.X = ds;
                vt.Y = valueY;
                pNewVertexs.Add(vt);
            }
            return pNewVertexs;
        }
        private List<ArcSe> getArcs(Eage eage,List<int> nodesindex) {
            List<ArcSe> arcSes = new List<ArcSe>();
          
            int count = nodesindex.Count();
            int eagecount = eage.vertexList.Count();
            List<Vertex> vertices = eage.vertexList;
            for (int i = 0; i < count ; i++) {//把
                int index1 = nodesindex[i];
                int index2 = nodesindex[(i + 1)%count];
                if (index2 < index1) 
                    index2 = eagecount + index2;//如果index2比index1小，那么由于eage顺时针，nodes顺时针，那么一定是走到了eage的末端，给index2直接加上count，后边有%，保证了顺序
                //而且这样出来的arc中的弧段都是可以直接顺序一一对应的。
                ArcSe arcSe = new ArcSe(vertices[index1], vertices[index2 % eagecount]);
                Eage eagetemp = new Eage();
                eagetemp.name = eage.name;
                for (int j = index1; j <= index2; j++) {//注意，这里应该是等于，因为弧段端点是重合的
                    eagetemp.AddVertex(vertices[j%eagecount]);//这里加个mod，防止越界同时可以让
                }
                arcSe.setEage(eagetemp);
                arcSe.id = i;
                arcSes.Add(arcSe);
            }
            /*int index11 = nodesindex[count - 1];
            int index22 = nodesindex[0];
            ArcSe arc = new ArcSe(vertices[index11], vertices[index22]);
            Eage temp = new Eage();
            temp.name = eage.name;
            for (int j = index11; j <= index22; j++)
            {//注意，这里应该是等于，因为弧段端点是重合的
                temp.AddVertex(vertices[j]);
            }
            arc.setEage(temp);
            arc.id = count - 1;
            arcSes.Add(arc);*/
            return arcSes;
        }
        private List<int> getCutPointsID(Eage eage,List<Vertex> vertices ) {
            //这个是找到截断Eage的弧段端点的ID用的函数
            List<Vertex> vertexList = eage.vertexList;
            int count2 = vertexList.Count();
            int count1 = vertices.Count();
            int countV = vertices.Count();
            bool[] marks = new bool[count2];//记录一下eage上所有的点哪个没有被用到，可用就是true ，用过就是false
            for (int i = 0; i < count2; i++) marks[i] = true;
            List<int> result = new List<int>();
            for (int i=0;i<count1;i++) {
                Vertex vertex1 = vertices[i];
                for (int j = 0; j < count2; j++) {
                    if (marks[j])
                    {
                        Vertex vertex2 = vertexList[j];
                        bool b = VertexEquals3D(vertex1, vertex2);
                        if (b)
                        {
                            result.Add(j);//每次找了相对应的节点，就把它加入到结果列表中
                            marks[j] = false;
                            break;
                        }
                    }
                }
            }
            return result;
        }
        private List<int> getCutPointsIDForArc(Eage eage,List<Vertex> vertices) {
            
            List<Vertex> vertexList = eage.vertexList;
            int count2 = vertexList.Count();
            int count1 = vertices.Count();
            int countV = vertices.Count();
            bool[] marks = new bool[count2];//记录一下eage上所有的点哪个没有被用到，然后就是我们要的点了
            for (int i = 0; i < count2; i++) marks[i] = true;
            List<int> result = new List<int>();
            for (int i = 0; i < count1; i++)
            {
                Vertex vertex1 = vertices[i];
                for (int j = 0; j < count2; j++)
                {
                    if (marks[j])
                    {
                        Vertex vertex2 = vertexList[j];
                        bool b = VertexEquals3D(vertex1, vertex2);
                        if (b)
                        {
                            result.Add(j);//每次找了相对应的节点，就把它加入到结果列表中
                            for (int k = 0; k <= j; k++) {//由于弧段上有重复的点，而我们给出的弧段的分割点一定是按照从小到大顺序的，所以我们要把找到后的位置的及其前面的所有位置都置为不可用，防止回头找
                                marks[k] = false;
                            }
                            break;
                        }
                    }
                }
            }
            return result;
        }
        private bool VertexEquals3D(Vertex vertex1,Vertex vertex2)
        {
            if ((Math.Abs(vertex1.x - vertex2.x) < 0.001) && (Math.Abs(vertex1.y - vertex2.y) < 0.001) && (Math.Abs(vertex1.z - vertex2.z) < 0.001))
            {
                return true;
            }
            else { return false; }
        }
        private Vertex getCenter2D(List<Vertex> vertices)
        {
            int count = vertices.Count();
            double sumx = 0,sumy = 0;
            for (int i=0; i<count; i++) {
                Vertex vertex = vertices[i];
                sumx += vertex.x;
                sumy += vertex.y;
            }
            Vertex result = new Vertex();
            result.x = sumx / count;
            result.y = sumy / count;
            return result;
        }
        private double distance(double x1,double y1,double x2,double y2) {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        private bool makeVertexListClockwise(ref List<Vertex> vertices) {
            int count = vertices.Count();
            double d = 0;
            for (int i = 1; i < count - 1; i++)
            {//把叉积相加，得到的结果如果为负，那么就是顺时针，否则是逆时针
                d = d + chaji(vertices[i - 1], vertices[i], vertices[i + 1]);
            }
            if (d < 0) return true;
            List<Vertex> daozhi = new List<Vertex>();
            for (int i = count - 1; i >= 0; i--) {
                daozhi.Add(vertices[i]);
            }
            vertices = daozhi;
            return false;
        }
        private bool makeEageClockwise(Eage eage,out Eage result) {//检查eage是不是顺时针，如果不是就返回一个逆置后的完全相同的eage
            result = new Eage();
            List<Vertex> vertices = eage.vertexList;
            int count = vertices.Count();
            double d = 0;
            for (int i = 1; i < count - 1; i++)
            {//把叉积相加，得到的结果如果为负，那么就是顺时针，否则是逆时针
                d = d + chaji(vertices[i - 1], vertices[i], vertices[i + 1]);
            }
            if (d < 0) { return true; } else {
                result = new Eage();
                result.name= eage.name;
                result.id = eage.id;
                result.belongTo = eage.belongTo;
                
                result.vertexList.Clear();
                for(int j = vertices.Count() - 1; j >= 0; j--)
                {
                    //result.vertexList.Add(eage.vertexList[j]);
                    result.vertexList.Add(vertices[j]);
                }
                return false;
            }
        }
        private double chaji(Vertex v1, Vertex v2, Vertex v3)
        {
            double x1 = v2.x - v1.x;
            double y1 = v2.y - v1.y;
            double x2 = v3.x - v2.x;
            double y2 = v3.y - v2.y;
            double result = x1 * y2 - x2 * y1;
            return result;
        }
        public BrepModel ContourHelpOntToOne(Eage eage1,Eage eage2)
        {//简简单单把ContourSingleToSingle做成一个接口
            this.eageA = eage1;
            this.eageB = eage2;

            BrepModel pBrep = ContourSingleToSingle(ref this.eageA, ref this.eageB, averageEage, densificationEage);
            return pBrep;
        }
        public BrepModel ContourHelpOntToOne(ref Eage eage1,ref Eage eage2)
        {//简简单单把ContourSingleToSingle做成一个接口
            this.eageA = eage1;
            this.eageB = eage2;

            BrepModel pBrep = ContourSingleToSingle(ref this.eageA, ref this.eageB, averageEage, densificationEage);
            eage1 = this.eageA;
            eage2 = this.eageB;
            return pBrep;
        }
        /// <summary>
        /// 主函数
        /// </summary>
        public void SolidModelBuild()
        {
            List<Contour> pConA = new List<Contour>();
            //pConA.Add(contourListA["C0"]);
            //pConA.Add(contourListA["F1"]);
            //pConA.Add(contourListA["C2"]);
            for (int i = 0; i < contourListA.Count(); i++) {
                pConA.Add(contourListA[i]);
            }
            //string temppath = @"D:\graduateGIS\temp";
            /*foreach (var por in pConA)
            {//输出边界点

                por.eageList[0].ExportEagePointToShapefile3D(temppath, por.eageList[0].name);
            }*/
            List<Contour> pConB = new List<Contour>();
            //pConB.Add(contourListB["B0"]);
            // pConB.Add(contourListB["G1"]);
            //pConB.Add(contourListB["B2"]);
            for (int i = 0; i < contourListB.Count(); i++)
            {
                pConB.Add(contourListB[i]);
            }
            /*foreach (var por in pConB)
            {//输出边界点
                por.eageList[0].ExportEagePointToShapefile3D(temppath, por.eageList[0].name);
            }*/

             

            #region 创建Voronoi图部分
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

            for (int i = 0; i < pConA.Count; i++)
            {
                #region 上下对应
                if (pConA[i].eageList.Count == pConB[i].eageList.Count)
                {

                    if (pConA[i].eageList.Count == 1)
                    {
                        //1对1创建几何体
                        Eage pEageA = pConA[i].eageList[0];
                        Eage pEageB = pConB[i].eageList[0];

                        BrepModel pBrep = ContourSingleToSingle(ref pEageA, ref pEageB, averageEage, densificationEage);
                        pBrepModelList.Add(pBrep);
                        //2插值
                        /*ContourIntePHelp pContourIntePHelper = new ContourIntePHelp(pEageA, pEageB, pBrep.mesh);
                        List<Eage> pEages = pContourIntePHelper.ContourInterPolate();

                        //List<Eage> pEages = new List<Eage>();
                        //pEages.Add(pConA[i].eageList[0]);
                        //pEages.Add(pConB[i].eageList[0]);
                        //构建模型
                        List<BrepModel> pModels = new List<BrepModel>();
                        for (int j = 0; j < pEages.Count - 1; j++)
                        {
                            Eage pEage_1 = pEages[j];
                            Eage pEage_2 = pEages[j + 1];
                            BrepModel pBrep1 = ContourSingleToSingle(ref pEage_1, ref pEage_2, false, false);
                            pModels.Add(pBrep1);
                        }

                        BrepModel pInteBrepModel = new BrepModel();
                        foreach (var vp in pModels)
                        {
                            foreach (var vpp in vp.triangleList)
                            {
                                Triangle tri = vpp as Triangle;
                                Vertex vt1 = vp.vertexTable[tri.v0] as Vertex;
                                Vertex vt2 = vp.vertexTable[tri.v1] as Vertex;
                                Vertex vt3 = vp.vertexTable[tri.v2] as Vertex;
                                pInteBrepModel.addTriangle(vt1.x, vt1.y, vt1.z, vt2.x, vt2.y, vt2.z, vt3.x, vt3.y, vt3.z);
                            }
                        }
                        pBrepModelList.Add(pInteBrepModel);*/
                    }
                }
                #endregion



                #region 左右对应
                //if (pConA[i].eageList.Count == pConB[i].eageList.Count)
                //{

                //    if (pConA[i].eageList.Count == 1)
                //    {
                //        //将点的集合保持到字典中便于查询
                //        Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(pConA[i].eageList[0].vertexList, pConB[i].eageList[0].vertexList);

                //        Eage pNewConA = new Eage();
                //        Eage pNewConB = new Eage();
                //        //0 转换维数
                //        double paraDist =0.0;
                //        double slopeK = GetParaDistance(pConA[i].eageList[0], pConB[i].eageList[0],ref paraDist);
                //        pNewConA = ConvertToEage(pConA[i].eageList[0], 0, slopeK);
                //        pNewConB = ConvertToEage(pConB[i].eageList[0], -paraDist, slopeK);
                //        pNewConA.ExportEagePointToShapefile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\ProgramData","PNEWa");
                //        pNewConB.ExportEagePointToShapefile(@"C:\Users\HakerTop\Desktop\IntrusiveRockModel\ProgramData", "PNEWb");
                //        //1 线均匀
                       

                //        if (averageEage)
                //        {
                //            pNewConA = GetAverageEage(pConA[i].eageList[0], 1f);
                //            pNewConB = GetAverageEage(pConB[i].eageList[0], 1f);
                //        }

                //        //2 线增密
                //        if (densificationEage)
                //        {
                //            DensifiPoint(ref pNewConA, ref pNewConB);
                //        }

                //        //3 构建实体
                //        //ContourOneTpOne(pConA[i].eageList[0], pConB[i].eageList[0]);
                //        ContourOneTpOne(pNewConA, pNewConB, pEageVertexDict);
                //    }
                //}
                #endregion
            }
        }

        #region 主函数
        public BrepModel ContourSingleToSingle(ref Eage pConAEage, ref Eage pConBEage, bool _averageEage, bool _densificationEage)
        {
            Eage pNewConA = new Eage();
            Eage pNewConB = new Eage();

            //1 线均匀 
            if (_averageEage)
            {
                pNewConA = GetAverageEage(pConAEage, 1f);
                pNewConB = GetAverageEage(pConBEage, 1f);
            }
            else
            {
                pNewConA = pConAEage;
                pNewConB = pConBEage;
            }

            //2 线增密
            if (_densificationEage)
            {
                DensifiPoint(ref pNewConA, ref pNewConB);
            }

            pConAEage = pNewConA;
            pConBEage = pNewConB;

            //3 将点的集合保持到字典中便于查询
            Dictionary<string, Vertex> pEageVertexDict = ConvertToDict(pNewConA.vertexList, pNewConB.vertexList);

            //4 构建实体
            BrepModel pBrep = ContourOneTpOne(pNewConA, pNewConB, pEageVertexDict);
            return pBrep;
        }


        /// <summary>
        /// 一个轮廓线只有一条边的情况
        /// </summary>
        /// <param name="pConA"></param>
        /// <param name="pConB"></param>
        public BrepModel ContourOneTpOne(Eage pConA, Eage pConB, Dictionary<string, Vertex> pEageVertexDict)
        {
           
            //获取外包矩形
            Envelope pEnvA = GetEnvelope(pConA);
            Envelope pEnvB = GetEnvelope(pConB);
            //获取外包矩形的中心点
            Vertex pCenPtA = GetCentralPoint(pEnvA);
            Vertex pCenPtB = GetCentralPoint(pEnvB);

            //计算转换参数
            double moveX = pCenPtB.x - pCenPtA.x;
            double moveY = pCenPtB.y - pCenPtA.y;
            double expandX = (pEnvB.MaxX - pEnvB.MinX) / (pEnvA.MaxX - pEnvA.MinX);
            double expandY = (pEnvB.MaxY - pEnvB.MinY) / (pEnvA.MaxY - pEnvA.MinY);

            //比较外包矩形大小
            if (GetArea(pEnvA) <= GetArea(pEnvB))
            {
                BrepModel pBrep = ConstructModelByArea(pConA, pCenPtB, pConB, pEageVertexDict, moveX, moveY, expandX, expandY);
                //pBrepModelList.Add(pBrep);
                return pBrep;
            }

            else
            {
                BrepModel pBrep = ConstructModelByArea(pConB, pCenPtA, pConA, pEageVertexDict, -moveX, -moveY, 1 / expandX, 1 / expandY);
                //pBrepModelList.Add(pBrep);
                return pBrep;
            }

        }

        /// <summary>
        /// 切开缝合法创建三维实体
        /// </summary>
        /// <param name="pConA">轮廓线A的边界</param>
        /// <param name="pCenPtB">轮廓线B的外包矩形的中心点</param>
        /// <param name="pConB">轮廓线B的边界</param>
        /// <param name="pEageVertexDict">轮廓线A与轮廓线B的边界线上点的集合</param>
        /// <param name="moveX">平移距离X</param>
        /// <param name="moveY">平移距离Y</param>
        /// <param name="expandX">X轴缩放</param>
        /// <param name="expandY">Y轴缩放</param>
        public BrepModel ConstructModelByArea(Eage pConA, Vertex pCenPtB, Eage pConB, Dictionary<string, Vertex> pEageVertexDict, double moveX, double moveY, double expandX, double expandY)
        {
            //获取变换后的边界
            Eage PNewEageA = MoveandExpand(pConA, pCenPtB, moveX, moveY, expandX, expandY);
           // PNewEageA.ExportEagePointToShapefile(@"D:\graduateGIS\temp", "pConA");
            //获取最邻近点（寻找到切口）
            Dictionary<Vertex, Vertex> pNearestPoint = GetNearVertex(PNewEageA, pConB);

            //重新排序
            Vertex[] vtsA = pNearestPoint.Keys.ToArray<Vertex>();
            Vertex vkeyA = vtsA[vtsA.Length - 1];
            List<Vertex> pListVertexA = NewSort(PNewEageA, vkeyA);
            Vertex vtsB = pNearestPoint[vkeyA];
            List<Vertex> pListVertexB = NewSort(pConB, vtsB);

            //切开轮廓线
            double da = 0.0;
            double db = 0.0;
            List<TriangleNet.Geometry.Vertex> pA = GetUnfoldPoints(pListVertexA,100,ref da);
            List<TriangleNet.Geometry.Vertex> pB = GetUnfoldPoints(pListVertexB,0,ref db);

            //重新调整上下点的对应位置
            //-------------   ---------------
            //--------        - - - - - - - -
            HandelListVertex(ref pA, ref pB, da, db);
            //pA.ExportTrianglePointToShapefile(@"D:\graduateGIS\temp", "pA");
            //pB.ExportTrianglePointToShapefile(@"D:\graduateGIS\temp", "pB");
            
            //创建三维实体
            TriangleNet.Mesh mesh = GetTriMesh(pA, pB);         
            BrepModel pBrep = GetBrepModel(mesh, pEageVertexDict);
            pBrep.mesh = mesh;

            //获取点的对应关系
            return pBrep;
        }

        /// <summary>
        /// 将边界上的点集保存到字典上便于查询
        /// </summary>
        /// <param name="pVertexListA"></param>
        /// <returns></returns>
        public Dictionary<string, Vertex> ConvertToDict(List<Vertex> pVertexListA, List<Vertex> pVertexListB)
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
        #endregion

        #region 线均匀与线增密

        public void DensifiPoint(ref Eage pNewConA, ref Eage pNewConB)
        {
            double scalen = (double)pNewConA.vertexList.Count / (double)pNewConB.vertexList.Count;
            double scalenb =(double)pNewConB.vertexList.Count / (double)pNewConA.vertexList.Count;
            
            if (pNewConA.vertexList.Count > pNewConB.vertexList.Count)
            {
                if (scalen > 1)
                {
                    pNewConB = EageDensification(pNewConB, Convert.ToInt16(scalen));
                }

            }
            else
            {
                if (scalenb > 1)
                {
                    pNewConA = EageDensification(pNewConA, Convert.ToInt16(scalenb));
                }
            }
        }

        public Eage EageDensification(Eage pEage, int n)
        {
            Eage pNewEage = new Eage();
            pNewEage.name = pEage.name;
            pNewEage.id = pEage.id;

            int count = pEage.vertexList.Count;
            for (int i = 0; i < pEage.vertexList.Count - 1; i++)
            {
                pNewEage.AddVertex(pEage.vertexList[i]);

                for (int j = 0; j < n-1; j++)
                {
                    Vertex vt = new Vertex();
                    vt.name = pEage.name + count;
                    vt.x = pEage.vertexList[i].x + ((pEage.vertexList[i + 1].x - pEage.vertexList[i].x) / (n + 1)) * (j + 1);
                    vt.y = pEage.vertexList[i].y + ((pEage.vertexList[i + 1].y - pEage.vertexList[i].y) / (n + 1)) * (j + 1);
                    vt.z = pEage.vertexList[i].z + ((pEage.vertexList[i + 1].z - pEage.vertexList[i].z) / (n + 1)) * (j + 1);
                    pNewEage.AddVertex(vt);

                    count++;
                }
                count++;

                if (i == pEage.vertexList.Count - 2)
                {
                    pNewEage.AddVertex(pEage.vertexList[i + 1]);
                }
            }


            return pNewEage;
        }

        /// <summary>
        /// 获取均匀插值后的边界
        /// </summary>
        /// <param name="pEage"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public Eage GetAverageEage(Eage pEage, float n)
        {
            double _distA = RegularEage(pEage, n);
            if (_distA != 0.0)
            {
                return AddPointToEage(pEage, _distA);
            }
            else
                return pEage;
        }

        /// <summary>
        /// 获取阈值
        /// </summary>
        /// <param name="pEage"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private double RegularEage(Eage pEage, float n)
        {
            //最小距离
            double minDistance = CommonFun.GetDistance3D(pEage.vertexList[0], pEage.vertexList[1]);
            //最大距离
            double maxDistance = minDistance;

            for (int i = 0; i < pEage.vertexList.Count - 1; i++)
            {
                double dist = CommonFun.GetDistance3D(pEage.vertexList[i], pEage.vertexList[i + 1]);

                if (dist <= minDistance)
                {
                    minDistance = dist;
                }
                else if (dist > maxDistance)
                {
                    maxDistance = dist;
                }
            }

            if (maxDistance / minDistance >= 2)
                return n * (maxDistance - minDistance) / 2;
            else
                return 0;

        }


        /// <summary>
        /// 获取阈值并添加点
        /// </summary>
        /// <param name="pEage"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public Eage AddPointToEage(Eage pEage, double _dist)
        {
            Eage pNewEage = new Eage();
            pNewEage.name = pEage.name;
            pNewEage.id = pEage.id;

            int count = pEage.vertexList.Count;
            for (int i = 0; i < pEage.vertexList.Count - 1; i++)
            {
                pNewEage.AddVertex(pEage.vertexList[i]);

                double dist = CommonFun.GetDistance3D(pEage.vertexList[i], pEage.vertexList[i + 1]);

                int n = (int)Math.Floor(dist / _dist);

                for (int j = 0; j < n; j++)
                {
                    Vertex vt = new Vertex();
                    vt.name = pEage.name + count;
                    vt.x = pEage.vertexList[i].x + ((pEage.vertexList[i + 1].x - pEage.vertexList[i].x) / (n + 1)) * (j + 1);
                    vt.y = pEage.vertexList[i].y + ((pEage.vertexList[i + 1].y - pEage.vertexList[i].y) / (n + 1)) * (j + 1);
                    vt.z = pEage.vertexList[i].z + ((pEage.vertexList[i + 1].z - pEage.vertexList[i].z) / (n + 1)) * (j + 1);
                    pNewEage.AddVertex(vt);

                    count++;
                }




                if (i == pEage.vertexList.Count - 2)
                {
                    pNewEage.AddVertex(pEage.vertexList[i + 1]);
                }
            }


            return pNewEage;
        }


        #endregion

        #region 轮廓线变换函数集合

        #region 针对竖直三维面

        /// <summary>
        /// 返回旋转后的边界
        /// </summary>
        /// <param name="pEageA"></param>
        /// <param name="dist"></param>
        /// <param name="slopeK"></param>
        /// <returns></returns>
        public Eage ConvertToEage(Eage pEageA,double dist,double slopeK)
        {
            Vertex vt = Get3DCentralPoint(pEageA);
            List<Vertex> moveVertexP = MoveEage(pEageA, vt);
            List<Vertex> rotateVertex = RotateEage(moveVertexP, slopeK);
            return GetTransformationEage(rotateVertex, dist, pEageA.name);

        }

        /// <summary>
        /// 获取两条三维剖面的距离
        /// </summary>
        /// <param name="pEageA"></param>
        /// <param name="pEageB"></param>
        public double GetParaDistance(Eage pEageA,Eage pEageB,ref double dist)
        {
            double k1 = (pEageA.vertexList[0].y - pEageA.vertexList[1].y) / (pEageA.vertexList[0].x - pEageA.vertexList[1].x);

            double k2 = (pEageB.vertexList[0].y - pEageB.vertexList[1].y) / (pEageB.vertexList[0].x - pEageB.vertexList[1].x);

            double C1 = pEageA.vertexList[0].y - k1 * pEageA.vertexList[0].x;
            double C2 = pEageB.vertexList[0].y - k1 * pEageB.vertexList[0].x;

            dist = Math.Abs(C1 - C2) / Math.Sqrt(1 + k1 * k1);

            return k1;

        }

        /// <summary>
        /// 获取三维平面的中心点坐标
        /// </summary>
        /// <param name="pEageA"></param>
        /// <param name="pEageB"></param>
        public Vertex Get3DCentralPoint(Eage pEageA)
        {
            double distX = 0;
            double distY = 0;
            double distZ = 0;

            foreach (var item in pEageA.vertexList)
            {
                distX = distX + item.x;
                distY = distY + item.y;
                distZ = distZ + item.z;
            }

            int count = pEageA.vertexList.Count;
            Vertex vt = new Vertex(distX / count, distY / count,distZ/count);

            return vt;
        }


        /// <summary>
        /// 边界上的点平移
        /// </summary>
        /// <param name="pEageA"></param>
        /// <param name="pEageB"></param>
        public List<Vertex> MoveEage(Eage pEageA,Vertex centralVertex)
        {
            List<Vertex> vertexList = new List<Vertex>();

            foreach (var vt in pEageA.vertexList)
            {

                Vertex vr = new Vertex(vt.x - centralVertex.x, vt.y - centralVertex.y,vt.z-centralVertex.z);
                vr.name = vt.name;
                vr.id = vt.id;
                vertexList.Add(vr);
            }
            return vertexList;
        }

        /// <summary>
        /// 边界上的点旋转
        /// </summary>
        /// <param name="pEageA"></param>
        /// <param name="pEageB"></param>
        public List<Vertex> RotateEage(List<Vertex> vertexs,double slopeK)
        {
            List<Vertex> vertexList = new List<Vertex>();

            double sita = Math.Atan(slopeK);

            foreach (var vt in vertexs)
            {

                Vertex vr = new Vertex(vt.x * Math.Cos(sita) + vt.y * Math.Sin(sita), vt.y * Math.Cos(sita) - vt.x * Math.Sin(sita), vt.z);
                vr.name = vt.name;
                vr.id = vt.id;
                vertexList.Add(vr);
            }
            return vertexList;
        }

        /// <summary>
        /// 返回变换后的边
        /// </summary>
        /// <param name="pEageA"></param>
        /// <param name="pEageB"></param>
        public Eage GetTransformationEage(List<Vertex> vertexs,double dist,string name)
        {
            Eage pEage = new Eage();
            pEage.name = name;

            foreach (var vt in vertexs)
            {

                Vertex vr = new Vertex(vt.x, vt.z, dist);
                vr.name = vt.name;
                pEage.AddVertex(vr);
            }
            return pEage;
        }
        #endregion

        #region 外包矩形函数
        /// <summary>
        /// 获取面的外包矩形
        /// </summary>
        /// <param name="eage"></param>
        /// <returns></returns>
        private Envelope GetEnvelope(Eage eage)
        {
            //创建面要素
            Geometry pg = new Geometry(wkbGeometryType.wkbPolygon);
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            for (int i = 0; i <= eage.vertexList.Count; i++)
            {
                if (i == eage.vertexList.Count)
                {
                    ring.AddPoint_2D(eage.vertexList[0].x, eage.vertexList[0].y);
                }
                else
                {

                    ring.AddPoint_2D(eage.vertexList[i].x, eage.vertexList[i].y);
                }
            }
            pg.AddGeometry(ring);
            //获取面的外包矩形
            Envelope pEnv = new Envelope();
            pg.GetEnvelope(pEnv);
            return pEnv;
        }

        /// <summary>
        /// 返回外包矩形的中心点
        /// </summary>
        /// <param name="pEnv"></param>
        /// <returns></returns>
        private Vertex GetCentralPoint(Envelope pEnv)
        {
            Vertex vt = new Vertex();
            vt.x = (pEnv.MaxX + pEnv.MinX) / 2;
            vt.y = (pEnv.MaxY + pEnv.MinY) / 2;

            return vt;
        }

        /// <summary>
        /// 返回外包矩形的中心点
        /// </summary>
        /// <param name="pEnv"></param>
        /// <returns></returns>
        private double GetArea(Envelope pEnv)
        {
            return (pEnv.MaxX - pEnv.MinX) * (pEnv.MaxY - pEnv.MinY);
        }

        /// <summary>
        /// 二维平移与缩放
        /// </summary>
        /// <param name="pOldEage"></param>
        /// <param name="distance"></param>
        /// <param name="expandX"></param>
        /// <param name="expandY"></param>
        /// <returns></returns>
        private Eage MoveandExpand(Eage pOldEage, Vertex centralPoint, double moveX, double moveY, double expandX, double expandY)
        {
            Eage pEage = new Eage();

            for (int i = 0; i < pOldEage.vertexList.Count; i++)
            {
                Vertex vt = new Vertex();
                Vertex vo = pOldEage.vertexList[i];
                vt.name = vo.name;
                vt.x = ((vo.x + moveX) - centralPoint.x) * expandX + centralPoint.x;
                vt.y = ((vo.y + moveY) - centralPoint.y) * expandY + centralPoint.y;
                //vt.x = (vo.x + moveX) * expandX ;
                //vt.y = (vo.y + moveY) * expandY ;
                vt.z = vo.z;
                pEage.AddVertex(vt);
            }

           // pEage.ExportEagePointToShapefile(@"D:\graduateGIS\temp", "ExpandD");

            return pEage;

        }
       


        #endregion

        #endregion

        #region 轮廓线切开并建模

        /// <summary>
        /// 寻找最近点
        /// </summary>
        /// <param name="pEageA">轮廓线A</param>
        /// <param name="pEageB">轮廓线B</param>
        /// <returns></returns>
        public Dictionary<Vertex, Vertex> GetNearVertex(Eage pEageA,Eage pEageB)
        {
            Dictionary<Vertex, Vertex> pNearestPoint = new Dictionary<Vertex, Vertex>();

            Vertex ptA = pEageA.vertexList[0];
            Vertex ptB = pEageB.vertexList[0];
            double dt = Math.Sqrt(Math.Pow(ptA.x - ptB.x, 2) + Math.Pow(ptA.y - ptB.y, 2));
            pNearestPoint.Add(ptA, ptB);

            for (int i = 0; i < pEageA.vertexList.Count; i++)
            {

                Vertex pNewPtA = pEageA.vertexList[i];
                for (int j = 0; j < pEageB.vertexList.Count; j++)
                {
                    Vertex pNewPtB = pEageB.vertexList[j];
                    double dv = Math.Sqrt(Math.Pow(pNewPtA.x - pNewPtB.x, 2) + Math.Pow(pNewPtA.y - pNewPtB.y, 2));
                    if (dv < dt)
                    {
                        dt = dv;
                        pNearestPoint[pNewPtA] = pNewPtB;
                        
                    }
                }
            }

            return pNearestPoint;
        }

        /// <summary>
        /// 重新排序
        /// </summary>
        /// <param name="PNewEageA">边</param>
        /// <param name="vkeyA">分隔点</param>
        /// <returns></returns>
        public List<Vertex> NewSort(Eage PNewEageA, Vertex vkeyA)
        {     
            List<Vertex> pListVertexA = new List<Vertex>();
            for (int i = vkeyA.id; i < PNewEageA.vertexList.Count; i++)
            {
                pListVertexA.Add(PNewEageA.vertexList[i]);
            }
            for (int i = 0; i < vkeyA.id; i++)
            {
                pListVertexA.Add(PNewEageA.vertexList[i]);
            }

            return pListVertexA;
        }

        /// <summary>
        /// 切开轮廓线
        /// </summary>
        /// <param name="pOldVertexsA"></param>
        /// <returns></returns>
        public List<TriangleNet.Geometry.Vertex> GetUnfoldPoints(List<Vertex> pOldVertexsA,double valueY,ref double dA)
        {
            List<TriangleNet.Geometry.Vertex> pNewVertexs = new List<TriangleNet.Geometry.Vertex>();

            double ds = 0;
            for (int i = 0; i < pOldVertexsA.Count; i++)
            {
                TriangleNet.Geometry.Vertex vt = new TriangleNet.Geometry.Vertex();
                vt.ID = pOldVertexsA[i].id;
                vt.NAME = pOldVertexsA[i].name;

                if (i == 0)
                {
                    vt.X = 0;
                    //vt.Y = pOldVertexsA[i].z;
                    vt.Y = valueY;
                    pNewVertexs.Add(vt);
                    continue;
                }

                //double dist = CommonFun.GetDistance3D(pOldVertexsA[i],pOldVertexsA[i-1]);
                double dist = CommonFun.GetDistance2D(pOldVertexsA[i], pOldVertexsA[i - 1]);
                ds = ds + dist;
                vt.X = ds;
                //vt.Y = pOldVertexsA[i].z;
                vt.Y = valueY;
                pNewVertexs.Add(vt);
            }

            TriangleNet.Geometry.Vertex vt_1 = new TriangleNet.Geometry.Vertex();
            vt_1.ID = pOldVertexsA[0].id;
            vt_1.NAME = pOldVertexsA[0].name;
            //vt_1.X = ds + CommonFun.GetDistance3D(pOldVertexsA[0], pOldVertexsA[pOldVertexsA.Count - 1]); 
            vt_1.X = ds + CommonFun.GetDistance2D(pOldVertexsA[0], pOldVertexsA[pOldVertexsA.Count - 1]); 
            //vt_1.Y = pOldVertexsA[0].z;
            vt_1.Y = valueY;
            pNewVertexs.Add(vt_1);

            dA = ds;
            return pNewVertexs;

        }


        /// <summary>
        /// 处理切开后的轮廓点
        /// </summary>
        /// <param name="pNewVertexA"></param>
        /// <param name="pNewVertexB"></param>
        /// <param name="da"></param>
        /// <param name="db"></param>
        public void HandelListVertex(ref List<TriangleNet.Geometry.Vertex> pNewVertexA, ref List<TriangleNet.Geometry.Vertex> pNewVertexB,double da,double db)
        {
            if (da > db)
            {
                double val = db / da;
                for (int i = 0; i < pNewVertexB.Count; i++)
                {
                    pNewVertexB[i].X= (pNewVertexB[i].X*da/db);
                }
            }
            else
            {
                 double val = da / db;
                for (int i = 0; i < pNewVertexA.Count; i++)
                {
                    pNewVertexA[i].X = (pNewVertexA[i].X * db / da);
                }
            }
        }


        /// <summary>
        /// 创建需要构建三角网的POlygon
        /// </summary>
        /// <param name="drillList"></param>
        /// <returns></returns>
        private TriangleNet.Geometry.IPolygon GetPolygon(List<TriangleNet.Geometry.Vertex> pA,List<TriangleNet.Geometry.Vertex> pB)
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

            for (int i = pB.Count - 1; i >= 0; i--)
            {
                TriangleNet.Geometry.Vertex triVertex = new TriangleNet.Geometry.Vertex(pB[i].X, pB[i].Y);
                triVertex.NAME = pB[i].NAME;
                ////vt.Label = 0;
                //vt.ID =data.Points.Count;
                data.Add(triVertex);
                ////pB[i].Label = 0;
                //pB[i].ID = data.Points.Count;
                //data.Add(pB[i]);
            }
             return data;
        }

        /// <summary>
        /// 获取三角剖分的边界线
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public TriangleNet.Geometry.Contour GetContourByTriangle(List<TriangleNet.Geometry.Vertex> pA, List<TriangleNet.Geometry.Vertex> pB)
        {
            List<TriangleNet.Geometry.Vertex> pv = new List<TriangleNet.Geometry.Vertex>();

            foreach (var vt in pA)
            {
                pv.Add(vt);
            }

            for (int i = pB.Count - 1; i >= 0; i--)
            {
                pv.Add(pB[i]);
            }
            TriangleNet.Geometry.Contour pNewCon = new TriangleNet.Geometry.Contour(pv);

            return pNewCon;

        }

        /// <summary>
        /// 三角剖分(1)
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        private TriangleNet.Mesh GetTriMesh(List<TriangleNet.Geometry.Vertex> pA, List<TriangleNet.Geometry.Vertex> pB)
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
            TriangleNet.Geometry.IPolygon input = GetPolygon(pA, pB);
            TriangleNet.Geometry.Contour con = GetContourByTriangle(pA, pB);
            //添加边界约束
            input.Add(con, false);


            TriangleNet.Mesh mesh = null;
            if (input != null)
            {
                mesh = (TriangleNet.Mesh)TriangleNet.Geometry.ExtensionMethods.Triangulate(input, options);

            }

            return mesh;
            #endregion

        }


        /// <summary>
        /// BrepModel模型构建
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <param name="pEageVertexDict"></param>
        /// <returns></returns>
        private BrepModel GetBrepModel(TriangleNet.Mesh mesh, Dictionary<string, Vertex> pEageVertexDict)
        {
            #region 模型构建

            //二维三角网
            TriMesh trimesh = new TriMesh();
            BrepModel pBrep = new BrepModel();

            foreach (var item in mesh.Triangles)
            {
                TriangleNet.Geometry.Vertex p1, p2, p3;

                TriangleNet.Topology.Triangle pTinTri = item;

                p1 = item.GetVertex(0);
                p2 = item.GetVertex(1);
                p3 = item.GetVertex(2);

                //将三角形添加到自定义的三角网中
                trimesh.AddTriangle(p1.X, p1.Y, 0, p2.X, p2.Y, 0, p3.X, p3.Y, 0);
               // trimesh.ExportTriMeshToShapfile(@"D:\graduateGIS\temp", "delaunaryA");

                Vertex pp1 = pEageVertexDict[p1.NAME];
                Vertex pp2 = pEageVertexDict[p2.NAME];
                Vertex pp3 = pEageVertexDict[p3.NAME];

                //构建边界三角网
                pBrep.addTriangle(pp1.x, pp1.y, pp1.z, pp2.x, pp2.y, pp2.z, pp3.x, pp3.y, pp3.z);
            }

            return pBrep;

            #endregion
        }

        #endregion
    }
}
