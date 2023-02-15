using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using OpenCvSharp;
using MathNet.Numerics.LinearAlgebra;//这个玩意儿是牛牛的矩阵计算类
namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// 切分地层的工具，
    /// </summary>
  public  class SplitStrataToolbox
    {
        //制作一些必要的工具
        //首先明确思路
        //第一，需要用哪些面去填充
        public static Dictionary<int, Geometry> getFullGeoms(Dictionary<int,Geometry> oriGeoms,out MinOutsourceRect rect ,double boundaryTolerate) {//这个工具是用来获取可以拼成大的矩形的可以分裂开的这个geom的
            //boundaryTolerate是给增密rect用的，这个就是基本的步长值，距离这么远就增加一个点
            Dictionary<int, List<double[]>> xydic = new Dictionary<int, List<double[]>>();
            List<double[]> xylistall = new List<double[]>();

            foreach (var vk in oriGeoms) {
                int id = vk.Key;
                Geometry geom = vk.Value;
                List<double[]> xylist;
                setPolyIntoList(geom, out xylist);
                xydic.Add(id, xylist);
                xylistall.AddRange(xylist);
            }
            //  double[] rectxy = getMinAreaRectByCv2(xylistall, out rotatedRect);//获得了最小外接矩形的xy坐标
            MinOutsourceRect minOutsourceRect = MinOutRectBuilder.buildMinOutRect(xylistall);
            rect = minOutsourceRect;
            double[] rectxy = minOutsourceRect.points();
            Geometry uniongeom = getUnionPoly(oriGeoms.Values.ToList<Geometry>());//获取所有的面的合体
            Geometry unionBoundary = uniongeom.GetBoundary();
            List<double[]> xyOutlines;
            setPolyIntoList(uniongeom, out xyOutlines);//获得最外缘的坐标
            List<double[]> rectxydenselist ;
            getRectDense(rectxy, xyOutlines, out rectxydenselist, boundaryTolerate);
            //下面做delaunay剖分
            List<TriangleNet.Geometry.Vertex> xyoutVertex = makexyListToTrangleNetVertex(xyOutlines);
            List<TriangleNet.Geometry.Vertex> xyrectVertex = makexyListToTrangleNetVertex(rectxydenselist);
            TriangleNet.Mesh mesh= GetTriMesh(xyrectVertex,xyoutVertex);//这里前后顺序不能变，一个是外边界，一个是内边界
            List<TriangleNet.Topology.Triangle> triangles = mesh.Triangles.ToList<TriangleNet.Topology.Triangle>();
            List<Geometry> triangleGeoms;
            saveTrangelesToGeoms(triangles, out triangleGeoms);
            Dictionary<Geometry, Geometry> centerPoint_Triangles;
            //下面按照最近距离法，把所有的
            //实践证明最近距离法不行，还是要用大分区的方式，就从union的大geom的最中心出发，连接边界线上各交界点，然后形成大的区块
            createCenterPointDic(triangleGeoms, out centerPoint_Triangles);
            //下面就是把所有中心点在boundary内的移除，他们没用
            removeCenterInGeom(ref centerPoint_Triangles, uniongeom);
            //现在就剩下了有用的了，把它们分类
            Dictionary<int, List<Geometry>> trianglesclassify;
            //trianglesClassifyWorker(out trianglesclassify, oriGeoms, uniongeom, centerPoint_Triangles, rectxydenselist);
            trianglesClassifyWokerNearSegment(out trianglesclassify, oriGeoms, uniongeom, centerPoint_Triangles);
           //string savetrianglepath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\temp7.shp";
        //  string spatialpath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\finalbuffers2Change_LTP.shp";
           //saveTriangles(trianglesclassify, savetrianglepath, spatialpath);
            //分类好了，然后把所有的面合成就好了
            Dictionary<int, Geometry> fullpolys;
            makefullpolys(trianglesclassify, oriGeoms, out fullpolys);
            int[] polycount = new int[oriGeoms.Count];
            int tttt = 0;
            foreach (var t in fullpolys) {
                Geometry geom = t.Value;
                polycount[tttt++] = geom.GetGeometryCount();
            }
            return fullpolys;
        }
        public static Dictionary<int, Geometry> getFullGeoms(Dictionary<int, Geometry> oriGeoms, out MinOutsourceRect rect, double boundaryTolerate,double xbuffer,double ybuffer)
        {//这个工具是用来获取可以拼成大的矩形的可以分裂开的这个geom的
            //boundaryTolerate是给增密rect用的，这个就是基本的步长值，距离这么远就增加一个点
            Dictionary<int, List<double[]>> xydic = new Dictionary<int, List<double[]>>();
            List<double[]> xylistall = new List<double[]>();

            foreach (var vk in oriGeoms)
            {
                int id = vk.Key;
                Geometry geom = vk.Value;
                List<double[]> xylist;
                setPolyIntoList(geom, out xylist);
                xydic.Add(id, xylist);
                xylistall.AddRange(xylist);
            }
            //  double[] rectxy = getMinAreaRectByCv2(xylistall, out rotatedRect);//获得了最小外接矩形的xy坐标
            MinOutsourceRect minOutsourceRect = MinOutRectBuilder.buildMinOutRect(xylistall);
            rect = minOutsourceRect;
            // double[] rectxy = minOutsourceRect.points();
            double[] rectxy = minOutsourceRect.bufferpoints(xbuffer,ybuffer);
            Geometry uniongeom = getUnionPoly(oriGeoms.Values.ToList<Geometry>());//获取所有的面的合体
            Geometry unionBoundary = uniongeom.GetBoundary();
            List<double[]> xyOutlines;
            setPolyIntoList(uniongeom, out xyOutlines);//获得最外缘的坐标
            List<double[]> rectxydenselist;
            getRectDense(rectxy, xyOutlines, out rectxydenselist, boundaryTolerate);
            //下面做delaunay剖分
            List<TriangleNet.Geometry.Vertex> xyoutVertex = makexyListToTrangleNetVertex(xyOutlines);
            List<TriangleNet.Geometry.Vertex> xyrectVertex = makexyListToTrangleNetVertex(rectxydenselist);
            TriangleNet.Mesh mesh = GetTriMesh(xyrectVertex, xyoutVertex);//这里前后顺序不能变，一个是外边界，一个是内边界
            List<TriangleNet.Topology.Triangle> triangles = mesh.Triangles.ToList<TriangleNet.Topology.Triangle>();
            List<Geometry> triangleGeoms;
            saveTrangelesToGeoms(triangles, out triangleGeoms);
            Dictionary<Geometry, Geometry> centerPoint_Triangles;
            //下面按照最近距离法，把所有的
            //实践证明最近距离法不行，还是要用大分区的方式，就从union的大geom的最中心出发，连接边界线上各交界点，然后形成大的区块
            createCenterPointDic(triangleGeoms, out centerPoint_Triangles);
            //下面就是把所有中心点在boundary内的移除，他们没用
            removeCenterInGeom(ref centerPoint_Triangles, uniongeom);
            //现在就剩下了有用的了，把它们分类
            Dictionary<int, List<Geometry>> trianglesclassify;
            //trianglesClassifyWorker(out trianglesclassify, oriGeoms, uniongeom, centerPoint_Triangles, rectxydenselist);
            trianglesClassifyWokerNearSegment(out trianglesclassify, oriGeoms, uniongeom, centerPoint_Triangles);
          //  string savetrianglepath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\temp8.shp";
           // string spatialpath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\finalbuffers2Change_LTP.shp";
            //saveTriangles(trianglesclassify, savetrianglepath, spatialpath);
            //分类好了，然后把所有的面合成就好了
            Dictionary<int, Geometry> fullpolys;
            makefullpolys(trianglesclassify, oriGeoms, out fullpolys);
            int[] polycount = new int[oriGeoms.Count];
            int tttt = 0;
            foreach (var t in fullpolys)
            {
                Geometry geom = t.Value;
                polycount[tttt++] = geom.GetGeometryCount();
            }
            return fullpolys;
        }
        private static void saveTriangles(Dictionary<int, List<Geometry>> trianglesClassfy,string path,string spatialpath) {
            //测试用，保存三角形，观察用的代码
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            DataSource spatialds = driver.Open(spatialpath, 1);
            Layer spatiallayer = spatialds.GetLayerByIndex(0);
            SpatialReference spatialReference = spatiallayer.GetSpatialRef();
            Layer layer = dataSource.CreateLayer("points", spatialReference, wkbGeometryType.wkbPolygon, null);
            FieldDefn fieldDefn = new FieldDefn("polyid", FieldType.OFTInteger);
            layer.CreateField(fieldDefn,1);
            foreach (var vk in trianglesClassfy) {
                int id = vk.Key;
                List<Geometry> geoms = vk.Value;
                int count2 = geoms.Count;
                Feature feature = new Feature(layer.GetLayerDefn());
                for (int i = 0; i < count2; i++) {
                    feature.SetField("polyid", id);
                    Geometry geometry111 = geoms[i];
                    feature.SetGeometry(geometry111);
                    layer.CreateFeature(feature);
                }
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public static Dictionary<int, Geometry> geomSplit(Dictionary<int,Geometry>fullpolys,Geometry staraGeom) {
            //采用已经满了的面切分目标面
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in fullpolys) {
                int id = vk.Key;
                Geometry splitgeom = vk.Value;
                string wkt,wkt2;
                splitgeom.ExportToWkt(out wkt);
                staraGeom.ExportToWkt(out wkt2);
                Geometry splitresult = staraGeom.Intersection(splitgeom);
                result.Add(id, splitresult);
            }
            return result;
        }
        public static Dictionary<int, Geometry> geomDicTrans(Dictionary<int,Geometry> geomdic,Matrix<double> transMat) {
            //对于个字典保存geometry类型，让它完成转换
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in geomdic) {
                int id = vk.Key;
                Geometry geometry = getTransGeom(vk.Value, transMat);
                result.Add(id, geometry);
            }
            return result;
        }
        public static Geometry getTransGeom(Geometry geom,Matrix<double > transMat) {
            //对于单独的一个geom，探查它的类型，然后加以转换
            wkbGeometryType geomType = geom.GetGeometryType();
            Geometry result = new Geometry(geomType);
            Geometry woker = geom;
            Geometry collectGeom = new Geometry(geomType);
            if (geomType == wkbGeometryType.wkbPolygon) {
                woker = woker.GetGeometryRef(0);
                collectGeom = new Geometry(wkbGeometryType.wkbLinearRing);
            }
            int pointcount = woker.GetPointCount();
            for (int i = 0; i < pointcount; i++) {
                double x = woker.GetX(i);
                double y = woker.GetY(i);
                double[] xy;
                transXY(transMat, x, y, out xy);
                collectGeom.AddPoint_2D(xy[0], xy[1]);
            }
            if (geomType == wkbGeometryType.wkbPolygon)
            {
                result.AddGeometry(collectGeom);
            }
            else { result = collectGeom; }
            return result;
        } 
        public static void transXY(Matrix<double> transMat, double x, double y, out double[] XY)
        {//转换
            double[,] xysite = { { x }, { y }, { 1 } };
            var mb = Matrix<double>.Build;
            var xyM = mb.DenseOfArray(xysite);
            var XYM = transMat * xyM;
            double[,] columnXYZ = XYM.ToArray();
            XY = new double[2];
            XY[0] = columnXYZ[0, 0];
            XY[1] = columnXYZ[1, 0];
        }
       /* public static Matrix<double> getTransMatWorker(MinOutsourceRect rotatedRectOri ,Dictionary<int,Geometry> geoms) {
            List<double[]> xylist;
            Geometry geomTarget = getUnionPoly(geoms.Values.ToList<Geometry>());
            setPolyIntoList(geomTarget,out xylist);
            MinOutsourceRect rotatedRectTarget;
            //getMinAreaRectByCv2(xylist, out rotatedRectTarget);
            rotatedRectTarget = MinOutRectBuilder.buildMinOutRect(xylist);
            Matrix<double> resultMat = getTransMat(rotatedRectOri, rotatedRectTarget);
            return resultMat;
        }*/
        public static Matrix<double> getTransMatWorker(MinOutsourceRect rotatedRectOri, Geometry geomTarget)
        {
            List<double[]> xylist;
         //   Geometry geomTarget = getUnionPoly(geoms.Values.ToList<Geometry>());
            setPolyIntoList(geomTarget, out xylist);
            MinOutsourceRect rotatedRectTarget;
            //getMinAreaRectByCv2(xylist, out rotatedRectTarget);
            rotatedRectTarget = MinOutRectBuilder.buildMinOutRect(xylist);
            Matrix<double> resultMat = getTransMat(rotatedRectOri, rotatedRectTarget);
            return resultMat;
        }
        public static Matrix<double> getTransMatWorker(MinOutsourceRect rotatedRectOri, Geometry geomTarget,double xbuffer,double ybuffer)
        {
            List<double[]> xylist;
            //   Geometry geomTarget = getUnionPoly(geoms.Values.ToList<Geometry>());
            setPolyIntoList(geomTarget, out xylist);
            MinOutsourceRect rotatedRectTarget;
            //getMinAreaRectByCv2(xylist, out rotatedRectTarget);
            rotatedRectTarget = MinOutRectBuilder.buildMinOutRect(xylist);
            Matrix<double> resultMat = getTransMat(rotatedRectOri, rotatedRectTarget,xbuffer,ybuffer);
            return resultMat;
        }
        public static Matrix<double> getTransMat(MinOutsourceRect rotateRectOri, MinOutsourceRect rotatedRectTarget, double xbuffer = 0, double ybuffer = 0) {
            double[] rectPointsOri, rectPointsTarget;
            if (xbuffer == 0)
            {
                rectPointsOri = getRotateRectPoints(rotateRectOri);
            }
            else{
                rectPointsOri = getRotateRectPoints(rotateRectOri,xbuffer,ybuffer);
            }
            if (xbuffer == 0)
            {
                rectPointsTarget = getRotateRectPoints(rotatedRectTarget);
            }
            else { rectPointsTarget = getRotateRectPoints(rotatedRectTarget,xbuffer,ybuffer); }
            double[,] transTo00 = { {1,0,-rectPointsOri[0] },//转到原点
                                    {0,1,-rectPointsOri[1]},
                                    {0,0,1 } };
            double[,] transToTarget = { {1,0,rectPointsTarget[0] },//转到目标位置
                                    {0,1,rectPointsTarget[1]},
                                    {0,0,1 } };
            double angeleori = rotateRectOri.angle;//角度参数angle 是矩形最下面的点（y坐标最大）P[0]发出的平行于x轴的射线，逆时针旋转，与碰到的第一个边的夹角（这个边的边长就作为width），取值范围[-90~0]。
            angeleori = (-angeleori / 180) * Math.PI;//角度转弧度
            double angletarget = rotatedRectTarget.angle;
            angletarget = (-angletarget / 180) * Math.PI;//角度转弧度
             double   theta=-angeleori;

            double[,] transRotateToX = { { Math.Cos(theta),-Math.Sin(theta),0},
                                            {Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1} };
            theta = angletarget;
            double[,] transRotateToTarget = { { Math.Cos(theta),-Math.Sin(theta),0},
                                            {Math.Sin(theta),Math.Cos(theta),0 },
                                            { 0,0,1} };
            //下面写一个比例变换

            double oriwidth, oriheigh, tarwidth, tarheigh;
            oriwidth = rotateRectOri.width;
            oriheigh = rotateRectOri.heigh;
            tarwidth = rotatedRectTarget.width;
            tarheigh = rotatedRectTarget.heigh;
            double sx = tarwidth / oriwidth;
            double sy = tarheigh / oriheigh;
            double[,] transproportion = { {sx,0,0 },//比例变换
                                        {0,sy,0},
                                        {0,0,1 } };
            //现在就是制作旋转矩阵了
            var mb = Matrix<double>.Build;
            var transTo00M = mb.DenseOfArray(transTo00);
            var transRotateToXM = mb.DenseOfArray(transRotateToX);
            var transproportionM = mb.DenseOfArray(transproportion);
            var transRotateToTargetM = mb.DenseOfArray(transRotateToTarget);
            var transToTargetM = mb.DenseOfArray(transToTarget);
            Matrix<double> result = transToTargetM * transRotateToTargetM * transproportionM * transRotateToXM * transTo00M;
            return result;
        }
        private static void makefullpolys(Dictionary<int, List<Geometry>> trianglesclassify, Dictionary<int, Geometry> polys,out Dictionary<int,Geometry> fullpolys) {
            fullpolys = new Dictionary<int, Geometry>();
            foreach (var vk in polys) {
                int id = vk.Key;
                Geometry polyori = vk.Value;
                List<Geometry> geomclass = new List<Geometry>();//防止传引用
                geomclass.AddRange(trianglesclassify[id]);
                //这种方式非常容易产生自相交。那么就用复杂一点的算法，
                /*foreach (Geometry geom in geomclass) {
                    string wkt,wkto;
                    geom.ExportToWkt(out wkt);
                    try
                    {
                        polyori = polyori.Simplify(0.01);
                        polyori = polyori.Union(geom);
                    }
                   catch {
                        //string = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\temp1";
                       // OSGeo.OGR.Driver driver=new 
                       // polyori = Ogr.ForceToPolygon(polyori);
                        polyori.ExportToWkt(out wkto);
                        Console.WriteLine(wkto);
                        polyori = polyori.Union(geom);}
                    if (polyori.GetGeometryType() == wkbGeometryType.wkbMultiPolygon) {
                        Console.WriteLine("Catch!");
                        polyori= Ogr.ForceToPolygon(polyori);
                        int countgeom = polyori.GetGeometryCount();
                        if (countgeom > 1) {
                            polyori = Ogr.ForceToMultiPolygon(polyori);
                        }
                        wkbGeometryType type11 = polyori.GetGeometryType();
                        Console.WriteLine("catch 111");
                    }
                }*/

                //刚才的直接union的算法被证明，可能太容易出现自相交啥的(并不是，主要是之前没有去掉过于相近的点)，应该按照是否相连来做
                int geomcount = geomclass.Count;
                while (geomcount > 0) {
                    for (int i = 0; i < geomcount; i++) {
                        Geometry geom = geomclass[i];
                        if (polyori.Intersect(geom)) {
                            Geometry geomtemp = polyori.Intersection(geom);
                            if (!(geomtemp.GetGeometryType() == wkbGeometryType.wkbPoint)) {
                                polyori = polyori.Union(geom);
                                geomclass.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    //经过多次实验，无论如何都无法消除重复的悬挂点，，，这个就很麻烦了
                    geomcount = geomclass.Count;
                }
                fullpolys.Add(id, polyori);
            }
        }

        //考虑到直接分问题还是比较大，所以还是采用最近邻方式进行分，下面这个方法废弃掉
        private static void trianglesClassifyWorker(out Dictionary<int, List<Geometry>> trianglesclassify, Dictionary<int, Geometry> polys, Geometry uniongeom, Dictionary<Geometry, Geometry> centerPoint_Triangles, List<double[]> rectxydenselist)
        {
            List<double[]> xylistoutline;
            //Geometry unionGeom=
            setPolyIntoList(uniongeom, out xylistoutline);
            trianglesclassify = new Dictionary<int, List<Geometry>>();
            Dictionary<int, List<Geometry>> boundaryPoints = new Dictionary<int, List<Geometry>>();
            Dictionary<int, List<Geometry>> cutPointsDic = new Dictionary<int, List< Geometry>>();
            foreach (var vk in polys)
            {
                List<Geometry> geomnew = new List<Geometry>();
                trianglesclassify.Add(vk.Key, geomnew);
                geomnew = new List<Geometry>();
                boundaryPoints.Add(vk.Key, geomnew);
                geomnew = new List<Geometry>();
                cutPointsDic.Add(vk.Key, geomnew);
            }

            int count = xylistoutline.Count;
            //string savepathpoints = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\temp\outlinepoints1.shp";
            //string spatialpath = @"D:\研究生项目\弧段轮廓线建模\弧段建模LoD2\workspace\finalbuffers2Change_LTP.shp";
            //savePoints(xylistoutline, savepathpoints, spatialpath);
            //首先获取了边界上点，以及边界上的被分割的点
            for (int i = 0; i < count; i++)
            {
                double[] xy = xylistoutline[i];
                Geometry point = new Geometry(wkbGeometryType.wkbPoint);
                point.AddPoint_2D(xy[0], xy[1]);
                int intersectid = -1;
                int intercount = 0;
                List<int> keylist = new List<int>();
                foreach (var vk2 in polys)
                {
                    Geometry poly = vk2.Value;
                    Geometry bound = poly.Boundary();
                    string wkt11;
                    point.ExportToWkt(out wkt11);
                    Console.WriteLine(wkt11);
                    bool inter = point.Intersect(bound);
                    //bool inter = point.Intersect(poly);
                    if (inter)
                    {
                        intersectid = vk2.Key;
                        keylist.Add(vk2.Key);
                        intercount++;
                    }
                }
                if (intercount == 1)
                {
                    boundaryPoints[intersectid].Add(point);
                }
                if (intercount == 2) {
                    cutPointsDic[keylist[0]].Add(point);
                    cutPointsDic[keylist[1]].Add(point);
                }
            }
            //这个cutpointdic实际上是按照poly名称来分的，所以可以一下子得到分割点。目前来看最好是从矩形上找到距离分割点最近的点然后进行连线
        }
        private static void savePoints(List<double []>xylist,string path ,string spatialpath ) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            DataSource spatialds = driver.Open(spatialpath, 1);
            Layer spatiallayer = spatialds.GetLayerByIndex(0);
            SpatialReference spatialReference = spatiallayer.GetSpatialRef();
            Layer layer = dataSource.CreateLayer("points", spatialReference, wkbGeometryType.wkbPoint, null);
            int count = xylist.Count();
            Feature feature = new Feature(layer.GetLayerDefn());
            for (int i = 0; i < count; i++) {
                double[] xy = xylist[i];
                Geometry point = new Geometry(wkbGeometryType.wkbPoint);
                point.AddPoint_2D(xy[0], xy[1]);
                feature.SetGeometry(point);
                layer.CreateFeature(feature);
            }
            spatiallayer.Dispose();
            spatialds.Dispose();
            layer.Dispose();
            dataSource.Dispose();
        }
        private static void trianglesClassifyWokerNearSegment(out Dictionary<int, List<Geometry>> trianglesclassify, Dictionary<int, Geometry> polys, Geometry uniongeom, Dictionary<Geometry, Geometry> centerPoint_Triangles) {
            //首先是筛选出在union边界上的所有的这个多边形的点
            //再用每个三角形的中心点和他们做距离，找到最近的，就分好了类
            List<double[]> xylistoutline;
            setPolyIntoList(uniongeom, out xylistoutline);
            trianglesclassify = new Dictionary<int, List<Geometry>>();
            Dictionary<int, List<double[]>> boundaryPoints = new Dictionary<int, List<double[]>>();
            foreach (var vk in polys)//初始化
            {
                //List<double[]> xylistnew = new List<double[]>();
                List<Geometry> geomnew = new List<Geometry>();
                trianglesclassify.Add(vk.Key, geomnew);
                //xylistnew = new List<double[]>();
                //boundaryPoints.Add(vk.Key, xylistnew);
            }
            Geometry boundryunion = uniongeom.Boundary();
            foreach (var vk in polys) {
                Geometry geomtemp = vk.Value;
                bool touches = geomtemp.Intersect(boundryunion);
                if (touches == false) {
                    continue;
                }
                Geometry touchline = geomtemp.Intersection(boundryunion);
                touchline = Ogr.ForceToLineString(touchline);
                wkbGeometryType geomtype = touchline.GetGeometryType();
                if (geomtype == wkbGeometryType.wkbMultiLineString) {
                    touchline = Ogr.ForceToLineString(touchline);
                }
                List<double[]> linexylist;
                setLineStringIntoList(touchline, out linexylist);
                boundaryPoints.Add(vk.Key, linexylist);
            }
            
            foreach (var vk in centerPoint_Triangles)
            {
                Geometry centerpoint = vk.Key;
                int nearid = checkPointNearestSegment(centerpoint, boundaryPoints);
                trianglesclassify[nearid].Add(vk.Value);
            }
        }
        private static int checkPointNearestSegment(Geometry point, Dictionary<int, List<double[]>> boundaryPoints)
        {
            int nearId = -1;
            double x1 = point.GetX(0);
            double y1 = point.GetY(0);
            double mindis = double.MaxValue;

            foreach (var vk in boundaryPoints)
            {
                int id = vk.Key;
                List<double[]> xylist = vk.Value;
                int xycount = xylist.Count;
                for (int j = 0; j < xycount-1; j++) {
                    double[] xy1 = xylist[j];
                    double[] xy2 = xylist[j + 1];
                    double dis = distanceFromPointToSegment(x1, y1, xy1[0], xy1[1], xy2[0], xy2[1]);
                    if (mindis > dis)
                    {
                        mindis = dis;
                        nearId = id;
                    }
                }
            }
            return nearId;
        }
        //最近点法，已经不好用了，废弃
        private static void trianglesClassifyWorkerNear(out Dictionary<int, List<Geometry>> trianglesclassify, Dictionary<int, Geometry> polys, Geometry uniongeom, Dictionary<Geometry, Geometry> centerPoint_Triangles) {
           
            //首先是筛选出在union边界上的所有的这个多边形的点
            //再用每个三角形的中心点和他们做距离，找到最近的，就分好了类
            List<double[]> xylistoutline;
            setPolyIntoList(uniongeom, out xylistoutline);
            trianglesclassify = new Dictionary<int, List<Geometry>>();
            Dictionary<int, List<Geometry>> boundaryPoints = new Dictionary<int, List<Geometry>>();
            foreach (var vk in polys) {
                List<Geometry> geomnew = new List<Geometry>();
                trianglesclassify.Add(vk.Key, geomnew);
                geomnew = new List<Geometry>();
                boundaryPoints.Add(vk.Key, geomnew);
            }
            

            int count = xylistoutline.Count;
            for (int i = 0; i < count; i++) {
                double[] xy = xylistoutline[i];
                Geometry point = new Geometry(wkbGeometryType.wkbPoint);
                point.AddPoint_2D(xy[0], xy[1]);
                int intersectid = -1;
                int intercount = 0;
                foreach (var vk2 in polys) {
                    Geometry poly = vk2.Value;
                    bool inter = point.Intersect(poly);
                    if (inter) {
                        intersectid = vk2.Key;
                        intercount++;
                    }
                }
                if (intercount == 1) {
                    boundaryPoints[intersectid].Add(point);
                }
            }
            foreach (var vk in centerPoint_Triangles) {
                Geometry centerpoint = vk.Key;
                int nearid = checkPointNearest(centerpoint, boundaryPoints);
                trianglesclassify[nearid].Add(vk.Value);
            }

        }
        private static int checkPointNearest(Geometry point, Dictionary<int, List<Geometry>> boundaryPoints) {
            int nearId = -1;
            double x1 = point.GetX(0);
            double y1 = point.GetY(0);
            double mindis = double.MaxValue;
            
            foreach (var vk in boundaryPoints) {
                int id = vk.Key;
                List<Geometry> pointsOnBound = vk.Value;

                foreach (Geometry geom in pointsOnBound) {
                    double dis = distance(x1, y1, geom.GetX(0), geom.GetY(0));
                    if (mindis > dis) {
                        mindis = dis;
                        nearId = id;
                    }
                }
            }
            return nearId;
        }

        //写个点到线段的距离算法
        public static double distanceFromPointToSegment(double x,double y,double x1,double y1,double x2,double y2) {
            double dxAP = x - x1;
            double dyAP = y - y1;
            double dxAB = x2 - x1;
            double dyAB = y2 - y1;
            double disAB = distance(x1, y1, x2, y2);
            double r = dxAP * dxAB + dyAP * dyAB;
            r = r / Math.Pow(disAB,2);
            if (r <= 0) {
                return distance(x, y, x1, y1);
            }
            if (r >= 1) {
                return distance(x, y, x2, y2);
            }
            double px = x1 + (x2 - x1) * r;
            double py = y1 + (y2 - y1) * r;
            return distance(x,y,px,py);
        }

        private static void removeCenterInGeom(ref Dictionary<Geometry, Geometry> centerPoint_Polys, Geometry biggeom) {
            List<Geometry> needtoRemove = new List<Geometry>();
            foreach (var vk in centerPoint_Polys) {
                Geometry centerpoint = vk.Key;
                bool inter = centerpoint.Intersect(biggeom);
                if (inter) {
                    needtoRemove.Add(centerpoint);
                }
            }
            foreach (Geometry tempgeom in needtoRemove) {
                centerPoint_Polys.Remove(tempgeom);
            }
        }
        private static void createCenterPointDic(List<Geometry> polys,out Dictionary<Geometry, Geometry> centerPoint_Polys) {
            centerPoint_Polys = new Dictionary<Geometry, Geometry>();
            foreach (Geometry poly in polys) {
                Geometry ring = poly.GetGeometryRef(0);
                int pointcount = ring.GetPointCount();
                double xsum = 0, ysum = 0;
                for (int i = 0; i < pointcount; i++) {
                    double x, y;
                    x = ring.GetX(i);
                    y = ring.GetY(i);
                    xsum += x;
                    ysum += y;
                }
                double centerx = xsum / pointcount;
                double centery = ysum / pointcount;
                Geometry centerpoint = new Geometry(wkbGeometryType.wkbPoint);
                centerpoint.AddPoint_2D(centerx, centery);
                centerPoint_Polys.Add(centerpoint, poly);
            }
        }
        private static void saveTrangelesToGeoms(List<TriangleNet.Topology.Triangle> triangles,out List<Geometry> triangleGeom) {
            //把这个triangle转成geom
            int count = triangles.Count;
            triangleGeom = new List<Geometry>(); 
            for (int i = 0; i < count; i++) {
                TriangleNet.Topology.Triangle triangle = triangles[i];
                List<TriangleNet.Geometry.Vertex> vertices = new List<TriangleNet.Geometry.Vertex>();
                vertices.Add(triangle.GetVertex(0));
                vertices.Add(triangle.GetVertex(1));
                vertices.Add(triangle.GetVertex(2));
                Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
                //foreach (var vk in vertices) {
                //   ring.AddPoint_2D(vk.X, vk.Y);}
                for (int j = 0; j < 3; j++) {
                    ring.AddPoint_2D(vertices[j].X, vertices[j].Y);
                }
                ring.AddPoint_2D(vertices[0].X, vertices[0].Y);//保证形成一个首尾相接的环
                Geometry poly = new Geometry(wkbGeometryType.wkbPolygon);
                poly.AddGeometry(ring);
                triangleGeom.Add(poly);
            }
        }
        private static List<TriangleNet.Geometry.Vertex> makexyListToTrangleNetVertex(List<double []>xylist) {
            int count = xylist.Count;
            List<TriangleNet.Geometry.Vertex> result = new List<TriangleNet.Geometry.Vertex>();
            for (int i = 0; i < count; i++) {
                double[] xy= xylist[i];
                TriangleNet.Geometry.Vertex vr = new TriangleNet.Geometry.Vertex();
                vr.X = xy[0];
                vr.Y = xy[1];
                result.Add(vr);
            }
            return result;
        }
        private static TriangleNet.Mesh GetTriMesh(List<TriangleNet.Geometry.Vertex> pA, List<TriangleNet.Geometry.Vertex> pB)
        {
            #region 三角剖分模块
            //1. 
            //约束选项（约束类）
            var options = new TriangleNet.Meshing.ConstraintOptions();
            options.SegmentSplitting = 1;
            options.ConformingDelaunay = true;
            options.Convex = false ;

            //质量选项（质量类）
            var quality = new TriangleNet.Meshing.QualityOptions();
            TriangleNet.Geometry.IPolygon input = GetPolygon(pA, pB);
            TriangleNet.Geometry.Contour conA = GetContourByTriangle(pA);
            TriangleNet.Geometry.Contour conB = GetContourByTriangle(pB);
            //添加边界约束
            input.Add(conA, false);
            input.Add(conB, true);

            TriangleNet.Mesh mesh = null;
            if (input != null)
            {
                mesh = (TriangleNet.Mesh)TriangleNet.Geometry.ExtensionMethods.Triangulate(input, options);

            }

            return mesh;
            #endregion

        }
        private static TriangleNet.Geometry.IPolygon GetPolygon(List<TriangleNet.Geometry.Vertex> pA, List<TriangleNet.Geometry.Vertex> pB)
        {
            TriangleNet.Geometry.IPolygon data = new TriangleNet.Geometry.Polygon();

            foreach (var vt in pA)
            {
                TriangleNet.Geometry.Vertex triVertex = new TriangleNet.Geometry.Vertex(vt.X, vt.Y);
                //triVertex.NAME = vt.NAME;
                data.Add(triVertex);
            }
            for (int i = pB.Count - 1; i >= 0; i--)
            {
                TriangleNet.Geometry.Vertex triVertex = new TriangleNet.Geometry.Vertex(pB[i].X, pB[i].Y);
               // triVertex.NAME = pB[i].NAME;
                data.Add(triVertex);
            }
            return data;
        }
        public static TriangleNet.Geometry.Contour GetContourByTriangle(List<TriangleNet.Geometry.Vertex> pA)
        {
            List<TriangleNet.Geometry.Vertex> pv = new List<TriangleNet.Geometry.Vertex>();

            foreach (var vt in pA)
            {
                pv.Add(vt);
            }
            TriangleNet.Geometry.Contour pNewCon = new TriangleNet.Geometry.Contour(pv);
            return pNewCon;
        }

        private static double[] getMinAreaRectByCv2(List<double[]> xylist, out RotatedRect rotatedRect) {
            //通过opencv获取到最小外接矩形 ，返回一个具有8个double的数组，代表矩形四个点的位置信息
            double[] result = new double[8];
            List<OpenCvSharp.Point2f> points = new List<Point2f>();
            int count = xylist.Count;
            for (int i = 0; i < count; i++) {
                double[] xy = xylist[i];
                OpenCvSharp.Point2f point = new Point2f((float)xy[0],(float) xy[1]);
                points.Add(point);
            }
             rotatedRect= Cv2.MinAreaRect(points);
           
            // Rect rect = rotatedRect.BoundingRect();
            Point2f[] point2Fs= rotatedRect.Points();
            for (int i = 0; i < 4; i++) {
                Point2f point2F = point2Fs[i];
                result[i * 2] = point2F.X;
                result[i * 2 + 1] = point2F.Y;
            }
            return result; 
        }
        private static double[] getRotateRectPoints(MinOutsourceRect rotatedRect) {
            double[] result = new double[8];
            result = rotatedRect.points();
            return result;
        }
        private static double[] getRotateRectPoints(MinOutsourceRect rotatedRect,double xbuffer,double ybuffer)
        {
            double[] result = new double[8];
            result = rotatedRect.bufferpoints(xbuffer,ybuffer);
            return result;
        }
        private static void getRectDense(double[] rect, List<double[]> outlinexy,out List<double[]> rectxylist,double tolerate) {
            //获取一个加密的矩形周围上的点
            rectxylist = new List<double[]>();
            for (int i = 0; i < 3; i++) {
                List<double[]> linedense;
                getLinedense(rect[i * 2], rect[i * 2 + 1], rect[i * 2 + 2], rect[i * 2 + 3], tolerate,out linedense);
                linedense.RemoveAt(linedense.Count - 1);//把尾点移除，因为下一条线首尾衔接，会有重复点
                rectxylist.AddRange(linedense);
            }
            List<double[]> linedense1;
            getLinedense(rect[6], rect[7], rect[0], rect[1], tolerate, out linedense1);
            linedense1.RemoveAt(linedense1.Count - 1);
            rectxylist.AddRange(linedense1);
            //下面就要检查一下矩形边界上的点，如果距离这个地层的外缘太近，就给它remove掉好了
            int rectxycount = rectxylist.Count;
            bool[] needToRemover = new bool[rectxycount];
            for (int i = 0; i < rectxycount; i++) needToRemover[i] = false;
            double threshold = tolerate / 3;
            for (int i = 0; i < rectxycount; i++) {//遍历所有的矩形上加密的点，然后求和每个地层边界上距离
                
                double[] xyrect = rectxylist[i];
                int countoutline = outlinexy.Count;
                for (int j = 0; j < countoutline; j++) {
                    double[] xy = outlinexy[j];
                    double dis = distance(xyrect[0], xyrect[1], xy[0], xy[1]);
                    if (dis <threshold) {
                        needToRemover[i] = true;
                        break;
                    }
                }
            }
            for (int i = rectxycount - 1; i >= 0; i--) {
                if (needToRemover[i]) {
                    rectxylist.RemoveAt(i);
                }
            }
        }
        private static void getLinedense(double x1,double y1,double x2, double y2 ,double tolerate,out List<double[]> xyline) {
            double dis = distance(x1, y1, x2, y2);
            int stepcount = (int)(dis / tolerate);
            //stepcount--;//求出应当加多少点
            if (dis < tolerate * 5) {
                tolerate = dis / 5;
                stepcount = 4;
            }
            xyline = new List<double[]>();
            double dx = (x2 - x1) / dis;//求出单位向量
            double dy = (y2 - y1) / dis;
            for (int i=0;i<stepcount;i++) 
            {
                double x = x1 + dx * tolerate * i;
                double y = y1 + dy * tolerate * i;
                double[] xy = { x, y };
                xyline.Add(xy);
            }
            double[] end = { x2, y2 };
            xyline.Add(end);//最后一个点加进去
        }
        private static Geometry getUnionPoly(List<Geometry> polys) {//获取这个列表中所有的geom的union
            Geometry geometry = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (Geometry geom in polys) {
                geometry = geometry.Union(geom);
            }
            return geometry;
        }
        public static double distance(double x1 ,double y1,double x2,double y2) {
            double dx = x1 - x2;
            double dy = y1 - y2;
            double dis2 = dx * dx + dy * dy;
            return Math.Sqrt(dis2);
        }
       public static void setPolyIntoList(Geometry poly,out List<double[]>xylist) {
            Geometry ring = poly.GetGeometryRef(0);
            int pointcount = ring.GetPointCount();
            xylist = new List<double[]>();
            for (int i = 0; i < pointcount; i++) {
                double x, y;
                x = ring.GetX(i);
                y = ring.GetY(i);
                double[] xy = { x, y };
                xylist.Add(xy);
            }
        }
        private static void setLineStringIntoList(Geometry line, out List<double[]> xylist)
        {
        
            int pointcount = line.GetPointCount();
            xylist = new List<double[]>();
            for (int i = 0; i < pointcount; i++)
            {
                double x, y;
                x = line.GetX(i);
                y = line.GetY(i);
                double[] xy = { x, y };
                xylist.Add(xy);
            }
        }
        public static void saveGeom(List<Geometry> geometries, string path, string spatialpath)
        {

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Geometry geom1 = geometries[0];
            Layer layer = dataSource.CreateLayer("result", getSpatialRef(spatialpath), geom1.GetGeometryType(), null);
            int count = geometries.Count;
            for (int i = 0; i < count; i++)
            {
                Geometry geom = geometries[i];
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetGeometry(geom);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        public static Layer openlayer(string path)
        {
            Gdal.AllRegister();
            Ogr.RegisterAll();
            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "");

            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            return layer;
        }
        public static SpatialReference getSpatialRef(string path)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(path, 1);
            Layer layer = dataSource.GetLayerByIndex(0);
            SpatialReference spatialReference = layer.GetSpatialRef();
            layer.Dispose();
            dataSource.Dispose();
            return spatialReference;
        }
    }
}
