import arcpy
arcpy.env.overwriteOutput = True

in_table = arcpy.GetParameterAsText(0)
keep_field = arcpy.GetParameterAsText(1)

arcpy.DeleteField_management(in_table, ["{0}".format(keep_field)] , "KEEP_FIELDS")

# The following message will be included in the message box from the calling button's OnClick routine
print("Deletion of fields from {0} complete.".format(in_table))

