
import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = arcpy.GetParameterAsText(0)

in_shp  = arcpy.GetParameterAsText(1)
out_folder = arcpy.GetParameterAsText(2)


arcpy.conversion.FeatureClassToShapefile(in_shp, out_folder)

# The following message will be included in the message box from the calling button's OnClick routine
print("{0} converted to shapefile.", in_shp)