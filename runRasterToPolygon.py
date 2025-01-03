import arcpy
arcpy.env.overwriteOutput = True
import os

in_raster = arcpy.GetParameterAsText(0)
out_shp  = arcpy.GetParameterAsText(1)

out_dir  = os.path.split(out_shp)[0]

out_shp0 = os.path.splitext(out_shp)[0] + '_temp.shp'

with arcpy.EnvManager(workspace=out_dir):
    arcpy.conversion.RasterToPolygon(in_raster, out_shp0)
    arcpy.management.Dissolve(out_shp0, 
                              out_shp,
                              "gridcode",
                              "",
                              "MULTI_PART",
                              "DISSOLVE_LINES")

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of {0} complete.", out_shp)

