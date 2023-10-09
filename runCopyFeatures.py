import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "c:/data"


in_features = arcpy.GetParameterAsText(0)
out_feature_class = arcpy.GetParameterAsText(1)

arcpy.management.CopyFeatures(in_features, out_feature_class)

# The following message will be included in the message box from the calling button's OnClick routine
print("Copying of shapefile {0} to feature class complete.".format(in_features))
