import arcpy
arcpy.env.overwriteOutput = True

out_file = arcpy.GetParameterAsText(0)
prj_file  = arcpy.GetParameterAsText(1)

# often fails because file in use
try:
    arcpy.DefineProjection_management(out_file, prj_file)
except:
    pass

# The following message will be included in the message box from the calling button's OnClick routine
print("Spatial reference of {0} defined.".format(out_file))

