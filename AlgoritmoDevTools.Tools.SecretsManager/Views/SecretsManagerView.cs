using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.SecretsManager.Dialogs;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.SecretsManager.Views;

public partial class SecretsManagerView : UserControl
{
    private static readonly string[] BoldKeys = { "Server=", "Database=", "User Id=", "Password=" };

    private readonly SecretService _service = SecretService.Shared;
    private readonly SavedConnectionsRepository _savedConnections;
    private bool _initialized;
    private bool _suppressSavedChanged;

    public SecretsManagerView(SavedConnectionsRepository savedConnections)
    {
        _savedConnections = savedConnections;
        InitializeComponent();
        SetupTooltips();
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 10_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(SavedConnectionsCmb, "Conexiones SQL guardadas localmente. Compartidas con Schema Change Detector.");
        tips.SetToolTip(DataBaseCmb, "Base de datos a usar para 'Modificar Secreto'. Se puebla cuando elegís una conexión.");
        tips.SetToolTip(NuevaConexionBtn, "Crea una conexión nueva (server, credenciales o Windows Auth). Valida el login antes de guardar.");
        tips.SetToolTip(ModificarConexionBtn, "Edita la conexión seleccionada. Re-valida el login antes de guardar.");
        tips.SetToolTip(EliminarConexionBtn, "Elimina la conexión seleccionada de la lista local.");
        tips.SetToolTip(ListarSecretosBtn, "Re-ejecuta 'dotnet user-secrets list' y muestra los secretos actuales del proyecto.");
        tips.SetToolTip(RestaurarSecretosBtn, "Restaura los secretos desde secrets/SoftCerealCore.ConnectionString.json.");
        tips.SetToolTip(ModificarSecretoBtn, "Actualiza los user-secrets Development y DAPR con la conexión + base de datos seleccionadas.");
        tips.SetToolTip(VisorTxt, "Listado actual de user-secrets. Valores variables en negrita.");
    }

    private async void SecretsManagerView_Load(object? sender, EventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        RefreshSavedConnections(selectNone: true);

        SetVisorPlain("Cargando secretos...");
        SetBusy(true);

        try
        {
            var raw = await Task.Run(() => _service.RefreshSecrets());
            SetVisorWithHighlight(raw);
        }
        catch (Exception ex)
        {
            SetVisorPlain(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetVisorPlain(string text)
    {
        VisorTxt.Clear();
        VisorTxt.Text = text;
        VisorTxt.SelectAll();
        VisorTxt.SelectionFont = new Font(VisorTxt.Font, FontStyle.Regular);
        VisorTxt.Select(0, 0);
    }

    private void SetVisorWithHighlight(string text)
    {
        VisorTxt.Clear();
        VisorTxt.Text = text;

        var content = VisorTxt.Text;
        var regularFont = new Font(VisorTxt.Font, FontStyle.Regular);
        var boldFont = new Font(VisorTxt.Font, FontStyle.Bold);

        VisorTxt.SelectAll();
        VisorTxt.SelectionFont = regularFont;

        foreach (var key in BoldKeys)
        {
            int searchFrom = 0;
            while (true)
            {
                int keyIdx = content.IndexOf(key, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (keyIdx < 0) break;

                int valueStart = keyIdx + key.Length;
                int valueEnd = content.IndexOfAny(new[] { ';', '\r', '\n' }, valueStart);
                if (valueEnd < 0) valueEnd = content.Length;

                int length = valueEnd - valueStart;
                if (length > 0)
                {
                    VisorTxt.Select(valueStart, length);
                    VisorTxt.SelectionFont = boldFont;
                }
                searchFrom = valueEnd + 1;
            }
        }

        VisorTxt.Select(0, 0);
    }

    private void RefreshSavedConnections(bool selectNone)
    {
        _suppressSavedChanged = true;
        try
        {
            SavedConnectionsCmb.DataSource = null;
            SavedConnectionsCmb.DataSource = _savedConnections.GetAll();
            if (selectNone) SavedConnectionsCmb.SelectedIndex = -1;
        }
        finally
        {
            _suppressSavedChanged = false;
        }

        DataBaseCmb.DataSource = null;
        DataBaseCmb.Enabled = false;
    }

    private async void SavedConnectionsCmb_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressSavedChanged) return;

        if (SavedConnectionsCmb.SelectedItem is not SavedConnection sc)
        {
            DataBaseCmb.DataSource = null;
            DataBaseCmb.Enabled = false;
            return;
        }

        DataBaseCmb.Enabled = false;
        DataBaseCmb.DataSource = new List<string> { "Cargando..." };

        var result = await Task.Run(() => SQLService.TryGetDatabases(
            new SQLService.ConnectionData(sc.Server, sc.User, sc.Password, sc.UseIntegratedSecurity)));

        if (result.IsSuccess && result.Databases.Count > 0)
        {
            DataBaseCmb.DataSource = result.Databases.ToList();
            if (result.Databases.Contains(sc.DataBase))
                DataBaseCmb.SelectedItem = sc.DataBase;
            DataBaseCmb.Enabled = true;
        }
        else
        {
            DataBaseCmb.DataSource = new List<string> { sc.DataBase };
            DataBaseCmb.SelectedIndex = 0;
            DataBaseCmb.Enabled = false;
            if (!result.IsSuccess)
                SetVisorPlain($"No se pudo conectar para listar bases de datos: {result.Error}");
        }
    }

    private void NuevaConexionBtn_Click(object? sender, EventArgs e)
    {
        using var dialog = new ConnectionEditorDialog(_savedConnections);
        if (dialog.ShowDialog(this.FindForm()) != DialogResult.OK) return;

        _savedConnections.Add(dialog.Result);
        RefreshSavedConnections(selectNone: true);
    }

    private void ModificarConexionBtn_Click(object? sender, EventArgs e)
    {
        if (SavedConnectionsCmb.SelectedItem is not SavedConnection sc)
        {
            MessageBox.Show("Seleccioná una conexión para modificar.",
                "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new ConnectionEditorDialog(_savedConnections, sc);
        if (dialog.ShowDialog(this.FindForm()) != DialogResult.OK) return;

        _savedConnections.Update(sc.Id, dialog.Result);
        RefreshSavedConnections(selectNone: true);
    }

    private void EliminarConexionBtn_Click(object? sender, EventArgs e)
    {
        if (SavedConnectionsCmb.SelectedItem is not SavedConnection sc)
        {
            MessageBox.Show("Seleccioná una conexión para eliminar.",
                "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show($"¿Eliminar conexión '{sc}'?",
            "Secret Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _savedConnections.Delete(sc.Id);
        RefreshSavedConnections(selectNone: true);
    }

    private async void ListarSecretosBtn_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            var raw = await Task.Run(() => _service.RefreshSecrets());
            SetVisorWithHighlight(raw);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void RestaurarSecretosBtn_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await Task.Run(() => _service.RestoreSecretsFromFile());
            SetVisorWithHighlight(_service.LastRawOutput);
            MessageBox.Show("Secretos Restaurados", "Secret Manager");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ModificarSecretoBtn_Click(object? sender, EventArgs e)
    {
        if (SavedConnectionsCmb.SelectedItem is not SavedConnection sc)
        {
            MessageBox.Show("Seleccioná una conexión para modificar el secreto.",
                "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedDb = DataBaseCmb.SelectedItem as string ?? sc.DataBase;
        var cn = new SQLService.ConnectionData(
            server: sc.Server,
            user: sc.User,
            password: sc.Password,
            dataBase: selectedDb,
            useIntegratedSecurity: sc.UseIntegratedSecurity);

        SetBusy(true);
        try
        {
            await Task.Run(() => _service.SetSecrets(cn));
            SetVisorWithHighlight(_service.LastRawOutput);
            MessageBox.Show("Secretos Modificados", "Secret Manager");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        ListarSecretosBtn.Enabled = !busy;
        RestaurarSecretosBtn.Enabled = !busy;
        ModificarSecretoBtn.Enabled = !busy;
        NuevaConexionBtn.Enabled = !busy;
        ModificarConexionBtn.Enabled = !busy;
        EliminarConexionBtn.Enabled = !busy;
        SavedConnectionsCmb.Enabled = !busy;
    }
}
