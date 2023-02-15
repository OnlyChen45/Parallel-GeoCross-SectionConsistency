using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using MathNet.Numerics.LinearAlgebra;
namespace ThreeDModelSystemForSection
{/// <summary>
 /// 处理尖灭地层生成对应虚拟地层的代码，业务层
 /// </summary>
    public class CreateMissBufferGeater3
    {//这个类，主要是从一个层出发，找到向另一个层尖灭的地层
        private Topology topology1, topology2;
        private List<int> idlist1, idlist2;
        public List<int> sharpen3,sharpen2;
        public CreateMissBufferGeater3(Topology topo1, Topology topo2, List<int> idl1, List<int> idl2) {
            this.topology1 = topo1;
            this.topology2 = topo2;
            this.idlist1 = idl1;
            this.idlist2 = idl2;
            List<int> missidlist = delListValue(idl1, idl2);
            
            Dictionary<int, List<int>> polys_Arc = topo1.poly_arcs_Pairs;
            this.sharpen2 = new List<int>();
            this.sharpen3 = new List<int>();
            foreach (int id in missidlist) {//区分一下三个的和两个的，两个的要单独做
                List<int> temparcs = polys_Arc[id];
                int count = temparcs.Count;
                if (count >= 3)
                {
                    this.sharpen3.Add(id);
                }
                else {
                    this.sharpen2.Add(id);
                }
            }
        }
        public Geometry getBoundaryByLinesDic(Dictionary<int,Geometry> arcs,Dictionary<int, int[]> arc_poly,int polyid=-1) {
            //List<Geometry> boundaryarcs = new List<Geometry>();
            Geometry edges = new Geometry(wkbGeometryType.wkbMultiLineString);
            foreach (var vk in arc_poly) {
                int[] polyids = vk.Value;
                int id1 = polyids[0];
                int id2 = polyids[1];
                if (id1 == polyid || id2 == polyid) {
                    //boundaryarcs.Add(arcs[vk.Key]);
                    edges.AddGeometry(arcs[vk.Key]);
                }
            }     
           return  Ogr.BuildPolygonFromEdges(edges,10,1,10);
        }
        public Geometry unionDicgeom(Dictionary<int, Geometry> polys)
        {
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var vk in polys) {
                result = result.Union(vk.Value);
            }
            return result;
        }
        public Dictionary<int, List<Geometry>> createArasForMiss(int statlong) {
            Dictionary<int, List<Geometry>> result = new Dictionary<int, List<Geometry>>();
            //首先应该遍历所有的含有三个地层的尖灭地层
            Dictionary<int, List<int>> polys_Arc1 = topology1.poly_arcs_Pairs;
            Dictionary<int, List<int>> polys_Arc2 = topology2.poly_arcs_Pairs;
            Dictionary<int, int[]> arc_poly1 = topology1.arcs_poly_Pairs;
            Dictionary<int, int[]> arc_poly2 = topology2.arcs_poly_Pairs;
            Dictionary<int, int[]> arc_point1 = topology1.arcs_points_Pairs;
            Dictionary<int, int[]> arc_point2 = topology2.arcs_points_Pairs;
            Dictionary<int, Geometry> arcindex2 = topology2.index_arcs_Pairs;
            Dictionary<int, Geometry> pointindex2 = topology2.index_points_Pairs;
            Dictionary<int, Geometry> polyindex1 = topology1.polys;
            Dictionary<int, Geometry> polyindex2 = topology2.polys;
            Dictionary<int, List<int>> point_arc1 = topology1.points_arcs_Pairs;
            Dictionary<int, List<int>> point_arc2 = topology2.points_arcs_Pairs;
            foreach (int id in this.sharpen3) {//遍历那个相邻三个地层的尖灭地层
                List<int> neighborPolys = new List<int>();//存储当前的几何图形周围的polygon
                List<int> arcs1 = polys_Arc1[id];
                foreach (int arcid in arcs1) {
                    int[] polypair = arc_poly1[arcid];
                    int anotherpolyid = polypair[0];
                    if (anotherpolyid == id) {
                        anotherpolyid = polypair[1];
                    }
                    neighborPolys.Add(anotherpolyid);//到此为止就获取了当前这个尖灭地层的周围的面了
                }
                int count2 = neighborPolys.Count;
                List<int[]> combinlist = new List<int[]>();//生成一下排列组合
                for (int i = 0; i < count2 - 1; i++) {
                    
                    for (int j = i + 1; j < count2; j++) {
                        int[] temp = new int[2];
                        temp[0] = neighborPolys[i];
                        temp[1] = neighborPolys[j];
                        combinlist.Add(temp);
                    }
                }
                Dictionary<int,int[]> existin2 = new Dictionary<int, int[]>();
                // List<int[]> existin1 = new List<int[]>();

                foreach (var vk2 in combinlist)
                {

                    //混淆问题是怎么一回事呢，就是说，一个图中不仅仅存在一个对应的，而是存在好多对应的。那就改改改改改
                    //目前的实例里边还是存在一个问题就是，emmm，这个两个混淆线段实际上它们都有一个能够适应的点，那咋办呢。咋办呢。
                    //两个方案嘛，两个方案
                    bool findedexist = false;
                    List<int> crushlineidsIn2 = new List<int>();
                    foreach (var vk in arc_poly2) {//这下获取了在第二个图里边有的（方式就是和排列组合比较，因为排除了二夹一的情况，所以就比较好）
                    int[] polyt = vk.Value;
       
                        if (equalforintPair(polyt, vk2)) {//就是在这，会出现混淆问题
                                                          //就是这！！！！ 混淆问题，而且记录了在图2中的线的id，草，草他妈的，真难搞啊
                            crushlineidsIn2.Add(vk.Key);
                            // existin2.Add(vk.Key,vk.Value);//找到存在的就退出,并且记录它的index和两侧
                            // break;
                            findedexist = true;
                        } 
                    }
                    if (findedexist)//首先确保找到了正确位置
                    {
                        if (crushlineidsIn2.Count == 1)
                        {
                            existin2.Add(crushlineidsIn2[0], arc_poly2[crushlineidsIn2[0]]);
                        }
                        else
                        {    //这时候就要处理一下了，不过倒也问题不大，
                            //目前有的就是一个尖灭地层的poly，然后一串arc在图2中的id
                            //还是学一下在剖分时候的这个方法吧，就是，用外接矩形去对准另一个外接矩形，然后把尖灭地层整个转过去
                            Geometry g1fullPoly = unionDicgeom(polyindex1);
                            Geometry g2fullPoly = unionDicgeom(polyindex2);
                            List<double[]> xyg1 = new List<double[]>();
                            List<double[]> xyg2 = new List<double[]>();
                            SplitStrataToolbox.setPolyIntoList(g1fullPoly, out xyg1);//首先拿到外围的点
                            SplitStrataToolbox.setPolyIntoList(g2fullPoly, out xyg2);
                            MinOutsourceRect rotatedRectTarget1, rotatedRectTarget2;
                            //getMinAreaRectByCv2(xylist, out rotatedRectTarget);
                            rotatedRectTarget1 = MinOutRectBuilder.buildMinOutRect(xyg1);//求两个最小外接矩形
                            rotatedRectTarget2 = MinOutRectBuilder.buildMinOutRect(xyg2);
                            Matrix<double> resultMat =SplitStrataToolbox. getTransMat(rotatedRectTarget1,rotatedRectTarget2 );//求出变换矩阵
                            Geometry sharpenGeom = polyindex1[id];
                            Geometry transedGeom= SplitStrataToolbox.getTransGeom(sharpenGeom, resultMat);//变换一下目标的尖灭地层的polygon
                            int nearlistarcid = -1;
                            double nearlistdistance = double.MaxValue;
                            List<double[]> xytarget;
                            SplitStrataToolbox.setPolyIntoList(transedGeom, out xytarget);//把变过的尖灭地层polygon坐标拿到
                            foreach (int arcid in crushlineidsIn2) {//遍历所有肯能冲突的这个线，拿到其中端点与尖灭地层变换后位置最近的那个线，就确定为对应线（肯定也不准，没办法
                                int[] pointids = arc_point2[arcid];
                                
                                foreach (int pointidt in pointids) {
                                    double avedistance = 0;
                                    Geometry pointt = pointindex2[pointidt];
                                    double px = pointt.GetX(0);
                                    double py = pointt.GetY(0);
                                    foreach (double[] xy in xytarget) {
                                        avedistance += distance(xy[0], xy[1], px, py);
                                    }
                                    avedistance = avedistance / xytarget.Count;
                                    if (avedistance < nearlistdistance) {
                                        nearlistdistance = avedistance;
                                        nearlistarcid = arcid;
                                    }
                                }
                            }
                            existin2.Add(nearlistarcid, arc_poly2[nearlistarcid]);//谁离得最近，就把谁加进去。
                        }
                    }
                }
                Dictionary<int, int[]> cantfindin1 = new Dictionary<int, int[]>();
                Dictionary<int, int[]> canfindin1 = new Dictionary<int, int[]>();
                int[] pointsAroundSharpenPoly = pointsidarcoundPoly(topology1, id);
                foreach (var vk in existin2) {//遍历每个存在于2中的和这个尖灭地层相关的弧段
                    //混淆的解决也不是在这，这实际上是在寻找对应的线段是否合法
                    int[] polyt =vk.Value;
                    bool exist = false;
                    foreach (var vk2 in arc_poly1) {//在1中寻找到这个弧段是不是有它的对应的弧段
                        if (equalforintPair(polyt, vk2.Value)) {
                            bool linkaimpoly = connectarcToPoly(topology1, pointsAroundSharpenPoly, vk2.Key);
                            if (linkaimpoly) {
                                exist = true;
                                break;
                            }
                            //exist = true; break;

                        }
                    }
/*                    foreach (var vk2 in arc_poly1)
                    {//在1中寻找到这个弧段是不是有它的对应的弧段
                        if (equalforintPair(polyt, vk2.Value))
                        {
                            bool linkaimpoly = connectarcToPoly(topology1, pointsAroundSharpenPoly, vk2.Key);
                            if (linkaimpoly)
                            {
                                exist = true;
                                break;
                            }
                            //exist = true; break;

                        }
                    }*/
                    if (exist)
                    {
                        canfindin1.Add(vk.Key, vk.Value);//这组是没有对应的
                    }
                    else {
                        cantfindin1.Add(vk.Key, vk.Value);//这组是有对应的
                    }
                }
                int[] pointsidarcoundPoly(Topology topology,int polyidi) {
                    //检查这个arc是否和这个poly相邻，相邻则返回true，否则返回false
                    //int[] endpointsi = topology.arcs_points_Pairs[arcidi];
                    /*                    int[] pointslinkarcs1 = topology.points_arcs_Pairs[endpointsi[0]].ToArray();
                                        int[] pointslinkarcs2 = topology.points_arcs_Pairs[endpointsi[1]].ToArray();*/
                    int[] arcsofpoly= topology.poly_arcs_Pairs[polyidi].ToArray();
                    List<int> pointlistaroundpoly = new List<int>();
                    foreach (int arcidttt in arcsofpoly)
                    {
                        int[] endpointsttt = topology.arcs_points_Pairs[arcidttt];
                        if (pointlistaroundpoly.Contains(endpointsttt[0]) == false)
                        {
                            pointlistaroundpoly.Add(endpointsttt[0]);
                        }
                        if (pointlistaroundpoly.Contains(endpointsttt[1]) == false)
                        {
                            pointlistaroundpoly.Add(endpointsttt[1]);
                        }
                    }
                    return pointlistaroundpoly.ToArray();
                }
                bool connectarcToPoly(Topology topology,int[] pointsAroundPoly,int arcidt) {
                    int[] endpointsi = topology.arcs_points_Pairs[arcidt];
                    bool resultttt = false;
                    if (pointsAroundPoly.Contains<int>(endpointsi[0])|| pointsAroundPoly.Contains<int>(endpointsi[1])) {
                        resultttt = true;
                    }
                    return resultttt;
                }
                //下面就是生成一个geometry List 作为弧段做buffer的这个基本的弧段。
                List<Geometry> centerArc = new List<Geometry>();
                List<Geometry> pointdefinite = new List<Geometry>();
                foreach (var vkk in cantfindin1) {
                    int indext = vkk.Key;
                    Geometry line = arcindex2[indext];
                    centerArc.Add(line);
                    int[] pointt = arc_point2[indext];
                    Geometry point1 = pointindex2[pointt[0]];
                    Geometry point2 = pointindex2[pointt[1]];
                    if (pointinlist(point1, pointdefinite) == false) {//收集一下确定在其中的线的端点。
                        pointdefinite.Add(point1);
                    }
                    if (pointinlist(point2, pointdefinite) == false)
                    {
                        pointdefinite.Add(point2);
                    }
                }
                #region
                //这里应该给那种连很多段线的点加入到pointdefinite
                List<Geometry> pointsends = new List<Geometry>(pointdefinite.ToArray());//记录一下端点
                List<int> pointscountlist = new List<int>();//记录每个端点出现的个数
                for (int ttt = 0; ttt < pointsends.Count; ttt++) {//初始化固定端点
                    pointscountlist.Add(3);
                }
                foreach (var vk in canfindin1) {
                    int index = vk.Key;
                    Geometry line = arcindex2[index];
                    int pointcountt = line.GetPointCount();
                    Geometry point0 = new Geometry(wkbGeometryType.wkbPoint);
                    Geometry pointe = new Geometry(wkbGeometryType.wkbPoint);
                    point0.AddPoint_2D(line.GetX(0), line.GetY(0));
                    pointe.AddPoint_2D(line.GetX(pointcountt - 1), line.GetY(pointcountt - 1));
                    //取出两个端点
                    int index1 = pointinlistindex(point0, pointsends);
                    int index2 = pointinlistindex(pointe, pointsends);
                    if (index1 != -1)
                    {
                        pointscountlist[index1] = pointscountlist[index1] + 1;
                    }
                    else {
                        pointsends.Add(point0);
                        pointscountlist.Add(1);
                    }
                    if (index2 != -1)
                    {
                        pointscountlist[index2] = pointscountlist[index2] + 1;
                    }
                    else
                    {
                        pointsends.Add(pointe);
                        pointscountlist.Add(1);
                    }
                }
                if (cantfindin1.Count == 0)//当消失的线为0的时候，我们应该为它添加额外的点，而如有消失的线，那么久不必添加。
                {
                    for (int k = 0; k < pointsends.Count; k++)
                    {
                        int pcount = pointscountlist[k];
                        Geometry ppp = pointsends[k];
                        if (pcount >= 3 && pointinlist(ppp, pointdefinite) == false)
                        {//把连接线大于3而且还没有添加进可用数组的点给加进去，作为可用点。
                            pointdefinite.Add(ppp);
                        }
                    }
                }
                #endregion
                //List<Geometry> segmentsintoucharc = new List<Geometry>();
                //  List<int[]> segmentfornear = new List<int[]>();
                foreach (var vk in canfindin1) {
                    int index = vk.Key;
                    Geometry line = arcindex2[index];
                    int pointcountt = line.GetPointCount();
                    Geometry point0 = new Geometry(wkbGeometryType.wkbPoint);
                    Geometry pointe = new Geometry(wkbGeometryType.wkbPoint);
                    point0.AddPoint_2D(line.GetX(0), line.GetY(0));
                    pointe.AddPoint_2D(line.GetX(pointcountt - 1), line.GetY(pointcountt - 1));//把首尾端点生成出来
                    bool b1 = pointinlist(point0, pointdefinite);//看看哪个在已经确定了的点的列表里
                    bool b2 = pointinlist(pointe, pointdefinite);
                    if (b1 == false && b2 == false)
                    {//点都不与确定存在的位置相邻，为啥要处理呢处理啥呢，草,不知道一开始咋想的
                        //想起来了，主要是为了处理无根的那些弧段，
                        //这次呢，我觉得吧，我觉得吧，应该直接在前边生成一个可用点，加进pointdefinite，不要在这临时处理
/*                        int[] endpoints = arc_point2[index];
                        int p1 = endpoints[0];
                        int p2 = endpoints[1];
                        List<int> touchline1 = point_arc2[p1];
                        List<int> touchline2 = point_arc2[p2];
                        int pcount1 = 0;
                        int pcount2 = 0;
                        foreach (int lineindextemp in touchline1) {
                            int[] polytouch = arc_poly2[lineindextemp];
                            if (neighborPolys.Contains(polytouch[0])) {
                                pcount1++;
                            }
                            if (neighborPolys.Contains(polytouch[1]))
                            {
                                pcount1++;
                            }
                        }
                        foreach (int lineindextemp in touchline2) {
                            int[] polytouch = arc_poly2[lineindextemp];
                            if (neighborPolys.Contains(polytouch[0])) {
                                pcount2++;
                            }
                            if (neighborPolys.Contains(polytouch[1]))
                            {
                                pcount2++;
                            }
                        }
                        Geometry canusePoint = new Geometry(wkbGeometryType.wkbPoint);
                        if (pcount1 > pcount2) {
                            canusePoint = pointindex2[p1];
                        } else {
                            canusePoint = pointindex2[p2];
                        }
                        double x1 = line.GetX(0);
                        double y1 = line.GetY(0);
                        double x2 = canusePoint.GetX(0);
                        double y2 = canusePoint.GetY(0);
                        bool bbbb = true;
                        if ((Math.Abs(x1 - x2) < 0.000001) && (Math.Abs(y1 - y2) < 0.000001))
                        { //看看是不是在最开头的点
                            bbbb = true;
                        }
                        else {
                            bbbb = false;
                        }
                        List<double> xlist, ylist;
                        //xlist = new List<double>();
                        //ylist = new List<double>();
                        getLineXY(line, out xlist, out ylist, bbbb);
                        //这边先用第一个segment代替哈，加入这个固定参数的算法稍后再说
                        Geometry segment1 = new Geometry(wkbGeometryType.wkbLineString);
                        segment1.AddPoint_2D(xlist[0], ylist[0]);
                        segment1.AddPoint_2D(xlist[1], ylist[1]);
                        centerArc.Add(segment1);*/
                    }
                    else
                    {
                        List<double> xlist, ylist;
                        //xlist = new List<double>();
                        //ylist = new List<double>();
                        getLineXY(line, out xlist, out ylist, b1);
                        //这边先用第一个segment代替哈，加入这个固定参数的算法稍后再说
                        Geometry segment1 = new Geometry(wkbGeometryType.wkbLineString);
                        segment1.AddPoint_2D(xlist[0], ylist[0]);
                        segment1.AddPoint_2D(xlist[1], ylist[1]);
                        centerArc.Add(segment1);
                    }
                }
                result.Add(id, centerArc);
            }

            foreach (int id in this.sharpen2) {
                List<int> arc1 = polys_Arc1[id];
                int[] poly1 = arc_poly1[arc1[0]];
                int[] poly2 = arc_poly1[arc1[1]];
                int poly11 = poly1[0];
                if (poly11 == id) poly11 = poly1[1];
                int poly22 = poly2[0];
                if (poly22 == id) poly22 = poly2[1];
                //到此为止获取了这个被两地层夹的地层周围的两个地层
                //首先看看这俩地层在剖面1中相连与否，如果不相连，那么它们之间的那个弧段应当是被当作生成buffer的弧段
                int[] polyttt = { poly11, poly22 };//记录夹着这个多边形的另两个多边形
                bool hastouch = false;
                foreach (var vk in arc_poly1) {
                    int[] polyt = vk.Value;
                    if (equalforintPair(polyt, polyttt)) {
                        hastouch = true;
                        break;
                    }
                }
                //如果
                if (hastouch == false) {
                    //这时候寻找一下在section2里边夹的那个
                    foreach (var vk2 in arc_poly2) {
                        int[] polyt = vk2.Value;
                        if (equalforintPair(polyt, polyttt)) {
                            Geometry line = arcindex2[vk2.Key];
                            List<Geometry> lines = new List<Geometry>();
                            lines.Add(line);
                            result.Add(id, lines);
                            break;
                        }
                    }
                    continue;
                }
                //现在就是考虑一下这个二夹一是不是靠边
                int betweenlinecount = 0;
                foreach (var vk in arc_poly1) {
                    int[] polyt = vk.Value;
                    if (equalforintPair(polyt, polyttt))
                    {
                        betweenlinecount += 1;//记录一下有几条边
                    }
                }
                //如果是包围着该地层的两地层有两条相交的边的话，那么就求一下三个地层合起来的envelope，然后求一下这个被包围地层的envelope的中心，求它的横纵向百分比，
                //然后求出那个对应的那个弧段上距离那个百分比位置定位点最近的segment，就确定了这个地层的位置
                if (betweenlinecount >= 2) {//显然不是只能有两个啊
                    List<Geometry> uniongeomcollection = new List<Geometry>();
                    Geometry polyofId = polyindex1[id];
                    uniongeomcollection.Add(polyofId);
                    if (poly11 != -1) uniongeomcollection.Add(polyindex1[poly11]);
                    if (poly22 != -1) uniongeomcollection.Add(polyindex1[poly22]);
                    Geometry uniongeom = new Geometry(wkbGeometryType.wkbPolygon);
                    foreach (Geometry ge in uniongeomcollection) {
                        uniongeom = uniongeom.Union(ge);
                    }
                    Envelope envelope1 = new Envelope();
                    uniongeom.GetEnvelope(envelope1);
                    Envelope envmin = new Envelope();
                    polyofId.GetEnvelope(envmin);
                    double idxc = (envmin.MaxX + envmin.MinX) / 2;
                    double idyc = (envmin.MaxY + envmin.MinY) / 2;
                    double ratex = (idxc - envelope1.MinX) / (envelope1.MaxX - envelope1.MinX);
                    double ratey = (idyc - envelope1.MinY) / (envelope1.MaxY - envelope1.MinY);
                    Geometry lineinSection2=new Geometry(wkbGeometryType.wkbLineString);
                    List<Geometry> lineinSection2s = new List<Geometry>();
                    //有时间继续优化的话，我觉得可以在这里加一个寻找对应的模块，就是找一下哪个和哪个是对应的。这个确实是有必要的，但是，额，目前比较困难，就算了。
                    //后续把那个，那个在完全对应图上找位置给加进去然后可能写了就比较好
                    foreach (var vk2 in arc_poly2)//找到这对应的线
                        //草，这就是问题啊，有两个位置就随便选了一个，草草草

                    {
                        int[] polyt = vk2.Value;
                        if (equalforintPair(polyt, polyttt))
                        {
                            Geometry line = arcindex2[vk2.Key];
                            lineinSection2 = line;
                            lineinSection2s.Add(line);
                            // break;
                        }
                    }
                    Geometry g1 = null, g2 = null;
                    int signal1 = 0;
                    if (poly11 != -1)
                    {
                        g1 = polyindex2[poly11];

                    }
                    else {
                        signal1 = 1;
                    }
                    if (poly22 != -1)
                    {
                        g2 = polyindex2[poly22];
                    }
                    else {
                        signal1 = 2;
                    }
                    Geometry union2 = new Geometry(wkbGeometryType.wkbPolygon);
                    switch (signal1) {//到这把section2中的两个多边形给拿到了，然后union在一起，形成一个大envelope
                        case 0: {
                                union2 = union2.Union(g1);
                                union2 = union2.Union(g2);
                                break; }
                        case 1: {
                                union2 = union2.Union(g2);
                                break; }
                        case 2: {
                                union2 = union2.Union(g1);
                                break; }
                    }
                    Envelope enve2 = new Envelope();
                    union2.GetEnvelope(enve2);
                    double xsite = (enve2.MaxX - enve2.MinX) * ratex + enve2.MinX;
                    double ysite = (enve2.MaxY - enve2.MaxY) * ratey + enve2.MinY;
                    int minindex = -1;
                    double mindistance = double.MaxValue;
                    List<double> xlist, ylist;
                    getLineXY(lineinSection2, out xlist, out ylist, true);
                    int count11 = xlist.Count();
                    for (int j = 2; j < count11-1; j++) {//现在找到除了首尾之外中间距离那个中心点最近的那个segment作为弧段
                        double x1, y1, x2, y2;
                        x1 = xlist[j - 1];
                        y1 = ylist[j - 1];
                        x2 = xlist[j];
                        y2 = ylist[j];
                        double cx, cy;
                        cx = (x1 + x2) / 2;
                        cy = (y1 + y2) / 2;
                        double dis = distance(cx, cy, xsite, ysite);
                        if (dis < mindistance) {
                            mindistance = dis;
                            minindex = j - 1;
                        }
                    }
                    //找到目标线上距离相对中心位置最近的点之后，就把它作为尖灭地层对应的位置给他存到结果里
                    Geometry pairsegment = new Geometry(wkbGeometryType.wkbLineString);
                    pairsegment.AddPoint_2D(xlist[minindex], ylist[minindex]);
                    pairsegment.AddPoint_2D(xlist[minindex + 1], ylist[minindex + 1]);
                    List<Geometry> ttgeoms = new List<Geometry>();
                    ttgeoms.Add(pairsegment);
                    result.Add(id, ttgeoms);
                    }
                if (betweenlinecount == 1) {
                    //现在就是考虑，怎么把偏向一方向的被两个地层夹的地层弄一个弧段出来。
                    //实际上最好的办法就是，找到原地层中的两个polygon夹的那条边，看看它的左右segment哪个离近一些
                    //算了，用这个办法吧，就是看看哪个点除掉包围的两个点，之外，和剖面图1中的外边的点更相似
                    List<int> arcwithsharp = polys_Arc1[id];
                    int onearc = arcwithsharp[0];
                    int[] pointids = arc_point1[onearc];
                    List<int> p1arcs = point_arc1[pointids[0]];
                    List<int> p2arcs = point_arc1[pointids[1]];
                    List<int> aimp = p1arcs;
                    if (p1arcs.Count <= 3) {
                        aimp = p2arcs;
                    }
                    List<int> aimtouchpolys = new List<int>();//记录一下这个
                    foreach (int arcidfromp in aimp) {
                        int[] polyids = arc_poly1[arcidfromp];
                        if (!aimtouchpolys.Contains(polyids[0])) {
                            aimtouchpolys.Add(polyids[0]);
                        }
                        if (!aimtouchpolys.Contains(polyids[1]))
                        {
                            aimtouchpolys.Add(polyids[1]);
                        }
                    }
                    Geometry lineinSection2 = new Geometry(wkbGeometryType.wkbLineString);
                    int aimlinein2 = -1;
                    foreach (var vk2 in arc_poly2)//找到这对应的线
                    {
                        int[] polyt = vk2.Value;
                        if (equalforintPair(polyt, polyttt))
                        {
                            Geometry line = arcindex2[vk2.Key];
                            lineinSection2 = line;
                            aimlinein2 = vk2.Key;
                            break;
                        }
                    }
                    int[] tp2 = arc_point2[aimlinein2];
                    List<int> tp2arcs1 = point_arc2[tp2[0]];
                    List<int> tp2arcs2 = point_arc2[tp2[1]];
                    List<int> tp2touch1 = new List<int>();
                    List<int> tp2touch2 = new List<int>();
                    //到这应当是寻找另外两个点的对应的点touch的列表
                    foreach (int arcid in tp2arcs1) {
                        int[] polyids = arc_poly2[arcid];
                        if (!tp2touch1.Contains(polyids[0])) {
                            tp2touch1.Add(polyids[0]);
                        }
                        if (!tp2touch1.Contains(polyids[1]))
                        {
                            tp2touch1.Add(polyids[1]);
                        }
                    }
                    foreach (int arcid in tp2arcs2)
                    {
                        int[] polyids = arc_poly2[arcid];
                        if (!tp2touch2.Contains(polyids[0]))
                        {
                            tp2touch2.Add(polyids[0]);
                        }
                        if (!tp2touch2.Contains(polyids[1]))
                        {
                            tp2touch2.Add(polyids[1]);
                        }
                    }
                    //到这应当求一下左右的点哪个包含的相对应的线多
                    int samecount1 = 0;
                    int samecount2 = 0;
                    foreach (int ttt in aimtouchpolys) {
                        if (tp2touch1.Contains(ttt)) {
                            samecount1++;
                        }
                        if (tp2touch2.Contains(ttt)) {
                            samecount2++;
                        }
                    }
                    int aimpoint2 = -1;
                    if (samecount1 >= samecount2)
                    {
                        aimpoint2 = tp2[0];
                    }
                    else {
                        aimpoint2 = tp2[1];
                    }
                    Geometry aimpoint = topology2.index_points_Pairs[aimpoint2];//这样就获取到了这个正确点的位置
                    List<double> xlist, ylist;
                    getLineXY(lineinSection2, out xlist, out ylist,true);
                    double[] segment1 = new double[4];
                    bool firstorend = true;//这个是用来记录首端点还是尾端点是正确的对应的点的bool变量。
                    double x1, y1,px,py; 
                    x1 = xlist[0];
                    y1 = ylist[0];
                    px = aimpoint.GetX(0);
                    py = aimpoint.GetY(0);
                    if (doublequal(x1, px) && doublequal(y1, py))
                    {
                        segment1[0] = xlist[0];
                        segment1[1] = ylist[0];
                        segment1[2] = xlist[1];
                        segment1[3] = ylist[1];
                    }
                    else {
                        int fullcount = xlist.Count;
                        segment1[0] = xlist[fullcount-1];
                        segment1[1] = ylist[fullcount - 1];
                        segment1[2] = xlist[fullcount - 2];
                        segment1[3] = ylist[fullcount - 2];
                    }
                    Geometry pairsegment = new Geometry(wkbGeometryType.wkbLineString);
                    pairsegment.AddPoint_2D(segment1[0], segment1[1]);
                    pairsegment.AddPoint_2D(segment1[2], segment1[3]);
                    List<Geometry> ttgeoms = new List<Geometry>();
                    ttgeoms.Add(pairsegment);
                    result.Add(id, ttgeoms);
                }
            }
            return result;
        }
        
        public Dictionary<int, Geometry> getbuffers(Dictionary<int,List<Geometry>> lines,double distance,int quadeses) {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in lines) {
                List<Geometry> geometries = vk.Value;
                Geometry union = new Geometry(wkbGeometryType.wkbPolygon);
                foreach (Geometry ge in geometries) {
                    Geometry genew = ge.Buffer(distance, quadeses);
                    union = union.Union(genew);
                }
                result.Add(vk.Key, union);
            }
            return result;
        }
        /// <summary>
        /// 新版本的寻找尖灭位置虚拟地层的函数
        /// 在此整体梳理一下这个对应尖灭地层位置然后创建对应虚拟地层面的过程的问题
        /// 首先就是寻找可能的线，由于是通过不完全的拓扑关系，即只能用两侧的面去寻找对应的线，所以这样就创造了一个根本问题
        /// 某两个面控制的线可能有多个，也可能有唯一的一个但是位置并不对，
        /// 所以第一步，就是在原始含尖灭地层的剖面上解决这种面控制的线可能不对应问题，也就是通过这个线是否与尖灭面相连来判断，相连就是对的，不相连就是错的
        /// 第二步，解决在对应不含尖灭面的剖面上解决这个问题，这就比较复杂了，目前就碰到了这样的数据，首先就是，这种线应当是不相连的，就是把无法直接对应的线给做touches检测，相连就分成一组，不相连就单独一组
        /// 然后把这个每组再做为进行其他的不完全作为尖灭位置的地层的核心（也可以有不含核心的组），比一比哪一组得到的结果是对的，
        /// 第一步已经解决了，现在需要解决第二步
        /// </summary>
        /// <param name="statlong"></param>
        /// <returns></returns>
        public Dictionary<int, List<Geometry>> createArasForMissNewVersion(int statlong)
        {
            Dictionary<int, List<Geometry>> result = new Dictionary<int, List<Geometry>>();
            //首先应该遍历所有的含有三个地层的尖灭地层
            Dictionary<int, List<int>> polys_Arc1 = topology1.poly_arcs_Pairs;
            Dictionary<int, List<int>> polys_Arc2 = topology2.poly_arcs_Pairs;
            Dictionary<int, int[]> arc_poly1 = topology1.arcs_poly_Pairs;
            Dictionary<int, int[]> arc_poly2 = topology2.arcs_poly_Pairs;
            Dictionary<int, int[]> arc_point1 = topology1.arcs_points_Pairs;
            Dictionary<int, int[]> arc_point2 = topology2.arcs_points_Pairs;
            Dictionary<int, Geometry> arcindex2 = topology2.index_arcs_Pairs;
            Dictionary<int, Geometry> pointindex2 = topology2.index_points_Pairs;
            Dictionary<int, Geometry> polyindex1 = topology1.polys;
            Dictionary<int, Geometry> polyindex2 = topology2.polys;
            Dictionary<int, List<int>> point_arc1 = topology1.points_arcs_Pairs;
            Dictionary<int, List<int>> point_arc2 = topology2.points_arcs_Pairs;
            foreach (int id in this.sharpen3)
            {//遍历那个相邻三个地层的尖灭地层
                List<int> neighborPolys = new List<int>();//存储当前的几何图形周围的polygon
                List<int> arcs1 = polys_Arc1[id];
                foreach (int arcid in arcs1)
                {
                    int[] polypair = arc_poly1[arcid];
                    int anotherpolyid = polypair[0];
                    if (anotherpolyid == id)
                    {
                        anotherpolyid = polypair[1];
                    }
                    neighborPolys.Add(anotherpolyid);//到此为止就获取了当前这个尖灭地层的周围的面了
                }
                int count2 = neighborPolys.Count;
                List<int[]> combinlist = new List<int[]>();//生成一下排列组合
                for (int i = 0; i < count2 - 1; i++)
                {

                    for (int j = i + 1; j < count2; j++)
                    {
                        int[] temp = new int[2];
                        temp[0] = neighborPolys[i];
                        temp[1] = neighborPolys[j];
                        combinlist.Add(temp);
                    }
                }
                Dictionary<int, int[]> existin2 = new Dictionary<int, int[]>();
                // List<int[]> existin1 = new List<int[]>();

                foreach (var vk2 in combinlist)
                {

                    //混淆问题是怎么一回事呢，就是说，一个图中不仅仅存在一个对应的，而是存在好多对应的。那就改改改改改
                    //目前的实例里边还是存在一个问题就是，emmm，这个两个混淆线段实际上它们都有一个能够适应的点，那咋办呢。咋办呢。
                    //两个方案嘛，两个方案
                    bool findedexist = false;
                    List<int> crushlineidsIn2 = new List<int>();
                    foreach (var vk in arc_poly2)
                    {//这下获取了在第二个图里边有的（方式就是和排列组合比较，因为排除了二夹一的情况，所以就比较好）
                        int[] polyt = vk.Value;

                        if (equalforintPair(polyt, vk2))
                        {//就是在这，会出现混淆问题
                         //就是这！！！！ 混淆问题，而且记录了在图2中的线的id，草，草他妈的，真难搞啊
                            crushlineidsIn2.Add(vk.Key);
                            // existin2.Add(vk.Key,vk.Value);//找到存在的就退出,并且记录它的index和两侧
                            // break;
                            findedexist = true;
                        }
                    }
                    if (findedexist)//首先确保找到了正确位置
                    {
                        if (crushlineidsIn2.Count == 1)
                        {
                            existin2.Add(crushlineidsIn2[0], arc_poly2[crushlineidsIn2[0]]);
                        }
                        else
                        //最开始这里的策略是要看一下相对距离，我觉得现在后边有了完备的处理方法，就可以都加进去
                        {
                            for (int i = 0; i < crushlineidsIn2.Count; i++)
                            {
                                existin2.Add(crushlineidsIn2[i], arc_poly2[crushlineidsIn2[i]]);
                            }
                        }
                    }
                }
                Dictionary<int, int[]> cantfindin1 = new Dictionary<int, int[]>();
                Dictionary<int, int[]> canfindin1 = new Dictionary<int, int[]>();
                int[] pointsAroundSharpenPoly = pointsidarcoundPoly(topology1, id);
                foreach (var vk in existin2)
                {//遍历每个存在于2中的和这个尖灭地层相关的弧段
                    //混淆的解决也不是在这，这实际上是在寻找对应的线段是否合法
                    int[] polyt = vk.Value;
                    bool exist = false;
                    foreach (var vk2 in arc_poly1)
                    {//在1中寻找到这个弧段是不是有它的对应的弧段
                        if (equalforintPair(polyt, vk2.Value))
                        {
                            bool linkaimpoly = connectarcToPoly(topology1, pointsAroundSharpenPoly, vk2.Key);
                            if (linkaimpoly)
                            {
                                exist = true;
                                break;
                            }
                            //exist = true; break;

                        }
                    }

                    if (exist)
                    {
                        canfindin1.Add(vk.Key, vk.Value);//这组是有对应的
                    }
                    else
                    {
                        cantfindin1.Add(vk.Key, vk.Value);//这组是没有对应的，里边的key都是图2中线的id，value都是这个线两侧面的id
                    }
                }
                int[] pointsidarcoundPoly(Topology topology, int polyidi)
                {

                    int[] arcsofpoly = topology.poly_arcs_Pairs[polyidi].ToArray();
                    List<int> pointlistaroundpoly = new List<int>();
                    foreach (int arcidttt in arcsofpoly)
                    {
                        int[] endpointsttt = topology.arcs_points_Pairs[arcidttt];
                        if (pointlistaroundpoly.Contains(endpointsttt[0]) == false)
                        {
                            pointlistaroundpoly.Add(endpointsttt[0]);
                        }
                        if (pointlistaroundpoly.Contains(endpointsttt[1]) == false)
                        {
                            pointlistaroundpoly.Add(endpointsttt[1]);
                        }
                    }
                    return pointlistaroundpoly.ToArray();
                }
                bool connectarcToPoly(Topology topology, int[] pointsAroundPoly, int arcidt)
                {
                    int[] endpointsi = topology.arcs_points_Pairs[arcidt];
                    bool resultttt = false;
                    if (pointsAroundPoly.Contains<int>(endpointsi[0]) || pointsAroundPoly.Contains<int>(endpointsi[1]))
                    {
                        resultttt = true;
                    }
                    return resultttt;
                }
                List<int[]> connectMissLine(Topology topologythis,Dictionary<int,int[]> cantfinein1dic) 
                {//这是为了将所有的这个无法找到对应的线进行连通检测 ,返回一个List<int[]>里边每个整型数组都是一组可以完全联通的线
                    UnionSet unionSet = new UnionSet(topologythis.index_arcs_Pairs.Count + 1);
                    foreach (var vk in cantfinein1dic) {//根据topo关系把连通性拿到，
                        int[] pointstt = topologythis.arcs_points_Pairs[vk.Key];
                        int point1 = pointstt[0];
                        int point2 = pointstt[1];
                        List<int> linklinesid = new List<int>();
                        linklinesid.AddRange(topologythis.points_arcs_Pairs[point1]);
                        linklinesid.Remove(vk.Key);
                        linklinesid.AddRange(topologythis.points_arcs_Pairs[point2]);
                        linklinesid.Remove(vk.Key);
                        foreach (int lineid in cantfinein1dic.Keys) 
                        {
                            if (linklinesid.Contains(lineid)) {
                                unionSet.Unite(vk.Key, lineid);
                            }    
                        }
                    }
                    Dictionary<int, List<int>> resultdic = new Dictionary<int, List<int>>();
                    foreach (var vk in cantfinein1dic) {
                        int fa=unionSet.Find(vk.Key);
                        if (resultdic.ContainsKey(fa) == false)
                        {
                            resultdic.Add(fa, new List<int>());
                            resultdic[fa].Add(vk.Key);
                        }
                        else {
                            resultdic[fa].Add(vk.Key);
                        }
                     }
                    List<int[]> resultlist = new List<int[]>();
                    foreach (var vk in resultdic) {
                        resultlist.Add(vk.Value.ToArray());
                    }
                    return resultlist;
                }
                List<int[]> lineblocks = connectMissLine(topology2, cantfindin1);
                int[] lineempty = new int[0];
                lineblocks.Add(lineempty);
                List<List<Geometry>> centerarcany = new List<List<Geometry>>();//用来收集每次的求出来的中心geometry表
                List<int> centerarcTocanfind = new List<int>();//统计每次收集了多少个并不完全隔开的边界，越多越好，最多的就是需要的结果
                foreach (int[] lineblock in lineblocks) 
                {
                    //下面就是生成一个geometry List 作为弧段做buffe r的这个基本的弧段。
                    List<Geometry> centerArc = new List<Geometry>();
                    List<Geometry> pointdefinite = new List<Geometry>();
                    int touchcanfindcount = 0;
                    //foreach (var vkk in cantfindin1)
                    foreach (var vkk in lineblock)
                    {//把每个固定线块给当作核
                        int indext = vkk;
                        Geometry line = arcindex2[indext];
                        centerArc.Add(line);
                        int[] pointt = arc_point2[indext];
                        Geometry point1 = pointindex2[pointt[0]];
                        Geometry point2 = pointindex2[pointt[1]];
                        if (pointinlist(point1, pointdefinite) == false)
                        {//收集一下确定在其中的线的端点。
                            pointdefinite.Add(point1);
                        }
                        if (pointinlist(point2, pointdefinite) == false)
                        {
                            pointdefinite.Add(point2);
                        }
                    }
                    #region
                    //这里应该给那种连很多段线的点加入到pointdefinite
                    List<Geometry> pointsends = new List<Geometry>(pointdefinite.ToArray());//记录一下端点
                    List<int> pointscountlist = new List<int>();//记录每个端点出现的个数
                    for (int ttt = 0; ttt < pointsends.Count; ttt++)
                    {//初始化固定端点
                        pointscountlist.Add(3);
                    }
                    foreach (var vk in canfindin1)
                    {
                        int index = vk.Key;
                        Geometry line = arcindex2[index];
                        int pointcountt = line.GetPointCount();
                        Geometry point0 = new Geometry(wkbGeometryType.wkbPoint);
                        Geometry pointe = new Geometry(wkbGeometryType.wkbPoint);
                        point0.AddPoint_2D(line.GetX(0), line.GetY(0));
                        pointe.AddPoint_2D(line.GetX(pointcountt - 1), line.GetY(pointcountt - 1));
                        //取出两个端点
                        int index1 = pointinlistindex(point0, pointsends);
                        int index2 = pointinlistindex(pointe, pointsends);
                        if (index1 != -1)
                        {
                            pointscountlist[index1] = pointscountlist[index1] + 1;
                        }
                        else
                        {
                            pointsends.Add(point0);
                            pointscountlist.Add(1);
                        }
                        if (index2 != -1)
                        {
                            pointscountlist[index2] = pointscountlist[index2] + 1;
                        }
                        else
                        {
                            pointsends.Add(pointe);
                            pointscountlist.Add(1);
                        }
                    }
                    if (lineblock.Length == 0)//当消失的线为0的时候，我们应该为它添加额外的点，而如有消失的线，那么久不必添加。
                    {
                        for (int k = 0; k < pointsends.Count; k++)
                        {
                            int pcount = pointscountlist[k];
                            Geometry ppp = pointsends[k];
                            if (pcount >= 3 && pointinlist(ppp, pointdefinite) == false)
                            {//把连接线大于3而且还没有添加进可用数组的点给加进去，作为可用点。
                                pointdefinite.Add(ppp);
                            }
                        }
                    }
                    #endregion
                    //List<Geometry> segmentsintoucharc = new List<Geometry>();
                    //  List<int[]> segmentfornear = new List<int[]>();
                    foreach (var vk in canfindin1)
                    {
                        int index = vk.Key;
                        Geometry line = arcindex2[index];
                        int pointcountt = line.GetPointCount();
                        Geometry point0 = new Geometry(wkbGeometryType.wkbPoint);
                        Geometry pointe = new Geometry(wkbGeometryType.wkbPoint);
                        point0.AddPoint_2D(line.GetX(0), line.GetY(0));
                        pointe.AddPoint_2D(line.GetX(pointcountt - 1), line.GetY(pointcountt - 1));//把首尾端点生成出来
                        bool b1 = pointinlist(point0, pointdefinite);//看看哪个在已经确定了的点的列表里
                        bool b2 = pointinlist(pointe, pointdefinite);
                        if (b1 == false && b2 == false)
                        {
                        }
                        else
                        {
                            List<double> xlist, ylist;
                            //xlist = new List<double>();
                            //ylist = new List<double>();
                            getLineXY(line, out xlist, out ylist, b1);
                            //这边先用第一个segment代替哈，加入这个固定参数的算法稍后再说
                            Geometry segment1 = new Geometry(wkbGeometryType.wkbLineString);
                            segment1.AddPoint_2D(xlist[0], ylist[0]);
                            segment1.AddPoint_2D(xlist[1], ylist[1]);
                            centerArc.Add(segment1);
                            touchcanfindcount++;
                        }

                    }
                    centerarcany.Add(centerArc);
                    centerarcTocanfind.Add(touchcanfindcount);
                }
                int maxarccount = int.MinValue;
                int maxcountidt = -1;
                for (int j = 0; j < centerarcany.Count; j++) {
                    if (centerarcTocanfind[j] > maxarccount) {
                        maxarccount = centerarcTocanfind[j];
                        maxcountidt = j;
                    }
                }
                result.Add(id, centerarcany[maxcountidt]);
            }

            foreach (int id in this.sharpen2)
            {
                List<int> arc1 = polys_Arc1[id];
                int[] poly1 = arc_poly1[arc1[0]];
                int[] poly2 = arc_poly1[arc1[1]];
                int poly11 = poly1[0];
                if (poly11 == id) poly11 = poly1[1];
                int poly22 = poly2[0];
                if (poly22 == id) poly22 = poly2[1];
                //到此为止获取了这个被两地层夹的地层周围的两个地层
                //首先看看这俩地层在剖面1中相连与否，如果不相连，那么它们之间的那个弧段应当是被当作生成buffer的弧段
                int[] polyttt = { poly11, poly22 };//记录夹着这个多边形的另两个多边形
                bool hastouch = false;
                foreach (var vk in arc_poly1)
                {
                    int[] polyt = vk.Value;
                    if (equalforintPair(polyt, polyttt))
                    {
                        hastouch = true;
                        break;
                    }
                }
                //如果
                if (hastouch == false)
                {
                    //这时候寻找一下在section2里边夹的那个
                    foreach (var vk2 in arc_poly2)
                    {
                        int[] polyt = vk2.Value;
                        if (equalforintPair(polyt, polyttt))
                        {
                            Geometry line = arcindex2[vk2.Key];
                            List<Geometry> lines = new List<Geometry>();
                            lines.Add(line);
                            result.Add(id, lines);
                            break;
                        }
                    }
                    continue;
                }
                //现在就是考虑一下这个二夹一是不是靠边
                int betweenlinecount = 0;
                foreach (var vk in arc_poly1)
                {
                    int[] polyt = vk.Value;
                    if (equalforintPair(polyt, polyttt))
                    {
                        betweenlinecount += 1;//记录一下有几条边
                    }
                }
                //如果是包围着该地层的两地层有两条相交的边的话，那么就求一下三个地层合起来的envelope，然后求一下这个被包围地层的envelope的中心，求它的横纵向百分比，
                //然后求出那个对应的那个弧段上距离那个百分比位置定位点最近的segment，就确定了这个地层的位置
                if (betweenlinecount >= 2)
                {//显然不是只能有两个啊
                    List<Geometry> uniongeomcollection = new List<Geometry>();
                    Geometry polyofId = polyindex1[id];
                    uniongeomcollection.Add(polyofId);
                    if (poly11 != -1) uniongeomcollection.Add(polyindex1[poly11]);
                    if (poly22 != -1) uniongeomcollection.Add(polyindex1[poly22]);
                    Geometry uniongeom = new Geometry(wkbGeometryType.wkbPolygon);
                    foreach (Geometry ge in uniongeomcollection)
                    {
                        uniongeom = uniongeom.Union(ge);
                    }
                    Envelope envelope1 = new Envelope();
                    uniongeom.GetEnvelope(envelope1);
                    Envelope envmin = new Envelope();
                    polyofId.GetEnvelope(envmin);
                    double idxc = (envmin.MaxX + envmin.MinX) / 2;
                    double idyc = (envmin.MaxY + envmin.MinY) / 2;
                    double ratex = (idxc - envelope1.MinX) / (envelope1.MaxX - envelope1.MinX);
                    double ratey = (idyc - envelope1.MinY) / (envelope1.MaxY - envelope1.MinY);
                    Geometry lineinSection2 = new Geometry(wkbGeometryType.wkbLineString);
                    List<Geometry> lineinSection2s = new List<Geometry>();
                    //有时间继续优化的话，我觉得可以在这里加一个寻找对应的模块，就是找一下哪个和哪个是对应的。这个确实是有必要的，但是，额，目前比较困难，就算了。
                    //后续把那个，那个在完全对应图上找位置给加进去然后可能写了就比较好
                    foreach (var vk2 in arc_poly2)//找到这对应的线
                                                  //草，这就是问题啊，有两个位置就随便选了一个，草草草

                    {
                        int[] polyt = vk2.Value;
                        if (equalforintPair(polyt, polyttt))
                        {
                            Geometry line = arcindex2[vk2.Key];
                            lineinSection2 = line;
                            lineinSection2s.Add(line);
                            // break;
                        }
                    }
                    Geometry g1 = null, g2 = null;
                    int signal1 = 0;
                    if (poly11 != -1)
                    {
                        g1 = polyindex2[poly11];

                    }
                    else
                    {
                        signal1 = 1;
                    }
                    if (poly22 != -1)
                    {
                        g2 = polyindex2[poly22];
                    }
                    else
                    {
                        signal1 = 2;
                    }
                    Geometry union2 = new Geometry(wkbGeometryType.wkbPolygon);
                    switch (signal1)
                    {//到这把section2中的两个多边形给拿到了，然后union在一起，形成一个大envelope
                        case 0:
                            {
                                union2 = union2.Union(g1);
                                union2 = union2.Union(g2);
                                break;
                            }
                        case 1:
                            {
                                union2 = union2.Union(g2);
                                break;
                            }
                        case 2:
                            {
                                union2 = union2.Union(g1);
                                break;
                            }
                    }
                    Envelope enve2 = new Envelope();
                    union2.GetEnvelope(enve2);
                    double xsite = (enve2.MaxX - enve2.MinX) * ratex + enve2.MinX;
                    double ysite = (enve2.MaxY - enve2.MaxY) * ratey + enve2.MinY;
                    int minindex = -1;
                    double mindistance = double.MaxValue;
                    List<double> xlist, ylist;
                    getLineXY(lineinSection2, out xlist, out ylist, true);
                    int count11 = xlist.Count();
                    for (int j = 2; j < count11 - 1; j++)
                    {//现在找到除了首尾之外中间距离那个中心点最近的那个segment作为弧段
                        double x1, y1, x2, y2;
                        x1 = xlist[j - 1];
                        y1 = ylist[j - 1];
                        x2 = xlist[j];
                        y2 = ylist[j];
                        double cx, cy;
                        cx = (x1 + x2) / 2;
                        cy = (y1 + y2) / 2;
                        double dis = distance(cx, cy, xsite, ysite);
                        if (dis < mindistance)
                        {
                            mindistance = dis;
                            minindex = j - 1;
                        }
                    }
                    //找到目标线上距离相对中心位置最近的点之后，就把它作为尖灭地层对应的位置给他存到结果里
                    Geometry pairsegment = new Geometry(wkbGeometryType.wkbLineString);
                    pairsegment.AddPoint_2D(xlist[minindex], ylist[minindex]);
                    pairsegment.AddPoint_2D(xlist[minindex + 1], ylist[minindex + 1]);
                    List<Geometry> ttgeoms = new List<Geometry>();
                    ttgeoms.Add(pairsegment);
                    result.Add(id, ttgeoms);
                }
                if (betweenlinecount == 1)
                {
                    //现在就是考虑，怎么把偏向一方向的被两个地层夹的地层弄一个弧段出来。
                    //实际上最好的办法就是，找到原地层中的两个polygon夹的那条边，看看它的左右segment哪个离近一些
                    //算了，用这个办法吧，就是看看哪个点除掉包围的两个点，之外，和剖面图1中的外边的点更相似
                    List<int> arcwithsharp = polys_Arc1[id];
                    int onearc = arcwithsharp[0];
                    int[] pointids = arc_point1[onearc];
                    List<int> p1arcs = point_arc1[pointids[0]];
                    List<int> p2arcs = point_arc1[pointids[1]];
                    List<int> aimp = p1arcs;
                    if (p1arcs.Count <= 3)
                    {
                        aimp = p2arcs;
                    }
                    List<int> aimtouchpolys = new List<int>();//记录一下这个
                    foreach (int arcidfromp in aimp)
                    {
                        int[] polyids = arc_poly1[arcidfromp];
                        if (!aimtouchpolys.Contains(polyids[0]))
                        {
                            aimtouchpolys.Add(polyids[0]);
                        }
                        if (!aimtouchpolys.Contains(polyids[1]))
                        {
                            aimtouchpolys.Add(polyids[1]);
                        }
                    }
                    Geometry lineinSection2 = new Geometry(wkbGeometryType.wkbLineString);
                    int aimlinein2 = -1;
                    foreach (var vk2 in arc_poly2)//找到这对应的线
                    {
                        int[] polyt = vk2.Value;
                        if (equalforintPair(polyt, polyttt))
                        {
                            Geometry line = arcindex2[vk2.Key];
                            lineinSection2 = line;
                            aimlinein2 = vk2.Key;
                            break;
                        }
                    }
                    int[] tp2 = arc_point2[aimlinein2];
                    List<int> tp2arcs1 = point_arc2[tp2[0]];
                    List<int> tp2arcs2 = point_arc2[tp2[1]];
                    List<int> tp2touch1 = new List<int>();
                    List<int> tp2touch2 = new List<int>();
                    //到这应当是寻找另外两个点的对应的点touch的列表
                    foreach (int arcid in tp2arcs1)
                    {
                        int[] polyids = arc_poly2[arcid];
                        if (!tp2touch1.Contains(polyids[0]))
                        {
                            tp2touch1.Add(polyids[0]);
                        }
                        if (!tp2touch1.Contains(polyids[1]))
                        {
                            tp2touch1.Add(polyids[1]);
                        }
                    }
                    foreach (int arcid in tp2arcs2)
                    {
                        int[] polyids = arc_poly2[arcid];
                        if (!tp2touch2.Contains(polyids[0]))
                        {
                            tp2touch2.Add(polyids[0]);
                        }
                        if (!tp2touch2.Contains(polyids[1]))
                        {
                            tp2touch2.Add(polyids[1]);
                        }
                    }
                    //到这应当求一下左右的点哪个包含的相对应的线多
                    int samecount1 = 0;
                    int samecount2 = 0;
                    foreach (int ttt in aimtouchpolys)
                    {
                        if (tp2touch1.Contains(ttt))
                        {
                            samecount1++;
                        }
                        if (tp2touch2.Contains(ttt))
                        {
                            samecount2++;
                        }
                    }
                    int aimpoint2 = -1;
                    if (samecount1 >= samecount2)
                    {
                        aimpoint2 = tp2[0];
                    }
                    else
                    {
                        aimpoint2 = tp2[1];
                    }
                    Geometry aimpoint = topology2.index_points_Pairs[aimpoint2];//这样就获取到了这个正确点的位置
                    List<double> xlist, ylist;
                    getLineXY(lineinSection2, out xlist, out ylist, true);
                    double[] segment1 = new double[4];
                    bool firstorend = true;//这个是用来记录首端点还是尾端点是正确的对应的点的bool变量。
                    double x1, y1, px, py;
                    x1 = xlist[0];
                    y1 = ylist[0];
                    px = aimpoint.GetX(0);
                    py = aimpoint.GetY(0);
                    if (doublequal(x1, px) && doublequal(y1, py))
                    {
                        segment1[0] = xlist[0];
                        segment1[1] = ylist[0];
                        segment1[2] = xlist[1];
                        segment1[3] = ylist[1];
                    }
                    else
                    {
                        int fullcount = xlist.Count;
                        segment1[0] = xlist[fullcount - 1];
                        segment1[1] = ylist[fullcount - 1];
                        segment1[2] = xlist[fullcount - 2];
                        segment1[3] = ylist[fullcount - 2];
                    }
                    Geometry pairsegment = new Geometry(wkbGeometryType.wkbLineString);
                    pairsegment.AddPoint_2D(segment1[0], segment1[1]);
                    pairsegment.AddPoint_2D(segment1[2], segment1[3]);
                    List<Geometry> ttgeoms = new List<Geometry>();
                    ttgeoms.Add(pairsegment);
                    result.Add(id, ttgeoms);
                }
            }
            return result;
        }


        public Dictionary<int, Geometry> getbuffers(Dictionary<int, List<Geometry>> lines,Dictionary<int,int[]> arc_poly1,Dictionary<int,Geometry>polys2, double bufferdistance, int quadeses,SpatialReference spa=null)
            //重载一下，解决buffer边缘突出的问题
            //输入什么呢，输入第一个图的arc_poly作为检查尖灭地层和谁相连的数据考虑。
        {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in lines)
            {
                List<Geometry> geometries = vk.Value;
                Geometry union = new Geometry(wkbGeometryType.wkbPolygon);
                foreach (Geometry ge in geometries)
                {
                    Geometry genew = ge.Buffer(bufferdistance, quadeses);
                    union = union.Union(genew);
                }
                //string wkt;
                //union.ExportToWkt(out wkt);
                result.Add(vk.Key, union);
            }
          // WrapWorker.saveDictionaryGeom(result, @"D:\temp\buffer1.shp", "LithCode",spa);
            Dictionary<int, Geometry> finallist = new Dictionary<int, Geometry>();
            foreach (var vk in result) {//首先遍历所有的buffer
                int id = vk.Key;
                Geometry geom = vk.Value;
               // string wkt;
               // geom.ExportToWkt(out wkt);
                //Console.WriteLine(wkt);
                List<int> touchPoly = new List<int>();
                foreach (var vk2 in arc_poly1) {//在这准备好，目前这个polygon周围的polygonid
                    int[] polyids = vk2.Value;
                    if (polyids[0] == id) {
                        if (!touchPoly.Contains(polyids[1])) touchPoly.Add(polyids[1]);
                    }
                    if (polyids[1] == id) {
                        if (!touchPoly.Contains(polyids[0])) touchPoly.Add(polyids[0]);
                    }
                }
                List<double> xlist, ylist;
                string wkt;
                geom.ExportToWkt(out wkt);//这个地方，如果buffer比较大的话，可能就会出来multipolygon

                getPolygonXY(geom, out xlist, out ylist);//获取buffer中的所有的点
                int pointcount = xlist.Count();
                //int[] linkto = new int[pointcount];
                bool[] useful = new bool[pointcount];
                Geometry union = new Geometry(wkbGeometryType.wkbPolygon);
                foreach (var vk2 in polys2) {
                    union = union.Union(vk2.Value);
                }
                Geometry boundary = union.Boundary();
                List<List<int>> pinsect = new List<List<int>>();
                for (int i = 0; i < pointcount; i++) useful[i] = true ;
                for (int i = 0; i < pointcount; i++) {
                    Geometry point = new Geometry(wkbGeometryType.wkbPoint);
                    double x, y;
                    x = xlist[i];
                    y = ylist[i];
                    point.AddPoint_2D(x, y);
                    List<int> pointinsection = new List<int>();
                    foreach (var vk2 in polys2) {
                        if (vk2.Value.Intersect(point)) {
                            pointinsection.Add(vk2.Key);
                        }
                    }
                    if (boundary.Intersect(point)) {
                        pointinsection.Add(-1);
                    }
                    if (pointinsection.Count == 0) {
                        pointinsection.Add(-1);
                    }
                    foreach (int p in pointinsection) {
                        Console.Write(p.ToString() + ' ');
                    }
                    Console.WriteLine();
                    //咋整呢，比一下和那个包含当前polygontouch的数组
                    foreach (int idt in pointinsection) {

                        if (!touchPoly.Contains(idt)) {
                            useful[i] = false;
                            break;
                        }
                    }

                }
                //在这，已经获得了在外边的有问题的点，把他们换成最近的在中心线上的点就行了
                List<Geometry> centerlines = lines[vk.Key];
                List<double> xCenterline = new List<double>();
                List<double> yCenterline = new List<double>();
                foreach (Geometry theline in centerlines) {
                    List<double> xl, yl;
                    getLineXY(theline, out xl, out yl, true);
                    xCenterline.AddRange(xl);
                    yCenterline.AddRange(yl);
                }
                for (int i = 0; i < pointcount; i++) {
                    if (useful[i] == false) {
                        double mindis = double.MaxValue;
                        int minindex = -1;
                        for (int j = 0; j < xCenterline.Count; j++) {
                            double dis = distance(xlist[i], ylist[i], xCenterline[j], yCenterline[j]);
                            if (dis < mindis) {
                                mindis = dis;
                                minindex = j;
                            }
                        }
                        xlist[i] = xCenterline[minindex];
                        ylist[i] = yCenterline[minindex];
                    }
                }
                bool[] different = new bool[pointcount];//标记一下，是否和相邻的点的坐标相同，如果相同，就标记为true，然后移除所有的true
                for (int i = 0; i < pointcount; i++) { different[i] = false; }
                for (int i = 0; i < pointcount; i++) {//对比一下，然后把相邻相同点都给去掉
                    int t = (i + 1)%pointcount;
                    double x1 = xlist[i];
                    double y1 = ylist[i];
                    double x2 = xlist[t];
                    double y2 = ylist[t];
                    if (doublequal(x1, x2) && doublequal(y1, y2)) {
                        different[i] = true;
                    }
                }
                for (int i = pointcount - 1; i >= 0; i--) {
                    if (different[i]) {
                        xlist.RemoveAt(i);
                        ylist.RemoveAt(i);
                    }
                }
                if (xlist.Count == 0) continue;
                Geometry finalbuffer = createPolyBylist(xlist, ylist);
                finallist.Add(id, finalbuffer);
            }
            return finallist;
        }


        public void createEraseBufferData(string outputpath,string layername,string idfieldname,string bufferpath,string section2path,SpatialReference spatialReference) {
            Layer bufferlayer = getFirstLayerfromPath(bufferpath);
            Layer sectionlayer = getFirstLayerfromPath(section2path);
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(outputpath,null);
            Layer layer2 = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPolygon, null);
           int t= sectionlayer.Erase(bufferlayer, layer2, null, null,null);
            // int t = (int)layer2.GetFeatureCount(0);
            long buffercount = bufferlayer.GetFeatureCount(1);
           
            for (int i = 0; i < buffercount; i++) {
                Feature layer2feature = new Feature(layer2.GetLayerDefn());
                Feature feature = bufferlayer.GetFeature(i);
                Geometry geometry = feature.GetGeometryRef();
                int lith = feature.GetFieldAsInteger(idfieldname);
                layer2feature.SetGeometry(geometry);
                layer2feature.SetField(idfieldname, lith);
                layer2.CreateFeature(layer2feature);
            }
            bufferlayer.Dispose();
            sectionlayer.Dispose();
            layer2.Dispose();
            ds.Dispose(); 
        }
        private Layer getFirstLayerfromPath(string path) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            return layer;
        }
        private Geometry createPolyBylist(List<double> xlist,List<double >ylist) {
            Geometry geometry = new Geometry(wkbGeometryType.wkbPolygon);
            Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            int count = xlist.Count;
            for (int i = 0; i < count; i++) {
                ring.AddPoint_2D(xlist[i], ylist[i]);
            }
            ring.AddPoint_2D(xlist[0], ylist[0]);
            geometry.AddGeometry(ring);
            return geometry;
        }
        private bool doublequal(double a,double b) {
            double t =Math.Abs( a - b);
            if (t < 0.000001) return true;
            return false;
        }
        private double distance(double x1,double y1,double x2,double y2) {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double s = dx * dx + dy * dy;
            s = Math.Sqrt(s);
            return s;
        }
        private void getPolygonXY(Geometry polygon, out List<double> xlist, out List<double> yList)
        {
            Geometry geom1 = polygon.GetGeometryRef(0);
            int count = geom1.GetPointCount();
            xlist = new List<double>();
            yList = new List<double>();
            for (int i = 0; i < count; i++)
            {
                int t = i;
                double x = geom1.GetX(t);
                double y = geom1.GetY(t);
                xlist.Add(x);
                yList.Add(y);
            }
        }
        private void getLineXY(Geometry line,out List<double > xlist,out List<double> yList,bool fromstart) {
            //如果是false，那么就输出它的倒置
            int count = line.GetPointCount();
            xlist = new List<double>();
            yList = new List<double>();
            for (int i = 0; i < count; i++) {
                int t = i;
                if (fromstart == false) {
                    t = count - 1 - i;
                }
                double x= line.GetX(t);
                double y = line.GetY(t);
                xlist.Add(x);
                yList.Add(y);
            }
        }
        private bool pointinlist(Geometry point,List<Geometry>pointlist) {
            foreach (var t in pointlist) {
                if (t.Equal(point))
                    return true;
            }
            return false;
        }
        private int pointinlistindex(Geometry point, List<Geometry> pointlist)
        {
            for(int i=0;i<pointlist.Count; i++)
            {
                Geometry t = pointlist[i];
                if (t.Equal(point))
                    return i;
            }
            return -1;
        }
        private bool equalforintPair(int[] a1,int [] a2) {
            if ((a1[0] == a2[0]) && (a1[1] == a2[1])) return true;
            if ((a1[1] == a2[0]) && (a1[0] == a2[1])) return true;
            return false;
        }
        private List<int> delListValue(List<int> list1,List<int> list2) {
            List<int> result = new List<int>();
            foreach (int v in list1) {
                bool in2list = false;
                foreach (int v2 in list2) {
                    if (v == v2) {
                        in2list = true;
                        break;
                    }
                }
                if (in2list == false) {
                    result.Add(v);
                }
            }
            return result;
        }
    }
}
