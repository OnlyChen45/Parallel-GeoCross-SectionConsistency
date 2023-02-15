using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
namespace ThreeDModelSystemForSection
{/// <summary>
 /// 处理三维格网的一个文件，就姑且算是业务层
 ///这个是建模时候夹封口的，生成封口的代码
 /// </summary>
    public class CreateBrepModelByXY
    {
        static public Dictionary<int, BrepModel> MakePolyToModel(Dictionary<int,List<double[]>>orixyzSite,Dictionary<int,List<double[]>>xy2dList) {
            Dictionary<int, BrepModel> result = new Dictionary<int, BrepModel>();
            foreach (var vk in xy2dList) {
                List<double[]> xylist = vk.Value;
                List<double[]> orixyzlist = orixyzSite[vk.Key];
                List<TriangleNet.Geometry.Vertex> ListVA = GetTraingNetVertexs(xylist);
                TriangleNet.Mesh mesh = GetTriMesh(ListVA);
                BrepModel brepModel = GetBrepModel(mesh, orixyzlist);
                result.Add(vk.Key, brepModel);
            }
            return result;
        }
        static private BrepModel GetBrepModel(TriangleNet.Mesh mesh, List<double[]> orixyzlist)
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

                //将三角形添加到自定义的三角网中
                trimesh.AddTriangle(p1.X, p1.Y, 0, p2.X, p2.Y, 0, p3.X, p3.Y, 0);
                // trimesh.ExportTriMeshToShapfile(@"D:\graduateGIS\temp", "delaunaryA");

                // Vertex pp1 = pEageVertexDict[p1.NAME];
                //Vertex pp2 = pEageVertexDict[p2.NAME];
                //Vertex pp3 = pEageVertexDict[p3.NAME];
                int id1 = int.Parse(p1.NAME);
                int id2 = int.Parse(p2.NAME);
                int id3 = int.Parse(p3.NAME);
                double[] point1 = orixyzlist[id1];
                double[] point2 = orixyzlist[id2];
                double[] point3 = orixyzlist[id3];
                //构建边界三角网
                pBrep.addTriangle(point1[0], point1[1], point1[2], point2[0], point2[1], point2[2], point3[0], point3[1], point3[2]);
            }

            return pBrep;

            #endregion
        }
        static  public List<TriangleNet.Geometry.Vertex> GetTraingNetVertexs(List<double[]> eageVertex)
        {
            List<TriangleNet.Geometry.Vertex> pNewVertexs = new List<TriangleNet.Geometry.Vertex>();
            double ds = 0;
            for (int i = 0; i < eageVertex.Count; i++)
            {
                TriangleNet.Geometry.Vertex vt = new TriangleNet.Geometry.Vertex();
                vt.ID = i;
                vt.NAME = i.ToString();
                double[] xy = eageVertex[i];
                vt.X = xy[0];
                vt.Y = xy[1];
                pNewVertexs.Add(vt);
            }
            return pNewVertexs;
        }
       static private TriangleNet.Mesh GetTriMesh(List<TriangleNet.Geometry.Vertex> pA)
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
        static private TriangleNet.Geometry.IPolygon GetPolygon(List<TriangleNet.Geometry.Vertex> pA)
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
        static public TriangleNet.Geometry.Contour GetContourByTriangle(List<TriangleNet.Geometry.Vertex> pA)
        {
            List<TriangleNet.Geometry.Vertex> pv = new List<TriangleNet.Geometry.Vertex>();

            foreach (var vt in pA)
            {
                pv.Add(vt);
            }


            TriangleNet.Geometry.Contour pNewCon = new TriangleNet.Geometry.Contour(pv);

            return pNewCon;

        }
    }
}
