using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace ThreeDModelSystemForSection
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文  
            Gdal.SetConfigOption("SHAPE_ENCODING", "");
            Gdal.AllRegister();
            Ogr.RegisterAll();
            // string projdbpath = @"D:\release-1928-x64-gdal-3-3-3-mapserver-7-6-4\bin\proj7\share\";
            string projdbpath = ".//proj7//";
            Osr.SetPROJSearchPath(projdbpath);//规定一下投影库依赖文件的所在位置
            Application.Run(new FrmMain());
        }
    }
}
