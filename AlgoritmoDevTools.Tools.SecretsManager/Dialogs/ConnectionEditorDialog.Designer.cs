namespace AlgoritmoDevTools.Tools.SecretsManager.Dialogs;

partial class ConnectionEditorDialog
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
        IntegratedSecurityChk = new System.Windows.Forms.CheckBox();
        ServerLbl = new System.Windows.Forms.Label();
        ServerTxt = new System.Windows.Forms.TextBox();
        UserLbl = new System.Windows.Forms.Label();
        UserTxt = new System.Windows.Forms.TextBox();
        PasswordLbl = new System.Windows.Forms.Label();
        PasswordTxt = new System.Windows.Forms.TextBox();
        TestBtn = new System.Windows.Forms.Button();
        StatusLbl = new System.Windows.Forms.Label();
        OkBtn = new System.Windows.Forms.Button();
        CancelBtn2 = new System.Windows.Forms.Button();
        SuspendLayout();
        //
        // IntegratedSecurityChk
        //
        IntegratedSecurityChk.AutoSize = true;
        IntegratedSecurityChk.Location = new System.Drawing.Point(20, 20);
        IntegratedSecurityChk.Name = "IntegratedSecurityChk";
        IntegratedSecurityChk.Size = new System.Drawing.Size(300, 24);
        IntegratedSecurityChk.TabIndex = 0;
        IntegratedSecurityChk.Text = "Autenticación Windows (Integrated Security)";
        IntegratedSecurityChk.UseVisualStyleBackColor = true;
        IntegratedSecurityChk.CheckedChanged += new System.EventHandler(IntegratedSecurityChk_CheckedChanged);
        //
        // ServerLbl
        //
        ServerLbl.AutoSize = true;
        ServerLbl.Location = new System.Drawing.Point(20, 63);
        ServerLbl.Name = "ServerLbl";
        ServerLbl.Size = new System.Drawing.Size(50, 20);
        ServerLbl.TabIndex = 1;
        ServerLbl.Text = "Server";
        //
        // ServerTxt
        //
        ServerTxt.Location = new System.Drawing.Point(130, 60);
        ServerTxt.Name = "ServerTxt";
        ServerTxt.Size = new System.Drawing.Size(300, 27);
        ServerTxt.TabIndex = 2;
        ServerTxt.TextChanged += new System.EventHandler(CredentialChanged);
        //
        // UserLbl
        //
        UserLbl.AutoSize = true;
        UserLbl.Location = new System.Drawing.Point(20, 103);
        UserLbl.Name = "UserLbl";
        UserLbl.Size = new System.Drawing.Size(55, 20);
        UserLbl.TabIndex = 3;
        UserLbl.Text = "User Id";
        //
        // UserTxt
        //
        UserTxt.Location = new System.Drawing.Point(130, 100);
        UserTxt.Name = "UserTxt";
        UserTxt.Size = new System.Drawing.Size(300, 27);
        UserTxt.TabIndex = 4;
        UserTxt.TextChanged += new System.EventHandler(CredentialChanged);
        //
        // PasswordLbl
        //
        PasswordLbl.AutoSize = true;
        PasswordLbl.Location = new System.Drawing.Point(20, 143);
        PasswordLbl.Name = "PasswordLbl";
        PasswordLbl.Size = new System.Drawing.Size(70, 20);
        PasswordLbl.TabIndex = 5;
        PasswordLbl.Text = "Password";
        //
        // PasswordTxt
        //
        PasswordTxt.Location = new System.Drawing.Point(130, 140);
        PasswordTxt.Name = "PasswordTxt";
        PasswordTxt.Size = new System.Drawing.Size(300, 27);
        PasswordTxt.TabIndex = 6;
        PasswordTxt.UseSystemPasswordChar = true;
        PasswordTxt.TextChanged += new System.EventHandler(CredentialChanged);
        //
        // TestBtn
        //
        TestBtn.Location = new System.Drawing.Point(20, 185);
        TestBtn.Name = "TestBtn";
        TestBtn.Size = new System.Drawing.Size(150, 30);
        TestBtn.TabIndex = 7;
        TestBtn.Text = "Probar conexión";
        TestBtn.UseVisualStyleBackColor = true;
        TestBtn.Click += new System.EventHandler(TestBtn_Click);
        //
        // StatusLbl
        //
        StatusLbl.AutoSize = false;
        StatusLbl.Location = new System.Drawing.Point(180, 189);
        StatusLbl.Name = "StatusLbl";
        StatusLbl.Size = new System.Drawing.Size(260, 22);
        StatusLbl.TabIndex = 8;
        StatusLbl.Text = "";
        StatusLbl.ForeColor = System.Drawing.Color.Gray;
        StatusLbl.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
        //
        // OkBtn
        //
        OkBtn.Location = new System.Drawing.Point(224, 230);
        OkBtn.Name = "OkBtn";
        OkBtn.Size = new System.Drawing.Size(100, 32);
        OkBtn.TabIndex = 9;
        OkBtn.Text = "Aceptar";
        OkBtn.UseVisualStyleBackColor = true;
        OkBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
        OkBtn.Click += new System.EventHandler(OkBtn_Click);
        //
        // CancelBtn2
        //
        CancelBtn2.Location = new System.Drawing.Point(334, 230);
        CancelBtn2.Name = "CancelBtn2";
        CancelBtn2.Size = new System.Drawing.Size(100, 32);
        CancelBtn2.TabIndex = 10;
        CancelBtn2.Text = "Cancelar";
        CancelBtn2.UseVisualStyleBackColor = true;
        CancelBtn2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        //
        // ConnectionEditorDialog
        //
        AcceptButton = OkBtn;
        CancelButton = CancelBtn2;
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(460, 278);
        Controls.Add(IntegratedSecurityChk);
        Controls.Add(ServerLbl);
        Controls.Add(ServerTxt);
        Controls.Add(UserLbl);
        Controls.Add(UserTxt);
        Controls.Add(PasswordLbl);
        Controls.Add(PasswordTxt);
        Controls.Add(TestBtn);
        Controls.Add(StatusLbl);
        Controls.Add(OkBtn);
        Controls.Add(CancelBtn2);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Name = "ConnectionEditorDialog";
        Text = "Nueva conexión";
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.CheckBox IntegratedSecurityChk;
    private System.Windows.Forms.Label ServerLbl;
    private System.Windows.Forms.TextBox ServerTxt;
    private System.Windows.Forms.Label UserLbl;
    private System.Windows.Forms.TextBox UserTxt;
    private System.Windows.Forms.Label PasswordLbl;
    private System.Windows.Forms.TextBox PasswordTxt;
    private System.Windows.Forms.Button TestBtn;
    private System.Windows.Forms.Label StatusLbl;
    private System.Windows.Forms.Button OkBtn;
    private System.Windows.Forms.Button CancelBtn2;
}
