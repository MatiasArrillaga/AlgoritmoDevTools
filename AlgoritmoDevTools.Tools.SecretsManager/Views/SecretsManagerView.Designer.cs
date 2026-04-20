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
        SuspendLayout();
        // 
        // ListarSecretosBtn
        // 
        ListarSecretosBtn.Location = new Point(12, 60);
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
        VisorTxt.Location = new Point(12, 105);
        VisorTxt.Name = "VisorTxt";
        VisorTxt.ReadOnly = true;
        VisorTxt.Size = new Size(1169, 238);
        VisorTxt.TabIndex = 9;
        VisorTxt.Text = "";
        VisorTxt.WordWrap = false;
        // 
        // RestaurarSecretosBtn
        // 
        RestaurarSecretosBtn.Location = new Point(160, 60);
        RestaurarSecretosBtn.Name = "RestaurarSecretosBtn";
        RestaurarSecretosBtn.Size = new Size(159, 32);
        RestaurarSecretosBtn.TabIndex = 7;
        RestaurarSecretosBtn.Text = "Restaurar Secretos";
        RestaurarSecretosBtn.UseVisualStyleBackColor = true;
        RestaurarSecretosBtn.Click += RestaurarSecretosBtn_Click;
        // 
        // ModificarSecretoBtn
        // 
        ModificarSecretoBtn.Location = new Point(328, 60);
        ModificarSecretoBtn.Name = "ModificarSecretoBtn";
        ModificarSecretoBtn.Size = new Size(159, 32);
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
        DataBaseLbl.Location = new Point(525, 15);
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
        DataBaseCmb.Location = new Point(605, 12);
        DataBaseCmb.Name = "DataBaseCmb";
        DataBaseCmb.Size = new Size(220, 28);
        DataBaseCmb.TabIndex = 3;
        // 
        // NuevaConexionBtn
        // 
        NuevaConexionBtn.Location = new Point(840, 10);
        NuevaConexionBtn.Name = "NuevaConexionBtn";
        NuevaConexionBtn.Size = new Size(100, 31);
        NuevaConexionBtn.TabIndex = 4;
        NuevaConexionBtn.Text = "Nuevo";
        NuevaConexionBtn.UseVisualStyleBackColor = true;
        NuevaConexionBtn.Click += NuevaConexionBtn_Click;
        //
        // ModificarConexionBtn
        //
        ModificarConexionBtn.Location = new Point(950, 10);
        ModificarConexionBtn.Name = "ModificarConexionBtn";
        ModificarConexionBtn.Size = new Size(100, 31);
        ModificarConexionBtn.TabIndex = 5;
        ModificarConexionBtn.Text = "Modificar";
        ModificarConexionBtn.UseVisualStyleBackColor = true;
        ModificarConexionBtn.Click += ModificarConexionBtn_Click;
        //
        // EliminarConexionBtn
        //
        EliminarConexionBtn.Location = new Point(1060, 10);
        EliminarConexionBtn.Name = "EliminarConexionBtn";
        EliminarConexionBtn.Size = new Size(100, 31);
        EliminarConexionBtn.TabIndex = 6;
        EliminarConexionBtn.Text = "Eliminar";
        EliminarConexionBtn.UseVisualStyleBackColor = true;
        EliminarConexionBtn.Click += EliminarConexionBtn_Click;
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
        Controls.Add(ListarSecretosBtn);
        Controls.Add(RestaurarSecretosBtn);
        Controls.Add(ModificarSecretoBtn);
        Controls.Add(VisorTxt);
        Controls.Add(ListarSecretosBtn);
        Name = "SecretsManagerView";
        Size = new Size(1193, 358);
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
}
