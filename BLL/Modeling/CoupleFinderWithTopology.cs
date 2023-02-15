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
    /// 处理对应问题，建模模块
    /// </summary>
   public class CoupleFinderWithTopology
    {//这是一个用来对两个topology对象寻找其对应的弧段和点的程序。

        private Topology topology1;
        private Topology topology2;
        public CoupleFinderWithTopology(Topology topology1,Topology topology2) {
            this.topology1 = topology1;
            this.topology2 = topology2;
        }
        public void makeArcPairsByRing(out IndexPairs arcpairs, out IndexPairs pointpairs) {
            arcpairs = new IndexPairs();//首先把结果给初始化
            pointpairs = new IndexPairs();
            //这块就用论文里边新的这个算法来做吧
            /*(1)获取剖面A中编号为d的地层da，获取剖面B中编号同为d的地层db；
                (2)将da与剖面A中所有其他地层进行公共边界线集合La，将db与剖面B中所有其他地层进行公共边界线集合Lb；
                (3)将La中的边界线按照顺时针排列，将Lb中的边界线按照顺时针排列，同时调整La、Lb中边界线的顺序，使得其集合中第1个边界线是对应的；
                (4)顺序读取La、Lb中边界线，将其记为对应边界线存入对应集合LinePair；
                (5)遍历两剖面内所有地层，使得整个剖面上所有的线都找到一一对应关系，形成完整的对应关系集合LinePair。
            此外，考虑到只有两个边界线的地层没有顺逆时针，考虑单独处理
            */
            IndexPairs indexPairs1 = getArcsParisFromPolys(this.topology1, this.topology2);//首先根据面-面获得弧段对应关系,这个是可以最先确定的。
            arcpairs.addindexPairs(indexPairs1);
            Dictionary<int, double> areaofPoly = new Dictionary<int, double>();
/*            //这块是仅仅采用一个面来排序，这时候如果有尖灭的话，可能就不好用了
 *            foreach (int polyid in topology1.polys.Keys) {
                Geometry polyt = topology1.polys[polyid];
                double areat = polyt.GetArea();
                areaofPoly.Add(polyid, areat);
            }*/
            foreach (int polyid in topology1.polys.Keys)//选择将对应面的面积加起来，这样就排除了尖灭的干扰
            {
                Geometry polyt = topology1.polys[polyid];
                Geometry polyt2 = topology2.polys[polyid];
                double areat = polyt.GetArea()+polyt2.GetArea();
                areaofPoly.Add(polyid, areat);
            }
            Dictionary<int, double> orderareaofPoly = DictionarySort(areaofPoly);//这里是排序，因为寻找顺逆时针算法在极小的面上不稳定
            Dictionary<int, double> DictionarySort(Dictionary<int, double> dic)
            {
                var dicSort = from objDic in dic orderby objDic.Value descending select objDic;
                return dicSort.ToDictionary(pair => pair.Key, pair => pair.Value); 
            }
            List<int> orderpolyids = orderareaofPoly.Keys.ToList<int>();
            foreach (int polyid in orderpolyids)//遍历所有的面
                                                // foreach (int polyid in topology1.polys.Keys)//遍历所有的面
            {
                List<int> arcs1 = topology1.poly_arcs_Pairs[polyid];//获取他们两个的id
                List<int> arcs2 = topology2.poly_arcs_Pairs[polyid];
                if (arcs1.Count > 2)//如果这个面有三条及以上边界线，那么说明就可以用顺序方式做线的对应，毕竟拓扑关系都是相同的了
                {
                    int[] arcs1order = makearcsClockWise(arcs1.ToArray(), topology1);
                    int[] arcs2order = makearcsClockWise(arcs2.ToArray(), topology2);
                    int[] matchorderRing1, matchorderRing2;
                    bool findonly = getStartLineInRing2(arcs1order, arcs2order, topology1.arcs_poly_Pairs, topology2.arcs_poly_Pairs,
                    out matchorderRing1, out matchorderRing2);
                    int length = matchorderRing1.Length;
                    for (int i = 0; i < length; i++) {
                        int lineid1 = matchorderRing1[i];
                        int lineid2 = matchorderRing2[i];
                        if (arcpairs.indexs1.ContainsKey(lineid1) == false) {
                            arcpairs.addindexPair(lineid1, lineid2);
                        }
                    }
                    List<int> matchorder1temp = new List<int>(matchorderRing1);
                    List<int> matchorder2temp = new List<int>(matchorderRing2);
                    matchorder1temp.Add(matchorderRing1[0]);//把判断点对应的圈围起来
                    matchorder2temp.Add(matchorderRing2[0]);
                    for (int i = 0; i < length; i++) {
                        int pointidlink1 = getpointidBylineid(matchorder1temp[i], matchorder1temp[i + 1], topology1.arcs_points_Pairs);
                        int pointidlink2 = getpointidBylineid(matchorder2temp[i], matchorder2temp[i + 1], topology2.arcs_points_Pairs);
                        if (pointidlink1 != -1 & pointidlink2 != -1) {//判断一下，找到了对的连接点就加进去吧
                            if (pointpairs.indexs1.ContainsKey(pointidlink1) == false) {
                                pointpairs.addindexPair(pointidlink1, pointidlink2);
                            }
                        }
                    }
                }
                else { //这时候一个面只有两个周围的边界了，就不适用于用顺时针的方式去查询了
                    int arc1id1 = arcs1[0];//先获取到所有线的id
                    int arc1id2 = arcs1[1];
                    int arc2id1 = arcs2[0];
                    int arc2id2 = arcs2[1];
                    int[] poly1id1 = topology1.arcs_poly_Pairs[arc1id1];//拿到线两边的poly
                    int[] poly1id2 = topology1.arcs_poly_Pairs[arc1id2];
                    int[] poly2id1 = topology2.arcs_poly_Pairs[arc2id1];
                    int[] poly2id2 = topology2.arcs_poly_Pairs[arc2id2];
                    bool btt1 = compareArc_polys(poly1id1, poly2id1);
                    bool btt2 = compareArc_polys(poly2id1, poly2id2);
                    if (btt1) {
                        if (arcpairs.indexs1.ContainsKey(arc1id1) == false)
                        {
                            arcpairs.addindexPair(arc1id1, arc2id1);
                        }
                        if (arcpairs.indexs1.ContainsKey(arc1id2) == false)
                        {
                            arcpairs.addindexPair(arc1id2, arc2id2);
                        }
                    }
                    if (btt2) {
                        if (arcpairs.indexs1.ContainsKey(arc1id1) == false)
                        {
                            arcpairs.addindexPair(arc1id1, arc2id2);
                        }
                        if (arcpairs.indexs1.ContainsKey(arc1id2) == false)
                        {
                            arcpairs.addindexPair(arc1id2, arc2id1);
                        }
                    }
                }
            }
            int getpointidBylineid(int line1,int line2,Dictionary<int,int[]> arc_points) { //给两个线id，给线与点的对应关系，获取他们之间连线的pointid，如果不相连，则返回-1
                int[] endpoint1 = arc_points[line1];//拿到两条线的点的id
                int[] endpoint2 = arc_points[line2];
                if (endpoint1[0] == endpoint2[0]) {
                    return endpoint1[0];
                }
                if (endpoint1[0] == endpoint2[1])
                {
                    return endpoint1[0];
                }
                if (endpoint1[1] == endpoint2[0])
                {
                    return endpoint1[1];
                }
                if (endpoint1[1] == endpoint2[1])
                {
                    return endpoint1[1];
                }
                return -1;
            }
            int[] makearcsClockWise(int[] lineids, Topology topology) {
                int[] idorder = new int[lineids.Length];
                bool[] orderindex = new bool[lineids.Length];
                for (int i = 0; i < lineids.Length; i++) orderindex[i] = false;
                List<int> disorderidTemp = new List<int>(lineids);
                idorder[0] = lineids[0];//反正是个环，随机给一个首线段
                disorderidTemp.Remove(lineids[0]);
                for (int i = 1; i < lineids.Length; i++)
                {
                    //if (orderindex[i] == true) continue;
                    //int disorderid = disorderRing[i];
                    int preid = idorder[i - 1];
                    int[] endpoints = topology.arcs_points_Pairs[preid];
                    int endpoint1 = endpoints[0];
                    int endpoint2 = endpoints[1];
                    List<int> endpoint1touch = topology.points_arcs_Pairs[endpoint1];
                    List<int> endpoint2touch = topology.points_arcs_Pairs[endpoint2];//拿到两个端点的这个相连的线
                    int findid = -2;
                    foreach (int id in disorderidTemp)
                    {
                        if (endpoint1touch.Contains(id))
                        {
                            findid = id;
                        }
                    }
                    if (findid == -2)
                    {
                        foreach (int id in disorderidTemp)
                        {
                            if (endpoint2touch.Contains(id))
                            {
                                findid = id;
                            }
                        }
                    }
                    disorderidTemp.Remove(findid);
                    idorder[i] = findid;
                }
                List<Geometry> eagerings = new List<Geometry>();
                for (int i = 0; i < idorder.Length; i++)
                {
                    eagerings.Add(topology.index_arcs_Pairs[idorder[i]]);
                }
                // bool clockwise = checkclockwise(eagerings);//检查一下这些线的中心点是否是逆时针排列的，如果不是统一的方向，将来顺逆时针反了就不对付了
                bool clockwise = checkclockwiseStrict(eagerings);
                if (clockwise != false)
                {//如果这些线不是按照逆时针排列，就给它倒置一下，保证是逆时针排列
                    int tn = idorder.Length;
                    for (int i = 0; i < tn / 2; i++)
                    {
                        swap(ref idorder[i], ref idorder[tn - 1 - i]);
                    }
                }
                return idorder;
            }
            void swap(ref int a, ref int b)
            {
                int t = a;
                a = b;
                b = t;
            }
            bool checkclockwiseStrict(List<Geometry> eages)
            {
                //顺时针是true，逆时针是false 
                //思考了一下，输入进来的是4个顺序连接但是开头结尾不一定相连的线
                //那么应该怎么给判断它们的顺逆时针呢，
                //首先是弄成首尾相接环，存成一个数组
                //再按照下面的博客写的公式求积分
                //https://www.cnblogs.com/kyokuhuang/p/4250526.html
                double s = 0;
                int n = eages.Count;
                double x0 = eages[0].GetX(0);
                double y0 = eages[0].GetY(0);
                int countt = eages[0].GetPointCount();
                double x1 = eages[0].GetX(countt-1);
                double y1 = eages[0].GetY(countt-1);
                double x2 = eages[1].GetX(0);
                double y2 = eages[1].GetY(0);
                int countt1 = eages[1].GetPointCount();
                double x3 = eages[1].GetX(countt1 - 1);
                double y3 = eages[1].GetY(countt1 - 1);
                double dis1 = GetDistance2D(x0, y0, x2, y2);
                double dis2 = GetDistance2D(x0, y0, x3, y3);
                double dis3 = GetDistance2D(x1, y1, x2, y2);
                double dis4 = GetDistance2D(x1, y1, x3, y3);
                bool startp = false;
                //标志第一个geometry的start是0还是n-1，true是0，false是n-1,,哦不对，true是0和line2挨着，false是1和line2挨着
                if (dis1 < 0.0001 || dis2 < 0.0001) { startp = true; }
                if (dis3 < 0.0001 || dis4 < 0.0001) { startp = false; }
                List<double> xlist = new List<double>();
                List<double> ylist = new List<double>();
                double xnow = 0, ynow = 0;
                if (startp)
                {
                    xnow = x1; ynow = y1;
                }
                else
                {
                    xnow = x0;ynow = y0;
                }
                for (int i = 0; i < n; i++)
                {
                    Geometry eage = eages[i];
                    double p0x = eage.GetX(0);
                    double p0y = eage.GetY(0);
                    int countp = eage.GetPointCount();
                    double p1x = eage.GetX(countp - 1);
                    double p1y= eage.GetY(countp - 1);
                    double disp0 = GetDistance2D(p0x, p0y, xnow, ynow);
                    double dispn = GetDistance2D(p1x, p1y, xnow, ynow);
                    if (disp0 < 0.00001)
                    {
                        for (int j = 0; j < countp; j++)
                        {
                            xlist.Add(eage.GetX(j));
                            ylist.Add(eage.GetY(j));
                        }
                        xnow = p1x;
                        ynow = p1y;
                    }
                    else if(dispn<0.00001)
                    {
                        for (int j = countp - 1; j >= 0; j--)
                        {
                            xlist.Add(eage.GetX(j));
                            ylist.Add(eage.GetY(j));
                        }
                        xnow = p0x;
                        ynow = p0y;
                    }
                }
                int pointcount = xlist.Count;
                double sumd = 0;
                for (int i = 0; i < pointcount-1; i++)
                {
                   sumd += -0.5 * (ylist[i + 1] + ylist[i]) * (xlist[i + 1] - xlist[i]);
                }
                sumd += -0.5 * (ylist[0] + ylist[pointcount - 1]) * (xlist[0] - xlist[pointcount - 1]);
                if (sumd > 0) return false;
                else 
                {
                    return true;
                }
            }
            bool checkclockwise(List<Geometry> eages)
            {//顺时针是true，逆时针是false 
             //使用这个一定要保证这些几何图形是单条的线
             //通过每条线的中心相连之后是不是顺时针，判断这些边是不是顺时针构成的，
                double s = 0;
                int n = eages.Count;
                double[] x = new double[n];
                double[] y = new double[n];
                for (int i = 0; i < n; i++)
                {
                    Geometry eage = eages[i];
                    int pointcount = eage.GetPointCount();
                    double xsum = 0, ysum = 0;
                    for (int j = 0; j < pointcount; j++)
                    {
                        double x1 = eage.GetX(j);
                        double y1 = eage.GetY(j);
                        xsum += x1;
                        ysum += y1;
                    }
                    x[i] = xsum / pointcount;
                    y[i] = ysum / pointcount;
                }
                for (int i = 0; i < n - 1; i++)
                {
                    double x0 = x[i];
                    double y0 = y[i];
                    double x1 = x[i + 1];
                    double y1 = y[i + 1];
                    s = s + (x0 * y1 - y0 * x1);
                }
                s = s + (x[n - 1] * y[0] - y[n - 1] * x[0]);
                if (s < 0) return false;
                else return true;
            }
            double GetDistance2D(double x1, double y1, double x2, double y2)
            {
                return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
            }
            bool getStartLineInRing2(int[] eageRingidlist1, int[] eageRingidlist2, Dictionary<int, int[]> arc_poly1, Dictionary<int, int[]> arc_poly2, out int[] matchorderRing1, out int[] matchorderRing2)
            {
                //把两个环的起点调整成相同的，而且不要一上来就不对应
                int startmatchid = -1;
                int startmatchid2 = -1;
                //有个危险，就是开头弧段并不是唯一对应在其中的，这可咋办呢，我想，这样，首先应该验证它是不是唯一，用唯一的对应的作为这个线的起始
                bool findonly = false;
                for (int i = 0; i < eageRingidlist1.Length; i++)
                {
                    int[] polys = arc_poly1[eageRingidlist1[i]];
                    bool findsameInRing1 = false;
                    for (int j = 0; j < eageRingidlist1.Length; j++)
                        if (i != j)
                        {
                            int[] polystt = arc_poly1[eageRingidlist1[j]];
                            bool comparet = compareArc_polys(polys, polystt);
                            if (comparet)
                            {
                                findsameInRing1 = true;
                            }
                        }
                    if (findsameInRing1) continue;//如果这个第一个位置的线在图1环中有重复的，就不用它；
                    int samearccount = 0;
                    for (int j = 0; j < eageRingidlist2.Length; j++)
                    {
                        int id2 = eageRingidlist2[j];
                        int[] polys2 = arc_poly2[id2];
                        bool compare1 = compareArc_polys(polys, polys2);
                        if (compare1 == true)
                        {
                            startmatchid = i;
                            startmatchid2 = j;
                            samearccount++;
                            //break;
                        }
                    }
                    if (startmatchid != -1 && samearccount == 1)
                    {
                        findonly = true;
                        break;
                    }
                }
                if (startmatchid == -1) Console.WriteLine("程序未找到对应情况，出了大问题");
                //下面就是按照上面找的对应状态，捋顺
                matchorderRing1 = new int[eageRingidlist1.Length];
                matchorderRing2 = new int[eageRingidlist2.Length];
                int p = 0;
                for (int i = startmatchid; i < eageRingidlist1.Length; i++)
                {
                    p = i - startmatchid;
                    matchorderRing1[p] = eageRingidlist1[i];
                }

                for (int i = 0; i < startmatchid; i++)
                {
                    p++;
                    matchorderRing1[p] = eageRingidlist1[i];
                }
                p = 0;
                for (int i = startmatchid2; i < eageRingidlist2.Length; i++)
                {
                    p = i - startmatchid2;
                    matchorderRing2[p] = eageRingidlist2[i];
                }
                for (int i = 0; i < startmatchid2; i++)
                {
                    p++;
                    matchorderRing2[p] = eageRingidlist2[i];
                }
                return findonly;
            }
            bool compareArc_polys(int[] polys1, int[] polys2)
            {
                if ((polys1[0] == polys2[0] && polys1[1] == polys2[1]) || (polys1[0] == polys2[1] && polys1[1] == polys2[0]))
                {
                    return true;
                }
                return false;
            }
        }
        public void makeArcPairs(out IndexPairs arcpairs,out IndexPairs pointpairs) {//把所有的匹配做出来，
            arcpairs = new IndexPairs();//首先把结果给初始化
            pointpairs = new IndexPairs();
            IndexPairs indexPairs1 = getArcsParisFromPolys(this.topology1, this.topology2);//首先根据面-面获得弧段对应关系
            arcpairs.addindexPairs(indexPairs1);
            indexPairs1 = getPointParisFromArs(this.topology1, this.topology2, arcpairs);
            pointpairs.addindexPairs(indexPairs1);
            while (true) {
                IndexPairs indexPairs2 = getPointPairsFromPointArc(this.topology1, this.topology2, arcpairs, pointpairs);
                if (indexPairs2.indexs1.Count > 0)
                    pointpairs.addindexPairs(indexPairs2);
                else { break; }
            }
            IndexPairs indexPairs3 = getArcPairsFromPoints(this.topology1, this.topology2, arcpairs, pointpairs);
            arcpairs.addindexPairs(indexPairs3);
            IndexPairs indexPairs4 = getArcPairtsFromArcs(this.topology1, this.topology2, arcpairs, pointpairs);
            arcpairs.addindexPairs(indexPairs4);
            IndexPairs indexPairs5 = getPointParisFromArs(this.topology1, this.topology2, arcpairs);//这有问题
            pointpairs.addindexPairs(indexPairs5);
            //现在就要写一个处理点的冲突的方法
            dealsamepoint(ref pointpairs, topology1.index_points_Pairs, topology2.index_points_Pairs, topology1.points_arcs_Pairs, topology1.arcs_points_Pairs, 
                          topology2.points_arcs_Pairs, topology2.arcs_points_Pairs);
        }
        private void dealsamepoint(ref IndexPairs pointpairs,Dictionary<int,Geometry > points1,Dictionary<int,Geometry>points2,
            Dictionary<int, List<int>> point_arc1, Dictionary<int, int[]> arc_point1, Dictionary<int,List<int>>point_arc2,Dictionary<int,int[]>arc_point2) {
            //现在就要写一个处理点的冲突的方法
            //具体怎么办呢，具体怎么办
            //首先就是检测pair里边有没有
            //重复其实是可以有，因为虽然键值不可重复，但是值可重复，所以就会形成，在添加已有键值时，两个键下的内容是同一个
            //所以检测重复就是，indexpairs里边有没有不同键值内容相同，相同就是重复了，把他们都remove了，收集成一个列表
            Dictionary<int, int[]> repePair1 = new Dictionary<int, int[]>();
            Dictionary<int, int[]> repePair2 = new Dictionary<int, int[]>();
            List<int> value1list = new List<int>();
            List<int> value2list = new List<int>();
            List<int[]> crushpoints1 = new List<int[]>();//这个用来保存
            List<int[]> crushpoints2 = new List<int[]>();//这个用来保存
            foreach (var vk in pointpairs.indexs1) {
                //这只是获取index1里边的，另外还有index2里边的。
                bool listcontain = value1list.Contains(vk.Value);
                if (listcontain == false)
                {
                    value1list.Add(vk.Value);
                }
                else {
                    List<int> pairt = new List<int>();
                    foreach (var vk2 in pointpairs.indexs1) {
                        if (vk2.Value == vk.Value) {
                            pairt.Add(vk.Key);
                        }
                    }//拿到存储好的图1中的同名点
                    //图2中的同名点应该是暂时只拿到了一个
                    //考虑到这个同名点的对称特性，所以这个图2中只要是它某根线的另一个点就应该是正确的同名点
                    pairt.Add(vk.Value);
                    int temparcid = point_arc2[vk.Value][0];
                    int pointtempid1 = arc_point2[temparcid][0];
                    int pointtempid2 = arc_point2[temparcid][1];
                    if (pointtempid1 != vk.Value)
                    {
                        pairt.Add(pointtempid1);
                    }
                    else {
                        pairt.Add(pointtempid2);
                    }
                    crushpoints1.Add(pairt.ToArray<int>());
                    //这样就拿到了冲突点的id，这个length==4的数组，前两个是图1的点id，后两个是图2点id
                }
            }
            foreach (int[] pairst in crushpoints1)
            {
                //应该先把这些东西给remove了，然后再想别的
                pointpairs.indexs1.Remove(pairst[0]);
                pointpairs.indexs1.Remove(pairst[1]);
                pointpairs.indexs2.Remove(pairst[2]);
                pointpairs.indexs2.Remove(pairst[3]);
            }
            foreach (var vk in pointpairs.indexs2)
            {
                //这只是获取index2里边的，另外还有index1里边的。
                bool listcontain = value2list.Contains(vk.Value);
                if (listcontain == false)
                {
                    value2list.Add(vk.Value);
                }
                else
                {
                    List<int> pairt = new List<int>();
                    foreach (var vk2 in pointpairs.indexs2)
                    {
                        if (vk2.Value == vk.Value)
                        {
                            pairt.Add(vk.Key);
                        }
                    }//拿到存储好的图1中的同名点
                    //图2中的同名点应该是暂时只拿到了一个
                    //考虑到这个同名点的对称特性，所以这个图2中只要是它某根线的另一个点就应该是正确的同名点
                    pairt.Add(vk.Value);
                    int temparcid = point_arc1[vk.Value][0];
                    int pointtempid1 = arc_point1[temparcid][0];
                    int pointtempid2 = arc_point1[temparcid][1];
                    if (pointtempid1 != vk.Value)
                    {
                        pairt.Add(pointtempid1);
                    }
                    else
                    {
                        pairt.Add(pointtempid2);
                    }
                    crushpoints2.Add(pairt.ToArray<int>());
                    //这样就拿到了冲突点的id，这个length==4的数组，前两个是图2的点id，后两个是图1点id
                }
            }
            foreach (int[] pairst in crushpoints2)
            {
                //应该先把这些东西给remove了，然后再想别的
                pointpairs.indexs2.Remove(pairst[0]);
                pointpairs.indexs2.Remove(pairst[1]);
                pointpairs.indexs1.Remove(pairst[2]);
                pointpairs.indexs1.Remove(pairst[3]);
            }
            List<int[]> crushpointsall = new List<int[]>();
            crushpointsall.AddRange(crushpoints1);
            foreach (int[] pairst2 in crushpoints2) {
                int[] pairst1 = new int[4];
                pairst1[0] = pairst2[2];
                pairst1[1] = pairst2[3];
                pairst1[2] = pairst2[0];
                pairst1[3] = pairst2[1];
                crushpointsall.Add(pairst1);//把第二个给捋顺成第一个一样的。
            }
            //这样我们就得到了这样一个数组，就是这数组里所有的int[]都是，前两个存储着图1的冲突点，后两个存储着图2的冲突点
            foreach (int[] arr in crushpointsall) {
                int id11 = arr[0];
                int id21 = arr[1];
                int id12 = arr[2];
                int id22 = arr[3];
                Geometry point1 = points1[id11];
                Geometry point2 = points1[id21];
                Geometry point3 = points2[id12];
                Geometry point4 = points2[id22];
                //现在做的是，两个方案
                //简单的就是直接连起来，然后chaeckcross，
                //别复杂了，就简单就完事了
                //周日一天争取给调通，加油
                bool cross= checkcross(point1, point3, point2, point4);//检查p1p2 p3p4两条线是否相交
                //如果要是相交，那么实际上就应该是，p1对p4,p2对p3
                //如果不相交，那么就是p1对p3，p2对p4
                if (cross)
                {
                    pointpairs.addindexPair(id11,id22);
                    pointpairs.addindexPair(id21, id12);
                }
                else {
                    pointpairs.addindexPair(id11, id12);
                    pointpairs.addindexPair(id21, id22);
                }
            }
        }
        private IndexPairs getArcsParisFromPolys(Topology topo1,Topology topo2) {//通过面-面获得唯一的能对应的弧段   
            IndexPairs arcpairs = new IndexPairs();//初始化结果
            Dictionary<int, int[]> arc_Poly1 = topo1.arcs_poly_Pairs;//获取第一个topo的线与面关系
            Dictionary<int, int[]> arc_Poly2 = topo2.arcs_poly_Pairs;//获取第二个topo的线与面关系
            foreach (var vk in arc_Poly1) {//遍历所有的线
                int[] poly = vk.Value;//获取两侧的面
                int collectcount = 0;//收集同拓扑关系线的数量
                int id1, id2=-2;
                id1 = vk.Key;
                foreach (var vk2 in arc_Poly2) {//遍历第二个topo中的线，找出有几个相同topu关系的线
                    int[] poly2 = vk2.Value;
                    if ((poly2[0] == poly[0] && poly2[1] == poly[1]) || (poly2[1] == poly[0] && poly2[0] == poly[1])) {
                        collectcount += 1;//找到相同关系的的线
                        id2 = vk2.Key;
                    }
                }
                if (collectcount == 1) {//如果对应关系是唯一的，那么就添加到对应列表
                    arcpairs.addindexPair(id1, id2);
                }
            } 
            return arcpairs;
        }

        private IndexPairs getPointParisFromArs(Topology topo1, Topology topo2,IndexPairs arcsPairs) {//用点周围所有的弧段获取点对应关系的
            Dictionary<int, List<int>> point_arcs1 = topo1.points_arcs_Pairs;//获取所有的点到线的关系
            Dictionary<int, List<int>> point_arcs2 = topo2.points_arcs_Pairs;
            IndexPairs result = new IndexPairs();
            foreach (var vk in point_arcs1) {//首先遍历所有点
                int id1, id2;
                id1 = vk.Key;
                List<int> arcsid = vk.Value;
                int count = arcsid.Count;
                List<int> arcsid2 = new List<int>();
                bool nopair = false;
                for (int i = 0; i < count; i++) {
                    int t = arcsPairs.getindex(arcsid[i], true);//找到这个点连接的所有的线段在另一拓扑图中对应的得线段
                    if (t == int.MinValue) {//如果存在弧段没有对应的情况，就设置一个bool，跳过这个点
                        nopair = true;
                        break;
                    }
                    arcsid2.Add(t);//记录一下这个点的所有连接弧段
                }
                if (nopair == true) continue;//跳过并非所有连接的弧段都对应到另一个拓扑图中的情况
                //到这里，如果一个点连接的所有弧段均有对应的弧段，那么就去寻找另一图中对应的所有弧段围起来的点
                foreach (var vk2 in point_arcs2) {//这里已经获取了一个所有线都对应的点，那么就去另一个topo中遍历，找到它的对应
                    //arcsid2 = vk2.Value;
                    List<int> arcids = vk2.Value;
                    int count2 = arcids.Count;
                    if (count2 != arcsid2.Count) {//首先，判断一下相连的线数量，数量不对马上撤退
                        continue;
                    }
                    bool contian = true;//记录一下是不是包含应该有的线
                    for (int j = 0; j < count2; j++) {//遍历一下第二个topo中的这个点连接的所有线
                        int ttt = arcids[j];
                        if (!arcsid2.Contains(ttt)) {//看看是不是包含在了对应线列表中。如果不包含，就置为false
                            contian = false;
                        }
                    }
                    id2 = vk2.Key;
                    if (contian) {//如果都包含，就加入结果列表
                        result.addindexPair(id1, id2);
                    }
                }
            }
            return result;
        }

        private IndexPairs getPointParisFromArs(Topology topo1, Topology topo2, IndexPairs arcsPairs,IndexPairs pointsPairs)
        {//用点周围所有的弧段获取点对应关系的
            Dictionary<int, List<int>> point_arcs1 = topo1.points_arcs_Pairs;//获取所有的点到线的关系
            Dictionary<int, List<int>> point_arcs2 = topo2.points_arcs_Pairs;
            IndexPairs result = new IndexPairs();
            foreach (var vk in point_arcs1)
            {//首先遍历所有点
                int id1, id2;
                id1 = vk.Key;
                List<int> arcsid = vk.Value;
                int count = arcsid.Count;
                List<int> arcsid2 = new List<int>();
                bool nopair = false;
                for (int i = 0; i < count; i++)
                {
                    int t = arcsPairs.getindex(arcsid[i], true);//找到这个点连接的所有的线段在另一拓扑图中对应的得线段
                    if (t == int.MinValue)
                    {//如果存在弧段没有对应的情况，就设置一个bool，跳过这个点
                        nopair = true;
                        break;
                    }
                    arcsid2.Add(t);//记录一下这个点的所有连接弧段
                }
                if (nopair == true) continue;//跳过并非所有连接的弧段都对应到另一个拓扑图中的情况
                //到这里，如果一个点连接的所有弧段均有对应的弧段，那么就去寻找另一图中对应的所有弧段围起来的点
                foreach (var vk2 in point_arcs2)
                {//这里已经获取了一个所有线都对应的点，那么就去另一个topo中遍历，找到它的对应
                    //arcsid2 = vk2.Value;
                    List<int> arcids = vk2.Value;
                    int count2 = arcids.Count;
                    if (count2 != arcsid2.Count)
                    {//首先，判断一下相连的线数量，数量不对马上撤退
                        continue;
                    }
                    bool contian = true;//记录一下是不是包含应该有的线
                    for (int j = 0; j < count2; j++)
                    {//遍历一下第二个topo中的这个点连接的所有线
                        int ttt = arcids[j];
                        if (!arcsid2.Contains(ttt))
                        {//看看是不是包含在了对应线列表中。如果不包含，就置为false
                            contian = false;
                        }
                    }
                    id2 = vk2.Key;
                    if (contian)
                    {//如果都包含，就加入结果列表
                        result.addindexPair(id1, id2);
                    }
                }
            }
            return result;
        }
        private IndexPairs getPointPairsFromPointArc(Topology topo1, Topology topo2, IndexPairs arcsPairs,IndexPairs pointsPairs) {
            //怎么做呢
            //第一步，遍历所有的已经获得对应的点
            //第二步，遍历该点所有的已经获得对应的线
            //第三步，根据对应把对面的点的对应关系找到，看看是否已存在，已存在就不加入，不存在就加入
            IndexPairs result = new IndexPairs();
            Dictionary<int, int> indexpairs1 = pointsPairs.indexs1;//初始化一些需要用的数据
            Dictionary<int, List<int>> point_arc1 = topo1.points_arcs_Pairs;
            Dictionary<int, int[]> arc_point1 = topo1.arcs_points_Pairs;
            Dictionary<int, int[]> arc_point2 = topo2.arcs_points_Pairs;
            foreach (var vk in indexpairs1) {//遍历所有的找到对应关系的点
                int id1 = vk.Key;
                int id2 = vk.Value;
                List<int> linewithid1 = point_arc1[id1];
                //List<int> usefullinesid = new List<int>();
                for (int j = 0; j < linewithid1.Count; j++) {//遍历对应关系点周围的线
                    int lineid1 = linewithid1[j];
                    bool contian1 = arcsPairs.indexs1.ContainsKey(lineid1);//判断这个线有没有对应关系
                    if (contian1 == false) continue;//如果当前的线段并没有对应，那么就下一条线
                    int[] pointidwitharc1 = arc_point1[lineid1];//在这把当前这条线段的两个端点的index拿到。
                    int id1pair = pointidwitharc1[0];
                    if (id1pair == id1) id1pair = pointidwitharc1[1];//判断一下，毕竟是根据点查询到的线，肯定有一个id是来的时候的点
                    int lineid2 = arcsPairs.getindex(lineid1, true);//查询获取到弧段对应的弧段的index
                    int[] pointidwitharc2 = arc_point2[lineid2];
                    int id2pair = pointidwitharc2[0];//把id2pair给赋予正确的值
                    if (id2pair == id2) id2pair = pointidwitharc2[1];
                    if(!pointsPairs.indexs1.ContainsKey(id1pair)) result.addindexPair(id1pair, id2pair);//判断一下，这个id1pair，也就是第一张图中的这个点，并不存在于点的对应关系中，那么才能把它加入
                }
            }

            return result;
        }
        private IndexPairs getArcPairsFromPoints(Topology topo1, Topology topo2, IndexPairs arcsPairs, IndexPairs pointsPairs) {
            //这个是通过一条线两个端点已经做好了对应，那么就认为它肯定也可以做好对应
            //第一步，遍历所有线
            //第二步，检查线的两端点是不是有对应点
            //第三步，如果有，那么久对应起来，如果没有，就算了
            IndexPairs result = new IndexPairs();
            Dictionary<int, int[]> arc_point1 = topo1.arcs_points_Pairs;//初始化需要用到的数据
            Dictionary<int, int[]> arc_point2 = topo2.arcs_points_Pairs;
            Dictionary<int, int> arcindex1 = arcsPairs.indexs1;
            Dictionary<int, int> pointindex1 = pointsPairs.indexs1;
            foreach (var vk in arc_point1) {//遍历所有的线，以及获取其两个端点
                int arcid1 = vk.Key;
                int[] points1 = vk.Value;
                if (arcindex1.ContainsKey(arcid1)) continue;//如果这个弧段已经被记录了对应的弧段，那么就跳过它
                int arcid2 = -2;
                if (pointindex1.ContainsKey(points1[0]) && pointindex1.ContainsKey(points1[1])) { //判断两个端点是不是都找到了对应的点，都找到了，就继续往下做
                    int pid1, pid2;
                    pid1 = pointindex1[points1[0]];
                    pid2 = pointindex1[points1[1]];
                    foreach (var vk2 in arc_point2) {//遍历第二个弧段-点表
                        int[] points2 = vk2.Value;
                        if ((points2[0] == pid1 && points2[1] == pid2) || (points2[1] == pid1 && points2[0] == pid2)) {//在这找到对应的弧段，也就是这两点对应的弧段，然后跳出查找的循环。
                            arcid2 = vk2.Key;
                            break;
                        }
                    }
                    result.addindexPair(arcid1, arcid2);//既然找到了对应，那么就添加到结果列表
                }
            }

            return result;
        }
        private IndexPairs getArcPairtsFromArcs(Topology topo1, Topology topo2, IndexPairs arcsPairs, IndexPairs pointsPairs) {
            //可以通过一条线两端点的其他所有线都确定了，那么这条线也应该是确定的
            //第一步，找到所有的没有对应的线
            //第二步，对于一个没有对应的线，获得它两个端点
            //第三步，对于他两个端点，获取所有的除这个线之外的所有的线
            //第四步，找出这些线的对应线
            //第五步，根据对应线，找到端点列表
            //第六步，根据端点列表，找出能连在一起的组合
            //第七步，这个组合对应的弧段，就是对应的
            IndexPairs result = new IndexPairs();
            Dictionary<int, int[]> arc_point1 = topo1.arcs_points_Pairs;
            Dictionary<int, List<int>> point_arc1 = topo1.points_arcs_Pairs;
            Dictionary<int, int[]> arc_point2 = topo2.arcs_points_Pairs;
            Dictionary<int, List<int>> point_arc2 = topo2.points_arcs_Pairs;
            Dictionary<int, int> arcindex1 = arcsPairs.indexs1;
            Dictionary<int, int> pointindex1 = pointsPairs.indexs1;
            foreach (var vk in arc_point1) {//遍历线与点的表，是为了找到目前没有配对的线
                int arcid1 = vk.Key;
                if (arcsPairs.indexs1.ContainsKey(arcid1)) continue;//如果这个弧段已经在arcsPairs里了（配对成功了），那么就跳过它
                int[] points1 = vk.Value;
                List<int> p10arcs = point_arc1[points1[0]];//获取这个线两个端点相连的弧段列表
                List<int> p11arcs = point_arc1[points1[1]];
                List<int> p10arcspair = new List<int>();//初始化两端点弧段列表对应表
                List<int> p11arcspair = new List<int>();
                bool fullpair = true;
                for (int j = 0; j < p10arcs.Count; j++) {//遍历点的相连弧段表，找到其所有的对应弧段
                    if (p10arcs[j] != arcid1) {
                        if (arcindex1.ContainsKey(p10arcs[j]))
                            p10arcspair.Add(arcindex1[p10arcs[j]]);
                        else fullpair = false;
                    }
                }
                for (int j = 0; j < p11arcs.Count; j++)
                {
                    if (p11arcs[j] != arcid1)
                    {
                        if (arcindex1.ContainsKey(p11arcs[j]))
                            p11arcspair.Add(arcindex1[p11arcs[j]]);
                        else fullpair = false;
                    }
                }//到这里就获取了两个点的所有相连线段的对应弧段了
                List<int> point0idsinTopo2 = new List<int>();//这两个是用来记录找到的对应的弧段的
                List<int> point1idsinTopo2 = new List<int>();
                foreach (var vk2 in point_arc2) {//遍历topo2中所有的点，看看哪个点连接的线段包含对应的，加入列表
                    int pid = vk2.Key;
                    List<int> arcsfromp = vk2.Value;
                    bool mark = true;
                    for (int j = 0; j < p10arcspair.Count; j++) {
                        if (!arcsfromp.Contains(p10arcspair[j])) {
                            mark = false;
                            break;
                        }
                    }
                    if (mark == true) point0idsinTopo2.Add(pid);
                    mark = true;
                    for (int j = 0; j < p11arcspair.Count; j++)
                    {
                        if (!arcsfromp.Contains(p11arcspair[j]))
                        {
                            mark = false;
                            break;
                        }
                    }
                    if (mark == true) point1idsinTopo2.Add(pid);//这样就获得了对应线能找到的对应的点
                }
                //下面的任务是，把point0inTopo2和point1inTopo2中所有点遍历做匹配，找到能连起来的弧段的id，就是结果了。
                foreach (int nid1 in point0idsinTopo2) {
                    foreach (int nid2 in point1idsinTopo2) {
                        foreach (var t in arc_point2) {
                            int[] endpoints = t.Value;
                            if ((endpoints[0] == nid1 && endpoints[1] == nid2) || (endpoints[1] == nid1 && endpoints[0] == nid2)) {
                                result.addindexPair(arcid1, t.Key);
                                goto Found;//找到之后就跳出循环
                            }
                        
                        }
                    }
                }
            Found:;
            }

            return result;
        }
        private IndexPairs getArcPairtsFromArcsSolveSamearc(Topology topo1, Topology topo2, IndexPairs arcsPairs, IndexPairs pointsPairs)
        {
            //可以通过一条线两端点的其他所有线都确定了，那么这条线也应该是确定的
            //第一步，找到所有的没有对应的线
            //第二步，对于一个没有对应的线，获得它两个端点
            //第三步，对于他两个端点，获取所有的除这个线之外的所有的线
            //第四步，找出这些线的对应线
            //第五步，根据对应线，找到端点列表
            //第六步，根据端点列表，找出能连在一起的组合
            //第七步，这个组合对应的弧段，就是对应的
            IndexPairs result = new IndexPairs();
            Dictionary<int, int[]> arc_point1 = topo1.arcs_points_Pairs;
            Dictionary<int, List<int>> point_arc1 = topo1.points_arcs_Pairs;
            Dictionary<int, int[]> arc_point2 = topo2.arcs_points_Pairs;
            Dictionary<int, List<int>> point_arc2 = topo2.points_arcs_Pairs;
            Dictionary<int, int> arcindex1 = arcsPairs.indexs1;
            Dictionary<int, int> pointindex1 = pointsPairs.indexs1;
            foreach (var vk in arc_point1)
            {//遍历线与点的表，是为了找到目前没有配对的线
                int arcid1 = vk.Key;
                if (arcsPairs.indexs1.ContainsKey(arcid1)) continue;//如果这个弧段已经在arcsPairs里了（配对成功了），那么就跳过它
                int[] points1 = vk.Value;
                List<int> p10arcs = point_arc1[points1[0]];//获取这个线两个端点相连的弧段列表
                List<int> p11arcs = point_arc1[points1[1]];
                List<int> p10arcspair = new List<int>();//初始化两端点弧段列表对应表
                List<int> p11arcspair = new List<int>();
                bool fullpair = true;
                for (int j = 0; j < p10arcs.Count; j++)
                {//遍历点的相连弧段表，找到其所有的对应弧段
                    if (p10arcs[j] != arcid1)
                    {
                        if (arcindex1.ContainsKey(p10arcs[j]))
                            p10arcspair.Add(arcindex1[p10arcs[j]]);
                        else fullpair = false;
                    }
                }
                for (int j = 0; j < p11arcs.Count; j++)
                {
                    if (p11arcs[j] != arcid1)
                    {
                        if (arcindex1.ContainsKey(p11arcs[j]))
                            p11arcspair.Add(arcindex1[p11arcs[j]]);
                        else fullpair = false;
                    }
                }//到这里就获取了两个点的所有相连线段的对应弧段了
                List<int> point0idsinTopo2 = new List<int>();//这两个是用来记录找到的对应的弧段的
                List<int> point1idsinTopo2 = new List<int>();
                foreach (var vk2 in point_arc2)
                {//遍历topo2中所有的点，看看哪个点连接的线段包含对应的，加入列表
                    int pid = vk2.Key;
                    List<int> arcsfromp = vk2.Value;
                    bool mark = true;
                    for (int j = 0; j < p10arcspair.Count; j++)
                    {
                        if (!arcsfromp.Contains(p10arcspair[j]))
                        {
                            mark = false;
                            break;
                        }
                    }
                    if (mark == true) point0idsinTopo2.Add(pid);
                    mark = true;
                    for (int j = 0; j < p11arcspair.Count; j++)
                    {
                        if (!arcsfromp.Contains(p11arcspair[j]))
                        {
                            mark = false;
                            break;
                        }
                    }
                    if (mark == true) point1idsinTopo2.Add(pid);//这样就获得了对应线能找到的对应的点
                }
                //下面的任务是，把point0inTopo2和point1inTopo2中所有点遍历做匹配，找到能连起来的弧段的id，就是结果了。
                List<int> pairtemp = new List<int>();
                foreach (int nid1 in point0idsinTopo2)
                {
                    foreach (int nid2 in point1idsinTopo2)
                    {
                        foreach (var t in arc_point2)
                        {
                            int[] endpoints = t.Value;
                            if ((endpoints[0] == nid1 && endpoints[1] == nid2) || (endpoints[1] == nid1 && endpoints[0] == nid2))
                            {
                                pairtemp.Add(t.Key);
                                
                               // goto Found;不要gotofound，这样把所有的都找到，然后好进行处理
                            }

                        }
                    }
                }
                //Found:;
                //现在就到了特殊处理的时间了
                //目前要处理的问题就是，这种，在
                //       ___________
                //      /           \
                //      ———————
                //     |             | 这一行两根线的拓扑是一样的，自动对应会起冲突
                //      ———————
                //     \             /
                //      ____________
                //怎么处理呢，主要就是先找到混淆的，然后根据各自四个点连成的多边形的中线左右位置判断谁和谁对应
                //方法就是各图的中心线左右判断哪个对应哪个
                //因为一般不会有180度的翻转对应，左右判断应该可行
                int countp = pairtemp.Count;
                int arcid2=-1;
                bool have2 = false;
                if (countp == 2) {
                    //如果多于一个对应，那么就需要处理一下冲突了
                    //第一步，找到冲突的部分，也就是
                    //第二步，通过一种办法处理冲突

                    //找到冲突
                    #region 这块内容就是专门用来找到在图1中与arcid1混淆冲突的另一个弧段
                    List<int> point0idsinTopo1 = new List<int>();//这两个是用来记录找到的对应的弧段的
                    List<int> point1idsinTopo1 = new List<int>();
                    //下面这个循环用来找到原来和
                    foreach (var vk2 in point_arc1)
                    {//遍历topo2中所有的点，看看哪个点连接的线段包含对应的，加入列表
                        int pid = vk2.Key;
                        List<int> arcsfromp = vk2.Value;
                        bool mark = true;
                        //这个p10arcs p11arcs是那个原来第一个图中的这个弧段
                        for (int j = 0; j < p10arcs.Count; j++)
                        {
                            if (!arcsfromp.Contains(p10arcs[j]))
                            {
                                mark = false;
                                break;
                            }
                        }
                        if (mark == true) point0idsinTopo1.Add(pid);
                        mark = true;
                        for (int j = 0; j < p11arcs.Count; j++)
                        {
                            if (!arcsfromp.Contains(p11arcs[j]))
                            {
                                mark = false;
                                break;
                            }
                        }
                        if (mark == true) point1idsinTopo1.Add(pid);//这样就获得了对应线能找到的对应的点
                    }
                    List<int> pairtemp2 = new List<int>();
                    
                    foreach (int nid1 in point0idsinTopo2)
                    {
                        foreach (int nid2 in point1idsinTopo2)
                        {
                            foreach (var t in arc_point2)
                            {
                                int[] endpoints = t.Value;
                                if (t.Key != arcid1&&((endpoints[0] == nid1 && endpoints[1] == nid2) || (endpoints[1] == nid1 && endpoints[0] == nid2)))
                                {
                                    arcid2 = t.Key;//找到原第一个图中与其对称的另一个弧段
                                    //pairtemp2.Add(t.Key);
                                    // goto Found;不要gotofound，这样把所有的都找到，然后好进行处理
                                    goto Found2;
                                }
                            }
                        }
                    }
                Found2:;
                    #endregion  

                    have2 = true;
                    //现在的问题就是，arcid1 arcid2 与pairtemp[0] pairtemp[1]怎么对应问题
                    //首先取出四个id对应的这个geometry，
                    //然后找到正确的连线，
                    //求到两个连线的中点
                    //中点连线，然后求出两个geometry各自在这个中线的哪边，同一边的线的id就对应上就对了
                    Geometry arc1m1, arc2m1, arc1m2, arc2m2;
                    Dictionary<int, Geometry> m1 = topology1.index_arcs_Pairs;
                    Dictionary<int, Geometry> m2 = topology2.index_arcs_Pairs;
                    arc1m1 = m1[arcid1];
                    arc2m1 = m1[arcid2];
                    arc1m2 = m2[pairtemp[0]];
                    arc2m2 = m2[pairtemp[1]];
                    int[] arc1m1points = topology1.arcs_points_Pairs[arcid1];
                    int[] arc2m1points = topology1.arcs_points_Pairs[arcid2];
                    int[] arc1m2points = topology2.arcs_points_Pairs[pairtemp[0]];
                    int[] arc2m2points = topology2.arcs_points_Pairs[pairtemp[1]];

                    Dictionary<int,int[]> arcpoint1= topology1.arcs_points_Pairs;
                    Dictionary<int, int[]> arcpoint2 = topology2.arcs_points_Pairs;
                    bool lineori1 = getLeftorRight(topology1.index_points_Pairs, arcpoint1, arc1m1points, arc2m1points);
                    bool lineori2 = getLeftorRight(topology2.index_points_Pairs, arcpoint2, arc1m2points, arc2m2points);
                    if (lineori1 != lineori2) {
                        int t = pairtemp[0];
                        pairtemp[0] = pairtemp[1];
                        pairtemp[1] = t;
                    }
                }

                result.addindexPair(arcid1, pairtemp[0]);
                if (have2 == true) {
                    result.addindexPair(arcid2, pairtemp[1]);
                }
            }

            return result;
        }
        public bool getLeftorRight(Dictionary<int,Geometry>points,Dictionary<int,int[]>arcpoints,int[] arc1points,int[] arc2points) {
            bool result = false;
            //true就是第一个在左第二个在右，false就是反过来，第一个在右第二个在左
            int id11 = arc1points[0];
            int id12 = arc2points[0];
            int id22 = arc2points[1];
            bool link11 = false;
            bool link12 = false;
            foreach (var vk in arcpoints) {
                int[] linepoint = vk.Value;
                if ((linepoint[0] == id11 && linepoint[1] == id12) || (linepoint[1] == id11 && linepoint[0] == id12)) {
                    link11 = true;
                }
                if ((linepoint[0] == id11 && linepoint[1] == id22) || (linepoint[1] == id11 && linepoint[0] == id22))
                {
                    link12 = true;
                }
            }
            //找一下他们连不连的上,改到正确的位置
            if (link12) { swapint(ref arc2points[0], ref arc2points[1]); }
            Geometry p11, p21, p12, p22;
            p11 = points[arc1points[0]];
            p21 = points[arc1points[1]];
            p12 = points[arc2points[0]];
            p22 = points[arc2points[1]];
            double midx1 = (p11.GetX(0) + p12.GetX(0)) / 2;
            double midy1 = (p11.GetY(0) + p12.GetY(0)) / 2;
            double midx2 = (p21.GetX(0) + p22.GetX(0)) / 2;
            double midy2 = (p21.GetY(0) + p22.GetY(0)) / 2;
            if (midy2 > midy1) {
                swapdouble(ref midx1, ref midx2);
                swapdouble(ref midy1, ref midy2);
            }
            //这样就拿到了中线
            //然后就是求两点的中点
            double line1midx = (p11.GetX(0) + p21.GetX(0)) / 2;
            double line1midy = (p11.GetY(0) + p21.GetY(0)) / 2;
            double line2midx = (p12.GetX(0) + p22.GetX(0)) / 2;
            double line2midy = (p12.GetY(0) + p22.GetY(0)) / 2;
            double ori1 = ori(midx1, midy1, midx2, midy2, line1midx, line1midy);
            double ori2 = ori(midx1, midy1, midx2, midy2, line2midx, line2midy);
            if (ori1 >= 0) { result = true; } else { result = false; }
            return result;
        }
        private void swapint(ref int a,ref int b) {
            int t = b;
            b = a;
            a = t;
        }
        private void swapdouble(ref double a, ref double b)
        {
            double t = b;
            b = a;
            a = t;
        }
         public   class temppoint {
            //这个是用来改代码用的一个内部类。
            public double x;
            public double y;
            public temppoint(double x,double y) {
                this.x = x;
                this.y = y;
            }
        }
        private bool checkcross(Geometry pg1, Geometry pg2, Geometry pg3, Geometry pg4)
        {//检查p1p2 p3p4两条线是否相交
            temppoint p1 = new temppoint(pg1.GetX(0), pg1.GetY(0));
            temppoint p2 = new temppoint(pg2.GetX(0), pg2.GetY(0));
            temppoint p3 = new temppoint(pg3.GetX(0), pg3.GetY(0));
            temppoint p4 = new temppoint(pg4.GetX(0), pg4.GetY(0));
            if (Math.Max(p3.x, p4.x) < Math.Min(p1.x, p2.x) || Math.Max(p1.x, p2.x) < Math.Min(p3.x, p4.x) || Math.Max(p3.y, p4.y) < Math.Min(p1.y, p2.y) || Math.Max(p1.y, p2.y) < Math.Min(p3.y, p4.y))
            {
                return false;
            }
            if (ori(p1.x,p1.y ,p4.x,p4.y, p3.x,p3.y) * ori(p2.x,p2.y, p4.x,p4.y, p3.x,p3.y) <= 0 && ori(p3.x,p3.y, p1.x,p1.y, p2.x,p2.y) * ori(p4.x,p4.y, p1.x,p1.y, p2.x,p2.y) <= 0)
            {
                return true;
            }
            else return false;
        }
        private double ori(double p1x, double p1y, double p2x, double p2y, double p3x, double p3y)
        {//求 p3p1×p2p1
            /*
             令矢量的起点为A，终点为B，判断的点为C， 
            如果S（A，B，C）为正数，则C在矢量AB的左侧； 
            如果S（A，B，C）为负数，则C在矢量AB的右侧； 
            如果S（A，B，C）为0，则C在直线AB上。*/
            double dx31 = p1x - p3x;
            double dy31 = p1y - p3y;
            double dx32 = p2x - p3x;
            double dy32 = p2y - p3y;
            return crossj(dx31, dy31, dx32, dy32);
        }
        private double crossj(double x1, double y1, double x2, double y2)
        {
            return x1 * y2 - x2 * y1;
        }
    }
}
