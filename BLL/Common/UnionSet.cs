using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeDModelSystemForSection
{
    class UnionSet
        //并查集类，用来对两个地层模块的连通性进行验证的
    {
        private int[] parent;
        private int groupNum;

        public UnionSet(int n)//初始化并查集
        {
            parent = new int[n];
            for (int i = 0; i < n; ++i)
            {
                parent[i] = i;
            }
            groupNum = n;
        }

        public int Find(int i)//查找标号为i的元素的原始连通点
        {
            while (i != parent[i])
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        public void Unite(int a, int b)
        {
            int x = Find(a);
            int y = Find(b);
            if (x == y) return;
            parent[x] = y;
            groupNum--;
        }

        public bool InSameSet(int a, int b)
        {
            return Find(a) == Find(b);
        }

        public int GetGroups()
        {
            return groupNum;
        }

        public Dictionary<int, List<int>> GetEachGroup()
        {
            Dictionary<int, List<int>> res = new Dictionary<int, List<int>>();
            for (int i = 0; i < parent.Length; ++i)
            {
                int gid = Find(parent[i]);
                if (!res.ContainsKey(gid))
                    res.Add(gid, new List<int>());
                res[gid].Add(i);
            }
            return res;
        }
    }
}
