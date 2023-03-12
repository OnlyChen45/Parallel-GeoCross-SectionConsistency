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
    public partial class StrataBranchesForm : Form
    {
        public string[] pathlist;
        private bool pair;
        private int groupcount;

        public StrataBranchesForm()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Initialize the auxiliary variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StrataBranchesForm_Load(object sender, EventArgs e)
        {
            SectionPath.Items.AddRange(pathlist);
            pair = true;
            groupcount = 0;

        }
        /// <summary>
        /// Enter the shp data file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "*.shp|*.SHP";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                SectionPath.Text = openFileDialog.FileName;

            }

        }
        /// <summary>
        /// Adds a data entry to the data group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            if (pair == false) //If the pair flag is false, then no new pair number is added, 
            {
                int index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells[0].Value = groupcount;
                dataGridView1.Rows[index].Cells[1].Value = SectionPath.Text;
                pair = true;
            }
            else
            {
                groupcount++;
                int index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells[0].Value = groupcount;
                dataGridView1.Rows[index].Cells[1].Value = SectionPath.Text;
                pair = false;
            }

        }
        /// <summary>
        /// Remove data from a data table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int currRow = dataGridView1.CurrentRow.Index;
                int pairdata = (int)dataGridView1.Rows[currRow].Cells[0].Value;
                dataGridView1.Rows.RemoveAt(currRow);
                int rcount = dataGridView1.Rows.Count;
                groupcount--;
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
            folder.Description = "Result Folder";  //Defines the text to display on the dialog box

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
        {//Read out all the groups in the datagridview and return them to a list
            int count = dataGridView1.Rows.Count-1;
            Dictionary<int, List<string>> pairs = new Dictionary<int, List<string>>();
            for (int i = 0; i < count; i++) {
                int p = (int)dataGridView1.Rows[i].Cells[0].Value;
                string path = (string)dataGridView1.Rows[i].Cells[1].Value;
                if (pairs.ContainsKey(p))
                {
                    pairs[p].Add(path);
                }
                else {
                    List<string> st = new List<string>();
                    st.Add(path);
                    pairs.Add(p, st);
                }
            }
            List<string[]> result = new List<string[]>();
            foreach (var vk in pairs) {
                result.Add(vk.Value.ToArray());
            }
            return result;
        }
        private string createGDB(string folder) {
            string workspacegdb="";
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
        /// <summary>
        /// Execute the algorithm logic for the corresponding section branch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            List<string[]> pairs = getDataPair();

            FrmMain frm = (FrmMain)this.Owner;
            string workspaceSS = ResultFolder.Text;//dir + "\\temp";
            string gdbpath = createGDB(workspaceSS);
            //FrmMain frmt = (FrmMain)this.Owner;
            foreach (string[] pair in pairs)//Iterate over the data in the data box
            {
                //Get a set of data
                string datapath1 = pair[0];
                string datapath2 = pair[1];
                string par1ori = get3Dpar(datapath1);
                string par2ori = get3Dpar(datapath2);
                string dir = Path.GetDirectoryName(pair[0]);
                string name1 = Path.GetFileNameWithoutExtension(pair[0]);
                string name2 = Path.GetFileNameWithoutExtension(pair[1]);
                //Initialize the workspace
                if (!Directory.Exists(workspaceSS))
                {
                    Directory.CreateDirectory(workspaceSS);
                }

                string filename1 = name1 + "_DoneBranch", filename2 = name2 + "_DoneBranch";
                string fenzhiresultpath1, fenzhiresultpath2;
                //The main logic of branching algorithms
                WrapWorker.dealBranching(datapath1, datapath2, workspaceSS, filename1, filename2, out fenzhiresultpath1, out fenzhiresultpath2, frm.arcpypath, gdbpath, frm.idname);

                //Copy the 3D parameter file that matches the shp file
                frm.NotificationBox.AppendText(fenzhiresultpath1 + "\n");
                frm.NotificationBox.AppendText(fenzhiresultpath2 + "\n\n");
                string par1 = get3Dpar(fenzhiresultpath1);
                string par2 = get3Dpar(fenzhiresultpath2);
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
                frm.addtoMap.Add(fenzhiresultpath1);
                frm.addtoMap.Add(fenzhiresultpath2);
            }
            frm.NotificationBox.AppendText("Done\n\n");
            this.Close();


            string get3Dpar(string shppath)
            {
                string[] pathsplit = shppath.Split('.');
                string resu = pathsplit[0] + ".txt";
                return resu;
            }
        }
    }
}
