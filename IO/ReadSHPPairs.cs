using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 还是数据读取，草，我怎么写了这么多，tmd，数据IO
    /// </summary>
   public class ReadSHPPairs//这个类是用来读取
    {
        string arc1path, arc2path;
        string point1path, point2path;
        string arcpairstxtpath, pointpairspath;
        public ReadSHPPairs(string arc1path, string arc2path,string point1path, string point2path, string arcpairstxtpath,string pointpairspath) {
            this.arc1path = arc1path;
            this.arc2path = arc2path;
            this.point1path = point1path;
            this.point2path = point2path;
            this.arcpairstxtpath = arcpairstxtpath;
            this.pointpairspath = pointpairspath;
        }
        public void getArsandPairs(out Dictionary<int, Geometry> arcs1, out Dictionary<int, int[]> arc_polys1, out Dictionary<int, int[]> arc_points1,out Dictionary<int, Geometry> points1,
            out Dictionary<int, Geometry> arcs2, out Dictionary<int, int[]> arc_polys2, out Dictionary<int, int[]> arc_points2,out Dictionary<int, Geometry> points2,
            out Dictionary<int,int> arcparis,out Dictionary<int,int> pointpairs) {
            arcparis = readPairsTXT(this.arcpairstxtpath, 1);
            pointpairs = readPairsTXT(this.pointpairspath, 1);
            readArcShp(this.arc1path, out arcs1, out arc_polys1, out arc_points1);
            readArcShp(this.arc2path, out arcs2, out arc_polys2, out arc_points2);
            readPointShp(this.point1path, out points1);
            readPointShp(this.point2path, out points2);
        }
        private Dictionary<int, int> readPairsTXT(string txtpath,int headlinecount) {
            Dictionary<int, int> result = new Dictionary<int, int>();
            FileStream fileStream = new FileStream(txtpath, FileMode.Open);
            StreamReader reader = new StreamReader(fileStream);
            for (int i = 0; i < headlinecount; i++) reader.ReadLine();
            string nextline;
            while (!reader.EndOfStream) {
                nextline = reader.ReadLine();
                string[] s1 = nextline.Split(',');
                int index1 = int.Parse(s1[0]);
                int index2 = int.Parse(s1[1]);
                result.Add(index1, index2);
            }
            return result;
        }
        private void readArcShp(string arcpath,out Dictionary<int,Geometry> arcs,out Dictionary<int,int[]>arc_polys,out Dictionary<int ,int[]>arc_points) {
            Layer arclayer = getfirstlayer(arcpath);
            arcs = new Dictionary<int, Geometry>();
            arc_polys = new Dictionary<int, int[]>();
            arc_points = new Dictionary<int, int[]>();
            long featurecount = arclayer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++) {
                Feature feature = arclayer.GetFeature(i);
                Geometry arc = feature.GetGeometryRef();
                int lineid = feature.GetFieldAsInteger("lineid");
                int[] polys = new int[2];
                int[] points = new int[2];
                polys[0] = feature.GetFieldAsInteger("leftPoly");
                polys[1] = feature.GetFieldAsInteger("rightPoly");
                points[0] = feature.GetFieldAsInteger("point1");
                points[1] = feature.GetFieldAsInteger("point2");
                arcs.Add(lineid, arc);
                arc_polys.Add(lineid, polys);
                arc_points.Add(lineid, points);
            }
            arclayer.Dispose();
        }
        private void readPointShp(string pointspath,out Dictionary<int ,Geometry>points) {
            Layer pointslayer = getfirstlayer(pointspath);
            points = new Dictionary<int, Geometry>();
            long featurecount = pointslayer.GetFeatureCount(1);
            for (int i = 0; i < featurecount; i++) {
                Feature feature = pointslayer.GetFeature(i);
                Geometry point = feature.GetGeometryRef();
                int pointid = feature.GetFieldAsInteger("id");
                points.Add(pointid, point);
            }
            pointslayer.Dispose();
        }
        private Layer getfirstlayer(string shppath) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(shppath,1);
            Layer layer = dataSource.GetLayerByIndex(0);
            return layer;
        }
    }
}
