namespace AlgoritmoDevTools.Tools.SecretsManager.Dialogs;

partial class ElegirBaseDialog
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
        ConexionLbl = new System.Windows.Forms.Label();
        ConexionesCmb = new System.Windows.Forms.ComboBox();
        BaseLbl = new System.Windows.Forms.Label();
        BasesCmb = new System.Windows.Forms.ComboBox();
        EstadoLbl = new System.Windows.Forms.Label();
        AplicarBtn = new System.Windows.Forms.Button();
        CancelarBtn = new System.Windows.Forms.Button();
        SuspendLayout();
        //
        // ConexionLbl
        //
        ConexionLbl.AutoSize = false;
        ConexionLbl.Location = new System.Drawing.Point(14, 18);
        ConexionLbl.Name = "ConexionLbl";
        ConexionLbl.Size = new System.Drawing.Size(110, 24);
        ConexionLbl.TabIndex = 0;
        ConexionLbl.Text = "Conexión:";
        ConexionLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // ConexionesCmb
        //
        ConexionesCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        ConexionesCmb.Location = new System.Drawing.Point(130, 14);
        ConexionesCmb.Name = "ConexionesCmb";
        ConexionesCmb.Size = new System.Drawing.Size(390, 28);
        ConexionesCmb.TabIndex = 1;
        ConexionesCmb.Font = new System.Drawing.Font("Segoe UI", 9F);
        ConexionesCmb.SelectedIndexChanged += new System.EventHandler(ConexionesCmb_SelectedIndexChanged);
        //
        // BaseLbl
        //
        BaseLbl.AutoSize = false;
        BaseLbl.Location = new System.Drawing.Point(14, 58);
        BaseLbl.Name = "BaseLbl";
        BaseLbl.Size = new System.Drawing.Size(110, 24);
        BaseLbl.TabIndex = 2;
        BaseLbl.Text = "Base de datos:";
        BaseLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // BasesCmb
        //
        BasesCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        BasesCmb.Location = new System.Drawing.Point(130, 54);
        BasesCmb.Name = "BasesCmb";
        BasesCmb.Size = new System.Drawing.Size(390, 28);
        BasesCmb.TabIndex = 3;
        BasesCmb.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // EstadoLbl
        //
        EstadoLbl.AutoSize = false;
        EstadoLbl.Location = new System.Drawing.Point(14, 92);
        EstadoLbl.Name = "EstadoLbl";
        EstadoLbl.Size = new System.Drawing.Size(506, 36);
        EstadoLbl.TabIndex = 4;
        EstadoLbl.Text = "";
        EstadoLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // AplicarBtn
        //
        AplicarBtn.Location = new System.Drawing.Point(290, 136);
        AplicarBtn.Name = "AplicarBtn";
        AplicarBtn.Size = new System.Drawing.Size(110, 32);
        AplicarBtn.TabIndex = 5;
        AplicarBtn.Text = "Aplicar";
        AplicarBtn.UseVisualStyleBackColor = true;
        AplicarBtn.Click += new System.EventHandler(AplicarBtn_Click);
        //
        // CancelarBtn
        //
        CancelarBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        CancelarBtn.Location = new System.Drawing.Point(410, 136);
        CancelarBtn.Name = "CancelarBtn";
        CancelarBtn.Size = new System.Drawing.Size(110, 32);
        CancelarBtn.TabIndex = 6;
        CancelarBtn.Text = "Cancelar";
        CancelarBtn.UseVisualStyleBackColor = true;
        //
        // ElegirBaseDialog
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton = CancelarBtn;
        ClientSize = new System.Drawing.Size(534, 182);
        Controls.Add(ConexionLbl);
        Controls.Add(ConexionesCmb);
        Controls.Add(BaseLbl);
        Controls.Add(BasesCmb);
        Controls.Add(EstadoLbl);
        Controls.Add(AplicarBtn);
        Controls.Add(CancelarBtn);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ElegirBaseDialog";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "Apuntar los secretos a una base";
        Load += new System.EventHandler(ElegirBaseDialog_Load);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label ConexionLbl;
    private System.Windows.Forms.ComboBox ConexionesCmb;
    private System.Windows.Forms.Label BaseLbl;
    private System.Windows.Forms.ComboBox BasesCmb;
    private System.Windows.Forms.Label EstadoLbl;
    private System.Windows.Forms.Button AplicarBtn;
    private System.Windows.Forms.Button CancelarBtn;
}
