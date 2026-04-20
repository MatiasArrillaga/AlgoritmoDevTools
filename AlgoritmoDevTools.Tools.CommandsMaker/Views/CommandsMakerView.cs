using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.CommandsMaker.Views;

public partial class CommandsMakerView : UserControl
{
    private const string DOMINIO = "*DOMINIO*";
    private const string MIGRATION_NAME = "*MIGRATION_NAME*";

    private const string PROJECT = "Algoritmo." + DOMINIO + ".Infrastructure";
    private const string DATABASE = "Server=localhost,1433;Database=Algoritmo";
    private const string CONNECTION_STRING = DATABASE + ";Integrated Security = true; MultipleActiveResultSets=True";

    private const string COMMON_COMMAND = "-Context " + DOMINIO + "DbContext -Project " + PROJECT + " -StartupProject " + PROJECT;
    private const string ARGS = " -Args '--Connection \"" + CONNECTION_STRING + "\"'";

    private const string AddMigration = "add-migration " + MIGRATION_NAME + " " + COMMON_COMMAND + ARGS;
    private const string RmvMigration = "remove-migration -force " + COMMON_COMMAND + ARGS;
    private const string UpdateDb = "update-database " + COMMON_COMMAND + " -Connection \"" + CONNECTION_STRING + "\"" + ARGS;

    private readonly DomainRepository _repository;

    public CommandsMakerView(DomainRepository repository)
    {
        _repository = repository;
        InitializeComponent();
        cmbDominios.DataSource = _repository.GetAll();
        migrationName.Text = ChangeMigrationName(true, "Inicial");
    }

    private void CastCommand(string command)
    {
        Clipboard.SetData(DataFormats.Text, command.Replace(DOMINIO, cmbDominios.Text));
        rtbText.Text = Clipboard.GetText();
    }

    private void bAdd_Click(object? sender, EventArgs e)
        => CastCommand(AddMigration.Replace(MIGRATION_NAME, migrationName.Text));

    private void bRemove_Click(object? sender, EventArgs e)
        => CastCommand(RmvMigration);

    private void bUpdate_Click(object? sender, EventArgs e)
        => CastCommand(UpdateDb);

    private void addDomain_Click(object? sender, EventArgs e)
    {
        var input = InputDialog.Show("Ingrese el nombre de un dominio:", "Add Domain", owner: this.FindForm());
        if (!string.IsNullOrEmpty(input))
        {
            _repository.Add(input);
            cmbDominios.DataSource = _repository.GetAll();
        }
    }

    private void cmbDominios_SelectedIndexChanged(object? sender, EventArgs e)
        => migrationName.Text = ChangeMigrationName(checkBox1.Checked, migrationName.Text);

    private void checkBox1_CheckedChanged(object? sender, EventArgs e)
        => migrationName.Text = ChangeMigrationName(checkBox1.Checked, migrationName.Text);

    private string ChangeMigrationName(bool addDomines, string mName)
    {
        var domainDescription = addDomines ? $"[{cmbDominios.Text}]." : string.Empty;

        return mName.Contains('[', StringComparison.CurrentCulture)
            ? mName.Replace(mName.Substring(mName.IndexOf('['), mName.IndexOf('.') + 1), domainDescription)
            : domainDescription + mName;
    }

    private void cmbDominios_KeyDown(object? sender, KeyEventArgs e)
    {
        var dominio = cmbDominios.Text;
        switch (e.KeyCode)
        {
            case Keys.Delete:
                _repository.Remove(dominio);
                cmbDominios.DataSource = _repository.GetAll();
                MessageBox.Show($"Dominio '{dominio}' eliminado");
                break;

            case Keys.Return:
                if (!string.IsNullOrEmpty(dominio))
                {
                    _repository.Add(dominio);
                    cmbDominios.DataSource = _repository.GetAll();
                    MessageBox.Show($"Dominio '{dominio}' agregado");
                }
                break;
        }
    }
}
