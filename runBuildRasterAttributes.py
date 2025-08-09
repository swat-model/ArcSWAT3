﻿import arcpy
arcpy.env.overwriteOutput = True

in_file = arcpy.GetParameterAsText(0)
overwrite = arcpy.GetParameterAsText(1)

try: 
    arcpy.BuildRasterAttributeTable_management(in_file, overwrite)
except:
    pass

# The following message will be included in the message box from the calling button's OnClick routine
print("Addition of attribute table to {0} complete.".format(in_file))
