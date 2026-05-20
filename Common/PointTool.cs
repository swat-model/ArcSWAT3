// Based on BufferedLineTool.cs which came with the following copyright and license:

// Copyright 2019 Esri 
//
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); 
//   you may not use this file except in compliance with the License. 
//   You may obtain a copy of the License at 
//
//       https://www.apache.org/licenses/LICENSE-2.0 
//
//   Unless required by applicable law or agreed to in writing, software 
//   distributed under the License is distributed on an "AS IS" BASIS, 
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//   See the License for the specific language governing permissions and 
//   limitations under the License. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Windows.Forms;

namespace ArcSWAT3
{
    internal class PointTool : MapTool { 

        //private SelectPointViewModel _vm;
        public PointTool() {
            IsSketchTool = true;
            // Select the type of construction tool you wish to implement.  
            // Make sure that the tool is correctly registered with the correct component category type in the daml 
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Screen;
            FireSketchEvents = true;
            ControlID = "ArcSWAT3_PointTool";
            //var systemCursor = System.Windows.Input.Cursors.Arrow;
            //Cursor = systemCursor;
        }

        //protected override Task<bool> OnSketchCompleteAsync(Geometry geometry) {
        //    return QueuedTask.Run(() => {
        //        MapPoint pt = geometry as MapPoint;
        //        System.Windows.Forms.MessageBox.Show(string.Format("({0}, {1})", pt.X, pt.Y));
        //        return false;
        //    });

        //}

        //protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e) {
        //    e.Handled = true;
        //}

        //protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e) {
        //    //Get the instance of the ViewModel
        //    if (_vm == null)
        //        return Task.FromResult(0);

        //    // cast vm to your viewModel in order to access your properties

        //    //Get the map coordinates from the click point and set the property on the ViewMode.
        //    return QueuedTask.Run(() =>
        //    {
        //        var mapPoint = MapView.Active.ClientToMap(e.ClientPoint);
        //        _vm.Text = string.Format("X: {0}, Y: {1}, Z: {2}", mapPoint.X, mapPoint.Y, mapPoint.Z);
        //    });
        //}
    }

    //#region Tool Options
    //private ReadOnlyToolOptions ToolOptions => CurrentTemplate?.GetToolOptions(ID);

    //    #endregion

    //    /// <summary>
    //    /// Called when the sketch finishes. This is where we will create the sketch operation and then execute it.
    //    /// </summary>
    //    /// <param name="geometry">The geometry created by the sketch.</param>
    //    /// <returns>A Task returning a Boolean indicating if the sketch complete event was successfully handled.</returns>
    //    protected override Task<bool> OnSketchCompleteAsync(Geometry geometry) {
    //        if (CurrentTemplate == null || geometry == null)
    //            return Task.FromResult(false);

    //        // Create an edit operation
    //        var createOperation = new EditOperation() {
    //            Name = string.Format("Create {0}", CurrentTemplate.Layer.Name),
    //            SelectNewFeatures = true
    //        };

    //        // Queue feature creation, which is the point created by the sketch
    //        createOperation.Create(CurrentTemplate, geometry);

    //        // Execute the operation
    //        return createOperation.ExecuteAsync();
    //    }
    //}
}