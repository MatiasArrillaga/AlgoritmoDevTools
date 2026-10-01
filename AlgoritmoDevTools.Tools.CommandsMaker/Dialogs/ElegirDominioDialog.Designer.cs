namespace AlgoritmoDevTools.Tools.CommandsMaker.Dialogs;

partial class ElegirDominioDialog
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
        DominioLbl = new System.Windows.Forms.Label();
        DominiosCmb = new System.Windows.Forms.ComboBox();
        AyudaLbl = new System.Windows.Forms.Label();
        AceptarBtn = new System.Windows.Forms.Button();
        CancelarBtn = new System.Windows.Forms.Button();
        SuspendLayout();
        //
        // DominioLbl
        //
        DominioLbl.AutoSize = false;
        DominioLbl.Location = new System.Drawing.Point(14, 18);
        DominioLbl.Name = "DominioLbl";
        DominioLbl.Size = new System.Drawing.Size(70, 24);
        DominioLbl.TabIndex = 0;
        DominioLbl.Text = "Dominio:";
        DominioLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // DominiosCmb
        //
        DominiosCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        DominiosCmb.Location = new System.Drawing.Point(90, 14);
        DominiosCmb.Name = "DominiosCmb";
        DominiosCmb.Size = new System.Drawing.Size(300, 28);
        DominiosCmb.TabIndex = 1;
        DominiosCmb.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // AyudaLbl
        //
        AyudaLbl.AutoSize = false;
        AyudaLbl.Location = new System.Drawing.Point(14, 50);
        AyudaLbl.Name = "AyudaLbl";
        AyudaLbl.Size = new System.Drawing.Size(376, 40);
        AyudaLbl.TabIndex = 2;
        AyudaLbl.Text = "Los comandos del menú pasan a ser los de este dominio.";
        AyudaLbl.ForeColor = System.Drawing.Color.Gray;
        AyudaLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // AceptarBtn
        //
        AceptarBtn.Location = new System.Drawing.Point(160, 96);
        AceptarBtn.Name = "AceptarBtn";
        AceptarBtn.Size = new System.Drawing.Size(110, 32);
        AceptarBtn.TabIndex = 3;
        AceptarBtn.Text = "Aceptar";
        AceptarBtn.UseVisualStyleBackColor = true;
        AceptarBtn.Click += new System.EventHandler(AceptarBtn_Click);
        //
        // CancelarBtn
        //
        CancelarBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        CancelarBtn.Location = new System.Drawing.Point(280, 96);
        CancelarBtn.Name = "CancelarBtn";
        CancelarBtn.Size = new System.Drawing.Size(110, 32);
        CancelarBtn.TabIndex = 4;
        CancelarBtn.Text = "Cancelar";
        CancelarBtn.UseVisualStyleBackColor = true;
        //
        // ElegirDominioDialog
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        AcceptButton = AceptarBtn;
        CancelButton = CancelarBtn;
        ClientSize = new System.Drawing.Size(404, 142);
        Controls.Add(DominioLbl);
        Controls.Add(DominiosCmb);
        Controls.Add(AyudaLbl);
        Controls.Add(AceptarBtn);
        Controls.Add(CancelarBtn);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ElegirDominioDialog";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "Elegir dominio";
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label DominioLbl;
    private System.Windows.Forms.ComboBox DominiosCmb;
    private System.Windows.Forms.Label AyudaLbl;
    private System.Windows.Forms.Button AceptarBtn;
    private System.Windows.Forms.Button CancelarBtn;
}
