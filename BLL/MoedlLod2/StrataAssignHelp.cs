using OSGeo.OGR;
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
    public class StrataAssignHelp
    {
        /// <summary>
        /// 获取地层间的相接线
        /// </summary>
        /// <param name="_strataP1"></param>
        /// <param name="_strataP2"></param>
        /// <returns></returns>
        public static Dictionary<Geometry,string > GetTouchLine(Dictionary<Geometry, string> pStrataPolygon)
        {
            //两个地层的相接线集合
            Dictionary<Geometry, string> pTouchLines = new Dictionary<Geometry, string>();

            //获取所有尖灭地层
            List<Geometry> pStrataCol = new List<Geometry>();
            foreach (var vp in pStrataPolygon.Keys)
            {
                pStrataCol.Add(vp);
            }

            //获取相接线
            for (int i = 0; i < pStrataCol.Count-1; i++)
            {
                for (int j = i + 1; j < pStrataCol.Count; j++)
                {
                    if (pStrataCol[i].Intersect(pStrataCol[j]))
                    {
                       
                        //获取的是线段分段
                        Geometry pTouchLineSegment = pStrataCol[i].Intersection(pStrataCol[j]);
                       

                        List<Geometry> pgs = new List<Geometry>();

                        //将分段的线段重新合并成一个折线
                        //应该用Osr.ForceToLine
                        Geometry pLine = new Geometry(wkbGeometryType.wkbLineString);
                        for (int k = 0; k < pTouchLineSegment.GetGeometryCount(); k++)
                        {
                            Geometry pg = pTouchLineSegment.GetGeometryRef(k);
                            pLine.SetPoint_2D(k,pg.GetX(0), pg.GetY(0));
                            if (k == (pTouchLineSegment.GetGeometryCount() - 1))
                            {
                                pLine.SetPoint_2D(k+1, pg.GetX(1), pg.GetY(1));
                            } 
                        }

                        pgs.Add(pLine);

                        pTouchLines.Add(pLine,pStrataPolygon[pStrataCol[i]] + "-" + pStrataPolygon[pStrataCol[j]]);

                        //pgs.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\SectionInterpolation", pStrataPolygon[pStrataCol[i]] + "-" + pStrataPolygon[pStrataCol[j]]);
                    }
                }
            }


            return pTouchLines;
        }


        /// <summary>
        /// 获取多个多边形合并后的外包矩形
        /// </summary>
        /// <param name="pStrataPolygon"></param>
        /// <returns></returns>
        public static Envelope GetPolygonEnvelope(List<Geometry> pStrataPolygon)
        {
            Geometry pMultiPolygon = new Geometry(wkbGeometryType.wkbMultiPolygon);
            foreach (var vp in pStrataPolygon)
            {
                pMultiPolygon.AddGeometry(vp);
            }

            Geometry pg = pMultiPolygon.UnionCascaded();
            Envelope pEnv = new Envelope();
            pg.GetEnvelope(pEnv);
            wkbGeometryType ps = pg.GetGeometryType();

            return pEnv;
        }


        /// <summary>
        /// 构建切割线集合
        /// </summary>
        /// <param name="pBeforeEnv"></param>
        /// <param name="pBehindEnv"></param>
        /// <param name="pTouchLines"></param>
        /// <returns></returns>
        public static Dictionary<Geometry, string> pJoinLineChange(Envelope pBeforeEnv, Envelope pBehindEnv,
            Dictionary<Geometry,string> pTouchLines)
        {
            //返回的切割线集合
            Dictionary<Geometry, string> returnSplitLines = new Dictionary<Geometry, string>();

            //1 获取两个外包矩形的中心点
            Geometry pt1 = GetCentralPoint(pBeforeEnv);
            Geometry pt2 = GetCentralPoint(pBehindEnv);

            List<Geometry> pts = new List<Geometry>();
            pts.Add(pt1);
            pts.Add(pt2);

            //pts.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\SectionInterpolation","points");

            //2 获取平移参数
            double detaX = pt2.GetX(0) - pt1.GetX(0);
            double detaY = pt2.GetY(0) - pt1.GetY(0);

            //3 获取比例参数
            double xScale = (pBehindEnv.MaxX - pBehindEnv.MinX) / (pBeforeEnv.MaxX - pBeforeEnv.MinX);
            double yScale = (pBehindEnv.MaxY - pBehindEnv.MinY) / (pBeforeEnv.MaxY - pBeforeEnv.MinY);

            //获取相接线
            List<Geometry> pJoinLines = new List<Geometry>();
            foreach(var vp in pTouchLines.Keys)
            {
                pJoinLines.Add(vp);
            }


            for (int i = 0; i < pJoinLines.Count; i++)
            {
                //平移后的线
                Geometry pTransLine = new Geometry(wkbGeometryType.wkbLineString);

                List<Geometry> newJoinLines = new List<Geometry>();

                int pointcount = pJoinLines[i].GetPointCount();

                for (int j = 0; j < pointcount; j++)
                {
                    //4 原坐标相对于矩形左上角的坐标
                    double dtx = pJoinLines[i].GetX(j) - pBeforeEnv.MinX ;
                    double dty = pJoinLines[i].GetY(j) - pBeforeEnv.MaxY ;

                    //5 缩放后相当于矩形左上角的坐标
                    double dttx = dtx * xScale;
                    double dtty = dty * yScale;

                    //6 缩放后的坐标
                    double newX = dttx + pBehindEnv.MinX;
                    double newY = dtty + pBehindEnv.MaxY;

                    pTransLine.AddPoint_2D(newX, newY);

                }

                newJoinLines.Add(pTransLine);

                returnSplitLines.Add(pTransLine, pTouchLines[pJoinLines[i]]);

                //newJoinLines.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\SectionInterpolation", i.ToString());
            }

            return returnSplitLines;
        }



        public static List<Geometry> SplitStrataPolygon(Dictionary<Geometry,string> pJoinLines,Geometry pStrataP)
        {
            List<Geometry> pStrataPolygons = new List<Geometry>();

            //修改后的线段
            List<Geometry> pModifyLines = new List<Geometry>();

            Geometry pMultiLines = new Geometry(wkbGeometryType.wkbMultiLineString);

            foreach (var vp in pJoinLines.Keys)
            {
                Geometry pStart = new Geometry(wkbGeometryType.wkbPoint);
                pStart.AddPoint_2D(vp.GetX(0), vp.GetY(0));

                Geometry pEnd = new Geometry(wkbGeometryType.wkbPoint);
                pEnd.AddPoint_2D(vp.GetX(vp.GetPointCount() - 1), vp.GetY(vp.GetPointCount() - 1));


                Geometry pStartNext = new Geometry(wkbGeometryType.wkbPoint);
                pStartNext.AddPoint_2D(vp.GetX(1), vp.GetY(1));

                Geometry pEndPre = new Geometry(wkbGeometryType.wkbPoint);
                pEndPre.AddPoint_2D(vp.GetX(vp.GetPointCount() - 2), vp.GetY(vp.GetPointCount() - 2));


                if (pStrataP.Contains(pStart))
                {
                    double n = 1.5;
                    while (pStrataP.Contains(pStart))
                    {
                        double strX = n * (pStart.GetX(0) - pStartNext.GetX(0)) + pStartNext.GetX(0);
                        double strY = n * (pStart.GetY(0) - pStartNext.GetY(0)) + pStartNext.GetY(0);

                        vp.SetPoint_2D(0, strX, strY);
                        pStart.SetPoint_2D(0, strX, strY);

                    }
                }

                if (pStrataP.Contains(pEnd))
                {
                    double n = 1.5;
                    while (pStrataP.Contains(pEnd))
                    {
                        double strX = n * (pEnd.GetX(0) - pEndPre.GetX(0)) + pEndPre.GetX(0);
                        double strY = n * (pEnd.GetY(0) - pEndPre.GetY(0)) + pEndPre.GetY(0);

                        vp.SetPoint_2D(vp.GetPointCount() - 1, strX, strY);
                        pEnd.SetPoint_2D(0, strX, strY);

                    }
                }

                pStrataPolygons.Add(vp);

                pMultiLines.AddGeometry(vp);
            }

            //pStrataPolygons.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\SectionInterpolation", "modifyLine");

            Geometry pMulitPolLines = new Geometry(wkbGeometryType.wkbMultiLineString);

            //获取边界
            Geometry pPOL = pStrataP.GetBoundary();

            //分离
            Geometry ptt2 = pPOL.SymDifference(pMultiLines);
            int count1 = ptt2.GetGeometryCount();
            for (int i = 0; i < count1; i++)
            {
                pMulitPolLines.AddGeometry(ptt2.GetGeometryRef(i));
            }

            //构建单个polygon
            Geometry splitPolygon = pMulitPolLines.Polygonize();
            int count = splitPolygon.GetGeometryCount();
            for (int i = 0; i < count; i++)
            {
                pStrataPolygons.Add(splitPolygon.GetGeometryRef(i));
            }
            //pStrataPolygons.ExportGeometryToShapfile(@"C:\Users\HakerTop\Desktop\SectionInterpolation", "modifyll");

            return pStrataPolygons;
        }

        /// <summary>
        /// 返回外包矩形的中心点
        /// </summary>
        /// <param name="pEnv"></param>
        /// <returns></returns>
        private static Geometry GetCentralPoint(Envelope pEnv)
        {
            Geometry vt = new Geometry(wkbGeometryType.wkbPoint);

            vt.AddPoint_2D((pEnv.MaxX + pEnv.MinX) / 2, (pEnv.MaxY + pEnv.MinY) / 2);
           
            return vt;
        }
    }
}
