namespace ArcSWAT3
{
    partial class SelectSubbasin
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectSubbasin));
            this.label1 = new System.Windows.Forms.Label();
            this.checkBox = new System.Windows.Forms.CheckBox();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.pushButton = new System.Windows.Forms.Button();
            this.threshold = new System.Windows.Forms.TextBox();
            this.percentButton = new System.Windows.Forms.RadioButton();
            this.areaButton = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(364, 240);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // checkBox
            // 
            this.checkBox.AutoSize = true;
            this.checkBox.Location = new System.Drawing.Point(24, 275);
            this.checkBox.Name = "checkBox";
            this.checkBox.Size = new System.Drawing.Size(143, 19);
            this.checkBox.TabIndex = 1;
            this.checkBox.Text = "Select small subbasins";
            this.checkBox.UseVisualStyleBackColor = true;
            this.checkBox.CheckedChanged += new System.EventHandler(this.checkBox_CheckedChanged);
            // 
            // groupBox
            // 
            this.groupBox.Controls.Add(this.pushButton);
            this.groupBox.Controls.Add(this.threshold);
            this.groupBox.Controls.Add(this.percentButton);
            this.groupBox.Controls.Add(this.areaButton);
            this.groupBox.Controls.Add(this.label2);
            this.groupBox.Location = new System.Drawing.Point(19, 308);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(368, 152);
            this.groupBox.TabIndex = 2;
            this.groupBox.TabStop = false;
            this.groupBox.Text = "Select by threshold";
            // 
            // pushButton
            // 
            this.pushButton.Location = new System.Drawing.Point(263, 97);
            this.pushButton.Name = "pushButton";
            this.pushButton.Size = new System.Drawing.Size(75, 23);
            this.pushButton.TabIndex = 4;
            this.pushButton.Text = "Select";
            this.pushButton.UseVisualStyleBackColor = true;
            this.pushButton.Click += new System.EventHandler(this.pushButton_Click);
            // 
            // threshold
            // 
            this.threshold.Location = new System.Drawing.Point(180, 94);
            this.threshold.Name = "threshold";
            this.threshold.Size = new System.Drawing.Size(66, 23);
            this.threshold.TabIndex = 3;
            // 
            // percentButton
            // 
            this.percentButton.AutoSize = true;
            this.percentButton.Location = new System.Drawing.Point(12, 111);
            this.percentButton.Name = "percentButton";
            this.percentButton.Size = new System.Drawing.Size(156, 19);
            this.percentButton.TabIndex = 2;
            this.percentButton.TabStop = true;
            this.percentButton.Text = "Percentage of mean area";
            this.percentButton.UseVisualStyleBackColor = true;
            // 
            // areaButton
            // 
            this.areaButton.AutoSize = true;
            this.areaButton.Location = new System.Drawing.Point(13, 82);
            this.areaButton.Name = "areaButton";
            this.areaButton.Size = new System.Drawing.Size(73, 19);
            this.areaButton.TabIndex = 1;
            this.areaButton.TabStop = true;
            this.areaButton.Text = "Area (ha)";
            this.areaButton.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(343, 45);
            this.label2.TabIndex = 0;
            this.label2.Text = "Set a threshold for small subbasins, either as an area in hectares \r\nor as a perc" +
    "entage of the mean subbasin area.   Click the Select \r\nbutton to select subbasin" +
    "s below the threshold.";
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(312, 466);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(221, 466);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 4;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // SelectSubbasin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 511);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.groupBox);
            this.Controls.Add(this.checkBox);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SelectSubbasin";
            this.Text = "Select subbasins for merging";
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBox;
        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.Button pushButton;
        private System.Windows.Forms.TextBox threshold;
        private System.Windows.Forms.RadioButton percentButton;
        private System.Windows.Forms.RadioButton areaButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button saveButton;
    }
}