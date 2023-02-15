using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class LayerHelp
    {

        /// <summary>
        /// 根据要素路径与要素名获取要素
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public static Layer GetLayerByLayerName(string filePath, string layerName)
        {
            DataSource ds = Ogr.Open(filePath, 0);
            if (ds == null)
            {
                throw new Exception("不能打开" + filePath);
            }

            OSGeo.OGR.Driver drv = ds.GetDriver();
            if (drv == null)
            {
                throw new Exception("不能获取驱动，请检查！");
            }

            Layer drillLayer = ds.GetLayerByName(layerName);
            if (drillLayer == null)
                throw new Exception("获取要素失败");

            return drillLayer;
        }



        /// <summary>
        /// 读取栅格数据
        /// </summary>
        public static void ReadRaster(string strFile)
        {
            //注册
            Gdal.AllRegister();

            Dataset ds = Gdal.Open(strFile, Access.GA_ReadOnly);

            if (ds == null)
            {
                Console.WriteLine("不能打开：" + strFile);
                System.Environment.Exit(-1);
            }

            OSGeo.GDAL.Driver drv = ds.GetDriver();
            if (drv == null)
            {
                Console.WriteLine("不能打开：" + strFile);
                System.Environment.Exit(-1);
            }

            Console.WriteLine("RasterCount:" + ds.RasterCount);
            Console.WriteLine("RasterSize:" + ds.RasterXSize + " " + ds.RasterYSize);
        }
    }
}
