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
namespace DotSpatialForm.UI
{
    public partial class EnvConfigForm : Form
    {
        public EnvConfigForm()
        {
            InitializeComponent();
        }

        private void SetArcpy_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "*.exe|*.EXE";
            openFileDialog.Title = "选择ArcgisPython程序路径";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                arcpyPath1.Text = openFileDialog.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FrmMain frm = (FrmMain)this.Owner;
            FileStream fileStream = new FileStream(frm.config, FileMode.Create);
            StreamWriter writer = new StreamWriter(fileStream);
            writer.WriteLine(arcpyPath1.Text);
            writer.WriteLine(IdNameBox.Text);
            writer.WriteLine(modeSt.Text);
            writer.Close();
            fileStream.Close();
            frm.arcpypath = arcpyPath1.Text;
            frm.idname = IdNameBox.Text;
            frm.initmodel = modeSt.Text;
        }

        private void EnvConfigForm_Load(object sender, EventArgs e)
        {
            FrmMain frm = (FrmMain)this.Owner;
            string config = frm.config;
            if (Directory.Exists(config))
            {
                FileStream fileStream = new FileStream(config, FileMode.Open);
                StreamReader reader = new StreamReader(fileStream);
                arcpyPath1.Text = reader.ReadLine();
                IdNameBox.Text = reader.ReadLine();
                modeSt.Text = reader.ReadLine();
                reader.Close();
                fileStream.Close();
            }
        }
    }
}
