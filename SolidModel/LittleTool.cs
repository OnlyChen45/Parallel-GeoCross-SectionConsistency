using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class LittleTool
    {
        public static void ExportToSingleShp(string pLayFile)
        {
            Layer pContourLayer = LayerHelp.GetLayerByLayerName(System.IO.Path.GetDirectoryName(pLayFile), System.IO.Path.GetFileNameWithoutExtension(pLayFile));

            if (pContourLayer == null)
            {
                return ;
            }

            long count = pContourLayer.GetFeatureCount(1);

            for (int i = 1; i <= Convert.ToInt16(count); i++)
            {

                List<Geometry> gps = new List<Geometry>();
                //对图层进行初始化，如果对图层进行了过滤操作，执行这句化后，之前的过滤全部清空
                pContourLayer.ResetReading();
                string sql = "id=" + i ;
                int k =pContourLayer.SetAttributeFilter(sql);
                Feature pfeature = pContourLayer.GetNextFeature();
                while (pfeature != null)
                {
                    Geometry pline = pfeature.GetGeometryRef();
                    for(int j=0;j<pline.GetPointCount();j++)
                    {
                        pline.SetPoint(j, pline.GetX(j), pline.GetY(j), -50 * (i - 1));
                    }
                    
                    gps.Add(pfeature.GetGeometryRef());


                    pfeature = pContourLayer.GetNextFeature();
                    
                }

                byte[] array = new byte[1];
                array[0] = (byte)(Convert.ToInt32(64+i));//ASCII码强制转换二进制
                string ret=Convert.ToString(System.Text.Encoding.ASCII.GetString(array));

                gps.ExportGeometryToShapfileByfield(System.IO.Path.GetDirectoryName(pLayFile), ret, "NAME");
                
            }
        }
    }
}
