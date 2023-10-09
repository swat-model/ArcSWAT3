import arcpy
arcpy.env.overwriteOutput = True
# arcpy.env.workspace = "C:/data"

out_dir = arcpy.GetParameterAsText(0)
out_name  = arcpy.GetParameterAsText(1)
geom_type  = arcpy.GetParameterAsText(2)

with arcpy.EnvManager(workspace=out_dir):
    arcpy.management.CreateFeatureclass(out_dir, out_name, geom_type)
    fields = [['PolygonId', 'LONG', None, None, None, None],
         ['Area', 'DOUBLE', None, None, None, None],
         ['Subbasin', 'LONG', None, None, None, None]]
    arcpy.management.AddFields(out_name, fields)

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of {0}/{1} complete.".format(out_dir, out_name))

