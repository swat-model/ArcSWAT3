import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "c:/data"

in_file = arcpy.GetParameterAsText(0)
overwrite = arcpy.GetParameterAsText(1)

arcpy.BuildRasterAttributeTable_management(in_file, overwrite)

# The following message will be included in the message box from the calling button's OnClick routine
print("Addition of attribute table to {0} complete.".format(in_file))
