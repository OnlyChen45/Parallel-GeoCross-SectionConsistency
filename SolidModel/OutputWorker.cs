using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
namespace SolidModel
{
    class OutputWorker
    {
        public BrepModel finalbrepModel;
        public void assembleBrep(List<BrepModel > brepModels) {
           this. finalbrepModel = new BrepModel();
            for (int i = 0; i < brepModels.Count(); i++) {//把每个模型的triangle都加入到最终的brepModel中去
                BrepModel tempModel = brepModels[i];
                for (int j = 0; j < tempModel.triangleList.Count; j++) {
                    Triangle triangle = (Triangle)tempModel.triangleList[j];
                    Vertex v0 = tempModel.GetVertexByIndex(triangle.v0);
                    Vertex v1 = tempModel.GetVertexByIndex(triangle.v1);
                    Vertex v2 = tempModel.GetVertexByIndex(triangle.v2);
                    finalbrepModel.addTriangle(v0.x, v0.y, v0.z, v1.x, v1.y, v1.z, v2.x, v2.y, v2.z);

                }

            } 
        }
    }
}
