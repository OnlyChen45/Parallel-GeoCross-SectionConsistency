using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{
    public class EraseSharpen
    {
        static public int BoundaryID = -1;  
        /// <summary>
        /// 这个类的工作就是，把两个剖面中所有的尖灭地层合并到他们隔壁的最大的地层上，当然如果是和外部空间相邻，那么外部空间自然是最大的了。
        /// 规则：如果和外部空间相邻，那么久删掉
        /// 如果和只与普通地层相邻，那么就合并到周围面积最大的一个里边
        /// </summary>
        /// <param name="geoms1"></param>
        /// <param name="geoms2"></param>
        /// <param name=""></param>
        static public void eraseSharpenStra(Dictionary<int,Geometry>geoms1,Dictionary<int,Geometry>geoms2,out Dictionary<int,Geometry>resultgeoms1,out Dictionary<int,Geometry>resultgeoms2) {

            List<int> keys1 = geoms1.Keys.ToList<int>();
            List<int> keys2 = geoms2.Keys.ToList<int>();
            List<int> sharpen1 = delListValue(keys1, keys2);//获取两个剖面的尖灭层。
            List<int> sharpen2 = delListValue(keys2, keys1);
            resultgeoms1 = getNoSharpenGeoms(geoms1, sharpen1);
            resultgeoms2 = getNoSharpenGeoms(geoms2, sharpen2);
        }
        static Dictionary<int, Geometry> getNoSharpenGeoms(Dictionary<int,Geometry> geoms,List<int> sharpenids) 
        {
           
            Dictionary<int, List<int>> touchesList = getPolyTouches(geoms);
            Dictionary<int, double> areas = getAreaDic(geoms);
            Dictionary<int, Geometry> result = new Dictionary<int, Geometry>();
            foreach (var vk in geoms) {
                int id = vk.Key;
                if (sharpenids.Contains(id) == false) {
                    result.Add(id, vk.Value);
                }
            }
            Geometry biggeom = new Geometry(wkbGeometryType.wkbPolygon);
            foreach (var tg in geoms) {
                biggeom = biggeom.Union(tg.Value);
            }
            Geometry bigboundry = biggeom.Boundary();
            foreach (int sharpenid in sharpenids) {
                List<int> touchessharpen = touchesList[sharpenid];//取出尖灭地层的相邻地层的id
                double maxlength = double.MinValue;
                int maxid = -2;
                foreach (int touchid in touchessharpen) {
                    double lengtht;
                    if (sharpenids.Contains(touchid)) continue;
                    if (touchid == -1)
                    {
                        Geometry touchline = geoms[sharpenid].Intersection(bigboundry);
                        lengtht = getLineLength(touchline);
                    }
                    else {
                        Geometry touchline = geoms[sharpenid].Intersection(geoms[touchid]);
                        lengtht = getLineLength(touchline);
                    }
                    
                    if (lengtht > maxlength) {
                        maxlength =lengtht;
                        maxid = touchid;
                    }
                }
                if (maxid == -1) continue;
                Geometry tempgeom = result[maxid];
                Geometry newgeom = tempgeom.Union(geoms[sharpenid]);
                result[maxid] = newgeom; 
            }
            return result;

            double getLineLength(Geometry linegeom) {
                wkbGeometryType geomtype = linegeom.GetGeometryType();
                double lengthsum = 0;
                switch (geomtype) {
                    case wkbGeometryType.wkbMultiLineString:
                        {
                            int linecount = linegeom.GetGeometryCount();
                            for (int i = 0; i < linecount; i++) {
                                Geometry linet = linegeom.GetGeometryRef(i);
                                lengthsum = lengthsum + linet.Length();
                            }
                        break;
                        }
                    case wkbGeometryType.wkbLineString:
                        {
                            lengthsum = lengthsum+ linegeom.Length();
                            break;
                        }
                    case wkbGeometryType.wkbGeometryCollection:
                        {
                            int linecount = linegeom.GetGeometryCount();
                            for (int i = 0; i < linecount; i++)
                            {
                                switch (geomtype)
                                {
                                    case wkbGeometryType.wkbMultiLineString:
                                        {
                                            int linecountt = linegeom.GetGeometryCount();
                                            for (int j = 0; j < linecountt; j++)
                                            {
                                                Geometry linet = linegeom.GetGeometryRef(j);
                                                lengthsum = lengthsum + linet.Length();
                                            }
                                            break;
                                        }
                                    case wkbGeometryType.wkbLineString:
                                        {
                                            lengthsum = lengthsum + linegeom.Length();
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                }
                return lengthsum;
            }
        }

        static Dictionary<int, double> getAreaDic(Dictionary<int, Geometry> geoms)
        {
            Dictionary<int, double> areadic = new Dictionary<int, double>();
            foreach (var vk in geoms)
            {
                int geomid = vk.Key;
                Geometry geometry = vk.Value;
                double area = geometry.Area();
                areadic.Add(geomid, area);
            }
            return areadic;
        }
        static Dictionary<int, List<int>> getPolyTouches(Dictionary<int, Geometry> polys)
        
        {
            //生成剖面中的各个地层的touches的表
            Dictionary<int, List<int>> touchesList = new Dictionary<int, List<int>>();
            Geometry superSection = new Geometry(wkbGeometryType.wkbPolygon);

            foreach (var poly in polys)
            {
                List<int> touches = new List<int>();

                foreach (var poly2 in polys)
                {
                    if (poly.Key == poly2.Key) continue;//如果是同个面，就跳过
                    if (touchesList.ContainsKey(poly2.Key))
                    {//检查一下这两个面是不是做过了，如果做过了，那么直接加进去就完了
                        if (touchesList[poly2.Key].Contains(poly.Key))
                        {
                            touches.Add(poly2.Key);
                            continue;
                        }
                    }
                    //特殊情况处理完毕，现在是正常情况
                    Geometry intersectGeom = poly.Value.Intersection(poly2.Value);
                    if (intersectGeom.IsEmpty() == true) continue;//不相交，就下一位
                    wkbGeometryType resulttype = intersectGeom.GetGeometryType();
                    //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
                    if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbGeometryCollection)
                    {
                        if (touches.Contains(poly2.Key) == false)
                            touches.Add(poly2.Key);
                    }
                }
                //touches表加入结果表
                touchesList.Add(poly.Key, touches);
            }
            //处理与开放边界相交的情况
            foreach (var poly in polys)
            {
                superSection = superSection.Union(poly.Value);
            }
            Geometry boundary = superSection.Boundary();
            List<int> boundarytouch = new List<int>();
            foreach (var poly in polys)
            {
                Geometry intersectGeom = poly.Value.Intersection(boundary);//每个都和外边界做相交
                wkbGeometryType resulttype = intersectGeom.GetGeometryType();
                if (intersectGeom.IsEmpty() == true) continue;
                //判断一下相交的这个多边形，可以是线，多段线，点线组合，（主要不能是点），然后给他添加进touches表
                if (resulttype == wkbGeometryType.wkbLineString || resulttype == wkbGeometryType.wkbMultiLineString || resulttype == wkbGeometryType.wkbGeometryCollection)
                {
                    touchesList[poly.Key].Add(BoundaryID);
                    boundarytouch.Add(poly.Key);
                }
            }
            touchesList.Add(BoundaryID, boundarytouch);//把外边界也加进去好了。
            return touchesList;
        }
            static private List<int> delListValue(List<int> list1, List<int> list2)
        {
            List<int> result = new List<int>();
            foreach (int v in list1)
            {
                bool in2list = false;
                foreach (int v2 in list2)
                {
                    if (v == v2)
                    {
                        in2list = true;
                        break;
                    }
                }
                if (in2list == false)
                {
                    result.Add(v);
                }
            }
            return result;
        }
    }
}
