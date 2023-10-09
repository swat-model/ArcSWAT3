namespace ArcSWAT3
{
    partial class ChoosePointsFile
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.currentbutton = new System.Windows.Forms.Button();
            this.newButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.inletsOutletsFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(258, 75);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select \"Current\" if you wish to draw new points \r\nin the existing inlets/outlets " +
    "layer.\r\nSelect \"New\" if you wish to make a new \r\ninlets/outlets file.\r\nSelect \"C" +
    "ancel\" to abandon drawing.";
            // 
            // currentbutton
            // 
            this.currentbutton.Location = new System.Drawing.Point(22, 110);
            this.currentbutton.Name = "currentbutton";
            this.currentbutton.Size = new System.Drawing.Size(75, 23);
            this.currentbutton.TabIndex = 1;
            this.currentbutton.Text = "Current";
            this.currentbutton.UseVisualStyleBackColor = true;
            this.currentbutton.Click += new System.EventHandler(this.currentbutton_Click);
            // 
            // newButton
            // 
            this.newButton.Location = new System.Drawing.Point(103, 110);
            this.newButton.Name = "newButton";
            this.newButton.Size = new System.Drawing.Size(75, 23);
            this.newButton.TabIndex = 2;
            this.newButton.Text = "New";
            this.newButton.UseVisualStyleBackColor = true;
            this.newButton.Click += new System.EventHandler(this.newButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(184, 110);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // inletsOutletsFileDialog
            // 
            this.inletsOutletsFileDialog.Filter = "\"Shapefiles|*.shp|All files|*.*\"";
            this.inletsOutletsFileDialog.Title = "Select an inlets/outlets shapefile";
            // 
            // ChoosePointsFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(277, 147);
            this.ControlBox = false;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.newButton);
            this.Controls.Add(this.currentbutton);
            this.Controls.Add(this.label1);
            this.Name = "ChoosePointsFile";
            this.Text = "Select inlets/outlets file to draw on";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button currentbutton;
        private System.Windows.Forms.Button newButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.SaveFileDialog inletsOutletsFileDialog;
    }
}