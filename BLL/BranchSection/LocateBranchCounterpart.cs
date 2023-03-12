using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.OGR;
using OSGeo.GDAL;
using OSGeo.OSR;
//using MakeTopologyForSection;

namespace ThreeDModelSystemForSection
{
    class LocateBranchCounterpart
    {
        /// <summary>
        /// Automatically locate the m:n match result
        /// </summary>
        /// <param name="section1"></param>
        /// <param name="section2"></param>
        /// <param name="spatialReference"></param>
        /// <param name="section1Pair"></param>
        /// <param name="section2Pair"></param>
        /// <param name="renumbersection1"></param>
        /// <param name="renumbersection2"></param>
        /// <param name="singlekeys"></param>
        public static void LocateCounterpart(Dictionary<int, List<Geometry>> section1, Dictionary<int, List<Geometry>> section2,SpatialReference spatialReference,
            out Dictionary<int, List<int>> section1Pair, out Dictionary<int, List<int>> section2Pair,out Dictionary<int,Geometry>renumbersection1, out Dictionary<int, Geometry> renumbersection2,out List<int>singlekeys)
        {
            section1Pair = new Dictionary<int, List<int>>();
            section2Pair = new Dictionary<int, List<int>>();
            /*
            */
            List<int> multikeys = new List<int>();//Make a note of the branch id
            singlekeys = new List<int>();//Keep track of the undisturbed strata
            Dictionary<int, List<Geometry>> section1multi = new Dictionary<int, List<Geometry>>();//Record branch layer
            Dictionary<int, List<Geometry>> section2multi = new Dictionary<int, List<Geometry>>();
            List<int> sharpen1to2 = new List<int>();//The pinch-out formation was recorded
            List<int> sharpen2to1 = new List<int>();
            List<int> section1keys = new List<int>(section1.Keys.ToArray<int>());//Generate the master key, which is used to obtain the branch layer and the pinout layer
            List<int> section2keys = new List<int>(section2.Keys.ToArray<int>());
/*            Dictionary<int, List<int>> mapsection1id = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> mapsection2id = new Dictionary<int, List<int>>();*/
            for (int i = 0; i < section1keys.Count; i++)
            {
                int id = section1keys[i];
                bool containin = section2keys.Contains<int>(id);
                if (containin == false)
                {
                    sharpen1to2.Add(id);
                    //Again, I want to add the vanishing to the unchanged result
                    singlekeys.Add(id);
                    continue;
                }
                int count1 = section1[id].Count;
                int count2 = section2[id].Count;
                if (count1 == 1 & count2 == 1)
                {//This is the normal one-to-one correspondence
                    singlekeys.Add(id);//Add once in a one-to-one correspondence
                    continue;
                }
                multikeys.Add(id);//Add to the list of finding branching strata
                section1multi.Add(id, section1[id]);
                section2multi.Add(id, section2[id]);
            }
            for (int i = 0; i < section2.Count; i++)
            {
                int id = section2keys[i];
                bool containin = section1keys.Contains<int>(id);
                if (containin == false)
                {
                    sharpen2to1.Add(id);
                    singlekeys.Add(id);
                    continue;
                }
                int count1 = section1[id].Count;
                int count2 = section2[id].Count;
                if (count1 == 1 & count2 == 1)
                {//This is the normal one-to-one correspondence, and since 11 corresponds, singlekeys have been added in the first round 
                    continue;
                }
                if (multikeys.Contains(id))
                {//That means this thing has been included in the branching strata. Skip it
                    continue;
                }
                multikeys.Add(id);//Add to the list of finding branching strata
                section1multi.Add(id, section1[id]);
                section2multi.Add(id, section2[id]);
            }
            //So far branching and pinch-out formations have been found

            Dictionary<int, int[]> pairsection1, pairsection2;
            int maxid = -1;//用一个连续的maxid是为了防止新id之间产生不必要的这个联系，起码新编id应当是不相同的。

            //这里缺一步，就是要把尖灭层给合并到它旁边层，

            Dictionary<int, Geometry> section1ReNumber = makeGeomUniqueId(section1, out pairsection1, ref maxid);
            Dictionary<int, Geometry> section2ReNumber = makeGeomUniqueId(section2, out pairsection2, ref maxid);
            //现在都重新编号完了，就，额，emmm给做一个这个拓扑.
            TopologyOfPoly topologyOfPoly1 = new TopologyOfPoly(section1ReNumber.Keys.ToList<int>(), section1ReNumber);
            topologyOfPoly1.makeTopology();
            TopologyOfPoly topologyOfPoly2 = new TopologyOfPoly(section2ReNumber.Keys.ToList<int>(), section2ReNumber);
            topologyOfPoly2.makeTopology();
            Topology topology1, topology2;
            topologyOfPoly1.exportToTopology(out topology1);
            topologyOfPoly2.exportToTopology(out topology2);

            //到此为止已经做好了一切的数据准备，可以开始配对了
            Dictionary<int, List<int>> branchpair1;
            Dictionary<int, List<int>> branchpair2;
            locateBranch(topology1, topology2, pairsection1, pairsection2, multikeys,sharpen1to2.ToArray<int>(),sharpen2to1.ToArray<int>(), out branchpair1, out branchpair2);
            section1Pair = branchpair1;
            section2Pair = branchpair2;
            renumbersection1 = section1ReNumber;
            renumbersection2 = section2ReNumber;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topology1"></param>
        /// <param name="topology2"></param>
        /// <param name="mapsection1id"></param>
        /// <param name="mapsection2id"></param>
        /// <param name="multikeys"></param>
        /// <param name="branchpair1">返回结果用，结果中一对一，一对多，一对零都通过list中元素的个数体现出来</param>
        /// <param name="branchpair2"></param>
        private static void locateBranch(Topology topology1,Topology topology2, Dictionary<int, int[]> mapsection1id, Dictionary<int, int[]> mapsection2id, List<int> multikeys,
            int[] sharpen1,int[] sharpen2,
            out Dictionary<int,List<int>> branchpair1, out Dictionary<int, List<int>> branchpair2) {
            //对于branchpair数组，定义是这样的，就是，key代表是的重新编号完了之后它本身对应面的geometry的id，就是在那个section1ReNumber、section2ReNumber
            //后边的value list int 指的是另一面中它对应的重新编号完了之后的id
            branchpair1 = new Dictionary<int, List<int>>();
            branchpair2 = new Dictionary<int, List<int>>();
            for (int i = 0; i < multikeys.Count; i++) {
                int key1 = multikeys[i];
                int[] mapid1 = mapsection1id[key1];//获取对应情况
                int[] mapid2 = mapsection2id[key1];
                //不存在一对一，对应情况有三种，
                //1对多，多对1，多对对
                int count1 = mapid1.Length;
                int count2 = mapid2.Length;

                if (count1 == 1) {//一对一的一种
                    int[] matchpoly = matchOneToMultiPoly(topology1, topology2, mapid1[0], mapid2,sharpen1,sharpen2);
                    branchpair1.Add(mapid1[0], new List<int>(matchpoly));
                    foreach (int idt in mapid2) {
                        if (matchpoly.Contains<int>(idt) == false) {
                            branchpair2.Add(idt,new List<int>());
                        }
                    }
                }
                else if (count2 == 1) {//一对一的另一种
                    int[] matchpoly = matchOneToMultiPoly(topology2, topology1, mapid2[0], mapid1,sharpen2,sharpen1);
                    branchpair2.Add(mapid2[0], new List<int>(matchpoly));
                    foreach (int idt in mapid1)
                    {
                        if (matchpoly.Contains<int>(idt) == false)
                        {
                            branchpair1.Add(idt, new List<int>());
                        }
                    }
                } 
                else { //多对多就很麻烦欸，麻烦欸
                    //目前来看多对多不能确保唯一解，至少目前是这样的，总是会有特殊情况
                    //目前的办法就是，遍历，对应最多的去掉然后再来一遍
                    //
                
                }
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topology1"></param>
        /// <param name="topology2"></param>
        /// <param name="id1"></param>
        /// <param name="id2list"></param>
        /// <returns></returns>
        private static int[] matchOneToMultiPoly(Topology topology1,Topology topology2,int id1,int[] id2list,int[] sharpen1,int[] sharpen2) {
            //输入一个拓扑关系数据，输入对应关系的list，输出对应关系。
            int[] polys1id = topology1.polys.Keys.ToArray<int>();
            int[] polys2id = topology2.polys.Keys.ToArray<int>();
            //int[] sharpen1 = arraydeletearray(polys1id, polys2id);
            //int[] sharpen2 = arraydeletearray(polys2id, polys1id);
            //List<int> ringlines = topology1.poly_arcs_Pairs[id1];
            //1,获取一个面周围的所有面
            //2.获取所有面之后，按照逆时针排序，
            //3.排序完毕，找到id2list里边所有面周围可以以端点连接或直接相邻的边界线，标记为可用
            //4.深度优先搜索，搜索出一条按照顺序外边界完全对应的环，就能确定谁对应这个面
            //5.如果搜索失败，说明谁都不挨着，就输出null
            //6.如果搜索成功，输出一个id数组，作为结果，即这个id1对应的面
            int[] oripolyring = getRingByTopo(topology1, id1);//对应1，2，输出了目标面的逆时针排列的线的id
            int[] surroundingPolys = transRingidToPolyid(oripolyring, id1, topology1);
            //下面做第三步
            Dictionary<int, bool> usefulline2 = lineusefulByPoly(topology2, id2list);
            //下面是深度优先搜索。
            //先找一个对的上的面
            int ringp = -1;
            foreach (var vk in usefulline2) {
                int lineid = vk.Key;
                int[] polys = topology2.arcs_poly_Pairs[lineid];
                if (polys.Contains<int>(surroundingPolys[0])) {//找到一开头的线
                    ringp = lineid;
                    break;
                }
            }
            int firstpointid = -1;
            int[] endpointstemp = topology2.arcs_points_Pairs[ringp];
            Geometry line = topology2.index_arcs_Pairs[ringp];
            Geometry point1 = topology2.index_points_Pairs[endpointstemp[0]];
            Geometry point2 = topology2.index_points_Pairs[endpointstemp[1]];
            Geometry geomt = topology2.polys[surroundingPolys[0]];
            bool b1 = checkleftright(line, geomt, point1);            //在左侧就true，在右侧就false
            if (b1 == false)
            {
                firstpointid = endpointstemp[0];//环上poly在这个点出发的线的右侧，就可以用这个点当出发点
            }
            else {
                firstpointid = endpointstemp[1];
            }
            //此时找好了出发点，就深度搜索就可以了。
            FindMatchRingDFSWorker DFSworkder = new FindMatchRingDFSWorker(topology2, usefulline2, surroundingPolys, sharpen1, sharpen2, firstpointid);
            //开始搜索应该找两条线交界的地方，不然的话实在是不好搜索
            DFSworkder.findMatchRingByDFS(firstpointid, 0);
            List<int> branchpolys;
            int[] surroundlineids = DFSworkder.getRingidlistByDFSresult(id2list,out branchpolys);//到这就获得了需要的环
            return branchpolys.ToArray<int>();
        }
        //很好，这个没啥用。。。。
        //写一个可用的给边改成邻接表形式的存储方式，这个很有用。
        //本质上是给边界线看作结点，顶点看作是边
        /*        private static Dictionary<int, List<int>> transTopoToGraph(Topology topology) {
                    Dictionary<int, List<int>> resultGraph = new Dictionary<int, List<int>>();
                    Dictionary<int,int[]>arcs_points= topology.arcs_points_Pairs;
                    Dictionary<int, List<int>> points_arcs = topology.points_arcs_Pairs;
                    foreach (var vk in arcs_points) {
                        int arcid = vk.Key;
                        int[] endpoints = vk.Value;
                        if (resultGraph.Keys.Contains<int>(arcid)==false) {
                            List<int> listt = new List<int>();
                        }

                    }
                }*/
/*        private static List<int> findMatchRingByDFS(Dictionary<int, int[]>) {

        }*/
        private static Dictionary<int, bool> lineusefulByPoly(Topology topology, int[] polyid) {
            //输出一个字典，让每个线都标记为是否可用
            //标准是这条线两端都是和这些地层相邻
            Dictionary<int, bool> result = new Dictionary<int, bool>();
            foreach (var vk in topology.index_arcs_Pairs) {
                int lineid = vk.Key;
                result.Add(lineid, false);
            }
            List<int> surroundlineslist = new List<int>();
            List<int> usefulpointid = new List<int>();
            for (int i = 0; i < polyid.Length; i++) {
                int polyidtemp = polyid[i];
                List<int> lines = topology.poly_arcs_Pairs[polyidtemp];
                for (int j = 0; j < lines.Count; j++) {
                    int linetemp = lines[j];
                    int[] endpointstemp = topology.arcs_points_Pairs[linetemp];
                    if (usefulpointid.Contains<int>(endpointstemp[0]) == false) {
                        usefulpointid.Add(endpointstemp[0]);
                    }
                    if (usefulpointid.Contains<int>(endpointstemp[1]) == false)
                    {
                        usefulpointid.Add(endpointstemp[1]);
                    }
                }
            }
            /*            for (int i = 0; i < usefulpointid.Count; i++) {
                            int pointidtemp = usefulpointid[i];
                            List<int> lineidlistt = topology.points_arcs_Pairs[pointidtemp];
                            foreach (int lineid in lineidlistt) {
                                if (surroundlineslist.Contains<int>(lineid) == false) {
                                    surroundlineslist.Add(lineid);
                                }
                            }
                        }*/
            //要保证这个线的两端都在这些面中间，才能保证它是可用的。
            foreach (var vk in topology.arcs_points_Pairs) {
                int[] endpoints = vk.Value;
                int lineid = vk.Key;
                if (usefulpointid.Contains<int>(endpoints[0]) && usefulpointid.Contains<int>(endpoints[1])) {
                    if (surroundlineslist.Contains<int>(lineid) == false)
                    {
                        surroundlineslist.Add(lineid);
                    }
                }
            }
            foreach (int lineid in surroundlineslist) {
                result[lineid] = true;
            }
            return result;
        }
        private static int[] getRingByTopo(Topology topology, int targetPolyid) {
            //获取它逆时针顺序的环，输出的是逆时针排列的线的id
            Geometry oripoly = topology.polys[targetPolyid];
            List<int> poly_arc = topology.poly_arcs_Pairs[targetPolyid];
            //bool[] usedarc = new bool[poly_arc.Count];
            Dictionary<int, bool> usedarc = new Dictionary<int, bool>();
            List<int> result = new List<int>();
            //for (int i = 0; i < usedarc.Length; i++) usedarc[i] = true;
            for (int i = 0; i < poly_arc.Count; i++) usedarc.Add(poly_arc[i],true);
            int arc1 = poly_arc[0];
            result.Add(arc1);
            usedarc[arc1] = false;
            int endpoint1 = topology.arcs_points_Pairs[arc1][0];
            int endpoint2 = topology.arcs_points_Pairs[arc1][1];
            int iteratepoint = -1;
            //逆时针的话，应该在左侧。
            bool p1check= checkleftright(topology.index_arcs_Pairs[arc1], oripoly, topology.index_points_Pairs[endpoint1]);
            bool p2check = checkleftright(topology.index_arcs_Pairs[arc1], oripoly, topology.index_points_Pairs[endpoint2]);
            if (p1check)
            {
                iteratepoint = endpoint2;
            }
            else if (p2check) {
                iteratepoint = endpoint1;
            }
            //遍历一下把环接上，是一定能接上的。
            for (int i = 1; i < poly_arc.Count; i++) {
                for (int j = 1; j < poly_arc.Count; j++)
                {
                    if (usedarc[poly_arc[j]])
                    {
                        int[] endpoints = topology.arcs_points_Pairs[poly_arc[j]];
                        if (endpoints[0] == iteratepoint)
                        {
                            result.Add(poly_arc[j]);
                            iteratepoint = endpoints[1];
                            usedarc[poly_arc[j]] = false;
                        }
                        if (endpoints[1] == iteratepoint)
                        {
                            result.Add(poly_arc[j]);
                            iteratepoint = endpoints[0];
                            usedarc[poly_arc[j]] = false;
                        }
                    }
                }
            }
            return result.ToArray<int>();

        }
        private static int[] transRingidToPolyid(int[] ringid,int polyid,Topology topology) {
            //把包裹着同个poly的弧段的id转变成他们外部的poly的id
            int length = ringid.Length;
            int[] result = new int[length];
            for (int i = 0; i < length; i++) {
                int[] polys = topology.arcs_poly_Pairs[ringid[i]];
                if (polys[0] == polyid) { result[i] = polys[1]; }
                if (polys[1] == polyid) { result[i] = polys[0]; }
            }
            return result;
        }
        private static bool checkleftright(Geometry line,Geometry poly,Geometry startpoint) {
            //用来检查这个面在这个线的哪一侧，点是开头
            //在左侧就true，在右侧就false
            double xp = startpoint.GetX(0);
            double yp = startpoint.GetY(0);
            List<double> linex = new List<double>();
            List<double> liney = new List<double>();
            int pointcount = line.GetPointCount();
            double x1 = line.GetX(0);
            double y1 = line.GetY(0);
            double xe = line.GetX(pointcount - 1);
            double ye = line.GetY(pointcount - 1);
            if (checkXYSame(x1, y1, xp, yp))
            {
                for (int i = 0; i < pointcount; i++)
                {
                    linex.Add(line.GetX(i));
                    liney.Add(line.GetY(i));
                }
            }
            else if (checkXYSame(xe, ye, xp, yp))
            {
                for (int i = pointcount - 1; i >= 0; i--)
                {
                    linex.Add(line.GetX(i));
                    liney.Add(line.GetY(i));
                }
            }
            else {
                String wkt;
                line.ExportToWkt(out wkt);
                Console.WriteLine("Error in" + wkt);
                return false;
            }
            Geometry ringOfPoly = poly.GetGeometryRef(0);
            int pointringcount = ringOfPoly.GetPointCount();
            double centerx = 0, centery = 0;
            for (int i = 0; i < pointringcount; i++) {
                centerx = centerx + ringOfPoly.GetX(i);
                centery = centery + ringOfPoly.GetY(i);
            }
            centerx = centerx / pointringcount;//这块实际上应该是求最小外接矩形的中心，但是就，先简要替代一下吧
            centery = centery / pointringcount;
            double s = 0;
            for (int i = 0; i < pointcount - 1; i++) {
                s = s + checkLeftRightBypoint(linex[i], liney[i], linex[i + 1], liney[i + 1], centerx, centery);
            }
            if (s >= 0) return true;
            else return false;
        }
        private static bool checkXYSame(double x1, double y1, double x2, double y2) {
            if (Math.Abs(x1-x2)<=0.00000001& Math.Abs(y1 - y2) <= 0.00000001) {
                return true;
            }
            return false;
        }
        private static double  checkLeftRightBypoint(double x1,double y1,double x2,double y2,double x3,double y3) {
            double s = (x1 - x3) * (y2 - y3) - (y1 - y3) * (x2 - x3);
            return s;
            /*如果S(A，B，C)为正数，则C在矢量AB的左侧；
              如果S(A，B，C)为负数，则C在矢量AB的右侧；
              如果S(A，B，C)为0，则C在直线AB上。*/
        }
        private static int[] arraydeletearray(int[] arr1,int[] arr2) {
            List<int> result = new List<int>();
            foreach (int t in arr1) {
                if (arr2.Contains<int>(t) == false) {
                    result.Add(t);
                }
            }
            return result.ToArray();
        }
        private static Dictionary<int, Geometry> makeGeomUniqueId(Dictionary<int,List<Geometry>>section,out Dictionary<int,int[]> pair,ref int maxid) {
            //输出一个
            pair = new Dictionary<int, int[]>();
            //int maxid = -1;//记录一下最大的id，然后从这开始可以分配新id
            if (maxid == -1)//如果没有被指定
            {
                int[] keys = section.Keys.ToArray<int>();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] > maxid)
                    {
                        maxid = keys[i];
                    }
                }
            }
            int iduseful = maxid + 1;//弄一个指针，用来指示目前顺序可用id是哪个
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            int p = 0;
            foreach (var vk in section) {
                int id = vk.Key;
                List<Geometry> ge = vk.Value;
                int count = ge.Count;
                if (count == 1)
                {
                    result.Add(id, ge[0]);
                    List<int> idlistt = new List<int>();
                    idlistt.Add(id);
                    pair.Add(id, idlistt.ToArray<int>());
                }
                else {
                    List<int> idlistt = new List<int>();
                    for (int j = 0; j < count; j++) {
                        result.Add(iduseful, ge[j]);
                        idlistt.Add(iduseful);
                        iduseful++;
                    }
                    pair.Add(id, idlistt.ToArray<int>());
                }
            }
            return result;
        }
    }
}
