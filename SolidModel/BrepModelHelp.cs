using GeoCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{


    public class BrepModelHelp
    {
        #region 属性字段和构造函数
        /// <summary>
        /// Brep模型列表
        /// </summary>
        private List<BrepModel> _brepList;
        private Dictionary<int, BrepModel> _brepDic;
        #endregion

        public BrepModelHelp(List<BrepModel> _brepList)
        {
            this._brepList = _brepList;
        }
        public BrepModelHelp(Dictionary<int, BrepModel> _brepDic)
        {
            this._brepDic = _brepDic;
        }
        /// <summary>
        /// 输出Brep模型
        /// </summary>
        /// <param name="fileDirectory">brepModel文件路径</param>
        public void ExportToObj(string workSpacePath, string folderName)
        {
            //创建BrepModel存储路径
            string fileDirectory = workSpacePath + "\\" + folderName;
            if (Directory.Exists(fileDirectory))
                Directory.Delete(fileDirectory, true);
            Directory.CreateDirectory(fileDirectory);

            //输出BrepModel
            string fileName = fileDirectory + "\\brep2obj";

            for (int i = 0; i < _brepList.Count; i++)
            {
                //string subFileName = fileName + Convert.ToString(i+1) + ".obj";
                string subFileName = fileDirectory + "\\" + Convert.ToString(i + 1) + ".obj";

                //_brepList[i].export2Obj(subFileName, "obj" + Convert.ToString(i + 1));    
                _brepList[i].export2Obj(subFileName, Convert.ToString(i + 1));
            }
        }

        public void ExportToObjByDic(string workSpacePath, string folderName)
        {
            //创建BrepModel存储路径
            string fileDirectory = workSpacePath + "\\" + folderName;
            if (Directory.Exists(fileDirectory))
                Directory.Delete(fileDirectory, true);
            Directory.CreateDirectory(fileDirectory);

            //输出BrepModel
            string fileName = fileDirectory + "\\brep2obj";

            foreach (var vk in this._brepDic)
            {
                //string subFileName = fileName + Convert.ToString(i+1) + ".obj";
                string subFileName = fileDirectory + "\\" + Convert.ToString(vk.Key) + ".obj";

                //_brepList[i].export2Obj(subFileName, "obj" + Convert.ToString(i + 1));    
                vk.Value.export2Obj(subFileName, Convert.ToString(vk.Key));
            }
        }
    }
}
