import arcpy
arcpy.env.overwriteOutput = True

in_features = arcpy.GetParameterAsText(0)
selection_type = arcpy.GetParameterAsText(1)
where_clause = arcpy.GetParameterAsText(2)

result = arcpy.management.SelectLayerByAttribute(in_features, selection_type, where_clause)

# The following message will be included in the message box from the calling button's OnClick routine
print("Selected {0} features.".format(result[1]))

