using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MaxRev.Gdal.Core;

namespace ArcSWAT3
{
    internal class ArcSWAT3StartButton : ArcGIS.Desktop.Framework.Contracts.Button
    {
        protected async override void OnClick()
        {
            try
            {
                // Inform user that add-in is about to call Python script.
                // ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Click OK to start script, and wait for completion messagebox.", "Info");
                // Create and format path to Project
                GdalBase.ConfigureAll();
                MaxRev.Gdal.Core.Proj.Configure();
                var arcSWATProj = new ArcSWAT();
                await arcSWATProj.run();
                //var pathProExe = System.IO.Path.GetDirectoryName((new System.Uri(Assembly.GetEntryAssembly().Location)).AbsolutePath);
                //if (pathProExe == null) return;
                //pathProExe = Uri.UnescapeDataString(pathProExe);
                //pathProExe = System.IO.Path.Combine(pathProExe, @"Python\envs\arcswat_env");
                // produces ~/AppData/Local/Programs/ArcGIS/Pro/bin/Python/envs/arcswat_env
                //Utils.information("pathProExe: " + pathProExe, false);
                // Create and format path to Python
                // This is where the add-in file ArcSWAT.esriAddinX is stored
                //var pathPython = System.IO.Path.GetDirectoryName(new System.Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);
                //if (pathPython == null) return;
                //pathPython = Uri.UnescapeDataString(pathPython);
                // produces ~/AppData/Local/ESRI/ArcGISPro/AssemblyCache/{...}
                //Utils.information(pathPython, false);
                //// Create and format process command string.
                //// NOTE:  Path to Python script is below, "K:\Users\Public\QSWAT3\QSWAT3\runArcSWAT.py", which can be kept or updated based on the location you place it.
                //var myCommand = String.Format(@"/c """"{0}"" ""{1}""""",
                //    System.IO.Path.Combine(pathProExe, "python.exe"),
                //    System.IO.Path.Combine(pathPython, @"K:\Users\Public\ArcSWAT\runArcSWAT.py"));
                //// Create process start info, with instruction settings
                //var procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", myCommand);
                ////procStartInfo.RedirectStandardOutput = true;
                ////procStartInfo.RedirectStandardError = true;
                //procStartInfo.UseShellExecute = false;
                //procStartInfo.CreateNoWindow = true;
                //// Create process and start it
                //System.Diagnostics.Process proc = new System.Diagnostics.Process();
                //proc.StartInfo = procStartInfo;
                //proc.Start();
                //// Create and format result string
                ////string result = proc.StandardOutput.ReadToEnd();
                ////string error = proc.StandardError.ReadToEnd();
                ////if (!string.IsNullOrEmpty(error)) result += String.Format("{0} Error: {1}", result, error);
                //// Show result string
                ////ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(result, "Info");
            }

            catch (Exception exc) {
                // Catch any exception found and display in a message box
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception caught while trying to run ArcSWAT: " + exc.Message);
                return;
            }
        }
    }
}
