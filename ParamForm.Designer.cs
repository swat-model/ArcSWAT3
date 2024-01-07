
namespace ArcSWAT3
{
    partial class ParamForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParamForm));
            this.editorLabel = new System.Windows.Forms.Label();
            this.editorBox = new System.Windows.Forms.TextBox();
            this.editorButton = new System.Windows.Forms.Button();
            this.checkUseMPI = new System.Windows.Forms.CheckBox();
            this.MPILabel = new System.Windows.Forms.Label();
            this.MPIBox = new System.Windows.Forms.TextBox();
            this.MPIButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // editorLabel
            // 
            this.editorLabel.AutoSize = true;
            this.editorLabel.Location = new System.Drawing.Point(13, 13);
            this.editorLabel.Name = "editorLabel";
            this.editorLabel.Size = new System.Drawing.Size(122, 15);
            this.editorLabel.TabIndex = 0;
            this.editorLabel.Text = "SWAT Editor Directory";
            this.toolTip1.SetToolTip(this.editorLabel, "This is the directory where the SWAT Editor is located.  On Windows it is usually" +
        " C:\\SWAT\\SWATEditor.\r\n");
            // 
            // editorBox
            // 
            this.editorBox.Location = new System.Drawing.Point(13, 32);
            this.editorBox.Name = "editorBox";
            this.editorBox.Size = new System.Drawing.Size(542, 23);
            this.editorBox.TabIndex = 1;
            // 
            // editorButton
            // 
            this.editorButton.Location = new System.Drawing.Point(561, 32);
            this.editorButton.Name = "editorButton";
            this.editorButton.Size = new System.Drawing.Size(47, 23);
            this.editorButton.TabIndex = 2;
            this.editorButton.Text = "...";
            this.toolTip1.SetToolTip(this.editorButton, "Browse to select SWAT editor directory");
            this.editorButton.UseVisualStyleBackColor = true;
            this.editorButton.Click += new System.EventHandler(this.editorButton_Click);
            // 
            // checkUseMPI
            // 
            this.checkUseMPI.AutoSize = true;
            this.checkUseMPI.Location = new System.Drawing.Point(13, 62);
            this.checkUseMPI.Name = "checkUseMPI";
            this.checkUseMPI.Size = new System.Drawing.Size(69, 19);
            this.checkUseMPI.TabIndex = 3;
            this.checkUseMPI.Text = "Use MPI";
            this.toolTip1.SetToolTip(this.checkUseMPI, resources.GetString("checkUseMPI.ToolTip"));
            this.checkUseMPI.UseVisualStyleBackColor = true;
            this.checkUseMPI.CheckedChanged += new System.EventHandler(this.checkUseMPI_CheckedChanged);
            // 
            // MPILabel
            // 
            this.MPILabel.AutoSize = true;
            this.MPILabel.Location = new System.Drawing.Point(13, 88);
            this.MPILabel.Name = "MPILabel";
            this.MPILabel.Size = new System.Drawing.Size(98, 15);
            this.MPILabel.TabIndex = 4;
            this.MPILabel.Text = "MPI bin direcrory";
            this.toolTip1.SetToolTip(this.MPILabel, resources.GetString("MPILabel.ToolTip"));
            // 
            // MPIBox
            // 
            this.MPIBox.Location = new System.Drawing.Point(12, 106);
            this.MPIBox.Name = "MPIBox";
            this.MPIBox.Size = new System.Drawing.Size(542, 23);
            this.MPIBox.TabIndex = 5;
            // 
            // MPIButton
            // 
            this.MPIButton.Location = new System.Drawing.Point(560, 105);
            this.MPIButton.Name = "MPIButton";
            this.MPIButton.Size = new System.Drawing.Size(47, 23);
            this.MPIButton.TabIndex = 6;
            this.MPIButton.Text = "...";
            this.toolTip1.SetToolTip(this.MPIButton, "Browse to select MPI bin directory\r\n");
            this.MPIButton.UseVisualStyleBackColor = true;
            this.MPIButton.Click += new System.EventHandler(this.MPIButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(437, 148);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 7;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(532, 148);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // ParamForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(619, 195);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.MPIButton);
            this.Controls.Add(this.MPIBox);
            this.Controls.Add(this.MPILabel);
            this.Controls.Add(this.checkUseMPI);
            this.Controls.Add(this.editorButton);
            this.Controls.Add(this.editorBox);
            this.Controls.Add(this.editorLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ParamForm";
            this.Text = "ParamForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label editorLabel;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox editorBox;
        private System.Windows.Forms.Button editorButton;
        private System.Windows.Forms.CheckBox checkUseMPI;
        private System.Windows.Forms.Label MPILabel;
        private System.Windows.Forms.TextBox MPIBox;
        private System.Windows.Forms.Button MPIButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}