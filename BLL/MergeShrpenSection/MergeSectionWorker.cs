using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OSR;
using OSGeo.OGR;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 合并相邻尖灭剖面，业务层
    /// </summary>
  public  class MergeSectionWorker
    {
        static public Dictionary<int, Geometry> findsharpenandMerge(Dictionary<int,Geometry> fromegeoms,Dictionary<int,Geometry>togeoms,out Dictionary<int,List<int>>shpPairs,out List<int> sharpenids) {
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            //shpPairs = new Dictionary<int, List<int>>();
            List<int> sharpenlist1;
            List<int> sharpenlist2;
            //找到尖灭地层
            getSharpenList(fromegeoms.Keys.ToList<int>(), togeoms.Keys.ToList<int>(), out sharpenlist1, out sharpenlist2);
            //找到相邻尖灭地层的id
            Dictionary<int,List<int>> idgroups= findShrpenGroup(fromegeoms, sharpenlist1);
            shpPairs = idgroups;
            //把它们给合并，返回尖灭地层合并的结果
            Dictionary<int, Geometry> mergesharpens = mergeSharpen(fromegeoms, idgroups);
            //初始化结果
            foreach (var vk in fromegeoms) {
                result.Add(vk.Key,vk.Value);
            }
            Dictionary<int, Geometry> orisharpengeoms = new Dictionary<int, Geometry>(); ;
            foreach (int i in sharpenlist1) {
                orisharpengeoms.Add(i, fromegeoms[i]);
            }
            sharpenids = getOrderByArea(orisharpengeoms);
            //把尖灭地层给remove了
            foreach (var vk in idgroups) {
                List<int> templist = vk.Value;
                foreach (int ttt in templist) {
                    result.Remove(ttt);
                }
            }
            //新增的地层给加进来
            foreach(var vk in mergesharpens) {
                result.Add(vk.Key, vk.Value);
            }
            return result;
        }
        static private List<int> getOrderByArea(Dictionary<int, Geometry> geoms)
        {
            //做一个排序，没问题
            List<int> result = new List<int>();
            Dictionary<double, int> area_id = new Dictionary<double, int>();
            foreach (var vk in geoms)
            {
                int id = vk.Key;
                Geometry geom = vk.Value;
                double area = geom.GetArea();
                area_id.Add(area, id);
            }
            List<double> arealist = area_id.Keys.ToList<double>();
            arealist.Sort();
            int count = arealist.Count;
            for (int i = 0; i < count; i++)
            {
                result.Add(area_id[arealist[i]]);
            }
            return result;
        }
        static public Dictionary<int,Geometry> mergeSharpen(Dictionary<int,Geometry> origeoms,Dictionary<int,List<int>>sharpenGroup ) {
            //这就是把他们合成为一个geom的方法
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in sharpenGroup) {
                List<int> groupids = vk.Value;
                int id = vk.Key;
                Geometry uniongeom = new Geometry(wkbGeometryType.wkbPolygon);
                foreach (int idt in groupids) {
                    Geometry ggg = origeoms[idt];
                    uniongeom = uniongeom.Union(ggg);
                }
                result.Add(id, uniongeom);
            }
            return result;
        }
        static public void getSharpenList(List<int> idlist1, List<int> idlist2, out List<int> sharpenlist1, out List<int> sharpenlist2) {
            sharpenlist1 = new List<int>();
            sharpenlist2 = new List<int>();
            List<int> commonlist = compare(idlist1, idlist2);
            sharpenlist1 = delList(idlist1, commonlist);
            sharpenlist2 = delList(idlist2, commonlist);
        }
        static public Dictionary<int, List<int>> findShrpenGroup(Dictionary<int, Geometry> Geoms, List<int> sharpenid) {
            //这个方法是通过一个尖灭地层id的list，和所有的geom的字典，给生成一个需要合并的尖灭地层的dic
            Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();
            Dictionary<int, Geometry> sharpens = new Dictionary<int, Geometry>();
            foreach (int ss in sharpenid) {
                sharpens.Add(ss, Geoms[ss]);
            }
            //弄个int数组，把不连续的地层的id给映射成连续的
            int[] matchid = sharpens.Keys.ToArray<int>();
            //并查集需要的前驱数组
            int[] pre = new int[matchid.Length];
            for (int i = 0; i < matchid.Length; i++) {
                pre[i] = i;
            }
           // List<int> intheshlist = new List<int>();
            int pointer = 0;
            //准备用并查集存储那些geom应该放一起
            for (int i = 0; i < matchid.Length; i++) {
                pointer = i;
                List<int> touchesids = new List<int>();
                Geometry geomnow = Geoms[matchid[i]];
                for (int j = 0; j < pointer; j++) {
                    //当前指针指向的这个面与前面所有已经遍历过的面做intersection,或者touches，对，应该是touches，
                    Geometry ge = Geoms[matchid[j]];
                    bool tou = ge.Intersect(geomnow);
                    if (tou) {
                        touchesids.Add(j);
                    }
                }
                foreach (int id in touchesids) {
                    joinb(id, pointer, ref pre);
                }
            }
            for (int i = 0; i < pre.Length; i++) {
                int idt = find(i,ref pre);
                bool contianb = result.Keys.Contains<int>(matchid[ idt]);
                if (contianb == false)
                {
                    List<int> listnew = new List<int>();
                    listnew.Add(matchid[i]);
                    result.Add(matchid[ idt], listnew);
                }
                else {
                    result[matchid[idt]].Add(matchid[i]);
                }
            }
            return result;
        }
        static private int find(int x, ref int[] pre) {
           if (pre[x] == x) {return x;}
            return pre[x] = find(pre[x],ref pre);
        }
        static private void joinb(int x, int y, ref int[] pre) {
            int fx = find(x, ref pre);
            int fy = find(y, ref  pre);
            if (fx != fy) {
                pre[fx] = fy;
            }
        }


        static public List<int> compare(List<int> list1, List<int> list2)
        {//输入两个intlist，返回他们共同的唯一的内容
            List<int> result = new List<int>();
            int count1 = list1.Count();
            int count2 = list2.Count();
            for (int i = 0; i < count1; i++)
            {
                int c1 = list1[i];
                for (int j = 0; j < count2; j++)
                {
                    int c2 = list2[j];
                    if ((c1 == c2) && (!result.Contains(c1)))
                    {
                        result.Add(c1);
                        break;
                    }
                }

            }
            return result;
        }
       static  public List<int> delList(List<int> list1, List<int> list2)//输出的是，list1去掉list2后的结果
        {
            List<int> result = new List<int>();
            int count1 = list1.Count();
            int count2 = list2.Count();
            for (int i = 0; i < count1; i++)
            {
                int c1 = list1[i];
                bool same = false;
                for (int j = 0; j < count2; j++)
                {
                    int c2 = list2[j];
                    if (c1 == c2)
                    {
                        same = true;
                    }
                }
                if (same == false)
                {
                    result.Add(c1);
                }

            }
            return result;
        }
    }
}
