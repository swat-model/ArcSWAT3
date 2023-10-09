/*
 * Created by SharpDevelop.
 * User: Chris
 * Date: 11/25/2022
 * Time: 5:51 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace ArcSWAT3
{
	partial class MainForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.newButton = new System.Windows.Forms.Button();
            this.existingButton = new System.Windows.Forms.Button();
            this.aboutButton = new System.Windows.Forms.Button();
            this.mainBox = new System.Windows.Forms.GroupBox();
            this.visualiseLabel = new System.Windows.Forms.Label();
            this.editLabel = new System.Windows.Forms.Label();
            this.hrusLabel = new System.Windows.Forms.Label();
            this.delinLabel = new System.Windows.Forms.Label();
            this.visualiseButton = new System.Windows.Forms.Button();
            this.editButton = new System.Windows.Forms.Button();
            this.hrusButton = new System.Windows.Forms.Button();
            this.delinButton = new System.Windows.Forms.Button();
            this.paramsButton = new System.Windows.Forms.Button();
            this.reportsBox = new System.Windows.Forms.ComboBox();
            this.reportsLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.projPath = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.mainBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(10, 14);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(205, 97);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // newButton
            // 
            this.newButton.Location = new System.Drawing.Point(223, 76);
            this.newButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.newButton.Name = "newButton";
            this.newButton.Size = new System.Drawing.Size(107, 27);
            this.newButton.TabIndex = 1;
            this.newButton.Text = "New Project";
            this.newButton.UseVisualStyleBackColor = true;
            this.newButton.Click += new System.EventHandler(this.newButton_Click);
            // 
            // existingButton
            // 
            this.existingButton.Location = new System.Drawing.Point(338, 76);
            this.existingButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.existingButton.Name = "existingButton";
            this.existingButton.Size = new System.Drawing.Size(105, 27);
            this.existingButton.TabIndex = 2;
            this.existingButton.Text = "Existing Project";
            this.existingButton.UseVisualStyleBackColor = true;
            this.existingButton.Click += new System.EventHandler(this.existingButton_Click);
            // 
            // aboutButton
            // 
            this.aboutButton.Location = new System.Drawing.Point(355, 14);
            this.aboutButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(88, 27);
            this.aboutButton.TabIndex = 3;
            this.aboutButton.Text = "About";
            this.aboutButton.UseVisualStyleBackColor = true;
            this.aboutButton.Click += new System.EventHandler(this.aboutButton_Click);
            // 
            // mainBox
            // 
            this.mainBox.Controls.Add(this.visualiseLabel);
            this.mainBox.Controls.Add(this.editLabel);
            this.mainBox.Controls.Add(this.hrusLabel);
            this.mainBox.Controls.Add(this.delinLabel);
            this.mainBox.Controls.Add(this.visualiseButton);
            this.mainBox.Controls.Add(this.editButton);
            this.mainBox.Controls.Add(this.hrusButton);
            this.mainBox.Controls.Add(this.delinButton);
            this.mainBox.Location = new System.Drawing.Point(213, 109);
            this.mainBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.mainBox.Name = "mainBox";
            this.mainBox.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.mainBox.Size = new System.Drawing.Size(240, 160);
            this.mainBox.TabIndex = 4;
            this.mainBox.TabStop = false;
            this.mainBox.Text = "MainSteps";
            // 
            // visualiseLabel
            // 
            this.visualiseLabel.Location = new System.Drawing.Point(7, 128);
            this.visualiseLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.visualiseLabel.Name = "visualiseLabel";
            this.visualiseLabel.Size = new System.Drawing.Size(47, 27);
            this.visualiseLabel.TabIndex = 7;
            this.visualiseLabel.Text = "Step 4";
            // 
            // editLabel
            // 
            this.editLabel.Location = new System.Drawing.Point(7, 95);
            this.editLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.editLabel.Name = "editLabel";
            this.editLabel.Size = new System.Drawing.Size(47, 27);
            this.editLabel.TabIndex = 6;
            this.editLabel.Text = "Step 3";
            // 
            // hrusLabel
            // 
            this.hrusLabel.Location = new System.Drawing.Point(7, 61);
            this.hrusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.hrusLabel.Name = "hrusLabel";
            this.hrusLabel.Size = new System.Drawing.Size(47, 27);
            this.hrusLabel.TabIndex = 5;
            this.hrusLabel.Text = "Step 2";
            // 
            // delinLabel
            // 
            this.delinLabel.Location = new System.Drawing.Point(7, 28);
            this.delinLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.delinLabel.Name = "delinLabel";
            this.delinLabel.Size = new System.Drawing.Size(47, 27);
            this.delinLabel.TabIndex = 4;
            this.delinLabel.Text = "Step 1";
            // 
            // visualiseButton
            // 
            this.visualiseButton.Location = new System.Drawing.Point(61, 122);
            this.visualiseButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.visualiseButton.Name = "visualiseButton";
            this.visualiseButton.Size = new System.Drawing.Size(173, 27);
            this.visualiseButton.TabIndex = 3;
            this.visualiseButton.Text = "Visualise";
            this.toolTip1.SetToolTip(this.visualiseButton, "Click to visualise outputs after a SWAT run");
            this.visualiseButton.UseVisualStyleBackColor = true;
            this.visualiseButton.Click += new System.EventHandler(this.visualiseButton_Click);
            // 
            // editButton
            // 
            this.editButton.Location = new System.Drawing.Point(61, 89);
            this.editButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.editButton.Name = "editButton";
            this.editButton.Size = new System.Drawing.Size(173, 27);
            this.editButton.TabIndex = 2;
            this.editButton.Text = "Edit Inputs and Run SWAT";
            this.toolTip1.SetToolTip(this.editButton, "Click to run the SWAT editor");
            this.editButton.UseVisualStyleBackColor = true;
            this.editButton.Click += new System.EventHandler(this.editButton_Click);
            // 
            // hrusButton
            // 
            this.hrusButton.Location = new System.Drawing.Point(61, 55);
            this.hrusButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.hrusButton.Name = "hrusButton";
            this.hrusButton.Size = new System.Drawing.Size(173, 27);
            this.hrusButton.TabIndex = 1;
            this.hrusButton.Text = "Create HRUs";
            this.toolTip1.SetToolTip(this.hrusButton, "Click to select landuse and soil files and create HRUs");
            this.hrusButton.UseVisualStyleBackColor = true;
            this.hrusButton.Click += new System.EventHandler(this.hrusButton_Click);
            // 
            // delinButton
            // 
            this.delinButton.Location = new System.Drawing.Point(61, 22);
            this.delinButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.delinButton.Name = "delinButton";
            this.delinButton.Size = new System.Drawing.Size(173, 27);
            this.delinButton.TabIndex = 0;
            this.delinButton.Text = "Delineate Watershed";
            this.toolTip1.SetToolTip(this.delinButton, "Click to select the DEM and delineate the watershed");
            this.delinButton.UseVisualStyleBackColor = true;
            this.delinButton.Click += new System.EventHandler(this.delinButton_Click);
            // 
            // paramsButton
            // 
            this.paramsButton.Location = new System.Drawing.Point(34, 192);
            this.paramsButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.paramsButton.Name = "paramsButton";
            this.paramsButton.Size = new System.Drawing.Size(156, 27);
            this.paramsButton.TabIndex = 7;
            this.paramsButton.Text = "ArcSWAT Parameters";
            this.toolTip1.SetToolTip(this.paramsButton, "Click to set the location of the SWAT editor and (optionally) MPI.  Unless these " +
        "locations change, you only need to do this once; the values are used for all pro" +
        "jects.");
            this.paramsButton.UseVisualStyleBackColor = true;
            this.paramsButton.Click += new System.EventHandler(this.paramsButton_Click);
            // 
            // reportsBox
            // 
            this.reportsBox.FormattingEnabled = true;
            this.reportsBox.Location = new System.Drawing.Point(34, 258);
            this.reportsBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.reportsBox.Name = "reportsBox";
            this.reportsBox.Size = new System.Drawing.Size(156, 23);
            this.reportsBox.TabIndex = 8;
            this.reportsBox.SelectedIndexChanged += new System.EventHandler(this.reportsBox_SelectedIndexChanged);
            // 
            // reportsLabel
            // 
            this.reportsLabel.Location = new System.Drawing.Point(34, 228);
            this.reportsLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.reportsLabel.Name = "reportsLabel";
            this.reportsLabel.Size = new System.Drawing.Size(117, 27);
            this.reportsLabel.TabIndex = 9;
            this.reportsLabel.Text = "Reports";
            this.reportsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(365, 298);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(88, 27);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // OKButton
            // 
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(251, 298);
            this.OKButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(88, 27);
            this.OKButton.TabIndex = 11;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // projPath
            // 
            this.projPath.Location = new System.Drawing.Point(35, 328);
            this.projPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.projPath.Name = "projPath";
            this.projPath.Size = new System.Drawing.Size(418, 27);
            this.projPath.TabIndex = 12;
            this.projPath.Text = "This is the path to the project";
            // 
            // MainForm
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(459, 355);
            this.Controls.Add(this.projPath);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.reportsLabel);
            this.Controls.Add(this.reportsBox);
            this.Controls.Add(this.paramsButton);
            this.Controls.Add(this.mainBox);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.existingButton);
            this.Controls.Add(this.newButton);
            this.Controls.Add(this.pictureBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "ArcSWAT";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.mainBox.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		private System.Windows.Forms.Label projPath;
		private System.Windows.Forms.Button OKButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Label reportsLabel;
		private System.Windows.Forms.ComboBox reportsBox;
		private System.Windows.Forms.Button paramsButton;
		private System.Windows.Forms.Button delinButton;
		private System.Windows.Forms.Button hrusButton;
		private System.Windows.Forms.Button editButton;
		private System.Windows.Forms.Button visualiseButton;
		private System.Windows.Forms.Label delinLabel;
		private System.Windows.Forms.Label hrusLabel;
		private System.Windows.Forms.Label editLabel;
		private System.Windows.Forms.Label visualiseLabel;
		private System.Windows.Forms.GroupBox mainBox;
		private System.Windows.Forms.Button aboutButton;
		private System.Windows.Forms.Button existingButton;
		private System.Windows.Forms.Button newButton;
		private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
