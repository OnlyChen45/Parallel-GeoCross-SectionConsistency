using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using GeoCommon;
using TriangleNet;
using SolidModel;
namespace ThreeDModelSystemForSection
{/// <summary>
 /// 这又是啥啊，我都不记得了，算了，哦，原来是LOd2建模，业务层喽
 /// </summary>
    public class SinglePolyWorker
    {
        public SinglePolyWorker() { }
        public static bool IsSinglePoly(string path)
        {
            
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.Open(path, 1);
            Layer layer = ds.GetLayerByIndex(0);
            wkbGeometryType geometryType = layer.GetGeomType();
            if (geometryType != wkbGeometryType.wkbPolygon25D) {
                return false;
            }
            long featurecount = layer.GetFeatureCount(1);
            if (featurecount == 1)
            {
                layer.Dispose();
                ds.Dispose();
                return true;
            }
            layer.Dispose();
            ds.Dispose();
            return false;
        }
        public static BrepModel SinglePolyModeling(string path1,string path2) {
            DataIO dataIO = new DataIO();
            List<Contour> contours1 = dataIO.GetContour(path1);
            DataIO dataIO2 = new DataIO();
            List<Contour> contours2 = dataIO2.GetContour(path2);
            Eage eageA = contours1[0].eageList[0];
            Eage eageB = contours2[0].eageList[0];
            Eage eagemoved = getMovedEage(eageA, eageB);
            int startindex = getNearIndex(eageA.vertexList[0], eagemoved);
            Eage eageBOrder= getEageStartTargetIndex(startindex, eageB);
            ContourHelp contourHelp = new ContourHelp();
            BrepModel brepModel=  contourHelp.getArcModel(eageA, eageBOrder);
            return brepModel;
        }
        
        private static int getNearIndex(Vertex vertex,Eage eage2) {
            double mindis = double.MaxValue;
            double x1, y1, z1, x2, y2, z2;
            x1 = vertex.x;
            y1 = vertex.y;
            z1 = vertex.z;
            int index = -1;
            List<Vertex> vertices = eage2.vertexList;
            int count = vertices.Count;
            for (int i = 0; i < count; i++) {
                Vertex vertext = vertices[i];
                x2 = vertext.x;
                y2 = vertext.y;
                z2 = vertext.z;
                double dis = distance(x1, y1, z1, x2, y2, z2);
                if (dis < mindis) {
                    mindis = dis;
                    index = i;
                }
            }
            return index;
        }
        private static Eage getEageStartTargetIndex(int index,Eage eage) {
            Eage result = new Eage();
            result.name = eage.name;
            int count = eage.vertexList.Count;
            for (int i = index; i < count; i++) {
                Vertex vertex = eage.vertexList[i];
                result.vertexList.Add(vertex);
            }
            for (int i = 0; i < index; i++) {
                Vertex vertex = eage.vertexList[i];
                result.vertexList.Add(vertex);
            }
            return result;
        }
        private static double distance(double x1,double y1,double z1,double x2,double y2,double z2) {
            double dx, dy, dz;
            dx = x2 - x1;
            dy = y2 - y1;
            dz = z2 - z1;
            double result = Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2);
            result = Math.Sqrt(result);
            return result;
        }
        private static Eage getMovedEage(Eage targetEage,Eage oriEage) {
            //吧orieage的中心点挪到target上
            double[] centerxyzTarget = getCenterXYZ(targetEage.vertexList);
            double[] centerxyzOri = getCenterXYZ(oriEage.vertexList);
            double[] vector = new double[3];
            vector[0] = centerxyzTarget[0] - centerxyzOri[0];
            vector[1] = centerxyzTarget[1] - centerxyzOri[1];
            vector[2] = centerxyzTarget[2] - centerxyzOri[2];
            Eage eageresult = new Eage();
            eageresult.name = oriEage.name;
           // eageresult.vertexList.AddRange(oriEage.vertexList);//这里偷偷穿了引用
           foreach(Vertex vk in oriEage.vertexList)
            {
                Vertex vertex = new Vertex();
                vertex.name = vk.name;
                vertex.id = vk.id;
                vertex.x = vk.x;
                vertex.y = vk.y;
                vertex.z = vk.z;
                eageresult.vertexList.Add(vertex);

            }
            int count = eageresult.vertexList.Count;
            for (int i = 0; i < count; i++) {
                eageresult.vertexList[i].x += vector[0];
                eageresult.vertexList[i].y += vector[1];
                eageresult.vertexList[i].z += vector[2];
            }
            return eageresult;
        }
        private static double[] getCenterXYZ(List<Vertex> vertices)
        {
            int count = vertices.Count;
            double xsum = 0, ysum = 0,zsum=0;
            for (int i = 0; i < count; i++)
            {
                xsum += vertices[i].x;
                ysum += vertices[i].y;
                zsum += vertices[i].z;
            }
            double aveX = xsum / count;
            double aveY = ysum / count;
            double aveZ = zsum / count;
            double[] result = { aveX, aveY,aveZ };
            return result;
        }
    }
}
