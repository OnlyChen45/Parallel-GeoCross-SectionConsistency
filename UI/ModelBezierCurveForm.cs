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
namespace DotSpatialForm.UI
{
    public partial class ModelBezierCurveForm : Form
    {
        public string[] pathlist;
        public ModelBezierCurveForm()
        {
            InitializeComponent();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void ModelBezierCurveForm_Load(object sender, EventArgs e)
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

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
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
        private bool pair;
        private int groupcount;

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

        private void button4_Click(object sender, EventArgs e)
        {
            List<string[]> namepairs = getDataPair();
            List<string[]> pairs = getDataPair2();
            List<string[]> bezierpairs = getDataPair2Bezier();
            FrmMain frm = (FrmMain)this.Owner;

            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
            string workspacegdb = createGDB(workspaceSS);
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
                //WrapWorker.makeLOD1Model(pathData3d, ResultFolder.Text, frm.idname, out arcmodel_polyids, "", name1, name2, true);
                int transicount =int.Parse( TransitionCountBox.Text);
                WrapWorker.makeLOD1ModelByCurve(pathData3d, ResultFolder.Text, frm.idname,transicount,out arcmodel_polyids, bezierattripair[0], bezierattripair[1], "", name1, name2, true);
            }
            frm.NotificationBox.AppendText("模型创建完成\n\n");
            Process p = new Process();//建模完成后直接打开建模结果文件夹
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = ResultFolder.Text;
            p.Start();
            frm.modellist.Clear();
            frm.beziershplist.Clear();
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

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
