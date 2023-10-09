@echo off
SET OSGEO4W_ROOT=C:\Program Files\QGIS 3.22.12
SET QGISNAME=qgis-ltr
SET QGIS=%OSGEO4W_ROOT%\apps\%QGISNAME%

CALL "%OSGEO4W_ROOT%\bin\o4w_env.bat"

SET PYTHONPATH=%QGIS%\python

"%OSGEO4W_ROOT%\bin\python3.exe" "%USERPROFILE%\AppData\Roaming\QGIS\QGIS3\profiles\default\python\plugins\QSWATPlus3_9\QSWATPlus\swatgraph.py" %1 %2






