import arcpy
arcpy.env.overwriteOutput = True

in_file = arcpy.GetParameterAsText(0)
field_name = arcpy.GetParameterAsText(1)
field_type = arcpy.GetParameterAsText(2)

# add field unless already has it
fields = arcpy.ListFields(in_file, field_name)
count = len(fields)
if count == 0:
    arcpy.management.AddField(in_file, field_name, field_type)

# The following message will be included in the message box from the calling button's OnClick routine
print("Addition of {0} field {1} to {2} complete: count was {3}.".format(field_type, field_name, in_file, count))
