using GeoCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModel
{
    public class CommonFun
    {
        /// <summary>
        /// 获取两点之间的距离（几何点法,三维）
        /// </summary>
        /// <returns></returns>
        public static double GetDistance3D(Vertex p1, Vertex p2)
        {
            return GetDistanceInDetail3D(p1.x, p1.y, p1.z, p2.x, p2.y, p2.z);
        }

        /// <summary>
        /// 获取两个点距离（坐标法，三维）
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <returns></returns>
        public static double GetDistanceInDetail3D(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2) + Math.Pow(z1 - z2, 2));
        }

        /// <summary>
        /// 获取两点之间的距离（几何点法,三维）
        /// </summary>
        /// <returns></returns>
        public static double GetDistance2D(Vertex p1, Vertex p2)
        {
            return GetDistanceInDetail2D(p1.x, p1.y, p2.x, p2.y);
        }

        /// <summary>
        /// 获取两个点距离（坐标法，三维）
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <returns></returns>
        public static double GetDistanceInDetail2D(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

    }
}
