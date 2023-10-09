import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "C:/data"

in_shp  = arcpy.GetParameterAsText(0)
field = arcpy.GetParameterAsText(1)
out_raster = arcpy.GetParameterAsText(2)
dem_raster = arcpy.GetParameterAsText(3)


arcpy.conversion.FeatureToRaster(in_shp, field, out_raster, dem_raster)

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of {0} complete.", out_raster)

