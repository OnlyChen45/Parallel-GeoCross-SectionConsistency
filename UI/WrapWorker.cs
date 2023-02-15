using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using GeoCommon;
using SolidModel;


namespace ThreeDModelSystemForSection
{
    class WrapWorker
    {
        static public string getNowTimeNumString()
        {
            string st = DateTime.Now.ToString("yyyyMMdd_hhmmss");
            return st;
        }
        static public string getnewFileName(string workfolder, string filename, string ex)
        {//给定一个文件夹，一个文件名，一个后缀名，生成特定路径下按照时间标记的文件绝对路径 
            //目前不需要这个后缀了，给他删了
            string datest = WrapWorker.getNowTimeNumString();
            string fullfilename = filename  + '.' + ex;
            //string fullfilename = filename + datest + '.' + ex;
            //string[] files = Directory.GetFiles(workfolder);
            string fullpath = workfolder + '\\' + fullfilename;
            return fullpath;

        }
        static public string createDirectory(string workspace, string name)
        {
            string sPath = workspace + '\\' + name + getNowTimeNumString();
            if (!Directory.Exists(sPath))
            {
                Directory.CreateDirectory(sPath);
            }
            return sPath;
        }
        static public string[] getAllfilepathinFolder(string folder, string ex)
        {
            string[] files = Directory.GetFiles(folder);
            List<string> result = new List<string>();
            foreach (string st in files)
            {
                if (st.EndsWith(ex))
                {
                    result.Add(st);
                }
            }
            return result.ToArray();
        }
        static public string[] shpFilesNameInFolder(string folderpath)
        {
            string ex = ".shp";
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
        static public void dealTopoChange(string path1, string path2, string workspace, out string resultpath1, out string resultpath2, string idFieldName,string name1,string name2, double bufferdis = 1,string idname="LithCode")
        {
            resultpath1 = WrapWorker.getnewFileName(workspace, name1, "shp");
            resultpath2 = WrapWorker.getnewFileName(workspace, name2, "shp");
            // string midresult1 = WrapWorker.getnewFileName(workspace, "1NoSharpen", "shp");
            //string midresult2 = WrapWorker.getnewFileName(workspace, "2NoSharpen", "shp");
            string midresult1, midresult2;
            WrapWorker.earseSharpenSection(path1, path2, "1NoSharpen", "2NoSharpen", workspace, idname, out midresult1, out midresult2);
          // makeNosharpenData(path1, path2, midresult1, midresult2, idFieldName);
            DealTopoChange.dealTopoChange(midresult1, midresult2, idFieldName, workspace, resultpath1, resultpath2, path1, path2, bufferdis);
        }
        static public void dealTopoChange(string path1, string path2, string workspace, out string resultpath1, out string resultpath2, string idFieldName, string name1, string name2, out string bufferpath1,out string bufferpath2,double bufferdis = 1)
        {
            resultpath1 = WrapWorker.getnewFileName(workspace, name1, "shp");
            resultpath2 = WrapWorker.getnewFileName(workspace, name2, "shp");
            string midresult1 = WrapWorker.getnewFileName(workspace, "1NoSharpen", "shp");
            string midresult2 = WrapWorker.getnewFileName(workspace, "2NoSharpen", "shp");
           // makeNosharpenData(path1, path2, midresult1, midresult2, idFieldName);
            DealTopoChange.dealTopoChange(midresult1, midresult2, idFieldName, workspace, resultpath1, resultpath2, path1, path2, bufferdis,out bufferpath1,out bufferpath2);
        }
        /// <summary>
        /// 制作没有尖灭地层的数据，便于后续处理拓扑关系变化
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <param name="nosharpen1"></param>
        /// <param name="nosharpen2"></param>
        /// <param name="idFieldName"></param>
        static public void makeNosharpenData(string path1, string path2, string nosharpen1, string nosharpen2, string idFieldName)
        {
            SpatialReference spatialReference1;
            Dictionary<int, Geometry> geoms1 = shpReader.getGeomListByFile(path1, idFieldName, out spatialReference1);
            Dictionary<int, Geometry> geoms2 = shpReader.getGeomListByFile(path2, idFieldName);
            List<int> sp1 = delListValue(geoms1.Keys.ToList<int>(), geoms2.Keys.ToList<int>());
            List<int> sp2 = delListValue(geoms2.Keys.ToList<int>(), geoms1.Keys.ToList<int>());
            Dictionary<int, Geometry> resultge1 = makenospDic(geoms1, sp1);
            Dictionary<int, Geometry> resultge2 = makenospDic(geoms2, sp2);
            saveDictionaryGeom(resultge1, nosharpen1, idFieldName, spatialReference1);
            saveDictionaryGeom(resultge2, nosharpen2, idFieldName, spatialReference1);
            Dictionary<int, Geometry> makenospDic(Dictionary<int, Geometry> geoms, List<int> sp)
            {
                Dictionary<int, Geometry> resultge = new Dictionary<int, Geometry>();
                foreach (var vk3 in geoms)
                {
                    if (sp.Contains(vk3.Key) == true)
                    {
                        foreach (var vk4 in geoms)
                        {
                            if (vk3.Value.Intersect(vk4.Value))
                            {
                                Geometry re = vk3.Value.Union(vk4.Value);
                                resultge.Add(vk4.Key, re);
                            }
                        }
                    }
                }
                foreach (var vk3 in geoms)
                {
                    if (resultge.ContainsKey(vk3.Key) == false)
                    {
                        resultge.Add(vk3.Key, vk3.Value);
                    }
                }
                return resultge;
            }
            List<int> delListValue(List<int> list1, List<int> list2)
            {
                List<int> result = new List<int>();
                foreach (int v in list1)
                {
                    bool in2list = false;
                    foreach (int v2 in list2)
                    {
                        if (v == v2)
                        {
                            in2list = true;
                            break;
                        }
                    }
                    if (in2list == false)
                    {
                        result.Add(v);
                    }
                }
                return result;
            }
        }
        static public void dealFenzhi(string path1, string path2, string workspace, string filename1,string filename2, out string resultpath1, out string resultpath2, string FieldName = "LithCode")
        {
            SpatialReference spatialReference;
            Dictionary<int, Geometry> geomlistfids1 = shpReader.getGeomListByFileWithFID(path1, out spatialReference);
            Dictionary<int, Geometry> geomlistfids2 = shpReader.getGeomListByFileWithFID(path2, out spatialReference);
            string[] usefulattriname = { "FID1", "FID2", "NewID1", "NewID2", "shpID", "shpID1", "shpID2" };
            attrireader attrireader1 = new attrireader(path1);
            attrireader attrireader2 = new attrireader(path2);
            Dictionary<string, Dictionary<int, int>> attritable1 = new Dictionary<string, Dictionary<int, int>>();
            Dictionary<string, Dictionary<int, int>> attritable2 = new Dictionary<string, Dictionary<int, int>>();
            //把所有的数据都提取出来。
            foreach (string namet in usefulattriname)
            {
                Dictionary<int, int> attrilist1 = attrireader1.getattrilistByName(namet);
                Dictionary<int, int> attrilist2 = attrireader2.getattrilistByName(namet);
                attritable1.Add(namet, attrilist1);
                attritable2.Add(namet, attrilist2);
            }
            Dictionary<int, int> fid1_LithCode = attrireader1.getattrilistByName(FieldName);//获取fid与LithCode
            Dictionary<int, int> fid2_LithCode = attrireader2.getattrilistByName(FieldName);
            attrireader1.layerdispose();
            attrireader2.layerdispose();
            //下面来判断一下用哪个数据来做内容
            Dictionary<int, int> FID1 = new Dictionary<int, int>(), FID2 = new Dictionary<int, int>(),
                NewID1 = new Dictionary<int, int>(), NewID2 = new Dictionary<int, int>();
            int shpid1 = attritable1["shpID"][0];
            int shpid2 = attritable2["shpID"][0];
            int shpid11 = attritable1["shpID1"][0];
            int shpid12 = attritable1["shpID2"][0];
            int shpid21 = attritable2["shpID1"][0];
            int shpid22 = attritable2["shpID2"][0];
            if (shpid2 == shpid11)
            {
                FID1 = attritable1["FID1"];
                NewID1 = attritable1["NewID1"];
            }
            else if (shpid2 == shpid12)
            {
                FID1 = attritable1["FID2"];
                NewID1 = attritable1["NewID2"];
            }
            if (shpid1 == shpid21)
            {
                FID2 = attritable2["FID1"];
                NewID2 = attritable2["NewID1"];
            }
            else if (shpid1 == shpid22)
            {
                FID2 = attritable2["FID2"];
                NewID2 = attritable2["NewID2"];
            }
            //以上就读好了所有的需要的ID数据了。
            //FID1，是path1中的元素FID作为主键，其他作为连接到path2中
            //FID2，是path2中的元素FID作为主键，其他作为连接到path1中
            //NewID1，NewID2实际上就是，就是从1 或者2出发向另一个path来对应的面需要获得的新的ID
            Dictionary<int, Geometry> resultsection1 = new Dictionary<int, Geometry>();
            Dictionary<int, Geometry> resultsection2 = new Dictionary<int, Geometry>();
            //这个resultsection是用来装最终结果的
            Dictionary<int, List<int>> splitqueue1 = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> splitqueue2 = new Dictionary<int, List<int>>();
            List<int> normalfid1 = new List<int>(), normalfid2 = new List<int>();
            List<int> sharpen1 = new List<int>();
            List<int> sharpen2 = new List<int>();
            foreach (var vk in FID2)
            {
                int id = vk.Key;
                int fid1 = vk.Value;
                if (fid1 == -2)
                {
                    normalfid2.Add(id);
                    continue;
                }//正常一一对应不需处理
                if (fid1 == -1)
                { //从2到1 尖灭，存入2尖灭列表
                    sharpen2.Add(id);
                    continue;
                }
                bool t = splitqueue1.ContainsKey(fid1);//余下的就是，2多对1中一
                if (t == false)
                {
                    List<int> templist = new List<int>();
                    templist.Add(id);
                    splitqueue1.Add(fid1, templist);
                }
                else
                {
                    splitqueue1[fid1].Add(id);
                }
            }
            foreach (var vk in FID1)
            {
                int id = vk.Key;
                int fid2 = vk.Value;
                if (fid2 == -2)
                {
                    normalfid1.Add(id);
                    continue;
                }//正常一一对应不需处理
                if (fid2 == -1)
                { //从1到2 尖灭，存入1尖灭列表
                    sharpen1.Add(id);
                    continue;
                }
                bool t = splitqueue2.ContainsKey(fid2);//余下的就是，1多对2中一
                if (t == false)
                {
                    List<int> templist = new List<int>();
                    templist.Add(id);
                    splitqueue2.Add(fid2, templist);
                }
                else
                {
                    splitqueue2[fid2].Add(id);
                }
            }
            //下面就是要分别处理两个面中间的这个分支问题和尖灭问题了。
            //其实具体办法也很简单，就是尖灭的按照新ID给处理了，然后分支的按照新ID做成新Dic处理。
            //主要就是要把resultsection1 和resultsection2 给做完整了，就对了。
            //先加普通面
            for (int i = 0; i < normalfid1.Count; i++)
            {
                int norfid = normalfid1[i];
                resultsection1.Add(fid1_LithCode[norfid], geomlistfids1[norfid]);
            }
            for (int i = 0; i < normalfid2.Count; i++)
            {
                int norfid = normalfid2[i];
                resultsection2.Add(fid2_LithCode[norfid], geomlistfids2[norfid]);
            }
            //再加尖灭面
            for (int i = 0; i < sharpen1.Count; i++)
            {
                int sid = sharpen1[i];
                int sharpennewid = NewID1[sid];
                resultsection1.Add(sharpennewid, geomlistfids1[sid]);
            }
            for (int i = 0; i < sharpen2.Count; i++)
            {
                int sid = sharpen2[i];
                int sharpennewid = NewID2[sid];
                resultsection2.Add(sharpennewid, geomlistfids2[sid]);
            }
            //最后处理分支面
            foreach (var vk in splitqueue1)
            {
                int fid1 = vk.Key;
                List<int> fid2s = vk.Value;
                Dictionary<int, Geometry> newidWithGeom = new Dictionary<int, Geometry>();
                for (int j = 0; j < fid2s.Count; j++)
                {
                    int fid2 = fid2s[j];
                    newidWithGeom.Add(NewID2[fid2], geomlistfids2[fid2]);
                }
                Dictionary<int, Geometry> fenzhijieguo1 = BifucationWorker.dealOnePairPluralnewVersion(geomlistfids1[fid1], newidWithGeom);
                foreach (var vk2 in fenzhijieguo1)
                {
                    resultsection1.Add(vk2.Key, vk2.Value);
                }
            }
            foreach (var vk in splitqueue2)
            {
                int fid2 = vk.Key;
                List<int> fid1s = vk.Value;
                Dictionary<int, Geometry> newidWithGeom = new Dictionary<int, Geometry>();
                for (int j = 0; j < fid1s.Count; j++)
                {
                    int fid1 = fid1s[j];
                    newidWithGeom.Add(NewID1[fid1], geomlistfids1[fid1]);
                }
                Dictionary<int, Geometry> fenzhijieguo2 = BifucationWorker.dealOnePairPluralnewVersion(geomlistfids1[fid2], newidWithGeom);
                foreach (var vk2 in fenzhijieguo2)
                {
                    resultsection2.Add(vk2.Key, vk2.Value);
                }
            }
            resultpath1 = WrapWorker.getnewFileName(workspace, filename1, "shp");
            resultpath2 = WrapWorker.getnewFileName(workspace, filename2, "shp");
            shpReader.saveDicOfGeoms(resultpath1, resultsection1, FieldName, spatialReference);
            shpReader.saveDicOfGeoms(resultpath2, resultsection2, FieldName, spatialReference);
        }
        static public void getSplitpathPair(ref Dictionary<int, string[]> splitpathpair, string[] files)
        {
            int count = files.Length;
            Dictionary<int, Dictionary<int, string>> worker = new Dictionary<int, Dictionary<int, string>>();
            for (int i = 0; i < count; i++)
            {
                string file1 = files[i];
                string filename = Path.GetFileNameWithoutExtension(file1);
                string[] splitst = filename.Split('S');
                int meid = int.Parse(splitst[1]);
                int id = int.Parse(splitst[3]);
                bool createkey = !(worker.Keys.Contains<int>(meid));
                if (createkey)
                {
                    Dictionary<int, string> stlist = new Dictionary<int, string>();
                    worker.Add(meid, stlist);
                }
                worker[meid].Add(id, file1);
            }
            foreach (var vk in worker)
            {
                int meid = vk.Key;
                Dictionary<int, string> stdic = vk.Value;
                List<int> keys = stdic.Keys.ToList<int>();
                keys.Sort();//默认升序排
                List<string> sts = new List<string>();
                int countt = keys.Count;
                for (int j = 0; j < countt; j++)
                {
                    sts.Add(stdic[keys[j]]);
                }
                splitpathpair.Add(meid, sts.ToArray<string>());
            }
        }
        static public Dictionary<int, BrepModel> ModelLod2(string path1, string path2, string idFieldname, string workspace, string gdbpath, string pypath)
        {
            //用两个路径去建模。
            //第一步应该是把两个面应该创建的缺失地层建好
            //第二步应该是看一看有几个面，如果是一对一，就不用拓扑建模了
            //第三步，应该是把两个面给还原成三维的
            //第四步，建模。
            Dictionary<int, Geometry> geoms1 = shpReader.getGeomListByFile(path1, idFieldname);
            Dictionary<int, Geometry> geoms2 = shpReader.getGeomListByFile(path2, idFieldname);
            Dictionary<int, BrepModel> result = new Dictionary<int, BrepModel>();
            int count1 = geoms1.Count;
            int count2 = geoms2.Count;
            if (count1 == 1 && count2 == 1)
            {
                //这时候就是，两个文件都只有一个，那么就直接单独建模就完事了
                Dictionary<string, double> attri1 = getTransformAttri(path1);
                string filename1 = Path.GetFileNameWithoutExtension(path1);
                string folder1 = Path.GetDirectoryName(path1);
                string filename3d1 = folder1 + filename1 + "3d.shp";
                // convert2DTo3D(filename1, filename3d1, attri1, 1);//这个是把建模需要用的数据输入进去，然后还回来一个3D数据
                convert2DTo3D(path1, filename3d1, attri1, 1);
                Dictionary<string, double> attri2 = getTransformAttri(path2);
                string filename2 = Path.GetFileNameWithoutExtension(path2);
                string folder2 = Path.GetDirectoryName(path2);
                string filename3d2 = folder2 + filename2 + "3d.shp";
                //convert2DTo3D(filename2, filename3d2, attri2, 1);
                //这样就把两个面都转成了3D的，然后直接建模就完事了
                convert2DTo3D(path2, filename3d2, attri2, 1);
                BrepModel brepModel = SinglePolyWorker.SinglePolyModeling(filename3d1, filename3d2);

                int id = geoms1.Keys.ToArray<int>()[0];
                /*
                brepModels.Add(brepModel);
                string modelname1 = getmodelname(filename3d1, filename3d2);
                BrepModelHelp brepModelHelp1 = new BrepModelHelp(brepModels);
                brepModelHelp1.ExportToObj(outputFolder, modelname1);*/
                result.Add(id, brepModel);

            }
            if (count1 > count2)
            {
                //这样就针对面1建立一个
                //这段就需要先创建buffer，然后再创建topo关系，然后再输入这个那个数据，然后建立
                //这个buffer应该是从path2上创建
                string folder1 = Path.GetDirectoryName(path1);
                string file2name = Path.GetFileNameWithoutExtension(path2);
                string path2added = createBuffers(path1, path2, idFieldname, folder1, 0);
                path2added = polyNormalization(path2added, folder1, gdbpath, pypath, file2name, idFieldname);//这样就做好了
                Dictionary<string, string> dataPathCollection;
                WrapWorker.makeTopotForSections(path1, path2added, out dataPathCollection, folder1, idFieldname);
                Dictionary<string, string> pathData3d;
                WrapWorker.transdataTo3D(dataPathCollection, out pathData3d);
                List<int[]> arcmodel_polyids;
                Dictionary<int, BrepModel> brepModels = WrapWorker.makeLOD2Model(pathData3d, out arcmodel_polyids);
                result = brepModels;
                //下面要做这个拓扑对应
                /*
            Dictionary<string, string> dataPathCollection;
            WrapWorker.makeTopotForSections(datapath1, datapath2, out dataPathCollection, workspaceFolder.Text, IdNameBox.Text);
            this.dataCollec = dataPathCollection;
            //第二步在这
            Dictionary<string, string> pathData3d;
            WrapWorker.transdataTo3D(dataPathCollection, out pathData3d);
            this.dataCollec = pathData3d;
            //第三步在这
            this.modelWithArcs= WrapWorker.makeLOD1Model(pathData3d,resultFolder.Text,IdNameBox.Text,out arcmodel_polyids, modelNameLOD1.Text);
                 */
            }
            if (count1 < count2)
            {

                string folder2 = Path.GetDirectoryName(path2);
                string file2name = Path.GetFileNameWithoutExtension(path1);
                string path1added = createBuffers(path2, path1, idFieldname, folder2, 0);
                path1added = polyNormalization(path1added, folder2, gdbpath, pypath, file2name, idFieldname);//这样就做好了
                Dictionary<string, string> dataPathCollection;
                WrapWorker.makeTopotForSections(path2, path1added, out dataPathCollection, folder2, idFieldname);
                Dictionary<string, string> pathData3d;
                WrapWorker.transdataTo3D(dataPathCollection, out pathData3d);
                List<int[]> arcmodel_polyids;
                Dictionary<int, BrepModel> brepModels = WrapWorker.makeLOD2Model(pathData3d, out arcmodel_polyids);
                result = brepModels;
                //这就是反过来。
            }

            return result;
        }
        static string getmodelname(string shppath1, string shppath2)
        {
            string filename1 = Path.GetFileName(shppath1);
            string filename2 = Path.GetFileName(shppath2);
            int l1 = filename1.Length;
            int l2 = filename2.Length;
            string n1 = filename1.Substring(0, l1 - 4);
            string n2 = filename2.Substring(0, l2 - 4);
            return n1 + '_' + n2;
        }
        static public string mode12DToDefault3D(string inputpath, string workspace, string idFieldName, double startX, double startY, double startZ, double endX, double endY, double firstX, double firstY, string filename = "to3D")
        {//这个玩意儿是把一个遵循着横向的地层存储方式的地层还原到三维
            wkbGeometryType geometryType = shpReader.getShpGeomType(inputpath);
            string result = "";
            switch (geometryType)
            {
                case wkbGeometryType.wkbPolygon:
                    {
                        StratumData stratumData = new StratumData(inputpath, startX, startY, startZ, endX, endY, firstX, firstY, idFieldName);
                        string outputpath = WrapWorker.getnewFileName(workspace, filename, "shp");
                        stratumData.saveStratumsToSHP(outputpath, "polys");
                        result = outputpath;
                        break;
                    }
                case wkbGeometryType.wkbLineString:
                    {
                        ConvertLinesTo3D convertLinesTo3D = new ConvertLinesTo3D(inputpath, startX, startY, startZ, endX, endY, firstX, firstY, idFieldName);
                        convertLinesTo3D.convertLines();
                        string outputpath = WrapWorker.getnewFileName(workspace, filename, "shp");
                        convertLinesTo3D.saveToShp(outputpath, "lines");
                        result = outputpath;
                        break;
                    }
                case wkbGeometryType.wkbPoint:
                    {
                        ConvertPointsTo3D convertPointsTo3D = new ConvertPointsTo3D(inputpath, startX, startY, startZ, endX, endY, firstX, firstY, idFieldName);
                        convertPointsTo3D.convertCoord();
                        // convertPointsTo3D.saveToTXT(outcsvtxt);
                        string outputpath = WrapWorker.getnewFileName(workspace, filename, "shp");
                        convertPointsTo3D.saveToShp(outputpath, "points");
                        result = outputpath;
                        break;
                    }

            }
            return result;
        }
        static public string Default3DToDefault2D(string inputpath, string workspace, string idFieldName, string outfilename = "poly2d")
        {//把一个三维的地层给弄成标准二维
            wkbGeometryType geometryType = shpReader.getShpGeomType(inputpath);
            string result = "";
            switch (geometryType)
            {
                case wkbGeometryType.wkbPolygon25D:
                    {
                        ConvertArcs3DTo2DWholeShp convertArcs3DTo2DWhole = new ConvertArcs3DTo2DWholeShp();
                        string outputpath = WrapWorker.getnewFileName(workspace, outfilename, "shp");
                        double[] transAttri;
                        convertArcs3DTo2DWhole.OpenPolysShpAndConvert(inputpath, outputpath, out transAttri);
                        result = outputpath;
                        break;
                    }
                case wkbGeometryType.wkbLineString25D:
                    {
                        ConvertArcs3DTo2DWholeShp convertArcs3DTo2DWhole = new ConvertArcs3DTo2DWholeShp();
                        string outputpath = WrapWorker.getnewFileName(workspace, outfilename, "shp");
                        double[] transAttri;
                        convertArcs3DTo2DWhole.OpenArcsShpAndConvert(inputpath, outputpath, out transAttri);
                        result = outputpath;
                        break;
                    }

            }
            return result;
        }
        static public string transInterLinesToPoly(string interlinesFolder, string idFieldName)
        {
            //这个是把插值出来的线变成面的过程
            string[] shps = WrapWorker.shpFilesNameInFolder(interlinesFolder);
            string outputLineFolder = WrapWorker.createDirectory(interlinesFolder, "interLines");
            string outputPolysFolder = WrapWorker.createDirectory(interlinesFolder, "interPolys");
            int filecount = shps.Length;
            for (int i = 0; i < filecount; i++)
            {
                string inputshp = shps[i];
                string shpname = Path.GetFileName(inputshp);
                string outputlinestring = outputLineFolder + '\\' + "line" + shpname;
                string polygongout = outputPolysFolder + '\\' + shpname;
                //ConvertArcs3DTo2D convertArcs3DTo2D = new ConvertArcs3DTo2D();
                //convertArcs3DTo2D.OpenArcsShpAndConvert(inputshp, outputlinestring);
                ConvertArcs3DTo2DWholeShp convertArcs3DTo2D = new ConvertArcs3DTo2DWholeShp();
                double[] transformAttribute;
                convertArcs3DTo2D.OpenArcsShpAndConvert(inputshp, outputlinestring, out transformAttribute);
                ConvertArcsToPolygon convertArcsToPolygon = new ConvertArcsToPolygon();
                Dictionary<int, Geometry> polys = convertArcsToPolygon.ReadAndConvert(outputlinestring, "polygon1", "polygon2");
                convertArcsToPolygon.savePolys(polygongout, idFieldName, polys, transformAttribute);
            }
            return outputPolysFolder;
        }
        static public string createTempGDB(string workspace)
        {
            string gdbpath = workspace + "\\tempWrokspace.gdb";
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("FileGDB");//OpenFileGDB
            //OSGeo.GDAL .Driver driver = Gdal.GetDriverByName("FileGDB");
            driver.CreateDataSource(gdbpath, null);
            return gdbpath;
        }
        static public void mergeSectionOri(string path1, string path2, string workspace, string idFieldname, out string path1result, out string path2result, out string pair1txt, out string pair2txt, out string sharpenids1txt, out string sharpenids2txt)
        {
            //这个函数为了把尖灭地层给合并，然后输出合并后的表
            string name1 = Path.GetFileNameWithoutExtension(path1);
            string name2 = Path.GetFileNameWithoutExtension(path2);
            string outputpath1 = getnewFileName(workspace,name1+ "section1", "shp");
            string outputpath2 = getnewFileName(workspace, name2+"section2", "shp");
            pair1txt = getnewFileName(workspace,name1+ "shppair1", "txt");
            pair2txt = getnewFileName(workspace, name2+"shppair2", "txt");
            sharpenids1txt = getnewFileName(workspace, name1+"sharpenorder1", "txt");
            sharpenids2txt = getnewFileName(workspace, name2+"sharpenorder2", "txt");
            path1result = outputpath1;
            path2result = outputpath2;
            SpatialReference spatialReference;
            Dictionary<int, Geometry> geoms1 = shpReader.getGeomListByFile(path1, idFieldname, out spatialReference);
            Dictionary<int, Geometry> geoms2 = shpReader.getGeomListByFile(path2, idFieldname);
            Dictionary<int, List<int>> shpPairs1, shpPairs2;
            List<int> sharpenids1, sharpenids2;
            Dictionary<int, Geometry> geoms1new = MergeSectionWorker.findsharpenandMerge(geoms1, geoms2, out shpPairs1, out sharpenids1);
            Dictionary<int, Geometry> geoms2new = MergeSectionWorker.findsharpenandMerge(geoms2, geoms1, out shpPairs2, out sharpenids2);
            shpReader.saveDicOfGeoms(outputpath1, geoms1new, idFieldname, spatialReference);
            shpReader.saveDicOfGeoms(outputpath2, geoms2new, idFieldname, spatialReference);
            List<string> pairtxt1 = new List<string>();
            List<string> pairtxt2 = new List<string>();
            List<string> sharpentxt1 = new List<string>();
            List<string> sharpentxt2 = new List<string>();
            makesharpenidtxtContain(ref sharpentxt1, path1, path1result, path2result, sharpenids1);
            makesharpenidtxtContain(ref sharpentxt2, path2, path2result, path1result, sharpenids2);
            makeMergePairtxtContain(ref pairtxt1, path1, path1result, shpPairs1);
            makeMergePairtxtContain(ref pairtxt2, path2, path2result, shpPairs2);

            //对输出这个txt做一个注解
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

            //这里少一段存储对应的txt的过程
            saveStringlistToFile(sharpentxt1, sharpenids1txt);
            saveStringlistToFile(sharpentxt2, sharpenids2txt);
            saveStringlistToFile(pairtxt1, pair1txt);
            saveStringlistToFile(pairtxt2, pair2txt);
        }
        static public void saveStringlistToFile(List<string> stlist, string filename)
        {
            FileStream fileStream = new FileStream(filename, FileMode.CreateNew);
            StreamWriter writer = new StreamWriter(fileStream);
            int count = stlist.Count;
            for (int i = 0; i < count; i++)
            {
                writer.WriteLine(stlist[i]);
            }
            writer.Close();
            fileStream.Close();
        }
        static public void makesharpenidtxtContain(ref List<string> sharpenidtxt, string oripath, string mergepath, string targetpath, List<int> orderidlist)
        {
            //顺序的尖灭id的txt
            //line 1 原始剖面的path
            //line 2 合并后剖面的path
            //line 3 尖灭去向的合并后地层的path
            //line 4 尖灭地层的顺序id 用','隔开
            sharpenidtxt.Add(oripath);
            sharpenidtxt.Add(mergepath);
            sharpenidtxt.Add(targetpath);
            int count = orderidlist.Count;
            string line1 = "";
            for (int i = 0; i < count - 1; i++)
            {
                line1 = line1 + orderidlist[i].ToString() + ',';
            }
            if (count > 1)
            {
                line1 = line1 + orderidlist[count - 1].ToString();
            }
            sharpenidtxt.Add(line1);
        }
        static public void outputSharpenLines(string outputpath, string importspatialreference, Dictionary<int, double[]> LithId_LineCoord_Pairs, Dictionary<int, int> LithId_LineId_Pairs, string idFieldName = "Lithid")
        {
            //把中间的剖面线数据输出出来
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(outputpath, null);
            DataSource usespatial = driver.Open(importspatialreference, 1);
            Layer templayer = usespatial.GetLayerByIndex(0);
            SpatialReference spatialReference = templayer.GetSpatialRef();
            Layer layer = dataSource.CreateLayer("newSectionLine", spatialReference, wkbGeometryType.wkbLineString, null);
            FieldDefn field1 = new FieldDefn(idFieldName, FieldType.OFTInteger);
            FieldDefn field2 = new FieldDefn("OriSection", FieldType.OFTInteger);
            layer.CreateField(field1, 1);
            layer.CreateField(field2, 1);
            int count = LithId_LineCoord_Pairs.Count;
            List<int> Lithidlist = new List<int>();
            foreach (int item in LithId_LineCoord_Pairs.Keys)
            {
                Lithidlist.Add(item);
            }
            Feature feature = new Feature(layer.GetLayerDefn());
            for (int i = 0; i < count; i++)
            {
                int Lithid = Lithidlist[i];
                double[] coord = LithId_LineCoord_Pairs[Lithid];
                int fromSection = LithId_LineId_Pairs[Lithid];
                Geometry line = new Geometry(wkbGeometryType.wkbLineString);
                line.AddPoint_2D(coord[0], coord[1]);
                line.AddPoint_2D(coord[2], coord[3]);
                feature.SetGeometry(line);
                feature.SetField(idFieldName, Lithid);
                feature.SetField("OriSection", fromSection);
                layer.CreateFeature(feature);
            }
        }
/*        static public double[] getLineCoordById(SectionLine line, int id)
        {
            double[] result = new double[4];
            int FID = line.ID_FID_Pairs[id];
            Dictionary<string, double> attri = line.attributeDic[FID];
            result[0] = attri["start_x"];
            result[1] = attri["start_y"];
            result[2] = attri["end_x"];
            result[3] = attri["end_y"];
            return result;

        }*/
        static public void makeMergePairtxtContain(ref List<string> mergepairtxt, string oripath, string mergepath, Dictionary<int, List<int>> mergepair)
        {
            mergepairtxt.Add(oripath);
            mergepairtxt.Add(mergepath);
            foreach (var vk in mergepair)
            {
                int id = vk.Key;
                List<int> pair = vk.Value;
                string line1 = id.ToString();
                string line2 = "";
                int count = pair.Count;
                for (int i = 0; i < count - 1; i++)
                {
                    line2 = line2 + pair[i].ToString() + ',';
                }
                line2 = line2 + pair[count - 1];
                mergepairtxt.Add(line1);
                mergepairtxt.Add(line2);
            }
        }
        static public double[] getstartendxy(string path)
        {
            //取得一个文件里边的startx,starty,endx,endy
            double[] result = new double[4];
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.Open(path, 1);
            Layer layer = ds.GetLayerByIndex(0);
            Feature feature = layer.GetFeature(0);
            result[0] = feature.GetFieldAsDouble("startx");
            result[1] = feature.GetFieldAsDouble("starty");
            result[2] = feature.GetFieldAsDouble("endx");
            result[3] = feature.GetFieldAsDouble("endy");
            return result;
        }
        /// <summary>
        /// 通过调用arcgis的工具给多边形边界上冲突消除掉
        /// 面加入gdb 面转线 线转面 输出面
        /// </summary>
        /// <param name="inputpath"></param>
        /// <param name="workspace"></param>
        /// <param name="gdbpath"></param>
        /// <param name="pythonexepath"></param>
        /// <param name="filename"></param>
        /// <param name="idFieldName"></param>
        /// <returns></returns>
        static public string polyNormalization(string inputpath, string workspace, string gdbpath, string pythonexepath, string filename, string idFieldName)
        {
            //这一段是用来对面要素进行标准化的，就是因为gdal生成的面要素往往会有一些问题，比如面和面之间的点不贴合，此外，还是要把两个面的属性值完全复制过去。这些都是完备的

            string pyscriptpath = System.AppDomain.CurrentDomain.BaseDirectory + "arcpyworker.py";
            string target = WrapWorker.getnewFileName(workspace, filename, "shp");
            string[] paras = { pythonexepath, pyscriptpath, inputpath, target, gdbpath };
            string commandstr = CMDHandler.makePara(paras);//把cmd命令的所有参数合成一行
            CMDHandler.Processing(commandstr);//执行
            MatchLayer matchLayer = new MatchLayer(inputpath, target, gdalDriverType.SHP, idFieldName);
            return target;
        }
        /// <summary>
        /// 调用带有density工具的arcgis工具脚本，给多边形增密同时还给边界冲突消除
        /// 面加入gdb 面增密，面转线 线转面 面输出
        /// </summary>
        /// <param name="inputpath"></param>
        /// <param name="workspace"></param>
        /// <param name="gdbpath"></param>
        /// <param name="pythonexepath"></param>
        /// <param name="filename"></param>
        /// <param name="idFieldName"></param>
        /// <returns></returns>
        static public string polyNormalizationWithDensity(string inputpath, string workspace, string gdbpath, string pythonexepath, string filename, string idFieldName)
        {
            //这一段是用来对面要素进行标准化的，就是因为gdal生成的面要素往往会有一些问题，比如面和面之间的点不贴合，此外，还是要把两个面的属性值完全复制过去。这些都是完备的

            string pyscriptpath = System.AppDomain.CurrentDomain.BaseDirectory + "arcpyworkerwithdensity.py";
            string target = WrapWorker.getnewFileName(workspace, filename, "shp");
            string[] paras = { pythonexepath, pyscriptpath, inputpath, target, gdbpath };
            string commandstr = CMDHandler.makePara(paras);//把cmd命令的所有参数合成一行
            CMDHandler.Processing(commandstr);//执行
            MatchLayer matchLayer = new MatchLayer(inputpath, target, gdalDriverType.SHP, idFieldName);
            return target;
        }
        /// <summary>
        /// 不包含将属性表复制过去的多边形边界一致化
        /// </summary>
        /// <param name="inputpath"></param>
        /// <param name="workspace"></param>
        /// <param name="gdbpath"></param>
        /// <param name="pythonexepath"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        static public string polyNormalSimple(string inputpath, string workspace, string gdbpath, string pythonexepath, string filename) {
            string pyscriptpath = System.AppDomain.CurrentDomain.BaseDirectory + "arcpyworker.py";
            string target = WrapWorker.getnewFileName(workspace, filename, "shp");
            string[] paras = { pythonexepath, pyscriptpath, inputpath, target, gdbpath };
            string commandstr = CMDHandler.makePara(paras);//把cmd命令的所有参数合成一行
            CMDHandler.Processing(commandstr);//执行
            return target;
        }
        static public void createBuffersboth(string inputpath1, string inputpath2, out string resultpath1, out string resultpath2, string idFieldName, string workspace, string gdbpath, string pythonpath,string name1,string name2)
        {
            //把两个面的最终结果都弄出来
            resultpath1 = "";
            /*            resultpath1 = createBuffers(inputpath2, inputpath1, idFieldName, workspace, 1);
                        resultpath2 = createBuffers(inputpath1, inputpath2, idFieldName, workspace, 2);
                        resultpath1 = polyNormalization(resultpath1, workspace, gdbpath, pythonpath, "fullbuffer1", idFieldName);
                        resultpath2 = polyNormalization(resultpath2, workspace, gdbpath, pythonpath, "fullbuffer2", idFieldName);*/
            //调整了一下这个建立buffer的顺序，第一个buffer的结果作为第二个的输入，这样防止两个相互插进去的buffer位置不相同
            resultpath1 = createBuffers(inputpath2, inputpath1, idFieldName, workspace, 1);
            resultpath1 = polyNormalization(resultpath1, workspace, gdbpath, pythonpath, name1, idFieldName);
            resultpath2 = createBuffers(resultpath1, inputpath2, idFieldName, workspace, 2);
            resultpath2 = polyNormalization(resultpath2, workspace, gdbpath, pythonpath, name2, idFieldName);

            //!!!!!!!!!!
            //在这考虑一下，是否应当把buffer后的这个图层，主动地加上这个三位参数，便于后边对其进行这个操作。对，是这样的，不过不着急去写，等需要的时候再说。
            //copy3Dattri(resultpath1, inputpath1);
            //copy3Dattri(resultpath2, inputpath2);
        }
        static public string createBuffers(string inputpath1, string inputpath2, string idFieldName, string workspace, int id)
        {
            //注，这个主要是因为输入的第一个path的地层数量小于第二个
            string path1 = inputpath1;
            string path2 = inputpath2;
            string name1 = Path.GetFileNameWithoutExtension(path1);
            string name2 = Path.GetFileNameWithoutExtension(path2);
            string idFieldname = idFieldName;
            PolygonIO polygonIO1 = new PolygonIO(path1, idFieldname);
            PolygonIO polygonIO2 = new PolygonIO(path2, idFieldname);
            Dictionary<int, Geometry> polys1, polys2;
            List<int> idlist1, idlist2;
            polygonIO1.getGeomAndId(out polys1, out idlist1);
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            Topology topo1, topo2;
            TopologyOfPoly maketopo1 = new TopologyOfPoly(idlist1, polys1);
            TopologyOfPoly maketopo2 = new TopologyOfPoly(idlist2, polys2);
            maketopo1.makeTopology();
            maketopo2.makeTopology();
            maketopo1.exportToTopology(out topo1);
            maketopo2.exportToTopology(out topo2);
            //string txt1 = @"D:\研究生项目\tempdata\topo1.txt";    
            //maketopo1.outTopologyToText(txt1);

            CreateMissBufferGeater3 createMissBuffer = new CreateMissBufferGeater3(topo1, topo2, idlist1, idlist2);
            Dictionary<int, List<Geometry>> centerarcsDic = createMissBuffer.createArasForMissNewVersion(10);
            //createArasForMissNewVersion Dictionary<int, List<Geometry>> centerarcsDic = createMissBuffer.createArasForMiss(10);
            // Dictionary<int, Geometry> buffers = createMissBuffer.getbuffers(centerarcsDic, 0.8,10);
            Dictionary<int, Geometry> buffers = createMissBuffer.getbuffers(centerarcsDic, topo1.arcs_poly_Pairs, topo2.polys, 0.1, 10,polygonIO1.getSpatialRef());
            //string savepath = @"D:\研究生项目\弧段轮廓线建模\buffer增添\testdata\buffer\buffers8.shp";

            string savepath = WrapWorker.getnewFileName(workspace,name1+ "clipmiddata" + id.ToString(), "shp");
            saveDictionaryGeom(buffers, savepath, idFieldname, polygonIO1.getSpatialRef());
            string savefinalresult = WrapWorker.getnewFileName(workspace, name2+"bufferfinaldata" + id.ToString(), "shp");
            createMissBuffer.createEraseBufferData(savefinalresult, "sections", idFieldname, savepath, path2, polygonIO1.getSpatialRef());
            return savefinalresult;
        }

        static public bool makeTopotForSections(string sectionpath1, string sectionpath2, out Dictionary<string, string> dataPathCollection, string workspace, string idFieldName)
        {
            dataPathCollection = new Dictionary<string, string>();
            string dataspace = workspace + "\\topo" + getNowTimeNumString();
            if (!Directory.Exists(dataspace))
            {
                Directory.CreateDirectory(dataspace);
            }
            #region
            // 在这确定一下这个datacollection的内容
            dataPathCollection.Add("path1", sectionpath1);
            dataPathCollection.Add("txt1", "");
            dataPathCollection.Add("outarc1", "");
            dataPathCollection.Add("outpoint1", "");
            dataPathCollection.Add("path2", sectionpath2);
            dataPathCollection.Add("txt2", "");
            dataPathCollection.Add("outarc2", "");
            dataPathCollection.Add("outpoint2", "");
            dataPathCollection.Add("arcPair", "");
            dataPathCollection.Add("pointPair", "");
            #endregion
            dataPathCollection["txt1"] = getnewFileName(dataspace, "txt1", "txt");
            dataPathCollection["txt2"] = getnewFileName(dataspace, "txt2", "txt");
            dataPathCollection["outarc1"] = getnewFileName(dataspace, "outarc1", "shp");
            dataPathCollection["outarc2"] = getnewFileName(dataspace, "outarc2", "shp");
            dataPathCollection["outpoint1"] = getnewFileName(dataspace, "outpoint1", "shp");
            dataPathCollection["outpoint2"] = getnewFileName(dataspace, "outpoint2", "shp");
            dataPathCollection["arcPair"] = getnewFileName(dataspace, "arcPair", "txt");
            dataPathCollection["pointPair"] = getnewFileName(dataspace, "pointPair", "txt");
            //到这就把所有的数据的参数都搞定了
            PolygonIO polygonIO = new PolygonIO(dataPathCollection["path1"], idFieldName);
            Dictionary<int, Geometry> polys;
            List<int> idlist;
            polygonIO.getGeomAndId(out polys, out idlist);
            TopologyOfPoly topologyworker1 = new TopologyOfPoly(idlist, polys);
            topologyworker1.makeTopology();


            topologyworker1.outTopologyToText(dataPathCollection["txt1"]);
            topologyworker1.saveArcsInshp(dataPathCollection["outarc1"], "lines", polygonIO.getSpatialRef());
            topologyworker1.savePointsInShp(dataPathCollection["outpoint1"], "points", polygonIO.getSpatialRef());
            Topology topology1;
            topologyworker1.exportToTopology(out topology1);

            PolygonIO polygonIO2 = new PolygonIO(dataPathCollection["path2"], idFieldName);
            Dictionary<int, Geometry> polys2;
            List<int> idlist2;
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            TopologyOfPoly topologyworker2 = new TopologyOfPoly(idlist2, polys2);
            topologyworker2.makeTopology();
            Topology topology2;
            topologyworker2.exportToTopology(out topology2);
            topologyworker2.outTopologyToText(dataPathCollection["txt2"]);
            topologyworker2.saveArcsInshp(dataPathCollection["outarc2"], "lines", polygonIO.getSpatialRef());
            topologyworker2.savePointsInShp(dataPathCollection["outpoint2"], "points", polygonIO.getSpatialRef());
            CoupleFinderWithTopology coupleFinder = new CoupleFinderWithTopology(topology1, topology2);
            IndexPairs arcindexPairs, pointindexPairs;
            int arcsCount = getFeatureCount(dataPathCollection["outarc1"]);

            // coupleFinder.makeArcPairs(out arcindexPairs, out pointindexPairs);
            coupleFinder.makeArcPairsByRing(out arcindexPairs, out pointindexPairs);
            string txtpath1 = dataPathCollection["arcPair"];
            string txtpath2 = dataPathCollection["pointPair"];
            if (arcsCount > arcindexPairs.indexs1.Count)
            {
                return false;
            }
            saveIndexPairToTxt(txtpath1, arcindexPairs);
            saveIndexPairToTxt(txtpath2, pointindexPairs);
            return true;
        }
        static public int getFeatureCount(string path)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.Open(path, 1);
            Layer layer = ds.GetLayerByIndex(0);
            int featurecount = (int)layer.GetFeatureCount(1);
            layer.Dispose();
            ds.Dispose();
            return featurecount;
        }
        static public void transdataTo3D(Dictionary<string, string> dataPathCollection, out Dictionary<string, string> resultPathCollection,string workspacepath = "")
        {
            //这个就是，把所有的这个需要输入的data都给变成3d的输入进去
            //这个dictionary里边，path1，和path2分别带有他们的三维参数属性
            string path1 = dataPathCollection["path1"];
            string path2 = dataPathCollection["path2"];
            Dictionary<string, double> attri1 = getTransformAttri(path1);
            Dictionary<string, double> attri2 = getTransformAttri(path2);
            if (workspacepath == "") {
                workspacepath = Path.GetDirectoryName(path1);//获取工作空间
            }
                resultPathCollection = new Dictionary<string, string>();
            string path13d = getnewFileName(workspacepath, "path13d", "shp");
            string arc13d = getnewFileName(workspacepath, "arc13d", "shp");
            string point13d = getnewFileName(workspacepath, "point13d", "shp");
            string path23d = getnewFileName(workspacepath, "path23d", "shp");
            string arc23d = getnewFileName(workspacepath, "arc23d", "shp");
            string point23d = getnewFileName(workspacepath, "point23d", "shp");
            /*在此提醒，dataPathCollection的内容
                         dataPathCollection.Add("path1", sectionpath1);
            dataPathCollection.Add("txt1", "");
            dataPathCollection.Add("outarc1", "");
            dataPathCollection.Add("outpoint1", "");
            dataPathCollection.Add("path2", sectionpath2);
            dataPathCollection.Add("txt2", "");
            dataPathCollection.Add("outarc2", "");
            dataPathCollection.Add("outpoint2", "");
            dataPathCollection.Add("arcPair", "");
            dataPathCollection.Add("pointPair", "");
             */
            resultPathCollection.Add("path1", path13d);
            resultPathCollection.Add("txt1", dataPathCollection["txt1"]);
            resultPathCollection.Add("outarc1", arc13d);
            resultPathCollection.Add("outpoint1", point13d);
            resultPathCollection.Add("path2", path23d);
            resultPathCollection.Add("txt2", dataPathCollection["txt2"]);
            resultPathCollection.Add("outarc2", arc23d);
            resultPathCollection.Add("outpoint2", point23d);
            resultPathCollection.Add("arcPair", dataPathCollection["arcPair"]);
            resultPathCollection.Add("pointPair", dataPathCollection["pointPair"]);
            convert2DTo3D(dataPathCollection["path1"], resultPathCollection["path1"], attri1, 1);
            convert2DTo3D(dataPathCollection["outarc1"], resultPathCollection["outarc1"], attri1, 2);
            convert2DTo3D(dataPathCollection["outpoint1"], resultPathCollection["outpoint1"], attri1, 3);
            convert2DTo3D(dataPathCollection["path2"], resultPathCollection["path2"], attri2, 1);
            convert2DTo3D(dataPathCollection["outarc2"], resultPathCollection["outarc2"], attri2, 2);
            convert2DTo3D(dataPathCollection["outpoint2"], resultPathCollection["outpoint2"], attri2, 3);
            //到这，就把所有的三维的数据都搞定了，问题不大，很好很好。
        }
        static private void convert2DTo3D(string inputpath, string outputpath, Dictionary<string, double> transAttri, int handletype)//这个是操作参数，如果为1 则是poly，2是line，3是point
        {
            string inputshp = inputpath;
            string outputshp = outputpath;
            string inputpointshp = inputpath;
            string outputpointshp = outputpath;
            double[] transAttri1;
            //这个handletype是操作参数，如果为1 则是poly，2是line，3是point
            ConvertArcs2DTo3DWholeShp convertArcs2DTo3DWholeShp = new ConvertArcs2DTo3DWholeShp();
            if (handletype == 1)
            {
                convertArcs2DTo3DWholeShp.OpenPolysShpAndConvert(inputshp, outputshp, out transAttri1);
            }
            else
            {
                //  Dictionary<string, double> transatt = convertArcs2DTo3DWholeShp.getTransformAttri(inputshp);
                if (handletype == 2)
                {
                    convertArcs2DTo3DWholeShp.ConvertLines2DTo3D(inputpointshp, outputpointshp, transAttri);
                }
                else
                {
                    convertArcs2DTo3DWholeShp.ConvertPoint2DTo3D(inputpointshp, outputpointshp, transAttri);
                }
            }
        }
        /// <summary>
        /// 简单轮廓线建模方法
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="outputspace"></param>
        /// <param name="idFieldName"></param>
        /// <param name="arcmodel_polyids"></param>
        /// <param name="modelname"></param>
        /// <param name="section1name"></param>
        /// <param name="section2name"></param>
        /// <param name="savemapping"></param>
        /// <returns></returns>
        static public List<ModelWithArc> makeLOD1Model(Dictionary<string, string> dataPath, string outputspace, string idFieldName, out List<int[]> arcmodel_polyids, string modelname = "LOD1Model", string section1name = "", string section2name = "", bool savemapping = false)
        {
            string arc1path = dataPath["outarc1"];
            string arc2path = dataPath["outarc2"];
            string point1path = dataPath["outpoint1"];
            string point2path = dataPath["outpoint2"];
            string arcpairtxt = dataPath["arcPair"];
            string pointpairtxt = dataPath["pointPair"];
            string poly1path = dataPath["path1"];
            string poly2path = dataPath["path2"];
            ReadSHPPairs readSHPPairs = new ReadSHPPairs(arc1path, arc2path, point1path, point2path, arcpairtxt, pointpairtxt);
            Dictionary<int, Geometry> arcs1;
            Dictionary<int, int[]> arc_polys1;
            Dictionary<int, int[]> arc_points1;
            Dictionary<int, Geometry> points1;
            Dictionary<int, Geometry> arcs2;
            Dictionary<int, int[]> arc_polys2;
            Dictionary<int, int[]> arc_points2;
            Dictionary<int, Geometry> points2;
            Dictionary<int, int> arcparis;
            Dictionary<int, int> pointpairs;
            readSHPPairs.getArsandPairs(out arcs1, out arc_polys1, out arc_points1, out points1,
            out arcs2, out arc_polys2, out arc_points2, out points2,
            out arcparis, out pointpairs);
            ArcPairsFactory arcPairsFactory = new ArcPairsFactory();

            List<ModelWithArc> modelWithArcs;
            arcPairsFactory.CreateArcPairs(arcs1, arc_polys1, arc_points1, points1, arcs2, arc_polys2, arc_points2, points2, arcparis, pointpairs, arc1path, arc2path, out modelWithArcs);
            ContourHelp contourHelp1 = new ContourHelp();
            if (savemapping == false)
            {

                //contourHelp1.buildModelsWithArcsPairsIncreaseDensification(ref modelWithArcs);
                contourHelp1.buildModelsWithArcsPairs(ref modelWithArcs);
            }
            else
            {
                string middatapath = outputspace + '\\' + "mappingdata" + getNowTimeNumString();
                if (!Directory.Exists(middatapath))
                {
                    Directory.CreateDirectory(middatapath);
                }
                SpatialReference spatialReference = getSpatialRef(arc1path);
                contourHelp1.buildModelsWithArcsPairs(ref modelWithArcs, true, middatapath, spatialReference);
            }
            //  List<int[]> arcmodel_polyids;//记录一下这个模型的id
            createModelPolyPair(modelWithArcs, arc_polys1, out arcmodel_polyids);
            Dictionary<int, BrepModel> sectionModels = SectionMergeByID.MergeArcModel(modelWithArcs, arc_polys1, arc_polys2);
            //BrepModelHelp brepModelHelp = new BrepModelHelp(sectionModels);
            //brepModelHelp.ExportToObjByDic(@"D:\研究生项目\弧段轮廓线建模\完整模型制作", "fullmodel5");
            //这里插入一下，制作这些地层封口
            sectionModels = addSectionPolyToModel(poly1path, idFieldName, sectionModels);
            sectionModels = addSectionPolyToModel(poly2path, idFieldName, sectionModels);
            // BrepModelHelp brepModelHelp = new BrepModelHelp(brepresults);
            BrepModelHelp brepModelHelp2 = new BrepModelHelp(sectionModels);
            modelname = modelname + '_' + section1name + '_' + section2name + '_' + getNowTimeNumString();//给模型自动打个时间名
            brepModelHelp2.ExportToObjByDic(outputspace, modelname);
            return modelWithArcs;
        }
        static public List<ModelWithArc> makeLOD1ModelByCurve(Dictionary<string, string> dataPath, string outputspace, string idFieldName, int transitioncount, out List<int[]> arcmodel_polyids,
            string pointBezierpath1,string pointBezierpath2,  string modelname = "LOD1Model", string section1name = "", string section2name = "", bool savemapping = false,string beziername="id") 
        {//两个线输进去，然后再输入控制点，然后返回一个List<List<Vertex>>列表
         //对这个vertex列表的name重新编组然后放进建模程序里边
         //建出来的模型给它放在一起
         //所以现在最大的麻烦就是如何输入，这个要好好想想。
         //争取明天把这块的代码搞定，后天上午调一下，后天下午开始搞表面平面建模的，周五可以把代码基本搞定周六调通。
         //加油啦，不能像今天这样子了
            string arc1path = dataPath["outarc1"];
            string arc2path = dataPath["outarc2"];
            string point1path = dataPath["outpoint1"];
            string point2path = dataPath["outpoint2"];
            string arcpairtxt = dataPath["arcPair"];
            string pointpairtxt = dataPath["pointPair"];
            string poly1path = dataPath["path1"];
            string poly2path = dataPath["path2"];
            ReadSHPPairs readSHPPairs = new ReadSHPPairs(arc1path, arc2path, point1path, point2path, arcpairtxt, pointpairtxt);
            Dictionary<int, Geometry> arcs1;
            Dictionary<int, int[]> arc_polys1;
            Dictionary<int, int[]> arc_points1;
            Dictionary<int, Geometry> points1;
            Dictionary<int, Geometry> arcs2;
            Dictionary<int, int[]> arc_polys2;
            Dictionary<int, int[]> arc_points2;
            Dictionary<int, Geometry> points2;
            Dictionary<int, int> arcparis;
            Dictionary<int, int> pointpairs;
            readSHPPairs.getArsandPairs(out arcs1, out arc_polys1, out arc_points1, out points1,
            out arcs2, out arc_polys2, out arc_points2, out points2,
            out arcparis, out pointpairs);
            //读取贝塞尔参数点图层
            Dictionary<int, double> bezierdx1, bezierdy1, bezierdz1;
            Dictionary<int, double> bezierdx2, bezierdy2, bezierdz2;
            attrireader attrireader1 = new attrireader(pointBezierpath1);
            bezierdx1 = attrireader1.getattridoublelistByName("dx", beziername);
            bezierdy1 = attrireader1.getattridoublelistByName("dy", beziername);
            bezierdz1 = attrireader1.getattridoublelistByName("dz", beziername);
            attrireader attrireader2 = new attrireader(pointBezierpath2);
            bezierdx2 = attrireader2.getattridoublelistByName("dx", beziername);
            bezierdy2 = attrireader2.getattridoublelistByName("dy", beziername);
            bezierdz2 = attrireader2.getattridoublelistByName("dz", beziername);

            ArcPairsFactory arcPairsFactory = new ArcPairsFactory();

            List<ModelWithArc> modelWithArcs;
            arcPairsFactory.CreateArcPairs(arcs1, arc_polys1, arc_points1, points1, arcs2, arc_polys2, arc_points2, points2, arcparis, pointpairs, arc1path, arc2path, out modelWithArcs);
            //到这就把建模的弧段部分对应做好了，
            List<ModelWithArc> modelWithArcsresult;
            BezierMorphingModelWorker.Modeling(modelWithArcs, transitioncount, out modelWithArcsresult, bezierdx1, bezierdy1, bezierdz1, bezierdx2, bezierdy2, bezierdz2);
            createModelPolyPair(modelWithArcsresult, arc_polys1, out arcmodel_polyids);
            Dictionary<int, BrepModel> sectionModels = SectionMergeByID.MergeArcModel(modelWithArcsresult, arc_polys1, arc_polys2);
            //BrepModelHelp brepModelHelp = new BrepModelHelp(sectionModels);
            //brepModelHelp.ExportToObjByDic(@"D:\研究生项目\弧段轮廓线建模\完整模型制作", "fullmodel5");
            //这里插入一下，制作这些地层封口
            sectionModels = addSectionPolyToModel(poly1path, idFieldName, sectionModels);
            sectionModels = addSectionPolyToModel(poly2path, idFieldName, sectionModels);
            // BrepModelHelp brepModelHelp = new BrepModelHelp(brepresults);
            BrepModelHelp brepModelHelp2 = new BrepModelHelp(sectionModels);
            modelname = modelname + '_' + section1name + '_' + section2name + '_' + getNowTimeNumString();//给模型自动打个时间名
            brepModelHelp2.ExportToObjByDic(outputspace, modelname);
            return modelWithArcsresult;
        }
        static public List<ModelWithArc> makeLOD1ModelByCurveWithSurface(Dictionary<string, string> dataPath, string outputspace, string idFieldName, int transitioncount, out List<int[]> arcmodel_polyids,
           string pointBezierpath1, string pointBezierpath2,
           Dictionary<int, int> surfacep1Tosurfacep2,Dictionary<int,BrepModel> surfaceBreps,string path12D,string path22D,DemIO dem,
           Dictionary<int,Geometry> surfacepoint1,Dictionary<int,Geometry> surfacepoint2,

           string modelname = "LOD1Model", string section1name = "", string section2name = "", bool savemapping = false, string beziername = "id")
        {
            string arc1path = dataPath["outarc1"];
            string arc2path = dataPath["outarc2"];
            string point1path = dataPath["outpoint1"];
            string point2path = dataPath["outpoint2"];
            string arcpairtxt = dataPath["arcPair"];
            string pointpairtxt = dataPath["pointPair"];
            string poly1path = dataPath["path1"];
            string poly2path = dataPath["path2"];
            ReadSHPPairs readSHPPairs = new ReadSHPPairs(arc1path, arc2path, point1path, point2path, arcpairtxt, pointpairtxt);
            Dictionary<int, Geometry> arcs1;
            Dictionary<int, int[]> arc_polys1;
            Dictionary<int, int[]> arc_points1;
            Dictionary<int, Geometry> points1;
            Dictionary<int, Geometry> arcs2;
            Dictionary<int, int[]> arc_polys2;
            Dictionary<int, int[]> arc_points2;
            Dictionary<int, Geometry> points2;
            Dictionary<int, int> arcparis;
            Dictionary<int, int> pointpairs;
            readSHPPairs.getArsandPairs(out arcs1, out arc_polys1, out arc_points1, out points1,
            out arcs2, out arc_polys2, out arc_points2, out points2,
            out arcparis, out pointpairs);
            //读取贝塞尔参数点图层
            Dictionary<int, double> bezierdx1, bezierdy1, bezierdz1;
            Dictionary<int, double> bezierdx2, bezierdy2, bezierdz2;
            attrireader attrireader1 = new attrireader(pointBezierpath1);
            bezierdx1 = attrireader1.getattridoublelistByName("dx", beziername);
            bezierdy1 = attrireader1.getattridoublelistByName("dy", beziername);
            bezierdz1 = attrireader1.getattridoublelistByName("dz", beziername);
            attrireader attrireader2 = new attrireader(pointBezierpath2);
            bezierdx2 = attrireader2.getattridoublelistByName("dx", beziername);
            bezierdy2 = attrireader2.getattridoublelistByName("dy", beziername);
            bezierdz2 = attrireader2.getattridoublelistByName("dz", beziername);

            ArcPairsFactory arcPairsFactory = new ArcPairsFactory();

            List<ModelWithArc> modelWithArcs;
            arcPairsFactory.CreateArcPairs(arcs1, arc_polys1, arc_points1, points1, arcs2, arc_polys2, arc_points2, points2, arcparis, pointpairs, arc1path, arc2path, out modelWithArcs);
            //到这就把建模的弧段部分对应做好了，
            List<ModelWithArc> modelWithArcsresult;
            BezierMorphingModelWorker.Modeling(modelWithArcs, transitioncount, out modelWithArcsresult, bezierdx1, bezierdy1, bezierdz1, bezierdx2, bezierdy2, bezierdz2);
            createModelPolyPair(modelWithArcsresult, arc_polys1, out arcmodel_polyids);

            //以上是正常建模，
            //下面用其他两种方式进行模型构建。
            //先获取一个这个，弧段分类
            Dictionary<int, Geometry> polys2d1 = shpReader.getGeomListByFile(path12D,idFieldName);
            Dictionary<int, Geometry> polys2d2 = shpReader.getGeomListByFile(path22D, idFieldName);
            TopologyOfPoly topologyOfsection1 = new TopologyOfPoly(polys2d1.Keys.ToList(), polys2d1);
            TopologyOfPoly topologyOfsection2 = new TopologyOfPoly(polys2d2.Keys.ToList(), polys2d2);
            Topology topo1, topo2;
            topologyOfsection1.makeTopology();
            topologyOfsection1.exportToTopology(out topo1);
            topologyOfsection2.makeTopology();
            topologyOfsection2.exportToTopology(out topo2);
            ArcClassify arcClassify1 = new ArcClassify(topo1, -3, surfacepoint1);
            ArcClassify arcClassify2 = new ArcClassify(topo2, -3, surfacepoint2);
            Dictionary<int,ArcClassWtihSurface>arcclass1=  arcClassify1.classifyArcs();
            Dictionary<int, ArcClassWtihSurface> arcclass2 = arcClassify2.classifyArcs();
            for (int k = 0; k < modelWithArcsresult.Count; k++) 
            {
                int arcid = modelWithArcsresult[k].arc1.id;
                if (arcclass1[arcid] == ArcClassWtihSurface.UnderGround)//如果就是地下的弧段，那么就跳过它
                {
                    continue;
                }
                else 
                {
                    if (arcclass1[arcid] == ArcClassWtihSurface.OnSurface)
                    {
                        int id1 = modelWithArcsresult[k].arc1.id;
                        int id2 = modelWithArcsresult[k].arc2.id;
                        int[] poly1 = arc_polys1[id1];
                        int[] poly2 = arc_polys2[id2];
                        int polyid1 = poly1[0];
                        int polyid2 = poly1[1];
                        if (surfaceBreps.ContainsKey(polyid1))
                            modelWithArcsresult[k].setModel(surfaceBreps[polyid1]);
                    }
                    else 
                    {
                    
                    }
                }
            }
            Dictionary<int, BrepModel> sectionModels = SectionMergeByID.MergeArcModel(modelWithArcsresult, arc_polys1, arc_polys2);
            //BrepModelHelp brepModelHelp = new BrepModelHelp(sectionModels);
            //brepModelHelp.ExportToObjByDic(@"D:\研究生项目\弧段轮廓线建模\完整模型制作", "fullmodel5");
            //这里插入一下，制作这些地层封口
            sectionModels = addSectionPolyToModel(poly1path, idFieldName, sectionModels);
            sectionModels = addSectionPolyToModel(poly2path, idFieldName, sectionModels);
            // BrepModelHelp brepModelHelp = new BrepModelHelp(brepresults);
            BrepModelHelp brepModelHelp2 = new BrepModelHelp(sectionModels);
            modelname = modelname + '_' + section1name + '_' + section2name + '_' + getNowTimeNumString();//给模型自动打个时间名
            brepModelHelp2.ExportToObjByDic(outputspace, modelname);
            return modelWithArcsresult;
        }
        static public Dictionary<int, BrepModel> makeLOD2Model(Dictionary<string, string> dataPath, out List<int[]> arcmodel_polyids)
        {
            string arc1path = dataPath["outarc1"];
            string arc2path = dataPath["outarc2"];
            string point1path = dataPath["outpoint1"];
            string point2path = dataPath["outpoint2"];
            string arcpairtxt = dataPath["arcPair"];
            string pointpairtxt = dataPath["pointPair"];
            string poly1path = dataPath["path1"];
            string poly2path = dataPath["path2"];
            ReadSHPPairs readSHPPairs = new ReadSHPPairs(arc1path, arc2path, point1path, point2path, arcpairtxt, pointpairtxt);
            Dictionary<int, Geometry> arcs1;
            Dictionary<int, int[]> arc_polys1;
            Dictionary<int, int[]> arc_points1;
            Dictionary<int, Geometry> points1;
            Dictionary<int, Geometry> arcs2;
            Dictionary<int, int[]> arc_polys2;
            Dictionary<int, int[]> arc_points2;
            Dictionary<int, Geometry> points2;
            Dictionary<int, int> arcparis;
            Dictionary<int, int> pointpairs;
            readSHPPairs.getArsandPairs(out arcs1, out arc_polys1, out arc_points1, out points1,
            out arcs2, out arc_polys2, out arc_points2, out points2,
            out arcparis, out pointpairs);
            ArcPairsFactory arcPairsFactory = new ArcPairsFactory();

            List<ModelWithArc> modelWithArcs;
            arcPairsFactory.CreateArcPairs(arcs1, arc_polys1, arc_points1, points1, arcs2, arc_polys2, arc_points2, points2, arcparis, pointpairs, arc1path, arc2path, out modelWithArcs);
            ContourHelp contourHelp1 = new ContourHelp();
            List<BrepModel> brepresults = contourHelp1.buildModelsWithArcsPairs(ref modelWithArcs);
            //  List<int[]> arcmodel_polyids;//记录一下这个模型的id
            createModelPolyPair(modelWithArcs, arc_polys1, out arcmodel_polyids);
            Dictionary<int, BrepModel> sectionModels = SectionMergeByID.MergeArcModel(modelWithArcs, arc_polys1, arc_polys2);
            return sectionModels;
            /*BrepModelHelp brepModelHelp = new BrepModelHelp(sectionModels);
            //brepModelHelp.ExportToObjByDic(@"D:\研究生项目\弧段轮廓线建模\完整模型制作", "fullmodel5");
            //这里插入一下，制作这些地层封口
            sectionModels = addSectionPolyToModel(poly1path, idFieldName, sectionModels);
            sectionModels = addSectionPolyToModel(poly2path, idFieldName, sectionModels);
            // BrepModelHelp brepModelHelp = new BrepModelHelp(brepresults);
            BrepModelHelp brepModelHelp2 = new BrepModelHelp(sectionModels);
            modelname = modelname + '_' + getNowTimeNumString();//给模型自动打个时间名
            brepModelHelp2.ExportToObjByDic(outputspace, modelname);
           
            return modelWithArcs;*/
        }
         public static void saveDictionaryGeom(Dictionary<int, Geometry> buffers, string path, string idname, SpatialReference spatialReference)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Layer layer = dataSource.CreateLayer("buffer", spatialReference, wkbGeometryType.wkbPolygon, null);
            FieldDefn fieldDefn = new FieldDefn(idname, FieldType.OFTInteger);
            layer.CreateField(fieldDefn, 1);
            Feature feature = new Feature(layer.GetLayerDefn());
            foreach (var vk in buffers)
            {
                int id = vk.Key;
                Geometry ge = vk.Value;
                feature.SetField(idname, id);
                feature.SetGeometry(ge);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        static public void saveIndexPairToTxt(string path, IndexPairs indexPairs)
        {
            FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(fileStream);
            string line = "p1,p2";
            writer.WriteLine(line);
            foreach (var vk in indexPairs.indexs1)
            {
                int p1 = vk.Key;
                int p2 = vk.Value;
                line = p1.ToString() + ',' + p2.ToString();
                writer.WriteLine(line);
            }
            writer.Close();
            fileStream.Close();
        }
        static public double getDistanceLinePoint(double x1, double y1, double x2, double y2, double px, double py)
        {
            //这个函数通过直线上两点和线外一点，求解该点到直线的距离。
            double d1 = distance(x1, y1, px, py);
            double d2 = distance(x2, y2, px, py);
            double d5 = distance(x1, y1, x2, y2);
            double d3 = (d1 * d1 - d2 * d2 - d5 * d5) / (-2 * d5);
            double xr2 = d2 * d2 - d3 * d3;
            double x = Math.Sqrt(Math.Abs(xr2));
            return x;
        }
        static double distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        static public string savelinesByDic(List<Eage> listeage, string workspace, string sectionid1, string sectionid2, string importspatialshp, string idFieldName, List<int[]> eage_poly)
        { //为每个剖面线创建一个新的shp文件然后保存所有切下来的数据
            string filename = workspace + "Section" + sectionid1.ToString() + "Section" + sectionid2.ToString() + "interline"  + ".shp";
            string layername = "Section" + sectionid1.ToString() + "Section" + sectionid2.ToString() + "interline";
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(filename, null);
            DataSource importspatialds = driver.Open(importspatialshp, 1);
            Layer templayer = importspatialds.GetLayerByIndex(0);
            Layer layer = dataSource.CreateLayer(layername, templayer.GetSpatialRef(), wkbGeometryType.wkbLineString25D, null);
            FieldDefn fieldDefn = new FieldDefn(idFieldName, FieldType.OFTInteger);
            layer.CreateField(fieldDefn, 1);
            FieldDefn fieldDefn1 = new FieldDefn("polygon1", FieldType.OFTInteger);
            FieldDefn fieldDefn2 = new FieldDefn("polygon2", FieldType.OFTInteger);
            layer.CreateField(fieldDefn1, 1);
            layer.CreateField(fieldDefn2, 1);

            int eagecount = listeage.Count();
            for (int k = 0; k < eagecount; k++)
            {
                Feature feature = new Feature(layer.GetLayerDefn());
                Eage eage = listeage[k];
                Geometry geomline = new Geometry(wkbGeometryType.wkbLineString25D);
                eageToLine3D(eage, ref geomline);
                feature.SetGeometry(geomline);

                int[] polys = eage_poly[k];
                feature.SetField("polygon1", polys[0]);
                feature.SetField("polygon2", polys[1]);
                layer.CreateFeature(feature);
                feature.Dispose();
            }
            layer.Dispose();
            dataSource.Dispose();
            templayer.Dispose();
            importspatialds.Dispose();
            return filename;
        }
        static void eageToLine3D(Eage eage, ref Geometry line)
        {
            int count = eage.vertexList.Count();
            for (int i = 0; i < count; i++)
            {
                Vertex vertex = eage.vertexList[i];
                line.AddPoint(vertex.x, vertex.y, vertex.z);
                // Console.WriteLine(vertex.x.ToString()+ ' '+vertex.y.ToString()+' '+ vertex.z.ToString());
            }
            // string wkt;
            //line.ExportToWkt(out wkt);
            // Console.WriteLine(wkt);
        }
        static public void outputmuliLinestring(string path, Geometry mulitlinestring, SpatialReference spatialReference, string layername)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(path, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbLineString, null);
            Feature feature = new Feature(layer.GetLayerDefn());
            int count = mulitlinestring.GetGeometryCount();
            for (int i = 0; i < count; i++)
            {
                Geometry geom = mulitlinestring.GetGeometryRef(i);
                feature.SetGeometry(geom);
                layer.CreateFeature(feature);
            }
        }
        static public void outputLinestring(string path, Geometry linestring, SpatialReference spatialReference, string layername)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(path, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbLineString, null);
            Feature feature = new Feature(layer.GetLayerDefn());
            feature.SetGeometry(linestring);
            layer.CreateFeature(feature);
        }
        static private void copy3Dattri(string target, string ori)
        {
            Dictionary<string, double> attri = getTransformAttri(ori);
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.Open(target, 1);
            Layer layer = ds.GetLayerByIndex(0);

            Feature feature = layer.GetFeature(0);
            int fieldnum = feature.GetFieldCount();

            foreach (var vk in attri)
            {
                //检测一下所有的这个参数字段，如果没有，就加入

                string name1 = vk.Key;
                bool hasetheField = false;
                for (int i = 0; i < fieldnum; i++)
                {
                    FieldDefn fd = feature.GetFieldDefnRef(i);
                    string fdname = fd.GetName();
                    if (name1.Equals(fdname))
                    {
                        hasetheField = true;
                        break;
                    }
                }
                if (hasetheField == false)
                {
                    FieldDefn fieldDefn = new FieldDefn(name1, FieldType.OFTReal);
                    layer.CreateField(fieldDefn, 1);
                }
            }
            feature.Dispose();
            long featurecount = layer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++)
            {
                Feature featuret = layer.GetFeature(i);
                foreach (var vk in attri)
                {
                    featuret.SetField(vk.Key, vk.Value);
                }
                layer.SetFeature(featuret);
            }
            layer.Dispose();
            ds.Dispose();
        }
        static public void outputpolygon(string path, List<Geometry> polys, SpatialReference spatialReference, string layername)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(path, null);
            Layer layer = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPolygon, null);
            Feature feature = new Feature(layer.GetLayerDefn());
            int count = polys.Count();
            for (int i = 0; i < count; i++)
            {
                Geometry geom = polys[i];
                feature.SetGeometry(geom);
                layer.CreateFeature(feature);
            }
        }
        public static void saveOrisplinter(string oripath, InterShpReader interShpReader, Dictionary<int, Dictionary<int, string>> splitShpPathDic, string idFieldName)
        {//这个是LOD2过程中做分割的代码
            //这个函数主要做两个问题，第一个就是输出原始剖面中需要被建模的那部分，而且最好要带上转换参数，这个问题嘛，应该把原始剖面和插值剖面放在那个三维转二维的程序里去跑得到统一结果(已解决)
            //第二输出应该顺序建模的这个txt，用来指导后续两两之间建模
            string splitfolder = interShpReader.splitfolder;
            //string oripath = interShpReader.orishppath;
            SpatialReference spatialReference = getSpatialRef(interShpReader.orishppath);
            Dictionary<string, double> stendxyz = getStartXYZEndXY(oripath);
            Dictionary<int, Geometry> id_Geoms = new Dictionary<int, Geometry>();

            string st1, st2;
            Dictionary<int, int[]> sharpenPairs = interShpReader.getsharpenPair(interShpReader.gettxtContain(interShpReader.sectionpairpath), out st1, out st2);
            Layer orilayer = openlayer(oripath);
            long featureCount = orilayer.GetFeatureCount(1);
            for (int i = 0; i < featureCount; i++)
            {
                Feature feature = orilayer.GetFeature(i);
                Geometry geomt = feature.GetGeometryRef();
                int idt = feature.GetFieldAsInteger(idFieldName);
                id_Geoms.Add(idt, geomt);
            }
            Dictionary<int, string> zeroShps = new Dictionary<int, string>();
            foreach (var vk in splitShpPathDic)
            {
                int id = vk.Key;
                string path = splitfolder + '\\' + "split" + 'S' + id.ToString() + "S0S0.shp";
                zeroShps.Add(id, path);
                Dictionary<int, Geometry> geoms = new Dictionary<int, Geometry>();
                int[] containpolyids = sharpenPairs[id];
                for (int j = 0; j < containpolyids.Length; j++)
                {
                    geoms.Add(containpolyids[j], id_Geoms[containpolyids[j]]);
                }
                SplitStrataWorker.saveGeom(geoms, path, spatialReference, idFieldName, stendxyz);
            }
            string outputTxt = splitfolder + '\\' + "modelingOrder.txt";
            FileStream fileStream = new FileStream(outputTxt, FileMode.CreateNew);
            StreamWriter writer = new StreamWriter(fileStream);
            int[] ids = splitShpPathDic.Keys.ToArray<int>();
            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                writer.WriteLine(id);
                string zeropath = zeroShps[id];
                writer.WriteLine(zeropath);
                Dictionary<int, string> shps = splitShpPathDic[id];
                foreach (var vk in shps)
                {
                    string temppath = vk.Value;
                    writer.WriteLine(temppath);
                }
            }
            writer.Close();
            fileStream.Close();
        }
        private static Dictionary<string, double> getStartXYZEndXY(string path)
        {
            Layer layer = openlayer(path);
            Feature feature = layer.GetFeature(0);
            Dictionary<string, double> result = new Dictionary<string, double>();
            result.Add("startx", feature.GetFieldAsDouble("startx"));
            result.Add("starty", feature.GetFieldAsDouble("starty"));
            result.Add("startz", feature.GetFieldAsDouble("startz"));
            result.Add("endx", feature.GetFieldAsDouble("endx"));
            result.Add("endy", feature.GetFieldAsDouble("endy"));
            feature.Dispose();
            layer.Dispose();
            return result;
        }
        public static void saveGeom(List<Geometry> geometries, string path, string spatialpath)
        {

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Geometry geom1 = geometries[0];
            Layer layer = dataSource.CreateLayer("result", getSpatialRef(spatialpath), geom1.GetGeometryType(), null);
            int count = geometries.Count;
            for (int i = 0; i < count; i++)
            {
                Geometry geom = geometries[i];
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(geom);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public static Layer openlayer(string path)
        {
            /*Gdal.AllRegister();
            Ogr.RegisterAll();
            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");*/
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            return layer;
        }
        public static SpatialReference getSpatialRef(string path)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            SpatialReference spatialReference = layer.GetSpatialRef();
            layer.Dispose();
            dataSource.Dispose();
            return spatialReference;
        }
        public static void earseSharpenSection(string shppath1,string shppath2,string name1,string name2,string workspace,string idFieldName,out string resultpath1,out string resultpath2) {
            resultpath1 = WrapWorker.getnewFileName(workspace, name1, "shp");
            resultpath2 = WrapWorker.getnewFileName(workspace, name2, "shp");
            SpatialReference spatialReference;
            Dictionary<int, Geometry> geoms1 = shpReader.getGeomListByFile(shppath1, idFieldName, out spatialReference);
            Dictionary<int, Geometry> geoms2 = shpReader.getGeomListByFile(shppath2, idFieldName);
            Dictionary<int, Geometry> resultg1, resultg2;
            EraseSharpen.eraseSharpenStra(geoms1, geoms2, out resultg1, out resultg2);
            shpReader.saveDicOfGeoms(resultpath1, resultg1, idFieldName, spatialReference);
            shpReader.saveDicOfGeoms(resultpath2, resultg2, idFieldName, spatialReference);
        }

        #region 
        //一些需要用到的这个小工具函数
        static public Dictionary<string, double> getTransformAttri(string path)
        {//取出一个数据的这个三维坐标转换参数
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            // Feature feature0 = layer.GetFeature(0);
            double startx = 0, starty = 0, endx = 0, endy = 0, startz = 0, endz;
            /*startx = feature0.GetFieldAsDouble("startx");
            starty = feature0.GetFieldAsDouble("starty");
            startz = feature0.GetFieldAsDouble("startz");
            endx = feature0.GetFieldAsDouble("endx");
            endy = feature0.GetFieldAsDouble("endy");*/
            // endz = feature0.GetFieldAsDouble("startx");
            getstartendxyzBylayer(ref startx, ref starty, ref endx, ref endy, ref startz, layer);
            Dictionary<string, double> result = new Dictionary<string, double>();
            result.Add("startx", startx);
            result.Add("starty", starty);
            result.Add("startz", startz);
            result.Add("endx", endx);
            result.Add("endy", endy);
            layer.Dispose();
            dataSource.Dispose();
            return result;
        }
        static private void getstartendxyzBylayer(ref double startx, ref double starty, ref double endx, ref double endy, ref double startz, Layer layer)
        {
            //做个这个主要是防止默认的第一个feature是不含startx之类信息的
            int count = (int)layer.GetFeatureCount(1);
            int i = 0;
            do
            {
                Feature feature0 = layer.GetFeature(i);
                startx = feature0.GetFieldAsDouble("startx");
                starty = feature0.GetFieldAsDouble("starty");
                startz = feature0.GetFieldAsDouble("startz");
                endx = feature0.GetFieldAsDouble("endx");
                endy = feature0.GetFieldAsDouble("endy");
                feature0.Dispose();
                i++;
            }
            while (startx == 0 && i < count);
        }
        static void createModelPolyPair(List<ModelWithArc> modelWithArcs, Dictionary<int, int[]> arc_polys, out List<int[]> model_polys)
        {
            model_polys = new List<int[]>();
            int count = modelWithArcs.Count;
            for (int i = 0; i < count; i++)
            {
                ModelWithArc modelWithArc = modelWithArcs[i];
                ArcSe arcSe = modelWithArc.arc1;
                int id1 = arcSe.id;
                int[] polys = arc_polys[id1];
                model_polys.Add(polys);
            }
        }
        static Dictionary<int, BrepModel> addSectionPolyToModel(string sectionshpPath, string idFieldName, Dictionary<int, BrepModel> models)
        {
            Dictionary<int, Geometry> geoms = shpReader.getGeomListByFile(sectionshpPath, idFieldName);
            Dictionary<int, BrepModel> result = new Dictionary<int, BrepModel>();
            ConvertArcs3DTo2DWholeShp convertArcs3DTo2 = new ConvertArcs3DTo2DWholeShp();
            double[] tranAttri;
            Dictionary<int, List<double[]>> geomxyz;
            Dictionary<int, List<double[]>> poly2dlist = convertArcs3DTo2.OpenPolysShpAndConvert(geoms, out tranAttri, out geomxyz);
            Dictionary<int, BrepModel> breps = CreateBrepModelByXY.MakePolyToModel(geomxyz, poly2dlist);
            // BrepModelHelp brepModelHelp = new BrepModelHelp(breps.Values.ToList<BrepModel>());
            foreach (var vk in models)
            {
                int id = vk.Key;
                BrepModel m1 = vk.Value;
                if (breps.Keys.Contains<int>(id) == false)
                {
                    continue;
                }
                BrepModel m2 = breps[id];
                BrepModel[] brepModels = { m1, m2 };
                BrepModel mergeB = MergeBreps.Merge3DModel(brepModels.ToList<BrepModel>());
                result.Add(id, mergeB);
            }
            return result;
        }
        #endregion
    }
}

