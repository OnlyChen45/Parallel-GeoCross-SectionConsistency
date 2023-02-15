using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using ThreeDModelSystemForSection;
using SolidModel;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{
    public class BezierMorphingModelWorker {

        public static void Modeling(List<ModelWithArc> modelWithArclist, int transitioncount,out List<ModelWithArc> resultmodels, Dictionary<int, double> bezierdx1, Dictionary<int, double> bezierdy1, Dictionary<int, double> bezierdz1,
        Dictionary<int, double> bezierdx2, Dictionary<int, double> bezierdy2, Dictionary<int, double> bezierdz2) {

            resultmodels = new List<ModelWithArc>();
            foreach (ModelWithArc modelWithArc1 in modelWithArclist) 
            {
                BezierMorphingModel bezierMorphing = new BezierMorphingModel(modelWithArc1,transitioncount, bezierdx1, bezierdy1, bezierdz1, bezierdx2, bezierdy2, bezierdz2);
                ModelWithArc resultmodel = bezierMorphing.getmodel();
                resultmodels.Add(resultmodel);
            }
        
        }
    }
    public class BezierMorphingModel
    {//这个类用来存放，那个那个贝塞尔建模的东西
        ///过程就是输入弧段类。
        ///读取两头，建立贝塞尔曲线随后采样
        ///采样完了之后就开始morphing，得到顺序的轮廓线
        ///顺序轮廓线拿到之后，组成新的eage，主要是需要个名字，然后就可以弧段建模了
        ///建模完了之后拼一起
        ///输出
        Dictionary<int, double> bezierdx1, bezierdy1, bezierdz1;
        Dictionary<int, double> bezierdx2, bezierdy2, bezierdz2;
        ModelWithArc aimmodel;
        int transitioncount;
        public BezierMorphingModel(ModelWithArc modelWithArc,int transitioncount, Dictionary<int, double> bezierdx1, Dictionary<int, double> bezierdy1, Dictionary<int, double> bezierdz1,
        Dictionary<int, double> bezierdx2, Dictionary<int, double> bezierdy2, Dictionary<int, double> bezierdz2) 
        {
            this.bezierdx1 = bezierdx1;
            this.bezierdx2 = bezierdx2;
            this.bezierdy1 = bezierdy1;
            this.bezierdy2 = bezierdy2;
            this.bezierdz1 = bezierdz1;
            this.bezierdz2 = bezierdz2;
            this.aimmodel = modelWithArc;
            this.transitioncount = transitioncount;
        }
        /// <summary>
        /// 执行对一个单独的弧段进行建模的代码
        /// </summary>
        /// <returns></returns>
        public ModelWithArc getmodel() {
            ModelWithArc resultmodel = aimmodel;
            int line1startp = aimmodel.arc1firstpoint;
            int line1endp = aimmodel.arc1endpoint;
            int line2startp = aimmodel.arc2firstpoint;
            int line2endp = aimmodel.arc2endpoint;
            Vertex vline10 = aimmodel.arc1.eage.vertexList[0];
            Vertex vline1n = aimmodel.arc1.eage.vertexList.Last();
            Vertex vline20 = aimmodel.arc2.eage.vertexList[0];
            Vertex vline2n = aimmodel.arc2.eage.vertexList.Last();
            //先是默认找不到贝塞尔参数，因为可能后续表面建模时候会有一些新加点
            Vertex startcontral1 = new Vertex( vline10.x,  vline10.y,  vline10.z);
            Vertex endcontral1 = new Vertex(vline1n.x, vline1n.y, vline1n.z);
            Vertex startcontral2 = new Vertex( vline20.x, vline20.y,  vline20.z);
            Vertex endcontral2 = new Vertex(vline2n.x,  vline2n.y, vline2n.z);
            //读取一下这个，，，这个首尾端点的这个贝塞尔参数，给它变成这个vertex
            if (bezierdx1.ContainsKey(line1startp) == true && bezierdx2.ContainsKey(line2startp) == true) { 
            startcontral1 = new Vertex(bezierdx1[line1startp] + vline10.x, bezierdy1[line1startp] + vline10.y, bezierdz1[line1startp] + vline10.z);
            endcontral1 = new Vertex(bezierdx1[line1endp] + vline1n.x, bezierdy1[line1endp] + vline1n.y, bezierdz1[line1endp] + vline1n.z);
            startcontral2 = new Vertex(bezierdx2[line2startp] + vline20.x, bezierdy2[line2startp] + vline20.y, bezierdz2[line2startp] + vline20.z);
            endcontral2 = new Vertex(bezierdx2[line2endp] + vline2n.x, bezierdy2[line2endp] + vline2n.y, bezierdz2[line2endp] + vline2n.z); }
            List<List<Vertex>> morphingresult = 
                MorphingWorker.GenerateMorphingPoints(aimmodel.arc1.eage.vertexList, aimmodel.arc2.eage.vertexList, startcontral1, endcontral1, startcontral2, endcontral2, transitioncount);
            //执行morphing操作，获得了中间过渡曲线的顶点，
            int linecount = morphingresult.Count;
            List<Eage> eagesformodel = new List<Eage>();
            eagesformodel.Add(aimmodel.arc1.eage);
            //下面拼装曲线
            for (int i = 0; i < linecount; i++) {
                List<Vertex> vertices = morphingresult[i];
                Eage eaget = new Eage();
                eaget.vertexList = vertices;
                eaget.name = "eagename" + i.ToString() + "_";
                int vcount = vertices.Count;
                for (int j = 0; j < vcount; j++) {
                    eaget.vertexList[j].name = eaget.name + j.ToString();
                }
                eagesformodel.Add(eaget);
            }
            eagesformodel.Add(aimmodel.arc2.eage);
            int eagecount = eagesformodel.Count;
            List<BrepModel> breps = new List<BrepModel>();
            for (int i = 0; i < eagecount - 1; i++) {
                Eage eage1 = eagesformodel[i];
                Eage eage2 = eagesformodel[i + 1];
                ContourHelp contourHelp = new ContourHelp();
                BrepModel brepModel = contourHelp.getArcModelNewVersion(eage1, eage2);
                breps.Add(brepModel);
            }
            BrepModel brepModelresult = new BrepModel();
            foreach (var brep in breps) {
                brepModelresult.addBrep(brep);
            }
            resultmodel.setModel(brepModelresult);
            return resultmodel;        
        }
    }


    public class BezierMorphingModelUnique
    {

        /// <summary>
        /// 执行对一个单独的弧段进行建模的代码
        /// </summary>
        /// <returns></returns>
        public BrepModel getmodelTouchGround(ArcSe arc1,ArcSe arc2, int transitioncount,
            double bezdxs1,double bezdys1,double bezdzs1, double bezdxe1, double bezdye1, double bezdze1,
            double bezdxs2, double bezdys2, double bezdzs2, double bezdxe2, double bezdye2, double bezdze2,
            ArcClassWtihSurface arcClass,Geometry startline,Geometry endline
            )
        {
            

            Vertex vline10 =arc1.eage.vertexList[0];
            Vertex vline1n = arc1.eage.vertexList.Last();
            Vertex vline20 = arc2.eage.vertexList[0];
            Vertex vline2n =arc2.eage.vertexList.Last();
            //读取一下这个，，，这个首尾端点的这个贝塞尔参数，给它变成这个vertex
            Vertex startcontral1 = new Vertex(bezdxs1 + vline10.x, bezdys1 + vline10.y, bezdzs1 + vline10.z);
            Vertex endcontral1 = new Vertex(bezdxe1 + vline1n.x, bezdye1 + vline1n.y, bezdze1 + vline1n.z);
            Vertex startcontral2 = new Vertex(bezdxs2 + vline20.x, bezdys2 + vline20.y, bezdzs2 + vline20.z);
            Vertex endcontral2 = new Vertex(bezdxe2 + vline2n.x, bezdye2 + vline2n.y, bezdze2 + vline2n.z);
            //BezierCurve3D bezierCurve1 = new BezierCurve3D(vertices1[0], control1start, control2start, vertices2[0]);
            //FC = new List<Vertex>(bezierCurve1.getVertexsByNumber(bessel_num_collected));
            List<Vertex> FC, LC;
            if (startline.IsEmpty() == true)
            {
                BezierCurve3D bezierCurve1 = new BezierCurve3D(arc1.eage.vertexList[0], startcontral1, startcontral2, arc2.eage.vertexList[0]);
                FC = new List<Vertex>(bezierCurve1.getVertexsByNumber(transitioncount));
            }
            else 
            {
                FC = Geom3DTransToVertexlist(startline);
            }
            if (endline.IsEmpty() == true)
            {
                BezierCurve3D bezierCurve1 = new BezierCurve3D(arc1.eage.vertexList.Last(), endcontral1, endcontral2, arc2.eage.vertexList.Last());
                LC = new List<Vertex>(bezierCurve1.getVertexsByNumber(transitioncount));
            }
            else
            {
                LC = Geom3DTransToVertexlist(endline);
            }
            List<List<Vertex>> morphingresult =
                MorphingWorker.GenerateMorphingPointsWithOutsideControlline(arc1.eage.vertexList, arc2.eage.vertexList,FC,LC,transitioncount);
                //GenerateMorphingPoints(arc1.eage.vertexList, arc2.eage.vertexList, startcontral1, endcontral1, startcontral2, endcontral2, transitioncount);
            //执行morphing操作，获得了中间过渡曲线的顶点，
            int linecount = morphingresult.Count;
            List<Eage> eagesformodel = new List<Eage>();
            eagesformodel.Add(arc1.eage);
            //下面拼装曲线
            for (int i = 0; i < linecount; i++)
            {
                List<Vertex> vertices = morphingresult[i];
                Eage eaget = new Eage();
                eaget.vertexList = vertices;
                eaget.name = "eagename" + i.ToString() + "_";
                int vcount = vertices.Count;
                for (int j = 0; j < vcount; j++)
                {
                    eaget.vertexList[j].name = eaget.name + j.ToString();
                }
                eagesformodel.Add(eaget);
            }
            eagesformodel.Add(arc2.eage);
            int eagecount = eagesformodel.Count;
            List<BrepModel> breps = new List<BrepModel>();
            for (int i = 0; i < eagecount - 1; i++)
            {
                Eage eage1 = eagesformodel[i];
                Eage eage2 = eagesformodel[i + 1];
                ContourHelp contourHelp = new ContourHelp();
                BrepModel brepModel = contourHelp.getArcModelNewVersion(eage1, eage2);
                breps.Add(brepModel);
            }
            BrepModel brepModelresult = new BrepModel();
            foreach (var brep in breps)
            {
                brepModelresult.addBrep(brep);
            }
            
            return brepModelresult;
            List<Vertex> Geom3DTransToVertexlist(Geometry line3d)
            {
                List<Vertex> vertexlistt = new List<Vertex>();
                int pointcountt = line3d.GetPointCount();
                for (int t = 0; t < pointcountt; t++) 
                {
                    double x = line3d.GetX(t);
                    double y = line3d.GetY(t);
                    double z = line3d.GetZ(t);

                    Vertex vertex = new Vertex(x,y,z);
                    vertex.id = t;
                    vertex.name = t.ToString();
                    vertexlistt.Add(vertex);  
                }
                return vertexlistt;
            }
        }
    }
}
