import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "c:/data"
from arcpy.sa import *
import os

in_raster = arcpy.GetParameterAsText(0)
in_stream = arcpy.GetParameterAsText(1)
out_raster = arcpy.GetParameterAsText(2)
cell_size = arcpy.GetParameterAsText(3)
reduction = arcpy.GetParameterAsText(4)

arcpy.env.extent = in_raster

temp_ras = os.path.join(arcpy.env.workspace, "temp.tif")
arcpy.conversion.FeatureToRaster(in_stream, "FID", temp_ras, int(cell_size))
out_ras = Con(IsNull(Raster("{0}".format(temp_ras))), Raster("{0}".format(in_raster)), Raster("{0}".format(in_raster)) - int(reduction))
out_ras.save(out_raster)

# The following message will be included in the message box from the calling button's OnClick routine
print("Burn in of {0} to {1} making {2} complete.", in_stream, in_raster, out_raster)
