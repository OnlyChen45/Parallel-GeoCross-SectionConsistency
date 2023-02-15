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
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using GeoCommon;
namespace DotSpatialForm.UI
{
    public partial class MultiSourceModelForm : Form
    {
        public string[] pathlist;
        private bool pair;
        private int groupcount;
        public MultiSourceModelForm()
        {
            InitializeComponent();
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

        private void MultiSourceModelForm_Load(object sender, EventArgs e)
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
            foreach (var vk in frm.beziershplist)
            {//加载建模队列
                int p = vk.Key;
                string[] st = vk.Value;
                int index1 = dataGridView2.Rows.Add();
                dataGridView2.Rows[index1].Cells[0].Value = p;
                dataGridView2.Rows[index1].Cells[1].Value = st[0];
                dataGridView2.Rows[index1].Cells[2].Value = st[2];
                int index2 = dataGridView2.Rows.Add();
                dataGridView2.Rows[index2].Cells[0].Value = p;
                dataGridView2.Rows[index2].Cells[1].Value = st[1];
                dataGridView2.Rows[index2].Cells[2].Value = st[3];
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "*.shp|*.SHP";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                bezierbox.Text = openFileDialog.FileName;

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (pair == false) //如果成对标志为false，那么就不增加新的成对编号，
            {
                int index = dataGridView2.Rows.Add();
                dataGridView2.Rows[index].Cells[0].Value = groupcount;
                dataGridView2.Rows[index].Cells[1].Value = bezierbox.Text;
                pair = true;
            }
            else//如果 
            {
                groupcount++;
                int index = dataGridView2.Rows.Add();
                dataGridView2.Rows[index].Cells[0].Value = groupcount;
                dataGridView2.Rows[index].Cells[1].Value = bezierbox.Text;
                pair = false;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                int currRow = dataGridView2.CurrentRow.Index;
                int pairdata = (int)dataGridView2.Rows[currRow].Cells[0].Value;
                dataGridView2.Rows.RemoveAt(currRow);
                groupcount--;
                int rcount = dataGridView2.Rows.Count - 1;
                int indexPairRow = -1;
                for (int i = 0; i < rcount; i++)
                {
                    int t = (int)dataGridView2.Rows[i].Cells[0].Value;
                    if (t == pairdata)
                    {
                        indexPairRow = i;
                        break;
                    }
                }
                if (indexPairRow != -1)
                {
                    dataGridView2.Rows.RemoveAt(indexPairRow);
                }
            }
            catch
            {
            }
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

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "*.shp|*.SHP";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                GeoMapPathBox.Text = openFileDialog.FileName;

            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "*.shp|*.SHP";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DemPathBox.Text = openFileDialog.FileName;

            }
        }
        /// <summary>
        /// 在这里希望能够实现多源融合建模
        /// 目前的方法就是将上表面和弧段进行连接然后融合建模
        /// ok，先去改文章，导师主要看这个文章
        /// 程序可以放放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            List<string[]> namepairs = getDataPair();
            List<string[]> pairs = getDataPair2();
            List<string[]> bezierpairs = getDataPair2Bezier();
            FrmMain frm = (FrmMain)this.Owner;

            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
            string workspacegdb = createGDB(workspaceSS);
            string dempath = DemPathBox.Text;
            string geomappath = GeoMapPathBox.Text;
            DemIO demIO = new DemIO(dempath);
            int count = pairs.Count;
            for (int i = 0; i < count; i++)
            {
                string[] pair = pairs[i];
                string[] namepair = namepairs[i];
                string[] bezierattripair = bezierpairs[i];
                string datapath1 = pair[0];
                string datapath2 = pair[1];//获取两个输入数据，这时候的输入数据实际上是一个二维的数据，还没有进行拓扑对应。当然，做完拓扑对应之后，还需要对其进行一些操作
                                           //下面写一下操作的步骤
                                           //第一步，做拓扑关系的构建与对应
                                           //第二步，做建模前的，把构建好的拓扑关系给转化成三维
                                           //第三步，建模
                string name1 = Path.GetFileNameWithoutExtension(datapath1);
                string name2 = Path.GetFileNameWithoutExtension(datapath2);
                //这一步利用arcpy工具箱，给面增密，提高模型质量。

                SpatialReference spatialReference;
                Dictionary<int,Geometry>sectionpolys1=  shpReader.getGeomListByFile(datapath1, frm.idname,out spatialReference);
                Dictionary<int, Geometry> sectionpolys2 = shpReader.getGeomListByFile(datapath2, frm.idname);

                double xs1, ys1,zs1, xe1, ye1,xs2,ys2,xe2,ye2,zs2;
                attrireader attrireader1 = new attrireader(datapath1);
                attrireader1.getstartendxyz(out xs1, out ys1, out xe1, out ye1, out zs1);
                attrireader1.layerdispose();
                attrireader attrireader2 = new attrireader(datapath2);
                attrireader2.getstartendxyz(out xs2, out ys2, out xe2, out ye2, out zs2);
                attrireader2.layerdispose();
                //这一步应该是添加表层的geometry，这样便于后续的继续添加模型
                Geometry sectionline1 = new Geometry(wkbGeometryType.wkbLineString);
                sectionline1.AddPoint_2D(xs1, ys1);
                sectionline1.AddPoint_2D(xe1, ye1);
                Geometry surface1=  ExtractSectionSurface.ExtractSurfaceModel2(sectionpolys1, xs1, ys1, xe1, ye1,zs1, demIO, sectionline1);
                sectionpolys1.Add(-3, surface1);
                Geometry sectionline2 = new Geometry(wkbGeometryType.wkbLineString);
                sectionline2.AddPoint_2D(xs2, ys2);
                sectionline2.AddPoint_2D(xe2, ye2);
                Geometry surface2 = ExtractSectionSurface.ExtractSurfaceModel2(sectionpolys2, xs2, ys2, xe2, ye2, zs2, demIO, sectionline2);
                sectionpolys2.Add(-3, surface2);
                string patht1 = WrapWorker.getnewFileName(workspaceSS, name1+"_addsurface", "shp");
                string patht2 = WrapWorker.getnewFileName(workspaceSS, name2 + "_addsurface", "shp");
                shpReader.saveDicOfGeoms(patht1, sectionpolys1, frm.idname, spatialReference);
                shpReader.saveDicOfGeoms(patht2, sectionpolys2, frm.idname, spatialReference);
                //把三位参数扔进去，方便后续使用
                attrireader attrireader3 = new attrireader(patht1);
                attrireader3.add3DAttributeField();
                attrireader3.save3DAttribute(xs1, ys1, xe1, ye1, zs1);
                attrireader3.layerdispose();
                attrireader attrireader4 = new attrireader(patht2);
                attrireader4.add3DAttributeField();
                attrireader4.save3DAttribute(xs2, ys2, xe2, ye2, zs2);
                attrireader4.layerdispose();
                datapath1 = WrapWorker.polyNormalizationWithDensity(patht1, workspaceSS, workspacegdb, frm.arcpypath, name1 + "fullbuffer1nor", frm.idname);
                datapath2 = WrapWorker.polyNormalizationWithDensity(patht2, workspaceSS, workspacegdb, frm.arcpypath, name2 + "fullbuffer2nor", frm.idname);
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
                //到这为止就弄到了所有的线，这时候就到了替换建模方法的过程了。

                //到这里首先应该把地质图处理好，第一步就是获取sectionline，然后给地质图切出来
                //做好表面的brep模型
                //切出来之后再把地质图上点和section上点做匹配，把地质图，剖面图，剖面线上三位一体的点都对号入座，顺带分析切出来的地质图，拿到线
                //第三步，弧段分类,弧段分类似乎应该放在建模方法里，那就放在建模方法里吧
                //第四步，按照分类进行建模，具体就是四类建模方法

                CropGeomapBylines cropGeomapBylines = new CropGeomapBylines(sectionline1, sectionline2);
                Dictionary<int,Geometry > geomapFull= shpReader.getGeomListByFile(geomappath, frm.idname) ;
                Dictionary<int,Geometry>geomap= cropGeomapBylines.getGeomapBetweenlines(geomapFull);
                Surface3DFormer surface3DFormer = new Surface3DFormer(demIO, geomap);
                Dictionary<int, BrepModel> surfacebreps = surface3DFormer.makeSurface3D();

                //做表面点匹配
                Dictionary<int, Geometry> points3d1 = shpReader.getGeomListByFile(pathData3d["outpoint1"], "id");
                Dictionary<int, Geometry> points3d2 = shpReader.getGeomListByFile(pathData3d["outpoint2"], "id");
                SurfacePointMatch pointMatch1 = new SurfacePointMatch();
                Dictionary<int, Geometry> surfacepointmatch1 = pointMatch1.PointMatch(points3d1, geomap, sectionline1, demIO);
                SurfacePointMatch pointMatch2 = new SurfacePointMatch();
                Dictionary<int, Geometry> surfacepointmatch2 = pointMatch2.PointMatch(points3d2, geomap, sectionline2, demIO);
                //表面地质图分析
                MatchSurfacePointid matchSurfacePointid = new MatchSurfacePointid(geomap);
                Dictionary<int, Geometry> lines1pointToSurfaceBoundary;
                Dictionary<int, int> surfacep1Tosurfacep2 = matchSurfacePointid.getsurfacepointPair(surfacepointmatch1, surfacepointmatch2, out lines1pointToSurfaceBoundary);
                //第三步在这
                List<int[]> arcmodel_polyids;
                //WrapWorker.makeLOD1Model(pathData3d, ResultFolder.Text, frm.idname, out arcmodel_polyids, "", namepair[0], namepair[1], true);
                //WrapWorker.makeLOD1Model(pathData3d, ResultFolder.Text, frm.idname, out arcmodel_polyids, "", name1, name2, true);
                int transicount = int.Parse(TransitionCountBox.Text);
                WrapWorker.makeLOD1ModelByCurve(pathData3d, ResultFolder.Text, frm.idname, transicount, out arcmodel_polyids, bezierattripair[0], bezierattripair[1], "", name1, name2, true);
            }
            frm.NotificationBox.AppendText("模型创建完成\n\n");
            Process p = new Process();//建模完成后直接打开建模结果文件夹
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = ResultFolder.Text;
            p.Start();
            this.Close();
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
        private List<string[]> getDataPair2Bezier()
        {//把datagridview里所有组都读出来返回成一个列表
            int count = dataGridView2.Rows.Count - 1;
            Dictionary<int, List<string>> pairs = new Dictionary<int, List<string>>();
            for (int i = 0; i < count; i++)
            {
                int p = (int)dataGridView2.Rows[i].Cells[0].Value;
                string path = (string)dataGridView2.Rows[i].Cells[2].Value;
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
    }
}
