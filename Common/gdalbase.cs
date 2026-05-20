using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;

#nullable enable

namespace ArcSWAT3
{
    /// <summary>
    /// Configures all variables and options for GDAL including plugins and proj.db path
    /// </summary>
    public static class GdalBase
    {
        /// <summary>
        /// Shows if gdal is already initialized.
        /// </summary>
        public static bool IsConfigured { get; private set; }

        /// <summary>
        /// Enable or disable assembly validation. Set it before calling <see cref="ConfigureGdalDrivers"/>, which checks for required native libraries are available.
        /// Can be useful for some cases when you want to load third-party plugins.
        /// Default is true.
        /// </summary>
        public static bool EnableRuntimeValidation { get; set; } = true;

        /// <summary>
        /// Performs search for gdalplugins and calls
        /// <see cref="OSGeo.GDAL.Gdal.AllRegister"/> and <see cref="OSGeo.OGR.Ogr.RegisterAll"/>
        /// <param name="gdalDataFolder">path to set as GDAL_DATA option</param>
        /// </summary>
        public static void ConfigureGdalDrivers(string? gdalDataFolder = null) {
            if (IsConfigured)
                return;

            if (EnableRuntimeValidation)
                AssemblyValidator.AssertRuntimeAvailable();

            string? path = Environment.GetEnvironmentVariable("PATH");
            var runtimes = $"runtimes/{GdalBaseExtensions.GetEnvRID()}/native"; 
            var runLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var extension = runLoc + ";" + runtimes;

            Environment.SetEnvironmentVariable("PATH", runLoc + ";" + extension + ";" + path);
            OSGeo.GDAL.Gdal.AllRegister();
            OSGeo.OGR.Ogr.RegisterAll();

            ConfigureGdalData(gdalDataFolder);
            // set flag only on success
            IsConfigured = true;
        }

        /// <summary>
        /// Set path for GDAL_DATA option.
        /// </summary>
        /// <param name="gdalDataFolder"></param>
        public static void ConfigureGdalData(string? gdalDataFolder = null) {
            if (gdalDataFolder is null) {
                var isArm = RuntimeInformation.ProcessArchitecture is Architecture.Arm64;
                var rid = isArm ? "any-arm64" : "any-x64";
                var runtimes = $"runtimes/{rid}/native";
                var helperLocations = GdalBaseExtensions.GetPackageDataPossibleLocations(runtimes, "gdal-data");
                gdalDataFolder = helperLocations.FirstOrDefault(Directory.Exists);
            }
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_DATA", gdalDataFolder);
        }

        /// <summary>
        /// Calls <see cref="ConfigureGdalDrivers"/> and <see cref="Proj.Configure"/>
        /// </summary>
        public static void ConfigureAll(string path) {
            if (IsConfigured) return;

            ConfigureGdalDrivers(path);

            ProjForGdal.Configure();
        }
    }
}