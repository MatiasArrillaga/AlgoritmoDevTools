namespace AlgoritmoDevTools.Tools.MarkdownConverter.Views;

partial class MarkdownConverterView
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
        PandocLbl = new System.Windows.Forms.Label();
        FormatosLbl = new System.Windows.Forms.Label();
        DropPanel = new System.Windows.Forms.Panel();
        DropLbl = new System.Windows.Forms.Label();
        ElegirBtn = new System.Windows.Forms.Button();
        DetenerBtn = new System.Windows.Forms.Button();
        AbrirCarpetaBtn = new System.Windows.Forms.Button();
        TachadoChk = new System.Windows.Forms.CheckBox();
        HtmlLbl = new System.Windows.Forms.Label();
        HtmlCmb = new System.Windows.Forms.ComboBox();
        MenuAgregarBtn = new System.Windows.Forms.Button();
        MenuQuitarBtn = new System.Windows.Forms.Button();
        MenuClasicoBtn = new System.Windows.Forms.Button();
        MenuEstadoLbl = new System.Windows.Forms.Label();
        StatusLbl = new System.Windows.Forms.Label();
        OutputTxt = new System.Windows.Forms.RichTextBox();
        DropPanel.SuspendLayout();
        SuspendLayout();
        //
        // PandocLbl
        //
        PandocLbl.AutoSize = false;
        PandocLbl.Location = new System.Drawing.Point(12, 12);
        PandocLbl.Name = "PandocLbl";
        PandocLbl.Size = new System.Drawing.Size(1200, 20);
        PandocLbl.TabIndex = 0;
        PandocLbl.Text = "pandoc: ...";
        PandocLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // FormatosLbl
        //
        FormatosLbl.AutoSize = false;
        FormatosLbl.Location = new System.Drawing.Point(12, 36);
        FormatosLbl.Name = "FormatosLbl";
        FormatosLbl.Size = new System.Drawing.Size(1200, 20);
        FormatosLbl.TabIndex = 1;
        FormatosLbl.Text = "Formatos: —";
        FormatosLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // DropPanel
        //
        DropPanel.AllowDrop = true;
        DropPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        DropPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        DropPanel.Location = new System.Drawing.Point(12, 66);
        DropPanel.Name = "DropPanel";
        DropPanel.Size = new System.Drawing.Size(1200, 90);
        DropPanel.TabIndex = 2;
        DropPanel.Controls.Add(DropLbl);
        //
        // DropLbl
        //
        DropLbl.AllowDrop = true;
        DropLbl.Dock = System.Windows.Forms.DockStyle.Fill;
        DropLbl.Name = "DropLbl";
        DropLbl.TabIndex = 0;
        DropLbl.Text = "Arrastrá acá los documentos a convertir";
        DropLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        DropLbl.Font = new System.Drawing.Font("Segoe UI", 10F);
        //
        // ElegirBtn
        //
        ElegirBtn.Location = new System.Drawing.Point(12, 168);
        ElegirBtn.Name = "ElegirBtn";
        ElegirBtn.Size = new System.Drawing.Size(160, 32);
        ElegirBtn.TabIndex = 3;
        ElegirBtn.Text = "Elegir archivos...";
        ElegirBtn.UseVisualStyleBackColor = true;
        ElegirBtn.Click += new System.EventHandler(ElegirBtn_Click);
        //
        // DetenerBtn
        //
        DetenerBtn.Enabled = false;
        DetenerBtn.Location = new System.Drawing.Point(180, 168);
        DetenerBtn.Name = "DetenerBtn";
        DetenerBtn.Size = new System.Drawing.Size(120, 32);
        DetenerBtn.TabIndex = 4;
        DetenerBtn.Text = "Detener";
        DetenerBtn.UseVisualStyleBackColor = true;
        DetenerBtn.Click += new System.EventHandler(DetenerBtn_Click);
        //
        // AbrirCarpetaBtn
        //
        AbrirCarpetaBtn.Enabled = false;
        AbrirCarpetaBtn.Location = new System.Drawing.Point(308, 168);
        AbrirCarpetaBtn.Name = "AbrirCarpetaBtn";
        AbrirCarpetaBtn.Size = new System.Drawing.Size(160, 32);
        AbrirCarpetaBtn.TabIndex = 5;
        AbrirCarpetaBtn.Text = "Abrir carpeta";
        AbrirCarpetaBtn.UseVisualStyleBackColor = true;
        AbrirCarpetaBtn.Click += new System.EventHandler(AbrirCarpetaBtn_Click);
        //
        // TachadoChk
        //
        TachadoChk.Checked = true;
        TachadoChk.CheckState = System.Windows.Forms.CheckState.Checked;
        TachadoChk.Location = new System.Drawing.Point(490, 174);
        TachadoChk.Name = "TachadoChk";
        TachadoChk.Size = new System.Drawing.Size(210, 24);
        TachadoChk.TabIndex = 6;
        TachadoChk.Text = "Quitar el texto tachado";
        TachadoChk.UseVisualStyleBackColor = true;
        TachadoChk.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // HtmlLbl
        //
        HtmlLbl.AutoSize = false;
        HtmlLbl.Location = new System.Drawing.Point(712, 176);
        HtmlLbl.Name = "HtmlLbl";
        HtmlLbl.Size = new System.Drawing.Size(90, 20);
        HtmlLbl.TabIndex = 7;
        HtmlLbl.Text = "HTML para leer:";
        HtmlLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // HtmlCmb
        //
        HtmlCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        HtmlCmb.Location = new System.Drawing.Point(808, 172);
        HtmlCmb.Name = "HtmlCmb";
        HtmlCmb.Size = new System.Drawing.Size(150, 28);
        HtmlCmb.TabIndex = 8;
        HtmlCmb.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // MenuAgregarBtn
        //
        MenuAgregarBtn.Location = new System.Drawing.Point(12, 208);
        MenuAgregarBtn.Name = "MenuAgregarBtn";
        MenuAgregarBtn.Size = new System.Drawing.Size(230, 32);
        MenuAgregarBtn.TabIndex = 9;
        MenuAgregarBtn.Text = "Agregar al menú contextual";
        MenuAgregarBtn.UseVisualStyleBackColor = true;
        MenuAgregarBtn.Click += new System.EventHandler(MenuAgregarBtn_Click);
        //
        // MenuQuitarBtn
        //
        MenuQuitarBtn.Location = new System.Drawing.Point(250, 208);
        MenuQuitarBtn.Name = "MenuQuitarBtn";
        MenuQuitarBtn.Size = new System.Drawing.Size(230, 32);
        MenuQuitarBtn.TabIndex = 10;
        MenuQuitarBtn.Text = "Quitar del menú contextual";
        MenuQuitarBtn.UseVisualStyleBackColor = true;
        MenuQuitarBtn.Click += new System.EventHandler(MenuQuitarBtn_Click);
        //
        // MenuClasicoBtn
        //
        MenuClasicoBtn.Location = new System.Drawing.Point(488, 208);
        MenuClasicoBtn.Name = "MenuClasicoBtn";
        MenuClasicoBtn.Size = new System.Drawing.Size(270, 32);
        MenuClasicoBtn.TabIndex = 11;
        MenuClasicoBtn.Text = "Menú clásico de Windows 11";
        MenuClasicoBtn.UseVisualStyleBackColor = true;
        MenuClasicoBtn.Visible = false;
        MenuClasicoBtn.Click += new System.EventHandler(MenuClasicoBtn_Click);
        //
        // MenuEstadoLbl
        //
        MenuEstadoLbl.AutoSize = false;
        MenuEstadoLbl.Location = new System.Drawing.Point(766, 214);
        MenuEstadoLbl.Name = "MenuEstadoLbl";
        MenuEstadoLbl.Size = new System.Drawing.Size(446, 20);
        MenuEstadoLbl.TabIndex = 12;
        MenuEstadoLbl.Text = string.Empty;
        MenuEstadoLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // StatusLbl
        //
        StatusLbl.AutoSize = false;
        StatusLbl.Location = new System.Drawing.Point(12, 252);
        StatusLbl.Name = "StatusLbl";
        StatusLbl.Size = new System.Drawing.Size(1200, 24);
        StatusLbl.TabIndex = 12;
        StatusLbl.Text = "Listo.";
        StatusLbl.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        //
        // OutputTxt
        //
        OutputTxt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        OutputTxt.Location = new System.Drawing.Point(12, 282);
        OutputTxt.Name = "OutputTxt";
        OutputTxt.ReadOnly = true;
        OutputTxt.WordWrap = false;
        OutputTxt.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
        OutputTxt.Font = new System.Drawing.Font("Consolas", 9.5F);
        OutputTxt.Size = new System.Drawing.Size(1200, 108);
        OutputTxt.TabIndex = 13;
        OutputTxt.Text = "";
        //
        // MarkdownConverterView
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        AllowDrop = true;
        Controls.Add(PandocLbl);
        Controls.Add(FormatosLbl);
        Controls.Add(DropPanel);
        Controls.Add(ElegirBtn);
        Controls.Add(DetenerBtn);
        Controls.Add(AbrirCarpetaBtn);
        Controls.Add(TachadoChk);
        Controls.Add(HtmlLbl);
        Controls.Add(HtmlCmb);
        Controls.Add(MenuAgregarBtn);
        Controls.Add(MenuQuitarBtn);
        Controls.Add(MenuClasicoBtn);
        Controls.Add(MenuEstadoLbl);
        Controls.Add(StatusLbl);
        Controls.Add(OutputTxt);
        Name = "MarkdownConverterView";
        Size = new System.Drawing.Size(1224, 400);
        Load += new System.EventHandler(MarkdownConverterView_Load);
        DropPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Label PandocLbl;
    private System.Windows.Forms.Label FormatosLbl;
    private System.Windows.Forms.Panel DropPanel;
    private System.Windows.Forms.Label DropLbl;
    private System.Windows.Forms.Button ElegirBtn;
    private System.Windows.Forms.Button DetenerBtn;
    private System.Windows.Forms.Button AbrirCarpetaBtn;
    private System.Windows.Forms.CheckBox TachadoChk;
    private System.Windows.Forms.Label HtmlLbl;
    private System.Windows.Forms.ComboBox HtmlCmb;
    private System.Windows.Forms.Button MenuAgregarBtn;
    private System.Windows.Forms.Button MenuQuitarBtn;
    private System.Windows.Forms.Button MenuClasicoBtn;
    private System.Windows.Forms.Label MenuEstadoLbl;
    private System.Windows.Forms.Label StatusLbl;
    private System.Windows.Forms.RichTextBox OutputTxt;
}
