using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.CommandsMaker.Views;

public partial class CommandsMakerView : UserControl
{
    private const string DOMINIO_TOKEN = "*DOMINIO*";
    private const string MIGRATION_NAME = "*MIGRATION_NAME*";
    private const string CONNECTION_STRING = "*CONNECTION_STRING*";

    private const string PROJECT = "Algoritmo." + DOMINIO_TOKEN + ".Infrastructure";
    // CS usada por PM Console: Server y Database del secreto Development + Integrated Security (Windows user con permisos SA).
    private const string FALLBACK_CONNECTION_STRING =
        "Server=localhost,1433;Database=Algoritmo;Integrated Security = true; MultipleActiveResultSets=True";

    private const string COMMON_COMMAND = "-Context " + DOMINIO_TOKEN + "DbContext -Project " + PROJECT + " -StartupProject " + PROJECT;
    private const string ARGS = " -Args '--Connection \"" + CONNECTION_STRING + "\"'";

    private const string AddMigrationTemplate = "add-migration " + MIGRATION_NAME + " " + COMMON_COMMAND + ARGS;
    private const string RmvMigrationTemplate = "remove-migration -force " + COMMON_COMMAND + ARGS;
    private const string UpdateDbTemplate = "update-database " + COMMON_COMMAND + " -Connection \"" + CONNECTION_STRING + "\"" + ARGS;

    private readonly DomainRepository _repository;
    private readonly SecretService _secretService = SecretService.Shared;

    public CommandsMakerView(DomainRepository repository)
    {
        _repository = repository;
        InitializeComponent();
        cmbDominios.DataSource = _repository.GetAll();
        migrationName.Text = ChangeMigrationName(true, "Inicial");
        SetupTooltips();
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 10_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(cmbDominios, "Dominio actual. Se usa para armar los comandos. Compartido con Schema Change Detector.");
        tips.SetToolTip(addDomain, "Agrega un dominio nuevo a la lista.");
        tips.SetToolTip(removeDomain, "Elimina el dominio seleccionado.");
        tips.SetToolTip(migrationName, "Nombre de la migraci�n (editable). Se sustituye en el comando add-migration.");
        tips.SetToolTip(checkBox1, "Si est� activo, antepone '[Dominio].' al nombre de la migraci�n.");
        tips.SetToolTip(bAdd, "Copia al clipboard el comando 'add-migration' con el nombre y la connection string actual, listo para pegarlo en PM Console.");
        tips.SetToolTip(bRemove, "Copia al clipboard el comando 'remove-migration -force', listo para pegarlo en PM Console.");
        tips.SetToolTip(bUpdate, "Copia al clipboard el comando 'update-database' con la connection string del secreto Development.");
        tips.SetToolTip(rtbText, "�ltimo comando generado. Ya est� copiado en el clipboard.");
    }

    private string ResolveConnectionString()
    {
        // Tomamos Server y Database del secreto Development. La autenticación la forzamos
        // a Integrated Security porque PM Console corre con el user de Windows (que tiene SA en dev).
        var dev = _secretService.GetConnectionString(Constantes.SecretKeys.Development);
        if (string.IsNullOrEmpty(dev)) return FALLBACK_CONNECTION_STRING;

        var parts = ConnectionStringParser.Parse(dev);
        var server = parts.GetValueOrDefault("Server");
        var database = parts.GetValueOrDefault("Database");
        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database)) return FALLBACK_CONNECTION_STRING;

        return $"Server={server};Database={database};Integrated Security = true; MultipleActiveResultSets=True";
    }

    private void CastCommand(string template)
    {
        var command = template
            .Replace(CONNECTION_STRING, ResolveConnectionString())
            .Replace(DOMINIO_TOKEN, cmbDominios.Text);

        Clipboard.SetData(DataFormats.Text, command);
        rtbText.Text = command;
    }

    private void bAdd_Click(object? sender, EventArgs e)
        => CastCommand(AddMigrationTemplate.Replace(MIGRATION_NAME, migrationName.Text));

    private void bRemove_Click(object? sender, EventArgs e)
        => CastCommand(RmvMigrationTemplate);

    private void bUpdate_Click(object? sender, EventArgs e)
        => CastCommand(UpdateDbTemplate);

    private void addDomain_Click(object? sender, EventArgs e)
    {
        var input = InputDialog.Show("Ingrese el nombre de un dominio:", "Add Domain", owner: this.FindForm());
        if (!string.IsNullOrEmpty(input))
        {
            _repository.Add(input);
            cmbDominios.DataSource = _repository.GetAll();
        }
    }

    private void removeDomain_Click(object? sender, EventArgs e)
    {
        var dominio = cmbDominios.Text;
        if (string.IsNullOrWhiteSpace(dominio))
        {
            MessageBox.Show("Seleccioná un dominio para eliminar.",
                "Commands Maker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show($"¿Eliminar dominio '{dominio}'?",
            "Commands Maker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _repository.Remove(dominio);
        cmbDominios.DataSource = _repository.GetAll();
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
