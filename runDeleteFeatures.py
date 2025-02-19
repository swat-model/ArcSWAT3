import arcpy
arcpy.env.overwriteOutput = True

in_features = arcpy.GetParameterAsText(0)

arcpy.DeleteFeatures_management(in_features)

# The following message will be included in the message box from the calling button's OnClick routine
print("Deletion of selected features from {0} complete.".format(in_features))

