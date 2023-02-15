using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{/// <summary>
/// 寻找拓扑变化内容，业务层
/// </summary>
    class FindTopoChangeByMultiLine
    //这个类是采用遍历地层间线的拓扑关系不一致查找方法来进行地层之间拓扑不一致边界线的查找和对应
    {
        const int BoundaryID = 0;
        static public void findTopoChange(Topology topology1, Topology topology2, Dictionary<int, Geometry> polys1, Dictionary<int, Geometry> polys2,
           out List<int[]> topochangeline1, out List<int[]> topochangeline2)
        {//这个函数用来完成整个工作，读取所有的拓扑关系表数据，还有地层数据，输出两个分组了的两剖面的字典topochangeline1 2，以及在这两个字典中对应关系的表lineindexpair
            //第一步，建立地层的邻接关系表
            Dictionary<int, List<int>> toucheslist1 = getPolyTouches(polys1);
            Dictionary<int, List<int>> toucheslist2 = getPolyTouches(polys2);//建立完了
            //第二步，制作出划分两部分地层的表
            //List<int[]> sectionAblock1, sectionAblock2, sectionBblock1, sectionBblock2;
            List<int[]> sectionblock1, sectionblock2;//其中Ablock1 Bblock1是相互对应的地层，block2同理，sectionA，B分别是在哪个剖面中
            //其中存的是每各剖面中的分成两块时候的地层的编号
            //下面就是如何生成这个玩儿
            getCreateBlocksScheme(toucheslist1, toucheslist2, out sectionblock1, out sectionblock2);
            //生成了合法的对应的两块，其中对于List<int[]> 中同样的index,
            int blockcount = sectionblock1.Count;
            topochangeline1 = new List<int[]>();
            topochangeline2 = new List<int[]>();
            for (int i = 0; i < blockcount; i++) {
                int[] block1 = sectionblock1[i];
                int[] block2 = sectionblock2[i];
                List<int> eageRingidlist1, eageRingidlist2;
                /*        if(block1.Length==2) if (block1[0] == 8 && block1[1] == 5)             
                                        Console.WriteLine("misstake");*/
               if (block2.Contains(10) && block2.Contains(4) && block2.Contains(6) && block2.Contains(6) && block2.Contains(7))
                    Console.WriteLine("misstake");
                getEageRingidlist(block1, block2, topology1, polys1, out eageRingidlist1);
                getEageRingidlist(block1, block2, topology2, polys2, out eageRingidlist2);//这样就找到了在图1，图2中，两个按照顺序排列的ring的原来边界线的id
                int[] matchorderRing1, matchorderRing2;
                //调整两个环，让它们便于使用
                bool findonly = getStartLineInRing2(eageRingidlist1.ToArray<int>(), eageRingidlist2.ToArray<int>(), topology1.arcs_poly_Pairs, topology2.arcs_poly_Pairs,
                    out matchorderRing1, out matchorderRing2);
                //findonly指的是，这个开头是不是唯一的，如果不是唯一的，那么就不是很好。。。
                //先不管了，我觉得每个环基本上都能有个唯一对应吧，如果是完全对不上的环那就不要他就完了（或者后续出了bug再说
                List<int[]> ids1list, ids2list;
                int[] ids1, ids2;
                bool successMatch = getMatchRingType(matchorderRing1, matchorderRing2, topology1.arcs_poly_Pairs, topology2.arcs_poly_Pairs, out ids1list, out ids2list);
                if (successMatch) {
                    int n = ids1list.Count;
                    for (int j = 0; j < n; j++) {//一个循环给每一组检查然后加进去就对劲了
                        ids1 = ids1list[j];
                        ids2 = ids2list[j];
                        bool initbool = checkArrinit(topochangeline1, topochangeline2, ids1, ids2);
                        //判断一下是不是有完全相同的，如果有，就不要加入进去。
                        if (initbool == false)
                        {
                            topochangeline1.Add(ids1);
                            topochangeline2.Add(ids2);
                        }
                    }
                }
            }
        }
        /*public enum MatchRingType : ushort { 
                FullEqual=1,
                OneDifference=2,
                NotEqual=3,
                ManyDifference=4
        }*/
        static bool checkArrinit(List<int[]> topolines1, List<int[]> topolines2, int[] ids1, int[] ids2) {
            int n = topolines1.Count;
            bool equal1 = false;
            bool equal2 = false;
            for (int i = 0; i < n; i++) {
                int[] tempids1 = topolines1[i];
                int[] tempids2 = topolines2[i];
                bool e1 = Enumerable.SequenceEqual<int>(tempids1, ids1);
                bool e2 = Enumerable.SequenceEqual<int>(tempids2, ids2);
                if (e1 && e2) {
                    equal1 = true;
                    equal2 = true;
                }
            }
            if (equal1 && equal2) {
                return true; }
            return false;
        }
        static bool getMatchRingType(int[] matchorderRing1, int[] matchorderRing2, Dictionary<int, int[]> arc_poly1, Dictionary<int, int[]> arc_poly2,
            out List<int[]> ids1, out List<int[]> ids2) 
        {
            ///就是根据环上的这个顺序和线两侧的面，来比较它们的顺序。目前为止这个线的头应该是对齐了的
            ///其实只是匹配一个模式，所以只要返回找到还是没找到，并不需要区分有什么模式
            ///这个模式就是，在这个环上，从头到中间，从尾到中间，然后中间剩下的块，
            ///如果直接从头到尾一下贯通全对应上了，那么说明实际上是拓扑完全对应的，就不需要从尾巴到中间去做。
            ///中间剩下的块有几种可能，第一种，就是1-1，那么没问题，就ok，是拓扑变动位置
            ///第二种，n-0,这种直接pass
            ///第三种 n-m，这种最麻烦，如果是两个连续不对应块，那么实际上就应该用它做对应
            ///这种只能是勉强做，就是找是不是有对应的，就是如果遇到对应的，那么就说明这并不是一个连续块，pass
            ///如果找不到对应，那就说明就是一个和1-1一样的拓扑变动位置

            //第一步，先从头到尾去推
            int startp1 = 0, startp2 = 0;
            int id1, id2;
            bool mark = true;
            int ring1length = matchorderRing1.Length;
            int ring2length = matchorderRing2.Length;
            ids1 = new List<int[]>();
            ids2 = new List<int[]>();
/*            if (matchorderRing1.Contains(19)) {
                Console.WriteLine("debug");
            }*/
            // ids1 = null;
            // ids2 = null;
            while (mark) {
                id1 = matchorderRing1[startp1];
                id2 = matchorderRing2[startp2];
                int[] polys1 = arc_poly1[id1];
                int[] polys2 = arc_poly2[id2];
                bool compareresult = compareArc_polys(polys1, polys2);
                if (compareresult == false) {
                    //如果比较下一个段是不匹配的，就做一下处理
                    mark = false;
                    startp1--;//把这个下标还原到对应的位置
                    startp2--;
                }
                if (mark) {//如果对上了，那么就继续往前走

                    startp1++;
                    startp2++;
                    if (startp1 == ring1length && startp2 == ring2length) {
                        return false;//如果都走到头了，那么说明就是完全对应的，不是我们需要的状态，返回一个false
                    }
                    if (startp1 == ring1length) {
                        return false;//只有一个到头了，那么说明是0-n
                    }
                    if (startp2 == ring2length)
                    {
                        return false;//只有一个到头了，那么说明是0-n
                    }
                }

            }
            //以上是从头到尾去捋，如果能解决（找到不满足条件的状态），就弹出去了，如果不能解决，那么就会到这
            //下面需要处理的情况就是,从后先前做了，
            int endp1 = matchorderRing1.Length - 1, endp2 = matchorderRing2.Length - 1;
            bool mark2 = true;
            while (mark2) {
                id1 = matchorderRing1[endp1];
                id2 = matchorderRing2[endp2];
                int[] polys1 = arc_poly1[id1];
                int[] polys2 = arc_poly2[id2];
                bool compareresult = compareArc_polys(polys1, polys2);
                if (compareresult == false)
                {
                    //如果比较下一个段是不匹配的，就做一下处理
                    mark2 = false;
                    endp1++;//把这个下标还原到对应的位置
                    endp2++;
                }
                if (mark2) {
                    //如果匹配上了，就退一格，继续往前走
                    endp1--;
                    endp2--;
                    //在这就不用担心数组越界，因为上一种情况已经找到了确实有不匹配的地方
                }
            }
            //到此为止，已经找到了，中间有多少匹配不上的。
            int count1 = endp1 - 1 - startp1;//找到不匹配的数量，第一环数量
            int count2 = endp2 - 1 - startp2;//第二环数量
            if (count1 == 0 || count2 == 0) {//如果其中有个直接通了，就肯定是不对的 ，就是1:0
                return false;
            }
            List<int> ids1list;
            List<int> ids2list;
            //没有通的，那就看看有没有一个的，有一个的就不用管了
            if (count1 == 1 || count2 == 1) {//如果有个ring之间的空挡是1，那么肯定是一个结果
                                             //不对，这是bug，有可能存在两个1：0夹着一个正常对应弧段的情况，参考阿拉巴马剖面的最上层
                #region 判断这一个弧段能不能正常对应 如果能够正常对应
                ids1list = new List<int>();
                ids2list = new List<int>();
                for (int j = startp1 + 1; j < endp1; j++)
                {
                    ids1list.Add(matchorderRing1[j]);
                }
                for (int j = startp2 + 1; j < endp2; j++)
                {
                    ids2list.Add(matchorderRing2[j]);
                }
                int temp = 0;
                
                for (int i = 0; i < ids1list.Count; i++)
                {
                    int id1temp = ids1list[i];
                    int id2temp;
                    int[] polys1 = arc_poly1[id1temp];

                    //bool findonly = false;

                    int polyid2 = -1;
                    
                    for (int j = 0; j < ids2list.Count; j++)
                    {//如果检查到了
                        id2temp = ids2list[j];
                        int[] polys2 = arc_poly2[id2temp];
                        bool compareresult = compareArc_polys(polys1, polys2);
                        if (compareresult == true)
                        {
                            temp++;
                            polyid2 = id2temp;
                          
                            break;
                        }
                    }
                    if (temp > 0)
                    {
                        return false;
                    }

                }
                #endregion


                ids1list = new List<int>();
                ids2list = new List<int>();
                for (int j = startp1 + 1; j < endp1; j++)
                {
                    ids1list.Add(matchorderRing1[j]);
                }
                for (int j = startp2 + 1; j < endp2; j++)
                {
                    ids2list.Add(matchorderRing2[j]);
                }
                ids1.Add(ids1list.ToArray<int>());//这种只有唯一的拓扑不对应的情况下，就直接返回一个int[] 构成的list就对了
                ids2.Add(ids2list.ToArray<int>());
                return true;
            }
            //这时候count1 count2就没有0 1 了，
            //先获取一下
            ids1list = new List<int>();
            ids2list = new List<int>();
            for (int j = startp1 + 1; j < endp1; j++)
            {
                ids1list.Add(matchorderRing1[j]);
            }
            for (int j = startp2 + 1; j < endp2; j++)
            {
                ids2list.Add(matchorderRing2[j]);
            }
            int countfind = 0;
            int midsite1 = 0, midsite2 = 0;
            for (int i = 0; i < ids1list.Count; i++)
            {
                int id1temp = ids1list[i];
                int id2temp;
                int[] polys1 = arc_poly1[id1temp];

                //bool findonly = false;

                int polyid2 = -1;
                midsite1 = startp1 + 1 + i;
                for (int j = 0; j < ids2list.Count; j++) {//如果检查到了
                    id2temp = ids2list[j];
                    int[] polys2 = arc_poly2[id2temp];
                    bool compareresult = compareArc_polys(polys1, polys2);
                    if (compareresult == true) {
                        countfind++;
                        polyid2 = id2temp;
                        midsite2 = startp2 + 1 + j;
                        break;
                    }
                }
                if (countfind > 0)
                {
                    break;
                }

            }
            if (countfind == 1)
            {
                //这种时候就是找到了，在这个环中间不匹配的部分实际上是有匹配的情况的，那么怎么办呢，就给它分成两半，递归前进
                int[] leftRing1, leftRing2, rightRing1, rightRing2;
                leftRing1 = new int[midsite1];
                leftRing2 = new int[midsite2];
                rightRing1 = new int[matchorderRing1.Length - midsite1];
                rightRing2 = new int[matchorderRing2.Length - midsite2];
                for (int j = 0; j < midsite1; j++)
                {
                    leftRing1[j] = matchorderRing1[j];
                }
                for (int j = 0; j < midsite2; j++)
                {
                    leftRing2[j] = matchorderRing2[j];
                }
                for (int j = midsite1; j < matchorderRing1.Length; j++)
                {
                    rightRing1[j - midsite1] = matchorderRing1[j];
                }
                for (int j = midsite2; j < matchorderRing2.Length; j++)
                {
                    rightRing2[j - midsite2] = matchorderRing2[j];
                }
                List<int[]> leftids1, leftids2, rightids1, rightids2;
                bool markleft = getMatchRingType(leftRing1, leftRing2, arc_poly1, arc_poly2, out leftids1, out leftids2);
                bool markright = getMatchRingType(rightRing1, rightRing2, arc_poly1, arc_poly2, out rightids1, out rightids2);
                if (markleft)
                {
                    ids1.AddRange(leftids1);
                    ids2.AddRange(leftids2);
                }
                if (markright)
                {
                    ids1.AddRange(rightids1);
                    ids2.AddRange(rightids2);
                }
                return markleft || markright;//返回这两个的或结果，因为只要有一边是能够输出有意义的数据的，就说明这个过程是对的。
            }
            else
            {
                ids1.Add(ids1list.ToArray<int>());//这种只有唯一的拓扑不对应的情况下，就直接返回一个int[] 构成的list就对了
                ids2.Add(ids2list.ToArray<int>());
                return true;
            }
        }
        static bool getStartLineInRing2(int[] eageRingidlist1,int[] eageRingidlist2,Dictionary<int,int[]>arc_poly1, Dictionary<int, int[]> arc_poly2,out int[] matchorderRing1,out int[] matchorderRing2) {
            //把两个环的起点调整成相同的，而且不要一上来就不对应
            int startmatchid = -1;
            int startmatchid2 = -1;
            //有个危险，就是开头弧段并不是唯一对应在其中的，这可咋办呢，我想，这样，首先应该验证它是不是唯一，用唯一的对应的作为这个线的起始
            bool findonly = false;
            for (int i = 0; i < eageRingidlist1.Length; i++) {
                int[] polys = arc_poly1[eageRingidlist1[i]];
                bool findsameInRing1 = false;
                for (int j = 0; j < eageRingidlist1.Length;j++)
                    if (i != j)
                    {
                        int[] polystt = arc_poly1[eageRingidlist1[j]];
                        bool comparet = compareArc_polys(polys, polystt);
                        if (comparet) {
                            findsameInRing1 = true;
                        }
                    }
                if (findsameInRing1) continue;//如果这个第一个位置的线在图1环中有重复的，就不用它；
                int samearccount = 0;
                for(int j =0;j< eageRingidlist2.Length;j++) {
                    int id2 = eageRingidlist2[j];
                    int[] polys2 = arc_poly2[id2];
                    bool compare1 = compareArc_polys(polys, polys2);
                    if (compare1 == true) {
                        startmatchid = i;
                        startmatchid2 = j;
                        samearccount++;
                        //break;
                    }
                }
                if (startmatchid != -1&& samearccount==1) {
                    findonly = true;
                    break;
                }
            }
            if (startmatchid == -1) Console.WriteLine("程序未找到对应情况，出了大问题");
            //下面就是按照上面找的对应状态，捋顺
            matchorderRing1 = new int[eageRingidlist1.Length];
            matchorderRing2 = new int[eageRingidlist2.Length];
            int p = 0;
            for (int i = startmatchid; i < eageRingidlist1.Length; i++) {
                p = i - startmatchid;
                matchorderRing1[p] = eageRingidlist1[i];
            }
            
            for (int i = 0; i < startmatchid; i++) {
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
        static bool compareArc_polys(int[] polys1,int[] polys2) {
            if ((polys1[0] == polys2[0] && polys1[1] == polys2[1]) || (polys1[0] == polys2[1] && polys1[1] == polys2[0])) {
                return true;
            }
            return false;
        }
        static Dictionary<int, List<int>> getPolyTouches(Dictionary<int, Geometry> polys) {
            //生成剖面中的各个地层的touches的表
            Dictionary<int, List<int>> touchesList = new Dictionary<int, List<int>>();
            Geometry superSection = new Geometry(wkbGeometryType.wkbPolygon);

            foreach (var poly in polys) {
                List<int> touches = new List<int>();
                
                foreach (var poly2 in polys) {
                    if (poly.Key == poly2.Key) continue;//如果是同个面，就跳过
                    if (touchesList.ContainsKey(poly2.Key)) {//检查一下这两个面是不是做过了，如果做过了，那么直接加进去就完了
                        if (touchesList[poly2.Key].Contains(poly.Key)) {
                            touches.Add(poly2.Key);
                            continue;
                        } 
                    }
                    //特殊情况处理完毕，现在是正常情况
                    Geometry intersectGeom = poly.Value.Intersection(poly2.Value);
                    if (intersectGeom.IsEmpty() == true) continue;//不相交，就下一位
                    wkbGeometryType resulttype = intersectGeom.GetGeometryType();
                    //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
                    if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbGeometryCollection) {
                       if (touches.Contains(poly2.Key)==false) 
                            touches.Add(poly2.Key);
                    }
                }
               //touches表加入结果表
                touchesList.Add(poly.Key, touches);
            }
            //处理与开放边界相交的情况
            foreach (var poly in polys)
            {
                superSection = superSection.Union(poly.Value);
            }
            Geometry boundary = superSection.Boundary();
            List<int> boundarytouch = new List<int>();
            foreach (var poly in polys)
            {
                Geometry intersectGeom = poly.Value.Intersection(boundary);//每个都和外边界做相交
                wkbGeometryType resulttype = intersectGeom.GetGeometryType();
                if (intersectGeom.IsEmpty() == true) continue;
                //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
                if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbGeometryCollection)
                {
                    touchesList[poly.Key].Add(BoundaryID);
                    boundarytouch.Add(poly.Key);
                }
            }
            touchesList.Add(BoundaryID, boundarytouch);//把外边界也加进去好了。
            return touchesList;
        }
        static void getCreateBlocksScheme(Dictionary<int, List<int>> toucheslist1, Dictionary<int, List<int>> toucheslist2,
            out List<int[]> sectionblock1, out List<int[]> sectionblock2)
        {
            sectionblock1 = new List<int[]>();
            sectionblock2 = new List<int[]>();
            //sectionBblock1 = new List<int[]>();///似乎没必要弄两个是吧，就一个block1，一个block2 就够了，毕竟是成对出现的
            //sectionBblock2 = new List<int[]>();
            int geomcount = toucheslist1.Count;
            int[] geomarray = toucheslist1.Keys.ToArray<int>();
            int maxcombination = geomcount / 2;
            //数据中应该确保这两个toucheslist里边的geometry的数量是一样的。
            for (int i = 1; i <= maxcombination; i++) {//因为除了获得指定数量的组合，还要
                List<int[]> combin1 = GetCombination(geomarray, i);
                
                foreach (int[] arr in combin1) {
                    if (arr.Length == 1 && arr[0] == BoundaryID) continue;
                    //if (arr[0] == BoundaryID) continue;
                    //if (arr.Contains(BoundaryID)) continue;
                    List<int> oppositegeom = new List<int>(geomarray);
                    foreach (int t in arr) {
                        oppositegeom.Remove(t);//做出对立的组合
                    }
                    int[] arr2 = oppositegeom.ToArray();
                    if (arr2.Length == 1 && arr2[0] == BoundaryID) continue;
                    /*                    if (arr.Length == 3 ) {if (arr[0] == 101 & arr[1] == 6 && arr[2] == 4)
                                            Console.WriteLine("getCreateBlocksScheme Test");
                                        }*/
                    bool checksc=  checkStratumCombination(arr, arr2, toucheslist1, toucheslist2);
                    if (checksc) {
                        sectionblock1.Add(arr);//这一组证明了可以连通，那么就愉快地给它加进去
                        sectionblock2.Add(arr2);
                    }
                }
            }
            
        }
        static bool checkStratumCombination(int[] block1,int[] block2, Dictionary<int, List<int>> toucheslist1, Dictionary<int, List<int>> toucheslist2) {
            //这个函数用来检查这个排列是否应是一个只分出两块地层的情况
            //具体做法就是，把两个block中的地层都分别都加入并查集，看看最后是不是只剩两个连通块，如果是两个，那么说明ok，如果不是，就说明不ok
            List<int> t1 = new List<int>(block1);
            List<int> t2 = new List<int>(block2);
            t1.Remove(BoundaryID);
            t2.Remove(BoundaryID);
            block1 = t1.ToArray();
            block2 = t2.ToArray();
            int maxid=0;
            foreach (int t in block1) {
                if (t > maxid) maxid = t;}
            foreach (int t in block2)
            {
                if (t > maxid) maxid = t;
            }
            maxid++;
            UnionSet unionSet1 = new UnionSet(maxid);
            UnionSet unionSet2 = new UnionSet(maxid);
            bool[] used1 = new bool[maxid];
            bool[] used2 = new bool[maxid];
            for (int i = 0; i < maxid; i++) {
                used1[i] = true;
                used2[i] = true;
            }
            //以下代码为block1，block2分别在2图中构建并查集
            #region 构建并查集过程
            foreach (int t in block1)
            {
               // int fa = unionSet1.Find(t);
                List<int> touch = toucheslist1[t];
                foreach (int target in touch)
                {
                    if (block1.Contains<int>(target))
                    {
                        //int targetfa = unionSet1.Find(target);
                        unionSet1.Unite(t, target);
                        //used1[target]
                    }
                }
            }
            foreach (int t in block2)
            {
                // int fa = unionSet1.Find(t);
                List<int> touch = toucheslist1[t];
                foreach (int target in touch)
                {
                    if (block2.Contains<int>(target))
                    {
                        //int targetfa = unionSet1.Find(target);
                        unionSet1.Unite(t, target);
                        //used1[target]
                    }
                }
            }
            foreach (int t in block1)
            {
                // int fa = unionSet1.Find(t);
                List<int> touch = toucheslist2[t];
                foreach (int target in touch)
                {
                    if (block1.Contains<int>(target))
                    {
                        //int targetfa = unionSet1.Find(target);
                        unionSet2.Unite(t, target);
                        //used1[target]
                    }
                }
            }
            foreach (int t in block2)
            {
                // int fa = unionSet1.Find(t);
                List<int> touch = toucheslist2[t];
                foreach (int target in touch)
                {
                    if (block2.Contains<int>(target))
                    {
                        //int targetfa = unionSet1.Find(target);
                        unionSet2.Unite(t, target);
                        //used1[target]
                    }
                }
            }
            #endregion
            //下面开始检查block1，block2是否唯一
            int samefa1, samefa2;
/*            if (block1[0] == BoundaryID) 
            {
                samefa1 = unionSet1.Find(block1[1]);
            }
            else
            {
                samefa1 = unionSet1.Find(block1[0]);
            }
            if (block1[0] == BoundaryID)
            {
                samefa2 = unionSet1.Find(block2[1]);
            }
            else
            {
                samefa2 = unionSet1.Find(block2[0]);
            }*/
            samefa1 = unionSet1.Find(block1[0]);
            samefa2 = unionSet1.Find(block2[0]);
            bool same1 = true, same2 = true;
            for(int i=1;i<block1.Length;i++) {//检查一下图1block1
                int fa1 = unionSet1.Find(block1[i]);
                if (fa1 != samefa1) {
                    same1 = false;
                    break;
                }
            }
            for (int i = 1; i < block2.Length; i++)//检查一下图1block2
            {
                int fa2 = unionSet1.Find(block2[i]);
                if (fa2 != samefa2)
                {
                    same2 = false;
                    break;
                }
            }
            if (same1 == false) return false;
            if (same2 == false) return false;
            samefa1 = unionSet2.Find(block1[0]);
            samefa2 = unionSet2.Find(block2[0]);
            same1 = true;
            same2 = true;
            for (int i = 1; i < block1.Length; i++)
            {//检查一下图2block1
                int fa1 = unionSet2.Find(block1[i]);
                if (fa1 != samefa1)
                {
                    same1 = false;
                    break;
                }
            }
            for (int i = 1; i < block2.Length; i++)//检查一下图2block1
            {
                int fa2 = unionSet2.Find(block2[i]);
                if (fa2 != samefa2)
                {
                    same2 = false;
                    break;
                }
            }
            if (same1 == false) return false;//如果有不成块的就返回false
            if (same2 == false) return false;
            return true;
        }
        static void getEageRingidlist(int[] block1, int[] block2, Topology topology,Dictionary<int,Geometry>polys, out List<int> eageRingidlist) {
            
            bool inner1or2 = false;
            List<int> block1list, block2list;
            if (block1.Contains<int>(BoundaryID)) inner1or2 = true;//看看外部空间被包围在哪里了，一般情况下会形成一个环
            else if (block2.Contains<int>(BoundaryID)) inner1or2 = false;
            if (inner1or2 == true)
            {
                block1list = new List<int>(block1);
                block2list = new List<int>(block2);
            }
            else
            {
                block1list = new List<int>(block2);
                block2list = new List<int>(block1);
            }//这样block1list就是外包的面，block2list就是内部面
            //做的时候，就是要block1list与block2list相交，然后就是外边界与block2list相交，这就是一个完整的环
            Geometry geomblock1 = unionPolysByIds(polys, block1list.ToArray<int>());
            Geometry geomblock2 = unionPolysByIds(polys, block2list.ToArray<int>());
            Geometry geomfull = unionPolysByIds(polys, polys.Keys.ToArray<int>());
            Geometry boundarygeom = geomfull.GetBoundary();
            Geometry block1touch2 = geomblock1.Intersection(geomblock2);//做一下边界线的获取
            Geometry boundarytouch2 = geomblock2.Intersection(boundarygeom);
            //获取了两条边界线
            //下面就是两条边界线中拿到线。。我得想想这个咋搞啊，直接intersection好像不是很好啊
            Dictionary<int, Geometry> lines = topology.index_arcs_Pairs;
            List<int> disorderRing = new List<int>();
            foreach (var line in lines) {//比较一下这些线，看看谁在这个环上，id加入disorderline里边
                int lineid = line.Key;
              // if (block1.Length == 2 && block1[0] == 4 && block1[1] == 0 && lineid == 7) Console.WriteLine("warning");
                Geometry linegeom = line.Value;
                double linelength = linegeom.Length();
                Geometry geom1= block1touch2.Intersection(linegeom);
                Geometry geom2 = boundarytouch2.Intersection(linegeom);
                bool emp1, emp2;
               // emp1 = geom1.IsEmpty();//如果是空的geometry就返回true
               // emp2 = geom2.IsEmpty();

                wkbGeometryType type1 = geom1.GetGeometryType();
                wkbGeometryType type2 = geom2.GetGeometryType();
                if (type1 == wkbGeometryType.wkbLineString || type1 == wkbGeometryType.wkbMultiLineString)
                {
                    emp1 = false;
                }
                else {
                    emp1 = true;
                }
                if (type2 == wkbGeometryType.wkbLineString || type2 == wkbGeometryType.wkbMultiLineString)
                {
                    emp2 = false;
                }
                else
                {
                    emp2 = true;
                }
               string wkt1, wkt2, wkt3;
                geom1.ExportToWkt(out wkt1);
                geom2.ExportToWkt(out wkt2);
                //
                if (emp1 == false) {
                    Geometry line1 = Ogr.ForceToLineString(geom1);//给它组成一条线，intersection结果往往是multiline
                    line1 = Ogr.ForceToLineString(line1);
                    wkbGeometryType geom1type = line1.GetGeometryType();
                    if (geom1type == wkbGeometryType.wkbLineString) {
                        disorderRing.Add(lineid);
                    }
                }
                if (emp2 == false)
                {
                    Geometry line1 = Ogr.ForceToLineString(geom2);//给它组成一条线，intersection结果往往是multiline
                    line1 = Ogr.ForceToLineString(line1);
                    wkbGeometryType geom2type = line1.GetGeometryType();
                    string wkttt;
                    line1.ExportToWkt(out wkttt);
                    if (geom2type == wkbGeometryType.wkbLineString)//只要能练成一条线，就说明确实是在这个环上的
                    {
                        disorderRing.Add(lineid);
                    }
                }
            }
            //到此为止找到了没有顺序的这个环，下面就是怎么能够把这个环给搞定
            //方法就是，获取一个线的首尾，从
            int[] idorder = new int[disorderRing.Count];
            bool[] orderindex = new bool[disorderRing.Count];
            for (int i = 0; i < disorderRing.Count; i++) orderindex[i] = false;
            List<int> disorderidTemp = new List<int>(disorderRing.ToArray<int>());
            idorder[0] = disorderRing[0];//反正是个环，随机给一个首线段
            disorderidTemp.Remove(disorderRing[0]);
            for (int i = 1; i < disorderRing.Count; i++) {
                //if (orderindex[i] == true) continue;
                //int disorderid = disorderRing[i];
                int preid = idorder[i - 1];
                int[] endpoints = topology.arcs_points_Pairs[preid];
                int endpoint1 = endpoints[0];
                int endpoint2 = endpoints[1];
                List<int> endpoint1touch = topology.points_arcs_Pairs[endpoint1];
                List<int> endpoint2touch = topology.points_arcs_Pairs[endpoint2];//拿到两个端点的这个相连的线
                int findid = -2;
                foreach (int id in disorderidTemp) {
                    if (endpoint1touch.Contains(id)) {
                        findid = id;
                    }
                }
                if (findid == -2) {
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
            for (int i = 0; i < idorder.Length; i++) {
                eagerings.Add(topology.index_arcs_Pairs[idorder[i]]);
            }
            bool clockwise = checkclockwise(eagerings);//检查一下这些线的中心点是否是逆时针排列的，如果不是统一的方向，将来顺逆时针反了就不对付了
            if (clockwise != false) {//如果这些线不是按照逆时针排列，就给它倒置一下，保证是逆时针排列
                int tn = idorder.Length;
                for (int i = 0; i < tn / 2; i++) {
                    swap(ref idorder[i], ref idorder[tn - 1 - i]);                
                }
            }
            eageRingidlist = new List<int>(idorder);
        }
        static void swap(ref int a,ref int b) {
            int t = a;
            a = b;
            b = t;
        }
        static bool checkclockwise(List<Geometry> eages) {//顺时针是true，逆时针是false 
            //使用这个一定要保证这些几何图形是单条的线
            //通过每条线的中心相连之后是不是顺时针，判断这些边是不是顺时针构成的，
            double s = 0;
            int n = eages.Count;
            double[] x = new double[n];
            double[] y = new double[n];
            for(int i=0;i<n; i++) {
                Geometry eage = eages[i];
                int pointcount = eage.GetPointCount();
                double xsum = 0, ysum = 0;
                for (int j = 0; j < pointcount; j++) {
                    double x1 = eage.GetX(j);
                    double y1 = eage.GetY(j);
                    xsum += x1;
                    ysum += y1;
                }
                x[i] = xsum / pointcount;
                y[i] = ysum / pointcount;
            }
            for (int i = 0; i < n - 1; i++) {
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
        static Geometry unionPolysByIds(Dictionary<int ,Geometry>polys,int[] ids) {
            Geometry result = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (int id in ids) {
                if (id == BoundaryID) continue;
                result = result.Union(polys[id]);
            }
            return result;
        }
        public static void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }
        /// <summary>
        /// 求数组中n个元素的组合
        /// </summary>
        /// <param name="t">所求数组</param>
        /// <param name="n">元素个数</param>
        /// <returns>数组中n个元素的组合的范型</returns>
        public static List<int[]> GetCombination(int[] t, int n)
        {
            if (t.Length < n)
            {
                return null;
            }
            int[] temp = new int[n];
            List<int[]> list = new List<int[]>();
            GetCombination(ref list, t, t.Length, n, temp, n);
            return list;
        }
        /// 递归算法求数组的组合(私有成员)
        /// </summary>
        /// <param name="list">返回的范型</param>
        /// <param name="t">所求数组</param>
        /// <param name="n">辅助变量</param>
        /// <param name="m">辅助变量</param>
        /// <param name="b">辅助数组</param>
        /// <param name="M">辅助变量M</param>
        /// 组合是干嘛用的呢，是用来生成新组合，然后判断这个组合下的这些面能不能合成两个需要用的块
        /// 
        private static void GetCombination(ref List<int[]> list, int[] t, int n, int m, int[] b, int M)
        {
            for (int i = n; i >= m; i--)
            {
                b[m - 1] = i - 1;
                if (m > 1)
                {
                    GetCombination(ref list, t, i - 1, m - 1, b, M);
                }
                else
                {
                    if (list == null)
                    {
                        list = new List<int[]>();
                    }
                    int[] temp = new int[M];
                    for (int j = 0; j < b.Length; j++)
                    {
                        temp[j] = t[b[j]];
                    }
                    list.Add(temp);
                }
            }
        }
    }
}
