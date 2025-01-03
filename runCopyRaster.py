import arcpy
arcpy.env.overwriteOutput = True


file_name = arcpy.GetParameterAsText(0)
gdb = arcpy.GetParameterAsText(1)
dataset_name = arcpy.GetParameterAsText(2)

gdb_ref = "{0}\\{1}".format(gdb, dataset_name)
arcpy.management.CopyRaster(file_name, gdb_ref)

# The following message will be included in the message box from the calling button's OnClick routine
print("Copying of raster {0} to geodatabase as {1} complete.".format(file_name, dataset_name))

