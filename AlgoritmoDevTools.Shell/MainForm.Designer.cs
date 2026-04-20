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
        toolsList = new System.Windows.Forms.ListBox();
        contentPanel = new System.Windows.Forms.Panel();
        splitter = new System.Windows.Forms.Splitter();
        descriptionLabel = new System.Windows.Forms.Label();
        SuspendLayout();
        //
        // toolsList
        //
        toolsList.Dock = System.Windows.Forms.DockStyle.Left;
        toolsList.FormattingEnabled = true;
        toolsList.IntegralHeight = false;
        toolsList.ItemHeight = 20;
        toolsList.Location = new System.Drawing.Point(0, 0);
        toolsList.Name = "toolsList";
        toolsList.Size = new System.Drawing.Size(220, 661);
        toolsList.TabIndex = 0;
        toolsList.SelectedIndexChanged += new System.EventHandler(toolsList_SelectedIndexChanged);
        //
        // splitter
        //
        splitter.Location = new System.Drawing.Point(220, 0);
        splitter.Name = "splitter";
        splitter.Size = new System.Drawing.Size(4, 661);
        splitter.TabIndex = 1;
        splitter.TabStop = false;
        //
        // descriptionLabel
        //
        descriptionLabel.Dock = System.Windows.Forms.DockStyle.Top;
        descriptionLabel.Location = new System.Drawing.Point(224, 0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new System.Drawing.Size(976, 40);
        descriptionLabel.TabIndex = 2;
        descriptionLabel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
        descriptionLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
        //
        // contentPanel
        //
        contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        contentPanel.Location = new System.Drawing.Point(224, 40);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new System.Drawing.Size(976, 621);
        contentPanel.TabIndex = 3;
        //
        // MainForm
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1200, 661);
        Controls.Add(contentPanel);
        Controls.Add(descriptionLabel);
        Controls.Add(splitter);
        Controls.Add(toolsList);
        Name = "MainForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "Algoritmo DevTools";
        ResumeLayout(false);
    }

    private System.Windows.Forms.ListBox toolsList;
    private System.Windows.Forms.Panel contentPanel;
    private System.Windows.Forms.Splitter splitter;
    private System.Windows.Forms.Label descriptionLabel;
}
