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
        this.rtbText = new System.Windows.Forms.RichTextBox();
        this.bAdd = new System.Windows.Forms.Button();
        this.bRemove = new System.Windows.Forms.Button();
        this.bUpdate = new System.Windows.Forms.Button();
        this.label1 = new System.Windows.Forms.Label();
        this.addDomain = new System.Windows.Forms.Button();
        this.cmbDominios = new System.Windows.Forms.ComboBox();
        this.migrationName = new System.Windows.Forms.TextBox();
        this.label2 = new System.Windows.Forms.Label();
        this.checkBox1 = new System.Windows.Forms.CheckBox();
        this.SuspendLayout();
        //
        // rtbText
        //
        this.rtbText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
        | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.rtbText.Location = new System.Drawing.Point(14, 168);
        this.rtbText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.rtbText.Name = "rtbText";
        this.rtbText.Size = new System.Drawing.Size(367, 100);
        this.rtbText.TabIndex = 1;
        this.rtbText.Text = "";
        //
        // bAdd
        //
        this.bAdd.Location = new System.Drawing.Point(132, 128);
        this.bAdd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.bAdd.Name = "bAdd";
        this.bAdd.Size = new System.Drawing.Size(80, 31);
        this.bAdd.TabIndex = 2;
        this.bAdd.Text = "Add";
        this.bAdd.UseVisualStyleBackColor = true;
        this.bAdd.Click += new System.EventHandler(this.bAdd_Click);
        //
        // bRemove
        //
        this.bRemove.Location = new System.Drawing.Point(218, 128);
        this.bRemove.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.bRemove.Name = "bRemove";
        this.bRemove.Size = new System.Drawing.Size(80, 31);
        this.bRemove.TabIndex = 3;
        this.bRemove.Text = "Remove";
        this.bRemove.UseVisualStyleBackColor = true;
        this.bRemove.Click += new System.EventHandler(this.bRemove_Click);
        //
        // bUpdate
        //
        this.bUpdate.Location = new System.Drawing.Point(304, 128);
        this.bUpdate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.bUpdate.Name = "bUpdate";
        this.bUpdate.Size = new System.Drawing.Size(80, 31);
        this.bUpdate.TabIndex = 4;
        this.bUpdate.Text = "UpdateDb";
        this.bUpdate.UseVisualStyleBackColor = true;
        this.bUpdate.Click += new System.EventHandler(this.bUpdate_Click);
        //
        // label1
        //
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(14, 15);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(73, 20);
        this.label1.TabIndex = 5;
        this.label1.Text = "Dominios";
        //
        // addDomain
        //
        this.addDomain.Location = new System.Drawing.Point(353, 9);
        this.addDomain.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.addDomain.Name = "addDomain";
        this.addDomain.Size = new System.Drawing.Size(31, 31);
        this.addDomain.TabIndex = 6;
        this.addDomain.Text = "+";
        this.addDomain.UseVisualStyleBackColor = true;
        this.addDomain.Click += new System.EventHandler(this.addDomain_Click);
        //
        // cmbDominios
        //
        this.cmbDominios.FormattingEnabled = true;
        this.cmbDominios.Location = new System.Drawing.Point(93, 11);
        this.cmbDominios.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.cmbDominios.Name = "cmbDominios";
        this.cmbDominios.Size = new System.Drawing.Size(254, 28);
        this.cmbDominios.TabIndex = 7;
        this.cmbDominios.SelectedIndexChanged += new System.EventHandler(this.cmbDominios_SelectedIndexChanged);
        this.cmbDominios.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cmbDominios_KeyDown);
        //
        // migrationName
        //
        this.migrationName.Location = new System.Drawing.Point(94, 49);
        this.migrationName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.migrationName.Name = "migrationName";
        this.migrationName.Size = new System.Drawing.Size(290, 27);
        this.migrationName.TabIndex = 8;
        //
        // label2
        //
        this.label2.Location = new System.Drawing.Point(15, 45);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(82, 47);
        this.label2.TabIndex = 9;
        this.label2.Text = "Nombre Migracion";
        //
        // checkBox1
        //
        this.checkBox1.AutoSize = true;
        this.checkBox1.Checked = true;
        this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
        this.checkBox1.Location = new System.Drawing.Point(93, 96);
        this.checkBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.checkBox1.Name = "checkBox1";
        this.checkBox1.Size = new System.Drawing.Size(299, 24);
        this.checkBox1.TabIndex = 10;
        this.checkBox1.Text = "Incluir dominio en nombre de migracion";
        this.checkBox1.UseVisualStyleBackColor = true;
        this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
        //
        // CommandsMakerView
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.checkBox1);
        this.Controls.Add(this.migrationName);
        this.Controls.Add(this.cmbDominios);
        this.Controls.Add(this.addDomain);
        this.Controls.Add(this.label2);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.bUpdate);
        this.Controls.Add(this.bRemove);
        this.Controls.Add(this.bAdd);
        this.Controls.Add(this.rtbText);
        this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.Name = "CommandsMakerView";
        this.Size = new System.Drawing.Size(400, 280);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.RichTextBox rtbText;
    private System.Windows.Forms.Button bAdd;
    private System.Windows.Forms.Button bRemove;
    private System.Windows.Forms.Button bUpdate;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button addDomain;
    private System.Windows.Forms.ComboBox cmbDominios;
    private System.Windows.Forms.TextBox migrationName;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.CheckBox checkBox1;
}
