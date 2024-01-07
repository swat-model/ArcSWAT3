namespace ArcSWAT3
{
    partial class OutletForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OutletForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ptsourceButton = new System.Windows.Forms.RadioButton();
            this.inletButton = new System.Windows.Forms.RadioButton();
            this.pondButton = new System.Windows.Forms.RadioButton();
            this.reservoirButton = new System.Windows.Forms.RadioButton();
            this.outletButton = new System.Windows.Forms.RadioButton();
            this.label = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ptsourceButton);
            this.groupBox1.Controls.Add(this.inletButton);
            this.groupBox1.Controls.Add(this.pondButton);
            this.groupBox1.Controls.Add(this.reservoirButton);
            this.groupBox1.Controls.Add(this.outletButton);
            this.groupBox1.Location = new System.Drawing.Point(26, 72);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(159, 150);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // ptsourceButton
            // 
            this.ptsourceButton.AutoSize = true;
            this.ptsourceButton.Location = new System.Drawing.Point(8, 122);
            this.ptsourceButton.Name = "ptsourceButton";
            this.ptsourceButton.Size = new System.Drawing.Size(91, 19);
            this.ptsourceButton.TabIndex = 4;
            this.ptsourceButton.TabStop = true;
            this.ptsourceButton.Text = "Point source";
            this.ptsourceButton.UseVisualStyleBackColor = true;
            // 
            // inletButton
            // 
            this.inletButton.AutoSize = true;
            this.inletButton.Location = new System.Drawing.Point(8, 97);
            this.inletButton.Name = "inletButton";
            this.inletButton.Size = new System.Drawing.Size(48, 19);
            this.inletButton.TabIndex = 3;
            this.inletButton.TabStop = true;
            this.inletButton.Text = "Inlet";
            this.inletButton.UseVisualStyleBackColor = true;
            // 
            // pondButton
            // 
            this.pondButton.AutoSize = true;
            this.pondButton.Location = new System.Drawing.Point(8, 72);
            this.pondButton.Name = "pondButton";
            this.pondButton.Size = new System.Drawing.Size(53, 19);
            this.pondButton.TabIndex = 2;
            this.pondButton.TabStop = true;
            this.pondButton.Text = "Pond";
            this.pondButton.UseVisualStyleBackColor = true;
            // 
            // reservoirButton
            // 
            this.reservoirButton.AutoSize = true;
            this.reservoirButton.Location = new System.Drawing.Point(8, 47);
            this.reservoirButton.Name = "reservoirButton";
            this.reservoirButton.Size = new System.Drawing.Size(73, 19);
            this.reservoirButton.TabIndex = 1;
            this.reservoirButton.TabStop = true;
            this.reservoirButton.Text = "Reservoir";
            this.reservoirButton.UseVisualStyleBackColor = true;
            // 
            // outletButton
            // 
            this.outletButton.AutoSize = true;
            this.outletButton.Location = new System.Drawing.Point(8, 22);
            this.outletButton.Name = "outletButton";
            this.outletButton.Size = new System.Drawing.Size(58, 19);
            this.outletButton.TabIndex = 0;
            this.outletButton.TabStop = true;
            this.outletButton.Text = "Outlet";
            this.outletButton.UseVisualStyleBackColor = true;
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(12, 9);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(184, 75);
            this.label.TabIndex = 1;
            this.label.Text = "Select type of point to add, then \r\nclick on map to place it.   \r\nRepeat as neede" +
    "d.  \r\nClick OK to confirm and exit,  \r\nCancel to remove points and exit.";
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(40, 236);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(121, 236);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // OutletForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(207, 271);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "OutletForm";
            this.Text = "Select point";
            this.Load += new System.EventHandler(this.OutletForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton ptsourceButton;
        private System.Windows.Forms.RadioButton inletButton;
        private System.Windows.Forms.RadioButton pondButton;
        private System.Windows.Forms.RadioButton reservoirButton;
        private System.Windows.Forms.RadioButton outletButton;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}