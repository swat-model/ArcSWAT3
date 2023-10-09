

    // Dialog to select subbasins.
    public class SelectSubbasins {
        
        public object _dlg;
        
        public object _gv;
        
        public object areaIndx;
        
        public object meanArea;
        
        public object wshedLayer;
        
        public SelectSubbasins(object gv, object wshedLayer) {
            this._gv = gv;
            this._dlg = SelectSubbasinsDialog();
            this._dlg.setWindowFlags(this._dlg.windowFlags() & ~Qt.WindowContextHelpButtonHint);
            this._dlg.move(this._gv.selectSubsPos);
            //# Watershed layer
            this.wshedLayer = wshedLayer;
            //# Index of AREA field in watershed layer
            this.areaIndx = this._gv.topo.getIndex(wshedLayer, QSWATTopology._AREA);
            //# mean area of subbasins in watershed
            this.meanArea = this.layerMeanArea();
        }
        
        // Set up the dialog.
        public virtual void init() {
            this._dlg.pushButton.clicked.connect(this.selectByThreshold);
            this._dlg.checkBox.stateChanged.connect(this.switchSelectSmall);
            this.wshedLayer.selectionChanged.connect(this.setCount);
            this._dlg.saveButton.clicked.connect(this.save);
            this._dlg.cancelButton.clicked.connect(this.cancel);
            this._dlg.checkBox.setChecked(false);
            this._dlg.groupBox.setVisible(false);
            this._dlg.areaButton.setChecked(false);
            this._dlg.percentButton.setChecked(true);
            this._dlg.threshold.setText("5");
        }
        
        // Run the dialog.
        public virtual void run() {
            this.init();
            this._dlg.show();
            this._dlg.exec_();
            this._gv.selectSubsPos = this._dlg.pos();
        }
        
        // Set visibility of threshold controls by check box state.
        public virtual void switchSelectSmall() {
            this._dlg.groupBox.setVisible(this._dlg.checkBox.isChecked());
        }
        
        // Select subbasins below the threshold, interpreted as area or percentage.
        public virtual object selectByThreshold() {
            object thresholdM2;
            var num = this._dlg.threshold.text();
            if (num == "") {
                QSWATUtils.error("No threshold is set", this._gv.isBatch);
                return;
            }
            try {
                var threshold = float(num);
            } catch (Exception) {
                QSWATUtils.error("Cannot parse {0} as a number".format(num), this._gv.isBatch);
                return;
            }
            if (this._dlg.areaButton.isChecked()) {
                thresholdM2 = threshold * 10000;
            } else {
                thresholdM2 = this.meanArea * threshold / 100;
            }
            var toAdd = new HashSet<object>();
            foreach (var f in this.wshedLayer.getFeatures()) {
                var area = f.attributes()[this.areaIndx];
                if (area < thresholdM2) {
                    toAdd.add(f.id());
                }
            }
            var ids = this.wshedLayer.selectedFeatureIds();
            foreach (var i in toAdd) {
                ids.append(i);
            }
            this.wshedLayer.select(ids);
        }
        
        // Return mean area of watershed layer subbasins in square metres.
        public virtual double layerMeanArea() {
            var count = this.wshedLayer.featureCount();
            // avoid division by zero
            if (count == 0) {
                return 0;
            }
            var total = 0;
            foreach (var f in this.wshedLayer.getFeatures()) {
                total += f.attributes()[this.areaIndx];
            }
            return float(total) / count;
        }
        
        // Set count text.
        public virtual void setCount() {
            this._dlg.count.setText("{0!s} selected".format(this.wshedLayer.selectedFeatureCount()));
        }
        
        // Close the dialog.
        public virtual void save() {
            this._dlg.close();
        }
        
        // Cancel selection and close.
        public virtual void cancel() {
            this.wshedLayer.removeSelection();
            this._dlg.close();
        }
    }
    
    // -*- coding: utf-8 -*-
    // Import the PyQt and QGIS libraries
    // Import the code for the dialog
    static selectsubs() {
    }
}
