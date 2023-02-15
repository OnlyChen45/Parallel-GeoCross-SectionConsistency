using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 业务层
    /// </summary>
  public  class MergeBreps
    {
        static public BrepModel Merge3DModel(List<BrepModel> brepModels) {
            int count = brepModels.Count;
            BrepModel brepResult = new BrepModel();
            for (int i = 0; i < count; i++) {
                BrepModel brepModel = brepModels[i];
                int vertcount = brepModel.triangleList.Count;
                Object[] triangles = brepModel.triangleList.ToArray();
                for (int j = 0; j < vertcount; j++) {
                    Triangle triangle = (Triangle)triangles[j];
                    int id1 = triangle.v0;
                    int id2 = triangle.v1;
                    int id3 = triangle.v2;
                    Vertex v1 = brepModel.GetVertexByIndex(id1);
                    Vertex v2 = brepModel.GetVertexByIndex(id2);
                    Vertex v3 = brepModel.GetVertexByIndex(id3);
                    brepResult.addTriangle(v1.x, v1.y, v1.z,
                                            v2.x, v2.y, v2.z,
                                            v3.x, v3.y, v3.z);
                }
            }
            return brepResult;
        }
    }
}
