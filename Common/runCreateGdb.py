import arcpy
arcpy.env.overwriteOutput = True

gdb_dir = arcpy.GetParameterAsText(0)
gdb_name = arcpy.GetParameterAsText(1)

arcpy.management.CreateFileGDB(gdb_dir, gdb_name)

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of file geodatabase {1} in  {0} complete.".format(gdb_dir, gdb_name))

