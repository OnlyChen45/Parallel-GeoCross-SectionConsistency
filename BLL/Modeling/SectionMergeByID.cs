using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 合并的一个小类，业务层
    /// </summary>
  public  class SectionMergeByID
    {
        static public Dictionary<int, BrepModel> MergeArcModel(List<ModelWithArc> arcModels,Dictionary<int,int[]>arc_poly1,Dictionary<int,int[]>arc_poly2) {
            //这个就是，额，那个，额，组装地层的函数
            int count = arcModels.Count;
            Dictionary<int, List<BrepModel>> sectionModelList = new Dictionary<int, List<BrepModel>>();
            Dictionary<int, BrepModel> result = new Dictionary<int, BrepModel>();
            for (int i = 0; i < count; i++) {
                ModelWithArc modelWithArc = arcModels[i];
                int id1 = modelWithArc.arc1.id;
                int id2 = modelWithArc.arc2.id;
                int[] poly1 = arc_poly1[id1];
                int[] poly2 = arc_poly2[id2];
                int polyid1 = poly1[0];
                int polyid2 = poly1[1];
                //int polyid2 = poly2[1];
                if (polyid1!=0) addKVToBrepDic(ref sectionModelList, polyid1, modelWithArc.getModel());
              if(polyid2 != 0)  addKVToBrepDic(ref sectionModelList, polyid2, modelWithArc.getModel());
            }
            foreach (var vk in sectionModelList) {
                List<BrepModel> breps = vk.Value;
                int idb = vk.Key;
                BrepModel brepModel = MergeBreps.Merge3DModel(breps);
                result.Add(idb, brepModel);
            }
            return result;
        }
        static private void addKVToBrepDic(ref Dictionary<int,List< BrepModel>> MergeList,int polyid,BrepModel model) {
            List<int> keys = MergeList.Keys.ToList<int>();
            bool init = keys.Contains(polyid);
            if (init == false) {
                List<BrepModel> brepsnew = new List<BrepModel>();
                brepsnew.Add(model);
                MergeList.Add(polyid, brepsnew);
                return;
            }
            MergeList[polyid].Add(model);
        }
    }
}
