import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "c:/data"

in_file = arcpy.GetParameterAsText(0)

arcpy.management.AddFields(in_file, 
            [['SubbasinR', "LONG", None, '', ''],
            ['AreaC', "DOUBLE", None, '', ''],
            ['Len2', "DOUBLE", None, '', ''],
            ['Slo2', "DOUBLE", None, '', ''],
            ['Wid2', "DOUBLE", None, '', ''],
            ['Dep2', "DOUBLE", None, '', ''],
            ['MinEl', "DOUBLE", None, '', ''],
            ['MaxEl', "DOUBLE", None, '', ''],
            ['Shape_Len', "DOUBLE", None, '', ''],
            ['HydroID', "LONG", None, '', ''],
            ['OutletID', "LONG", None, '', '']])

# The following message will be included in the message box from the calling button's OnClick routine
print("Addition of fields to {0} complete.", in_file)
