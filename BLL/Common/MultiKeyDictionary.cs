using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 双主键字典，一个特殊的数据结构
    /// </summary>
    /// <typeparam name="Tkey1"></typeparam>
    /// <typeparam name="Tkey2"></typeparam>
    /// <typeparam name="Tvalue"></typeparam>
    public class MultiKeyDictionary<Tkey1, Tkey2, Tvalue> : Dictionary<Tkey1, Dictionary<Tkey2, Tvalue>>
    {
        new public Dictionary<Tkey2, Tvalue> this[Tkey1 key]
        {
            get
            {
                if (!ContainsKey(key))
                    Add(key, new Dictionary<Tkey2, Tvalue>());

                Dictionary<Tkey2, Tvalue> returnObj;
                TryGetValue(key, out returnObj);

                return returnObj;
            }
        }
        public Tvalue this[Tkey1 key1, Tkey2 key2]
        {
            get
            {
                Tvalue returnObj;
                Dictionary<Tkey2, Tvalue> temObj = this[key1];
                temObj.TryGetValue(key2, out returnObj);
                return returnObj;
            }
        }
        public virtual void Add(Tkey1 key1, Tkey2 key2, Tvalue value)
        {
            this[key1].Add(key2, value);
        }
    }
}
