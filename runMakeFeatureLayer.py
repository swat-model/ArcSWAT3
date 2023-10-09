import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "c:/data"

in_features = arcpy.GetParameterAsText(0)
out_layer = arcpy.GetParameterAsText(1)

arcpy.MakeFeatureLayer_management(in_features, out_layer)

# The following message will be included in the message box from the calling button's OnClick routine
print("Made feature layer {0}.", out_layer)

