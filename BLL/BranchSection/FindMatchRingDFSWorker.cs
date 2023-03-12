using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using MakeTopologyForSection;
using OSGeo.GDAL;
using OSGeo.OGR;
namespace ThreeDModelSystemForSection
{
    public class FindMatchRingDFSWorker
    {
        private const int WALL = int.MaxValue;
        private Topology topology;//Recursion data, second section diagram
        private Dictionary<int, bool> arcsuseful;//能用的线的id
        private Dictionary<int, bool> pointused,arcsused0To1, arcsused1To0;//递归调用的标记
        private Dictionary<int, List<int>> points_vectors,points_vectorsRightPoly;//用来当有向图的表，points_vectors 是有向图，points_vectorsRightPoly存储每条边上的左右poly编号
        private int[] topo1ring;
        private int[] sharpens1, sharpens2;
        private int maxcontaincount;
        private List<int> pointsinSection2;//还是应该记录点的这个id，方便做，然后再还原成环
        private List<int> lines;//记录一下这个点路过的线
        private int firstcountid;
        private List<int[]> findpointslist;
        private int startpointid;
        public FindMatchRingDFSWorker(Topology topology, Dictionary<int, bool> arcsuseful, int[] topo1ring, int[] sharpens1, int[] sharpens2,int startpointid) {
            this.topology = topology;
            this.arcsuseful = arcsuseful;
            this.startpointid = startpointid;
            maxcontaincount = int.MinValue;
            this.pointused = new Dictionary<int, bool>();
            this.arcsused0To1 = new Dictionary<int, bool>();
            this.arcsused1To0 = new Dictionary<int, bool>();
            this.lines = new List<int>();
            List<int> oritoporing = new List<int>(topo1ring);
            for (int i = 0; i < sharpens1.Length; i++) {//把环上的尖灭地层去掉
                oritoporing.Remove(sharpens1[i]);
            }
            int t = oritoporing[0];
            oritoporing.Add(t);//把头上一个放进来，防止从中间取得的有问题
            oritoporing.Add(WALL);//多加一个数防止越界
            this.topo1ring = oritoporing.ToArray<int>();
            this.sharpens1 = sharpens1;//这个是尖灭了的地层，不参与
            this.sharpens2 = sharpens2;
            foreach (var vk in topology.index_arcs_Pairs) {//初始化
                this.arcsused0To1.Add(vk.Key, true);
                this.arcsused1To0.Add(vk.Key, true);
            }
            foreach (var vk in topology.index_points_Pairs) {
                this.pointused.Add(vk.Key, true);
            }
            points_vectors = new Dictionary<int, List<int>>();
            points_vectorsRightPoly = new Dictionary<int, List<int>>();
            this.findpointslist = new List<int[]>();
            this.pointsinSection2 = new List<int>();
            makeGraph();//把有用的线给建立成一个邻接表。
        }
        private void makeGraph() {
            foreach (var vk in this.topology.arcs_points_Pairs) {
                int lineid = vk.Key;
/*                if (lineid == 1) {
                    Console.WriteLine("error");
                }*/
                if (this.arcsuseful[lineid]) {
                    int[] points =vk.Value;
                    //初始化两个点的链表
                    if (points_vectors.ContainsKey(points[0]) == false) {
                        List<int> list1 = new List<int>();
                        List<int> list2 = new List<int>();
                        points_vectors.Add(points[0], list1);
                        points_vectorsRightPoly.Add(points[0], list2);
                    }
                    if (points_vectors.ContainsKey(points[1]) == false)
                    {
                        List<int> list1 = new List<int>();
                        List<int> list2 = new List<int>();
                        points_vectors.Add(points[1], list1);
                        points_vectorsRightPoly.Add(points[1], list2);
                    }
                    Geometry line = topology.index_arcs_Pairs[lineid];
                    int polyidtemp1 = topology.arcs_poly_Pairs[lineid][0];
                    int polyidtemp2 = topology.arcs_poly_Pairs[lineid][1];
/*                    if (polyidtemp == -1) {
                        polyidtemp = topology.arcs_poly_Pairs[lineid][1];
                    }*/
                    Geometry poly0 = topology.polys[polyidtemp1];
                    Geometry poly1 = null;
                    double area0 = poly0.Area();
                    double area1 = double.MaxValue;

                    if (polyidtemp2 != -1) {//判断一下防止第二个poly是外围开放空间。 
                        poly1 = topology.polys[polyidtemp2];
                        area1 = poly1.Area();
                    }
                    if (area0 <= area1)//如果某个面的面积太大，则有可能通过它中心点控制的线左右判断会不准。
                    {
                        Geometry endpoint0 = topology.index_points_Pairs[points[0]];
                        Geometry endpoint1 = topology.index_points_Pairs[points[1]];
                        bool b1 = checkleftright(line, poly0, endpoint0);//判断一下这条线从两端出发，poly0在左还是右
                        bool b2 = checkleftright(line, poly0, endpoint1);
                        points_vectors[points[0]].Add(points[1]);//把这个边加入邻接表
                        points_vectors[points[1]].Add(points[0]);
                        if (b1 == true)//把这条线右侧的poly的id加紧邻接表对应位置
                        {
                            points_vectorsRightPoly[points[0]].Add(topology.arcs_poly_Pairs[lineid][1]);
                        }
                        else
                        {
                            points_vectorsRightPoly[points[0]].Add(topology.arcs_poly_Pairs[lineid][0]);
                        }
                        if (b2 == true)//把这条线右侧的poly的id加紧邻接表对应位置
                        {//true是说明poly0在从endpoint1出发的line的左侧，所以就添加poly1
                            points_vectorsRightPoly[points[1]].Add(topology.arcs_poly_Pairs[lineid][1]);
                        }
                        else
                        {//添加poly0
                            points_vectorsRightPoly[points[1]].Add(topology.arcs_poly_Pairs[lineid][0]);
                        }
                    }
                    else {
                        Geometry endpoint0 = topology.index_points_Pairs[points[0]];
                        Geometry endpoint1 = topology.index_points_Pairs[points[1]];
                        bool b1 = checkleftright(line, poly1, endpoint0);//判断一下这条线从两端出发，poly1在左还是右
                        bool b2 = checkleftright(line, poly1, endpoint1);//在左侧就true，在右侧就false
                        points_vectors[points[0]].Add(points[1]);//把这个边加入邻接表
                        points_vectors[points[1]].Add(points[0]);
                        if (b1 == true)//把这条线右侧的poly的id加紧邻接表对应位置
                        {
                            points_vectorsRightPoly[points[0]].Add(topology.arcs_poly_Pairs[lineid][0]);
                        }
                        else
                        {
                            points_vectorsRightPoly[points[0]].Add(topology.arcs_poly_Pairs[lineid][1]);
                        }
                        if (b2 == true)//把这条线右侧的poly的id加紧邻接表对应位置
                        {//true是说明poly0在从endpoint1出发的line的左侧，所以就添加poly1
                            points_vectorsRightPoly[points[1]].Add(topology.arcs_poly_Pairs[lineid][0]);
                        }
                        else
                        {//添加poly0
                            points_vectorsRightPoly[points[1]].Add(topology.arcs_poly_Pairs[lineid][1]);
                        }
                    }
                   //Geometry poly1 = topology.polys[topology.arcs_poly_Pairs[lineid][1]];

                }
            }
        }

        /// <summary>
        /// Recursive algorithm to find rings that correspond to all boundaries of a single poly in another graph  
        /// Enter a starting position, a matching flag, and look for it counterclockwise. You can actually make the point a normal node, which is the same as the figure, and then have an edge, go through, and use the face to its right as its weight.  
        /// Right now 
        /// </summary>
        /// <param name="nowpoint"></param>
        /// <param name="ringP"></param>
        /// <returns></returns>
        public void findMatchRingByDFS(int nowpoint, int ringP) {
            // There are two cases of recursion. One is to take a step on the graph and the ringP stays the same  

            // The other way is to go one step on the graph and add one ringP  

            // The two cases mainly depend on which one works. The one that works works  

            // There is a condition for determining the end of the recursion  

            // If you loop through the points, then end the recursion, save the search results, and think about something else, does it work if you loop halfway? Well, loop halfway, oh, it works, and loop halfway works, because it's a directed graph  

            // Does that mean it works if it's half way through? Well, half way through, oh, it works, half way through, because it's a directed graph 
            if (pointsinSection2.Contains<int>(nowpoint)&&(ringP+3)>=this.topo1ring.Length)
            {
                int indext = -1;
                for (int i = 0; i < this.pointsinSection2.Count; i++) {//定位index
                    if (nowpoint == this.pointsinSection2[i]) {
                        indext = i;
                        break;
                    }
                }

                int countt = pointsinSection2.Count;
               // int resultlength = countt - indext;//取出闭合的环
                List<int> result =pointsinSection2.GetRange( indext,pointsinSection2.Count-indext);
                List<int> polyscontain = lines.GetRange(indext, pointsinSection2.Count - indext);
                bool fullRing = true;
                foreach (int polyid in topo1ring) {
                    if (polyid == WALL) continue;
                    if (polyscontain.Contains(polyid) == false) {
                        fullRing = false;
                        break;
                    }
                }
                if(fullRing) this.findpointslist.Add(result.ToArray<int>());//闭合的环作为结果加入结果数组
                //return;
                //Since it may be a small loop on a large loop when found, do not exit. Exit uses the method of ending naturally because the entry condition cannot be found 
            }
            //进入递归，加入一个点
            this.pointsinSection2.Add(nowpoint);
            int length = this.topo1ring.Length;

            int nextringP = (ringP + 1) % length;
            int nowpoly = this.topo1ring[ringP];
            int nextpoly = this.topo1ring[nextringP];//获取下一步应该对应的poly的id
            List<int> nextpointlist = points_vectors[nowpoint];
            List<int> rightpolylist = points_vectorsRightPoly[nowpoint];
            int eagecount = nextpointlist.Count;
            for (int i = 0; i < eagecount; i++) {
                //进递归两种情况
                //出递归两种情况
                //今天到这，明天再说
                int nextpointtemp = nextpointlist[i];
                int rightpolytemp = rightpolylist[i];
                if (rightpolytemp == nowpoly) {
                    //不再环上前进，而是图上走一步
                    //递归操作具体来说 
                    //就是先更新一下pointsinSection2
                    lines.Add(rightpolytemp);
                    findMatchRingByDFS(nextpointtemp, ringP);
                    int linescount = lines.Count;
                    lines.RemoveAt(linescount - 1);
                }
                if (rightpolytemp == nextpoly) { //在环上进一步，图上也进一步
                    lines.Add(rightpolytemp);
                    findMatchRingByDFS(nextpointtemp, nextringP);
                    int linescount = lines.Count;
                    lines.RemoveAt(linescount - 1);
                }
                if (sharpens2.Contains<int>(rightpolytemp)) { //这个是环上相关的是是尖灭层，那么应该是跳过，具体怎么跳，我想想，额，就环上不前进，图上进一步吧
                    lines.Add(rightpolytemp);
                    findMatchRingByDFS(nextpointtemp, ringP);
                    int linescount = lines.Count;
                    lines.RemoveAt(linescount - 1);
                }
            }
            //如果以上三种条件都不满足，就会结束循环，开始回溯，不用管
            //递归回溯，消除增加的点
            int count = this.pointsinSection2.Count; 
            this.pointsinSection2.RemoveAt(count-1);
        }
        /// <summary>
        /// DFS得到的结果是环上的点顺序集，本函数要把点集转成线的id的列表
        /// </summary>
        /// <param name="branchSectionIds">输入在section2中的分支地层的id列表</param>
        /// <returns></returns>
        public int[] getRingidlistByDFSresult(int[] branchSectionIds,out List<int> polybranchresult) {
            List<int> ringidlist = new List<int>();
            
            List<int> ringidlisttemp = new List<int>();//记一下某个的ring
            List<int> countpoly = new List<int>();//用来记录这个圈围起来了多少
            int maxcontainpolycount = -1;
            foreach (int[] pointlistori in this.findpointslist) {
                ringidlisttemp.Clear();//清空
                List<int> temp = new List<int>(pointlistori);
                temp.Add(pointlistori[0]);
               int[] pointlist = temp.ToArray();//处理一下首尾相接省的外边再写一遍
                int pointcount = pointlist.Length;
                for (int i = 0; i < pointcount - 1; i++) {
                    int point1id = pointlist[i];
                    int point2id = pointlist[i + 1];
                    List<int> linesids = topology.points_arcs_Pairs[point1id];
                    int nowline = -1;
                    foreach (int lineid in linesids) {//找一下这个点出发的哪个线是延续出来的。
                        int[] endpoints = topology.arcs_points_Pairs[lineid];
                        if (endpoints.Contains<int>(point2id)) {
                            ringidlisttemp.Add(lineid);
                            nowline = lineid;
                            break;
                        }
                    }
                    int[] polys = topology.arcs_poly_Pairs[nowline];
                    foreach (int polyid in branchSectionIds) {
                        if (polys.Contains<int>(polyid)==true&&countpoly.Contains<int>(polyid)==false) {
                            countpoly.Add(polyid);
                            break;
                        }
                    }
                }
                int polycount = countpoly.Count;
                if (polycount > maxcontainpolycount) {
                    maxcontainpolycount = polycount;
                    ringidlist = new List<int>(ringidlisttemp.ToArray());
                }
            }
            polybranchresult = new List<int>(countpoly);
            return ringidlist.ToArray<int>();
        }

        private  bool checkleftright(Geometry line, Geometry poly, Geometry startpoint)
        {
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
            else
            {
                String wkt;
                line.ExportToWkt(out wkt);
                Console.WriteLine("Error in" + wkt);
                return false;
            }
            Geometry ringOfPoly = poly.GetGeometryRef(0);
            int pointringcount = ringOfPoly.GetPointCount();
            double centerx = 0, centery = 0;
            List<double[]> xylist = new List<double[]>();//为了求最小外接矩形，就不得不用这玩意儿
            for (int i = 0; i < pointringcount; i++)
            {
                double xt = ringOfPoly.GetX(i);
                double yt = ringOfPoly.GetY(i);
                double[] xy = { xt, yt };
                xylist.Add(xy);
                //centerx = centerx + ringOfPoly.GetX(i);
                //centery = centery + ringOfPoly.GetY(i);
            }
            //centerx = centerx / pointringcount;//这块实际上应该是求最小外接矩形的中心，但是就，先简要替代一下吧
            //centery = centery / pointringcount;
            //绷不住了，如果线太短加上多边形太大，这么搞就太麻烦了
            MinOutsourceRect minOutsource = MinOutRectBuilder.buildMinOutRect(xylist);
            centerx = minOutsource.centerx;
            centery = minOutsource.centery;
            double s = 0;
            for (int i = 0; i < pointcount - 1; i++)
            {
                s = s + checkLeftRightBypoint(linex[i], liney[i], linex[i + 1], liney[i + 1], centerx, centery);
            }
            /*            double s = 0;
                        foreach (double[] xy in xylist) {
                            for (int i = 0; i < pointcount - 1; i++)
                            {
                                s = s + checkLeftRightBypoint(linex[i], liney[i], linex[i + 1], liney[i + 1], xy[0], xy[1]);
                            }
                        }*/
            if (s >= 0) return true;
            else return false;
        }
        private  bool checkXYSame(double x1, double y1, double x2, double y2)
        {
            if (Math.Abs(x1 - x2) <= 0.00000001 & Math.Abs(y1 - y2) <= 0.00000001)
            {
                return true;
            }
            return false;
        }
        private  double checkLeftRightBypoint(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double s = (x1 - x3) * (y2 - y3) - (y1 - y3) * (x2 - x3);
            return s;
            /*如果S(A，B，C)为正数，则C在矢量AB的左侧；
              如果S(A，B，C)为负数，则C在矢量AB的右侧；
              如果S(A，B，C)为0，则C在直线AB上。*/
        }
    }
}
