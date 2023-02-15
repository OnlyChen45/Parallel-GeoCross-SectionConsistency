using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using TriangleNet;
namespace SolidModel
{
    public class ArcModelHelp
    {
        public Dictionary<int,Eage> LayerA;
        public Dictionary<int, Eage> LayerB;
        public List<Vertex> nodesA;
        public List<Vertex> nodesB;
        public List<ModelWithArc> modesWithArcs;
        public ArcModelHelp() { 
        
        
        }
        public ArcModelHelp(Dictionary<int, Eage> LayerA, Dictionary<int, Eage> LayerB, List<Vertex> nodesA, List<Vertex> nodesB) {
            this.LayerA = LayerA;
            this.LayerB = LayerB;
            this.nodesA = nodesA;
            this.nodesB = nodesB;
            this.modesWithArcs = new List<ModelWithArc>();
        }
        /*为了满足三维数据的需求，所以要有一个转坐标的过程，那么之前的一套方法就不适用了，这会涉及到一个坐标转过去坐标转回来的问题
         * public List<List<BrepModel>> getModels(List<int> eagesIndex) {
            List<List<BrepModel>> result = new List<List<BrepModel>>();
            int count = eagesIndex.Count();
            for (int i = 0; i < count; i++) {
                Eage eageA = this.LayerA[eagesIndex[i]];
                Eage eageB = this.LayerB[eagesIndex[i]];
                List<Vertex> nodeA = getSuccVertex(eageA, this.nodesA);
                List<Vertex> nodeB = getSuccVertex(eageB, this.nodesB);
                ContourHelp contourHelp = new ContourHelp();
                List<BrepModel>brepModels = contourHelp.ArcToModels(eageA, eageB, nodeA, nodeB, ref this.modesWithArcs, false, false);
                result.Add(brepModels);
            }
            return result;
        }*/
        private List<Vertex> realCopy(List<Vertex> vertices) {
            List<Vertex> result = new List<Vertex>();
            int count = vertices.Count();
            for (int i = 0; i < count; i++) {
                Vertex vertex = new Vertex();
                Vertex vertexInList = vertices[i];
                vertex.x = vertexInList.x;
                vertex.y = vertexInList.y;
                vertex.z = vertexInList.z;
                vertex.name = vertexInList.name;
                result.Add(vertex);
            }
            return result;
        }
        public List<BrepModel> getModels(List<int> eagesIndex,Sectiontransform transformworker)
        {
            List<BrepModel> result = new List<BrepModel>();
            int count = eagesIndex.Count();
            for (int i = 0; i < count; i++)
            {
                if (i == 4) {
                    Console.WriteLine("debug用");
                }
                Eage eageA = this.LayerA[eagesIndex[i]];
                Eage eageB = this.LayerB[eagesIndex[i]];
                List<Vertex> nodesAcopy = realCopy(this.nodesA);
                List<Vertex> nodesBcopy = realCopy(this.nodesB);
                List<Vertex> nodeA = getSuccVertex(eageA, nodesAcopy);
                List<Vertex> nodeB = getSuccVertex(eageB, nodesBcopy); 
                transformworker.transEage(ref eageA, true);//把三维转成XOY面上的
                transformworker.transEage(ref eageB, true);
                transformworker.transVertexList(ref nodeA, true);
                transformworker.transVertexList(ref nodeB, true);
                ContourHelp contourHelp = new ContourHelp();
                contourHelp.getArcPairs(eageA, eageB, nodeA, nodeB, eagesIndex[i], ref this.modesWithArcs, false, false);

            }
            int pairscount = this.modesWithArcs.Count();
            for (int i = 0; i < pairscount; i++) {
                ModelWithArc arcpair = this.modesWithArcs[i];
                transformworker.transEage(ref arcpair.arc1.eage, false);//把线段转回去
                transformworker.transEage(ref arcpair.arc2.eage, false);
                this.modesWithArcs[i] = arcpair;
            }
            ContourHelp contourHelp1 = new ContourHelp();
            result= contourHelp1.buildModelsWithArcsPairs(ref this.modesWithArcs);
            return result;
        }

        public List<List<BrepModel>> GetModelsForSharpen(List<int> eagesIndex,bool toAorB, Dictionary<int, List<Vertex>> sharpenArcsV, Dictionary<int, List<Vertex>> sharpenEage,string eagename) {
            //输入toAorB表明这个尖灭地层与哪个面上轮廓相联系，true是A，false是B。然后sharpenArcsV是弧段上的顶点列表，sharpenEage是尖灭弧段的所有的点，eagename是为了模仿DataIO作eage用的
            List<List<BrepModel>> result = new List<List<BrepModel>>();
            Dictionary<int, Eage> layer ;
            List<Vertex> nodest = new List<Vertex>();
            if (toAorB)
            {
                layer = this.LayerA;
                nodest = this.nodesA;
            }
            else { 
                layer = this.LayerB;
                nodest = this.nodesB;
            }
            for (int i = 0; i < eagesIndex.Count; i++) {//循环处理输入的每个尖灭地层
                Eage eage = layer[eagesIndex[i]];
               
                List<Vertex> nodes = getSuccVertex(eage, nodest);
                List<Vertex> cutvs = sharpenEage[eagesIndex[i]];//这个是弧段上被分割时候用的点
                List<Vertex> vertices = sharpenArcsV[eagesIndex[i]];//这个是弧段上的点
                Eage arcEage = new Eage();//制作弧段拉开的边界eage
                arcEage.name = eagename + i.ToString();
                int  vercount = 0;
                for (int j = 0; j < vertices.Count; j++) {
                    Vertex vertex = vertices[j];
                    vertex.name = arcEage.name + vercount.ToString();
                    vercount++;
                    arcEage.AddVertex(vertex);
                }
                for (int j = vertices.Count - 2; j > 0; j--) {
                    Vertex vertex = vertices[j];
                    vertex.name = arcEage.name + vercount.ToString();
                    vercount++;
                    bool t = true;
                    arcEage.AddVertex(vertex,t);
                }
                //Eage polyeage=
                ContourHelp contourHelp = new ContourHelp();
                bool special = true;
                List<BrepModel> brepModels = contourHelp.ArcToModels(arcEage, eage, cutvs, nodes, special, false, false);
                result.Add(brepModels);
            }
            return result;
        }
       /* public List<List<BrepModel>> getModels(List<int> eagesIndex,Dictionary<int,List<Vertex>>sharpenArcsVA, Dictionary<int, List<Vertex>> sharpenEageA, Dictionary<int,List<Vertex>>sharpenArcsVB, Dictionary<int, List<Vertex>> sharpenEageB)
        {//本来是打算重构一下然后可以实现把弧段和面建模同一到面面建模上去的，结果发现不是很行，尤其是在寻找对应弧段的时候，几乎做不到。那就算了，写一个单独的函数来做吧。
            List<List<BrepModel>> result = new List<List<BrepModel>>();
            int count = eagesIndex.Count();
           // var keysA = sharpenArcsVA.Keys;
            //var keysB = sharpenArcsVB.Keys;
            for (int i = 0; i < count; i++)
            {
                Eage eageA, eageB;
                List<Vertex> nodeA, nodeB;
                if (sharpenArcsVA.ContainsKey(eagesIndex[i])) {
                    eageA = createEageByArc(sharpenEageA[eagesIndex[i]]);
                    nodeA = sharpenArcsVA[eagesIndex[i]];
                }
                else { 
                    eageA = this.LayerA[eagesIndex[i]];
                    nodeA = getSuccVertex(eageA, this.nodesA);
                }
                if (sharpenArcsVB.ContainsKey(eagesIndex[i]))
                {
                    eageB = createEageByArc(sharpenEageB[eagesIndex[i]]);
                    nodeB = sharpenArcsVB[eagesIndex[i]];
                }
                else
                {
                    eageB = this.LayerB[eagesIndex[i]];
                    nodeB = getSuccVertex(eageB, this.nodesB);
                }

                ContourHelp contourHelp = new ContourHelp();
                List<BrepModel> brepModels = contourHelp.ArcToModels(eageA, eageB, nodeA, nodeB, ref this.modesWithArcs, false, false);
                result.Add(brepModels);
            }
            return result;
        }
        private Eage createEageByArc(List<Vertex> eageVertex) {
            Eage eage = new Eage();
            return eage;
        }*/
        
        private List<Vertex> getSuccVertex(Eage eage, List<Vertex>nodes) {//获取eage对应的顺序的结点，node
            List<Vertex> vertices = eage.vertexList;
            List<Vertex> result = new List<Vertex>();
            int count = vertices.Count();
            int nodescount = nodes.Count();
            for (int i = 0; i < count; i++) {
                Vertex vertex = vertices[i];
                for (int j = 0; j < nodescount; j++) {
                    Vertex thenode = nodes[j];
                    if (compareVertex(vertex, thenode)) {
                        result.Add(thenode);
                        break;
                    }
                }
            }
            return result;
        }
        private bool compareVertex(Vertex vertex1, Vertex vertex2)
        {
            if ((Math.Abs(vertex1.x - vertex2.x) < 0.001) && (Math.Abs(vertex1.y - vertex2.y) < 0.001) && (Math.Abs(vertex1.z - vertex2.z) < 0.001))
            {
                return true;
            }
            else { return false; }
        }
    }
}
