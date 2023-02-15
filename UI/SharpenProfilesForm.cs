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
namespace DotSpatialForm.UI
{
    public partial class SharpenProfilesForm : Form
    {
        public string[] pathlist;
        private bool pair;
        private int groupcount;

        public SharpenProfilesForm()
        {
            InitializeComponent();
        }

        private void SharpenProfilesForm_Load(object sender, EventArgs e)
        {
            SectionPath.Items.AddRange(pathlist);
            pair = true;
            groupcount = 0;
            usecount = new Dictionary<string, int>();

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
                int rcount = dataGridView1.Rows.Count-1;
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
        Dictionary<string, int> usecount;//记录使用过的文件名，防止冲突；
        private int addTocount(string path) 
        {//返回用过的次数
            if (usecount.ContainsKey(path) == false)
            {
                usecount.Add(path, 1);
                return 1;
            }
            else {
                usecount[path]++;
                return usecount[path]++;
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            List<string[]> pairs = getDataPair();

            FrmMain frm = (FrmMain)this.Owner;
            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
           
            string workspacegdb = createGDB(workspaceSS);
            int pairordinal = 0;
            foreach (string[] pair in pairs)
            {
                string path1 = pair[0];
                string path2 = pair[1];
                string par1ori = get3Dpar(path1);
                string par2ori = get3Dpar(path2);

                int path1count= addTocount(path1);
                int path2count= addTocount(path2);
                string workspace = workspaceSS+"\\"+pairordinal.ToString();
                pairordinal++;
                if (Directory.Exists(workspace) == false) {
                    Directory.CreateDirectory(workspace);
                }
                path1 = WrapWorker.polyNormalization(path1, workspace, workspacegdb, frm.arcpypath, Path.GetFileNameWithoutExtension(path1) + "_" + path1count.ToString() + "Nor", frm.idname);
                path2 = WrapWorker.polyNormalization(path2, workspace, workspacegdb, frm.arcpypath, Path.GetFileNameWithoutExtension(path2) + "_" + path2count.ToString() + "Nor", frm.idname);
                string name1 = Path.GetFileNameWithoutExtension(path1);
                string name2 = Path.GetFileNameWithoutExtension(path2);
                name1 = name1 + "_DoneSharpen";
                name2 = name2 + "_DoneSharpen";
                /*                this.shpOripath1 = path1;
                                this.shpOripath2 = path2;*/

                string mergeresult1, mergeresult2, sectionpairtxt1, sectionpairtxt2, sharpenids1txt, sharpenids2txt;
                WrapWorker.mergeSectionOri(path1, path2, workspace, frm.idname, out mergeresult1, out mergeresult2, out sectionpairtxt1, out sectionpairtxt2, out sharpenids1txt, out sharpenids2txt);
                mergeresult1 = WrapWorker.polyNormalization(mergeresult1, workspace, workspacegdb, frm.arcpypath, name1+"section1normerge", frm.idname);
                mergeresult2 = WrapWorker.polyNormalization(mergeresult2, workspace, workspacegdb, frm.arcpypath, name2+"section2normerge", frm.idname);
                /*                this.section1pair = sectionpairtxt1;
                                this.section2pair = sectionpairtxt2;
                                this.sharpenorder1 = sharpenids1txt;
                                this.sharpenorder2 = sharpenids2txt;*/
                frm.NotificationBox.AppendText("复杂尖灭情况已经排除\n\n");
                string section1path = mergeresult1;
                string section2path = mergeresult2;
                string resultpath1;
                string resultpath2;
                //double[] startend1 = WrapWorker.getstartendxy(section1path);
                //double[] startend2 = WrapWorker.getstartendxy(section2path);
                // this.sectionlinepath = savePairLinesToshp.saveSectionLines(startend1, startend2, workspaceSS, section1path);
                WrapWorker.createBuffersboth(section1path, section2path, out resultpath1, out resultpath2, frm.idname, workspaceSS, workspacegdb, frm.arcpypath,name1,name2);


                frm.NotificationBox.AppendText(resultpath1 + "\n");
                frm.NotificationBox.AppendText(resultpath2 + "\n\n");

                string par1 = get3Dpar(resultpath1);
                string par2 = get3Dpar(resultpath2);
                if (File.Exists(par1ori) && File.Exists(par2ori))//判断一下参数是否存在，不存在的时候就跳过省的报错

                {
                    FileStream fileStream1 = new FileStream(par1ori, FileMode.Open);
                    FileStream fileStream11 = new FileStream(par1, FileMode.Create);
                    fileStream1.CopyTo(fileStream11);
                    FileStream fileStream2 = new FileStream(par2ori, FileMode.Open);
                    FileStream fileStream22 = new FileStream(par2, FileMode.Create);
                    fileStream2.CopyTo(fileStream22);
                    fileStream1.Close();
                    fileStream2.Close();
                    fileStream11.Close();
                    fileStream22.Close();
                }
                frm.addtoMap.Add(resultpath1);
                frm.addtoMap.Add(resultpath2);
            }
            frm.NotificationBox.AppendText("尖灭地层处理完成\n\n");
            this.Close();
        }
        string get3Dpar(string shppath)
        {
            string[] pathsplit = shppath.Split('.');
            string resu = pathsplit[0] + ".txt";
            return resu;
        }

    }
}

