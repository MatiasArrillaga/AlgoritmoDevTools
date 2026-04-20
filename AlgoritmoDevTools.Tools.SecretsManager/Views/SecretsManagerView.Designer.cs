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
        ListarSecretosBtn = new System.Windows.Forms.Button();
        VisorTxt = new System.Windows.Forms.TextBox();
        RestaurarSecretosBtn = new System.Windows.Forms.Button();
        ModificarSecretoBtn = new System.Windows.Forms.Button();
        DataBaseCmb = new System.Windows.Forms.ComboBox();
        label5 = new System.Windows.Forms.Label();
        label4 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        PasswordTxt = new System.Windows.Forms.TextBox();
        UserTxt = new System.Windows.Forms.TextBox();
        ServerNameTxt = new System.Windows.Forms.TextBox();
        VisualizadorTxt = new System.Windows.Forms.TextBox();
        RefreshBtn = new System.Windows.Forms.Button();
        SuspendLayout();
        //
        // ListarSecretosBtn
        //
        ListarSecretosBtn.Location = new System.Drawing.Point(10, 162);
        ListarSecretosBtn.Name = "ListarSecretosBtn";
        ListarSecretosBtn.Size = new System.Drawing.Size(139, 32);
        ListarSecretosBtn.TabIndex = 0;
        ListarSecretosBtn.Text = "Listar Secretos";
        ListarSecretosBtn.UseVisualStyleBackColor = true;
        ListarSecretosBtn.Click += new System.EventHandler(ListarSecretosBtn_Click);
        //
        // VisorTxt
        //
        VisorTxt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        VisorTxt.Location = new System.Drawing.Point(10, 200);
        VisorTxt.Multiline = true;
        VisorTxt.Name = "VisorTxt";
        VisorTxt.Size = new System.Drawing.Size(1132, 168);
        VisorTxt.TabIndex = 1;
        //
        // RestaurarSecretosBtn
        //
        RestaurarSecretosBtn.Location = new System.Drawing.Point(185, 162);
        RestaurarSecretosBtn.Name = "RestaurarSecretosBtn";
        RestaurarSecretosBtn.Size = new System.Drawing.Size(159, 32);
        RestaurarSecretosBtn.TabIndex = 2;
        RestaurarSecretosBtn.Text = "Restaurar Secretos";
        RestaurarSecretosBtn.UseVisualStyleBackColor = true;
        RestaurarSecretosBtn.Click += new System.EventHandler(RestaurarSecretosBtn_Click);
        //
        // ModificarSecretoBtn
        //
        ModificarSecretoBtn.Location = new System.Drawing.Point(374, 164);
        ModificarSecretoBtn.Name = "ModificarSecretoBtn";
        ModificarSecretoBtn.Size = new System.Drawing.Size(152, 28);
        ModificarSecretoBtn.TabIndex = 3;
        ModificarSecretoBtn.Text = "Modificar Secreto";
        ModificarSecretoBtn.UseVisualStyleBackColor = true;
        ModificarSecretoBtn.Click += new System.EventHandler(ModificarSecretoBtn_Click);
        //
        // DataBaseCmb
        //
        DataBaseCmb.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        DataBaseCmb.FormattingEnabled = true;
        DataBaseCmb.Location = new System.Drawing.Point(865, 20);
        DataBaseCmb.Name = "DataBaseCmb";
        DataBaseCmb.Size = new System.Drawing.Size(185, 28);
        DataBaseCmb.TabIndex = 4;
        DataBaseCmb.SelectedIndexChanged += new System.EventHandler(DataBaseCmb_SelectedIndexChanged);
        //
        // label5
        //
        label5.AutoSize = true;
        label5.Location = new System.Drawing.Point(533, 23);
        label5.Name = "label5";
        label5.Size = new System.Drawing.Size(70, 20);
        label5.TabIndex = 5;
        label5.Text = "Password";
        //
        // label4
        //
        label4.AutoSize = true;
        label4.Location = new System.Drawing.Point(280, 23);
        label4.Name = "label4";
        label4.Size = new System.Drawing.Size(55, 20);
        label4.TabIndex = 6;
        label4.Text = "User Id";
        //
        // label3
        //
        label3.AutoSize = true;
        label3.Location = new System.Drawing.Point(785, 23);
        label3.Name = "label3";
        label3.Size = new System.Drawing.Size(72, 20);
        label3.TabIndex = 7;
        label3.Text = "DataBase";
        //
        // label2
        //
        label2.AutoSize = true;
        label2.Location = new System.Drawing.Point(12, 23);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(50, 20);
        label2.TabIndex = 8;
        label2.Text = "Server";
        //
        // PasswordTxt
        //
        PasswordTxt.Location = new System.Drawing.Point(609, 20);
        PasswordTxt.Name = "PasswordTxt";
        PasswordTxt.Size = new System.Drawing.Size(161, 27);
        PasswordTxt.TabIndex = 9;
        //
        // UserTxt
        //
        UserTxt.Location = new System.Drawing.Point(341, 20);
        UserTxt.Name = "UserTxt";
        UserTxt.Size = new System.Drawing.Size(161, 27);
        UserTxt.TabIndex = 10;
        //
        // ServerNameTxt
        //
        ServerNameTxt.Location = new System.Drawing.Point(68, 20);
        ServerNameTxt.Name = "ServerNameTxt";
        ServerNameTxt.Size = new System.Drawing.Size(192, 27);
        ServerNameTxt.TabIndex = 11;
        //
        // VisualizadorTxt
        //
        VisualizadorTxt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        VisualizadorTxt.Location = new System.Drawing.Point(12, 53);
        VisualizadorTxt.Multiline = true;
        VisualizadorTxt.Name = "VisualizadorTxt";
        VisualizadorTxt.Size = new System.Drawing.Size(1132, 90);
        VisualizadorTxt.TabIndex = 12;
        //
        // RefreshBtn
        //
        RefreshBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        RefreshBtn.Location = new System.Drawing.Point(1056, 17);
        RefreshBtn.Name = "RefreshBtn";
        RefreshBtn.Size = new System.Drawing.Size(88, 31);
        RefreshBtn.TabIndex = 13;
        RefreshBtn.Text = "Refresh";
        RefreshBtn.UseVisualStyleBackColor = true;
        RefreshBtn.Click += new System.EventHandler(RefreshBtn_Click);
        //
        // SecretsManagerView
        //
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        Controls.Add(RefreshBtn);
        Controls.Add(DataBaseCmb);
        Controls.Add(label5);
        Controls.Add(label4);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(PasswordTxt);
        Controls.Add(UserTxt);
        Controls.Add(ServerNameTxt);
        Controls.Add(VisualizadorTxt);
        Controls.Add(ModificarSecretoBtn);
        Controls.Add(RestaurarSecretosBtn);
        Controls.Add(VisorTxt);
        Controls.Add(ListarSecretosBtn);
        Name = "SecretsManagerView";
        Size = new System.Drawing.Size(1154, 410);
        Load += new System.EventHandler(SecretsManagerView_Load);
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button ListarSecretosBtn;
    private System.Windows.Forms.TextBox VisorTxt;
    private System.Windows.Forms.Button RestaurarSecretosBtn;
    private System.Windows.Forms.Button ModificarSecretoBtn;
    private System.Windows.Forms.ComboBox DataBaseCmb;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox PasswordTxt;
    private System.Windows.Forms.TextBox UserTxt;
    private System.Windows.Forms.TextBox ServerNameTxt;
    private System.Windows.Forms.TextBox VisualizadorTxt;
    private System.Windows.Forms.Button RefreshBtn;
}
