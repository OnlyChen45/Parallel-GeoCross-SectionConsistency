using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeDModelSystemForSection
{/// <summary>
/// 用来对应的辅助类，业务层
/// </summary>
    public class IndexPairs
    {
        public Dictionary<int, int> indexs1;
        public Dictionary<int, int> indexs2;
        public IndexPairs() {
            this.indexs1 = new Dictionary<int, int>();
            this.indexs2 = new Dictionary<int, int>();
        }
        public void addindexPair(int index1,int index2) {
            if(!this.indexs1.ContainsKey(index1))
            this.indexs1.Add(index1, index2);
            if(!this.indexs2.ContainsKey(index2))
            this.indexs2.Add(index2, index1);
        }
        public void addindexPairs(IndexPairs indexPairs) {
            foreach (var vk in indexPairs.indexs1) {
                int index1 = vk.Key;
                int index2 = vk.Value;
                addindexPair(index1, index2);
            }
        }
        public int getindex(int index,bool oneOrtwo) {
            if (oneOrtwo)
            {
                if (this.indexs1.ContainsKey(index) == false) return int.MinValue;
                return this.indexs1[index];
            }
            else {
                if (this.indexs2.ContainsKey(index) == false) return int.MinValue;
                return this.indexs2[index];
            }
        }


    }
}
