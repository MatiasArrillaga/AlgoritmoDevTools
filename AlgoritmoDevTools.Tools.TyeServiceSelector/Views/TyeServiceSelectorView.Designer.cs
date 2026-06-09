namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Views;

partial class TyeServiceSelectorView
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
        PathLbl = new System.Windows.Forms.Label();
        PerfilLbl = new System.Windows.Forms.Label();
        ProfilesCombo = new System.Windows.Forms.ComboBox();
        GuardarPerfilBtn = new System.Windows.Forms.Button();
        EliminarPerfilBtn = new System.Windows.Forms.Button();
        RefrescarBtn = new System.Windows.Forms.Button();
        MarcarTodosBtn = new System.Windows.Forms.Button();
        DesmarcarTodosBtn = new System.Windows.Forms.Button();
        ServicesList = new System.Windows.Forms.CheckedListBox();
        GenerarBtn = new System.Windows.Forms.Button();
        CopiarComandoBtn = new System.Windows.Forms.Button();
        StatusLbl = new System.Windows.Forms.Label();
        SuspendLayout();
        //
        // PathLbl
        //
        PathLbl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        PathLbl.AutoSize = false;
        PathLbl.Location = new System.Drawing.Point(12, 12);
        PathLbl.Name = "PathLbl";
        PathLbl.Size = new System.Drawing.Size(696, 36);
        PathLbl.TabIndex = 0;
        PathLbl.Text = "Master: ...";
        PathLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // PerfilLbl
        //
        PerfilLbl.AutoSize = false;
        PerfilLbl.Location = new System.Drawing.Point(12, 58);
        PerfilLbl.Name = "PerfilLbl";
        PerfilLbl.Size = new System.Drawing.Size(50, 24);
        PerfilLbl.TabIndex = 1;
        PerfilLbl.Text = "Perfil:";
        PerfilLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // ProfilesCombo
        //
        ProfilesCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        ProfilesCombo.Location = new System.Drawing.Point(66, 54);
        ProfilesCombo.Name = "ProfilesCombo";
        ProfilesCombo.Size = new System.Drawing.Size(300, 28);
        ProfilesCombo.TabIndex = 2;
        ProfilesCombo.Font = new System.Drawing.Font("Segoe UI", 9F);
        ProfilesCombo.SelectedIndexChanged += new System.EventHandler(ProfilesCombo_SelectedIndexChanged);
        //
        // GuardarPerfilBtn
        //
        GuardarPerfilBtn.Location = new System.Drawing.Point(374, 53);
        GuardarPerfilBtn.Name = "GuardarPerfilBtn";
        GuardarPerfilBtn.Size = new System.Drawing.Size(130, 30);
        GuardarPerfilBtn.TabIndex = 3;
        GuardarPerfilBtn.Text = "Guardar perfil";
        GuardarPerfilBtn.UseVisualStyleBackColor = true;
        GuardarPerfilBtn.Click += new System.EventHandler(GuardarPerfilBtn_Click);
        //
        // EliminarPerfilBtn
        //
        EliminarPerfilBtn.Location = new System.Drawing.Point(510, 53);
        EliminarPerfilBtn.Name = "EliminarPerfilBtn";
        EliminarPerfilBtn.Size = new System.Drawing.Size(130, 30);
        EliminarPerfilBtn.TabIndex = 4;
        EliminarPerfilBtn.Text = "Eliminar perfil";
        EliminarPerfilBtn.UseVisualStyleBackColor = true;
        EliminarPerfilBtn.Click += new System.EventHandler(EliminarPerfilBtn_Click);
        //
        // RefrescarBtn
        //
        RefrescarBtn.Location = new System.Drawing.Point(12, 92);
        RefrescarBtn.Name = "RefrescarBtn";
        RefrescarBtn.Size = new System.Drawing.Size(110, 30);
        RefrescarBtn.TabIndex = 5;
        RefrescarBtn.Text = "Refrescar";
        RefrescarBtn.UseVisualStyleBackColor = true;
        RefrescarBtn.Click += new System.EventHandler(RefrescarBtn_Click);
        //
        // MarcarTodosBtn
        //
        MarcarTodosBtn.Location = new System.Drawing.Point(128, 92);
        MarcarTodosBtn.Name = "MarcarTodosBtn";
        MarcarTodosBtn.Size = new System.Drawing.Size(130, 30);
        MarcarTodosBtn.TabIndex = 6;
        MarcarTodosBtn.Text = "Marcar todos";
        MarcarTodosBtn.UseVisualStyleBackColor = true;
        MarcarTodosBtn.Click += new System.EventHandler(MarcarTodosBtn_Click);
        //
        // DesmarcarTodosBtn
        //
        DesmarcarTodosBtn.Location = new System.Drawing.Point(264, 92);
        DesmarcarTodosBtn.Name = "DesmarcarTodosBtn";
        DesmarcarTodosBtn.Size = new System.Drawing.Size(130, 30);
        DesmarcarTodosBtn.TabIndex = 7;
        DesmarcarTodosBtn.Text = "Desmarcar todos";
        DesmarcarTodosBtn.UseVisualStyleBackColor = true;
        DesmarcarTodosBtn.Click += new System.EventHandler(DesmarcarTodosBtn_Click);
        //
        // ServicesList
        //
        ServicesList.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        ServicesList.CheckOnClick = true;
        ServicesList.IntegralHeight = false;
        ServicesList.Location = new System.Drawing.Point(12, 128);
        ServicesList.Name = "ServicesList";
        ServicesList.Size = new System.Drawing.Size(696, 360);
        ServicesList.TabIndex = 8;
        ServicesList.Font = new System.Drawing.Font("Segoe UI", 10F);
        //
        // GenerarBtn
        //
        GenerarBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        GenerarBtn.Location = new System.Drawing.Point(12, 498);
        GenerarBtn.Name = "GenerarBtn";
        GenerarBtn.Size = new System.Drawing.Size(200, 34);
        GenerarBtn.TabIndex = 9;
        GenerarBtn.Text = "Generar y guardar";
        GenerarBtn.UseVisualStyleBackColor = true;
        GenerarBtn.Click += new System.EventHandler(GenerarBtn_Click);
        //
        // CopiarComandoBtn
        //
        CopiarComandoBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        CopiarComandoBtn.Location = new System.Drawing.Point(220, 498);
        CopiarComandoBtn.Name = "CopiarComandoBtn";
        CopiarComandoBtn.Size = new System.Drawing.Size(200, 34);
        CopiarComandoBtn.TabIndex = 10;
        CopiarComandoBtn.Text = "Copiar comando run";
        CopiarComandoBtn.UseVisualStyleBackColor = true;
        CopiarComandoBtn.Click += new System.EventHandler(CopiarComandoBtn_Click);
        //
        // StatusLbl
        //
        StatusLbl.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        StatusLbl.AutoSize = false;
        StatusLbl.Location = new System.Drawing.Point(12, 538);
        StatusLbl.Name = "StatusLbl";
        StatusLbl.Size = new System.Drawing.Size(696, 24);
        StatusLbl.TabIndex = 11;
        StatusLbl.Text = "Listo.";
        StatusLbl.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        //
        // TyeServiceSelectorView
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        Controls.Add(PathLbl);
        Controls.Add(PerfilLbl);
        Controls.Add(ProfilesCombo);
        Controls.Add(GuardarPerfilBtn);
        Controls.Add(EliminarPerfilBtn);
        Controls.Add(RefrescarBtn);
        Controls.Add(MarcarTodosBtn);
        Controls.Add(DesmarcarTodosBtn);
        Controls.Add(ServicesList);
        Controls.Add(GenerarBtn);
        Controls.Add(CopiarComandoBtn);
        Controls.Add(StatusLbl);
        Name = "TyeServiceSelectorView";
        Size = new System.Drawing.Size(720, 580);
        Load += new System.EventHandler(TyeServiceSelectorView_Load);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label PathLbl;
    private System.Windows.Forms.Label PerfilLbl;
    private System.Windows.Forms.ComboBox ProfilesCombo;
    private System.Windows.Forms.Button GuardarPerfilBtn;
    private System.Windows.Forms.Button EliminarPerfilBtn;
    private System.Windows.Forms.Button RefrescarBtn;
    private System.Windows.Forms.Button MarcarTodosBtn;
    private System.Windows.Forms.Button DesmarcarTodosBtn;
    private System.Windows.Forms.CheckedListBox ServicesList;
    private System.Windows.Forms.Button GenerarBtn;
    private System.Windows.Forms.Button CopiarComandoBtn;
    private System.Windows.Forms.Label StatusLbl;
}
