namespace ArcSWAT3
{
    partial class AboutForm
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
            this.textBrowser = new System.Windows.Forms.TextBox();
            this.SWATHomeButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBrowser
            // 
            this.textBrowser.Location = new System.Drawing.Point(0, 2);
            this.textBrowser.Multiline = true;
            this.textBrowser.Name = "textBrowser";
            this.textBrowser.ReadOnly = true;
            this.textBrowser.Size = new System.Drawing.Size(229, 187);
            this.textBrowser.TabIndex = 0;
            // 
            // SWATHomeButton
            // 
            this.SWATHomeButton.Location = new System.Drawing.Point(0, 196);
            this.SWATHomeButton.Name = "SWATHomeButton";
            this.SWATHomeButton.Size = new System.Drawing.Size(131, 23);
            this.SWATHomeButton.TabIndex = 1;
            this.SWATHomeButton.Text = "SWAT home page";
            this.SWATHomeButton.UseVisualStyleBackColor = true;
            this.SWATHomeButton.Click += new System.EventHandler(this.SWATHomeButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(137, 195);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(92, 23);
            this.closeButton.TabIndex = 2;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(230, 221);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.SWATHomeButton);
            this.Controls.Add(this.textBrowser);
            this.MaximizeBox = false;
            this.Name = "AboutForm";
            this.Text = "About ArcSWAT";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBrowser;
        private System.Windows.Forms.Button SWATHomeButton;
        private System.Windows.Forms.Button closeButton;
    }
}