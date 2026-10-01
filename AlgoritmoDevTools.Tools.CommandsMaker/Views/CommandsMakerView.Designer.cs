namespace AlgoritmoDevTools.Tools.CommandsMaker.Views;

partial class CommandsMakerView
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
        rtbText = new RichTextBox();
        bAdd = new Button();
        bRemove = new Button();
        bUpdate = new Button();
        label1 = new Label();
        addDomain = new Button();
        removeDomain = new Button();
        cmbDominios = new ComboBox();
        migrationName = new TextBox();
        label2 = new Label();
        checkBox1 = new CheckBox();
        MenuEstadoLbl = new Label();
        MenuAgregarBtn = new Button();
        MenuQuitarBtn = new Button();
        SuspendLayout();
        // 
        // rtbText
        // 
        rtbText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        rtbText.Location = new Point(14, 105);
        rtbText.Margin = new Padding(3, 4, 3, 4);
        rtbText.Name = "rtbText";
        rtbText.Size = new Size(1165, 243);
        rtbText.TabIndex = 9;
        rtbText.Text = "";
        // 
        // bAdd
        // 
        bAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bAdd.Location = new Point(492, 50);
        bAdd.Margin = new Padding(3, 4, 3, 4);
        bAdd.Name = "bAdd";
        bAdd.Size = new Size(80, 31);
        bAdd.TabIndex = 5;
        bAdd.Text = "Add";
        bAdd.UseVisualStyleBackColor = true;
        bAdd.Click += bAdd_Click;
        // 
        // bRemove
        // 
        bRemove.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bRemove.Location = new Point(578, 50);
        bRemove.Margin = new Padding(3, 4, 3, 4);
        bRemove.Name = "bRemove";
        bRemove.Size = new Size(80, 31);
        bRemove.TabIndex = 6;
        bRemove.Text = "Remove";
        bRemove.UseVisualStyleBackColor = true;
        bRemove.Click += bRemove_Click;
        // 
        // bUpdate
        // 
        bUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bUpdate.Location = new Point(664, 50);
        bUpdate.Margin = new Padding(3, 4, 3, 4);
        bUpdate.Name = "bUpdate";
        bUpdate.Size = new Size(80, 31);
        bUpdate.TabIndex = 7;
        bUpdate.Text = "UpdateD";
        bUpdate.UseVisualStyleBackColor = true;
        bUpdate.Click += bUpdate_Click;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(22, 19);
        label1.Name = "label1";
        label1.Size = new Size(73, 20);
        label1.TabIndex = 0;
        label1.Text = "Dominios";
        // 
        // addDomain
        // 
        addDomain.Location = new Point(361, 13);
        addDomain.Margin = new Padding(3, 4, 3, 4);
        addDomain.Name = "addDomain";
        addDomain.Size = new Size(31, 31);
        addDomain.TabIndex = 2;
        addDomain.Text = "+";
        addDomain.UseVisualStyleBackColor = true;
        addDomain.Click += addDomain_Click;
        // 
        // removeDomain
        // 
        removeDomain.Location = new Point(398, 13);
        removeDomain.Margin = new Padding(3, 4, 3, 4);
        removeDomain.Name = "removeDomain";
        removeDomain.Size = new Size(31, 31);
        removeDomain.TabIndex = 3;
        removeDomain.Text = "-";
        removeDomain.UseVisualStyleBackColor = true;
        removeDomain.Click += removeDomain_Click;
        // 
        // cmbDominios
        // 
        cmbDominios.FormattingEnabled = true;
        cmbDominios.Location = new Point(101, 15);
        cmbDominios.Margin = new Padding(3, 4, 3, 4);
        cmbDominios.Name = "cmbDominios";
        cmbDominios.Size = new Size(254, 28);
        cmbDominios.TabIndex = 1;
        cmbDominios.SelectedIndexChanged += cmbDominios_SelectedIndexChanged;
        cmbDominios.KeyDown += cmbDominios_KeyDown;
        // 
        // migrationName
        // 
        migrationName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        migrationName.Location = new Point(101, 52);
        migrationName.Margin = new Padding(3, 4, 3, 4);
        migrationName.Name = "migrationName";
        migrationName.Size = new Size(385, 27);
        migrationName.TabIndex = 5;
        // 
        // label2
        // 
        label2.Location = new Point(5, 46);
        label2.Name = "label2";
        label2.Size = new Size(90, 40);
        label2.TabIndex = 4;
        label2.Text = "Nombre Migracion";
        // 
        // checkBox1
        // 
        checkBox1.AutoSize = true;
        checkBox1.Checked = true;
        checkBox1.CheckState = CheckState.Checked;
        checkBox1.Location = new Point(445, 18);
        checkBox1.Margin = new Padding(3, 4, 3, 4);
        checkBox1.Name = "checkBox1";
        checkBox1.Size = new Size(299, 24);
        checkBox1.TabIndex = 8;
        checkBox1.Text = "Incluir dominio en nombre de migracion";
        checkBox1.UseVisualStyleBackColor = true;
        checkBox1.CheckedChanged += checkBox1_CheckedChanged;
        // 
        // CommandsMakerView
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        //
        // MenuEstadoLbl
        //
        MenuEstadoLbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        MenuEstadoLbl.AutoSize = false;
        MenuEstadoLbl.Location = new Point(760, 18);
        MenuEstadoLbl.Name = "MenuEstadoLbl";
        MenuEstadoLbl.Size = new Size(346, 24);
        MenuEstadoLbl.TabIndex = 12;
        MenuEstadoLbl.Text = "";
        //
        // MenuAgregarBtn
        //
        MenuAgregarBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        MenuAgregarBtn.Location = new Point(760, 50);
        MenuAgregarBtn.Name = "MenuAgregarBtn";
        MenuAgregarBtn.Size = new Size(170, 31);
        MenuAgregarBtn.TabIndex = 13;
        MenuAgregarBtn.Text = "Agregar al menú";
        MenuAgregarBtn.UseVisualStyleBackColor = true;
        MenuAgregarBtn.Click += MenuAgregarBtn_Click;
        //
        // MenuQuitarBtn
        //
        MenuQuitarBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        MenuQuitarBtn.Location = new Point(936, 50);
        MenuQuitarBtn.Name = "MenuQuitarBtn";
        MenuQuitarBtn.Size = new Size(170, 31);
        MenuQuitarBtn.TabIndex = 14;
        MenuQuitarBtn.Text = "Quitar del menú";
        MenuQuitarBtn.UseVisualStyleBackColor = true;
        MenuQuitarBtn.Click += MenuQuitarBtn_Click;
        //
        Controls.Add(MenuEstadoLbl);
        Controls.Add(MenuAgregarBtn);
        Controls.Add(MenuQuitarBtn);
        Controls.Add(rtbText);
        Controls.Add(checkBox1);
        Controls.Add(bUpdate);
        Controls.Add(bRemove);
        Controls.Add(bAdd);
        Controls.Add(migrationName);
        Controls.Add(label2);
        Controls.Add(removeDomain);
        Controls.Add(addDomain);
        Controls.Add(cmbDominios);
        Controls.Add(label1);
        Margin = new Padding(3, 4, 3, 4);
        Name = "CommandsMakerView";
        Size = new Size(1193, 358);
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.RichTextBox rtbText;
    private System.Windows.Forms.Button bAdd;
    private System.Windows.Forms.Button bRemove;
    private System.Windows.Forms.Button bUpdate;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button addDomain;
    private System.Windows.Forms.Button removeDomain;
    private System.Windows.Forms.ComboBox cmbDominios;
    private System.Windows.Forms.TextBox migrationName;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.CheckBox checkBox1;
    private System.Windows.Forms.Label MenuEstadoLbl;
    private System.Windows.Forms.Button MenuAgregarBtn;
    private System.Windows.Forms.Button MenuQuitarBtn;
}
