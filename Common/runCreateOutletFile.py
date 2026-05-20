import arcpy
arcpy.env.overwriteOutput = True

out_dir = arcpy.GetParameterAsText(0)
out_name  = arcpy.GetParameterAsText(1)
geom_type  = arcpy.GetParameterAsText(2)
with_sub = arcpy.GetParameterAsText(3)

with arcpy.EnvManager(workspace=out_dir):
    # enable z values so can load MapPoint
    arcpy.management.CreateFeatureclass(out_dir, out_name, geom_type, None, "DISABLED", "DISABLED")
    fields = [['ID', 'LONG', None, None, None, None],
         ['INLET', 'SHORT', None, None, None, None],
         ['RES', 'SHORT', None, None, None, None],
         ['PTSOURCE', 'SHORT', None, None, None, None]]
    if with_sub == "true":
        fields.append(['Subbasin', 'LONG', None, None, None, None])
    arcpy.management.AddFields(out_name, fields)

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of {0}/{1} complete.".format(out_dir, out_name))

