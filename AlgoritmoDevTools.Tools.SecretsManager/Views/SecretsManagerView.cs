using AlgoritmoDevTools.Integrations.SoftCerealCore;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.SecretsManager.Views;

public partial class SecretsManagerView : UserControl
{
    private readonly SecretService _service = new();
    private bool _initialized;

    public SecretsManagerView()
    {
        InitializeComponent();
    }

    private async void SecretsManagerView_Load(object? sender, EventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        VisorTxt.Text = "Cargando secretos...";
        SetBusy(true);

        try
        {
            var raw = await Task.Run(() => _service.RefreshSecrets());
            VisorTxt.Text = raw;
            await PrevisualizarDatosAsync();
        }
        catch (Exception ex)
        {
            VisorTxt.Text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ListarSecretosBtn_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            VisorTxt.Text = await Task.Run(() => _service.RefreshSecrets());
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
            VisorTxt.Text = _service.LastRawOutput;
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
        var cn = BuildConnectionData();
        SetBusy(true);
        try
        {
            await Task.Run(() => _service.SetSecrets(cn));
            VisorTxt.Text = _service.LastRawOutput;
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

    private void DataBaseCmb_SelectedIndexChanged(object? sender, EventArgs e)
        => PrevisualizarSecreto();

    private async void RefreshBtn_Click(object? sender, EventArgs e)
        => await RefreshDBDataAsync(ServerNameTxt.Text, UserTxt.Text, PasswordTxt.Text);

    private async Task PrevisualizarDatosAsync()
    {
        if (!_service.Secrets.TryGetValue(Constantes.SecretKeys.Development, out var devConn)) return;

        var parsed = ParseConnectionString(devConn);

        ServerNameTxt.Text = parsed.GetValueOrDefault("Server");
        UserTxt.Text = parsed.GetValueOrDefault("User Id");
        PasswordTxt.Text = parsed.GetValueOrDefault("Password");

        await RefreshDBDataAsync(ServerNameTxt.Text, UserTxt.Text, PasswordTxt.Text);
        DataBaseCmb.Text = parsed.GetValueOrDefault("Database");
    }

    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            result[kv[0].Trim()] = kv[1].Trim();
        }
        return result;
    }

    private async Task RefreshDBDataAsync(string serverName, string user, string pass)
    {
        try
        {
            var databases = await Task.Run(() => SQLService.GetDatabases(new SQLService.ConnectionData(
                server: serverName,
                user: user,
                password: pass)));
            DataBaseCmb.DataSource = databases;
        }
        catch (Exception ex)
        {
            DataBaseCmb.DataSource = null;
            VisorTxt.Text = $"Error consultando bases de datos: {ex.Message}";
        }
    }

    private void PrevisualizarSecreto()
    {
        var cn = BuildConnectionData();
        VisualizadorTxt.Text =
            $"{Constantes.GetSecretKey(Constantes.SecretKeys.Development)} = {Constantes.GetConnectionString(cn)}" + Environment.NewLine +
            $"{Constantes.GetSecretKey(Constantes.SecretKeys.DAPR)} = {Constantes.GetConnectionString(cn, Constantes.SecretKeys.DAPR)}";
    }

    private SQLService.ConnectionData BuildConnectionData()
        => new(
            server: ServerNameTxt.Text,
            dataBase: DataBaseCmb.Text,
            user: UserTxt.Text,
            password: PasswordTxt.Text);

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        ListarSecretosBtn.Enabled = !busy;
        RestaurarSecretosBtn.Enabled = !busy;
        ModificarSecretoBtn.Enabled = !busy;
        RefreshBtn.Enabled = !busy;
    }
}
