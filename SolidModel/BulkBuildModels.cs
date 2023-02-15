using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using OSGeo.GDAL;
using OSGeo.OGR;
namespace SolidModel
{
    class BulkBuildModels
    {
        public void bulkBuild(List<double> zarray,Dictionary<double,List<Eage>> eageListPairs,out List<BrepModel> brepresults) {//这个是批量处理的部分
            brepresults = new List<BrepModel>();
            for (int i = 0; i < zarray.Count() - 1; i++) {
                List<Eage> eages1 = eageListPairs[zarray[i]];//拿到相邻的两个eages
                
                List<Eage> eages2 = eageListPairs[zarray[i+1]];
                ContourMapping contourMapping = new ContourMapping();//开始做eage的匹配
                contourMapping.runMapping(eages1, eages2);
                List<Eage[]> eagepairs = new List<Eage[]>();
                contourMapping.oneToOneEageWork(out eagepairs);//最终得到一个Eage[2]组成的List，每个Eage[2]保证两两相对组成模型
                for (int j = 0; j < eagepairs.Count(); j++) {//模型建模然后加入到结果List中
                    Eage[] eagearray = eagepairs[j];
                    ContourHelp contourHelp = new ContourHelp();
                    BrepModel brep = contourHelp.ContourSingleToSingle(ref eagearray[0], ref eagearray[1], true, false);
                    brepresults.Add(brep);
                }
            }

        }
    }
    class ContourMapping {//这个是用来做eage的配对的过程
        public double Az;
        public double Bz;
        public int[,] Atype;
        public int[,] Btype;
        public  List<Eage> eagesA;
        public  List<Eage> eagesB;
        private bool MyContain(Geometry geom1, Geometry geom2) {//测试geom1是否包含geom2，主要是因为Geometry.Contain在边界重叠是返回值为false，不能采用
            double area2 = geom2.Area();
            Geometry geominter = geom2.Intersection(geom1);
            double areaI = geominter.Area();
            if ((areaI / area2) > 0.9999)
            {
                return true;
            }
            else { return false; }
        }
        public void runMapping(List<Eage> eageListA,List<Eage>eageListB) {
            this.eagesA = eageListA;
            this.eagesB = eageListB;
            Atype = new int[eageListA.Count(), 3];
            Btype = new int[eageListB.Count(), 3];
            Geometry[] Ageom = new Geometry[eageListA.Count()];
            Geometry[] Bgeom = new Geometry[eageListB.Count()];
            //这两个主要是记录每个contour与另一个图层的关系的，0,2 暂时不用，1为within,即当前这个图形在哪个图形内。
            for (int i = 0; i < eageListA.Count(); i++)
            {
                Geometry geom1 = new Geometry(wkbGeometryType.wkbLinearRing);//新建一个可以eage转过去的2Dgeometry，然后利用拓扑计算判断是否重叠
                Geometry geom2 = new Geometry(wkbGeometryType.wkbPolygon);
                List<Vertex> vertices = eageListA[i].vertexList;
                for (int j = 0; j < vertices.Count(); j++)
                {
                    Vertex vertex = vertices[j];
                    geom1.AddPoint_2D(vertex.x, vertex.y);

                }
                geom1.AddPoint_2D(vertices[0].x, vertices[0].y);//注意，根据Eage的代码来看，它是不像Ring一样需要首尾坐标一致的。所以要把它第一个Vertex最后加到末尾
                geom2.AddGeometry(geom1);
                Ageom[i] = geom2;
            }
            for (int i = 0; i < eageListB.Count(); i++)
            {
                Geometry geom1 = new Geometry(wkbGeometryType.wkbLinearRing);
                Geometry geom2 = new Geometry(wkbGeometryType.wkbPolygon);
                List<Vertex> vertices = eageListB[i].vertexList;
                for (int j = 0; j < vertices.Count(); j++)
                {
                    Vertex vertex = vertices[j];
                    geom1.AddPoint_2D(vertex.x, vertex.y);

                }
                geom1.AddPoint_2D(vertices[0].x, vertices[0].y);
                geom2.AddGeometry(geom1);
                Bgeom[i] = geom2;
            }
            //给存储A Btype的数组初始化
            for (int i = 0; i < eageListA.Count(); i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Atype[i, j] = -1;
                }
            }
            for (int i = 0; i < eageListB.Count(); i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Btype[i, j] = -1;
                }
            }

            for (int i = 0; i < Ageom.Length; i++) {
                for (int j = 0; j < Bgeom.Length; j++) {
                    Geometry  geom1 = Ageom[i];
                    Geometry geom2 = Bgeom[j];
                    if (MyContain(geom2,geom1))
                    {//geom2.Contains(geom1)不行
                        Atype[i,1] = j;
                    }
                }

            }
            for (int i = 0; i < Bgeom.Length; i++)
            {
                for (int j = 0; j < Ageom.Length; j++)
                {
                    Geometry geom1 = Bgeom[i];
                    Geometry geom2 = Ageom[j];
                    if (MyContain(geom2, geom1)) {
                        Btype[i, 1] = j;
                    }
                }

            }
        }
        public void oneToOneEageWork(out List<Eage[]> oneToOneEage) {
            oneToOneEage = new List<Eage[]>();
            for (int i = 0; i < eagesA.Count(); i++) {
                int count = 0;
                Eage theeage = eagesA[i];
                List<Eage> contian = new List<Eage>();
                for (int j = 0; j < eagesB.Count(); j++) {
                    if (Btype[j, 1] == i) {//在这，查找一下b中所有包含在当前eage的eage 如果为1，那么就是1对1，如果大于1 ，那么就是1对多，如果为0，那么说明当前eage被包含。
                        count++;
                        contian.Add(eagesB[j]);
                    }

                }
              //  Console.WriteLine(count);
                if (count == 1) { Eage[] eagearray = new Eage[2];
                    eagearray[0] = theeage;
                    eagearray[1] = contian[0];
                    oneToOneEage.Add(eagearray);
                }
                if (count > 1) {
                    SplitEageByVoronoi splitEage = new SplitEageByVoronoi();
                    List<Eage[]> tempeages = splitEage.SplitEageByVor(contian, theeage);
                    oneToOneEage.AddRange(tempeages);

                }
            }
            for (int i = 0; i < eagesB.Count(); i++)
            {
                int count = 0;
                Eage theeage = eagesB[i];
                List<Eage> contian = new List<Eage>();
                for (int j = 0; j < eagesA.Count(); j++)
                {
                    if (Atype[j, 1] == i)
                    {//在这，查找一下b中所有包含在当前eage的eage 如果为1，那么就是1对1，如果大于1 ，那么就是1对多，如果为0，那么说明当前eage被包含。
                        count++;
                        contian.Add(eagesA[j]);
                    }

                }
               // Console.WriteLine(count);
                if (count == 1)
                {
                    Eage[] eagearray = new Eage[2];
                    eagearray[0] = theeage;
                    eagearray[1] = contian[0];
                    oneToOneEage.Add(eagearray);
                }
                if (count > 1)
                {
                    SplitEageByVoronoi splitEage = new SplitEageByVoronoi();
                    List<Eage[]> tempeages = splitEage.SplitEageByVor(contian, theeage);
                    oneToOneEage.AddRange(tempeages);

                }
            }
        }

    }
}
