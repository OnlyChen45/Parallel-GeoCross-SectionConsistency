using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

using System.Runtime.Serialization.Formatters.Binary;

namespace ThreeDModelSystemForSection
{
    /// <summary>
    /// Stratum relative position change, that is, topological transformation code, business layer
    /// </summary>
    public class DealTopoChange
    {
        /// The first is to find the point between the strata in the middle. This is done by the Topu class.  

        // And then do the topo table of each point and surface  

        // Look for points that don't match exactly  

        // Then find the lines that don't necessarily correspond  

        // Make a union set of the two graphs respectively, connect the points that do not correspond, and discard the ones that do not connect  

        // Then two parallel sets, according to the contact polygon correspond  

        // The buffer is generated after corresponding to the line of combining and searching, and new lithcode is given according to the corresponding relationship  

        // Crop in the result 
        static public void dealTopoChange(string section1, string section2, string idFieldName, string workspace, string outputsection1, string outputsection2, string orisectionpath1,string orisectionpath2, double buffer)
        {
            PolygonIO polygonIO1 = new PolygonIO(section1, idFieldName);
            PolygonIO polygonIO2 = new PolygonIO(section2, idFieldName);
            Dictionary<int, Geometry> polys1, polys2;
            List<int> idlist1, idlist2;
            polygonIO1.getGeomAndId(out polys1, out idlist1);
            TopologyOfPoly topologyworker1 = new TopologyOfPoly(idlist1, polys1);
            topologyworker1.makeTopology();
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            TopologyOfPoly topologyworker2 = new TopologyOfPoly(idlist2, polys2);
            topologyworker2.makeTopology();
            Topology topology1, topology2;
            topologyworker1.exportToTopology(out topology1);//Make a topology table with two sides, so you can get the points easily
            topologyworker2.exportToTopology(out topology2);
            //topologyworker1.saveArcsInshp(@"D:\GISworkspace\QS69\topotemp\topo1.shp","line",polygonIO1.getSpatialRef());
            //topologyworker2.saveArcsInshp(@"D:\GISworkspace\QS69\topotemp\topo2.shp", "line", polygonIO2.getSpatialRef());
            List<int[]> topochangeline1, topochangeline2;
            Dictionary<int, List<int>> pointid_arclist1, pointid_arclist2;
            FindTopoChangeByMultiLine.findTopoChange(topology1, topology2, polys1, polys2, out topochangeline1, out topochangeline2);
            maketemptranse(topochangeline1, out pointid_arclist1);
            maketemptranse(topochangeline2, out pointid_arclist2);
            Dictionary<int, Geometry> buffers1, buffers2, buffers1reNumber, buffers2reNumber;
            // This is it. We have found the corresponding group and got all the data  

            // The next step is to create a buffer for each connected block and then create a corresponding buffer 
            createBuffersByTouches(topology1.index_arcs_Pairs, pointid_arclist1, out buffers1, buffer);
            createBuffersByTouches(topology2.index_arcs_Pairs, pointid_arclist2, out buffers2, buffer);
            string bufferpath1 = workspace + "\\buffer1.shp";
            string bufferpath2 = workspace + "\\buffer2.shp";
            
            buffers1reNumber = new Dictionary<int, Geometry>();
            buffers2reNumber = new Dictionary<int, Geometry>();
            /*foreach (var pair in headpair) {
                int idnew = 1000 + pair.Key;
                buffers1reNumber.Add(idnew, buffers1[pair.Key]);
                buffers2reNumber.Add(idnew, buffers2[pair.Value]);
            }*/
            int maxid1= polygonIO1.getMaxid();// Find the largest id to prevent new id conflicts
            int maxid2 = polygonIO2.getMaxid();
            if (maxid1 < maxid2) maxid1 = maxid2;
            for (int i = 0; i < buffers1.Count; i++)
            {
                int idnew = maxid1+1000 + i;
                buffers1reNumber.Add(idnew, buffers1[i]);
                buffers2reNumber.Add(idnew, buffers2[i]);
            }
            saveDictionaryGeom(buffers1reNumber, bufferpath1, idFieldName, polygonIO1.getSpatialRef());
            saveDictionaryGeom(buffers2reNumber, bufferpath2, idFieldName, polygonIO2.getSpatialRef());
            createEraseBufferData(outputsection1, "section1", idFieldName, bufferpath1, orisectionpath1, polygonIO1.getSpatialRef());
            createEraseBufferData(outputsection2, "section2", idFieldName, bufferpath2, orisectionpath2, polygonIO2.getSpatialRef());
            // MatchLithIDForSections.MatchLayer matchLayer1 = new MatchLithIDForSections.MatchLayer(section1, outputsection1, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
            //MatchLithIDForSections.MatchLayer matchLayer2 = new MatchLithIDForSections.MatchLayer(section2, outputsection2, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
        }
        static public void dealTopoChange(string section1, string section2, string idFieldName, string workspace, string outputsection1, string outputsection2, string orisectionpath1, string orisectionpath2, double buffer,out string bufferpath1,out string bufferpath2)
        {
            PolygonIO polygonIO1 = new PolygonIO(section1, idFieldName);
            PolygonIO polygonIO2 = new PolygonIO(section2, idFieldName);
            Dictionary<int, Geometry> polys1, polys2;
            List<int> idlist1, idlist2;
            polygonIO1.getGeomAndId(out polys1, out idlist1);
            TopologyOfPoly topologyworker1 = new TopologyOfPoly(idlist1, polys1);
            topologyworker1.makeTopology();
            polygonIO2.getGeomAndId(out polys2, out idlist2);
            TopologyOfPoly topologyworker2 = new TopologyOfPoly(idlist2, polys2);
            topologyworker2.makeTopology();
            Topology topology1, topology2;
            topologyworker1.exportToTopology(out topology1);// Make a topology table with two sides so that you can get the points easily
            topologyworker2.exportToTopology(out topology2);
            List<int[]> topochangeline1, topochangeline2;
            Dictionary<int, List<int>> pointid_arclist1, pointid_arclist2;
            FindTopoChangeByMultiLine.findTopoChange(topology1, topology2, polys1, polys2, out topochangeline1, out topochangeline2);
            maketemptranse(topochangeline1, out pointid_arclist1);
            maketemptranse(topochangeline2, out pointid_arclist2);
            Dictionary<int, Geometry> buffers1, buffers2, buffers1reNumber, buffers2reNumber;
            // This is it. We have found the corresponding group and got all the data  

            // The next step is to create a buffer for each connected block and then create a corresponding buffer
            createBuffersByTouches(topology1.index_arcs_Pairs, pointid_arclist1, out buffers1, buffer);
            createBuffersByTouches(topology2.index_arcs_Pairs, pointid_arclist2, out buffers2, buffer);
            bufferpath1 = workspace + "\\buffer1.shp";
            bufferpath2 = workspace + "\\buffer2.shp";
            // There is a missing process for creating two buffer renumber Dictionary<int,Geometry> 
            buffers1reNumber = new Dictionary<int, Geometry>();
            buffers2reNumber = new Dictionary<int, Geometry>();
            /*foreach (var pair in headpair) {
                int idnew = 1000 + pair.Key;
                buffers1reNumber.Add(idnew, buffers1[pair.Key]);
                buffers2reNumber.Add(idnew, buffers2[pair.Value]);
            }*/
            for (int i = 0; i < buffers1.Count; i++)
            {
                int idnew = 1000 + i;
                buffers1reNumber.Add(idnew, buffers1[i]);
                buffers2reNumber.Add(idnew, buffers2[i]);
            }
            saveDictionaryGeom(buffers1reNumber, bufferpath1, idFieldName, polygonIO1.getSpatialRef());// To be able to use erase, create a new file to erase the data 
            saveDictionaryGeom(buffers2reNumber, bufferpath2, idFieldName, polygonIO2.getSpatialRef());
            createEraseBufferData(outputsection1, "section1", idFieldName, bufferpath1, orisectionpath1, polygonIO1.getSpatialRef());
            createEraseBufferData(outputsection2, "section2", idFieldName, bufferpath2, orisectionpath2, polygonIO2.getSpatialRef());
            // MatchLithIDForSections.MatchLayer matchLayer1 = new MatchLithIDForSections.MatchLayer(section1, outputsection1, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
            //MatchLithIDForSections.MatchLayer matchLayer2 = new MatchLithIDForSections.MatchLayer(section2, outputsection2, MatchLithIDForSections.gdalDriverType.SHP, idFieldName);
        }
        public static void maketemptranse(List<int[]> arrlist,out Dictionary<int, List<int>> listdic) {
            //用的一个简单的转换
            listdic = new Dictionary<int, List<int>>();
            for (int i=0;i< arrlist.Count;i++) {
                int[] arr = arrlist[i];
                List<int> line = new List<int>(arr);
                listdic.Add(i, line);
            }
        }
        /// <summary>
        /// Get a clone of an object (serialization and deserialization of binary)-- you need to flag serializable 
        /// </summary>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }
        static void saveDictionaryGeom(Dictionary<int, Geometry> buffers, string path, string idname, SpatialReference spatialReference)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.CreateDataSource(path, null);
            Layer layer = dataSource.CreateLayer("buffer", spatialReference, wkbGeometryType.wkbPolygon, null);
            FieldDefn fieldDefn = new FieldDefn(idname, FieldType.OFTInteger);
            layer.CreateField(fieldDefn, 1);
            Feature feature = new Feature(layer.GetLayerDefn());
            foreach (var vk in buffers)
            {
                int id = vk.Key;
                Geometry ge = vk.Value;
                feature.SetField(idname, id);
                feature.SetGeometry(ge);
                layer.CreateFeature(feature);
            }
            layer.Dispose();
            dataSource.Dispose();
        }
        static public  void createEraseBufferData(string outputpath, string layername, string idfieldname, string bufferpath, string sectionpath, SpatialReference spatialReference=null)
        {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource dataSource = driver.Open(bufferpath, 1);  
            Layer bufferlayer = dataSource.GetLayerByIndex(0); 
            DataSource dataSource1=driver.Open(sectionpath, 1);
            Layer sectionlayer = dataSource1.GetLayerByIndex(0);
            //OSGeo.OGR.Driver driver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource ds = driver.CreateDataSource(outputpath, null);
            if (spatialReference == null) {
                spatialReference = sectionlayer.GetSpatialRef();
            }
            Layer layer2 = ds.CreateLayer(layername, spatialReference, wkbGeometryType.wkbPolygon, null);
            sectionlayer.Erase(bufferlayer, layer2, null, null, null);
            // int t = (int)layer2.GetFeatureCount(0);
            long buffercount = bufferlayer.GetFeatureCount(1);
            Feature layer2feature = new Feature(layer2.GetLayerDefn());
            for (int i = 0; i < buffercount; i++)
            {
                Feature feature = bufferlayer.GetFeature(i);
                Geometry geometry = feature.GetGeometryRef();
                int lith = feature.GetFieldAsInteger(idfieldname);
                layer2feature.SetGeometry(geometry);
                layer2feature.SetField(idfieldname, lith);
                layer2.CreateFeature(layer2feature);
            }
            bufferlayer.Dispose();
            sectionlayer.Dispose();
            layer2.Dispose();
            dataSource.Dispose();
            dataSource1.Dispose();
            ds.Dispose();
        }
        static void createBuffersByTouches(Dictionary<int ,Geometry> arcs, Dictionary<int, List<int>> pointid_arclist,out Dictionary<int ,Geometry>buffers,double distance, int quadeses=30) {
            buffers = new Dictionary<int, Geometry>();
           
            foreach (var point_arcs in pointid_arclist) {
                List<Geometry> lines = new List<Geometry>();
                int idtemp = point_arcs.Key;
                List<int> lineids = point_arcs.Value;
                foreach (int lineid in lineids) {
                    lines.Add(arcs[lineid]);
                }
                Geometry union = new Geometry(wkbGeometryType.wkbPolygon);
                foreach (Geometry ge in lines)
                {
                    Geometry genew = ge.Buffer(distance, quadeses);
                    union = union.Union(genew);
                }
                buffers.Add(point_arcs.Key, union);
            }
            
        }
       
        static int find(int x,ref Dictionary<int, int> father,ref Dictionary<int, List<int>> pointid_arclist) {
            if (x == father[x]) return x;
            else {
                int fx = find(father[x], ref father, ref pointid_arclist);
                if (pointid_arclist.ContainsKey(x))
                {
                    pointid_arclist[fx].AddRange(pointid_arclist[x]);//I'm going to erase the nodes represented by x
                    pointid_arclist.Remove(x);
                }
                father[x] = fx;
                return father[x];
            }
        }

    }
}
