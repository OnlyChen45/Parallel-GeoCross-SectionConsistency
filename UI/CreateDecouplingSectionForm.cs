using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ThreeDModelSystemForSection;
using GeoCommon;
using SolidModel;
using OSGeo.OGR;
using OSGeo.GDAL;
using OSGeo.OSR;
namespace DotSpatialForm.UI
{
    public partial class CreateDecouplingSectionForm : Form
    {
        public string[] pathlist;
        private bool pair;
        private int groupcount;
        public CreateDecouplingSectionForm()
        {
            InitializeComponent();
        }

        private void CreateDecouplingSectionForm_Load(object sender, EventArgs e)
        {
            SectionPath.Items.AddRange(pathlist);
            pair = true;
            groupcount = 0;
            usecount = new Dictionary<string, int>();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (pair == false) //如果成对标志为false，那么就不增加新的成对编号，
            {
                int index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells[0].Value = groupcount;
                dataGridView1.Rows[index].Cells[1].Value = SectionPath.Text;
                pair = true;
            }
            else//如果 
            {
                groupcount++;
                int index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells[0].Value = groupcount;
                dataGridView1.Rows[index].Cells[1].Value = SectionPath.Text;
                pair = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int currRow = dataGridView1.CurrentRow.Index;
                int pairdata = (int)dataGridView1.Rows[currRow].Cells[0].Value;
                dataGridView1.Rows.RemoveAt(currRow);
                groupcount--;
                int rcount = dataGridView1.Rows.Count - 1;
                int indexPairRow = -1;
                for (int i = 0; i < rcount; i++)
                {
                    int t = (int)dataGridView1.Rows[i].Cells[0].Value;
                    if (t == pairdata)
                    {
                        indexPairRow = i;
                        break;
                    }
                }
                if (indexPairRow != -1)
                {
                    dataGridView1.Rows.RemoveAt(indexPairRow);
                }
            }
            catch
            {
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "*.shp|*.SHP";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                SectionPath.Text = openFileDialog.FileName;

            }
        }

        private void SelectResultfolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "结果文件夹";  //定义在对话框上显示的文本

            if (folder.ShowDialog() == DialogResult.OK)
            {
                ResultFolder.Text = folder.SelectedPath;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        Dictionary<string, int> usecount;//记录使用过的文件名，防止冲突；
        private int addTocount(string path)
        {//返回用过的次数
            if (usecount.ContainsKey(path) == false)
            {
                usecount.Add(path, 1);
                return 1;
            }
            else
            {
                usecount[path]++;
                return usecount[path]++;
            }
        }
        /// <summary>
        /// 这个就是最终执行建模的那个函数。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            //步骤，第一，将输入的两个面的尖灭地层抹掉
            //第二，两抹掉尖灭地层的数据进入三维建模过程，先把拓扑对应gap东
            //第三，再升维，再建模
            //第三，建模完成了，然后切面出来，转成2D的，同时生成一个参数txt
            //这个2D的可能有瑕疵，用那个arcpy过一下面转线转面，然后给碎面扔进大面里
            //这样就获得了一个很好用的面
            List<string[]> pairs = getDataPair();
            FrmMain frm = (FrmMain)this.Owner;
            string resultfolder = ResultFolder.Text;
            string workspaceSS = ResultFolder.Text+"\\middata";
            if (Directory.Exists(workspaceSS) == false) {
                Directory.CreateDirectory(workspaceSS);
            }
            string workspacegdb = createGDB(workspaceSS);
            string modest = frm.initmodel;
            foreach (var pair in pairs) {
                string path1 = pair[0];
                string path2 = pair[1];

                int path1count = addTocount(path1);
                int path2count = addTocount(path2);
                string name1 = Path.GetFileNameWithoutExtension(pair[0])+"_"+path1count.ToString()+"_";
                string name2 = Path.GetFileNameWithoutExtension(pair[1])+"_"+path2count.ToString()+"_";
                string nameerase1 = name1 + "_EraseSharpen";
                string nameerase2 = name2 + "_EraseSharpen";
                string toponame1 = name1 + "_donetopoChange";
                string toponame2 = name2 + "_donetopoChange";
                string rename1 = nameerase1 + "_DoneSt3D";
                string rename2 = nameerase2 + "_DoneSt3D";
                string erasepath1, erasepath2;
                string section2DNor1 = "", section2DNor2 = "";
                //抹掉尖灭地层
                WrapWorker.earseSharpenSection(path1, path2, nameerase1, nameerase2, workspaceSS, frm.idname, out erasepath1, out erasepath2);
                //消除拓扑不一致
                string toporesult1, toporesult2;
                string topobufferpath1, topobufferpath2;
                WrapWorker.dealTopoChange(erasepath1, erasepath2, workspaceSS, out toporesult1, out toporesult2, frm.idname, toponame1, toponame2, out topobufferpath1,out topobufferpath2,1);
                //下面是升维
                switch (modest)
                {
                    //这个就是从第一种数据格式转过来需要的模式，然后把输出的到达了标准化的2D数据的路径信息给更新一下
                    case "Model1":
                        {
                            //string setion1path = worksection1;
                            // string setion2path = worksection2;
                            string worksection1 = toporesult1;
                            string worksection2 = toporesult2;
                            string setion1path = WrapWorker.polyNormalization(worksection1, workspaceSS, workspacegdb, frm.arcpypath, name1+"polynor11", frm.idname);
                            string setion2path = WrapWorker.polyNormalization(worksection2, workspaceSS, workspacegdb, frm.arcpypath, name2+"polynor22", frm.idname);

                            string section13Dparpath = get3Dpar(path1);
                            string section23Dparpath = get3Dpar(path2);
                            string par1 = section13Dparpath;// get3Dpar(this.shpOripath1);
                            string par2 = section23Dparpath;//get3Dpar(this.shpOripath2);
                            if (File.Exists(par1) == false)
                            {
                                frm.NotificationBox.AppendText(section13Dparpath + "三维位置定义文件缺失\n");
                            }
                            if (File.Exists(par2) == false)
                            {
                                frm.NotificationBox.AppendText(section23Dparpath + "三维位置定义文件缺失\n");
                            }
                            Dictionary<string, double> par1dic, par2dic;
                            ThreeDModelSystemForSection.DataIOWorker.Read3Dpara.makeparaIntoFrom(par1, out par1dic);
                            ThreeDModelSystemForSection.DataIOWorker.Read3Dpara.makeparaIntoFrom(par2, out par2dic);
                            string section13D1 = WrapWorker.mode12DToDefault3D(setion1path, workspaceSS, frm.idname, par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"], name1);
                            string section2D1 = WrapWorker.Default3DToDefault2D(section13D1, workspaceSS, frm.idname, name1+"section2D1Poly");
                            section2DNor1 = WrapWorker.polyNormalization(section2D1, workspaceSS, workspacegdb, frm.arcpypath, rename1, frm.idname);

                            string section13D2 = WrapWorker.mode12DToDefault3D(setion2path, workspaceSS, frm.idname, par2dic["startX"], par2dic["startY"], par2dic["startZ"], par2dic["endX"], par2dic["endY"], par2dic["firstX"], par2dic["firstY"], name2);
                            string section2D2 = WrapWorker.Default3DToDefault2D(section13D2, workspaceSS, frm.idname, name2+"section2D2Poly");
                            section2DNor2 = WrapWorker.polyNormalization(section2D2, workspaceSS, workspacegdb, frm.arcpypath, rename2, frm.idname);

                            frm.addtoMap.Add(section2DNor1);
                            frm.addtoMap.Add(section2DNor2);
                            break;
                        }
                    case "Default":
                        {
                            //这个模式就是说，数据是一个符合比较标准的数据的状况，
                            string worksection1 = toporesult1;
                            string worksection2 = toporesult2;
                            section2DNor1 = WrapWorker.polyNormalization(worksection1, workspaceSS, workspacegdb, frm.arcpypath, "polynor1", frm.idname);
                            section2DNor2 = WrapWorker.polyNormalization(worksection2, workspaceSS, workspacegdb, frm.arcpypath, "polynor2", frm.idname);
                            break;
                        }
                }
                //三维建模
                string datapath1 = section2DNor1;
                string datapath2 = section2DNor2;
                //这里采用加密方式建模应当会提高模型的质量
                datapath1 = WrapWorker.polyNormalizationWithDensity(datapath1, workspaceSS, workspacegdb, frm.arcpypath, name1+"fullbuffer1nor", frm.idname);
                datapath2 = WrapWorker.polyNormalizationWithDensity(datapath2, workspaceSS, workspacegdb, frm.arcpypath, name2+"fullbuffer2nor", frm.idname);
                Dictionary<string, string> dataPathCollection;
                bool signal = WrapWorker.makeTopotForSections(datapath1, datapath2, out dataPathCollection, workspaceSS, frm.idname);

                if (signal == false)
                {
                    string mess = "由于创建的尖灭地层对应位置面过于小或其他拓扑问题，需要手动进行修改\n\r请修改以下两个文件以保证符合弧段一一对应的标准\n\r" + datapath1 + "\n\r" + datapath2;
                    MessageBox.Show(mess);
                    frm.NotificationBox.AppendText("建模出错，请检查数据\n\n");
                    return;
                }
                Dictionary<string, string> pathData3d;
                WrapWorker.transdataTo3D(dataPathCollection, out pathData3d);
                List<int[]> arcmodel_polyids;
                List<ModelWithArc> arcmodels=  WrapWorker.makeLOD1Model(pathData3d, ResultFolder.Text, frm.idname, out arcmodel_polyids, "", name1, name2, true);
                //插值
                double rate = 0.5;//取出指定的比例
                //string sectionlinePath = workspaceSS+"\\sectionlines.shp";
                string idFieldName = frm.idname;
                int id1 = 1;
                int id2 = 2;

                string outputfolder = WrapWorker.createDirectory(resultfolder, "intersection");
                string outputspace = outputfolder + '\\';
                string outputs1Tos2 = outputspace;//目的是能够把两个层出发的尖灭的插值剖面给分清楚，所以新建两个文件夹，分别存放两个方向的插值出来的面


                List<Eage> eagesss = new List<Eage>();
                int brepModelcount = arcmodels.Count();
                for (int k = 0; k < brepModelcount; k++)
                {
                    ModelWithArc modelWithArc = arcmodels[k];
                    BrepModel brep = modelWithArc.getModel();
                    ContourIntePHelp intePHelp = new ContourIntePHelp(modelWithArc.arc1.eage, modelWithArc.arc2.eage, brep.mesh);
                    //Eage inteEage = intePHelp.ContourInterPolateByRate(rate);
                    Eage inteEage = intePHelp.ContourInterPolateByRateStable(rate);
                    eagesss.Add(inteEage);
                }
               string interlineresultpath= WrapWorker.savelinesByDic(eagesss, outputs1Tos2,name1,name2, path1, idFieldName, arcmodel_polyids);
                //下面3D线转2D面
                string inputshp = interlineresultpath;
                string shpname = Path.GetFileName(inputshp);
                string outputlinestring =resultfolder+ '\\' + "line" + shpname;
                string polygongout = resultfolder+ '\\' + shpname;
                ConvertArcs3DTo2DWholeShp convertArcs3DTo2D = new ConvertArcs3DTo2DWholeShp();
                double[] transformAttribute;
                convertArcs3DTo2D.OpenArcsShpAndConvertToModel1(inputshp, outputlinestring, out transformAttribute);//这里给三位参数存到了线同位的txt里，所以应当，给面也来一份。
                ConvertArcsToPolygon convertArcsToPolygon = new ConvertArcsToPolygon();
                Dictionary<int, Geometry> polys = convertArcsToPolygon.ReadAndConvert(outputlinestring, "polygon1", "polygon2");
                convertArcsToPolygon.savePolys(polygongout, idFieldName, polys, transformAttribute);

                //到此为止，就求出了中间剖面和它的三位参数
                //但是存在一个很大的问题，就是仍然可能是存在碎屑。
                //所以呢，就要用那个类似于面转线转面的方法去把碎屑拿到，拿到之后给它合并到周围较大的面里，然后再标号再输出。
                Dictionary<int, Geometry> geoms = shpReader.getGeomListByFile(polygongout,idFieldName);
                //string norpath = WrapWorker.polyNormalSimple(polygongout, workspaceSS, workspacegdb, frm.arcpypath, Path.GetFileNameWithoutExtension(polygongout) + "_nor");
                string norpath = WrapWorker.polyNormalization(polygongout, resultfolder, workspacegdb, frm.arcpypath, Path.GetFileNameWithoutExtension(polygongout) + "_nor",frm.idname);
                //string txt3dline = get3Dpar(outputlinestring);
                SpatialReference spatialReference;
                Dictionary<int, Geometry> geomsnor = shpReader.getGeomListByFileWithFID(norpath,out spatialReference);
                int origeomcount = geoms.Keys.Count;
                List<Geometry>geomsright= mergeMiniGeomToOthers(geomsnor.Values.ToList<Geometry>(), origeomcount);//获取了没有细碎问题的面。
                Dictionary<int, Geometry> finalresult = new Dictionary<int, Geometry>();
                for (int i = 0; i < geomsright.Count; i++) {
                    Geometry geomt = geomsright[i];
                    int maxid = -1;
                    double maxinterarea = double.MinValue;
                    foreach (var vk in geoms) {
                        Geometry inter = vk.Value.Intersection(geomt);
                        double area = inter.Area();
                        if (area > maxinterarea) {
                            maxid = vk.Key;
                            maxinterarea = area;
                        }
                    }
                    finalresult.Add(maxid, geomt);
                }
                string resultfinalname= WrapWorker.getnewFileName(resultfolder, name1+'_'+name2+ "_complete", "shp");
                shpReader.saveDicOfGeoms(resultfinalname, finalresult,frm.idname, spatialReference);
               

                /*string par1txt = get3Dpar(outputlinestring);
                string par2txt = get3Dpar(resultfinalname);
                FileStream fileStream1 = new FileStream(par1txt, FileMode.Open);
                FileStream fileStream11 = new FileStream(par2txt, FileMode.Create);
                fileStream1.CopyTo(fileStream11);
                fileStream1.Close();
                fileStream11.Close();*/

                //最后还应该制作两个剖面的可以和插值面建模的那个shp文件
                //具体就是，取出拓扑对应结果，取出
                string outputoripath1 = resultfolder +'\\'+ name1 + "_TopoDone.shp";
                string outputoripath2 = resultfolder +'\\'+ name2 + "_TopoDone.shp";
                DealTopoChange.createEraseBufferData(outputoripath1, "section1", frm.idname, topobufferpath1, path1);
                DealTopoChange.createEraseBufferData(outputoripath2, "section2", frm.idname, topobufferpath2, path2);
                copy3Dtxt(outputlinestring, resultfinalname);
                copy3Dtxt(path1, outputoripath1);
                copy3Dtxt(path2, outputoripath2);


                frm.addtoMap.Add(resultfinalname);
                frm.addtoMap.Add(outputoripath1);
                frm.addtoMap.Add(outputoripath2);
            }
            this.Close();
        }
        private void copy3Dtxt(string path1,string path2) {
            string par1txt = get3Dpar(path1);
            string par2txt = get3Dpar(path2);
            FileStream fileStream1 = new FileStream(par1txt, FileMode.Open);
            FileStream fileStream11 = new FileStream(par2txt, FileMode.Create);
            fileStream1.CopyTo(fileStream11);
            fileStream1.Close();
            fileStream11.Close();
        }
        private List<string[]> getDataPair()
        {//把datagridview里所有组都读出来返回成一个列表
            int count = dataGridView1.Rows.Count - 1;
            Dictionary<int, List<string>> pairs = new Dictionary<int, List<string>>();
            for (int i = 0; i < count; i++)
            {
                int p = (int)dataGridView1.Rows[i].Cells[0].Value;
                string path = (string)dataGridView1.Rows[i].Cells[1].Value;
                if (pairs.ContainsKey(p))
                {
                    pairs[p].Add(path);
                }
                else
                {
                    List<string> st = new List<string>();
                    st.Add(path);
                    pairs.Add(p, st);
                }
            }
            List<string[]> result = new List<string[]>();
            foreach (var vk in pairs)
            {
                result.Add(vk.Value.ToArray());
            }
            return result;
        }
        private string createGDB(string folder)
        {
            string workspacegdb = "";
            string[] files = Directory.GetDirectories(folder);
            bool containGDB = false;
            foreach (string st in files)
            {
                string filename1 = Path.GetFileName(st);
                if (filename1.Equals("tempWrokspace.gdb"))
                {
                    containGDB = true;
                    workspacegdb = st;
                    break;
                }
            }
            if (containGDB == false)
            {
                workspacegdb = WrapWorker.createTempGDB(folder);
            }
            return workspacegdb;
        }
        string get3Dpar(string shppath)
        {
            string[] pathsplit = shppath.Split('.');
            string resu = pathsplit[0] + ".txt";
            return resu;
        }
        static public int BoundaryID = -1;
        List<Geometry> mergeMiniGeomToOthers(List<Geometry> geoms,int bigcount) {
            int geomcount = geoms.Count;
            int minicount = geomcount - bigcount;
            //List< Geometry> result = new List<Geometry>();
            List<Geometry> geomsworking = new List<Geometry>(geoms.ToArray());
            List<double> areas = new List<double>();
            Geometry biggeom = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var tg in geoms)
            {
                biggeom = biggeom.Union(tg);
            }
            for (int i = 0; i < geomsworking.Count; i++) {
                double area = geomsworking[i].Area();
                areas.Add(area);
            }
            if (minicount <= 0) return geoms;
            for (int i = 0; i < minicount; i++) {
                int miniindex = -1;
                double miniarea = double.MaxValue;
                for (int j = 0; j < geomsworking.Count; j++) {
                    if (areas[j] < miniarea) {
                        miniindex = j;
                        miniarea = areas[j];
                    }
                }
                Geometry geomtemp = geomsworking[miniindex];
                geomsworking.RemoveAt(miniindex);
                areas.RemoveAt(miniindex);
                int mergeid=-2;
                double lengthmax = double.MinValue;
                for(int j=0;j< geomsworking.Count;j++) {
                    Geometry get = geomsworking[j];
                    bool touches = touchCheck(geomtemp, get);
                    if (touches) {
                        double lengtht= getLineLength(geomtemp.Intersection(get));
                        if (lengtht > lengthmax) {
                            mergeid = j;
                            lengthmax = lengtht;
                        }
                    }
                }
                geomsworking[mergeid] = geomsworking[mergeid].Union(geomtemp);
                areas[mergeid] = geomsworking[mergeid].Area();
            }
            return geomsworking;
        }
        bool touchCheck(Geometry geom1,Geometry geom2) {
            Geometry geominter = geom1.Intersection(geom2);
            if (geominter.IsEmpty() == true) return false;
            wkbGeometryType resulttype = geominter.GetGeometryType();
            //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
            if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbPolygon || resulttype == wkbGeometryType.wkbMultiPolygon|| resulttype == wkbGeometryType.wkbGeometryCollection)
            {
                if (resulttype == wkbGeometryType.wkbGeometryCollection) {
                    int count = geominter.GetGeometryCount();
                    for (int j = 0; j < count; j++) {
                        wkbGeometryType ttype = geominter.GetGeometryRef(j).GetGeometryType();
                        if (ttype == wkbGeometryType.wkbLineString || ttype == wkbGeometryType.wkbMultiLineString || ttype == wkbGeometryType.wkbPolygon || ttype == wkbGeometryType.wkbMultiPolygon) {
                            return true;
                        }
                    }
                    return false;
                } else {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 把一个线（各种形式）的长度获取到
        /// </summary>
        /// <param name="linegeom"></param>
        /// <returns></returns>
        double getLineLength(Geometry linegeom)
        {
            wkbGeometryType geomtype = linegeom.GetGeometryType();
            double lengthsum = 0;
            switch (geomtype)
            {
                case wkbGeometryType.wkbMultiLineString:
                    {
                        int linecount = linegeom.GetGeometryCount();
                        for (int i = 0; i < linecount; i++)
                        {
                            Geometry linet = linegeom.GetGeometryRef(i);
                            lengthsum = lengthsum + linet.Length();
                        }
                        break;
                    }
                case wkbGeometryType.wkbLineString:
                    {
                        lengthsum = lengthsum + linegeom.Length();
                        break;
                    }
                case wkbGeometryType.wkbGeometryCollection:
                    {
                        int linecount = linegeom.GetGeometryCount();
                        for (int i = 0; i < linecount; i++)
                        {
                            switch (geomtype)
                            {
                                case wkbGeometryType.wkbMultiLineString:
                                    {
                                        int linecountt = linegeom.GetGeometryCount();
                                        for (int j = 0; j < linecountt; j++)
                                        {
                                            Geometry linet = linegeom.GetGeometryRef(j);
                                            lengthsum = lengthsum + linet.Length();
                                        }
                                        break;
                                    }
                                case wkbGeometryType.wkbLineString:
                                    {
                                        lengthsum = lengthsum + linegeom.Length();
                                        break;
                                    }
                            }
                        }
                        break;
                    }
            }
            return lengthsum;
        }
        static Dictionary<int, List<int>> getPolyTouches(Dictionary<int, Geometry> polys)
        {
            //生成剖面中的各个地层的touches的表
            Dictionary<int, List<int>> touchesList = new Dictionary<int, List<int>>();
            Geometry superSection = new Geometry(wkbGeometryType.wkbPolygon);

            foreach (var poly in polys)
            {
                List<int> touches = new List<int>();

                foreach (var poly2 in polys)
                {
                    if (poly.Key == poly2.Key) continue;//如果是同个面，就跳过
                    if (touchesList.ContainsKey(poly2.Key))
                    {//检查一下这两个面是不是做过了，如果做过了，那么直接加进去就完了
                        if (touchesList[poly2.Key].Contains(poly.Key))
                        {
                            touches.Add(poly2.Key);
                            continue;
                        }
                    }
                    //特殊情况处理完毕，现在是正常情况
                    Geometry intersectGeom = poly.Value.Intersection(poly2.Value);
                    if (intersectGeom.IsEmpty() == true) continue;//不相交，就下一位
                    wkbGeometryType resulttype = intersectGeom.GetGeometryType();
                    //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
                    if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbGeometryCollection)
                    {
                        if (touches.Contains(poly2.Key) == false)
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
    }
}
