namespace ArcSWAT3
{
    partial class SplitForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplitForm));
            this.newCombo = new System.Windows.Forms.ComboBox();
            this.splitCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.addButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.deleteSplitButton = new System.Windows.Forms.Button();
            this.cancelEditButton = new System.Windows.Forms.Button();
            this.saveEditButton = new System.Windows.Forms.Button();
            this.saveSplitsButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.table = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.table)).BeginInit();
            this.SuspendLayout();
            // 
            // newCombo
            // 
            this.newCombo.FormattingEnabled = true;
            this.newCombo.Location = new System.Drawing.Point(25, 55);
            this.newCombo.Name = "newCombo";
            this.newCombo.Size = new System.Drawing.Size(87, 23);
            this.newCombo.TabIndex = 0;
            this.newCombo.SelectionChangeCommitted += new System.EventHandler(this.newCombo_SelectionChangeCommitted);
            // 
            // splitCombo
            // 
            this.splitCombo.FormattingEnabled = true;
            this.splitCombo.Location = new System.Drawing.Point(207, 55);
            this.splitCombo.Name = "splitCombo";
            this.splitCombo.Size = new System.Drawing.Size(88, 23);
            this.splitCombo.TabIndex = 1;
            this.splitCombo.SelectionChangeCommitted += new System.EventHandler(this.splitCombo_SelectionChangeCommitted);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 30);
            this.label1.TabIndex = 2;
            this.label1.Text = "Select landuse\r\nto split\r\n";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(188, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 30);
            this.label2.TabIndex = 3;
            this.label2.Text = "Select split landuse\r\nto edit\r\n";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(30, 84);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(89, 45);
            this.addButton.TabIndex = 5;
            this.addButton.Text = "Add\r\nsub-landuse\r\n";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // deleteButton
            // 
            this.deleteButton.Location = new System.Drawing.Point(30, 135);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(89, 45);
            this.deleteButton.TabIndex = 8;
            this.deleteButton.Text = "Delete\r\nsub-landuse\r\n";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // deleteSplitButton
            // 
            this.deleteSplitButton.Location = new System.Drawing.Point(30, 186);
            this.deleteSplitButton.Name = "deleteSplitButton";
            this.deleteSplitButton.Size = new System.Drawing.Size(89, 45);
            this.deleteSplitButton.TabIndex = 9;
            this.deleteSplitButton.Text = "Delete\r\nsplit landuse\r\n";
            this.deleteSplitButton.UseVisualStyleBackColor = true;
            this.deleteSplitButton.Click += new System.EventHandler(this.deleteSplitButton_Click);
            // 
            // cancelEditButton
            // 
            this.cancelEditButton.Location = new System.Drawing.Point(163, 247);
            this.cancelEditButton.Name = "cancelEditButton";
            this.cancelEditButton.Size = new System.Drawing.Size(75, 44);
            this.cancelEditButton.TabIndex = 10;
            this.cancelEditButton.Text = "Cancel\r\nedits";
            this.cancelEditButton.UseVisualStyleBackColor = true;
            this.cancelEditButton.Click += new System.EventHandler(this.cancelEditButton_Click);
            // 
            // saveEditButton
            // 
            this.saveEditButton.Location = new System.Drawing.Point(246, 247);
            this.saveEditButton.Name = "saveEditButton";
            this.saveEditButton.Size = new System.Drawing.Size(75, 44);
            this.saveEditButton.TabIndex = 11;
            this.saveEditButton.Text = "Save\r\nedits";
            this.saveEditButton.UseVisualStyleBackColor = true;
            this.saveEditButton.Click += new System.EventHandler(this.saveEditButton_Click);
            // 
            // saveSplitsButton
            // 
            this.saveSplitsButton.Location = new System.Drawing.Point(183, 297);
            this.saveSplitsButton.Name = "saveSplitsButton";
            this.saveSplitsButton.Size = new System.Drawing.Size(75, 44);
            this.saveSplitsButton.TabIndex = 12;
            this.saveSplitsButton.Text = "Save\r\nsplits";
            this.saveSplitsButton.UseVisualStyleBackColor = true;
            this.saveSplitsButton.Click += new System.EventHandler(this.saveSplitsButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(269, 297);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 44);
            this.cancelButton.TabIndex = 13;
            this.cancelButton.Text = "Cancel\r\n";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // table
            // 
            this.table.BackgroundColor = System.Drawing.SystemColors.Control;
            this.table.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.table.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.table.Location = new System.Drawing.Point(137, 84);
            this.table.Name = "table";
            this.table.RowHeadersWidth = 25;
            this.table.RowTemplate.Height = 25;
            this.table.Size = new System.Drawing.Size(215, 147);
            this.table.TabIndex = 14;
            // 
            // SplitForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 354);
            this.Controls.Add(this.table);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveSplitsButton);
            this.Controls.Add(this.saveEditButton);
            this.Controls.Add(this.cancelEditButton);
            this.Controls.Add(this.deleteSplitButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.splitCombo);
            this.Controls.Add(this.newCombo);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SplitForm";
            this.Text = "Split landuses";
            ((System.ComponentModel.ISupportInitialize)(this.table)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox newCombo;
        private System.Windows.Forms.ComboBox splitCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button deleteSplitButton;
        private System.Windows.Forms.Button cancelEditButton;
        private System.Windows.Forms.Button saveEditButton;
        private System.Windows.Forms.Button saveSplitsButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.DataGridView table;
    }
}