using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThreeDModelSystemForSection;
using System.IO;
using System.Diagnostics;
using GeoCommon;
using SolidModel;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace DotSpatialForm.UI
{
    public partial class InterpolationForm : Form
    {
        public string[] pathlist;
        public InterpolationForm()
        {
            InitializeComponent();
        }

        private void InterpolationForm_Load(object sender, EventArgs e)
        {

            FrmMain frm = (FrmMain)this.Owner;
            foreach (var vk in frm.modellist)
            {//加载建模队列
                int p = vk.Key;
                string[] st = vk.Value;
                int index1 = dataGridView1.Rows.Add();
                dataGridView1.Rows[index1].Cells[0].Value = p;
                dataGridView1.Rows[index1].Cells[1].Value = st[0];
                dataGridView1.Rows[index1].Cells[2].Value = st[2];
                int index2 = dataGridView1.Rows.Add();
                dataGridView1.Rows[index2].Cells[0].Value = p;
                dataGridView1.Rows[index2].Cells[1].Value = st[1];
                dataGridView1.Rows[index2].Cells[2].Value = st[3];
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<string[]> namepairs = getDataPair();
            List<string[]> pairs = getDataPair2();
            FrmMain frm = (FrmMain)this.Owner;

            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
            string workspacegdb = createGDB(workspaceSS);
            int count = pairs.Count;
            string outputfolder = WrapWorker.createDirectory(ResultFolder.Text, "intersection");
            string outputspace = outputfolder + '\\';
            string outputs1Tos2 = outputspace;
            for (int i = 0; i < count; i++)
            {
                string[] pair = pairs[i];
                string[] namepair = namepairs[i];
                string datapath1 = pair[0];
                string datapath2 = pair[1];//获取两个输入数据，这时候的输入数据实际上是一个二维的数据，还没有进行拓扑对应。当然，做完拓扑对应之后，还需要对其进行一些操作
                                           //下面写一下操作的步骤
                                           //第一步，做拓扑关系的构建与对应
                                           //第二步，做建模前的，把构建好的拓扑关系给转化成三维
                                           //第三步，建模
                string name1 = Path.GetFileNameWithoutExtension(datapath1);
                string name2 = Path.GetFileNameWithoutExtension(datapath2);
                //这一步利用arcpy工具箱，给面增密，提高模型质量。
                datapath1 = WrapWorker.polyNormalizationWithDensity(datapath1, workspaceSS, workspacegdb, frm.arcpypath, name1 + "fullbuffer1nor", frm.idname);
                datapath2 = WrapWorker.polyNormalizationWithDensity(datapath2, workspaceSS, workspacegdb, frm.arcpypath, name2 + "fullbuffer2nor", frm.idname);
                //datapath1 = WrapWorker.polyNormalization(datapath1, workspaceSS, workspacegdb, frm.arcpypath, name1 + "fullbuffer1nor", frm.idname);
                //datapath2 = WrapWorker.polyNormalization(datapath2, workspaceSS, workspacegdb, frm.arcpypath, name2 + "fullbuffer2nor", frm.idname);
                //第一步在这
                Dictionary<string, string> dataPathCollection;
                bool signal = WrapWorker.makeTopotForSections(datapath1, datapath2, out dataPathCollection, workspaceSS, frm.idname);

                if (signal == false)
                {
                    string mess = "由于创建的尖灭地层对应位置面过于小或其他拓扑问题，需要手动进行修改\n\r请修改以下两个文件以保证符合弧段一一对应的标准\n\r" + datapath1 + "\n\r" + datapath2;
                    MessageBox.Show(mess);
                    frm.NotificationBox.AppendText("建模出错，请检查数据\n\n");
                    return;
                }
                //第二步在这
                string workspacett = workspaceSS + "\\space" + name1;
                if (Directory.Exists(workspacett) == false)
                {
                    Directory.CreateDirectory(workspacett);
                }
                Dictionary<string, string> pathData3d;

                WrapWorker.transdataTo3D(dataPathCollection, out pathData3d, workspacett);

                //第三步在这
                List<int[]> arcmodel_polyids;
                //WrapWorker.makeLOD1Model(pathData3d, ResultFolder.Text, frm.idname, out arcmodel_polyids, "", namepair[0], namepair[1], true);
                List<ModelWithArc> modelWithArcs= WrapWorker.makeLOD1Model(pathData3d, ResultFolder.Text, frm.idname, out arcmodel_polyids, "", name1, name2, true);
                double rate = double.Parse(InterpolationStep.Text);//取出指定的比例
                                                                   //string mappath = geomappath.Text;
                int timest = 1; 
                double nowrate = timest*rate;

                while ((Math.Abs(nowrate - 1) > 0.001)&&(nowrate<1))
                {
                    string spatialimportPath = datapath1;
                    string idFieldName = frm.idname;
                    int id1 = 1;
                    int id2 = 2;
                    List<Eage> eagesss = new List<Eage>();
                    int brepModelcount = modelWithArcs.Count();
                    for (int k = 0; k < brepModelcount; k++)
                    {
                        ModelWithArc modelWithArc = modelWithArcs[k];
                        BrepModel brep = modelWithArc.getModel();
                        ContourIntePHelp intePHelp = new ContourIntePHelp(modelWithArc.arc1.eage, modelWithArc.arc2.eage, brep.mesh);
                        //Eage inteEage = intePHelp.ContourInterPolateByRate(rate);
                        Eage inteEage = intePHelp.ContourInterPolateByRateStable(nowrate);
                        eagesss.Add(inteEage);
                    }
                    //WrapWorker. savelinesByDic(eagesss, outputspace, id1, id2, key,sectionlinePath, idFieldName, arcmodel_polyids);

                  string interlineresult=  WrapWorker.savelinesByDic(eagesss, outputs1Tos2, name1, name2+'_'+timest.ToString(), spatialimportPath, idFieldName, arcmodel_polyids);
                    //WrapWorker.savelinesByDic(eagesss, outputs2Tos1, id1, id2, key, sectionlinePath, idFieldName, arcmodel_polyids);
                    timest++;
                    nowrate = timest * rate;
                    if (TransTo2dOrNor.Checked) 
                    {
                        string name1t = name1.Split('_')[0];
                        string name2t = name2.Split('_')[0];
                        trans3DlineTo3Dpolygon(interlineresult, outputfolder, frm.idname, frm, workspacegdb, name1t, name2t);
                    }
                }
            }
            frm.NotificationBox.AppendText("模型创建完成\n\n");
            Process p = new Process();//建模完成后直接打开建模结果文件夹
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = outputfolder;
            p.Start();
            frm.modellist.Clear();
            this.Close();
        }
        private void trans3DlineTo3Dpolygon(string interlineresultpath,string resultfolder,string idFieldName,FrmMain frm,string workspacegdb,string name1,string name2) {
            string inputshp = interlineresultpath;
            string shpname = Path.GetFileName(inputshp);
            string outputlinestring = resultfolder + '\\' + "line" + shpname;
            string polygongout = resultfolder + '\\' +"poly"+ shpname;
            ConvertArcs3DTo2DWholeShp convertArcs3DTo2D = new ConvertArcs3DTo2DWholeShp();
            double[] transformAttribute;
            Dictionary<string ,double >model1attribute=  convertArcs3DTo2D.OpenArcsShpAndConvertToModel1(inputshp, outputlinestring, out transformAttribute);//这里给三位参数存到了线同位的txt里，所以应当，给面也来一份。
            ConvertArcsToPolygon convertArcsToPolygon = new ConvertArcsToPolygon();
            Dictionary<int, Geometry> polys = convertArcsToPolygon.ReadAndConvert(outputlinestring, "polygon1", "polygon2");
            convertArcsToPolygon.savePolys(polygongout, idFieldName, polys, transformAttribute);
            saveAttriTotxt(model1attribute, polygongout);
            //到此为止，就求出了中间剖面和它的三位参数
            //但是存在一个很大的问题，就是仍然可能是存在碎屑。
            //所以呢，就要用那个类似于面转线转面的方法去把碎屑拿到，拿到之后给它合并到周围较大的面里，然后再标号再输出。
            Dictionary<int, Geometry> geoms = shpReader.getGeomListByFile(polygongout, idFieldName);
            //string norpath = WrapWorker.polyNormalSimple(polygongout, workspaceSS, workspacegdb, frm.arcpypath, Path.GetFileNameWithoutExtension(polygongout) + "_nor");
            string norpath = WrapWorker.polyNormalization(polygongout, resultfolder, workspacegdb, frm.arcpypath, Path.GetFileNameWithoutExtension(polygongout) + "_nor", frm.idname);
            saveAttriTotxt(model1attribute, norpath);
            //string txt3dline = get3Dpar(outputlinestring);
            SpatialReference spatialReference;
            Dictionary<int, Geometry> geomsnor = shpReader.getGeomListByFileWithFID(norpath, out spatialReference);
            int origeomcount = geoms.Keys.Count;
            List<Geometry> geomsright = mergeMiniGeomToOthers(geomsnor.Values.ToList<Geometry>(), origeomcount);//获取了没有细碎问题的面。
            Dictionary<int, Geometry> finalresult = new Dictionary<int, Geometry>();
            for (int i = 0; i < geomsright.Count; i++)
            {
                Geometry geomt = geomsright[i];
                int maxid = -1;
                double maxinterarea = double.MinValue;
                foreach (var vk in geoms)
                {
                    Geometry inter = vk.Value.Intersection(geomt);
                    double area = inter.Area();
                    if (area > maxinterarea)
                    {
                        maxid = vk.Key;
                        maxinterarea = area;
                    }
                }
                finalresult.Add(maxid, geomt);
            }
            string resultfinalname = WrapWorker.getnewFileName(resultfolder, name1 + '_' + name2 + "_complete", "shp");
            shpReader.saveDicOfGeoms(resultfinalname, finalresult, frm.idname, spatialReference);
        }
        List<Geometry> mergeMiniGeomToOthers(List<Geometry> geoms, int bigcount)
        {
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
            for (int i = 0; i < geomsworking.Count; i++)
            {
                double area = geomsworking[i].Area();
                areas.Add(area);
            }
            if (minicount <= 0) return geoms;
            for (int i = 0; i < minicount; i++)
            {
                int miniindex = -1;
                double miniarea = double.MaxValue;
                for (int j = 0; j < geomsworking.Count; j++)
                {
                    if (areas[j] < miniarea)
                    {
                        miniindex = j;
                        miniarea = areas[j];
                    }
                }
                Geometry geomtemp = geomsworking[miniindex];
                geomsworking.RemoveAt(miniindex);
                areas.RemoveAt(miniindex);
                int mergeid = -2;
                double lengthmax = double.MinValue;
                for (int j = 0; j < geomsworking.Count; j++)
                {
                    Geometry get = geomsworking[j];
                    bool touches = touchCheck(geomtemp, get);
                    if (touches)
                    {
                        double lengtht = getLineLength(geomtemp.Intersection(get));
                        if (lengtht > lengthmax)
                        {
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
        bool touchCheck(Geometry geom1, Geometry geom2)
        {
            Geometry geominter = geom1.Intersection(geom2);
            if (geominter.IsEmpty() == true) return false;
            wkbGeometryType resulttype = geominter.GetGeometryType();
            //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
            if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbPolygon || resulttype == wkbGeometryType.wkbMultiPolygon || resulttype == wkbGeometryType.wkbGeometryCollection)
            {
                if (resulttype == wkbGeometryType.wkbGeometryCollection)
                {
                    int count = geominter.GetGeometryCount();
                    for (int j = 0; j < count; j++)
                    {
                        wkbGeometryType ttype = geominter.GetGeometryRef(j).GetGeometryType();
                        if (ttype == wkbGeometryType.wkbLineString || ttype == wkbGeometryType.wkbMultiLineString || ttype == wkbGeometryType.wkbPolygon || ttype == wkbGeometryType.wkbMultiPolygon)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        private void saveAttriTotxt(Dictionary<string, double> attri, string outshppath)
        {
            string txtpath = get3Dpar(outshppath);
            FileStream file = new FileStream(txtpath, FileMode.Create);
            StreamWriter writer = new StreamWriter(file);
            foreach (var vk in attri)
            {
                writer.WriteLine(vk.Key + ':' + vk.Value.ToString());
            }
            writer.Close();
            file.Close();
            string get3Dpar(string shppath)
            {
                string[] pathsplit = shppath.Split('.');
                string resu = pathsplit[0] + ".txt";
                return resu;
            }
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
        private List<string[]> getDataPair2()
        {//把datagridview里所有组都读出来返回成一个列表
            int count = dataGridView1.Rows.Count - 1;
            Dictionary<int, List<string>> pairs = new Dictionary<int, List<string>>();
            for (int i = 0; i < count; i++)
            {
                int p = (int)dataGridView1.Rows[i].Cells[0].Value;
                string path = (string)dataGridView1.Rows[i].Cells[2].Value;
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
        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "结果文件夹";  //定义在对话框上显示的文本

            if (folder.ShowDialog() == DialogResult.OK)
            {
                ResultFolder.Text = folder.SelectedPath;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int currRow = dataGridView1.CurrentRow.Index;
                int pairdata = (int)dataGridView1.Rows[currRow].Cells[0].Value;
                dataGridView1.Rows.RemoveAt(currRow);
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
    }
}
