import arcpy
arcpy.env.overwriteOutput = True

in_grid = arcpy.GetParameterAsText(0)

arcpy.management.CalculateStatistics(in_grid)

# The following message will be included in the message box from the calling button's OnClick routine
print("Calculation of statistics for {0} complete.".format(in_grid))
