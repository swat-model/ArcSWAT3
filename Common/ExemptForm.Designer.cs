namespace ArcSWAT3
{
    partial class ExemptForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExemptForm));
            this.chooseBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.exemptBox = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cancelExemptionButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chooseBox
            // 
            this.chooseBox.FormattingEnabled = true;
            this.chooseBox.Location = new System.Drawing.Point(12, 58);
            this.chooseBox.Name = "chooseBox";
            this.chooseBox.Size = new System.Drawing.Size(84, 23);
            this.chooseBox.TabIndex = 0;
            this.chooseBox.SelectionChangeCommitted += new System.EventHandler(this.chooseBox_SelectionChangeCommitted);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 30);
            this.label1.TabIndex = 1;
            this.label1.Text = "Select landuse\r\nto be exempt";
            // 
            // exemptBox
            // 
            this.exemptBox.FormattingEnabled = true;
            this.exemptBox.ItemHeight = 15;
            this.exemptBox.Location = new System.Drawing.Point(135, 58);
            this.exemptBox.Name = "exemptBox";
            this.exemptBox.Size = new System.Drawing.Size(87, 79);
            this.exemptBox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(135, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Exempt landuses";
            // 
            // cancelExemptionButton
            // 
            this.cancelExemptionButton.Location = new System.Drawing.Point(28, 93);
            this.cancelExemptionButton.Name = "cancelExemptionButton";
            this.cancelExemptionButton.Size = new System.Drawing.Size(75, 44);
            this.cancelExemptionButton.TabIndex = 4;
            this.cancelExemptionButton.Text = "Cancel \r\nexemption";
            this.cancelExemptionButton.UseVisualStyleBackColor = true;
            this.cancelExemptionButton.Click += new System.EventHandler(this.cancelExemptionButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(62, 152);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 5;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(147, 152);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // ExemptForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(239, 188);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelExemptionButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.exemptBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chooseBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ExemptForm";
            this.Text = "Exempt landuses";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox chooseBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox exemptBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button cancelExemptionButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}