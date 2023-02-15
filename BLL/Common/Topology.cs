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
  public  class Topology
    {
        public Dictionary<int, List<int>> poly_arcs_Pairs;//面到弧段的索引
        public Dictionary<int, List<int>> points_arcs_Pairs;//点到弧段的连接
        public Dictionary<int, int[]> arcs_points_Pairs;//弧段的端点index
        public Dictionary<int, int[]> arcs_poly_Pairs;//弧段到面
        public Dictionary<int, Geometry> index_arcs_Pairs;//弧段的index到实体
        public Dictionary<int, Geometry> index_points_Pairs;//点的index的实体
        public List<int> idlist;
        public Dictionary<int, Geometry> polys;
        //private Dictionary<int, List<int>> touches;
        public Topology( Dictionary<int, List<int>> poly_arcs_Pairs,Dictionary<int, List<int>> points_arcs_Pairs,Dictionary<int, int[]> arcs_points_Pairs,
        Dictionary<int, int[]> arcs_poly_Pairs,Dictionary<int, Geometry> index_arcs_Pairs,Dictionary<int, Geometry> index_points_Pairs,List<int> idlist,Dictionary<int, Geometry> polys) {
            this.poly_arcs_Pairs = poly_arcs_Pairs;
            this.points_arcs_Pairs = points_arcs_Pairs;
            this.arcs_points_Pairs = arcs_points_Pairs;
            this.arcs_poly_Pairs = arcs_poly_Pairs;
            this.index_arcs_Pairs = index_arcs_Pairs;
            this.index_points_Pairs = index_points_Pairs;
            this.idlist = idlist;
            this.polys = polys;

        }
    }
}
