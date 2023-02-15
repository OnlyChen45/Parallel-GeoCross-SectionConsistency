using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.IO;
using System.Runtime.InteropServices;
namespace ThreeDModelSystemForSection
{   public struct gdalDriverType {
        public const string GDB = "FileGDB";
        public const string SHP = "ESRI Shapefile";
    } 
    /// <summary>
    /// 匹配两个shp让他们中间的相同geom完全一样，数据IO层
    /// </summary>
    public class MatchLayer
    {
        /* [DllImport("gdal302.dll", EntryPoint = "OGR_F_GetFieldAsBinary", CallingConvention = CallingConvention.Cdecl)]
         public extern static System.IntPtr OGR_F_GetFieldAsBinary(HandleRef handle, int index, out int byteCount);*/
        [DllImport("gdal303.dll", EntryPoint = "OGR_F_GetFieldAsBinary", CallingConvention = CallingConvention.Cdecl)]
        public extern static System.IntPtr OGR_F_GetFieldAsBinary(HandleRef handle, int index, out int byteCount);
        public MatchLayer(string sourcepath,string targetpath,string drivername ,string idFileName) {
            OSGeo.OGR.Driver driver = Ogr.GetDriverByName(drivername);
            Layer layersource = null, layertarget = null;
            DataSource ds1=null;
                 DataSource ds2=null;
            switch (drivername) {
                case gdalDriverType.GDB: {
                        string folder1 = Path.GetFullPath(sourcepath);
                        string file1 = Path.GetFileName(sourcepath);
                        string folder2 = Path.GetFullPath(targetpath);
                        string file2 = Path.GetFileName(targetpath);
                       ds1 = driver.Open(folder1, 1);
                        layersource = ds1.GetLayerByName(file1);
                        ds2 = driver.Open(folder2,1);
                        layertarget = ds2.GetLayerByName(file2);
                        break;
                    }
                case gdalDriverType.SHP:{
                        ds1 = driver.Open(sourcepath,1);
                        layersource = ds1.GetLayerByIndex(0);
                        ds2 = driver.Open(targetpath, 1);
                        layertarget = ds2.GetLayerByIndex(0);
                    break;
                }            
            }
            matchId(layersource, layertarget, idFileName);
            layersource.Dispose();
            layertarget.Dispose();
            ds1.Dispose();
            ds2.Dispose();
        }
        public void matchId(Layer layersource,Layer layertarget,string idFieldName) {
           /* Feature featuret= layertarget.GetFeature(0);
            int featurecount= featuret.GetFieldCount();
            bool idfieldexist = false;
            for (int i = 0; i < featurecount; i++) {
                FieldDefn fieldDefn = featuret.GetFieldDefnRef(i);
                string fieldname = fieldDefn.GetName();
                if (idFieldName.Equals(fieldname)) {
                    idfieldexist = true;
                    break;
                }
            }*/
            List<FieldDefn> fieldlist;
            getFieldList(layersource, out fieldlist);
            int fieldcount = fieldlist.Count;
            for (int i = 0; i < fieldcount; i++)
            {
                string fieldname=  fieldlist[i].GetName();
                int tempindex= layertarget.FindFieldIndex(fieldname, -1);
                if (tempindex == -1)
                {
                    layertarget.CreateField(fieldlist[i], 1);
                }
            }
           // featuret.Dispose();
           /* if (idfieldexist == false) {//如果目标面没有idfield，那么就添加进去
                FieldDefn fielddefn = new FieldDefn(idFieldName,FieldType.OFTInteger);
                layertarget.CreateField(fielddefn,1);
            }*/
            int featurecountSource = (int)layersource.GetFeatureCount(1);
            int featurecountTarget = (int)layertarget.GetFeatureCount(1);
            Dictionary<int, Geometry> geominSource = new Dictionary<int, Geometry>();
            Dictionary<int, int> id_index = new Dictionary<int, int>();
            for (int j = 0; j < featurecountSource; j++)
            {
                Feature feature = layersource.GetFeature(j);
                Geometry geom = feature.GetGeometryRef();
                int id = feature.GetFieldAsInteger(idFieldName);
                geominSource.Add(id, geom);
                id_index.Add(id, j);
            }
            for (int i = 0; i < featurecountTarget; i++) {
                double maxarea = -1;
                int idright = -1;
                Feature featuretarget = layertarget.GetFeature(i);
                Geometry geomtarget = featuretarget.GetGeometryRef();
                foreach (var vk in geominSource) {
                    int id = vk.Key;
                    Geometry geom = vk.Value;
                    Geometry inter = geom.Intersection(geomtarget);
                    if (inter.IsEmpty()) {
                        continue;
                    }
                    double area = inter.Area();
                    if (area > maxarea) {
                        idright = id;
                        maxarea = area;
                    }
                }
                featuretarget.SetField(idFieldName,idright);
                Feature feature = layersource.GetFeature(id_index[idright]);
                for (int j = 0; j < fieldcount; j++)
                {
                    FieldDefn fieldDefn = fieldlist[j];
                    string fieldname = fieldDefn.GetName();
                    FieldType fieldType = fieldDefn.GetFieldType();
                    switch (fieldType)
                    {
                        case FieldType.OFTInteger:
                            {
                                int fieldvalue = feature.GetFieldAsInteger(fieldname);
                                featuretarget.SetField(fieldname, fieldvalue);
                                break;
                            }
                        case FieldType.OFTString:
                            {
                                //string fieldvalue = feature.GetFieldAsString(j);
                                //featureout.SetField(j, fieldvalue);
                                int index = feature.GetFieldIndex(fieldname);
                                int byteCount;
                                IntPtr intPtr = OGR_F_GetFieldAsBinary(Feature.getCPtr(feature), index, out byteCount);
                                if (intPtr == IntPtr.Zero)
                                {
                                    featuretarget.SetField(fieldname, "");
                                    break;
                                }
                                byte[] bytearray = new byte[byteCount];
                                Marshal.Copy(intPtr, bytearray, 0, byteCount);
                                string s = Encoding.UTF8.GetString(bytearray);
                                //Console.WriteLine(s);

                                featuretarget.SetField(fieldname, s);
                                break;
                            }
                        case FieldType.OFTReal:
                            {
                                double fieldvalue = feature.GetFieldAsDouble(fieldname);
                                featuretarget.SetField(fieldname, fieldvalue);
                                break;
                            }
                    }
                }
                layertarget.SetFeature(featuretarget);
            }
            layertarget.Dispose();

        }
        private void getFieldList(Layer layer, out List<FieldDefn> fieldlist)
        {
            fieldlist = new List<FieldDefn>();
            //  Feature feature = new Feature(layer.GetLayerDefn());
            Feature feature = layer.GetFeature(0);
            int fieldcount = feature.GetFieldCount();
            for (int i = 0; i < fieldcount; i++)
            {
                FieldDefn fieldDefn = feature.GetFieldDefnRef(i);
                fieldlist.Add(fieldDefn);
            }
        }

    }
}
