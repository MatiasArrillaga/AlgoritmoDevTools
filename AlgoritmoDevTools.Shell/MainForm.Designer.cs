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
        sidePanel = new Panel();
        toolsList = new ListView();
        settingsButton = new Button();
        toolsImages = new ImageList(components);
        contentPanel = new Panel();
        splitter = new Splitter();
        descriptionLabel = new Label();
        statusStrip = new StatusStrip();
        statusServerLabel = new ToolStripStatusLabel();
        statusDatabaseLabel = new ToolStripStatusLabel();
        statusMessageLabel = new ToolStripStatusLabel();
        sidePanel.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // sidePanel
        //
        sidePanel.Controls.Add(toolsList);
        sidePanel.Controls.Add(settingsButton);
        sidePanel.Dock = DockStyle.Left;
        sidePanel.Location = new Point(0, 0);
        sidePanel.Name = "sidePanel";
        sidePanel.Padding = new Padding(0, 12, 0, 0);
        sidePanel.Size = new Size(230, 281);
        sidePanel.TabIndex = 0;
        sidePanel.Tag = "lateral";
        //
        // toolsList
        //
        toolsList.BorderStyle = BorderStyle.None;
        toolsList.Dock = DockStyle.Fill;
        toolsList.FullRowSelect = true;
        toolsList.HeaderStyle = ColumnHeaderStyle.None;
        toolsList.Location = new Point(0, 12);
        toolsList.MultiSelect = false;
        toolsList.Name = "toolsList";
        toolsList.Size = new Size(230, 225);
        toolsList.SmallImageList = toolsImages;
        toolsList.TabIndex = 0;
        toolsList.Tag = "lateral";
        toolsList.UseCompatibleStateImageBehavior = false;
        toolsList.View = View.Details;
        toolsList.SelectedIndexChanged += toolsList_SelectedIndexChanged;
        //
        // settingsButton
        //
        // Va al pie del panel, separado de la lista de herramientas: no es una herramienta más.
        settingsButton.Dock = DockStyle.Bottom;
        settingsButton.FlatStyle = FlatStyle.Flat;
        settingsButton.Location = new Point(0, 237);
        settingsButton.Name = "settingsButton";
        settingsButton.Padding = new Padding(14, 0, 0, 0);
        settingsButton.Size = new Size(230, 44);
        settingsButton.TabIndex = 1;
        settingsButton.Text = "Configuraciones";
        settingsButton.TextAlign = ContentAlignment.MiddleLeft;
        settingsButton.UseVisualStyleBackColor = false;
        settingsButton.Click += settingsButton_Click;
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
        contentPanel.Location = new Point(231, 44);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(1158, 241);
        contentPanel.Padding = new Padding(24, 20, 24, 20);
        contentPanel.TabIndex = 4;
        // 
        // splitter
        //
        // Un solo pixel: es la linea que separa secciones, y alcanza para seguir arrastrando el panel.
        splitter.Location = new Point(230, 0);
        splitter.MinExtra = 180;
        splitter.MinSize = 190;
        splitter.Name = "splitter";
        splitter.Size = new Size(1, 281);
        splitter.TabIndex = 2;
        splitter.TabStop = false;
        splitter.Tag = "separador";
        //
        // descriptionLabel
        //
        descriptionLabel.Dock = DockStyle.Top;
        descriptionLabel.Font = new Font("Segoe UI", 10F);
        descriptionLabel.Location = new Point(231, 0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Padding = new Padding(24, 10, 24, 10);
        descriptionLabel.Size = new Size(1151, 44);
        descriptionLabel.TabIndex = 3;
        descriptionLabel.Tag = "sutil";
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
        statusServerLabel.BorderSides = ToolStripStatusLabelBorderSides.None;
        statusServerLabel.Name = "statusServerLabel";
        statusServerLabel.Padding = new Padding(6, 0, 6, 0);
        statusServerLabel.Size = new Size(79, 24);
        statusServerLabel.Text = "Server: -";
        statusServerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statusDatabaseLabel
        // 
        statusDatabaseLabel.BorderSides = ToolStripStatusLabelBorderSides.None;
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
        // El alto tiene que entrar la vista más alta de las tools (460 px del Selector de Servicios)
        // más la descripción de arriba (40) y el status bar (30); si no hay que estirar la ventana
        // a mano cada vez. El mínimo evita que se achique por debajo de eso.
        ClientSize = new Size(1460, 580);
        MinimumSize = new Size(1180, 560);
        Controls.Add(contentPanel);
        Controls.Add(descriptionLabel);
        Controls.Add(splitter);
        Controls.Add(sidePanel);
        Controls.Add(statusStrip);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Algoritmo DevTools";
        Load += MainForm_Load;
        sidePanel.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Panel sidePanel;
    private System.Windows.Forms.ListView toolsList;
    private System.Windows.Forms.Button settingsButton;
    private System.Windows.Forms.ImageList toolsImages;
    private System.Windows.Forms.Panel contentPanel;
    private System.Windows.Forms.Splitter splitter;
    private System.Windows.Forms.Label descriptionLabel;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel statusServerLabel;
    private System.Windows.Forms.ToolStripStatusLabel statusDatabaseLabel;
    private System.Windows.Forms.ToolStripStatusLabel statusMessageLabel;
}
