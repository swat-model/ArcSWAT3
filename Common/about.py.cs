

namespace ArcSWAT3 {
    
 // provide basic information about QSWAT, including version, and link to SWAT website.
    public class AboutArcSWAT {
        
        public object _dlg;
        
        public object _gv;
        
        public AboutArcSWAT(object gv) {
            this._gv = gv;
            this._dlg = aboutDialog();
            this._dlg.setWindowFlags(this._dlg.windowFlags() & ~Qt.WindowContextHelpButtonHint);
            if (this._gv) {
                this._dlg.move(this._gv.aboutPos);
            }
        }
        
        // Run the form.
        public virtual void run(object version) {
            this._dlg.SWATHomeButton.clicked.connect(this.openSWATUrl);
            this._dlg.closeButton.clicked.connect(this._dlg.close);
            var text = @"
ArcSWAT3 version: {0}

Python version: 3.9

Current restrictions:
- runs only in Windows
        ".format(version);
            this._dlg.textBrowser.setText(text);
            this._dlg.exec_();
            if (this._gv) {
                this._gv.aboutPos = this._dlg.pos();
            }
        }
        
        // Open SWAT website.
        public virtual void openSWATUrl() {
            webbrowser.open("http://swat.tamu.edu/");
        }
    }
    
    // -*- coding: utf-8 -*-
    // Import the PyQt and QGIS libraries
    // Import the code for the dialog
    static about() {
    }
}
