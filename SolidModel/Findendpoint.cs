using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCommon;
using TriangleNet;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
namespace SolidModel
{
    public class Findendpoint
    {//这个类主要是用来寻找一个polygon文件中所有的面之间相交线的端点
       // List<>
        public Findendpoint() { 
        
        }
        public Findendpoint(Layer polygonlayer) { 
             
        }
        public Findendpoint(string polyshppath ) { 
        
        }
    }
}
