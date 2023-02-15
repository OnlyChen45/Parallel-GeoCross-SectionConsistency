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
using OSGeo.GDAL;
using OSGeo.OSR;
using OSGeo.OGR;
namespace DotSpatialForm.UI
{
    public partial class Transe3DForm : Form
    {
        public string[] pathlist;
        private bool pair;
        private int groupcount;

        public Transe3DForm()
        {
            InitializeComponent();
        }

        private void Transe3DForm_Load(object sender, EventArgs e)
        {
            SectionPath.Items.AddRange(pathlist);
            pair = true;
            groupcount = 0;

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
                int rcount = dataGridView1.Rows.Count;
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

        private void button3_Click(object sender, EventArgs e)
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
        private List<string[]> getDataPair()
        {//把datagridview里所有组都读出来返回成一个列表
            int count = dataGridView1.Rows.Count-1;
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
        private void button5_Click(object sender, EventArgs e)
        {
            FrmMain frm = (FrmMain)this.Owner;
            string modest = frm.initmodel;
            string idFieldname = frm.idname;
            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
            List<string[]> pairs = getDataPair();
            string workspacegdb = createGDB(workspaceSS);
            int groupcount = 0;
        
            foreach (string[] pair in pairs)
            {
                string name1 = Path.GetFileNameWithoutExtension(pair[0]);
                string name2 = Path.GetFileNameWithoutExtension(pair[1]);
                string rename1 = name1 + "_DoneSt3D";
                string rename2 = name2 + "_DoneSt3D";
               string resultname1 = name1 + "_Done3D";
               string resultname2 = name2 + "_Done3D";
                switch (modest)
                {
                    //这个就是从第一种数据格式转过来需要的模式，然后把输出的到达了标准化的2D数据的路径信息给更新一下
                    case "Model1":
                        {
                            //string setion1path = worksection1;
                            // string setion2path = worksection2;
                            string worksection1 = pair[0];
                            string worksection2 = pair[1];
                            string setion1path = WrapWorker.polyNormalization(worksection1, workspaceSS, workspacegdb,frm. arcpypath,name1+ "polynor11", frm.idname);
                            string setion2path = WrapWorker.polyNormalization(worksection2, workspaceSS, workspacegdb,frm. arcpypath, name2+"polynor22", frm.idname);
                            /*                       //这个是通过一个程序输入的数据
                             *                       SectionParameterPadding form2 = new SectionParameterPadding();
                                                    form2.nowshpPath = sectionname1;
                                                    form2.ShowDialog(this);*/
                            string section13Dparpath = get3Dpar(worksection1);
                            string section23Dparpath = get3Dpar(worksection2);
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
                            string section13D1 = WrapWorker.mode12DToDefault3D(setion1path, workspaceSS, frm.idname, par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"],name1);
                            string section2D1 = WrapWorker.Default3DToDefault2D(section13D1, workspaceSS, frm.idname, name1+"section2D1Poly");
                            string section2DNor1 = WrapWorker.polyNormalization(section2D1, workspaceSS, workspacegdb, frm.arcpypath, rename1, frm.idname);
/*                            string orishp1 = this.shpOripath1;//把原始数据也给标准化了
                            orishp1 = WrapWorker.mode12DToDefault3D(orishp1, workspaceSS, frm.idname, par1dic["startX"], par1dic["startY"], par1dic["startZ"], par1dic["endX"], par1dic["endY"], par1dic["firstX"], par1dic["firstY"], "section3DOri1");
                            orishp1 = WrapWorker.Default3DToDefault2D(orishp1, workspaceSS, frm.idname, "section2DOri1");
                            orishp1 = WrapWorker.polyNormalization(orishp1, workspaceSS, workspacegdb, frm.arcpypath, "OriShp1", frm.idname);*/
/*                            this.shpOripath1 = orishp1;
                            this.inputshppath1 = section2DNor1;*/
                            /*                        SectionParameterPadding form3 = new SectionParameterPadding();
                                                    //form3.nowshpPath = setion2path;
                                                    form3.nowshpPath = sectionname2;
                                                    form3.ShowDialog(this);*/
                            string section13D2 = WrapWorker.mode12DToDefault3D(setion2path, workspaceSS, frm.idname, par2dic["startX"], par2dic["startY"], par2dic["startZ"], par2dic["endX"], par2dic["endY"], par2dic["firstX"], par2dic["firstY"],name2);
                            string section2D2 = WrapWorker.Default3DToDefault2D(section13D2, workspaceSS, frm.idname,name2+ "section2D2Poly");
                            string section2DNor2 = WrapWorker.polyNormalization(section2D2, workspaceSS, workspacegdb, frm.arcpypath, rename2, frm.idname);
                            /*                            string orishp2 = this.shpOripath2;//把原始数据也给标准化了
                                                        orishp2 = WrapWorker.mode12DToDefault3D(orishp2, workspaceSS, frm.idname, par2dic["startX"], par2dic["startY"], par2dic["startZ"], par2dic["endX"], par2dic["endY"], par2dic["firstX"], par2dic["firstY"], "section3DOri2");
                                                        orishp2 = WrapWorker.Default3DToDefault2D(orishp2, workspaceSS, frm.idname, "section2DOri2");
                                                        orishp2 = WrapWorker.polyNormalization(orishp2, workspaceSS, workspacegdb, frm.arcpypath, "OriShp2", frm.idname);*/
                            /*                            this.shpOripath2 = orishp2;
                                                        this.inputshppath2 = section2DNor2;*/
                            frm.addtoMap.Add(section2DNor1);
                            frm.addtoMap.Add(section2DNor2);
                            if (AddToQueueCheckBox.Checked) {
                                groupcount++;
                                string filenamet1 = Path.GetFileNameWithoutExtension(section2DNor1);
                                string filenamet2 = Path.GetFileNameWithoutExtension(section2DNor2);
                                string[] namet1 = filenamet1.Split('_');
                                string[] namet2 = filenamet2.Split('_');
                                string[] pairsting = { namet1[0],namet2[0],section2DNor1, section2DNor2 };
                                frm.modellist.Add(groupcount, pairsting);
                            }
                            if (AddToQueueCheckBox.Checked&&BeziershpCheck.Checked) {//对于贝塞尔曲线，给它生成一个拓扑的点图层，然后后续的话用这个点图层进行匹配获取它的这个贝塞尔参数，这样就可以输入贝塞尔参数了
                                string bezierdir = workspaceSS + "\\BezierShp";
                                if (Directory.Exists(bezierdir)==false) {
                                    Directory.CreateDirectory(bezierdir);
                                }
                                string beziershp1 = bezierdir + "\\" + name1 + "_bezierattri.shp";
                                string beziershp2 = bezierdir + "\\" + name2 + "_bezierattri.shp";
                                saveBezierAtttri(section2DNor1, beziershp1, idFieldname);
                                saveBezierAtttri(section2DNor2, beziershp2, idFieldname);
                                string filenamet1 = Path.GetFileNameWithoutExtension(section2DNor1);
                                string filenamet2 = Path.GetFileNameWithoutExtension(section2DNor2);
                                string[] namet1 = filenamet1.Split('_');
                                string[] namet2 = filenamet2.Split('_');
                                string[] pairsting = { namet1[0], namet2[0], beziershp1, beziershp2 };
                                frm.beziershplist.Add(groupcount, pairsting);
                            }
                            break;

                        }
                    case "Default":
                        {
                            //这个模式就是说，数据是一个符合比较标准的数据的状况，
                            string worksection1 = pair[0];
                            string worksection2 = pair[1];
                            string section2DNor1 = WrapWorker.polyNormalization(worksection1, workspaceSS, workspacegdb, frm.arcpypath, "polynor1", frm.idname);
                            string section2DNor2 = WrapWorker.polyNormalization(worksection2, workspaceSS, workspacegdb, frm.arcpypath, "polynor2", frm.idname);
                            break;
                        }

                }
                if (AddToQueueCheckBox.Checked == true) { 
                    
                }
            }
            frm.NotificationBox.AppendText("三维参数加载完成\n\n");
            this.Close();
            string get3Dpar(string shppath)
            {
                string[] pathsplit = shppath.Split('.');
                string resu = pathsplit[0] + ".txt";
                return resu;
            }
            void saveBezierAtttri(string polypath,string outputpath,string idFieldnamet) {
                PolygonIO polygonIO1 = new PolygonIO(polypath, idFieldnamet);
                Dictionary<int, Geometry> polys1;
                List<int> idlist1;
                polygonIO1.getGeomAndId(out polys1, out idlist1);
                TopologyOfPoly topologyOfPoly1 = new TopologyOfPoly(idlist1, polys1);
                topologyOfPoly1.makeTopology();
                
                FieldDefn fieldDefnx = new FieldDefn("dx", FieldType.OFTReal);
                FieldDefn fieldDefny = new FieldDefn("dy", FieldType.OFTReal);
                FieldDefn fieldDefnz = new FieldDefn("dz", FieldType.OFTReal);
                FieldDefn[] fields = { fieldDefnx, fieldDefny, fieldDefnz };
                topologyOfPoly1.savePointsInShp(outputpath, "points", polygonIO1.getSpatialRef(),fields.ToList<FieldDefn>());
            }
        }
    }
}
