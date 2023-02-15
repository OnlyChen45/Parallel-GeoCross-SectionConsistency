
import  arcpy
import  sys
import  os
import time
def polyToline(inputpath,outputpath):{
    arcpy.PolygonToLine_management(inputpath,
                                   outputpath,
                                   "IGNORE_NEIGHBORS")

}
def lineTopoly(inputpath,outputpath):
    arcpy.FeatureToPolygon_management(inputpath,
                                        outputpath)

def polyToLineToPoly(inputpath,midpath,outputpath):
    polyToline(inputpath,midpath)
    lineTopoly(midpath,outputpath)


# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    print('Use The Arcpy')

    #arcpy.env.workspace = "C:/data"
    #inputpath=r'D:\temp\workspace\polyTopoly.gdb\text2'
    #midpath=r'D:\temp\workspace\polyTopoly.gdb\text2Toline2'
    #outputpath=r'D:\temp\workspace\polyTopoly.gdb\text2Topoly'
    #polyToLineToPoly(inputpath,midpath,outputpath)
    inputshppath= sys.argv[1]
    outputshppath=sys.argv[2]
    (inputfolder,inputfilename)=os.path.split(inputshppath)#make all the path string useful
    (outputfolder,outputfilename)=os.path.split(outputshppath)
    (inputname, inputsuffix) = os.path.splitext(inputfilename)
    (outputname, outputsuffix) = os.path.splitext(outputfilename)
    length1=len(sys.argv)
    workspacegdb=r'D:\temp\workspace\polyTopoly.gdb'

    if (length1>=4):
        workspacegdb=sys.argv[3]
    print ('Workspace is ' + workspacegdb)
    ttt = time.time()
    time_string = time.strftime("%Y%m%d_%H%M%S", time.localtime(ttt))
    gdbinputname = inputname+time_string
    midname='line_mid_'+gdbinputname
    resultname='poly_result_'+gdbinputname
    arcpy.FeatureClassToFeatureClass_conversion(inputshppath,workspacegdb,gdbinputname)
    polyToLineToPoly(workspacegdb+'\\'+gdbinputname,workspacegdb+'\\'+midname,workspacegdb+'\\'+resultname)

    arcpy.FeatureClassToFeatureClass_conversion(workspacegdb+'\\'+resultname,outputfolder,outputname)
    arcpy.Delete_management(workspacegdb+'\\'+gdbinputname)
    arcpy.Delete_management(workspacegdb+'\\'+midname)
    arcpy.Delete_management(workspacegdb+'\\'+resultname)
    print ('Done')
    # splitgdb=r'D:\arcgisWorkspace\splitResult.gdb'
    # arcpy.env.workspace=splitgdb
    # midgdb=r'D:\arcgisWorkspace\polytoline.gdb'
    # resultgdb=r'D:\arcgisWorkspace\finalresult.gdb'
    # splitfcs = arcpy.ListFeatureClasses()#this is the namelist in the gdb
    # for fc in splitfcs:
    #     inputpath=splitgdb+'\\'+fc
    #     midpath=midgdb+'\\'+fc+'_PTL'
    #     outputpath=resultgdb+'\\'+fc
    #     polyToLineToPoly(inputpath, midpath, outputpath)




