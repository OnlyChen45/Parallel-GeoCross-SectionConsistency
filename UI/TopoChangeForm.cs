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
    public partial class TopoChangeForm : Form
    {
        public string[] pathlist;
        private bool pair;
        private int groupcount;
        public TopoChangeForm()
        {
            InitializeComponent();
        }

        private void TopoChangeForm_Load(object sender, EventArgs e)
        {
            SectionPath.Items.AddRange(pathlist);
            pair = true;
            groupcount = 0 ;
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
        {//这个按钮是要一次性删除成对数据
            try 
            {
                int currRow = dataGridView1.CurrentRow.Index;
                int pairdata =(int) dataGridView1.Rows[currRow].Cells[0].Value;
                dataGridView1.Rows.RemoveAt(currRow);
                int rcount= dataGridView1.Rows.Count;
                int indexPairRow = -1;
                for (int i = 1; i <= rcount; i++)
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
            catch { 
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
        private List<string[]> getDataPair()
        {//把datagridview里所有组都读出来返回成一个列表
            int count = dataGridView1.Rows.Count-1;
            Dictionary<int, List<string>> pairs = new Dictionary<int, List<string>>();
            for (int i = 0; i < count; i++)
            {
                var ttt = dataGridView1.Rows[i].Cells[0];
                int p = (int)ttt.Value;
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
        private void button5_Click(object sender, EventArgs e)
        {
            List<string[]> pairs = getDataPair();
            
            FrmMain frm = (FrmMain)this.Owner;
            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
           // string gdb=frm.
            foreach (string[] pair in pairs)
            {
                string datapath1 = pair[0];
                string datapath2 = pair[1];
                string par1ori = get3Dpar(datapath1);
                string par2ori = get3Dpar(datapath2);
                string dir = Path.GetDirectoryName(pair[0]);
                string name1 = Path.GetFileNameWithoutExtension(pair[0]);
                string name2 = Path.GetFileNameWithoutExtension(pair[1]);
                name1 = name1 + "_DoneTopo";
                name2 = name2 + "_DoneTopo";
                string path1 = pair[0];
                string path2 = pair[1];
                string result1, result2;
                //WrapWorker.polyNormalization(path1,workspaceSS,)
                WrapWorker.dealTopoChange(path1, path2, workspaceSS, out result1, out result2,frm. idname,name1,name2, 0.1);
                //result1 = WrapWorker.polyNormalization(result1, workspaceFolder.Text, this.workspacegdb, arcpyPath1.Text, "dealTopo1nor", IdNameBox.Text);
                //result2 = WrapWorker.polyNormalization(result2, workspaceFolder.Text, this.workspacegdb, arcpyPath1.Text, "dealTopo2nor", IdNameBox.Text);
                frm.NotificationBox.AppendText(result1 + "\n");
                frm. NotificationBox.AppendText(result2+"\n\n");
                string par1 = get3Dpar(result1);
                string par2 = get3Dpar(result2);
                if (File.Exists(par1ori)&&File.Exists(par2ori))//判断一下参数是否存在，不存在的时候就跳过省的报错

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
                frm.addtoMap.Add(result1);
                frm.addtoMap.Add(result2);

            }
            frm.NotificationBox.AppendText("地层相对位置不对应情况已处理完成\n\n");
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
