import arcpy
arcpy.env.overwriteOutput = True
# arcpy.env.workspace = "C:/data"

out_dir = arcpy.GetParameterAsText(0)
out_name  = arcpy.GetParameterAsText(1)
geom_type  = arcpy.GetParameterAsText(2)

with arcpy.EnvManager(workspace=out_dir):
    arcpy.management.CreateFeatureclass(out_dir, out_name, geom_type)
    fields = [['Subbasin', 'LONG', None, None, None, None],
              ['Area', 'DOUBLE', None, None, None, None],
              ['Slo1', 'DOUBLE', None, None, None, None],
              ['Len1', 'DOUBLE', None, None, None, None],
              ['Sll', 'DOUBLE', None, None, None, None],
              ['Csl', 'DOUBLE', None, None, None, None],
              ['Wid1', 'DOUBLE', None, None, None, None],
              ['Dep1', 'DOUBLE', None, None, None, None],
              ['Lat', 'DOUBLE', None, None, None, None],
              ['Long_', 'DOUBLE', None, None, None, None],
              ['Elev', 'DOUBLE', None, None, None, None],
              ['ElevMin', 'DOUBLE', None, None, None, None],
              ['ElevMax', 'DOUBLE', None, None, None, None],
              ['Bname', 'TEXT', None, None, None, None],
              ['Shape_Len', 'DOUBLE', None, None, None, None],
              ['Shape_Area', 'DOUBLE', None, None, None, None],
              ['HydroID', 'LONG', None, None, None, None],
              ['OutletID', 'LONG', None, None, None, None]]
    arcpy.management.AddFields(out_name, fields)

# The following message will be included in the message box from the calling button's OnClick routine
print("Creation of {0}/{1} complete.".format(out_dir, out_name))

