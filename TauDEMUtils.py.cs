

using System;

using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace ArcSWAT3 {
    
 
    
    // Methods for calling TauDEM executables.
    public class TauDEMUtils {
        
        // Run PitFill.
        public static async Task<bool> runPitFill(string demFile, string felFile, int numProcesses, TextBox output) {
            return await TauDEMUtils.run("PitRemove", new List<Tuple<string, string>> {
                new Tuple<string, string>("-z", demFile)
            }, new List<Tuple<string, string>>(), new List<Tuple<string, string>> {
                new Tuple<string, string>("-fel", felFile)
            }, numProcesses, output, false);
        }
        
        // Run D8FlowDir.
        
        public static async Task<bool> runD8FlowDir(
            string felFile,
            string sd8File,
            string pFile,
            int numProcesses,
            TextBox output) {
            return await TauDEMUtils.run("D8FlowDir", new List<Tuple<string, string>> {
                new Tuple<string, string>("-fel", felFile)
            }, new List<Tuple<string, string>> (), new List<Tuple<string, string>> {
                new Tuple<string, string>("-sd8", sd8File),
                new Tuple < string, string >("-p", pFile)
            }, numProcesses, output, false);
        }
        
        // Run DinfFlowDir.
        
        public static async Task<bool> runDinfFlowDir(
            string felFile,
            string slpFile,
            string angFile,
            int numProcesses,
            TextBox output) {
            return await TauDEMUtils.run("DinfFlowDir", new List<Tuple<string, string>> {
                new Tuple < string, string >("-fel", felFile)
            }, new List<Tuple<string, string>> (), new List<Tuple<string, string>> {
                new Tuple<string, string>("-slp", slpFile),
                new Tuple < string, string >("-ang", angFile)
            }, numProcesses, output, false);
        }
        
        // Run AreaD8.
        
        public static async Task<bool> runAreaD8(
            string pFile,
            string ad8File,
            string outletFile,
            string weightFile,
            int numProcesses,
            TextBox output,
            bool contCheck = false,
            bool mustRun = true) {
            var inFiles = new List<Tuple<string, string>> {
                new Tuple < string, string >("-p", pFile)
            };
            if (outletFile is not null) {
                inFiles.Add(new Tuple<string, string>("-o", outletFile));
            }
            if (weightFile is not null) {
                inFiles.Add(new Tuple<string, string>("-wg", weightFile));
            }
            var check = contCheck ? new List<Tuple<string, string>> () : new List<Tuple<string, string>> {
                new Tuple<string, string>("-nc", "")
            };
            return await TauDEMUtils.run("AreaD8", inFiles, check, new List<Tuple<string, string>> {
                new Tuple<string, string>("-ad8", ad8File)
            }, numProcesses, output, mustRun);
        }
        
        // Run AreaDinf.
        
        public static async Task<bool> runAreaDinf(
            string angFile,
            string scaFile,
            string outletFile,
            int numProcesses,
            TextBox output,
            bool mustRun = true) {
            var inFiles = new List<Tuple<string, string>> {
                new Tuple<string, string>("-ang", angFile)
            };
            if (outletFile is not null) {
                inFiles.Add(new Tuple<string, string>("-o", outletFile));
            }
            return await TauDEMUtils.run("AreaDinf", inFiles, new List<Tuple<string, string>> {
                new Tuple<string, string>("-nc", "")
            }, new List<Tuple<string, string>> {
                new Tuple<string, string>("-sca", scaFile)
            }, numProcesses, output, mustRun);
        }
        
        // Run GridNet.
        
        public static async Task<bool> runGridNet(
            string pFile,
            string plenFile,
            string tlenFile,
            string gordFile,
            string outletFile,
            int numProcesses,
            TextBox output,
            bool mustRun = true) {
            var inFiles = new List<Tuple<string, string>> {
                new Tuple<string, string>("-p", pFile)
            };
            if (outletFile is not null) {
                inFiles.Add(new Tuple<string, string>("-o", outletFile));
            }
            return await TauDEMUtils.run("GridNet", inFiles, new List<Tuple<string, string>>(), new List<Tuple<string, string>> {
                new Tuple<string, string>("-plen", plenFile),
                new Tuple<string, string>("-tlen", tlenFile),
                new Tuple<string, string>("-gord", gordFile)
            }, numProcesses, output, mustRun);
        }
        
        // Run Threshold.
        
        public static async Task<bool> runThreshold(
            string ad8File,
            string srcFile,
            string threshold,
            int numProcesses,
            TextBox output,
            bool mustRun = true) {
            return await TauDEMUtils.run("Threshold", new List<Tuple<string, string>> {
                new Tuple<string, string>("-ssa", ad8File)
            }, new List<Tuple<string, string>> {
                new Tuple<string, string>("-thresh", threshold)
            }, new List<Tuple<string, string>> {
                new Tuple<string, string>("-src", srcFile)
            }, numProcesses, output, mustRun);
        }
        
        // Run StreamNet.
        
        public static async Task<bool> runStreamNet(
            string felFile,
            string pFile,
            string ad8File,
            string srcFile,
            string outletFile,
            string ordFile,
            string treeFile,
            string coordFile,
            string streamFile,
            string wFile,
            int numProcesses,
            TextBox output,
            bool mustRun = true) {
            var inFiles = new List<Tuple<string, string>> {
                new Tuple<string, string>("-fel", felFile),
                new Tuple<string, string>("-p", pFile),
                new Tuple<string, string>("-ad8", ad8File),
                new Tuple<string, string>("-src", srcFile)
            };
            if (outletFile is not null) {
                inFiles.Add(new Tuple<string, string>("-o", outletFile));
            }
            return await TauDEMUtils.run("StreamNet", inFiles, new List<Tuple<string, string>> (), new List<Tuple<string, string>> {
                new Tuple<string, string>("-ord", ordFile),
                new Tuple<string, string>("-tree", treeFile),
                new Tuple<string, string>("-coord", coordFile),
                new Tuple<string, string>("-net", streamFile),
                new Tuple<string, string>("-w", wFile)
            }, numProcesses, output, mustRun);
        }
        
        // Run MoveOutlets.
        
        public static async Task<bool> runMoveOutlets(
            string pFile,
            string srcFile,
            string outletFile,
            string movedOutletFile,
            int numProcesses,
            TextBox output,
            bool mustRun = true) {
            return await TauDEMUtils.run("MoveOutletsToStreams", new List<Tuple<string, string>> {
                new Tuple<string, string>("-p", pFile),
                new Tuple<string, string>("-src", srcFile),
                new Tuple<string, string>("-o", outletFile)
            }, new List<Tuple<string, string>> (), new List<Tuple<string, string>> {
                new Tuple<string, string>("-om", movedOutletFile)
            }, numProcesses, output, mustRun);
        }
        
        // Run D8HDistToStrm.
        
        public static async Task<bool> runDistanceToStreams(
            string pFile,
            string hd8File,
            string distFile,
            string threshold,
            int numProcesses,
            TextBox output,
            bool mustRun = true) {
            return await TauDEMUtils.run("D8HDistToStrm", new List<Tuple<string, string>> {
                new Tuple<string, string>("-p", pFile),
                new Tuple<string, string>("-src", hd8File)
            }, new List<Tuple<string, string>> {
                new Tuple<string, string>("-thresh", threshold)
            }, new List<Tuple<string, string>> {
                new Tuple<string, string>("-dist", distFile)
            }, numProcesses, output, mustRun);
        }
        
        // 
        //         Run TauDEM command, using mpiexec if numProcesses is not zero.
        //         
        //         Parameters:
        //         inFiles: list of pairs of parameter id (string) and file path (string) 
        //         for input files.  May not be empty.
        //         inParms: list of pairs of parameter id (string) and parameter value 
        //         (string) for input parameters.
        //         For a parameter which is a flag with no value, parameter value 
        //         should be empty string.
        //         outFiles: list of pairs of parameter id (string) and file path 
        //         (string) for output files.
        //         numProcesses: number of processes to use (int).  
        //         Zero means do not use mpiexec.
        //         output: buffer for TauDEM output (QTextEdit).
        //         if output is None use as flag that running in batch, and errors are simply printed.
        //         Return: True if no error detected, else false.
        //         The command is not executed if 
        //         (1) mustRun is false (since it is set true for results that depend 
        //         on the threshold setting or an outlets file, which might have changed), and
        //         (2) all output files exist and were last modified no earlier 
        //         than the first input file.
        //         An error is detected if any input file does not exist or,
        //         after running the TauDEM command, 
        //         any output file does not exist or was last modified earlier 
        //         than the first input file.
        //         For successful output files the .prj file is copied 
        //         from the first input file.
        //         The Taudem executable directory and the mpiexec path are 
        //         read from QSettings.
        //         
        
        public static async Task<bool> run(
            string command,
            List<Tuple<string, string>> inFiles,
            List<Tuple<string, string>> inParms,
            List<Tuple<string, string>> outFiles,
            int numProcesses,
            TextBox output,
            bool mustRun) {
            string streamDir = "";
            string tauDEMDir;
            string swatEditorDir;
            var hasGIS = !(output is null);
            var baseFile = inFiles[0].Item2;
            var needToRun = mustRun;
            if (!needToRun) {
                foreach (var (_, fileName) in outFiles) {
                    if (!Utils.isUpToDate(baseFile, fileName)) {
                        needToRun = true;
                        break;
                    }
                }
            }
            if (!needToRun) {
                return true;
            }
            var commands = new List<string>();
            if (hasGIS) {
                //var settings = QSettings();
                output.AppendText("\r\n------------------- TauDEM command: -------------------");
                //var mpiexecDir = settings.value("/QSWAT/mpiexecDir", "");
                var mpiexecDir = Parameters._MPIEXECDEFAULTDIR;
                var mpiexecPath = mpiexecDir != "" ? Utils.join(mpiexecDir, Parameters._MPIEXEC) : "";
                if (numProcesses != 0 && mpiexecDir != "" && Directory.Exists(mpiexecDir)) {
                    commands.Add(mpiexecPath);
                    commands.Add("-n");
                    commands.Add(numProcesses.ToString());
                }
                //swatEditorDir = settings.value("/QSWAT/SWATEditorDir", Parameters._SWATEDITORDEFAULTDIR);
                swatEditorDir = Parameters._SWATEDITORDEFAULTDIR;
            } else {
                // batch mode
                swatEditorDir = Parameters._SWATEDITORDEFAULTDIR;
            }
            var tauDEM539Dir = Utils.join(swatEditorDir, Parameters._TAUDEM539DIR);
            if (Directory.Exists(tauDEM539Dir)) {
                tauDEMDir = tauDEM539Dir;
                // pass StreamNet a directory rather than shapefile so shapefile created as a directory
                // this prevents problem that .shp cannot be deleted, but GDAL then complains that the .shp file is not a directory
                // also have to set -netlyr parameter to stop TauDEM failing to parse filename without .shp as a layer name
                // TauDEM version 5.1.2 does not support -netlyr parameter
                if (command == "StreamNet") {
                    // make copy so can rewrite
                    var outFilesCopy = new List<Tuple<string, string>>(outFiles);
                    outFiles = new List<Tuple<string, string>>();
                    foreach (var (pid, outFile) in outFilesCopy) {
                        if (pid == "-net") {
                            var streamBase = Path.ChangeExtension(outFile, null);
                            // streamBase may have form P/X/X, in which case streamDir is P/X, else streamDir is streamBase
                            var streamDir1 = Path.GetDirectoryName(streamBase);
                            var baseName = Path.GetFileName(streamBase);
                            var dirName = Path.GetFileName(streamDir1);
                            if (dirName == baseName) {
                                streamDir = streamDir1;
                            } else {
                                streamDir = streamBase;
                                Directory.CreateDirectory(streamDir);
                            }
                            outFiles.Add(new Tuple<string, string>(pid, streamDir));
                        } else {
                            outFiles.Add(new Tuple<string, string>(pid, outFile));
                        }
                    }
                    inParms.Add(new Tuple<string, string>("-netlyr", Path.GetFileName(streamDir)));
                }
            } else {
                tauDEMDir = Utils.join(swatEditorDir, Parameters._TAUDEMDIR);
                if (!Directory.Exists(tauDEMDir)) {
                    TauDEMUtils.error(String.Format("Cannot find TauDEM directory as {0} or {1}", tauDEM539Dir, tauDEMDir), hasGIS);
                    return false;
                }
            }
            commands.Add(Utils.join(tauDEMDir, command));
            foreach (var (pid, fileName) in inFiles) {
                if (!File.Exists(fileName)) {
                    TauDEMUtils.error(String.Format("File {0} does not exist", fileName), hasGIS);
                    return false;
                }
                commands.Add(pid);
                commands.Add(fileName);
            }
            foreach (var (pid, parm) in inParms) {
                commands.Add(pid);
                // allow for parameter which is flag with no value
                if (!(parm == "")) {
                    commands.Add(parm);
                }
            }
            // remove outFiles so any error will be reported
            foreach (var (_, fileName) in outFiles) {
                if (Directory.Exists(fileName)) {
                    await Utils.tryRemoveShapefileLayerAndDir(fileName);
                } else {
                    await Utils.removeLayerAndFiles(fileName);
                }
            }
            foreach (var (pid, fileName) in outFiles) {
                commands.Add(pid);
                commands.Add(fileName);
            }
            string fullCommand = String.Join(" ", commands);
            if (hasGIS) {
                output.AppendText("\r\n" + fullCommand);
            }
            var procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe") {
                ArgumentList = {
                    "/c",
                    commands[0]
                }
            };
            for (int i = 1; i < commands.Count; i++) {
                procStartInfo.ArgumentList.Add(commands[i]);
            }

            procStartInfo.RedirectStandardOutput = true;
            // TauDEM needs to be able to find proj.db
            var pathProBin = Path.GetDirectoryName(new System.Uri(Assembly.GetEntryAssembly().Location).AbsolutePath);
            var pathPro = Uri.UnescapeDataString(Directory.GetParent(pathProBin).FullName);
            procStartInfo.EnvironmentVariables["PROJ_LIB"] = Path.Combine(pathPro, @"Resources\pedata\gdaldata");
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            // Create process and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            string result;
            using (new CursorWait()) {
                proc.Start();
                // Create and format result string
                // much output is on standard error when there is nothing wrong,
                // so use existence of output files as check
                result = "";
                while (!proc.StandardOutput.EndOfStream) {
                    string line = proc.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line)) {
                        if (hasGIS) {
                            output.AppendText("\r\n" + line);
                        }
                        result += line + "\r\n";
                    }
                }
                while (!proc.StandardError.EndOfStream) {
                    string line = proc.StandardError.ReadLine();
                    if (!string.IsNullOrEmpty(line)) {
                        if (hasGIS) {
                            output.AppendText("\r\n" + line);
                        }
                        result += line + "\r\n";
                    }
                }
            }
            //var proc = subprocess.run(commands, shell: true, stdout: subprocess.PIPE, stderr: subprocess.STDOUT, universal_newlines: true);
            //if (hasGIS) {
            //    Debug.Assert(output is not null);
            //    output.append(proc.stdout);
            //    output.moveCursor(QTextCursor.End);
            //} else {
            //    Console.WriteLine(proc.stdout);
            //}
            //// proc.returncode always seems to be None
            //// so check TauDEM run by checking output file exists and modified later than DEM
            //// not ideal as may eg generate empty file
            //// TODO: improve on this
            //var ok = proc.returncode == 0;
            bool ok = true;
            var msg = command + " created ";
            foreach (var (pid, fileName) in outFiles) {
                if (pid == "-net") {
                    if (!Directory.Exists(fileName)) {
                        ok = false;
                        break;
                    } else {
                        msg += fileName;
                        msg += " ";
                    }
                } else {
                    if (Utils.isUpToDate(baseFile, fileName)) {
                        msg += fileName;
                        msg += " ";
                    } else {
                        ok = false;
                        break;
                    }
                }
            }
            if (ok) {
                TauDEMUtils.loginfo(msg, hasGIS);
            } else {
                if (hasGIS) {
                    //Debug.Assert(output is not null);
                    var origColour = output.ForeColor;
                    output.ForeColor = System.Drawing.Color.Red;
                    output.AppendText(Utils.trans(String.Format("*** Problem with TauDEM {0}: please examine output above. ***", command)));
                    output.ForeColor = origColour;
                }
                msg += "and failed";
                TauDEMUtils.logerror(msg, hasGIS);
            }
            return ok;
        }
        
        // Display TauDEM help file.
        
        public static void taudemHelp() {
            //var settings = QSettings();
            //var taudemHelpFile = Utils.join(Utils.join(settings.value("/QSWAT/SWATEditorDir"), Parameters._TAUDEMDIR), Parameters._TAUDEMHELP);
            var taudemHelpFile = Utils.join(Parameters._SWATEDITORDEFAULTDIR, Utils.join(Parameters._TAUDEMDIR, Parameters._TAUDEMHELP));
            Process p = new()
            {
                StartInfo = new ProcessStartInfo(taudemHelpFile)
                { UseShellExecute = true }
            };
            p.Start();
            //System.Diagnostics.Process.Start(taudemHelpFile);
        }
        
        // Report error, just printing if no QGIS running.
        
        public static void error(string msg, bool hasGIS) {
            if (hasGIS) {
                Utils.error(msg, false);
            } else {
                Console.WriteLine(msg);
            }
        }
        
        // Log msg, just printing if no QGIS running.
        
        public static void loginfo(string msg, bool hasGIS) {
            if (hasGIS) {
                Utils.loginfo(msg);
            } else {
                Console.WriteLine(msg);
            }
        }
        
        // Log error msg, just printing if no QGIS running.
        
        public static void logerror(string msg, bool hasGIS) {
            if (hasGIS) {
                Utils.logerror(msg);
            } else {
                Console.WriteLine(msg);
            }
        }
    }
}
