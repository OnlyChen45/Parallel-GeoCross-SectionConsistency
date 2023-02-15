using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using OSGeo.GDAL;
using OSGeo.OGR;
using TriangleNet;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 输入地质图上某个geometry+整体dem，返回一个拉起来的obj的brepmodel对象
    /// </summary>
     public class Surface3DFormer
    {
        DemIO mydem;
        Dictionary<int, Geometry> geopolys;
        Dictionary<int, BrepModel> surfacemodels;
        public Surface3DFormer(DemIO demIO, Dictionary<int, Geometry> geopolys) {
            this.mydem = demIO;
            this.geopolys = geopolys;
        }
        public Dictionary<int, BrepModel> makeSurface3D() {
            this.surfacemodels = new Dictionary<int, BrepModel>();
            foreach (var vk in geopolys) {
                int id = vk.Key;
                Geometry poly = vk.Value;
                Geometry boundary = poly.Boundary();
                int pcount = boundary.GetPointCount();
                Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
/*                Dictionary<char, double[]> demboundrypoints = new Dictionary<char, double[]>();
                List<double> xlist1 = new List<double>();
                List<double> ylist1 = new List<double>();
                List<double> zlist1 = new List<double>();*/
                for (int i = 0; i < pcount; i++) {
                    double x = boundary.GetX(i);
                    double y = boundary.GetY(i);
                    ring.AddPoint_2D(x, y);
/*                    double z = mydem.BilinearInterpolation(x, y,mydem.);
                    xlist1.Add(x);
                    ylist1.Add(y);
                    zlist1.Add(z);*/
                }
/*                demboundrypoints.Add('x', xlist1.ToArray());
                demboundrypoints.Add('y', ylist1.ToArray());
                demboundrypoints.Add('z', zlist1.ToArray());*/
                Geometry polyarea = new Geometry(wkbGeometryType.wkbPolygon);
                polyarea.AddGeometry(ring);
                Dictionary<char,double[]>dempoints=  mydem.getCenterPointInGeom(polyarea);
                List<TriangleNet.Geometry.Vertex> vertexlist = transDicToVertex(dempoints);
                TriangleNet.Mesh mesh = GetTriMesh(vertexlist);
                BrepModel brep = GetBrepModel(mesh, dempoints);
                this.surfacemodels.Add(id, brep);
            }
            return this.surfacemodels;
        }
        private List<TriangleNet.Geometry.Vertex> transDicToVertex(Dictionary<char, double[]> points) {
            List<TriangleNet.Geometry.Vertex> pNewVertexs = new List<TriangleNet.Geometry.Vertex>();
            int count = points['x'].Length;
            for (int i = 0; i < count; i++)
            {
                TriangleNet.Geometry.Vertex vt = new TriangleNet.Geometry.Vertex();
                vt.ID = i;
                vt.NAME = i.ToString();
                vt.X = points['x'][i];
                vt.Y = points['x'][i];
                pNewVertexs.Add(vt);
            }
            return pNewVertexs;
        }
        private TriangleNet.Mesh GetTriMesh(List<TriangleNet.Geometry.Vertex> pA)
        {
            #region 三角剖分模块
            //1. 
            //约束选项（约束类）
            var options = new TriangleNet.Meshing.ConstraintOptions();
            options.SegmentSplitting = 1;
            options.ConformingDelaunay = false;
            options.Convex = false;

            //质量选项（质量类）
            var quality = new TriangleNet.Meshing.QualityOptions();
            TriangleNet.Geometry.IPolygon input = GetPolygon(pA);
            TriangleNet.Geometry.Contour con = GetContourByTriangle(pA);
            //添加边界约束
            input.Add(con, false);


            TriangleNet.Mesh mesh = null;
            if (input != null)
            {
                mesh = (TriangleNet.Mesh)TriangleNet.Geometry.ExtensionMethods.Triangulate(input, options);

            }

            return mesh;
            #endregion

        }
        private TriangleNet.Geometry.IPolygon GetPolygon(List<TriangleNet.Geometry.Vertex> pA)
        {
            TriangleNet.Geometry.IPolygon data = new TriangleNet.Geometry.Polygon();

            foreach (var vt in pA)
            {
                TriangleNet.Geometry.Vertex triVertex = new TriangleNet.Geometry.Vertex(vt.X, vt.Y);
                triVertex.NAME = vt.NAME;
                ////vt.Label = 0;
                //vt.ID =data.Points.Count;
                data.Add(triVertex);

            }

            return data;
        }

        /// <summary>
        /// 获取三角剖分的边界线
        /// </summary>
        /// <param name="pA"></param>
 
        /// <returns></returns>
        public TriangleNet.Geometry.Contour GetContourByTriangle(List<TriangleNet.Geometry.Vertex> pA)
        {
            List<TriangleNet.Geometry.Vertex> pv = new List<TriangleNet.Geometry.Vertex>();

            foreach (var vt in pA)
            {
                pv.Add(vt);
            }

            TriangleNet.Geometry.Contour pNewCon = new TriangleNet.Geometry.Contour(pv);

            return pNewCon;

        }
        private BrepModel GetBrepModel(TriangleNet.Mesh mesh,Dictionary<char,double[]>points)
        {
            #region 模型构建

            //二维三角网
            TriMesh trimesh = new TriMesh();
            BrepModel pBrep = new BrepModel();

            foreach (var item in mesh.Triangles)
            {
                TriangleNet.Geometry.Vertex p1, p2, p3;

                TriangleNet.Topology.Triangle pTinTri = item;

                p1 = item.GetVertex(0);
                p2 = item.GetVertex(1);
                p3 = item.GetVertex(2);
                int id1 = p1.ID;
                int id2 = p2.ID;
                int id3 = p3.ID;
                double z1 = points['z'][id1];
                double z2 = points['z'][id2];
                double z3 = points['z'][id3];
             
               // trimesh.AddTriangle(p1.X, p1.Y, z1, p2.X, p2.Y, z2, p3.X, p3.Y, z3);
               


                //构建边界三角网
                pBrep.addTriangle(p1.X, p1.Y, z1, p2.X, p2.Y, z2, p3.X, p3.Y, z3);
            }

            return pBrep;

            #endregion
        }
    }
}
