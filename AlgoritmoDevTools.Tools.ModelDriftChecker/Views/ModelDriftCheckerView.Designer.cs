namespace AlgoritmoDevTools.Tools.ModelDriftChecker.Views;

partial class ModelDriftCheckerView
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
        RepoLbl = new System.Windows.Forms.Label();
        BaselineLbl = new System.Windows.Forms.Label();
        HeadLbl = new System.Windows.Forms.Label();
        VerificarBtn = new System.Windows.Forms.Button();
        SetBaselineBtn = new System.Windows.Forms.Button();
        MarcarMigradoBtn = new System.Windows.Forms.Button();
        StatusLbl = new System.Windows.Forms.Label();
        OutputTxt = new System.Windows.Forms.RichTextBox();
        SuspendLayout();
        //
        // RepoLbl
        //
        RepoLbl.AutoSize = false;
        RepoLbl.Location = new System.Drawing.Point(12, 12);
        RepoLbl.Name = "RepoLbl";
        RepoLbl.Size = new System.Drawing.Size(1200, 20);
        RepoLbl.TabIndex = 0;
        RepoLbl.Text = "Repo: ...";
        RepoLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // BaselineLbl
        //
        BaselineLbl.AutoSize = false;
        BaselineLbl.Location = new System.Drawing.Point(12, 36);
        BaselineLbl.Name = "BaselineLbl";
        BaselineLbl.Size = new System.Drawing.Size(1200, 20);
        BaselineLbl.TabIndex = 1;
        BaselineLbl.Text = "Baseline: —";
        BaselineLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // HeadLbl
        //
        HeadLbl.AutoSize = false;
        HeadLbl.Location = new System.Drawing.Point(12, 60);
        HeadLbl.Name = "HeadLbl";
        HeadLbl.Size = new System.Drawing.Size(1200, 20);
        HeadLbl.TabIndex = 2;
        HeadLbl.Text = "HEAD: —";
        HeadLbl.Font = new System.Drawing.Font("Segoe UI", 9F);
        //
        // VerificarBtn
        //
        VerificarBtn.Location = new System.Drawing.Point(12, 95);
        VerificarBtn.Name = "VerificarBtn";
        VerificarBtn.Size = new System.Drawing.Size(160, 32);
        VerificarBtn.TabIndex = 3;
        VerificarBtn.Text = "Verificar";
        VerificarBtn.UseVisualStyleBackColor = true;
        VerificarBtn.Click += new System.EventHandler(VerificarBtn_Click);
        //
        // SetBaselineBtn
        //
        SetBaselineBtn.Location = new System.Drawing.Point(180, 95);
        SetBaselineBtn.Name = "SetBaselineBtn";
        SetBaselineBtn.Size = new System.Drawing.Size(220, 32);
        SetBaselineBtn.TabIndex = 4;
        SetBaselineBtn.Text = "Usar HEAD como baseline";
        SetBaselineBtn.UseVisualStyleBackColor = true;
        SetBaselineBtn.Click += new System.EventHandler(SetBaselineBtn_Click);
        //
        // MarcarMigradoBtn
        //
        MarcarMigradoBtn.Location = new System.Drawing.Point(408, 95);
        MarcarMigradoBtn.Name = "MarcarMigradoBtn";
        MarcarMigradoBtn.Size = new System.Drawing.Size(250, 32);
        MarcarMigradoBtn.TabIndex = 5;
        MarcarMigradoBtn.Text = "Ya migré (mover baseline a HEAD)";
        MarcarMigradoBtn.UseVisualStyleBackColor = true;
        MarcarMigradoBtn.Click += new System.EventHandler(MarcarMigradoBtn_Click);
        //
        // StatusLbl
        //
        StatusLbl.AutoSize = false;
        StatusLbl.Location = new System.Drawing.Point(12, 140);
        StatusLbl.Name = "StatusLbl";
        StatusLbl.Size = new System.Drawing.Size(1200, 24);
        StatusLbl.TabIndex = 6;
        StatusLbl.Text = "Listo.";
        StatusLbl.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        //
        // OutputTxt
        //
        OutputTxt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        OutputTxt.Location = new System.Drawing.Point(12, 170);
        OutputTxt.Name = "OutputTxt";
        OutputTxt.ReadOnly = true;
        OutputTxt.WordWrap = false;
        OutputTxt.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
        OutputTxt.Font = new System.Drawing.Font("Consolas", 9.5F);
        OutputTxt.Size = new System.Drawing.Size(1200, 220);
        OutputTxt.TabIndex = 7;
        OutputTxt.Text = "";
        //
        // ModelDriftCheckerView
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        Controls.Add(RepoLbl);
        Controls.Add(BaselineLbl);
        Controls.Add(HeadLbl);
        Controls.Add(VerificarBtn);
        Controls.Add(SetBaselineBtn);
        Controls.Add(MarcarMigradoBtn);
        Controls.Add(StatusLbl);
        Controls.Add(OutputTxt);
        Name = "ModelDriftCheckerView";
        Size = new System.Drawing.Size(1224, 400);
        Load += new System.EventHandler(ModelDriftCheckerView_Load);
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Label RepoLbl;
    private System.Windows.Forms.Label BaselineLbl;
    private System.Windows.Forms.Label HeadLbl;
    private System.Windows.Forms.Button VerificarBtn;
    private System.Windows.Forms.Button SetBaselineBtn;
    private System.Windows.Forms.Button MarcarMigradoBtn;
    private System.Windows.Forms.Label StatusLbl;
    private System.Windows.Forms.RichTextBox OutputTxt;
}
