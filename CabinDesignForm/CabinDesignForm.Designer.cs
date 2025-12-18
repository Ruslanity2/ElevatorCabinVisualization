namespace CabinDesignTool
{
    partial class CabinDesignForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.groupBoxVisualization = new System.Windows.Forms.GroupBox();
            this.pictureBoxVisualization = new System.Windows.Forms.PictureBox();
            this.groupBoxSizes = new System.Windows.Forms.GroupBox();
            this.groupBoxMasks = new System.Windows.Forms.GroupBox();
            this.groupBoxOptions = new System.Windows.Forms.GroupBox();
            this.groupBoxButtons = new System.Windows.Forms.GroupBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.tableLayoutPanel.SuspendLayout();
            this.groupBoxVisualization.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVisualization)).BeginInit();
            this.groupBoxButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.tableLayoutPanel.Controls.Add(this.groupBoxSizes, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.groupBoxVisualization, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.groupBoxMasks, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.groupBoxOptions, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.groupBoxButtons, 1, 3);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.Size = new System.Drawing.Size(720, 650);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // groupBoxVisualization
            // 
            this.groupBoxVisualization.Controls.Add(this.pictureBoxVisualization);
            this.groupBoxVisualization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxVisualization.Location = new System.Drawing.Point(3, 3);
            this.groupBoxVisualization.Name = "groupBoxVisualization";
            this.tableLayoutPanel.SetRowSpan(this.groupBoxVisualization, 4);
            this.groupBoxVisualization.Size = new System.Drawing.Size(454, 644);
            this.groupBoxVisualization.TabIndex = 1;
            this.groupBoxVisualization.TabStop = false;
            this.groupBoxVisualization.Text = "Визуализация";
            // 
            // pictureBoxVisualization
            // 
            this.pictureBoxVisualization.BackColor = System.Drawing.Color.White;
            this.pictureBoxVisualization.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxVisualization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxVisualization.Location = new System.Drawing.Point(3, 16);
            this.pictureBoxVisualization.Name = "pictureBoxVisualization";
            this.pictureBoxVisualization.Size = new System.Drawing.Size(448, 625);
            this.pictureBoxVisualization.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxVisualization.TabIndex = 0;
            this.pictureBoxVisualization.TabStop = false;
            // 
            // groupBoxSizes
            // 
            this.groupBoxSizes.AutoSize = true;
            this.groupBoxSizes.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBoxSizes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxSizes.Location = new System.Drawing.Point(463, 3);
            this.groupBoxSizes.MinimumSize = new System.Drawing.Size(0, 25);
            this.groupBoxSizes.Name = "groupBoxSizes";
            this.groupBoxSizes.Size = new System.Drawing.Size(254, 25);
            this.groupBoxSizes.TabIndex = 0;
            this.groupBoxSizes.TabStop = false;
            this.groupBoxSizes.Text = "Размеры";
            // 
            // groupBoxMasks
            // 
            this.groupBoxMasks.AutoSize = true;
            this.groupBoxMasks.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBoxMasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxMasks.Location = new System.Drawing.Point(463, 34);
            this.groupBoxMasks.MinimumSize = new System.Drawing.Size(0, 25);
            this.groupBoxMasks.Name = "groupBoxMasks";
            this.groupBoxMasks.Size = new System.Drawing.Size(254, 25);
            this.groupBoxMasks.TabIndex = 2;
            this.groupBoxMasks.TabStop = false;
            this.groupBoxMasks.Text = "Маски";
            // 
            // groupBoxOptions
            // 
            this.groupBoxOptions.AutoSize = true;
            this.groupBoxOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBoxOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxOptions.Location = new System.Drawing.Point(463, 65);
            this.groupBoxOptions.MinimumSize = new System.Drawing.Size(0, 25);
            this.groupBoxOptions.Name = "groupBoxOptions";
            this.groupBoxOptions.Size = new System.Drawing.Size(254, 25);
            this.groupBoxOptions.TabIndex = 3;
            this.groupBoxOptions.TabStop = false;
            this.groupBoxOptions.Text = "Опции";
            // 
            // groupBoxButtons
            // 
            this.groupBoxButtons.AutoSize = true;
            this.groupBoxButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBoxButtons.Controls.Add(this.btnExport);
            this.groupBoxButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxButtons.Location = new System.Drawing.Point(463, 159);
            this.groupBoxButtons.Name = "groupBoxButtons";
            this.groupBoxButtons.Size = new System.Drawing.Size(254, 488);
            this.groupBoxButtons.TabIndex = 4;
            this.groupBoxButtons.TabStop = false;
            this.groupBoxButtons.Text = "Кнопки";
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(18, 25);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(217, 30);
            this.btnExport.TabIndex = 0;
            this.btnExport.Text = "Передать";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.BtnExport_Click);
            // 
            // CabinDesignForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 650);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "CabinDesignForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CabinDesignTool";
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.groupBoxVisualization.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVisualization)).EndInit();
            this.groupBoxButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.GroupBox groupBoxVisualization;
        private System.Windows.Forms.PictureBox pictureBoxVisualization;
        private System.Windows.Forms.GroupBox groupBoxSizes;
        private System.Windows.Forms.GroupBox groupBoxMasks;
        private System.Windows.Forms.GroupBox groupBoxOptions;
        private System.Windows.Forms.GroupBox groupBoxButtons;
        private System.Windows.Forms.Button btnExport;
    }
}
