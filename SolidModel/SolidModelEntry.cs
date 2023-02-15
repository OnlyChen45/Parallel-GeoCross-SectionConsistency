using GeoCommon;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace SolidModel
{
    public class SolidModelEntry
    {


        /// <summary>
        /// 主函数
        /// </summary>
        /// <returns></returns>
        public bool ModelBuild(string shpFilefolder,int  shpnumber,string resultobjFolder,string resultname,string processDatafolder)
        {
            /* //1、获取轮廓线
             DataIO pDataIOHelper = new DataIO();
             List<Contour> pContours_A = pDataIOHelper.GetContour(shpFile1);
             List<Contour> pContours_B = pDataIOHelper.GetContour(shpFile2);

             //2、构建模型三角网
             ContourHelp pContourHelper = new ContourHelp(pContours_A, pContours_B);
             pContourHelper.SolidModelBuild();

             //3、输出OBJ模型
             BrepModelHelp pBrepModelHelper = new BrepModelHelp(pContourHelper.pBrepModelList);
             pBrepModelHelper.ExportToObj(@"D:\graduateGIS\water3D\objresult", "01obj");*/

            // DataIO pDataIOHelper = new DataIO();
            List<double> zList;//获取各个轮廓的纵轴z坐标，可以用来排序
            Dictionary<double, List<Eage>> eagesDictionary;//获取各个轮廓的以纵轴坐标索引的Eages
            BrepModel resultmodel = new BrepModel();//这个记录最终结果

            //将整个文件夹下所有的shp读取并根据z生成dictionary
            ReadAndSortSHP readAndSortSHP = new ReadAndSortSHP(shpFilefolder);
            bool parallelmark= readAndSortSHP.getEageDictionary(out zList, out eagesDictionary);
            if (parallelmark == false) {
                Console.WriteLine("输入数据不平行");
                Console.ReadLine();
            }

            //把读到的数据输入到综合处理对象中，然后获得分散的结果存在brepModels中
            List<BrepModel> brepModels = new List<BrepModel>();
            BulkBuildModels bulkBuildModels = new BulkBuildModels();
            bulkBuildModels.bulkBuild(zList, eagesDictionary, out brepModels);
            BrepModelHelp brepModelHelp1 = new BrepModelHelp(brepModels);
            brepModelHelp1.ExportToObj(@"D:\graduateGIS\water3D\finalobj", "midresult");
            //把读取的数据整合成一个大的model，然后输出
            OutputWorker outputWorker = new OutputWorker();
            outputWorker.assembleBrep(brepModels);
            List<BrepModel> breptemplist = new List<BrepModel>();
            breptemplist.Add(outputWorker.finalbrepModel);
            BrepModelHelp brepModelHelp = new BrepModelHelp(breptemplist);
            brepModelHelp.ExportToObj(resultobjFolder, resultname);

            /*for (int i = 0; i < shpnumber-1; i++) {
                string shpFile1 = shpFilepath + i.ToString() + ".shp";
                string shpFile2 = shpFilepath + (i+1).ToString() + ".shp";

                List<Contour> pContours_A = pDataIOHelper.GetContour(shpFile1);
                List<Contour> pContours_B = pDataIOHelper.GetContour(shpFile2);
                int[,] Atype;
                int[,] Btype;
                ContourHelp pContourHelper = new ContourHelp(pContours_A, pContours_B,out Atype ,out Btype);
                pContourHelper.SolidModelBuild();//主要构造用的函数
                List<BrepModel> brepModels = pContourHelper.pBrepModelList;
                for (int j = 0; j < brepModels.Count(); j++) {
                    BrepModel brep = brepModels[j];
                    //Hashtable vertexTable = brep.vertexTable;
                    for (int k = 0; k < brep.triangleList.Count; k++) {
                        Triangle triangle =(Triangle) brep.triangleList[k];
                        Vertex v0 = brep.GetVertexByIndex(triangle.v0);
                        Vertex v1 = brep.GetVertexByIndex(triangle.v1);
                        Vertex v2 = brep.GetVertexByIndex(triangle.v2);
                        resultmodel.addTriangle(v0.x, v0.y, v0.z, v1.x, v1.y, v1.z, v2.x, v2.y, v2.z);
                    }

                }
            }*/

            return true;
        }

        
    }
}
