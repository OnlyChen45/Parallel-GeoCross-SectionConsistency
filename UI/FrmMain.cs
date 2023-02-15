using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DotSpatial.Controls;
using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DotSpatialForm.UI;

namespace ThreeDModelSystemForSection
{
    public partial class FrmMain : Form
    {
        #region 属性字段

        // 工作目录
        private string workSpace = string.Empty;

        // TIN2OBJ工作空间目录
        private string workspacePath;

        //属性查询状态变量
        //矩形选时为1，圈选时为2
        bool circleflag = false;
        bool rectangleflag = false;
        private int SelectFlag = -1;

        //属性查询范围变量
        Coordinate coord11;
        Coordinate coord22;
        System.Drawing.Point coord1;
        System.Drawing.Point coord2;
        System.Drawing.Point coord3;

        DotSpatial.Topology.IEnvelope env;
        Rectangle circle;
        List<System.Drawing.Point>
            ListDownpoints = new List<System.Drawing.Point>();//用于多边形任意属性      
        #endregion

        #region 构造函数
        public FrmMain()
        {
            InitializeComponent();
            //添加新按钮
            //ToolStripButton SelBySQL = new ToolStripButton();
            ToolStripButton SelByCircle = new ToolStripButton();
            ToolStripButton SelByRectancle = new ToolStripButton();
            //设置按钮名称
            //SelBySQL.ToolTipText = "SQL查询";
            SelByCircle.ToolTipText = "圆查询";
            SelByRectancle.ToolTipText = "矩形查询";
            //设置按钮图片
            //string appStratPath = Application.StartupPath;
            //SelBySQL.Image = Image.FromFile(appStratPath + @"\picture\SQL.jpg");
            //SelByCircle.Image = Image.FromFile(appStratPath + @"\picture\Circle.jpg");
            //SelByRectancle.Image = Image.FromFile(appStratPath + @"\picture\Rect.jpg");
            //设置点击事件
            //SelBySQL.Click += new EventHandler(SQLQueryMenu_Click);
            SelByCircle.Click += new EventHandler(CircleSelectMenu_Click);
            SelByRectancle.Click += new EventHandler(RectangleSelectMenu_Click);
            //添加菜单按钮
            //SpatialToolStrip.Items.Add(SelBySQL);
            SpatialToolStrip.Items.Add(SelByCircle);
            SpatialToolStrip.Items.Add(SelByRectancle);
            //初始状态设置
            splitContainer3.Panel2Collapsed = true;
            AttributeMenu.Checked = false;
            //SQLQueryMenu.Enabled = false;
            CircleSelectMenu.Enabled = false;
        }
        #endregion

        #region 属性查询
        /// <summary>
        /// 查询状态
        /// </summary>
        private void SelectState()
        {
            if (AttributeMenu.Checked)
            {
                AttributeMenu.Checked = false;
                ArrtiBox.Visible = false;
                splitContainer3.Panel2Collapsed = true;
                CircleSelectMenu.Enabled = false;
                //SQLQueryMenu.Enabled = false;
            }
            else
            {
                AttributeMenu.Checked = true;
                ArrtiBox.Visible = true;
                splitContainer3.Panel2Collapsed = false;
                CircleSelectMenu.Enabled = true;
                //SQLQueryMenu.Enabled = true;
            }
        }

        /// <summary>
        /// 属性查询
        /// </summary>
        private void AttributeMenu_Click(object sender, EventArgs e)
        {
            if (MainMap.Layers.Count == 0)
            {
                MessageBox.Show("您未添加任何图层！");
                return;
            }
            DataTable dt = null;
            //未选中，则默认选中第一个图层
            if (MainMap.Layers.SelectedLayer == null)
            {
                MessageBox.Show("由于您未选中任何图层,默认选择最下面的图层");
                if (MainMap.Layers[0] is MapPolygonLayer)
                {
                    MapPolygonLayer p1 = (MapPolygonLayer)MainMap.Layers[0];
                    dt = p1.DataSet.DataTable;
                    ArrtiBox.DataSource = dt;
                }
                else if (MainMap.Layers[0] is MapLineLayer)
                {
                    MapLineLayer p2 = (MapLineLayer)MainMap.Layers[0];
                    dt = p2.DataSet.DataTable;
                    ArrtiBox.DataSource = dt;
                }
                else
                {
                    MapPointLayer p2 = (MapPointLayer)MainMap.Layers[0];
                    dt = p2.DataSet.DataTable;
                    ArrtiBox.DataSource = dt;
                }
            }
            else if (MainMap.Layers.SelectedLayer is MapPointLayer)
            {
                MapPointLayer temlayer = default(MapPointLayer);
                temlayer = (MapPointLayer)MainMap.Layers.SelectedLayer;
                dt = temlayer.DataSet.DataTable;
                ArrtiBox.DataSource = dt;
            }
            else if (MainMap.Layers.SelectedLayer is MapLineLayer)
            {
                MapLineLayer temlayer = default(MapLineLayer);
                temlayer = (MapLineLayer)MainMap.Layers.SelectedLayer;
                dt = temlayer.DataSet.DataTable;
                ArrtiBox.DataSource = dt;
            }
            else
            {
                MapPolygonLayer temlayer = default(MapPolygonLayer);
                temlayer = (MapPolygonLayer)MainMap.Layers.SelectedLayer;
                dt = temlayer.DataSet.DataTable;
                ArrtiBox.DataSource = dt;
            }
            SelectState();
        }

        /// <summary>
        /// SQL查询
        /// </summary>
        //private void SQLQueryMenu_Click(object sender, EventArgs e)
        //{
        //    ArrtiBox.ClearSelection();
        //    if (this.MainMap.Layers.Count == 0)
        //    {
        //        MessageBox.Show("您未添加任何图层！");
        //        return;
        //    }
        //    else
        //    {
        //        DataTable dt = null;
        //        if (MainMap.Layers.SelectedLayer == null)
        //        {
        //            MessageBox.Show("请您选中图层后，重新查询");
        //        }
        //        else if (MainMap.Layers.SelectedLayer is MapPointLayer)
        //        {
        //            MapPointLayer p1 = (MapPointLayer)MainMap.Layers.SelectedLayer;
        //            dt = p1.DataSet.DataTable;
        //            ArrtiBox.DataSource = dt;
        //            Query FC = new Query();
        //            this.AddOwnedForm(FC);
        //            string[] LayerName = new string[MainMap.Layers.Count];
        //            for (int i = 0; i < MainMap.Layers.Count; i++)
        //            {
        //                LayerName[i] = MainMap.Layers[i].LegendText;
        //            }
        //            FC.Show();
        //            FC.SelectAtt(LayerName, dt);
        //        }
        //        else if (MainMap.Layers.SelectedLayer is MapLineLayer)
        //        {
        //            MapLineLayer p2 = (MapLineLayer)MainMap.Layers.SelectedLayer;
        //            dt = p2.DataSet.DataTable;
        //            ArrtiBox.DataSource = dt;
        //            Query FC = new Query();
        //            this.AddOwnedForm(FC);
        //            string[] LayerName = new string[MainMap.Layers.Count];
        //            for (int i = 0; i < MainMap.Layers.Count; i++)
        //            {
        //                LayerName[i] = MainMap.Layers[i].LegendText;
        //            }
        //            FC.Show();
        //            FC.SelectAtt(LayerName, dt);

        //        }
        //        else
        //        {
        //            MapPolygonLayer p3 = (MapPolygonLayer)MainMap.Layers.SelectedLayer;
        //            dt = p3.DataSet.DataTable;
        //            ArrtiBox.DataSource = dt;
        //            Query FC = new Query();
        //            this.AddOwnedForm(FC);
        //            string[] LayerName = new string[MainMap.Layers.Count];
        //            for (int i = 0; i < MainMap.Layers.Count; i++)
        //            {
        //                LayerName[i] = MainMap.Layers[i].LegendText;
        //            }
        //            FC.Show();
        //            FC.SelectAtt(LayerName, dt);
        //        }
        //    }
        //}

        /// <summary>
        /// 圆形查询
        /// </summary>
        private void CircleSelectMenu_Click(object sender, EventArgs e)
        {
            //圈选
            MainMap.Refresh();
            ArrtiBox.ClearSelection();
            SelectFlag = 2;
        }

        /// <summary>
        /// 矩形查询
        /// </summary>
        private void RectangleSelectMenu_Click(object sender, EventArgs e)
        {
            //矩形选
            MainMap.Refresh();
            ArrtiBox.ClearSelection();
            SelectFlag = 1;
            MainMap.FunctionMode = FunctionMode.Select;
        }

        /// <summary>
        /// 读出线(面)要素的点集
        /// </summary>
        /// <param name="f">线/面要素</param>
        /// <returns>返回点要素集</returns>
        public IFeatureSet ReadPoint(DotSpatial.Data.IFeature f)
        {
            IFeatureSet point = new FeatureSet(FeatureType.Point);
            List<Coordinate> Coords = f.Coordinates.ToList();
            Coords.ForEach(delegate(Coordinate PointCoords)
            {
                DotSpatial.Topology.Point Point = new DotSpatial.Topology.Point(PointCoords);
                DotSpatial.Data.IFeature currentFeature = point.AddFeature(Point);
            });
            return point;
        }

        /// <summary>
        /// 点图层的圈选、矩形选
        /// </summary>
        /// <param name="p">传递点要素图层</param>
        /// <param name="pointF">点要素图层的要素集</param>
        private void SelectModeMoveP(MapPointLayer p, FeatureSet pointF)
        {
            //矩形选
            if ((SelectFlag == 1) && (rectangleflag == true) && ArrtiBox.Visible)
            {
                rectangleflag = false;
                env = new DotSpatial.Topology.Envelope(coord11, coord22);//生成矩形框
                List<Int32> feaCount = pointF.SelectIndices(new Extent(env));//搜寻在矩形框内的点
                foreach (int i in feaCount)
                {
                    DataGridViewRow dr = ArrtiBox.Rows[i];
                    ArrtiBox.MultiSelect = true;
                    dr.Selected = true;
                }
                if (feaCount.Count > 0)
                    ArrtiBox.FirstDisplayedScrollingRowIndex = feaCount[0];
                SelectFlag = -1;
            }
            if ((SelectFlag == 2) && (circleflag == false))
            {
                //圆选
                p.ClearSelection();
                double radious = Math.Sqrt((coord2.Y - coord1.Y) * (coord2.Y - coord1.Y) + (coord2.X - coord1.X) * (coord2.X - coord1.X));
                double r = Math.Sqrt((coord22.Y - coord11.Y) * (coord22.Y - coord11.Y) + (coord22.X - coord11.X) * (coord22.X - coord11.X));
                circle = new Rectangle((int)(coord1.X - radious), (int)(coord1.Y - radious), (int)(2 * radious), (int)(2 * radious));
                //筛选圆内的点
                Coordinate e1 = new Coordinate();
                Coordinate e2 = new Coordinate();
                e1.X = coord11.X - r;
                e1.Y = coord11.Y - r;
                e2.X = coord11.X + r;
                e2.Y = coord11.Y + r;
                DotSpatial.Topology.IEnvelope env1 = new DotSpatial.Topology.Envelope(e1, e2);
                List<Int32> feaCount = pointF.SelectIndices(new Extent(env1));
                foreach (int i in feaCount)
                {
                    DotSpatial.Data.IFeature pointFea = pointF.GetFeature(i) as DotSpatial.Data.IFeature;
                    DotSpatial.Topology.Point pPoint = (DotSpatial.Topology.Point)pointFea.BasicGeometry;
                    //圆内点小于半径r
                    if (Math.Sqrt((pPoint.Y - coord11.Y) * (pPoint.Y - coord11.Y) + (pPoint.X - coord11.X) * (pPoint.X - coord11.X)) < r)
                    {
                        p.Select(i);
                        DataGridViewRow dr = ArrtiBox.Rows[i];
                        dr.Selected = true;
                    }
                }
                if (feaCount.Count > 0)
                    ArrtiBox.FirstDisplayedScrollingRowIndex = feaCount[0];
                SelectFlag = -1;
            }
        }

        /// <summary>
        /// 线图层的圈选、矩形选
        /// </summary>
        /// <param name="p">传递线要素图层</param>
        /// <param name="pointF">线要素图层的要素集</param>
        private void SelectModeMoveL(MapLineLayer p, FeatureSet lineF)
        {
            int n = 0;
            List<Int32> num = new List<Int32>();
            //矩形选            
            if ((SelectFlag == 1) && (rectangleflag == true) && ArrtiBox.Visible)
            {
                rectangleflag = false;
                env = new DotSpatial.Topology.Envelope(coord11, coord22);//生成矩形框
                for (int i = 0; i < lineF.NumRows(); i++)
                {
                    IFeatureSet pointF = ReadPoint(lineF.GetFeature(i));
                    List<Int32> feaCount = pointF.SelectIndices(new Extent(env));
                    //有1个及1个以上的点在矩形框内
                    if (feaCount.Count > 1)
                    {
                        DataGridViewRow dr = ArrtiBox.Rows[i];
                        ArrtiBox.MultiSelect = true;
                        dr.Selected = true;
                        num.Add(i);
                    }
                }
                if (num.Count > 0)
                    ArrtiBox.FirstDisplayedScrollingRowIndex = num[0];
                SelectFlag = -1;
                num.Clear();
            }
            if ((SelectFlag == 2) && (circleflag == false))
            {
                //圆选
                p.ClearSelection();
                double radious = Math.Sqrt((coord2.Y - coord1.Y) * (coord2.Y - coord1.Y) + (coord2.X - coord1.X) * (coord2.X - coord1.X));
                double r = Math.Sqrt((coord22.Y - coord11.Y) * (coord22.Y - coord11.Y) + (coord22.X - coord11.X) * (coord22.X - coord11.X));
                circle = new Rectangle((int)(coord1.X - radious), (int)(coord1.Y - radious), (int)(2 * radious), (int)(2 * radious));
                //筛选圆内的点
                Coordinate e1 = new Coordinate();
                Coordinate e2 = new Coordinate();
                e1.X = coord11.X - r;
                e1.Y = coord11.Y - r;
                e2.X = coord11.X + r;
                e2.Y = coord11.Y + r;
                DotSpatial.Topology.IEnvelope env1 = new DotSpatial.Topology.Envelope(e1, e2);
                for (int j = 0; j < lineF.NumRows(); j++)
                {
                    //初判
                    bool result = lineF.GetFeature(j).Intersects(env1);
                    if (result == true)
                    {
                        IFeatureSet pointF1 = ReadPoint(lineF.GetFeature(j));
                        for (int q = 0; q < pointF1.NumRows(); q++)
                        {
                            DotSpatial.Data.IFeature pointFea = pointF1.GetFeature(q) as DotSpatial.Data.IFeature;
                            DotSpatial.Topology.Point pPoint = (DotSpatial.Topology.Point)pointFea.BasicGeometry;
                            //圆内点到圆心距离小于半径r
                            if (Math.Sqrt((pPoint.Y - coord11.Y) * (pPoint.Y - coord11.Y) + (pPoint.X - coord11.X) * (pPoint.X - coord11.X)) < r)
                                n++;
                        }
                        //细判，有1个以上的点在圆内
                        if (n > 1)
                        {
                            p.Select(j);
                            DataGridViewRow dr = ArrtiBox.Rows[j];
                            ArrtiBox.MultiSelect = true;
                            dr.Selected = true;
                            num.Add(j);
                        }
                    }
                }
                if (num.Count > 0)
                    ArrtiBox.FirstDisplayedScrollingRowIndex = num[0];
                SelectFlag = -1;
                num.Clear();
            }
        }

        /// <summary>
        /// 面图层的圈选、矩形选
        /// </summary>
        /// <param name="p">传递面要素图层</param>
        /// <param name="pointF">面要素图层的要素集</param>
        private void SelectModeMoveM(MapPolygonLayer p, IFeatureSet polygonF)
        {
            int m = 0;
            List<Int32> num = new List<Int32>();//记录面要素序号            
            if ((SelectFlag == 2) && (circleflag == false))
            {
                //圆选   
                p.ClearSelection();
                double radious = Math.Sqrt((coord2.Y - coord1.Y) * (coord2.Y - coord1.Y) + (coord2.X - coord1.X) * (coord2.X - coord1.X));
                double r = Math.Sqrt((coord22.Y - coord11.Y) * (coord22.Y - coord11.Y) + (coord22.X - coord11.X) * (coord22.X - coord11.X));
                circle = new Rectangle((int)(coord1.X - radious), (int)(coord1.Y - radious), (int)(2 * radious), (int)(2 * radious));
                //筛选圆内的点
                Coordinate e1 = new Coordinate();
                Coordinate e2 = new Coordinate();
                e1.X = coord11.X - r;
                e1.Y = coord11.Y - r;
                e2.X = coord11.X + r;
                e2.Y = coord11.Y + r;
                DotSpatial.Topology.IEnvelope env1 = new DotSpatial.Topology.Envelope(e1, e2);
                for (int j = 0; j < polygonF.NumRows(); j++)
                {
                    //初判
                    bool result = polygonF.GetFeature(j).Intersects(env1);
                    if (result == true)
                    {
                        IFeatureSet pointF1 = ReadPoint(polygonF.GetFeature(j));
                        for (int pp = 0; pp < pointF1.NumRows(); pp++)
                        {
                            DotSpatial.Data.IFeature pointFea = pointF1.GetFeature(pp) as DotSpatial.Data.IFeature;
                            DotSpatial.Topology.Point pPoint = (DotSpatial.Topology.Point)pointFea.BasicGeometry;
                            //圆内点到圆心距离小于半径r
                            if (Math.Sqrt((pPoint.Y - coord11.Y) * (pPoint.Y - coord11.Y) + (pPoint.X - coord11.X) * (pPoint.X - coord11.X)) < r)
                                m++;
                        }
                        //细判，有1个以上的点在圆内
                        if (m > 1)
                        {
                            p.Select(j);
                            DataGridViewRow dr = ArrtiBox.Rows[j];
                            ArrtiBox.MultiSelect = true;
                            dr.Selected = true;
                            num.Add(j);
                        }
                    }
                }
                if (num.Count > 0)
                    ArrtiBox.FirstDisplayedScrollingRowIndex = num[0];
                SelectFlag = -1;
                num.Clear();
            }
        }

        /// <summary>
        /// 鼠标事件
        /// </summary>
        private void MainMap_Paint(object sender, PaintEventArgs e)
        {
            if (circleflag)
            {
                e.Graphics.DrawEllipse(Pens.Red, circle);
            }
        }

        private void MainMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (SelectFlag == -1)
                return;
            circleflag = false;
            MainMap.Invalidate();
            coord22 = MainMap.PixelToProj(e.Location);
            coord2 = e.Location;
            //选中图层为空，则默认选中第一个图层（面图层）
            if (MainMap.Layers.SelectedLayer == null)
            {
                if (MainMap.Layers[0] is MapPolygonLayer)
                {
                    MapPolygonLayer tem1 = (MapPolygonLayer)MainMap.Layers[0]; ;//显示选中的图层
                    FeatureSet polygonF = tem1.FeatureSet as FeatureSet;
                    tem1.ClearSelection();
                    SelectModeMoveM(tem1, polygonF);//属性查询操作
                    MainMap.FunctionMode = FunctionMode.None;
                    if (!ArrtiBox.Visible)
                        AttributeMenu_Click(sender, e);
                    MessageBox.Show("由于您未选中任何图层，默认您选择了最下面的图层");
                }
                else if (MainMap.Layers[0] is MapLineLayer)
                {
                    MapLineLayer tem2 = (MapLineLayer)MainMap.Layers[0]; ;//显示选中的图层
                    FeatureSet lineF = tem2.FeatureSet as FeatureSet;
                    tem2.ClearSelection();
                    SelectModeMoveL(tem2, lineF);//属性查询操作
                    MainMap.FunctionMode = FunctionMode.None;
                    if (!ArrtiBox.Visible)
                        AttributeMenu_Click(sender, e);
                    MessageBox.Show("由于您未选中任何图层，默认您选择了最下面的图层");
                }
                else
                {
                    MapPointLayer tem3 = (MapPointLayer)MainMap.Layers[0]; ;//显示选中的图层
                    FeatureSet pointF = tem3.FeatureSet as FeatureSet;
                    tem3.ClearSelection();
                    SelectModeMoveP(tem3, pointF);//属性查询操作
                    MainMap.FunctionMode = FunctionMode.None;
                    if (!ArrtiBox.Visible)
                        AttributeMenu_Click(sender, e);
                    MessageBox.Show("由于您未选中任何图层，默认您选择了最下面的图层");
                }
            }
            //选中图层是面要素图层
            else if (MainMap.Layers.SelectedLayer is MapPolygonLayer)
            {
                MapPolygonLayer tem1 = default(MapPolygonLayer);
                tem1 = (MapPolygonLayer)MainMap.Layers.SelectedLayer;//显示选中的图层
                FeatureSet polygonF1 = tem1.FeatureSet as FeatureSet;
                tem1.ClearSelection();
                SelectModeMoveM(tem1, polygonF1);//属性查询操作
                MainMap.FunctionMode = FunctionMode.None;
                if (!ArrtiBox.Visible)
                    AttributeMenu_Click(sender, e);
            }
            //选中图层是线要素图层
            else if (MainMap.Layers.SelectedLayer is MapLineLayer)
            {
                MapLineLayer tem2 = default(MapLineLayer);
                tem2 = (MapLineLayer)MainMap.Layers.SelectedLayer;//显示选中的图层
                FeatureSet lineF = tem2.FeatureSet as FeatureSet;
                tem2.ClearSelection();
                SelectModeMoveL(tem2, lineF);//属性查询操作
                MainMap.FunctionMode = FunctionMode.None;
                if (!ArrtiBox.Visible)
                    AttributeMenu_Click(sender, e);
            }
            //选中图层是点图层
            else
            {
                MapPointLayer tem3 = default(MapPointLayer);
                tem3 = (MapPointLayer)MainMap.Layers.SelectedLayer;//显示选中的图层
                FeatureSet pointF = tem3.FeatureSet as FeatureSet;
                tem3.ClearSelection();
                SelectModeMoveP(tem3, pointF);//属性查询操作
                MainMap.FunctionMode = FunctionMode.None;
                if (!ArrtiBox.Visible)
                    AttributeMenu_Click(sender, e);
            }
        }

        private void MainMap_MouseDown(object sender, MouseEventArgs e)
        {
            MainMap.Invalidate();
            coord11 = MainMap.PixelToProj(e.Location);
            coord1.X = e.X;
            coord1.Y = e.Y;
            switch (SelectFlag)
            {
                case -1:
                    return;
                case 1://矩形选
                    rectangleflag = true;
                    break;
                case 2://圈选
                    circleflag = true;
                    break;
            }
        }

        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            GeoMouseArgs args = new GeoMouseArgs(e, MainMap);
            CoordX.Text = " " + args.GeographicLocation.X + "  ";
            CoordY.Text = " " + args.GeographicLocation.Y + "";
            if (SelectFlag == -1)
                return;
            Coordinate coord = MainMap.PixelToProj(e.Location);
            coord3 = e.Location;
            if (SelectFlag == 2 && circleflag)
            {
                int r = (int)(Math.Sqrt((coord1.Y - coord3.Y) * (coord1.Y - coord3.Y) + (coord1.X - coord3.X) * (coord1.X - coord3.X)));
                circle = new Rectangle(coord1.X - r, coord1.Y - r, 2 * r, 2 * r);
                MainMap.Invalidate();//失效一个区域，并使其重绘
            }
        }
        #endregion

        #region 视图操作
        /// <summary>
        /// 当选中不同图层时，属性表的切换
        /// </summary>
        private void Legend_Click(object sender, EventArgs e)
        {
            DataTable dt = null;
            if (ArrtiBox.Visible)
            {
                if (MainMap.Layers.SelectedLayer.GetType() == typeof(MapPointLayer))
                {
                    MapPointLayer temlayer = default(MapPointLayer);
                    temlayer = (MapPointLayer)MainMap.Layers.SelectedLayer;
                    dt = temlayer.DataSet.DataTable;
                    ArrtiBox.DataSource = dt;
                }
                else if (MainMap.Layers.SelectedLayer.GetType() == typeof(MapLineLayer))
                {
                    MapLineLayer temlayer = default(MapLineLayer);
                    temlayer = (MapLineLayer)MainMap.Layers.SelectedLayer;
                    dt = temlayer.DataSet.DataTable;
                    ArrtiBox.DataSource = dt;
                }
                else
                {
                    MapPolygonLayer temlayer = default(MapPolygonLayer);
                    temlayer = (MapPolygonLayer)MainMap.Layers.SelectedLayer;
                    dt = temlayer.DataSet.DataTable;
                    ArrtiBox.DataSource = dt;
                }
            }
        }

        /// <summary>
        /// 状态栏设置
        /// </summary>
        private void StateMenu_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                ((ToolStripMenuItem)sender).Checked = false;
                this.StatusStrip.Visible = false;
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                this.StatusStrip.Visible = true;
            }
        }

        /// <summary>
        /// 工具条设置
        /// </summary>
        private void ToolbarMenu_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                ((ToolStripMenuItem)sender).Checked = false;
                this.SpatialToolStrip.Visible = false;
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                this.SpatialToolStrip.Visible = true;
            }
        }

        /// <summary>
        /// 鹰眼显示控制
        /// </summary>
        private void HawkeyeMenu_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                ((ToolStripMenuItem)sender).Checked = false;
                splitContainer2.Panel2Collapsed = true;//Panel1收缩
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                splitContainer2.Panel2Collapsed = false;///Panel1展开
            }
        }

        /// <summary>
        /// 工作空间显示设置
        /// </summary>
        private void WorkSpaceMenu_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                ((ToolStripMenuItem)sender).Checked = false;
                splitContainer2.Panel1Collapsed = true;//Panel2收缩
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                splitContainer2.Panel1Collapsed = false;///Panel2展开
            }
        }

        /// <summary>
        /// 主地图界面范围改变，鹰眼对应显示
        /// </summary>
        private void MianMap_ViewExtentsChanged(object sender, ExtentArgs e)
        {
            //对地图进行放大/缩小操作时
            if (MainMap.Layers.Count == 0)
                return;
            else
            {
                OverView.ClearLayers();
                //加载地图
                for (int i = 0; i < MainMap.Layers.Count; i++)
                {
                    OverView.Layers.Add(MainMap.Layers[i]);
                }
                //鹰眼地图内extents绘制（加载框）
                MapPolygonLayer polygonLayer = default(MapPolygonLayer);
                FeatureSet polygonF = new FeatureSet(FeatureType.Polygon);
                //新建面要素
                DotSpatial.Topology.Polygon p = (DotSpatial.Topology.Polygon)MainMap.ViewExtents.ToEnvelope().ToPolygon();
                polygonF.AddFeature(p);
                //显示
                polygonLayer = (MapPolygonLayer)OverView.Layers.Add(polygonF);
                PolygonSymbolizer symbol = new PolygonSymbolizer(Color.Empty, Color.Blue);

                polygonLayer.Symbolizer = symbol;
            }
        }
        #endregion 

        #region 文件操作
        /// <summary>
        /// 新建文件
        /// </summary>
        private void NewFileMenu_Click(object sender, EventArgs e)
        {
            //地图和鹰眼均清空
            MainMap.Layers.Clear();
            OverView.Layers.Clear();
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        private void OpenFileMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "supported files (*.dspx)|*.dspx|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                AppManager appManage = new AppManager();
                appManage.Map = MainMap;
                appManage.Legend = Legend;
                SerializationManager seriManage = new SerializationManager(appManage);
                seriManage.OpenProject(fileName);

                this.workSpace = System.IO.Path.GetDirectoryName(fileName);
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        private void SaveFileMenuItem_Click(object sender, EventArgs e)
        {
            if (this.MainMap.Layers.Count == 0)
            {
                MessageBox.Show("您未添加任何图层!",
                    "系统消息", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "supported files (*.dspx)|*.dspx|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;
                AppManager appManage = new AppManager();
                appManage.Map = MainMap;
                appManage.Legend = Legend;
                SerializationManager seriManager = new SerializationManager(appManage);
                seriManager.SaveProject(fileName);
            }
        }

        /// <summary>
        /// 另存为
        /// </summary>
        private void SaveAsAnotherMenu_Click(object sender, EventArgs e)
        {
            if (this.MainMap.Layers.Count == 0)
            {
                MessageBox.Show("您未添加任何图层！");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "supported files (*.dspx)|*.dspx|All files (*.*)|*.*"; //过滤文件类型
            DialogResult r = sfd.ShowDialog();
            string temFilePath = "";
            if (r == DialogResult.OK)
            {
                temFilePath = sfd.FileName;
                AppManager am = new AppManager();
                am.Legend = Legend;
                am.Map = MainMap;
                SerializationManager sm = new SerializationManager(am);
                sm.SaveProject(temFilePath);
            }
        }

        /// <summary>
        /// 导入文件
        /// </summary>
        private void ImportFileMenuItem_Click(object sender, EventArgs e)
        {
            MainMap.AddLayers();
        }

        /// <summary>
        /// 导出文件
        /// </summary>
        private void ExportFileMenuItem_Click(object sender, EventArgs e)
        {
            if (MainMap.Layers.Count > 0)
            {
                MainMap.SaveLayer();
            }
            else
            {
                MessageBox.Show("您未添加任何图层！");
            }
        }

        /// <summary>
        /// 退出程序
        /// </summary>
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("程序正在运行，确定退出?",
                "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                this.Close();
        }
        #endregion 

        #region 建模
        private void FangModelingWithoutMorphingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void 地质界线的分段ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            
        }

        private void 方山地貌的三维建模ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            
        }

        private void 方山构造建模noMorphingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //FangModelingEntry fangModelingEntry = new FangModelingEntry(MainMap, workSpace);
            //if (fangModelingEntry.ModelBuilding())
            //    MessageBox.Show("方山地貌模型构建成功!");
        }

        private void 方山地貌的三维建模ToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }

        private void 地质界线的分段ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void 石墙建模ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

       
        #endregion

        #region 数据转换
        private void TIN2OBJToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        #endregion


        public string idname;
        public string arcpypath;
        public string initmodel;
        public string config;
        public int crushdonecount;
        public Dictionary<int, string[]> beziershplist;
        public Dictionary<int, string[]> modellist;
        private string[] getlayersFilePath() {
            List<string> filepathlist = new List<string>();
            foreach (var layer in MainMap.Layers)
            {
                DotSpatial.Data.DataSet dataSet = layer.DataSet as DotSpatial.Data.DataSet;//这里是dotspatial的内容，即如何取出目前已经加载到地图上的layers的属性信息
                
                FeatureSet sp = dataSet as FeatureSet;//把IFeatureLayer 强制转换为ShapeFile
                string path = sp.Filename;
                filepathlist.Add(path);
            }
            return filepathlist.ToArray();
        }
        private void addResultToMap() {
            if (addtoMap.Count > 0) {
                foreach (string path in addtoMap) {
                    MainMap.AddLayer(path);
                }
                addtoMap.Clear();
            }
        }
        public List<string> addtoMap;
        private void 分支地层一致ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StrataBranchesForm branchesForm = new StrataBranchesForm();
            branchesForm.pathlist = getlayersFilePath();
            branchesForm.ShowDialog(this);
            addResultToMap();
        }

        private void 错动地层一致ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoChangeForm topoChangeForm = new TopoChangeForm();
            topoChangeForm.pathlist = getlayersFilePath();
            topoChangeForm.ShowDialog(this);
            addResultToMap();
        }

        private void 尖灭地层一致ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SharpenProfilesForm sharpenProfilesForm = new SharpenProfilesForm();
            sharpenProfilesForm.pathlist = getlayersFilePath();
            sharpenProfilesForm.ShowDialog(this);
            addResultToMap();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            addtoMap = new List<string>();
            modellist = new Dictionary<int, string[]>();
            beziershplist = new Dictionary<int, string[]>();
            crushdonecount = 0;
            config = "EnvConfig.txt";
            if (Directory.Exists(config)) {
                FileStream fileStream = new FileStream(config, FileMode.Open);
                StreamReader reader = new StreamReader(fileStream);
                this.arcpypath = reader.ReadLine();
                this.idname = reader.ReadLine();
                this.initmodel = reader.ReadLine();
                reader.Close();
                fileStream.Close();
            } else {
                FileStream fileStream = new FileStream(config, FileMode.Create);
                StreamWriter writer = new StreamWriter(fileStream);
                writer.WriteLine(@"C:\Python27\ArcGIS10.2\python.exe");
                writer.WriteLine(@"LithCode");
                writer.WriteLine("Model1");
                this.arcpypath = @"C:\Python27\ArcGIS10.2\python.exe";
                this.idname = "LithCode";
                this.initmodel = "Model1";
                writer.Close();
                fileStream.Close();
            }
           
        }

        private void 三维剖面生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Transe3DForm transe3DForm = new Transe3DForm();
            transe3DForm.pathlist = getlayersFilePath();
            transe3DForm.ShowDialog(this);
            addResultToMap();
        }

        private void 三维模型构建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModelQueueForm modelerForm = new ModelQueueForm();
            modelerForm.ShowDialog(this);
        }

        private void 配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnvConfigForm envConfigForm = new EnvConfigForm();
            envConfigForm.ShowDialog(this);
            FileStream fileStream = new FileStream(config, FileMode.Open);
            StreamReader reader = new StreamReader(fileStream);
            this.arcpypath = reader.ReadLine();
            this.idname = reader.ReadLine();
            this.initmodel = reader.ReadLine();
            reader.Close();
            fileStream.Close();
        }

        private void 边界冲突处理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EageCrushForm eageCrushForm = new EageCrushForm();
            eageCrushForm.pathlist = getlayersFilePath();
            eageCrushForm.ShowDialog(this);
            addResultToMap();
        }

        private void 解耦插值剖面生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateDecouplingSectionForm decouplingFrom = new CreateDecouplingSectionForm();
            decouplingFrom.pathlist = getlayersFilePath();
            decouplingFrom.ShowDialog(this);
            addResultToMap();
        }

        private void 三维模型构建曲面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModelBezierCurveForm modelBezierCurveForm = new ModelBezierCurveForm();
            modelBezierCurveForm.pathlist = getlayersFilePath();
            modelBezierCurveForm.ShowDialog(this);
        }

        private void 插值剖面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InterpolationForm interpolationForm = new InterpolationForm();
            interpolationForm.pathlist = getlayersFilePath();
            interpolationForm.ShowDialog(this);

        }

        private void 帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
