
namespace DotSpatialForm.UI
{
    partial class EnvConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EnvConfigForm));
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.modeSt = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.IdNameBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.arcpyPath1 = new System.Windows.Forms.TextBox();
            this.SetArcpy = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button2.Location = new System.Drawing.Point(388, 212);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(93, 32);
            this.button2.TabIndex = 51;
            this.button2.Text = "Close";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.Location = new System.Drawing.Point(289, 212);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(93, 32);
            this.button1.TabIndex = 50;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 144);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 16);
            this.label1.TabIndex = 49;
            this.label1.Text = "Data loading mode";
            // 
            // modeSt
            // 
            this.modeSt.FormattingEnabled = true;
            this.modeSt.Items.AddRange(new object[] {
            "Model1",
            "Default"});
            this.modeSt.Location = new System.Drawing.Point(15, 174);
            this.modeSt.Name = "modeSt";
            this.modeSt.Size = new System.Drawing.Size(432, 20);
            this.modeSt.TabIndex = 48;
            this.modeSt.Text = "Model1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(12, 78);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(320, 16);
            this.label5.TabIndex = 47;
            this.label5.Text = "The formation identifies the field name";
            // 
            // IdNameBox
            // 
            this.IdNameBox.Location = new System.Drawing.Point(15, 104);
            this.IdNameBox.Name = "IdNameBox";
            this.IdNameBox.Size = new System.Drawing.Size(432, 21);
            this.IdNameBox.TabIndex = 46;
            this.IdNameBox.Text = "LithCode";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.Location = new System.Drawing.Point(12, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 16);
            this.label6.TabIndex = 45;
            this.label6.Text = "Arcpy Path";
            // 
            // arcpyPath1
            // 
            this.arcpyPath1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.arcpyPath1.Location = new System.Drawing.Point(15, 38);
            this.arcpyPath1.Name = "arcpyPath1";
            this.arcpyPath1.Size = new System.Drawing.Size(432, 23);
            this.arcpyPath1.TabIndex = 44;
            this.arcpyPath1.Text = "C:\\Python27\\ArcGIS10.2\\python.exe";
            // 
            // SetArcpy
            // 
            this.SetArcpy.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("SetArcpy.BackgroundImage")));
            this.SetArcpy.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.SetArcpy.Location = new System.Drawing.Point(454, 34);
            this.SetArcpy.Name = "SetArcpy";
            this.SetArcpy.Size = new System.Drawing.Size(30, 30);
            this.SetArcpy.TabIndex = 43;
            this.SetArcpy.UseVisualStyleBackColor = true;
            this.SetArcpy.Click += new System.EventHandler(this.SetArcpy_Click);
            // 
            // EnvConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(493, 255);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.modeSt);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.IdNameBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.arcpyPath1);
            this.Controls.Add(this.SetArcpy);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EnvConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Config";
            this.Load += new System.EventHandler(this.EnvConfigForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox modeSt;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox IdNameBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox arcpyPath1;
        private System.Windows.Forms.Button SetArcpy;
    }
}