using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{/// <summary>
/// 数据IO层
/// </summary>
    public class InterShpReader
    {
        //这个类主要是用来读取和设计所有需要切分的数据的
        //需要的内容有，出发的真实剖面，所有插值剖面，从该剖面出发后的顺序的尖灭id的txt，合并后的txt

        //顺序的尖灭id的txt
        //line 1 原始剖面的path
        //line 2 合并后剖面的path
        //line 3 尖灭去向的合并后地层的path
        //line 4 尖灭地层的顺序id 用','隔开

        //合并后的txt
        //line 1 原始剖面path
        //line 2 合并后剖面path
        //line 3 合并后剖面id
        //line 4 对应上一行的合并前的剖面id ，用','隔开
        public string shppath1, shppath2,orishppath;
        public string intershpsfolder;
        public string sharpenidtxtpath, sectionpairpath;
        public string workspacefolder;
        public string mergefolder, splitfolder;
        public string compairtxt;//这个是专门记录切分好的数据应当如何对应建模的，也记录它对应的插值文件的名字便于从2d恢复成3d
        public string idFiledName;
        public List<int> sharpenids;
        public Dictionary<int, List<int>> sharpen_unionid_Pairs;
        public InterShpReader() {
            //初始化一下gdal
            Gdal.AllRegister();
            Ogr.RegisterAll();
            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");
            sharpenids = new List<int>();
            sharpen_unionid_Pairs = new Dictionary<int, List<int>>();
        }
        //哎呀，现在要写怎么分了，先写读取吧
        public void loaddata(string shppath1,string shppath2,string orishppath,string intershpsfolder,string sharpenidtxtpath,string sectionpairpath) {
            //所有的数据加载进来
            this.shppath1 = shppath1;
            this.shppath2 = shppath2;
            this.orishppath = orishppath;
            this.intershpsfolder = intershpsfolder;
            this.sharpenidtxtpath = sharpenidtxtpath;
            this.sectionpairpath = sectionpairpath;
        }
        public void loadworkspace(string workspacefolder,string foldermark="000") {
            this.workspacefolder = workspacefolder;
            this.mergefolder = workspacefolder + '\\' + "mergeresult"+foldermark;
            this.splitfolder = workspacefolder + '\\' + "splitfolder"+foldermark;
            this.compairtxt = workspacefolder + '\\' + " compairtxt"+foldermark+".txt";
            if (Directory.Exists(this.mergefolder) == false)//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(this.mergefolder);
            }
            if (Directory.Exists(this.splitfolder) == false)//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(this.splitfolder);
            }
        }
        public string[] gettxtContain(string path) {

            List<string> result = new List<string>();
            StreamReader streamReader = getTxtReader(path);
            while (!streamReader.EndOfStream) { 
                string st = streamReader.ReadLine();
                result.Add(st);
            }
            streamReader.Close();
            return result.ToArray();
        }
        public int[] getsharpenOrder(string[] txtcontain,out string orisection,out string mergesection,out string othersection) {
            //读取尖灭地层顺序的所有数据
            orisection = txtcontain[0];
            mergesection = txtcontain[1];
            othersection = txtcontain[2];
            string tempst = txtcontain[3];
            string[] idst = tempst.Split(',');
            int count = idst.Length;
            if (idst[0].Length == 0) {
                return null;
            }
            List<int> result = new List<int>();
            for (int i = 0; i < count; i++) {
                int id = int.Parse(idst[i]);
                result.Add(id);
            }
            return result.ToArray();
        }
        public Dictionary<int, int[]> getsharpenPair(string[] txtcontain, out string orisection, out string mergesection) {
            //读取所有的剖面合并对应的数据
            orisection = txtcontain[0];
            mergesection = txtcontain[1];
            int count = txtcontain.Length;
            Dictionary<int, int[]> result = new Dictionary<int, int[]>();
            for (int i = 2; i < count; i+=2) {
                string mergeidst = txtcontain[i];
                string orisharpens = txtcontain[i + 1];
                string[] idsst = orisharpens.Split(',');
                List<int> idlist = new List<int>();
                foreach (string st in idsst) {
                    int id = int.Parse(st);
                    idlist.Add(id);
                }
                int mergeid = int.Parse(mergeidst);
                result.Add(mergeid, idlist.ToArray());
            }
            return result;
        }
        public Dictionary<int, string> getid_intershpPair(string interfolderpath) {
            //截取所有的插值shp文件的id，然后返回成一个字典，key为这个插值底层生成位置尖灭的剖面的id，内容为插值地层剖面的绝对路径string
            Dictionary<int, string> result = new Dictionary<int, string>();
            string[] shppaths = getFildspathInFolder(interfolderpath, ".shp");
            int count = shppaths.Length;
            for (int i = 0; i < count; i++) {
                string shppath = shppaths[i];
                int IDindex = shppath.IndexOf("ID");
                int dotindex = shppath.IndexOf('.');
                int idlength = dotindex - 2 - IDindex;
                string idstring = shppath.Substring(IDindex + 2, idlength);
                int id = int.Parse(idstring);
                result.Add(id, shppath);
            }
            return result;
        }
        private string[] getFildspathInFolder(string folderpath,string ex)
        {
            //string ex = ".shp";
            string[] paths = Directory.GetFiles(folderpath);
            List<string> result = new List<string>();
            for (int i = 0; i < paths.Length; i++)
            {
                int lastex = paths[i].LastIndexOf('.');
                string tex = paths[i].Substring(lastex);
                var blo = false;
                if (tex == ex)
                {
                    blo = true;
                }
                if (blo)
                {
                    result.Add(paths[i]);
                }
            }
            return result.ToArray();
        }
        private StreamReader getTxtReader(string path) {
            FileStream fileStream = new FileStream(path, FileMode.Open);
            StreamReader result = new StreamReader(fileStream);
            return result;
        }
        public static Layer openlayer(string path)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            return layer;
        }
    }
}
