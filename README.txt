Issues with ArcSWAT3

24 January 2024
Experience with Arun Bawa in installing and running ArcSWAT3
1.  His installation of ArcGIS Pro was not in C:\Program Files, so the path to the ArcGIS Pro .dll files 
was wrong.  Fixed by global edit of paths in ArcSWAT3.csproj, replacing 
C:\Program Files\ArcGIS\Pro\bin
Another consequence is that the the Debug path in Visual Studio is wrong, but this only seems to be a Visual Studio problem
if you try to use it to debug.
2.  Not having used the old ArcSWAT he had no SWAT Editor.  Needs to be installed.
He also lacked C:\SWAT\SWATEditor\Databases\ArcSWATProj2012.mdb
(which might come with editor: need to check)
