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
    public partial class EageCrushForm : Form
    {
        public string[] pathlist;
        public EageCrushForm()
        {
            InitializeComponent();
        }

        private void EageCrushForm_Load(object sender, EventArgs e)
        {
            SectionPath.Items.AddRange(pathlist);
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
            int count = dataGridView1.Rows.Count - 1;
            FrmMain frmMain = (FrmMain)this.Owner;
            string workspacegdb = createGDB(ResultFolder.Text);
            for (int i = 0; i < count; i++) {
                string path = (string)dataGridView1.Rows[i].Cells[0].Value;
                string par1ori = get3Dpar(path);
                string name = Path.GetFileNameWithoutExtension(path);
                name = name + "_Nor" + frmMain.crushdonecount.ToString();
                string datapath1 = WrapWorker.polyNormalization(path, ResultFolder.Text, workspacegdb, frmMain.arcpypath, name, frmMain.idname);
                string par1 = get3Dpar(datapath1);
                FileStream fileStream1 = new FileStream(par1ori, FileMode.Open);
                FileStream fileStream2 = new FileStream(par1, FileMode.Create);
                fileStream1.CopyTo(fileStream2);
                fileStream1.Close();
                fileStream2.Close();
                frmMain.addtoMap.Add(name);
            }
            frmMain.crushdonecount++;
            this.Close();
            string get3Dpar(string shppath)
            {
                string[] pathsplit = shppath.Split('.');
                string resu = pathsplit[0] + ".txt";
                return resu;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.Rows.Add();
            dataGridView1.Rows[index].Cells[0].Value = SectionPath.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int currRow = dataGridView1.CurrentRow.Index;
            dataGridView1.Rows.RemoveAt(currRow);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
