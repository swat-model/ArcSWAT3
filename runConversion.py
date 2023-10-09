import arcpy
arcpy.env.overwriteOutput = True
arcpy.env.workspace = "c:/data"

in_grid = arcpy.GetParameterAsText(0)
out_folder = arcpy.GetParameterAsText(1)

arcpy.conversion.RasterToOtherFormat(in_grid, out_folder, "TIFF")

# The following message will be included in the message box from the calling button's OnClick routine
print("Conversion of {0} to TIFF {1} complete.".format(in_grid, out_folder))