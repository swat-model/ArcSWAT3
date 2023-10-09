import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "C:/data"
import os

dem_raster = arcpy.GetParameterAsText(0)
in_shp  = arcpy.GetParameterAsText(1)
field = arcpy.GetParameterAsText(2)
out_raster = arcpy.GetParameterAsText(3)

out_dir  = os.path.split(out_raster)[0]

with arcpy.EnvManager(workspace=out_dir):
    arcpy.management.GenerateRasterFromRasterFunction(
        'Rasterize Features',
        out_raster,
        r"Raster {0}; 'Input Features' {1}; Field {2}; 'Resolve Overlap Method' First".format(dem_raster, in_shp, field))

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of {0} complete.", out_raster)

