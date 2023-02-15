using GeoCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SolidModel
    
{
    class ReadAndSortSHP//这个类用来完成对某个指定文件夹下所有的shp文件的读取，依照Z进行排序，并提供返回存有所有shp中边界数据的List<double>z（索引）  Dictionary<double z,List<Eage>>作为可用数据
    {
        public string dataFolder;
        public List<string> shpFilesList; 
        public ReadAndSortSHP(string folder) {
            shpFilesList = new List<string>();
            this.dataFolder = folder;
            string[] paths = Directory.GetFiles(folder);
            foreach (string item in paths) {
                string extension = Path.GetExtension(item).ToLower();
                if (extension.Equals(".shp")||extension.Equals(".SHP") )
                {
                    shpFilesList.Add(item);//添加到图片list中
                }
            }
        }

        public bool getEageDictionary(out List<double> zarray,out Dictionary <double ,List<Eage>> eagesDictionary) {
            bool parallelmark = true;
            zarray = new List<double>();
            eagesDictionary = new Dictionary<double, List<Eage>>();
            for (int i = 0; i < shpFilesList.Count(); i++) {
                string filepath = shpFilesList[i];
                DataIO dataIO = new DataIO();
                List<Contour> contours= dataIO.GetContour(filepath);
                List<Eage> eages = new List<Eage>();
                for (int j = 0; j < contours.Count(); j++) {
                    eages.AddRange(contours[j].eageList);
                }
                double z = eages[0].vertexList[0].z;
                for (int j = 0; j < eages.Count(); j++) {
                    Eage eage1 = eages[j];
                    for (int k = 0; k < eage1.vertexList.Count(); k++) {
                        if (z != eage1.vertexList[k].z) parallelmark = false;
                    }

                }
                zarray.Add(z);
                eagesDictionary.Add(z, eages);
            }
            zarray.Sort();


            return parallelmark;

        }
    }
}
