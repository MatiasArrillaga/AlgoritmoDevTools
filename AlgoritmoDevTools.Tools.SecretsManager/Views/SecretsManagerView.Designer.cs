namespace AlgoritmoDevTools.Tools.SecretsManager.Views;

partial class SecretsManagerView
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
        ListarSecretosBtn = new Button();
        VisorTxt = new RichTextBox();
        RestaurarSecretosBtn = new Button();
        ModificarSecretoBtn = new Button();
        SavedConnectionsCmb = new ComboBox();
        SavedConnectionsLbl = new Label();
        DataBaseLbl = new Label();
        DataBaseCmb = new ComboBox();
        NuevaConexionBtn = new Button();
        ModificarConexionBtn = new Button();
        EliminarConexionBtn = new Button();
        MenuAgregarBtn = new Button();
        MenuQuitarBtn = new Button();
        MenuEstadoLbl = new Label();
        SuspendLayout();
        // 
        // ListarSecretosBtn
        // 
        ListarSecretosBtn.Location = new Point(110, 84);
        ListarSecretosBtn.Name = "ListarSecretosBtn";
        ListarSecretosBtn.Size = new Size(139, 32);
        ListarSecretosBtn.TabIndex = 6;
        ListarSecretosBtn.Text = "Listar Secretos";
        ListarSecretosBtn.UseVisualStyleBackColor = true;
        ListarSecretosBtn.Click += ListarSecretosBtn_Click;
        // 
        // VisorTxt
        // 
        VisorTxt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        VisorTxt.Location = new Point(12, 124);
        VisorTxt.Name = "VisorTxt";
        VisorTxt.ReadOnly = true;
        VisorTxt.Size = new Size(1169, 234);
        VisorTxt.TabIndex = 9;
        VisorTxt.Text = "";
        VisorTxt.WordWrap = false;
        // 
        // RestaurarSecretosBtn
        // 
        RestaurarSecretosBtn.Location = new Point(255, 84);
        RestaurarSecretosBtn.Name = "RestaurarSecretosBtn";
        RestaurarSecretosBtn.Size = new Size(159, 32);
        RestaurarSecretosBtn.TabIndex = 7;
        RestaurarSecretosBtn.Text = "Restaurar Secretos";
        RestaurarSecretosBtn.UseVisualStyleBackColor = true;
        RestaurarSecretosBtn.Click += RestaurarSecretosBtn_Click;
        // 
        // ModificarSecretoBtn
        // 
        ModificarSecretoBtn.Location = new Point(516, 46);
        ModificarSecretoBtn.Name = "ModificarSecretoBtn";
        ModificarSecretoBtn.Size = new Size(153, 32);
        ModificarSecretoBtn.TabIndex = 8;
        ModificarSecretoBtn.Text = "Modificar Secreto";
        ModificarSecretoBtn.UseVisualStyleBackColor = true;
        ModificarSecretoBtn.Click += ModificarSecretoBtn_Click;
        // 
        // SavedConnectionsCmb
        // 
        SavedConnectionsCmb.DropDownStyle = ComboBoxStyle.DropDownList;
        SavedConnectionsCmb.FormattingEnabled = true;
        SavedConnectionsCmb.Location = new Point(110, 12);
        SavedConnectionsCmb.Name = "SavedConnectionsCmb";
        SavedConnectionsCmb.Size = new Size(400, 28);
        SavedConnectionsCmb.TabIndex = 1;
        SavedConnectionsCmb.SelectedIndexChanged += SavedConnectionsCmb_SelectedIndexChanged;
        // 
        // SavedConnectionsLbl
        // 
        SavedConnectionsLbl.AutoSize = true;
        SavedConnectionsLbl.Location = new Point(12, 15);
        SavedConnectionsLbl.Name = "SavedConnectionsLbl";
        SavedConnectionsLbl.Size = new Size(85, 20);
        SavedConnectionsLbl.TabIndex = 0;
        SavedConnectionsLbl.Text = "Conexiones";
        // 
        // DataBaseLbl
        // 
        DataBaseLbl.AutoSize = true;
        DataBaseLbl.Location = new Point(12, 49);
        DataBaseLbl.Name = "DataBaseLbl";
        DataBaseLbl.Size = new Size(72, 20);
        DataBaseLbl.TabIndex = 2;
        DataBaseLbl.Text = "DataBase";
        // 
        // DataBaseCmb
        // 
        DataBaseCmb.DropDownStyle = ComboBoxStyle.DropDownList;
        DataBaseCmb.Enabled = false;
        DataBaseCmb.FormattingEnabled = true;
        DataBaseCmb.Location = new Point(110, 46);
        DataBaseCmb.Name = "DataBaseCmb";
        DataBaseCmb.Size = new Size(400, 28);
        DataBaseCmb.TabIndex = 3;
        // 
        // NuevaConexionBtn
        // 
        NuevaConexionBtn.Location = new Point(516, 10);
        NuevaConexionBtn.Name = "NuevaConexionBtn";
        NuevaConexionBtn.Size = new Size(100, 31);
        NuevaConexionBtn.TabIndex = 4;
        NuevaConexionBtn.Text = "Nuevo";
        NuevaConexionBtn.UseVisualStyleBackColor = true;
        NuevaConexionBtn.Click += NuevaConexionBtn_Click;
        // 
        // ModificarConexionBtn
        // 
        ModificarConexionBtn.Location = new Point(626, 10);
        ModificarConexionBtn.Name = "ModificarConexionBtn";
        ModificarConexionBtn.Size = new Size(100, 31);
        ModificarConexionBtn.TabIndex = 5;
        ModificarConexionBtn.Text = "Modificar";
        ModificarConexionBtn.UseVisualStyleBackColor = true;
        ModificarConexionBtn.Click += ModificarConexionBtn_Click;
        // 
        // EliminarConexionBtn
        // 
        EliminarConexionBtn.Location = new Point(736, 10);
        EliminarConexionBtn.Name = "EliminarConexionBtn";
        EliminarConexionBtn.Size = new Size(100, 31);
        EliminarConexionBtn.TabIndex = 6;
        EliminarConexionBtn.Text = "Eliminar";
        EliminarConexionBtn.UseVisualStyleBackColor = true;
        EliminarConexionBtn.Click += EliminarConexionBtn_Click;
        // 
        // MenuAgregarBtn
        // 
        MenuAgregarBtn.Location = new Point(516, 84);
        MenuAgregarBtn.Name = "MenuAgregarBtn";
        MenuAgregarBtn.Size = new Size(153, 32);
        MenuAgregarBtn.TabIndex = 10;
        MenuAgregarBtn.Text = "Agregar al menú";
        MenuAgregarBtn.UseVisualStyleBackColor = true;
        MenuAgregarBtn.Click += MenuAgregarBtn_Click;
        // 
        // MenuQuitarBtn
        // 
        MenuQuitarBtn.Location = new Point(675, 86);
        MenuQuitarBtn.Name = "MenuQuitarBtn";
        MenuQuitarBtn.Size = new Size(155, 32);
        MenuQuitarBtn.TabIndex = 11;
        MenuQuitarBtn.Text = "Quitar del menú";
        MenuQuitarBtn.UseVisualStyleBackColor = true;
        MenuQuitarBtn.Click += MenuQuitarBtn_Click;
        // 
        // MenuEstadoLbl
        // 
        MenuEstadoLbl.Location = new Point(836, 86);
        MenuEstadoLbl.Name = "MenuEstadoLbl";
        MenuEstadoLbl.Size = new Size(345, 22);
        MenuEstadoLbl.TabIndex = 12;
        // 
        // SecretsManagerView
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(SavedConnectionsLbl);
        Controls.Add(SavedConnectionsCmb);
        Controls.Add(DataBaseLbl);
        Controls.Add(DataBaseCmb);
        Controls.Add(NuevaConexionBtn);
        Controls.Add(ModificarConexionBtn);
        Controls.Add(EliminarConexionBtn);
        Controls.Add(MenuAgregarBtn);
        Controls.Add(MenuQuitarBtn);
        Controls.Add(MenuEstadoLbl);
        Controls.Add(RestaurarSecretosBtn);
        Controls.Add(ModificarSecretoBtn);
        Controls.Add(VisorTxt);
        Controls.Add(ListarSecretosBtn);
        Name = "SecretsManagerView";
        Size = new Size(1193, 373);
        Load += SecretsManagerView_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button ListarSecretosBtn;
    private System.Windows.Forms.RichTextBox VisorTxt;
    private System.Windows.Forms.Button RestaurarSecretosBtn;
    private System.Windows.Forms.Button ModificarSecretoBtn;
    private System.Windows.Forms.ComboBox SavedConnectionsCmb;
    private System.Windows.Forms.Label SavedConnectionsLbl;
    private System.Windows.Forms.Label DataBaseLbl;
    private System.Windows.Forms.ComboBox DataBaseCmb;
    private System.Windows.Forms.Button NuevaConexionBtn;
    private System.Windows.Forms.Button ModificarConexionBtn;
    private System.Windows.Forms.Button EliminarConexionBtn;
    private System.Windows.Forms.Button MenuAgregarBtn;
    private System.Windows.Forms.Button MenuQuitarBtn;
    private System.Windows.Forms.Label MenuEstadoLbl;
}
