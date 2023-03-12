# Parallel-GeoCross-SectionConsistency
## Description
  Parallel-GeoCross-SectionConsistency is a desktop software developed based on C#.Net and Python. It realizes the topological consistency of parallel geological section data and the section comparison algorithm 3D modeling function and visualization. In geological 3D modeling work, it is tedious and inefficient to manually find the correspondence of each boundary line in the face of relatively complex geological profile data. This software provides a solution on the desktop side.
## Dependency
-.NET Framework 4.7.2

-Dotspatial 1.7

-GDAL 3.7.2

-Triangle.Net (https://github.com/wo80/Triangle.NET)

-ArcPy

    GDAL, Dotspatial, and Triangle.Net have been localized and integrated into the program, while ArcPy, as commercial software, needs to be installed by the user, and this software mainly calls the external python interface provided by it. GDAL's C# release Geometry. The intersection method, when processing the same set of data for spatial superposition several times, will, Therefore, the author had to use CMD to call the relevant methods in Arcpy in the background to remove these error points. Due to the development cycle of the software, the authors have not yet written a module to replace the method in Arcpy, so they have to use this commercial module for the time being. In addition, the call to Arcpy requires that every character in the file path is in the Ascii table.
## Installation
  Download the project library directly to install it. It is recommended to use Visual Studio 2019 to open it. Environment configuration should be done in the menu of the software.
## Input
  .shp file, 2D spatial data of type Polygon, containing profile data, and a short int field to identify the id of the stratum in the profile
	.txt file, which exists as a parameter file that records the location of the profile in the corresponding shp file. It should have the same filename as the shp being tagged. Its contents should be as follows.

startX:39626645.2191
startY:3635185.14616
startZ:34
endX:39626645.2275
endY:3634684.51115
firstX:39626153.644
firstY:3634473.557

The parameters are illustrated as
![](https://github.com/OnlyChen45/Parallel-GeoCross-SectionConsistency/blob/master/media/image1.png)

## Use
### Configure the usage environment
  Arcpy Path is the python runtime program corresponding to Arcgis installed on the local machine; the authors recommend using python 2.7, which comes with ArcGIS 10.2. ‘The formation identifies the field name’ is the id of the formation in the shp file. Data loading mode is the mode of input data. Model1 is selected by default; no other mode has been developed yet.
![](https://github.com/OnlyChen45/Parallel-GeoCross-SectionConsistency/blob/master/media/image2.png)

### Handling stratigraphic branches
  Stratum data can be tested using the path . /TestData/Branching/ for testing.
	Click on the SectionConsistency tab and select Branching in the menu to call up the method box. Click the File button to select the test data, and click the plus button to add it to the processing box after each selection. The group in the processing box symbolizes pairs of data, which are automatically grouped in order as a set of 2.
	Finally, select the result folder and click ok to start the program automatically.
 ![](https://github.com/OnlyChen45/Parallel-GeoCross-SectionConsistency/blob/master/media/image3.png)
 
### Dealing with pinch-out strata
  The stratum data can be tested using the path ./TestData/Pinch-out/.
	Click on the SectionConsistency tab and select pinch-out in the menu to call up the method box. Click the File button to select the test data, and click the plus button to add it to the processing box after each selection. The group in the processing box symbolizes data pairs, automatically grouped in order as a set of 2.
	Finally, select the result folder and click ok to start the program automatically.
 ![](https://github.com/OnlyChen45/Parallel-GeoCross-SectionConsistency/blob/master/media/image4.png)
 
### Handling of non-corresponding arcs
  The stratum data can be tested using the path . / TestData/No-Corresponding/ for testing.
	Click on the SectionConsistency tab and select No-corresponding Arcs in the menu to call up the method box. Click the File button to select the test data, and click the plus button to add it to the processing box after each selection. The group in the processing box symbolizes pairs of data, and the data are automatically grouped in order as a set of 2.
	Finally, select the result folder and click ok to start the program automatically.
 ![](https://github.com/OnlyChen45/Parallel-GeoCross-SectionConsistency/blob/master/media/image5.png)
 
### Three-dimensional modeling
  The stratum data can be tested using the path . / TestData/3Dmodeling/ for testing.
  3D modeling requires shp files and their corresponding txt file; first convert the 2D shp files to 3D coordinates. Use the Trans2DTo3D tab in the menu.
 ![](https://github.com/OnlyChen45/Parallel-GeoCross-SectionConsistency/blob/master/media/image6.png)
  When the conversion is successful, the data will be automatically populated into the modeling queue, and the modeling can be executed by clicking on the 3Dmodeling tab.
