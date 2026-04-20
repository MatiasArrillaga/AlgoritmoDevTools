namespace AlgoritmoDevTools.Shell;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        toolsList = new ListView();
        toolsImages = new ImageList(components);
        contentPanel = new Panel();
        splitter = new Splitter();
        descriptionLabel = new Label();
        statusStrip = new StatusStrip();
        statusServerLabel = new ToolStripStatusLabel();
        statusDatabaseLabel = new ToolStripStatusLabel();
        statusMessageLabel = new ToolStripStatusLabel();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // toolsList
        // 
        toolsList.Dock = DockStyle.Left;
        toolsList.FullRowSelect = true;
        toolsList.HeaderStyle = ColumnHeaderStyle.None;
        toolsList.Location = new Point(0, 0);
        toolsList.MultiSelect = false;
        toolsList.Name = "toolsList";
        toolsList.Size = new Size(220, 281);
        toolsList.SmallImageList = toolsImages;
        toolsList.TabIndex = 0;
        toolsList.UseCompatibleStateImageBehavior = false;
        toolsList.View = View.Details;
        toolsList.SelectedIndexChanged += toolsList_SelectedIndexChanged;
        // 
        // toolsImages
        // 
        toolsImages.ColorDepth = ColorDepth.Depth32Bit;
        toolsImages.ImageSize = new Size(24, 24);
        toolsImages.TransparentColor = Color.Transparent;
        // 
        // contentPanel
        // 
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(224, 40);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(1158, 241);
        contentPanel.TabIndex = 3;
        // 
        // splitter
        // 
        splitter.Location = new Point(220, 0);
        splitter.Name = "splitter";
        splitter.Size = new Size(4, 281);
        splitter.TabIndex = 1;
        splitter.TabStop = false;
        // 
        // descriptionLabel
        // 
        descriptionLabel.Dock = DockStyle.Top;
        descriptionLabel.Font = new Font("Segoe UI", 10F);
        descriptionLabel.Location = new Point(224, 0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Padding = new Padding(12, 10, 12, 10);
        descriptionLabel.Size = new Size(1158, 40);
        descriptionLabel.TabIndex = 2;
        descriptionLabel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
        descriptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
        // 
        // statusStrip
        // 
        statusStrip.ImageScalingSize = new Size(20, 20);
        statusStrip.Items.AddRange(new ToolStripItem[] { statusServerLabel, statusDatabaseLabel, statusMessageLabel });
        statusStrip.Location = new Point(0, 281);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1382, 30);
        statusStrip.TabIndex = 4;
        // 
        // statusServerLabel
        // 
        statusServerLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
        statusServerLabel.BorderStyle = Border3DStyle.Etched;
        statusServerLabel.Name = "statusServerLabel";
        statusServerLabel.Padding = new Padding(6, 0, 6, 0);
        statusServerLabel.Size = new Size(79, 24);
        statusServerLabel.Text = "Server: -";
        statusServerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statusDatabaseLabel
        // 
        statusDatabaseLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
        statusDatabaseLabel.BorderStyle = Border3DStyle.Etched;
        statusDatabaseLabel.Name = "statusDatabaseLabel";
        statusDatabaseLabel.Padding = new Padding(6, 0, 6, 0);
        statusDatabaseLabel.Size = new Size(69, 24);
        statusDatabaseLabel.Text = "Base: -";
        statusDatabaseLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statusMessageLabel
        // 
        statusMessageLabel.Name = "statusMessageLabel";
        statusMessageLabel.Padding = new Padding(6, 0, 6, 0);
        statusMessageLabel.Size = new Size(1219, 24);
        statusMessageLabel.Spring = true;
        statusMessageLabel.Text = "Cargando secretos...";
        statusMessageLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1382, 311);
        Controls.Add(contentPanel);
        Controls.Add(descriptionLabel);
        Controls.Add(splitter);
        Controls.Add(toolsList);
        Controls.Add(statusStrip);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Algoritmo DevTools";
        Load += MainForm_Load;
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.ListView toolsList;
    private System.Windows.Forms.ImageList toolsImages;
    private System.Windows.Forms.Panel contentPanel;
    private System.Windows.Forms.Splitter splitter;
    private System.Windows.Forms.Label descriptionLabel;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel statusServerLabel;
    private System.Windows.Forms.ToolStripStatusLabel statusDatabaseLabel;
    private System.Windows.Forms.ToolStripStatusLabel statusMessageLabel;
}
