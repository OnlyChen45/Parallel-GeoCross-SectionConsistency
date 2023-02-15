using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using SolidModel;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{ /// <summary>
/// 读取弧段，数据IO
/// </summary>
    public class readARCs
    {
        public List<ModelWithArc> modelWithArcs;
        public Dictionary<int, Eage> arcs1, arcs2;
        public readARCs(string arcspath1,string arcspath2) {//把弧段对应关系给对应上
            this.arcs1 = new Dictionary<int, Eage>();
            this.arcs2 = new Dictionary<int, Eage>();
            this.modelWithArcs = new List<ModelWithArc>();
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dsarc1 = driver.Open(arcspath1, 1);
            DataSource dsarc2 = driver.Open(arcspath2, 1);
            Layer layer1 = dsarc1.GetLayerByIndex(0);
            Layer layer2 = dsarc2.GetLayerByIndex(0);
            long featurecount1 = layer1.GetFeatureCount(0);
            long featurecount2 = layer2.GetFeatureCount(0);
            for (int i = 0; i < featurecount1; i++) {
                Feature feature = layer1.GetFeature(i);
                Geometry geom = feature.GetGeometryRef();
                int pointcount = geom.GetPointCount();
                Eage eage = new Eage();
                eage.name = arcspath1 + i.ToString();
                for (int j = 0; j < pointcount; j++) {
                    Vertex vertex = new Vertex();
                    vertex.name = eage.name + j.ToString();
                    vertex.x = geom.GetX(j);
                    vertex.y = geom.GetY(j);
                    vertex.z = geom.GetZ(j);
                    eage.AddVertex(vertex);
                }
                int id = feature.GetFieldAsInteger("arcID");
                this.arcs1.Add(id, eage);
            }
            for (int i = 0; i < featurecount2; i++)
            {
                Feature feature = layer2.GetFeature(i);
                Geometry geom = feature.GetGeometryRef();
                int pointcount = geom.GetPointCount();
                Eage eage = new Eage();
                eage.name = arcspath2 + i.ToString();
                for (int j = 0; j < pointcount; j++)
                {
                    Vertex vertex = new Vertex();
                    vertex.name = eage.name + j.ToString();
                    vertex.x = geom.GetX(j);
                    vertex.y = geom.GetY(j);
                    vertex.z = geom.GetZ(j);
                    eage.AddVertex(vertex);
                }
                int id = feature.GetFieldAsInteger("arcID");
                this.arcs2.Add(id, eage);
            }
        }
        public void makeArcPairs() {//做弧段对应
            int count = arcs1.Count();
            foreach (var vk in arcs1) {
                int key = vk.Key;
                Eage eage1 = arcs1[key];
                Eage eage2 = arcs2[key];
                List<Vertex> vlist1 = eage1.vertexList;
                List<Vertex> vlist2 = eage2.vertexList;
                ArcSe arcse1 = new ArcSe(vlist1[0], vlist1[vlist1.Count - 1]);
                arcse1.setEage(eage1);
                ArcSe arcse2 = new ArcSe(vlist2[0], vlist2[vlist2.Count - 1]);
                arcse2.setEage(eage2);
                ModelWithArc modelarc1 = new ModelWithArc(arcse1, arcse2);
                Eage eage3 = getnizhi(eage2);
                List<Vertex> vlist3 = eage3.vertexList;
                ArcSe arcse3 = new ArcSe(vlist3[0], vlist3[vlist3.Count - 1]);
                arcse3.setEage(eage3);
                ModelWithArc modelarc2 = new ModelWithArc(arcse1, arcse3);
                #region 这个模块是用来测试怎么能判断弧段的顺逆的。
                bool mark = false;
                Vertex v00, v01, v10, v11;
                v00 = eage1.vertexList[0];
                int eage1Vcount = eage1.vertexList.Count();
                int eage2Vcount = eage2.vertexList.Count();
                v01 = eage1.vertexList[eage1Vcount - 1];
                v10 = eage2.vertexList[0];
                v11 = eage2.vertexList[eage2Vcount - 1];
                //下边把v10 v11 加上中心点的距离给挪到一起的向量
                Vertex center1 = getEageCenter3D(eage1);
                Vertex center2 = getEageCenter3D(eage2);
                double[] dxyz = new double[3];
                dxyz[0] = center1.x - center2.x;
                dxyz[1] = center1.y - center2.y;
                dxyz[2] = center2.z - center2.z;
                Vertex v101 = new Vertex();
                Vertex v111 = new Vertex();

                v101.x = v10.x + dxyz[0];
                v101.y = v10.y + dxyz[1];
                v101.z = v10.z + dxyz[2];
                v111.x = v11.x + dxyz[0];
                v111.y = v11.y + dxyz[1];
                v111.z = v11.z + dxyz[2];
                double dis1, dis2;
                dis1 = distance3D(v00, v101) + distance3D(v01, v111);
                dis2 = distance3D(v00, v111) + distance3D(v01, v101);
                if (dis1 < dis2) mark = true; else mark = false;
                #endregion
                if(mark)
                this.modelWithArcs.Add(modelarc1);
                else 
                this.modelWithArcs.Add(modelarc2);
            }
        
        }
        public List<BrepModel> makemodels() {
            ContourHelp contourHelp1 = new ContourHelp();
            List<BrepModel> result = contourHelp1.buildModelsWithArcsPairs(ref this.modelWithArcs);
            return result;
        }
        private Eage getnizhi(Eage  eage) {
            Eage result = new Eage();
            result.name = eage.name;
            result.belongTo = eage.belongTo;
            result.id = eage.id;
            List<Vertex> vertices = eage.vertexList;
            int count = vertices.Count();
            for (int i = count - 1; i >= 0; i--) {
                result.AddVertex(vertices[i]);
            }
            return result;
        }
        private double distance3D(Vertex v1,Vertex v2) {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;
            double dz = v1.z - v2.z;
            double result = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
            return result;
        }
        private Vertex getEageCenter3D(Eage eage1) {
            Vertex vertex = new Vertex();
            List<Vertex> vertices = eage1.vertexList;
            int count = vertices.Count();
            double x = 0, y = 0, z = 0;
            for (int i = 0; i < count; i++) {
                Vertex vertex1 = vertices[i];
                x += vertex1.x;
                y += vertex1.y;
                z += vertex1.z;
            }
            vertex.x = x / count;
            vertex.y = y / count;
            vertex.z = z / count;
            return vertex;
        }
    }
}
